using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Noggog;

namespace ModForge;

public static class Archives
{
    /// <summary>
    /// Extracts files from a Bethesda archive (.bsa or .ba2) matching a path filter.
    /// </summary>
    public static int Extract(string archivePath, string outputDir, string? pathFilter = null)
    {
        if (!File.Exists(archivePath)) return 0;
        
        var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath, IFileSystemExt.DefaultFilesystem);
        int count = 0;
        
        foreach (var file in reader.Files)
        {
            if (pathFilter != null && !Match(file.Path, pathFilter))
                continue;

            // An archive path is stored with backslashes. Windows treats those as separators, but on
            // Linux a backslash is an ordinary filename character — combining raw would produce ONE
            // file literally named "sound\voice\...\x.fuz" instead of a tree, which then breaks every
            // caller that reads the name back (voice-annotate parses the FormID out of it).
            // ResolveUnder also keeps a hostile archive from writing outside outputDir: a .bsa is an
            // untrusted input, it is whatever mod the user pointed us at.
            var target = SafeOutputPath.ResolveUnder(outputDir, Normalize(file.Path));
            var dir = Path.GetDirectoryName(target);
            if (dir != null) Directory.CreateDirectory(dir);

            using var sourceStream = file.AsStream();
            using var targetStream = File.Create(target);
            sourceStream.CopyTo(targetStream);
            count++;
        }
        
        return count;
    }

    /// <summary>
    /// Lists all file paths in an archive.
    /// </summary>
    public static List<string> List(string archivePath)
    {
        if (!File.Exists(archivePath)) return new();
        var reader = Archive.CreateReader(GameRelease.SkyrimSE, archivePath, IFileSystemExt.DefaultFilesystem);
        return reader.Files.Select(f => f.Path).ToList();
    }

    // Archive paths are stored with backslashes, but callers build filters with forward slashes
    // (Program.Build.Voice.Extract composes "sound/voice/<plugin>/<voiceType>/"). Compare both sides
    // in one separator style so the filter matches whichever style the reader hands back.
    private static bool Match(string archivePath, string filter) =>
        archivePath.Replace('\\', '/').Contains(filter.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);

    // ...and translate to THIS platform's separator before touching the filesystem.
    private static string Normalize(string archivePath) =>
        archivePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
}
