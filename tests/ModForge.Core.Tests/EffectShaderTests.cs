using System.Drawing;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

public class EffectShaderTests
{
    private static ModSpec Sample() => new()
    {
        PluginName = "ShaderTest.esp",
        EffectShaders =
        {
            new EffectShaderSpec
            {
                EditorId = "MF_FireGlow",
                FillTexture = "MFVfx/firefill.dds",
                ParticleTexture = "MFVfx/spark.dds",
                PaletteTexture = "MFVfx/gradient.dds",
                Flags = { "ParticleGrayscaleColor" },
                Membrane = new()
                {
                    FillColor = new() { R = 255, G = 140, B = 40 },
                    EdgeColor = new() { R = 255, G = 80, B = 0 },
                    FillFadeInTime = 0.25f, FillFullTime = 1f, FillFadeOutTime = 0.5f,
                    EdgeFallOff = 0.8f,
                },
                Particle = new()
                {
                    PersistentCount = 80, Lifetime = 1.2f, InitialSpeed = 30, Acceleration = -10,
                    ScaleKeys = { new() { Time = 0, Scale = 0.4f }, new() { Time = 1, Scale = 1.2f } },
                    ColorKeys =
                    {
                        new() { Time = 0, Color = new() { R = 255, G = 200, B = 50 }, Alpha = 1 },
                        new() { Time = 1, Color = new() { R = 120, G = 20, B = 0 }, Alpha = 0 },
                    },
                },
            },
        },
        MagicEffects =
        {
            new MagicEffectSpec
            {
                EditorId = "MF_FireEffect", Archetype = "ValueModifier", ActorValue = "Health",
                HitShader = "MF_FireGlow", EnchantShader = "MF_FireGlow",
            },
        },
    };

    [Fact]
    public void Build_EmitsTextureOnlyEfshAndParticleKeys()
    {
        var result = Generator.Build(Sample(), ModKey.FromNameAndExtension("ShaderTest.esp"));
        var shader = Assert.Single(result.Mod.EffectShaders);
        Assert.Equal("MF_FireGlow", shader.EditorID);
        Assert.Equal("MFVfx/firefill.dds", shader.FillTexture.GivenPath);
        Assert.Equal("MFVfx/spark.dds", shader.ParticleShaderTexture.GivenPath);
        Assert.Equal("MFVfx/gradient.dds", shader.MembranePaletteTexture.GivenPath);
        Assert.Equal("MFVfx/gradient.dds", shader.ParticlePaletteTexture.GivenPath);
        Assert.Equal(EffectShader.BlendMode.SourceAlpha, shader.MembraneSourceBlendMode);
        Assert.Equal(EffectShader.BlendMode.One, shader.ParticleDestBlendMode);
        Assert.Equal(Color.FromArgb(0, 255, 140, 40), shader.FillColorKey1);
        Assert.Equal(80, shader.ParticlePeristentCount);
        Assert.Equal(0.4f, shader.ParticleScaleKey1);
        Assert.Equal(1.2f, shader.ParticleScaleKey2);
        Assert.Equal(Color.FromArgb(0, 120, 20, 0), shader.ColorKey2);
        Assert.True(shader.Flags.HasFlag(EffectShader.Flag.ParticleGrayscaleColor));
    }

    [Fact]
    public void Build_WiresHitAndEnchantShadersIntoMagicEffect()
    {
        var mod = Generator.Build(Sample(), ModKey.FromNameAndExtension("ShaderTest.esp")).Mod;
        var shader = Assert.Single(mod.EffectShaders);
        var effect = Assert.Single(mod.MagicEffects);
        Assert.Equal(shader.FormKey, effect.HitShader.FormKey);
        Assert.Equal(shader.FormKey, effect.EnchantShader.FormKey);
    }

    [Fact]
    public void MissingParticlePalette_IsNonFatalButLoud()
    {
        var spec = Sample();
        spec.EffectShaders[0].PaletteTexture = "";
        var result = Generator.Build(spec, ModKey.FromNameAndExtension("ShaderTest.esp"));
        Assert.Contains(result.Warnings, w => w.Contains("MF_FireGlow") && w.Contains("palette"));
    }

    [Fact]
    public void ValidSpec_HasNoProblems() => Assert.Empty(Generator.Validate(Sample()));

    [Fact]
    public void Validate_RejectsInvalidShapeAndNormalizedParticleTimes()
    {
        var spec = Sample();
        var shader = spec.EffectShaders[0];
        shader.FillTexture = "Textures/MFVfx/fire.png";
        shader.Flags.Add("Bogus");
        shader.Membrane!.SourceBlend = "Bogus";
        shader.Particle!.ScaleKeys.Add(new() { Time = 2, Scale = -1 });
        shader.Particle.ColorKeys[0].Alpha = 2;
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("must NOT start with 'Textures"));
        Assert.Contains(problems, p => p.Contains("must be a .dds"));
        Assert.Contains(problems, p => p.Contains("flag 'Bogus'"));
        Assert.Contains(problems, p => p.Contains("sourceBlend 'Bogus'"));
        Assert.Contains(problems, p => p.Contains("at most 2"));
        Assert.Contains(problems, p => p.Contains("scale must be >= 0"));
        Assert.Contains(problems, p => p.Contains("alpha must be 0..1"));
    }
}
