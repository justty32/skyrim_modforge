using System.Drawing;
using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

// Regression tests for the WEATHER (WTHR) + CLIMATE (CLMT) build/validate (It.W).
// Pure object-in/object-out: build a spec in code and assert on the in-memory mod —
// no Skyrim install, no file I/O. Verifies colours/flags/clouds are written, the
// climate→weather FormLinks resolve, sun times parse, and the validate guardrails fire.
public class WeatherClimateTests
{
    private static readonly ModKey Out = ModKey.FromNameAndExtension("Test.esp");

    private static BuildResult Build(ModSpec spec) => Generator.Build(spec, Out);

    private static T Single<T>(BuildResult r) where T : class, IMajorRecordGetter =>
        r.Mod.EnumerateMajorRecords<T>().Single();

    // ---------------------------------------------------------------- weather build

    [Fact]
    public void Weather_WritesFlagsAndColors()
    {
        var spec = new ModSpec
        {
            Weathers =
            {
                new WeatherSpec
                {
                    EditorId = "MF_Eerie",
                    Flags = { "Cloudy", "Rainy" },
                    SkyUpperColor = new WeatherColorSpec
                    {
                        Day = new ColorSpec { R = 46, G = 92, B = 58 },
                        Night = new ColorSpec { R = 8, G = 20, B = 14 },
                    },
                },
            },
        };

        var w = Single<IWeatherGetter>(Build(spec));
        Assert.Equal("MF_Eerie", w.EditorID);
        Assert.True(w.Flags.HasFlag(Weather.Flag.Cloudy));
        Assert.True(w.Flags.HasFlag(Weather.Flag.Rainy));
        Assert.Equal(Color.FromArgb(0, 46, 92, 58), w.SkyUpperColor!.Day);
        Assert.Equal(Color.FromArgb(0, 8, 20, 14), w.SkyUpperColor!.Night);
    }

    [Fact]
    public void Weather_OmittedTimeOfDay_FallsBackToDay()
    {
        var spec = new ModSpec
        {
            Weathers =
            {
                new WeatherSpec
                {
                    EditorId = "MF_W",
                    FogNearColor = new WeatherColorSpec { Day = new ColorSpec { R = 60, G = 120, B = 70 } },
                },
            },
        };

        var w = Single<IWeatherGetter>(Build(spec));
        // Sunrise/Sunset/Night were unset ⇒ seeded from Day.
        Assert.Equal(w.FogNearColor!.Day, w.FogNearColor!.Sunrise);
        Assert.Equal(w.FogNearColor!.Day, w.FogNearColor!.Night);
    }

    [Fact]
    public void Weather_NoFlags_DefaultsToPleasant()
    {
        var spec = new ModSpec { Weathers = { new WeatherSpec { EditorId = "MF_Clear" } } };
        var w = Single<IWeatherGetter>(Build(spec));
        Assert.Equal(Weather.Flag.Pleasant, w.Flags);
    }

    [Fact]
    public void Weather_CloudLayer_WritesTextureSpeedAndAlpha()
    {
        var spec = new ModSpec
        {
            Weathers =
            {
                new WeatherSpec
                {
                    EditorId = "MF_W",
                    Clouds =
                    {
                        new CloudLayerSpec
                        {
                            Index = 3, Texture = "Sky\\SkyrimCloudsUpper01.dds",
                            XSpeed = 0.018f, YSpeed = -0.004f, AlphaDay = 0.9f, AlphaNight = 0.5f,
                        },
                    },
                },
            },
        };

        var w = Single<IWeatherGetter>(Build(spec));
        var layer = w.Clouds[3];
        Assert.Equal(0.018f, layer.XSpeed);
        Assert.Equal(-0.004f, layer.YSpeed);
        Assert.Equal(0.9f, layer.Alphas!.Day);
        Assert.Equal(0.5f, layer.Alphas!.Night);
        Assert.Equal("Sky\\SkyrimCloudsUpper01.dds", w.CloudTextures[3]!.GivenPath);
    }

    [Fact]
    public void Weather_WindDirection_StoredAsFractionOfCircle()
    {
        var spec = new ModSpec { Weathers = { new WeatherSpec { EditorId = "MF_W", WindDirection = 180f } } };
        var w = Single<IWeatherGetter>(Build(spec));
        Assert.Equal(0.5f, w.WindDirection, 3);   // 180° ⇒ 0.5 of a full circle
    }

    [Fact]
    public void Weather_WindSpeed_Percent_NormalizedToFraction()
    {
        // Authored as 35 (percent) ⇒ 0.35 fraction.
        var spec = new ModSpec { Weathers = { new WeatherSpec { EditorId = "MF_W", WindSpeed = 35f } } };
        var w = Single<IWeatherGetter>(Build(spec));
        Assert.Equal(0.35, w.WindSpeed.Value, 3);
    }

    [Fact]
    public void Weather_FogDistances_OnlyWrittenWhenAuthored()
    {
        var spec = new ModSpec
        {
            Weathers = { new WeatherSpec { EditorId = "MF_W", FogDayNear = 256, FogDayFar = 9000 } },
        };
        var w = Single<IWeatherGetter>(Build(spec));
        Assert.Equal(256f, w.FogDistanceDayNear);
        Assert.Equal(9000f, w.FogDistanceDayFar);
    }

    // ---------------------------------------------------------------- climate build

    [Fact]
    public void Climate_WiresWeatherLinksWithChances()
    {
        var spec = new ModSpec
        {
            Weathers =
            {
                new WeatherSpec { EditorId = "MF_Fog" },
                new WeatherSpec { EditorId = "MF_Clear" },
            },
            Climates =
            {
                new ClimateSpec
                {
                    EditorId = "MF_Clim",
                    Weathers =
                    {
                        new WeatherChanceSpec { Weather = "MF_Fog", Chance = 75 },
                        new WeatherChanceSpec { Weather = "MF_Clear", Chance = 25 },
                    },
                },
            },
        };

        var result = Build(spec);
        var clim = Single<IClimateGetter>(result);
        var weathers = result.Mod.EnumerateMajorRecords<IWeatherGetter>().ToDictionary(w => w.EditorID!, w => w.FormKey);

        Assert.Equal(2, clim.WeatherTypes!.Count);
        var fog = clim.WeatherTypes!.Single(t => t.Weather.FormKey == weathers["MF_Fog"]);
        var clear = clim.WeatherTypes!.Single(t => t.Weather.FormKey == weathers["MF_Clear"]);
        Assert.Equal(75, fog.Chance);
        Assert.Equal(25, clear.Chance);
    }

    [Fact]
    public void Climate_ParsesSunTimes()
    {
        var spec = new ModSpec
        {
            Weathers = { new WeatherSpec { EditorId = "MF_W" } },
            Climates =
            {
                new ClimateSpec
                {
                    EditorId = "MF_Clim",
                    Weathers = { new WeatherChanceSpec { Weather = "MF_W" } },
                    SunriseBegin = "06:00", SunriseEnd = "09:30",
                    SunsetBegin = "17:00", SunsetEnd = "20:00",
                    Moons = { "Masser" }, Volatility = 40,
                },
            },
        };

        var clim = Single<IClimateGetter>(Build(spec));
        Assert.Equal(new TimeOnly(6, 0), clim.SunriseBegin);
        Assert.Equal(new TimeOnly(9, 30), clim.SunriseEnd);
        Assert.Equal(new TimeOnly(17, 0), clim.SunsetBegin);
        Assert.Equal(new TimeOnly(20, 0), clim.SunsetEnd);
        Assert.Equal(Climate.Moon.Masser, clim.Moons);
        Assert.Equal(40, clim.Volatility);
    }

    [Fact]
    public void Climate_ResolvesExternalWeatherRef()
    {
        var spec = new ModSpec
        {
            Climates =
            {
                new ClimateSpec
                {
                    EditorId = "MF_Clim",
                    Weathers = { new WeatherChanceSpec { Weather = "Skyrim.esm:0x0C8220", Chance = 100 } },
                },
            },
        };
        var clim = Single<IClimateGetter>(Build(spec));
        Assert.Single(clim.WeatherTypes!);
        Assert.Equal(0x0C8220u, clim.WeatherTypes![0].Weather.FormKey.ID);
    }

    [Fact]
    public void Climate_UnresolvedWeatherRef_IsDroppedAndWarned()
    {
        var spec = new ModSpec
        {
            Climates =
            {
                new ClimateSpec
                {
                    EditorId = "MF_Clim",
                    Weathers = { new WeatherChanceSpec { Weather = "DoesNotExist", Chance = 100 } },
                },
            },
        };
        var result = Build(spec);
        var clim = Single<IClimateGetter>(result);
        Assert.Empty(clim.WeatherTypes!);   // bad ref dropped, not a null-FormKey entry
        Assert.Contains(result.Warnings, w => w.Contains("DoesNotExist"));
    }

    // ---------------------------------------------------------------- validate

    [Fact]
    public void Validate_Clean_WeatherClimate_NoProblems()
    {
        var spec = new ModSpec
        {
            Weathers = { new WeatherSpec { EditorId = "MF_W", Flags = { "Cloudy" } } },
            Climates =
            {
                new ClimateSpec
                {
                    EditorId = "MF_Clim",
                    Weathers = { new WeatherChanceSpec { Weather = "MF_W", Chance = 100 } },
                },
            },
        };
        Assert.Empty(Generator.Validate(spec));
    }

    [Fact]
    public void Validate_ColorOutOfRange_Flagged()
    {
        var spec = new ModSpec
        {
            Weathers =
            {
                new WeatherSpec
                {
                    EditorId = "MF_W",
                    SkyUpperColor = new WeatherColorSpec { Day = new ColorSpec { R = 300, G = -5, B = 0 } },
                },
            },
        };
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("r = 300"));
        Assert.Contains(problems, p => p.Contains("g = -5"));
    }

    [Fact]
    public void Validate_BadFlag_Flagged()
    {
        var spec = new ModSpec { Weathers = { new WeatherSpec { EditorId = "MF_W", Flags = { "Sunny" } } } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("invalid flag 'Sunny'"));
    }

    [Fact]
    public void Validate_EmptyClimate_Flagged()
    {
        var spec = new ModSpec { Climates = { new ClimateSpec { EditorId = "MF_Clim" } } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("lists no weathers"));
    }

    [Fact]
    public void Validate_ZeroChanceSum_Flagged()
    {
        var spec = new ModSpec
        {
            Weathers = { new WeatherSpec { EditorId = "MF_W" } },
            Climates =
            {
                new ClimateSpec
                {
                    EditorId = "MF_Clim",
                    Weathers = { new WeatherChanceSpec { Weather = "MF_W", Chance = 0 } },
                },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("sum to 0"));
    }

    [Fact]
    public void Validate_BadSunTime_Flagged()
    {
        var spec = new ModSpec
        {
            Weathers = { new WeatherSpec { EditorId = "MF_W" } },
            Climates =
            {
                new ClimateSpec
                {
                    EditorId = "MF_Clim",
                    Weathers = { new WeatherChanceSpec { Weather = "MF_W" } },
                    SunriseBegin = "25:99",
                },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("not a HH:MM"));
    }

    [Fact]
    public void Validate_SunriseAfterSunset_Flagged()
    {
        var spec = new ModSpec
        {
            Weathers = { new WeatherSpec { EditorId = "MF_W" } },
            Climates =
            {
                new ClimateSpec
                {
                    EditorId = "MF_Clim",
                    Weathers = { new WeatherChanceSpec { Weather = "MF_W" } },
                    SunriseEnd = "18:00", SunsetBegin = "12:00",
                },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("sunriseEnd"));
    }

    [Fact]
    public void Validate_DuplicateCloudIndex_Flagged()
    {
        var spec = new ModSpec
        {
            Weathers =
            {
                new WeatherSpec
                {
                    EditorId = "MF_W",
                    Clouds =
                    {
                        new CloudLayerSpec { Index = 2 },
                        new CloudLayerSpec { Index = 2 },
                    },
                },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("duplicate cloud index 2"));
    }

    [Fact]
    public void Validate_DuplicateEditorId_AcrossWeatherAndClimate_Flagged()
    {
        var spec = new ModSpec
        {
            Weathers = { new WeatherSpec { EditorId = "MF_Dup" } },
            Climates = { new ClimateSpec { EditorId = "MF_Dup", Weathers = { new WeatherChanceSpec { Weather = "MF_Dup" } } } },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("duplicate editorId 'MF_Dup'"));
    }
}
