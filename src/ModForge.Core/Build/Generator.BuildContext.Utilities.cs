namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        private readonly string skyrimData;

        // The build's only file-system dependency, behind its own type (Build/VanillaMasters.cs).
        // BuildContext hands it a warning sink and otherwise just asks it for caches.
        private readonly VanillaMasters masters;

        // Open (and cache) a master's link-cache by file name; warns + caches null if missing.
        private ILinkCache<ISkyrimMod, ISkyrimModGetter>? MasterCache(string masterName)
            => masters.Cache(masterName);

        // Resolve a vanilla record (by "<master>:0xFORMID" ref) to clone from. False (caller warns)
        // if the ref is malformed or the master/record can't be found. Ref PARSING stays here — the
        // master service knows about files, not about how this generator spells a reference.
        private bool TryResolveTemplate<T>(string templateRef, out T? tmpl) where T : class, ISkyrimMajorRecordGetter
        {
            tmpl = null;
            if (string.IsNullOrWhiteSpace(templateRef)) return false;
            int colon = templateRef.IndexOf(':');
            if (colon <= 0 || !TryExternalRef(templateRef, out var fk)) return false;
            var cache = MasterCache(templateRef[..colon].Trim());
            return cache is not null && cache.TryResolve<T>(fk, out tmpl);
        }
    }
}
