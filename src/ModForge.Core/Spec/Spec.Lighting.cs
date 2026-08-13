namespace ModForge;

// =====================================================================================
//  LIGHTING records: LightingTemplate (LGTM) + ImageSpace (IMGS) + inline CELL XCLL.
//
//  Skyrim interiors are dark by *authoring choice*, not engine limit — lighting is almost
//  entirely a record-layer concern. These specs make a cave/dungeon bright.
//
//  AUTHORING MODEL = template-copy + override: point `template` at a vanilla LGTM/IMGS,
//  it is DeepCopied as the base, then ONLY the fields you set here overwrite it (all
//  nullable → unset means "keep the vanilla value"). No template → engine-neutral defaults.
//
//  NOTE: distinct from ImageSpaceModifierSpec (IMAD, a screen post-process curve). This is
//  the IMGS *base* record (HDR / cinematic / tint) you attach to a CELL.
//
//  Colours reuse ColorSpec (Spec.Weather.cs) — 0..255 RGB.
// =====================================================================================

/// <summary>Six-direction hemisphere ambient light (DALC) — the flat fill that brightens a
/// dark room overall. Any omitted direction/specular keeps the template value.</summary>
public sealed class AmbientColorsSpec
{
    public ColorSpec? XPlus { get; set; }
    public ColorSpec? XMinus { get; set; }
    public ColorSpec? YPlus { get; set; }
    public ColorSpec? YMinus { get; set; }
    public ColorSpec? ZPlus { get; set; }
    public ColorSpec? ZMinus { get; set; }
    public ColorSpec? Specular { get; set; }
    public float? Scale { get; set; }
}

/// <summary>A LightingTemplate (LGTM): reusable interior lighting (ambient/directional/fog +
/// DALC). Author by copying a vanilla LGTM via <see cref="Template"/> then overriding.</summary>
public sealed class LightingTemplateSpec
{
    /// <summary>Required, unique. CELLs reference it by this editorId.</summary>
    public string EditorId { get; set; } = "";
    /// <summary>Optional vanilla LGTM to DeepCopy as base ("&lt;master&gt;:0xFORMID",
    /// e.g. Skyrim.esm:0x0300E2 = DefaultLightingTemplate).</summary>
    public string Template { get; set; } = "";

    public ColorSpec? AmbientColor { get; set; }
    public ColorSpec? DirectionalColor { get; set; }
    public int? DirectionalRotationXY { get; set; }
    public int? DirectionalRotationZ { get; set; }
    public float? DirectionalFade { get; set; }
    public ColorSpec? FogNearColor { get; set; }
    public ColorSpec? FogFarColor { get; set; }
    public float? FogNear { get; set; }
    public float? FogFar { get; set; }
    public float? FogMax { get; set; }
    public float? FogClipDistance { get; set; }
    public float? FogPower { get; set; }
    public float? LightFadeStart { get; set; }
    public float? LightFadeEnd { get; set; }
    /// <summary>DALC six-direction ambient → LGTM.DirectionalAmbientColors.</summary>
    public AmbientColorsSpec? DirectionalAmbient { get; set; }
}

/// <summary>An ImageSpace (IMGS): screen-space HDR / cinematic / tint attached to a CELL.
/// "Bright clean saturated" is mostly HDR eye-adapt + bloom + saturation. Copy a vanilla
/// IMGS via <see cref="Template"/> then bump.</summary>
public sealed class ImageSpaceSpec
{
    public string EditorId { get; set; } = "";
    public string Template { get; set; } = "";

    // Hdr
    public float? EyeAdaptSpeed { get; set; }
    public float? EyeAdaptStrength { get; set; }
    public float? BloomBlurRadius { get; set; }
    public float? BloomThreshold { get; set; }
    public float? BloomScale { get; set; }
    public float? ReceiveBloomThreshold { get; set; }
    public float? White { get; set; }
    public float? SunlightScale { get; set; }
    public float? SkyScale { get; set; }
    // Cinematic (1 = neutral, >1 boosts)
    public float? Brightness { get; set; }
    public float? Contrast { get; set; }
    public float? Saturation { get; set; }
    // Tint
    public float? TintAmount { get; set; }
    public ColorSpec? TintColor { get; set; }
}

/// <summary>Inline CELL lighting (XCLL) overrides. Fields left null are pulled from the cell's
/// LightingTemplate (the <see cref="Inherit"/> flags decide which). Note XCLL uses
/// LightFadeBegin/End (vs LGTM's Start/End).</summary>
public sealed class CellLightingSpec
{
    public ColorSpec? AmbientColor { get; set; }
    public ColorSpec? DirectionalColor { get; set; }
    public int? DirectionalRotationXY { get; set; }
    public int? DirectionalRotationZ { get; set; }
    public float? DirectionalFade { get; set; }
    public ColorSpec? FogNearColor { get; set; }
    public ColorSpec? FogFarColor { get; set; }
    public float? FogNear { get; set; }
    public float? FogFar { get; set; }
    public float? FogMax { get; set; }
    public float? FogClipDistance { get; set; }
    public float? FogPower { get; set; }
    public float? LightFadeBegin { get; set; }
    public float? LightFadeEnd { get; set; }
    /// <summary>DALC six-direction ambient → CellLighting.AmbientColors.</summary>
    public AmbientColorsSpec? DirectionalAmbient { get; set; }
    /// <summary>Field-flag names still inherited from the LightingTemplate (CellLighting.Inherit:
    /// AmbientColor / DirectionalColor / FogColor / FogNear / FogFar / DirectionalRotation /
    /// DirectionalFade / ClipDistance / FogPower / FogMax / LightFadeDistances). A field both set
    /// inline AND listed here is inherited (template wins) + warned.</summary>
    public List<string> Inherit { get; set; } = new();
}
