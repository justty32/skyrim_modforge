namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 1: Projectile (PROJ) scalar fields. The bolt that flies on an Aimed cast: carries
        // its own visible `model` + flight physics. `type`/`flags` parse from strings (bad names warned
        // in validate, dropped here). FormLink refs (light/muzzleFlash/sound/explosion) are wired in
        // pass 2 (WireMagicFxRefs). Built AFTER Explosions so an in-spec `explosion` ref resolves. ---
        public void BuildProjectiles()
        {
            foreach (var pr in spec.Projectiles)
            {
                var r = mod.Projectiles.AddNew();
                r.EditorID = pr.EditorId;
                if (!string.IsNullOrEmpty(pr.Name)) r.Name = pr.Name;
                if (!string.IsNullOrWhiteSpace(pr.Model)) r.Model = new Model { File = pr.Model.Trim() };
                if (Enum.TryParse<Projectile.TypeEnum>(pr.Type, ignoreCase: true, out var ty)) r.Type = ty;
                r.Speed = pr.Speed;
                r.Gravity = pr.Gravity;
                r.Range = pr.Range;
                r.Lifetime = pr.Lifetime;
                r.ImpactForce = pr.ImpactForce;
                if (pr.Flags.Count > 0) r.Flags = ParseFlags<Projectile.Flag>(pr.Flags);
                if (pr.CollisionRadius is { } cr) r.CollisionRadius = cr;
                if (pr.ConeSpread is { } cs) r.ConeSpread = cs;
            }
        }

        // --- pass 2: PROJ + EXPL FormLink refs (may point forward, or at vanilla forms). Resolve()
        // skips empty refs, so only authored ones are wired. ---
        public void WireMagicFxRefs()
        {
            foreach (var ex in spec.Explosions)
            {
                if (!recordsByEd.TryGetValue(ex.EditorId, out var rec) || rec is not IExplosion expl) continue;
                Resolve($"explosion '{ex.EditorId}' light",              ex.Light,              fk => expl.Light.SetTo(fk));
                Resolve($"explosion '{ex.EditorId}' sound",              ex.Sound,              fk => expl.Sound1.SetTo(fk));
                Resolve($"explosion '{ex.EditorId}' impactDataSet",      ex.ImpactDataSet,      fk => expl.ImpactDataSet.SetTo(fk));
                Resolve($"explosion '{ex.EditorId}' imageSpaceModifier", ex.ImageSpaceModifier, fk => expl.ImageSpaceModifier.SetTo(fk));
                Resolve($"explosion '{ex.EditorId}' objectEffect",       ex.ObjectEffect,       fk => expl.ObjectEffect.SetTo(fk));
            }
            foreach (var pr in spec.Projectiles)
            {
                if (!recordsByEd.TryGetValue(pr.EditorId, out var rec) || rec is not IProjectile proj) continue;
                Resolve($"projectile '{pr.EditorId}' light",       pr.Light,       fk => proj.Light.SetTo(fk));
                Resolve($"projectile '{pr.EditorId}' muzzleFlash", pr.MuzzleFlash, fk => proj.MuzzleFlash.SetTo(fk));
                Resolve($"projectile '{pr.EditorId}' sound",       pr.Sound,       fk => proj.Sound.SetTo(fk));
                Resolve($"projectile '{pr.EditorId}' explosion",   pr.Explosion,   fk => proj.Explosion.SetTo(fk));
            }
        }
    }
}
