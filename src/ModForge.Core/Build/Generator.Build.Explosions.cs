namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 1: Explosion (EXPL) scalar fields. The boom on a projectile/spell impact: deals
        // `damage` in `radius`, shows model FX + light, plays sound, can apply an objectEffect (MGEF)
        // AoE in radius. All FormLink refs (light/sound/impactDataSet/imageSpaceModifier/objectEffect)
        // are wired in pass 2 (WireMagicFxRefs). Built BEFORE Projectiles so a PROJ can resolve an
        // in-spec `explosion` editorId. ---
        public void BuildExplosions()
        {
            foreach (var ex in spec.Explosions)
            {
                var r = mod.Explosions.AddNew();
                r.EditorID = ex.EditorId;
                if (!string.IsNullOrEmpty(ex.Name)) r.Name = ex.Name;
                if (!string.IsNullOrWhiteSpace(ex.Model)) r.Model = new Model { File = ex.Model.Trim() };
                r.Damage = ex.Damage;
                r.Force = ex.Force;
                r.Radius = ex.Radius;
                if (ex.IsRadius is { } isr) r.ISRadius = isr;
                if (ex.Flags.Count > 0) r.Flags = ParseFlags<Explosion.Flag>(ex.Flags);
            }
        }
    }
}
