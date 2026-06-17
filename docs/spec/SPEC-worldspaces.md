# ModForge spec — worldspaces, lists & spawns

← [index](SPEC-index.md) · cells, placements & lighting → [SPEC-world](SPEC-world.md)

Exterior worldspaces & regions, area music, leveled lists / formLists / containers, encounter-zone
spawns, and vendor/merchant setup. For interior cells, object placement and lighting see
[SPEC-world](SPEC-world.md).

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
- **`heightmap`** — **non-flat** terrain from a 16-bit grayscale PNG (mutually exclusive with
  `cells`; if both are given, heightmap wins and build warns). ModForge derives the whole cell grid
  from the PNG size and emits a sloped LAND (VHGT) per cell. Replaces the per-cell `cells` list.
  ```jsonc
  "heightmap": {
    "path": "worldspace_heightmap.png",  // relative to the spec file; PNG width MUST be N×32+1,
    "originX": 0, "originY": 0,           //   height M×32+1 (e.g. 33,65,97…) → N×M cells generated
    "minHeight": 4000,                    // game units at png value 0   (linear map)
    "maxHeight": 4500                     // game units at png value 65535
  }
  ```
  Image orientation: **bottom-left pixel = cell `(originX, originY)` south-west vertex**; image-right
  = cell +X (east), image-up = cell +Y (north). Adjacent cells share the boundary pixel column/row —
  build also propagates reconstructed edge heights between cells (**seam stitching**) so both sides
  of every shared boundary decode to identical game-unit heights; no visible crack in engine.
  Per-vertex height = `minHeight + (png/65535) × (maxHeight − minHeight)`. The per-128-unit slope
  between vertices is capped at ±1016 game units (VHGT signed-byte limit); steeper terrain is
  clamped and build warns. **`defaultLandHeight` tip:** set it equal to `minHeight` so cells outside
  the PNG area meet the heightmap perimeter at the same elevation (no cliff at the world boundary).
  **MVP scope:** no navmesh is generated in heightmap mode; per-vertex **VNML normals ARE** computed
  from the heightmap (central-difference, self-verified byte-for-byte vs vanilla Tamriel LAND
  2026-06-16 — see `landed/world`). Same **ESL LIMIT** as `cells` (LAND ⇒ `"esl": false`). Worked example:
  `examples/worldspace_heightmap.json` (+ `worldspace_heightmap.png`, a 97×33 = 3×1-cell hill).
  **In-game confirmed** (2026-06-16): terrain has bumps, cell seams closed, no cracks between cells.
- **`baseTexture`** — optional single-layer terrain texture: an LTEX *ref* applied as the BASE
  layer (BTXT) of **every** cell's LAND, all 4 quadrants. The whole world gets one ground texture
  with no per-vertex blending (`""` / omit = untextured, engine falls back to the default land
  texture). Works with both `cells` and `heightmap`. Multi-texture blending is `textureLayers` below.
  ```jsonc
  "baseTexture": "Skyrim.esm:0x000C16"   // LTEX ref; one ground texture for the whole world
  ```
  **Construction-tested offline** (2026-06-17); byte-level parity vs vanilla LAND BTXT is a
  pending xEdit check on the main machine (WAIT_USER).
- **`textureLayers`** — optional **multi-texture blend** on top of `baseTexture`. Each entry is an
  LTEX *ref* + a **grayscale splatmap PNG** (same grid rule as the heightmap: width `N×32+1`,
  shared edge columns). Each splatmap pixel `0..255` → that vertex's alpha `0..1` for that texture;
  ModForge emits sparse `ATXT`+`VTXT` alpha layers per cell quadrant (vertices with zero alpha are
  omitted, like vanilla; a quadrant the splatmap doesn't cover gets no layer). List order = stacking
  order (`baseTexture` = layer 0, then `textureLayers[0]` = layer 1, …). A splatmap's `originX/Y`
  must align with the heightmap/cell grid; cells outside a splatmap's extent simply skip that layer.
  ```jsonc
  "baseTexture": "Skyrim.esm:0x000C16",            // base dirt for the whole world
  "textureLayers": [
    { "texture": "Skyrim.esm:0x0008C5",            // grass, painted where the splatmap is non-zero
      "splatmap": { "path": "grass_alpha.png", "originX": 0, "originY": 0 } }
  ]
  ```
  **Construction-tested offline** (2026-06-17). Byte-level parity vs vanilla `VTXT` (exact point
  position order, per-quadrant layer-number packing) is a pending xEdit check (WAIT_USER). The Godot
  splat-paint brush that authors these PNGs is the next front-end step (`godot-worldspace-editor`).
- **`godotPlacements`** — optional: object placements authored in the `godot-worldspace-editor`
  front-end, exported as a `placements.json` and converted to REFR. Use **alongside** `heightmap`;
  set `originX/Y` to match the heightmap grid. Fields: `path` (json relative to the spec file),
  `originX/Y` (cell-grid origin of the Godot scene's south-west corner). Coordinates are converted
  from the editor's Y-up space into Skyrim world coords and merged into the placement pipeline.
  ```jsonc
  "godotPlacements": { "path": "placements.json", "originX": 0, "originY": 0 }
  ```
  **Offline-complete** (2026-06-17); a one-pass Godot GUI run on the main machine is pending (WAIT_USER).
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

### music (MUSC / MUST) — custom area music

Two records: **Music Tracks** (`musicTracks[]`, MUST — the audio entries) and **Music Types**
(`music[]`, MUSC — containers the game selects between and assigns to a place).

```jsonc
"musicTracks": [
  { "editorId": "MFMU_A", "type": "SingleTrack",   // SingleTrack | Palette | SilentTrack
    "file": "Music\\ModForge\\a.xwm",               // audio under Data/Music (.xwm/.wav); a loose asset
    "loopBegins": 0, "loopEnds": 60, "loopCount": 0, // optional loop (seconds; loopCount 0 = infinite)
    "fadeOut": 2 },
  { "editorId": "MFMU_Pool", "type": "Palette", "tracks": [ "MFMU_A", "MFMU_B" ] } // Palette = a pool of MUST
],
"music": [
  { "editorId": "MFMU_Type", "flags": [ "CycleTracks" ], // PlaysOneSelection|AbruptTransition|CycleTracks|MaintainTrackOrder|DucksCurrentTrack|DoesNotQueue
    "priority": 10,             // higher wins over lower-priority music playing at the same time
    "duckingDecibel": 6,        // POSITIVE dB attenuation applied to other audio (0–655)
    "fadeDuration": 4,
    "tracks": [ "MFMU_Pool" ] } // refs -> MUST (SingleTrack, Palette, or SilentTrack)
]
```

**Assign a MUSC** to play it: `cells[].music: "MFMU_Type"` (interior cell) and/or
`worldspaces[].music: "MFMU_Type"` (whole exterior world). Both take an in-spec MUSC editorId or a
vanilla `<master>:0xFORMID`.

**Audio assets:** the `.xwm` (or `.wav`) files are loose assets under `Data/Music/...`; the builder only
writes the path. Ship the files via `package --assets <dir>` / `spec.assets` (like voice files). A
missing file = silence, no crash. Worked example: `examples/music.json`.

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

### formLists — FLST
A **FormList** (`FLST`) is an ordered list of FormIDs of **any** type — a reusable grouped set
of forms.
```jsonc
"formLists": [
  { "editorId": "MF_FancyClothes",
    "items": [ "MF_NobleDress", "Skyrim.esm:0x0010DF5", "Skyrim.esm:0x0010CD1" ] }   // refs, any type
]
```
- `items` are *refs* — an in-spec `editorId` or a vanilla `<master>:0xFORMID` — and **order is
  preserved**.
- The big use is as the **parameter of a list-taking condition**: `GetItemCount`, `GetEquipped`,
  `GetIsVoiceType`, and `GetInWorldspace` all accept a FormList in their `param`, so e.g. a dialogue
  line can gate on "is the player wearing **any** of these" by pointing `GetEquipped`'s `param` at a
  clothing FLST. FLSTs also serve as keyword/clothing sets and anywhere a grouped set of forms is
  wanted. (There is **no** standalone `GetIsInList` condition in Mutagen 0.49 — feed the FLST to the
  existing `*OrList` params instead.)
- The **`GetInCurrentLoc`** condition gates on whether the run-on actor is in a given **Location**
  (`param` = a LCTN ref) — useful for location-aware comments.

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
