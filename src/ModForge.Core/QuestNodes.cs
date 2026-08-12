using Mutagen.Bethesda.Skyrim;

namespace ModForge;

/// <summary>A provenance pointer carried by a mechanically extracted quest node.</summary>
public sealed record QuestNodeSource(string Kind, string Record);

/// <summary>
/// Schema-facing quest-stage node. Location, NPC, and graph fields are intentionally absent from
/// mechanical extraction because QUST stage logs do not encode those semantics reliably.
/// </summary>
public sealed record QuestNode(
    string QuestId,
    string Plugin,
    ushort Stage,
    string Summary,
    bool Major,
    IReadOnlyList<string> ReactionTags,
    QuestNodeSource Source);

/// <summary>Deterministic QUST stage-log extraction for the quest-node JSON hand-off.</summary>
public static class QuestNodeExtractor
{
    public static IReadOnlyList<QuestNode> Extract(IQuestGetter quest, string sourcePlugin)
    {
        ArgumentNullException.ThrowIfNull(quest);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePlugin);
        sourcePlugin = SchemaSafePlugin(sourcePlugin);

        var questId = SchemaSafeQuestId(quest.EditorID, quest.FormKey.ID);
        var nodes = new List<QuestNode>();
        foreach (var stage in quest.Stages.OrderBy(stage => stage.Index))
        {
            var entries = stage.LogEntries
                .Select(entry => (Text: ReadText(entry.Entry)?.Trim(), Flags: entry.Flags ?? 0))
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Text))
                .ToArray();
            if (entries.Length == 0) continue;

            var complete = entries.Any(entry => entry.Flags.HasFlag(QuestLogEntry.Flag.CompleteQuest));
            var failed = entries.Any(entry => entry.Flags.HasFlag(QuestLogEntry.Flag.FailQuest));
            var tags = new List<string>();
            if (complete && failed) tags.Add("conditional-outcome");
            else if (complete) tags.Add("quest-complete");
            else if (failed) tags.Add("quest-failed");
            if (tags.Count == 0) tags.Add("unclassified");
            var texts = entries.Select(entry => complete && failed
                    ? $"[{Outcome(entry.Flags)}] {entry.Text}"
                    : entry.Text!)
                .Distinct(StringComparer.Ordinal);

            nodes.Add(new QuestNode(
                questId,
                sourcePlugin,
                stage.Index,
                string.Join("\n\n", texts),
                complete || failed,
                tags,
                new QuestNodeSource("gamedata",
                    $"QUST:{quest.FormKey.ModKey.FileName}:0x{quest.FormKey.ID:X6} stage {stage.Index} (source {sourcePlugin})")));
        }
        return nodes;
    }

    private static string Outcome(QuestLogEntry.Flag flags)
    {
        var complete = flags.HasFlag(QuestLogEntry.Flag.CompleteQuest);
        var failed = flags.HasFlag(QuestLogEntry.Flag.FailQuest);
        if (complete && failed) return "quest-complete+quest-failed";
        if (complete) return "quest-complete";
        if (failed) return "quest-failed";
        return "other";
    }

    private static string? ReadText(Mutagen.Bethesda.Strings.ITranslatedStringGetter? value)
    {
        try { return value?.String; }
        catch { return null; }
    }

    private static string SchemaSafeQuestId(string? editorId, uint formId)
    {
        var candidate = editorId?.Trim();
        if (!string.IsNullOrEmpty(candidate)
            && candidate.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or ':' or '-'))
            return candidate;
        return $"QUST_{formId:X6}";
    }

    private static string SchemaSafePlugin(string plugin)
    {
        plugin = Path.GetFileName(plugin.Trim());
        var extension = Path.GetExtension(plugin);
        if (!new[] { ".esp", ".esm", ".esl" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("sourcePlugin must be an .esp, .esm, or .esl filename", nameof(plugin));
        return Path.GetFileNameWithoutExtension(plugin) + extension.ToLowerInvariant();
    }
}
