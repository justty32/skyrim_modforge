using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

/// <summary>One deterministic, offline-verifiable Story Manager graph problem.</summary>
public sealed record StoryManagerIssue(string Code, string Message);

/// <summary>Structural checks for SMBN/SMQN trees that do not require a game load order.</summary>
public static class StoryManagerDiagnostics
{
    private sealed record Node(
        FormKey Key, string Kind, string? EditorId, FormKey Parent, FormKey PreviousSibling);

    public static IReadOnlyList<StoryManagerIssue> Analyze(ISkyrimModGetter mod)
    {
        ArgumentNullException.ThrowIfNull(mod);
        var issues = new List<StoryManagerIssue>();
        var events = mod.EnumerateMajorRecords<IStoryManagerEventNodeGetter>()
            .Select(x => x.FormKey).ToHashSet();
        var branches = mod.EnumerateMajorRecords<IStoryManagerBranchNodeGetter>().ToArray();
        var questNodes = mod.EnumerateMajorRecords<IStoryManagerQuestNodeGetter>().ToArray();
        var localRecords = mod.EnumerateMajorRecords().Select(x => x.FormKey).ToHashSet();
        var nodes = branches.Select(x => new Node(
                x.FormKey, "SMBN", x.EditorID, x.Parent.FormKey, x.PreviousSibling.FormKey))
            .Concat(questNodes.Select(x => new Node(
                x.FormKey, "SMQN", x.EditorID, x.Parent.FormKey, x.PreviousSibling.FormKey)))
            .ToArray();
        var byKey = nodes.ToDictionary(x => x.Key);

        foreach (var group in nodes.Where(x => !string.IsNullOrWhiteSpace(x.EditorId))
                     .GroupBy(x => x.EditorId!, StringComparer.OrdinalIgnoreCase)
                     .Where(x => x.Count() > 1))
            Add(issues, "duplicate-editor-id",
                $"Story Manager EditorID '{group.Key}' is used by {Keys(group)}");

        foreach (var node in nodes)
        {
            if (node.Parent.IsNull)
                Add(issues, "missing-parent", $"{Label(node)} has no parent");
            else if (node.Parent.ModKey == mod.ModKey &&
                     !byKey.ContainsKey(node.Parent) && !events.Contains(node.Parent))
                Add(issues, localRecords.Contains(node.Parent) ? "invalid-parent-type" : "orphan-parent",
                    localRecords.Contains(node.Parent)
                        ? $"{Label(node)} points to local parent {node.Parent}, which is not an SMEN/SMBN"
                        : $"{Label(node)} points to missing local parent {node.Parent}");
            else if (byKey.TryGetValue(node.Parent, out var parent) && parent.Kind == "SMQN")
                Add(issues, "invalid-parent-type",
                    $"{Label(node)} is parented to quest node {Label(parent)}");
        }
        ValidateParentCycles(nodes, byKey, issues);
        ValidateSiblingCycles(nodes, byKey, issues);
        ValidateSiblingChains(nodes, byKey, issues);
        ValidateQuestNodes(mod, questNodes, issues);
        return issues.OrderBy(x => x.Code, StringComparer.Ordinal)
            .ThenBy(x => x.Message, StringComparer.Ordinal).ToArray();
    }

    private static void ValidateSiblingCycles(
        IReadOnlyList<Node> nodes, IReadOnlyDictionary<FormKey, Node> byKey,
        ICollection<StoryManagerIssue> issues)
    {
        var reported = new HashSet<FormKey>();
        foreach (var start in nodes.Where(x => !x.PreviousSibling.IsNull))
        {
            var path = new List<Node>();
            var positions = new Dictionary<FormKey, int>();
            var current = start;
            while (true)
            {
                if (positions.TryGetValue(current.Key, out var cycleAt))
                {
                    var cycle = path.Skip(cycleAt).ToArray();
                    if (cycle.Any(x => reported.Add(x.Key)))
                        Add(issues, "sibling-cycle",
                            $"previous-sibling cycle: {string.Join(" -> ", cycle.Select(Label))} -> {Label(current)}");
                    break;
                }
                positions[current.Key] = path.Count;
                path.Add(current);
                if (current.PreviousSibling.IsNull ||
                    !byKey.TryGetValue(current.PreviousSibling, out current!)) break;
            }
        }
    }

    private static void ValidateParentCycles(
        IReadOnlyList<Node> nodes, IReadOnlyDictionary<FormKey, Node> byKey,
        ICollection<StoryManagerIssue> issues)
    {
        var reported = new HashSet<FormKey>();
        foreach (var start in nodes)
        {
            var path = new List<Node>();
            var positions = new Dictionary<FormKey, int>();
            var current = start;
            while (true)
            {
                if (positions.TryGetValue(current.Key, out var cycleAt))
                {
                    var cycle = path.Skip(cycleAt).ToArray();
                    if (cycle.Any(x => reported.Add(x.Key)))
                        Add(issues, "parent-cycle",
                            $"parent cycle: {string.Join(" -> ", cycle.Select(Label))} -> {Label(current)}");
                    break;
                }
                positions[current.Key] = path.Count;
                path.Add(current);
                if (!byKey.TryGetValue(current.Parent, out current!)) break;
            }
        }
    }

    private static void ValidateSiblingChains(
        IReadOnlyList<Node> nodes, IReadOnlyDictionary<FormKey, Node> byKey,
        ICollection<StoryManagerIssue> issues)
    {
        foreach (var siblings in nodes.Where(x => !x.Parent.IsNull).GroupBy(x => x.Parent))
        {
            var group = siblings.ToArray();
            var heads = group.Where(x => x.PreviousSibling.IsNull ||
                                         x.PreviousSibling.ModKey != x.Key.ModKey).ToArray();
            if (heads.Length != 1)
                Add(issues, "sibling-head-count",
                    $"parent {siblings.Key} has {heads.Length} sibling heads across {group.Length} children");

            foreach (var node in group.Where(x => !x.PreviousSibling.IsNull))
            {
                if (node.PreviousSibling.ModKey != node.Key.ModKey)
                    continue;
                if (!byKey.TryGetValue(node.PreviousSibling, out var previous))
                    Add(issues, "missing-previous-sibling",
                        $"{Label(node)} points to missing previous sibling {node.PreviousSibling}");
                else if (previous.Parent != node.Parent)
                    Add(issues, "cross-parent-sibling",
                        $"{Label(node)} points to previous sibling {Label(previous)} under another parent");
            }

            foreach (var duplicate in group.Where(x => !x.PreviousSibling.IsNull)
                         .GroupBy(x => x.PreviousSibling).Where(x => x.Count() > 1))
                Add(issues, "duplicate-sibling-link",
                    $"previous sibling {duplicate.Key} is claimed by {Keys(duplicate)}");
        }
    }

    private static void ValidateQuestNodes(
        ISkyrimModGetter mod, IReadOnlyList<IStoryManagerQuestNodeGetter> nodes,
        ICollection<StoryManagerIssue> issues)
    {
        var localQuests = mod.EnumerateMajorRecords<IQuestGetter>().ToDictionary(x => x.FormKey);
        var localLvln = mod.EnumerateMajorRecords<ILeveledNpcGetter>()
            .Select(x => x.FormKey).ToHashSet();
        var routedQuests = new Dictionary<FormKey, FormKey>();
        foreach (var node in nodes)
        {
            if (node.Quests.Count == 0)
                Add(issues, "empty-quest-node", $"SMQN {node.FormKey} contains no quests");
            foreach (var duplicate in node.Quests.GroupBy(x => x.Quest.FormKey).Where(x => x.Count() > 1))
                Add(issues, "duplicate-quest-entry",
                    $"SMQN {node.FormKey} contains quest {duplicate.Key} more than once");

            foreach (var entry in node.Quests)
            {
                var questKey = entry.Quest.FormKey;
                if (questKey.IsNull)
                {
                    Add(issues, "missing-quest-link",
                        $"SMQN {node.FormKey} contains an empty quest link");
                    continue;
                }
                if (routedQuests.TryGetValue(questKey, out var firstNode) && firstNode != node.FormKey)
                    Add(issues, "duplicate-quest-route",
                        $"quest {questKey} is routed by both {firstNode} and {node.FormKey}");
                else routedQuests[questKey] = node.FormKey;

                if (questKey.ModKey != mod.ModKey) continue;
                if (!localQuests.TryGetValue(questKey, out var quest))
                {
                    Add(issues, "missing-local-quest",
                        $"SMQN {node.FormKey} points to missing local quest {questKey}");
                    continue;
                }
                foreach (var duplicateAlias in quest.Aliases.GroupBy(x => x.ID).Where(x => x.Count() > 1))
                    Add(issues, "duplicate-alias-id",
                        $"quest {questKey} uses alias ID {duplicateAlias.Key} more than once");
                foreach (var duplicateAlias in quest.Aliases
                             .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                             .GroupBy(x => x.Name!, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
                    Add(issues, "duplicate-alias-name",
                        $"quest {questKey} uses alias name '{duplicateAlias.Key}' more than once");
                if (quest.Aliases.Count > 0 && quest.NextAliasID <= quest.Aliases.Max(x => x.ID))
                    Add(issues, "invalid-next-alias-id",
                        $"quest {questKey} NextAliasID {quest.NextAliasID} is not above its highest alias ID {quest.Aliases.Max(x => x.ID)}");
                foreach (var alias in quest.Aliases)
                {
                    if (localLvln.Contains(alias.ForcedReference.FormKey))
                        Add(issues, "lvln-forced-reference",
                            $"quest {questKey} alias {alias.ID} ('{alias.Name}') forces LVLN {alias.ForcedReference.FormKey}; a forced reference must be a placed reference");
                    if (localLvln.Contains(alias.UniqueActor.FormKey))
                        Add(issues, "lvln-unique-actor",
                            $"quest {questKey} alias {alias.ID} ('{alias.Name}') uses LVLN {alias.UniqueActor.FormKey} as a unique actor");
                    if (alias.CreateReferenceToObject is { } create && localLvln.Contains(create.Object.FormKey))
                    {
                        var target = quest.Aliases.FirstOrDefault(x => x.ID == (uint)create.AliasID);
                        if (target is null)
                            Add(issues, "lvln-create-target-missing",
                                $"quest {questKey} alias {alias.ID} creates LVLN {create.Object.FormKey} at missing alias {create.AliasID}");
                        else if (target.Type != QuestAlias.TypeEnum.Reference)
                            Add(issues, "lvln-create-target-type",
                                $"quest {questKey} alias {alias.ID} creates LVLN {create.Object.FormKey} at alias {target.ID} ('{target.Name}') of type {target.Type}; the target must be Reference");
                    }
                }
            }
        }
    }

    private static string Label(Node node) =>
        $"{node.Kind} {node.Key}" + (string.IsNullOrWhiteSpace(node.EditorId) ? "" : $" ('{node.EditorId}')");

    private static string Keys(IEnumerable<Node> nodes) =>
        string.Join(", ", nodes.Select(x => x.Key).OrderBy(x => x.ToString(), StringComparer.Ordinal));

    private static void Add(ICollection<StoryManagerIssue> issues, string code, string message) =>
        issues.Add(new StoryManagerIssue(code, message));
}
