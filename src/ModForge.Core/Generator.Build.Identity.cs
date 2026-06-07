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
    }
}
