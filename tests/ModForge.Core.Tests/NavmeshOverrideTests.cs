using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// navmeshOverrides[] — re-emit a VANILLA navmesh from our plugin, unchanged (navmesh plan P0 /
// Spec.NavmeshOverrides.cs).
//
// Everything real about this primitive needs the master (the mesh being copied lives in Skyrim.esm),
// so the geometry tests are RequiresSkyrim. What CAN be asserted offline is the thing that matters
// most offline: with no master, NOTHING is emitted and NOTHING is warned — the offline machine's
// build stays byte-identical to a build without this section at all.
public class NavmeshOverrideTests
{
    private const string WhiterunWorld = "Skyrim.esm:0x01A26F";
    private const string BanneredMare = "Skyrim.esm:0x01605E";   // interior; holds NAVM 0x0C9064
    private const uint BanneredMareNavm = 0x0C9064;
    private static readonly ModKey Key = ModKey.FromNameAndExtension("MFNavOv.esp");

    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static ModSpec Interior()
    {
        var s = new ModSpec { PluginName = "MFNavOv.esp" };
        s.NavmeshOverrides.Add(new NavmeshOverrideSpec { Cell = BanneredMare });
        return s;
    }

    // --- offline: unknown is never reported as broken ---------------------------------------------

    [Fact]
    public void NoMaster_EmitsNothingAndSaysNothing()
    {
        var s = Interior();
        s.NavmeshOverrides.Add(new NavmeshOverrideSpec { Worldspace = WhiterunWorld, X = 5, Y = -2 });

        var r = Generator.Build(s, Key, new BuildOptions { SkyrimDataPath = "/nonexistent" });

        Assert.Equal(0, r.Stats.NavmeshOverrides);
        Assert.Empty(r.Mod.EnumerateMajorRecords<INavigationMeshGetter>());
        // "I cannot read Skyrim.esm" is a fact about the machine, not a fault in the spec: the only
        // warning allowed is the generic master-not-found one the cell resolution already emits.
        Assert.DoesNotContain(r.Warnings, w => w.Contains("navmeshOverride"));
    }

    // --- validate ---------------------------------------------------------------------------------

    [Fact]
    public void Validate_RejectsAnInSpecTarget()
    {
        // A cell WE authored has no vanilla navmesh in it — there is nothing to override. (Authoring
        // navmesh for our own interiors is P3 of the plan, and it is a different primitive.)
        var s = new ModSpec { PluginName = "MFNavOv.esp" };
        s.Cells.Add(new CellSpec { EditorId = "MFRoom", Name = "Room" });
        s.NavmeshOverrides.Add(new NavmeshOverrideSpec { Cell = "MFRoom" });

        Assert.Contains(Validate(s), p => p.Contains("no vanilla navmesh to override"));
    }

    [Fact]
    public void Validate_RequiresATarget_AndOnlyOne()
    {
        var empty = new ModSpec { PluginName = "MFNavOv.esp" };
        empty.NavmeshOverrides.Add(new NavmeshOverrideSpec());
        Assert.Contains(Validate(empty), p => p.Contains("needs a vanilla `cell`"));

        var both = new ModSpec { PluginName = "MFNavOv.esp" };
        both.NavmeshOverrides.Add(new NavmeshOverrideSpec { Cell = BanneredMare, Worldspace = WhiterunWorld, X = 5, Y = -2 });
        Assert.Contains(Validate(both), p => p.Contains("BOTH cell and worldspace"));
    }

    [Fact]
    public void Validate_AnExteriorTargetMustNameItsGridCell()
    {
        var s = new ModSpec { PluginName = "MFNavOv.esp" };
        s.NavmeshOverrides.Add(new NavmeshOverrideSpec { Worldspace = WhiterunWorld });
        Assert.Contains(Validate(s), p => p.Contains("needs `x`+`y`"));

        var half = new ModSpec { PluginName = "MFNavOv.esp" };
        half.NavmeshOverrides.Add(new NavmeshOverrideSpec { Worldspace = WhiterunWorld, X = 5 });
        Assert.Contains(Validate(half), p => p.Contains("`x` and `y` are a pair"));
    }

    // --- the copy itself --------------------------------------------------------------------------

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Interior_CopiesTheCellsNavmeshUnderTheSameFormKey()
    {
        var r = TestBuild.Raw(Interior());
        Assert.Equal(1, r.Stats.NavmeshOverrides);

        var navm = r.Mod.EnumerateMajorRecords<INavigationMeshGetter>().Single();
        // SAME FormKey = an OVERRIDE. That is the whole point: the NAVI info-map entry in Skyrim.esm,
        // every neighbouring mesh's EdgeLink and every door portal keep pointing at this mesh.
        Assert.Equal(FormKey.Factory($"{BanneredMareNavm:X6}:Skyrim.esm"), navm.FormKey);
        Assert.Equal(0x40000, navm.MajorRecordFlagsRaw);          // the Compressed flag comes across too

        // and it lives under a CELL override of the vanilla cell (CELL -> GRUP6 -> GRUP9 -> NAVM)
        var cell = r.Mod.EnumerateMajorRecords<ICellGetter>().Single(c => c.NavigationMeshes.Count > 0);
        Assert.Equal(FormKey.Factory("01605E:Skyrim.esm"), cell.FormKey);
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void TheCopyIsANoOp_NotOneTriangleMovesOrIsRenumbered()
    {
        // 🔴 THE IRON RULE (plan §2): triangle indices are POSITIONAL and neighbouring meshes hold
        // them. A no-op override must reproduce the array element-for-element, in order.
        var r = TestBuild.Raw(Interior());
        var ours = r.Mod.EnumerateMajorRecords<INavigationMeshGetter>().Single().Data!;

        var vanilla = VanillaBanneredMareNavmesh();
        Assert.Equal(vanilla.Vertices.Count, ours.Vertices.Count);
        Assert.Equal(vanilla.Triangles.Count, ours.Triangles.Count);

        for (int i = 0; i < vanilla.Triangles.Count; i++)
        {
            Assert.Equal(vanilla.Triangles[i].Vertices, ours.Triangles[i].Vertices);   // same index, same triangle
            Assert.Equal(vanilla.Triangles[i].Flags, ours.Triangles[i].Flags);
            Assert.Equal(vanilla.Triangles[i].EdgeLink_0_1, ours.Triangles[i].EdgeLink_0_1);
            Assert.Equal(vanilla.Triangles[i].EdgeLink_1_2, ours.Triangles[i].EdgeLink_1_2);
            Assert.Equal(vanilla.Triangles[i].EdgeLink_2_0, ours.Triangles[i].EdgeLink_2_0);
        }
        for (int i = 0; i < vanilla.Vertices.Count; i++)
            Assert.Equal(vanilla.Vertices[i], ours.Vertices[i]);

        // the parts nobody can regenerate: the opaque spatial grid, the door portals, the version
        Assert.Equal(vanilla.NavmeshGrid, ours.NavmeshGrid);
        Assert.Equal(vanilla.NavmeshGridDivisor, ours.NavmeshGridDivisor);
        Assert.Equal(vanilla.DoorTriangles.Count, ours.DoorTriangles.Count);
        Assert.Equal(vanilla.NavmeshVersion, ours.NavmeshVersion);
        Assert.Equal(vanilla.Min, ours.Min);
        Assert.Equal(vanilla.Max, ours.Max);
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void NaviIsNotTouched()
    {
        // U4: the mesh keeps its FormID, so vanilla's own NVMI entry still describes it. A NEW
        // NavigationMeshInfoMap record is the one thing that reliably CTDs, so assert we make none.
        var r = TestBuild.Raw(Interior());
        Assert.Empty(r.Mod.EnumerateMajorRecords<INavigationMeshInfoMapGetter>());
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Exterior_CopiesEveryNavmeshOfTheGridCell_AndKeepsTheWorldspaceSafe()
    {
        var s = new ModSpec { PluginName = "MFNavOv.esp" };
        s.NavmeshOverrides.Add(new NavmeshOverrideSpec { Worldspace = WhiterunWorld, X = 5, Y = -2 });
        var r = TestBuild.Raw(s);

        // WhiterunPLainsDistrict03 holds six meshes, incl. 0x105319 (1106 triangles, 41 cross-mesh
        // edge links, 10 door triangles) — the hard case, on purpose.
        Assert.Equal(6, r.Stats.NavmeshOverrides);
        var street = r.Mod.EnumerateMajorRecords<INavigationMeshGetter>()
            .Single(n => n.FormKey.ID == 0x105319);
        Assert.Equal(41, street.Data!.EdgeLinks.Count);     // the cross-mesh links survive the copy
        Assert.Equal(10, street.Data.DoorTriangles.Count);  // …and so do the door portals

        // The WRLD override this drags in is the one with scar tissue (memory: worldspace-override-*).
        var ws = r.Mod.Worldspaces.Single();
        Assert.Equal("WhiterunWorld", ws.EditorID);        // else the terrain-LOD texture atlas breaks
        Assert.NotEmpty(ws.LargeReferences);               // RNAM: drop it and the world map goes corrupt
        Assert.NotNull(ws.Name);                           // FULL: an override that omits it BLANKS the name
        Assert.Null(ws.OffsetData);                        // OFST: absolute offsets into Skyrim.esm — never carry
        Assert.NotNull(ws.TopCell);                        // the persistent cell, WITH its record flags…
        Assert.Equal(0x00040400, ws.TopCell!.MajorRecordFlagsRaw);   // …0x400 Persistent — twice a CTD without it
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Navmesh_NarrowsToOneMesh_AndListingACellTwiceCopiesItOnce()
    {
        var s = new ModSpec { PluginName = "MFNavOv.esp" };
        s.NavmeshOverrides.Add(new NavmeshOverrideSpec
        { Worldspace = WhiterunWorld, X = 5, Y = -2, Navmesh = "Skyrim.esm:0x105319" });
        s.NavmeshOverrides.Add(new NavmeshOverrideSpec { Worldspace = WhiterunWorld, Position = new Vec3 { X = 21750f, Y = -7625f } });

        var r = TestBuild.Raw(s);
        // entry 1 copies one mesh; entry 2 names the SAME grid cell via a point inside it and copies
        // the other five — the shared mesh is not emitted twice.
        Assert.Equal(6, r.Stats.NavmeshOverrides);
        Assert.Equal(6, r.Mod.EnumerateMajorRecords<INavigationMeshGetter>().Count());
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void AnEmptyCell_Warns()
    {
        // WhiterunWorld (5,0) exists but carries no navmesh at all. Asking to override it is a
        // mistake worth a word — silence would look like success.
        var s = new ModSpec { PluginName = "MFNavOv.esp" };
        s.NavmeshOverrides.Add(new NavmeshOverrideSpec { Worldspace = WhiterunWorld, X = 5, Y = 0 });
        var r = Generator.Build(s, Key);
        Assert.Equal(0, r.Stats.NavmeshOverrides);
        Assert.Contains(r.Warnings, w => w.Contains("no navmesh at all"));
    }

    // The mesh as Skyrim.esm itself holds it (read through the same link cache the generator uses).
    private static INavigationMeshDataGetter VanillaBanneredMareNavmesh()
    {
        var data = System.Environment.GetEnvironmentVariable("MODFORGE_SKYRIM_DATA")
            ?? System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                ".local", "share", "Steam", "steamapps", "common", "Skyrim Special Edition", "Data");
        using var esm = SkyrimMod.CreateFromBinaryOverlay(
            new ModPath(System.IO.Path.Combine(data, "Skyrim.esm")), SkyrimRelease.SkyrimSE);
        return esm.EnumerateMajorRecords<INavigationMeshGetter>()
                  .Single(n => n.FormKey.ID == BanneredMareNavm).Data!;
    }
}
