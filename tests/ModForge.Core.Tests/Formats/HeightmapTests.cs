using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class HeightmapTests
{
    // 造一張 width×height 的 L16 PNG，像素值 = f(x,y)，回傳暫存路徑
    private static string MakePng(int w, int h, System.Func<int, int, ushort> f)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"mf_hm_{System.Guid.NewGuid():N}.png");
        using var img = new Image<L16>(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            img[x, y] = new L16(f(x, y));
        img.Save(path);
        return path;
    }

    // 寬高非 N×32+1 → 丟 ArgumentException（不靜默裁切）
    [Fact]
    public void Load_BadDimensions_Throws()
    {
        var path = MakePng(64, 64, (x, y) => 0);   // 64 ≠ N×32+1
        var spec = new HeightmapSpec { Path = path, MinHeight = 0, MaxHeight = 4000 };
        Assert.Throws<System.ArgumentException>(() => Heightmap.Load(spec, System.IO.Path.GetDirectoryName(path)!));
        System.IO.File.Delete(path);
    }

    // 1×1 cell（33×33 px）：min/max 線性映射正確，原點/朝向正確
    [Fact]
    public void Load_SingleCell_MapsHeightsAndOrientation()
    {
        // png=0 → 1000, png=65535 → 5000。底列(影像 y=32)全黑、頂列(y=0)全白
        var path = MakePng(33, 33, (x, y) => y == 0 ? (ushort)65535 : (ushort)0);
        var spec = new HeightmapSpec { Path = path, OriginX = 0, OriginY = 0, MinHeight = 1000, MaxHeight = 5000 };

        var hm = Heightmap.Load(spec, System.IO.Path.GetDirectoryName(path)!);

        Assert.Equal(1, hm.CellsX);
        Assert.Equal(1, hm.CellsY);
        var grid = hm.SampleCell(0, 0);   // 回傳 [row,col]，row 0 = 南/影像底
        // 影像頂列(白=max)對到世界北 → grid 的最北一列(row 32)應是 5000
        Assert.Equal(5000f, grid[32, 0], 3);
        Assert.Equal(1000f, grid[0, 0], 3);   // 南列=黑=min
        System.IO.File.Delete(path);
    }

    // SampleCellExtended：中心 33×33 與 SampleCell 結果相同
    [Fact]
    public void SampleCellExtended_CenterMatchesSampleCell()
    {
        var path = MakePng(33, 33, (x, y) => (ushort)(x * 1000 + y * 500));
        var spec = new HeightmapSpec { Path = path, MinHeight = 0, MaxHeight = 8000 };
        var hm = Heightmap.Load(spec, System.IO.Path.GetDirectoryName(path)!);

        var cell = hm.SampleCell(0, 0);
        var ext  = hm.SampleCellExtended(0, 0);

        for (int r = 0; r < 33; r++)
            for (int c = 0; c < 33; c++)
                Assert.Equal(cell[r, c], ext[r + 1, c + 1], 3f);

        System.IO.File.Delete(path);
    }

    // SampleCellExtended：PNG 邊界外夾取最近邊（border clamping）
    [Fact]
    public void SampleCellExtended_BorderClamping()
    {
        // 單格 PNG，西南角 (col=0, row=0) 高度 = 100
        var path = MakePng(33, 33, (x, y) => (ushort)(x == 0 && y == 32 ? 1000 : 32768));
        var spec = new HeightmapSpec { Path = path, MinHeight = 0, MaxHeight = 65535 };
        var hm = Heightmap.Load(spec, System.IO.Path.GetDirectoryName(path)!);

        var ext = hm.SampleCellExtended(0, 0);
        // ext[0, 0] 是 PNG 外西南角，應夾取 _world[0, 0] = SampleCell 最南最西頂點
        var cell = hm.SampleCell(0, 0);
        Assert.Equal(cell[0, 0], ext[0, 0], 3f);   // 南邊夾取
        Assert.Equal(cell[0, 0], ext[0, 1], 3f);   // 確認西邊列也夾

        System.IO.File.Delete(path);
    }

    // 2×1 cells（65×33 px）：相鄰格共用第 32 欄 → 邊緣逐頂點相等（seam）
    [Fact]
    public void SampleCell_SharedEdge_MatchesBetweenNeighbors()
    {
        // 水平漸層，值依 x 變化
        var path = MakePng(65, 33, (x, y) => (ushort)(x * 1000));
        var spec = new HeightmapSpec { Path = path, OriginX = 0, OriginY = 0, MinHeight = 0, MaxHeight = 6000 };

        var hm = Heightmap.Load(spec, System.IO.Path.GetDirectoryName(path)!);
        Assert.Equal(2, hm.CellsX);

        var left = hm.SampleCell(0, 0);    // 取 px 欄 [0..32]
        var right = hm.SampleCell(1, 0);   // 取 px 欄 [32..64]
        for (int row = 0; row < 33; row++)
            Assert.Equal(left[row, 32], right[row, 0], 3);   // 共用第 32 欄
        System.IO.File.Delete(path);
    }
}
