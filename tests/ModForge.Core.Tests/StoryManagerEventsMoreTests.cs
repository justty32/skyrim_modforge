using Mutagen.Bethesda.Plugins;
using ModForge;
using Xunit;

// Byte-accurate assertions for the SM events decoded from Skyrim.esm beyond KillActor.
// Slot bytes: "R1"=52 31 00 00, "R2"=52 32 00 00, "L1"=4C 31 00 00, "L2"=4C 32 00 00.
public class StoryManagerEventsMoreTests
{
    private static readonly byte[] R1 = { 0x52, 0x31, 0x00, 0x00 };
    private static readonly byte[] R2 = { 0x52, 0x32, 0x00, 0x00 };
    private static readonly byte[] L1 = { 0x4C, 0x31, 0x00, 0x00 };
    private static readonly byte[] L2 = { 0x4C, 0x32, 0x00, 0x00 };

    [Fact]
    public void ChangeLocation_def_has_root_code_and_location_slots()
    {
        Assert.True(StoryManagerEvents.TryGet("ChangeLocation", out var def));
        Assert.Equal(new RecordType("CLOC"), def.Code);
        Assert.Equal(0x01320Eu, def.Root.ID);
        Assert.Equal("Skyrim.esm", def.Root.ModKey.FileName);
        Assert.Equal(L1, def.Slots["oldLocation"]);
        Assert.Equal(L2, def.Slots["newLocation"]);
    }

    [Fact]
    public void CastMagic_def_has_root_code_and_slots()
    {
        Assert.True(StoryManagerEvents.TryGet("CastMagic", out var def));
        Assert.Equal(new RecordType("CAST"), def.Code);
        Assert.Equal(0x046829u, def.Root.ID);
        Assert.Equal(R1, def.Slots["caster"]);
        Assert.Equal(R2, def.Slots["target"]);
        Assert.Equal(L1, def.Slots["location"]);
    }

    [Fact]
    public void AddItem_def_has_root_code_and_slots()
    {
        Assert.True(StoryManagerEvents.TryGet("AddItem", out var def));
        Assert.Equal(new RecordType("AIPL"), def.Code);
        Assert.Equal(0x02C439u, def.Root.ID);
        Assert.Equal(R1, def.Slots["owner"]);
        Assert.Equal(L1, def.Slots["location"]);
    }

    [Fact]
    public void Assault_def_has_root_code_and_slots()
    {
        Assert.True(StoryManagerEvents.TryGet("Assault", out var def));
        Assert.Equal(new RecordType("ASSU"), def.Code);
        Assert.Equal(0x02C494u, def.Root.ID);
        Assert.Equal(R1, def.Slots["victim"]);
        Assert.Equal(R2, def.Slots["attacker"]);
        Assert.Equal(L1, def.Slots["location"]);
    }

    [Fact]
    public void All_new_events_are_case_insensitive()
    {
        Assert.True(StoryManagerEvents.TryGet("changelocation", out _));
        Assert.True(StoryManagerEvents.TryGet("castmagic", out _));
        Assert.True(StoryManagerEvents.TryGet("additem", out _));
        Assert.True(StoryManagerEvents.TryGet("assault", out _));
    }
}
