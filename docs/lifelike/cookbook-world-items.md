<!-- World & items patterns -->
# Recipe cookbook — world & items

← [cookbook index](cookbook-index.md) | [lifelike hub](README.md)

## "Usable interior cell" (lighting + floor, not a black void)

A brand-new interior cell needs three things or it's a pitch-black void you fall through:

```jsonc
{ "cells": [
    { "editorId": "MF_Hall", "name": "Forged Hall",
      "template": "Skyrim.esm:0x0165A8" }   // Breezehome — inherits interior lighting via CopyCellEnv
  ],
  "statics": [ { "editorId": "MF_Floor", "model": "..." } ],  // or place vanilla WRIntFloorSTMid01Large 0x1044AA
  "placements": [
    // a 3×3 floor grid at 256 spacing, a non-PortalStrict omni key light, wall pieces
    { "base": "Skyrim.esm:0x1044AA", "cell": "MF_Hall", "position": { "x": 0,   "y": 0, "z": 0 } },
    { "base": "Skyrim.esm:0x0C82AE", "cell": "MF_Hall", "position": { "x": 0,   "y": 0, "z": 200 } } // WRShadowOmni key light
  ] }
```

Lighting comes from the `template` (code path `CopyCellEnv`); floor + light are just placements.
Use a non-PortalStrict omni light (`WRShadowOmni 0x0C82AE`) — a `PortalStrict` light lights nothing
in a portal-less cell.

## "Populate a dungeon with scaled enemies" (encounter zone + leveled spawns)

Drop **level-appropriate** enemies into an area: an encounter zone (ECZN) sets the level range +
respawn; each spawn's `base` is a **LeveledNpc list** so the engine rolls a scaled actor at load.

```jsonc
{ "encounterZones": [
    { "editorId": "MF_BanditDenZone", "minLevel": 4, "maxLevel": 0,   // max 0 = uncapped (scales w/ player)
      "flags": [ "MatchPcBelowMinimumLevel" ] }
  ],
  "cells": [
    { "editorId": "MF_BanditDen", "name": "Bandit Den",
      "template": "Skyrim.esm:0x0165A8",          // lighting (else black) — see "Usable interior cell"
      "encounterZone": "MF_BanditDenZone" }       // the whole cell's level scaling/respawn
  ],
  "placements": [
    // ... a floor grid (WRIntFloorSTMid01Large 0x1044AA) so you don't fall into the void ...
    { "base": "Skyrim.esm:0x01E79C", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditMeleeAny (NPC_)
      "position": { "x": -180, "y": 120, "z": 0 } },
    { "base": "Skyrim.esm:0x01B0D5", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditMissileNordM (NPC_ archer)
      "position": { "x":  180, "y": 120, "z": 0 } },
    { "base": "Skyrim.esm:0x01B0E1", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditBossNordM (NPC_ boss)
      "position": { "x": 0, "y": -120, "z": 0 }, "encounterZone": "MF_BanditDenZone" }  // per-ref XEZN (optional)
  ] }
```

- **CRITICAL — confirmed CTD (It.36, 2026-06-02):** `LChar*` formids (e.g. `0x03DECD` `LCharBanditMeleeAny`)
  are **LVLN records**, and a raw LVLN as an ACHR base **crashes Skyrim at load**. Place the `LvlBandit*`
  **NPC_ wrappers** instead (`Lvl…` prefix = NPC_, safe to place; `LChar…` prefix = LVLN, never place
  directly). An **in-spec** `leveledNpcs` base auto-detects as an actor (the build emits a warning for it).
- `maxLevel 0` = uncapped (the vanilla idiom; `HelgenZone` is min 6 / max 0). `MatchPcBelowMinimumLevel`
  gives a low-level player player-scaled spawns instead of clamping to `minLevel`; `NeverResets` makes
  a cleared den stay cleared.
- Verify: `validate` → `build` → `dump` (cell `encZone ->`, each `placed npc -> base …`, the ECZN's
  `levels [min..max]`) and `eczndiag <plugin> <0xFORMID>`. Worked spec: `examples/encounter_spec.json`.
- **Navmesh:** a brand-new cell has **no navmesh**, so spawns stand where placed and can't path/pursue
  until you navmesh the cell in the CK. Actors snap to the floor, so placement coords are forgiving, but
  movement/combat AI is not active until navmeshed (structural-only until then).

## "Craftable item" (COBJ recipe)

Simpler than it looks: the workbench is a plain keyword FormLink (defaults to the forge), **not** a
CTDA condition; components reuse the container item/count shape; perk/skill gating (`conditions`) is
optional and a basic recipe needs none.

```jsonc
{ "recipes": [
    { "editorId": "MF_ForgeSword", "createdObject": "<MF_MySword>", "count": 1,
      // "workbench": "forge",   // named selector — forge is the default, can omit
      "components": [ { "item": "Skyrim.esm:0x05ACE4", "count": 3 },    // IngotIron
                      { "item": "Skyrim.esm:0x0800E4", "count": 1 } ] } // LeatherStrips
  ] }
```

## "Retexture without a new mesh" (TXST + alternateTextures)

Reskin a vanilla object by reusing its `.nif` and pointing one of its materials at your own
**TextureSet (TXST)**. No mesh authoring — only texture paths.

```jsonc
{ "textureSets": [
    { "editorId": "MF_GildedRubbleTexture",
      "diffuse": "ModForge\\rubble\\gilded_rubble_d.dds",   // relative to Data\Textures\ — OMIT the "Textures\" prefix
      "normal":  "ModForge\\rubble\\gilded_rubble_n.dds",
      "flags": [ "NoSpecularMap" ] }
  ],
  "statics": [
    { "editorId": "MF_GildedRubble",
      "model": "Dungeons\\Nordic\\Rubble\\NorRubblePiece03.nif",   // a VANILLA mesh, reused as-is
      "alternateTextures": [
        { "name": "NorRubblePiece03:0", "index": 0,                 // material/3D-name inside the .nif
          "textureSet": "MF_GildedRubbleTexture" } ] }
  ] }
```

Gotchas:
- **Path root.** TXST slot paths are relative to `Data\Textures\`, so they OMIT the leading
  `Textures\` (mirrors how `model` omits `Meshes\`). Validate rejects a stray `Textures\` prefix.
- **The `name` must match the mesh.** It's `<3DName>:<index>` from the `.nif`'s shader properties
  (CK *Model Data → AltTex*, or NifSkope's `BSLightingShaderProperty` names). A wrong name swaps
  nothing — silently. Mirror a vanilla example: `txstdiag <Skyrim.esm>` lists every TXST, `dump`
  prints a record's `altTexture` lines, and vanilla STAT `NorExtRubblePiece03_HeavySN` shows the
  `NorRubblePiece03:0` / index 0 pattern this recipe copies.
- **`textureSet` is a ref** — an in-spec TXST editorId or a vanilla `<master>:0xFORMID`.
- **You author the `.dds`.** ModForge writes the record + references; it cannot create or render
  texture content, and the headless toolchain can't confirm the swap looks right — only a Skyrim
  launch does. Drop your authored `.dds` files under `Data/Textures/<your path>/` in the mod folder.

Verify structurally: `validate` → `build` → `txstdiag <out.esp>` (slots written) and
`dump <out.esp>` (the `altTexture` wiring + its `-> <TXST>` target).

## "Craftable + temperable weapon" (perk-gated forge + grindstone temper + smelt)

A complete smithing chain: forge the weapon (perk-gated so it only shows once you've taken
SteelSmithing), improve it at the grindstone, and smelt ore into the ingots it costs. `workbench` is
a **named selector** (`forge`/`sharpeningWheel`/`armorTable`/`smelter`/`tanningRack`/`skyforge`); the
recipe `kind` (`craft`/`temper`/`smelt`/`breakdown`) sets a sensible default bench so you can often
omit it. A `temper` recipe's `createdObject` **is** the weapon itself, and mirrors vanilla by adding
the `TemperIsEnchanted`(`or: true`) guard before the smithing `HasPerk`. Conditions are the shared
CTDA `ConditionSpec` (`function`/`param`/`comparison`/`value`/`or`). Discover perk/ingredient FormIDs
with `find Skyrim.esm SteelSmithing Perk`; inspect any recipe with `cobjdiag <esp> <0xID>`. A full
runnable version is [`examples/smithing_spec.json`](../../examples/smithing_spec.json).

```jsonc
{ "recipes": [
    // FORGE — perk-gated craft (SteelSmithing perk = Skyrim.esm:0x0CB40D)
    { "editorId": "MF_ForgeBlade", "kind": "craft", "createdObject": "<MF_MyBlade>",
      "workbench": "forge",
      "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 2 },     // SteelIngot
                      { "item": "Skyrim.esm:0x0800E4", "count": 1 } ],   // LeatherStrips
      "conditions": [ { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] },

    // GRINDSTONE — temper (createdObject = the blade; enchant-guard + perk, exactly like vanilla)
    { "editorId": "MF_TemperBlade", "kind": "temper", "createdObject": "<MF_MyBlade>",
      "workbench": "sharpeningWheel",
      "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 1 } ],   // SteelIngot
      "conditions": [
        { "function": "TemperIsEnchanted", "comparison": "!=", "value": 1, "or": true },
        { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] },

    // SMELTER — ore -> ingot (no conditions)
    { "editorId": "MF_SmeltIron", "kind": "smelt", "createdObject": "Skyrim.esm:0x05ACE4",
      "components": [ { "item": "Skyrim.esm:0x071CF3", "count": 1 } ] }   // IronOre -> IronIngot
  ] }
```

Structurally verified (`dump`/`cobjdiag` show the temper recipe byte-for-byte matching vanilla
`TemperWeaponSteelSword` apart from the target/perk). **In-game NOT yet confirmed** — that the
recipe actually appears at the bench / temper applies requires running the game.
