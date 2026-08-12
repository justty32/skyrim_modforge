using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using System.Text.Json;

namespace ModForge.Tests;

public sealed class CatalogTests
{
    [Fact]
    public void Build_Query_AndReplace_WorkWithoutSkyrim()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var firstKey = ModKey.FromNameAndExtension("CatalogOne.esp");
            var first = WriteFixture(dir, firstKey.FileName.String);
            var second = WriteFixture(dir, "CatalogTwo.esp", new FormKey(firstKey, 0x000802));
            var database = Path.Combine(dir, "catalog.db");

            var built = Catalog.Build(database, new[] { first, second });
            Assert.Equal(2, built.SourceCount);
            Assert.True(built.RecordCount > 0);
            var sources = Catalog.Sources(database);
            Assert.Equal(new[] { "CatalogOne.esp", "CatalogTwo.esp" }, sources.Select(s => s.Plugin));
            Assert.Equal(new[] { 0, 1 }, sources.Select(s => s.LoadOrderIndex));
            Assert.All(sources, source => Assert.True(source.RecordCount > 0 && source.Sha256.Length == 64));

            var one = Catalog.Query(database, "aldric", recordType: "Npc", sourcePlugin: "CatalogOne.esp");
            var npc = Assert.Single(one);
            Assert.Equal("CatalogOne.esp:0x000802", npc.FormKey);
            Assert.Equal("MF_DemoNpc", npc.EditorId);
            Assert.Equal("CatalogOne.esp", npc.SourcePlugin);
            Assert.Equal(Path.GetFullPath(first), npc.SourcePath);
            Assert.Single(Catalog.Query(database, "demonpc", recordType: "Npc", sourcePlugin: "CatalogOne.esp"));
            var exact = Assert.Single(Catalog.Get(database, "catalogone.esp:0x000802", "CatalogOne.esp"));
            Assert.Equal("MF_DemoNpc", exact.EditorId);
            Assert.Equal("CatalogOne.esp", exact.SourcePlugin);
            Assert.Single(Catalog.Get(database, exact.FormKey, sourcePlugin: "catalogone.esp"));
            Assert.Single(Catalog.Get(database, exact.FormKey, sourcePlugin: "CatalogTwo.esp"));
            Assert.Empty(Catalog.Get(database, "CatalogOne.esp:0xFFFFFF"));

            var occurrences = Catalog.Get(database, "CatalogOne.esp:0x000802");
            Assert.Equal(new[] { "CatalogOne.esp", "CatalogTwo.esp" }, occurrences.Select(r => r.SourcePlugin));

            var jsonPath = Path.Combine(dir, "scene-catalog.json");
            var exported = Catalog.ExportJsonFile(database, jsonPath);
            Assert.Equal(1, exported.SchemaVersion);
            var winner = Assert.Single(exported.Records, r => r.FormKey == "CatalogOne.esp:0x000802");
            Assert.Equal("CatalogTwo.esp", winner.SourcePlugin);
            Assert.Equal("MF_DemoNpcOverride", winner.EditorId);
            var modeled = Assert.Single(exported.Records,
                r => r.EditorId == "MF_CatalogStatic" && r.SourcePlugin == "CatalogOne.esp");
            Assert.Equal("Architecture\\Farmhouse\\Farmhouse01.nif", modeled.ModelPath);
            using (var json = JsonDocument.Parse(File.ReadAllText(jsonPath)))
            {
                Assert.Equal(1, json.RootElement.GetProperty("schemaVersion").GetInt32());
                Assert.Equal(2, json.RootElement.GetProperty("sources").GetArrayLength());
            }
            var firstExport = File.ReadAllText(jsonPath);
            Catalog.ExportJsonFile(database, jsonPath);
            Assert.Equal(firstExport, File.ReadAllText(jsonPath));
            Assert.Throws<ArgumentException>(() => Catalog.ExportJsonFile(database, database));
            Assert.Equal(2, Catalog.Sources(database).Count);

            var reversedDatabase = Path.Combine(dir, "catalog-reversed.db");
            Catalog.Build(reversedDatabase, new[] { second, first });
            var reversed = Catalog.ExportJsonFile(reversedDatabase, Path.Combine(dir, "reversed.json"));
            var reversedWinner = Assert.Single(reversed.Records,
                r => r.FormKey == "CatalogOne.esp:0x000802");
            Assert.Equal("CatalogOne.esp", reversedWinner.SourcePlugin);
            Assert.Equal("MF_DemoNpc", reversedWinner.EditorId);

            // Rebuilding the same destination replaces its contents rather than accumulating rows.
            Catalog.Build(database, new[] { first });
            Assert.Single(Catalog.Sources(database));
            Assert.Single(Catalog.Query(database, "aldric", recordType: "Npc", sourcePlugin: "CatalogOne.esp"));
            Assert.Empty(Catalog.Query(database, "aldric", sourcePlugin: "CatalogTwo.esp"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Build_RejectsDistinctPathsWithTheSamePluginName()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-catalog-" + Guid.NewGuid().ToString("N"));
        var firstDir = Path.Combine(dir, "one");
        var secondDir = Path.Combine(dir, "two");
        Directory.CreateDirectory(firstDir);
        Directory.CreateDirectory(secondDir);
        try
        {
            var first = WriteFixture(firstDir, "Duplicate.esp");
            var second = WriteFixture(secondDir, "Duplicate.esp");
            Assert.Throws<InvalidOperationException>(() =>
                Catalog.Build(Path.Combine(dir, "catalog.db"), new[] { first, second }));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ExportJson_RejectsBadTargets_AndPreservesExistingFileOnFailure()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var output = Path.Combine(dir, "catalog.json");
            File.WriteAllText(output, "keep me");
            var invalidDatabase = Path.Combine(dir, "invalid.db");
            File.WriteAllText(invalidDatabase, "not sqlite");

            Assert.ThrowsAny<Exception>(() => Catalog.ExportJsonFile(invalidDatabase, output));
            Assert.Equal("keep me", File.ReadAllText(output));
            Assert.Throws<IOException>(() => Catalog.ExportJsonFile(invalidDatabase, dir));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ReadCommands_ExplainThatLegacyCatalogsMustBeRebuilt()
    {
        var path = Path.Combine(Path.GetTempPath(), "mf-catalog-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE sources(plugin TEXT);";
                command.ExecuteNonQuery();
            }

            var error = Assert.Throws<InvalidOperationException>(() => Catalog.Sources(path));
            Assert.Contains("rebuild", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static string WriteFixture(string directory, string fileName, FormKey? overrideKey = null)
    {
        var path = Path.Combine(directory, fileName);
        var mod = Demo.CreateDemoPlugin(ModKey.FromNameAndExtension(fileName));
        var stat = mod.Statics.AddNew();
        stat.EditorID = "MF_CatalogStatic";
        stat.Model = new Model();
        stat.Model.File.GivenPath = "Architecture\\Farmhouse\\Farmhouse01.nif";
        if (overrideKey is { } key)
        {
            mod.ModHeader.MasterReferences.Add(new MasterReference { Master = key.ModKey });
            var npcOverride = new Npc(key, SkyrimRelease.SkyrimSE)
            {
                EditorID = "MF_DemoNpcOverride",
                Name = "Aldric Overridden",
            };
            mod.Npcs.Add(npcOverride);
        }
        PluginIo.Write(mod, path);
        return path;
    }
}
