# ModForge spec — magic & stats

← [index](SPEC-index.md)

### Gameplay stats
- **Weapons:** give a `damage` (and usually `value`/`weight`). `speed` and `reach`
  default to `1.0` when any stat is set, so the weapon is swingable; override for slower/
  faster or longer/shorter weapons. A weapon with no stats is an inert item (it'll equip
  but do nothing useful).
- **Armor:** `armorType` is `light` / `heavy` / `clothing` (default `clothing`); `slots`
  lists the biped slots it occupies by `BipedObjectFlag` name — `Body`, `Head`, `Hands`,
  `Feet`, `Forearms`, `Calves`, `Shield`, `Amulet`, `Ring`, `Circlet`, … (multiple slots
  are OR'd). `armorRating` is the protection value.

### effects (spells & potions)
A spell or potion **does nothing without at least one effect**. Each effect is:
```jsonc
{ "magicEffect": "Skyrim.esm:0x03EB15",  // a MagicEffect *ref* (usually vanilla)
  "magnitude": 25, "area": 0, "duration": 0 }   // duration in seconds; 0 = instant
```
The `magicEffect` is a *ref* — a vanilla one (`find <Skyrim.esm> <query> MagicEffect`, e.g.
`AlchRestoreHealth = Skyrim.esm:0x03EB15`, `AlchDamageHealth = Skyrim.esm:0x03EB42`) **or** an
in-spec `magicEffects` entry's `editorId` (see below). A potion is fully functional with one
effect; a spell also wants cast/spell-type tuning but the effect is the core.

### magicEffects (custom MGEF)
Define your OWN effect instead of reusing a vanilla one; a spell/potion/ingredient/scroll `effect`
then points at it by `editorId` (and the per-cast `magnitude`/`area`/`duration` stay on that effect).
```jsonc
{ "editorId": "MF_RestoreHealthEffect", "name": "ModForge Restore Health",
  "archetype": "ValueModifier",   // ValueModifier (damage/heal/fortify) | DualValueModifier | SummonCreature | Bound | Light | Script | …
  "actorValue": "Health",          // what it acts on: Health | Magicka | Stamina | …
  "secondActorValue": "Magicka",   // DualValueModifier only: a 2nd affected AV (omit otherwise)
  "secondActorValueWeight": 0.5,   // DualValueModifier only: how the magnitude splits to the 2nd AV (0 = all to primary)
  "magicSkill": "Restoration",     // school: Alteration|Conjuration|Destruction|Illusion|Restoration
  "resistValue": "ResistFire",     // AV that resists it (optional): ResistFire | ResistFrost | PoisonResist | …
  "castType": "FireAndForget",     // FireAndForget | Concentration | ConstantEffect
  "targetType": "Self",            // Self | Touch | Aimed | TargetActor | TargetLocation
  "baseCost": 8.0,
  "flags": ["Recover"],            // Hostile | Detrimental | Recover | NoArea | NoDuration | NoMagnitude | …
  "association": "<ref>",          // summoned/bound form (only for Summon/Bound archetypes)
  "projectile": "<ref>",           // PROJ — the bolt that travels (needed for Aimed spells)
  "castingArt": "<ref>",           // ARTO — FX at the caster's hands
  "hitEffectArt": "<ref>",         // ARTO — FX at the impact point
  "explosion": "<ref>" }           // EXPL — AoE explosion on impact
```
A bare `ValueModifier` MGEF (no visual art/projectile) still applies its value — fine for Self/Touch
and for potions. A damage spell that *travels* (`targetType: Aimed`) needs a `projectile` (+ usually
`castingArt`); harvest a vanilla one with `mgefdiag <Skyrim.esm> <0xFORMID>` (e.g. the fire effect
`FireDamageFFAimed75 0x10F7F1` uses projectile `0x10FBEA` + castingArt `0x01B211`).

- **`DualValueModifier`** affects **two** actor values from one magnitude — set `archetype:
  "DualValueModifier"`, the primary `actorValue`, plus `secondActorValue` and `secondActorValueWeight`
  (the fraction of the magnitude routed to the second AV). This is how absorb/transfer-style effects
  (damage one stat, feed another) are built.
- **`Script`-archetype MGEF** (boss-spell logic, custom on-apply behaviour) runs **Papyrus**: set
  `archetype: "Script"` and attach a script. Two equivalent ways: an **inline** `magicEffects[].scripts[]`
  entry (the `targetEditorId` is implied — keeps the script next to the effect), or a top-level
  **`scripts[]`** entry whose `targetEditorId` is this MGEF's `editorId`. Both use the same shape (see
  [SPEC-quests](SPEC-quests.md) § scripts) and `package` compiles each entry's `source`. The `.psc`
  extends `ActiveMagicEffect`. Prefer the inline form for readability.

**Flags matter — match the effect's timing (this is the #1 gotcha):**
- **Instant** restore/damage (`duration` 0) → `["NoDuration", "NoArea"]`, and add `"Detrimental"`
  (+`"Hostile"`) for damage. Do **NOT** set `Recover` — `Recover` reverts the value when the effect
  *ends*, and an instant effect ends immediately, so the change is undone (a heal applies +N then
  instantly removes it → **net zero, looks like "casts but does nothing"**).
- **Timed** fortify (`duration` > 0, e.g. +50 Health for 60s) → `["Recover", "NoArea"]`: `Recover`
  cleanly removes the bonus when the timer expires. This is `Recover`'s correct use.
Keep `baseCost` low (vanilla restore/damage effects use ~0.5–3); the spell's magicka cost is
auto-calculated from `baseCost` × `magnitude`, so a large `baseCost` makes the spell absurdly
expensive. Compare any effect to a vanilla one with `mgefdiag <Skyrim.esm> <0xFORMID>`.

### effectShaders (EFSH) — texture-only membrane and particle VFX

`effectShaders[]` emits normal EFSH records without generating a particle `.nif`. An EFSH can tint
the target's existing mesh (the membrane layer) and emit flat particle sprites from actors. Point a
`magicEffects[]` entry's `hitShader` or `enchantShader` at its editorId:

```jsonc
"effectShaders": [{
  "editorId": "MFEffShFireGlow",
  // Paths are relative to Data/Textures — do not include a leading Textures/.
  "fillTexture": "MFVfx/firefill.dds",
  "particleTexture": "MFVfx/spark.dds",
  "paletteTexture": "MFVfx/gradient.dds", // fallback for both membrane + particle palettes
  "flags": ["ParticleGrayscaleColor"],
  "membrane": {
    "sourceBlend": "SourceAlpha", "destBlend": "One", "blendOperation": "Add",
    "fillColor": {"r":255,"g":140,"b":40}, "edgeColor": {"r":255,"g":80,"b":0},
    "fillFadeInTime": 0.25, "fillFullTime": 1.0, "fillFadeOutTime": 0.5
  },
  "particle": {
    "persistentCount": 80, "lifetime": 1.2, "initialSpeed": 30, "acceleration": -10,
    "scaleKeys": [{"time":0,"scale":0.4},{"time":1,"scale":1.2}],
    "colorKeys": [
      {"time":0,"color":{"r":255,"g":200,"b":50},"alpha":1},
      {"time":1,"color":{"r":120,"g":20,"b":0},"alpha":0}
    ]
  }
}],
"magicEffects": [{
  "editorId": "MFEff_FireGlow", "archetype": "ValueModifier", "actorValue": "Health",
  "hitShader": "MFEffShFireGlow", "enchantShader": "MFEffShFireGlow"
}]
```

Particle key times are normalized `0..1` over particle lifetime; membrane fade times are seconds.
The engine may silently render no particles when the particle palette is missing, so build emits a
loud warning. `paletteTexture` fills both palette slots; use `membranePaletteTexture` and
`particlePaletteTexture` when they differ. EFSH sprites emit from actors; inanimate placed statics do
not become general-purpose particle emitters. The offline builder and record wiring are verified;
appearance still needs an in-game check with real `.dds` assets. Full example: `effect_shader.json`.

### projectiles (PROJ) & explosions (EXPL) — custom spell bolts & booms
Give a custom destruction spell its OWN flying bolt and impact explosion (instead of reusing a vanilla
one). The chain, built bottom-up: **EXPL** ← **PROJ** (references the EXPL) ← **MGEF** (`projectile` =
the PROJ) ← **SPEL** (Aimed / FireAndForget).
```jsonc
"explosions": [
  { "editorId": "MF_Boom", "name": "Forged Blast",
    "model": "Effects\\FXEmptyExplosionArt.nif",   // a VERIFIED vanilla nif (wrong path = invisible)
    "damage": 15, "force": 7, "radius": 256, "isRadius": 1280,
    "sound": "Skyrim.esm:0x02518F",                // vanilla fire-impact sound
    "imageSpaceModifier": "Skyrim.esm:0x0010FBE8", // vanilla fire-blast screen FX
    "flags": [ "IgnoreLosCheck" ] } ],
"projectiles": [
  { "editorId": "MF_Bolt", "name": "Forged Bolt",
    "type": "Missile",                             // Missile|Lobber|Beam|Flame|Cone|Barrier|Arrow
    "speed": 2500, "gravity": 0, "range": 10000, "lifetime": 10, "impactForce": 1,
    "flags": [ "Explosion" ],                      // trigger the explosion on impact
    "model": "Magic\\FireBoltProjectile.nif",      // the REAL vanilla firebolt nif → visible bolt
    "light": "Skyrim.esm:0x0001CBB3", "sound": "Skyrim.esm:0x0003C8FE",
    "explosion": "MF_Boom" } ],                     // ref → the in-spec EXPL (built first)
```
Then point a MGEF at the bolt: `"projectile": "MF_Bolt"` on a custom `magicEffects` entry, and put that
effect on an Aimed `spells` entry (see `examples/projectile-explosion.json` for the full castable chain).
**Always verify nif/art paths** against Skyrim.esm (a wrong `model` = an invisible projectile, no error) —
decode a vanilla PROJ/EXPL with Mutagen and copy its model/light/sound/imagespace. Explosions are built
before projectiles, so a PROJ resolves its `explosion` by editorId. Both are normal base records;
`ImpactDataSet`/`ObjectEffect` (AoE MGEF) are optional refs.

### imageSpaceModifiers (IMAD) — screen-space post-process

A top-level `imageSpaceModifiers: []` of screen post-process records (brightness/contrast/tint), used
by an `explosions[].imageSpaceModifier` ref or applied/removed from a Papyrus `ImageSpaceModifier`
property (`ApplyCrossFade()` / `Remove()`).

```jsonc
"imageSpaceModifiers": [
  { "editorId": "MFDaylightIMAD",
    "brightnessMultiplier": 1.6,   // CinematicBrightnessMult (1=neutral, >1 brighter)
    "contrast": 1.05, "saturation": 0.92,
    "tintColor": { "r": 255, "g": 250, "b": 235 }, "tintAmount": 0.15,  // amount -> colour alpha
    "duration": 1.0, "animatable": false }
]
```

Mutagen models every IMAD field as an animatable curve; the builder writes one keyframe per field
(tint = one ColorFrame). See `examples/daylight_spell_spec.json`. Note: that example's *runtime*
"daylight" effect was ultimately moved to an SKSE plugin (real follow-light + live cell-ambient,
which — unlike a screen filter — don't wash out low-albedo objects); the IMAD builder remains a
general ESP-side capability.

### hazards (HAZD) — radius effect / placed trap

A top-level `hazards: []` of environmental hazards — a fire/frost/poison patch that periodically
applies a spell to actors inside its radius (the engine's fire-trap / lingering-AoE mechanism).

```jsonc
"hazards": [
  { "editorId": "MFHZ_Fire", "name": "Flames",
    "model": "Meshes/Traps/PressurePlateFire/NorTrapFirePlateFX.nif",  // visual nif (verify vs Skyrim.esm)
    "radius": 150,            // effect radius
    "lifetime": 8,           // seconds it persists (0 = inherit from the spawning spell / permanent)
    "targetInterval": 1,     // seconds between applying `spell` to actors in radius
    "limit": 0,              // max simultaneous instances (0 = unlimited)
    "spell": "MFHZ_BurnSpell", // ref -> the SPEL applied periodically (the actual effect)
    "flags": [ "DropToGround" ], // AffectsPlayerOnly | InheritDurationFromSpawnSpell | AlignToImpactNormal | InheritRadiusFromSpawnSpell | DropToGround
    "light": "...", "sound": "Skyrim.esm:0x000F57E6", // optional refs (LIGT / SNDR)
    "imageSpaceModifier": "...", "impactDataSet": "..." } // optional refs (IMAD / IPDS)
]
```

**Two ways to use a hazard** (both shipped):
1. **Spell-spawn** — a `magicEffects[]` entry with `"archetype": "SpawnHazard"` and
   `"association": "MFHZ_Fire"`, put on a `TargetLocation` spell → a castable spell that drops the
   hazard on the ground. Reuses the existing MGEF archetype/association wiring (no special fields).
2. **Placed trap** — a `placements[]` entry whose `base` is the hazard editorId (or `"kind": "hazard"`)
   → a static `PlacedHazard` in the cell (a dungeon fire trap). See `SPEC-world.md`.

A hazard with no `spell` applies nothing (validate warns); a model-less hazard is invisible (verify
the nif path vs Skyrim.esm — see vanilla-nif-paths-must-be-verified). Full worked example (both paths):
`examples/hazard.json`.

### enchantments (ENCH / Object Effect)
An **Object Effect** bundles one or more MGEF-based `effects` (the SAME `{ magicEffect, magnitude,
area, duration }` shape as a spell/potion effect) into a reusable enchantment that a **weapon** or
**armor** references via its `enchantment` field. `enchantType` picks the behaviour family and its
vanilla-default cast/target (verified against `Skyrim.esm`):

| `enchantType` | EnchantType | default castType / targetType | charge | use |
|---------------|-------------|-------------------------------|--------|-----|
| `weapon`  | `Enchantment`      | `FireAndForget` / `Touch` | weapon carries the pool (`enchantmentAmount`) | cast-on-strike (frost/fire/absorb weapon) |
| `apparel` | `Enchantment`      | `ConstantEffect` / `Self` | none — always-on while worn | fortify/resist/regen apparel |
| `staff`   | `StaffEnchantment` | `FireAndForget` / `Aimed` | staff carries the pool | staff "cast on use" (vanilla staves set `chargeTime` ~0.5) |

```jsonc
"enchantments": [
  { "editorId": "MF_FrostWeaponEnch", "name": "Frost Damage",
    "enchantType": "weapon",          // weapon | apparel | staff
    "enchantmentCost": 15,            // per-cast charge cost drained from the weapon's pool
    // "castType": "...", "targetType": "...",  // optional — override the family defaults
    "effects": [ { "magicEffect": "MF_FrostDamageEnchEffect", "magnitude": 10 } ] }
],
"weapons": [
  { "editorId": "MF_FrostIronSword", "name": "Frostbite Iron Sword",
    "template": "Skyrim.esm:0x012EB7",   // clone a vanilla weapon for the model (else CRASH on equip)
    "damage": 8,
    "enchantment": "MF_FrostWeaponEnch", // ref → in-spec ENCH or vanilla <master>:0xFORMID
    "enchantmentAmount": 1500 }          // the weapon's charge pool (casts before recharge)
]
```
An `apparel` (constant-effect) enchantment goes on an **armor** the same way (no `enchantmentAmount` —
apparel is passive). The `enchantment` ref may also be a **vanilla** ObjectEffect
(`find <Skyrim.esm> Ench... ObjectEffect`, e.g. `EnchWeaponFrostDamageBase = Skyrim.esm:0x10FB96`).
Inspect a built or vanilla ENCH with `enchdiag <in.esp> <0xFORMID>`. Worked example:
[`examples/enchantment_spec.json`](../../examples/enchantment_spec.json). *(Structurally verified; the
enchantment actually firing in-game is unconfirmed — see the cookbook recipe note.)*
