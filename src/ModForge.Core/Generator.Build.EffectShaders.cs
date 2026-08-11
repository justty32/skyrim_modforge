using System.Drawing;
using Mutagen.Bethesda.Plugins.Assets;
using Mutagen.Bethesda.Skyrim.Assets;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        public void BuildEffectShaders()
        {
            foreach (var es in spec.EffectShaders)
            {
                var r = mod.EffectShaders.AddNew();
                r.EditorID = es.EditorId;

                SetTexture(es.FillTexture, v => r.FillTexture = v);
                SetTexture(es.ParticleTexture, v => r.ParticleShaderTexture = v);
                SetTexture(es.HolesTexture, v => r.HolesTexture = v);
                SetTexture(First(es.MembranePaletteTexture, es.PaletteTexture), v => r.MembranePaletteTexture = v);
                SetTexture(First(es.ParticlePaletteTexture, es.PaletteTexture), v => r.ParticlePaletteTexture = v);

                foreach (var value in es.Flags)
                    if (Enum.TryParse<EffectShader.Flag>(value, true, out var flag)) r.Flags |= flag;

                if (es.Membrane is { } m) BuildMembrane(r, m);
                if (es.Particle is { } p) BuildParticle(r, p);
                if (es.Particle is not null && !string.IsNullOrWhiteSpace(es.ParticleTexture)
                    && string.IsNullOrWhiteSpace(es.ParticlePaletteTexture)
                    && string.IsNullOrWhiteSpace(es.PaletteTexture))
                    Warn($"  ! effectShader '{es.EditorId}' has particles but no particle palette texture — Skyrim may render nothing");
            }
        }

        private static void BuildMembrane(EffectShader r, EffectShaderMembraneSpec m)
        {
            Parse(m.SourceBlend, out EffectShader.BlendMode src, EffectShader.BlendMode.SourceAlpha);
            Parse(m.DestBlend, out EffectShader.BlendMode dst, EffectShader.BlendMode.One);
            Parse(m.BlendOperation, out EffectShader.BlendOperation op, EffectShader.BlendOperation.Add);
            Parse(m.ZTest, out EffectShader.ZTest z, EffectShader.ZTest.Normal);
            r.MembraneSourceBlendMode = src; r.MembraneDestBlendMode = dst;
            r.MembraneBlendOperation = op; r.MembraneZTest = z;
            if (m.FillColor is not null) r.FillColorKey1 = ToShaderColor(m.FillColor);
            if (m.EdgeColor is not null) r.EdgeEffectColor = ToShaderColor(m.EdgeColor);
            r.FillAlphaFadeInTime = m.FillFadeInTime; r.FillFullAlphaTime = m.FillFullTime;
            r.FillFadeOutTime = m.FillFadeOutTime; r.FillPersistentAlphaRatio = m.FillPersistentAlphaRatio;
            r.FillFullAlphaRatio = m.FillFullAlphaRatio; r.FillAlphaPulseAmplitude = m.FillAlphaPulseAmplitude;
            r.FillAlphaPulseFrequency = m.FillAlphaPulseFrequency;
            r.FillTextureAnimationSpeedU = m.FillTextureAnimationSpeedU;
            r.FillTextureAnimationSpeedV = m.FillTextureAnimationSpeedV;
            r.EdgeEffectFallOff = m.EdgeFallOff; r.EdgeEffectAlphaFadeInTime = m.EdgeFadeInTime;
            r.EdgeEffectFullAlphaTime = m.EdgeFullTime; r.EdgeEffectAlphaFadeOutTime = m.EdgeFadeOutTime;
            r.EdgeEffectPersistentAlphaRatio = m.EdgePersistentAlphaRatio;
            r.EdgeEffectFullAlphaRatio = m.EdgeFullAlphaRatio;
            r.EdgeEffectAlphaPulseAmplitude = m.EdgeAlphaPulseAmplitude;
            r.EdgeEffectAlphaPulseFrequency = m.EdgeAlphaPulseFrequency;
        }

        private static void BuildParticle(EffectShader r, EffectShaderParticleSpec p)
        {
            Parse(p.SourceBlend, out EffectShader.BlendMode src, EffectShader.BlendMode.SourceAlpha);
            Parse(p.DestBlend, out EffectShader.BlendMode dst, EffectShader.BlendMode.One);
            Parse(p.BlendOperation, out EffectShader.BlendOperation op, EffectShader.BlendOperation.Add);
            Parse(p.ZTest, out EffectShader.ZTest z, EffectShader.ZTest.Normal);
            r.ParticleSourceBlendMode = src; r.ParticleDestBlendMode = dst;
            r.ParticleBlendOperation = op; r.ParticleZTest = z;
            r.ParticleBirthRampUpTime = p.BirthRampUpTime; r.ParticleFullBirthTime = p.FullBirthTime;
            r.ParticleBirthRampDownTime = p.BirthRampDownTime; r.ParticleFullBirthRatio = p.FullBirthRatio;
            r.ParticlePeristentCount = p.PersistentCount; r.ParticleLifetime = p.Lifetime;
            r.ParticleLifetimePlusMinus = p.LifetimePlusMinus;
            r.ParticleInitialSpeedAlongNormal = p.InitialSpeed;
            r.ParticleInitialSpeedAlongNormalPlusMinus = p.InitialSpeedPlusMinus;
            r.ParticleAccelerationAlongNormal = p.Acceleration;
            r.ParticleInitialRotationDegree = p.InitialRotationDegrees;
            r.ParticleInitialRotationDegreePlusMinus = p.InitialRotationDegreesPlusMinus;
            r.ParticleRotationSpeedDegreePerSec = p.RotationSpeedDegreesPerSecond;
            r.ParticleRotationSpeedDegreePerSecPlusMinus = p.RotationSpeedDegreesPerSecondPlusMinus;

            if (p.ScaleKeys.Count > 0)
            { r.ParticleScaleKey1Time = p.ScaleKeys[0].Time; r.ParticleScaleKey1 = p.ScaleKeys[0].Scale; }
            if (p.ScaleKeys.Count > 1)
            { r.ParticleScaleKey2Time = p.ScaleKeys[1].Time; r.ParticleScaleKey2 = p.ScaleKeys[1].Scale; }
            for (var i = 0; i < Math.Min(3, p.ColorKeys.Count); i++) SetColorKey(r, i, p.ColorKeys[i]);
        }

        private static void SetColorKey(EffectShader r, int i, EffectShaderColorKeySpec key)
        {
            var color = ToShaderColor(key.Color);
            if (i == 0) { r.ColorKey1 = color; r.ColorKey1Time = key.Time; r.ColorKey1Alpha = key.Alpha; }
            else if (i == 1) { r.ColorKey2 = color; r.ColorKey2Time = key.Time; r.ColorKey2Alpha = key.Alpha; }
            else { r.ColorKey3 = color; r.ColorKey3Time = key.Time; r.ColorKey3Alpha = key.Alpha; }
        }

        private static Color ToShaderColor(ColorSpec c) =>
            Color.FromArgb(0, Math.Clamp(c.R, 0, 255), Math.Clamp(c.G, 0, 255), Math.Clamp(c.B, 0, 255));
        private static string First(string preferred, string fallback) =>
            string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
        private static void SetTexture(string path, Action<AssetLink<SkyrimTextureAssetType>> setter)
        { if (!string.IsNullOrWhiteSpace(path)) setter(new(path.Trim())); }
        private static void Parse<T>(string value, out T parsed, T fallback) where T : struct, Enum
        { if (!Enum.TryParse(value, true, out parsed)) parsed = fallback; }
    }
}
