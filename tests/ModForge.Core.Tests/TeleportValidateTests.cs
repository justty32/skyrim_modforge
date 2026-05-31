using System.Linq;
using ModForge;

namespace ModForge.Tests;

// Validate-side rules for load-door teleports (caught before Build, so an LLM-authored spec can
// self-correct): partner resolves, door is named, no self-link, reciprocity for in-spec pairs.
public class TeleportValidateTests
{
    private static ModSpec Pair(string aTeleport, string bTeleport)
    {
        var spec = new ModSpec();
        spec.Cells.Add(new CellSpec { EditorId = "CellA", Name = "A" });
        spec.Cells.Add(new CellSpec { EditorId = "CellB", Name = "B" });
        spec.Statics.Add(new StaticSpec { EditorId = "DoorBase", Model = "x.nif" });
        spec.Placements.Add(new PlacementSpec { EditorId = "DoorA", Base = "DoorBase", Cell = "CellA", Teleport = aTeleport });
        spec.Placements.Add(new PlacementSpec { EditorId = "DoorB", Base = "DoorBase", Cell = "CellB", Teleport = bTeleport });
        return spec;
    }

    [Fact]
    public void ReciprocalPair_IsValid()
    {
        Assert.Empty(Generator.Validate(Pair("DoorB", "DoorA")));
    }

    [Fact]
    public void OneWayInSpecLink_IsFlagged()
    {
        // DoorA -> DoorB, but DoorB doesn't point back.
        var problems = Generator.Validate(Pair("DoorB", ""));
        Assert.Contains(problems, p => p.Contains("DoorA") && p.Contains("does not teleport back"));
    }

    [Fact]
    public void TeleportToUnknownEditorId_IsUnresolved()
    {
        var problems = Generator.Validate(Pair("Ghost", "DoorA"));
        Assert.Contains(problems, p => p.Contains("teleport partner") && p.Contains("Ghost"));
    }

    [Fact]
    public void SelfTeleport_IsFlagged()
    {
        var spec = new ModSpec();
        spec.Cells.Add(new CellSpec { EditorId = "CellA", Name = "A" });
        spec.Statics.Add(new StaticSpec { EditorId = "DoorBase", Model = "x.nif" });
        spec.Placements.Add(new PlacementSpec { EditorId = "DoorA", Base = "DoorBase", Cell = "CellA", Teleport = "DoorA" });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("DoorA") && p.Contains("itself"));
    }

    [Fact]
    public void Teleport_WithoutEditorId_IsFlagged()
    {
        var spec = new ModSpec();
        spec.Cells.Add(new CellSpec { EditorId = "CellA", Name = "A" });
        spec.Statics.Add(new StaticSpec { EditorId = "DoorBase", Model = "x.nif" });
        // A door with a teleport but no editorId — its partner can't link back.
        spec.Placements.Add(new PlacementSpec { Base = "DoorBase", Cell = "CellA", Teleport = "Skyrim.esm:0x013424" });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("teleport") && p.Contains("no editorId"));
    }

    // A teleport to a VANILLA door (external ref) needs no reciprocity check (can't edit the master).
    [Fact]
    public void TeleportToVanillaDoor_NeedsNoReciprocity()
    {
        var spec = new ModSpec();
        spec.Cells.Add(new CellSpec { EditorId = "CellA", Name = "A" });
        spec.Statics.Add(new StaticSpec { EditorId = "DoorBase", Model = "x.nif" });
        spec.Placements.Add(new PlacementSpec { EditorId = "DoorA", Base = "DoorBase", Cell = "CellA", Teleport = "Skyrim.esm:0x013424" });
        Assert.Empty(Generator.Validate(spec));
    }
}
