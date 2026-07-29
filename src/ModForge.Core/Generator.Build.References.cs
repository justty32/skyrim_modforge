using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        private const int PersistentFlag = 0x400;   // SkyrimMajorRecordFlag.Persistent
        private static readonly FormKey XMarkerHeadingBase =
            new(ModKey.FromNameAndExtension("Skyrim.esm"), 0x34);

        // --- references[] (Idea #24 referrer): bind a LABEL to an EXISTING placed ref ---------------
        // Runs in pass 2 AFTER BuildPlacements/BuildMapMarkers (so an in-file target exists) and BEFORE
        // every wire step that resolves refs (forced aliases, linkedRefs, package targets/locations,
        // script props) — so a label is usable in any of them. Emits NO records unless an `anchor` asks
        // for one, so a spec without references[] is byte-identical to before.
        //
        // The label is registered straight into the pass-2 ref table (formKeyByEd), which is what makes
        // "point at it from anywhere" work with zero changes to the consuming sites.
        public void BuildReferences()
        {
            // From here on every placement is built and every label below joins the ref table, so a ref
            // (a package target, an XESP parent, a CTDA param/reference…) can finally name a PLACED ref.
            // BuildCondition's build-order guard keys off this flag — see Generator.Build.Conditions.cs.
            refsIndexed = true;
            if (spec.References.Count == 0) return;
            var bound = new List<string>();   // labels that actually got registered (fuel for the area-anchor hint)
            int i = 0;
            foreach (var r in spec.References)
            {
                i++;
                var refStr = (r.Ref ?? "").Trim();
                var label = (r.Label ?? "").Trim();
                if (string.IsNullOrWhiteSpace(refStr)) { Warn($"  ! reference[{i}]: empty ref — skipped"); continue; }
                if (string.IsNullOrWhiteSpace(label))
                { Warn($"  ! reference[{i}] ('{refStr}'): empty label — nothing could point at it; skipped"); continue; }

                var anchor = (r.Anchor ?? "").Trim();
                if (anchor.Length == 0) anchor = "none";

                FormKey fk;
                IMajorRecord? rec = null;

                if (!LooksExternalRef(refStr))
                {
                    // (B) IN-FILE: the target is one of OUR placements. BuildPlacements already put it in
                    // the cell's Persistent group (it reads spec.References for exactly this reason); stamp
                    // the 0x400 record flag too, so the anchor survives save/load like a vanilla XMarker.
                    if (!placementsByEd.TryGetValue(refStr, out var placed))
                    {
                        Warn($"  ! reference '{label}': ref '{refStr}' is neither an in-spec placements[] editorId nor a <master>:0xFORMID — skipped");
                        continue;
                    }
                    placed.MajorRecordFlagsRaw |= PersistentFlag;
                    fk = placed.FormKey;
                    rec = (IMajorRecord)placed;
                    if (!anchor.Equals("none", StringComparison.OrdinalIgnoreCase))
                        Warn($"  ! reference '{label}': anchor '{r.Anchor}' ignored — an in-spec placement is already ours and is forced persistent");
                }
                else
                {
                    // (A) EXTERNAL: an existing vanilla/other-mod ref. We name it, never author it.
                    if (!TryExternalRef(refStr, out fk))
                    { Warn($"  ! reference '{label}': malformed external ref '{refStr}' (expect <master>:0xFORMID) — skipped"); continue; }

                    if (anchor.Equals("none", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ExternalRefIsPersistent(label, refStr, fk) == false)
                            Warn($"  ! reference '{label}' ('{refStr}') is a TEMPORARY ref (no 0x400 persistent flag in its master) — "
                               + "a quest alias's specific-reference fill or a package SingleRef target may not hold across save/load. "
                               + "Set anchor:\"marker\" (author a persistent XMarkerHeading at that spot) or anchor:\"replace\" "
                               + "(author our own persistent copy of the object and disable the original).");
                    }
                    else if (TryBuildAnchor(r, label, i, anchor, refStr, out var anchorRec))
                    { fk = anchorRec.FormKey; rec = (IMajorRecord)anchorRec; }
                    else
                        Warn($"  ! reference '{label}': anchor '{anchor}' not built — the label falls back to the raw ref '{refStr}'");
                }

                if (formKeyByEd.TryGetValue(label, out var clash) && clash != fk)
                    Warn($"  ! reference label '{label}' collides with an existing record's editorId — the label now wins for refs resolved after this point (run validate)");
                formKeyByEd[label] = fk;
                if (rec is not null) recordsByEd[label] = rec;
                if (rec is IPlaced ipl) placementsByEd[label] = ipl;
                bound.Add(label);
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
            }

            NoteLabelsUsedAsAreaAnchors(bound);
        }

        // --- the guardrail for the defect that looks perfect: a LABEL in a LOCATION slot -------------
        //
        // `references[]` exists to say "I care about THIS ONE OBJECT" (that is the whole primitive —
        // Spec.References.cs). A package LOCATION slot says the opposite: LocationTarget + radius = "an
        // AREA around this ref", and the engine then uses WHATEVER furniture/bed/food it finds inside
        // the radius. Put a label in one and you get a plugin that builds green, dumps clean, warns
        // about nothing — and in-game the NPC sits in a DIFFERENT chair.
        //
        // It is NOT an error: "wander near that chair" is a legitimate (and useful) intent, and only the
        // author knows which they meant. So this is an INFO note (never a warning), and it fires ONLY
        // when the ref in the slot is a references[] LABEL — a slot holding a plain vanilla cell/marker
        // FormID or an in-spec placement editorId is the ordinary area case and says nothing suspicious.
        // The SingleRef/Location split itself lives in PackageRefSlots (one table, anti-rot test).
        private void NoteLabelsUsedAsAreaAnchors(List<string> labels)
        {
            if (labels.Count == 0) return;
            var labelSet = new HashSet<string>(labels, StringComparer.Ordinal);   // formKeyByEd resolves ordinally
            foreach (var pk in spec.Packages)
                foreach (var slot in PackageRefSlots.OfKind(PackageSlotKind.Location))
                {
                    var refStr = (slot.Get(pk) ?? "").Trim();
                    // "area:<label>" = the author already declared the area intent explicitly — the whole
                    // reason the note exists is answered, so stay silent (StripAreaPrefix in the builder
                    // still resolves the label). Only an UNMARKED label in a location slot is ambiguous.
                    if (HasAreaPrefix(refStr)) continue;
                    if (!labelSet.Contains(refStr)) continue;
                    uint radius = slot.Radius?.Invoke(pk) ?? 0;
                    Note($"  i reference label '{refStr}' → package '{pk.EditorId}' {slot.Path} (radius {radius}): a LOCATION slot "
                       + $"anchors an AREA at that ref, it does not lock onto it."
                       + $"\n      The engine walks the actor to that spot and then uses ANY furniture/bed/food it likes inside the radius"
                       + $" — it may well be a DIFFERENT object than '{refStr}'."
                       + $"\n      If you meant \"be near that thing\", this is right — write \"area:{refStr}\" to say so and silence this line."
                       + $" If you meant \"use THAT object\", put the label in a SingleRef target slot instead ({PackageRefSlots.SingleRefPaths})"
                       + $" — those emit PackageTargetSpecificReference(that ref) and the engine acts on it and no other.");
                }
        }

        // Is an existing external placed ref persistent? null = couldn't tell (no link cache / not a
        // placed ref) — both cases warn, because "can't tell" must not read as "fine".
        private bool? ExternalRefIsPersistent(string label, string refStr, FormKey fk)
        {
            var cache = MasterCache(refStr[..refStr.IndexOf(':')].Trim());
            if (cache is null)
            {
                Warn($"  ! reference '{label}' ('{refStr}'): master link cache unavailable (set MODFORGE_SKYRIM_DATA) — cannot check whether the ref is persistent");
                return null;
            }
            if (!cache.TryResolve<IPlacedGetter>(fk, out var placed))
            {
                Warn($"  ! reference '{label}' ('{refStr}'): not a resolvable placed ref (REFR/ACHR) in its master — the label points at a form that may not be placeable");
                return null;
            }
            return (placed.MajorRecordFlagsRaw & PersistentFlag) != 0;
        }

        // Persistent-anchor fallback for an EXTERNAL (temporary) target. "marker" = an XMarkerHeading at
        // the spot (a PLACE to sandbox/travel to); "replace" = our own persistent copy of the object
        // (same base + transform) plus a removal of the vanilla original (so there is no duplicate).
        // base/position/rotation/scale fall back to the original record's values when the spec omits them.
        private bool TryBuildAnchor(ReferenceSpec r, string label, int i, string anchor, string refStr, out IPlaced anchorRec)
        {
            anchorRec = null!;
            bool isMarker = anchor.Equals("marker", StringComparison.OrdinalIgnoreCase);
            bool isReplace = anchor.Equals("replace", StringComparison.OrdinalIgnoreCase);
            if (!isMarker && !isReplace)
            { Warn($"  ! reference '{label}': unknown anchor '{anchor}' (none | marker | replace)"); return false; }

            // The original record (when the link cache can see it) supplies whatever the spec omitted.
            IPlacedObjectGetter? orig = null;
            if (TryExternalRef(refStr, out var origFk))
            {
                var cache = MasterCache(refStr[..refStr.IndexOf(':')].Trim());
                cache?.TryResolve<IPlacedObjectGetter>(origFk, out orig);
            }

            var pos = r.Position ?? (orig?.Placement is { } op
                ? new Vec3 { X = op.Position.X, Y = op.Position.Y, Z = op.Position.Z }
                : null);
            if (pos is null)
            { Warn($"  ! reference '{label}': anchor '{anchor}' needs a position (the spec omits it and the original ref is not resolvable)"); return false; }

            ICell? cell;
            if (!string.IsNullOrWhiteSpace(r.Worldspace))
            {
                cell = ExteriorCell(r.Worldspace, PosToGrid(pos.X), PosToGrid(pos.Y));
                if (cell is null) { Warn($"  ! reference '{label}': anchor worldspace '{r.Worldspace}' unresolved"); return false; }
            }
            else if (LooksExternalRef(r.Cell))
            {
                cell = VanillaCellOverride(r.Cell);
                if (cell is null) { Warn($"  ! reference '{label}': anchor vanilla cell '{r.Cell}' unresolved"); return false; }
                vanillaCells++;
            }
            else if (!string.IsNullOrWhiteSpace(r.Cell) && cellsByEd.TryGetValue(r.Cell, out var inSpecCell)) cell = inSpecCell;
            else
            { Warn($"  ! reference '{label}': anchor '{anchor}' needs a cell or worldspace (where the anchor is placed)"); return false; }

            FormKey baseFk;
            if (isMarker) baseFk = XMarkerHeadingBase;
            else if (!string.IsNullOrWhiteSpace(r.Base))
            {
                if (!TryResolveRef(r.Base, formKeyByEd, out baseFk))
                { Warn($"  ! reference '{label}': anchor base '{r.Base}' unresolved"); return false; }
            }
            else if (orig is not null) baseFk = orig.Base.FormKey;
            else
            { Warn($"  ! reference '{label}': anchor \"replace\" needs a `base` (the form to re-place; the original ref is not resolvable)"); return false; }

            var obj = new PlacedObject(mod);
            obj.Base.SetTo(baseFk);
            obj.Placement = new Placement
            {
                Position = new Noggog.P3Float(pos.X, pos.Y, pos.Z),
                // spec rotation is DEGREES (PlacementSpec contract); a rotation read back off the master
                // record is already RADIANS — don't convert it twice.
                Rotation = r.Rotation is { } rot
                    ? new Noggog.P3Float(Deg2Rad(rot.X), Deg2Rad(rot.Y), Deg2Rad(rot.Z))
                    : orig?.Placement?.Rotation ?? default,
            };
            if (isReplace && (r.Scale ?? orig?.Scale) is float sc && sc != 1f) obj.Scale = sc;
            obj.MajorRecordFlagsRaw |= PersistentFlag;

            var ed = "MFRef_" + SanitizeEd(label) + "_" + i;
            obj.EditorID = ed;
            formKeyByEd[ed] = obj.FormKey;
            recordsByEd[ed] = obj;
            placementsByEd[ed] = obj;
            cell.Persistent.Add(obj);
            placed++;

            // "replace": our copy stands where the vanilla one did → the vanilla one must go, or the
            // player sees two chairs. BuildRemovals (which runs after us) consumes this list.
            if (isReplace) referenceRemovals.Add(refStr);

            anchorRec = obj;
            return true;
        }
    }
}
