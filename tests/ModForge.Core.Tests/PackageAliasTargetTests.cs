using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// C組 #2 — package target/location → quest alias index (radiant performance packages). A package whose
// ownerQuest fills aliases can target/anchor on those aliases: travel to alias:Dungeon, escort alias:Victim.
// Mutagen shape reflection-verified (PackageTargetAlias{Alias}; LocationFallback{AliasForReference/Location,Data}).
public class PackageAliasTargetTests
{
    private const string TravelTemplate = "Skyrim.esm:0x016FAA";
    private const string EscortTemplate = "Skyrim.esm:0x023B73";
    private const string ActivateTemplate = "Skyrim.esm:0x019B2D";

    private static IPackageGetter Pkg(BuildResult r, string ed)
        => r.Mod.EnumerateMajorRecords<IPackageGetter>().Single(p => p.EditorID == ed);
    private static IAPackageDataGetter Slot(IPackageGetter p, sbyte i)
        => p.Data.Single(kv => kv.Key == i).Value;
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    // A quest with two aliases (index 0 = Victim ref, index 1 = Dungeon location) + packages that target them.
    private static ModSpec RadiantSpec() => new()
    {
        Quests =
        {
            new QuestSpec
            {
                EditorId = "MFRad", Name = "Rad", StartGameEnabled = true,
                Aliases =
                {
                    new QuestAliasSpec { Name = "Victim", Fill = "forced:Skyrim.esm:0x000007" },              // index 0
                    new QuestAliasSpec { Name = "Dungeon", Fill = "findMatchingLocation:Skyrim.esm:0x0130DE" },// index 1
                },
            },
        },
        Packages =
        {
            new PackageSpec { EditorId = "GoToDungeon", Template = TravelTemplate, OwnerQuest = "MFRad",
                Travel = new TravelSpec { Place = "aliasLoc:Dungeon" } },
            new PackageSpec { EditorId = "GrabVictim", Template = ActivateTemplate, OwnerQuest = "MFRad",
                Activate = new ActivateSpec { Target = "alias:Victim" } },
        },
    };

    [Fact]
    public void TravelLocation_AliasLoc_BindsAliasForLocation()
    {
        var pkg = Pkg(TestBuild.Ok(RadiantSpec()), "GoToDungeon");
        var loc = Assert.IsAssignableFrom<IPackageDataLocationGetter>(Slot(pkg, 0));
        var fb = Assert.IsAssignableFrom<ILocationFallbackGetter>(loc.Location.Target);
        Assert.Equal(LocationTargetRadius.LocationType.AliasForLocation, fb.Type);
        Assert.Equal(1, fb.Data);                       // Dungeon = alias index 1
    }

    [Fact]
    public void ActivateTarget_Alias_BindsPackageTargetAlias()
    {
        var pkg = Pkg(TestBuild.Ok(RadiantSpec()), "GrabVictim");
        var tgt = Assert.IsAssignableFrom<IPackageDataTargetGetter>(Slot(pkg, 0));
        var al = Assert.IsAssignableFrom<IPackageTargetAliasGetter>(tgt.Target);
        Assert.Equal(0, al.Alias);                      // Victim = alias index 0
    }

    [Fact]
    public void Location_Alias_RefForm_BindsAliasForReference()
    {
        // alias: (not aliasLoc:) on a location slot → AliasForReference (the alias holds a ref).
        var spec = new ModSpec
        {
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q", StartGameEnabled = true,
                Aliases = { new QuestAliasSpec { Name = "Bed", Fill = "forced:Skyrim.esm:0x000007" } } } },
            Packages = { new PackageSpec { EditorId = "Sleep", Template = "Skyrim.esm:0x019717", OwnerQuest = "Q",
                Sleep = new SleepSpec { Location = "alias:Bed" } } },
        };
        var loc = Assert.IsAssignableFrom<IPackageDataLocationGetter>(Slot(Pkg(TestBuild.Ok(spec), "Sleep"), 0));
        var fb = Assert.IsAssignableFrom<ILocationFallbackGetter>(loc.Location.Target);
        Assert.Equal(LocationTargetRadius.LocationType.AliasForReference, fb.Type);
        Assert.Equal(0, fb.Data);
    }

    [Fact]
    public void RadiantSpec_Validates_Clean()
    {
        Assert.DoesNotContain(Validate(RadiantSpec()), p => p.Contains("alias") || p.Contains("ownerQuest"));
    }

    [Fact]
    public void Validate_AliasRef_NoOwnerQuest_Reported()
    {
        var spec = new ModSpec { Packages = { new PackageSpec { EditorId = "P", Template = ActivateTemplate,
            Activate = new ActivateSpec { Target = "alias:Victim" } } } };
        Assert.Contains(Validate(spec), p => p.Contains("needs an in-spec 'ownerQuest'"));
    }

    [Fact]
    public void Validate_AliasRef_UnknownAlias_Reported()
    {
        var spec = new ModSpec
        {
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q", StartGameEnabled = true,
                Aliases = { new QuestAliasSpec { Name = "Victim", Fill = "forced:Skyrim.esm:0x000007" } } } },
            Packages = { new PackageSpec { EditorId = "P", Template = ActivateTemplate, OwnerQuest = "Q",
                Activate = new ActivateSpec { Target = "alias:Ghost" } } },
        };
        Assert.Contains(Validate(spec), p => p.Contains("no alias 'Ghost'"));
    }

    [Fact]
    public void Validate_AliasRef_ExternalOwnerQuest_Reported()
    {
        var spec = new ModSpec { Packages = { new PackageSpec { EditorId = "P", Template = ActivateTemplate,
            OwnerQuest = "Skyrim.esm:0x00ABCD", Activate = new ActivateSpec { Target = "alias:Victim" } } } };
        Assert.Contains(Validate(spec), p => p.Contains("not an in-spec quest"));
    }

    [Fact]
    public void EscortDestinationAndTarget_BothAcceptAliases()
    {
        var spec = new ModSpec
        {
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q", StartGameEnabled = true,
                Aliases =
                {
                    new QuestAliasSpec { Name = "VIP", Fill = "forced:Skyrim.esm:0x000007" },                 // 0
                    new QuestAliasSpec { Name = "Safehouse", Fill = "findMatchingLocation:Skyrim.esm:0x0130DE" },// 1
                } } },
            Packages = { new PackageSpec { EditorId = "Esc", Template = EscortTemplate, OwnerQuest = "Q",
                Escort = new EscortSpec { Target = "alias:VIP", Destination = "aliasLoc:Safehouse" } } },
        };
        var pkg = Pkg(TestBuild.Ok(spec), "Esc");
        var tgt = Assert.IsAssignableFrom<IPackageTargetAliasGetter>(
            Assert.IsAssignableFrom<IPackageDataTargetGetter>(Slot(pkg, 11)).Target);
        Assert.Equal(0, tgt.Alias);                     // VIP
        var dest = Assert.IsAssignableFrom<ILocationFallbackGetter>(
            Assert.IsAssignableFrom<IPackageDataLocationGetter>(Slot(pkg, 3)).Location.Target);
        Assert.Equal(LocationTargetRadius.LocationType.AliasForLocation, dest.Type);
        Assert.Equal(1, dest.Data);                     // Safehouse
    }
}
