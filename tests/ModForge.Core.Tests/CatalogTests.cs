using Mutagen.Bethesda.Plugins;
using ModForge;

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
            var first = WriteFixture(dir, "CatalogOne.esp");
            var second = WriteFixture(dir, "CatalogTwo.esp");
            var database = Path.Combine(dir, "catalog.db");

            var built = Catalog.Build(database, new[] { second, first });
            Assert.Equal(2, built.SourceCount);
            Assert.True(built.RecordCount > 0);
            var sources = Catalog.Sources(database);
            Assert.Equal(new[] { "CatalogOne.esp", "CatalogTwo.esp" }, sources.Select(s => s.Plugin));
            Assert.All(sources, source => Assert.True(source.RecordCount > 0 && source.Sha256.Length == 64));

            var one = Catalog.Query(database, "aldric", recordType: "Npc", sourcePlugin: "CatalogOne.esp");
            var npc = Assert.Single(one);
            Assert.Equal("CatalogOne.esp:0x000802", npc.FormKey);
            Assert.Equal("MF_DemoNpc", npc.EditorId);
            Assert.Equal("CatalogOne.esp", npc.SourcePlugin);
            Assert.Equal(Path.GetFullPath(first), npc.SourcePath);
            Assert.Single(Catalog.Query(database, "demonpc", recordType: "Npc", sourcePlugin: "CatalogOne.esp"));
            var exact = Assert.Single(Catalog.Get(database, "catalogone.esp:0x000802"));
            Assert.Equal("MF_DemoNpc", exact.EditorId);
            Assert.Equal("CatalogOne.esp", exact.SourcePlugin);
            Assert.Single(Catalog.Get(database, exact.FormKey, sourcePlugin: "catalogone.esp"));
            Assert.Empty(Catalog.Get(database, exact.FormKey, sourcePlugin: "CatalogTwo.esp"));
            Assert.Empty(Catalog.Get(database, "CatalogOne.esp:0xFFFFFF"));

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

    private static string WriteFixture(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        var mod = Demo.CreateDemoPlugin(ModKey.FromNameAndExtension(fileName));
        PluginIo.Write(mod, path);
        return path;
    }
}
