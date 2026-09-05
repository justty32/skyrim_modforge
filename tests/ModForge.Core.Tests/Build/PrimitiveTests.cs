using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// placements[].primitive — XPRM trigger volumes (Spec.Primitives.cs / Generator.Build.Primitives.cs).
//
// All master-free: a primitive is inline data on the REFR, so nothing here needs Skyrim.esm. The
// vanilla shape these assert against was measured on Skyrim.esm (SSE 1.6.1170, 2026-09-05):
// 13,668 REFRs carry an XPRM, and every *TRIG family (defaultActivateSelfTRIG 0x048AC0 and friends)
// is a plain Box on an ACTIVATOR base with colour (204,76,51) and opacity 0.15 — NO collision layer.
public class PrimitiveTests
{
    private static ModSpec BaseSpec()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Statics.Add(new StaticSpec { EditorId = "Obj", Model = @"Clutter\Box.nif" });
        return spec;
    }

    private static IPlacedObjectGetter Object(BuildResult r, string ed) =>
        r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(o => o.EditorID == ed);

    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    // --- the record shape ---------------------------------------------------------------

    [Fact]
    public void Primitive_DefaultsToTheVanillaTriggerBoxRecipe()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room",
            Primitive = new PrimitiveSpec { Bounds = new Vec3 { X = 300f, Y = 200f, Z = 128f } },
        });

        var prim = Object(TestBuild.Ok(spec), "Trig").Primitive;
        Assert.NotNull(prim);
        Assert.Equal(PlacedPrimitive.TypeEnum.Box, prim!.Type);                     // no `type` = box
        Assert.Equal(new Noggog.P3Float(300f, 200f, 128f), prim.Bounds);            // FULL size, verbatim
        Assert.Equal(System.Drawing.Color.FromArgb(0, 204, 76, 51), prim.Color);    // the vanilla trigger red
        Assert.Equal(0.15f, prim.Unknown);                                          // the vanilla opacity
    }

    [Fact]
    public void Primitive_ColorAndOpacity_AreAuthorable()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room",
            Primitive = new PrimitiveSpec
            {
                Bounds = new Vec3 { X = 64f, Y = 64f, Z = 64f },
                Color = new ColorSpec { R = 0, G = 128, B = 255 },
                Opacity = 0.2f,
            },
        });

        var prim = Object(TestBuild.Ok(spec), "Trig").Primitive!;
        Assert.Equal(System.Drawing.Color.FromArgb(0, 0, 128, 255), prim.Color);
        Assert.Equal(0.2f, prim.Unknown);
    }

    [Theory]
    [InlineData("box", PlacedPrimitive.TypeEnum.Box)]
    [InlineData("SPHERE", PlacedPrimitive.TypeEnum.Sphere)]
    [InlineData("portalBox", PlacedPrimitive.TypeEnum.PortalBox)]
    [InlineData("none", PlacedPrimitive.TypeEnum.None)]
    public void Primitive_TypeName_IsCaseInsensitive(string name, PlacedPrimitive.TypeEnum expected)
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room",
            Primitive = new PrimitiveSpec { Type = name, Bounds = new Vec3 { X = 64f, Y = 64f, Z = 64f } },
        });
        Assert.Equal(expected, Object(TestBuild.Ok(spec), "Trig").Primitive!.Type);
    }

    // Skyrim.esm carries 122 refs whose XPRM type is 4, which Mutagen's enum has no name for.
    // A spec must be able to say what vanilla says.
    [Fact]
    public void Primitive_RawNumericType_IsAccepted()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room",
            Primitive = new PrimitiveSpec { Type = "4", Bounds = new Vec3 { X = 64f, Y = 16f, Z = 16f } },
        });
        Assert.Equal((PlacedPrimitive.TypeEnum)4, Object(TestBuild.Ok(spec), "Trig").Primitive!.Type);
    }

    // A sphere's three axes are one number (vanilla WordWallTrigger stores 4129.57 three times).
    [Fact]
    public void Primitive_Sphere_FillsAllThreeAxesFromX()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room",
            Primitive = new PrimitiveSpec { Type = "sphere", Bounds = new Vec3 { X = 512f } },
        });
        Assert.Equal(new Noggog.P3Float(512f, 512f, 512f), Object(TestBuild.Ok(spec), "Trig").Primitive!.Bounds);
    }

    [Fact]
    public void Primitive_Sphere_MismatchedAxes_WarnsAndUsesX()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room",
            Primitive = new PrimitiveSpec { Type = "sphere", Bounds = new Vec3 { X = 512f, Y = 100f, Z = 100f } },
        });
        var r = TestBuild.Raw(spec);
        Assert.Contains(r.Warnings, w => w.Contains("sphere has ONE size"));
        Assert.Equal(new Noggog.P3Float(512f, 512f, 512f), Object(r, "Trig").Primitive!.Bounds);
    }

    [Fact]
    public void Primitive_Omitted_WritesNoXprm()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "Plain", Base = "Obj", Cell = "Room" });
        Assert.Null(Object(TestBuild.Ok(spec), "Plain").Primitive);
    }

    // --- collisionLayer ------------------------------------------------------------------

    [Fact]
    public void CollisionLayer_IsWrittenAndOtherwiseAbsent()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "Cut", Base = "Obj", Cell = "Room", CollisionLayer = 49 });
        spec.Placements.Add(new PlacementSpec { EditorId = "Plain", Base = "Obj", Cell = "Room" });
        var r = TestBuild.Ok(spec);
        Assert.Equal(49u, Object(r, "Cut").CollisionLayer);
        Assert.Null(Object(r, "Plain").CollisionLayer);
    }

    // --- the REFR-only rule ---------------------------------------------------------------
    //
    // Silently dropping a primitive off an ACHR would leave a trigger that never fires and nothing
    // anywhere to explain it, so both validate AND build have to say it.

    [Fact]
    public void Primitive_OnAnActor_IsRejectedByValidate()
    {
        var spec = BaseSpec();
        spec.Npcs.Add(new NpcSpec { EditorId = "Guy", Name = "Guy" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "A", Base = "Guy", Cell = "Room", Kind = "npc",
            Primitive = new PrimitiveSpec { Bounds = new Vec3 { X = 64f, Y = 64f, Z = 64f } },
            CollisionLayer = 49,
        });

        var problems = Validate(spec);
        Assert.Contains(problems, p => p.Contains("`primitive` is REFR-only"));
        Assert.Contains(problems, p => p.Contains("`collisionLayer` is REFR-only"));
    }

    [Fact]
    public void Primitive_OnAnActor_WarnsAtBuildAndWritesNothing()
    {
        var spec = BaseSpec();
        spec.Npcs.Add(new NpcSpec { EditorId = "Guy", Name = "Guy" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "A", Base = "Guy", Cell = "Room", Kind = "npc",
            Primitive = new PrimitiveSpec { Bounds = new Vec3 { X = 64f, Y = 64f, Z = 64f } },
        });

        var r = TestBuild.Raw(spec);
        Assert.Contains(r.Warnings, w => w.Contains("`primitive` is REFR-only"));
        Assert.DoesNotContain(r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>(), o => o.Primitive is not null);
    }

    // --- validate: the spec mistakes that build clean and never fire -----------------------

    [Fact]
    public void Primitive_WithoutBounds_IsRejected()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room", Primitive = new PrimitiveSpec(),
        });
        Assert.Contains(Validate(spec), p => p.Contains("needs `bounds`"));
    }

    [Fact]
    public void Primitive_WithoutBounds_WarnsAtBuildAndWritesNoXprm()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room", Primitive = new PrimitiveSpec(),
        });
        var r = TestBuild.Raw(spec);
        Assert.Contains(r.Warnings, w => w.Contains("needs `bounds`"));
        Assert.Null(Object(r, "Trig").Primitive);
    }

    [Fact]
    public void Primitive_ZeroBounds_IsRejected()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room",
            Primitive = new PrimitiveSpec { Bounds = new Vec3 { X = 300f, Y = 0f, Z = 128f } },
        });
        Assert.Contains(Validate(spec), p => p.Contains("positive on all three axes"));
    }

    [Fact]
    public void Primitive_UnknownTypeName_IsRejectedAndFallsBackToBox()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room",
            Primitive = new PrimitiveSpec { Type = "cylinder", Bounds = new Vec3 { X = 64f, Y = 64f, Z = 64f } },
        });
        Assert.Contains(Validate(spec), p => p.Contains("unknown type 'cylinder'"));

        var r = TestBuild.Raw(spec);
        Assert.Contains(r.Warnings, w => w.Contains("unknown type 'cylinder'"));
        Assert.Equal(PlacedPrimitive.TypeEnum.Box, Object(r, "Trig").Primitive!.Type);
    }

    [Fact]
    public void Primitive_OpacityOutOfRange_IsRejected()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room",
            Primitive = new PrimitiveSpec { Bounds = new Vec3 { X = 64f, Y = 64f, Z = 64f }, Opacity = 4f },
        });
        Assert.Contains(Validate(spec), p => p.Contains("opacity must be within 0..1"));
    }

    [Fact]
    public void Primitive_CleanTriggerSpec_ValidatesClean()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trig", Base = "Obj", Cell = "Room",
            Primitive = new PrimitiveSpec { Bounds = new Vec3 { X = 300f, Y = 200f, Z = 128f } },
        });
        Assert.DoesNotContain(Validate(spec), p => p.Contains("primitive"));
    }

    // --- the shared-builder guarantee ------------------------------------------------------
    //
    // navCuts[] now goes through the same MakePrimitive as placements[].primitive. That refactor is
    // only safe if the navcut record is bit-for-bit what it always was, so pin it here too (the full
    // navcut contract stays in NavCutTests).
    [Fact]
    public void NavCut_StillEmitsTheCollisionMarkerRecipe_AfterSharingTheBuilder()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.NavCuts.Add(new NavCutSpec
        {
            EditorId = "Cut", Cell = "Room",
            Position = new Vec3 { X = 0f, Y = 0f, Z = 0f },
            Size = new Vec3 { X = 100f, Y = 200f, Z = 300f },
            Padding = 0f,
        });

        var prim = Object(TestBuild.Ok(spec), "Cut").Primitive!;
        Assert.Equal(PlacedPrimitive.TypeEnum.Box, prim.Type);
        Assert.Equal(new Noggog.P3Float(100f, 200f, 300f), prim.Bounds);
        Assert.Equal(System.Drawing.Color.FromArgb(0, 255, 255, 0), prim.Color);   // yellow, NOT trigger red
        Assert.Equal(0.15f, prim.Unknown);
    }
}
