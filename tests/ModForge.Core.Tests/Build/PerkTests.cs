using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

// Master-free regression tests for the PERK feature: flags, ability→spell link, entry-point
// EntryPoint/function/value, perk-level + effect-level conditions, NPC perk placement, and the
// validate guardrails. Build() needs no Skyrim.esm here — the spec uses no `template` clones and
// every external ref (Skyrim.esm:0x…) resolves to a bare FormKey without an overlay.
public class PerkTests
{
    private static readonly ModKey OutKey = ModKey.FromNameAndExtension("PerkTest.esp");

    // A spec exercising both effect kinds, conditions, and an NPC that carries the perks.
    private static ModSpec MakeSpec() => new()
    {
        PluginName = "PerkTest.esp",
        MagicEffects =
        {
            new MagicEffectSpec
            {
                EditorId = "TestAbilityEffect", Name = "Test Ability",
                Archetype = "ValueModifier", ActorValue = "DamageResist",
                CastType = "ConstantEffect", TargetType = "Self",
                Flags = { "Recover", "NoArea", "NoDuration" },
            },
        },
        Spells =
        {
            new SpellSpec
            {
                EditorId = "TestAbilitySpell", Name = "Test Ability",
                SpellType = "Ability", CastType = "ConstantEffect", TargetType = "Self",
                Effects = { new EffectSpec { MagicEffect = "TestAbilityEffect", Magnitude = 25 } },
            },
        },
        Perks =
        {
            new PerkSpec
            {
                EditorId = "TestAbilityPerk", Name = "Test Ability Perk",
                Description = "Grants an ability.", Playable = true, Hidden = false, NumRanks = 2,
                Effects = { new PerkEffectSpec { Kind = "ability", Spell = "TestAbilitySpell", Rank = 0, Priority = 1 } },
            },
            new PerkSpec
            {
                EditorId = "TestEntryPerk", Name = "Test Entry Perk",
                Playable = true, Hidden = true, NumRanks = 1,
                Conditions =
                {
                    new ConditionSpec { Function = "GetBaseActorValue", ActorValue = "OneHanded", Comparison = "GreaterThanOrEqualTo", Value = 30 },
                },
                Effects =
                {
                    new PerkEffectSpec
                    {
                        Kind = "entryPoint", EntryPoint = "ModAttackDamage", Function = "Multiply", Value = 1.2f,
                        Conditions = { new ConditionSpec { Function = "WornHasKeyword", Param = "Skyrim.esm:0x01E711", Comparison = "EqualTo", Value = 1 } },
                    },
                },
            },
        },
        Npcs =
        {
            new NpcSpec
            {
                EditorId = "TestPerkNpc", Name = "Test Guard", Race = "Skyrim.esm:0x013746",
                Perks = { "TestAbilityPerk", "TestEntryPerk" },
            },
        },
    };

    private static IPerkGetter Perk(ISkyrimMod mod, string ed) =>
        mod.Perks.First(p => p.EditorID == ed);

    [Fact]
    public void Validate_GoodSpec_NoProblems()
    {
        Assert.Empty(Generator.Validate(MakeSpec()));
    }

    [Fact]
    public void Build_EmitsTwoPerks_AndCountsThem()
    {
        var result = Generator.Build(MakeSpec(), OutKey);
        Assert.Equal(2, result.Stats.Perks);
        Assert.Equal(2, result.Mod.Perks.Count);
    }

    [Fact]
    public void AbilityPerk_FlagsAndRanks_AreSet()
    {
        var perk = Perk(Generator.Build(MakeSpec(), OutKey).Mod, "TestAbilityPerk");
        Assert.True(perk.Playable);
        Assert.False(perk.Hidden);
        Assert.Equal(2, perk.NumRanks);
    }

    [Fact]
    public void AbilityEffect_LinksToTheSpell()
    {
        var mod = Generator.Build(MakeSpec(), OutKey).Mod;
        var perk = Perk(mod, "TestAbilityPerk");
        var spell = mod.Spells.First(s => s.EditorID == "TestAbilitySpell");
        var eff = Assert.IsAssignableFrom<IPerkAbilityEffectGetter>(Assert.Single(perk.Effects));
        Assert.Equal(spell.FormKey, eff.Ability.FormKey);
        Assert.Equal(1, eff.Priority);
    }

    [Fact]
    public void EntryPointEffect_HasEntryPointFunctionAndValue()
    {
        var perk = Perk(Generator.Build(MakeSpec(), OutKey).Mod, "TestEntryPerk");
        Assert.True(perk.Hidden);
        var mv = Assert.IsAssignableFrom<IPerkEntryPointModifyValueGetter>(Assert.Single(perk.Effects));
        Assert.Equal(APerkEntryPointEffect.EntryType.ModAttackDamage, mv.EntryPoint);
        Assert.Equal(PerkEntryPointModifyValue.ModificationType.Multiply, mv.Modification);
        Assert.Equal(1.2f, mv.Value);
    }

    // Regression (in-game CTD root-caused 2026-05-31 via CrashLoggerSSE): the entry point's
    // PerkConditionTabCount must carry the vanilla-canonical count for that EntryType, never 0.
    // The engine sizes its per-tab condition array from this byte; leaving it 0 while a PRKC
    // condition tab is present overflows the array and hard-crashes during "Loading Files".
    // ModAttackDamage is canonically 3 tabs.
    [Fact]
    public void EntryPointEffect_SetsVanillaPerkConditionTabCount()
    {
        var perk = Perk(Generator.Build(MakeSpec(), OutKey).Mod, "TestEntryPerk");
        var mv = Assert.IsAssignableFrom<IPerkEntryPointModifyValueGetter>(Assert.Single(perk.Effects));
        Assert.Equal(3, mv.PerkConditionTabCount);
    }

    [Fact]
    public void PerkLevelCondition_IsWired()
    {
        var perk = Perk(Generator.Build(MakeSpec(), OutKey).Mod, "TestEntryPerk");
        var cond = Assert.Single(perk.Conditions);
        var data = Assert.IsAssignableFrom<IConditionFloatGetter>(cond).Data;
        var avData = Assert.IsAssignableFrom<IGetBaseActorValueConditionDataGetter>(data);
        Assert.Equal(ActorValue.OneHanded, avData.ActorValue);
        Assert.Equal(CompareOperator.GreaterThanOrEqualTo, ((IConditionFloatGetter)cond).CompareOperator);
        Assert.Equal(30f, ((IConditionFloatGetter)cond).ComparisonValue);
    }

    [Fact]
    public void EffectLevelCondition_IsWrappedInPerkCondition()
    {
        var perk = Perk(Generator.Build(MakeSpec(), OutKey).Mod, "TestEntryPerk");
        var mv = Assert.IsAssignableFrom<IPerkEntryPointModifyValueGetter>(Assert.Single(perk.Effects));
        var pcond = Assert.Single(mv.Conditions);
        var inner = Assert.Single(pcond.Conditions);
        var data = Assert.IsAssignableFrom<IConditionFloatGetter>(inner).Data;
        Assert.IsAssignableFrom<IWornHasKeywordConditionDataGetter>(data);
    }

    [Fact]
    public void Npc_CarriesBothPerks_AsPlacements()
    {
        var mod = Generator.Build(MakeSpec(), OutKey).Mod;
        var npc = mod.Npcs.First(n => n.EditorID == "TestPerkNpc");
        Assert.NotNull(npc.Perks);
        Assert.Equal(2, npc.Perks!.Count);
        // The ability perk has 2 ranks, so its placement carries rank 2.
        var abilityPerk = Perk(mod, "TestAbilityPerk");
        var placement = npc.Perks.First(p => p.Perk.FormKey == abilityPerk.FormKey);
        Assert.Equal(2, placement.Rank);
    }

    // --- validate guardrails ---

    [Fact]
    public void Validate_EmptyName_IsReported()
    {
        var spec = MakeSpec();
        spec.Perks[0].Name = "";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("empty name"));
    }

    [Fact]
    public void Validate_AbilitySpellRefMustResolve()
    {
        var spec = MakeSpec();
        spec.Perks[0].Effects[0].Spell = "NoSuchSpell";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("ability effect spell"));
    }

    [Fact]
    public void Validate_UnknownEntryPoint_IsReported()
    {
        var spec = MakeSpec();
        spec.Perks[1].Effects[0].EntryPoint = "NotARealEntryPoint";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("unknown entryPoint"));
    }

    [Fact]
    public void Validate_BadFunction_IsReported()
    {
        var spec = MakeSpec();
        spec.Perks[1].Effects[0].Function = "Divide";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("invalid function"));
    }

    [Fact]
    public void Validate_NumRanksMustBeAtLeastOne()
    {
        var spec = MakeSpec();
        spec.Perks[0].NumRanks = 0;
        Assert.Contains(Generator.Validate(spec), p => p.Contains("numRanks must be >= 1"));
    }

    [Fact]
    public void Validate_PerkWithNoEffects_IsReported()
    {
        var spec = MakeSpec();
        spec.Perks[0].Effects.Clear();
        Assert.Contains(Generator.Validate(spec), p => p.Contains("no effects"));
    }

    [Fact]
    public void Validate_UnsupportedConditionFunction_IsReported()
    {
        var spec = MakeSpec();
        spec.Perks[1].Conditions[0].Function = "GetWeekDay";   // a real but not-yet-supported function
        Assert.Contains(Generator.Validate(spec), p => p.Contains("unsupported condition function"));
    }

    [Fact]
    public void Validate_InvalidEffectKind_IsReported()
    {
        var spec = MakeSpec();
        spec.Perks[0].Effects[0].Kind = "bogus";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("invalid kind"));
    }
}
