using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- Removals (Idea #24 §E eraser): DISABLE existing vanilla placed refs -------------------
        // Each `removals[]` entry is a "<master>:0xFORMID" of an existing placed ref (REFR/ACHR). We
        // resolve its full context from the master link cache and GetOrAddAsOverride it INTO our mod —
        // which also pulls its parent cell (and worldspace, for exterior) in as overrides automatically,
        // same FormKey — then set the record-header InitiallyDisabled flag (0x800) and bury it far below
        // (Z −30000) so a havok object can't linger where it stood. This is the standard "disable vanilla
        // clutter" patch (USSEP-style); the ref still exists (reversible), just invisible/non-collidable.
        //
        // NOTE: relies on the master link cache (MODFORGE_SKYRIM_DATA / Steam path). Unresolvable refs
        // are warned and skipped. Interior refs in named cells also work (GetOrAddAsOverride copies the
        // cell's Name FormID reference, not a re-resolved string, so it's headless-safe).
        //
        // Also consumes `referenceRemovals` — the vanilla originals that an anchor:"replace" reference
        // stood in for (BuildReferences authored our own persistent copy in their place, so the original
        // has to go or the player sees two chairs).
        public void BuildRemovals()
        {
            foreach (var refStr in spec.Removals.Concat(referenceRemovals))
            {
                if (string.IsNullOrWhiteSpace(refStr)) continue;
                if (!TryExternalRef(refStr, out var fk))
                { Warn($"  ! removal '{refStr}' is not a <master>:0xFORMID ref — skipped"); continue; }
                int colon = refStr.IndexOf(':');
                var cache = MasterCache(refStr[..colon].Trim());
                if (cache is null)
                { Warn($"  ! removal '{refStr}': master link cache unavailable (set MODFORGE_SKYRIM_DATA) — skipped"); continue; }

                if (!cache.TryResolveContext<IPlaced, IPlacedGetter>(fk, out var ctx))
                { Warn($"  ! removal '{refStr}': not a resolvable placed ref (REFR/ACHR) in its master — skipped"); continue; }

                var ov = ctx.GetOrAddAsOverride(mod);       // also overrides the parent cell/worldspace chain
                ov.MajorRecordFlagsRaw |= 0x800;             // InitiallyDisabled
                if (ov.Placement is { } p)                   // bury so a havok object doesn't linger in place
                    p.Position = new Noggog.P3Float(p.Position.X, p.Position.Y, p.Position.Z - 30000f);
            }
        }
    }
}
