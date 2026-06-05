namespace ModForge;

public static partial class Generator
{
    // Build all Region (REGN) records in the spec, resolving worldspace/weather refs via formKeyByEd.
    // Returns (regionCount, linksAdded, extLinksAdded).
    private static (int Regions, int Links, int ExtLinks) BuildRegions(
        SkyrimMod mod, ModSpec spec, Dictionary<string, FormKey> formKeyByEd, Action<string> warn)
    {
        int regions = 0, links = 0, extLinks = 0;

        void Wire(string what, string refStr, Action<FormKey> set)
        {
            if (string.IsNullOrWhiteSpace(refStr)) return;
            if (TryResolveRef(refStr, formKeyByEd, out var fk))
            {
                set(fk);
                links++;
                if (LooksExternalRef(refStr)) extLinks++;
            }
            else warn($"  ! {what} ref '{refStr}' unresolved (need in-spec editorId or <master>:0xFORMID)");
        }

        foreach (var rg in spec.Regions)
        {
            var r = mod.Regions.AddNew();
            r.EditorID = rg.EditorId;

            Wire($"region '{rg.EditorId}' worldspace", rg.Worldspace, fk => r.Worldspace.SetTo(fk));

            // Area polygon (RPLD). Each point is a 2-D world position; the closed loop bounds the area.
            if (rg.Area.Count > 0)
            {
                var area = new RegionArea { EdgeFallOff = rg.EdgeFallOff };
                area.RegionPointListData = new Noggog.ExtendedList<Noggog.P2Float>();
                foreach (var p in rg.Area) area.RegionPointListData.Add(new Noggog.P2Float(p.X, p.Y));
                r.RegionAreas.Add(area);
            }

            // Map color (RCLR) — 0xRRGGBB.
            if (TryParseRgb(rg.MapColor, out var color)) r.MapColor = color;

            // Weather table (RDWT) — the climate hook. Each entry is a Weather ref + chance weight.
            if (rg.Weather.Count > 0)
            {
                var weather = new RegionWeather { Priority = rg.WeatherPriority };
                weather.Weathers = new Noggog.ExtendedList<WeatherType>();
                foreach (var we in rg.Weather)
                {
                    var wt = new WeatherType { Chance = we.Chance };
                    Wire($"region '{rg.EditorId}' weather", we.Weather, fk => wt.Weather.SetTo(fk));
                    Wire($"region '{rg.EditorId}' weather global", we.Global, fk => wt.Global.SetTo(fk));
                    weather.Weathers.Add(wt);
                }
                r.Weather = weather;
            }

            regions++;
        }

        return (regions, links, extLinks);
    }

    // Parse "0xRRGGBB" / "RRGGBB" (or "#RRGGBB") into an opaque Color; false if blank/malformed.
    private static bool TryParseRgb(string s, out System.Drawing.Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim().TrimStart('#');
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s[2..];
        if (!uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) return false;
        color = System.Drawing.Color.FromArgb(0, (byte)(v >> 16), (byte)(v >> 8), (byte)v);
        return true;
    }

    // BuildContext entry point — runs the worldspace/region build after the formKey table exists
    // (so regions can resolve in-spec worldspace editorIds), folding counts/links into the context.
    private sealed partial class BuildContext
    {
        public void BuildWorldspacesAndRegions()
        {
            var (w, tc, nc, l, e) = Generator.BuildWorldspaces(mod, spec, formKeyByEd, Warn);
            var (r, rl, re) = Generator.BuildRegions(mod, spec, formKeyByEd, Warn);
            worldspacesBuilt = w;
            regionsBuilt = r;
            terrainCellsBuilt = tc;
            navmeshCellsBuilt = nc;
            linksWired += l + rl;
            extLinks += e + re;
        }
    }
}
