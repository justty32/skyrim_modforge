using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

internal static partial class Program
{
    private const string QuestNodeManifest = ".modforge-quest-nodes.json";
    private static readonly Regex GeneratedQuestNodeFile = new(
        @"^.+-[0-9]+-[0-9A-Fa-f]{6}\.json$", RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions QuestNodeJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    internal static int QuestNodesCmd(string pluginPath, string outDir, string? stringsOverride = null)
    {
        pluginPath = Path.GetFullPath(pluginPath);
        if (!File.Exists(pluginPath)) throw new FileNotFoundException("plugin not found", pluginPath);
        var sourcePlugin = Path.GetFileName(pluginPath);
        var parameters = GameDataReadParameters(pluginPath, stringsOverride, out var localized);
        using var mod = SkyrimMod.CreateFromBinaryOverlay(
            new ModPath(pluginPath), SkyrimRelease.SkyrimSE, parameters);

        var nodes = mod.EnumerateMajorRecords<IQuestGetter>()
            .SelectMany(quest => QuestNodeExtractor.Extract(quest, sourcePlugin))
            .OrderBy(node => node.QuestId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.Stage)
            .ToArray();
        WriteQuestNodeDirectory(outDir, nodes);
        Console.WriteLine($"questnodes: {nodes.Length} node(s) from {sourcePlugin} (localized={localized}) -> {Path.GetFullPath(outDir)}");
        return 0;
    }

    internal static void WriteQuestNodeDirectory(string outDir, IReadOnlyList<QuestNode> nodes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outDir);
        ArgumentNullException.ThrowIfNull(nodes);
        outDir = Path.GetFullPath(outDir);
        Directory.CreateDirectory(outDir);

        var duplicate = nodes.GroupBy(node => $"{node.QuestId}@{node.Stage}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"duplicate quest-node identity: {duplicate.Key}");

        var previous = ReadQuestNodeManifest(outDir);
        var desired = nodes.Select(node => (Node: node,
            FileName: $"{SafeFileStem(node.QuestId)}-{node.Stage}-{SourceFormId(node.Source.Record)}.json")).ToArray();
        var fileCollision = desired.GroupBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (fileCollision is not null)
            throw new InvalidOperationException($"quest-node filename collision: {fileCollision.Key}");
        var written = desired.Select(item => item.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var stalePaths = previous.Except(written, StringComparer.OrdinalIgnoreCase)
            .Select(stale => SafeOutputPath.ResolveUnder(outDir, stale)).ToArray();

        foreach (var (node, fileName) in desired)
        {
            var outputPath = SafeOutputPath.ResolveUnder(outDir, fileName);
            RefuseFileLink(outputPath);
            File.WriteAllText(outputPath,
                JsonSerializer.Serialize(node, QuestNodeJson) + Environment.NewLine);
        }

        foreach (var stalePath in stalePaths)
        {
            RefuseFileLink(stalePath);
            if (File.Exists(stalePath)) File.Delete(stalePath);
        }
        var manifestPath = SafeOutputPath.ResolveUnder(outDir, QuestNodeManifest);
        RefuseFileLink(manifestPath);
        File.WriteAllText(manifestPath,
            JsonSerializer.Serialize(written.Order(StringComparer.OrdinalIgnoreCase), QuestNodeJson) + Environment.NewLine);
    }

    private static IReadOnlyList<string> ReadQuestNodeManifest(string outDir)
    {
        var path = SafeOutputPath.ResolveUnder(outDir, QuestNodeManifest);
        RefuseFileLink(path);
        if (!File.Exists(path)) return Array.Empty<string>();
        try
        {
            var files = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path)) ?? Array.Empty<string>();
            if (files.Any(file => string.IsNullOrWhiteSpace(file)
                    || file != Path.GetFileName(file)
                    || !GeneratedQuestNodeFile.IsMatch(file)))
                throw new InvalidOperationException($"quest-node manifest contains a non-generated filename: {path}");
            return files;
        }
        catch (JsonException exception) { throw new InvalidOperationException($"invalid quest-node manifest: {path}", exception); }
    }

    private static string SafeFileStem(string value) =>
        string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

    private static string SourceFormId(string record)
    {
        var marker = record.IndexOf("0x", StringComparison.OrdinalIgnoreCase);
        return marker >= 0 && record.Length >= marker + 8 ? record.Substring(marker + 2, 6) : "unknown";
    }

    private static void RefuseFileLink(string path)
    {
        if (File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            throw new IOException($"refusing to overwrite or delete a quest-node symlink: {path}");
    }
}
