using Mutagen.Bethesda.Archives;

namespace ModForge;

// -------------------------------------------------------------------------------
//  Read-only overlays of the installed vanilla masters (Skyrim.esm, Update.esm, …),
//  opened lazily by file name and cached for the life of one build.
//
//  Pulled out of BuildContext because it is the build's only real I/O boundary: it
//  touches the Skyrim Data folder, unpacks .STRINGS out of a BSA into a temp folder,
//  and holds the disposables that keep those overlays alive. Everything else in a
//  build is pure spec-in / records-out. Keeping it separate means the file-system
//  behaviour can be tested on its own, and that a build step cannot reach into the
//  cache dictionaries by accident — it can only ask for a cache.
//
//  Warnings go out through the injected sink rather than a BuildContext reference, so
//  nothing here knows what a build is.
// -------------------------------------------------------------------------------
internal sealed class VanillaMasters : IDisposable
{
    private readonly string dataDir;
    private readonly Action<string> warn;

    private readonly Dictionary<string, ILinkCache<ISkyrimMod, ISkyrimModGetter>?> caches
        = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IDisposable> disposables = new();

    // Shared temp Strings/ folder holding the vanilla English .STRINGS we extract so a LOCALIZED
    // master's Name/Description resolve headless (see ProvisionStrings). Null once we know extraction
    // is impossible (then we open without strings — fine for the clone path, which masks Name out).
    private string? stringsDir;
    private bool stringsDirTried;
    private readonly HashSet<string> stringsDone = new(StringComparer.OrdinalIgnoreCase);

    public VanillaMasters(string dataDir, Action<string> warn)
    {
        this.dataDir = dataDir;
        this.warn = warn;
    }

    // Open (and cache) a master's link-cache by file name; warns + caches null if missing.
    // Skyrim.esm is LOCALIZED — its TranslatedString fields (Name/Description/BookText) live in
    // .STRINGS inside a BSA, and Mutagen's default resolve needs the plugins.txt/load-order listings
    // path (absent headless on Linux). We side-step that by pre-extracting the vanilla English
    // strings to a loose temp folder (ProvisionStrings) and opening the overlay pointed straight at
    // it — so the clone path can keep masking Name, AND npcPatches can now carry the real name.
    public ILinkCache<ISkyrimMod, ISkyrimModGetter>? Cache(string masterName)
    {
        if (caches.TryGetValue(masterName, out var cached)) return cached;
        var path = Path.Combine(dataDir, masterName);
        ILinkCache<ISkyrimMod, ISkyrimModGetter>? cache = null;
        if (!File.Exists(path))
            warn($"  ! master '{masterName}' not found at {path} (set MODFORGE_SKYRIM_DATA to your Data folder)");
        else
        {
            var sd = ProvisionStrings(masterName);
            ISkyrimModDisposableGetter getter = sd is null
                ? SkyrimMod.CreateFromBinaryOverlay(new ModPath(path), SkyrimRelease.SkyrimSE)
                : SkyrimMod.CreateFromBinaryOverlay(new ModPath(path), SkyrimRelease.SkyrimSE,
                    BinaryReadParameters.Default with
                    {
                        // A BSA-free folder for BsaFolderOverride keeps Mutagen from scanning archives
                        // (that scan reads the load order → the headless throw); loose strings only.
                        StringsParam = new StringsReadParameters
                        {
                            TargetLanguage = Language.English,
                            StringsFolderOverride = sd,
                            BsaFolderOverride = sd,
                        },
                    });
            disposables.Add(getter);
            cache = getter.ToImmutableLinkCache<ISkyrimMod, ISkyrimModGetter>();
        }
        caches[masterName] = cache;
        return cache;
    }

    // Lazily extract a master's English .STRINGS into a shared loose temp folder and return it (or
    // null if none could be provisioned). Vanilla masters (Skyrim/Update/Dawnguard/Dragonborn) keep
    // their strings inside "Skyrim - Interface.bsa"; we pull just the three tiny
    // <master>_english.{strings,ilstrings,dlstrings} files. They MUST be named in the ModKey's case
    // (e.g. "Skyrim_English.STRINGS") — Linux is case-sensitive and Mutagen looks up by ModKey case.
    // Only the targeted strings entries are read (Archive reading is lazy — the 250 MB master and the
    // 100 MB BSA are never materialized whole).
    private string? ProvisionStrings(string masterName)
    {
        if (!stringsDirTried)
        {
            stringsDirTried = true;
            try
            {
                var dir = Path.Combine(Path.GetTempPath(), "modforge-vanilla-strings", "Strings");
                Directory.CreateDirectory(dir);
                stringsDir = dir;
            }
            catch { stringsDir = null; }
        }
        if (stringsDir is null) return null;
        if (stringsDone.Contains(masterName)) return stringsDir;
        stringsDone.Add(masterName);

        var baseName = Path.GetFileNameWithoutExtension(masterName);             // "Skyrim"
        bool any = File.Exists(Path.Combine(stringsDir, $"{baseName}_English.STRINGS"));
        if (!any)
        {
            var bsa = Path.Combine(dataDir, "Skyrim - Interface.bsa");
            try
            {
                if (File.Exists(bsa))
                {
                    var want = $"strings/{baseName.ToLowerInvariant()}_english.";
                    foreach (var f in Archive.CreateReader(GameRelease.SkyrimSE, bsa).Files)
                    {
                        var p = f.Path.Replace('\\', '/');
                        if (!p.StartsWith(want, StringComparison.OrdinalIgnoreCase)) continue;
                        var ext = Path.GetExtension(p).ToUpperInvariant();        // ".STRINGS"
                        File.WriteAllBytes(Path.Combine(stringsDir, $"{baseName}_English{ext}"), f.GetSpan().ToArray());
                        any = true;
                    }
                }
            }
            catch (Exception ex)
            { warn($"  ! could not extract '{baseName}' English strings ({ex.GetType().Name}) — vanilla names may be blank"); }
        }
        return any ? stringsDir : null;
    }

    // Release the overlays. Safe as soon as the build stops reading them: every template clone /
    // cell-env copy is eager (DeepCopyIn / CopyCellEnv) and FormLinks only hold FormKeys, so nothing
    // the plugin write needs depends on these staying open.
    public void Dispose()
    {
        foreach (var d in disposables) d.Dispose();
        disposables.Clear();
    }
}
