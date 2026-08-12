using System.Security.Cryptography;
using System.Text;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;

internal static partial class Program
{
    internal static BinaryReadParameters GameDataReadParameters(
        string pluginPath, string? stringsOverride, out bool localized)
    {
        var dataDir = Path.GetDirectoryName(pluginPath) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(pluginPath);
        using (var probe = SkyrimMod.CreateFromBinaryOverlay(new ModPath(pluginPath), SkyrimRelease.SkyrimSE))
            localized = probe.UsingLocalization;
        if (!localized) return BinaryReadParameters.Default;

        var stringsDir = stringsOverride ?? ProvisionEnglishStringsAnyBsa(dataDir, baseName);
        if (stringsDir is null)
        {
            Console.Error.WriteLine($"  ! {baseName} is localized but no <base>_english.* found in any .bsa beside it — names/text may be blank");
            return BinaryReadParameters.Default;
        }
        return BinaryReadParameters.Default with
        {
            StringsParam = new StringsReadParameters
            {
                TargetLanguage = Language.English,
                StringsFolderOverride = stringsDir,
                BsaFolderOverride = stringsDir,
            },
        };
    }

    // Scan every BSA beside the plugin; localized masters do not have inline stage-log text.
    private static string? ProvisionEnglishStringsAnyBsa(string dataDir, string baseName)
    {
        var archives = Directory.EnumerateFiles(dataDir)
            .Where(path => Path.GetExtension(path).Equals(".bsa", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var sourceKey = EnglishStringsSourceKey(dataDir, archives);
        var dir = Path.Combine(Path.GetTempPath(), "modforge-gamedata-strings", baseName, sourceKey, "Strings");
        Directory.CreateDirectory(dir);
        var completed = Path.Combine(dir, ".complete");
        if (File.Exists(completed)) return dir;
        // A prior process may have stopped midway through this fingerprint. Clear only our three
        // generated tables, then mark complete after all matching archive entries are written.
        foreach (var extension in new[] { ".STRINGS", ".DLSTRINGS", ".ILSTRINGS" })
        {
            var old = Path.Combine(dir, $"{baseName}_English{extension}");
            if (File.Exists(old)) File.Delete(old);
        }
        var want = $"strings/{baseName.ToLowerInvariant()}_english.";
        var any = false;
        foreach (var bsa in archives)
        {
            try
            {
                foreach (var file in Archive.CreateReader(GameRelease.SkyrimSE, bsa).Files)
                {
                    var path = file.Path.Replace('\\', '/');
                    if (!path.StartsWith(want, StringComparison.OrdinalIgnoreCase)) continue;
                    var extension = Path.GetExtension(path).ToUpperInvariant();
                    File.WriteAllBytes(Path.Combine(dir, $"{baseName}_English{extension}"), file.GetSpan().ToArray());
                    any = true;
                }
            }
            catch { /* One unreadable archive must not hide strings available in another archive. */ }
        }
        if (!any) return null;
        File.WriteAllText(completed, sourceKey);
        return dir;
    }

    internal static string EnglishStringsSourceKey(string dataDir, IEnumerable<string> archives)
    {
        var fingerprint = new StringBuilder(Path.GetFullPath(dataDir));
        foreach (var archive in archives.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var info = new FileInfo(archive);
            fingerprint.Append('\n').Append(info.FullName).Append('|').Append(info.Length)
                .Append('|').Append(info.LastWriteTimeUtc.Ticks);
        }
        return Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(fingerprint.ToString()))).Substring(0, 16);
    }
}
