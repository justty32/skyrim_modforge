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
    }
}
