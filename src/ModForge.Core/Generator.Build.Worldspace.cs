namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Worldspace (WRLD) build.
    //
    //  Emits a NEW exterior worldspace (name, climate, water, parent, map bounds, land/water
    //  defaults). Records are created and all FormLinks (climate/water/parent/…) wired here in one
    //  go, resolving in-spec editorIds OR external "<master>:0xFORMID" refs.
    //
    //  HONEST SCOPE: this is the RECORD layer only. A worldspace with no SubCells block tree,
    //  no terrain (LAND), no LOD meshes and no navmesh is a valid record but NOT a walkable world
    //  — that heightmap/LOD/navmesh authoring is Creation-Kit work ModForge does not do. The value
    //  here is (a) attaching a custom Climate to a world and (b) defining weather/spawn REGIONS,
    //  which the Climate/Weather feature pairs with. See docs/SPEC.md.
    //
    //  Region (REGN) build is in Generator.Build.Regions.cs.
    //  Returns counts folded into BuildStats + the link tallies.
    // -------------------------------------------------------------------------------
    private static (int Worldspaces, int TerrainCells, int NavmeshCells, int Links, int ExtLinks) BuildWorldspaces(
        SkyrimMod mod, ModSpec spec, Dictionary<string, FormKey> formKeyByEd, Action<string> warn, string specDir = "")
    {
        int worldspaces = 0, terrainCells = 0, navmeshCells = 0, links = 0, extLinks = 0;
        // Per-cell navmeshes collected here → single NAVI override written after all worldspaces
        // (the flat-navmesh build + NAVI write live in Generator.Build.Navmesh.cs).
        var navmInfos = new List<NavmCellInfo>();

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
            // Build one CELL+LAND and slot it into the worldspace's block tree. Shared by the flat
            // (per-cell Height) path and the heightmap (PNG-derived) path. `heightDeltas` are the
            // 33×33 VHGT signed-delta bytes; flat cells pass all-zero (terrain at Z = Offset*8).
            // VertexNormalsHeightMap flag MUST be set or the engine skips VHGT/VNML and the player
            // falls through. Block/sub-block coords use the same /32 and /8 floor-division as the
            // exterior placement code (proven against vanilla Skyrim.esm cell groups).
            void EmitCell(int cx, int cy, float offset, Noggog.Array2d<byte> heightDeltas, bool navmesh)
            {
                short bx = (short)FloorDiv(cx, 32), by = (short)FloorDiv(cy, 32);
                short sx = (short)FloorDiv(cx, 8),  sy = (short)FloorDiv(cy, 8);

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
                var xTag = cx < 0 ? $"m{-cx}" : cx.ToString();
                var yTag = cy < 0 ? $"m{-cy}" : cy.ToString();
                var cell = new Cell(mod, $"{edBase}_Cell_{xTag}_{yTag}");
                cell.Grid = new CellGrid { Point = new Noggog.P2Int(cx, cy) };

                var land = new Landscape(mod);
                land.Flags = Landscape.Flag.VertexNormalsHeightMap;
                land.VertexHeightMap = new LandscapeVertexHeightMap
                {
                    Offset = offset,           // VHGT scale: actual_Z = Offset * 8
                    HeightMap = heightDeltas,
                    Unknown = new Noggog.P3UInt8(0, 0, 0),
                };
                // MVP: normals straight up (128,128,255). B-route recomputes from neighbour heights.
                land.VertexNormals = new Noggog.Array2d<Noggog.P3UInt8>(33, 33, new Noggog.P3UInt8(128, 128, 255));
                cell.Landscape = land;

                if (navmesh)
                {
                    var cs = new WorldspaceCellSpec { X = cx, Y = cy, Navmesh = true };
                    AddFlatCellNavmesh(mod, cell, cs, w.FormKey, navmInfos);
                    navmeshCells++;
                }

                sub.Items.Add(cell);
                terrainCells++;
            }

            if (ws.Heightmap is { } hmSpec)
            {
                // Non-flat terrain: derive the cell grid from PNG size and encode each cell's VHGT.
                if (ws.Cells.Count > 0)
                    warn($"  ! worldspace '{ws.EditorId}' has both heightmap and cells — using heightmap, ignoring {ws.Cells.Count} flat cell(s)");

                var hm = Heightmap.Load(hmSpec, specDir);
                for (int cyi = 0; cyi < hm.CellsY; cyi++)
                    for (int cxi = 0; cxi < hm.CellsX; cxi++)
                    {
                        int gx = hm.OriginX + cxi, gy = hm.OriginY + cyi;
                        var grid = hm.SampleCell(cxi, cyi);
                        var (offset, deltas) = Vhgt.Encode(grid, warn, $"cell({gx},{gy})");
                        EmitCell(gx, gy, offset, deltas, navmesh: false);   // MVP: heightmap mode skips navmesh
                    }
            }
            else
            {
                // Flat terrain: each cell a uniform-height LAND (all-zero deltas).
                foreach (var cs in ws.Cells)
                    EmitCell(cs.X, cs.Y, cs.Height / 8f, new Noggog.Array2d<byte>(33, 33, 0), cs.Navmesh);
            }
        }

        // One additive NAVI override (master 0x00012FB4) carrying every cell's navmesh info.
        WriteNaviInfoMap(mod, navmInfos);

        return (worldspaces, terrainCells, navmeshCells, links, extLinks);
    }
}
