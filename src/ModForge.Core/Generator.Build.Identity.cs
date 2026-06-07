namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
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
        private const string PlayerRef = "Skyrim.esm:0x000014";

        public List<ConditionSpec> ExpandIdentityConditions(string identity, string primaryIdentity, string label)
        {
            var outc = new List<ConditionSpec>();
            static ConditionSpec InFaction(string fac, string cmp, float val) => new()
            {
                Function = "GetInFaction", Param = fac, Comparison = cmp, Value = val,
                RunOn = "Reference", Reference = PlayerRef,
            };
            void One(string id, bool primary)
            {
                if (string.IsNullOrWhiteSpace(id)) return;
                var idn = spec.Identities.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
                if (idn is null) { Warn($"  ! {label}: unknown identity '{id}'"); return; }
                outc.Add(InFaction(idn.Faction, ">=", 1));
                if (primary)
                    foreach (var hi in spec.Identities.Where(x => x.Priority > idn.Priority && !string.IsNullOrWhiteSpace(x.Faction)))
                        outc.Add(InFaction(hi.Faction, "==", 0));
            }
            One(identity, false);
            One(primaryIdentity, true);
            return outc;
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
    }
}
