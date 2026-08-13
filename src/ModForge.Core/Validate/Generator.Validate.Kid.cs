using System;
using System.Collections.Generic;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // KID record types (sub_projs/mod-survey/findings/keyword-item-distributor-config-1.md §欄位二).
        // Note the multi-word names ("Magic Effect", "Misc Item", "Soul Gem", "Talking Activator").
        private static readonly HashSet<string> KidTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Weapon", "Armor", "Ammo", "Magic Effect", "Potion", "Scroll", "Location",
            "Ingredient", "Book", "Misc Item", "Key", "Soul Gem", "Spell", "Activator",
            "Flora", "Furniture", "Race", "Talking Activator", "Enchantment",
        };

        // KID _KID.ini loose-file specs. Structural-only (no esp record); KID resolves the keyword /
        // candidate records against the player's load order at runtime — not verifiable offline.
        public void ValidateKidDistributions()
        {
            foreach (var s in spec.KidDistributions)
            {
                if (string.IsNullOrWhiteSpace(s.File))
                    Problems.Add("kidDistribution has empty 'file' name");
                foreach (var e in s.Entries)
                {
                    var who = $"kidDistribution '{s.File}'";
                    if (string.IsNullOrWhiteSpace(e.Keyword))
                        Problems.Add($"{who} entry has empty 'keyword' (field 1 is required)");
                    if (!KidTypes.Contains(e.Type ?? ""))
                        Problems.Add($"{who} entry '{e.Keyword}' has unknown type '{e.Type}' ({string.Join(" | ", KidTypes)})");
                    if (e.Chance is double ch && (ch < 0 || ch > 100))
                        Problems.Add($"{who} entry '{e.Keyword}' chance {ch} out of range (0–100)");
                }
            }
        }
    }
}
