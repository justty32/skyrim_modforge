using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// cells[].navmeshGeometry — an authored NAVM mesh replacing the flat quad.
// See src/ModForge.Core/Build/Generator.Build.Navmesh.Custom.cs.
public class WorldspaceNavmeshGeometryTests
{
    private static Vec3 V(float x, float y, float z) => new() { X = x, Y = y, Z = z };

    // A 2-storey box in cell (0,0): a lower floor and an upper floor directly above it. The point
    // a flat quad cannot express — two walkable altitudes over the same ground.
    private static NavmeshGeometrySpec TwoStoreyMesh() => new()
    {
        Vertices =
        {
            V(100, 100, 1000), V(3000, 100, 1000), V(3000, 3000, 1000), V(100, 3000, 1000),
            V(100, 100, 1400), V(3000, 100, 1400), V(3000, 3000, 1400), V(100, 3000, 1400),
        },
        Triangles =
        {
            new NavmeshGeometryTriangleSpec { V0 = 0, V1 = 1, V2 = 2, Edge01 = -1, Edge12 = -1, Edge20 = 1 },
            new NavmeshGeometryTriangleSpec { V0 = 0, V1 = 2, V2 = 3, Edge01 = 0, Edge12 = -1, Edge20 = -1 },
            new NavmeshGeometryTriangleSpec { V0 = 4, V1 = 5, V2 = 6, Edge01 = -1, Edge12 = -1, Edge20 = 3 },
            new NavmeshGeometryTriangleSpec { V0 = 4, V1 = 6, V2 = 7, Edge01 = 2, Edge12 = -1, Edge20 = -1 },
        },
    };

    private static SkyrimMod Build(WorldspaceSpec world) => (SkyrimMod)Generator.Build(
        new ModSpec { Esl = false, Worldspaces = { world } },
        ModKey.FromNameAndExtension("NavmeshGeom.esp")).Mod;

    private static Dictionary<(int, int), NavigationMesh> Meshes(SkyrimMod mod, string editorId) =>
        mod.Worldspaces.Single(w => w.EditorID == editorId).SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items)
            .Where(c => c.NavigationMeshes.Count > 0)
            .ToDictionary(c => (c.Grid!.Point.X, c.Grid.Point.Y), c => c.NavigationMeshes.Single());

    private static WorldspaceSpec World(string ed = "GeomWorld") =>
        new() { EditorId = ed, Climate = "Skyrim.esm:0x000812" };

    // 1. The authored vertices and triangles reach the record verbatim — no re-winding, no welding,
    //    no reordering (triangle order is the contract every neighbour's index depends on).
    [Fact]
    public void AuthoredGeometry_IsEmittedVerbatim_NotAFlatQuad()
    {
        var world = World();
        world.Cells.Add(new WorldspaceCellSpec
        { X = 0, Y = 0, Height = 1000, Navmesh = true, NavmeshGeometry = TwoStoreyMesh() });

        var data = Meshes(Build(world), "GeomWorld")[(0, 0)].Data!;

        Assert.Equal(8, data.Vertices.Count);
        Assert.Equal(4, data.Triangles.Count);
        Assert.Equal(new Noggog.P3Float(100, 100, 1000), data.Vertices[0]);
        Assert.Equal(new Noggog.P3Float(100, 3000, 1400), data.Vertices[7]);
        Assert.Equal(new Noggog.P3Int16(4, 5, 6), data.Triangles[2].Vertices);
        // Two distinct walkable altitudes over the same footprint — the whole point.
        Assert.Equal(1000f, data.Min.Z);
        Assert.Equal(1400f, data.Max.Z);
    }

    // 2. In-cell neighbours land in the edge fields WITHOUT the edge-link flag. The flag means
    //    "this number indexes EdgeLinks[]"; setting it for a local neighbour would send the engine
    //    to a table entry that does not exist.
    [Fact]
    public void InCellNeighbours_UseRawTriangleIndices_WithNoEdgeLinkFlag()
    {
        var world = World();
        world.Cells.Add(new WorldspaceCellSpec
        { X = 0, Y = 0, Height = 1000, Navmesh = true, NavmeshGeometry = TwoStoreyMesh() });

        var data = Meshes(Build(world), "GeomWorld")[(0, 0)].Data!;

        Assert.Equal(1, data.Triangles[0].EdgeLink_2_0);
        Assert.Equal(0, data.Triangles[1].EdgeLink_0_1);
        Assert.Equal(-1, data.Triangles[0].EdgeLink_0_1);
        Assert.Equal(3, data.Triangles[2].EdgeLink_2_0);
        Assert.Empty(data.EdgeLinks);
        foreach (var t in data.Triangles)
        {
            Assert.False(t.Flags.HasFlag(NavmeshTriangle.Flag.EdgeLink_0_1));
            Assert.False(t.Flags.HasFlag(NavmeshTriangle.Flag.EdgeLink_1_2));
            Assert.False(t.Flags.HasFlag(NavmeshTriangle.Flag.EdgeLink_2_0));
        }
    }

    // 3. The one-bucket grid must list EVERY triangle: [int32 count][uint16 index]*count, divisor 1.
    //    A short list silently hides triangles from the engine's spatial query.
    [Fact]
    public void OneBucketGrid_ListsEveryTriangle()
    {
        var world = World();
        world.Cells.Add(new WorldspaceCellSpec
        { X = 0, Y = 0, Height = 1000, Navmesh = true, NavmeshGeometry = TwoStoreyMesh() });

        var data = Meshes(Build(world), "GeomWorld")[(0, 0)].Data!;

        var grid = data.NavmeshGrid.ToArray();
        Assert.Equal(1u, data.NavmeshGridDivisor);
        Assert.Equal(4 + 4 * 2, grid.Length);
        Assert.Equal(4, BinaryPrimitives.ReadInt32LittleEndian(grid.AsSpan(0, 4)));
        for (int i = 0; i < 4; i++)
            Assert.Equal((ushort)i,
                BinaryPrimitives.ReadUInt16LittleEndian(grid.AsSpan(4 + i * 2, 2)));
        // Bounds come from the vertices, not the 4096 cell box.
        Assert.Equal(2900f, data.MaxDistanceX);
        Assert.Equal(2900f, data.MaxDistanceY);
    }

    // 4. A declared cross-cell link becomes an EdgeLinks[] entry naming the neighbouring NAVM, with
    //    the triangle's edge field pointing at that entry and the matching flag set.
    [Fact]
    public void CrossCellLink_ResolvesToNeighbourNavmeshFormKey()
    {
        var world = World();
        var left = TwoStoreyMesh();
        left.Triangles[0].Links.Add(new NavmeshCellLinkSpec { Edge = 1, X = 1, Y = 0, Triangle = 3 });
        var right = TwoStoreyMesh();
        right.Triangles[3].Links.Add(new NavmeshCellLinkSpec { Edge = 2, X = 0, Y = 0, Triangle = 0 });

        world.Cells.Add(new WorldspaceCellSpec { X = 0, Y = 0, Height = 1000, Navmesh = true, NavmeshGeometry = left });
        world.Cells.Add(new WorldspaceCellSpec { X = 1, Y = 0, Height = 1000, Navmesh = true, NavmeshGeometry = right });

        var meshes = Meshes(Build(world), "GeomWorld");
        var a = meshes[(0, 0)].Data!;
        var b = meshes[(1, 0)].Data!;

        var linkA = Assert.Single(a.EdgeLinks);
        Assert.Equal(meshes[(1, 0)].FormKey, linkA.Mesh.FormKey);
        Assert.Equal(3, linkA.TriangleIndex);
        Assert.True(a.Triangles[0].Flags.HasFlag(NavmeshTriangle.Flag.EdgeLink_1_2));
        Assert.Equal(0, a.Triangles[0].EdgeLink_1_2);   // index INTO EdgeLinks[], not a triangle

        var linkB = Assert.Single(b.EdgeLinks);
        Assert.Equal(meshes[(0, 0)].FormKey, linkB.Mesh.FormKey);
        Assert.Equal(0, linkB.TriangleIndex);
        Assert.True(b.Triangles[3].Flags.HasFlag(NavmeshTriangle.Flag.EdgeLink_2_0));
    }

    // 5. Two authored neighbours must NOT also get the flat-quad grid seam bolted on: that seam
    //    names triangle 0/1 and the quad's V1-V2 edge, which mean nothing in an authored mesh.
    [Fact]
    public void AdjacentAuthoredCells_DoNotGetTheFlatQuadSeam()
    {
        var world = World();
        world.Cells.Add(new WorldspaceCellSpec { X = 0, Y = 0, Height = 1000, Navmesh = true, NavmeshGeometry = TwoStoreyMesh() });
        world.Cells.Add(new WorldspaceCellSpec { X = 1, Y = 0, Height = 1000, Navmesh = true, NavmeshGeometry = TwoStoreyMesh() });

        var meshes = Meshes(Build(world), "GeomWorld");

        Assert.Empty(meshes[(0, 0)].Data!.EdgeLinks);
        Assert.Empty(meshes[(1, 0)].Data!.EdgeLinks);
    }

    // 6. Bad input falls back to the flat quad AND says so. Silence here is the failure mode that
    //    turns into "my NPCs don't move" with nothing in any log.
    [Fact]
    public void OutOfRangeVertexIndex_WarnsAndFallsBackToFlatQuad()
    {
        var world = World();
        var bad = TwoStoreyMesh();
        bad.Triangles[1].V2 = 99;
        world.Cells.Add(new WorldspaceCellSpec { X = 0, Y = 0, Height = 1000, Navmesh = true, NavmeshGeometry = bad });

        var result = Generator.Build(
            new ModSpec { Esl = false, Worldspaces = { world } },
            ModKey.FromNameAndExtension("NavmeshGeom.esp"));
        var warnings = result.Warnings;

        var data = Meshes((SkyrimMod)result.Mod, "GeomWorld")[(0, 0)].Data!;
        Assert.Equal(4, data.Vertices.Count);      // the flat quad, not the 8-vertex mesh
        Assert.Equal(2, data.Triangles.Count);
        Assert.Contains(warnings, w => w.Contains("v2=99") && w.Contains("out of range"));
    }

    // 7. A link naming a cell that has no navmesh is dropped with a warning rather than throwing or
    //    writing an EdgeLink to a FormKey that is not there.
    [Fact]
    public void LinkToMissingCell_IsDroppedWithAWarning()
    {
        var world = World();
        var geom = TwoStoreyMesh();
        geom.Triangles[0].Links.Add(new NavmeshCellLinkSpec { Edge = 1, X = 9, Y = 9, Triangle = 0 });
        world.Cells.Add(new WorldspaceCellSpec { X = 0, Y = 0, Height = 1000, Navmesh = true, NavmeshGeometry = geom });

        var result = Generator.Build(
            new ModSpec { Esl = false, Worldspaces = { world } },
            ModKey.FromNameAndExtension("NavmeshGeom.esp"));
        var warnings = result.Warnings;

        Assert.Empty(Meshes((SkyrimMod)result.Mod, "GeomWorld")[(0, 0)].Data!.EdgeLinks);
        Assert.Contains(warnings, w => w.Contains("(9,9)") && w.Contains("no navmesh"));
    }

    // 8. navmeshGeometry without navmesh:true authors nothing — the flag stays the single switch
    //    that decides whether a cell has a navmesh at all.
    [Fact]
    public void GeometryWithoutNavmeshFlag_AuthorsNoNavmesh()
    {
        var world = World();
        world.Cells.Add(new WorldspaceCellSpec { X = 0, Y = 0, Height = 1000, Navmesh = false, NavmeshGeometry = TwoStoreyMesh() });

        Assert.Empty(Meshes(Build(world), "GeomWorld"));
    }

    // 9. A cell with no geometry still gets exactly the flat quad it always did (no regression for
    //    every existing spec in the wild).
    [Fact]
    public void CellWithoutGeometry_StillGetsTheFlatQuad()
    {
        var world = World();
        world.Cells.Add(new WorldspaceCellSpec { X = 0, Y = 0, Height = 1000, Navmesh = true });

        var data = Meshes(Build(world), "GeomWorld")[(0, 0)].Data!;
        Assert.Equal(4, data.Vertices.Count);
        Assert.Equal(2, data.Triangles.Count);
        Assert.Equal(4096f, data.MaxDistanceX);
    }

    // 10. The NAVI info map (the additive 0x12FB4 override) must index an authored mesh too,
    //     otherwise cross-cell path queries never find it.
    [Fact]
    public void AuthoredMesh_IsIndexedInTheNaviInfoMap()
    {
        var world = World();
        world.Cells.Add(new WorldspaceCellSpec
        { X = 0, Y = 0, Height = 1000, Navmesh = true, NavmeshGeometry = TwoStoreyMesh() });

        var mod = Build(world);
        var navm = Meshes(mod, "GeomWorld")[(0, 0)];
        var navi = mod.NavigationMeshInfoMaps.Single();
        var info = Assert.Single(navi.MapInfos);

        Assert.Equal(navm.FormKey, info.NavigationMesh.FormKey);
        // Centre of the authored bounds, not the cell centre.
        Assert.Equal(1550f, info.Point.X);
        Assert.Equal(1200f, info.Point.Z);
    }
}
