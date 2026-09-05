using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- navCuts: L_NAVCUT volumes that switch vanilla navmesh OFF at runtime -----------------
        // The full contract, the vanilla evidence and the four engine limits are in Spec.NavCuts.cs.
        // Here we only author the record — one PlacedObject per box:
        //     Base           = Skyrim.esm:0x000021   (CollisionMarker — the engine hardcodes this)
        //     CollisionLayer = 49                    (L_NAVCUT — the ONLY half of the gate that matters)
        //     Primitive      = Box{ Bounds = full size, Color = 255,255,0, Unknown = 0.15 }
        // byte-verified against HearthFires.esm's 1003 navcuts and Skyrim.esm's 441.
        //
        // TEMPORARY, not persistent: Skyrim.esm's own static navcuts carry no 0x400 (the HearthFires
        // ones do only because its house-building script enable-parents them). A navcut only matters
        // while its cell is loaded, and going persistent in an exterior would drag the worldspace
        // persistent TopCell into the override — the map-render landmine we already have scars from.
        private const uint NavCutCollisionLayer = 49;            // L_NAVCUT
        private const float NavCutPrimitiveUnknown = 0.15f;      // the constant on every vanilla navcut XPRM
        private static readonly FormKey CollisionMarkerBase =
            new(ModKey.FromNameAndExtension("Skyrim.esm"), 0x21);

        // A box we emitted, kept so the P1 diagnostics know which vanilla triangles are already handled
        // (and so a placement that IS cut doesn't also get warned about). Frame = the cell's frame.
        private readonly record struct NavBox(ICell Cell, Noggog.P3Float Centre, Noggog.P3Float Size, float RotZ);
        private readonly List<NavBox> navCutBoxes = new();
        private int navCutsBuilt;

        public void BuildNavCuts()
        {
            BuildExplicitNavCuts();
            BuildAutoNavCuts();
        }

        // Top-level `navCuts[]` — an explicit box, or "wrap that placement".
        private void BuildExplicitNavCuts()
        {
            for (int i = 0; i < spec.NavCuts.Count; i++)
            {
                var nc = spec.NavCuts[i];
                string who = $"navCut[{i}]" + (nc.EditorId.Length == 0 ? "" : $" '{nc.EditorId}'");
                float pad = nc.Padding ?? spec.Navmesh.Padding;

                if (!string.IsNullOrWhiteSpace(nc.Placement))
                {
                    var hit = builtPlacements.FirstOrDefault(b =>
                        nc.Placement.Equals(b.Spec.EditorId, StringComparison.OrdinalIgnoreCase));
                    if (hit.Cell is null)
                    { Warn($"  ! {who}: placement '{nc.Placement}' is not a placements[] editorId in this spec — skipped"); continue; }
                    if (!TryPlacementBox(hit.Spec, nc.Size, offset: null, pad, out var c, out var s))
                    { Warn($"  ! {who}: could not size a box for placement '{nc.Placement}' — its base has no OBND in the master link cache; give the navCut an explicit `size`"); continue; }
                    Emit(hit.Cell, c, s, hit.Spec.Rotation.Z, nc.EditorId);
                    continue;
                }

                if (nc.Position is null || nc.Size is null)
                { Warn($"  ! {who}: needs `position` (box centre) + `size`, or a `placement` to wrap — skipped"); continue; }

                var cell = NavCutCell(nc, who);
                if (cell is null) continue;
                var centre = new Noggog.P3Float(nc.Position.X, nc.Position.Y, nc.Position.Z);
                var size = Inflate(nc.Size.X, nc.Size.Y, nc.Size.Z, pad);
                Emit(cell, centre, size, nc.RotationZ, nc.EditorId);
            }
        }

        // AUTO (user ruling 2026-07-12: default-on, overridable): every placement big enough to block
        // a path AND actually sitting on vanilla navmesh gets a box. Three guards keep this honest:
        //   * `navCut: false` on the placement always wins (a fake wall stays walk-through-able);
        //   * the base's OBND must clear navmesh.minFootprint / minHeight (so clutter is never cut) —
        //     unless the author said `navCut: true`, which is the explicit override;
        //   * it must cover >= 1 live vanilla triangle. Nothing to cut = no record. That last guard is
        //     also what makes this OFFLINE-SAFE: with no Skyrim.esm the coverage is UNKNOWN, so nothing
        //     is emitted and the build is byte-identical to before.
        private void BuildAutoNavCuts()
        {
            foreach (var (pl, rec, cell) in builtPlacements)
            {
                if (rec is not PlacedObject) continue;                        // an ACHR/hazard blocks nothing
                var hint = pl.NavCut;
                if (hint is { Enabled: false }) continue;
                bool forced = hint is { Enabled: true };
                if (!forced && !spec.Navmesh.AutoNavCuts) continue;
                if (!IsVanillaHost(pl)) continue;                             // our own cells have no vanilla navmesh to cut

                float pad = hint?.Padding ?? spec.Navmesh.Padding;
                if (!TryPlacementBox(pl, hint?.Size, hint?.Offset, pad, out var centre, out var size)) continue;
                if (!forced && !IsBlocking(pl, hint?.Size)) continue;

                var tris = NavTrisAt(pl.Cell, pl.Worldspace, pl.Position, cell);
                if (tris is null || tris.Count == 0) continue;                // unknown, or genuinely nothing to cut
                var box = new NavBox(cell, centre, size, Deg2Rad(pl.Rotation.Z));
                if (!tris.Any(t => BoxCovers(box, t))) continue;

                Emit(cell, centre, size, pl.Rotation.Z,
                     string.IsNullOrWhiteSpace(pl.EditorId) ? "" : "MFNavCut_" + pl.EditorId);
            }
        }

        // A placement lands on VANILLA navmesh only when its cell/worldspace is an external ref — an
        // in-spec interior has no navmesh at all and an in-spec worldspace carries our own flat quad
        // (which we author, so there is nothing to cut).
        private static bool IsVanillaHost(PlacementSpec pl) =>
            LooksExternalRef(pl.Worldspace) || (string.IsNullOrWhiteSpace(pl.Worldspace) && LooksExternalRef(pl.Cell));

        // "Big enough to block a path": OBND footprint AND height both clear the thresholds. A house,
        // a wall or a boulder does; a chair (60×60×100 = 3600 units²) or a barrel does not.
        private bool IsBlocking(PlacementSpec pl, Vec3? sizeOverride)
        {
            if (sizeOverride is { } s)
                return s.X * s.Y >= spec.Navmesh.MinFootprint && s.Z >= spec.Navmesh.MinHeight;
            if (!TryBaseBounds(pl.Base, out var min, out var max)) return false;
            float sc = pl.Scale <= 0f ? 1f : pl.Scale;
            float w = (max.X - min.X) * sc, d = (max.Y - min.Y) * sc, h = (max.Z - min.Z) * sc;
            return w * d >= spec.Navmesh.MinFootprint && h >= spec.Navmesh.MinHeight;
        }

        // Centre + (padded) size of the box that wraps a placement. The centre comes from the base's
        // OBND box mapped through the placement's scale + Z rotation + position, so an off-origin model
        // (most of them) is wrapped where it actually stands, not where its origin is.
        private bool TryPlacementBox(PlacementSpec pl, Vec3? sizeOverride, Vec3? offset, float pad,
                                     out Noggog.P3Float centre, out Noggog.P3Float size)
        {
            centre = default; size = default;
            float sc = pl.Scale <= 0f ? 1f : pl.Scale;
            float lx = 0f, ly = 0f, lz = 0f;   // OBND centre in the base's local frame

            if (sizeOverride is { } so) size = Inflate(so.X, so.Y, so.Z, pad);
            else
            {
                if (!TryBaseBounds(pl.Base, out var min, out var max)) return false;
                size = Inflate((max.X - min.X) * sc, (max.Y - min.Y) * sc, (max.Z - min.Z) * sc, pad);
                lx = (min.X + max.X) / 2f * sc; ly = (min.Y + max.Y) / 2f * sc; lz = (min.Z + max.Z) / 2f * sc;
            }
            if (offset is { } off) { lx += off.X; ly += off.Y; lz += off.Z; }

            float rot = Deg2Rad(pl.Rotation.Z);
            float cs = MathF.Cos(rot), sn = MathF.Sin(rot);
            centre = new Noggog.P3Float(
                pl.Position.X + lx * cs - ly * sn,
                pl.Position.Y + lx * sn + ly * cs,
                pl.Position.Z + lz);
            return true;
        }

        // Padding grows the box OUTWARD on every axis: X/Y because the engine tests the actor as a
        // zero-volume POINT (leave a gap and an NPC walks through the gap), Z so the box straddles the
        // navmesh plane instead of resting exactly on it.
        private static Noggog.P3Float Inflate(float w, float d, float h, float pad)
            => new(MathF.Max(1f, w + 2f * pad), MathF.Max(1f, d + 2f * pad), MathF.Max(1f, h + 2f * pad));

        // A base form's OBND (min, max in its own frame). External bases only — ModForge never authors
        // OBND on its own statics, so an in-spec base has none and the caller falls back / warns.
        private bool TryBaseBounds(string baseRef, out Noggog.P3Int16 min, out Noggog.P3Int16 max)
        {
            min = default; max = default;
            if (!LooksExternalRef(baseRef) || !TryExternalRef(baseRef, out var fk)) return false;
            var cache = MasterCache(baseRef[..baseRef.IndexOf(':')].Trim());
            if (cache is null || !cache.TryResolve<ISkyrimMajorRecordGetter>(fk, out var rec)) return false;
            if (rec is not IObjectBoundedGetter ob) return false;
            min = ob.ObjectBounds.First; max = ob.ObjectBounds.Second;
            return max.X > min.X && max.Y > min.Y && max.Z > min.Z;
        }

        // Is the triangle's centroid inside the (Z-rotated) box? That is the cut test the diagnostics
        // and the auto gate share — same metric the plan states ("this placement covers N triangles").
        private static bool BoxCovers(in NavBox b, in NavTri t)
        {
            float px = (t.A.X + t.B.X + t.C.X) / 3f - b.Centre.X;
            float py = (t.A.Y + t.B.Y + t.C.Y) / 3f - b.Centre.Y;
            float pz = (t.A.Z + t.B.Z + t.C.Z) / 3f - b.Centre.Z;
            float cs = MathF.Cos(-b.RotZ), sn = MathF.Sin(-b.RotZ);
            float lx = px * cs - py * sn, ly = px * sn + py * cs;
            return MathF.Abs(lx) <= b.Size.X / 2f
                && MathF.Abs(ly) <= b.Size.Y / 2f
                && MathF.Abs(pz) <= b.Size.Z / 2f;
        }

        private void Emit(ICell cell, Noggog.P3Float centre, Noggog.P3Float size, float rotZdeg, string editorId)
        {
            var obj = new PlacedObject(mod);
            obj.Base.SetTo(CollisionMarkerBase);
            obj.CollisionLayer = NavCutCollisionLayer;
            // Same builder placements[].primitive uses (Generator.Build.Primitives.cs) — a navcut is
            // just an XPRM with the CollisionMarker recipe's four values pinned.
            obj.Primitive = MakePrimitive(
                PlacedPrimitive.TypeEnum.Box, size,
                System.Drawing.Color.FromArgb(0, 255, 255, 0),   // the yellow every vanilla navcut uses
                NavCutPrimitiveUnknown);
            obj.Placement = new Placement
            {
                Position = centre,
                Rotation = new Noggog.P3Float(0f, 0f, Deg2Rad(rotZdeg)),
            };
            if (!string.IsNullOrWhiteSpace(editorId) && !formKeyByEd.ContainsKey(editorId))
            {
                obj.EditorID = editorId;
                formKeyByEd[editorId] = obj.FormKey;
                recordsByEd[editorId] = obj;
                placementsByEd[editorId] = obj;
            }
            cell.Temporary.Add(obj);
            navCutBoxes.Add(new NavBox(cell, centre, size, Deg2Rad(rotZdeg)));
            navCutsBuilt++;
            placed++;
            extLinks++;   // the CollisionMarker base is an external (Skyrim.esm) link
        }

        // Where an explicit navCut's box lives — same three targeting modes as a placement.
        private ICell? NavCutCell(NavCutSpec nc, string who)
        {
            if (!string.IsNullOrWhiteSpace(nc.Worldspace))
            {
                var cell = ExteriorCell(nc.Worldspace, PosToGrid(nc.Position!.X), PosToGrid(nc.Position.Y));
                if (cell is null) Warn($"  ! {who}: worldspace '{nc.Worldspace}' unresolved — skipped");
                return cell;
            }
            if (LooksExternalRef(nc.Cell))
            {
                var cell = VanillaCellOverride(nc.Cell);
                if (cell is null) Warn($"  ! {who}: vanilla cell '{nc.Cell}' unresolved — skipped");
                return cell;
            }
            if (!string.IsNullOrWhiteSpace(nc.Cell) && cellsByEd.TryGetValue(nc.Cell, out var inSpec)) return inSpec;
            Warn($"  ! {who}: needs a `cell` or `worldspace` (a navcut volume is a placed ref — it must live somewhere) — skipped");
            return null;
        }
    }
}
