namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 2: Linked References between placements (the Patrol route, etc.) ---
        // Done after ALL placements exist so a marker can link forward to one defined later in the list
        // (and the last back to the first to loop). null keyword = the default link the patrol follows.
        public void WireLinkedRefs()
        {
            foreach (var pl in placements)
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
            foreach (var pl in placements)
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

        // --- pass 2: deferred Enable Parent (XESP) target refs — done after ALL placements AND ---
        // references[] labels exist, since `enableParent.ref` may name either (see BuildPlacements).
        // A resolve failure leaves NO EnableParent set at all — mirrors the old eager-resolve behaviour
        // (there is no "self" fallback for XESP, unlike a package's selfOnUnresolved target).
        public void WireDeferredEnableParents()
        {
            foreach (var (placed, ed, refStr, flag) in deferredEnableParentWires)
            {
                if (!TryResolveRef(refStr, formKeyByEd, out var epFk))
                { Warn($"  ! placement '{ed}' enableParent ref '{refStr}' unresolved — skipped"); continue; }
                var xesp = new EnableParent();
                xesp.Reference.SetTo(new FormLink<IPlacedGetter>(epFk));
                xesp.Flags = flag switch
                {
                    "SetDisable" => EnableParent.Flag.SetEnableStateToOppositeOfParent,
                    "PopIn"      => EnableParent.Flag.PopIn,
                    _            => 0,  // "SetEnable" = default (no flag)
                };
                if (placed is PlacedObject epObj) epObj.EnableParent = xesp;
                else if (placed is PlacedNpc epNpc) epNpc.EnableParent = xesp;
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
            }
        }

        // --- pass 2: deferred SingleRef target slots (every PackageRefSlots SingleRef: Patrol Start, ---
        // Follow/Escort Target, SitTarget/Activate/UseMagic Target). Emitted now that placements AND
        // references[] labels exist, as PackageTargetSpecificReference. The ref is an in-spec placement
        // (a patrol marker, a chair, an NPC to follow), a references[] label for one, or a vanilla ref.
        public void WireDeferredTargets()
        {
            foreach (var (pack, slot, slotName, ed, refStr, selfOnUnresolved) in deferredTargetWires)
            {
                // An "alias:<name>" target → PackageTargetAlias (radiant performance package whose target
                // is filled by the ownerQuest's alias). aliasLoc: is a location form — invalid as a target.
                if (TryResolveAliasIndex(refStr, ed, out var isLocAlias, out var aliasIdx))
                {
                    if (isLocAlias)
                    { Warn($"  ! package '{ed}' {slotName} '{refStr}': aliasLoc: is a location, not a target — use alias:"); continue; }
                    if (aliasIdx < 0) continue;   // already warned in TryResolveAliasIndex
                    pack.Data[slot] = new PackageDataTarget
                    {
                        Name = slotName,
                        Type = PackageDataTarget.Types.SingleRef,
                        Target = new PackageTargetAlias { Alias = aliasIdx },
                    };
                    linksWired++;
                    continue;
                }
                if (!TryResolveRef(refStr, formKeyByEd, out var tgtFk))
                {
                    // selfOnUnresolved slots already hold the PackageTargetSelf their builder wrote — the
                    // package still does something (casts on itself), so say that instead of "no-op".
                    Warn(selfOnUnresolved
                        ? $"  ! package '{ed}' {slotName} '{refStr}' unresolved — defaulting to PackageTargetSelf"
                        : $"  ! package '{ed}' {slotName} '{refStr}' unresolved — package will no-op");
                    continue;
                }
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

        // --- pass 2: deferred PackageDataLocation slots (every PackageRefSlots Location: Sandbox/Sleep/ ---
        // Eat Location, Travel Place, Escort Destination, UseMagic Location). Resolved now that placements
        // AND references[] labels exist, so a marker/placement editorId or a label resolves.
        // MakeLocationSlot handles vanilla refs, aliases, in-spec placements, and the NearSelf fallback.
        // Slots the builder RESERVED (Eat, UseMagic) are overwritten in place — Data's insertion order,
        // and so the emitted bytes, are whatever eager filling produced.
        public void WireDeferredLocations()
        {
            foreach (var (pack, slot, slotName, ed, refStr, radius) in deferredLocationWires)
                pack.Data[slot] = MakeLocationSlot(slotName, ed, refStr, radius);
        }
    

        // Build a PackageDataLocation: an authored placed-ref → LocationTarget anchored at that
        // ref, else LocationFallback(NearSelf) — anchors at the actor's current position with no
        // external dependency. NEVER use NearEditorLocation: it needs a CK-set Editor Location on
        // the NPC; Mutagen-generated NPCs don't have one, so sandbox/travel silently no-ops in-game.
        // Called ONLY from WireDeferredLocations — every location slot is deferred, because the ref may
        // be a placement editorId or a references[] label that doesn't exist during BuildPackageData.
        private PackageDataLocation MakeLocationSlot(string slotName, string packageEd, string refStr, uint radius)
        {
            // An explicit "area:<ref>" prefix (author declaring "a region, not that one object") strips to
            // the bare ref here — every location slot funnels through this one method, so this is the single
            // point that has to understand it. No-op on an unprefixed ref (byte-identical old behaviour).
            refStr = StripAreaPrefix(refStr);

            // An "alias:<name>" / "aliasLoc:<name>" location → LocationFallback bound to the ownerQuest's
            // alias index (AliasForReference = the alias holds a ref; AliasForLocation = a location alias).
            if (TryResolveAliasIndex(refStr, packageEd, out var isLocAlias, out var aliasIdx) && aliasIdx >= 0)
                return new PackageDataLocation
                {
                    Name = slotName,
                    Location = new LocationTargetRadius
                    {
                        Target = new LocationFallback
                        {
                            Type = isLocAlias
                                ? LocationTargetRadius.LocationType.AliasForLocation
                                : LocationTargetRadius.LocationType.AliasForReference,
                            Data = aliasIdx,
                        },
                        Radius = radius,
                    }
                };

            if (!string.IsNullOrWhiteSpace(refStr)
                && TryResolveRef(refStr, formKeyByEd, out var fk))
            {
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
                return new PackageDataLocation
                {
                    Name = slotName,
                    Location = new LocationTargetRadius
                    {
                        Target = new LocationTarget { Link = new FormLink<IPlacedGetter>(fk) },
                        Radius = radius,
                    }
                };
            }
            if (!string.IsNullOrWhiteSpace(refStr))
                Warn($"  ! package '{packageEd}' {slotName.ToLowerInvariant()} '{refStr}' unresolved — falling back to NearSelf");
            return new PackageDataLocation
            {
                Name = slotName,
                Location = new LocationTargetRadius
                {
                    Target = new LocationFallback { Type = LocationTargetRadius.LocationType.NearSelf },
                    Radius = radius,
                }
            };
        }
    }
}
