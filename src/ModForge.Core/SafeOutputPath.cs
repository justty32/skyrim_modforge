namespace ModForge;

/// <summary>Resolves a generated relative path while guaranteeing it stays below an output root.</summary>
public static class SafeOutputPath
{
    public static string ResolveUnder(string outputRoot, string relativePath)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(outputRoot));
        var dest = Path.GetFullPath(Path.Combine(root,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!dest.StartsWith(prefix, comparison))
            throw new InvalidDataException($"generated output path escapes package directory: '{relativePath}'");
        return dest;
    }

    /// <summary>Rejects an output path whose file or any parent is a symbolic link or reparse point.</summary>
    public static void RejectReparsePoints(string path, string description = "generated output path")
    {
        var fullPath = Path.GetFullPath(path);
        FileSystemInfo? current = Directory.Exists(fullPath)
            ? new DirectoryInfo(fullPath)
            : new FileInfo(fullPath);

        while (current is not null)
        {
            if (current.LinkTarget is not null
                || (current.Exists && current.Attributes.HasFlag(FileAttributes.ReparsePoint)))
                throw new IOException($"{description} may not traverse a reparse point: {current.FullName}");

            current = current switch
            {
                FileInfo file => file.Directory,
                DirectoryInfo directory => directory.Parent,
                _ => null,
            };
        }
    }
}
