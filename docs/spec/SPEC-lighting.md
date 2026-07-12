# ModForge spec — custom lights & lighting (LGTM/IMGS/DALC)

← [index](SPEC-index.md) · cells, placements & navmesh → [SPEC-world](SPEC-world.md) · in-world
macros → [SPEC-world-macros](SPEC-world-macros.md)

Custom light sources (LIGT) and interior/exterior lighting — LightingTemplate (LGTM), ImageSpace
(IMGS), DALC ambient fill, and per-cell/per-weather overrides. For placing objects (including a
light) into a cell or worldspace see [SPEC-world](SPEC-world.md).

### lights — custom light sources (LIGT)
Define a custom light (colour, radius, flicker) and PLACE it like any other base object. ModForge
could already *place* vanilla lights; `lights[]` lets you author new ones.
```jsonc
"lights": [
  { "editorId": "MF_EerieLight", "name": "Eerie Glow",
    "color": { "r": 70, "g": 230, "b": 110 },   // RGB 0..255
    "radius": 420, "fadeValue": 1.0,            // radius in units; fade = brightness multiplier
    "flags": [ "Dynamic", "Flicker" ],          // Light.Flag names: Dynamic / Flicker / FlickerSlow /
                                                //   Pulse / PulseSlow / OffByDefault / SpotLight / CanBeCarried / …
    "falloffExponent": 1.0, "fov": 90.0,        // optional (spotlights)
    "value": 0, "weight": 0.0 } ]               // optional (only matter for a carriable light)
```
Place it with a normal `placements[]` entry whose `base` is the light's `editorId`:
```jsonc
"placements": [ { "base": "MF_EerieLight", "cell": "Skyrim.esm:0x0133C6",
                  "position": { "x": -650, "y": 100, "z": 140 } } ]
```
A LIGT base radius defaults to 256, fade to 1.0. Use `Dynamic` so it lights actors that move through
it, `Flicker`/`Pulse` for torch/candle/magical effects. Validation checks flag names, colour range
(0..255), and radius > 0. (A free-standing light has no model — for a visible *fixture* place a vanilla
torch/lantern static too, or carry the light on a torch object.)

### lighting
Skyrim interiors are dark by *authoring choice*, not engine limit — lighting is almost entirely
a record-layer concern. Three record types work together:

- **LGTM (LightingTemplate)** — reusable interior ambient/directional/fog + DALC settings.
- **IMGS (ImageSpace)** — screen-space HDR eye-adapt, bloom, cinematic colour, and tint.
- **Inline XCLL** — per-cell overrides of specific lighting fields (the rest are inherited from the LGTM).

```jsonc
"lightingTemplates": [
  { "editorId": "MF_BrightCaveLGTM",
    "template": "Skyrim.esm:0x0300E2",         // DeepCopy DefaultLightingTemplate as base
    "ambientColor":     { "r": 150, "g": 155, "b": 170 },
    "directionalColor": { "r": 210, "g": 210, "b": 200 },
    "fogNear": 0, "fogFar": 8192,
    "directionalAmbient": {                    // DALC — six-direction hemisphere fill
      "scale": 1.0,
      "zPlus":  { "r": 200, "g": 205, "b": 215 },
      "zMinus": { "r": 120, "g": 122, "b": 130 },
      "xPlus":  { "r": 170, "g": 172, "b": 180 },
      "xMinus": { "r": 170, "g": 172, "b": 180 },
      "yPlus":  { "r": 170, "g": 172, "b": 180 },
      "yMinus": { "r": 170, "g": 172, "b": 180 } } }
],
"imageSpaces": [
  { "editorId": "MF_BrightIMGS",              // no template — start from engine defaults (see pitfall below)
    "brightness": 1.35, "saturation": 1.2, "contrast": 1.0,
    "bloomScale": 0.8, "sunlightScale": 1.2, "white": 1.5 }
],
"cells": [
  { "editorId": "MF_BrightRoom", "name": "Bright Test Room",
    "template": "Skyrim.esm:0x0165A8",         // copy Breezehome env as structural base
    "lightingTemplate": "MF_BrightCaveLGTM",  // in-spec LGTM editorId (or "Skyrim.esm:0xFORMID")
    "imageSpace": "MF_BrightIMGS" }            // in-spec IMGS editorId (or "Skyrim.esm:0xFORMID")
]
```

**Authoring model — template-copy + override.** Set `template` on a LGTM or IMGS to a vanilla
record `"<master>:0xFORMID"`; it is DeepCopied as the base, then only the fields you specify
overwrite it (all fields are optional; omitting one keeps the vanilla value). No `template` →
engine-neutral defaults (a blank IMGS has zero-d HDR fields — see pitfall below).

**LGTM fields** (`lightingTemplates[]`):
`editorId`, `template` (vanilla LGTM ref); colours `ambientColor` / `directionalColor` /
`fogNearColor` / `fogFarColor` (RGB 0..255); floats `directionalRotationXY` / `directionalRotationZ` /
`directionalFade` / `fogNear` / `fogFar` / `fogMax` / `fogClipDistance` / `fogPower` /
`lightFadeStart` / `lightFadeEnd`; `directionalAmbient` (DALC, see below).

**DALC — `directionalAmbient`** (`AmbientColorsSpec`): the six-direction hemisphere fill —
`xPlus` / `xMinus` / `yPlus` / `yMinus` / `zPlus` / `zMinus` + `specular` (all `ColorSpec`)
and `scale` (float). Skyrim has no global illumination; DALC is the practical substitute for
ambient fill that brightens a dark room from all directions. On a LGTM it maps to
`DirectionalAmbientColors`; on an inline CELL XCLL it maps to `AmbientColors` (different
Mutagen field, same data).

**IMGS fields** (`imageSpaces[]`):
`editorId`, `template` (vanilla IMGS ref);
HDR: `eyeAdaptSpeed` / `eyeAdaptStrength` / `bloomBlurRadius` / `bloomThreshold` / `bloomScale` /
`receiveBloomThreshold` / `white` / `sunlightScale` / `skyScale`;
Cinematic (1 = neutral): `brightness` / `contrast` / `saturation`;
Tint: `tintAmount` / `tintColor` (ColorSpec). "Bright, clean, saturated" look is mostly IMGS
(boost `brightness`, `saturation`, lower `bloomThreshold`).

**CELL lighting fields** (on a `cells[]` entry):
- `lightingTemplate` — in-spec LGTM `editorId` **or** vanilla `"<master>:0xFORMID"` LGTM ref.
- `imageSpace` — in-spec IMGS `editorId` **or** vanilla `"<master>:0xFORMID"` IMGS ref.
- `lighting` — inline `CellLightingSpec`: the same colour/fog/fade fields as LGTM (note:
  CELL uses `lightFadeBegin`/`lightFadeEnd`, not `lightFadeStart`/`lightFadeEnd`) plus
  `directionalAmbient` (DALC → `AmbientColors`) and `inherit` (list of flag names below).

**Inherit flags rule.** An interior CELL must carry an XCLL record or it renders pitch black.
The `lighting.inherit` list names which fields are pulled from the `lightingTemplate` instead of
the inline XCLL. Valid flag names: `AmbientColor` / `DirectionalColor` / `FogColor` / `FogNear` /
`FogFar` / `DirectionalRotation` / `DirectionalFade` / `ClipDistance` / `FogPower` / `FogMax` /
`LightFadeDistances`.
Special cases:
- No inline `lighting` **and** a `lightingTemplate` is set → the cell inherits **all** flags
  (fully template-driven; the build writes an XCLL with every inherit flag set).
- A field set inline AND listed in `inherit` → the template wins (warned).

**IMAD vs IMGS.** `imageSpaces[]` produces IMGS *base* records (HDR/cinematic/tint attached to a
CELL). The existing `imageSpaceModifiers[]` (IMAD) are screen post-process curves triggered by
spells/scripts — a different record and a different workflow.

**Coexists with `cells[].template`.** The existing `template` field (copies a whole vanilla
interior's lighting/water env as a structural base) still works; `lightingTemplate` / `imageSpace`
/ `lighting` then layer on top to override exactly the fields you care about.

**Pitfall — blank IMGS.** A fresh IMGS with no `template` starts from engine-zero HDR values
(`bloomThreshold`, `eyeAdaptSpeed`, `white` all 0). The result is an overbright or washed-out
look. For a sane appearance, prefer giving the IMGS a vanilla `template` (e.g.
`Skyrim.esm:0x1A27E0` `DefaultImageSpace`) and bumping only the fields you want, rather than
authoring HDR from scratch. Use `imgsdiag <Skyrim.esm>` to list vanilla IMGS records and their
values.

**Diagnostics.**
- `lgtmdiag <esp> [0xFORMID]` — dump a LightingTemplate's ambient/directional/fog colors + DALC.
  No FormID = list all LGTMs in the file. Use to verify the built result or to read a vanilla
  template's values before using it as `template`.
- `imgsdiag <esp> [0xFORMID]` — dump an ImageSpace's HDR / cinematic / tint. Same list-all
  behaviour without a FormID.

Worked example: `examples/lighting.json` (bright interior: custom LGTM + IMGS, cell with
template-driven lighting, DALC hemisphere fill).

**Outdoor / weather IMGS.** The LGTM + CELL XCLL path above is **interior-only**. Outdoors,
ambient lighting comes from the Weather record's own sky/sunlight/ambient colour channels
(the `WeatherSpec` `skyUpperColor` / `sunlightColor` / `ambientColor` per-time-of-day fields —
already supported). Screen-space colour grading outdoors uses a separate mechanism: the Weather
record's per-time-of-day **ImageSpace** slots. Set them via `weathers[].imageSpaces`:

```jsonc
"imageSpaces": [
  { "editorId": "MF_OutdoorBrightIMGS", "template": "Skyrim.esm:0x012F88",
    "brightness": 1.1, "saturation": 1.25, "bloomScale": 0.9, "sunlightScale": 1.2, "skyScale": 0.12 }
],
"weathers": [
  { "editorId": "MF_BrightWeather",
    "template": "Skyrim.esm:0x10E1F2",                       // SkyrimClear_A — inherit clouds + tuned sky
    "imageSpaces": { "default": "MF_OutdoorBrightIMGS" } }   // default fills all four ToD
]
```

`weathers[].imageSpaces` fields: `default` (fills any unset time-of-day), `sunrise`, `day`,
`sunset`, `night`. Each value is an in-spec `imageSpaces[]` editorId **or** a vanilla
`"<master>:0xFORMID"` IMGS ref. A single `default` is sufficient to grade all four
times-of-day uniformly.

**Weather `template` (clouds!).** A weather built **from scratch has NO clouds** (and only
baseline sky colours) — the sky is a flat empty gradient. Set `weathers[].template` to a vanilla
weather `"<master>:0xFORMID"` (e.g. `Skyrim.esm:0x10E1F2` = SkyrimClear_A): the clone inherits its
cloud layers + cloud textures + per-time-of-day sky/sunlight/ambient colours + atmospherics, and
then you override **only** what you set (a colour left null keeps the template's; an empty `clouds`
list keeps the template's clouds). This is the recommended outdoor base: copy a vanilla clear
weather for a proper cloudy sky, then push the screen grading via `imageSpaces`. Two levers stay
independent — **sky brightness** = the weather's `skyUpperColor`/`skyLowerColor` + the IMGS
`skyScale`; **ground/scene** = `sunlightScale` + the weather's `ambientColor`.

> **Note:** the LGTM / CELL path does NOT apply to exterior cells — do not attach a
> `lightingTemplate` or `imageSpace` directly to a weather. The weather's own colour fields
> drive outdoor ambient; IMGS on the weather drives screen-space HDR/bloom/saturation.

**In-game test (non-invasive).** `fw <weatherFormID>` (ForceWeather) activates the weather
immediately without editing any climate or worldspace. Find the FormID with
`find <esp> MF_BrightWeather Weather` and pass the hex FormID to `fw` in the console
(e.g. `fw 0800` for an ESL slot). Verify the IMGS is wired with
`weatherdiag <esp> <0xFormID>` — the `ImageSpaces` line must show the custom IMGS FormKey
for all four ToD. No climate/worldspace assignment needed to test the visual result.

Worked example: `examples/weather_bright.json` (outdoor IMGS grading via `imageSpaces.default`).
Cross-reference: see the indoor [lighting](#lighting) subsection above for LGTM / CELL / XCLL.


