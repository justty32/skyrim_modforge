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
    }
}
