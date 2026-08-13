using Mutagen.Bethesda.Archives;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        private readonly string skyrimData;
        private readonly List<IDisposable> masterDisposables = new();

        // Shared temp Strings/ folder holding the vanilla English .STRINGS we extract so a LOCALIZED
        // master's Name/Description resolve headless (see ProvisionStrings). Null once we know extraction
        // is impossible (then we open without strings — fine for the clone path, which masks Name out).
        private string? stringsDir;
        private bool stringsDirTried;
        private readonly HashSet<string> stringsDone = new(StringComparer.OrdinalIgnoreCase);

        // Open (and cache) a master's link-cache by file name; warns + caches null if missing.
        // Skyrim.esm is LOCALIZED — its TranslatedString fields (Name/Description/BookText) live in
        // .STRINGS inside a BSA, and Mutagen's default resolve needs the plugins.txt/load-order listings
        // path (absent headless on Linux). We side-step that by pre-extracting the vanilla English
        // strings to a loose temp folder (ProvisionStrings) and opening the overlay pointed straight at
        // it — so the clone path can keep masking Name, AND npcPatches can now carry the real name.
        private ILinkCache<ISkyrimMod, ISkyrimModGetter>? MasterCache(string masterName)
        {
            if (masterCaches.TryGetValue(masterName, out var cached)) return cached;
            var path = Path.Combine(skyrimData, masterName);
            ILinkCache<ISkyrimMod, ISkyrimModGetter>? cache = null;
            if (!File.Exists(path))
                Warn($"  ! master '{masterName}' not found at {path} (set MODFORGE_SKYRIM_DATA to your Data folder)");
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
                masterDisposables.Add(getter);
                cache = getter.ToImmutableLinkCache<ISkyrimMod, ISkyrimModGetter>();
            }
            masterCaches[masterName] = cache;
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
                var bsa = Path.Combine(skyrimData, "Skyrim - Interface.bsa");
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
                { Warn($"  ! could not extract '{baseName}' English strings ({ex.GetType().Name}) — vanilla names may be blank"); }
            }
            return any ? stringsDir : null;
        }

        // Resolve a vanilla record (by "<master>:0xFORMID" ref) to clone from. False (caller warns)
        // if the ref is malformed or the master/record can't be found.
        private bool TryResolveTemplate<T>(string templateRef, out T? tmpl) where T : class, ISkyrimMajorRecordGetter
        {
            tmpl = null;
            if (string.IsNullOrWhiteSpace(templateRef)) return false;
            int colon = templateRef.IndexOf(':');
            if (colon <= 0 || !TryExternalRef(templateRef, out var fk)) return false;
            var cache = MasterCache(templateRef[..colon].Trim());
            return cache is not null && cache.TryResolve<T>(fk, out tmpl);
        }

        // Build a PackageDataLocation: an authored placed-ref → LocationTarget anchored at that
        // ref, else LocationFallback(NearSelf) — anchors at the actor's current position with no
        // external dependency. NEVER use NearEditorLocation: it needs a CK-set Editor Location on
        // the NPC; Mutagen-generated NPCs don't have one, so sandbox/travel silently no-ops in-game.
        // Called ONLY from WireDeferredLocations — every location slot is deferred, because the ref may
        // be a placement editorId or a references[] label that doesn't exist during BuildPackageData.
        private PackageDataLocation MakeLocationSlot(string slotName, string packageEd, string refStr, uint radius)
        {
            // An explicit "area:<ref>" prefix (author declaring "a region, not that one object") strips to
            // the bare ref here — every location slot funnels through this one method, so this is the single
            // point that has to understand it. No-op on an unprefixed ref (byte-identical old behaviour).
            refStr = StripAreaPrefix(refStr);

            // An "alias:<name>" / "aliasLoc:<name>" location → LocationFallback bound to the ownerQuest's
            // alias index (AliasForReference = the alias holds a ref; AliasForLocation = a location alias).
            if (TryResolveAliasIndex(refStr, packageEd, out var isLocAlias, out var aliasIdx) && aliasIdx >= 0)
                return new PackageDataLocation
                {
                    Name = slotName,
                    Location = new LocationTargetRadius
                    {
                        Target = new LocationFallback
                        {
                            Type = isLocAlias
                                ? LocationTargetRadius.LocationType.AliasForLocation
                                : LocationTargetRadius.LocationType.AliasForReference,
                            Data = aliasIdx,
                        },
                        Radius = radius,
                    }
                };

            if (!string.IsNullOrWhiteSpace(refStr)
                && TryResolveRef(refStr, formKeyByEd, out var fk))
            {
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
                return new PackageDataLocation
                {
                    Name = slotName,
                    Location = new LocationTargetRadius
                    {
                        Target = new LocationTarget { Link = new FormLink<IPlacedGetter>(fk) },
                        Radius = radius,
                    }
                };
            }
            if (!string.IsNullOrWhiteSpace(refStr))
                Warn($"  ! package '{packageEd}' {slotName.ToLowerInvariant()} '{refStr}' unresolved — falling back to NearSelf");
            return new PackageDataLocation
            {
                Name = slotName,
                Location = new LocationTargetRadius
                {
                    Target = new LocationFallback { Type = LocationTargetRadius.LocationType.NearSelf },
                    Radius = radius,
                }
            };
        }
    }
}
