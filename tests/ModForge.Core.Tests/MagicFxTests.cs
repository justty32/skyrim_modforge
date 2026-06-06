using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Core.Tests;

// PROJ (Projectile) + EXPL (Explosion) builders. Master-free: every spec here builds entirely from
// in-spec records and external <master>:0xFORMID FormKeys (NOT resolved at build time), so no
// Skyrim.esm read is needed. These assert the record SLOTS + cross-record WIRING (PROJ→EXPL,
// MGEF→PROJ) the .esp stores — flight/boom rendering is unverifiable headless.
public class MagicFxTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("Test.esp");

    private static (ISkyrimMod Mod, BuildResult Result) Build(ModSpec spec)
    {
        var r = Generator.Build(spec, Key);
        return (r.Mod, r);
    }

    private static IExplosionGetter ExplOf(ISkyrimMod mod, string ed) =>
        Assert.Single(mod.Explosions, x => x.EditorID == ed);
    private static IProjectileGetter ProjOf(ISkyrimMod mod, string ed) =>
        Assert.Single(mod.Projectiles, x => x.EditorID == ed);

    [Fact]
    public void Explosion_Scalars_And_Refs_Are_Written()
    {
        var spec = new ModSpec
        {
            Explosions =
            {
                new ExplosionSpec
                {
                    EditorId = "MF_Expl",
                    Name = "Boom",
                    Model = "Effects\\FXEmptyExplosionArt.nif",
                    Damage = 25, Force = 10, Radius = 250, IsRadius = 1280,
                    Light = "Skyrim.esm:0x01CBB3",
                    Sound = "Skyrim.esm:0x02518F",
                    ImpactDataSet = "Skyrim.esm:0x0303FB",
                    ImageSpaceModifier = "Skyrim.esm:0x10FBE8",
                    ObjectEffect = "Skyrim.esm:0x0001C00E",
                    Flags = { "AlwaysUsesWorldOrientation" },
                },
            },
        };
        var (mod, _) = Build(spec);
        var e = ExplOf(mod, "MF_Expl");
        Assert.Equal("Boom", e.Name?.String);
        Assert.Equal("Effects\\FXEmptyExplosionArt.nif", e.Model?.File.GivenPath);
        Assert.Equal(25f, e.Damage);
        Assert.Equal(10f, e.Force);
        Assert.Equal(250f, e.Radius);
        Assert.Equal(1280f, e.ISRadius);
        Assert.Equal(0x01CBB3u, e.Light.FormKey.ID);
        Assert.Equal(0x02518Fu, e.Sound1.FormKey.ID);
        Assert.Equal(0x0303FBu, e.ImpactDataSet.FormKey.ID);
        Assert.Equal(0x10FBE8u, e.ImageSpaceModifier.FormKey.ID);
        Assert.Equal(0x0001C00Eu, e.ObjectEffect.FormKey.ID);
    }

    [Fact]
    public void Explosion_Radius_Defaults_To_200_When_Unset()
    {
        var (mod, _) = Build(new ModSpec { Explosions = { new ExplosionSpec { EditorId = "MF_E" } } });
        Assert.Equal(200f, ExplOf(mod, "MF_E").Radius);
    }

    [Fact]
    public void Projectile_Scalars_Defaults_And_Type_Are_Written()
    {
        var spec = new ModSpec
        {
            Projectiles = { new ProjectileSpec { EditorId = "MF_Proj", Name = "Bolt", Model = "Magic\\FireBoltProjectile.nif" } },
        };
        var (mod, _) = Build(spec);
        var p = ProjOf(mod, "MF_Proj");
        Assert.Equal("Bolt", p.Name?.String);
        Assert.Equal("Magic\\FireBoltProjectile.nif", p.Model?.File.GivenPath);
        Assert.Equal(Projectile.TypeEnum.Missile, p.Type);
        Assert.Equal(3000f, p.Speed);
        Assert.Equal(0f, p.Gravity);
        Assert.Equal(12000f, p.Range);
        Assert.Equal(10f, p.Lifetime);
        Assert.Equal(1f, p.ImpactForce);
        Assert.True(p.Flags.HasFlag(Projectile.Flag.Explosion));   // default ["Explosion"]
    }

    [Fact]
    public void Projectile_Type_And_Flags_Parse_From_Strings()
    {
        var spec = new ModSpec
        {
            Projectiles =
            {
                new ProjectileSpec
                {
                    EditorId = "MF_Beam", Type = "Beam",
                    Flags = new() { "Supersonic", "MuzzleFlash" },   // explicit list replaces the default
                    Speed = 5000, Gravity = 0.1f, Range = 8000, Lifetime = 3, ImpactForce = 2,
                    CollisionRadius = 4, ConeSpread = 1.5f,
                },
            },
        };
        var (mod, _) = Build(spec);
        var p = ProjOf(mod, "MF_Beam");
        Assert.Equal(Projectile.TypeEnum.Beam, p.Type);
        Assert.True(p.Flags.HasFlag(Projectile.Flag.Supersonic));
        Assert.True(p.Flags.HasFlag(Projectile.Flag.MuzzleFlash));
        Assert.False(p.Flags.HasFlag(Projectile.Flag.Explosion));   // a fresh list does not carry the default
        Assert.Equal(5000f, p.Speed);
        Assert.Equal(4f, p.CollisionRadius);
        Assert.Equal(1.5f, p.ConeSpread);
    }

    [Fact]
    public void Projectile_Resolves_InSpec_Explosion_By_EditorId()
    {
        var spec = new ModSpec
        {
            Explosions = { new ExplosionSpec { EditorId = "MF_Boom", Radius = 300 } },
            Projectiles = { new ProjectileSpec { EditorId = "MF_Bolt", Explosion = "MF_Boom" } },
        };
        var (mod, _) = Build(spec);
        var expl = ExplOf(mod, "MF_Boom");
        var proj = ProjOf(mod, "MF_Bolt");
        Assert.Equal(expl.FormKey, proj.Explosion.FormKey);
    }

    [Fact]
    public void Projectile_Resolves_Refs_Light_Muzzle_Sound()
    {
        var spec = new ModSpec
        {
            Projectiles =
            {
                new ProjectileSpec
                {
                    EditorId = "MF_Bolt",
                    Light = "Skyrim.esm:0x01CBB3",
                    MuzzleFlash = "Skyrim.esm:0x01CBB3",
                    Sound = "Skyrim.esm:0x03C8FE",
                },
            },
        };
        var (mod, _) = Build(spec);
        var p = ProjOf(mod, "MF_Bolt");
        Assert.Equal(0x01CBB3u, p.Light.FormKey.ID);
        Assert.Equal(0x01CBB3u, p.MuzzleFlash.FormKey.ID);
        Assert.Equal(0x03C8FEu, p.Sound.FormKey.ID);
    }

    [Fact]
    public void MagicEffect_Projectile_Ref_Wired_To_InSpec_Proj()
    {
        var spec = new ModSpec
        {
            Projectiles = { new ProjectileSpec { EditorId = "MF_Bolt" } },
            MagicEffects = { new MagicEffectSpec { EditorId = "MF_Mgef", Projectile = "MF_Bolt" } },
        };
        var (mod, _) = Build(spec);
        var proj = ProjOf(mod, "MF_Bolt");
        var mgef = Assert.Single(mod.MagicEffects, m => m.EditorID == "MF_Mgef");
        Assert.Equal(proj.FormKey, mgef.Projectile.FormKey);
    }

    [Fact]
    public void Build_Counts_Proj_And_Expl_As_TopLevel_Records()
    {
        var spec = new ModSpec
        {
            Explosions = { new ExplosionSpec { EditorId = "MF_E1" }, new ExplosionSpec { EditorId = "MF_E2" } },
            Projectiles = { new ProjectileSpec { EditorId = "MF_P1" } },
        };
        var (_, result) = Build(spec);
        Assert.Equal(3, result.Stats.TopLevelRecords);
    }

    [Fact]
    public void Validate_Flags_Duplicate_BadEnum_And_Bad_Radius()
    {
        var spec = new ModSpec
        {
            Explosions =
            {
                new ExplosionSpec { EditorId = "Dup" },
                new ExplosionSpec { EditorId = "Dup", Radius = -5 },
            },
            Projectiles =
            {
                new ProjectileSpec { EditorId = "" },                                 // empty editorId
                new ProjectileSpec { EditorId = "BadP", Type = "Nonsense", Speed = -1 },
                new ProjectileSpec { EditorId = "BadFlag", Flags = { "NotAFlag" } },
            },
        };
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("duplicate editorId 'Dup'"));
        Assert.Contains(problems, p => p.Contains("radius"));
        Assert.Contains(problems, p => p.Contains("empty editorId"));
        Assert.Contains(problems, p => p.Contains("Nonsense"));
        Assert.Contains(problems, p => p.Contains("speed"));
        Assert.Contains(problems, p => p.Contains("NotAFlag"));
    }

    [Fact]
    public void Validate_Clean_Spec_Has_No_MagicFx_Problems()
    {
        var spec = new ModSpec
        {
            Explosions = { new ExplosionSpec { EditorId = "MF_Boom", Radius = 250, Flags = { "AlwaysUsesWorldOrientation" } } },
            Projectiles = { new ProjectileSpec { EditorId = "MF_Bolt", Type = "Missile", Flags = { "Explosion" }, Explosion = "MF_Boom" } },
        };
        var problems = Generator.Validate(spec);
        Assert.DoesNotContain(problems, p => p.Contains("MF_Boom") || p.Contains("MF_Bolt"));
    }
}
