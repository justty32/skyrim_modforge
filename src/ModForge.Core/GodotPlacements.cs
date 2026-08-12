using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModForge;

// Godot 4 placements JSON → List<PlacementSpec> 轉換器。
//
// 座標換算（godot4_y_up：X=東、Y=上、Z=南）→ Skyrim（X=東、Y=北、Z=上）：
//   skyrim_x = OriginX×4096 + godot_x / k
//   skyrim_y = OriginY×4096 − godot_z / k   （Godot +Z 朝南，Skyrim +Y 朝北，方向相反）
//   skyrim_z =               godot_y / k
//   k = 0.014286 m/unit（社群共識；1 unit ≈ 1.4286cm，player 高度 128 units ≈ 1.8m）
// rotation：rad → deg，且軸要跟著座標系換（用與 position 同一個基底變換 M = Rx(90°)）。
//   座標換軸 M 把 (gx,gy,gz)→(gx,−gz,gy)；旋轉軸經同一個 M 共軛後得到同樣的 per-axis 對應：
//     skyrim_rotX =  godot_rotX        （東軸不變）
//     skyrim_rotY = −godot_rotZ        （Godot 繞 +Z〔南〕→ Skyrim 繞 −Y〔北〕，角度反號）
//     skyrim_rotZ =  godot_rotY        （Godot 繞 +Y〔上〕yaw → Skyrim 繞 +Z〔上〕yaw；Skyrim RotZ=heading）
//   單軸旋轉（編輯器的主要情境，多為 yaw）下完全正確；複合 Euler 因兩邊 Euler 套用順序不同會有殘差，
//   待主力機實機對朝向校準（見 wait_todo/worldspace-editor.md「rotation 軸對應」）。
public static class GodotPlacements
{
    private const int SupportedFormatVersion = 1;
    private const float MetersPerUnit = 0.014286f;
    private const float RadToDeg = 180f / MathF.PI;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// 載入 Godot placements JSON，換算座標系後回傳 <see cref="PlacementSpec"/> 清單。
    /// </summary>
    /// <param name="spec">spec 中的 godotPlacements 設定。</param>
    /// <param name="specDir">spec 檔所在資料夾（用於解析相對路徑）。</param>
    /// <param name="worldspaceEditorId">目標 worldspace 的 editorId（填進每個 PlacementSpec.Worldspace）。</param>
    public static List<PlacementSpec> Load(
        GodotPlacementsSpec spec, string specDir, string worldspaceEditorId)
    {
        var path = System.IO.Path.IsPathRooted(spec.Path)
            ? spec.Path
            : System.IO.Path.Combine(specDir, spec.Path);

        if (!System.IO.File.Exists(path))
            throw new System.IO.FileNotFoundException($"godotPlacements JSON not found: {path}");

        var file = JsonSerializer.Deserialize<GodotPlacementsFile>(
            System.IO.File.ReadAllText(path), JsonOpts)
            ?? throw new System.InvalidOperationException($"Failed to parse godotPlacements: {path}");

        if (file.Version != SupportedFormatVersion)
        {
            var actualVersion = file.Version?.ToString() ?? "<missing>";
            throw new System.NotSupportedException(
                $"godotPlacements '{path}': unsupported format version '{actualVersion}'" +
                $" (only version {SupportedFormatVersion} is supported)");
        }

        if (file.CoordinateSystem != "godot4_y_up")
            throw new System.NotSupportedException(
                $"godotPlacements '{path}': unsupported coordinate_system '{file.CoordinateSystem}'" +
                " (only 'godot4_y_up' is supported)");

        float baseX = spec.OriginX * 4096f;
        float baseY = spec.OriginY * 4096f;

        var result = new List<PlacementSpec>(file.Placements.Count);
        foreach (var e in file.Placements)
        {
            result.Add(new PlacementSpec
            {
                Base       = e.Base,
                EditorId   = e.InstanceId ?? "",
                Worldspace = worldspaceEditorId,
                Position   = new Vec3
                {
                    X = baseX + e.Position.X / MetersPerUnit,
                    Y = baseY - e.Position.Z / MetersPerUnit,   // Godot +Z = south → flip
                    Z =         e.Position.Y / MetersPerUnit,
                },
                Rotation = new Vec3
                {
                    X =  e.Rotation.X * RadToDeg,   // east axis unchanged
                    Y = -e.Rotation.Z * RadToDeg,   // Godot +Z(south) → Skyrim −Y(north)
                    Z =  e.Rotation.Y * RadToDeg,   // Godot +Y(up) yaw → Skyrim +Z(up) yaw
                },
                Scale = e.Scale,
            });
        }
        return result;
    }

    // ── internal deserialization types ──────────────────────────────────────

    private sealed class GodotPlacementsFile
    {
        public int? Version { get; set; }

        [JsonPropertyName("coordinate_system")]
        public string CoordinateSystem { get; set; } = "";

        public List<GodotEntry> Placements { get; set; } = new();
    }

    private sealed class GodotEntry
    {
        public string Base { get; set; } = "";
        public string? InstanceId { get; set; }
        public GodotVec3 Position { get; set; } = new();
        public GodotVec3 Rotation { get; set; } = new();
        public float Scale { get; set; } = 1f;
    }

    private sealed class GodotVec3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}
