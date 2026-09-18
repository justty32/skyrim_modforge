using System.Text.Json;

namespace ModForge;

public sealed record SnapshotChange<T>(string Name, T Before, T After);
public sealed record SnapshotEntryDiff<TEntry, TEnabled, TIndex>(
    IReadOnlyList<TEntry> Added, IReadOnlyList<TEntry> Removed,
    IReadOnlyList<SnapshotChange<TEnabled>> EnabledChanged,
    IReadOnlyList<SnapshotChange<TIndex>> PositionChanged);
public sealed record ModlistSnapshotDiff(
    SnapshotEntryDiff<SnapshotMod, bool, int> Mods,
    SnapshotEntryDiff<SnapshotPlugin, bool?, int?> Plugins,
    IReadOnlyList<SnapshotChange<SnapshotSource>> SourcesChanged)
{
    public bool HasChanges => SourcesChanged.Count != 0
        || Mods.Added.Count + Mods.Removed.Count + Mods.EnabledChanged.Count + Mods.PositionChanged.Count != 0
        || Plugins.Added.Count + Plugins.Removed.Count + Plugins.EnabledChanged.Count + Plugins.PositionChanged.Count != 0;

    public string ToJson() => JsonSerializer.Serialize(this, ModlistSnapshot.JsonOptions) + "\n";
}

public sealed partial record ModlistSnapshot
{
    public static ModlistSnapshotDiff Diff(ModlistSnapshot before, ModlistSnapshot after)
    {
        before.Validate();
        after.Validate();
        var sources = new List<SnapshotChange<SnapshotSource>>();
        AddSource("modlist.txt", before.Sources.Modlist, after.Sources.Modlist);
        AddSource("plugins.txt", before.Sources.Plugins, after.Sources.Plugins);
        AddSource("loadorder.txt", before.Sources.Loadorder, after.Sources.Loadorder);
        return new ModlistSnapshotDiff(
            Compare(before.Mods, after.Mods, x => x.Name, x => x.Enabled, x => x.PriorityIndex),
            Compare(before.Plugins, after.Plugins, x => x.Name, x => x.Enabled, x => x.LoadOrderIndex), sources);

        void AddSource(string name, SnapshotSource a, SnapshotSource b)
        {
            if (a != b) sources.Add(new SnapshotChange<SnapshotSource>(name, a, b));
        }
    }

    private static SnapshotEntryDiff<T, TEnabled, TIndex> Compare<T, TEnabled, TIndex>(
        IReadOnlyList<T> before, IReadOnlyList<T> after, Func<T, string> name,
        Func<T, TEnabled> enabled, Func<T, TIndex> index)
    {
        var a = before.ToDictionary(name, StringComparer.OrdinalIgnoreCase);
        var b = after.ToDictionary(name, StringComparer.OrdinalIgnoreCase);
        var added = b.Keys.Except(a.Keys, StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal).Select(key => b[key]).ToArray();
        var removed = a.Keys.Except(b.Keys, StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal).Select(key => a[key]).ToArray();
        var states = new List<SnapshotChange<TEnabled>>();
        var positions = new List<SnapshotChange<TIndex>>();
        foreach (var key in b.Keys.Intersect(a.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal))
        {
            if (!EqualityComparer<TEnabled>.Default.Equals(enabled(a[key]), enabled(b[key])))
                states.Add(new SnapshotChange<TEnabled>(key, enabled(a[key]), enabled(b[key])));
            if (!EqualityComparer<TIndex>.Default.Equals(index(a[key]), index(b[key])))
                positions.Add(new SnapshotChange<TIndex>(key, index(a[key]), index(b[key])));
        }
        return new SnapshotEntryDiff<T, TEnabled, TIndex>(added, removed, states, positions);
    }
}
