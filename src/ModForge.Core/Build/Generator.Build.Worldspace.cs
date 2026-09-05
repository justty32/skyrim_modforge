namespace ModForge;

public static partial class Generator
{
    // LAND base-texture (BTXT) layer index. Vanilla marks the quadrant base layer with 0xFFFF (-1),
    // distinguishing it from the 0-indexed alpha (ATXT) layers stacked on top.
    private const ushort BaseLayerNumber = 0xFFFF;

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
        SkyrimMod mod, ModSpec spec, List<PlacementSpec> placements,
        Dictionary<string, FormKey> formKeyByEd,
        Dictionary<string, string> godotImportedIdSources,
        Action<string> warn, string specDir = "")
    {
        int worldspaces = 0, terrainCells = 0, navmeshCells = 0, links = 0, extLinks = 0;
        // Per-cell navmeshes collected here → single NAVI override written after all worldspaces
        // (the flat-navmesh build + NAVI write live in Generator.Build.Navmesh.cs).
        var navmInfos = new List<NavmCellInfo>();
        var reservedIds = GodotReservedIds(mod, spec, placements);

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

            // Land/water defaults（淹水修正）＋ 地圖選單邊界與 local-map 鏡頭：純資料，見 .Terrain.cs。
            ApplyWorldDefaultsAndMap(w, ws);

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

            // Optional single-layer terrain texture: resolve the LTEX once; EmitCell stamps it on
            // every cell's LAND as the BASE layer of all 4 quadrants (BTXT). No per-vertex blend.
            FormKey? baseTexFk = null;
            Wire($"worldspace '{ws.EditorId}' baseTexture", ws.BaseTexture, fk => baseTexFk = fk);

            var texLayers = ResolveTextureLayers(ws, specDir, formKeyByEd, warn, ref links, ref extLinks);

            // Build one CELL+LAND and slot it into the worldspace's block tree. Shared by the flat
            // (per-cell Height) path and the heightmap (PNG-derived) path. `heightDeltas` are the
            // 33×33 VHGT signed-delta bytes; flat cells pass all-zero (terrain at Z = Offset*8).
            // This stays a local function because it is the one piece that DOES capture the loop's
            // mutable state (terrainCells / navmeshCells / navmInfos / w / ws); the two pure pieces
            // it used to inline — the block-tree lookup and the LAND record itself — moved to
            // GetOrAddSubBlock / BuildCellLandscape in Generator.Build.Worldspace.Terrain.cs, which
            // is also where the VertexNormalsHeightMap-flag and floor-division notes now live.
            void EmitCell(int cx, int cy, float offset, Noggog.Array2d<byte> heightDeltas, bool navmesh,
                          Noggog.Array2d<Noggog.P3UInt8>? normals = null,
                          WorldspaceCellSpec? cellSpec = null)
            {
                var sub = GetOrAddSubBlock(w, cx, cy);
                var edBase = string.IsNullOrWhiteSpace(ws.EditorId) ? "MF" : ws.EditorId;
                var xTag = cx < 0 ? $"m{-cx}" : cx.ToString();
                var yTag = cy < 0 ? $"m{-cy}" : cy.ToString();
                var cell = new Cell(mod, $"{edBase}_Cell_{xTag}_{yTag}");
                cell.Grid = new CellGrid { Point = new Noggog.P2Int(cx, cy) };
                if (cellSpec is not null)
                {
                    Wire($"worldspace '{ws.EditorId}' cell ({cx},{cy}) lightingTemplate",
                        cellSpec.LightingTemplate, fk => cell.LightingTemplate.SetTo(fk));
                    Wire($"worldspace '{ws.EditorId}' cell ({cx},{cy}) imageSpace",
                        cellSpec.ImageSpace, fk => cell.ImageSpace.SetTo(fk));
                }

                cell.Landscape = BuildCellLandscape(mod, baseTexFk, texLayers, cx, cy, offset, heightDeltas, normals);
                if (navmesh)
                {
                    var cs = new WorldspaceCellSpec
                    {
                        X = cx, Y = cy, Height = offset * 8f, Navmesh = true,
                        // Carried through so an authored mesh survives the heightmap/flat split;
                        // null (the heightmap path) keeps the flat-quad behaviour unchanged.
                        NavmeshGeometry = cellSpec?.NavmeshGeometry,
                    };
                    AddCellNavmesh(mod, cell, cs, w.FormKey, navmInfos, warn);
                    navmeshCells++;
                }

                sub.Items.Add(cell);
                terrainCells++;
            }

            if (ws.Heightmap is { } hmSpec)
            {
                if (ws.Cells.Count > 0)
                    warn($"  ! worldspace '{ws.EditorId}' has both heightmap and cells — using heightmap, ignoring {ws.Cells.Count} flat cell(s)");
                EmitHeightmapCells(hmSpec, specDir, warn,
                    (cx, cy, offset, deltas, navmesh, normals) =>
                        EmitCell(cx, cy, offset, deltas, navmesh, normals));
            }
            else
            {
                // Flat terrain: each cell a uniform-height LAND (all-zero deltas).
                foreach (var cs in ws.Cells)
                    EmitCell(cs.X, cs.Y, cs.Height / 8f, new Noggog.Array2d<byte>(33, 33, 0),
                        cs.Navmesh, cellSpec: cs);
            }

            // Expand Godot placements into this run's placement view for BuildPlacements(), which runs
            // after worldspaces. Keep the caller's spec untouched so repeated Build() calls are stable.
            if (ws.GodotPlacements is { } gpSpec)
                placements.AddRange(LoadValidatedGodotPlacements(
                    gpSpec, specDir, ws.EditorId, spec, formKeyByEd, reservedIds,
                    godotImportedIdSources));
        }

        // All quads now have stable FormKeys, so reciprocal cross-cell edge links can name them.
        ConnectAdjacentCellNavmeshes(navmInfos);

        // Authored meshes declare their own cross-cell seams; resolve them to FormKeys now.
        ConnectSpecExternalEdges(navmInfos, warn);

        // One additive NAVI override (master 0x00012FB4) carrying every cell's navmesh info.
        WriteNaviInfoMap(mod, navmInfos);

        return (worldspaces, terrainCells, navmeshCells, links, extLinks);
    }
}
