internal static partial class Program
{
    // Diagnostic: print a Worldspace's (WRLD) climate/water/parent links + the full map bounds,
    // camera, and land/water DEFAULTS — the fields `dump` abbreviates. Use to harvest sensible
    // vanilla values (e.g. Tamriel 0x00003C: land=-27000 water=-14000, nw=(-30,15) se=(40,-40))
    // when authoring a custom worldspace.
    private static int WorldDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var w in mod.EnumerateMajorRecords<IWorldspaceGetter>())
        {
            if (w.FormKey.ID != id) continue;
            Console.WriteLine($"0x{id:X6}  EditorID={w.EditorID}  flags={w.Flags}");
            Console.WriteLine($"  climate={(w.Climate.IsNull ? "-" : w.Climate.FormKey.ToString())}  water={(w.Water.IsNull ? "-" : w.Water.FormKey.ToString())}  lodWater={(w.LodWater.IsNull ? "-" : w.LodWater.FormKey.ToString())}");
            Console.WriteLine($"  parent={(w.Parent?.Worldspace.IsNull == false ? w.Parent!.Worldspace.FormKey.ToString() : "-")}  music={(w.Music.IsNull ? "-" : w.Music.FormKey.ToString())}  interiorLighting={(w.InteriorLighting.IsNull ? "-" : w.InteriorLighting.FormKey.ToString())}");
            Console.WriteLine($"  landDefaults: land={w.LandDefaults?.DefaultLandHeight} water={w.LandDefaults?.DefaultWaterHeight}");
            if (w.MapData is { } md)
                Console.WriteLine($"  map: nw={md.NorthwestCellCoords} se={md.SoutheastCellCoords} usable={md.UsableDimensions} pitch={md.CameraInitialPitch} camH={md.CameraMinHeight}..{md.CameraMaxHeight}");
            Console.WriteLine($"  objBounds: min={w.ObjectBoundsMin} max={w.ObjectBoundsMax}  mapScale={w.WorldMapOffsetScale}");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Worldspace in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Diagnostic: print a Region's (REGN) worldspace, area polygons, map color, and the weather
    // table (priority + each Weather ref + chance) — the climate hook. Harvest vanilla weather
    // tables (e.g. a Tamriel hold region) when authoring a custom region.
    private static int RegnDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var r in mod.EnumerateMajorRecords<IRegionGetter>())
        {
            if (r.FormKey.ID != id) continue;
            Console.WriteLine($"0x{id:X6}  EditorID={r.EditorID}");
            Console.WriteLine($"  worldspace={(r.Worldspace.IsNull ? "-" : r.Worldspace.FormKey.ToString())}  mapColor={(r.MapColor is { } mc ? $"#{mc.R:X2}{mc.G:X2}{mc.B:X2}" : "-")}  areas={r.RegionAreas.Count}");
            foreach (var a in r.RegionAreas)
                Console.WriteLine($"  area: {a.RegionPointListData?.Count ?? 0} point(s) edgeFallOff={a.EdgeFallOff}");
            if (r.Weather is { Weathers: { } wls } rw)
            {
                Console.WriteLine($"  weather: priority={rw.Priority} flags={rw.Flags} {wls.Count} entry(s)");
                foreach (var we in wls)
                    Console.WriteLine($"    weather={we.Weather.FormKey} chance={we.Chance} global={(we.Global.IsNull ? "-" : we.Global.FormKey.ToString())}");
            }
            Console.WriteLine($"  other data: map={(r.Map is null ? "-" : "set")} sounds={(r.Sounds is null ? "-" : "set")} objects={(r.Objects is null ? "-" : "set")} grasses={(r.Grasses is null ? "-" : "set")} land={(r.Land is null ? "-" : "set")}");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Region in {Path.GetFileName(inPath)}");
        return 0;
    }
}
