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

        // MCM Helper config.json/settings.ini specs. `global` is the supported high-level
        // CallFunction path; raw PropertyValue*/action authoring remains out of scope.
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

                        if (!string.IsNullOrWhiteSpace(c.Global))
                        {
                            if (!string.Equals(c.Type, "toggle", StringComparison.OrdinalIgnoreCase)
                                || !string.Equals(c.SourceType, "ModSettingBool", StringComparison.OrdinalIgnoreCase))
                                Problems.Add($"{cw} global binding requires type 'toggle' and sourceType 'ModSettingBool'");
                            CheckRef(c.Global, $"{cw} global");
                            var declaredGlobal = spec.Globals.FirstOrDefault(g =>
                                string.Equals(g.EditorId, c.Global, StringComparison.OrdinalIgnoreCase));
                            if (declaredGlobal?.Constant == true)
                                Problems.Add($"{cw} global '{c.Global}' is constant and cannot be changed by MCM");
                            if (declaredGlobal is not null && declaredGlobal.Value != (c.DefaultBool ? 1f : 0f))
                                Problems.Add($"{cw} defaultBool does not match global '{c.Global}' initial value "
                                    + $"(expected {(c.DefaultBool ? 1 : 0)})");
                        }

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
