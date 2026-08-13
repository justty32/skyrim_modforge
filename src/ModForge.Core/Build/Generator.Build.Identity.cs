namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 1: the holding FACTION for each identity ---
        // An identity's persistent "has it" signal is a faction the player is added to. If `faction` is
        // a bare in-spec editorId that nothing else declares, build a plain FACT for it here; if it's an
        // external ref (a vanilla / Sofia faction like the Thieves Guild) or already a factions[] entry,
        // leave it alone. Storing the signal as a faction future-proofs vanilla GetInFaction gating.
        public void BuildIdentities()
        {
            var have = new HashSet<string>(spec.Factions.Select(f => f.EditorId), StringComparer.OrdinalIgnoreCase);
            foreach (var idn in spec.Identities)
            {
                if (string.IsNullOrWhiteSpace(idn.Faction)) continue;
                if (LooksExternalRef(idn.Faction) || have.Contains(idn.Faction)) continue;
                var r = mod.Factions.AddNew();
                r.EditorID = idn.Faction;
                r.Name = idn.Id;
                have.Add(idn.Faction);   // two identities may share a faction — build it once
            }
        }

        // --- pass 2: expand identity / primaryIdentity dialogue tags into player-faction CTDA specs ---
        // Returns ConditionSpecs (run through the shared BuildCondition by the caller). `identity` → the
        // PLAYER is in that identity's faction (GetInFaction ≥ 1). `primaryIdentity` → that PLUS the
        // player is NOT in any HIGHER-priority identity's faction (GetInFaction == 0), so only the top
        // held identity's greeting fires. Unknown ids warn and contribute nothing.

        public List<ConditionSpec> ExpandIdentityConditions(string identity, string primaryIdentity, string label)
        {
            var outc = new List<ConditionSpec>();
            static ConditionSpec InFaction(string fac, string cmp, float val) => new()
            {
                Function = "GetInFaction", Param = fac, Comparison = cmp, Value = val,
                RunOn = "Reference", Reference = PlayerNpcBase,
            };
            void One(string id, bool primary)
            {
                if (string.IsNullOrWhiteSpace(id)) return;
                var idn = spec.Identities.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
                if (idn is null) { Warn($"  ! {label}: unknown identity '{id}'"); return; }
                outc.Add(InFaction(idn.Faction, ">=", 1));   // held (both gates)
                // primaryIdentity: the controller-resolved MF_PrimaryIdentity global == this identity's code.
                // (Phase-2 #4 — replaces the old higher-priority faction-exclusion chain with a single global
                // read, so a dialogue option can override which identity is "primary". The controller picks
                // override-if-held else highest-priority held.)
                if (primary)
                    outc.Add(new ConditionSpec
                    {
                        Function = "GetGlobalValue", Param = Generator.IdentityPrimaryGlobal,
                        Comparison = "==", Value = Generator.IdentityCode(spec, idn.Id),
                    });
                // activeWhen NARROWS the positive gate: the identity only counts while these pass. Each is
                // player-centric — default it to run on the player if the author didn't pin a runOn.
                foreach (var aw in idn.ActiveWhen)
                    outc.Add(OnPlayerByDefault(aw));
            }
            One(identity, false);
            One(primaryIdentity, true);
            return outc;
        }

        // An activeWhen condition describes the PLAYER (worn gear / skills / relationships), but a dialogue
        // INFO's default runOn is Subject (the speaker NPC). Pin it to the player unless the author chose a
        // NON-default runOn (anything other than the "Subject" default / empty). Returns a copy so the
        // author's spec object isn't mutated.
        private static ConditionSpec OnPlayerByDefault(ConditionSpec c)
        {
            var chosen = !string.IsNullOrWhiteSpace(c.RunOn)
                         && !string.Equals(c.RunOn, "Subject", StringComparison.OrdinalIgnoreCase);
            if (chosen) return c;
            return new ConditionSpec
            {
                Function = c.Function, Param = c.Param, Comparison = c.Comparison, Value = c.Value,
                ActorValue = c.ActorValue, ItemType = c.ItemType, Or = c.Or,
                RunOn = "Reference", Reference = PlayerNpcBase,
            };
        }

        // The reusable identity-acquire book script (a prebuilt .pex embedded in the CLI + shipped by
        // Package, like the dispatcher/controller). Attached to a book; OnRead joins/leaves the faction.
        internal const string IdentityBookScript = "MFIdentityBook";

        // --- pass 2: attach MFIdentityBook to each identity's acquire book + bind its properties ---
        // Unconditional (like AttachSceneController) — the prebuilt .pex ships with every package whose
        // identities declare an acquireBook. Binds: TheFaction (the held signal), GrantAbility (first
        // grant, optional), AcquireScene (optional onAcquire performance), Toggle.
        public void AttachIdentityBooks()
        {
            foreach (var idn in spec.Identities)
            {
                if (string.IsNullOrWhiteSpace(idn.AcquireBook)) continue;
                if (!recordsByEd.TryGetValue(idn.AcquireBook, out var rec) || rec is not Book book)
                { Warn($"  ! identity '{idn.Id}': acquireBook '{idn.AcquireBook}' is not an in-spec book"); continue; }

                var entry = new ScriptEntry { Name = IdentityBookScript, Flags = ScriptEntry.Flag.Local };
                AddObjProp(entry, "TheFaction", idn.Faction, $"identity '{idn.Id}' faction");
                if (idn.Grants.Count > 0) AddObjProp(entry, "GrantAbility", idn.Grants[0], $"identity '{idn.Id}' grant");
                if (idn.GrantPerks.Count > 0) AddObjProp(entry, "GrantPerk", idn.GrantPerks[0], $"identity '{idn.Id}' grantPerk");
                if (idn.OnAcquire is { Scene: var scn } && !string.IsNullOrWhiteSpace(scn))
                    AddObjProp(entry, "AcquireScene", scn, $"identity '{idn.Id}' onAcquire.scene");
                entry.Properties.Add(new ScriptBoolProperty { Name = "Toggle", Data = idn.Toggle, Flags = ScriptProperty.Flag.Edited });

                var vmad = book.VirtualMachineAdapter ?? new VirtualMachineAdapter();
                vmad.Scripts.Add(entry);
                book.VirtualMachineAdapter = vmad;
                scriptsAttached++;
            }
        }

        private void AddObjProp(ScriptEntry entry, string name, string @ref, string label)
        {
            var p = new ScriptObjectProperty { Name = name, Flags = ScriptProperty.Flag.Edited };
            if (TryResolveRef(@ref, formKeyByEd, out var fk)) p.Object.SetTo(fk);
            else Warn($"  ! {label}: ref '{@ref}' unresolved");
            entry.Properties.Add(p);
        }

        // The reusable default-identity granter (a prebuilt .pex; same embed/ship model as the book).
        // Attached to a StartGameEnabled quest; OnInit adds the player to every default identity's faction.
        internal const string IdentityDefaultScript = "MFIdentityDefault";

        // --- pass 2: a StartGameEnabled quest that auto-grants every `default:true` identity on game start ---
        // The MVP's Adventurer baseline: a player should hold the default identity from the first load with
        // no book to read. We create one host quest carrying MFIdentityDefault (extends Quest); its OnInit
        // adds the player to each default identity's faction + grants its standing abilities. The quest is
        // StartGameEnabled so it also fires on existing saves (it lands in the generated .seq). Runs after
        // the formKey table exists so faction/grant refs resolve. No-op when no identity is `default`.
        public void BuildDefaultIdentityQuest()
        {
            var defaults = spec.Identities
                .Where(i => i.Default && !string.IsNullOrWhiteSpace(i.Faction))
                .ToList();
            if (defaults.Count == 0) return;

            var quest = mod.Quests.AddNew();
            quest.EditorID = "MF_IdentityDefaultQuest";
            quest.Name = "ModForge Default Identity";
            quest.Flags |= Quest.Flag.StartGameEnabled;

            var entry = new ScriptEntry { Name = IdentityDefaultScript, Flags = ScriptEntry.Flag.Local };
            entry.Properties.Add(ObjListProp("Factions", defaults.Select(i => i.Faction), "default identity faction"));
            var grants = defaults.SelectMany(i => i.Grants).Where(g => !string.IsNullOrWhiteSpace(g)).Distinct().ToList();
            if (grants.Count > 0)
                entry.Properties.Add(ObjListProp("Grants", grants, "default identity grant"));
            var perks = defaults.SelectMany(i => i.GrantPerks).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
            if (perks.Count > 0)
                entry.Properties.Add(ObjListProp("Perks", perks, "default identity grantPerk"));

            var qad = new QuestAdapter { Version = 5, ObjectFormat = 2 };
            qad.Scripts.Add(entry);
            quest.VirtualMachineAdapter = qad;
            scriptsAttached++;
        }

        private ScriptObjectListProperty ObjListProp(string name, IEnumerable<string> refs, string label)
        {
            var list = new ScriptObjectListProperty { Name = name, Flags = ScriptProperty.Flag.Edited };
            foreach (var @ref in refs)
            {
                var p = new ScriptObjectProperty();
                if (TryResolveRef(@ref, formKeyByEd, out var fk)) p.Object.SetTo(fk);
                else Warn($"  ! {label}: ref '{@ref}' unresolved");
                list.Objects.Add(p);
            }
            return list;
        }

        private static ScriptIntListProperty IntListProp(string name, IEnumerable<int> vals)
        {
            var list = new ScriptIntListProperty { Name = name, Flags = ScriptProperty.Flag.Edited };
            foreach (var v in vals) list.Data.Add(v);
            return list;
        }

        private static ScriptStringListProperty StrListProp(string name, IEnumerable<string> vals)
        {
            var list = new ScriptStringListProperty { Name = name, Flags = ScriptProperty.Flag.Edited };
            foreach (var v in vals) list.Data.Add(v);
            return list;
        }

        private static ScriptFloatListProperty FloatListProp(string name, IEnumerable<float> vals)
        {
            var list = new ScriptFloatListProperty { Name = name, Flags = ScriptProperty.Flag.Edited };
            foreach (var v in vals) list.Data.Add(v);
            return list;
        }

        internal const string IdentityAutoGrantScript = "MFIdentityAutoGrant";

        // --- pass 2: the auto-grant trigger quest (StartGameEnabled, MFIdentityAutoGrant) ---
        // For each identity with `autoGrantWhen`, the controller joins the player to its faction once
        // GetActorValue(name) >= threshold (e.g. Dragonborn on DragonSouls >= 1). Factions[]/AvNames[]/
        // Thresholds[] are parallel; the faction signal is granted (greetings/gates follow). No autoGrant → not built.
        public void BuildIdentityAutoGrantQuest()
        {
            var auto = spec.Identities
                .Where(i => i.AutoGrantWhen is { } a && !string.IsNullOrWhiteSpace(a.ActorValue) && !string.IsNullOrWhiteSpace(i.Faction))
                .ToList();
            if (auto.Count == 0) return;

            var quest = mod.Quests.AddNew();
            quest.EditorID = "MF_IdentityAutoGrantQuest";
            quest.Name = "ModForge Identity Auto-Grant";
            quest.Flags |= Quest.Flag.StartGameEnabled;

            var entry = new ScriptEntry { Name = IdentityAutoGrantScript, Flags = ScriptEntry.Flag.Local };
            entry.Properties.Add(ObjListProp("Factions", auto.Select(i => i.Faction), "auto-grant identity faction"));
            entry.Properties.Add(StrListProp("AvNames", auto.Select(i => i.AutoGrantWhen!.ActorValue)));
            entry.Properties.Add(FloatListProp("Thresholds", auto.Select(i => i.AutoGrantWhen!.Threshold)));

            var qad = new QuestAdapter { Version = 5, ObjectFormat = 2 };
            qad.Scripts.Add(entry);
            quest.VirtualMachineAdapter = qad;
            scriptsAttached++;
        }

        // True when a controller + the two primary-identity globals are needed: some dialogue gates on
        // primaryIdentity (reads MF_PrimaryIdentity) or sets the override (writes MF_IdentityOverride).
        private bool IdentityControllerNeeded() =>
            spec.Identities.Count > 0 && spec.Dialogue.Any(d =>
                !string.IsNullOrWhiteSpace(d.PrimaryIdentity) || !string.IsNullOrWhiteSpace(d.SetPrimaryIdentity));

        // --- pass 1: auto-build the two primary-identity globals (Phase-2 #4 controller) ---
        // MF_PrimaryIdentity (controller-written, greeting-read) + MF_IdentityOverride (dialogue-written,
        // controller-read). Built in pass 1 so primaryIdentity CTDA + the override fragment resolve them by
        // editorId. An author who already declared a global of the same name keeps theirs.
        public void BuildIdentityGlobals()
        {
            if (!IdentityControllerNeeded()) return;
            var have = new HashSet<string>(spec.Globals.Select(g => g.EditorId), StringComparer.OrdinalIgnoreCase);
            foreach (var ed in new[] { Generator.IdentityPrimaryGlobal, Generator.IdentityOverrideGlobal })
            {
                if (!have.Add(ed)) continue;
                MakeGlobalShort(0).EditorID = ed;
            }
        }

        // --- pass 2: the primary-identity controller quest (StartGameEnabled, MFIdentityController) ---
        // Maintains MF_PrimaryIdentity = override (if held) else highest-priority held identity. Greetings
        // read it (single GetGlobalValue == code CTDA), which also lets a dialogue option override the
        // primary. Factions[]/Codes[] are parallel, sorted by priority DESC; code = 1-based spec index.
        public void BuildIdentityControllerQuest()
        {
            if (!IdentityControllerNeeded()) return;
            var ordered = spec.Identities
                .Select((idn, i) => (idn, code: i + 1))
                .Where(x => !string.IsNullOrWhiteSpace(x.idn.Faction))
                .OrderByDescending(x => x.idn.Priority)
                .ToList();
            if (ordered.Count == 0) return;

            var quest = mod.Quests.AddNew();
            quest.EditorID = "MF_IdentityControllerQuest";
            quest.Name = "ModForge Identity Controller";
            quest.Flags |= Quest.Flag.StartGameEnabled;

            var entry = new ScriptEntry { Name = Generator.IdentityController, Flags = ScriptEntry.Flag.Local };
            AddObjProp(entry, "Primary", Generator.IdentityPrimaryGlobal, "identity controller Primary global");
            AddObjProp(entry, "Override", Generator.IdentityOverrideGlobal, "identity controller Override global");
            entry.Properties.Add(ObjListProp("Factions", ordered.Select(x => x.idn.Faction), "identity controller faction"));
            entry.Properties.Add(IntListProp("Codes", ordered.Select(x => x.code)));

            var qad = new QuestAdapter { Version = 5, ObjectFormat = 2 };
            qad.Scripts.Add(entry);
            quest.VirtualMachineAdapter = qad;
            scriptsAttached++;
        }
    }
}
