<!-- Part 3/5 — World building and items -->
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
    { "base": "Skyrim.esm:0x03DECD", "cell": "MF_BanditDen", "kind": "npc",   // LCharBanditMeleeAny
      "position": { "x": -180, "y": 120, "z": 0 } },
    { "base": "Skyrim.esm:0x01A348", "cell": "MF_BanditDen", "kind": "npc",   // LCharBanditMissileNordM (archer)
      "position": { "x":  180, "y": 120, "z": 0 } },
    { "base": "Skyrim.esm:0x01A341", "cell": "MF_BanditDen", "kind": "npc",   // LCharBanditBossNordM (boss)
      "position": { "x": 0, "y": -120, "z": 0 }, "encounterZone": "MF_BanditDenZone" }  // per-ref XEZN (optional)
  ] }
```

- A **vanilla** LVLN base (`Skyrim.esm:0x…`) needs `"kind": "npc"` — the build can't read the master's
  record type headlessly. An **in-spec** `leveledNpcs` base auto-detects as an actor.
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

## "Custom aimed combat spell" (MGEF + projectile + SPEL)

```jsonc
{ "magicEffects": [
    { "editorId": "MF_Firebolt", "archetype": "ValueModifier", "actorValue": "Health",
      "magicSkill": "Destruction", "resistValue": "ResistFire",
      "castType": "FireAndForget", "targetType": "Aimed", "baseCost": 12.0,
      "flags": [ "Hostile", "Detrimental", "NoArea" ],   // NOT Recover (it's instant)
      "projectile": "Skyrim.esm:0x10FBEA",               // reuse vanilla firebolt projectile (visible bolt + impact)
      "castingArt": "Skyrim.esm:0x01B211" }              // hands FX
  ],
  "spells": [
    { "editorId": "MF_FireboltSpell", "name": "Forged Firebolt",
      "spellType": "Spell", "castType": "FireAndForget", "targetType": "Aimed",
      "equipType": "Skyrim.esm:0x013F44",                // EitherHand — REQUIRED or the NPC can't equip/cast it
      "effects": [ { "magicEffect": "MF_Firebolt", "magnitude": 25, "area": 0, "duration": 0 } ] }
  ] }
```

Reusing a vanilla `projectile` + `castingArt` is what makes the bolt visible and lets it deliver the
hit. Without `equipType` the NPC melees / never casts — the #1 silent failure for a generated combat spell.

## "Enchanted weapon for a custom effect" (MGEF + ENCH + WEAP + COBJ)

Three layers: a custom **MGEF** (what happens on hit) → an **enchantment** / ENCH (the reusable
"object effect", `enchantType: weapon`) → a **weapon** that references it and carries a charge pool.
Add a COBJ so the player can craft it. (For a passive **apparel** enchant, use `enchantType: apparel`
and put `enchantment` on an `armor` instead — no `enchantmentAmount`, it's always-on while worn.)

> **Armor must carry a `template` or it equips INVISIBLE** (IN-GAME CONFIRMED 2026-06-01: the
> templated cuirass shows the iron-armor mesh when worn). An ARMO's worn mesh lives on its
> Armature (ARMA addon records), not the ARMO — a spec armor with only `armorType`+`slots` renders
> nothing when worn (it does *not* crash). Set `template` to a vanilla armor of the same slot, e.g.
> `"template": "Skyrim.esm:0x00012E49"` (ArmorIronCuirass); the clone brings the Armature (worn mesh),
> the WorldModel (ground model), and the BodyTemplate. Build warns if a `template` is missing.

```jsonc
{ "magicEffects": [
    { "editorId": "MF_FrostDamageEnchEffect", "name": "Frost Damage",
      "archetype": "ValueModifier", "actorValue": "Health",
      "magicSkill": "Destruction", "resistValue": "ResistFrost",
      "castType": "FireAndForget", "targetType": "Touch", "baseCost": 1.5,
      "flags": [ "Hostile", "Detrimental", "NoArea" ] }
  ],
  "enchantments": [
    { "editorId": "MF_FrostWeaponEnch", "name": "Frost Damage",
      "enchantType": "weapon",          // → EnchantType=Enchantment, cast=FireAndForget, target=Touch
      "enchantmentCost": 15,            // per-strike charge drained from the weapon's pool
      "effects": [ { "magicEffect": "MF_FrostDamageEnchEffect", "magnitude": 10 } ] }
  ],
  "weapons": [
    { "editorId": "MF_FrostIronSword", "name": "Frostbite Iron Sword",
      "template": "Skyrim.esm:0x012EB7", "damage": 8,   // template = model (else CRASH on equip)
      "enchantment": "MF_FrostWeaponEnch", "enchantmentAmount": 1500 }   // 1500 = charge pool
  ],
  "recipes": [
    { "editorId": "MF_FrostIronSwordRecipe", "createdObject": "MF_FrostIronSword",
      "components": [ { "item": "Skyrim.esm:0x05ACE4", "count": 2 },     // IngotIron
                      { "item": "Skyrim.esm:0x02E4FC", "count": 1 } ] }  // SoulGemGrand
  ] }
```

Full file: [`examples/enchantment_spec.json`](../../examples/enchantment_spec.json). Verify with
`enchdiag <out.esp> <0xFORMID>` (ENCH type/cost/effects) and `dump` (the weapon's `enchantment ->`
link + charge). **Note — structurally verified only:** the records build, validate, link and round-trip
correctly and mirror vanilla ENCH structure exactly, but the enchantment actually *firing* in-game has
not been confirmed (no in-game test was run). The `enchantmentCost` ↔ `enchantmentAmount` tuning and
whether the engine auto-prices the charge are the most likely things to verify in-game.

