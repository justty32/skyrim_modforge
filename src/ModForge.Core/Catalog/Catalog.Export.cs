using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace ModForge;

/// <summary>Versioned scene-capture catalog consumed by the runtime browser.</summary>
public sealed record SceneCatalogDocument(
    int SchemaVersion,
    IReadOnlyList<CatalogSource> Sources,
    IReadOnlyList<CatalogRecord> Records);

public static partial class Catalog
{
    private static readonly string[] ScenePlaceableRecordTypes =
    [
        "Static", "MoveableStatic", "StaticCollection", "Tree", "Flora", "Furniture",
        "Activator", "TalkingActivator", "Door", "Container", "Light", "MiscItem",
        "Weapon", "Armor", "Ammunition", "Book", "Ingestible", "Ingredient",
        "SoulGem", "Key", "Scroll",
    ];

    private static readonly JsonSerializerOptions ExportJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>Writes only the winning occurrence of each FormKey as deterministic JSON.</summary>
    public static SceneCatalogDocument ExportJsonFile(
        string databasePath, string outputPath, bool placeableOnly = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        databasePath = Path.GetFullPath(databasePath);
        outputPath = SafeJsonOutputPath(outputPath);
        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (string.Equals(databasePath, outputPath, pathComparison))
            throw new ArgumentException("catalog JSON output must not overwrite its input database", nameof(outputPath));

        var document = new SceneCatalogDocument(
            1, Sources(databasePath), WinnerRecords(databasePath, placeableOnly));
        var tempPath = outputPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, document, ExportJson);
                stream.Flush(flushToDisk: true);
            }
            File.Move(tempPath, outputPath, overwrite: true);
            return document;
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static IReadOnlyList<CatalogRecord> WinnerRecords(
        string databasePath, bool placeableOnly)
    {
        using var connection = Open(databasePath, SqliteOpenMode.ReadOnly);
        RequireCurrentSchema(connection);
        using var command = connection.CreateCommand();
        var typeFilter = placeableOnly
            ? "AND record_type IN (" + string.Join(", ",
                ScenePlaceableRecordTypes.Select((_, index) => $"$type{index}")) + ")"
            : string.Empty;
        command.CommandText = $"""
            WITH ranked AS (
                SELECT r.form_key, r.plugin, r.record_type, r.editor_id, r.name, r.model_path,
                       s.plugin AS source_plugin, s.source_path,
                       ROW_NUMBER() OVER (
                           PARTITION BY r.form_key COLLATE NOCASE
                           ORDER BY s.load_order_index DESC
                       ) AS winner_rank
                FROM records AS r
                JOIN sources AS s ON s.plugin = r.source_plugin
            )
            SELECT form_key, plugin, record_type, editor_id, name, model_path, source_plugin, source_path
            FROM ranked
            WHERE winner_rank = 1
              {typeFilter}
            ORDER BY form_key COLLATE NOCASE;
            """;
        if (placeableOnly)
            for (var index = 0; index < ScenePlaceableRecordTypes.Length; index++)
                command.Parameters.AddWithValue($"$type{index}", ScenePlaceableRecordTypes[index]);
        return ReadRecords(command);
    }

    private static string SafeJsonOutputPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath))
            throw new IOException($"catalog JSON output is a directory: {fullPath}");
        SafeOutputPath.RejectReparsePoints(fullPath, "catalog JSON output");
        return fullPath;
    }
}
