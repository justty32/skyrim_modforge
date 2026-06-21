# ModForge spec — cells, placements & lighting

← [index](SPEC-index.md) · exterior worlds, lists, spawns & vendors → [SPEC-worldspaces](SPEC-worldspaces.md)

### cells & placements — putting things in the world
```jsonc
"cells": [
  { "editorId": "MF_TestRoom", "name": "ModForge Test Room",     // a new interior cell
    "template": "Skyrim.esm:0x0165A8" }                          //   copy lighting from Breezehome (else BLACK)
],
"placements": [
  { "base": "MF_Smith", "cell": "MF_TestRoom",                   // an in-spec NPC ...
    "position": { "x": 0, "y": 0, "z": 0 },
    "rotation": { "x": 0, "y": 0, "z": 0 } },                    //   rotation in degrees
  { "base": "MF_Chest", "cell": "Skyrim.esm:0x01605E",          // ... into a VANILLA INTERIOR cell
    "position": { "x": 100, "y": 0, "z": 0 } },                  //   (Skyrim.esm WhiterunBanneredMare)
  { "base": "MF_Coin", "worldspace": "Skyrim.esm:0x00003C",     // ... into the OPEN WORLD (Tamriel);
    "position": { "x": 22528, "y": 22528, "z": 200 } }           //   position is WORLD coords
]
```
- A `placement` targets **either** an interior `cell` **or** an exterior `worldspace` (set one):
  - **interior** — `cell` is a new in-spec interior cell's `editorId`, **or** an external/vanilla
    interior cell `"<master>:0xFORMID"` (find with `find <Skyrim.esm> <name> Cell`). A new cell
    with no `template` renders **pitch-black** and has **no floor** (you fall into the void): set
    the cell's `template` to a vanilla interior (copies its lighting) and place a floor static in
    it. `position` is local to the cell.
  - **exterior** — `worldspace` is a worldspace ref `"<master>:0xFORMID"` (Tamriel =
    `Skyrim.esm:0x00003C`; find with `find <Skyrim.esm> <name> Worldspace`). `position` is the
    **world** position; the exterior cell at `floor(x/4096), floor(y/4096)` is found in the master
    and overridden to add your ref. If that grid has no master cell, a new exterior cell is made
    there (structural only — not in-game verified). `worldspace` wins if both it and `cell` are set.
- `base` is a *ref* (in-spec or external); NPCs become `PlacedNpc`, anything else `PlacedObject`
  (`kind` overrides the guess). `rotation` is **degrees**. `persistent: true` puts it in the
  cell's persistent list (needed if a quest/script references it).
- **Placing a hazard:** when `base` is an in-spec `hazards[]` editorId (or `kind: "hazard"`), the ref
  is a `PlacedHazard` — a static environmental trap (fire/frost/poison patch). See the HAZD record in
  `SPEC-magic.md § hazards`.
- **`kind: "xmarker"` / `"xmarkerHeading"`** — a helper for placing an **invisible anchor**. With an
  empty `base` it defaults to the vanilla XMarker (`Skyrim.esm:0x0000003B`) / XMarkerHeading
  (`0x00000034`) static, and the ref is **forced persistent** (a quest-target anchor must persist or a
  `forced:` alias resolves to a dropped temp ref). Give it an `editorId`, bind it with a
  `forced:<editorId>` alias, and point an `objectives[].targets[]` at that alias to put a quest marker
  on a fixed spot that has no NPC.
- **Vanilla placement** (interior cell or exterior worldspace) overrides the cell/worldspace to
  *add* your reference (vanilla contents are untouched — they come from the master). Needs the
  game's `Data` folder — set `MODFORGE_SKYRIM_DATA` if it isn't at the default Steam path. (Placing
  into a vanilla worldspace also additively carries its persistent cell, so vanilla map markers and
  the world map stay intact.)

#### placement extra fields
```jsonc
"placements": [
  { "base": "MF_GoldCoins", "cell": "MF_Room",
    "position": { "x": 0, "y": 50, "z": 80 },
    "count": 50 },                                      // XCNT: 50-coin stack

  { "base": "MF_LockedChest", "cell": "MF_Room",
    "position": { "x": 200, "y": 0, "z": 0 },
    "lock": { "level": "master" },                      // XLOC: master-locked
    "ownership": { "owner": "MF_BanditFaction" } },     // XOWN: belongs to this faction

  { "base": "MF_Trophy", "cell": "MF_Room",
    "position": { "x": -100, "y": 0, "z": 100 },
    "scale": 1.5 },                                     // XSCL: 1.5× size

  { "base": "MF_SecretDoor", "cell": "MF_Room",        // hidden until quest stage fires
    "editorId": "MF_SecretDoorRef",
    "initiallyDisabled": true,                          // invisible + non-collidable
    "enableParent": {                                   // XESP: follows quest marker
      "ref": "MF_QuestTrigger",
      "flag": "SetEnable" } }                           //   appears when trigger is enabled
]
```
- **`scale`** (XSCL): uniform scale multiplier. `1.0` = default (subrecord omitted). Valid for
  statics, furniture, lights; actors ignore it in-game. Must be > 0.
- **`initiallyDisabled`** (record flag `0x800`): the ref exists in the cell but is invisible and
  non-collidable until explicitly enabled (via script, quest stage, or `enableParent`). Common
  pattern: hidden object + `enableParent` pointing at a quest-trigger XMarker.
- **`enableParent`** (XESP): this ref's enabled state follows another placed ref (`ref` =
  placement editorId or external ref).
  - `flag`: `SetEnable` (I enable when my parent enables — default), `SetDisable` (inverted),
    `PopIn` (appear without the fade-in flash).
- **`lock`** (XLOC): lock a door or container (`PlacedObject` only).
  - `level`: `novice` | `apprentice` | `adept` | `expert` | `master` | `requiresKey` |
    `inaccessible`, or a raw byte value as a string (e.g. `"50"`).
  - `key` (optional): item ref that bypasses the lock.
- **`ownership`** (XOWN): who owns this object — picking it up counts as theft.
  - `owner`: a FACT or NPC ref.
  - `rank` (optional, int ≥ 0): faction rank required (ignored for NPC owners; `0` = any member).
- **`count`** (XCNT): item stack count for a placed item (e.g. 50 gold coins). `0` = single
  instance (subrecord omitted). Not meaningful for actors or statics.

### map markers (XMRK) — permanent world-map icons

`mapMarkers[]` adds discoverable/fast-travel **location markers** to the world map — independent of any
quest:

```jsonc
"mapMarkers": [
  { "editorId": "MF_HiddenCamp", "name": "Hidden Camp",
    "worldspace": "Skyrim.esm:0x00003C",                 // Tamriel
    "position": { "x": 0, "y": -9000, "z": 0 },
    "type": "Camp",                                       // MarkerType: City/Town/Settlement/Cave/Camp/Fort/Landmark/…
    "flags": ["Visible", "CanTravelTo"] }                 // empty = hidden until the player discovers it
]
```

- Each entry builds a `PlacedObject` on the vanilla **MapMarker** static (`0x10`) carrying an XMRK
  `MapMarker` (name + type + flags), added to the worldspace's **persistent cell** alongside the
  vanilla markers. `type` is a `MapMarker.MarkerType` name; `flags` are `Visible | CanTravelTo |
  ShowAllIsHidden`.
- Because it's a persistent named ref, a map marker can **also** be an `objectives[].targets[]` source
  (bind it with a `forced:<editorId>` alias) — a quest arrow that points at a map location. Worked
  example combining objective markers + an xmarker anchor + a map marker: `examples/quest-markers.json`.

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


---

## in-world skill trees (`skillTrees`)

A **clickable, in-world perk tree** — floating star nodes the player walks up to and activates to
spend points and learn abilities, with prerequisite gating and lit-up visual feedback. **Zero
external-mod dependency** (only `Skyrim.esm`); IN-GAME CONFIRMED. `skillTrees` is a high-level
*macro*: the generator expands it into the low-level records (per-node rank globals, a shared points
global, node + connector-line activators, their placements, and the `MFSkillNode` script wiring) —
the same records a hand-authored tree would use.

```jsonc
"skillTrees": [
  { "editorId": "MFForgeTree", "name": "Forge Mastery",
    "cell": "Skyrim.esm:0x01605E",                 // where it lives (vanilla interior or in-spec cell)
    "origin": { "x": -49, "y": -504, "z": 110 },   // world pos of the ROOT (bottom) node
    "spacing": 65,                                  // vertical gap; 65 = the line mesh's native fit
    "startingPoints": 3,                            // points the player starts with
    "nodes": [                                      // ORDERED bottom→top; node[i] gated on node[i-1]
      { "editorId": "Resolve", "name": "Forged Resolve", "ability": "MFGen_Node0Ability" },
      { "editorId": "Vigor",   "name": "Forged Vigor",   "ability": "MFGen_Node1Ability" },
      { "editorId": "Mastery", "name": "Forged Mastery", "ability": "MFGen_Node2Ability" }
    ] }
],
"assets": "assets/skilltree"                        // bundle the star/line meshes (see below)
```

In-game: the player activates a node → if its prerequisite is owned and a point is available, the
node's `ability` is added to the player, the star lights up, the connector line lights, and a point
is spent. Re-activating a learned node, or one whose prerequisite isn't met, is refused with a
notification.

**Fields** (`skillTrees[]`): `editorId` (prefixes all generated ids), `name`, `cell` (in-spec
interior editorId **or** vanilla `"<master>:0xFORMID"`), `origin` (Vec3, the root node's position),
`spacing` (default 65), `pointsGlobal` (existing GLOB to drive the pool from elsewhere — empty
auto-creates `<editorId>_Points` seeded with `startingPoints`), `startingPoints` (default 3),
`nodeModel` / `lineModel` (Data-relative mesh overrides), and `nodes`.
**Node** (`nodes[]`): `editorId` (unique in the tree), `name` (activate prompt + notification),
`ability` (a SPEL ref — usually an in-spec `spells[]` ability, or vanilla — granted on learn).

**Abilities are yours.** A node references an `ability` you define in `spells[]`/`magicEffects[]`
(or a vanilla SPEL). The tree drives the *learning UX*; the *effect* is an ordinary ability.

**Art (no Campfire install).** The default node/line meshes are Campfire's star/line nifs — but they
are NOT a master dependency: bundle the kit (the two `.nif` + their all-vanilla textures) as loose
files via `assets` (provided at `examples/assets/skilltree`). Override `nodeModel`/`lineModel` to use
your own meshes. The `MFSkillNode.pex` (node behaviour) ships automatically with `package`.

**MVP scope.** A **vertical linear chain** (nodes stacked, each gated on the one below, connected by
vertical lines) — the IN-GAME-CONFIRMED layout. Branching / free 2-D layouts are a future extension
(diagonal connector orientation needs calibration). Worked example: `examples/skill_tree_spec.json`
(the generator) vs `examples/inworld_skill_tree_standalone_spec.json` (the same result hand-authored).
