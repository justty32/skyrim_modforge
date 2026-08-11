using System;
using System.Collections.Generic;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        private static readonly HashSet<string> OarConditionNames = new(StringComparer.OrdinalIgnoreCase)
        { "IsActorBase", "IsEquippedType", "IsFemale", "IsRace", "Random", "CompareValues", "PRESET", "AND", "OR" };
        private static readonly HashSet<string> OarFunctionNames = new(StringComparer.Ordinal)
        { "CONDITION", "RANDOM", "ONE", "PlaySound" };
        private static readonly HashSet<string> OarVariantModes = new(StringComparer.OrdinalIgnoreCase)
        { "random", "sequential" };
        private static readonly HashSet<string> OarVariantStateScopes = new(StringComparer.OrdinalIgnoreCase)
        { "local", "submod", "replacerMod", "reference" };
        private static readonly HashSet<string> BdiTypes = new(StringComparer.OrdinalIgnoreCase)
        { "kInt", "kBool", "kFloat", "kEvent" };

        // OAR replacer / BDI / PIE loose-file specs. (.hkx existence is checked in `package`, which
        // has the spec/assets dir — Validate only sees the ModSpec.)
        public void ValidateAnimationReplacers()
        {
            var replacerModNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in spec.AnimationReplacers)
            {
                if (string.IsNullOrWhiteSpace(r.Mod))
                    Problems.Add("animationReplacer has empty 'mod' name");
                else if (!replacerModNames.Add(r.Mod))
                    Problems.Add($"animationReplacer has duplicate mod name '{r.Mod}'");
                var presetNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var preset in r.ConditionPresets)
                {
                    if (string.IsNullOrWhiteSpace(preset.Name)) Problems.Add($"animationReplacer '{r.Mod}' has a conditionPreset with empty name");
                    else if (!presetNames.Add(preset.Name)) Problems.Add($"animationReplacer '{r.Mod}' has duplicate conditionPreset '{preset.Name}'");
                }
                foreach (var preset in r.ConditionPresets)
                {
                    var who = $"animationReplacer '{r.Mod}' conditionPreset '{preset.Name}'";
                    if (preset.Conditions.Count == 0) Problems.Add($"{who} has no conditions");
                    foreach (var c in preset.Conditions) CheckOarCondition(c, who, presetNames, checkPresetReference: true);
                }

                var submodNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var s in r.Submods)
                {
                    var who = $"animationReplacer '{r.Mod}' submod '{s.Name}'";
                    if (string.IsNullOrWhiteSpace(s.Name))
                        Problems.Add($"animationReplacer '{r.Mod}' has a submod with empty name");
                    else if (!submodNames.Add(s.Name))
                        Problems.Add($"animationReplacer '{r.Mod}' has duplicate submod '{s.Name}'");
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
                    foreach (var c in s.Conditions) CheckOarCondition(c, who, presetNames, checkPresetReference: true);
                    ValidateReplacementAnimations(s, who);
                    ValidateFunctionSet(s.FunctionsOnActivate, $"{who} functionsOnActivate", presetNames, triggersRequired: false);
                    ValidateFunctionSet(s.FunctionsOnDeactivate, $"{who} functionsOnDeactivate", presetNames, triggersRequired: false);
                    ValidateFunctionSet(s.FunctionsOnTrigger, $"{who} functionsOnTrigger", presetNames, triggersRequired: true);
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

        void CheckOarCondition(OarConditionSpec c, string who, HashSet<string> presetNames, bool checkPresetReference)
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
                    foreach (var sub in c.Conditions) CheckOarCondition(sub, who, presetNames, checkPresetReference);
                    break;
                case "ISACTORBASE":
                case "ISRACE":
                    CheckForm(c.Form, $"{who} {c.Condition}.form");
                    break;
                case "PRESET":
                    if (string.IsNullOrWhiteSpace(c.Preset)) Problems.Add($"{who} PRESET has empty preset reference");
                    else if (checkPresetReference && !presetNames.Contains(c.Preset)) Problems.Add($"{who} PRESET references unknown conditionPreset '{c.Preset}'");
                    break;
            }
        }

        void ValidateReplacementAnimations(OarSubmodSpec s, string who)
        {
            foreach (var animation in s.ReplacementAnimations)
            {
                var animationWho = $"{who} replacementAnimation '{animation.Path}'";
                if (string.IsNullOrWhiteSpace(animation.ProjectName)) Problems.Add($"{animationWho} has empty projectName");
                if (string.IsNullOrWhiteSpace(animation.Path)) Problems.Add($"{animationWho} has empty path");
                if (!OarVariantModes.Contains(animation.VariantMode)) Problems.Add($"{animationWho} unknown variantMode '{animation.VariantMode}' (random | sequential)");
                if (!OarVariantStateScopes.Contains(animation.VariantStateScope)) Problems.Add($"{animationWho} unknown variantStateScope '{animation.VariantStateScope}' (local | submod | replacerMod | reference)");
                var filenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var variant in animation.Variants)
                {
                    if (string.IsNullOrWhiteSpace(variant.Filename)) Problems.Add($"{animationWho} has variant metadata with empty filename");
                    else if (!filenames.Add(variant.Filename)) Problems.Add($"{animationWho} has duplicate variant filename '{variant.Filename}'");
                    else if (!s.Variants.Select((_, index) => $"{index + 1}.hkx").Contains(variant.Filename, StringComparer.OrdinalIgnoreCase))
                        Problems.Add($"{animationWho} variant filename '{variant.Filename}' does not reference a generated variants[] file");
                    if (!float.IsFinite(variant.Weight) || variant.Weight <= 0) Problems.Add($"{animationWho} variant '{variant.Filename}' weight must be finite and > 0 (got {variant.Weight})");
                }
            }
        }

        void ValidateFunctionSet(IEnumerable<OarFunctionSpec> functions, string who, HashSet<string> presetNames, bool triggersRequired)
        {
            foreach (var function in functions)
            {
                if (!OarFunctionNames.Contains(function.Function))
                {
                    Problems.Add($"{who} has unsupported OAR function '{function.Function}'");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(function.RequiredVersion)) Problems.Add($"{who} function '{function.Function}' has empty requiredVersion");
                if (triggersRequired && function.Triggers.Count == 0) Problems.Add($"{who} function '{function.Function}' needs at least one trigger");
                foreach (var trigger in function.Triggers)
                    if (string.IsNullOrWhiteSpace(trigger.Event)) Problems.Add($"{who} function '{function.Function}' has trigger with empty event");

                switch (function.Function)
                {
                    case "CONDITION":
                        if (function.Conditions.Count == 0 || function.Functions.Count == 0) Problems.Add($"{who} CONDITION needs conditions and functions");
                        foreach (var condition in function.Conditions) CheckOarCondition(condition, $"{who} CONDITION", presetNames, checkPresetReference: true);
                        ValidateFunctionSet(function.Functions, $"{who} CONDITION", presetNames, triggersRequired: false);
                        break;
                    case "RANDOM":
                        if (function.Functions.Count == 0) Problems.Add($"{who} RANDOM needs functions");
                        if (function.Weights.Count > 0 && function.Weights.Count != function.Functions.Count) Problems.Add($"{who} RANDOM weights count must match functions count");
                        foreach (var weight in function.Weights)
                            if (!float.IsFinite(weight) || weight <= 0) Problems.Add($"{who} RANDOM weight must be finite and > 0 (got {weight})");
                        ValidateFunctionSet(function.Functions, $"{who} RANDOM", presetNames, triggersRequired: false);
                        break;
                    case "ONE":
                        if (function.Functions.Count == 0) Problems.Add($"{who} ONE needs functions");
                        ValidateFunctionSet(function.Functions, $"{who} ONE", presetNames, triggersRequired: false);
                        break;
                    case "PlaySound":
                        CheckForm(function.SoundForm, $"{who} PlaySound.soundForm");
                        break;
                }
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
