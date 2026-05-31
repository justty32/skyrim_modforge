namespace ModForge;

/// <summary>
/// External-resource bundling — the file half of the asset pipeline (the record half is the
/// `model`/`sounds` wiring in <see cref="Generator"/>). ModForge does NOT author meshes/anims:
/// it copies a user-supplied source tree's recognised asset sub-trees next to the generated .esp
/// so the packaged mod is self-contained / MO2-ready. See docs/external_assets.md.
/// </summary>
public static class Assets
{
    /// <summary>
    /// The Data-relative folders Skyrim loads loose assets from. We copy these (case-insensitively)
    /// from a source dir into the output mod folder; anything else in the source is ignored.
    /// </summary>
    public static readonly string[] BundledFolders =
        { "Meshes", "Textures", "Sounds", "Sound", "Music", "Seq" };

    public sealed class BundleResult
    {
        public int FilesCopied { get; set; }
        public long BytesCopied { get; set; }
        public List<string> CopiedFolders { get; } = new();
        public List<string> Warnings { get; } = new();
    }

    /// <summary>
    /// Copy the recognised asset sub-trees of <paramref name="sourceDir"/> into
    /// <paramref name="outModDir"/>, preserving relative structure (so a source
    /// <c>Meshes/Mine/Bell.nif</c> lands at <c>outModDir/Meshes/Mine/Bell.nif</c>). Overwrites.
    /// Returns counts + warnings; never throws on a missing source (warns instead) so a build
    /// without assets still succeeds.
    /// </summary>
    public static BundleResult Bundle(string sourceDir, string outModDir)
    {
        var result = new BundleResult();
        if (string.IsNullOrWhiteSpace(sourceDir)) return result;
        if (!Directory.Exists(sourceDir))
        {
            result.Warnings.Add($"  ! assets source not found: {sourceDir} (nothing bundled)");
            return result;
        }

        foreach (var folder in BundledFolders)
        {
            // Match the source sub-dir case-insensitively (Linux is case-sensitive; Bethesda trees
            // are inconsistently cased). Copy under the canonical name above.
            var srcSub = Directory.EnumerateDirectories(sourceDir)
                .FirstOrDefault(d => string.Equals(Path.GetFileName(d), folder, StringComparison.OrdinalIgnoreCase));
            if (srcSub is null) continue;

            var destSub = Path.Combine(outModDir, folder);
            int before = result.FilesCopied;
            CopyTree(srcSub, destSub, result);
            if (result.FilesCopied > before) result.CopiedFolders.Add(folder);
        }

        if (result.FilesCopied == 0 && result.Warnings.Count == 0)
            result.Warnings.Add($"  ! assets source '{sourceDir}' has no Meshes/Textures/Sounds/… sub-folders — nothing bundled");
        return result;
    }

    private static void CopyTree(string src, string dest, BundleResult result)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(src, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
            result.FilesCopied++;
            result.BytesCopied += new FileInfo(file).Length;
        }
    }
}
