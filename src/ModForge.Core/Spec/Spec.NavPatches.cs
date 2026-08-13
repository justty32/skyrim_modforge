namespace ModForge;

// navPatches[] — append-only geometry edits to an existing vanilla interior NAVM.
// Existing vertices/triangles retain their indices; the new polygon is triangulated as a fan and
// must share exactly one complete boundary edge with the old mesh. See the durable design contract:
// workflows/specs/navmesh-patch-design.md.
public sealed class NavPatchSpec
{
    public string Cell { get; set; } = "";       // vanilla interior CELL: <master>:0xFORMID
    public string Navmesh { get; set; } = "";    // NAVM in that cell: <master>:0xFORMID
    public List<Vec3> Polygon { get; set; } = new();
    public string LinkTo { get; set; } = "auto"; // MVP supports only exact boundary-edge auto stitching
    public float Epsilon { get; set; } = 8f;      // maximum endpoint distance for a seam match, in game units
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<NavPatchSpec> NavPatches { get; set; } = new(); // append a convex walkable polygon to one vanilla interior NAVM, preserving every existing triangle index (P3 MVP). See Spec.NavPatches.cs / Generator.Build.NavPatches.cs
}
