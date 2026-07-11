using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- Overrides (Idea #24 numpad editor): MOVE existing placed refs ------------------------
        // The sibling of BuildRemovals — same resolve machinery (master link cache →
        // TryResolveContext → GetOrAddAsOverride pulls the parent cell/worldspace chain in, the
        // path in-game confirmed 2026-07-11 via removals), but instead of disable+bury we re-stamp
        // the transform: position/rotation always (that's the point of the entry), scale only when
        // the spec says so (null = keep the original record's XSCL untouched).
        //
        // Runs BEFORE BuildRemovals in Build.cs: if the same ref appears in both lists the removal's
        // flag+bury lands LAST and wins — a removed ref stays removed (validate also flags the
        // contradiction, so this ordering is a backstop, not the UX).
        public void BuildOverrides()
        {
            foreach (var o in spec.Overrides)
            {
                if (string.IsNullOrWhiteSpace(o.Ref)) continue;
                if (!TryExternalRef(o.Ref, out var fk))
                { Warn($"  ! override '{o.Ref}' is not a <master>:0xFORMID ref — skipped"); continue; }
                int colon = o.Ref.IndexOf(':');
                var cache = MasterCache(o.Ref[..colon].Trim());
                if (cache is null)
                { Warn($"  ! override '{o.Ref}': master link cache unavailable (set MODFORGE_SKYRIM_DATA) — skipped"); continue; }

                if (!cache.TryResolveContext<IPlaced, IPlacedGetter>(fk, out var ctx))
                { Warn($"  ! override '{o.Ref}': not a resolvable placed ref (REFR/ACHR) in its master — skipped"); continue; }

                var ov = ctx.GetOrAddAsOverride(mod);       // also overrides the parent cell/worldspace chain
                ov.Placement = new Placement
                {
                    Position = new Noggog.P3Float(o.Position.X, o.Position.Y, o.Position.Z),
                    Rotation = new Noggog.P3Float(Deg2Rad(o.Rotation.X), Deg2Rad(o.Rotation.Y), Deg2Rad(o.Rotation.Z)),
                };
                if (o.Scale is float s)                     // null = keep original XSCL; 1.0 = drop it (engine default)
                {
                    if (ov is PlacedObject po) po.Scale = s == 1f ? null : s;
                    else if (ov is PlacedNpc pn) pn.Scale = s == 1f ? null : s;  // ACHR ignores XSCL in-game; keep the record honest anyway
                }
            }
        }
    }
}
