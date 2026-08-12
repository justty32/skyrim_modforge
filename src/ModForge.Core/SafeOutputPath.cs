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
}
