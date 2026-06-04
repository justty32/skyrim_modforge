namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
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
