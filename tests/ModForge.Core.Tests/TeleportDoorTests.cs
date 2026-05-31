using System;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Load-door teleports (XTEL) + XMarker placement support. All master-free: two NEW in-spec cells,
// in-spec door bases, markers (vanilla static FormKey only — no record cloned), teleport wiring
// between two in-spec doors. Locks the XTEL shape (partner FormKey + PARTNER arrival pos/rot) and
// the persistence/uniqueness rules the engine needs.
public class TeleportDoorTests
{
    // A door base placed in-spec, so the teleport tests don't need Skyrim.esm. (The engine wants a
    // real DOOR record; an in-spec Activator-shaped door isn't a thing, so we use a Static stand-in
    // for the BASE — irrelevant to the XTEL wiring, which is the unit under test.)
    private static ModSpec TwoDoorSpec()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Cells.Add(new CellSpec { EditorId = "CellA", Name = "Cell A" });
        spec.Cells.Add(new CellSpec { EditorId = "CellB", Name = "Cell B" });
        // In-spec static bases standing in for door meshes (master-free).
        spec.Statics.Add(new StaticSpec { EditorId = "DoorBase", Model = @"Clutter\Door01.nif" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "DoorA", Base = "DoorBase", Cell = "CellA",
            Position = new Vec3 { X = 100, Y = 0, Z = 0 },
            Rotation = new Vec3 { X = 0, Y = 0, Z = 90 },
            Teleport = "DoorB",
        });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "DoorB", Base = "DoorBase", Cell = "CellB",
            Position = new Vec3 { X = -200, Y = 50, Z = 10 },
            Rotation = new Vec3 { X = 0, Y = 0, Z = 270 },
            Teleport = "DoorA",
        });
        return spec;
    }

    private static IPlacedObjectGetter Door(BuildResult r, string ed) =>
        r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(o => o.EditorID == ed);

    // The CORE assertion: each door's XTEL points at the PARTNER door's FormKey.
    [Fact]
    public void Teleport_Xtel_PointsAtPartnerDoor()
    {
        var r = TestBuild.Ok(TwoDoorSpec());
        var a = Door(r, "DoorA");
        var b = Door(r, "DoorB");

        Assert.NotNull(a.TeleportDestination);
        Assert.NotNull(b.TeleportDestination);
        Assert.Equal(b.FormKey, a.TeleportDestination!.Door.FormKey);
        Assert.Equal(a.FormKey, b.TeleportDestination!.Door.FormKey);
    }

    // XTEL position/rotation is where the player MATERIALISES = the PARTNER door's pos/rot (rotation
    // converted deg->rad). Mirrors every vanilla load-door pair.
    [Fact]
    public void Teleport_ArrivalPoint_IsPartnerPositionAndRotation()
    {
        var r = TestBuild.Ok(TwoDoorSpec());
        var a = Door(r, "DoorA").TeleportDestination!;   // A arrives AT B
        var b = Door(r, "DoorB").TeleportDestination!;   // B arrives AT A

        // A's arrival == B's placed position (-200,50,10)
        Assert.Equal(-200f, a.Position.X, 3);
        Assert.Equal(50f, a.Position.Y, 3);
        Assert.Equal(10f, a.Position.Z, 3);
        Assert.Equal((float)(270 * Math.PI / 180), a.Rotation.Z, 3);

        // B's arrival == A's placed position (100,0,0) rot 90deg
        Assert.Equal(100f, b.Position.X, 3);
        Assert.Equal(0f, b.Position.Y, 3);
        Assert.Equal((float)(90 * Math.PI / 180), b.Rotation.Z, 3);
    }

    // Both doors must persist across save/load (a temporary teleport door breaks the link).
    [Fact]
    public void Teleport_Doors_AreForcedPersistent()
    {
        var r = TestBuild.Ok(TwoDoorSpec());
        var cells = r.Mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).ToList();
        var aCell = cells.Single(c => c.EditorID == "CellA");
        var bCell = cells.Single(c => c.EditorID == "CellB");

        Assert.Contains(aCell.Persistent, p => p.EditorID == "DoorA");
        Assert.Contains(bCell.Persistent, p => p.EditorID == "DoorB");
        Assert.DoesNotContain(aCell.Temporary, p => p.EditorID == "DoorA");
    }

    // A teleport to a non-existent in-spec partner warns and leaves no XTEL (rather than crashing).
    [Fact]
    public void Teleport_UnresolvedPartner_Warns_NoXtel()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Cells.Add(new CellSpec { EditorId = "CellA", Name = "A" });
        spec.Statics.Add(new StaticSpec { EditorId = "DoorBase", Model = @"x.nif" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "DoorA", Base = "DoorBase", Cell = "CellA", Teleport = "Nope",
        });
        var r = TestBuild.Raw(spec);
        Assert.Contains(r.Warnings, w => w.Contains("DoorA") && w.Contains("teleport"));
        Assert.Null(Door(r, "DoorA").TeleportDestination);
    }
}
