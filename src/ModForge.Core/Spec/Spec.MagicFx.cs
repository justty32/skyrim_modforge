namespace ModForge;

// --- Magic combat FX records: the visible parts of a destruction spell -------------------
// PROJ (the flying bolt) + EXPL (the boom on impact). A MagicEffect's `projectile`/`explosion`
// refs (Spec.Magic.cs) point at these so a custom Aimed spell gets its own travelling art + AoE.

// Projectile (PROJ): the thing that flies from the caster to the target on an Aimed cast. Carries
// its own visible `model` (.nif) + flight physics + the `explosion` (EXPL) it triggers on impact.
// `type` (Missile = firebolt-style; Lobber/Beam/Flame/Cone/Barrier/Arrow) and `flags`
// (Explosion/Supersonic/Hitscan/MuzzleFlash/CanBeDisabled/PassThroughSmallTransparent/Rotation/…)
// parse from strings; bad names are warned (validate) + dropped (build). All refs are optional —
// `explosion` may be an in-spec EXPL editorId (resolved in pass 2 after the formKey table exists).
public sealed class ProjectileSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Missile";          // Projectile.TypeEnum
    public float Speed { get; set; } = 3000;
    public float Gravity { get; set; } = 0;
    public float Range { get; set; } = 12000;
    public float Lifetime { get; set; } = 10;
    public float ImpactForce { get; set; } = 1;
    public List<string> Flags { get; set; } = new() { "Explosion" };   // Projectile.Flag names
    public string Model { get; set; } = "";                // .nif — VERIFY against vanilla (wrong = invisible)
    public string Light { get; set; } = "";                // LIGT ref
    public string MuzzleFlash { get; set; } = "";          // LIGT ref
    public string Sound { get; set; } = "";                // Sound ref (in-flight)
    public string Explosion { get; set; } = "";            // EXPL ref — the boom on impact
    public float? CollisionRadius { get; set; }
    public float? ConeSpread { get; set; }
}

// Explosion (EXPL): the impact event a projectile (or a spell) triggers — deals `damage` in `radius`,
// shows the `model` FX + `light`, plays `sound`, swaps `imageSpaceModifier`, and can apply an
// `objectEffect` (MGEF) to everything in radius (the AoE). `isRadius` is the imagespace blend radius.
// `flags` (AlwaysUsesWorldOrientation/KnockDownAlways/IgnoreLosCheck/Chain/…) parse from strings.
// All refs optional. Built before Projectiles so a PROJ can resolve an in-spec `explosion` directly.
public sealed class ExplosionSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public float Damage { get; set; } = 0;
    public float Force { get; set; } = 0;
    public float Radius { get; set; } = 200;
    public float? IsRadius { get; set; }                   // imagespace radius
    public string Light { get; set; } = "";                // LIGT ref
    public string Sound { get; set; } = "";                // Sound ref → Explosion.Sound1
    public string ImpactDataSet { get; set; } = "";        // IPDS ref
    public string ImageSpaceModifier { get; set; } = "";   // IMAD ref
    public string ObjectEffect { get; set; } = "";         // MGEF ref — the in-radius AoE effect
    public List<string> Flags { get; set; } = new();       // Explosion.Flag names
    public string Model { get; set; } = "";                // .nif FX art
}

// ImageSpace Modifier (IMAD): a screen-space post-process applied/removed at runtime (the way vanilla
// Night-Eye / cinematic flashes work). Used here to brighten the whole view ("daylight" feel) for as
// long as a magic effect script keeps it applied. Referenced by `ExplosionSpec.imageSpaceModifier`
// (existing) AND by a Papyrus ImageSpaceModifier property (a script applies it via ApplyCrossFade).
//
// Mutagen models every IMAD curve as a list of value+time keyframes (animatable). We only need a
// single static value, so the builder writes ONE keyframe per field. `tintColor`+`tintAmount` map to
// a single ColorFrame (amount = the colour's alpha). `brightnessMultiplier` is CinematicBrightnessMult
// (1.0 = no change; >1 brightens). All optional — a bare editorId yields a no-op IMAD.
public sealed class ImageSpaceModifierSpec
{
    public string EditorId { get; set; } = "";
    public float BrightnessMultiplier { get; set; } = 1.6f;   // CinematicBrightnessMult (1=neutral, >1 brighter)
    public float Contrast { get; set; } = 1.0f;               // CinematicContrastMult
    public float Saturation { get; set; } = 1.0f;             // CinematicSaturationMult
    public ColorSpec? TintColor { get; set; }                 // optional screen tint (RGB)
    public float TintAmount { get; set; } = 0f;               // tint strength 0..1 (mapped to colour alpha)
    public float Duration { get; set; } = 1.0f;               // IMAD fade/animate duration (s)
    public bool Animatable { get; set; } = false;             // animate curves over Duration
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    // Magic combat FX (Spec.MagicFx.cs): a Projectile (PROJ) is the flying bolt; an Explosion
    // (EXPL) is the boom on impact. A MagicEffect's projectile/explosion refs point at these.
    public List<ProjectileSpec> Projectiles { get; set; } = new();

    public List<ExplosionSpec> Explosions { get; set; } = new();

    // ImageSpace Modifiers (IMAD): screen-space post-process (brightness/tint) a magic-effect script
    // applies at runtime. Referenced by Explosions[].imageSpaceModifier or a Papyrus property.
    public List<ImageSpaceModifierSpec> ImageSpaceModifiers { get; set; } = new();
}
