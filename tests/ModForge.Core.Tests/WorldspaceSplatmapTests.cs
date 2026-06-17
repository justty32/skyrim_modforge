using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Per-vertex alpha texture blend (WorldspaceSpec.TextureLayers → ATXT+VTXT alpha layers).
// Two halves: (1) pure Vtxt.BuildLayers unit tests over a hand-built 33×33 alpha grid — no PNG,
// exercises quadrant split + position encoding + sparsity; (2) an end-to-end Build with a generated
// 8-bit PNG splatmap. Byte-level parity vs vanilla LAND (VTXT position order, per-quadrant layer
// packing) is a separate xEdit check on the main machine — see WAIT_USER.
public class WorldspaceSplatmapTests
{
    private static readonly FormKey Tex = FormKey.Factory("000C16:Skyrim.esm");

    private static float[,] Zero33() => new float[33, 33];

    // ---- Pure Vtxt.BuildLayers ----------------------------------------------------------------

    [Fact]
    public void Build_SouthWestCorner_OnlyBottomLeft_Position0()
    {
        var a = Zero33();
        a[0, 0] = 1f;   // row0=south, col0=west → BottomLeft local (0,0)
        var layers = Vtxt.BuildLayers(a, Tex, 1).ToList();

        var layer = Assert.Single(layers);
        Assert.Equal(Quadrant.BottomLeft, layer.Header.Quadrant);
        Assert.Equal(1, layer.Header.LayerNumber);
        Assert.Equal(0x000C16u, layer.Header.Texture.FormKey.ID);
        var pt = Assert.Single(layer.AlphaLayerData);
        Assert.Equal(0, pt.Position);
        Assert.Equal(1f, pt.Opacity);
    }

    [Fact]
    public void Build_CentreVertex_SharedByAllFourQuadrants()
    {
        var a = Zero33();
        a[16, 16] = 1f;   // shared centre vertex
        var byQuad = Vtxt.BuildLayers(a, Tex, 1).ToDictionary(l => l.Header.Quadrant);

        Assert.Equal(4, byQuad.Count);
        // local position of the centre vertex within each quadrant (pos = localRow*17 + localCol)
        Assert.Equal(16 * 17 + 16, Assert.Single(byQuad[Quadrant.BottomLeft].AlphaLayerData).Position);
        Assert.Equal(16 * 17 + 0,  Assert.Single(byQuad[Quadrant.BottomRight].AlphaLayerData).Position);
        Assert.Equal(0 * 17 + 16,  Assert.Single(byQuad[Quadrant.TopLeft].AlphaLayerData).Position);
        Assert.Equal(0 * 17 + 0,   Assert.Single(byQuad[Quadrant.TopRight].AlphaLayerData).Position);
    }

    [Fact]
    public void Build_IsSparse_AndClampsOpacity()
    {
        var a = Zero33();
        a[1, 2] = 0.5f;
        a[3, 4] = 2f;     // > 1 → clamped to 1
        var layer = Assert.Single(Vtxt.BuildLayers(a, Tex, 1).ToList());   // both in BottomLeft

        Assert.Equal(Quadrant.BottomLeft, layer.Header.Quadrant);
        Assert.Equal(2, layer.AlphaLayerData.Count);   // only the 2 non-zero vertices
        Assert.Equal(0.5f, layer.AlphaLayerData.Single(p => p.Position == 1 * 17 + 2).Opacity);
        Assert.Equal(1f,   layer.AlphaLayerData.Single(p => p.Position == 3 * 17 + 4).Opacity);
    }

    [Fact]
    public void Build_AllOnes_FourFullQuadrants()
    {
        var a = new float[33, 33];
        for (int r = 0; r < 33; r++) for (int c = 0; c < 33; c++) a[r, c] = 1f;
        var layers = Vtxt.BuildLayers(a, Tex, 1).ToList();

        Assert.Equal(4, layers.Count);
        Assert.All(layers, l => Assert.Equal(17 * 17, l.AlphaLayerData.Count));
    }

    // ---- End-to-end Build with a PNG splatmap -------------------------------------------------

    private static string MakeL8Png(int w, int h, System.Func<int, int, byte> f)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mf_splat_{System.Guid.NewGuid():N}.png");
        using var img = new Image<L8>(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) img[x, y] = new L8(f(x, y));
        img.Save(path);
        return path;
    }

    private static ILandscapeGetter BuildLand(WorldspaceSpec ws)
    {
        var result = Generator.Build(new ModSpec { Esl = false, Worldspaces = { ws } },
            ModKey.FromNameAndExtension("Test.esp"));
        return result.Mod.Worldspaces.First().SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items).First().Landscape!;
    }

    [Fact]
    public void TextureLayer_UniformSplatmap_StampsAlphaLayerPerQuadrant()
    {
        var path = MakeL8Png(33, 33, (x, y) => 128);   // uniform alpha ≈ 0.502 everywhere
        var ws = new WorldspaceSpec
        {
            EditorId = "SplatWorld", Name = "Splat", Climate = "Skyrim.esm:0x000812",
            Flags = { "SmallWorld" },
            BaseTexture = "Skyrim.esm:0x000C16",
            Cells = { new WorldspaceCellSpec { X = 0, Y = 0, Height = 4000f } },
            TextureLayers =
            {
                new TerrainTextureLayerSpec
                {
                    Texture = "Skyrim.esm:0x0008C5",
                    Splatmap = new SplatmapSpec { Path = path, OriginX = 0, OriginY = 0 },
                },
            },
        };

        var land = BuildLand(ws);

        // 4 base (BTXT, layer 0) + 4 alpha (ATXT/VTXT, layer 1). NOTE: AlphaLayer derives from
        // BaseLayer in Mutagen, so a plain BTXT layer = BaseLayer that is NOT an AlphaLayer.
        var alphaLayers = land.Layers.OfType<IAlphaLayerGetter>().ToList();
        var baseLayers = land.Layers.Where(l => l is not IAlphaLayerGetter).ToList();
        Assert.Equal(4, baseLayers.Count);
        Assert.Equal(4, alphaLayers.Count);
        Assert.All(alphaLayers, l =>
        {
            Assert.Equal(1, l.Header.LayerNumber);
            Assert.Equal(0x0008C5u, l.Header.Texture.FormKey.ID);
            Assert.Equal(17 * 17, l.AlphaLayerData.Count);     // full quadrant covered
            Assert.All(l.AlphaLayerData, p => Assert.Equal(128f / 255f, p.Opacity, 4));
        });
        System.IO.File.Delete(path);
    }

    [Fact]
    public void TextureLayer_CellOutsideSplatmap_GetsNoAlphaLayer()
    {
        var path = MakeL8Png(33, 33, (x, y) => 255);   // covers only cell (0,0)
        var ws = new WorldspaceSpec
        {
            EditorId = "SplatWorld2", Name = "Splat2", Climate = "Skyrim.esm:0x000812",
            Flags = { "SmallWorld" },
            Cells =
            {
                new WorldspaceCellSpec { X = 0, Y = 0, Height = 4000f },
                new WorldspaceCellSpec { X = 5, Y = 5, Height = 4000f },   // outside the splatmap
            },
            TextureLayers =
            {
                new TerrainTextureLayerSpec
                {
                    Texture = "Skyrim.esm:0x0008C5",
                    Splatmap = new SplatmapSpec { Path = path, OriginX = 0, OriginY = 0 },
                },
            },
        };

        var result = Generator.Build(new ModSpec { Esl = false, Worldspaces = { ws } },
            ModKey.FromNameAndExtension("Test.esp"));
        var cells = result.Mod.Worldspaces.First().SubCells
            .SelectMany(b => b.Items).SelectMany(s => s.Items).ToDictionary(c => (c.Grid!.Point.X, c.Grid.Point.Y));

        Assert.Equal(4, cells[(0, 0)].Landscape!.Layers.OfType<IAlphaLayerGetter>().Count());
        Assert.Empty(cells[(5, 5)].Landscape!.Layers.OfType<IAlphaLayerGetter>());
        System.IO.File.Delete(path);
    }
}
