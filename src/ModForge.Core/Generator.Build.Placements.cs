namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 2: world placement — put a base form (npc/object) into a cell at position/rotation ---
        // The target cell is either an in-spec interior cell, a VANILLA interior cell we override, or
        // an exterior worldspace cell. NPC base -> PlacedNpc (ACHR), other -> PlacedObject (REFR).
        public void BuildPlacements()
        {
            // editorIds that a deferred wire (SingleRef target or Destination location) points at — these
            // placements must be persistent so the engine doesn't drop the anchor the package depends on.
            var deferredAnchorEds = new HashSet<string>(
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

                // kind:"xmarker"/"xmarkerHeading" is a thin helper: an empty base defaults to the vanilla
                // XMarker (0x3B) / XMarkerHeading (0x34) static, and the ref is forced persistent below
                // (a quest-target anchor must exist before its cell loads, else a `forced:` alias resolves
                // to a dropped temp ref). Bind one with a `forced:<editorId>` alias to use as an objective
                // target.
                bool isXMarker = pl.Kind.Equals("xmarker", StringComparison.OrdinalIgnoreCase);
                bool isXMarkerHeading = pl.Kind.Equals("xmarkerHeading", StringComparison.OrdinalIgnoreCase);
                var baseRef = pl.Base;
                if (string.IsNullOrWhiteSpace(baseRef) && isXMarker) baseRef = "Skyrim.esm:0x0000003B";
                else if (string.IsNullOrWhiteSpace(baseRef) && isXMarkerHeading) baseRef = "Skyrim.esm:0x00000034";

                if (!TryResolveRef(baseRef, formKeyByEd, out var baseFk))
                { Warn($"  ! placement: base '{baseRef}' unresolved — skipped"); continue; }

                var placement = new Placement
                {
                    Position = new Noggog.P3Float(pl.Position.X, pl.Position.Y, pl.Position.Z),
                    Rotation = new Noggog.P3Float(Deg2Rad(pl.Rotation.X), Deg2Rad(pl.Rotation.Y), Deg2Rad(pl.Rotation.Z)),
                };

                // Explicit kind wins; otherwise an in-spec NPC_ base -> npc (ACHR), anything else ->
                // object (REFR). IMPORTANT: a raw LVLN (LeveledNpc list) is placeable as NEITHER: as an
                // ACHR base it CTDs at load, and as a REFR base it's an invalid (un-placeable) form. So
                // warn whenever the base is an in-spec LVLN, regardless of kind (the no-kind case would
                // otherwise fall through to a silent, equally-broken PlacedObject). The correct pattern is
                // an NPC_ whose TEMPLATE chain references the LVLN (e.g. Skyrim.esm LvlBanditMeleeAny =
                // 0x01E79C, not LCharBanditMeleeAny = 0x03DECD).
                if (recordsByEd.TryGetValue(pl.Base, out var bk) && bk is ILeveledNpc)
                    Warn($"  ! placement '{pl.EditorId ?? pl.Base}' base is a LeveledNpc list (LVLN) — LVLN bases CTD at load; use an NPC_ actor whose template references the list (e.g. LvlBandit* not LChar*)");
                bool isNpc = pl.Kind.Equals("npc", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrEmpty(pl.Kind) && recordsByEd.TryGetValue(pl.Base, out var br) && br is INpc);
                bool isHazard = pl.Kind.Equals("hazard", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrEmpty(pl.Kind) && recordsByEd.TryGetValue(pl.Base, out var hr) && hr is IHazard);

                IPlaced placedRec;
                if (isHazard)   { var hz = new PlacedHazard(mod); hz.Hazard.SetTo(baseFk); hz.Placement = placement; placedRec = hz; }
                else if (isNpc) { var a = new PlacedNpc(mod); a.Base.SetTo(baseFk); a.Placement = placement; placedRec = a; }
                else            { var o = new PlacedObject(mod); o.Base.SetTo(baseFk); o.Placement = placement; placedRec = o; }

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

                // Scale (XSCL): omit when 1.0 (default). Actors ignore XSCL in-game but we still write it.
                if (pl.Scale != 1f)
                {
                    if (placedRec is PlacedObject scObj) scObj.Scale = pl.Scale;
                    else if (placedRec is PlacedNpc scNpc) scNpc.Scale = pl.Scale;
                }

                // InitiallyDisabled: record header flag 0x800 — ref exists but is invisible/non-collidable.
                if (pl.InitiallyDisabled) placedRec.MajorRecordFlagsRaw |= 0x800;

                // NoHavokSettle: record header flag 0x20000000 (DontHavokSettle) — the engine skips
                // the havok settle pass it would otherwise run on this ref at cell load, so a
                // deliberately-placed object stays exactly where it was authored instead of being
                // flung. REFR only (an ACHR has no settle semantics). See PlacementSpec.NoHavokSettle.
                if (pl.NoHavokSettle && placedRec is PlacedObject) placedRec.MajorRecordFlagsRaw |= 0x20000000;

                // Enable Parent (XESP): this ref's enabled state follows another ref.
                if (pl.EnableParent is { } ep)
                {
                    if (TryResolveRef(ep.Ref, formKeyByEd, out var epFk))
                    {
                        var xesp = new EnableParent();
                        xesp.Reference.SetTo(new FormLink<IPlacedGetter>(epFk));
                        xesp.Flags = ep.Flag switch
                        {
                            "SetDisable" => EnableParent.Flag.SetEnableStateToOppositeOfParent,
                            "PopIn"      => EnableParent.Flag.PopIn,
                            _            => 0,  // "SetEnable" = default (no flag)
                        };
                        if (placedRec is PlacedObject epObj) epObj.EnableParent = xesp;
                        else if (placedRec is PlacedNpc epNpc) epNpc.EnableParent = xesp;
                        linksWired++;
                        if (LooksExternalRef(ep.Ref)) extLinks++;
                    }
                    else Warn($"  ! placement '{pl.EditorId}' enableParent ref '{ep.Ref}' unresolved — skipped");
                }

                // Lock (XLOC): only PlacedObject (doors, containers); silently ignored on actors.
                if (pl.Lock is { } lk && placedRec is PlacedObject lockObj)
                {
                    var xloc = new LockData { Level = ParseLockLevel(lk.Level) };
                    if (!string.IsNullOrWhiteSpace(lk.Key))
                    {
                        if (TryResolveRef(lk.Key, formKeyByEd, out var keyFk)) xloc.Key.SetTo(keyFk);
                        else Warn($"  ! placement '{pl.EditorId}' lock key '{lk.Key}' unresolved — skipped");
                    }
                    lockObj.Lock = xloc;
                }

                // Ownership (XOWN): who owns this placed object (theft/crime).
                if (pl.Ownership is { } own)
                {
                    if (TryResolveRef(own.Owner, formKeyByEd, out var ownFk))
                    {
                        if (placedRec is PlacedObject ownObj)
                        {
                            ownObj.Owner.SetTo(ownFk);
                            if (own.Rank != 0) ownObj.FactionRank = own.Rank;
                        }
                        else if (placedRec is PlacedNpc ownNpc)
                        {
                            ownNpc.Owner.SetTo(ownFk);
                            if (own.Rank != 0) ownNpc.FactionRank = own.Rank;
                        }
                        linksWired++;
                        if (LooksExternalRef(own.Owner)) extLinks++;
                    }
                    else Warn($"  ! placement '{pl.EditorId}' ownership owner '{own.Owner}' unresolved — skipped");
                }

                // Count (XCNT): item stack count on placed object; not meaningful for actors.
                if (pl.Count > 0 && placedRec is PlacedObject cntObj) cntObj.ItemCount = pl.Count;

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

                placed++;
            }

            BuildWordWallTriggers();
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
