using Microsoft.Data.Sqlite;

namespace ModForge;

public static partial class Catalog
{
    private const int SchemaVersion = 2;

    private static void RequireCurrentSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(command.ExecuteScalar());
        if (version != SchemaVersion)
            throw new InvalidOperationException(
                $"catalog database schema is version {version}, expected {SchemaVersion}; " +
                "rebuild it with 'catalog build'");
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;
            CREATE TABLE sources (
                plugin TEXT PRIMARY KEY,
                source_path TEXT NOT NULL,
                sha256 TEXT NOT NULL,
                localized INTEGER NOT NULL,
                record_count INTEGER NOT NULL,
                load_order_index INTEGER NOT NULL UNIQUE
            );
            CREATE TABLE records (
                id INTEGER PRIMARY KEY,
                form_key TEXT NOT NULL,
                plugin TEXT NOT NULL,
                record_type TEXT NOT NULL,
                editor_id TEXT NULL,
                name TEXT NULL,
                model_path TEXT NULL,
                source_plugin TEXT NOT NULL REFERENCES sources(plugin),
                UNIQUE(form_key, source_plugin)
            );
            CREATE INDEX records_type_plugin ON records(record_type, plugin);
            CREATE VIRTUAL TABLE records_fts USING fts5(name, editor_id);
            PRAGMA user_version = 2;
            """;
        command.ExecuteNonQuery();
    }
}
