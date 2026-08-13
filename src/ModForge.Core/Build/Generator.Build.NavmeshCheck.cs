using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // -------------------------------------------------------------------------------
        //  P1 navmesh diagnostics — turn the two SILENT, in-game-only failures into a build warning.
        //
        //   ② "my NPC just stands there"      → its ACHR is not on any navmesh triangle. Sandbox,
        //      travel, follow and combat ALL path through the navmesh; with no triangle under its
        //      feet an actor does literally nothing, and the game says nothing either.
        //   ① "NPCs walk through my house"    → the placement sits on live vanilla navmesh that
        //      nothing cut, so the engine still thinks that ground is walkable.
        //   ③ "NPCs walk on air"              → a removal/override took away a walkable structure but
        //      the navmesh it carried stays where it was.
        //
        //  Warnings only — this step NEVER authors a record. Silence it with `"navmesh": {"warnings": false}`.
        //
        //  ⚠️ OFFLINE: every geometry lookup goes through NavTrisAt, which returns null ("unknown")
        //  without the master link cache. Unknown = say nothing. The one check that still fires with
        //  no Skyrim.esm is the in-spec interior cell (we know for a fact we never authored navmesh
        //  there) — which is the most valuable one anyway.
        // -------------------------------------------------------------------------------

        // How far above/below a triangle an actor may stand and still be "on" it. Actors snap down to
        // the floor at load; a marker authored a bit high is fine, one authored a storey up is not.
        private const float NavZAbove = 200f;
        private const float NavZBelow = 400f;

        public void CheckNavmesh()
        {
            if (!spec.Navmesh.Warnings) return;
            var cellsWarned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (pl, rec, cell) in builtPlacements)
            {
                if (!pl.NavmeshCheck) continue;                         // deliberately off-stage (see PlacementSpec)
                if (rec is PlacedNpc) CheckActorOnNavmesh(pl, cell, cellsWarned);
                else if (rec is PlacedObject) CheckObstacleOnNavmesh(pl, cell);
            }
            CheckRemovedStructures();
        }

        // ② An ACHR with no navmesh under it will never move. This is the highest-value warning in the
        // whole plan: today the symptom is a mute NPC and no diagnostic anywhere.
        private void CheckActorOnNavmesh(PlacementSpec pl, ICell cell, HashSet<string> cellsWarned)
        {
            var tris = NavTrisAt(pl.Cell, pl.Worldspace, pl.Position, cell);
            if (tris is null) return;                                   // unknown (offline) → say nothing
            string who = string.IsNullOrWhiteSpace(pl.EditorId) ? $"base '{pl.Base}'" : $"'{pl.EditorId}'";

            if (tris.Count == 0)
            {
                // A cell with no navmesh at all. For an IN-SPEC cell that is not a mistake, it is the
                // state of the world — ModForge cannot author interior navmesh (P3), so this would fire
                // on every custom interior ever built. Opt in with navmesh.warnEmptyCells if you want the
                // reminder; a VANILLA cell with no navmesh is unusual enough to always be worth saying.
                bool inSpec = ReferenceEquals(tris, NoTris);
                if (inSpec && !spec.Navmesh.WarnEmptyCells) return;

                var key = string.IsNullOrWhiteSpace(pl.Worldspace) ? pl.Cell : pl.Worldspace + "/ws";
                if (!cellsWarned.Add(key)) return;
                Warn($"  ! navmesh: cell '{key}' has NO navmesh at all — every NPC placed in it (incl. {who}) "
                   + "will stand still forever (sandbox/travel/follow/combat all path through the navmesh). "
                   + "ModForge does not author interior navmesh yet; place the NPC in a vanilla cell, or give a "
                   + "custom worldspace cell `navmesh: true`.");
                return;
            }

            if (!NearestTri(pl.Position.X, pl.Position.Y, tris, out var best, out float dist)) return;
            if (dist == 0f)
            {
                float floor = TriZAt(pl.Position.X, pl.Position.Y, best);
                float dz = pl.Position.Z - floor;
                if (dz <= NavZAbove && dz >= -NavZBelow) return;        // standing on it — fine
                Warn($"  ! navmesh: NPC {who} is {MathF.Abs(dz):F0} units {(dz > 0 ? "ABOVE" : "BELOW")} the navmesh "
                   + $"under it (floor z={floor:F0}, placed z={pl.Position.Z:F0}) — it will not move. Put it on the walkable "
                   + "surface (`refpos <plugin> <0xFORMID>` copies a vanilla ref's proven-walkable coords).");
                return;
            }
            Warn($"  ! navmesh: NPC {who} is off the navmesh — the nearest walkable triangle is {dist:F0} units away. "
               + "It will NOT move (sandbox/travel/follow/combat all need a triangle under its feet). Move it onto "
               + "walkable ground, or give the spot navmesh.");
        }

        // ① A big enough placement on live vanilla navmesh, with nothing cutting that navmesh, is the
        // "NPCs walk straight through my new house" bug. If a navCut box already covers those triangles
        // (auto or explicit) we say nothing — the case is handled.
        private void CheckObstacleOnNavmesh(PlacementSpec pl, ICell cell)
        {
            if (pl.NavCut is { Enabled: true }) return;                 // author asked for a cut; it was emitted (or warned)
            if (!IsVanillaHost(pl)) return;
            if (!IsBlocking(pl, pl.NavCut?.Size)) return;

            var tris = NavTrisAt(pl.Cell, pl.Worldspace, pl.Position, cell);
            if (tris is null || tris.Count == 0) return;

            if (!TryPlacementBox(pl, pl.NavCut?.Size, pl.NavCut?.Offset, 0f, out var centre, out var size)) return;
            var probe = new NavBox(cell, centre, size, Deg2Rad(pl.Rotation.Z));
            int covered = tris.Count(t => BoxCovers(probe, t));
            if (covered == 0) return;

            var mine = navCutBoxes.Where(b => ReferenceEquals(b.Cell, cell)).ToList();
            int cut = tris.Count(t => BoxCovers(probe, t) && mine.Any(b => BoxCovers(b, t)));
            if (cut >= covered) return;                                 // fully handled by a navcut

            string who = string.IsNullOrWhiteSpace(pl.EditorId) ? $"base '{pl.Base}'" : $"'{pl.EditorId}'";
            if (pl.NavCut is { Enabled: false })
                Warn($"  ! navmesh: placement {who} sits on {covered} live vanilla navmesh triangle(s) and has "
                   + "`navCut: false` — NPCs will path straight into it. That is fine if it is scenery they are "
                   + "meant to walk through; otherwise drop the `navCut: false`.");
            else
                Warn($"  ! navmesh: placement {who} covers {covered} vanilla navmesh triangle(s) but nothing cuts them "
                   + $"({cut} cut) — NPCs will walk into it. Add a `navCuts[]` box over it, or set `navCut: true` on the "
                   + "placement (`navmesh.autoNavCuts` is off, or its OBND is under navmesh.minFootprint/minHeight).");
        }

        // ③ Erasing/moving a WALKABLE structure (stairs, a bridge, a platform) leaves its navmesh
        // hanging in mid-air — NPCs then walk on nothing. We cannot tell a walkable structure from a
        // decorative one, so this only fires when the thing is (a) big enough to have been walkable and
        // (b) actually has navmesh sitting ON TOP of it, which decorative clutter never does.
        private void CheckRemovedStructures()
        {
            foreach (var refStr in spec.Removals.Select(r => r.Ref).Concat(spec.Overrides.Select(o => o.Ref)).Distinct())
            {
                if (string.IsNullOrWhiteSpace(refStr) || !TryExternalRef(refStr, out var fk)) continue;
                var cache = MasterCache(refStr[..refStr.IndexOf(':')].Trim());
                if (cache is null) continue;
                if (!cache.TryResolveContext<IPlacedObject, IPlacedObjectGetter>(fk, out var ctx)) continue;
                var orig = ctx.Record;
                if (orig.Placement is not { } p) continue;
                if (ctx.Parent?.Record is not ICellGetter parentCell) continue;
                if (!TryBaseBounds(FormKeyRef(orig.Base.FormKey), out var min, out var max)) continue;

                float sc = orig.Scale ?? 1f;
                if ((max.X - min.X) * sc * ((max.Y - min.Y) * sc) < spec.Navmesh.MinFootprint) continue;

                var tris = TrisOfCellGetter(parentCell);
                if (tris.Count == 0) continue;
                // navmesh right at the object's TOP surface, over its footprint = it was walked ON.
                float top = p.Position.Z + max.Z * sc;
                bool walkedOn = tris.Any(t =>
                    InTri2D(p.Position.X, p.Position.Y, t)
                    && MathF.Abs(TriZAt(p.Position.X, p.Position.Y, t) - top) < 128f);
                if (!walkedOn) continue;

                Warn($"  ! navmesh: '{refStr}' is removed/moved and carries vanilla navmesh on its TOP surface — if it "
                   + "was a WALKABLE structure (stairs / bridge / platform), that navmesh stays where it was and NPCs "
                   + "will walk on thin air above the hole. (Purely decorative? Ignore this.)");
            }
        }

        private static List<NavTri> TrisOfCellGetter(ICellGetter cell)
        { var tris = new List<NavTri>(); AddTris(cell, tris); return tris; }

        private static string FormKeyRef(Mutagen.Bethesda.Plugins.FormKey fk)
            => $"{fk.ModKey.FileName}:0x{fk.ID:X6}";
    }
}
