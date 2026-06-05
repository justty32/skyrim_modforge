namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // Open (and cache) a master's link-cache by file name; warns + caches null if missing.
        // NOTE: Skyrim.esm is LOCALIZED, so its TranslatedString fields (Name/Description/
        // BookText) live in .STRINGS inside a BSA. We must NOT DeepCopy those (it triggers
        // an all-string-source resolve that needs the plugins.txt/load-order listings path,
        // absent headless on Linux). The weapon/book clone uses a TranslationMask to skip
        // exactly those fields (we override them anyway), so no string resolution happens.
        private ILinkCache<ISkyrimMod, ISkyrimModGetter>? MasterCache(string masterName)
        {
            if (masterCaches.TryGetValue(masterName, out var cached)) return cached;
            var path = Path.Combine(skyrimData, masterName);
            ILinkCache<ISkyrimMod, ISkyrimModGetter>? cache = null;
            if (!File.Exists(path))
                Warn($"  ! master '{masterName}' not found at {path} (set MODFORGE_SKYRIM_DATA to your Data folder)");
            else
            {
                var getter = SkyrimMod.CreateFromBinaryOverlay(new ModPath(path), SkyrimRelease.SkyrimSE);
                masterDisposables.Add(getter);
                cache = getter.ToImmutableLinkCache<ISkyrimMod, ISkyrimModGetter>();
            }
            masterCaches[masterName] = cache;
            return cache;
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
        private PackageDataLocation MakeLocationSlot(string slotName, string ownerLabel, string refStr, uint radius)
        {
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
                Warn($"  ! {ownerLabel} location '{refStr}' unresolved — falling back to NearSelf");
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
