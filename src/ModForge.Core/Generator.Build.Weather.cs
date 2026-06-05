using System.Drawing;
using Mutagen.Bethesda.Skyrim.Assets;
using Mutagen.Bethesda.Plugins.Assets;
using Noggog;

namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  WEATHER (WTHR) + CLIMATE (CLMT) build (It.W).
    //
    //  A fresh Weather from AddNew() already allocates its fixed-length CloudTextures[29]
    //  and Clouds[32] arrays (all null elements) and its WeatherColor sub-objects, but the
    //  per-element CloudLayer/AssetLink instances are null until we set them. A fresh
    //  Climate's WeatherTypes list is null until assigned. We default everything a minimal
    //  spec omits to a vanilla-sane clear-day baseline so the record is always valid.
    //
    //  Colours are authored 0..255 (validate clamps); Mutagen stores System.Drawing.Color.
    //  Wind/precipitation fades are Noggog.Percent (0..1). Climate sun times are TimeOnly.
    // -------------------------------------------------------------------------------

    // A neutral clear-day baseline used to seed any colour the spec leaves unset, so a
    // weather is never authored with pure-black skies. Values mirror vanilla SkyrimClear.
    private static readonly Color BaselineSky = Color.FromArgb(0, 57, 119, 155);
    private static readonly Color BaselineFog = Color.FromArgb(0, 40, 111, 140);
    private static readonly Color BaselineCloud = Color.FromArgb(0, 237, 243, 254);
    private static readonly Color BaselineSun = Color.FromArgb(0, 255, 248, 224);

    private static Color ToColor(ColorSpec c) =>
        Color.FromArgb(Clamp255(c.A), Clamp255(c.R), Clamp255(c.G), Clamp255(c.B));

    private static int Clamp255(int v) => v < 0 ? 0 : v > 255 ? 255 : v;

    // Wrap a degree value into [0,360).
    private static float NormalizeDegrees(float deg)
    {
        deg %= 360f;
        return deg < 0 ? deg + 360f : deg;
    }

    // Fill a Mutagen WeatherColor (Sunrise/Day/Sunset/Night) from a spec colour set, seeding
    // any omitted time-of-day from Day, and Day from the given baseline. Always 4 ToD set.
    private static void FillWeatherColor(WeatherColor dst, WeatherColorSpec? src, Color baseline)
    {
        var day = src?.Day is { } d ? ToColor(d) : baseline;
        dst.Day = day;
        dst.Sunrise = src?.Sunrise is { } sr ? ToColor(sr) : day;
        dst.Sunset = src?.Sunset is { } ss ? ToColor(ss) : day;
        dst.Night = src?.Night is { } nt ? ToColor(nt) : day;
    }

    // WindSpeed/fades are 0..1 fractions; tolerate authoring as 0..100 percent.
    private static Percent ToFraction(float v)
    {
        double d = v > 1.0001f ? v / 100.0 : v;
        if (d < 0) d = 0; if (d > 1) d = 1;
        return Percent.FactoryPutInRange(d);
    }

    private static void BuildWeathers(ModSpec spec, ISkyrimMod mod, Action<string> warn)
    {
        foreach (var ws in spec.Weathers)
        {
            var w = mod.Weathers.AddNew();
            w.EditorID = ws.EditorId;

            // Classification flags (default Pleasant clear weather).
            Weather.Flag flags = 0;
            foreach (var f in ws.Flags)
                if (Enum.TryParse<Weather.Flag>(f, ignoreCase: true, out var fl)) flags |= fl;
                else warn($"  ! weather '{ws.EditorId}' unknown flag '{f}' (Pleasant|Cloudy|Rainy|Snow|SkyStaticsAlwaysVisible|SkyStaticsFollowsSunPosition)");
            if (flags == 0) flags = Weather.Flag.Pleasant;
            w.Flags = flags;

            // Per-time-of-day colours (each seeded so a partial/omitted colour is still valid).
            FillWeatherColor(w.SkyUpperColor, ws.SkyUpperColor, BaselineSky);
            FillWeatherColor(w.SkyLowerColor, ws.SkyLowerColor, BaselineSky);
            FillWeatherColor(w.FogNearColor, ws.FogNearColor, BaselineFog);
            FillWeatherColor(w.FogFarColor, ws.FogFarColor, BaselineFog);
            FillWeatherColor(w.HorizonColor, ws.HorizonColor, BaselineSky);
            FillWeatherColor(w.SunColor, ws.SunColor, BaselineSun);
            FillWeatherColor(w.SunlightColor, ws.SunlightColor, BaselineSun);
            FillWeatherColor(w.AmbientColor, ws.AmbientColor, BaselineSky);
            FillWeatherColor(w.StarsColor, ws.StarsColor, Color.FromArgb(0, 0, 0, 0));

            // Cloud layers: texture + scroll speed + per-ToD colours/alphas, by index 0..31.
            foreach (var cl in ws.Clouds)
            {
                if (cl.Index < 0 || cl.Index >= w.Clouds.Length)
                { warn($"  ! weather '{ws.EditorId}' cloud index {cl.Index} out of range 0..{w.Clouds.Length - 1} — skipped"); continue; }

                var layer = new CloudLayer
                {
                    Enabled = true,
                    XSpeed = cl.XSpeed,
                    YSpeed = cl.YSpeed,
                    Colors = new WeatherColor(),
                    Alphas = new WeatherAlpha
                    {
                        Sunrise = cl.AlphaSunrise ?? 1f,
                        Day = cl.AlphaDay ?? 1f,
                        Sunset = cl.AlphaSunset ?? 1f,
                        Night = cl.AlphaNight ?? 1f,
                    },
                };
                FillWeatherColor(layer.Colors, cl.Colors ?? ws.CloudColor, BaselineCloud);
                w.Clouds[cl.Index] = layer;

                if (cl.Index < w.CloudTextures.Length && !string.IsNullOrWhiteSpace(cl.Texture))
                    w.CloudTextures[cl.Index] = new AssetLink<SkyrimTextureAssetType>(cl.Texture);
                else if (!string.IsNullOrWhiteSpace(cl.Texture))
                    warn($"  ! weather '{ws.EditorId}' cloud index {cl.Index} has a texture but the texture table only holds {w.CloudTextures.Length} — texture dropped");
            }

            // Wind. WindSpeed is a Noggog.Percent (0..1). WindDirection/Range are stored as a
            // *fraction of a full circle* (0..1) on disk even though the property is a float — the
            // binary writer rejects values outside 0..1. We author in friendlier degrees, so divide.
            w.WindSpeed = ToFraction(ws.WindSpeed);
            w.WindDirection = NormalizeDegrees(ws.WindDirection) / 360f;
            w.WindDirectionRange = Math.Clamp(ws.WindDirectionRange, 0f, 360f) / 360f;

            // Fog distances (only when authored; -1 ⇒ leave the engine baseline of 0).
            if (ws.FogDayNear >= 0) w.FogDistanceDayNear = ws.FogDayNear;
            if (ws.FogDayFar >= 0) w.FogDistanceDayFar = ws.FogDayFar;
            if (ws.FogNightNear >= 0) w.FogDistanceNightNear = ws.FogNightNear;
            if (ws.FogNightFar >= 0) w.FogDistanceNightFar = ws.FogNightFar;

            if (ws.TransitionDelta > 0) w.TransDelta = ws.TransitionDelta;
        }
    }

    // BuildContext entry point — thin instance wrapper so the orchestrator (Generator.Build.cs)
    // can call this like the other passes. Qualified with Generator. because the same-named
    // instance step would otherwise hide the outer static.
    private sealed partial class BuildContext
    {
        public void BuildWeatherRecords() => Generator.BuildWeathers(spec, mod, Warn);
    }
}
