using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// XMarker placement + the marker being usable as a Travel anchor (the deferred-resolution fix that
// lets Travel `place` point at an in-spec placement editorId). Master-free.
public class XMarkerTests
{
    private const string XMarkerHeading = "Skyrim.esm:0x000034";
    private const string TravelTemplate = "Skyrim.esm:0x016FAA";

    // A marker placement becomes a PlacedObject (REFR) over the vanilla XMarkerHeading base, named so
    // packages/links can target it. Forced persistent because the Travel package targets it.
    [Fact]
    public void XMarker_AnchorsATravelPackage_AsDeferredTarget()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Marker", Base = XMarkerHeading, Cell = "Room",
            Position = new Vec3 { X = 0, Y = 200, Z = 0 },
        });
        spec.Npcs.Add(new NpcSpec
        {
            EditorId = "Walker", Name = "Walker",
            Race = "Skyrim.esm:0x013746", Packages = { "Trip" },
        });
        spec.Packages.Add(new PackageSpec
        {
            EditorId = "Trip", Template = TravelTemplate,
            Travel = new TravelSpec { Place = "Marker" },
        });
        spec.Placements.Add(new PlacementSpec { Base = "Walker", Cell = "Room", Kind = "npc" });

        var r = TestBuild.Ok(spec);

        // Marker placed as XMarkerHeading.
        var marker = r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(o => o.EditorID == "Marker");
        Assert.Equal(0x000034u, marker.Base.FormKey.ID);
        Assert.Equal("Skyrim.esm", marker.Base.FormKey.ModKey.FileName);

        // The Travel package's slot 0 (Place to Travel) resolves to the marker — NOT NearSelf.
        var pack = r.Mod.EnumerateMajorRecords<IPackageGetter>().Single(p => p.EditorID == "Trip");
        var place = (IPackageDataLocationGetter)pack.Data.First(d => d.Value.Name == "Place to Travel").Value;
        var target = Assert.IsAssignableFrom<ILocationTargetGetter>(place.Location.Target);
        Assert.Equal(marker.FormKey, target.Link.FormKey);

        // Marker must persist (a package SingleRef/location anchor the engine could otherwise drop).
        var room = r.Mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).Single(c => c.EditorID == "Room");
        Assert.Contains(room.Persistent, p => p.EditorID == "Marker");
    }
}
