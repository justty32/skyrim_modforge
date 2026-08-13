namespace ModForge;

/// <summary>EFSH record: a mesh membrane tint and/or actor-emitted sprite particles.</summary>
public sealed class EffectShaderSpec
{
    public string EditorId { get; set; } = "";
    // All texture paths are relative to Data\Textures (do not include the Textures\ prefix).
    public string FillTexture { get; set; } = "";
    public string ParticleTexture { get; set; } = "";
    public string HolesTexture { get; set; } = "";
    // Convenience fallback for both palettes; a specific palette wins when supplied.
    public string PaletteTexture { get; set; } = "";
    public string MembranePaletteTexture { get; set; } = "";
    public string ParticlePaletteTexture { get; set; } = "";
    public List<string> Flags { get; set; } = new();
    public EffectShaderMembraneSpec? Membrane { get; set; }
    public EffectShaderParticleSpec? Particle { get; set; }
}

public sealed class EffectShaderMembraneSpec
{
    public string SourceBlend { get; set; } = "SourceAlpha";
    public string DestBlend { get; set; } = "One";
    public string BlendOperation { get; set; } = "Add";
    public string ZTest { get; set; } = "Normal";
    public ColorSpec? FillColor { get; set; }
    public ColorSpec? EdgeColor { get; set; }
    public float FillFadeInTime { get; set; }
    public float FillFullTime { get; set; }
    public float FillFadeOutTime { get; set; }
    public float FillPersistentAlphaRatio { get; set; }
    public float FillFullAlphaRatio { get; set; } = 1f;
    public float FillAlphaPulseAmplitude { get; set; }
    public float FillAlphaPulseFrequency { get; set; }
    public float FillTextureAnimationSpeedU { get; set; }
    public float FillTextureAnimationSpeedV { get; set; }
    public float EdgeFallOff { get; set; }
    public float EdgeFadeInTime { get; set; }
    public float EdgeFullTime { get; set; }
    public float EdgeFadeOutTime { get; set; }
    public float EdgePersistentAlphaRatio { get; set; }
    public float EdgeFullAlphaRatio { get; set; } = 1f;
    public float EdgeAlphaPulseAmplitude { get; set; }
    public float EdgeAlphaPulseFrequency { get; set; }
}

public sealed class EffectShaderParticleSpec
{
    public string SourceBlend { get; set; } = "SourceAlpha";
    public string DestBlend { get; set; } = "One";
    public string BlendOperation { get; set; } = "Add";
    public string ZTest { get; set; } = "Normal";
    public float BirthRampUpTime { get; set; }
    public float FullBirthTime { get; set; }
    public float BirthRampDownTime { get; set; }
    public float FullBirthRatio { get; set; } = 1f;
    public float PersistentCount { get; set; }
    public float Lifetime { get; set; }
    public float LifetimePlusMinus { get; set; }
    public float InitialSpeed { get; set; }
    public float InitialSpeedPlusMinus { get; set; }
    public float Acceleration { get; set; }
    public float InitialRotationDegrees { get; set; }
    public float InitialRotationDegreesPlusMinus { get; set; }
    public float RotationSpeedDegreesPerSecond { get; set; }
    public float RotationSpeedDegreesPerSecondPlusMinus { get; set; }
    public List<EffectShaderScaleKeySpec> ScaleKeys { get; set; } = new();
    public List<EffectShaderColorKeySpec> ColorKeys { get; set; } = new();
}

public sealed class EffectShaderScaleKeySpec
{
    public float Time { get; set; }
    public float Scale { get; set; }
}

public sealed class EffectShaderColorKeySpec
{
    public float Time { get; set; }
    public ColorSpec Color { get; set; } = new();
    public float Alpha { get; set; } = 1f;
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<EffectShaderSpec> EffectShaders { get; set; } = new();   // EffectShader (EFSH), texture-only VFX
}
