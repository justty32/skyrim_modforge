using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class WorldspaceHeightmapTests
{
    private static ModKey Out => ModKey.FromNameAndExtension("Test.esp");

    private static string MakePng(int w, int h, System.Func<int, int, ushort> f)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mf_ws_{System.Guid.NewGuid():N}.png");
        using var img = new Image<L16>(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) img[x, y] = new L16(f(x, y));
        img.Save(path);
        return path;
    }

    private static WorldspaceSpec World(HeightmapSpec hm) => new()
    {
        EditorId = "HMWorld", Name = "HM", Climate = "Skyrim.esm:0x000812",
        Water = "Skyrim.esm:0x000018", Flags = { "SmallWorld" }, Heightmap = hm,
    };

    // 2×1 PNG → 衍生出 2 格 cell（OriginX..OriginX+1, OriginY）
    [Fact]
    public void Heightmap_DerivesCellGridFromPngSize()
    {
        var path = MakePng(65, 33, (x, y) => 30000);
        var spec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = path, OriginX = 5, OriginY = 7, MinHeight = 0, MaxHeight = 4000 }) } };

        var w = Generator.Build(spec, Out).Mod.Worldspaces.First(x => x.EditorID == "HMWorld");
        var cells = w.SubCells.SelectMany(b => b.Items).SelectMany(s => s.Items).ToList();

        Assert.Equal(2, cells.Count);
        Assert.Contains(cells, c => c.Grid!.Point.X == 5 && c.Grid.Point.Y == 7);
        Assert.Contains(cells, c => c.Grid!.Point.X == 6 && c.Grid.Point.Y == 7);
        System.IO.File.Delete(path);
    }

    // 起伏 PNG → LAND 的 VHGT delta 非全 0（地形真的有起伏）
    [Fact]
    public void Heightmap_ProducesNonZeroVhgtDeltas()
    {
        var path = MakePng(33, 33, (x, y) => (ushort)(y * 2000));   // 南北漸層
        var spec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = path, OriginX = 0, OriginY = 0, MinHeight = 0, MaxHeight = 8000 }) } };

        var w = Generator.Build(spec, Out).Mod.Worldspaces.First(x => x.EditorID == "HMWorld");
        var land = w.SubCells.SelectMany(b => b.Items).SelectMany(s => s.Items).First().Landscape!;

        bool anyNonZero = false;
        var hmArr = land.VertexHeightMap!.HeightMap;
        for (int x = 0; x < 33; x++) for (int y = 0; y < 33; y++)
            if (hmArr[x, y] != 0) anyNonZero = true;
        Assert.True(anyNonZero);
        Assert.Equal(Landscape.Flag.VertexNormalsHeightMap, land.Flags & Landscape.Flag.VertexNormalsHeightMap);
        System.IO.File.Delete(path);
    }

    // 行為不變：heightmap 全等值 PNG 生成的 LAND，與同高度 flat Cells spec 的 VHGT 完全相同
    [Fact]
    public void Heightmap_FlatPng_MatchesFlatCellPath()
    {
        var path = MakePng(33, 33, (x, y) => 65535);   // 全白 → 全 = MaxHeight
        var hmSpec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = path, OriginX = 0, OriginY = 0, MinHeight = 0, MaxHeight = 4000 }) } };
        var flatWs = new WorldspaceSpec { EditorId = "HMWorld", Name = "HM", Climate = "Skyrim.esm:0x000812",
            Water = "Skyrim.esm:0x000018", Flags = { "SmallWorld" } };
        flatWs.Cells.Add(new WorldspaceCellSpec { X = 0, Y = 0, Height = 4000f });
        var flatSpec = new ModSpec { Esl = false, Worldspaces = { flatWs } };

        var hmLand = Generator.Build(hmSpec, Out).Mod.Worldspaces.First().SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items).First().Landscape!;
        var flatLand = Generator.Build(flatSpec, Out).Mod.Worldspaces.First().SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items).First().Landscape!;

        Assert.Equal(flatLand.VertexHeightMap!.Offset, hmLand.VertexHeightMap!.Offset, 3);
        for (int x = 0; x < 33; x++) for (int y = 0; y < 33; y++)
            Assert.Equal(flatLand.VertexHeightMap!.HeightMap[x, y], hmLand.VertexHeightMap!.HeightMap[x, y]);
        System.IO.File.Delete(path);
    }

    // 2×1 cells — 共用邊（PNG col 32）兩側重建高度必須逐頂點完全相同。
    // 此測試在修 seam stitching 前會失敗（兩側獨立 encode，rounding 可差 ±8 units）。
    [Fact]
    public void Heightmap_TwoCells_SeamReconstructedHeightsMatch()
    {
        // 斜坡 PNG (65×33)：從左到右線性升高，最大坡度 ~6 units/vertex
        var path = MakePng(65, 33, (x, y) => (ushort)(x * 1000));
        var spec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = path, OriginX = 0, OriginY = 0, MinHeight = 0, MaxHeight = 8000 }) } };

        var w = Generator.Build(spec, Out).Mod.Worldspaces.First(x => x.EditorID == "HMWorld");
        var cells = w.SubCells.SelectMany(b => b.Items).SelectMany(s => s.Items)
            .OrderBy(c => c.Grid!.Point.X).ToList();
        Assert.Equal(2, cells.Count);

        var vhm0 = cells[0].Landscape!.VertexHeightMap!;
        var vhm1 = cells[1].Landscape!.VertexHeightMap!;
        var h0 = Vhgt.Decode(vhm0.Offset, vhm0.HeightMap);
        var h1 = Vhgt.Decode(vhm1.Offset, vhm1.HeightMap);

        for (int row = 0; row < 33; row++)
            Assert.True(h0[row, 32] == h1[row, 0],
                $"seam row {row}: cell0 east={h0[row,32]} != cell1 west={h1[row,0]}");
        System.IO.File.Delete(path);
    }

    [Fact]
    public void Validate_HeightmapMinNotLessThanMax_IsFlagged()
    {
        var spec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = "x.png", OriginX = 0, OriginY = 0, MinHeight = 5000, MaxHeight = 4000 }) } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("minHeight"));
    }

    [Fact]
    public void Validate_HeightmapEmptyPath_IsFlagged()
    {
        var spec = new ModSpec { Esl = false, Worldspaces =
            { World(new HeightmapSpec { Path = "", MinHeight = 0, MaxHeight = 4000 }) } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("heightmap"));
    }

    [Fact]
    public void Validate_EslWithHeightmap_IsFlagged()
    {
        var ws = World(new HeightmapSpec { Path = "x.png", MinHeight = 0, MaxHeight = 4000 });
        var spec = new ModSpec { Esl = true, Worldspaces = { ws } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("esl"));
    }
}
