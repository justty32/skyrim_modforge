namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // Copy a master cell's ENVIRONMENT data (water height/type/textures, lighting + template,
        // region list, imagespace, music, acoustic space, encounter zone, location, ownership,
        // sky/weather-from-region) onto an override cell. CRITICAL: an override CELL that omits
        // these does NOT inherit them from the master at runtime — the engine resets them to
        // defaults. The worst offender is WaterHeight -> 0, which floods any terrain below sea
        // level (the "whole world is underwater" bug); a missing interior LightingTemplate -> a
        // pitch-black room. We DELIBERATELY skip the localized Name (copying it needs the BSA/
        // load-order string lookup, absent headless) and the child reference lists (we ADD our ref
        // to Temporary; vanilla refs stay in the master). Every field copied here is inline or a
        // FormLink — no string resolution, so no plugins.txt dependency.
        private void CopyCellEnv(ICellGetter src, Cell dst)
        {
            dst.Flags = src.Flags;
            if (src.Grid is { } g)
                dst.Grid = new CellGrid { Point = new Noggog.P2Int(g.Point.X, g.Point.Y), Flags = g.Flags };
            dst.Lighting = src.Lighting?.DeepCopy();
            dst.WaterHeight = src.WaterHeight;
            dst.WaterNoiseTexture = src.WaterNoiseTexture;
            dst.WaterEnvironmentMap = src.WaterEnvironmentMap;
            dst.LightingTemplate.SetTo(src.LightingTemplate);
            dst.Water.SetTo(src.Water);
            dst.Location.SetTo(src.Location);
            dst.Owner.SetTo(src.Owner);
            dst.SkyAndWeatherFromRegion.SetTo(src.SkyAndWeatherFromRegion);
            dst.AcousticSpace.SetTo(src.AcousticSpace);
            dst.EncounterZone.SetTo(src.EncounterZone);
            dst.Music.SetTo(src.Music);
            dst.ImageSpace.SetTo(src.ImageSpace);
            if (src.Regions is { } regions)
            {
                dst.Regions = new Noggog.ExtendedList<IFormLinkGetter<IRegionGetter>>();
                foreach (var rg in regions) dst.Regions.Add(rg);
            }
        }

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

        // Interior cells nest CellBlock(type 2, label=block) -> CellSubBlock(type 3, label=sub) ->
        // Cell, and Skyrim groups them BY FORMID: block = id % 10, sub = (id / 10) % 10 (decimal,
        // 24-bit ID — verified by walking Skyrim.esm, e.g. WhiterunBanneredMare 0x01605E/dec 90206
        // is in block 6 / sub 0). This is CRITICAL for OVERRIDES: a vanilla-cell override placed in
        // the wrong block GRUP is never matched against the master cell, so the engine SILENTLY
        // IGNORES it (the It.10 bug — placed objects + lighting didn't apply; we'd hardcoded 0/0).
        // get-or-add the correct (block, sub) GRUP for any cell's FormID.
        private CellSubBlock InteriorSubFor(FormKey fk)
        {
            int id = (int)fk.ID;
            int blk = id % 10, sub = (id / 10) % 10;
            if (interiorSubs.TryGetValue((blk, sub), out var cached)) return cached;
            var block = mod.Cells.Records.FirstOrDefault(
                b => b.BlockNumber == blk && b.GroupType == GroupTypeEnum.InteriorCellBlock);
            if (block is null)
            {
                block = new CellBlock { BlockNumber = blk, GroupType = GroupTypeEnum.InteriorCellBlock };
                mod.Cells.Records.Add(block);
            }
            var subBlock = new CellSubBlock { BlockNumber = sub, GroupType = GroupTypeEnum.InteriorCellSubBlock };
            block.SubBlocks.Add(subBlock);
            interiorSubs[(blk, sub)] = subBlock;
            return subBlock;
        }

        // --- pass 1: interior (new) cells ---
        public void BuildCells()
        {
            foreach (var c in spec.Cells)
            {
                var cell = new Cell(mod, c.EditorId) { Flags = Cell.Flag.IsInteriorCell };
                // A cell with no Lighting/LightingTemplate renders PITCH BLACK in-game. Optionally copy a
                // vanilla interior cell's lighting/water ENV (same `template` pattern as It.8 item models):
                // point `template` at a known-good vanilla interior (e.g. a player home). Floor still comes
                // from a placed static — without one the player falls into the void.
                if (!string.IsNullOrWhiteSpace(c.Template))
                {
                    if (TryResolveTemplate<ICellGetter>(c.Template, out var tmplCell) && tmplCell is not null)
                    {
                        if (tmplCell.Flags.HasFlag(Cell.Flag.IsInteriorCell)) CopyCellEnv(tmplCell, cell);
                        else Warn($"  ! cell '{c.EditorId}' template '{c.Template}' is exterior — ignored (need an interior cell)");
                    }
                    else Warn($"  ! cell '{c.EditorId}' template '{c.Template}' unresolved — created without lighting (may render black)");
                }
                cell.Flags |= Cell.Flag.IsInteriorCell;   // CopyCellEnv overwrote Flags — keep it interior
                if (!string.IsNullOrEmpty(c.Name)) cell.Name = c.Name;
                InteriorSubFor(cell.FormKey).Cells.Add(cell);
                if (!string.IsNullOrEmpty(c.EditorId)) cellsByEd[c.EditorId] = cell;
            }
        }

        // --- pass 2: world placement — put a base form (npc/object) into a cell at position/rotation ---
        // The target cell is either an in-spec interior cell, a VANILLA interior cell we override, or
        // an exterior worldspace cell. NPC base -> PlacedNpc (ACHR), other -> PlacedObject (REFR).
        public void BuildPlacements()
        {
            // Vanilla-cell override (the careful bit): we resolve the cell's *context* from a link
            // cache over its master and override it (same FormKey) into our mod, copying only the inline
            // ENVIRONMENT data via CopyCellEnv (NOT GetOrAddAsOverride, which deep-copies the localized
            // Name → needs the BSA/load-order string lookup, absent headless). The vanilla references
            // still come from the master at load time (omitting them doesn't delete them); we only ADD
            // our new ref. (master link-cache infra MasterCache + TryResolveTemplate is on the context.)
            var vanillaCellOverrides = new Dictionary<FormKey, ICell>();

            ICell? VanillaCellOverride(string cellRef)
            {
                if (!TryExternalRef(cellRef, out var fk)) return null;
                if (vanillaCellOverrides.TryGetValue(fk, out var existing)) return existing;
                var masterName = cellRef[..cellRef.IndexOf(':')].Trim();
                var cache = MasterCache(masterName);
                if (cache is null) return null;
                if (!cache.TryResolve<ICellGetter>(fk, out var vanilla))
                { Warn($"  ! vanilla cell '{cellRef}' not found in {masterName}"); return null; }
                if (!vanilla.Flags.HasFlag(Cell.Flag.IsInteriorCell))
                { Warn($"  ! vanilla cell '{cellRef}' is exterior — only interior vanilla cells supported (phase 2); skipped"); return null; }

                var ov = new Cell(fk, SkyrimRelease.SkyrimSE);
                CopyCellEnv(vanilla, ov);
                InteriorSubFor(fk).Cells.Add(ov);
                vanillaCellOverrides[fk] = ov;
                return ov;
            }

            // --- Exterior / worldspace placement (It.7d phase 3) ---------------------------------
            // An exterior cell lives inside a WRLD, nested WorldspaceBlock(type 4, /32 grid) ->
            // WorldspaceSubBlock(type 5, /8 grid) -> Cell(grid x,y). To add a ref to the world we
            // OVERRIDE the existing master cell at the target grid (same careful, Flags+Grid-only
            // override as the interior vanilla case — no localized deep-copy). We host it on a minimal
            // Worldspace override that re-states only OUR block tree (vanilla cells stay in the master).
            var worldspaceOverrides = new Dictionary<FormKey, Worldspace>();
            var exteriorCells = new Dictionary<(FormKey Ws, int X, int Y), Cell>();

            Worldspace WorldspaceOverride(FormKey wsFk, IWorldspaceGetter? src)
            {
                if (worldspaceOverrides.TryGetValue(wsFk, out var ex)) return ex;
                var ws = new Worldspace(wsFk, SkyrimRelease.SkyrimSE); // override that hosts our block tree
                if (src is not null) CopyWorldspaceEnv(src, ws);       // carry land/water defaults etc.
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
            ICellGetter? FindMasterExteriorCell(string masterName, FormKey wsFk, int cx, int cy)
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

            // Get-or-add the exterior cell at grid (cx,cy) inside the worldspace override's block tree.
            Cell? ExteriorCell(string worldspaceRef, int cx, int cy)
            {
                if (!TryExternalRef(worldspaceRef, out var wsFk))
                { Warn($"  ! placement worldspace '{worldspaceRef}' must be an external <master>:0xFORMID ref"); return null; }
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

            // editorIds that a deferred wire (SingleRef target or Destination location) points at — these
            // placements must be persistent so the engine doesn't drop the anchor the package depends on.
            var deferredAnchorEds = new HashSet<string>(
                deferredTargetWires.Select(w => w.Ref)
                    .Concat(deferredLocationWires.Select(w => w.Ref))
                    .Concat(MerchantContainerRefs())   // the merchant chest holds gold/stock — must persist
                    .Where(r => !string.IsNullOrWhiteSpace(r) && !LooksExternalRef(r)),
                StringComparer.OrdinalIgnoreCase);
            // EditorIds named as some door's teleport PARTNER — a teleport anchor must persist (the engine
            // drops a temporary door, breaking the link). Both ends of a pair end up here.
            var teleportAnchorEds = new HashSet<string>(
                spec.Placements.Select(p => p.Teleport)
                    .Where(t => !string.IsNullOrWhiteSpace(t) && !LooksExternalRef(t)),
                StringComparer.OrdinalIgnoreCase);
            foreach (var pl in spec.Placements)
            {
                ICell? cell;
                if (!string.IsNullOrWhiteSpace(pl.Worldspace))
                {
                    // Exterior: the world position picks the grid cell in the worldspace.
                    int cx = PosToGrid(pl.Position.X), cy = PosToGrid(pl.Position.Y);
                    cell = ExteriorCell(pl.Worldspace, cx, cy);
                    if (cell is null) { Warn($"  ! placement: worldspace '{pl.Worldspace}' unresolved — skipped"); continue; }
                }
                else if (LooksExternalRef(pl.Cell))
                {
                    int before = vanillaCellOverrides.Count;
                    cell = VanillaCellOverride(pl.Cell);
                    if (cell is null) { Warn($"  ! placement: vanilla cell '{pl.Cell}' unresolved — skipped"); continue; }
                    if (vanillaCellOverrides.Count > before) vanillaCells++;
                }
                else if (!cellsByEd.TryGetValue(pl.Cell, out var inSpec))
                { Warn($"  ! placement: cell '{pl.Cell}' not found in spec — skipped"); continue; }
                else cell = inSpec;

                if (!TryResolveRef(pl.Base, formKeyByEd, out var baseFk))
                { Warn($"  ! placement: base '{pl.Base}' unresolved — skipped"); continue; }

                var placement = new Placement
                {
                    Position = new Noggog.P3Float(pl.Position.X, pl.Position.Y, pl.Position.Z),
                    Rotation = new Noggog.P3Float(Deg2Rad(pl.Rotation.X), Deg2Rad(pl.Rotation.Y), Deg2Rad(pl.Rotation.Z)),
                };

                // Explicit kind wins; otherwise an in-spec NPC *or LeveledNpc* base -> npc (ACHR),
                // anything else -> object (REFR). A LeveledNpc base makes the ACHR a LEVELED SPAWN: the
                // engine rolls a level-appropriate actor from that list at load. (For a vanilla base we
                // can't see the record type headlessly, so an external LVLN spawn needs explicit
                // kind:"npc" — but in practice the spawn list is usually in-spec or the author sets kind.)
                bool isNpc = pl.Kind.Equals("npc", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrEmpty(pl.Kind) && recordsByEd.TryGetValue(pl.Base, out var br) && (br is INpc || br is ILeveledNpc));

                IPlaced placedRec;
                if (isNpc) { var a = new PlacedNpc(mod); a.Base.SetTo(baseFk); a.Placement = placement; placedRec = a; }
                else       { var o = new PlacedObject(mod); o.Base.SetTo(baseFk); o.Placement = placement; placedRec = o; }

                // Per-ref encounter zone (XEZN) — scopes THIS spawn to its own zone (else it inherits the
                // cell's). EncounterZone lives on both ACHR and REFR (no shared settable interface), so set
                // the concrete one. Usually only meaningful on a leveled-actor spawn.
                if (!string.IsNullOrWhiteSpace(pl.EncounterZone) && TryResolveRef(pl.EncounterZone, formKeyByEd, out var ezFk))
                {
                    if (placedRec is PlacedNpc pnRec) pnRec.EncounterZone.SetTo(ezFk);
                    else if (placedRec is PlacedObject poRec) poRec.EncounterZone.SetTo(ezFk);
                    linksWired++;
                    if (LooksExternalRef(pl.EncounterZone)) extLinks++;
                }
                else if (!string.IsNullOrWhiteSpace(pl.EncounterZone))
                    Warn($"  ! placement encounterZone '{pl.EncounterZone}' unresolved — skipped");

                // Named placements register so other refs (patrol start, linkedRefs target) can find
                // them. A placement that's a linkedRefs *target* must persist across save/load to be a
                // stable anchor, so we force it Persistent (markers are cheap; this avoids the engine
                // dropping a temporary ref another ref points at).
                if (!string.IsNullOrWhiteSpace(pl.EditorId))
                {
                    // A placement editorId that collides with an already-registered record would
                    // silently clobber that record's FormKey here, breaking any ref to the original.
                    // validate enforces uniqueness, but Build can run without it — so warn.
                    if (formKeyByEd.ContainsKey(pl.EditorId))
                        Warn($"  ! placement editorId '{pl.EditorId}' collides with an existing record — its FormKey is now overwritten (run validate to catch this)");
                    placedRec.EditorID = pl.EditorId;
                    formKeyByEd[pl.EditorId] = placedRec.FormKey;
                    recordsByEd[pl.EditorId] = (IMajorRecord)placedRec;
                    placementsByEd[pl.EditorId] = placedRec;
                    placementSpecByEd[pl.EditorId] = pl;
                }

                // A placement is a stable anchor that must persist across save/load if: it's an explicit
                // persistent, it's a linkedRefs source, a teleport door (or named as one's partner), or
                // another record's deferred wire points at it — a package SingleRef target (patrol start /
                // follow / escort target) or a package Destination location. The engine can drop a
                // temporary ref that something else links to.
                bool linkTarget = pl.LinkedRefs.Count > 0
                    || !string.IsNullOrWhiteSpace(pl.Teleport)
                    || (!string.IsNullOrWhiteSpace(pl.EditorId)
                        && (deferredAnchorEds.Contains(pl.EditorId) || teleportAnchorEds.Contains(pl.EditorId)));
                (pl.Persistent || linkTarget ? cell.Persistent : cell.Temporary).Add(placedRec);
                placed++;
            }

            // Word-wall triggers: place a WordWallTrigger activator (vanilla 0x05095E unless
            // overridden) at each word wall's location. The trigger is the physical thing the player
            // walks into; its teaching quest + generated fragment (attached in AttachWordWallScripts)
            // does the learning. Reuses the SAME interior/worldspace cell resolution as a placement;
            // forced Persistent so a quest-relevant trigger isn't dropped across save/load.
            foreach (var ww in spec.WordWalls)
            {
                ICell? cell;
                if (!string.IsNullOrWhiteSpace(ww.Worldspace))
                {
                    int cx = PosToGrid(ww.Position.X), cy = PosToGrid(ww.Position.Y);
                    cell = ExteriorCell(ww.Worldspace, cx, cy);
                    if (cell is null) Warn($"  ! wordWall '{ww.EditorId}': worldspace '{ww.Worldspace}' unresolved — trigger skipped");
                }
                else if (LooksExternalRef(ww.Cell)) cell = VanillaCellOverride(ww.Cell);
                else if (cellsByEd.TryGetValue(ww.Cell, out var inSpec)) cell = inSpec;
                else { Warn($"  ! wordWall '{ww.EditorId}': cell '{ww.Cell}' not found — trigger skipped"); cell = null; }

                if (cell is not null)
                {
                    var triggerBase = string.IsNullOrWhiteSpace(ww.TriggerBase) ? VanillaWordWallTrigger : ww.TriggerBase;
                    if (!TryResolveRef(triggerBase, formKeyByEd, out var baseFk))
                        Warn($"  ! wordWall '{ww.EditorId}': trigger base '{triggerBase}' unresolved — trigger skipped");
                    else
                    {
                        var trigger = new PlacedObject(mod);
                        trigger.Base.SetTo(baseFk);
                        trigger.Placement = new Placement
                        {
                            Position = new Noggog.P3Float(ww.Position.X, ww.Position.Y, ww.Position.Z),
                            Rotation = new Noggog.P3Float(Deg2Rad(ww.Rotation.X), Deg2Rad(ww.Rotation.Y), Deg2Rad(ww.Rotation.Z)),
                        };
                        var triggerEd = string.IsNullOrWhiteSpace(ww.TriggerEditorId) ? ww.EditorId + "Trigger" : ww.TriggerEditorId;
                        if (!formKeyByEd.ContainsKey(triggerEd))
                        {
                            trigger.EditorID = triggerEd;
                            formKeyByEd[triggerEd] = trigger.FormKey;
                            recordsByEd[triggerEd] = trigger;
                        }
                        cell.Persistent.Add(trigger);   // a quest-relevant trigger must persist across save/load
                        placed++;
                    }
                }
                if (LooksExternalRef(ww.Shout)) extLinks++;
                wordWallsBuilt++;
            }
        }

        // --- pass 2: Linked References between placements (the Patrol route, etc.) ---
        // Done after ALL placements exist so a marker can link forward to one defined later in the list
        // (and the last back to the first to loop). null keyword = the default link the patrol follows.
        public void WireLinkedRefs()
        {
            foreach (var pl in spec.Placements)
            {
                if (pl.LinkedRefs.Count == 0 || string.IsNullOrWhiteSpace(pl.EditorId)) continue;
                if (!placementsByEd.TryGetValue(pl.EditorId, out var src)) continue;
                // LinkedReferences (XLKR) lives on REFR (IPlacedObject) and ACHR (IPlacedNpc) separately
                // — no shared settable interface — so pick the concrete list.
                var list = (src as IPlacedObject)?.LinkedReferences ?? (src as IPlacedNpc)?.LinkedReferences;
                if (list is null) continue;
                foreach (var lr in pl.LinkedRefs)
                {
                    if (!TryResolveRef(lr.Target, formKeyByEd, out var tgtFk))
                    { Warn($"  ! placement '{pl.EditorId}' linkedRef target '{lr.Target}' unresolved — skipped"); continue; }
                    var link = new LinkedReferences();
                    link.Reference.SetTo(new FormLink<IPlacedGetter>(tgtFk));
                    if (!string.IsNullOrWhiteSpace(lr.Keyword) && TryResolveRef(lr.Keyword, formKeyByEd, out var kwFk))
                        link.KeywordOrReference.SetTo(new FormLink<IKeywordLinkedReferenceGetter>(kwFk));
                    list.Add(link);
                    linksWired++;
                    if (LooksExternalRef(lr.Target)) extLinks++;
                }
            }
        }

        // --- pass 2: load-door TELEPORTS (XTEL) — done after all placements exist so a door can point ---
        // at a partner defined later in the list. A load door is a PlacedObject (REFR) over a DOOR base
        // whose TeleportDestination = { partner door FormKey, partner position, partner rotation } — the
        // player walks through this door and materialises AT THE PARTNER. So the XTEL position/rotation is
        // the PARTNER's, not this door's (mirrors every vanilla load-door pair). The partner is an in-spec
        // door placement (its position read from the spec) or a vanilla door ref (its position read from
        // the master). Author both doors of a pair, each `teleport`-ing at the other.
        public void WireTeleportDoors()
        {
            bool PartnerArrival(string partnerRef, out FormKey doorFk, out Noggog.P3Float pos, out Noggog.P3Float rot)
            {
                doorFk = default; pos = default; rot = default;
                if (!LooksExternalRef(partnerRef))
                {
                    if (!placementsByEd.TryGetValue(partnerRef, out var partner)
                        || !placementSpecByEd.TryGetValue(partnerRef, out var ps)) return false;
                    doorFk = partner.FormKey;
                    pos = new Noggog.P3Float(ps.Position.X, ps.Position.Y, ps.Position.Z);
                    rot = new Noggog.P3Float(Deg2Rad(ps.Rotation.X), Deg2Rad(ps.Rotation.Y), Deg2Rad(ps.Rotation.Z));
                    return true;
                }
                if (!TryExternalRef(partnerRef, out doorFk)) return false;
                // Vanilla partner door: pull its world/cell-local position+rotation from the master so the
                // player arrives where the vanilla door actually is (already-radians rotation in the master).
                var cache = MasterCache(partnerRef[..partnerRef.IndexOf(':')].Trim());
                if (cache is not null && cache.TryResolve<IPlacedObjectGetter>(doorFk, out var vd) && vd.Placement is { } vp)
                { pos = vp.Position; rot = vp.Rotation; }
                else Warn($"  ! teleport partner '{partnerRef}' position not resolvable from master — arrival point defaults to (0,0,0)");
                return true;
            }
            foreach (var pl in spec.Placements)
            {
                if (string.IsNullOrWhiteSpace(pl.Teleport) || string.IsNullOrWhiteSpace(pl.EditorId)) continue;
                if (!placementsByEd.TryGetValue(pl.EditorId, out var src)) continue;
                if (src is not IPlacedObject door)
                { Warn($"  ! placement '{pl.EditorId}' has teleport but is not an object (door) ref — skipped"); continue; }
                if (!PartnerArrival(pl.Teleport, out var partnerFk, out var pos, out var rot))
                { Warn($"  ! placement '{pl.EditorId}' teleport partner '{pl.Teleport}' unresolved — skipped"); continue; }
                var xtel = new TeleportDestination { Position = pos, Rotation = rot };
                xtel.Door.SetTo(new FormLink<IPlacedObjectGetter>(partnerFk));
                door.TeleportDestination = xtel;
                linksWired++;
                if (LooksExternalRef(pl.Teleport)) extLinks++;
            }
        }

        // --- pass 2: deferred SingleRef slot-0 targets (Patrol "Patrol Start", Follow "Target to Follow") ---
        // Emitted now that placements exist, as PackageTargetSpecificReference. The ref is an in-spec
        // placement (e.g. a patrol marker, or an NPC to follow) or a vanilla ref (e.g. the player).
        public void WireDeferredTargets()
        {
            foreach (var (pack, slot, slotName, ed, refStr) in deferredTargetWires)
            {
                if (!TryResolveRef(refStr, formKeyByEd, out var tgtFk))
                { Warn($"  ! package '{ed}' {slotName} '{refStr}' unresolved — package will no-op"); continue; }
                pack.Data[slot] = new PackageDataTarget
                {
                    Name = slotName,
                    Type = PackageDataTarget.Types.SingleRef,
                    Target = new PackageTargetSpecificReference { Reference = new FormLink<IPlacedGetter>(tgtFk) },
                };
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
            }
        }

        // --- pass 2: deferred PackageDataLocation slots (Escort "Destination", Travel "Place to Travel") ---
        // Resolved now that placements exist, so an in-spec marker/placement editorId resolves.
        // MakeLocationSlot handles vanilla refs, in-spec placements, and the NearSelf fallback.
        public void WireDeferredLocations()
        {
            foreach (var (pack, slot, slotName, ed, refStr, radius) in deferredLocationWires)
                pack.Data[slot] = MakeLocationSlot(slotName, $"package '{ed}' {slotName.ToLowerInvariant()}", refStr, radius);
        }
    }
}
