namespace ModForge;

// =====================================================================================
//  Atmospheric records (It.W): WEATHER (WTHR) + CLIMATE (CLMT).
//
//  A WTHR is a *sky*: cloud-layer textures + scroll speeds, per-time-of-day colours for
//  the sky/fog/clouds/sun, precipitation, wind, and fog distances. A CLMT is a *cycle*:
//  the list of weathers that can occur (each with a chance), the sunrise/sunset timing,
//  and the sun/moon textures. A CLMT references WTHRs; together they give a worldspace or
//  region its atmosphere.
//
//  HONEST SCOPE: emitting a WTHR + CLMT does NOT by itself change any sky in-game — the
//  climate has to be *assigned*, which a vanilla game does via a WRLD's Climate field or a
//  REGN region-data (sky/weather) entry. Those records are out of scope here; this file
//  builds valid WTHR/CLMT you can then point a worldspace/region at (see docs/SPEC.md).
//
//  Everything is a "sensible subset": a minimal spec (just an editorId) produces a valid,
//  vanilla-sane clear-day weather/climate; you override only what you care about.
// =====================================================================================

/// <summary>
/// One RGB(A) colour authored as 0–255 components. Skyrim weather colours are 8-bit per
/// channel; alpha is usually 0 (unused) for weather colours. Validate clamps to 0–255.
/// </summary>
public sealed class ColorSpec
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    public int A { get; set; }   // usually 0 for weather colours
}

/// <summary>
/// A weather colour across the four times of day. Any omitted time-of-day falls back to
/// <see cref="Day"/> (and Day itself falls back to a baseline) so a partial colour is valid.
/// </summary>
public sealed class WeatherColorSpec
{
    public ColorSpec? Sunrise { get; set; }
    public ColorSpec? Day { get; set; }
    public ColorSpec? Sunset { get; set; }
    public ColorSpec? Night { get; set; }
}

/// <summary>Per-time-of-day ImageSpace (IMGS) attached to a Weather — the outdoor color-grading
/// lever (HDR/bloom/saturation by time of day). Each ref is an in-spec ImageSpace editorId OR a
/// vanilla "&lt;master&gt;:0xFORMID". <see cref="Default"/> fills any time-of-day left empty, so a
/// single bright IMGS can grade the whole day.</summary>
public sealed class WeatherImageSpacesSpec
{
    public string Default { get; set; } = "";
    public string Sunrise { get; set; } = "";
    public string Day { get; set; } = "";
    public string Sunset { get; set; } = "";
    public string Night { get; set; } = "";
}

/// <summary>
/// One sky cloud layer: a texture path (relative to Data\Textures, e.g.
/// "Sky\\SkyrimCloudsUpper04.dds"), scroll speeds, per-time-of-day colours and alphas.
/// Skyrim weathers have up to 32 layers; author only the ones you need (Index 0..31).
/// </summary>
public sealed class CloudLayerSpec
{
    public int Index { get; set; }                 // 0..31 cloud-layer slot
    public string Texture { get; set; } = "";       // Textures-relative .dds path
    public float XSpeed { get; set; }
    public float YSpeed { get; set; }
    public WeatherColorSpec? Colors { get; set; }   // per-time-of-day tint
    // Per-time-of-day opacity 0..1 (default: fully opaque day, fading at night).
    public float? AlphaSunrise { get; set; }
    public float? AlphaDay { get; set; }
    public float? AlphaSunset { get; set; }
    public float? AlphaNight { get; set; }
}

/// <summary>
/// A WEATHER (WTHR) record: a custom sky. Minimal spec = just an editorId (a clear-day
/// baseline). Override classification flags, the sky/fog/cloud/sun colours, cloud layers,
/// precipitation, wind and fog distances as needed.
/// </summary>
public sealed class WeatherSpec
{
    public string EditorId { get; set; } = "";

    /// <summary>Optional vanilla weather to DeepCopy as the base ("&lt;master&gt;:0xFORMID", e.g.
    /// Skyrim.esm:0x10E1F2 = SkyrimClear_A). The clone brings clouds + cloud textures + per-ToD sky
    /// colours + atmospherics; then ONLY the fields you set below override it (a colour you leave
    /// null keeps the template's; an empty Clouds list keeps the template's clouds). Without a
    /// template the weather is built from scratch (vanilla-sane baselines, NO clouds).</summary>
    public string Template { get; set; } = "";

    // Classification flags: any of Pleasant / Cloudy / Rainy / Snow (and the SkyStatics
    // flags). These drive engine behaviour (Rainy/Snow toggle precipitation systems, the
    // ambient soundscape, "is it raining" script checks). Default: Pleasant.
    public List<string> Flags { get; set; } = new();

    // --- Per-time-of-day colours (each Sunrise/Day/Sunset/Night, 0..255 RGB). ---
    public WeatherColorSpec? SkyUpperColor { get; set; }
    public WeatherColorSpec? SkyLowerColor { get; set; }
    public WeatherColorSpec? FogNearColor { get; set; }
    public WeatherColorSpec? FogFarColor { get; set; }
    public WeatherColorSpec? HorizonColor { get; set; }
    public WeatherColorSpec? CloudColor { get; set; }      // tints any cloud layer with no own colour
    public WeatherColorSpec? SunColor { get; set; }
    public WeatherColorSpec? SunlightColor { get; set; }    // directional sunlight on the world
    public WeatherColorSpec? AmbientColor { get; set; }
    public WeatherColorSpec? StarsColor { get; set; }

    /// <summary>Per-time-of-day screen ImageSpace (IMGS) — outdoor color grading. Optional.</summary>
    public WeatherImageSpacesSpec? ImageSpaces { get; set; }

    // --- Cloud layers (texture + speed + colours/alphas). ---
    public List<CloudLayerSpec> Clouds { get; set; } = new();

    // --- Precipitation + wind. ---
    public string Precipitation { get; set; } = "";   // ref to a SPGD shader-particle-geometry (e.g. Skyrim.esm:0x... rain)
    public float WindSpeed { get; set; }               // 0..1 (fraction); also accepts 0..100 (percent)
    public float WindDirection { get; set; }            // degrees 0..360
    public float WindDirectionRange { get; set; }        // degrees 0..180

    // --- Fog distances (world units; near/far for day & night). ---
    public float FogDayNear { get; set; } = -1;        // -1 ⇒ leave baseline
    public float FogDayFar { get; set; } = -1;
    public float FogNightNear { get; set; } = -1;
    public float FogNightFar { get; set; } = -1;

    // Transition speed between weathers (TransDelta). 0 ⇒ baseline.
    public float TransitionDelta { get; set; }
}

/// <summary>One weather choice inside a climate's cycle: a weather ref + a chance weight.</summary>
public sealed class WeatherChanceSpec
{
    public string Weather { get; set; } = "";   // in-spec WeatherSpec editorId OR <master>:0xFORMID
    public int Chance { get; set; } = 100;        // relative weight (vanilla climates sum to 100)
}

/// <summary>
/// A CLIMATE (CLMT) record: a weather cycle. Lists which weathers occur (with chances),
/// the sunrise/sunset begin/end times, the sun/moon textures, and which moons are visible.
/// Times are "HH:MM" 24-hour strings (e.g. "05:30"). Minimal spec = an editorId + at least
/// one weather; the rest defaults to a vanilla-sane clear climate.
/// </summary>
public sealed class ClimateSpec
{
    public string EditorId { get; set; } = "";
    public List<WeatherChanceSpec> Weathers { get; set; } = new();

    // Sun timing as "HH:MM" (24h). begin < end; sunrise before sunset. Defaults: vanilla.
    public string SunriseBegin { get; set; } = "05:30";
    public string SunriseEnd { get; set; } = "10:00";
    public string SunsetBegin { get; set; } = "16:00";
    public string SunsetEnd { get; set; } = "20:30";

    // Textures (Data\Textures-relative). Defaults to the vanilla sun textures.
    public string SunTexture { get; set; } = "Sky\\Sun.dds";
    public string SunGlareTexture { get; set; } = "Sky\\SunGlare.dds";

    // Which moons are visible: any of Masser / Secunda. Default: both.
    public List<string> Moons { get; set; } = new();
    public int PhaseLength { get; set; } = 3;    // days per moon phase
    public int Volatility { get; set; } = 50;     // 0..255: how fast weather changes
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    // Atmospheric records. A Weather (WTHR) is a sky; a Climate (CLMT) is a weather
    // cycle + sun/moon timing. See WeatherSpec / ClimateSpec in Spec.Weather.cs.
    public List<WeatherSpec> Weathers { get; set; } = new();

    public List<ClimateSpec> Climates { get; set; } = new();
}
