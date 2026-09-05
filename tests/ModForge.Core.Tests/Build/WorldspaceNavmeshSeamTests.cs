using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class WorldspaceNavmeshSeamTests
{
    private static Vec3 V(float x, float y, float z) => new() { X = x, Y = y, Z = z };

    private static NavmeshGeometrySpec AuthoredQuad(int x, int y, float z)
    {
        float x0 = x * 4096f, y0 = y * 4096f;
        float x1 = x0 + 4096f, y1 = y0 + 4096f;
        return new NavmeshGeometrySpec
        {
            Vertices = { V(x0, y0, z), V(x1, y0, z), V(x1, y1, z), V(x0, y1, z) },
            Triangles =
            {
                new NavmeshGeometryTriangleSpec
                    { V0 = 0, V1 = 1, V2 = 2, Edge01 = -1, Edge12 = -1, Edge20 = 1 },
                new NavmeshGeometryTriangleSpec
                    { V0 = 0, V1 = 2, V2 = 3, Edge01 = 0, Edge12 = -1, Edge20 = -1 },
            },
        };
    }

    private static (BuildResult Result, NavigationMesh Left, NavigationMesh Right) BuildPair(float zDelta)
    {
        var world = new WorldspaceSpec { EditorId = "SeamWorld", Climate = "Skyrim.esm:0x000812" };
        world.Cells.Add(new WorldspaceCellSpec
        {
            X = 0, Y = 0, Height = 1000f, Navmesh = true,
            NavmeshGeometry = AuthoredQuad(0, 0, 1000f),
        });
        world.Cells.Add(new WorldspaceCellSpec
            { X = 1, Y = 0, Height = 1000f + zDelta, Navmesh = true });

        var result = Generator.Build(
            new ModSpec { Esl = false, Worldspaces = { world } },
            ModKey.FromNameAndExtension("NavmeshSeam.esp"));
        var cells = result.Mod.Worldspaces.Single().SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items)
            .ToDictionary(c => (c.Grid!.Point.X, c.Grid.Point.Y));
        return (result, cells[(0, 0)].NavigationMeshes.Single(), cells[(1, 0)].NavigationMeshes.Single());
    }

    [Fact]
    public void AuthoredGeometryAndFlatCell_GetReciprocalSeamAtTolerance()
    {
        Assert.Equal(128f, Generator.CustomNavmeshSeamMaxZDelta);
        var (result, authored, flat) = BuildPair(Generator.CustomNavmeshSeamMaxZDelta);
        Assert.Empty(result.Warnings);

        var a = authored.Data!;
        var b = flat.Data!;
        var aLink = Assert.Single(a.EdgeLinks);
        var bLink = Assert.Single(b.EdgeLinks);
        Assert.Equal(flat.FormKey, aLink.Mesh.FormKey);
        Assert.Equal(1, aLink.TriangleIndex);
        Assert.Equal(authored.FormKey, bLink.Mesh.FormKey);
        Assert.Equal(0, bLink.TriangleIndex);
        Assert.True(a.Triangles[0].Flags.HasFlag(NavmeshTriangle.Flag.EdgeLink_1_2));
        Assert.True(b.Triangles[1].Flags.HasFlag(NavmeshTriangle.Flag.EdgeLink_2_0));
    }

    [Fact]
    public void HeightDeltaOverTolerance_DoesNotLinkAndWarns()
    {
        var (result, authored, flat) = BuildPair(Generator.CustomNavmeshSeamMaxZDelta + 1f);

        Assert.Empty(authored.Data!.EdgeLinks);
        Assert.Empty(flat.Data!.EdgeLinks);
        var warning = Assert.Single(result.Warnings, w => w.Contains("navmesh seam cells (0,0)<->(1,0)"));
        Assert.Contains("Z delta 129", warning);
        Assert.Contains("CustomNavmeshSeamMaxZDelta=128", warning);
        Assert.Contains("(0,0) tri 0 edge 1", warning);
        Assert.Contains("(1,0) tri 1 edge 2", warning);
    }
}
