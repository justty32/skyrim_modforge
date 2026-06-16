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
// rotation：各軸 radians → degrees（PlacementSpec 存 degrees，generator 再轉回 radians 給 Mutagen）。
public static class GodotPlacements
{
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
                    X = e.Rotation.X * RadToDeg,
                    Y = e.Rotation.Y * RadToDeg,
                    Z = e.Rotation.Z * RadToDeg,
                },
                Scale = e.Scale,
            });
        }
        return result;
    }

    // ── internal deserialization types ──────────────────────────────────────

    private sealed class GodotPlacementsFile
    {
        public int Version { get; set; }

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
