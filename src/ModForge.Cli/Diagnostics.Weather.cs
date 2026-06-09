internal static partial class Program
{
    // Compact "r,g,b" for a weather colour (alpha is unused on weather colours).
    private static string Rgb(System.Drawing.Color c) => $"{c.R},{c.G},{c.B}";

    // -------------------------------------------------------------------------------
    //  weatherdiag / climatediag — print a WTHR or CLMT record's full atmospheric field
    //  set from any plugin (compare a generated record against a vanilla one). Discover
    //  vanilla FormIDs with `find <Skyrim.esm> sky Weather` / `find <Skyrim.esm> climate Climate`.
    // -------------------------------------------------------------------------------
    private static int WeatherDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var w in mod.EnumerateMajorRecords<IWeatherGetter>())
        {
            if (w.FormKey.ID != id) continue;
            void Col(string name, IWeatherColorGetter? c)
            {
                if (c is null) { Console.WriteLine($"  {name,-14} -"); return; }
                Console.WriteLine($"  {name,-14} sunrise={Rgb(c.Sunrise)}  day={Rgb(c.Day)}  sunset={Rgb(c.Sunset)}  night={Rgb(c.Night)}");
            }
            static string Fk(Mutagen.Bethesda.Plugins.IFormLinkGetter<IImageSpaceGetter>? l)
                => l is { } x && !x.FormKey.IsNull ? x.FormKey.ToString() : "-";
            Console.WriteLine($"0x{id:X6}  EditorID={w.EditorID}");
            Console.WriteLine($"  Flags = {w.Flags}");
            Console.WriteLine($"  Wind  speed={w.WindSpeed} direction={w.WindDirection * 360f:0.#}deg range={w.WindDirectionRange * 360f:0.#}deg transDelta={w.TransDelta}");
            Console.WriteLine($"  Precipitation = {(w.Precipitation.FormKeyNullable is { } pk && !pk.IsNull ? pk.ToString() : "-")}");
            var isp = w.ImageSpaces;
            Console.WriteLine($"  ImageSpaces  sunrise={Fk(isp?.Sunrise)} day={Fk(isp?.Day)} sunset={Fk(isp?.Sunset)} night={Fk(isp?.Night)}");
            Console.WriteLine($"  FogDist day(near={w.FogDistanceDayNear} far={w.FogDistanceDayFar}) night(near={w.FogDistanceNightNear} far={w.FogDistanceNightFar})");
            Col("skyUpper", w.SkyUpperColor);
            Col("skyLower", w.SkyLowerColor);
            Col("fogNear", w.FogNearColor);
            Col("fogFar", w.FogFarColor);
            Col("horizon", w.HorizonColor);
            Col("sun", w.SunColor);
            Col("sunlight", w.SunlightColor);
            Col("ambient", w.AmbientColor);
            Col("stars", w.StarsColor);
            for (int i = 0; i < w.Clouds.Count; i++)
            {
                var c = w.Clouds[i];
                if (c.Enabled != true) continue;
                var tex = i < w.CloudTextures.Count ? w.CloudTextures[i]?.GivenPath : null;
                Console.WriteLine($"  cloud[{i}] xs={c.XSpeed} ys={c.YSpeed} tex={tex ?? "-"}"
                    + (c.Colors is { } cc ? $" day={Rgb(cc.Day)}" : "")
                    + (c.Alphas is { } al ? $" alpha(day={al.Day} night={al.Night})" : ""));
            }
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Weather in {Path.GetFileName(inPath)}");
        return 0;
    }

    private static int ClimateDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var c in mod.EnumerateMajorRecords<IClimateGetter>())
        {
            if (c.FormKey.ID != id) continue;
            Console.WriteLine($"0x{id:X6}  EditorID={c.EditorID}");
            Console.WriteLine($"  Sunrise {c.SunriseBegin:HH:mm}-{c.SunriseEnd:HH:mm}   Sunset {c.SunsetBegin:HH:mm}-{c.SunsetEnd:HH:mm}");
            Console.WriteLine($"  Moons={c.Moons}  PhaseLength={c.PhaseLength}  Volatility={c.Volatility}");
            Console.WriteLine($"  SunTexture={c.SunTexture?.GivenPath ?? "-"}  SunGlareTexture={c.SunGlareTexture?.GivenPath ?? "-"}");
            Console.WriteLine($"  WeatherTypes ({c.WeatherTypes?.Count ?? 0}):");
            foreach (var wt in c.WeatherTypes ?? Enumerable.Empty<IWeatherTypeGetter>())
                Console.WriteLine($"    weather={wt.Weather.FormKey} chance={wt.Chance}"
                    + (wt.Global.FormKey.IsNull ? "" : $" global={wt.Global.FormKey}"));
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Climate in {Path.GetFileName(inPath)}");
        return 0;
    }
}
