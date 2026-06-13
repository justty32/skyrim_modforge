namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // Copy a master WORLDSPACE's inline data onto a minimal override that hosts our new cell.
        // CRITICAL: a worldspace override that omits LandDefaults resets DefaultWaterHeight to 0 —
        // and Tamriel's real default is -14000, so any terrain between -14000 and 0 gets flooded
        // ("the whole world is underwater"). We copy land/water defaults, water forms, climate, map,
        // bounds, parent, lighting, etc. but NOT the localized Name or the SubCells block tree
        // (we build our own; vanilla cells stay in the master).
        // MUST carry (transplantable — FormID/position/path based, valid in any file):
        //   • HdLodDiffuseTexture/HdLodNormalTexture (TNAM/UNAM) — LOD terrain textures
        //   • LargeReferences (RNAM): the LOD large-object list (mountains, big rocks). Each entry is
        //     (FormID, world cell X/Y) — world-position based, so it copies cleanly across files. A
        //     full WRLD override that DROPS these renders the world-map terrain corrupt (the engine
        //     has texture but no large-object LOD geometry to place). Decoded 2026-06-13.
        // Do NOT carry:
        //   • OffsetData (OFST): a FILE-LAYOUT offset table — its 11400 uint32 values are ABSOLUTE
        //     byte offsets into Skyrim.esm (range 0–154M in a 249M file). Copying them into our ESP
        //     makes the engine seek to garbage positions in OUR file → severe map corruption. OFST is
        //     vestigial in SSE (engine rebuilds the cell-offset cache at runtime), so omitting it is
        //     safe; transplanting it is not.
        // We NO LONGER carry TopCell here (handled in WorldspaceOverride directly).
        private void CopyWorldspaceEnv(IWorldspaceGetter src, Worldspace dst)
        {
            dst.Flags = src.Flags;
            dst.ObjectBoundsMin = src.ObjectBoundsMin;
            dst.ObjectBoundsMax = src.ObjectBoundsMax;
            dst.WorldMapOffsetScale = src.WorldMapOffsetScale;
            dst.DistantLodMultiplier = src.DistantLodMultiplier;
            dst.LodWaterHeight = src.LodWaterHeight;
            dst.LandDefaults = src.LandDefaults?.DeepCopy();   // DefaultWaterHeight (-14000) = THE flood fix
            dst.MaxHeight = src.MaxHeight?.DeepCopy();
            dst.MapData = src.MapData?.DeepCopy();
            dst.Parent = src.Parent?.DeepCopy();
            dst.Water.SetTo(src.Water);
            dst.LodWater.SetTo(src.LodWater);
            dst.Climate.SetTo(src.Climate);
            dst.Location.SetTo(src.Location);
            dst.EncounterZone.SetTo(src.EncounterZone);
            dst.InteriorLighting.SetTo(src.InteriorLighting);
            dst.Music.SetTo(src.Music);
            // LOD terrain texture paths (TNAM/UNAM)
            var diffuse = src.HdLodDiffuseTexture;
            if (diffuse is not null && !diffuse.IsNull)
                dst.HdLodDiffuseTexture = new AssetLink<SkyrimTextureAssetType>(diffuse.GivenPath);
            var normal = src.HdLodNormalTexture;
            if (normal is not null && !normal.IsNull)
                dst.HdLodNormalTexture = new AssetLink<SkyrimTextureAssetType>(normal.GivenPath);
            // Large References (RNAM): LOD large-object list — transplantable (FormID + world pos).
            // A full WRLD override that drops these renders the world map terrain corrupt.
            foreach (var lr in src.LargeReferences)
                dst.LargeReferences.Add(lr.DeepCopy());
            // OFST (OffsetData) intentionally NOT copied — file-layout offset table specific to
            // Skyrim.esm's byte layout; transplanting it corrupts our ESP (engine seeks to garbage).
        }

        // --- Exterior / worldspace placement (It.7d phase 3) ---------------------------------
        // An exterior cell lives inside a WRLD, nested WorldspaceBlock(type 4, /32 grid) ->
        // WorldspaceSubBlock(type 5, /8 grid) -> Cell(grid x,y). To add a ref to the world we
        // OVERRIDE the existing master cell at the target grid (same careful, Flags+Grid-only
        // override as the interior vanilla case — no localized deep-copy). We host it on a minimal
        // Worldspace override that re-states only OUR block tree (vanilla cells stay in the master).
        private Worldspace WorldspaceOverride(FormKey wsFk, IWorldspaceGetter? src)
        {
            if (worldspaceOverrides.TryGetValue(wsFk, out var ex)) return ex;
            var ws = new Worldspace(wsFk, SkyrimRelease.SkyrimSE); // override that hosts our block tree
            if (src is not null) CopyWorldspaceEnv(src, ws);       // carry land/water defaults etc.
            // Carry the worldspace PERSISTENT (top) cell as an ADDITIVE override (Tamriel = 0xD74), else
            // our override (last in load order) drops it → blank world map + no vanilla markers. The
            // CRITICAL bit, decoded 2026-06-13 after two CTDs: the persistent world cell's record-header
            // flags MUST be copied (master/USSEP 0xD74 = MajorRecordFlagsRaw 0x00040400 = Cell Persistent
            // 0x400 + internal 0x40000). CopyCellEnv copies only the DATA flags, so without this the cell
            // isn't flagged Persistent and the engine null-derefs queuing actors in Tamriel. We re-state
            // NO vanilla refs (additive); map markers are added into ws.TopCell.Persistent by BuildMapMarkers.
            if (src?.TopCell is { } srcTop)
            {
                var top = new Cell(srcTop.FormKey, SkyrimRelease.SkyrimSE);
                CopyCellEnv(srcTop, top);
                top.MajorRecordFlagsRaw = srcTop.MajorRecordFlagsRaw;   // <-- the persistent-cell flag (THE fix)
                ws.TopCell = top;
            }
            // Headless can't resolve the master's LOCALIZED worldspace Name; an omitted Name makes the
            // override blank it -> saves/HUD show "unknown location". Restate a plain Name for known
            // worldspaces. (TODO: a spec field for arbitrary worldspaces.)
            if (wsFk.ModKey.Name.Equals("Skyrim", StringComparison.OrdinalIgnoreCase) && wsFk.ID == 0x00003C)
                ws.Name = "Skyrim";
            mod.Worldspaces.Add(ws);
            worldspaceOverrides[wsFk] = ws;
            worldspaceCount++;
            return ws;
        }

        // The existing master exterior cell at grid (cx,cy), or null if that grid is ungenerated.
        private ICellGetter? FindMasterExteriorCell(string masterName, FormKey wsFk, int cx, int cy)
        {
            var cache = MasterCache(masterName);
            if (cache is null) return null;
            if (!cache.TryResolve<IWorldspaceGetter>(wsFk, out var ws))
            { Warn($"  ! worldspace {wsFk} not found in {masterName}"); return null; }
            short bx = (short)FloorDiv(cx, 32), by = (short)FloorDiv(cy, 32);
            short sx = (short)FloorDiv(cx, 8),  sy = (short)FloorDiv(cy, 8);
            foreach (var block in ws.SubCells)
            {
                if (block.BlockNumberX != bx || block.BlockNumberY != by) continue;
                foreach (var sub in block.Items)
                {
                    if (sub.BlockNumberX != sx || sub.BlockNumberY != sy) continue;
                    foreach (var c in sub.Items)
                        if (c.Grid?.Point is { } p && p.X == cx && p.Y == cy) return c;
                }
            }
            return null;
        }

        // A custom worldspace we built earlier this run (BuildWorldspacesAndRegions runs before
        // placements). Locate the cell we generated for grid (cx,cy) — it already carries LAND +
        // navmesh — so refs/markers land in the navmeshed cell and patrol/sandbox actually works.
        private Cell OwnExteriorCell(IWorldspace ownWs, int cx, int cy)
        {
            var key = (ownWs.FormKey, cx, cy);
            if (exteriorCells.TryGetValue(key, out var cached)) return cached;
            short bx = (short)FloorDiv(cx, 32), by = (short)FloorDiv(cy, 32);
            short sx = (short)FloorDiv(cx, 8),  sy = (short)FloorDiv(cy, 8);
            var block = ownWs.SubCells.FirstOrDefault(b => b.BlockNumberX == bx && b.BlockNumberY == by);
            if (block is null)
            { block = new WorldspaceBlock { BlockNumberX = bx, BlockNumberY = by, GroupType = GroupTypeEnum.ExteriorCellBlock }; ownWs.SubCells.Add(block); }
            var sub = block.Items.FirstOrDefault(s => s.BlockNumberX == sx && s.BlockNumberY == sy);
            if (sub is null)
            { sub = new WorldspaceSubBlock { BlockNumberX = sx, BlockNumberY = sy, GroupType = GroupTypeEnum.ExteriorCellSubBlock }; block.Items.Add(sub); }
            var cell = sub.Items.FirstOrDefault(c => c.Grid?.Point is { } p && p.X == cx && p.Y == cy);
            if (cell is null)
            {
                Warn($"  ! placement grid ({cx},{cy}) has no generated cell in worldspace '{ownWs.EditorID}' — creating a bare cell (no navmesh; NPCs there can't path)");
                cell = new Cell(mod, $"{ownWs.EditorID}_Cell_{(cx < 0 ? "m" : "")}{Math.Abs(cx)}_{(cy < 0 ? "m" : "")}{Math.Abs(cy)}")
                { Grid = new CellGrid { Point = new Noggog.P2Int(cx, cy) } };
                sub.Items.Add(cell);
            }
            exteriorCells[key] = cell;
            return cell;
        }

        // The worldspace's PERSISTENT (top) cell override — where worldspace-persistent refs (map
        // markers) belong, alongside the vanilla markers. Triggers the worldspace override (which carries
        // the master persistent cell, with its record flags, additively). Null for a custom worldspace
        // (no master TopCell) or a master that doesn't resolve — caller falls back to a grid cell.
        private Cell? WorldspacePersistentCell(string worldspaceRef)
        {
            if (!LooksExternalRef(worldspaceRef)) return null;
            if (!TryExternalRef(worldspaceRef, out var wsFk)) return null;
            var masterName = worldspaceRef[..worldspaceRef.IndexOf(':')].Trim();
            IWorldspaceGetter? wsSrc = null;
            MasterCache(masterName)?.TryResolve<IWorldspaceGetter>(wsFk, out wsSrc);
            if (wsSrc?.TopCell is null) return null;
            return WorldspaceOverride(wsFk, wsSrc).TopCell;
        }

        // Get-or-add the exterior cell at grid (cx,cy) inside the worldspace override's block tree.
        private Cell? ExteriorCell(string worldspaceRef, int cx, int cy)
        {
            // In-spec custom worldspace (editorId) built earlier this run → use its generated cell.
            if (!LooksExternalRef(worldspaceRef)
                && formKeyByEd.TryGetValue(worldspaceRef, out var ownFk)
                && mod.Worldspaces.FirstOrDefault(w => w.FormKey == ownFk) is { } ownWs)
                return OwnExteriorCell(ownWs, cx, cy);

            if (!TryExternalRef(worldspaceRef, out var wsFk))
            { Warn($"  ! placement worldspace '{worldspaceRef}' must be an external <master>:0xFORMID ref or an in-spec worldspace editorId"); return null; }
            var key = (wsFk, cx, cy);
            if (exteriorCells.TryGetValue(key, out var cached)) return cached;

            var masterName = worldspaceRef[..worldspaceRef.IndexOf(':')].Trim();
            var existing = FindMasterExteriorCell(masterName, wsFk, cx, cy);

            // Resolve the master worldspace so the override can carry its land/water defaults + name.
            IWorldspaceGetter? wsSrc = null;
            MasterCache(masterName)?.TryResolve<IWorldspaceGetter>(wsFk, out wsSrc);
            var ws = WorldspaceOverride(wsFk, wsSrc);
            short bx = (short)FloorDiv(cx, 32), by = (short)FloorDiv(cy, 32);
            short sx = (short)FloorDiv(cx, 8),  sy = (short)FloorDiv(cy, 8);
            var block = ws.SubCells.FirstOrDefault(b => b.BlockNumberX == bx && b.BlockNumberY == by);
            if (block is null)
            { block = new WorldspaceBlock { BlockNumberX = bx, BlockNumberY = by, GroupType = GroupTypeEnum.ExteriorCellBlock }; ws.SubCells.Add(block); }
            var sub = block.Items.FirstOrDefault(s => s.BlockNumberX == sx && s.BlockNumberY == sy);
            if (sub is null)
            { sub = new WorldspaceSubBlock { BlockNumberX = sx, BlockNumberY = sy, GroupType = GroupTypeEnum.ExteriorCellSubBlock }; block.Items.Add(sub); }

            Cell cell;
            if (existing is not null)
            {
                // Override the master cell (same FormKey). Copy the cell's inline ENVIRONMENT data
                // (Flags, Grid, water height/type, lighting, regions, imagespace, …) via CopyCellEnv.
                // Omitting these does NOT inherit from the master — the engine defaults them, e.g.
                // WaterHeight -> 0 floods sub-sea-level terrain ("whole world underwater"). Localized
                // Name skipped; vanilla refs stay in the master, we only ADD ours.
                cell = new Cell(existing.FormKey, SkyrimRelease.SkyrimSE);
                CopyCellEnv(existing, cell);
            }
            else
            {
                // Ungenerated grid (no master cell). Make a NEW exterior cell at the grid: structurally
                // valid, but a land-less exterior cell created this way is NOT in-game verified.
                Warn($"  ! exterior grid ({cx},{cy}) has no master cell in {masterName} — creating a NEW cell (structural only, not in-game verified)");
                cell = new Cell(mod, $"MF_Ext_{(cx < 0 ? "m" : "")}{Math.Abs(cx)}_{(cy < 0 ? "m" : "")}{Math.Abs(cy)}")
                { Grid = new CellGrid { Point = new Noggog.P2Int(cx, cy) } };
                exteriorNewCells++;
            }
            sub.Items.Add(cell);
            exteriorCells[key] = cell;
            return cell;
        }
    }
}
