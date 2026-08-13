using System;
using System.Collections.Generic;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // SPID distribution types (sub_projs/mod-survey/findings/spid.md §三). SleepOutfit/Skin must be
        // explicit; Item/Package carry a 6th-field param (count / package index).
        private static readonly HashSet<string> SpidTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Spell", "Perk", "Item", "Shout", "LevSpell", "Package",
            "Outfit", "SleepOutfit", "Keyword", "DeathItem", "Faction", "Skin",
        };

        // SPID _DISTR.ini loose-file specs. Structural-only checks (no esp record); SPID resolves the
        // RecordID/EditorID at the player's load order — ModForge can't verify those offline.
        public void ValidateSpidDistributions()
        {
            foreach (var s in spec.SpidDistributions)
            {
                if (string.IsNullOrWhiteSpace(s.File))
                    Problems.Add("spidDistribution has empty 'file' name");
                foreach (var e in s.Entries)
                {
                    var who = $"spidDistribution '{s.File}'";
                    if (!SpidTypes.Contains(e.Type))
                        Problems.Add($"{who} entry has unknown type '{e.Type}' ({string.Join(" | ", SpidTypes)})");
                    if (string.IsNullOrWhiteSpace(e.Record))
                        Problems.Add($"{who} '{e.Type}' entry has empty 'record' (RecordID is required, cannot be NONE)");
                    if (e.Chance is int ch && (ch < 0 || ch > 100))
                        Problems.Add($"{who} '{e.Type}' chance {ch} out of range (0–100)");
                }
            }
        }
    }
}
