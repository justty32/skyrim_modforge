using System.Text;

internal static partial class Program
{
    private static int ModlistSnapshotCmd(string[] args)
    {
        if (args.Length == 3 && args[0] == "snapshot")
        {
            var inputs = new[] { "modlist.txt", "plugins.txt", "loadorder.txt" }
                .Select(name => Path.GetFullPath(Path.Combine(args[1], name))).ToArray();
            var output = Path.GetFullPath(args[2]);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            string Resolve(string path)
            {
                var parent = Path.GetDirectoryName(path);
                if (parent is null) return path;
                var resolved = Path.Combine(Resolve(parent), Path.GetFileName(path));
                FileSystemInfo info = Directory.Exists(resolved) ? new DirectoryInfo(resolved) : new FileInfo(resolved);
                return info.Exists ? info.ResolveLinkTarget(true)?.FullName ?? resolved : resolved;
            }
            if (inputs.Any(path => string.Equals(path, output, comparison)
                || string.Equals(Resolve(path), Resolve(output), comparison)))
                throw new ArgumentException("snapshot output must not overwrite a profile input");
            // Strict UTF-8; invalid bytes must not silently turn into replacement characters.
            var encoding = new UTF8Encoding(false, true);
            var snapshot = ModlistSnapshot.Parse(File.ReadAllText(inputs[0], encoding),
                File.ReadAllText(inputs[1], encoding), File.ReadAllText(inputs[2], encoding));
            var bytes = encoding.GetBytes(snapshot.ToJson());
            var temp = output + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes);
                    stream.Flush(flushToDisk: true);
                }
                File.Move(temp, output, overwrite: true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            Console.WriteLine($"wrote {args[2]}");
            return 0;
        }
        if (args.Length is 3 or 4 && args[0] == "diff" && (args.Length == 3 || args[3] == "--json"))
        {
            var diff = ModlistSnapshot.Diff(ModlistSnapshot.FromJson(File.ReadAllText(args[1])),
                ModlistSnapshot.FromJson(File.ReadAllText(args[2])));
            if (args.Length == 4) Console.Write(diff.ToJson());
            else
            {
                PrintSnapshotChanges("mod", "priority", diff.Mods, x => x.Name);
                PrintSnapshotChanges("plugin", "load order", diff.Plugins, x => x.Name);
                foreach (var source in diff.SourcesChanged)
                    Console.WriteLine($"source {source.Name}: {source.Before.Sha256} ({source.Before.LineCount} lines) -> "
                        + $"{source.After.Sha256} ({source.After.LineCount} lines)");
                if (!diff.HasChanges) Console.WriteLine("No differences.");
            }
            return diff.HasChanges ? 1 : 0;
        }
        throw new ArgumentException("usage: modlist snapshot <profileDir> <out.json> | modlist diff <a.json> <b.json> [--json]");
    }

    private static void PrintSnapshotChanges<T, TEnabled, TIndex>(string kind, string position,
        SnapshotEntryDiff<T, TEnabled, TIndex> diff, Func<T, string> name)
    {
        static string Value<TValue>(TValue value) => value is null ? "absent" : value.ToString()!;
        foreach (var entry in diff.Added) Console.WriteLine($"{kind} added: {name(entry)}");
        foreach (var entry in diff.Removed) Console.WriteLine($"{kind} removed: {name(entry)}");
        foreach (var change in diff.EnabledChanged)
            Console.WriteLine($"{kind} enabled: {change.Name}: {Value(change.Before)} -> {Value(change.After)}");
        foreach (var change in diff.PositionChanged)
            Console.WriteLine($"{kind} {position}: {change.Name}: {Value(change.Before)} -> {Value(change.After)}");
    }
}
