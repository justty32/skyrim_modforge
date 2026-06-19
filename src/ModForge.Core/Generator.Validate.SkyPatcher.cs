using System;
using System.Collections.Generic;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // SkyPatcher record-type folders (sub_projs/mod-survey/findings/skypatcher-records-and-config.md).
        private static readonly HashSet<string> SkyPatcherRecordTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "npc", "armor", "weapon", "ammo", "leveledList", "formList", "race", "container",
        };

        // SkyPatcher .ini loose-file specs. Structural-only (no esp record); SkyPatcher resolves the
        // filter/mod form refs against the player's load order at runtime — not verifiable offline.
        public void ValidateSkyPatchers()
        {
            foreach (var s in spec.SkyPatchers)
            {
                var who = $"skyPatcher '{s.File}'";
                if (string.IsNullOrWhiteSpace(s.File))
                    Problems.Add("skyPatcher has empty 'file' name");
                if (!SkyPatcherRecordTypes.Contains(s.RecordType ?? ""))
                    Problems.Add($"{who} has unknown recordType '{s.RecordType}' ({string.Join(" | ", SkyPatcherRecordTypes)})");
                if (s.Patches.Count == 0)
                    Problems.Add($"{who} has no patches");
                foreach (var p in s.Patches)
                {
                    if (p.Mods.Count == 0)
                        Problems.Add($"{who} has a patch line with no mods (nothing to change)");
                    foreach (var f in p.Filters)
                        if (string.IsNullOrWhiteSpace(f.Key)) Problems.Add($"{who} has a filter with an empty key");
                    foreach (var m in p.Mods)
                        if (string.IsNullOrWhiteSpace(m.Key)) Problems.Add($"{who} has a mod with an empty key");
                }
            }
        }
    }
}
