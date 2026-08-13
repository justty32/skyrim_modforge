using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// #7 findMatchingLocation (LocationAlias) + #8 findInLocationAlias (find ref in location alias).
// Mutagen binary shape verified offline by reflection (QuestAlias.Location = LocationAliasReference
// {AliasID, Keyword, RefType}); exact CK semantics pending main-machine xEdit compare vs Missives.
public class RadiantAliasTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    // A standalone (StartGameEnabled) quest carrying a radiant Hold→Dungeon→Boss alias chain.
    private static ModSpec ChainSpec()
    {
        var spec = new ModSpec();
        spec.Keywords.Add(new KeywordSpec { EditorId = "LocTypeHold" });
        spec.Keywords.Add(new KeywordSpec { EditorId = "LocTypeDungeon" });
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFRadiant",
            Name = "Radiant",
            StartGameEnabled = true,
            Aliases =
            {
                new QuestAliasSpec { Name = "Hold",    Fill = "findMatchingLocation:LocTypeHold" },             // idx 0
                new QuestAliasSpec { Name = "Dungeon", Fill = "findMatchingLocation:LocTypeDungeon@Hold" },     // idx 1
                new QuestAliasSpec { Name = "Boss",    Fill = "findInLocationAlias:Dungeon#Skyrim.esm:0x0130DE",// idx 2
                    Conditions = { new ConditionSpec { Function = "GetRandomPercent", Comparison = "<", Value = 50 } } },
            },
        });
        return spec;
    }

    private static QuestAlias Alias(ISkyrimMod mod, string name) =>
        mod.Quests.First(q => q.EditorID == "MFRadiant").Aliases.First(a => a.Name == name);

    [Fact]
    public void FindMatchingLocation_NoParent_IsLocationTypeWithKeywordCondition()
    {
        // Byte-shape verified vs shipping Missives (2026-06-21): a Find-Matching-Location radiant fill is a
        // Location-type alias with a LocationHasKeyword==1 CTDA (NOT a LocationAliasReference.Keyword, which
        // the engine ignores on a Location alias). No parent → exactly one condition.
        var hold = Alias(Build(ChainSpec()), "Hold");
        Assert.Equal(QuestAlias.TypeEnum.Location, hold.Type);
        Assert.Null(hold.Location);                            // no LocationAliasReference — conditions do the matching
        Assert.True(hold.Flags!.Value.HasFlag(QuestAlias.Flag.StoresText)); // <Alias=Hold> token renders
        var kw = Assert.Single(hold.Conditions);
        Assert.IsType<LocationHasKeywordConditionData>(((IConditionFloatGetter)kw).Data);
    }

    [Fact]
    public void FindMatchingLocation_WithParent_AddsGetInCurrentLocAliasCondition()
    {
        // Narrowed to a parent location alias (@Hold): add a GetInCurrentLocAlias==1 CTDA whose
        // LocationAliasIndex points at the parent (Hold = alias index 0) — Missives Alias_Dungeon shape.
        var dungeon = Alias(Build(ChainSpec()), "Dungeon");
        Assert.Equal(QuestAlias.TypeEnum.Location, dungeon.Type);
        Assert.Null(dungeon.Location);
        Assert.Contains(dungeon.Conditions, c =>
            ((IConditionFloatGetter)c).Data is LocationHasKeywordConditionData);
        var inAlias = Assert.Single(dungeon.Conditions, c =>
            ((IConditionFloatGetter)c).Data is GetInCurrentLocAliasConditionData);
        Assert.Equal(0, ((GetInCurrentLocAliasConditionData)((IConditionFloatGetter)inAlias).Data).LocationAliasIndex);
    }

    [Fact]
    public void FindInLocationAlias_IsReferenceWithLocationAndRefType()
    {
        var boss = Alias(Build(ChainSpec()), "Boss");
        Assert.Equal(QuestAlias.TypeEnum.Reference, boss.Type);
        Assert.NotNull(boss.Location);
        Assert.Equal(1, boss.Location!.AliasID);               // search within Dungeon (alias index 1)
        Assert.False(boss.Location.RefType.IsNull);            // LCRT refType bound
        Assert.Single(boss.Conditions);                        // GetDead match filter wired
    }

    [Fact]
    public void FindInLocationAlias_ConditionsOnly_NoRefType_IsValid()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q", StartGameEnabled = true,
            Aliases =
            {
                new QuestAliasSpec { Name = "Loc", Fill = "findMatchingLocation:Skyrim.esm:0x0130DE" },
                new QuestAliasSpec { Name = "Ref", Fill = "findInLocationAlias:Loc",
                    Conditions = { new ConditionSpec { Function = "GetRandomPercent", Comparison = "<", Value = 50 } } },
            },
        });
        Assert.DoesNotContain(Validate(spec), p => p.Contains("findInLocationAlias"));
        var refA = Build(spec).Quests.First().Aliases.First(a => a.Name == "Ref");
        Assert.True(refA.Location!.RefType.IsNull);             // no refType → conditions do the filtering
        Assert.Equal(0, refA.Location.AliasID);
    }

    [Fact]
    public void ChainSpec_Validates_Clean()
    {
        Assert.DoesNotContain(Validate(ChainSpec()),
            p => p.Contains("findMatchingLocation") || p.Contains("findInLocationAlias"));
    }

    [Fact]
    public void FindMatchingLocation_BadKeyword_Reported()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q", StartGameEnabled = true,
            Aliases = { new QuestAliasSpec { Name = "L", Fill = "findMatchingLocation:NoSuchKeyword" } },
        });
        Assert.Contains(Validate(spec), p => p.Contains("LocType keyword") && p.Contains("NoSuchKeyword"));
    }

    [Fact]
    public void FindMatchingLocation_UnknownParent_Reported()
    {
        var spec = new ModSpec();
        spec.Keywords.Add(new KeywordSpec { EditorId = "LocTypeDungeon" });
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q", StartGameEnabled = true,
            Aliases = { new QuestAliasSpec { Name = "L", Fill = "findMatchingLocation:LocTypeDungeon@Ghost" } },
        });
        Assert.Contains(Validate(spec), p => p.Contains("parent alias 'Ghost'"));
    }

    [Fact]
    public void FindInLocationAlias_UnknownLocationAlias_Reported()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q", StartGameEnabled = true,
            Aliases = { new QuestAliasSpec { Name = "R", Fill = "findInLocationAlias:Ghost",
                Conditions = { new ConditionSpec { Function = "GetRandomPercent", Comparison = "<", Value = 50 } } } },
        });
        Assert.Contains(Validate(spec), p => p.Contains("location alias 'Ghost'"));
    }

    [Fact]
    public void FindInLocationAlias_SelfReference_Reported()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q", StartGameEnabled = true,
            Aliases = { new QuestAliasSpec { Name = "R", Fill = "findInLocationAlias:R",
                Conditions = { new ConditionSpec { Function = "GetRandomPercent", Comparison = "<", Value = 50 } } } },
        });
        Assert.Contains(Validate(spec), p => p.Contains("cannot search within itself"));
    }

    [Fact]
    public void FindInLocationAlias_NoRefTypeNoConditions_Reported()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q", StartGameEnabled = true,
            Aliases =
            {
                new QuestAliasSpec { Name = "Loc", Fill = "findMatchingLocation:Skyrim.esm:0x0130DE" },
                new QuestAliasSpec { Name = "R", Fill = "findInLocationAlias:Loc" },
            },
        });
        Assert.Contains(Validate(spec), p => p.Contains("needs a refType") || p.Contains("conditions"));
    }
}
