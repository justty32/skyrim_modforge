using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ModForge;

// 載入一張 8-bit grayscale PNG → 全域 per-vertex alpha 網格，按 cell grid 切割採樣。
// 與 Heightmap 同一套網格約定：PNG 寬 = CellsX×32+1、高 = CellsY×32+1，相鄰格共用邊緣欄
// → 紋理 alpha 在 cell 邊界自動對齊。影像左下=西南角；影像往上 = 世界 +Y(北)。
// 像素值 0..255 → alpha 0..1（0=該紋理不覆蓋此頂點，255=完全覆蓋）。
// 採樣回傳 [row, col]：row 0 = 最南、col 0 = 最西（餵 Vtxt.BuildLayers）。
public sealed class Splatmap
{
    private const int CellVerts = 33;
    private const int CellStep = 32;

    public int CellsX { get; }
    public int CellsY { get; }
    public int OriginX { get; }   // PNG 左下角像素對到的 cell 座標 X（與 heightmap 一致）
    public int OriginY { get; }
    private readonly float[,] _world;   // [worldRow, worldCol]，row 0 = 最南

    private Splatmap(float[,] world, int cx, int cy, int ox, int oy)
    { _world = world; CellsX = cx; CellsY = cy; OriginX = ox; OriginY = oy; }

    public static Splatmap Load(SplatmapSpec spec, string specDir)
    {
        var path = System.IO.Path.IsPathRooted(spec.Path)
            ? spec.Path : System.IO.Path.Combine(specDir, spec.Path);
        if (!System.IO.File.Exists(path))
            throw new System.IO.FileNotFoundException($"splatmap PNG not found: {path}");

        using var img = Image.Load<L8>(path);
        int w = img.Width, h = img.Height;
        if ((w - 1) % CellStep != 0 || (h - 1) % CellStep != 0 || w < CellVerts || h < CellVerts)
            throw new System.ArgumentException(
                $"splatmap '{spec.Path}' is {w}×{h}px; width must be N×32+1 and height M×32+1 (e.g. 33,65,97…)");

        int cx = (w - 1) / CellStep, cy = (h - 1) / CellStep;

        // 影像 y=0 在頂端=世界最北。翻轉成 world[row,col] row0=最南（與 Heightmap 一致）。
        var world = new float[h, w];
        img.ProcessPixelRows(accessor =>
        {
            for (int imgY = 0; imgY < h; imgY++)
            {
                var rowSpan = accessor.GetRowSpan(imgY);
                int worldRow = (h - 1) - imgY;
                for (int x = 0; x < w; x++)
                    world[worldRow, x] = rowSpan[x].PackedValue / 255f;
            }
        });

        return new Splatmap(world, cx, cy, spec.OriginX, spec.OriginY);
    }

    /// <summary>
    /// 取全域 cell 座標 (cellX, cellY) 的 33×33 alpha 網格。cell 落在本圖涵蓋範圍外 → 回 false。
    /// 涵蓋範圍 = [OriginX, OriginX+CellsX) × [OriginY, OriginY+CellsY)。
    /// </summary>
    public bool TrySampleCell(int cellX, int cellY, out float[,] alpha)
    {
        int lx = cellX - OriginX, ly = cellY - OriginY;
        if (lx < 0 || ly < 0 || lx >= CellsX || ly >= CellsY) { alpha = null!; return false; }

        var grid = new float[CellVerts, CellVerts];
        int baseCol = lx * CellStep;   // 相鄰格共用第 32 欄 → 邊緣自動對齊
        int baseRow = ly * CellStep;
        for (int row = 0; row < CellVerts; row++)
            for (int col = 0; col < CellVerts; col++)
                grid[row, col] = _world[baseRow + row, baseCol + col];
        alpha = grid;
        return true;
    }
}
