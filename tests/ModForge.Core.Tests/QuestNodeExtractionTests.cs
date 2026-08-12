using System.Text.Json;
using ModForge;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

public sealed class QuestNodeExtractionTests
{
    [Fact]
    public void Extract_EmitsOnlyTextStages_AndMarksTerminalFlags()
    {
        var mod = new SkyrimMod(ModKey.FromNameAndExtension("Story.esp"), SkyrimRelease.SkyrimSE);
        var quest = mod.Quests.AddNew();
        quest.EditorID = "MF_Story";
        quest.Stages.Add(Stage(10, "  The journey begins.  "));
        quest.Stages.Add(Stage(20));
        quest.Stages.Add(Stage(30, "The threat is defeated.", QuestLogEntry.Flag.CompleteQuest));
        quest.Stages.Add(Stage(40, "The trail goes cold.", QuestLogEntry.Flag.FailQuest));

        var nodes = QuestNodeExtractor.Extract(quest, "Story.ESP");

        Assert.Equal(new ushort[] { 10, 30, 40 }, nodes.Select(node => node.Stage));
        Assert.Equal("The journey begins.", nodes[0].Summary);
        Assert.False(nodes[0].Major);
        Assert.Equal(new[] { "unclassified" }, nodes[0].ReactionTags);
        Assert.True(nodes[1].Major);
        Assert.Equal(new[] { "quest-complete" }, nodes[1].ReactionTags);
        Assert.True(nodes[2].Major);
        Assert.Equal(new[] { "quest-failed" }, nodes[2].ReactionTags);
        Assert.All(nodes, node => Assert.Equal("gamedata", node.Source.Kind));
        Assert.All(nodes, node => Assert.Equal("Story.esp", node.Plugin));
    }

    [Fact]
    public void Extract_FallsBackToSchemaSafeFormId_WhenEditorIdIsUnsafe()
    {
        var mod = new SkyrimMod(ModKey.FromNameAndExtension("Story.esp"), SkyrimRelease.SkyrimSE);
        var quest = mod.Quests.AddNew();
        quest.EditorID = "unsafe quest/id";
        quest.Stages.Add(Stage(5, "A real journal entry."));

        var node = Assert.Single(QuestNodeExtractor.Extract(quest, "Story.esp"));

        Assert.Matches("^QUST_[0-9A-F]{6}$", node.QuestId);
    }

    [Fact]
    public void Extract_PreservesConditionalCompleteFailOutcomeMapping()
    {
        var mod = new SkyrimMod(ModKey.FromNameAndExtension("Story.esp"), SkyrimRelease.SkyrimSE);
        var quest = mod.Quests.AddNew();
        quest.EditorID = "MF_Branch";
        var stage = Stage(50, "The hostage survived.", QuestLogEntry.Flag.CompleteQuest);
        stage.LogEntries.Add(new QuestLogEntry { Entry = "The hostage died.", Flags = QuestLogEntry.Flag.FailQuest });
        quest.Stages.Add(stage);

        var node = Assert.Single(QuestNodeExtractor.Extract(quest, "Story.esp"));

        Assert.True(node.Major);
        Assert.Equal(new[] { "conditional-outcome" }, node.ReactionTags);
        Assert.Contains("[quest-complete] The hostage survived.", node.Summary);
        Assert.Contains("[quest-failed] The hostage died.", node.Summary);
    }

    [Fact]
    public void QuestNodesCommand_WritesCamelCaseSchemaShape_AndCleansOnlyManifestFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-questnodes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var plugin = Path.Combine(dir, "Story.esp");
            var output = Path.Combine(dir, "nodes");
            var mod = new SkyrimMod(ModKey.FromNameAndExtension("Story.esp"), SkyrimRelease.SkyrimSE);
            var quest = mod.Quests.AddNew();
            quest.EditorID = "MF_Story";
            quest.Stages.Add(Stage(10, "The journey begins."));
            quest.Stages.Add(Stage(100, "The journey ends.", QuestLogEntry.Flag.CompleteQuest));
            PluginIo.Write(mod, plugin);

            Assert.Equal(0, Program.QuestNodesCmd(plugin, output));
            var files = Directory.GetFiles(output, "*.json")
                .Where(path => !Path.GetFileName(path).StartsWith('.')).ToArray();
            Assert.Equal(2, files.Length);
            using var document = JsonDocument.Parse(File.ReadAllText(files.Single(path => path.Contains("-100-"))));
            var root = document.RootElement;
            Assert.Equal("MF_Story", root.GetProperty("questId").GetString());
            Assert.Equal("Story.esp", root.GetProperty("plugin").GetString());
            Assert.Equal(100, root.GetProperty("stage").GetInt32());
            Assert.True(root.GetProperty("major").GetBoolean());
            Assert.Equal("quest-complete", root.GetProperty("reactionTags")[0].GetString());
            Assert.Equal("gamedata", root.GetProperty("source").GetProperty("kind").GetString());

            var unrelated = Path.Combine(output, "notes.json");
            File.WriteAllText(unrelated, "{}");
            Program.WriteQuestNodeDirectory(output, Array.Empty<QuestNode>());
            Assert.True(File.Exists(unrelated));
            Assert.Empty(Directory.GetFiles(output, "MF_Story-*.json"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Writer_RejectsDuplicateNodeIdentity()
    {
        var source = new QuestNodeSource("gamedata", "QUST:Story.esp:0x000800 stage 10 (source Story.esp)");
        var nodes = new[]
        {
            new QuestNode("Q", "Story.esp", 10, "First", false, new[] { "unclassified" }, source),
            new QuestNode("Q", "Story.esp", 10, "Second", false, new[] { "unclassified" }, source),
        };
        var output = Path.Combine(Path.GetTempPath(), "mf-questnodes-" + Guid.NewGuid().ToString("N"));
        try { Assert.Throws<InvalidOperationException>(() => Program.WriteQuestNodeDirectory(output, nodes)); }
        finally { if (Directory.Exists(output)) Directory.Delete(output, recursive: true); }
    }

    [Fact]
    public void Writer_RejectsTamperedManifest_WithoutDeletingUnrelatedFile()
    {
        var output = Path.Combine(Path.GetTempPath(), "mf-questnodes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var unrelated = Path.Combine(output, "notes.json");
        File.WriteAllText(unrelated, "{}");
        File.WriteAllText(Path.Combine(output, ".modforge-quest-nodes.json"), "[\"notes.json\"]");
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                Program.WriteQuestNodeDirectory(output, Array.Empty<QuestNode>()));
            Assert.True(File.Exists(unrelated));
        }
        finally { if (Directory.Exists(output)) Directory.Delete(output, recursive: true); }
    }

    [UnixSymlinkFact]
    public void Writer_RejectsDesiredFileSymlink_WithoutChangingExternalTarget()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-questnodes-" + Guid.NewGuid().ToString("N"));
        var output = Path.Combine(dir, "nodes");
        Directory.CreateDirectory(output);
        var external = Path.Combine(dir, "external.json");
        File.WriteAllText(external, "keep me");
        var link = Path.Combine(output, "Q-10-000800.json");
        var source = new QuestNodeSource("gamedata", "QUST:Story.esp:0x000800 stage 10 (source Story.esp)");
        var node = new QuestNode("Q", "Story.esp", 10, "Changed", false,
            new[] { "unclassified" }, source);
        try
        {
            File.CreateSymbolicLink(link, external);
            Assert.Throws<IOException>(() => Program.WriteQuestNodeDirectory(output, new[] { node }));
            Assert.Equal("keep me", File.ReadAllText(external));
        }
        finally
        {
            DeleteDirectoryLink(output);
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Writer_RejectsSymlinkOutputDirectory_WithoutWritingThroughIt()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-questnodes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var external = Path.Combine(dir, "external");
        Directory.CreateDirectory(external);
        var output = Path.Combine(dir, "nodes");

        try
        {
            CreateDirectoryLink(output, external);
            Assert.Throws<IOException>(() =>
                Program.WriteQuestNodeDirectory(output, Array.Empty<QuestNode>()));
            Assert.Empty(Directory.EnumerateFileSystemEntries(external));
        }
        finally
        {
            DeleteDirectoryLink(output);
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Writer_RejectsSymlinkParent_BeforeCreatingOutputDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-questnodes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var external = Path.Combine(dir, "external");
        Directory.CreateDirectory(external);
        var linkedParent = Path.Combine(dir, "linked-parent");
        var output = Path.Combine(linkedParent, "nodes");

        try
        {
            CreateDirectoryLink(linkedParent, external);
            Assert.Throws<IOException>(() =>
                Program.WriteQuestNodeDirectory(output, Array.Empty<QuestNode>()));
            Assert.False(Directory.Exists(Path.Combine(external, "nodes")));
        }
        finally
        {
            DeleteDirectoryLink(linkedParent);
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    private static QuestStage Stage(ushort index, string? text = null, QuestLogEntry.Flag flags = 0)
    {
        var stage = new QuestStage { Index = index };
        if (text is not null) stage.LogEntries.Add(new QuestLogEntry { Entry = text, Flags = flags });
        return stage;
    }

    private static void CreateDirectoryLink(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(link, target);
            return;
        }

        var start = new System.Diagnostics.ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in new[] { "/d", "/c", "mklink", "/J", link, target })
            start.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(start)
            ?? throw new InvalidOperationException("failed to start cmd.exe for junction test");
        process.WaitForExit();
        Assert.True(process.ExitCode == 0,
            $"mklink /J failed: {process.StandardError.ReadToEnd()} {process.StandardOutput.ReadToEnd()}");
    }

    private static void DeleteDirectoryLink(string path)
    {
        if (Directory.Exists(path)
            && new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint))
            Directory.Delete(path);
    }

    private sealed class UnixSymlinkFactAttribute : FactAttribute
    {
        public UnixSymlinkFactAttribute()
        {
            if (OperatingSystem.IsWindows())
                Skip = "file symlink creation requires Windows developer-mode/admin privilege";
        }
    }
}
