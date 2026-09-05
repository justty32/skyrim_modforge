namespace ModForge;

public static partial class Generator
{
    // Automatic authored-mesh seams may bridge small differences between the authored walkable
    // surface and the neighbouring cell's terrain plane. 128 units = 16 VHGT height quanta (8 units
    // each), enough for a modest terrain mismatch while staying far below the 400-unit storey gap
    // in the existing two-storey authored-mesh fixture. Tests pin the inclusive limit and +1 reject.
    internal const float CustomNavmeshSeamMaxZDelta = 128f;
    private const float CustomNavmeshSeamBoundaryEpsilon = 0.01f;

    // Fill undeclared seams involving at least one authored mesh. The established flat<->flat pass
    // remains entirely separate and unchanged: this method explicitly skips those pairs. Explicit
    // authored links run first and win; an edge already carrying any neighbour is not considered.
    private static void ConnectAuthoredCellNavmeshSeams(
        IReadOnlyList<NavmCellInfo> navmInfos, Action<string> warn)
    {
        var byCell = new Dictionary<(FormKey Ws, int X, int Y), NavmCellInfo>();
        foreach (var info in navmInfos)
            byCell.TryAdd((info.WorldspaceFk, info.X, info.Y), info);

        foreach (var info in navmInfos)
        {
            TrySeam(info, info.X + 1, info.Y, vertical: true);   // east/west
            TrySeam(info, info.X, info.Y + 1, vertical: false); // north/south
        }

        void TrySeam(NavmCellInfo from, int tx, int ty, bool vertical)
        {
            if (!byCell.TryGetValue((from.WorldspaceFk, tx, ty), out var to)) return;
            if (from.Geometry is null && to.Geometry is null) return; // flat path owns this pair

            float boundary = vertical ? tx * 4096f : ty * 4096f;
            var a = BoundaryEdges(from, vertical, boundary);
            var b = BoundaryEdges(to, vertical, boundary);
            if (a.Count == 0 && b.Count == 0) return;

            var overlaps = new List<SeamPair>();
            for (int ai = 0; ai < a.Count; ai++)
            for (int bi = 0; bi < b.Count; bi++)
            {
                float lo = Math.Max(AxisMin(a[ai], vertical), AxisMin(b[bi], vertical));
                float hi = Math.Min(AxisMax(a[ai], vertical), AxisMax(b[bi], vertical));
                float overlap = hi - lo;
                if (overlap <= CustomNavmeshSeamBoundaryEpsilon) continue;
                float dz = Math.Max(
                    Math.Abs(ZAt(a[ai], lo, vertical) - ZAt(b[bi], lo, vertical)),
                    Math.Abs(ZAt(a[ai], hi, vertical) - ZAt(b[bi], hi, vertical)));
                overlaps.Add(new SeamPair(ai, bi, overlap, dz));
            }

            var usedA = new HashSet<int>();
            var usedB = new HashSet<int>();
            foreach (var pair in overlaps
                .Where(p => p.MaxZDelta <= CustomNavmeshSeamMaxZDelta)
                .OrderByDescending(p => p.Overlap)
                .ThenBy(p => p.MaxZDelta)
                .ThenBy(p => a[p.A].Triangle).ThenBy(p => a[p.A].Edge)
                .ThenBy(p => b[p.B].Triangle).ThenBy(p => b[p.B].Edge))
            {
                if (usedA.Contains(pair.A) || usedB.Contains(pair.B)) continue;
                usedA.Add(pair.A);
                usedB.Add(pair.B);
                AddReciprocalSeam(a[pair.A], b[pair.B]);
            }

            var unmatchedA = Enumerable.Range(0, a.Count).Where(i => !usedA.Contains(i)).ToArray();
            var unmatchedB = Enumerable.Range(0, b.Count).Where(i => !usedB.Contains(i)).ToArray();
            if (unmatchedA.Length == 0 && unmatchedB.Length == 0) return;

            var rejectedByZ = overlaps.Where(p => !usedA.Contains(p.A) && !usedB.Contains(p.B)
                && p.MaxZDelta > CustomNavmeshSeamMaxZDelta).ToArray();
            string reason = rejectedByZ.Length > 0
                ? $"closest overlapping-edge Z delta {rejectedByZ.Min(p => p.MaxZDelta):0.###} exceeds "
                  + $"{nameof(CustomNavmeshSeamMaxZDelta)}={CustomNavmeshSeamMaxZDelta:0.###}"
                : "no unused overlapping counterpart at the shared boundary";
            var missing = unmatchedA.Select(i => EdgeName(a[i]))
                .Concat(unmatchedB.Select(i => EdgeName(b[i]))).Take(8).ToArray();
            int missingCount = unmatchedA.Length + unmatchedB.Length;
            string more = missingCount > missing.Length ? $", +{missingCount - missing.Length} more" : "";
            warn($"  ! navmesh seam cells ({from.X},{from.Y})<->({to.X},{to.Y}): "
                + $"{missingCount} boundary edge(s) unmatched ({reason}): {string.Join(", ", missing)}{more}");
        }
    }

    private readonly record struct SeamEdge(
        NavmCellInfo Info, int Triangle, int Edge, Noggog.P3Float A, Noggog.P3Float B);
    private readonly record struct SeamPair(int A, int B, float Overlap, float MaxZDelta);

    private static List<SeamEdge> BoundaryEdges(NavmCellInfo info, bool vertical, float boundary)
    {
        var data = info.Navm.Data!;
        var result = new List<SeamEdge>();
        for (int ti = 0; ti < data.Triangles.Count; ti++)
        {
            var tri = data.Triangles[ti];
            for (int edge = 0; edge < 3; edge++)
            {
                var flag = edge switch
                {
                    0 => NavmeshTriangle.Flag.EdgeLink_0_1,
                    1 => NavmeshTriangle.Flag.EdgeLink_1_2,
                    _ => NavmeshTriangle.Flag.EdgeLink_2_0,
                };
                short neighbour = edge switch
                {
                    0 => tri.EdgeLink_0_1,
                    1 => tri.EdgeLink_1_2,
                    _ => tri.EdgeLink_2_0,
                };
                if (neighbour != -1 || tri.Flags.HasFlag(flag)) continue;

                int va = edge switch { 0 => tri.Vertices.X, 1 => tri.Vertices.Y, _ => tri.Vertices.Z };
                int vb = edge switch { 0 => tri.Vertices.Y, 1 => tri.Vertices.Z, _ => tri.Vertices.X };
                var a = data.Vertices[va];
                var b = data.Vertices[vb];
                float pa = vertical ? a.X : a.Y;
                float pb = vertical ? b.X : b.Y;
                if (Math.Abs(pa - boundary) <= CustomNavmeshSeamBoundaryEpsilon
                    && Math.Abs(pb - boundary) <= CustomNavmeshSeamBoundaryEpsilon)
                    result.Add(new SeamEdge(info, ti, edge, a, b));
            }
        }
        return result;
    }

    private static float AxisMin(SeamEdge e, bool vertical) =>
        Math.Min(vertical ? e.A.Y : e.A.X, vertical ? e.B.Y : e.B.X);
    private static float AxisMax(SeamEdge e, bool vertical) =>
        Math.Max(vertical ? e.A.Y : e.A.X, vertical ? e.B.Y : e.B.X);

    private static float ZAt(SeamEdge e, float position, bool vertical)
    {
        float a = vertical ? e.A.Y : e.A.X;
        float b = vertical ? e.B.Y : e.B.X;
        if (Math.Abs(b - a) <= CustomNavmeshSeamBoundaryEpsilon) return (e.A.Z + e.B.Z) / 2f;
        return e.A.Z + (e.B.Z - e.A.Z) * ((position - a) / (b - a));
    }

    private static void AddReciprocalSeam(SeamEdge a, SeamEdge b)
    {
        AddOne(a, b);
        AddOne(b, a);

        static void AddOne(SeamEdge from, SeamEdge to)
        {
            var data = from.Info.Navm.Data!;
            var index = checked((short)data.EdgeLinks.Count);
            var link = new EdgeLink { Unknown = 0, TriangleIndex = checked((short)to.Triangle) };
            link.Mesh.SetTo(to.Info.Navm.FormKey);
            data.EdgeLinks.Add(link);

            var tri = data.Triangles[from.Triangle];
            switch (from.Edge)
            {
                case 0: tri.EdgeLink_0_1 = index; tri.Flags |= NavmeshTriangle.Flag.EdgeLink_0_1; break;
                case 1: tri.EdgeLink_1_2 = index; tri.Flags |= NavmeshTriangle.Flag.EdgeLink_1_2; break;
                default: tri.EdgeLink_2_0 = index; tri.Flags |= NavmeshTriangle.Flag.EdgeLink_2_0; break;
            }
        }
    }

    private static string EdgeName(SeamEdge e) =>
        $"({e.Info.X},{e.Info.Y}) tri {e.Triangle} edge {e.Edge}";
}
