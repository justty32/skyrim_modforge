using System;
using System.Collections.Generic;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        private static readonly HashSet<string> OarConditionNames = new(StringComparer.OrdinalIgnoreCase)
        { "IsActorBase", "IsEquippedType", "IsFemale", "IsRace", "Random", "CompareValues", "AND", "OR" };
        private static readonly HashSet<string> BdiTypes = new(StringComparer.OrdinalIgnoreCase)
        { "kInt", "kBool", "kFloat", "kEvent" };

        // OAR replacer / BDI / PIE loose-file specs. (.hkx existence is checked in `package`, which
        // has the spec/assets dir — Validate only sees the ModSpec.)
        public void ValidateAnimationReplacers()
        {
            foreach (var r in spec.AnimationReplacers)
            {
                if (string.IsNullOrWhiteSpace(r.Mod))
                    Problems.Add("animationReplacer has empty 'mod' name");
                foreach (var s in r.Submods)
                {
                    var who = $"animationReplacer '{r.Mod}' submod '{s.Name}'";
                    if (string.IsNullOrWhiteSpace(s.Name))
                        Problems.Add($"animationReplacer '{r.Mod}' has a submod with empty name");
                    if (!s.ReplaceVanillaPath && s.Priority <= 0)
                        Problems.Add($"{who} priority must be > 0 (got {s.Priority})");
                    if (s.Hkx.Count == 0 && s.Variants.Count == 0)
                        Problems.Add($"{who} ships no .hkx (hkx/variants both empty)");

                    if (s.NpcMoveset is { } m)
                    {
                        CheckWeapon(m.RightWeapon, $"{who} npcMoveset.rightWeapon");
                        CheckWeapon(m.LeftWeapon, $"{who} npcMoveset.leftWeapon");
                        if (!string.IsNullOrWhiteSpace(m.Race)) CheckForm(m.Race, $"{who} npcMoveset.race");
                        if (m.RandomPick is float rp && (rp < 0 || rp > 1))
                            Problems.Add($"{who} npcMoveset.randomPick {rp} out of range (0–1)");
                    }
                    foreach (var c in s.Conditions) CheckOarCondition(c, who);
                }
            }

            foreach (var b in spec.BehaviorData)
            {
                if (string.IsNullOrWhiteSpace(b.File))
                    Problems.Add("behaviorData has empty 'file' name");
                foreach (var e in b.Entries)
                {
                    if (!BdiTypes.Contains(e.Type))
                        Problems.Add($"behaviorData '{b.File}' entry '{e.Name}' unknown type '{e.Type}' (kInt | kBool | kFloat | kEvent)");
                    if (string.IsNullOrWhiteSpace(e.Name))
                        Problems.Add($"behaviorData '{b.File}' has an entry with empty name");
                }
            }

            foreach (var p in spec.PayloadMacros)
            {
                if (string.IsNullOrWhiteSpace(p.File))
                    Problems.Add("payloadMacro has empty 'file' name");
                foreach (var macro in p.Macros)
                    if (string.IsNullOrWhiteSpace(macro.Name) || string.IsNullOrWhiteSpace(macro.Command))
                        Problems.Add($"payloadMacro '{p.File}' has a macro with empty name/command");
            }
        }

        void CheckOarCondition(OarConditionSpec c, string who)
        {
            if (!OarConditionNames.Contains(c.Condition))
            {
                Problems.Add($"{who} unknown OAR condition '{c.Condition}' ({string.Join(" | ", OarConditionNames)})");
                return;
            }
            switch (c.Condition.ToUpperInvariant())
            {
                case "AND":
                case "OR":
                    if (c.Conditions.Count == 0) Problems.Add($"{who} {c.Condition} container has no child conditions");
                    foreach (var sub in c.Conditions) CheckOarCondition(sub, who);
                    break;
                case "ISACTORBASE":
                case "ISRACE":
                    CheckForm(c.Form, $"{who} {c.Condition}.form");
                    break;
            }
        }

        void CheckWeapon(string name, string who)
        {
            try { OarConditions.WeaponType(name); }
            catch (ArgumentException ex) { Problems.Add($"{who}: {ex.Message}"); }
        }

        void CheckForm(string form, string who)
        {
            try { OarConditions.ParseForm(form); }
            catch (ArgumentException ex) { Problems.Add($"{who}: {ex.Message}"); }
        }
    }
}
