namespace ModForge;

// =====================================================================================
//  LIGHT (LIGT) record.
//
//  A LIGT is a light source: a colour, a radius (how far it reaches), a fade multiplier,
//  flicker/pulse behaviour, and (for carriable lights like torches) an inventory value +
//  weight + model. A custom light becomes a normal base record with an editorId, so it can
//  be PLACED into a cell via the existing `placements[]` (base = the light's editorId) —
//  no placement-code changes are needed.
//
//  Minimal spec = just an editorId; that yields a vanilla-sane omnidirectional light
//  (radius 256, fade 1.0, no special flags). Override only what you care about.
//
//  Colours reuse ColorSpec (Spec.Weather.cs) — 0..255 RGB components.
// =====================================================================================

/// <summary>
/// A LIGHT (LIGT) record: a custom light source authored by colour / radius / flicker.
/// </summary>
public sealed class LightSpec
{
    /// <summary>Required, unique. Placements reference the light by this editorId.</summary>
    public string EditorId { get; set; } = "";

    /// <summary>Optional in-game name (only shown for carriable lights like torches).</summary>
    public string Name { get; set; } = "";

    /// <summary>RGB tint (0..255 components; alpha unused). Default: warm white.</summary>
    public ColorSpec? Color { get; set; }

    /// <summary>How far the light reaches, in world units. Default 256 (vanilla torch).</summary>
    public uint Radius { get; set; } = 256;

    /// <summary>Brightness fade multiplier. Default 1.0.</summary>
    public float FadeValue { get; set; } = 1.0f;

    /// <summary>Optional light-falloff exponent (how quickly brightness drops with distance).</summary>
    public float? FalloffExponent { get; set; }

    /// <summary>Optional field-of-view (degrees) for spotlights.</summary>
    public float? Fov { get; set; }

    /// <summary>
    /// Behaviour flags — any of Light.Flag names, e.g. Dynamic / Flicker / PortalStrict /
    /// CanBeCarried / SpotLight / ShadowSpotlight / OffByDefault / FlickerSlow / Pulse /
    /// PulseSlow / SpotShadow. Invalid names are warned about and skipped at build.
    /// </summary>
    public List<string> Flags { get; set; } = new();

    /// <summary>Optional inventory gold value (carriable lights).</summary>
    public uint? Value { get; set; }

    /// <summary>Optional inventory weight (carriable lights).</summary>
    public float? Weight { get; set; }
}
