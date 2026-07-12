using System.Linq;
using System.Text.Json;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// navCuts[] — L_NAVCUT collision volumes (navmesh plan T2.0 / Spec.NavCuts.cs).
//
// The RECORD half is offline (a navcut is just a PlacedObject on an external base, so no link cache
// is needed to author one). The AUTO half needs the base's OBND from the master, so it is
// RequiresSkyrim — and its absence is exactly what makes an offline build byte-identical to before.
public class NavCutTests
{
    private const string WhiterunWorld = "Skyrim.esm:0x01A26F";
    private const string CollisionMarker = "000021:Skyrim.esm";
    private static readonly ModKey Key = ModKey.FromNameAndExtension("MFNav.esp");

    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static IPlacedObjectGetter TheNavCut(ISkyrimMod mod) =>
        mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(p => p.CollisionLayer is not null);

    // A box in an in-spec interior cell: no master anywhere in the picture.
    private static ModSpec BoxSpec()
    {
        var s = new ModSpec { PluginName = "MFNav.esp" };
        s.Cells.Add(new CellSpec { EditorId = "MFNavRoom", Name = "Nav Room" });
        s.NavCuts.Add(new NavCutSpec
        {
            EditorId = "MFNavBox", Cell = "MFNavRoom",
            Position = new Vec3 { X = 100f, Y = 200f, Z = 50f },
            Size = new Vec3 { X = 300f, Y = 200f, Z = 400f },
            RotationZ = 90f,
            Padding = 0f,
        });
        return s;
    }

    // --- the record shape: this is the whole mechanism, and it is byte-verified against vanilla ----

    [Fact]
    public void ExplicitBox_AuthorsTheVanillaCollisionMarkerRecipe()
    {
        var cut = TheNavCut(TestBuild.Ok(BoxSpec()).Mod);

        // base = the engine's hardcoded CollisionMarker (HearthFires' 1003 navcuts + Skyrim's 441 all use it)
        Assert.Equal(FormKey.Factory(CollisionMarker), cut.Base.FormKey);
        // 🔴 the half of the two-stage gate that actually matters: L_NAVCUT is one of only six vanilla
        // COLL layers carrying NavmeshObstacle. Without this the record is inert.
        Assert.Equal(49u, cut.CollisionLayer);

        var prim = Assert.IsAssignableFrom<IPlacedPrimitiveGetter>(cut.Primitive);
        Assert.Equal(PlacedPrimitive.TypeEnum.Box, prim.Type);
        Assert.Equal(0.15f, prim.Unknown);                                    // the constant on every vanilla navcut
        Assert.Equal(System.Drawing.Color.FromArgb(0, 255, 255, 0), prim.Color);
    }

    [Fact]
    public void Size_IsTheFullBoxSize_NotHalfExtents()
    {
        // Verified against vanilla: HearthFires 00410D's XPRM Bounds is 116 x 52.8 x 46.9 around a chest
        // whose OBND is 96 x 49 x 48 — a ~1.0-1.2x hug, so Bounds is the FULL size. `size` maps 1:1.
        var prim = TheNavCut(TestBuild.Ok(BoxSpec()).Mod).Primitive!;
        Assert.Equal(300f, prim.Bounds.X, 3);
        Assert.Equal(200f, prim.Bounds.Y, 3);
        Assert.Equal(400f, prim.Bounds.Z, 3);
    }

    [Fact]
    public void Padding_InflatesTheBoxOutwardOnEveryAxis()
    {
        // The engine tests an actor as a ZERO-VOLUME POINT, so an un-padded box leaks NPCs through the
        // seam. Padding grows it by `padding` on each side (hence 2x per axis).
        var s = BoxSpec();
        s.NavCuts[0].Padding = 32f;
        var prim = TheNavCut(TestBuild.Ok(s).Mod).Primitive!;
        Assert.Equal(364f, prim.Bounds.X, 3);   // 300 + 2*32
        Assert.Equal(264f, prim.Bounds.Y, 3);   // 200 + 2*32
        Assert.Equal(464f, prim.Bounds.Z, 3);   // 400 + 2*32 — Z too, so the box straddles the navmesh plane
    }

    [Fact]
    public void Box_IsCentredOnPosition_AndYawsAroundZ()
    {
        var cut = TheNavCut(TestBuild.Ok(BoxSpec()).Mod);
        Assert.Equal(new Noggog.P3Float(100f, 200f, 50f), cut.Placement!.Position);
        Assert.Equal(System.MathF.PI / 2f, cut.Placement.Rotation.Z, 0.001f);   // rotationZ is DEGREES in the spec
    }

    [Fact]
    public void NavCut_IsTemporary_LikeSkyrimsOwnStaticNavCuts()
    {
        // Skyrim.esm's 441 plain navcuts carry no 0x400. (HearthFires' are persistent only because its
        // house-building script enable-parents them.) Staying temporary also keeps an exterior navcut
        // out of the worldspace persistent TopCell — the map-render landmine.
        var cut = TheNavCut(TestBuild.Ok(BoxSpec()).Mod);
        Assert.True((cut.MajorRecordFlagsRaw & 0x400) == 0, "a plain navcut should not be persistent");
    }

    [Fact]
    public void NoNavCuts_EmitsNothing_BehaviourUnchanged()
    {
        var s = new ModSpec { PluginName = "MFNav.esp" };
        s.Cells.Add(new CellSpec { EditorId = "MFNavRoom", Name = "Nav Room" });
        var r = TestBuild.Ok(s);
        Assert.Empty(r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>());
        Assert.Equal(0, r.Stats.NavCuts);
    }

    // --- the `navCut` field on a placement: bool OR object (the user's "both" ruling) ---------------

    [Fact]
    public void PlacementNavCut_ParsesFromBareTrueAndFalse()
    {
        var no = JsonSerializer.Deserialize<PlacementSpec>("""{"base":"X","navCut":false}""",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.False(no.NavCut!.Enabled);

        var yes = JsonSerializer.Deserialize<PlacementSpec>("""{"base":"X","navCut":true}""",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.True(yes.NavCut!.Enabled);
    }

    [Fact]
    public void PlacementNavCut_ParsesFromAnObject()
    {
        var tuned = JsonSerializer.Deserialize<PlacementSpec>(
            """{"base":"X","navCut":{"size":{"x":10,"y":20,"z":30},"padding":48}}""",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.True(tuned.NavCut!.Enabled);               // an object without `enabled` means "cut it"
        Assert.Equal(20f, tuned.NavCut.Size!.Y);
        Assert.Equal(48f, tuned.NavCut.Padding);
    }

    [Fact]
    public void PlacementNavCut_Omitted_MeansNoField()
    {
        var plain = JsonSerializer.Deserialize<PlacementSpec>("""{"base":"X"}""",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Null(plain.NavCut);
    }

    // --- validate ---------------------------------------------------------------------------------

    [Fact]
    public void Validate_BoxNeedsPositionSizeAndSomewhereToLive()
    {
        var s = new ModSpec { PluginName = "MFNav.esp" };
        s.NavCuts.Add(new NavCutSpec { EditorId = "Bad" });
        var p = Validate(s);
        Assert.Contains(p, x => x.Contains("needs a `position`"));
        Assert.Contains(p, x => x.Contains("needs a `size`"));
        Assert.Contains(p, x => x.Contains("needs a `cell` or `worldspace`"));
    }

    [Fact]
    public void Validate_RejectsNonPositiveSizeAndUnknownPlacement()
    {
        var s = new ModSpec { PluginName = "MFNav.esp" };
        s.NavCuts.Add(new NavCutSpec
        {
            EditorId = "Bad", Worldspace = WhiterunWorld,
            Position = new Vec3(), Size = new Vec3 { X = 10f, Y = 0f, Z = 10f },
        });
        s.NavCuts.Add(new NavCutSpec { EditorId = "Bad2", Placement = "NoSuchPlacement" });
        var p = Validate(s);
        Assert.Contains(p, x => x.Contains("size must be positive"));
        Assert.Contains(p, x => x.Contains("'NoSuchPlacement' is not a placements[] editorId"));
    }

    [Fact]
    public void Validate_RejectsBothCellAndWorldspace_AndANavCutOnAnActor()
    {
        var s = new ModSpec { PluginName = "MFNav.esp" };
        s.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        s.NavCuts.Add(new NavCutSpec
        {
            EditorId = "Bad", Cell = "Room", Worldspace = WhiterunWorld,
            Position = new Vec3(), Size = new Vec3 { X = 1f, Y = 1f, Z = 1f },
        });
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Guy", Base = "Skyrim.esm:0x013B99", Kind = "npc", Cell = "Room",
            NavCut = new PlacementNavCutSpec { Enabled = true },
        });
        var p = Validate(s);
        Assert.Contains(p, x => x.Contains("BOTH cell and worldspace"));
        Assert.Contains(p, x => x.Contains("is an NPC (ACHR)"));
    }

    [Fact]
    public void Validate_DisabledNavCutCarryingABoxIsContradictory()
    {
        var s = new ModSpec { PluginName = "MFNav.esp" };
        s.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Wall", Base = "Skyrim.esm:0x0DCD68", Cell = "Room",
            NavCut = new PlacementNavCutSpec { Enabled = false, Size = new Vec3 { X = 1f, Y = 1f, Z = 1f } },
        });
        Assert.Contains(Validate(s), x => x.Contains("disabled but still carries size/offset/padding"));
    }

    // --- offline degradation: the auto path is a no-op without the master link cache ---------------

    [Fact]
    public void WithoutTheMasterCache_AutoNavCutsEmitNothing()
    {
        // The auto box is sized from the base's OBND, which lives in Skyrim.esm. No master ⇒ no size ⇒
        // no record. This is what keeps an offline build byte-identical (CLAUDE.md rule ①).
        var s = new ModSpec { PluginName = "MFNav.esp" };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Wall", Base = "Skyrim.esm:0x0DCD68",       // WRWallStr01Stockades02, 520x51x350
            Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7625f, Z = -3570f },
        });
        var r = Generator.Build(s, Key, new BuildOptions { SkyrimDataPath = "/nonexistent" });
        Assert.Equal(0, r.Stats.NavCuts);
    }

    // --- auto: needs the real OBND ----------------------------------------------------------------

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Auto_CutsABlockingPlacementSittingOnVanillaNavmesh()
    {
        // A 520 x 51 x 350 Whiterun stockade wall dropped across the Whiterun main street: footprint
        // 26520 units² (> minFootprint 10000) and 350 tall (> minHeight 100), and it sits on live
        // vanilla navmesh. That is the whole "NPCs walk through my new house" case.
        var s = new ModSpec { PluginName = "MFNav.esp" };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Wall", Base = "Skyrim.esm:0x0DCD68", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7625f, Z = -3570f },
        });
        var r = Generator.Build(s, Key);
        Assert.Equal(1, r.Stats.NavCuts);

        var cut = TheNavCut(r.Mod);
        Assert.Equal(49u, cut.CollisionLayer);
        // OBND 520 x 51 x 350, padded by the default 32 per side.
        Assert.Equal(584f, cut.Primitive!.Bounds.X, 1);
        Assert.Equal(115f, cut.Primitive.Bounds.Y, 1);
        Assert.Equal(414f, cut.Primitive.Bounds.Z, 1);
        // no "NPCs will walk into it" warning — the case is handled
        Assert.DoesNotContain(r.Warnings, w => w.Contains("NPCs will walk into it"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Auto_SkipsClutter_AndRespectsAnExplicitFalse()
    {
        var s = new ModSpec { PluginName = "MFNav.esp" };
        // a sign post: 18 x 18 x 192 → footprint 324 units², nowhere near blocking
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Post", Base = "Skyrim.esm:0x09625E", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7625f, Z = -3570f },
        });
        // the same big wall, explicitly opted out (a fake wall NPCs are MEANT to walk through)
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "FakeWall", Base = "Skyrim.esm:0x0DCD68", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 22550f, Y = -7625f, Z = -3516f },
            NavCut = new PlacementNavCutSpec { Enabled = false },
        });
        var r = Generator.Build(s, Key);
        Assert.Equal(0, r.Stats.NavCuts);
        // and the opt-out is called out, not silently obeyed
        Assert.Contains(r.Warnings, w => w.Contains("FakeWall") && w.Contains("navCut: false"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void AutoNavCutsOff_LeavesTheObstacleUncut_AndWarns()
    {
        var s = new ModSpec { PluginName = "MFNav.esp", Navmesh = new NavmeshSpec { AutoNavCuts = false } };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Wall", Base = "Skyrim.esm:0x0DCD68", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7625f, Z = -3570f },
        });
        var r = Generator.Build(s, Key);
        Assert.Equal(0, r.Stats.NavCuts);
        Assert.Contains(r.Warnings, w => w.Contains("Wall") && w.Contains("NPCs will walk into it"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void NavCutFromPlacement_WrapsThatPlacementsOwnFootprint()
    {
        var s = new ModSpec { PluginName = "MFNav.esp", Navmesh = new NavmeshSpec { AutoNavCuts = false } };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Wall", Base = "Skyrim.esm:0x0DCD68", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7625f, Z = -3570f },
        });
        s.NavCuts.Add(new NavCutSpec { EditorId = "Cut", Placement = "Wall", Padding = 0f });
        var r = Generator.Build(s, Key);

        var cut = TheNavCut(r.Mod);
        Assert.Equal(520f, cut.Primitive!.Bounds.X, 1);   // the wall's raw OBND, unpadded
        Assert.Equal(51f, cut.Primitive.Bounds.Y, 1);
        Assert.Equal(350f, cut.Primitive.Bounds.Z, 1);
    }
}
