using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

// Skyrim LAND per-vertex texture blend (ATXT header + VTXT alpha points) builder.
//
// A cell's 33×33 vertex grid is split into FOUR 17×17 quadrants that share the centre row/col
// (vertex index 16 belongs to both sides). Each additional texture layer (above the BTXT base)
// is stored per quadrant as an AlphaLayer = { LayerHeader(texture, quadrant, layerNumber) +
// sparse list of AlphaLayerData(position, opacity) }. "Sparse" = only vertices with non-zero
// alpha are stored (vanilla does the same); a quadrant with no coverage emits NO layer at all.
//
// GRID CONVENTION (aligned with Heightmap.SampleCell / Vnml): the input alpha grid is
// alpha[row, col] with row 0 = south (−Y), col 0 = west (−X). Quadrants therefore map as:
//   BottomLeft  = south-west = rows 0..16, cols 0..16
//   BottomRight = south-east = rows 0..16, cols 16..32
//   TopLeft     = north-west = rows 16..32, cols 0..16
//   TopRight    = north-east = rows 16..32, cols 16..32
// VTXT position within a quadrant = localRow*17 + localCol (row-major, localRow = y/north-south).
//
// ⚠️ BYTE-VERIFY PENDING (main machine, xEdit vs vanilla LAND): the exact (a) VTXT position
// row/col order, (b) per-quadrant LayerNumber packing — see WAIT_USER. Structure + sparsity are
// modelled on the documented format; visual blend direction is correct, exact bytes await xEdit.
public static class Vtxt
{
    public const int CellVerts = 33;   // full grid edge
    public const int QuadVerts = 17;   // quadrant edge (shares centre row/col with neighbour)
    public const int Mid = 16;         // shared centre index

    // (Quadrant, base row offset, base col offset) — local (r,c) maps to alpha[baseRow+r, baseCol+c].
    private static readonly (Quadrant Q, int BaseRow, int BaseCol)[] Quads =
    {
        (Quadrant.BottomLeft,  0,   0),
        (Quadrant.BottomRight, 0,   Mid),
        (Quadrant.TopLeft,     Mid, 0),
        (Quadrant.TopRight,    Mid, Mid),
    };

    /// <summary>
    /// Build the alpha layers for ONE additional texture across all 4 quadrants of a cell.
    /// <paramref name="alpha"/> is a 33×33 grid of per-vertex opacity in [0,1] (alpha[row,col],
    /// row0=south, col0=west). Quadrants with no non-zero alpha are omitted. <paramref name="layerNumber"/>
    /// is the stacking index (base BTXT = 0, first alpha texture = 1, …).
    /// </summary>
    public static IEnumerable<AlphaLayer> BuildLayers(float[,] alpha, FormKey texture, ushort layerNumber)
    {
        if (alpha.GetLength(0) != CellVerts || alpha.GetLength(1) != CellVerts)
            throw new System.ArgumentException(
                $"alpha grid must be {CellVerts}×{CellVerts}, got {alpha.GetLength(0)}×{alpha.GetLength(1)}");

        foreach (var (q, baseRow, baseCol) in Quads)
        {
            var points = new List<AlphaLayerData>();
            for (int r = 0; r < QuadVerts; r++)
                for (int c = 0; c < QuadVerts; c++)
                {
                    float a = alpha[baseRow + r, baseCol + c];
                    if (a <= 0f) continue;
                    points.Add(new AlphaLayerData
                    {
                        Position = (ushort)(r * QuadVerts + c),
                        Opacity = a > 1f ? 1f : a,
                        Unused = 0,
                    });
                }
            if (points.Count == 0) continue;   // texture absent from this quadrant — no layer

            var header = new LayerHeader { Quadrant = q, LayerNumber = layerNumber };
            header.Texture.SetTo(texture);
            // AlphaLayerData (the VTXT list) defaults to null — it's an optional subrecord — so assign.
            var layer = new AlphaLayer { Header = header, AlphaLayerData = new Noggog.ExtendedList<AlphaLayerData>() };
            layer.AlphaLayerData.AddRange(points);
            yield return layer;
        }
    }
}
