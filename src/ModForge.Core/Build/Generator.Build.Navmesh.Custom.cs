using System.Buffers.Binary;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Authored per-cell NAVM geometry — the non-flat path.
    //
    //  Generator.Build.Navmesh.cs writes one plane per cell. That is enough for open ground and
    //  useless for anything with an interior, a roof or a staircase: NPCs get a single walkable
    //  altitude and path through the building. This file takes `cells[].navmeshGeometry` (see
    //  Spec.NavmeshGeometry.cs) and emits those triangles verbatim, plus the cross-cell EdgeLinks
    //  the author declared.
    //
    //  What it deliberately does NOT do: no re-winding, no welding, no retriangulation, no
    //  reordering. Triangle order is the contract (neighbouring meshes hold indices into it), and
    //  silently "fixing" geometry is how a navmesh tool becomes unpredictable. Bad input is
    //  REPORTED and the cell falls back to the flat quad, so a broken mesh is visible in the build
    //  log instead of being an in-game mystery.
    // -------------------------------------------------------------------------------

    // Mutagen stores triangle indices as Int16 and vertex indices as Int16 inside P3Int16, so a
    // single cell's mesh is bounded by those. 32,767 triangles per cell is far past anything the
    // engine handles well anyway.
    private const int MaxNavmeshVerticesPerCell = short.MaxValue;
    private const int MaxNavmeshTrianglesPerCell = short.MaxValue;

    // Emit one cell's navmesh: the authored mesh when there is one, otherwise the flat quad.
    private static void AddCellNavmesh(
        SkyrimMod mod, Cell cell, WorldspaceCellSpec cs, FormKey worldspaceFk,
        List<NavmCellInfo> navmInfos, Action<string> warn)
    {
        if (cs.NavmeshGeometry is { } geom && geom.Triangles.Count > 0)
        {
            if (TryAddCustomCellNavmesh(mod, cell, cs, geom, worldspaceFk, navmInfos, warn)) return;
            // Validation failed and said why. A cell with no navmesh at all is worse than a flat
            // one (NPCs in it stop dead), so still lay the floor.
        }
        AddFlatCellNavmesh(mod, cell, cs, worldspaceFk, navmInfos);
    }

    // Returns false (having warned) if the authored mesh is unusable; the caller then falls back.
    private static bool TryAddCustomCellNavmesh(
        SkyrimMod mod, Cell cell, WorldspaceCellSpec cs, NavmeshGeometrySpec geom,
        FormKey worldspaceFk, List<NavmCellInfo> navmInfos, Action<string> warn)
    {
        string where = $"worldspace cell ({cs.X},{cs.Y}) navmeshGeometry";
        int nv = geom.Vertices.Count, nt = geom.Triangles.Count;

        if (nv < 3)
        { warn($"  ! {where} has {nv} vertices (need >= 3) — falling back to a flat navmesh"); return false; }
        if (nv > MaxNavmeshVerticesPerCell)
        { warn($"  ! {where} has {nv} vertices (max {MaxNavmeshVerticesPerCell}) — falling back to a flat navmesh"); return false; }
        if (nt > MaxNavmeshTrianglesPerCell)
        { warn($"  ! {where} has {nt} triangles (max {MaxNavmeshTrianglesPerCell}) — falling back to a flat navmesh"); return false; }

        for (int i = 0; i < nt; i++)
        {
            var t = geom.Triangles[i];
            foreach (var (name, vi) in new[] { ("v0", t.V0), ("v1", t.V1), ("v2", t.V2) })
                if (vi < 0 || vi >= nv)
                { warn($"  ! {where} triangle {i} {name}={vi} is out of range (0..{nv - 1}) — falling back to a flat navmesh"); return false; }
            if (t.V0 == t.V1 || t.V1 == t.V2 || t.V2 == t.V0)
            { warn($"  ! {where} triangle {i} is degenerate (repeated vertex index) — falling back to a flat navmesh"); return false; }
            foreach (var (name, ei) in new[] { ("edge01", t.Edge01), ("edge12", t.Edge12), ("edge20", t.Edge20) })
                if (ei < -1 || ei >= nt)
                { warn($"  ! {where} triangle {i} {name}={ei} is out of range (-1..{nt - 1}) — falling back to a flat navmesh"); return false; }
            foreach (var link in t.Links)
                if (link.Edge is < 0 or > 2)
                { warn($"  ! {where} triangle {i} link edge={link.Edge} must be 0, 1 or 2 — falling back to a flat navmesh"); return false; }
        }

        var navm = new NavigationMesh(mod);
        var data = new NavigationMeshData
        {
            NavmeshVersion = NavigationMeshData.NavmeshVersionDefault,
            // Same CK magic the flat path stamps (xEdit's default 3C A0 E9 A5 → 0xA5E9A03C).
            CrcHash = 0xA5E9A03C,
        };
        var navParent = new WorldspaceNavmeshParent();
        navParent.Parent.SetTo(worldspaceFk);
        data.Parent = navParent;

        foreach (var v in geom.Vertices)
            data.Vertices.Add(new Noggog.P3Float(v.X, v.Y, v.Z));

        // In-cell neighbours go straight into the edge fields WITHOUT the edge-link flag; that flag
        // means "this number is an index into EdgeLinks[]" and is set later, per declared link.
        foreach (var t in geom.Triangles)
            data.Triangles.Add(new NavmeshTriangle
            {
                Vertices = new Noggog.P3Int16((short)t.V0, (short)t.V1, (short)t.V2),
                EdgeLink_0_1 = (short)t.Edge01,
                EdgeLink_1_2 = (short)t.Edge12,
                EdgeLink_2_0 = (short)t.Edge20,
            });

        // One-bucket grid: divisor 1 means the whole mesh is a single spatial bucket listing every
        // triangle — [int32 count][uint16 index]*count. Identical to Formats/NavmeshPatch.cs.
        var grid = new byte[4 + nt * 2];
        BinaryPrimitives.WriteInt32LittleEndian(grid.AsSpan(0, 4), nt);
        for (int i = 0; i < nt; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(grid.AsSpan(4 + i * 2, 2), (ushort)i);
        data.NavmeshGrid = grid;
        data.NavmeshGridDivisor = 1;

        // Bounds come from the actual vertices, not the cell box: an authored mesh may overhang.
        float minX = geom.Vertices.Min(v => v.X), maxX = geom.Vertices.Max(v => v.X);
        float minY = geom.Vertices.Min(v => v.Y), maxY = geom.Vertices.Max(v => v.Y);
        float minZ = geom.Vertices.Min(v => v.Z), maxZ = geom.Vertices.Max(v => v.Z);
        data.Min = new Noggog.P3Float(minX, minY, minZ);
        data.Max = new Noggog.P3Float(maxX, maxY, maxZ);
        data.MaxDistanceX = maxX - minX;
        data.MaxDistanceY = maxY - minY;

        navm.Data = data;
        cell.NavigationMeshes.Add(navm);
        navmInfos.Add(new NavmCellInfo(navm, cs.X, cs.Y,
            new Noggog.P3Float((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f),
            data.Min, data.Max, worldspaceFk, geom));
        return true;
    }

    // Resolve every authored cross-cell link now that all cells have FormKeys. Mirrors what
    // ConnectAdjacentCellNavmeshes does for flat quads, but the seams are named by the author
    // rather than derived from grid adjacency — an authored mesh has no predictable edge to guess.
    private static void ConnectSpecExternalEdges(
        IReadOnlyList<NavmCellInfo> navmInfos, Action<string> warn)
    {
        var byCell = new Dictionary<(FormKey Ws, int X, int Y), NavmCellInfo>();
        foreach (var info in navmInfos)
            byCell.TryAdd((info.WorldspaceFk, info.X, info.Y), info);

        foreach (var info in navmInfos)
        {
            if (info.Geometry is not { } geom) continue;
            var data = info.Navm.Data!;
            for (int ti = 0; ti < geom.Triangles.Count; ti++)
            {
                foreach (var link in geom.Triangles[ti].Links)
                {
                    string what = $"worldspace cell ({info.X},{info.Y}) navmeshGeometry triangle {ti} link";
                    if (!byCell.TryGetValue((info.WorldspaceFk, link.X, link.Y), out var target))
                    { warn($"  ! {what} names cell ({link.X},{link.Y}), which has no navmesh — link dropped"); continue; }
                    if (target.Geometry is not { } targetGeom)
                    { warn($"  ! {what} names cell ({link.X},{link.Y}), which is a flat navmesh with no triangle {link.Triangle} — link dropped"); continue; }
                    if (link.Triangle < 0 || link.Triangle >= targetGeom.Triangles.Count)
                    { warn($"  ! {what} names triangle {link.Triangle} in cell ({link.X},{link.Y}), which has {targetGeom.Triangles.Count} — link dropped"); continue; }

                    var index = checked((short)data.EdgeLinks.Count);
                    var el = new EdgeLink { Unknown = 0, TriangleIndex = (short)link.Triangle };
                    el.Mesh.SetTo(target.Navm.FormKey);
                    data.EdgeLinks.Add(el);

                    var tri = data.Triangles[ti];
                    switch (link.Edge)
                    {
                        case 0: tri.EdgeLink_0_1 = index; tri.Flags |= NavmeshTriangle.Flag.EdgeLink_0_1; break;
                        case 1: tri.EdgeLink_1_2 = index; tri.Flags |= NavmeshTriangle.Flag.EdgeLink_1_2; break;
                        default: tri.EdgeLink_2_0 = index; tri.Flags |= NavmeshTriangle.Flag.EdgeLink_2_0; break;
                    }
                }
            }
        }
    }
}
