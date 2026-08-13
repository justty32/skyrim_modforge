using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;

internal static partial class Program
{
    // booktext — print a Book's localized Name + full BookText (the actual lore prose). The master is
    // LOCALIZED, so the text lives in .STRINGS inside "Skyrim - Interface.bsa"; we extract just the
    // English strings to a loose BSA-free temp folder (named in the ModKey's case — Linux is
    // case-sensitive) and open the overlay pointed straight at it, so resolution never needs the
    // (headless-absent) plugin-listings path. Lazy overlay + targeted single book → memory-safe.
    private static int BookText(string esmPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        var dataDir = Path.GetDirectoryName(Path.GetFullPath(esmPath)) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(esmPath);             // "Skyrim"
        var stringsDir = ProvisionEnglishStrings(dataDir, baseName);
        if (stringsDir is null) { Console.Error.WriteLine("  ! could not extract English strings — text unavailable"); return 1; }

        var prm = BinaryReadParameters.Default with
        {
            StringsParam = new StringsReadParameters
            {
                TargetLanguage = Language.English,
                StringsFolderOverride = stringsDir,
                BsaFolderOverride = stringsDir,   // BSA-free dir → no load-order archive scan
            },
        };
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(esmPath), SkyrimRelease.SkyrimSE, prm);
        foreach (var b in mod.EnumerateMajorRecords<IBookGetter>())
        {
            if (b.FormKey.ID != id) continue;
            string name; try { name = b.Name?.String ?? "-"; } catch { name = "<unresolved>"; }
            string body; try { body = b.BookText?.String ?? "-"; } catch { body = "<unresolved>"; }
            Console.WriteLine($"0x{id:X6}  EditorID={b.EditorID}  Name=\"{name}\"");
            Console.WriteLine("--- BookText ---");
            Console.WriteLine(body);
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Book in {Path.GetFileName(esmPath)}");
        return 0;
    }

    // Extract <base>_english.{strings,ilstrings,dlstrings} from "Skyrim - Interface.bsa" into a shared
    // BSA-free temp Strings/ folder, named in the ModKey's case (Skyrim_English.STRINGS). Returns the
    // folder, or null if the BSA / entries are absent.
    private static string? ProvisionEnglishStrings(string dataDir, string baseName)
    {
        var dir = Path.Combine(Path.GetTempPath(), "modforge-vanilla-strings", "Strings");
        Directory.CreateDirectory(dir);
        if (File.Exists(Path.Combine(dir, $"{baseName}_English.STRINGS"))) return dir;
        var bsa = Path.Combine(dataDir, "Skyrim - Interface.bsa");
        if (!File.Exists(bsa)) return null;
        bool any = false;
        var want = $"strings/{baseName.ToLowerInvariant()}_english.";
        foreach (var f in Archive.CreateReader(GameRelease.SkyrimSE, bsa).Files)
        {
            var p = f.Path.Replace('\\', '/');
            if (!p.StartsWith(want, StringComparison.OrdinalIgnoreCase)) continue;
            var ext = Path.GetExtension(p).ToUpperInvariant();
            File.WriteAllBytes(Path.Combine(dir, $"{baseName}_English{ext}"), f.GetSpan().ToArray());
            any = true;
        }
        return any ? dir : null;
    }
}
