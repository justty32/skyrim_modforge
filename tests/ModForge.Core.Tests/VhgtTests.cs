using ModForge;
using Xunit;

namespace ModForge.Tests;

public class VhgtTests
{
    // 33×33 全等高 → offset=H/8、所有 delta=0（與現有 flat LAND 位元一致）
    [Fact]
    public void Encode_FlatGrid_AllDeltasZero()
    {
        var h = new float[33, 33];
        for (int r = 0; r < 33; r++) for (int c = 0; c < 33; c++) h[r, c] = 4000f;

        var (offset, deltas) = Vhgt.Encode(h);

        Assert.Equal(500f, offset);            // 4000/8
        for (int x = 0; x < 33; x++) for (int y = 0; y < 33; y++)
            Assert.Equal((byte)0, deltas[x, y]);
    }

    // 任意平滑網格：Encode→Decode 重建，誤差 ≤ 半 delta 單位（±4 game units）
    [Fact]
    public void EncodeDecode_RoundTrip_WithinHalfStep()
    {
        var h = new float[33, 33];
        for (int r = 0; r < 33; r++) for (int c = 0; c < 33; c++)
            h[r, c] = 3000f + r * 24f + c * 16f;   // 平緩斜面，遠在 ±1016/step 內

        var (offset, deltas) = Vhgt.Encode(h);
        var back = Vhgt.Decode(offset, deltas);

        for (int r = 0; r < 33; r++) for (int c = 0; c < 33; c++)
            Assert.True(System.Math.Abs(back[r, c] - h[r, c]) <= 4f,
                $"[{r},{c}] {back[r, c]} vs {h[r, c]}");
    }

    // 超陡（單步 > 1016 units）→ clamp 且呼叫 warn
    [Fact]
    public void Encode_TooSteep_ClampsAndWarns()
    {
        var h = new float[33, 33];
        for (int r = 0; r < 33; r++) for (int c = 0; c < 33; c++) h[r, c] = 0f;
        h[0, 1] = 5000f;   // 一步 +5000 units 遠超 1016

        bool warned = false;
        Vhgt.Encode(h, _ => warned = true, "cell(0,0)");

        Assert.True(warned);
    }
}
