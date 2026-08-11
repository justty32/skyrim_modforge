using System.Buffers.Binary;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

// Pure append-only NAVM geometry operation. The caller applies this to a DeepCopy and publishes the
// copy only on success, making every failure transactional. Existing triangle indices never move.
internal static class NavmeshPatch
{
    private const double GeometryTolerance = 1e-4;

    internal static bool TryValidatePolygon(IReadOnlyList<Vec3>? polygon, out string error)
    {
        error = "";
        if (polygon is null || polygon.Count < 3 || polygon.Count > 32)
        { error = "polygon needs 3–32 points"; return false; }
        if (polygon.Any(p => !float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z)))
        { error = "polygon coordinates must all be finite"; return false; }

        double area2 = SignedArea2(polygon);
        if (Math.Abs(area2) <= GeometryTolerance)
        { error = "polygon has zero XY area"; return false; }

        for (int i = 0; i < polygon.Count; i++)
        for (int j = i + 1; j < polygon.Count; j++)
            if (DistanceSquared(polygon[i], polygon[j]) <= GeometryTolerance * GeometryTolerance)
            { error = $"polygon repeats point {i} at point {j}"; return false; }

        for (int i = 0; i < polygon.Count; i++)
        {
            int i2 = (i + 1) % polygon.Count;
            for (int j = i + 1; j < polygon.Count; j++)
            {
                int j2 = (j + 1) % polygon.Count;
                if (i == j || i2 == j || j2 == i) continue;
                if (SegmentsIntersect(polygon[i], polygon[i2], polygon[j], polygon[j2]))
                { error = $"polygon edges {i} and {j} cross"; return false; }
            }
        }

        double sign = Math.Sign(area2);
        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count]; var c = polygon[(i + 2) % polygon.Count];
            double cross = Cross(a, b, c);
            if (Math.Abs(cross) <= GeometryTolerance || Math.Sign(cross) != sign)
            { error = "polygon must be strictly convex in XY (no concave or collinear corners)"; return false; }
        }
        return true;
    }

    internal static bool TryApply(NavigationMesh candidate, IReadOnlyList<Vec3> authored, float epsilon, out string error)
    {
        error = "";
        if (!TryValidatePolygon(authored, out error)) return false;
        if (!float.IsFinite(epsilon) || epsilon <= 0f || epsilon > 64f)
        { error = "epsilon must be finite, > 0 and <= 64"; return false; }
        if (candidate.Data is not { } data)
        { error = "target NAVM has no geometry data"; return false; }
        if (data.Vertices.Count + authored.Count > short.MaxValue)
        { error = "patch would exceed the signed 16-bit NAVM vertex-index limit"; return false; }
        int newTriangleCount = authored.Count - 2;
        if (data.Triangles.Count + newTriangleCount > short.MaxValue)
        { error = "patch would exceed the signed 16-bit NAVM triangle-index limit"; return false; }

        var polygon = authored.Select(p => new Vec3 { X = p.X, Y = p.Y, Z = p.Z }).ToList();
        if (SignedArea2(polygon) < 0) polygon.Reverse();

        var seams = FindSeams(data, polygon, epsilon);
        if (seams.Count != 1)
        {
            error = seams.Count == 0
                ? $"auto link found no complete polygon edge matching an unlinked NAVM boundary edge within epsilon {epsilon}"
                : $"auto link is ambiguous: found {seams.Count} matching NAVM boundary edges (MVP requires exactly one)";
            return false;
        }

        int vertexBase = data.Vertices.Count;
        int triangleBase = data.Triangles.Count;
        foreach (var p in polygon) data.Vertices.Add(new Noggog.P3Float(p.X, p.Y, p.Z));

        for (int i = 0; i < newTriangleCount; i++)
            data.Triangles.Add(new NavmeshTriangle
            {
                Vertices = new Noggog.P3Int16((short)vertexBase, (short)(vertexBase + i + 1), (short)(vertexBase + i + 2)),
                EdgeLink_0_1 = -1, EdgeLink_1_2 = -1, EdgeLink_2_0 = -1,
            });

        // Link the new fan internally by comparing vertex-index pairs. This touches new triangles only.
        for (int a = 0; a < newTriangleCount; a++)
        for (int b = a + 1; b < newTriangleCount; b++)
            LinkSharedEdge(data.Triangles[triangleBase + a], triangleBase + b,
                           data.Triangles[triangleBase + b], triangleBase + a);

        var seam = seams[0];
        int newTriOffset = PolygonBoundaryOwner(seam.PolygonEdge, polygon.Count);
        var newTri = data.Triangles[triangleBase + newTriOffset];
        int newEdge = FindTriangleEdge(newTri, vertexBase + seam.PolygonEdge,
            vertexBase + ((seam.PolygonEdge + 1) % polygon.Count));
        if (newEdge < 0)
        { error = "internal error locating the stitched edge in the fan"; return false; }
        SetEdge(newTri, newEdge, (short)seam.OldTriangle);
        SetEdge(data.Triangles[seam.OldTriangle], seam.OldEdge, (short)(triangleBase + newTriOffset));

        RebuildOneBucketGrid(data);
        return true;
    }

    private readonly record struct Seam(int PolygonEdge, int OldTriangle, int OldEdge);

    private static List<Seam> FindSeams(NavigationMeshData data, IReadOnlyList<Vec3> polygon, float epsilon)
    {
        var result = new List<Seam>();
        double eps2 = (double)epsilon * epsilon;
        for (int pe = 0; pe < polygon.Count; pe++)
        {
            var pa = polygon[pe]; var pb = polygon[(pe + 1) % polygon.Count];
            for (int ti = 0; ti < data.Triangles.Count; ti++)
            for (int edge = 0; edge < 3; edge++)
            {
                var tri = data.Triangles[ti];
                if (GetEdge(tri, edge) != -1) continue;
                (int ai, int bi) = EdgeVertices(tri, edge);
                if ((Near(pa, data.Vertices[ai], eps2) && Near(pb, data.Vertices[bi], eps2)) ||
                    (Near(pa, data.Vertices[bi], eps2) && Near(pb, data.Vertices[ai], eps2)))
                    result.Add(new Seam(pe, ti, edge));
            }
        }
        return result;
    }

    private static int PolygonBoundaryOwner(int edge, int count)
        => edge == 0 ? 0 : edge == count - 1 ? count - 3 : edge - 1;

    private static void LinkSharedEdge(NavmeshTriangle a, int bIndex, NavmeshTriangle b, int aIndex)
    {
        for (int ae = 0; ae < 3; ae++)
        for (int be = 0; be < 3; be++)
        {
            var av = EdgeVertices(a, ae); var bv = EdgeVertices(b, be);
            if ((av.Item1 == bv.Item1 && av.Item2 == bv.Item2) || (av.Item1 == bv.Item2 && av.Item2 == bv.Item1))
            { SetEdge(a, ae, (short)bIndex); SetEdge(b, be, (short)aIndex); }
        }
    }

    private static int FindTriangleEdge(NavmeshTriangle t, int a, int b)
    {
        for (int e = 0; e < 3; e++)
        { var v = EdgeVertices(t, e); if ((v.Item1 == a && v.Item2 == b) || (v.Item1 == b && v.Item2 == a)) return e; }
        return -1;
    }

    private static (int, int) EdgeVertices(NavmeshTriangle t, int edge) => edge switch
    {
        0 => (t.Vertices.X, t.Vertices.Y), 1 => (t.Vertices.Y, t.Vertices.Z), _ => (t.Vertices.Z, t.Vertices.X),
    };
    private static short GetEdge(NavmeshTriangle t, int edge) => edge switch
    { 0 => t.EdgeLink_0_1, 1 => t.EdgeLink_1_2, _ => t.EdgeLink_2_0 };
    private static void SetEdge(NavmeshTriangle t, int edge, short value)
    { if (edge == 0) t.EdgeLink_0_1 = value; else if (edge == 1) t.EdgeLink_1_2 = value; else t.EdgeLink_2_0 = value; }

    private static void RebuildOneBucketGrid(NavigationMeshData data)
    {
        var bytes = new byte[4 + data.Triangles.Count * 2];
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), data.Triangles.Count);
        for (int i = 0; i < data.Triangles.Count; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4 + i * 2, 2), (ushort)i);
        data.NavmeshGrid = bytes;
        data.NavmeshGridDivisor = 1;
        data.Min = new Noggog.P3Float(data.Vertices.Min(v => v.X), data.Vertices.Min(v => v.Y), data.Vertices.Min(v => v.Z));
        data.Max = new Noggog.P3Float(data.Vertices.Max(v => v.X), data.Vertices.Max(v => v.Y), data.Vertices.Max(v => v.Z));
        data.MaxDistanceX = data.Max.X - data.Min.X;
        data.MaxDistanceY = data.Max.Y - data.Min.Y;
    }

    private static bool Near(Vec3 a, Noggog.P3Float b, double eps2)
        => ((double)a.X - b.X) * (a.X - b.X) + ((double)a.Y - b.Y) * (a.Y - b.Y) + ((double)a.Z - b.Z) * (a.Z - b.Z) <= eps2;
    private static double DistanceSquared(Vec3 a, Vec3 b)
        => ((double)a.X - b.X) * (a.X - b.X) + ((double)a.Y - b.Y) * (a.Y - b.Y) + ((double)a.Z - b.Z) * (a.Z - b.Z);
    private static double SignedArea2(IReadOnlyList<Vec3> p)
    { double a = 0; for (int i = 0; i < p.Count; i++) a += (double)p[i].X * p[(i + 1) % p.Count].Y - (double)p[(i + 1) % p.Count].X * p[i].Y; return a; }
    private static double Cross(Vec3 a, Vec3 b, Vec3 c)
        => ((double)b.X - a.X) * (c.Y - a.Y) - ((double)b.Y - a.Y) * (c.X - a.X);
    private static bool SegmentsIntersect(Vec3 a, Vec3 b, Vec3 c, Vec3 d)
    {
        double abC = Cross(a, b, c), abD = Cross(a, b, d), cdA = Cross(c, d, a), cdB = Cross(c, d, b);
        return abC * abD <= GeometryTolerance && cdA * cdB <= GeometryTolerance;
    }
}
