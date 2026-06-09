# ModForge spec — world, cells & spawns

← [index](SPEC-index.md)

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
- **Vanilla placement** (interior cell or exterior worldspace) overrides the cell/worldspace to
  *add* your reference (vanilla contents are untouched — they come from the master). Needs the
  game's `Data` folder — set `MODFORGE_SKYRIM_DATA` if it isn't at the default Steam path.

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

### worldspaces (WRLD) & regions (REGN) — exterior worlds & weather
Create a **new** exterior worldspace and attach a climate, and define **regions** (areas inside a
worldspace) whose **weather table** drives which weathers play there:
```jsonc
"worldspaces": [
  { "editorId": "MFTestWorld", "name": "ModForge Test Vale",
    "climate": "Skyrim.esm:0x000812",      // CLMT — the sky/lighting cycle (REQUIRED in practice)
    "water":   "Skyrim.esm:0x000018",      // WATR — DefaultWater (optional)
    "parent":  "Skyrim.esm:0x00003C",      // parent WRLD = Tamriel (optional)
    "flags":   ["SmallWorld", "CannotFastTravel"],
    "defaultLandHeight":  -27000,          // the FLOOD-FIX: omitting these defaults water to 0,
    "defaultWaterHeight": -14000,          //   which drowns any terrain below sea level
    "map": { "northwestX": -4, "northwestY": 4, "southeastX": 4, "southeastY": -4,
             "cameraInitialPitch": 50, "cameraMinHeight": 50000, "cameraMaxHeight": 80000 },
    "cells": [
      { "x": 0, "y": 0, "navmesh": true } // flat terrain cell + navmesh; height defaults to 4000
    ] }
],
"regions": [
  { "editorId": "MFTestWorldWeather", "worldspace": "MFTestWorld",  // ref to in-spec WRLD or vanilla
    "edgeFallOff": 1024, "mapColor": "0x3CA0F0", "weatherPriority": 60,
    "weather": [                                                     // the climate hook — >=1 entry
      { "weather": "Skyrim.esm:0x10E1F2", "chance": 60 },           //   SkyrimClear  (relative weight)
      { "weather": "Skyrim.esm:0x10E1F1", "chance": 30 },           //   SkyrimCloudy
      { "weather": "Skyrim.esm:0x10E1F0", "chance": 10 } ],         //   SkyrimClearSN
    "area": [ { "x": -16384, "y": -16384 }, { "x": 16384, "y": -16384 },
              { "x": 16384, "y": 16384 }, { "x": -16384, "y": 16384 } ] }   // >=3 world-space points
  ]
```
- **worldspaces** (WRLD): a new exterior world. `climate` is a CLMT *ref* (vanilla default =
  `Skyrim.esm:0x000812`) — without it the world has **no sky/lighting cycle**; validate flags a
  missing climate. `water`/`lodWater`/`parent`/`interiorLighting`/`location`/`music`/`encounterZone`
  are optional *refs*. `flags` from the WRLD set (`SmallWorld`, `CannotFastTravel`, `NoLodWater`,
  `NoLandscape`, `NoSky`, `FixedDimensions`, `NoGrass`). `defaultLandHeight`/`defaultWaterHeight`
  default to Tamriel's values (-27000 / -14000) — **leave them** unless you know better, since a 0
  water default floods the world. `map` sets the world-map cell-corner bounds + local-map camera.
- **`cells`** — flat walkable terrain cells inside the worldspace. Each entry `{ "x": N, "y": N }`
  generates a CELL + LAND record (flat 33×33 heightmap) placed in the worldspace's SubCell block
  tree. Optional `"height"` (game units, default 4000) sets the terrain elevation — Z=0 is
  approximately Skyrim's sea level, so 4000+ is safely above water. Enter in-game with:
  `cow <worldspace editorId> X Y`. Optional `"navmesh": true` adds a flat quad NAVM (4 vertices,
  2 triangles) covering the full 4096×4096 cell so NPCs can path on it, plus a NAVI index entry.
  **In-game confirmed** (2026-06-04): a guard placed in a navmeshed cell of MFTestWorld walks an
  m1→m2→m3→m1 patrol on the generated mesh — NPCs path on a program-generated custom-worldspace
  navmesh. No edge-links to neighbour cells — cross-cell pathfinding requires matching NAVMs in
  adjacent cells. For varied terrain/LOD/detailed navmesh use the Creation Kit.
  **ESL LIMIT:** Skyrim's engine ignores LAND records in ESL (light) plugins — specs with `cells`
  must use `"esl": false` (the validator enforces this).
- **regions** (REGN): an area inside a `worldspace` (an in-spec WRLD `editorId` or a vanilla
  `"<master>:0xFORMID"`). `area` is a polygon of **>=3** world-space points (not cell grid).
  `weather` is the table that picks the active weather — each entry a WTHR *ref* + a relative
  `chance` (the chances must sum > 0); `weatherPriority` orders overlapping regions. `mapColor` is
  `0xRRGGBB`. Other RegionData kinds (sound/objects/grass/land) are CK-side and not emitted.
- Discover vanilla values with `find <Skyrim.esm> <name> Worldspace`, then
  `worlddiag <Skyrim.esm> <0xFORMID>` (climate/water/parent + map bounds + land/water defaults) and
  `regndiag <Skyrim.esm> <0xFORMID>` (worldspace/area/mapColor + weather table). Example:
  `examples/worldspace_spec.json`. **In-game confirmed** (2026-06-03): `cow MFTestWorld 0 0` →
  player lands on solid flat terrain. Navmesh-patrol example: `examples/worldspace_navmesh_test_spec.json`
  (guard pacing m1→m2→m3→m1 on the z=4000 mesh) — **in-game confirmed** (2026-06-04).

### leveled lists & containers
```jsonc
"leveledItems": [
  { "editorId": "MF_LootList", "chanceNone": 25,                 // 25% chance of nothing
    "flags": ["CalculateFromAllLevelsLessThanOrEqualPlayer"],
    "entries": [ { "reference": "MF_Blade", "level": 1, "count": 1 },
                 { "reference": "MF_Coin",  "level": 1, "count": 5 } ] }
],
"containers": [
  { "editorId": "MF_Chest", "name": "Forged Chest",
    "items": [ { "item": "MF_Coin", "count": 10 }, { "item": "MF_Apron", "count": 1 } ] }
]
```
- `leveledItems` (LVLI) and `leveledNpcs` (LVLN) are level-gated weighted lists: each
  `entry`'s `reference` is a *ref* (an in-spec item/npc, an external one, or another leveled
  list), gated by `level` and repeated `count` times. `chanceNone` (0–100) is the chance the
  list yields nothing; `flags` names come from the LVLI/LVLN flag set.
- `containers` (CONT) hold `items`, each an item *ref* + `count`. (To make the container
  appear in the world, place it with a `placement`, same as any object.)

### encounter zones & leveled-actor spawns — populating an area with scaled enemies
Two pieces work together to drop **level-appropriate** enemies into an area:

**1. A leveled-actor spawn** uses an **NPC_ wrapper** as the `base` — an NPC_ whose TEMPLATE chain
references a LeveledNpc list (LVLN), letting the engine roll a level-appropriate actor at spawn time.

> **CRITICAL GOTCHA — confirmed CTD (It.36, 2026-06-02):** `LChar*` formids (e.g. `0x03DECD`
> `LCharBanditMeleeAny`) are **LVLN records**, and a raw LVLN as an ACHR base **crashes Skyrim at
> load** — the engine calls NPC_-specific vtable methods on it. Use `LvlBandit*` NPC_ wrappers
> instead. The naming rule: `Lvl…` prefix = NPC_ (safe to place); `LChar…` prefix = LVLN (never
> place directly).

```jsonc
{ "base": "Skyrim.esm:0x01E79C", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditMeleeAny (NPC_)
  "position": { "x": -180, "y": 120, "z": 0 } }
```
- Find NPC_ wrappers with `find <Skyrim.esm> Lvl<…> Npc` (e.g. `LvlBanditMeleeAny` `0x01E79C`,
  `LvlBanditMissileNordM` `0x01B0D5`, `LvlBanditBossNordM` `0x01B0E1`). Their underlying LVLN lists
  (`LCharBanditMeleeAny` `0x03DECD`, etc.) are **not** valid placement bases.
- For an **in-spec** `leveledNpcs` list used as a placement base, add `"kind": "npc"` so the build
  emits a warning rather than silently producing a crashing plugin.

**2. An encounter zone** (`encounterZones`, ECZN) sets the **level range + respawn** the spawns roll
inside. A cell points at one via `encounterZone` (the whole cell), and/or an individual spawn does
(its own XEZN — a per-ref override).
```jsonc
"encounterZones": [
  { "editorId": "MF_BanditDenZone",
    "minLevel": 4, "maxLevel": 0,            // floor 4; maxLevel 0 = uncapped (scales with the player)
    "flags": ["MatchPcBelowMinimumLevel"] }  // below-min players get player-level spawns, not min
],
"cells": [
  { "editorId": "MF_BanditDen", "template": "Skyrim.esm:0x0165A8",
    "encounterZone": "MF_BanditDenZone" }    // wires the cell's level scaling/respawn
]
```
- `maxLevel 0` means **uncapped** — the vanilla dungeon idiom (e.g. `HelgenZone` is min 6 / max 0).
  Validate enforces `minLevel ≤ maxLevel` only when a real cap (`maxLevel > 0`) is set.
- `flags`: `NeverResets` (cleared dungeons stay cleared — no respawn), `MatchPcBelowMinimumLevel`
  (spawns match a low-level player instead of clamping to `minLevel`), `DisableCombatBoundary`
  (actors may chase out of the zone). `owner` (FACT/NPC) + `rank` set zone ownership; `location` (LCTN)
  links it to a map location.
- Inspect any zone with `eczndiag <plugin> <0xFORMID>` (level range / rank / flags / owner / location).
- **Navmesh caveat:** a brand-NEW in-spec cell has **no navmesh**, so spawned actors can't *path*
  until it's navmeshed in the Creation Kit — they stand where placed. Actors snap to the floor (unlike
  static markers), so any sane in-room coordinate works for placement, but movement/combat AI needs
  navmesh. Anchor on proven-walkable coords (`refpos`) or navmesh the cell in the CK before relying on
  patrols/pursuit. (See the worked `examples/encounter_spec.json`.)
- **IN-GAME CONFIRMED (It.36, 2026-06-02):** `coc MF_BanditDen` — cell loads, bandits spawn, no CTD.
  Full round-trip: encounter zone, cell template, NPC_ placements all verified in SSE 1.6.1170.

### vendors / merchants — a working shopkeeper
Turn an NPC into a functioning shop (buys + sells) by giving a **faction** a `vendor` sub-object and
making the NPC a member of it. A vanilla merchant is exactly this: a **Vendor-flagged FACT** (trade
hours, sell radius, buy-stolen flag, a buy/sell item-category list, and a **merchant chest** holding
the gold + stock) whose member NPC the engine treats as a shopkeeper.
```jsonc
"factions": [
  { "editorId": "MF_ShopFaction", "name": "ModForge General Goods",
    "vendor": {
      "startHour": 8, "endHour": 20,          // when the shop is open (0..24; start < end)
      "radius": 0,                             // how far the player may stray and still trade (0 = engine default)
      "buysStolen": false,                     // true = a fence (OnlyBuysStolenItems)
      "sellBuyList": "Skyrim.esm:0x06CB48",    // a FormList of VendorItem keywords (categories traded)
      "notSellBuyList": true,                  // true ⇒ sellBuyList is a NOT-sell list (trade ALL except those — the "general goods" pattern)
      "merchantContainer": "MF_ShopChestRef"   // ref to a PLACEMENT editorId: the placed merchant chest (gold + stock)
    } }
],
"containers": [
  { "editorId": "MF_ShopChest", "name": "Merchant Chest",
    "items": [ { "item": "Skyrim.esm:0x072AE7", "count": 1 },    // VendorGoldMisc (the vendor's gold pool)
               { "item": "Skyrim.esm:0x09AF0A", "count": 10 } ] }  // a stock leveled-list (LItemMiscVendorMiscItems75)
],
"placements": [
  { "editorId": "MF_ShopChestRef", "base": "MF_ShopChest", "cell": "MF_Shop", "persistent": true,
    "position": { "x": 0, "y": 256, "z": 0 } }
],
"npcs": [
  { "editorId": "MF_Shopkeeper", "name": "...", "race": "Skyrim.esm:0x013746",
    "factions": [ "MF_ShopFaction" ],          // membership = "this NPC is the vendor"
    "greeting": "Looking to buy?" }            // a greeting (or custom dialogue) makes it conversable — REQUIRED for the prompt
]
```
- **`sellBuyList`** is a *ref* to a vanilla `VendorItemsX` **FormList** (a list of `VendorItem*`
  keywords). Useful ones: `Skyrim.esm:0x06CB48` `VendorItemsMisc` (general goods), `0x066333`
  `VendorItemsBlacksmith`. With `notSellBuyList: false` the list names the categories the vendor
  **does** trade; with `notSellBuyList: true` it's a NOT-sell list (trade everything **except**).
  (In-spec FormLists aren't a record type yet, so reference a vanilla list — `find <Skyrim.esm>
  VendorItems FormList`.)
- **`merchantContainer`** must reference a **placement** `editorId` (the placed chest REFR), not the
  bare container — only a *placed* ref holds the gold/stock the engine reads. Put `VendorGoldMisc`
  (`Skyrim.esm:0x072AE7`, the leveled gold pool) in the chest so the vendor has money to buy with;
  add stock leveled-lists for what it sells. Build forces the chest placement `persistent`.
- **Membership = the shopkeeper.** An NPC in the vendor faction is the merchant. Build **auto-adds**
  `JobMerchantFaction` (`Skyrim.esm:0x051596`) to that NPC, because the vanilla generic "I'd like to
  trade" topic (`DialogueGeneric.OfferServicesTopic`) is gated on `GetInFaction JobMerchantFaction`
  + `GetOffersServicesNow`. You don't (and can't) emit that topic — it's universal vanilla dialogue
  that surfaces on any conversable, vendor-faction NPC during trade hours.
- **Conversable.** Same rule as all custom NPCs: the trade prompt only appears once the NPC opens a
  dialogue menu, which needs a `greeting` (auto-emits a Hello) or custom `dialogue[]`. A vendor with
  no greeting just mumbles (`validate` flags this).
- Inspect with `factdiag <plugin> <0xFORMID>` (vendor flag / hours / buy-sell list / merchant chest);
  `dump` also prints the vendor block. Compare to a vanilla merchant, e.g. `factdiag <Skyrim.esm>
  0x09CAF5` (Belethor's General Goods).
- **In-game-unconfirmed:** the FACT/chest/membership are structurally identical to a vanilla vendor
  (verified via `factdiag` diff), but whether the "I'd like to trade" prompt actually opens the
  barter menu needs a Proton/Skyrim launch — like all dialogue, it also only registers on a game
  **load** (new game or save+reload), not a mid-session `coc`.
