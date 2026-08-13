using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class StoryManagerEventsTests
{
    [Fact]
    public void KillActor_def_has_root_code_and_slots()
    {
        Assert.True(StoryManagerEvents.TryGet("KillActor", out var def));
        Assert.Equal(new RecordType("KILL"), def.Code);
        Assert.Equal(0x013010u, def.Root.ID);
        Assert.Equal("Skyrim.esm", def.Root.ModKey.FileName);
        Assert.Equal(new byte[] { 0x52, 0x31, 0x00, 0x00 }, def.Slots["victim"]);
        Assert.Equal(new byte[] { 0x52, 0x32, 0x00, 0x00 }, def.Slots["killer"]);
    }

    [Fact]
    public void TryGet_is_case_insensitive_and_rejects_unknown()
    {
        Assert.True(StoryManagerEvents.TryGet("killactor", out _));
        Assert.False(StoryManagerEvents.TryGet("Nope", out _));
    }

    [Theory]
    [InlineData("fromEvent:victim", true, "fromEvent", "victim")]
    [InlineData("forced:SomeEd", true, "forced", "SomeEd")]
    [InlineData("forced:Skyrim.esm:0x013010", true, "forced", "Skyrim.esm:0x013010")]
    [InlineData("garbage", false, "", "")]
    [InlineData("forced:", false, "", "")]
    [InlineData(":victim", false, "", "")]
    [InlineData("", false, "", "")]
    public void TryParseFill_splits_kind_and_arg(string fill, bool ok, string kind, string arg)
    {
        Assert.Equal(ok, StoryManagerEvents.TryParseFill(fill, out var k, out var a));
        if (ok) { Assert.Equal(kind, k); Assert.Equal(arg, a); }
    }
}
