namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // Copy a master WORLDSPACE's inline data onto a minimal override that hosts our new cell.
        // CRITICAL: a worldspace override that omits LandDefaults resets DefaultWaterHeight to 0 —
        // and Tamriel's real default is -14000, so any terrain between -14000 and 0 gets flooded
        // ("the whole world is underwater"). We copy land/water defaults, water forms, climate, map,
        // bounds, parent, lighting, etc. but NOT the localized Name or the giant child structures
        // (SubCells block tree — we build our own; TopCell/LargeReferences/OffsetData). All copied
        // fields are inline / FormLink / sub-objects — no localized string resolution. (Skipped:
        // the AssetLink LOD/water/map TEXTURE paths — cosmetic, and getter≠setter type.)
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
            // NOTE: we intentionally do NOT override the worldspace PERSISTENT (top) cell (Tamriel 0xD74).
            // An override of it built with a fresh Cell + CopyCellEnv CRASHES the engine (EXCEPTION_ACCESS
            // _VIOLATION while queuing actors anywhere in Tamriel — in-game 2026-06-13). The correct,
            // crash-free way to additively add a single map marker to a vanilla worldspace's persistent
            // cell is still TODO (decode a known-good map-marker mod first); meanwhile map markers go in a
            // regular exterior grid cell's persistent list (shows on the map, does not corrupt loading).
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
