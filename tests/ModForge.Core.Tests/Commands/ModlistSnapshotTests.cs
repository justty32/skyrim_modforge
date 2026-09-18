using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Json.Schema;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public sealed class ModlistSnapshotTests
{
    [Fact]
    public void Modlist_PlusMinusSeparateEnabledAndDisabled()
    {
        var snapshot = Fixture();
        Assert.True(snapshot.Mods[0].Enabled);
        Assert.False(snapshot.Mods[1].Enabled);
    }

    [Fact]
    public void Priority_FirstModIsHighest_ZeroAndIncreasingTowardLowerPriority()
    {
        var mods = Fixture().Mods;
        Assert.Equal("Winning Patch", mods[0].Name);
        Assert.Equal(0, mods[0].PriorityIndex);
        Assert.Equal("Base Assets", mods[^1].Name);
        Assert.Equal(3, mods[^1].PriorityIndex);
        Assert.True(mods[0].PriorityIndex < mods[^1].PriorityIndex);
    }

    [Fact]
    public void Fixtures_CrlfAndLfProduceByteIdenticalJson()
    {
        Assert.Contains("\r\n", File.ReadAllText(FixturePath("crlf", "modlist.txt")));
        Assert.DoesNotContain("\r", File.ReadAllText(FixturePath("lf", "modlist.txt")));
        Assert.Equal(Bytes(Fixture("lf")), Bytes(Fixture("crlf")));
    }

    [Fact]
    public void SameInputTwice_IsByteIdentical_WithoutMachineMetadata()
    {
        Assert.Equal(Bytes(Fixture()), Bytes(Fixture()));
        using var doc = JsonDocument.Parse(Fixture().ToJson());
        Assert.Equal(new[] { "schemaVersion", "sources", "mods", "plugins" },
            doc.RootElement.EnumerateObject().Select(x => x.Name));
        Assert.Equal(Fixture().ToJson(), ModlistSnapshot.FromJson(Fixture().ToJson()).ToJson());
    }

    [Fact]
    public void Separator_IsOmitted_ConsumesNoPriorityIndex()
    {
        Assert.Equal(4, Fixture().Mods.Count);
        Assert.DoesNotContain(Fixture().Mods, x => x.Name.Contains("Separator"));
        Assert.Equal(2, Fixture().Mods.Single(x => x.Name == "風景  美化").PriorityIndex);
    }

    [Fact]
    public void ModNames_PreserveSpacesAndNonAscii()
    {
        Assert.Contains(Fixture().Mods, x => x.Name == "風景  美化");
        Assert.Contains(Fixture().Mods, x => x.Name == "停用 模組");
    }

    [Fact]
    public void Plugins_ReusePluginListingSemantics_AndRejectInvalidNames()
    {
        var text = "\uFEFF# header\n*Hello World.esp\n*hello world.ESP\n停用.esl\n";
        var expected = PluginListing.Parse(text.Split('\n'), starred: true);
        var snapshot = ModlistSnapshot.Parse("", text, "");
        Assert.Equal(expected.Enabled.Count + expected.Disabled.Count, snapshot.Plugins.Count);
        Assert.All(snapshot.Plugins, x => Assert.Equal(expected.Enabled.Contains(x.Name), x.Enabled));
        Assert.Throws<InvalidDataException>(() => ModlistSnapshot.Parse("", "*not-plugin.txt", ""));
        Assert.Throws<InvalidDataException>(() => ModlistSnapshot.Parse("", "*Same.esp\nsame.ESP", ""));
        Assert.Throws<InvalidDataException>(() => ModlistSnapshot.Parse("", "", "folder/Bad.esp"));
    }

    [Fact]
    public void UnequalLists_PreserveBothSides_WithoutInferringOfficialMasters()
    {
        var snapshot = ModlistSnapshot.Parse("", "*Only.esp", "Skyrim.esm\nAnother.esp");
        Assert.Equal(1, snapshot.Sources.Plugins.LineCount);
        Assert.Equal(2, snapshot.Sources.Loadorder.LineCount);
        Assert.Null(snapshot.Plugins.Single(x => x.Name == "Skyrim.esm").Enabled);
        Assert.Null(snapshot.Plugins.Single(x => x.Name == "Only.esp").LoadOrderIndex);
        Assert.DoesNotContain(snapshot.Plugins, x => x.Name == "Update.esm");
        Assert.Empty(ModlistSnapshot.Parse("", "", "").Plugins);
    }

    [Fact]
    public void Diff_ModEnabledStateChanged()
    {
        var diff = ModlistSnapshot.Diff(ModlistSnapshot.Parse("+A", "", ""), ModlistSnapshot.Parse("-A", "", ""));
        Assert.Equal(new SnapshotChange<bool>("A", true, false), Assert.Single(diff.Mods.EnabledChanged));
    }

    [Fact]
    public void Diff_PluginAdded()
    {
        var diff = ModlistSnapshot.Diff(ModlistSnapshot.Parse("", "", ""), ModlistSnapshot.Parse("", "*New.esp", ""));
        Assert.Equal("New.esp", Assert.Single(diff.Plugins.Added).Name);
    }

    [Fact]
    public void Diff_LoadOrderPositionChanged()
    {
        var diff = ModlistSnapshot.Diff(ModlistSnapshot.Parse("", "", "A.esp\nB.esp"),
            ModlistSnapshot.Parse("", "", "B.esp\nA.esp"));
        Assert.Contains(new SnapshotChange<int?>("A.esp", 0, 1), diff.Plugins.PositionChanged);
        Assert.Contains(new SnapshotChange<int?>("B.esp", 1, 0), diff.Plugins.PositionChanged);
    }

    [Fact]
    public void Diff_RemovalsModAdditionPriorityAndPluginState_AreSeparate()
    {
        var a = ModlistSnapshot.Parse("+A\n-B", "*A.esp\n*Removed.esp", "A.esp");
        var b = ModlistSnapshot.Parse("+New\n+A", "A.esp", "");
        var diff = ModlistSnapshot.Diff(a, b);
        Assert.Equal("New", Assert.Single(diff.Mods.Added).Name);
        Assert.Equal("B", Assert.Single(diff.Mods.Removed).Name);
        Assert.Equal(new SnapshotChange<int>("A", 0, 1), Assert.Single(diff.Mods.PositionChanged));
        Assert.Equal("Removed.esp", Assert.Single(diff.Plugins.Removed).Name);
        Assert.Equal(new SnapshotChange<bool?>("A.esp", true, false), Assert.Single(diff.Plugins.EnabledChanged));
        Assert.Equal(new SnapshotChange<int?>("A.esp", 0, null), Assert.Single(diff.Plugins.PositionChanged));
        Assert.False(ModlistSnapshot.Diff(a, a).HasChanges);
    }

    [Fact]
    public void Sources_HashNormalizedFullText_AndCountPhysicalLines()
    {
        const string text = "# comment\r\n+A\r\n\r\n";
        var snapshot = ModlistSnapshot.Parse(text, "", "");
        Assert.Equal(3, snapshot.Sources.Modlist.LineCount);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("# comment\n+A\n\n"))).ToLowerInvariant(),
            snapshot.Sources.Modlist.Sha256);
        var diff = ModlistSnapshot.Diff(snapshot, ModlistSnapshot.Parse(text + "# extra", "", ""));
        Assert.True(diff.HasChanges);
        Assert.Single(diff.SourcesChanged);
        Assert.Empty(diff.Mods.PositionChanged);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{broken")]
    [InlineData("{\"schemaVersion\":1,\"schemaVersion\":1}")]
    public void MalformedSnapshotJson_FailsClosed(string json) =>
        Assert.ThrowsAny<Exception>(() => ModlistSnapshot.FromJson(json));

    [Fact]
    public void SnapshotValidation_RejectsVersionMissingFieldsUnknownFieldsAndInvalidIndices()
    {
        var json = Fixture().ToJson();
        foreach (var bad in new[] { json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2"),
            json.Replace("\"priorityIndex\": 0", "\"priorityIndex\": -1"),
            json.Replace("\"enabled\": true,", ""), json.Replace("\"sources\":", "\"unknown\":") })
            Assert.ThrowsAny<Exception>(() => ModlistSnapshot.FromJson(bad));
        Assert.Throws<InvalidDataException>(() => ModlistSnapshot.Parse("+A\n-A", "", ""));
        Assert.Throws<InvalidDataException>(() => ModlistSnapshot.Parse("A", "", ""));
        Assert.Throws<InvalidDataException>(() => ModlistSnapshot.Parse("", "", "A.esp\na.ESP"));
    }

    [Fact]
    public void SnapshotJson_MatchesPublishedSchema()
    {
        using var schemaDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "schemas/modlist-snapshot.schema.json")));
        var schema = JsonSchema.Build(schemaDoc.RootElement);
        using var json = JsonDocument.Parse(Fixture().ToJson());
        Assert.True(schema.Evaluate(json.RootElement).IsValid);
    }

    [Fact]
    public void CliRealProcess_ReturnsZeroOneTwo_AndDoesNotChangeInputs()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-snapshot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            foreach (var name in new[] { "modlist.txt", "plugins.txt", "loadorder.txt" })
                File.Copy(FixturePath("lf", name), Path.Combine(dir, name));
            var before = Directory.GetFiles(dir).ToDictionary(x => x, File.ReadAllBytes);
            var a = Path.Combine(dir, "a.json");
            var b = Path.Combine(dir, "b.json");
            Assert.Equal(0, RunCli("modlist", "snapshot", dir, a).Code);
            Assert.Equal(0, RunCli("modlist", "snapshot", dir, b).Code);
            Assert.Equal(File.ReadAllBytes(a), File.ReadAllBytes(b));
            before.Add(a, File.ReadAllBytes(a));
            Assert.Equal(0, RunCli("modlist", "diff", a, b).Code);
            File.WriteAllText(b, ModlistSnapshot.Parse("+Different", "*New.esp", "New.esp").ToJson());
            before.Add(b, File.ReadAllBytes(b));
            var changed = RunCli("modlist", "diff", a, b, "--json");
            Assert.Equal(1, changed.Code);
            using var diff = JsonDocument.Parse(changed.Out);
            Assert.True(diff.RootElement.GetProperty("hasChanges").GetBoolean());
            Assert.Contains("mod added: Different", RunCli("modlist", "diff", a, b).Out);
            Assert.Equal(2, RunCli("modlist").Code);
            Assert.Equal(2, RunCli("modlist", "diff", a, b, "--bogus").Code);
            Assert.Equal(2, RunCli("modlist", "diff", a, Path.Combine(dir, "missing.json")).Code);
            Assert.Equal(2, RunCli("modlist", "diff", a, Path.Combine(dir, "plugins.txt")).Code);
            Assert.Equal(2, RunCli("modlist", "snapshot", dir, Path.Combine(dir, "modlist.txt")).Code);
            foreach (var (path, bytes) in before) Assert.Equal(bytes, File.ReadAllBytes(path));
            File.WriteAllText(Path.Combine(dir, "plugins.txt"), "*broken.txt");
            Assert.Equal(2, RunCli("modlist", "snapshot", dir, a).Code);
            Assert.Equal(before[a], File.ReadAllBytes(a));
            Assert.Empty(Directory.GetFiles(dir, "*.tmp-*"));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    private static byte[] Bytes(ModlistSnapshot snapshot) => Encoding.UTF8.GetBytes(snapshot.ToJson());
    private static ModlistSnapshot Fixture(string kind = "lf") => ModlistSnapshot.Parse(
        File.ReadAllText(FixturePath(kind, "modlist.txt")), File.ReadAllText(FixturePath(kind, "plugins.txt")),
        File.ReadAllText(FixturePath(kind, "loadorder.txt")));
    private static string FixturePath(string kind, string name) => Path.Combine(Root(), "fixtures/modlist-snapshot", kind, name);
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repo root unavailable");
    }
    private static (int Code, string Out, string Error) RunCli(params string[] args)
    {
        var start = new ProcessStartInfo("dotnet")
        { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, stdout, stderr);
    }
}
