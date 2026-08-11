using System.Buffers.Binary;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class NavmeshPatchTests
{
    private static NavigationMesh TriangleMesh()
    {
        var navm = new NavigationMesh(FormKey.Factory("000802:MFFakeNav.esm"), SkyrimRelease.SkyrimSE);
        var data = new NavigationMeshData();
        data.Vertices.Add(new Noggog.P3Float(0, 0, 0));
        data.Vertices.Add(new Noggog.P3Float(100, 0, 0));
        data.Vertices.Add(new Noggog.P3Float(0, 100, 0));
        data.Triangles.Add(new NavmeshTriangle
        {
            Vertices = new Noggog.P3Int16(0, 1, 2),
            EdgeLink_0_1 = -1, EdgeLink_1_2 = -1, EdgeLink_2_0 = -1,
        });
        navm.Data = data;
        return navm;
    }

    private static List<Vec3> Platform() =>
    [
        new() { X = 0, Y = 0, Z = 0 },
        new() { X = 100, Y = 0, Z = 0 },
        new() { X = 100, Y = -100, Z = 0 },
        new() { X = 0, Y = -100, Z = 0 },
    ];

    [Fact]
    public void Validate_RejectsConcaveAndUnsupportedLinkMode()
    {
        var s = new ModSpec();
        s.NavPatches.Add(new NavPatchSpec
        {
            Cell = "MFFakeNav.esm:0x000801", Navmesh = "MFFakeNav.esm:0x000802", LinkTo = "island",
            Polygon =
            [
                new() { X = 0, Y = 0 }, new() { X = 100, Y = 0 }, new() { X = 50, Y = 20 },
                new() { X = 100, Y = 100 }, new() { X = 0, Y = 100 },
            ],
        });

        var problems = Generator.Validate(s);
        Assert.Contains(problems, p => p.Contains("only 'auto'"));
        Assert.Contains(problems, p => p.Contains("strictly convex"));
    }

    [Fact]
    public void Apply_AppendsFan_StitchesOneSeam_AndNeverRenumbersOldTriangle()
    {
        var navm = TriangleMesh();
        var oldTriangle = navm.Data!.Triangles[0];
        var oldVertices = oldTriangle.Vertices;

        Assert.True(NavmeshPatch.TryApply(navm, Platform(), 0.01f, out var error), error);
        var data = navm.Data!;

        Assert.Equal(7, data.Vertices.Count);       // 3 old + 4 appended
        Assert.Equal(3, data.Triangles.Count);      // 1 old + 2 fan triangles
        Assert.Equal(oldVertices, data.Triangles[0].Vertices);
        Assert.Equal(2, data.Triangles[0].EdgeLink_0_1); // old seam points at its appended fan triangle
        Assert.Equal(0, data.Triangles[2].EdgeLink_1_2); // appended seam points back at old triangle
        Assert.Equal(2, data.Triangles[1].EdgeLink_2_0); // fan diagonal is linked both ways
        Assert.Equal(1, data.Triangles[2].EdgeLink_0_1);

        Assert.Equal(1u, data.NavmeshGridDivisor);
        Assert.Equal(3, BinaryPrimitives.ReadInt32LittleEndian(data.NavmeshGrid.ToArray().AsSpan(0, 4)));
        Assert.Equal(10, data.NavmeshGrid.Length); // int32 count + three uint16 triangle indices
        Assert.Equal(-100, data.Min.Y);
        Assert.Equal(100, data.MaxDistanceX);
        Assert.Equal(200, data.MaxDistanceY);
    }

    [Fact]
    public void Apply_WithNoSeam_FailsBeforeMutatingCandidate()
    {
        var navm = TriangleMesh();
        var polygon = Platform();
        foreach (var p in polygon) p.X += 1000;

        Assert.False(NavmeshPatch.TryApply(navm, polygon, 0.01f, out var error));
        Assert.Contains("no complete polygon edge", error);
        Assert.Equal(3, navm.Data!.Vertices.Count);
        Assert.Single(navm.Data.Triangles);
        Assert.Equal(-1, navm.Data.Triangles[0].EdgeLink_0_1);
    }

    [Fact]
    public void Apply_WithMultipleSeams_IsAmbiguousAndDoesNotMutate()
    {
        var navm = TriangleMesh();
        var sameTriangle = new List<Vec3>
        {
            new() { X = 0, Y = 0 }, new() { X = 100, Y = 0 }, new() { X = 0, Y = 100 },
        };

        Assert.False(NavmeshPatch.TryApply(navm, sameTriangle, 0.01f, out var error));
        Assert.Contains("ambiguous", error);
        Assert.Equal(3, navm.Data!.Vertices.Count);
        Assert.Single(navm.Data.Triangles);
    }

    [Fact]
    public void Apply_SecondPatchCanStitchToTheFirstPatchesNewBoundary()
    {
        var navm = TriangleMesh();
        Assert.True(NavmeshPatch.TryApply(navm, Platform(), 0.01f, out var first), first);
        var next = new List<Vec3>
        {
            new() { X = 0, Y = -100 }, new() { X = 100, Y = -100 },
            new() { X = 100, Y = -200 }, new() { X = 0, Y = -200 },
        };

        Assert.True(NavmeshPatch.TryApply(navm, next, 0.01f, out var second), second);
        Assert.Equal(11, navm.Data!.Vertices.Count);
        Assert.Equal(5, navm.Data.Triangles.Count);
        Assert.Equal(4 + 5 * 2, navm.Data.NavmeshGrid.Length);
    }

    [Fact]
    public void NoMaster_IsOfflineSafeAndEmitsNoNavm()
    {
        var s = new ModSpec();
        s.NavPatches.Add(new NavPatchSpec
        {
            Cell = "MFFakeNav.esm:0x000801", Navmesh = "MFFakeNav.esm:0x000802", Polygon = Platform(),
        });

        var r = Generator.Build(s, ModKey.FromNameAndExtension("MFNavPatch.esp"),
            new BuildOptions { SkyrimDataPath = "/nonexistent" });
        Assert.Equal(0, r.Stats.NavPatches);
        Assert.Empty(r.Mod.EnumerateMajorRecords<INavigationMeshGetter>());
        Assert.DoesNotContain(r.Warnings, w => w.Contains("navPatch"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void BanneredMare_BuildIsAppendOnlyAndPublishesAStitchedOverride()
    {
        const string cellRef = "Skyrim.esm:0x01605E";
        const string navmRef = "Skyrim.esm:0x0C9064";
        var vanilla = VanillaBanneredMare();
        var polygon = FindWorkingPlatform(vanilla);
        int oldVertexCount = vanilla.Data!.Vertices.Count;
        int oldTriangleCount = vanilla.Data.Triangles.Count;

        var s = new ModSpec { PluginName = "MFNavPatch.esp", Esl = false };
        s.NavPatches.Add(new NavPatchSpec { Cell = cellRef, Navmesh = navmRef, Polygon = polygon, Epsilon = 0.01f });
        var r = TestBuild.Raw(s);

        Assert.Equal(1, r.Stats.NavPatches);
        Assert.Equal(0, r.Stats.NavmeshOverrides);
        Assert.DoesNotContain(r.Warnings, w => w.Contains("navPatch"));
        Assert.Empty(r.Mod.EnumerateMajorRecords<INavigationMeshInfoMapGetter>()); // existing FormID: NAVI stays vanilla

        var ours = r.Mod.EnumerateMajorRecords<INavigationMeshGetter>().Single().Data!;
        Assert.Equal(oldVertexCount + 4, ours.Vertices.Count);
        Assert.Equal(oldTriangleCount + 2, ours.Triangles.Count);
        for (int i = 0; i < oldVertexCount; i++) Assert.Equal(vanilla.Data.Vertices[i], ours.Vertices[i]);
        for (int i = 0; i < oldTriangleCount; i++) Assert.Equal(vanilla.Data.Triangles[i].Vertices, ours.Triangles[i].Vertices);

        // Exactly one old border EdgeLink changes: the seam. No old triangle is reordered or rewritten.
        int changedOldEdges = 0;
        for (int i = 0; i < oldTriangleCount; i++)
        {
            if (vanilla.Data.Triangles[i].EdgeLink_0_1 != ours.Triangles[i].EdgeLink_0_1) changedOldEdges++;
            if (vanilla.Data.Triangles[i].EdgeLink_1_2 != ours.Triangles[i].EdgeLink_1_2) changedOldEdges++;
            if (vanilla.Data.Triangles[i].EdgeLink_2_0 != ours.Triangles[i].EdgeLink_2_0) changedOldEdges++;
        }
        Assert.Equal(1, changedOldEdges);
        Assert.Equal(4 + ours.Triangles.Count * 2, ours.NavmeshGrid.Length);
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void BanneredMare_RejectedSeamEmitsNoNavmOrStrayCellOverride()
    {
        var s = new ModSpec { PluginName = "MFNavPatch.esp" };
        var far = Platform();
        foreach (var p in far) { p.X += 10000; p.Y += 10000; }
        s.NavPatches.Add(new NavPatchSpec
        {
            Cell = "Skyrim.esm:0x01605E", Navmesh = "Skyrim.esm:0x0C9064", Polygon = far, Epsilon = 0.01f,
        });

        var r = TestBuild.Raw(s);
        Assert.Equal(0, r.Stats.NavPatches);
        Assert.Contains(r.Warnings, w => w.Contains("no complete polygon edge"));
        Assert.Empty(r.Mod.EnumerateMajorRecords<INavigationMeshGetter>());
        Assert.Empty(r.Mod.EnumerateMajorRecords<ICellGetter>());
    }

    private static NavigationMesh VanillaBanneredMare()
    {
        string data = Environment.GetEnvironmentVariable("MODFORGE_SKYRIM_DATA")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share", "Steam", "steamapps", "common", "Skyrim Special Edition", "Data");
        using var esm = SkyrimMod.CreateFromBinaryOverlay(new ModPath(Path.Combine(data, "Skyrim.esm")), SkyrimRelease.SkyrimSE);
        return esm.EnumerateMajorRecords<INavigationMeshGetter>().Single(n => n.FormKey.ID == 0x0C9064).DeepCopy();
    }

    // Derive, never guess, a four-point platform from a real unlinked boundary edge. TryApply is the
    // oracle for duplicate/ambiguous seams; the first exact single-seam candidate becomes the fixture.
    private static List<Vec3> FindWorkingPlatform(NavigationMesh vanilla)
    {
        var d = vanilla.Data!;
        foreach (var t in d.Triangles)
        for (int edge = 0; edge < 3; edge++)
        {
            short link = edge == 0 ? t.EdgeLink_0_1 : edge == 1 ? t.EdgeLink_1_2 : t.EdgeLink_2_0;
            if (link != -1) continue;
            int ai = edge == 0 ? t.Vertices.X : edge == 1 ? t.Vertices.Y : t.Vertices.Z;
            int bi = edge == 0 ? t.Vertices.Y : edge == 1 ? t.Vertices.Z : t.Vertices.X;
            int ci = edge == 0 ? t.Vertices.Z : edge == 1 ? t.Vertices.X : t.Vertices.Y;
            var a = d.Vertices[ai]; var b = d.Vertices[bi]; var c = d.Vertices[ci];
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float length = MathF.Sqrt(dx * dx + dy * dy);
            if (length < 50f) continue;
            float side = MathF.Sign(dx * (c.Y - a.Y) - dy * (c.X - a.X));
            float nx = side * dy / length * 64f, ny = -side * dx / length * 64f; // away from triangle interior
            var p = new List<Vec3>
            {
                new() { X = a.X, Y = a.Y, Z = a.Z }, new() { X = b.X, Y = b.Y, Z = b.Z },
                new() { X = b.X + nx, Y = b.Y + ny, Z = b.Z }, new() { X = a.X + nx, Y = a.Y + ny, Z = a.Z },
            };
            if (NavmeshPatch.TryApply(vanilla.DeepCopy(), p, 0.01f, out _)) return p;
        }
        throw new InvalidOperationException("Bannered Mare has no unique usable boundary edge");
    }
}
