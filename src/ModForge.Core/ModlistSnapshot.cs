using System.Security.Cryptography;
using System.Text;

namespace ModForge;

public sealed record SnapshotSource(string Sha256, int LineCount);
public sealed record SnapshotSources(SnapshotSource Modlist, SnapshotSource Plugins, SnapshotSource Loadorder);
public sealed record SnapshotMod(string Name, bool Enabled, int PriorityIndex);
// Null means absent from that source, never inferred enabled/disabled or an implicit master.
public sealed record SnapshotPlugin(string Name, bool? Enabled, int? LoadOrderIndex);

public sealed partial record ModlistSnapshot(
    int SchemaVersion, SnapshotSources Sources,
    IReadOnlyList<SnapshotMod> Mods, IReadOnlyList<SnapshotPlugin> Plugins)
{
    public static ModlistSnapshot Parse(string modlist, string plugins, string loadorder)
    {
        var modLines = Lines(modlist);
        var pluginLines = Lines(plugins);
        var orderLines = Lines(loadorder);
        var mods = new List<SnapshotMod>();
        var modNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in Entries(modLines))
        {
            if (line.StartsWith('*')) continue; // MO2 separator: not a mod, consumes no priority index.
            if (line[0] is not ('+' or '-') || string.IsNullOrWhiteSpace(line[1..]))
                throw new InvalidDataException($"invalid modlist entry '{line}'");
            var name = line[1..];
            if (!modNames.Add(name)) throw new InvalidDataException($"duplicate mod '{name}'");
            mods.Add(new SnapshotMod(name, line[0] == '+', mods.Count)); // 0 = highest priority.
        }

        var listing = PluginListing.Parse(pluginLines, starred: true);
        var order = Entries(orderLines).ToArray();
        _ = PluginListing.Parse(order, starred: false); // Reuse the same filename validation.
        if (order.Distinct(StringComparer.OrdinalIgnoreCase).Count() != order.Length)
            throw new InvalidDataException("duplicate plugin in loadorder.txt");
        var rows = order.Select((name, index) => new SnapshotPlugin(name,
            listing.Enabled.Contains(name) ? true : listing.Disabled.Contains(name) ? false : null, index))
            .ToList();
        var orderedNames = order.ToHashSet(StringComparer.OrdinalIgnoreCase);
        // plugins.txt is not load order; entries absent from loadorder sort by ordinal filename.
        rows.AddRange(listing.Enabled.Concat(listing.Disabled)
            .Where(name => !orderedNames.Contains(name)).OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => new SnapshotPlugin(name, listing.Enabled.Contains(name), null)));
        var snapshot = new ModlistSnapshot(1, new SnapshotSources(
            Source(modlist, modLines), Source(plugins, pluginLines), Source(loadorder, orderLines)), mods, rows);
        snapshot.Validate();
        return snapshot;
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string[] Lines(string text)
    {
        using var reader = new StringReader(Normalize(text));
        var lines = new List<string>();
        while (reader.ReadLine() is { } line) lines.Add(line);
        return lines.ToArray();
    }

    private static IEnumerable<string> Entries(IEnumerable<string> lines) => lines
        .Select(line => line.Trim().TrimStart('\uFEFF').Trim())
        .Where(line => line.Length > 0 && !line.StartsWith('#'));

    // Hash normalized UTF-8 text (CRLF -> LF), including comments, blanks and final newline.
    private static SnapshotSource Source(string text, string[] lines) => new(
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(text)))).ToLowerInvariant(),
        lines.Length);
}
