namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Validate — PROJ (Projectile) + EXPL (Explosion) guardrails.
    //
    //  editorId presence/uniqueness is handled centrally by Reg() (Generator.Validate.cs).
    //  Here: enum/flag name validity, positive radius/speed where set, and referential
    //  integrity of every optional FormLink (light/sound/explosion/objectEffect/…).
    // -------------------------------------------------------------------------------
    private sealed partial class ValidateContext
    {
        public void ValidateProjectiles()
        {
            foreach (var pr in spec.Projectiles)
            {
                var w = $"projectile '{pr.EditorId}'";
                if (!string.IsNullOrWhiteSpace(pr.Type)
                    && !Enum.TryParse<Mutagen.Bethesda.Skyrim.Projectile.TypeEnum>(pr.Type, true, out _))
                    Problems.Add($"{w} invalid type '{pr.Type}' (Missile|Lobber|Beam|Flame|Cone|Barrier|Arrow)");
                foreach (var f in pr.Flags)
                    if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.Projectile.Flag>(f, true, out _))
                        Problems.Add($"{w} invalid flag '{f}' (Explosion|Supersonic|Hitscan|MuzzleFlash|CanBeDisabled|PassThroughSmallTransparent|Rotation|…)");
                if (pr.Speed <= 0) Problems.Add($"{w} speed {pr.Speed} must be > 0");
                if (pr.CollisionRadius is { } cr && cr < 0) Problems.Add($"{w} collisionRadius {cr} must be >= 0");
                CheckRef(pr.Light, $"{w} light");
                CheckRef(pr.MuzzleFlash, $"{w} muzzleFlash");
                CheckRef(pr.Sound, $"{w} sound");
                CheckRef(pr.Explosion, $"{w} explosion");
            }
        }

        public void ValidateExplosions()
        {
            foreach (var ex in spec.Explosions)
            {
                var w = $"explosion '{ex.EditorId}'";
                foreach (var f in ex.Flags)
                    if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.Explosion.Flag>(f, true, out _))
                        Problems.Add($"{w} invalid flag '{f}' (AlwaysUsesWorldOrientation|KnockDownAlways|KnockDownByFormula|IgnoreLosCheck|Chain|…)");
                if (ex.Radius <= 0) Problems.Add($"{w} radius {ex.Radius} must be > 0");
                if (ex.IsRadius is { } isr && isr < 0) Problems.Add($"{w} isRadius {isr} must be >= 0");
                CheckRef(ex.Light, $"{w} light");
                CheckRef(ex.Sound, $"{w} sound");
                CheckRef(ex.ImpactDataSet, $"{w} impactDataSet");
                CheckRef(ex.ImageSpaceModifier, $"{w} imageSpaceModifier");
                CheckRef(ex.ObjectEffect, $"{w} objectEffect");
            }
        }

        // IMAD: brightness/contrast/saturation/duration are multipliers/seconds — must be >= 0.
        // tintAmount is clamped to 0..1 at build, so it's non-fatal (not checked here).
        public void ValidateImageSpaceModifiers()
        {
            foreach (var im in spec.ImageSpaceModifiers)
            {
                var w = $"imageSpaceModifier '{im.EditorId}'";
                if (im.BrightnessMultiplier < 0) Problems.Add($"{w} brightnessMultiplier {im.BrightnessMultiplier} must be >= 0");
                if (im.Contrast < 0) Problems.Add($"{w} contrast {im.Contrast} must be >= 0");
                if (im.Saturation < 0) Problems.Add($"{w} saturation {im.Saturation} must be >= 0");
                if (im.Duration < 0) Problems.Add($"{w} duration {im.Duration} must be >= 0");
            }
        }
    }
}
