namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // placements[].primitive / .collisionLayer — XPRM trigger volumes (Spec.Primitives.cs).
        // The failure mode these catch is the nastiest kind: a trigger that builds clean, loads
        // clean, and simply never fires. Nothing in-game says why, so the spec has to.
        private static readonly string[] PrimitiveTypeNames = { "box", "sphere", "portalbox", "none" };

        public void ValidatePrimitives()
        {
            foreach (var pl in spec.Placements)
            {
                string who = $"placement '{(string.IsNullOrWhiteSpace(pl.EditorId) ? pl.Base : pl.EditorId)}'";
                bool isNpc = pl.Kind.Equals("npc", StringComparison.OrdinalIgnoreCase);

                if (pl.CollisionLayer is not null && isNpc)
                    Problems.Add($"{who}: `collisionLayer` is REFR-only — an actor (ACHR) has no collision layer");

                if (pl.Primitive is not { } p) continue;

                if (isNpc)
                    Problems.Add($"{who}: `primitive` is REFR-only — an actor (ACHR) has no primitive volume; a trigger volume is a placed OBJECT whose base is an activator");

                if (p.Bounds is not { } b)
                    Problems.Add($"{who} primitive: needs `bounds` (the FULL size w×d×h of the volume, not half-extents) — a primitive with no bounds is an inert record");
                else if (b.X <= 0f || b.Y < 0f || b.Z < 0f)
                    Problems.Add($"{who} primitive: bounds must be positive (got {b.X}×{b.Y}×{b.Z}) — a zero-width volume can never be entered");
                else if (!IsSphere(p.Type) && (b.Y <= 0f || b.Z <= 0f))
                    Problems.Add($"{who} primitive: bounds must be positive on all three axes (got {b.X}×{b.Y}×{b.Z}) — only a sphere may give X alone");

                if (!string.IsNullOrWhiteSpace(p.Type)
                    && !PrimitiveTypeNames.Contains(p.Type.Trim().ToLowerInvariant())
                    && !int.TryParse(p.Type.Trim(), out _))
                    Problems.Add($"{who} primitive: unknown type '{p.Type}' (expected box / sphere / portalBox / none)");

                if (p.Opacity is { } op && (op < 0f || op > 1f))
                    Problems.Add($"{who} primitive: opacity must be within 0..1 (got {op}) — it is the CK render-window fill, not a game value");

                if (string.IsNullOrWhiteSpace(pl.Base))
                    Problems.Add($"{who} primitive: a volume still needs a `base` — vanilla triggers use an activator (e.g. Skyrim.esm:0x048AC0 defaultActivateSelfTRIG) or your own ACTI carrying an OnTriggerEnter script");
            }
        }

        private static bool IsSphere(string type) =>
            type.Trim().Equals("sphere", StringComparison.OrdinalIgnoreCase);
    }
}
