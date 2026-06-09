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
}
