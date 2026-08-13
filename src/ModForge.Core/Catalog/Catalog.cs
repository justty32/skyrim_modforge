using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

/// <summary>One source plugin recorded in a catalog, including reproducible file provenance.</summary>
public sealed record CatalogSource(
    string Plugin,
    string SourcePath,
    string Sha256,
    bool Localized,
    int RecordCount,
    int LoadOrderIndex = -1);

/// <summary>One generic major record returned from the offline catalog.</summary>
public sealed record CatalogRecord(
    string FormKey,
    string Plugin,
    string RecordType,
    string? EditorId,
    string? Name,
    string SourcePlugin,
    string SourcePath,
    string? ModelPath = null);

/// <summary>Summary emitted after replacing a catalog database.</summary>
public sealed record CatalogBuildResult(int SourceCount, int RecordCount);

/// <summary>
/// Offline SQLite/FTS catalog for generic Skyrim plugin records. The deliberately small schema is
/// the stable boundary: record-specific extractors can later add separate tables keyed by records.id.
/// </summary>
public static partial class Catalog
{
    /// <summary>Build a new catalog and atomically replace <paramref name="databasePath"/> on success.</summary>
    public static CatalogBuildResult Build(string databasePath, IEnumerable<string> pluginPaths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(pluginPaths);

        var sources = pluginPaths
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (sources.Length == 0) throw new ArgumentException("catalog build needs at least one plugin", nameof(pluginPaths));
        foreach (var path in sources)
            if (!File.Exists(path)) throw new FileNotFoundException("plugin not found", path);

        databasePath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        var tempPath = databasePath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            int recordCount;
            using (var connection = Open(tempPath, SqliteOpenMode.ReadWriteCreate))
            {
                CreateSchema(connection);
                using var transaction = connection.BeginTransaction();
                var seenPlugins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                recordCount = 0;
                for (var loadOrderIndex = 0; loadOrderIndex < sources.Length; loadOrderIndex++)
                {
                    var sourcePath = sources[loadOrderIndex];
                    using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(sourcePath), SkyrimRelease.SkyrimSE);
                    var sourcePlugin = mod.ModKey.FileName;
                    if (!seenPlugins.Add(sourcePlugin))
                        throw new InvalidOperationException($"catalog sources have the same plugin name: {sourcePlugin}");

                    var sourceRecords = InsertSource(connection, transaction, sourcePlugin, sourcePath,
                        HashFile(sourcePath), mod.UsingLocalization, loadOrderIndex);
                    foreach (var record in mod.EnumerateMajorRecords())
                    {
                        InsertRecord(connection, transaction, record, sourcePlugin);
                        sourceRecords++;
                        recordCount++;
                    }
                    UpdateSourceCount(connection, transaction, sourcePlugin, sourceRecords);
                }

                using (var fts = connection.CreateCommand())
                {
                    fts.Transaction = transaction;
                    fts.CommandText = "INSERT INTO records_fts(rowid, name, editor_id) " +
                        "SELECT id, COALESCE(name, ''), COALESCE(editor_id, '') FROM records ORDER BY id;";
                    fts.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            File.Move(tempPath, databasePath, overwrite: true);
            return new CatalogBuildResult(sources.Length, recordCount);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    /// <summary>Searches FTS-indexed display names and EditorIDs, with optional exact type/source filters.</summary>
    public static IReadOnlyList<CatalogRecord> Query(
        string databasePath, string query, string? recordType = null, string? sourcePlugin = null, int limit = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit), "limit must be 1 through 1000");

        using var connection = Open(Path.GetFullPath(databasePath), SqliteOpenMode.ReadOnly);
        RequireCurrentSchema(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.form_key, r.plugin, r.record_type, r.editor_id, r.name, r.model_path, s.plugin, s.source_path
            FROM records_fts AS f
            JOIN records AS r ON r.id = f.rowid
            JOIN sources AS s ON s.plugin = r.source_plugin
            WHERE records_fts MATCH $query
              AND ($type IS NULL OR r.record_type = $type COLLATE NOCASE)
              AND ($plugin IS NULL OR s.plugin = $plugin COLLATE NOCASE)
            ORDER BY r.plugin COLLATE NOCASE, r.form_key COLLATE NOCASE
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$query", query);
        command.Parameters.AddWithValue("$type", (object?)recordType ?? DBNull.Value);
        command.Parameters.AddWithValue("$plugin", (object?)sourcePlugin ?? DBNull.Value);
        command.Parameters.AddWithValue("$limit", limit);

        return ReadRecords(command);
    }

    /// <summary>Looks up every indexed occurrence of an exact resolver-ready FormKey.</summary>
    public static IReadOnlyList<CatalogRecord> Get(
        string databasePath, string formKey, string? sourcePlugin = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formKey);
        using var connection = Open(Path.GetFullPath(databasePath), SqliteOpenMode.ReadOnly);
        RequireCurrentSchema(connection);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.form_key, r.plugin, r.record_type, r.editor_id, r.name, r.model_path, s.plugin, s.source_path
            FROM records AS r
            JOIN sources AS s ON s.plugin = r.source_plugin
            WHERE r.form_key = $formKey COLLATE NOCASE
              AND ($plugin IS NULL OR s.plugin = $plugin COLLATE NOCASE)
            ORDER BY s.plugin COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$formKey", formKey);
        command.Parameters.AddWithValue("$plugin", (object?)sourcePlugin ?? DBNull.Value);
        return ReadRecords(command);
    }

    /// <summary>Lists source files and hashes recorded for provenance.</summary>
    public static IReadOnlyList<CatalogSource> Sources(string databasePath)
    {
        using var connection = Open(Path.GetFullPath(databasePath), SqliteOpenMode.ReadOnly);
        RequireCurrentSchema(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT plugin, source_path, sha256, localized, record_count, load_order_index FROM sources ORDER BY load_order_index;";
        using var reader = command.ExecuteReader();
        var sources = new List<CatalogSource>();
        while (reader.Read())
            sources.Add(new CatalogSource(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetBoolean(3), reader.GetInt32(4), reader.GetInt32(5)));
        return sources;
    }

    private static SqliteConnection Open(string path, SqliteOpenMode mode)
    {
        // A build renames its temporary DB immediately after disposing the connection. Disable
        // pooling so Windows does not retain that temp file's handle for a later pooled connection.
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            Pooling = false,
        }.ToString());
        connection.Open();
        return connection;
    }

    private static IReadOnlyList<CatalogRecord> ReadRecords(SqliteCommand command)
    {
        var records = new List<CatalogRecord>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            records.Add(new CatalogRecord(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetString(6), reader.GetString(7), reader.IsDBNull(5) ? null : reader.GetString(5)));
        return records;
    }

    private static int InsertSource(SqliteConnection connection, SqliteTransaction transaction, string plugin,
        string sourcePath, string sha256, bool localized, int loadOrderIndex)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO sources(plugin, source_path, sha256, localized, record_count, load_order_index) VALUES($plugin, $path, $sha, $localized, 0, $index);";
        command.Parameters.AddWithValue("$plugin", plugin);
        command.Parameters.AddWithValue("$path", sourcePath);
        command.Parameters.AddWithValue("$sha", sha256);
        command.Parameters.AddWithValue("$localized", localized);
        command.Parameters.AddWithValue("$index", loadOrderIndex);
        command.ExecuteNonQuery();
        return 0;
    }

    private static void UpdateSourceCount(SqliteConnection connection, SqliteTransaction transaction, string plugin, int count)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE sources SET record_count = $count WHERE plugin = $plugin;";
        command.Parameters.AddWithValue("$plugin", plugin);
        command.Parameters.AddWithValue("$count", count);
        command.ExecuteNonQuery();
    }

    private static void InsertRecord(SqliteConnection connection, SqliteTransaction transaction,
        IMajorRecordGetter record, string sourcePlugin)
    {
        var key = record.FormKey;
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO records(form_key, plugin, record_type, editor_id, name, model_path, source_plugin) " +
            "VALUES($formKey, $plugin, $type, $editorId, $name, $modelPath, $sourcePlugin);";
        command.Parameters.AddWithValue("$formKey", $"{key.ModKey.FileName}:0x{key.ID:X6}");
        command.Parameters.AddWithValue("$plugin", key.ModKey.FileName.ToString());
        command.Parameters.AddWithValue("$type", RecordType(record));
        command.Parameters.AddWithValue("$editorId", (object?)record.EditorID ?? DBNull.Value);
        command.Parameters.AddWithValue("$name", (object?)NameOf(record) ?? DBNull.Value);
        command.Parameters.AddWithValue("$modelPath", (object?)ModelPathOf(record) ?? DBNull.Value);
        command.Parameters.AddWithValue("$sourcePlugin", sourcePlugin);
        command.ExecuteNonQuery();
    }

    private static string? NameOf(IMajorRecordGetter record)
    {
        try { return (record as INamedGetter)?.Name; }
        catch { return null; } // Localized text may need archives unavailable in an offline catalog build.
    }

    private static string? ModelPathOf(IMajorRecordGetter record)
    {
        try { return (record as IModeledGetter)?.Model?.File.GivenPath; }
        catch { return null; }
    }

    private static string RecordType(IMajorRecordGetter record)
    {
        var type = record.GetType().Name;
        foreach (var suffix in new[] { "BinaryOverlay", "Getter" })
            if (type.EndsWith(suffix, StringComparison.Ordinal)) type = type[..^suffix.Length];
        return type;
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
