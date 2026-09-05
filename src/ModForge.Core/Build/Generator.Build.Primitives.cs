using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- XPRM: the one place ModForge builds a PlacedPrimitive ---------------------------------
        // Two callers: navCuts[] (Generator.Build.NavCuts.cs — Box, CollisionMarker yellow, 0.15) and
        // placements[].primitive (a trigger volume, any shape). They differ only in the four values
        // below, so they share this builder rather than each hardcoding a `new PlacedPrimitive`.
        // The contract + the vanilla evidence for the defaults live in Spec.Primitives.cs.

        // Vanilla trigger cosmetics — what defaultActivateSelfTRIG / defaultSetStageTRIG carry.
        private static readonly System.Drawing.Color TriggerPrimitiveColor =
            System.Drawing.Color.FromArgb(0, 204, 76, 51);
        private const float DefaultPrimitiveOpacity = 0.15f;

        private static PlacedPrimitive MakePrimitive(
            PlacedPrimitive.TypeEnum type, Noggog.P3Float bounds, System.Drawing.Color color, float opacity)
            => new() { Type = type, Bounds = bounds, Color = color, Unknown = opacity };

        // Spec-driven primitive. Returns null (after warning) when the spec cannot describe a volume.
        private PlacedPrimitive? BuildPrimitive(PrimitiveSpec p, string who)
        {
            if (p.Bounds is not { } b)
            {
                Warn($"  ! {who} primitive: needs `bounds` (the FULL size w×d×h of the volume, not half-extents) — skipped");
                return null;
            }

            var type = ParsePrimitiveType(p.Type, who);
            float x = b.X, y = b.Y, z = b.Z;

            // A sphere's three axes are one number (vanilla stores the diameter in all three). Giving
            // X alone is the ergonomic form; a mismatched pair is a spec mistake, not a squashed ball.
            if (type == PlacedPrimitive.TypeEnum.Sphere)
            {
                if (y == 0f && z == 0f) y = z = x;
                else if (y != x || z != x)
                {
                    Warn($"  ! {who} primitive: a sphere has ONE size — bounds {x}×{y}×{z} are not equal; using X on all three axes");
                    y = z = x;
                }
            }

            var color = p.Color is { } c ? ToColor(c) : TriggerPrimitiveColor;
            return MakePrimitive(type, new Noggog.P3Float(x, y, z), color, p.Opacity ?? DefaultPrimitiveOpacity);
        }

        // "box" / "sphere" / "portalBox" / "none", or a raw number (Skyrim.esm uses type 4 on 122 refs
        // and Mutagen has no name for it). Empty = Box, which is the 90% case.
        private PlacedPrimitive.TypeEnum ParsePrimitiveType(string type, string who)
        {
            if (string.IsNullOrWhiteSpace(type)) return PlacedPrimitive.TypeEnum.Box;
            if (Enum.TryParse<PlacedPrimitive.TypeEnum>(type.Trim(), ignoreCase: true, out var named)
                && Enum.IsDefined(named)) return named;
            if (int.TryParse(type.Trim(), out var raw) && raw is >= 0 and <= 255)
                return (PlacedPrimitive.TypeEnum)raw;
            Warn($"  ! {who} primitive: unknown type '{type}' (expected box / sphere / portalBox / none) — using box");
            return PlacedPrimitive.TypeEnum.Box;
        }
    }
}
