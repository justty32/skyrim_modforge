using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // -------------------------------------------------------------------------------
        //  Navmesh index — read the navmesh triangles that already cover a spot, so build can
        //  answer the two questions nobody could answer offline before:
        //      "is there navmesh under this NPC?"          (else it will never move)
        //      "does this building sit on vanilla navmesh?" (else NPCs walk into it)
        //
        //  Coordinates line up for free: an interior NAVM's vertices are CELL-LOCAL and an exterior
        //  NAVM's are WORLD — exactly the two frames PlacementSpec.Position already uses. No
        //  conversion anywhere in this file.
        //
        //  ⚠️ OFFLINE: everything here goes through the master link cache. Without Skyrim.esm every
        //  lookup returns null = "unknown", and every caller must treat unknown as "say nothing" —
        //  never as "no navmesh". That is what keeps the offline machine buildable (CLAUDE.md ①).
        //  MasterCache() is only ever reached here for a cell/worldspace the placement pass ALREADY
        //  resolved through it, so this adds no new "master not found" warning of its own.
        // -------------------------------------------------------------------------------

        // One walkable triangle, in whatever frame its mesh uses (cell-local or world).
        private readonly record struct NavTri(Noggog.P3Float A, Noggog.P3Float B, Noggog.P3Float C);

        private static readonly List<NavTri> NoTris = new();   // KNOWN-empty (a cell with no navmesh at all)
        private readonly Dictionary<string, List<NavTri>?> navTriCache = new();

        // The triangles covering (cellRef | worldspaceRef, position). null = UNKNOWN (no link cache /
        // unresolvable) — the callers stay silent. An empty list = KNOWN to have no navmesh.
        private List<NavTri>? NavTrisAt(string cellRef, string worldspaceRef, Vec3 pos, ICell? builtCell)
        {
            if (!string.IsNullOrWhiteSpace(worldspaceRef))
            {
                // An in-spec worldspace is OURS: the cell object we built carries the flat quad (or
                // carries nothing, if `navmesh` was not set on it — which is exactly worth warning about).
                if (!LooksExternalRef(worldspaceRef)) return builtCell is null ? null : TrisOfCell(builtCell);

                if (!TryExternalRef(worldspaceRef, out var wsFk)) return null;
                int cx = PosToGrid(pos.X), cy = PosToGrid(pos.Y);
                var key = $"W:{wsFk}:{cx}:{cy}";
                if (navTriCache.TryGetValue(key, out var hit)) return hit;

                var master = worldspaceRef[..worldspaceRef.IndexOf(':')].Trim();
                List<NavTri>? tris = null;
                if (MasterCache(master) is not null)
                {
                    // An exterior mesh belongs to one cell but its triangles run right up to (and its
                    // neighbours' over) the cell border, so a point near an edge is covered by the
                    // NEIGHBOUR's mesh. Gather the 3×3 block or we would report false "off navmesh".
                    tris = new List<NavTri>();
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            if (FindMasterExteriorCell(master, wsFk, cx + dx, cy + dy) is { } mc)
                                AddTris(mc, tris);
                }
                navTriCache[key] = tris;
                return tris;
            }

            if (LooksExternalRef(cellRef))
            {
                if (!TryExternalRef(cellRef, out var cellFk)) return null;
                var key = $"C:{cellFk}";
                if (navTriCache.TryGetValue(key, out var hit)) return hit;

                List<NavTri>? tris = null;
                var master = cellRef[..cellRef.IndexOf(':')].Trim();
                if (MasterCache(master) is { } cache && cache.TryResolve<ICellGetter>(cellFk, out var vanilla))
                { tris = new List<NavTri>(); AddTris(vanilla, tris); }
                navTriCache[key] = tris;
                return tris;
            }

            // An in-spec INTERIOR cell. ModForge has never authored interior navmesh (only the flat
            // quads for custom worldspaces), so this is KNOWN-empty, not unknown — and that is a real,
            // silent, gameplay-breaking fact worth a warning. No master needed: works offline.
            if (cellsByEd.ContainsKey(cellRef)) return NoTris;
            return null;
        }

        private static List<NavTri> TrisOfCell(ICell cell)
        { var tris = new List<NavTri>(); AddTris(cell, tris); return tris; }

        private static void AddTris(ICellGetter cell, List<NavTri> into)
        {
            foreach (var nm in cell.NavigationMeshes)
            {
                if (nm.Data is not { } d) continue;
                var vs = d.Vertices;
                foreach (var t in d.Triangles)
                {
                    // A Deleted triangle is switched off for pathing (the engine's own flag, bit 3) —
                    // it is not walkable, so it must not count as coverage.
                    if ((t.Flags & NavmeshTriangle.Flag.Deleted) != 0) continue;
                    int a = t.Vertices.X, b = t.Vertices.Y, c = t.Vertices.Z;
                    if (a < 0 || b < 0 || c < 0 || a >= vs.Count || b >= vs.Count || c >= vs.Count) continue;
                    into.Add(new NavTri(vs[a], vs[b], vs[c]));
                }
            }
        }

        // --- geometry (2D-XY projection; navmesh pathing is a walkable surface, not a solid) ------

        // Is (px,py) inside the triangle's XY projection? Barycentric sign test.
        private static bool InTri2D(float px, float py, in NavTri t)
        {
            float d1 = Cross(px, py, t.A.X, t.A.Y, t.B.X, t.B.Y);
            float d2 = Cross(px, py, t.B.X, t.B.Y, t.C.X, t.C.Y);
            float d3 = Cross(px, py, t.C.X, t.C.Y, t.A.X, t.A.Y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0;
            bool pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }
        private static float Cross(float px, float py, float ax, float ay, float bx, float by)
            => (px - bx) * (ay - by) - (ax - bx) * (py - by);

        // Height of the triangle's plane at (px,py) — barycentric interpolation. Only meaningful for
        // a point that is actually inside (see InTri2D); degenerate triangles fall back to vertex A.
        private static float TriZAt(float px, float py, in NavTri t)
        {
            float det = (t.B.Y - t.C.Y) * (t.A.X - t.C.X) + (t.C.X - t.B.X) * (t.A.Y - t.C.Y);
            if (MathF.Abs(det) < 1e-6f) return t.A.Z;
            float l1 = ((t.B.Y - t.C.Y) * (px - t.C.X) + (t.C.X - t.B.X) * (py - t.C.Y)) / det;
            float l2 = ((t.C.Y - t.A.Y) * (px - t.C.X) + (t.A.X - t.C.X) * (py - t.C.Y)) / det;
            return l1 * t.A.Z + l2 * t.B.Z + (1f - l1 - l2) * t.C.Z;
        }

        // XY distance from (px,py) to the triangle (0 when inside) — "the nearest navmesh is N units away".
        private static float DistToTri2D(float px, float py, in NavTri t)
        {
            if (InTri2D(px, py, t)) return 0f;
            return MathF.Min(DistToSeg(px, py, t.A, t.B),
                   MathF.Min(DistToSeg(px, py, t.B, t.C), DistToSeg(px, py, t.C, t.A)));
        }
        private static float DistToSeg(float px, float py, in Noggog.P3Float a, in Noggog.P3Float b)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float len2 = dx * dx + dy * dy;
            float u = len2 < 1e-6f ? 0f : Math.Clamp(((px - a.X) * dx + (py - a.Y) * dy) / len2, 0f, 1f);
            float qx = a.X + u * dx - px, qy = a.Y + u * dy - py;
            return MathF.Sqrt(qx * qx + qy * qy);
        }

        // The nearest triangle to a point, and how far (XY) it is. Returns false for an empty mesh set.
        private static bool NearestTri(float px, float py, List<NavTri> tris, out NavTri best, out float dist)
        {
            best = default; dist = float.MaxValue;
            foreach (var t in tris)
            {
                float d = DistToTri2D(px, py, t);
                if (d < dist) { dist = d; best = t; }
                if (d == 0f) break;
            }
            return dist < float.MaxValue;
        }
    }
}
