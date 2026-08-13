using System.Linq;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// KID / BOS / AOS / SkyPatcher loose-ini generation + validation (roadmap D-3/5/6/7).
// Formats verified against sub_projs/mod-survey/findings/{keyword-item-distributor,base-object-swapper,
// animobject-swapper,skypatcher}-*.md.
public class DistributorIniTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    // ---- KID ----
    [Fact]
    public void Kid_EmitsLine_AtModRoot_WithTrailingNoneTrim()
    {
        var f = KidGen.Generate(new KidDistributionSpec
        {
            File = "MyMod",
            Entries = { new KidEntrySpec { Keyword = "WeapTypeSword", Type = "Weapon", Filters = { "*Iron" } } },
        });
        Assert.Equal("MyMod_KID.ini", f.RelPath);
        Assert.Equal("Keyword = WeapTypeSword|Weapon|*Iron\n", f.Content);
    }

    [Fact]
    public void Kid_MiddleGap_HeldOpenWithNone()
    {
        // traits set but filter empty → field 3 must stay NONE to keep field 4's slot.
        var line = KidGen.Line(new KidEntrySpec { Keyword = "NoviceDestruction", Type = "Magic Effect", Traits = "20(0/25)" });
        Assert.Equal("Keyword = NoviceDestruction|Magic Effect|NONE|20(0/25)", line);
    }

    [Fact]
    public void Kid_Chance_KeepsItsSlot()
    {
        var line = KidGen.Line(new KidEntrySpec { Keyword = "MysticalAmmo", Type = "Ammo", Filters = { "*Bound" }, Chance = 50 });
        Assert.Equal("Keyword = MysticalAmmo|Ammo|*Bound|NONE|50", line);
    }

    [Fact]
    public void Kid_UnknownType_Reported()
    {
        var s = new ModSpec { KidDistributions = { new KidDistributionSpec { File = "m",
            Entries = { new KidEntrySpec { Keyword = "K", Type = "Banana" } } } } };
        Assert.Contains(Validate(s), p => p.Contains("unknown type 'Banana'"));
    }

    // ---- BOS ----
    [Fact]
    public void Bos_FormsSection_BasicSwap()
    {
        var f = BosGen.Generate(new ObjectSwapSpec
        {
            File = "MyMod",
            Groups = { new ObjectSwapGroupSpec { Entries =
                { new ObjectSwapEntrySpec { Base = "0x10C0E3~Skyrim.esm", Swaps = { "0x806~MyMod.esp" } } } } },
        });
        Assert.Equal("MyMod_SWAP.ini", f.RelPath);
        Assert.Contains("[Forms]", f.Content);
        Assert.Contains("0x10C0E3~Skyrim.esm|0x806~MyMod.esp", f.Content);
    }

    [Fact]
    public void Bos_ConditionalSection_AndGapHeldOpen()
    {
        var f = BosGen.Generate(new ObjectSwapSpec
        {
            File = "m",
            Groups = { new ObjectSwapGroupSpec
            {
                Conditions = { "WhiterunLocation", "-AzuraShrineLocation" },
                Entries = { new ObjectSwapEntrySpec { Base = "A", Swaps = { "B" }, Chance = 75 } },
            } },
        });
        Assert.Contains("[Forms|WhiterunLocation,-AzuraShrineLocation]", f.Content);
        // properties empty but chance set → "A|B||75" (gap held as ||).
        Assert.Contains("A|B||75", f.Content);
    }

    [Fact]
    public void Bos_EntryWithoutSwaps_Reported()
    {
        var s = new ModSpec { ObjectSwaps = { new ObjectSwapSpec { File = "m",
            Groups = { new ObjectSwapGroupSpec { Entries = { new ObjectSwapEntrySpec { Base = "A" } } } } } } };
        Assert.Contains(Validate(s), p => p.Contains("has no 'swaps'"));
    }

    // ---- AOS ----
    [Fact]
    public void Aos_UnconditionalSwap_RandomPool()
    {
        var f = AosGen.Generate(new AnimObjectSwapSpec
        {
            File = "MyMod",
            Entries = { new AnimObjectSwapEntrySpec { Base = "DrinkingCupANIO", Swaps = { "WoodCupANIO", "MeadHornANIO" } } },
        });
        Assert.Equal("MyMod_ANIO.ini", f.RelPath);
        Assert.Contains("[DrinkingCupANIO]\n", f.Content);
        Assert.Contains("DrinkingCupANIO|WoodCupANIO,MeadHornANIO\n", f.Content);
    }

    [Fact]
    public void Aos_FilterAndTraits_InHeader()
    {
        var h = AosGen.Header(new AnimObjectSwapEntrySpec
        {
            Base = "BookReadingANIO", Swaps = { "X" }, Filters = { "+ThievesGuildFaction" }, Traits = "F",
        });
        Assert.Equal("BookReadingANIO|+ThievesGuildFaction|F", h);
    }

    [Fact]
    public void Aos_TraitsOnly_HoldsFilterGapOpen()
    {
        var h = AosGen.Header(new AnimObjectSwapEntrySpec { Base = "A", Swaps = { "X" }, Traits = "-C" });
        Assert.Equal("A||-C", h);
    }

    // ---- SkyPatcher ----
    [Fact]
    public void SkyPatcher_FlatLine_UnderRecordTypeFolder()
    {
        var f = SkyPatcherGen.Generate(new SkyPatcherSpec
        {
            File = "patch", RecordType = "npc",
            Patches = { new SkyPatcherLineSpec
            {
                Filters = { new SkyPatcherFieldSpec { Key = "filterByRaces", Value = "NordRace" } },
                Mods = { new SkyPatcherFieldSpec { Key = "spellsToAdd", Value = "MagicResistance50" },
                         new SkyPatcherFieldSpec { Key = "perksToAdd", Value = "HalfCostSpells" } },
            } },
        });
        Assert.Equal("SKSE/Plugins/SkyPatcher/npc/patch.ini", f.RelPath);
        Assert.Equal("filterByRaces=NordRace:spellsToAdd=MagicResistance50:perksToAdd=HalfCostSpells\n", f.Content);
    }

    [Fact]
    public void SkyPatcher_UnknownRecordType_Reported()
    {
        var s = new ModSpec { SkyPatchers = { new SkyPatcherSpec { File = "m", RecordType = "banana",
            Patches = { new SkyPatcherLineSpec { Mods = { new SkyPatcherFieldSpec { Key = "x", Value = "y" } } } } } } };
        Assert.Contains(Validate(s), p => p.Contains("unknown recordType 'banana'"));
    }

    [Fact]
    public void SkyPatcher_LineWithoutMods_Reported()
    {
        var s = new ModSpec { SkyPatchers = { new SkyPatcherSpec { File = "m", RecordType = "npc",
            Patches = { new SkyPatcherLineSpec { Filters = { new SkyPatcherFieldSpec { Key = "filterByRaces", Value = "NordRace" } } } } } } };
        Assert.Contains(Validate(s), p => p.Contains("no mods"));
    }

    [Fact]
    public void AllFour_ValidSpecs_NoProblems()
    {
        var s = new ModSpec
        {
            KidDistributions = { new KidDistributionSpec { File = "k", Entries = { new KidEntrySpec { Keyword = "K", Type = "Weapon" } } } },
            ObjectSwaps = { new ObjectSwapSpec { File = "b", Groups = { new ObjectSwapGroupSpec { Entries = { new ObjectSwapEntrySpec { Base = "A", Swaps = { "B" } } } } } } },
            AnimObjectSwaps = { new AnimObjectSwapSpec { File = "a", Entries = { new AnimObjectSwapEntrySpec { Base = "A", Swaps = { "B" } } } } },
            SkyPatchers = { new SkyPatcherSpec { File = "s", RecordType = "leveledList", Patches = { new SkyPatcherLineSpec { Mods = { new SkyPatcherFieldSpec { Key = "objectsToAdd", Value = "X" } } } } } },
        };
        Assert.DoesNotContain(Validate(s), p => p.Contains("kidDistribution") || p.Contains("objectSwap") || p.Contains("animObjectSwap") || p.Contains("skyPatcher"));
    }
}
