using System;
using System.Collections.Generic;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        private static readonly HashSet<string> McmControlTypes = new(StringComparer.OrdinalIgnoreCase)
        { "toggle", "hiddenToggle", "slider", "stepper", "enum", "keymap", "header", "empty" };

        private static readonly HashSet<string> McmSourceTypes = new(StringComparer.OrdinalIgnoreCase)
        { "ModSettingBool", "ModSettingInt", "ModSettingFloat", "ModSettingString" };

        // MCM Helper config.json/settings.ini loose-file specs. Structural-only (no esp record).
        // MVP rejects PropertyValue* sourceTypes / action.CallFunction (need a Quest+Papyrus host).
        public void ValidateMcmConfigs()
        {
            foreach (var m in spec.McmConfigs)
            {
                var who = $"mcm '{m.ModName}'";
                if (string.IsNullOrWhiteSpace(m.ModName))
                    Problems.Add("mcm config has empty 'modName' (it names the MCM/Config/<modName>/ folder)");
                if (m.Pages.Count == 0)
                    Problems.Add($"{who} has no pages");

                foreach (var p in m.Pages)
                {
                    if (string.IsNullOrWhiteSpace(p.Name))
                        Problems.Add($"{who} has a page with empty 'name'");
                    if (!string.IsNullOrWhiteSpace(p.CursorFillMode)
                        && p.CursorFillMode is not ("topToBottom" or "leftToRight"))
                        Problems.Add($"{who} page '{p.Name}' cursorFillMode '{p.CursorFillMode}' invalid (topToBottom | leftToRight)");

                    foreach (var c in p.Content)
                    {
                        var cw = $"{who} page '{p.Name}' control";
                        if (!McmControlTypes.Contains(c.Type ?? ""))
                            { Problems.Add($"{cw} has unknown type '{c.Type}' ({string.Join(" | ", McmControlTypes)})"); continue; }

                        bool valueType = c.Type is not ("header" or "empty");
                        if (!string.IsNullOrEmpty(c.SourceType) && !McmSourceTypes.Contains(c.SourceType))
                            Problems.Add($"{cw} '{c.Type}' sourceType '{c.SourceType}' unsupported "
                                + $"(MVP is ini-backed: {string.Join(" | ", McmSourceTypes)}; PropertyValue*/action need a Quest script — out of scope)");

                        // A value control with a sourceType is ini-backed → needs a "key:Section" id.
                        if (valueType && !string.IsNullOrEmpty(c.SourceType))
                        {
                            var (key, section) = McmGen.SplitId(c.Id);
                            if (key.Length == 0 || section.Length == 0)
                                Problems.Add($"{cw} '{c.Type}' has sourceType but a malformed id '{c.Id}' (needs \"key:Section\")");
                        }

                        if (string.Equals(c.Type, "slider", StringComparison.OrdinalIgnoreCase)
                            && (c.Min is null || c.Max is null))
                            Problems.Add($"{cw} slider '{c.Id}' needs both min and max");
                        if ((string.Equals(c.Type, "stepper", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(c.Type, "enum", StringComparison.OrdinalIgnoreCase))
                            && c.Options.Count == 0)
                            Problems.Add($"{cw} {c.Type} '{c.Id}' needs an options list");
                        if (!string.IsNullOrWhiteSpace(c.GroupBehavior)
                            && c.GroupBehavior is not ("disable" or "skip"))
                            Problems.Add($"{cw} '{c.Id}' groupBehavior '{c.GroupBehavior}' invalid (disable | skip)");
                    }
                }
            }
        }
    }
}
