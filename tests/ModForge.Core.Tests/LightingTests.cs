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
}
