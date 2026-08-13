using Noggog;

namespace ModForge;

// VHGT (vertex height map) 編解碼。純函式、無 Mutagen/ImageSharp 依賴 → 離線可單測。
//
// 格式（查證自 Mutagen 0.53.1 + UESP + xEdit，見 specs/worldspace-editor-design.md）：
//  - Offset(float) = 頂點 [0,0] 高度基準；尺度 = ×8 game units。
//  - 33×33 signed-int8 delta，row-wise 累積：第 0 欄沿列往北累積成各列基準，
//    第 1–32 欄沿列內往東累積；每 delta 單位也 = 8 game units。
//  - Mutagen 寫 byte 原值不轉換 → 此處自做二補數（store (byte)(sbyte)d）。
// 索引約定：heights[row, col]（row=北向 0..32, col=東向 0..32）；Array2d 用 [col, row]（x=col, y=row）。
public static class Vhgt
{
    private const float Scale = 8f;
    public const int Size = 33;

    /// <summary>絕對高度網格(game units) → VHGT offset + 33×33 二補數 delta byte。</summary>
    public static (float Offset, Array2d<byte> HeightMap) Encode(
        float[,] heights, System.Action<string>? warn = null, string cellLabel = "")
    {
        var deltas = new Array2d<byte>(Size, Size, 0);
        bool clamped = false;

        float offset = heights[0, 0] / Scale;
        float col0 = offset;          // 第 0 欄累積值（offset 單位）

        for (int r = 0; r < Size; r++)
        {
            if (r > 0)
            {
                float target = heights[r, 0] / Scale;
                int d = Clamp((int)System.Math.Round(target - col0), ref clamped);
                deltas[0, r] = unchecked((byte)(sbyte)d);
                col0 += d;            // 用實際重建值累積
            }
            float rowVal = col0;
            for (int c = 1; c < Size; c++)
            {
                float target = heights[r, c] / Scale;
                int d = Clamp((int)System.Math.Round(target - rowVal), ref clamped);
                deltas[c, r] = unchecked((byte)(sbyte)d);
                rowVal += d;
            }
        }

        if (clamped)
            warn?.Invoke($"  ! heightmap {cellLabel}: 地形過陡，VHGT delta 已 clamp 到 ±127（單步上限 1016 units）—高度差會被壓平");

        return (offset, deltas);
    }

    /// <summary>VHGT offset + delta → 絕對高度網格(game units)。Encode 的逆，供測試與文件。</summary>
    public static float[,] Decode(float offset, IReadOnlyArray2d<byte> deltas)
    {
        var h = new float[Size, Size];
        float col0 = offset;
        for (int r = 0; r < Size; r++)
        {
            if (r > 0) col0 += (sbyte)deltas[0, r];
            float rowVal = col0;
            h[r, 0] = rowVal * Scale;
            for (int c = 1; c < Size; c++)
            {
                rowVal += (sbyte)deltas[c, r];
                h[r, c] = rowVal * Scale;
            }
        }
        return h;
    }

    private static int Clamp(int d, ref bool clamped)
    {
        if (d < -128) { clamped = true; return -128; }
        if (d > 127) { clamped = true; return 127; }
        return d;
    }
}
