namespace ModForge;

// --- navmeshOverrides[]: re-emit a VANILLA navmesh from our plugin, unchanged ------------------
//
// P0 of workflows/plans/navmesh.md — the minimum falsifiable experiment for the whole navmesh
// route. It authors a NO-OP: every NAVM of the targeted vanilla cell is deep-copied into our
// plugin under its OWN FormKey (an override, not a new record) with not one byte of its geometry
// touched. The .esp then says "this cell's navmesh is mine now" while describing exactly the mesh
// the engine already had.
//
// Why ship a change that changes nothing: the FILE-FORMAT question is already settled (Mutagen's
// NVNM read/write round-trips byte-identical — cover table, opaque NavmeshGrid blob, cross-mesh
// EdgeLinks, DoorTriangles, the compressed-record flag; proven offline and re-provable any time
// with `navdiag <plugin>`). The ENGINE question is not: does the engine accept a NAVM that arrives
// from a plugin instead of the master, with the NAVI info-map still describing it from Skyrim.esm
// and the CELL/WRLD pulled in as overrides? If it does not, the NPCs in that cell simply stop
// moving — silently, with no error anywhere — and P2 (cut) / P3 (add) are dead before they start.
// So: change nothing, ship it, watch whether the guards still walk. A no-op is the only experiment
// whose failure has exactly one possible cause.
//
// 🔴 THE IRON RULE (plan §2): NEVER RENUMBER A TRIANGLE. A neighbouring mesh's EdgeLink stores an
// index INTO OUR triangle array; the CK renumbers on Finalize, which is why it is forced to re-save
// the surrounding cells' navmeshes too, and why "never edit navmesh outside the CK" became folklore.
// We deep-copy the array as-is: every index every neighbour holds stays valid, so no neighbour cell
// has to be touched. A no-op override renumbers nothing by construction — and P2/P3 must keep it
// that way (append at the tail, delete via the Deleted flag, never reorder).
//
// NAVI (the NavigationMeshInfoMap, Skyrim.esm:0x00012FB4) is deliberately NOT touched: the mesh's
// FormID is unchanged, so vanilla's own NVMI entry still describes it. (That is U4 of the plan, and
// P0 is what tests it. If it turns out an overridden mesh needs its own NVMI entry, the mechanism
// already exists — Generator.Build.Navmesh.cs WriteNaviInfoMap does an ADDITIVE override of 0x12FB4,
// never a new record, which is the one thing that reliably CTDs.)
//
// ⚠️ OFFLINE: the source mesh lives in the master, so with no Skyrim.esm nothing is emitted and the
// build is byte-identical to one without this section — no error, no warning. "Can't read it" is
// never reported as "it's broken".
//
// Two targeting forms — the same two a placement has:
//
//   { "cell": "Skyrim.esm:0x01605E" }                          // an INTERIOR vanilla cell (all its NAVMs)
//   { "worldspace": "Skyrim.esm:0x01A26F", "x": 5, "y": -2 }   // one EXTERIOR cell, by grid coords
//   { "worldspace": "Skyrim.esm:0x01A26F",                     // …or name the grid cell by a point in it
//     "position": { "x": 21750, "y": -7625, "z": 0 } }
//
// `navmesh` narrows a target to a single mesh (a cell can hold several): with it, only that NAVM is
// overridden; without it, every NAVM in the cell is.
public sealed class NavmeshOverrideSpec
{
    public string Cell { get; set; } = "";        // interior vanilla cell "<master>:0xFORMID"
    public string Worldspace { get; set; } = "";  // exterior worldspace "<master>:0xFORMID"
    public int? X { get; set; }                   // exterior cell grid X (with Y)
    public int? Y { get; set; }                   // exterior cell grid Y (with X)
    public Vec3? Position { get; set; }           // exterior: a world point — names the grid cell containing it
    public string Navmesh { get; set; } = "";     // optional: just this one NAVM ("<master>:0xFORMID")
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<NavmeshOverrideSpec> NavmeshOverrides { get; set; } = new(); // re-emit a VANILLA cell's NAVM(s) from our plugin, unchanged (no-op override). P0 of the navmesh plan: proves the engine accepts a navmesh that arrives from a patch. See Spec.NavmeshOverrides.cs / Generator.Build.NavmeshOverrides.cs
}
