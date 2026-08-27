namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // Continues BuildPlacements (Generator.Build.Placements.cs): the three per-placement stages
        // that turn one PlacementSpec into a placed record. Split out verbatim; no logic changed.

        // Resolve the base form and create the right placed record (ACHR / REFR / hazard REFR).
        // Returns null (after warning) when the base ref does not resolve and the placement is skipped.
        private IPlaced? CreatePlacedRecord(PlacementSpec pl, bool isXMarker, bool isXMarkerHeading)
        {
            var baseRef = pl.Base;
            if (string.IsNullOrWhiteSpace(baseRef) && isXMarker) baseRef = "Skyrim.esm:0x0000003B";
            else if (string.IsNullOrWhiteSpace(baseRef) && isXMarkerHeading) baseRef = "Skyrim.esm:0x00000034";

            if (!TryResolveRef(baseRef, formKeyByEd, out var baseFk))
            { Warn($"  ! placement: base '{baseRef}' unresolved — skipped"); return null; }

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
            return placedRec;
        }

        // The optional per-ref data: encounter zone, scale, the two record-header flags, the deferred
        // enable parent, lock, ownership, and item count.
        private void ApplyPlacementAttributes(PlacementSpec pl, IPlaced placedRec)
        {
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

            // Enable Parent (XESP): this ref's enabled state follows another ref. DEFERRED (like a
            // package SingleRef target): `ref` may be an in-spec placement editorId defined LATER in
            // placements[] (this loop resolves top-to-bottom, so a forward pointer misses) or a
            // references[] label (BuildReferences runs entirely after this loop). A perfectly
            // reasonable spec — "this crate shows once that door opens", crate authored before the
            // door — would silently miss on an eager resolve. WireDeferredEnableParents (Generator.
            // Build.PlacementRefs.cs) fills the XESP once placements AND references[] both exist.
            if (pl.EnableParent is { } ep)
                deferredEnableParentWires.Add((placedRec, pl.EditorId ?? "", ep.Ref, ep.Flag));

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
        }

        // Named placements register so other refs (patrol start, linkedRefs target) can find them.
        private void RegisterPlacement(PlacementSpec pl, IPlaced placedRec)
        {
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
        }
    }
}
