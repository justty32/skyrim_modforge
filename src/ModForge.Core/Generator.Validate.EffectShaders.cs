namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        public void ValidateEffectShaders()
        {
            foreach (var es in spec.EffectShaders)
            {
                var what = $"effectShader '{es.EditorId}'";
                CheckTexPath(es.FillTexture, $"{what} fillTexture");
                CheckTexPath(es.ParticleTexture, $"{what} particleTexture");
                CheckTexPath(es.HolesTexture, $"{what} holesTexture");
                CheckTexPath(es.PaletteTexture, $"{what} paletteTexture");
                CheckTexPath(es.MembranePaletteTexture, $"{what} membranePaletteTexture");
                CheckTexPath(es.ParticlePaletteTexture, $"{what} particlePaletteTexture");
                foreach (var flag in es.Flags) CheckEnum<EffectShader.Flag>(flag, $"{what} flag");
                if (es.Membrane is { } m) ValidateMembrane(m, what);
                if (es.Particle is { } p) ValidateParticle(p, what);
                if (es.Membrane is null && es.Particle is null)
                    Problems.Add($"{what} has neither membrane nor particle settings — it will have no visible shader");
            }
        }

        private void ValidateMembrane(EffectShaderMembraneSpec m, string what)
        {
            CheckEnum<EffectShader.BlendMode>(m.SourceBlend, $"{what} membrane sourceBlend");
            CheckEnum<EffectShader.BlendMode>(m.DestBlend, $"{what} membrane destBlend");
            CheckEnum<EffectShader.BlendOperation>(m.BlendOperation, $"{what} membrane blendOperation");
            CheckEnum<EffectShader.ZTest>(m.ZTest, $"{what} membrane zTest");
            CheckShaderColor(m.FillColor, $"{what} membrane fillColor");
            CheckShaderColor(m.EdgeColor, $"{what} membrane edgeColor");
            CheckNonNegative(what, "membrane time/pulse", m.FillFadeInTime, m.FillFullTime,
                m.FillFadeOutTime, m.FillAlphaPulseAmplitude, m.FillAlphaPulseFrequency,
                m.EdgeFallOff, m.EdgeFadeInTime, m.EdgeFullTime, m.EdgeFadeOutTime,
                m.EdgeAlphaPulseAmplitude, m.EdgeAlphaPulseFrequency);
            CheckRatio(what, "membrane fillPersistentAlphaRatio", m.FillPersistentAlphaRatio);
            CheckRatio(what, "membrane fillFullAlphaRatio", m.FillFullAlphaRatio);
            CheckRatio(what, "membrane edgePersistentAlphaRatio", m.EdgePersistentAlphaRatio);
            CheckRatio(what, "membrane edgeFullAlphaRatio", m.EdgeFullAlphaRatio);
        }

        private void ValidateParticle(EffectShaderParticleSpec p, string what)
        {
            CheckEnum<EffectShader.BlendMode>(p.SourceBlend, $"{what} particle sourceBlend");
            CheckEnum<EffectShader.BlendMode>(p.DestBlend, $"{what} particle destBlend");
            CheckEnum<EffectShader.BlendOperation>(p.BlendOperation, $"{what} particle blendOperation");
            CheckEnum<EffectShader.ZTest>(p.ZTest, $"{what} particle zTest");
            CheckNonNegative(what, "particle timing/count", p.BirthRampUpTime, p.FullBirthTime,
                p.BirthRampDownTime, p.PersistentCount, p.Lifetime, p.LifetimePlusMinus);
            CheckRatio(what, "particle fullBirthRatio", p.FullBirthRatio);
            if (p.ScaleKeys.Count > 2) Problems.Add($"{what} particle scaleKeys supports at most 2 keys");
            if (p.ColorKeys.Count > 3) Problems.Add($"{what} particle colorKeys supports at most 3 keys");
            foreach (var (key, i) in p.ScaleKeys.Select((v, i) => (v, i)))
            {
                CheckRatio(what, $"particle scaleKeys[{i}].time", key.Time);
                if (key.Scale < 0) Problems.Add($"{what} particle scaleKeys[{i}].scale must be >= 0");
            }
            foreach (var (key, i) in p.ColorKeys.Select((v, i) => (v, i)))
            {
                CheckRatio(what, $"particle colorKeys[{i}].time", key.Time);
                CheckRatio(what, $"particle colorKeys[{i}].alpha", key.Alpha);
                CheckShaderColor(key.Color, $"{what} particle colorKeys[{i}].color");
            }
        }

        private void CheckShaderColor(ColorSpec? c, string what)
        {
            if (c is not null && (c.R is < 0 or > 255 || c.G is < 0 or > 255 || c.B is < 0 or > 255))
                Problems.Add($"{what} RGB components must be 0..255");
        }
        private void CheckRatio(string what, string field, float value)
        { if (value is < 0 or > 1) Problems.Add($"{what} {field} must be 0..1"); }
        private void CheckNonNegative(string what, string fields, params float[] values)
        { if (values.Any(v => v < 0)) Problems.Add($"{what} {fields} values must be >= 0"); }
    }
}
