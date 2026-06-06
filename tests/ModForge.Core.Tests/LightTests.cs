using System.Drawing;
using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

// Regression tests for the LIGHT (LIGT) build/validate. Pure object-in/object-out:
// build a spec in code and assert on the in-memory mod — no Skyrim install, no file
// I/O. Verifies colour/radius/fade land, a flag combination parses, and the validate
// guardrails fire (bad flag, duplicate editorId, non-positive radius).
public class LightTests
{
    private static readonly ModKey Out = ModKey.FromNameAndExtension("Test.esp");

    private static BuildResult Build(ModSpec spec) => Generator.Build(spec, Out);

    private static T Single<T>(BuildResult r) where T : class, IMajorRecordGetter =>
        r.Mod.EnumerateMajorRecords<T>().Single();

    // ---------------------------------------------------------------- build

    [Fact]
    public void Light_WritesColorRadiusFade()
    {
        var spec = new ModSpec
        {
            Lights =
            {
                new LightSpec
                {
                    EditorId = "MF_GreenLight",
                    Name = "Eerie Glow",
                    Color = new ColorSpec { R = 30, G = 200, B = 60 },
                    Radius = 384,
                    FadeValue = 1.5f,
                },
            },
        };

        var l = Single<ILightGetter>(Build(spec));
        Assert.Equal("MF_GreenLight", l.EditorID);
        Assert.Equal("Eerie Glow", l.Name?.String);
        Assert.Equal((uint)384, l.Radius);
        Assert.Equal(1.5f, l.FadeValue);
        Assert.Equal(30, l.Color.R);
        Assert.Equal(200, l.Color.G);
        Assert.Equal(60, l.Color.B);
    }

    [Fact]
    public void Light_DefaultsRadiusAndFade()
    {
        var spec = new ModSpec { Lights = { new LightSpec { EditorId = "MF_Plain" } } };
        var l = Single<ILightGetter>(Build(spec));
        Assert.Equal((uint)256, l.Radius);
        Assert.Equal(1.0f, l.FadeValue);
    }

    [Fact]
    public void Light_ParsesFlagCombination()
    {
        var spec = new ModSpec
        {
            Lights =
            {
                new LightSpec
                {
                    EditorId = "MF_Flicker",
                    Flags = { "Dynamic", "Flicker", "PortalStrict" },
                },
            },
        };

        var l = Single<ILightGetter>(Build(spec));
        Assert.True(l.Flags.HasFlag(Light.Flag.Dynamic));
        Assert.True(l.Flags.HasFlag(Light.Flag.Flicker));
        Assert.True(l.Flags.HasFlag(Light.Flag.PortalStrict));
    }

    [Fact]
    public void Light_OptionalFields_Land()
    {
        var spec = new ModSpec
        {
            Lights =
            {
                new LightSpec
                {
                    EditorId = "MF_Opt",
                    FalloffExponent = 2f,
                    Fov = 75f,
                    Value = 25,
                    Weight = 1.5f,
                },
            },
        };

        var l = Single<ILightGetter>(Build(spec));
        Assert.Equal(2f, l.FalloffExponent);
        Assert.Equal(75f, l.FOV);
        Assert.Equal((uint)25, l.Value);
        Assert.Equal(1.5f, l.Weight);
    }

    [Fact]
    public void Light_BadFlag_Warns()
    {
        var spec = new ModSpec
        {
            Lights = { new LightSpec { EditorId = "MF_BadFlag", Flags = { "Nonexistent" } } },
        };
        Assert.Contains(Build(spec).Warnings, w => w.Contains("Nonexistent"));
    }

    // ---------------------------------------------------------------- validate

    [Fact]
    public void Validate_Clean_NoProblems()
    {
        var spec = new ModSpec
        {
            Lights =
            {
                new LightSpec { EditorId = "MF_Ok", Flags = { "Dynamic", "Flicker" }, Radius = 256 },
            },
        };
        Assert.Empty(Generator.Validate(spec));
    }

    [Fact]
    public void Validate_BadFlag_Flagged()
    {
        var spec = new ModSpec { Lights = { new LightSpec { EditorId = "MF_L", Flags = { "Sunny" } } } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("invalid flag 'Sunny'"));
    }

    [Fact]
    public void Validate_ZeroRadius_Flagged()
    {
        var spec = new ModSpec { Lights = { new LightSpec { EditorId = "MF_L", Radius = 0 } } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("radius"));
    }

    [Fact]
    public void Validate_DuplicateEditorId_Flagged()
    {
        var spec = new ModSpec
        {
            Lights =
            {
                new LightSpec { EditorId = "MF_Dup" },
                new LightSpec { EditorId = "MF_Dup" },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("duplicate editorId 'MF_Dup'"));
    }
}
