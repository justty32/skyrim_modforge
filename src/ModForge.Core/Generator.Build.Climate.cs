using System.Drawing;
using Mutagen.Bethesda.Skyrim.Assets;
using Mutagen.Bethesda.Plugins.Assets;

namespace ModForge;

public static partial class Generator
{
    private static void BuildClimates(ModSpec spec, ISkyrimMod mod, Action<string> warn)
    {
        foreach (var cs in spec.Climates)
        {
            var cl = mod.Climates.AddNew();
            cl.EditorID = cs.EditorId;
            cl.WeatherTypes ??= new();   // null on a fresh Climate; weathers wired in pass 2

            cl.SunriseBegin = ParseTime(cs.SunriseBegin, new TimeOnly(5, 30), cs.EditorId, "sunriseBegin", warn);
            cl.SunriseEnd = ParseTime(cs.SunriseEnd, new TimeOnly(10, 0), cs.EditorId, "sunriseEnd", warn);
            cl.SunsetBegin = ParseTime(cs.SunsetBegin, new TimeOnly(16, 0), cs.EditorId, "sunsetBegin", warn);
            cl.SunsetEnd = ParseTime(cs.SunsetEnd, new TimeOnly(20, 30), cs.EditorId, "sunsetEnd", warn);

            if (!string.IsNullOrWhiteSpace(cs.SunTexture))
                cl.SunTexture = new AssetLink<SkyrimTextureAssetType>(cs.SunTexture);
            if (!string.IsNullOrWhiteSpace(cs.SunGlareTexture))
                cl.SunGlareTexture = new AssetLink<SkyrimTextureAssetType>(cs.SunGlareTexture);

            Climate.Moon moons = 0;
            foreach (var m in cs.Moons)
                if (Enum.TryParse<Climate.Moon>(m, ignoreCase: true, out var mv)) moons |= mv;
                else warn($"  ! climate '{cs.EditorId}' unknown moon '{m}' (Masser|Secunda)");
            if (cs.Moons.Count == 0) moons = Climate.Moon.Masser | Climate.Moon.Secunda;
            cl.Moons = moons;

            cl.PhaseLength = (byte)Math.Clamp(cs.PhaseLength, 0, 255);
            cl.Volatility = (byte)Math.Clamp(cs.Volatility, 0, 255);
        }
    }

    // Parse "HH:MM" (24h) into a TimeOnly; warn + fall back on bad input.
    private static TimeOnly ParseTime(string s, TimeOnly fallback, string ed, string field, Action<string> warn)
    {
        if (TimeOnly.TryParseExact(s?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
            || TimeOnly.TryParse(s, CultureInfo.InvariantCulture, out t))
            return t;
        warn($"  ! climate '{ed}' {field} '{s}' is not HH:MM — using {fallback:HH:mm}");
        return fallback;
    }

    // Pass 2: weather → precipitation (SPGD shader-particle-geometry) ref.
    private static void WireWeatherLinks(ModSpec spec, Dictionary<string, IMajorRecord> recordsByEd,
                                         Action<string, string, Action<FormKey>> resolve)
    {
        foreach (var ws in spec.Weathers)
        {
            if (string.IsNullOrWhiteSpace(ws.Precipitation)) continue;
            if (!recordsByEd.TryGetValue(ws.EditorId, out var rec) || rec is not IWeather w) continue;
            resolve($"weather '{ws.EditorId}' precipitation", ws.Precipitation,
                fk => w.Precipitation.SetTo(fk));
        }
    }

    // Pass 2: climate → weather (WTHR) FormLinks, each with its chance weight.
    private static void WireClimateLinks(ModSpec spec, Dictionary<string, IMajorRecord> recordsByEd,
                                         Action<string, string, Action<FormKey>> resolve)
    {
        foreach (var cs in spec.Climates)
        {
            if (!recordsByEd.TryGetValue(cs.EditorId, out var rec) || rec is not IClimate cl) continue;
            cl.WeatherTypes ??= new();
            foreach (var wc in cs.Weathers)
            {
                var wt = new WeatherType { Chance = wc.Chance };
                resolve($"climate '{cs.EditorId}' weather", wc.Weather, fk => wt.Weather.SetTo(fk));
                // Only keep the entry if the weather actually resolved (FormKey set).
                if (!wt.Weather.FormKey.IsNull) cl.WeatherTypes.Add(wt);
            }
        }
    }

    // BuildContext entry points — thin instance wrappers so the orchestrator (Generator.Build.cs)
    // can call these like the other passes. The static implementations above take mod/recordsByEd/
    // Resolve as parameters (no BuildContext coupling); here we hand them the context's privates.
    private sealed partial class BuildContext
    {
        public void BuildClimateRecords() => Generator.BuildClimates(spec, mod, Warn);

        public void WireWeatherAndClimateLinks()
        {
            WireWeatherLinks(spec, recordsByEd, Resolve);
            WireClimateLinks(spec, recordsByEd, Resolve);
        }
    }
}
