namespace ModForge;

// --- cells[].navmeshGeometry: an AUTHORED NAVM triangle mesh for one exterior cell -------------
//
// Generator.Build.Navmesh.cs emits a flat 4-vertex quad for `"navmesh": true`. That is a floor,
// not a level: it is one plane at the cell's terrain height, so anything with an inside, an upper
// storey or a staircase gets NPCs pathing straight through it. This section is the escape hatch —
// hand the cell the actual walkable surface and ModForge writes it verbatim.
//
// Vertices are WORLD-space game units (the same convention the flat quad uses — NOT cell-local),
// so a triangle may legitimately overhang the cell border; the cell a triangle belongs to is
// whatever the author decided, and ModForge does not second-guess it.
//
// Triangle winding is the caller's responsibility: Skyrim expects counter-clockwise seen from
// above (the flat quad's V0=SW, V1=SE, V2=NE is the reference). A clockwise triangle is a
// "walkable ceiling" the engine will refuse to path on, and nothing in the file format says so.
//
// 🔴 The iron rule from Spec.NavmeshOverrides.cs applies to the EDGE INDICES here too: edge01 /
// edge12 / edge20 are indices INTO THIS CELL'S OWN triangles[] array, so the array's order IS the
// contract. Reordering triangles between two builds silently rewires the mesh.
//
//   { "x": 0, "y": 0, "navmesh": true,
//     "navmeshGeometry": {
//       "vertices": [ {"x": 2048, "y": 2048, "z": 19932}, ... ],
//       "triangles": [
//         { "v0": 0, "v1": 1, "v2": 2, "edge01": 3, "edge12": -1, "edge20": 7,
//           "links": [ { "edge": 1, "x": 1, "y": 0, "triangle": 42 } ] }
//       ] } }
//
// An edge is -1 when it is a border (no neighbour). `links` names a neighbour in a DIFFERENT
// cell — see NavmeshCellLinkSpec.

/// <summary>An authored NAVM mesh for one cell: a world-space vertex table plus its triangles.</summary>
public sealed class NavmeshGeometrySpec
{
    public List<Vec3> Vertices { get; set; } = new();
    public List<NavmeshGeometryTriangleSpec> Triangles { get; set; } = new();
}

/// <summary>
/// One NAVM triangle. v0/v1/v2 index this mesh's Vertices. edge01/edge12/edge20 are the
/// neighbouring TRIANGLE index across the edge between the named local vertices, or -1 for a
/// border edge. A neighbour in another cell goes in <see cref="Links"/> instead, not here.
/// </summary>
public sealed class NavmeshGeometryTriangleSpec
{
    public int V0 { get; set; }
    public int V1 { get; set; }
    public int V2 { get; set; }
    public int Edge01 { get; set; } = -1;
    public int Edge12 { get; set; } = -1;
    public int Edge20 { get; set; } = -1;
    public List<NavmeshCellLinkSpec> Links { get; set; } = new();
}

/// <summary>
/// A cross-cell neighbour for one edge of one triangle. Skyrim stores this indirectly: the
/// triangle's edge field holds an index into the mesh's EdgeLinks[] table (and the matching
/// edge-link flag is set); that table entry names the neighbouring NAVM and the triangle in it.
/// ModForge resolves X/Y to the neighbouring cell's NAVM FormKey after every cell has one.
///
/// Links are declared INDEPENDENTLY on both sides — cell A says "my triangle 5 edge 1 meets B's
/// triangle 42", and cell B says the mirror. ModForge does not synthesise the reciprocal, because
/// only the author knows which of B's three edges is the shared one.
/// </summary>
public sealed class NavmeshCellLinkSpec
{
    public int Edge { get; set; }        // which edge of THIS triangle: 0 = v0-v1, 1 = v1-v2, 2 = v2-v0
    public int X { get; set; }           // neighbouring cell grid X
    public int Y { get; set; }           // neighbouring cell grid Y
    public int Triangle { get; set; }    // triangle index inside that cell's navmeshGeometry
}
