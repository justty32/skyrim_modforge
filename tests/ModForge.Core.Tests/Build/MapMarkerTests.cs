using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class MapMarkerTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    private static ModSpec CampSpec() => new()
    {
        MapMarkers =
        {
            new MapMarkerSpec
            {
                EditorId = "MF_Camp", Name = "Test Camp",
                Worldspace = "Skyrim.esm:0x00003C",
                Position = new Vec3 { X = 0, Y = -9000, Z = 0 },
                Type = "Camp", Flags = { "Visible", "CanTravelTo" },
            },
        },
    };

    // Building into a vanilla worldspace resolves the master exterior/persistent cell → RequiresSkyrim.
    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void MapMarker_emits_placed_object_on_mapmarker_base_with_xmrk()
    {
        var mod = Build(CampSpec());
        var marker = mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(r => r.EditorID == "MF_Camp");
        Assert.Equal(0x10u, marker.Base.FormKey.ID);                 // MapMarker static
        Assert.NotNull(marker.MapMarker);
        Assert.Equal(MapMarker.MarkerType.Camp, marker.MapMarker!.Type);
        Assert.True(marker.MapMarker.Flags.HasFlag(MapMarker.Flag.Visible));
        Assert.True(marker.MapMarker.Flags.HasFlag(MapMarker.Flag.CanTravelTo));
    }

    // The map marker lands in the worldspace persistent (top) cell, carried additively. The persistent
    // cell override MUST copy the master's record flags (0x00040400 = Cell Persistent 0x400 + internal
    // 0x40000) — without them the engine doesn't recognise it as the persistent cell and CTDs queuing
    // actors (in-game x2 before this was found). The marker ref carries the 0x400 persistent flag too.
    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void MapMarker_persistent_topcell_copies_the_master_record_flags()
    {
        var mod = Build(CampSpec());
        var ws = mod.Worldspaces.Single(w => w.FormKey.ID == 0x3C);
        Assert.NotNull(ws.TopCell);
        Assert.Equal(0xD74u, ws.TopCell!.FormKey.ID);
        Assert.Equal(0x00040400, (int)ws.TopCell.MajorRecordFlagsRaw);   // matches vanilla/USSEP (the fix)
        var marker = ws.TopCell.Persistent.OfType<IPlacedObjectGetter>().Single(r => r.EditorID == "MF_Camp");
        Assert.NotNull(marker.MapMarker);
        Assert.True((marker.MajorRecordFlagsRaw & 0x400) != 0);
        Assert.Single(ws.TopCell.Persistent);                        // additive: only our marker re-stated
    }

    [Fact]
    public void MapMarker_bad_type_and_flag_are_validate_problems()
    {
        var spec = new ModSpec();
        spec.MapMarkers.Add(new MapMarkerSpec
        {
            EditorId = "MF_Bad", Worldspace = "Skyrim.esm:0x00003C",
            Type = "Nonsense", Flags = { "Glowing" },
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_Bad") && p.Contains("Nonsense"));
        Assert.Contains(problems, p => p.Contains("MF_Bad") && p.Contains("Glowing"));
    }
}
