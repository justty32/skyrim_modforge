namespace ModForge;

// Skyrim LAND vertex normals (VNML) 計算。
//
// 法線儲存格式：P3UInt8(X, Y, Z)，Skyrim 座標軸 X=東, Y=北, Z=上。
// 編碼：byte = clamp(round(n × 127) + 128, 0, 255)；垂直向上 = (128, 128, 255)。
//
// 輸入：35×35 高度格（cell 33×33 + 四邊各 1px 邊框，來自 Heightmap.SampleCellExtended）。
// 邊框讓每個頂點都能做中心差分，不需特判邊緣。
public static class Vnml
{
    private const float StepUnits = 8f;   // 相鄰頂點間距（game units）

    /// <summary>
    /// 從 35×35 高度格計算 33×33 頂點法線。
    /// heights35[r, c]：r/c ∈ [0..34]，cell 頂點在 [1..33, 1..33]。
    /// </summary>
    public static Noggog.Array2d<Noggog.P3UInt8> Compute(float[,] heights35)
    {
        var result = new Noggog.Array2d<Noggog.P3UInt8>(33, 33, new Noggog.P3UInt8(128, 128, 255));
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

                result[r, c] = new Noggog.P3UInt8(NToByte(nx), NToByte(ny), NToByte(nz));
            }
        return result;
    }

    private static byte NToByte(float n) =>
        (byte)Math.Clamp((int)MathF.Round(n * 127f) + 128, 0, 255);
}
