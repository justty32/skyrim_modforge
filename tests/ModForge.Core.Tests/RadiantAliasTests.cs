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
    public void FindMatchingLocation_NoParent_IsLocationTypeWithKeyword()
    {
        var hold = Alias(Build(ChainSpec()), "Hold");
        Assert.Equal(QuestAlias.TypeEnum.Location, hold.Type);
        Assert.NotNull(hold.Location);
        Assert.False(hold.Location!.Keyword.IsNull);          // LocTypeHold keyword bound
        Assert.Null(hold.Location.AliasID);                    // no parent narrowing
    }

    [Fact]
    public void FindMatchingLocation_WithParent_SetsParentAliasId()
    {
        var dungeon = Alias(Build(ChainSpec()), "Dungeon");
        Assert.Equal(QuestAlias.TypeEnum.Location, dungeon.Type);
        Assert.False(dungeon.Location!.Keyword.IsNull);        // LocTypeDungeon
        Assert.Equal(0, dungeon.Location.AliasID);             // search within Hold (alias index 0)
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
