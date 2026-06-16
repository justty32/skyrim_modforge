using ModForge;
using Xunit;

namespace ModForge.Tests;

public class VnmlTests
{
    // 平地（全等高）→ 所有法線垂直向上 (128, 128, 255)
    [Fact]
    public void Compute_FlatTerrain_AllNormalsUp()
    {
        var h35 = new float[35, 35];
        for (int r = 0; r < 35; r++) for (int c = 0; c < 35; c++) h35[r, c] = 4000f;

        var normals = Vnml.Compute(h35);

        for (int r = 0; r < 33; r++)
            for (int c = 0; c < 33; c++)
            {
                var n = normals[r, c];
                Assert.Equal(128, n.X);
                Assert.Equal(128, n.Y);
                Assert.Equal(255, n.Z);
            }
    }

    // 均勻東坡（col 增加高度增加）→ X 分量 < 128（法線朝西傾），Z 分量減少，Y 分量維持 128
    [Fact]
    public void Compute_UniformEastSlope_NormalTiltsWest()
    {
        var h35 = new float[35, 35];
        for (int r = 0; r < 35; r++)
            for (int c = 0; c < 35; c++)
                h35[r, c] = c * 32f;   // 每步 32 game units（4 倍一般 delta 上限，坡度明顯）

        var normals = Vnml.Compute(h35);

        // 東坡：法線 X < 128（朝西），Y = 128（無南北分量），Z > 128（仍朝上）
        var n = normals[16, 16];   // 中心頂點，遠離 clamp 邊界
        Assert.True(n.X < 128, $"X={n.X} should be < 128 for eastward slope");
        Assert.Equal(128, n.Y);
        Assert.True(n.Z > 128, $"Z={n.Z} should be > 128");
    }

    // 均勻北坡（row 增加高度增加）→ Y 分量 < 128，X 分量維持 128
    [Fact]
    public void Compute_UniformNorthSlope_NormalTiltsSouth()
    {
        var h35 = new float[35, 35];
        for (int r = 0; r < 35; r++)
            for (int c = 0; c < 35; c++)
                h35[r, c] = r * 32f;

        var normals = Vnml.Compute(h35);

        var n = normals[16, 16];
        Assert.Equal(128, n.X);
        Assert.True(n.Y < 128, $"Y={n.Y} should be < 128 for northward slope");
        Assert.True(n.Z > 128);
    }

    // 法線長度應接近 1（編碼後 Z 分量可用來反推）；垂直上的 Z=255 ≈ round(1*127)+128=255
    [Fact]
    public void Compute_FlatTerrain_ZComponentIs255()
    {
        var h35 = new float[35, 35];
        var normals = Vnml.Compute(h35);
        Assert.Equal((byte)255, normals[0, 0].Z);
    }

    // 均勻東西對稱斜坡：法線 X 對稱，Y=128
    [Fact]
    public void Compute_SymmetricDiagonalSlope_XYSymmetric()
    {
        var h35 = new float[35, 35];
        for (int r = 0; r < 35; r++)
            for (int c = 0; c < 35; c++)
                h35[r, c] = (r + c) * 16f;   // 等坡角 NE 方向

        var normals = Vnml.Compute(h35);

        var n = normals[16, 16];
        // 等比例 NE 坡 → X 和 Y 分量相同（對稱）
        Assert.Equal(n.X, n.Y);
        Assert.True(n.Z > 128);
    }
}
