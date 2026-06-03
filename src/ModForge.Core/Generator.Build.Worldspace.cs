namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Worldspace (WRLD) + Region (REGN) build.
    //
    //  Emits a NEW exterior worldspace (name, climate, water, parent, map bounds, land/water
    //  defaults) and regions inside it (area polygon + weather table + map color). Records are
    //  created and all FormLinks (climate/water/parent/worldspace/weather/…) wired here in one go,
    //  resolving in-spec editorIds OR external "<master>:0xFORMID" refs.
    //
    //  HONEST SCOPE: this is the RECORD layer only. A worldspace with no SubCells block tree,
    //  no terrain (LAND), no LOD meshes and no navmesh is a valid record but NOT a walkable world
    //  — that heightmap/LOD/navmesh authoring is Creation-Kit work ModForge does not do. The value
    //  here is (a) attaching a custom Climate to a world and (b) defining weather/spawn REGIONS,
    //  which the Climate/Weather feature pairs with. See docs/SPEC.md.
    //
    //  Returns counts folded into BuildStats + the link tallies.
    // -------------------------------------------------------------------------------
    private static (int Worldspaces, int Regions, int TerrainCells, int Links, int ExtLinks) BuildWorldspacesAndRegions(
        SkyrimMod mod, ModSpec spec, Dictionary<string, FormKey> formKeyByEd, Action<string> warn)
    {
        int worldspaces = 0, regions = 0, terrainCells = 0, links = 0, extLinks = 0;

        // Resolve a ref (in-spec editorId OR external <master>:0xFORMID) and run `set`; tally links.
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

        // --- Worldspaces (WRLD) -----------------------------------------------------------------
        foreach (var ws in spec.Worldspaces)
        {
            var w = mod.Worldspaces.AddNew();
            w.EditorID = ws.EditorId;
            if (!string.IsNullOrEmpty(ws.Name)) w.Name = ws.Name;
            w.Flags = ParseFlags<Worldspace.Flag>(ws.Flags);

            // Land/water defaults — the flood-fix (a 0 default water height drowns sub-0 terrain).
            w.LandDefaults = new WorldspaceLandDefaults
            {
                DefaultLandHeight = ws.DefaultLandHeight,
                DefaultWaterHeight = ws.DefaultWaterHeight,
            };

            // Map-menu bounds + local-map camera.
            var m = ws.Map ?? new WorldMapDataSpec();
            w.MapData = new WorldspaceMap
            {
                NorthwestCellCoords = new Noggog.P2Int16((short)m.NorthwestX, (short)m.NorthwestY),
                SoutheastCellCoords = new Noggog.P2Int16((short)m.SoutheastX, (short)m.SoutheastY),
                UsableDimensions = new Noggog.P2Int(m.UsableWidth, m.UsableHeight),
                CameraInitialPitch = m.CameraInitialPitch,
                CameraMinHeight = m.CameraMinHeight,
                CameraMaxHeight = m.CameraMaxHeight,
            };

            // FormLinks. Climate is the whole point — without it the world has no sky/light cycle.
            Wire($"worldspace '{ws.EditorId}' climate", ws.Climate, fk => w.Climate.SetTo(fk));
            Wire($"worldspace '{ws.EditorId}' water", ws.Water, fk => w.Water.SetTo(fk));
            Wire($"worldspace '{ws.EditorId}' lodWater", ws.LodWater, fk => w.LodWater.SetTo(fk));
            Wire($"worldspace '{ws.EditorId}' interiorLighting", ws.InteriorLighting, fk => w.InteriorLighting.SetTo(fk));
            Wire($"worldspace '{ws.EditorId}' location", ws.Location, fk => w.Location.SetTo(fk));
            Wire($"worldspace '{ws.EditorId}' music", ws.Music, fk => w.Music.SetTo(fk));
            Wire($"worldspace '{ws.EditorId}' encounterZone", ws.EncounterZone, fk => w.EncounterZone.SetTo(fk));

            // Parent worldspace (WNAM). A child inherits the parent's climate/water/etc. for any
            // flag-controlled aspect; we set the link and leave the inherit flags at default.
            if (!string.IsNullOrWhiteSpace(ws.Parent))
            {
                if (TryResolveRef(ws.Parent, formKeyByEd, out var pfk))
                {
                    var parent = new WorldspaceParent();
                    parent.Worldspace.SetTo(pfk);
                    w.Parent = parent;
                    links++;
                    if (LooksExternalRef(ws.Parent)) extLinks++;
                }
                else warn($"  ! worldspace '{ws.EditorId}' parent ref '{ws.Parent}' unresolved");
            }

            // Register so an in-spec region (or placement) can reference this world by editorId.
            if (!string.IsNullOrWhiteSpace(ws.EditorId)) formKeyByEd[ws.EditorId] = w.FormKey;
            worldspaces++;

            // Flat terrain cells: each cell spec gets a CELL + LAND so the player can enter the
            // world via `cow <editorId> X Y` without falling into the void. Terrain is a flat
            // 33×33-vertex heightmap at Z=0 with straight-up normals — no textures needed for
            // collision. Block/sub-block coords follow the same /32 and /8 floor-division the
            // exterior placement code uses (proven against vanilla Skyrim.esm cell groups).
            foreach (var cs in ws.Cells)
            {
                short bx = (short)FloorDiv(cs.X, 32), by = (short)FloorDiv(cs.Y, 32);
                short sx = (short)FloorDiv(cs.X, 8),  sy = (short)FloorDiv(cs.Y, 8);

                var block = w.SubCells.FirstOrDefault(b => b.BlockNumberX == bx && b.BlockNumberY == by);
                if (block is null)
                {
                    block = new WorldspaceBlock { BlockNumberX = bx, BlockNumberY = by, GroupType = GroupTypeEnum.ExteriorCellBlock };
                    w.SubCells.Add(block);
                }
                var sub = block.Items.FirstOrDefault(s => s.BlockNumberX == sx && s.BlockNumberY == sy);
                if (sub is null)
                {
                    sub = new WorldspaceSubBlock { BlockNumberX = sx, BlockNumberY = sy, GroupType = GroupTypeEnum.ExteriorCellSubBlock };
                    block.Items.Add(sub);
                }

                var edBase = string.IsNullOrWhiteSpace(ws.EditorId) ? "MF" : ws.EditorId;
                var xTag = cs.X < 0 ? $"m{-cs.X}" : cs.X.ToString();
                var yTag = cs.Y < 0 ? $"m{-cs.Y}" : cs.Y.ToString();
                var cell = new Cell(mod, $"{edBase}_Cell_{xTag}_{yTag}");
                cell.Grid = new CellGrid { Point = new Noggog.P2Int(cs.X, cs.Y) };

                // Flat LAND: all 33×33 height-map deltas = 0 → terrain at Z = Offset*8 = 0.
                // All normals point straight up (128,128,255 in Skyrim's unsigned-byte encoding).
                // VertexNormalsHeightMap flag MUST be set; without it the engine skips VHGT/VNML
                // and the player falls through with no collision.
                var land = new Landscape(mod);
                land.Flags = Landscape.Flag.VertexNormalsHeightMap;
                land.VertexHeightMap = new LandscapeVertexHeightMap
                {
                    Offset = 0f,
                    HeightMap = new Noggog.Array2d<byte>(33, 33, 0),
                    Unknown = new Noggog.P3UInt8(0, 0, 0),
                };
                land.VertexNormals = new Noggog.Array2d<Noggog.P3UInt8>(33, 33, new Noggog.P3UInt8(128, 128, 255));
                cell.Landscape = land;

                sub.Items.Add(cell);
                terrainCells++;
            }
        }

        // --- Regions (REGN) ---------------------------------------------------------------------
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

        return (worldspaces, regions, terrainCells, links, extLinks);
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
            var (w, r, tc, l, e) = Generator.BuildWorldspacesAndRegions(mod, spec, formKeyByEd, Warn);
            worldspacesBuilt = w;
            regionsBuilt = r;
            terrainCellsBuilt = tc;
            linksWired += l;
            extLinks += e;
        }
    }
}
