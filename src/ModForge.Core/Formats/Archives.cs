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
            if (pathFilter != null && !file.Path.Contains(pathFilter, StringComparison.OrdinalIgnoreCase))
                continue;
                
            var target = Path.Combine(outputDir, file.Path);
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
}
