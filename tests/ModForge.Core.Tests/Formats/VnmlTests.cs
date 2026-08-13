using ModForge;
using Xunit;

namespace ModForge.Tests;

public class VnmlTests
{
    // Skyrim VNML 是 signed byte（二補數）：n = (sbyte)b / 127。向上 = (0, 0, 127)。
    private static sbyte S(byte b) => unchecked((sbyte)b);

    // 平地（全等高）→ 所有法線垂直向上 (0, 0, 127)
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
                Assert.Equal(0, n.X);
                Assert.Equal(0, n.Y);
                Assert.Equal(127, n.Z);
            }
    }

    // 均勻東坡（col 增加高度增加）→ X 分量為負（法線朝西傾），Y=0，Z 仍正
    [Fact]
    public void Compute_UniformEastSlope_NormalTiltsWest()
    {
        var h35 = new float[35, 35];
        for (int r = 0; r < 35; r++)
            for (int c = 0; c < 35; c++)
                h35[r, c] = c * 32f;

        var normals = Vnml.Compute(h35);

        var n = normals[16, 16];   // 中心頂點
        Assert.True(S(n.X) < 0, $"signed X={S(n.X)} should be < 0 for eastward slope");
        Assert.Equal(0, S(n.Y));
        Assert.True(S(n.Z) > 0, $"signed Z={S(n.Z)} should be > 0");
    }

    // 均勻北坡（row 增加高度增加）→ Y 分量為負，X=0
    [Fact]
    public void Compute_UniformNorthSlope_NormalTiltsSouth()
    {
        var h35 = new float[35, 35];
        for (int r = 0; r < 35; r++)
            for (int c = 0; c < 35; c++)
                h35[r, c] = r * 32f;

        var normals = Vnml.Compute(h35);

        var n = normals[16, 16];
        Assert.Equal(0, S(n.X));
        Assert.True(S(n.Y) < 0, $"signed Y={S(n.Y)} should be < 0 for northward slope");
        Assert.True(S(n.Z) > 0);
    }

    // 垂直上的 Z = round(1×127) = 127
    [Fact]
    public void Compute_FlatTerrain_ZComponentIs127()
    {
        var h35 = new float[35, 35];
        var normals = Vnml.Compute(h35);
        Assert.Equal((byte)127, normals[0, 0].Z);
    }

    // 均勻 NE 對稱斜坡：X、Y 分量相同（對稱），Z 為正
    [Fact]
    public void Compute_SymmetricDiagonalSlope_XYSymmetric()
    {
        var h35 = new float[35, 35];
        for (int r = 0; r < 35; r++)
            for (int c = 0; c < 35; c++)
                h35[r, c] = (r + c) * 16f;   // 等坡角 NE 方向

        var normals = Vnml.Compute(h35);

        var n = normals[16, 16];
        Assert.Equal(n.X, n.Y);              // 等比例 NE 坡 → X 和 Y 分量相同
        Assert.True(S(n.Z) > 0);
    }
}
