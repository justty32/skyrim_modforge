using System.Collections.Generic;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// SPID _DISTR.ini generation. Line format + field semantics verified against the SPID 7.3 reference
// and real ini files (sub_projs/mod-survey/findings/spid.md).
public class SpidGenTests
{
    [Fact]
    public void Emits_DistrIni_AtModRoot()
    {
        var f = SpidGen.Generate(new SpidDistributionSpec
        {
            File = "MyMod",
            Entries = new List<SpidEntrySpec>
            {
                new() { Type = "Perk", Record = "0xCF788~Skyrim.esm", StringFilters = { "ActorTypeNPC" } },
            },
        });
        Assert.Equal("MyMod_DISTR.ini", f.RelPath);          // root of mod folder (= Data/), not under SKSE/
        Assert.Equal("Perk = 0xCF788~Skyrim.esm|ActorTypeNPC\n", f.Content);
    }

    [Fact]
    public void TrailingNone_IsTrimmed()
    {
        // Only RecordID + StringFilter given → fields 3-7 are NONE and dropped.
        var line = SpidGen.Line(new SpidEntrySpec
        {
            Type = "Perk", Record = "ActorTypeNPC", StringFilters = { "Brenuin" },
        });
        Assert.Equal("Perk = ActorTypeNPC|Brenuin", line);
    }

    [Fact]
    public void RecordOnly_EmitsBareLine()
    {
        var line = SpidGen.Line(new SpidEntrySpec { Type = "Keyword", Record = "ActorTypePoor" });
        Assert.Equal("Keyword = ActorTypePoor", line);
    }

    [Fact]
    public void MiddleGap_HeldOpenWithNone()
    {
        // Chance set but filters/level/traits empty → fields 2-6 must stay as NONE to keep position 7.
        var line = SpidGen.Line(new SpidEntrySpec
        {
            Type = "Perk", Record = "0x9DE80~test.esp", FormFilters = { "0x1BCC0~test.esp" }, Chance = 50,
        });
        Assert.Equal("Perk = 0x9DE80~test.esp|NONE|0x1BCC0~test.esp|NONE|NONE|NONE|50", line);
    }

    [Fact]
    public void Item_PutsCountInField6()
    {
        var line = SpidGen.Line(new SpidEntrySpec
        {
            Type = "Item", Record = "0xF~Skyrim.esm",
            StringFilters = { "ActorTypeNPC", "-Nazeem" }, Count = 3000,
        });
        Assert.Equal("Item = 0xF~Skyrim.esm|ActorTypeNPC,-Nazeem|NONE|NONE|NONE|3000", line);
    }

    [Fact]
    public void Package_PutsIndexInField6()
    {
        var line = SpidGen.Line(new SpidEntrySpec
        {
            Type = "Package", Record = "0x123~My.esp", PackageIndex = 0, Chance = 100,
        });
        // index 0 is a real value (field 6 = "0"), so field 7 chance keeps its slot.
        Assert.Equal("Package = 0x123~My.esp|NONE|NONE|NONE|NONE|0|100", line);
    }

    [Fact]
    public void Count_IgnoredForNonItemType()
    {
        // Count only applies to Item; on a Spell it must not leak into field 6.
        var line = SpidGen.Line(new SpidEntrySpec { Type = "Spell", Record = "MySpell", Count = 5 });
        Assert.Equal("Spell = MySpell", line);
    }

    [Fact]
    public void Traits_And_Level_PassThroughRaw()
    {
        var line = SpidGen.Line(new SpidEntrySpec
        {
            Type = "Spell", Record = "0x12FCD~Skyrim.esm", LevelFilters = "14(10)", Traits = "M/U",
        });
        Assert.Equal("Spell = 0x12FCD~Skyrim.esm|NONE|NONE|14(10)|M/U", line);
    }

    [Fact]
    public void MultipleEntries_OneLineEach()
    {
        // Real ImGladYoureHere_DISTR.ini pattern: faction attach to specific NPCs.
        var f = SpidGen.Generate(new SpidDistributionSpec
        {
            File = "ImGladYoureHere",
            Entries = new List<SpidEntrySpec>
            {
                new() { Type = "Faction", Record = "WW42GYHFaction", StringFilters = { "JJSofiaFollower" } },
                new() { Type = "Faction", Record = "WW42GYHPetFaction", StringFilters = { "PumpkinTheFoxActor" } },
            },
        });
        var lines = f.Content.TrimEnd('\n').Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Equal("Faction = WW42GYHFaction|JJSofiaFollower", lines[0]);
        Assert.Equal("Faction = WW42GYHPetFaction|PumpkinTheFoxActor", lines[1]);
    }
}
