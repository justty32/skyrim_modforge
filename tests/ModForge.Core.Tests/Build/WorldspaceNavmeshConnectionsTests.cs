using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class WorldspaceNavmeshConnectionsTests
{
    [Fact]
    public void AdjacentTwoByTwoCells_HaveReciprocalExternalEdgeLinks()
    {
        var world = new WorldspaceSpec { EditorId = "LinkedWorld", Climate = "Skyrim.esm:0x000812" };
        world.Cells.Add(new WorldspaceCellSpec { X = 0, Y = 0, Height = 1000, Navmesh = true });
        world.Cells.Add(new WorldspaceCellSpec { X = 1, Y = 0, Height = 2000, Navmesh = true });
        world.Cells.Add(new WorldspaceCellSpec { X = 0, Y = 1, Height = 3000, Navmesh = true });
        world.Cells.Add(new WorldspaceCellSpec { X = 1, Y = 1, Height = 4000, Navmesh = true });

        var mod = Generator.Build(
            new ModSpec { Esl = false, Worldspaces = { world } },
            ModKey.FromNameAndExtension("NavmeshLinks.esp")).Mod;
        var cells = mod.Worldspaces.Single(w => w.EditorID == "LinkedWorld").SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items)
            .ToDictionary(c => (c.Grid!.Point.X, c.Grid.Point.Y));
        var meshes = cells.ToDictionary(x => x.Key, x => x.Value.NavigationMeshes.Single());

        Assert.Equal(8, meshes.Values.Sum(n => n.Data!.EdgeLinks.Count));
        AssertLink(meshes[(0, 0)], 0, NavmeshTriangle.Flag.EdgeLink_1_2,
            t => t.EdgeLink_1_2, meshes[(1, 0)], targetTriangle: 1);
        AssertLink(meshes[(1, 0)], 1, NavmeshTriangle.Flag.EdgeLink_2_0,
            t => t.EdgeLink_2_0, meshes[(0, 0)], targetTriangle: 0);
        AssertLink(meshes[(0, 0)], 1, NavmeshTriangle.Flag.EdgeLink_1_2,
            t => t.EdgeLink_1_2, meshes[(0, 1)], targetTriangle: 0);
        AssertLink(meshes[(0, 1)], 0, NavmeshTriangle.Flag.EdgeLink_0_1,
            t => t.EdgeLink_0_1, meshes[(0, 0)], targetTriangle: 1);

        Assert.Equal(1000f, meshes[(0, 0)].Data!.Vertices[0].Z);
        Assert.Equal(2000f, meshes[(1, 0)].Data!.Vertices[0].Z);
    }

    private static void AssertLink(
        NavigationMesh from, int triangle, NavmeshTriangle.Flag flag,
        Func<INavmeshTriangleGetter, short> edgeIndex, NavigationMesh to, short targetTriangle)
    {
        var data = from.Data!;
        var tri = data.Triangles[triangle];
        Assert.True(tri.Flags.HasFlag(flag));
        var link = data.EdgeLinks[edgeIndex(tri)];
        Assert.Equal(to.FormKey, link.Mesh.FormKey);
        Assert.Equal(targetTriangle, link.TriangleIndex);
        Assert.Equal(0, link.Unknown); // xEdit's Portal type
    }
}
