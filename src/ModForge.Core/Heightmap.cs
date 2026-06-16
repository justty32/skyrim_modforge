using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ModForge;

// 載入一張 16-bit grayscale PNG → 全域高度網格，按 cell grid 切割採樣。
// PNG 寬 = CellsX×32+1、高 = CellsY×32+1。影像左下=西南角；影像往上 = 世界 +Y(北)。
// 採樣回傳的網格索引 [row, col]：row 0 = 最南(世界 -Y)、col 0 = 最西。
public sealed class Heightmap
{
    private const int CellVerts = 33;
    private const int CellStep = 32;

    public int CellsX { get; }
    public int CellsY { get; }
    public int OriginX { get; }
    public int OriginY { get; }
    private readonly float[,] _world;   // [worldRow, worldCol]，row 0 = 最南

    private Heightmap(float[,] world, int cx, int cy, int ox, int oy)
    { _world = world; CellsX = cx; CellsY = cy; OriginX = ox; OriginY = oy; }

    public static Heightmap Load(HeightmapSpec spec, string specDir)
    {
        var path = System.IO.Path.IsPathRooted(spec.Path)
            ? spec.Path : System.IO.Path.Combine(specDir, spec.Path);
        if (!System.IO.File.Exists(path))
            throw new System.IO.FileNotFoundException($"heightmap PNG not found: {path}");

        using var img = Image.Load<L16>(path);
        int w = img.Width, h = img.Height;
        if ((w - 1) % CellStep != 0 || (h - 1) % CellStep != 0 || w < CellVerts || h < CellVerts)
            throw new System.ArgumentException(
                $"heightmap '{spec.Path}' is {w}×{h}px; width must be N×32+1 and height M×32+1 (e.g. 33,65,97…)");

        int cx = (w - 1) / CellStep, cy = (h - 1) / CellStep;
        float range = spec.MaxHeight - spec.MinHeight;

        // 影像 y=0 在頂端=世界最北。翻轉成 world[row,col] row0=最南。
        var world = new float[h, w];
        img.ProcessPixelRows(accessor =>
        {
            for (int imgY = 0; imgY < h; imgY++)
            {
                var rowSpan = accessor.GetRowSpan(imgY);
                int worldRow = (h - 1) - imgY;       // 翻轉
                for (int x = 0; x < w; x++)
                    world[worldRow, x] = spec.MinHeight + (rowSpan[x].PackedValue / 65535f) * range;
            }
        });

        return new Heightmap(world, cx, cy, spec.OriginX, spec.OriginY);
    }

    /// <summary>取第 (cellX, cellY) 格的 33×33 高度網格，[row,col] row0=最南、col0=最西。</summary>
    public float[,] SampleCell(int cellX, int cellY)
    {
        var grid = new float[CellVerts, CellVerts];
        int baseCol = cellX * CellStep;   // 相鄰格共用第 32 欄 → 邊緣自動對齊
        int baseRow = cellY * CellStep;
        for (int row = 0; row < CellVerts; row++)
            for (int col = 0; col < CellVerts; col++)
                grid[row, col] = _world[baseRow + row, baseCol + col];
        return grid;
    }
}
