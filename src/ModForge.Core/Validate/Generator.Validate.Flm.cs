using System;
using System.Collections.Generic;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // FLM Collection FormType set (sub_projs/mod-survey/findings/formlist-manipulator-config-advanced.md).
        private static readonly HashSet<string> FlmFormTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Armor", "Weapon", "Ammo", "MagicEffect", "AlchemyItem", "Scroll", "Location",
            "Ingredient", "Book", "Misc", "Key", "Soulgem", "Activator", "Flora", "Furniture",
            "Race", "TalkingActivator", "Enchantment", "NPC", "Spell",
        };

        // FormList Manipulator _FLM.ini loose-file specs. Structural-only (no esp record); FLM resolves
        // the target FLST / form refs against the player's load order at runtime — not verifiable offline.
        public void ValidateFormListInjects()
        {
            foreach (var s in spec.FormListInjects)
            {
                var who = $"formListInject '{s.File}'";
                if (string.IsNullOrWhiteSpace(s.File))
                    Problems.Add("formListInject has empty 'file' name");

                foreach (var f in s.Filters)
                {
                    if (string.IsNullOrWhiteSpace(f.Name)) Problems.Add($"{who} has a filter with empty 'name'");
                    if (f.Conditions.Count == 0) Problems.Add($"{who} filter '{f.Name}' has no conditions");
                }
                foreach (var a in s.Aliases)
                {
                    if (string.IsNullOrWhiteSpace(a.Name)) Problems.Add($"{who} has an alias with empty 'name'");
                    if (a.Items.Count == 0) Problems.Add($"{who} alias '{a.Name}' has no items (target FormLists)");
                }
                foreach (var g in s.Groups)
                {
                    if (string.IsNullOrWhiteSpace(g.Name)) Problems.Add($"{who} has a group with empty 'name'");
                    if (g.Items.Count == 0) Problems.Add($"{who} group '{g.Name}' has no items (forms)");
                }
                foreach (var c in s.Collections)
                {
                    if (string.IsNullOrWhiteSpace(c.Name)) Problems.Add($"{who} has a collection with empty 'name'");
                    if (!FlmFormTypes.Contains(c.FormType ?? ""))
                        Problems.Add($"{who} collection '{c.Name}' has unknown formType '{c.FormType}' ({string.Join(" | ", FlmFormTypes)})");
                }
                foreach (var e in s.Entries)
                {
                    if (string.IsNullOrWhiteSpace(e.Target))
                        Problems.Add($"{who} has a FormList entry with empty 'target' (the FormList to append to)");
                    if (e.Forms.Count == 0)
                        Problems.Add($"{who} FormList entry '{e.Target}' has no forms to add");
                }
            }
        }
    }
}
