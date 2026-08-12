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

        if (file.Placements is null)
            throw Invalid(path, "placements must be an array, not null");

        var result = new List<PlacementSpec>(file.Placements.Count);
        var instanceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < file.Placements.Count; i++)
        {
            var e = file.Placements[i]
                ?? throw Invalid(path, $"placements[{i}] must be an object, not null");
            if (string.IsNullOrWhiteSpace(e.Base))
                throw Invalid(path, $"placements[{i}].base must be a non-empty form ref or editorId");
            if (e.Position is null)
                throw Invalid(path, $"placements[{i}].position must be an object, not null");
            if (e.Rotation is null)
                throw Invalid(path, $"placements[{i}].rotation must be an object, not null");
            ValidateVec3(path, i, "position", e.Position);
            ValidateVec3(path, i, "rotation", e.Rotation);
            if (e.Scale is not { } scale || !float.IsFinite(scale) || scale <= 0f)
                throw Invalid(path, $"placements[{i}].scale must be present, finite, and > 0" +
                    (e.Scale is { } actual ? $" (got {actual})" : ""));
            if (!string.IsNullOrWhiteSpace(e.InstanceId) && !instanceIds.Add(e.InstanceId))
                throw Invalid(path, $"placements[{i}].instanceId '{e.InstanceId}' is duplicated");

            var position = new Vec3
            {
                X = baseX + e.Position.X!.Value / MetersPerUnit,
                Y = baseY - e.Position.Z!.Value / MetersPerUnit,
                Z =         e.Position.Y!.Value / MetersPerUnit,
            };
            var rotation = new Vec3
            {
                X =  e.Rotation.X!.Value * RadToDeg,
                Y = -e.Rotation.Z!.Value * RadToDeg,
                Z =  e.Rotation.Y!.Value * RadToDeg,
            };
            if (!Finite(position))
                throw Invalid(path, $"placements[{i}].position overflows during Skyrim coordinate conversion");
            if (!Finite(rotation))
                throw Invalid(path, $"placements[{i}].rotation overflows during degree conversion");

            result.Add(new PlacementSpec
            {
                Base       = e.Base,
                EditorId   = e.InstanceId ?? "",
                Worldspace = worldspaceEditorId,
                Position   = position,
                Rotation   = rotation,
                Scale = scale,
            });
        }
        return result;
    }

    private static InvalidDataException Invalid(string path, string detail) =>
        new($"godotPlacements '{path}': {detail}");

    private static void ValidateVec3(string path, int index, string field, GodotVec3 value)
    {
        if (value.X is not { } x || value.Y is not { } y || value.Z is not { } z)
            throw Invalid(path, $"placements[{index}].{field} must contain numeric x, y, and z");
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z))
            throw Invalid(path, $"placements[{index}].{field} x, y, and z must be finite");
    }

    private static bool Finite(Vec3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    // ── internal deserialization types ──────────────────────────────────────

    private sealed class GodotPlacementsFile
    {
        public int? Version { get; set; }

        [JsonPropertyName("coordinate_system")]
        public string CoordinateSystem { get; set; } = "";

        public List<GodotEntry?>? Placements { get; set; }
    }

    private sealed class GodotEntry
    {
        public string? Base { get; set; } = "";
        public string? InstanceId { get; set; }
        public GodotVec3? Position { get; set; }
        public GodotVec3? Rotation { get; set; }
        public float? Scale { get; set; }
    }

    private sealed class GodotVec3
    {
        public float? X { get; set; }
        public float? Y { get; set; }
        public float? Z { get; set; }
    }
}
