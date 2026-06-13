using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class MusicTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    [Fact]
    public void Track_and_type_build_with_fields_loop_and_track_refs()
    {
        var spec = new ModSpec();
        spec.MusicTracks.Add(new MusicTrackSpec
        {
            EditorId = "MF_Theme", Type = "SingleTrack", File = "Music\\MF\\theme.xwm",
            FadeOut = 2f, LoopBegins = 1.5f, LoopEnds = 30f, LoopCount = 0,
        });
        spec.Music.Add(new MusicTypeSpec
        {
            EditorId = "MF_Explore", Flags = { "CycleTracks", "DoesNotQueue" },
            Priority = 5, DuckingDecibel = -6f, FadeDuration = 4f, Tracks = { "MF_Theme" },
        });
        var mod = Build(spec);
        var t = mod.MusicTracks.Single(x => x.EditorID == "MF_Theme");
        Assert.Equal(MusicTrack.TypeEnum.SingleTrack, t.Type);
        Assert.Equal("Music\\MF\\theme.xwm", t.TrackFilename!.GivenPath);
        Assert.Equal(1.5f, t.LoopData!.Begins);
        Assert.Equal(30f, t.LoopData.Ends);
        var m = mod.MusicTypes.Single(x => x.EditorID == "MF_Explore");
        Assert.True(m.Flags.HasFlag(MusicType.Flag.CycleTracks));
        Assert.Equal((ushort)5, m.Data!.Priority);
        Assert.Equal(-6f, m.Data.DuckingDecibel);
        Assert.Equal(t.FormKey, m.Tracks.Single().FormKey);   // MUSC -> MUST wired in pass 2
    }

    [Fact]
    public void Palette_track_references_its_sub_tracks()
    {
        var spec = new ModSpec();
        spec.MusicTracks.Add(new MusicTrackSpec { EditorId = "MF_A", File = "Music\\a.xwm" });
        spec.MusicTracks.Add(new MusicTrackSpec { EditorId = "MF_B", File = "Music\\b.xwm" });
        spec.MusicTracks.Add(new MusicTrackSpec { EditorId = "MF_Pool", Type = "Palette", Tracks = { "MF_A", "MF_B" } });
        var mod = Build(spec);
        var pool = mod.MusicTracks.Single(t => t.EditorID == "MF_Pool");
        Assert.Equal(2, pool.Tracks!.Count);
    }

    [Fact]
    public void Music_assigns_to_cell_and_worldspace()
    {
        var spec = new ModSpec();
        spec.MusicTracks.Add(new MusicTrackSpec { EditorId = "MF_T", File = "Music\\t.xwm" });
        spec.Music.Add(new MusicTypeSpec { EditorId = "MF_M", Tracks = { "MF_T" } });
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room", Music = "MF_M" });
        spec.Worldspaces.Add(new WorldspaceSpec { EditorId = "MF_World", Name = "W", Music = "MF_M" });
        var mod = Build(spec);
        var m = mod.MusicTypes.Single(x => x.EditorID == "MF_M");
        var cell = mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).Single(c => c.EditorID == "Room");
        Assert.Equal(m.FormKey, cell.Music.FormKey);
        var ws = mod.Worldspaces.Single(w => w.EditorID == "MF_World");
        Assert.Equal(m.FormKey, ws.Music.FormKey);
    }
}
