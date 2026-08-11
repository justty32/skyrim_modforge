using System.Linq;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class ValidateAnimationReplacerTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    [Fact]
    public void ValidSpec_NoProblems()
    {
        var s = new ModSpec
        {
            AnimationReplacers =
            {
                new AnimationReplacerSpec
                {
                    Mod = "M",
                    Submods =
                    {
                        new OarSubmodSpec
                        {
                            Name = "atk", Priority = 10, Hkx = { "a.hkx" },
                            NpcMoveset = new NpcMovesetSpec { RightWeapon = "sword", LeftWeapon = "shield", PlayerOnly = false },
                        },
                    },
                },
            },
            BehaviorData = { new BehaviorDataSpec { File = "x", Entries = { new BdiEntrySpec { Type = "kInt", Name = "v" } } } },
        };
        Assert.DoesNotContain(Validate(s),
            p => p.Contains("animationReplacer") || p.Contains("behaviorData"));
    }

    [Fact]
    public void BadWeaponName_Reported()
    {
        var s = new ModSpec
        {
            AnimationReplacers =
            {
                new AnimationReplacerSpec { Mod = "M", Submods = { new OarSubmodSpec {
                    Name = "a", Priority = 1, Hkx = { "a.hkx" },
                    NpcMoveset = new NpcMovesetSpec { RightWeapon = "lightsaber", LeftWeapon = "shield" } } } },
            },
        };
        Assert.Contains(Validate(s), p => p.Contains("rightWeapon") && p.Contains("lightsaber"));
    }

    [Fact]
    public void ZeroPriority_Reported()
    {
        var s = new ModSpec
        {
            AnimationReplacers = { new AnimationReplacerSpec { Mod = "M", Submods = {
                new OarSubmodSpec { Name = "a", Priority = 0, Hkx = { "a.hkx" } } } } },
        };
        Assert.Contains(Validate(s), p => p.Contains("priority must be > 0"));
    }

    [Fact]
    public void BadForm_Reported()
    {
        var s = new ModSpec
        {
            AnimationReplacers = { new AnimationReplacerSpec { Mod = "M", Submods = {
                new OarSubmodSpec { Name = "a", Priority = 1, Hkx = { "a.hkx" },
                    Conditions = { new OarConditionSpec { Condition = "IsActorBase", Form = "no-separator" } } } } } },
        };
        Assert.Contains(Validate(s), p => p.Contains("IsActorBase.form"));
    }

    [Fact]
    public void UnknownCondition_Reported()
    {
        var s = new ModSpec
        {
            AnimationReplacers = { new AnimationReplacerSpec { Mod = "M", Submods = {
                new OarSubmodSpec { Name = "a", Priority = 1, Hkx = { "a.hkx" },
                    Conditions = { new OarConditionSpec { Condition = "IsBananaEquipped" } } } } } },
        };
        Assert.Contains(Validate(s), p => p.Contains("unknown OAR condition"));
    }

    [Fact]
    public void BadBdiType_Reported()
    {
        var s = new ModSpec
        {
            BehaviorData = { new BehaviorDataSpec { File = "x", Entries = { new BdiEntrySpec { Type = "kDouble", Name = "v" } } } },
        };
        Assert.Contains(Validate(s), p => p.Contains("unknown type 'kDouble'"));
    }

    [Fact]
    public void EmptyAndContainer_Reported()
    {
        var s = new ModSpec
        {
            AnimationReplacers = { new AnimationReplacerSpec { Mod = "M", Submods = {
                new OarSubmodSpec { Name = "a", Priority = 1, Hkx = { "a.hkx" },
                    Conditions = { new OarConditionSpec { Condition = "AND" } } } } } },
        };
        Assert.Contains(Validate(s), p => p.Contains("container has no child conditions"));
    }

    [Fact]
    public void Oar22DuplicateNamesBadWeightsAndReferences_AreReported()
    {
        var s = new ModSpec
        {
            AnimationReplacers =
            {
                new AnimationReplacerSpec
                {
                    Mod = "M",
                    ConditionPresets = { new OarConditionPresetSpec { Name = "P", Conditions = { new OarConditionSpec { Condition = "IsFemale" } } }, new OarConditionPresetSpec { Name = "P", Conditions = { new OarConditionSpec { Condition = "IsFemale" } } } },
                    Submods =
                    {
                        new OarSubmodSpec
                        {
                            Name = "a", Priority = 1, Hkx = { "a.hkx" }, Variants = { "v.hkx" },
                            Conditions = { new OarConditionSpec { Condition = "PRESET", Preset = "Missing" } },
                            ReplacementAnimations = { new OarReplacementAnimationSpec { ProjectName = "P", Path = "x", Variants = { new OarVariantMetadataSpec { Filename = "missing.hkx", Weight = 0 } } } },
                            FunctionsOnTrigger = { new OarFunctionSpec { Function = "RANDOM", Weights = { 0 } } },
                        },
                        new OarSubmodSpec { Name = "a", Priority = 2, Hkx = { "b.hkx" } },
                    },
                },
                new AnimationReplacerSpec { Mod = "M" },
            },
        };

        var problems = Validate(s);
        Assert.Contains(problems, p => p.Contains("duplicate mod name"));
        Assert.Contains(problems, p => p.Contains("duplicate conditionPreset"));
        Assert.Contains(problems, p => p.Contains("duplicate submod"));
        Assert.Contains(problems, p => p.Contains("unknown conditionPreset 'Missing'"));
        Assert.Contains(problems, p => p.Contains("weight must be finite and > 0"));
        Assert.Contains(problems, p => p.Contains("does not reference a generated variants[] file"));
        Assert.Contains(problems, p => p.Contains("needs at least one trigger"));
    }
}
