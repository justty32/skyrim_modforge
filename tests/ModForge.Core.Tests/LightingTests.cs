using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

// Regression tests for the lighting pipeline (LGTM / IMGS / CELL XCLL). Pure object-in/
// object-out: build a spec in code, assert on the in-memory mod. LGTM/IMGS template-copy
// tests use a vanilla ref, so they need Skyrim.esm — gated like WordWallTests; the no-template
// tests run everywhere.
public class LightingTests
{
    private static readonly ModKey Out = ModKey.FromNameAndExtension("Test.esp");
    private static BuildResult Build(ModSpec spec) => Generator.Build(spec, Out);
    private static T Single<T>(BuildResult r) where T : class, IMajorRecordGetter =>
        r.Mod.EnumerateMajorRecords<T>().Single();

    [Fact]
    public void Lgtm_NoTemplate_WritesAuthoredFieldsAndDalc()
    {
        var spec = new ModSpec
        {
            LightingTemplates =
            {
                new LightingTemplateSpec
                {
                    EditorId = "MF_BrightCaveLGTM",
                    AmbientColor = new ColorSpec { R = 180, G = 185, B = 200 },
                    DirectionalColor = new ColorSpec { R = 220, G = 220, B = 210 },
                    FogNear = 0f, FogFar = 8192f,
                    DirectionalAmbient = new AmbientColorsSpec
                    {
                        Scale = 1.0f,
                        ZPlus = new ColorSpec { R = 200, G = 205, B = 215 },
                    },
                },
            },
        };

        var lt = Single<ILightingTemplateGetter>(Build(spec));
        Assert.Equal("MF_BrightCaveLGTM", lt.EditorID);
        Assert.Equal(180, lt.AmbientColor.R);
        Assert.Equal(210, lt.DirectionalColor.B);
        Assert.Equal(8192f, lt.FogFar);
        Assert.Equal(1.0f, lt.DirectionalAmbientColors!.Scale);
        Assert.Equal(200, lt.DirectionalAmbientColors!.DirectionalZPlus.R);
    }

    [Fact]
    public void Imgs_NoTemplate_WritesHdrCinematicTint()
    {
        var spec = new ModSpec
        {
            ImageSpaces =
            {
                new ImageSpaceSpec
                {
                    EditorId = "MF_BrightIMGS",
                    Brightness = 1.4f, Saturation = 1.2f, Contrast = 1.0f,
                    BloomScale = 0.8f, SunlightScale = 1.3f,
                    TintAmount = 0.1f, TintColor = new ColorSpec { R = 255, G = 240, B = 210 },
                },
            },
        };

        var img = Single<IImageSpaceGetter>(Build(spec));
        Assert.Equal("MF_BrightIMGS", img.EditorID);
        Assert.Equal(1.4f, img.Cinematic!.Brightness);
        Assert.Equal(1.2f, img.Cinematic!.Saturation);
        Assert.Equal(0.8f, img.Hdr!.BloomScale);
        Assert.Equal(1.3f, img.Hdr!.SunlightScale);
        Assert.Equal(0.1f, img.Tint!.Amount);
        Assert.Equal(255, img.Tint!.Color.R);
    }

    // CELL points at an in-spec custom LGTM with no inline lighting → auto Inherits=ALL flags.
    [Fact]
    public void Cell_WithCustomLgtm_InheritsAll()
    {
        var spec = new ModSpec
        {
            LightingTemplates = { new LightingTemplateSpec { EditorId = "MF_BrightLGTM" } },
            Cells = { new CellSpec { EditorId = "MF_BrightRoom", LightingTemplate = "MF_BrightLGTM" } },
        };

        var r = Build(spec);
        var lgtm = r.Mod.EnumerateMajorRecords<ILightingTemplateGetter>().Single();
        var cell = r.Mod.EnumerateMajorRecords<ICellGetter>().Single(c => c.EditorID == "MF_BrightRoom");
        Assert.Equal(lgtm.FormKey, cell.LightingTemplate.FormKey);
        Assert.NotNull(cell.Lighting);
        // every inherit flag set → fully driven by the template
        foreach (CellLighting.Inherit f in Enum.GetValues<CellLighting.Inherit>())
            Assert.True(cell.Lighting!.Inherits.HasFlag(f), $"missing inherit flag {f}");
    }

    // Inline lighting: fields set inline are used; flags listed in `inherit` come from the template.
    [Fact]
    public void Cell_InlineLighting_SetsFieldsAndInheritSubset()
    {
        var spec = new ModSpec
        {
            LightingTemplates = { new LightingTemplateSpec { EditorId = "MF_BaseLGTM" } },
            Cells =
            {
                new CellSpec
                {
                    EditorId = "MF_TunedRoom",
                    LightingTemplate = "MF_BaseLGTM",
                    Lighting = new CellLightingSpec
                    {
                        AmbientColor = new ColorSpec { R = 160, G = 165, B = 175 },
                        FogFar = 6000f,
                        DirectionalAmbient = new AmbientColorsSpec { Scale = 1.0f },
                        Inherit = { "FogColor", "DirectionalColor" },
                    },
                },
            },
        };

        var cell = Build(spec).Mod.EnumerateMajorRecords<ICellGetter>().Single(c => c.EditorID == "MF_TunedRoom");
        Assert.Equal(160, cell.Lighting!.AmbientColor.R);
        Assert.Equal(6000f, cell.Lighting!.FogFar);
        Assert.Equal(1.0f, cell.Lighting!.AmbientColors!.Scale);
        Assert.True(cell.Lighting!.Inherits.HasFlag(CellLighting.Inherit.FogColor));
        Assert.True(cell.Lighting!.Inherits.HasFlag(CellLighting.Inherit.DirectionalColor));
        Assert.False(cell.Lighting!.Inherits.HasFlag(CellLighting.Inherit.AmbientColor));
    }

    [Fact]
    public void Validate_FlagsBadColorDuplicateRefCrossTypeAndInherit()
    {
        var spec = new ModSpec
        {
            LightingTemplates =
            {
                new LightingTemplateSpec { EditorId = "MF_DupLGTM", AmbientColor = new ColorSpec { R = 300, G = 0, B = 0 } },
                new LightingTemplateSpec { EditorId = "MF_DupLGTM" },   // duplicate editorId
            },
            ImageSpaces = { new ImageSpaceSpec { EditorId = "MF_SomeIMGS" } },
            Cells =
            {
                new CellSpec
                {
                    EditorId = "MF_BadCell",
                    LightingTemplate = "MF_DoesNotExist",   // unresolved ref
                    ImageSpace = "MF_DupLGTM",              // cross-type: an LGTM id used in the imageSpace slot
                    Lighting = new CellLightingSpec { Inherit = { "NotARealFlag" } },
                },
            },
        };

        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_DupLGTM") && p.Contains("duplicate"));
        Assert.Contains(problems, p => p.Contains("MF_DupLGTM") && p.Contains("ambientColor"));
        Assert.Contains(problems, p => p.Contains("MF_BadCell") && p.Contains("lightingTemplate"));
        Assert.Contains(problems, p => p.Contains("MF_BadCell") && p.Contains("imageSpace"));
        Assert.Contains(problems, p => p.Contains("MF_BadCell") && p.Contains("NotARealFlag"));
    }
}
