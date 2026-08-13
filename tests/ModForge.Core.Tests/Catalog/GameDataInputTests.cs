using System.Text.Json;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;
using Xunit;

namespace ModForge.Tests;

[CollectionDefinition("TranslatedString serial", DisableParallelization = true)]
public sealed class TranslatedStringSerialCollection;

[Collection("TranslatedString serial")]
public sealed class GameDataInputTests
{
    [Fact]
    public void QuestNodesCommand_ExtractsLocalizedStageLog_FromBsa_AndRefreshesOnMetadataChange()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-questnodes-localized-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var cacheDirectories = new List<string>();
        var oldLanguage = TranslatedString.DefaultLanguage;
        try
        {
            var plugin = Path.Combine(dir, "Localized.esp");
            var output = Path.Combine(dir, "nodes");
            TranslatedString.DefaultLanguage = Language.English;
            WriteLocalizedFixture(dir, plugin, "Resolved journal text.");
            cacheDirectories.Add(CacheDirectory(dir));

            Assert.Equal(0, Program.QuestNodesCmd(plugin, output));
            Assert.Equal("Resolved journal text.", ReadSummary(output));

            WriteLocalizedFixture(dir, plugin, "Updated journal text is longer.");
            cacheDirectories.Add(CacheDirectory(dir));
            Assert.Equal(0, Program.QuestNodesCmd(plugin, output));
            Assert.Equal("Updated journal text is longer.", ReadSummary(output));
        }
        finally
        {
            TranslatedString.DefaultLanguage = oldLanguage;
            foreach (var cacheDirectory in cacheDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
                if (Directory.Exists(cacheDirectory)) Directory.Delete(cacheDirectory, recursive: true);
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void QuestNodesCommand_ResolvesLocalizedStageLog_FromStringsOverride()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-questnodes-override-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var oldLanguage = TranslatedString.DefaultLanguage;
        try
        {
            var plugin = Path.Combine(dir, "Localized.esp");
            var strings = Path.Combine(dir, "Strings");
            var output = Path.Combine(dir, "nodes");
            TranslatedString.DefaultLanguage = Language.English;
            WriteLocalizedPlugin(plugin, strings, "Override journal text.");

            Assert.Equal(0, Program.QuestNodesCmd(plugin, output, strings));
            Assert.Equal("Override journal text.", ReadSummary(output));
        }
        finally
        {
            TranslatedString.DefaultLanguage = oldLanguage;
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void EnglishStringsCacheKey_IsPerSource_AndChangesWhenArchiveChanges()
    {
        var root = Path.Combine(Path.GetTempPath(), "mf-strings-cache-" + Guid.NewGuid().ToString("N"));
        var firstDir = Path.Combine(root, "first");
        var secondDir = Path.Combine(root, "second");
        Directory.CreateDirectory(firstDir);
        Directory.CreateDirectory(secondDir);
        try
        {
            var firstArchive = Path.Combine(firstDir, "Data.bsa");
            var secondArchive = Path.Combine(secondDir, "Data.bsa");
            File.WriteAllText(firstArchive, "old");
            File.WriteAllText(secondArchive, "old");
            var firstKey = Program.EnglishStringsSourceKey(firstDir, new[] { firstArchive });
            var secondKey = Program.EnglishStringsSourceKey(secondDir, new[] { secondArchive });

            Assert.NotEqual(firstKey, secondKey);

            File.AppendAllText(firstArchive, " updated");
            var refreshedKey = Program.EnglishStringsSourceKey(firstDir, new[] { firstArchive });
            Assert.NotEqual(firstKey, refreshedKey);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteLocalizedFixture(string dataDir, string plugin, string text)
    {
        var strings = Path.Combine(dataDir, "staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(strings);
        try
        {
            WriteLocalizedPlugin(plugin, strings, text);
            WriteSseBsa(Path.Combine(dataDir, "Localized - Strings.bsa"),
                Directory.GetFiles(strings).Order(StringComparer.OrdinalIgnoreCase).ToArray());
        }
        finally
        {
            Directory.Delete(strings, recursive: true);
        }
    }

    private static void WriteLocalizedPlugin(string plugin, string strings, string text)
    {
        Directory.CreateDirectory(strings);
        var mod = new SkyrimMod(ModKey.FromNameAndExtension("Localized.esp"), SkyrimRelease.SkyrimSE)
        {
            UsingLocalization = true,
        };
        var quest = mod.Quests.AddNew();
        quest.EditorID = "MF_Localized";
        var stage = new QuestStage { Index = 25 };
        stage.LogEntries.Add(new QuestLogEntry { Entry = text });
        quest.Stages.Add(stage);
        using var writer = new StringsWriter(
            GameRelease.SkyrimSE, mod.ModKey, strings, new Utf8EncodingProvider());
        mod.WriteToBinary(plugin, new BinaryWriteParameters
        {
            ModKey = ModKeyOption.NoCheck,
            StringsWriter = writer,
            TargetLanguageOverride = Language.English,
        });
    }

    private static void WriteSseBsa(string path, IReadOnlyList<string> files)
    {
        var folderName = System.Text.Encoding.ASCII.GetBytes("strings\0");
        var names = files.Select(file => System.Text.Encoding.ASCII.GetBytes(Path.GetFileName(file) + "\0")).ToArray();
        var contents = files.Select(File.ReadAllBytes).ToArray();
        const int headerLength = 0x24;
        const int folderHeaderLength = 0x18;
        var folderBlockLength = 1 + folderName.Length + files.Count * 0x10;
        var namesLength = names.Sum(name => name.Length);
        var dataOffset = headerLength + folderHeaderLength + folderBlockLength + namesLength;

        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("BSA\0"));
        writer.Write(0x69u);
        writer.Write((uint)headerLength);
        writer.Write(0x3u);
        writer.Write(1u);
        writer.Write((uint)files.Count);
        writer.Write((uint)folderName.Length);
        writer.Write((uint)namesLength);
        writer.Write(0x100u);
        writer.Write(0ul);
        writer.Write((uint)files.Count);
        writer.Write(0u);
        writer.Write((ulong)(headerLength + folderHeaderLength));
        writer.Write((byte)folderName.Length);
        writer.Write(folderName);
        var nextOffset = dataOffset;
        for (var index = 0; index < files.Count; index++)
        {
            writer.Write(0ul);
            writer.Write((uint)contents[index].Length);
            writer.Write((uint)nextOffset);
            nextOffset += contents[index].Length;
        }
        foreach (var name in names) writer.Write(name);
        foreach (var content in contents) writer.Write(content);
    }

    private static string? ReadSummary(string output)
    {
        var nodePath = Assert.Single(Directory.GetFiles(output, "MF_Localized-*.json"));
        using var document = JsonDocument.Parse(File.ReadAllText(nodePath));
        Assert.Equal(25, document.RootElement.GetProperty("stage").GetInt32());
        return document.RootElement.GetProperty("summary").GetString();
    }

    private static string CacheDirectory(string dataDir)
    {
        var archive = Path.Combine(dataDir, "Localized - Strings.bsa");
        var key = Program.EnglishStringsSourceKey(dataDir, new[] { archive });
        return Path.Combine(Path.GetTempPath(), "modforge-gamedata-strings", "Localized", key);
    }
}
