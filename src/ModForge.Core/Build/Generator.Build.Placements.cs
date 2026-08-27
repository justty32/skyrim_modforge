namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 2: world placement — put a base form (npc/object) into a cell at position/rotation ---
        // The target cell is either an in-spec interior cell, a VANILLA interior cell we override, or
        // an exterior worldspace cell. NPC base -> PlacedNpc (ACHR), other -> PlacedObject (REFR).
        //
        // 這個方法原本是一個 204 行的單一迴圈。拆檔後這裡只留「每個 placement 依序經過哪幾關」，
        // 每一關的實作在 Generator.Build.Placements.Record.cs（建立記錄／套屬性／登記）與下方的
        // ResolvePlacementCell（找 cell）。四關都是 BuildContext 的實例方法，不需要傳 context。
        public void BuildPlacements()
        {
            var deferredAnchorEds = DeferredAnchorEditorIds();
            var teleportAnchorEds = TeleportAnchorEditorIds();
            foreach (var pl in placements)
            {
                var cell = ResolvePlacementCell(pl);
                if (cell is null) continue;

                // kind:"xmarker"/"xmarkerHeading" is a thin helper: an empty base defaults to the vanilla
                // XMarker (0x3B) / XMarkerHeading (0x34) static, and the ref is forced persistent below
                // (a quest-target anchor must exist before its cell loads, else a `forced:` alias resolves
                // to a dropped temp ref). Bind one with a `forced:<editorId>` alias to use as an objective
                // target.
                bool isXMarker = pl.Kind.Equals("xmarker", StringComparison.OrdinalIgnoreCase);
                bool isXMarkerHeading = pl.Kind.Equals("xmarkerHeading", StringComparison.OrdinalIgnoreCase);

                var placedRec = CreatePlacedRecord(pl, isXMarker, isXMarkerHeading);
                if (placedRec is null) continue;

                ApplyPlacementAttributes(pl, placedRec);
                RegisterPlacement(pl, placedRec);

                // A placement is a stable anchor that must persist across save/load if: it's an explicit
                // persistent, it's a linkedRefs source, a teleport door (or named as one's partner), or
                // another record's deferred wire points at it — a package SingleRef target (patrol start /
                // follow / escort target) or a package Destination location. The engine can drop a
                // temporary ref that something else links to.
                bool persistent = pl.Persistent
                    || isXMarker || isXMarkerHeading
                    || pl.LinkedRefs.Count > 0
                    || !string.IsNullOrWhiteSpace(pl.Teleport)
                    || (!string.IsNullOrWhiteSpace(pl.EditorId)
                        && (deferredAnchorEds.Contains(pl.EditorId) || teleportAnchorEds.Contains(pl.EditorId)));

                if (persistent)
                {
                    // A persistent xmarker/xmarkerHeading quest anchor must carry the 0x400 persistent
                    // record flag — EVERY vanilla XMarker has it (10890/10890 in Skyrim.esm), and without
                    // it a forced: alias can lose its target across save/reload. (Other persistent
                    // placement kinds are left as-is here — tolerated in grid/interior cells.)
                    if (isXMarker || isXMarkerHeading) placedRec.MajorRecordFlagsRaw |= 0x400;
                    cell.Persistent.Add(placedRec);
                }
                else cell.Temporary.Add(placedRec);

                builtPlacements.Add((pl, placedRec, cell));
                placed++;
            }

            BuildWordWallTriggers();
        }

        // editorIds that a deferred wire (SingleRef target or Destination location) points at — these
        // placements must be persistent so the engine doesn't drop the anchor the package depends on.
        private HashSet<string> DeferredAnchorEditorIds() =>
            new HashSet<string>(
                deferredTargetWires.Select(w => w.Ref)
                    .Concat(deferredLocationWires.Select(w => w.Ref))
                    .Concat(deferredForcedAliases.Select(w => w.Ref))  // a forced-alias ACHR/marker (e.g. a
                                                                       // living NPC's ref MoveTo'd around) must
                                                                       // persist or the engine drops it
                    .Concat(MerchantContainerRefs())   // the merchant chest holds gold/stock — must persist
                    // a references[] target (the in-game referrer's IN-FILE path: "sofia's chair" IS this
                    // placement) — the whole point of naming it is that something else can target it, so it
                    // must survive save/load. This is what makes the clean path clean: the object is ours,
                    // so "an alias/package can point at it" is satisfied by construction.
                    .Concat(spec.References.Select(r => r.Ref))
                    // "alias:"/"aliasLoc:" refs name a quest alias, not a placement — exclude them.
                    .Where(r => !string.IsNullOrWhiteSpace(r) && !LooksExternalRef(r) && !TryParseAliasRef(r, out _, out _)),
                StringComparer.OrdinalIgnoreCase);

        // EditorIds named as some door's teleport PARTNER — a teleport anchor must persist (the engine
        // drops a temporary door, breaking the link). Both ends of a pair end up here.
        private HashSet<string> TeleportAnchorEditorIds() =>
            new HashSet<string>(
                placements.Select(p => p.Teleport)
                    .Where(t => !string.IsNullOrWhiteSpace(t) && !LooksExternalRef(t)),
                StringComparer.OrdinalIgnoreCase);

        // The target cell of one placement: exterior grid cell, vanilla interior override, or in-spec
        // interior cell. Returns null (after warning) when the placement must be skipped.
        private ICell? ResolvePlacementCell(PlacementSpec pl)
        {
            ICell? cell;
            if (!string.IsNullOrWhiteSpace(pl.Worldspace))
            {
                // Exterior: the world position picks the grid cell in the worldspace.
                int cx = PosToGrid(pl.Position.X), cy = PosToGrid(pl.Position.Y);
                cell = ExteriorCell(pl.Worldspace, cx, cy);
                if (cell is null) { Warn($"  ! placement: worldspace '{pl.Worldspace}' unresolved — skipped"); return null; }
            }
            else if (LooksExternalRef(pl.Cell))
            {
                int before = vanillaCellOverrides.Count;
                cell = VanillaCellOverride(pl.Cell);
                if (cell is null) { Warn($"  ! placement: vanilla cell '{pl.Cell}' unresolved — skipped"); return null; }
                if (vanillaCellOverrides.Count > before) vanillaCells++;
            }
            else if (!cellsByEd.TryGetValue(pl.Cell, out var inSpec))
            { Warn($"  ! placement: cell '{pl.Cell}' not found in spec — skipped"); return null; }
            else cell = inSpec;
            return cell;
        }

        // --- placement cell resolution (shared by BuildPlacements + BuildWordWallTriggers) -------
        // Vanilla-cell override (the careful bit): we resolve the cell's *context* from a link
        // cache over its master and override it (same FormKey) into our mod, copying only the inline
        // ENVIRONMENT data via CopyCellEnv (NOT GetOrAddAsOverride, which deep-copies the localized
        // Name → needs the BSA/load-order string lookup, absent headless). The vanilla references
        // still come from the master at load time (omitting them doesn't delete them); we only ADD
        // our new ref. (master link-cache infra MasterCache + TryResolveTemplate is on the context.)
        private ICell? VanillaCellOverride(string cellRef)
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

        // Word-wall triggers: place a WordWallTrigger activator (vanilla 0x05095E unless
        // overridden) at each word wall's location. The trigger is the physical thing the player
        // walks into; its teaching quest + generated fragment (attached in AttachWordWallScripts)
        // does the learning. Reuses the SAME interior/worldspace cell resolution as a placement;
        // forced Persistent so a quest-relevant trigger isn't dropped across save/load.
        private void BuildWordWallTriggers()
        {
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
                        cell.Persistent.Add(trigger);
                        placed++;
                    }
                }
                if (LooksExternalRef(ww.Shout)) extLinks++;
                wordWallsBuilt++;
            }
        }
    }
}
