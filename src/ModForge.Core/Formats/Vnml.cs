namespace ModForge;

// Skyrim LAND vertex normals (VNML) 計算。
//
// 法線儲存格式：P3UInt8(X, Y, Z)，Skyrim 座標軸 X=東, Y=北, Z=上。
// 編碼：**signed byte**（二補數），byte = (sbyte)round(n × 127)，範圍 −127..127。
// 垂直向上 = (0, 0, 127)（驗證自 vanilla Tamriel 平地 LAND，VhgtTests.VnmlCompute_OrientationMatchesVanilla）。
// 注意：曾誤用 +128 偏移（→ up=(128,128,255)），與引擎不符會造成全域光影錯誤。
//
// 輸入：35×35 高度格（cell 33×33 + 四邊各 1px 邊框，來自 Heightmap.SampleCellExtended）。
// 邊框讓每個頂點都能做中心差分，不需特判邊緣。
public static class Vnml
{
    // 相鄰 LAND 頂點的水平間距：cell 4096 units / 32 段 = 128 game units。
    // （注意：別跟 VHGT 的高度 delta 尺度 ×8 搞混——那是 Z 軸，這是 X/Y 軸。）
    private const float StepUnits = 128f;

    /// <summary>
    /// 從 35×35 高度格計算 33×33 頂點法線。
    /// heights35[r, c]：r/c ∈ [0..34]，cell 頂點在 [1..33, 1..33]。
    /// </summary>
    public static Noggog.Array2d<Noggog.P3UInt8> Compute(float[,] heights35)
    {
        var result = new Noggog.Array2d<Noggog.P3UInt8>(33, 33, new Noggog.P3UInt8(0, 0, 127));
        for (int r = 0; r < 33; r++)
            for (int c = 0; c < 33; c++)
            {
                // Cell vertex (r, c) = heights35[r+1, c+1].
                // Central difference over 2 steps (16 units each axis).
                float dhEast  = heights35[r + 1, c + 2] - heights35[r + 1, c];
                float dhNorth = heights35[r + 2, c + 1] - heights35[r,     c + 1];

                // Normal = E × N where E = (2step, 0, dhEast), N = (0, 2step, dhNorth).
                // = (-dhEast × 2step, -dhNorth × 2step, (2step)²) → simplified before normalize:
                float nx = -dhEast;
                float ny = -dhNorth;
                float nz = 2f * StepUnits;

                float len = MathF.Sqrt(nx * nx + ny * ny + nz * nz);
                if (len > 0f) { nx /= len; ny /= len; nz /= len; }

                // Store [col=East, row=North] to match VHGT's Array2d convention (x=col, y=row).
                // result[r, c] would transpose the grid relative to VertexHeightMap → wrong lighting.
                result[c, r] = new Noggog.P3UInt8(NToByte(nx), NToByte(ny), NToByte(nz));
            }
        return result;
    }

    // n ∈ [−1,1] → signed byte（二補數）。round(n×127) ∈ [−127,127]，無偏移。
    private static byte NToByte(float n) =>
        unchecked((byte)(sbyte)Math.Clamp((int)MathF.Round(n * 127f), -127, 127));
}
