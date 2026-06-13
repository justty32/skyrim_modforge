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

    // Regression for the blank-map bug: the worldspace override must carry the persistent TopCell
    // ADDITIVELY (our marker lands in it; no vanilla refs are re-stated) so vanilla map markers survive.
    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void MapMarker_lands_in_the_persistent_topcell_which_is_carried_additively()
    {
        var mod = Build(CampSpec());
        var ws = mod.Worldspaces.Single(w => w.FormKey.ID == 0x3C);
        Assert.NotNull(ws.TopCell);                                  // persistent cell carried (else map blanks)
        Assert.Equal(0xD74u, ws.TopCell!.FormKey.ID);               // Tamriel persistent cell
        var marker = ws.TopCell.Persistent.OfType<IPlacedObjectGetter>().Single(r => r.EditorID == "MF_Camp");
        Assert.NotNull(marker.MapMarker);
        // Additive: ONLY our marker is re-stated; the engine keeps the master's vanilla persistent refs.
        Assert.Single(ws.TopCell.Persistent);
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
