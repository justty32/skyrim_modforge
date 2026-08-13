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
    public void CraftItem_def_has_root_code_and_workbench_slot()
    {
        Assert.True(StoryManagerEvents.TryGet("CraftItem", out var def));
        Assert.Equal(new RecordType("CRFT"), def.Code);
        Assert.Equal(0x039D86u, def.Root.ID);
        Assert.Equal("Skyrim.esm", def.Root.ModKey.FileName);
        Assert.Equal(R1, def.Slots["workbench"]);
    }

    [Fact]
    public void PlayerRemoveItem_def_has_root_code_and_slots()
    {
        Assert.True(StoryManagerEvents.TryGet("PlayerRemoveItem", out var def));
        Assert.Equal(new RecordType("REMP"), def.Code);
        Assert.Equal(0x02C6ACu, def.Root.ID);
        Assert.Equal(R1, def.Slots["owner"]);
        Assert.Equal(R2, def.Slots["item"]);
    }

    [Fact]
    public void Arrest_def_has_root_code_and_slots()
    {
        Assert.True(StoryManagerEvents.TryGet("Arrest", out var def));
        Assert.Equal(new RecordType("ARRT"), def.Code);
        Assert.Equal(0x06B369u, def.Root.ID);
        Assert.Equal(R1, def.Slots["guard"]);
        Assert.Equal(R2, def.Slots["criminal"]);
    }

    [Fact]
    public void IncreaseLevel_def_has_root_code_and_no_slots()
    {
        Assert.True(StoryManagerEvents.TryGet("IncreaseLevel", out var def));
        Assert.Equal(new RecordType("LEVL"), def.Code);
        Assert.Equal(0x05BD79u, def.Root.ID);
        Assert.Empty(def.Slots);   // no event ref slots — gate via storyEvent.conditions
    }

    [Fact]
    public void All_new_events_are_case_insensitive()
    {
        Assert.True(StoryManagerEvents.TryGet("changelocation", out _));
        Assert.True(StoryManagerEvents.TryGet("castmagic", out _));
        Assert.True(StoryManagerEvents.TryGet("additem", out _));
        Assert.True(StoryManagerEvents.TryGet("assault", out _));
        Assert.True(StoryManagerEvents.TryGet("craftitem", out _));
        Assert.True(StoryManagerEvents.TryGet("playerremoveitem", out _));
        Assert.True(StoryManagerEvents.TryGet("arrest", out _));
        Assert.True(StoryManagerEvents.TryGet("increaselevel", out _));
    }

    [Fact]
    public void ScriptEvent_has_root_code_and_payload_slots()
    {
        Assert.True(StoryManagerEvents.TryGet("ScriptEvent", out var def));
        Assert.Equal(0x01379Au, def.Root.ID);
        Assert.Equal(new RecordType("SCPT"), def.Code);
        Assert.Equal(R1, def.Slots["ref1"]);
        Assert.Equal(R2, def.Slots["ref2"]);
        Assert.Equal(L1, def.Slots["loc"]);
    }
}
