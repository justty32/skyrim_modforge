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

    // The map marker lands in a regular exterior grid cell. We deliberately do NOT override the
    // worldspace persistent (top) cell — doing so CTDs the engine (in-game x2, 2026-06-13) — so TopCell
    // stays null. KNOWN ISSUE: this blanks the world map; the crash-free persistent-cell override is TODO.
    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void MapMarker_lands_in_a_grid_cell_and_does_not_touch_the_persistent_topcell()
    {
        var mod = Build(CampSpec());
        var ws = mod.Worldspaces.Single(w => w.FormKey.ID == 0x3C);
        Assert.Null(ws.TopCell);                                     // must NOT override the persistent cell (CTDs)
        var marker = ws.SubCells.SelectMany(b => b.Items).SelectMany(s => s.Items)
            .SelectMany(c => c.Persistent).OfType<IPlacedObjectGetter>().Single(r => r.EditorID == "MF_Camp");
        Assert.NotNull(marker.MapMarker);
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
