namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Validate — WEATHER (WTHR) + CLIMATE (CLMT) guardrails (It.W).
    //
    //  Called from the main Validate() so problems land in the same list. Checks: colour
    //  components 0..255, cloud indices/textures, wind/fog sanity, climate times monotone
    //  and in range, weather refs resolve, and that a climate actually lists a weather and
    //  its chances sum to something usable.
    // -------------------------------------------------------------------------------
    private static void ValidateWeather(ModSpec spec, List<string> problems,
                                        HashSet<string> ids, Action<string, string> checkRef)
    {
        void CheckColor(ColorSpec? c, string what)
        {
            if (c is null) return;
            foreach (var (v, n) in new[] { (c.R, "r"), (c.G, "g"), (c.B, "b"), (c.A, "a") })
                if (v < 0 || v > 255)
                    problems.Add($"{what}.{n} = {v} out of range 0..255");
        }
        void CheckWeatherColor(WeatherColorSpec? wc, string what)
        {
            if (wc is null) return;
            CheckColor(wc.Sunrise, $"{what}.sunrise");
            CheckColor(wc.Day, $"{what}.day");
            CheckColor(wc.Sunset, $"{what}.sunset");
            CheckColor(wc.Night, $"{what}.night");
        }

        foreach (var ws in spec.Weathers)
        {
            var w = $"weather '{ws.EditorId}'";
            foreach (var f in ws.Flags)
                if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.Weather.Flag>(f, true, out _))
                    problems.Add($"{w} invalid flag '{f}' (Pleasant|Cloudy|Rainy|Snow|SkyStaticsAlwaysVisible|SkyStaticsFollowsSunPosition)");

            CheckWeatherColor(ws.SkyUpperColor, $"{w} skyUpperColor");
            CheckWeatherColor(ws.SkyLowerColor, $"{w} skyLowerColor");
            CheckWeatherColor(ws.FogNearColor, $"{w} fogNearColor");
            CheckWeatherColor(ws.FogFarColor, $"{w} fogFarColor");
            CheckWeatherColor(ws.HorizonColor, $"{w} horizonColor");
            CheckWeatherColor(ws.CloudColor, $"{w} cloudColor");
            CheckWeatherColor(ws.SunColor, $"{w} sunColor");
            CheckWeatherColor(ws.SunlightColor, $"{w} sunlightColor");
            CheckWeatherColor(ws.AmbientColor, $"{w} ambientColor");
            CheckWeatherColor(ws.StarsColor, $"{w} starsColor");

            var seenIdx = new HashSet<int>();
            foreach (var cl in ws.Clouds)
            {
                if (cl.Index < 0 || cl.Index > 31)
                    problems.Add($"{w} cloud index {cl.Index} out of range 0..31");
                else if (!seenIdx.Add(cl.Index))
                    problems.Add($"{w} duplicate cloud index {cl.Index}");
                CheckWeatherColor(cl.Colors, $"{w} cloud[{cl.Index}].colors");
                foreach (var (a, n) in new[] { (cl.AlphaSunrise, "alphaSunrise"), (cl.AlphaDay, "alphaDay"), (cl.AlphaSunset, "alphaSunset"), (cl.AlphaNight, "alphaNight") })
                    if (a is { } av && (av < 0f || av > 1f))
                        problems.Add($"{w} cloud[{cl.Index}].{n} = {av} out of range 0..1");
            }

            if (ws.WindSpeed < 0 || ws.WindSpeed > 100)
                problems.Add($"{w} windSpeed {ws.WindSpeed} out of range (0..1 fraction or 0..100 percent)");
            if (ws.WindDirection < 0 || ws.WindDirection > 360)
                problems.Add($"{w} windDirection {ws.WindDirection} out of range 0..360 degrees");
            if (ws.FogDayNear >= 0 && ws.FogDayFar >= 0 && ws.FogDayNear > ws.FogDayFar)
                problems.Add($"{w} fogDayNear ({ws.FogDayNear}) must be <= fogDayFar ({ws.FogDayFar})");
            if (ws.FogNightNear >= 0 && ws.FogNightFar >= 0 && ws.FogNightNear > ws.FogNightFar)
                problems.Add($"{w} fogNightNear ({ws.FogNightNear}) must be <= fogNightFar ({ws.FogNightFar})");

            checkRef(ws.Precipitation, $"{w} precipitation");
        }

        foreach (var cs in spec.Climates)
        {
            var c = $"climate '{cs.EditorId}'";
            if (cs.Weathers.Count == 0)
                problems.Add($"{c} lists no weathers — a climate with no weather has no sky");

            int chanceSum = 0;
            foreach (var wc in cs.Weathers)
            {
                checkRef(wc.Weather, $"{c} weather");
                if (string.IsNullOrWhiteSpace(wc.Weather))
                    problems.Add($"{c} has a weather entry with an empty weather ref");
                if (wc.Chance < 0)
                    problems.Add($"{c} weather '{wc.Weather}' chance {wc.Chance} is negative");
                chanceSum += Math.Max(0, wc.Chance);
            }
            if (cs.Weathers.Count > 0 && chanceSum == 0)
                problems.Add($"{c} weather chances all sum to 0 — no weather can ever be picked");

            TimeOnly? sb = TryTime(cs.SunriseBegin), se = TryTime(cs.SunriseEnd),
                      tb = TryTime(cs.SunsetBegin), te = TryTime(cs.SunsetEnd);
            foreach (var (t, raw, n) in new[] { (sb, cs.SunriseBegin, "sunriseBegin"), (se, cs.SunriseEnd, "sunriseEnd"), (tb, cs.SunsetBegin, "sunsetBegin"), (te, cs.SunsetEnd, "sunsetEnd") })
                if (t is null) problems.Add($"{c} {n} '{raw}' is not a HH:MM 24-hour time");
            if (sb is { } a1 && se is { } a2 && a1 > a2) problems.Add($"{c} sunriseBegin ({cs.SunriseBegin}) must be <= sunriseEnd ({cs.SunriseEnd})");
            if (tb is { } b1 && te is { } b2 && b1 > b2) problems.Add($"{c} sunsetBegin ({cs.SunsetBegin}) must be <= sunsetEnd ({cs.SunsetEnd})");
            if (se is { } s2 && tb is { } t1 && s2 > t1) problems.Add($"{c} sunriseEnd ({cs.SunriseEnd}) must be <= sunsetBegin ({cs.SunsetBegin})");

            foreach (var m in cs.Moons)
                if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.Climate.Moon>(m, true, out _))
                    problems.Add($"{c} invalid moon '{m}' (Masser|Secunda)");
            if (cs.PhaseLength is < 0 or > 255) problems.Add($"{c} phaseLength {cs.PhaseLength} out of range 0..255");
            if (cs.Volatility is < 0 or > 255) problems.Add($"{c} volatility {cs.Volatility} out of range 0..255");
        }
    }

    private static TimeOnly? TryTime(string s) =>
        TimeOnly.TryParseExact(s?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
            ? t : (TimeOnly?)null;
}
