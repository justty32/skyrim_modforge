# ModForge spec — authoring reference

The **spec** is a JSON file describing the content of one Skyrim plugin. It is the
contract between intent (natural language, turned into a spec by an AI agent) and the
deterministic generator (Mutagen). You write/produce a spec, `validate` it, then `build`
or `package` it.

```
NL / idea ──(AI agent: Claude Code)──▶ spec.json ──(validate)──▶ ──(build | package)──▶ .esp [+ .pex]
```

Property names are **case-insensitive** (`editorId` == `EditorId`); examples use camelCase.

## Cross-references & IDs

- Every record has an **`editorId`** — a stable, unique name you choose. It is how
  records reference each other *within the spec* (an npc joins a faction by its
  `editorId`; a dialogue names its quest by `editorId`). It is **not** a FormID:
  Mutagen assigns FormIDs and masters automatically.
- `editorId` must be **non-empty and unique** across the whole spec (`validate` enforces).
- `esl: true` (default) flags the plugin as a light master — new records must fit FormIDs
  **0x800–0xFFF, i.e. ≤ 2048** total. Exceeding it is a hard error at write time (with a clear
  message); set `esl: false` or split the content across plugins if you need more.

### References to vanilla / external forms
Some fields are **refs**: they accept *either* an in-spec `editorId` *or* an external
form in another plugin, written **`"<master>:0xFORMID"`** (e.g. `"Skyrim.esm:0x013746"`
= `NordRace`). External refs let your content point at vanilla races, classes, outfits,
keywords, factions, etc. The named master is **added to the plugin automatically** on build.

- **Discover FormIDs** with the `find` command:
  `find "<Skyrim Data>/Skyrim.esm" <query> [type]` → prints `Skyrim.esm:0xFORMID  Type  EditorID`.
  `[type]` (e.g. `Race`, `Class`, `Outfit`, `Keyword`, `Faction`, `Weapon`, `Npc`) narrows the
  search to one record kind. (Search/display is by **EditorID**; localized display *names* are
  BSA-packed and not resolved headless — EditorIDs like `NordRace` are descriptive enough.)
- `validate` checks ref fields: an in-spec ref must exist; an external ref must be well-formed.

## Top-level shape

```jsonc
{
  "pluginName": "MyMod.esp",   // output filename / ModKey
  "esl": true,                  // light-master flag (default true); ≤2048 new records

  "miscItems": [...], "books": [...], "weapons": [...], "npcs": [...],
  "quests": [...], "dialogue": [...], "banter": [...], "spells": [...], "potions": [...],
  "armors": [...], "factions": [...], "messages": [...],
  "scripts": [...],             // Papyrus attachments (see below)
  "cells": [...], "placements": [...],  // new interior cells + placing forms in them
  "leveledItems": [...], "leveledNpcs": [...], "containers": [...],
  "ingredients": [...], "ammunitions": [...], "scrolls": [...], "soulGems": [...],
  "keys": [...], "keywords": [...], "outfits": [...], "statics": [...], "activators": [...],
  "textureSets": [...],          // TXST — retexture an existing mesh without a new .nif
  "furniture": [...], "sounds": [...],  // custom-mesh furniture + Sound Descriptors (external assets)
  "assets": "path/to/asset/dir",        // source dir whose Meshes/Textures/Sounds `package` bundles
  "packages": [...],             // AI Packages — what an NPC DOES (sandbox/travel/use furniture)
  "weathers": [...], "climates": [...],  // custom skies (WTHR) + weather cycles (CLMT)
  "encounterZones": [...]        // ECZN — level scaling / respawn for an area (a cell/spawn points at one)
}
```

## Record types

| section | fields |
|---------|--------|
| `miscItems` | `editorId`, `name`, `value` (int≥0), `weight` (number), `keywords` (array of *refs*), `template` (vanilla MISC ref to clone a model from), `model` (custom `.nif` path — overrides `template`'s mesh), `pickUpSound`/`putDownSound` (SNDR *refs*) — see [external_assets.md](external_assets.md) |
| `books` | `editorId`, `name`, `text` (book body), `template` (*ref* → a vanilla BOOK to clone a model from — a takeable/readable book NEEDS one or it CRASHES on 3D-read), `value` (int; 0 ⇒ keep template's), `weight` (number; 0 ⇒ keep template's), `flags` (array of `Book.Flag` names, e.g. `CantBeTaken`), `teaches` (optional — a *teaching* book; see below) |
| `books[].teaches` | `{ "kind": "spell", "spell": <ref> }` — a **spell tome** that grants a SPEL on first read (`spell` is an in-spec spell editorId OR a vanilla `<master>:0xFORMID`); OR `{ "kind": "skill", "skill": <name> }` — a **skill book** that raises a `Skill` (e.g. `Destruction`, `OneHanded`, `Smithing`) on first read; OR omit ⇒ a plain book (teaches nothing). A teaching book must have a `template`. |
| `weapons` | `editorId`, `name`, `value`, `weight`, `damage` (int≥0), `speed` (number), `reach` (number), `keywords` (array of *refs*), `enchantment` (*ref* → ENCH, in-spec or vanilla `<master>:0xFORMID`), `enchantmentAmount` (int — the weapon's charge pool, e.g. 1500–3000; 0 = engine auto-calc), `template` (vanilla WEAP ref — clones model/anim/equip; needed to avoid an equip CRASH), `model` (custom world-mesh `.nif` path — pair WITH `template`), `pickUpSound`/`putDownSound` (SNDR *refs*) |
| `npcs` | `editorId`, `name`, `factions` (array of *refs*), `race` (*ref*), `class` (*ref*), `outfit` (*ref* → DefaultOutfit), `level` (int), `autoCalcStats` (bool — derive H/M/S + skills from level + class), `packages` (array of *refs* → PACK; the NPC's AI package list, evaluated in order), `voiceType` (*ref* → VTYP), `crimeFaction` (*ref* → FACT; city-citizen identity, required for cross-cell Travel), `unique` (bool — one-off actor, helps engine AI tracking), `combatStyle` (*ref* → CSTY; HOW the AI fights), `spells` (array of *refs* → SPEL; the AI's spell list), `perks` (array of *refs* → PERK; granted to the actor as passive ability/entry-point perks at game start), `greeting` (string — the Hello line; when this NPC has custom `dialogue`, a Hello info is auto-emitted so it's conversable. Empty ⇒ a default line) |
| `quests` | `editorId`, `name`, `startGameEnabled` (bool, default true), `priority` (0–255), `objectives` (array of `{ index (int), text, showStage?, completeStage? }`), `stages` (array of `{ index (int), logEntry?, completeQuest?, failQuest?, conditions? }`) — see *Quest stages* below |
| `dialogue` | `editorId`, `questEditorId`, `speakerNpcEditorId` (optional), `prompt`, `responses` (array of strings), `emotion` (optional — `Neutral`\|`Anger`\|`Disgust`\|`Fear`\|`Sad`\|`Happy`\|`Surprise`), `emotionValue` (0–100). `setStage` (int — advance the quest to this stage when the line is picked; `package` auto-compiles + VMAD-attaches the TIF fragment and auto-adds a `GetStage < N` condition so the line won't repeat). Optional **custom result fragment** (overrides the auto TIF): `resultScript` (Scriptname, `Extends TopicInfo`, `Fragment_0`), `resultScriptSource` (`.psc`), `resultProperties` (bound props), `goodbye` (bool — close menu after). Build wires the full chain (Quest→DialogView→Branch→Topic→INFO + a Hello) — see the dialogue section below |
| `banter` | `editorId` (optional), `questEditorId`, `speakerNpcEditorId`, `responses` (array of strings — one unprompted comment), `emotion`/`emotionValue`, `conditions` (situational CTDA gates). Proactive (NPC-initiated) lines; entries sharing a (speaker, quest) merge into one ambient Misc/`IDLE` topic with Random INFOs. Needs the speaker to have idle chatter enabled (a Sandbox/follow package). See the *banter* section below |
| `scenes` | `editorId`, `questEditorId` (host quest), `actors` (array of `{ aliasId (int), npc (*ref*), name }`), `phases` (ordered array of `{ speaker (an aliasId), lines (array of strings), emotion, emotionValue }`), `beginOnQuestStart` (bool, default true), `stopQuestOnEnd` (bool). A **SCEN** — two NPCs talking to EACH OTHER. Build emits the host quest's `UniqueActor`-bound aliases, the Scene (actors + phases + Dialog actions), and one Scene/`SCEN` topic+INFO per phase line. See the *scenes* section below |
| `spells` | `editorId`, `name`, `effects` (array of *effects*), `spellType`, `castType`, `targetType`, `baseCost` (int), `chargeTime` (number), `equipType` (EQUP *ref*). **Castable types (Spell/Voice/Power/LesserPower) auto-default to EitherHand `Skyrim.esm:0x00013F44` when omitted** — a Voice/shout spell with no EQUP is learned but **can't be shouted**; set only to override |
| `magicEffects` | `editorId`, `name`, `description`, `archetype`, `actorValue`, `magicSkill`, `resistValue`, `castType`, `targetType`, `baseCost` (number), `flags` (array), `association` (*ref*), `projectile`/`castingArt`/`hitEffectArt`/`explosion` (*refs* — the visible bolt + cast/impact FX; an Aimed spell/shout needs a `projectile` or it fires invisibly/silently), `sounds` (array of `{ type (default `Release`), sound (SNDR *ref*) }` — `Release` is the cast-out/effect sound; a shout's spoken-word *voice* is a recorded voice asset, not settable here) — a custom MGEF an `effect` can point at |
| `enchantments` | `editorId`, `name`, `enchantType` (`weapon`\|`apparel`\|`staff`), `castType`/`targetType` (optional overrides), `enchantmentCost` (int — per-cast charge cost / worn cost), `chargeTime` (number — staff charge-up), `effects` (array of *effects*) — an Object Effect (ENCH) a weapon/armor `enchantment` field points at |
| `potions` | `editorId`, `name`, `value`, `weight`, `effects` (array of *effects*) |
| `armors` | `editorId`, `name`, `value`, `weight`, `armorRating` (number), `armorType` (`light`\|`heavy`\|`clothing`), `slots` (array of biped-slot names), `keywords` (array of *refs*), `enchantment` (*ref* → ENCH, normally an `apparel` constant-effect one), `template` (vanilla ARMO *ref* — clones its **Armature** (worn mesh) + WorldModel; **required or the armor equips INVISIBLE**, e.g. `Skyrim.esm:0x00012E49` ArmorIronCuirass), `model` (custom ground-mesh `.nif` path — pair WITH `template`) |
| `factions` | `editorId`, `name`, `vendor` (optional sub-object — turns this into a MERCHANT faction; see *vendors / merchants* below) |
| `classes` | `editorId`, `name`, `description`, `teaches` (Skill), `maxTrainingLevel`, `healthWeight`/`magickaWeight`/`staminaWeight` (attribute distribution), `skillWeights` (`{ Skill: 0–255 }`) — an npc `class` can point at one |
| `messages` | `editorId`, `name`, `description` (body text) |
| `cells` | `editorId`, `name`, `template` (vanilla interior cell `<master>:0xFORMID` to copy lighting from — else the new cell is black), `encounterZone` (*ref* → ECZN — level scaling/respawn for the whole cell) |
| `placements` | `base` (*ref* — a concrete NPC_ actor or object form; **never a raw LeveledNpc list (LVLN)** — LVLN as ACHR base CTDs at load, see the encounter-zone section); **interior:** `cell` (in-spec editorId **or** vanilla interior cell `<master>:0xFORMID`) **or exterior:** `worldspace` (`<master>:0xFORMID`, position is world coords); `kind` (`npc`\|`object`), `position` (`{x,y,z}`), `rotation` (`{x,y,z}` degrees), `persistent` (bool), `encounterZone` (*ref* → ECZN — per-ref override of the cell's zone) |
| `leveledItems` | `editorId`, `chanceNone` (0–100), `flags` (array), `entries` (array of `{ reference (*ref*), level (int), count (int) }`) |
| `leveledNpcs` | same shape as `leveledItems`, but `reference` is an npc/leveled-npc |
| `containers` | `editorId`, `name`, `weight`, `items` (array of `{ item (*ref*), count (int) }`) |
| `ingredients` | `editorId`, `name`, `value`, `weight`, `effects` (array of *effects*), `keywords` (array of *refs*) |
| `ammunitions` | `editorId`, `name`, `value`, `weight`, `damage` (number), `keywords` (array of *refs*) |
| `scrolls` | `editorId`, `name`, `value`, `weight`, `effects` (array of *effects*), `spellType`, `castType`, `targetType`, `baseCost` (int), `keywords` (array of *refs*) |
| `soulGems` | `editorId`, `name`, `value`, `weight`, `maximumCapacity` (`None`\|`Petty`\|`Lesser`\|`Common`\|`Greater`\|`Grand`), `keywords` (array of *refs*) |
| `keys` | `editorId`, `name`, `value`, `weight`, `keywords` (array of *refs*) |
| `keywords` | `editorId` (define your own keyword so in-spec records can list it in `keywords`) |
| `outfits` | `editorId`, `items` (array of *refs* → armors/weapons; an npc `outfit` can point at this editorId) |
| `statics` | `editorId`, `model` (a `.nif` path — vanilla OR custom mesh; a placement base, no name), `alternateTextures` (array — swap the mesh's textures to a TXST; see *textureSets* below) |
| `activators` | `editorId`, `name`, `model` (`.nif` path), `keywords` (array of *refs*), `alternateTextures` (array — same as `statics`), `activationSound`/`loopingSound` (SNDR *refs*); attach behaviour via `scripts` |
| `furniture` | `editorId`, `name`, `model` (`.nif` path — vanilla OR custom mesh), `keywords` (array of *refs*) — a placeable interactive object (chair/bed/bench/idle marker); place it with a `placement` |
| `sounds` | `editorId`, `files` (array of Data-relative `Sound\...` `.wav`/`.xwm` paths), `category` (SNCT *ref*, default AudioCategorySFX), `outputModel` (SOPM *ref*, default vanilla SFX), `priority` (0–255), `staticAttenuation` (dB) — a Sound Descriptor (SNDR) a record's sound field points at. See [external_assets.md](external_assets.md) |
| `recipes` | `editorId`, `kind` (`craft`/`temper`/`smelt`/`breakdown`), `createdObject` (*ref*), `count` (int), `workbench` (named selector `forge`/`sharpeningWheel`/`armorTable`/`smelter`/`tanningRack`/`skyforge` OR a keyword *ref*; defaults by kind), `components` (array of `{ item (*ref*), count (int) }`), `conditions` (array of shared CTDA `{ function, param (*ref*), comparison, value, or }` — perk/item/skill gating, e.g. `HasPerk`/`TemperIsEnchanted`) — a crafting/tempering/smelting recipe (COBJ) |
| `packages` | `editorId`, `template` (*ref* → a vanilla procedure template, e.g. Sandbox = `Skyrim.esm:0x01C254`, Travel = `Skyrim.esm:0x016FAA`, UseMagic = `Skyrim.esm:0x0504F5`), `flags` (array — `Package.Flag` names), `interruptFlags` (array — `HellosToPlayer`/`AllowIdleChatter`/`WorldInteractions`/…), `preferredSpeed` (`Walk`\|`Jog`\|`Run`\|`FastWalk`), `combatStyle` (*ref*, optional), `ownerQuest` (*ref*, optional), `schedule` (`{ month, dayOfWeek, date, hour, minute, durationInMinutes }` — a time window `[hour, hour+durationInMinutes)`; `hour:-1` = any time), `sandbox` / `sleep` / `travel` / `useMagic` / `patrol` / `follow` / `escort` (template-input subobjects — see below), `conditions` (array of CTDA gates — see *conditions* below). An NPC's `packages` list is in **priority order**: the engine runs the first package whose schedule **and** conditions pass — so put scheduled/conditioned packages first and an unconditioned fallback last (e.g. a Sleep package scheduled 22:00–07:00 above an unconditioned Sandbox; or a Follow package gated on `GetInFaction CurrentFollowerFaction==1` above a downtime Sandbox). Assign to one or more NPCs via `npcs[].packages`. |
| `combatStyles` | `editorId`, `offensiveMult`/`defensiveMult`/`groupOffensiveMult` (~aggression/blocking/group-boldness), `equipMultMelee`/`equipMultMagic`/`equipMultRanged`/`equipMultShout`/`equipMultUnarmed`/`equipMultStaff` (AI weapon-preference scores; for a mage NPC, push Magic high relative to the others — vanilla `csVampireMagic` uses 8.1/2.15/0.51), `avoidThreatChance` (0..1), `flags` (array — `Dueling`\|`Flanking`\|`AllowDualWielding`). An npc's `combatStyle` ref can point at one. |
| `encounterZones` | `editorId`, `minLevel` (0–255), `maxLevel` (0–255; **0 = uncapped**, scales with the player), `rank` (int, owner rank), `owner` (*ref* → FACT/NPC, optional), `location` (*ref* → LCTN, optional), `flags` (array — `NeverResets`\|`MatchPcBelowMinimumLevel`\|`DisableCombatBoundary`). A cell's / placed spawn's `encounterZone` points at one. |
| `perks` | `editorId`, `name`, `description`, `playable`/`hidden`/`trait` (bool trunk flags), `level` (int), `numRanks` (int, ≥1), `nextPerk` (*ref*, optional rank chain), `conditions` (array — perk-level CTDA gates), `effects` (array — `ability` or `entryPoint`; see *perks (PERK)* below). A passive ability / stat-or-combat modifier. Attach to an NPC via `npcs[].perks`. |
| `wordsOfPower` | `editorId`, `name` (dragon-script glyph shown in the shout menu), `translation` (English gloss) — one Word of Power (WOOP). Referenced by a shout's `words[].word` and a word wall's `word`. |
| `shouts` | `editorId`, `name`, `description`, `menuDisplayObject` (*ref* → STAT, optional), `words` (array of up to 3 `{ word (*ref* → WOOP), spell (*ref* → SPEL, a Voice-type spell), recoveryTime (seconds) }`). A dragon shout (SHOU): word 1 = tap, words 1+2 = hold, 1+2+3 = full charge — each word's `spell` is the progressively stronger Voice fired at that level. Record-correct but **unusable until its words are learned** — see `wordWalls`. |
| `wordWalls` | `editorId` (the teaching quest's editorId), `name`, `shout` (*ref* → SHOU), `wordIndex` (1\|2\|3 — which word to teach), `word` (*ref* → WOOP, optional; auto-derived from an in-spec shout's `wordIndex`, **required** for a vanilla shout), `scriptName` (generated fragment name; defaults `<editorId>Script`), `triggerEditorId`/`triggerBase` (the placed REFR id / ACTI base — defaults to vanilla `WordWallTrigger` `Skyrim.esm:0x05095E`), and a trigger location like a placement (`cell` **or** `worldspace` + `position`/`rotation`). The learnable layer: emits a start-enabled teaching QUEST + a GENERATED Papyrus fragment (`Game.GetPlayer().AddShout` + `TeachWord`) attached via VMAD with `WordWallShout`/`WordWallWord` object properties, plus the WordWallTrigger placement. See the cookbook recipe — the `.psc` must be CK-compiled; in-game learning is **UNCONFIRMED**. |
| `textureSets` | `editorId`, eight optional `.dds` slot paths — `diffuse`, `normal`, `mask`, `glow`, `height`, `environment`, `multilayer`, `backlight` — each **relative to `Data\Textures\`** (omit the leading `Textures\`), `flags` (array — `NoSpecularMap`\|`FaceGenTextures`\|`HasModelSpaceNormalMap`). A TXST retextures an existing mesh; wire it via a record's `alternateTextures`. See the *textureSets* section below. |
| `weathers` | `editorId`, `flags` (array — `Pleasant`\|`Cloudy`\|`Rainy`\|`Snow`\|`SkyStaticsAlwaysVisible`\|`SkyStaticsFollowsSunPosition`), per-time-of-day *colours* (`skyUpperColor`/`skyLowerColor`/`fogNearColor`/`fogFarColor`/`horizonColor`/`cloudColor`/`sunColor`/`sunlightColor`/`ambientColor`/`starsColor`), `clouds` (array of `{ index (0–31), texture, xSpeed, ySpeed, colors, alphaSunrise/Day/Sunset/Night }`), `precipitation` (*ref* → SPGD), `windSpeed` (0–1 or 0–100), `windDirection`/`windDirectionRange` (degrees), `fogDayNear`/`fogDayFar`/`fogNightNear`/`fogNightFar` (world units), `transitionDelta`. A custom sky — see the section below |
| `climates` | `editorId`, `weathers` (array of `{ weather (*ref* → WTHR), chance (int weight) }`), `sunriseBegin`/`sunriseEnd`/`sunsetBegin`/`sunsetEnd` (`"HH:MM"` 24h), `sunTexture`/`sunGlareTexture` (Textures-relative paths), `moons` (array — `Masser`\|`Secunda`), `phaseLength` (int), `volatility` (0–255). A weather cycle — see the section below |

A field marked *ref* takes an in-spec `editorId` **or** `"<master>:0xFORMID"` (see
*References to vanilla / external forms* above). A standing NPC needs at least `race` +
`class` to behave as a real actor in-game; `outfit` gives it clothing/gear.

### Gameplay stats
- **Weapons:** give a `damage` (and usually `value`/`weight`). `speed` and `reach`
  default to `1.0` when any stat is set, so the weapon is swingable; override for slower/
  faster or longer/shorter weapons. A weapon with no stats is an inert item (it’ll equip
  but do nothing useful).
- **Armor:** `armorType` is `light` / `heavy` / `clothing` (default `clothing`); `slots`
  lists the biped slots it occupies by `BipedObjectFlag` name — `Body`, `Head`, `Hands`,
  `Feet`, `Forearms`, `Calves`, `Shield`, `Amulet`, `Ring`, `Circlet`, … (multiple slots
  are OR’d). `armorRating` is the protection value.

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
  "archetype": "ValueModifier",   // ValueModifier (damage/heal/fortify) | SummonCreature | Bound | Light | Paralysis | …
  "actorValue": "Health",          // what it acts on: Health | Magicka | Stamina | …
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
[`examples/enchantment_spec.json`](../examples/enchantment_spec.json). *(Structurally verified; the
enchantment actually firing in-game is unconfirmed — see the cookbook recipe note.)*

### classes (CLAS)
An NPC's "profession" — set an npc's `class` ref to one. It drives the actor's attribute
distribution and favoured skills (and, for a trainer NPC, what it `teaches`).
```jsonc
{ "editorId": "MF_Battlemage", "name": "ModForge Battlemage",
  "teaches": "Destruction",        // a Skill the class can train (trainers); optional
  "maxTrainingLevel": 50,
  "healthWeight": 30, "magickaWeight": 50, "staminaWeight": 20,   // attribute split (~sum 100)
  "skillWeights": { "Destruction": 100, "Restoration": 75, "OneHanded": 50 } }  // Skill -> 0–255 favour
```
Skill names: `OneHanded`, `TwoHanded`, `Archery`, `Block`, `Smithing`, `HeavyArmor`, `LightArmor`,
`Pickpocket`, `Lockpicking`, `Sneak`, `Alchemy`, `Speech`, `Alteration`, `Conjuration`,
`Destruction`, `Illusion`, `Restoration`, `Enchanting`. A class only drives an NPC's actual
attribute/skill values when that npc has **`level` > 0 and `autoCalcStats: true`** — otherwise the
engine uses flat defaults (a bare NPC reads 50/50/50 regardless of class). To see it: spawn a
magicka-heavy and a health-heavy NPC (both `autoCalcStats` at the same level) and compare
`getav magicka`/`getav health`.

### dialogue
A `dialogue` entry is a player topic shown under a quest's branch, optionally limited
to one speaker NPC (a `GetIsID` condition). `questEditorId` must name a quest in this
spec; `speakerNpcEditorId`, if set, must name an npc. `prompt` is the player's line;
`responses` are the NPC's spoken lines.

From one `dialogue` entry the build emits the **whole vanilla chain** so the topic
actually surfaces in-game (confirmed It.23, SSE 1.6.1170):
- the **Topic** (`Custom`, `SNAM='CUST'` — a null subtype crashes on load) + **Branch**
  (`TopLevel`, Player) + an **INFO** carrying the responses. Each INFO gets `ENAM`
  (flags) + `CNAM` (favor level) — **an INFO without `ENAM` is treated as invalid and
  its topic is silently dropped from the menu**;
- a **DialogView (DLVW)** per quest tying its branches to the quest (without it the
  quest's player dialogue is never served);
- a **Hello** info (`Misc`/`Hello`/`SNAM='HELO'`) per speaking NPC so the NPC is
  *conversable* at all — set the line with `npc.greeting`.

**Result fragment (do something when the line is picked).** A dialogue choice can only
*act* (take gold, join the follower system, set a stage) through a Papyrus fragment — JSON
holds static data, never control flow. Set `resultScript` (the fragment's Scriptname, which
must `Extends TopicInfo` and define `Function Fragment_0(ObjectReference akSpeakerRef)`),
`resultScriptSource` (the `.psc`, compiled by `package`), and `resultProperties` (bind its
`Auto` properties — same shape as a `scripts[]` entry's properties: `int`/`float`/`bool`/
`string`/`object`). The build attaches the INFO's `OnBegin` fragment VMAD (fires when the player
selects the line; use `OnEnd` only for effects that must follow the full voiced response). Set `goodbye: true`
to close the menu after the line (vanilla recruit/dismiss lines all do). See
`examples/follower_paid_spec.json` + `MFHirePaidRecruit.psc` for a paid-follower recruit.

> **Three runtime requirements (not record bugs):** (1) the dialogue only registers on a
> **game LOAD** — test with a genuine new game, or `save`+`load` after the quest starts;
> a main-menu `coc` or mid-session `startquest` leaves the NPC mute even with a perfect
> plugin. (2) Place the speaker at a real in-room coordinate — a no-package NPC at cell
> origin **(0,0,0)** lands off-navmesh and can't be reached. (3) Unvoiced lines flash past;
> install **Fuz Ro D-oh** (or bundle silent `.fuz`) and enable subtitles. See `lifelike/gotchas.md`.

### banter — proactive (unprompted) NPC lines
A `banter` entry is a line the NPC says **on its own**, with no player menu — the vanilla
follower-comment pattern (`HirelingIdles`). Shape: `editorId` (optional), `questEditorId`,
`speakerNpcEditorId`, `responses` (the spoken line(s) — one comment), `emotion`/`emotionValue`,
`conditions` (situational gates). All banter entries sharing a (speaker, quest) collapse into
**one ambient topic** — Category=Misc, SNAM=`IDLE`, no branch — with one **Random**-flagged INFO
per entry; the engine random-picks one whose `conditions` currently pass and plays it. **Trigger
requirement:** the speaker must have **idle chatter enabled** — an AI package carrying the
`AllowIdleChatter` interrupt flag (a `Sandbox` package, or the vanilla follow package). Make it
situational with `conditions` (e.g. `GetCurrentTime` for night, `IsInInterior`, `GetActorValuePercent`
for "I'm hurt", and `GetInFaction CurrentFollowerFaction==1` for follower-only). This is the
*unprompted* counterpart to a `dialogue` line the player asks for. NOTE: ambient/idle only — true
combat shouts use a different subtype (Taunt/Attack), not yet supported. See `examples/follower_vanilla_spec.json`.

### scenes — two NPCs talking to EACH OTHER (SCEN)
A `scene` is a scripted conversation between NPCs (not the player) — the vanilla **Scene** record.
A scene is **hosted by a quest**, its participants are that quest's **aliases** (not direct NPC refs),
and it plays an ordered list of **phases**, one spoken line per phase.
```jsonc
{ "editorId": "MF_TavernArgument",
  "questEditorId": "MF_SceneQuest",     // a StartGameEnabled quest in this spec (the scene runs while it does)
  "beginOnQuestStart": true,            // play the moment the host quest starts (= on game load); default true
  "stopQuestOnEnd": false,              // stop the host quest when the scene finishes (vanilla one-shots set true)
  "actors": [                            // each actor = an alias INDEX + the NPC that fills it
    { "aliasId": 0, "npc": "MF_Borin", "name": "Borin" },
    { "aliasId": 1, "npc": "MF_Hilda", "name": "Hilda" } ],
  "phases": [                            // played in order; `speaker` is one of the actors' aliasId
    { "speaker": 0, "emotion": "Anger",   "lines": [ "You still owe me for the ale, Hilda." ] },
    { "speaker": 1, "emotion": "Disgust", "lines": [ "Owe you? That swill wasn't worth a clipped septim." ] },
    { "speaker": 0, "emotion": "Anger",   "lines": [ "Watch your tongue, or there'll be trouble." ] },
    { "speaker": 1, "emotion": "Happy",   "lines": [ "Ha! Buy me a drink and we're even." ] } ] }
```
From this one entry the build emits the **whole vanilla chain** (mirrors `scenediag` on
`dunIronbindBeemJaMourningScene`):
- one **QuestAlias** per actor on the host quest, each `UniqueActor`-bound to the named NPC (so the
  alias fills with that specific actor);
- the **Scene (SCEN)**: its `SceneActors` reference the **alias indices** (not NPC FormKeys); its
  `Phases` are the ordered beats; one **Dialog `SceneAction`** per phase ties (speaking alias, phase)
  → the line's topic, with the *other* actor as the headtrack target so they face each other;
- one **Scene-subtype DialogTopic** (Category=Scene, SNAM=`SCEN`) + **INFO** per phase, carrying the
  spoken `lines` + `emotion`.

> **Runtime requirements (not record bugs):** (1) the two NPCs must be **placed near each other** —
> add a `placements[]` entry per NPC into the **same cell** (they have to be co-located to converse).
> (2) Like all quest dialogue, a scene only loads on a **game LOAD** — test a new game, or `save`+`load`
> after the host quest starts (the build auto-writes the `.seq` entry). (3) Unvoiced lines flash past;
> install **Fuz Ro D-oh** and enable subtitles. **Status: structural only** — `build`/`validate`/`dump`
> verified against the vanilla scene shape; **not yet in-game confirmed.** See `examples/scene_spec.json`
> and `lifelike/cookbook.md`.

### conditions — CTDA gates (on a `dialogue` INFO, a `banter` INFO, or a `package`)
A condition is **static gate data**, so it lives in the spec (logic still belongs in Papyrus). Both
`dialogue[].conditions` and `packages[].conditions` take the same shape:
```jsonc
{ "function": "GetItemCount",          // form-arg: HasPerk | GetInFaction | GetItemCount | GetGlobalValue | GetStage | GetIsID | GetRelationshipRank
  //                                    // actorValue-arg: GetActorValue | GetActorValuePercent (0..1 fraction)
  //                                    // no-arg situational: GetCurrentTime (hour 0..24) | IsInInterior | IsInCombat | GetRandomPercent (0..99) | TemperIsEnchanted (recipe temper guard)
  "comparison": ">=",                  // == != > >= < <=
  "value": 500,
  "param": "Skyrim.esm:0x00000F",      // the function's form arg (faction/item/global/quest/npc) as a ref
  "actorValue": "",                    // for GetActorValue/GetActorValuePercent instead of param — e.g. "Health", "WaitingForPlayer"
  "runOn": "Reference",                // whose value: Subject (default) | Reference | Target | CombatTarget | ...
  "reference": "Skyrim.esm:0x000014",  // the ref read when runOn=Reference (here, the player)
  "or": false }                        // OR with the NEXT condition (default AND)
```
A `dialogue` INFO already carries an auto `GetIsID` speaker gate; these are appended. Typical follower
uses: hide a paid recruit line unless `GetItemCount Gold >= 500` (on the player) **and**
`GetInFaction CurrentFollowerFaction == 0`; gate a Follow package on `GetInFaction
CurrentFollowerFaction == 1` so it only runs after recruitment. See `examples/follower_paid_spec.json`.

### Quest stages, log entries & objective wiring
A quest's `stages[]` are integer milestones the quest can be **set to** (10, 20, 30…). Each stage
optionally writes a **journal log entry** and can carry a quest-state flag. Objectives display and
complete as stages are set; a `dialogue` line can advance a stage when picked.

```jsonc
"quests": [{
  "editorId": "MF_ErrandQuest", "name": "A Forged Errand",
  "startGameEnabled": true, "priority": 60,
  "stages": [
    { "index": 10, "logEntry": "Joren asked me to retrieve his lost hammer." },
    { "index": 20, "logEntry": "I agreed to help. Time to search the riverbank.",
      "conditions": [ { "function": "GetStage", "comparison": "GreaterThanOrEqualTo",
                        "value": 10, "param": "MF_ErrandQuest" } ] },   // optional CTDA gate on the log entry
    { "index": 30, "logEntry": "I returned the hammer. Done.", "completeQuest": true }   // closes the quest
  ],
  "objectives": [
    { "index": 10, "text": "Agree to help Joren", "showStage": 10, "completeStage": 20 },
    { "index": 20, "text": "Find Joren's hammer",  "showStage": 20, "completeStage": 30 }
  ]
}]
```
- **`stages[]`** — `index` (unique, **ascending**), `logEntry` (journal text; omit for a silent
  milestone), `completeQuest` / `failQuest` (set the QuestLogEntry flag that closes / fails the quest
  when this stage is reached — at most one), `conditions` (optional CTDA gate on the log entry, built
  with the shared **ConditionSpec**: `function` (a `GetStage`/`GetIsID`/… name), `comparison`
  (`==`/`>=`/… or `EqualTo`/`GreaterThanOrEqualTo`/…, default `>=`), `value`, `param` (ref → the
  function's form parameter, e.g. the quest for `GetStage`)).
- **`objectives[].showStage` / `.completeStage`** — link an objective to stages: it's
  `SetObjectiveDisplayed` at `showStage` and `SetObjectiveCompleted` at `completeStage`. `-1` (the
  default) means "not stage-linked".
- **`dialogue[].setStage`** — picking that topic advances the host quest to this stage.

**What's record-only vs. what needs Papyrus:** stages, log entries, the `completeQuest`/`failQuest`
flags and log-entry conditions are **pure record data** — they build, `dump`/`questdiag` cleanly,
and the engine reads them directly. But *displaying* an objective on stage-set and *advancing* a
stage from a dialogue line require **Papyrus fragments**. The `package` command handles this
end-to-end (**no CK needed, IN-GAME CONFIRMED It.36 2026-06-02**):

1. Generates `Scripts/Source/<quest>_Stages.psc` — one `Fragment_Stage_XXXX_Item00000()` function
   per stage that shows/completes objectives (CK-standard naming; engine calls it when `SetStage()` fires).
2. Generates `Scripts/Source/TIF_<dialogue>.psc` — `extends TopicInfo Hidden`, with an explicit
   `Quest Property OwningQuest Auto` bound to the quest FormKey; `Fragment_0` calls
   `OwningQuest.SetStage(N)`. Uses `OnBegin` (fires when the player selects the line).
   **Do not use `GetOwningQuest()` — it returns None for StartGameEnabled quests on game-load.**
3. Compiles both `.psc` → `.pex` with the Linux-native `papyrus-compiler` (falls back to Wine/CK).
4. Attaches the VMAD to the QUST (`QuestScriptFragment.Unknown2=1` required — the enable flag; 0
   skips the fragment even when `SetStage()` fires) and to the INFO (`DialogResponsesAdapter`, `OnBegin`).
5. Auto-adds a `GetStage(quest) < setStage` condition on every `setStage` dialogue line so the NPC
   won't repeat it after the player has already picked it.

Inspect any quest with `questdiag <plugin> <0xFORMID>`. Dialogue still only registers on a game
**LOAD** (see the gotcha above). Worked example: `examples/quest_stages_spec.json`.

### scripts — Papyrus attachment
```jsonc
{
  "targetEditorId": "MF_Q1",          // record to attach to (any editorId in the spec)
  "scriptName": "MFDemoQuestScript",  // must match the .pex/.psc Scriptname
  "source": "scripts/MFDemoQuestScript.psc",  // optional: .psc path (rel. to this spec);
                                              //  `package` compiles it via Wine
  "properties": [
    { "name": "GreetingCount", "type": "int",    "int": 3 },
    { "name": "PlayerRef",     "type": "object", "objectEditorId": "MF_Smith" }
  ]
}
```
- Property `type` ∈ `int | float | bool | string | object`. Set the matching value
  field: `int` / `float` / `bool` / `str`, or `objectEditorId` (for `object`, resolved
  to a FormLink). Properties are flagged *Edited* so the game reads them.
- Attaching works on any record that supports scripts (Quest, Npc, Activator,
  MagicEffect, Weapon, Armor, MiscItem, Book, Ingestible, …). The script `Name` must
  match the compiled `.pex`.

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
  - **interior** — `cell` is a new in-spec interior cell’s `editorId`, **or** an external/vanilla
    interior cell `"<master>:0xFORMID"` (find with `find <Skyrim.esm> <name> Cell`). A new cell
    with no `template` renders **pitch-black** and has **no floor** (you fall into the void): set
    the cell’s `template` to a vanilla interior (copies its lighting) and place a floor static in
    it. `position` is local to the cell.
  - **exterior** — `worldspace` is a worldspace ref `"<master>:0xFORMID"` (Tamriel =
    `Skyrim.esm:0x00003C`; find with `find <Skyrim.esm> <name> Worldspace`). `position` is the
    **world** position; the exterior cell at `floor(x/4096), floor(y/4096)` is found in the master
    and overridden to add your ref. If that grid has no master cell, a new exterior cell is made
    there (structural only — not in-game verified). `worldspace` wins if both it and `cell` are set.
- `base` is a *ref* (in-spec or external); NPCs become `PlacedNpc`, anything else `PlacedObject`
  (`kind` overrides the guess). `rotation` is **degrees**. `persistent: true` puts it in the
  cell’s persistent list (needed if a quest/script references it).
- **Vanilla placement** (interior cell or exterior worldspace) overrides the cell/worldspace to
  *add* your reference (vanilla contents are untouched — they come from the master). Needs the
  game’s `Data` folder — set `MODFORGE_SKYRIM_DATA` if it isn’t at the default Steam path.

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
             "cameraInitialPitch": 50, "cameraMinHeight": 50000, "cameraMaxHeight": 80000 } }
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
- **regions** (REGN): an area inside a `worldspace` (an in-spec WRLD `editorId` or a vanilla
  `"<master>:0xFORMID"`). `area` is a polygon of **>=3** world-space points (not cell grid).
  `weather` is the table that picks the active weather — each entry a WTHR *ref* + a relative
  `chance` (the chances must sum > 0); `weatherPriority` orders overlapping regions. `mapColor` is
  `0xRRGGBB`. Other RegionData kinds (sound/objects/grass/land) are CK-side and not emitted.
- WARNING **RECORD LAYER ONLY — not a playable world.** ModForge emits the WRLD/REGN records and
  wires their links, but a real walkable exterior also needs **terrain (LAND heightmap), LOD meshes,
  and navmesh**, all authored in the **Creation Kit** — ModForge does not generate them. A new
  worldspace here is the hook to **attach a custom Climate/Weather** and to **define weather / spawn
  regions**; pair `climate` (or a region's `weather`) with a generated/chosen CLMT/WTHR. This
  feature is **structurally verified** (build/dump/round-trip) but **not in-game confirmed**.
- Discover vanilla values with `find <Skyrim.esm> <name> Worldspace`, then
  `worlddiag <Skyrim.esm> <0xFORMID>` (climate/water/parent + map bounds + land/water defaults) and
  `regndiag <Skyrim.esm> <0xFORMID>` (worldspace/area/mapColor + weather table). Example:
  `examples/worldspace_spec.json`.

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
  `entry`’s `reference` is a *ref* (an in-spec item/npc, an external one, or another leveled
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

### recipes (crafting / COBJ)
Make an item craftable, temperable, or smeltable at a workbench. A recipe's `kind` picks the
flavour (default `craft`) and the **default bench**; `workbench` is a **named selector** (`forge` /
`sharpeningWheel` (=`grindstone`) / `armorTable` (=`workbench`) / `smelter` / `tanningRack` /
`skyforge`) — or a raw `<master>:0xID` keyword ref, which overrides the kind default. Omit
`workbench` to take the kind's default.

```jsonc
{ "editorId": "MF_ForgedBladeRecipe",
  "kind": "craft",                      // craft | temper | smelt | breakdown   (default craft)
  "createdObject": "MF_ForgedBlade",    // a ref — usually an in-spec weapon/armor
  "count": 1,
  "workbench": "forge",                 // named selector OR a keyword ref; OMIT -> kind default
  "components": [                        // consumed on craft (ref + count)
    { "item": "Skyrim.esm:0x05ACE5", "count": 2 },   // SteelIngot
    { "item": "Skyrim.esm:0x0800E4", "count": 1 } ], // LeatherStrips
  "conditions": [                        // perk/item/skill gating (shared CTDA) — optional
    { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] }
```

**`kind` defaults** — `craft` → forge, `temper` → sharpening wheel, `smelt`/`breakdown` → smelter.

**`kind: "temper"`** — IMPROVE an existing weapon/armor at a grindstone (weapons) / armor table
(armor). The `createdObject` IS the item being improved (must be an in-spec weapon/armor or an
external ref); the component is the temper material. Mirror vanilla by adding the enchanted-item
guard `TemperIsEnchanted` (`or: true`) before the smithing `HasPerk`:
```jsonc
{ "editorId": "MF_ForgedBladeTemper", "kind": "temper",
  "createdObject": "MF_ForgedBlade", "workbench": "sharpeningWheel",
  "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 1 } ],
  "conditions": [
    { "function": "TemperIsEnchanted", "comparison": "!=", "value": 1, "or": true },
    { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] }
```

**`kind: "smelt"` / `"breakdown"`** — ore → ingot, or break an item down into materials at the
smelter (`createdObject` = the output ingot, component = the ore/item consumed).

**`conditions`** — each is a shared CTDA (the same `ConditionSpec` as dialogue/package gates — see the *conditions — CTDA gates* section above).
`function` ∈ `HasPerk` | `GetItemCount` | `GetGlobalValue` (each needs a `param` ref) |
`TemperIsEnchanted` (no param). `comparison` is the operator (`==` `!=` `>` `>=` `<` `<=`, default
`>=`), `value` the test value, `or: true` OR-chains with the NEXT condition. Use `find Skyrim.esm
<name> Perk` to discover perk FormIDs; `cobjdiag <esp> <0xID>` prints any recipe's full shape.

Common bench keyword FormIDs (probed from Skyrim.esm): `0x088105` forge, `0x0ADB78` armor table,
`0x088108` sharpening wheel, `0x0A5CCE` smelter, `0x07866A` tanning rack, `0x0F46CE` Skyforge.

### perks (PERK)
A perk is a passive ability or a quantitative stat/combat modifier — the building block of the skill
trees, race abilities, and quest-reward bonuses. The trunk carries `name`/`description`, the
`playable`/`hidden`/`trait` flags, `level` + `numRanks` (≥1), optional player-facing `conditions`
(perk-level CTDA gates), and a list of `effects`. Two effect kinds are supported:

```jsonc
{ "editorId": "MF_IronHidePerk", "name": "Iron Hide", "numRanks": 1,
  "effects": [
    // (a) ABILITY — grant a SPEL. Pair with an in-spec Ability/constant-effect spell + MGEF.
    { "kind": "ability", "spell": "MF_IronHideAbility" } ] }

{ "editorId": "MF_DeadlyStrikesPerk", "name": "Deadly Strikes", "numRanks": 1,
  "conditions": [   // perk-level gate (when the perk applies at all)
    { "function": "GetBaseActorValue", "actorValue": "OneHanded",
      "comparison": "GreaterThanOrEqualTo", "value": 30 } ],
  "effects": [
    // (b) ENTRY-POINT — a quantitative modifier on a named EntryPoint.
    { "kind": "entryPoint",
      "entryPoint": "ModAttackDamage",      // an EntryType name
      "function": "Multiply",               // Set | Add | Multiply
      "value": 1.2,                          // ×1.2 = +20%
      "conditions": [                        // effect-level gate (when the bonus fires)
        { "function": "WornHasKeyword", "param": "Skyrim.esm:0x01E711",  // WeapTypeSword
          "comparison": "EqualTo", "value": 1 } ] } ] }
```

- **`entryPoint`** is one of Skyrim's `EntryType` values — `ModAttackDamage`, `ModSpellMagnitude`,
  `CalculateMyCriticalHitChance`, `ModArmorRating`, `GetMaxCarryWeight`, … Discover the full set with
  `perkdiag <Skyrim.esm> entrypoints`, or dump a vanilla perk to copy a working shape:
  `perkdiag <Skyrim.esm> 0x079343` (Armsman20 = ModAttackDamage ×1.4).
- **`conditions`** (both perk-level and per-effect) use the shared CTDA builder (the same
  `ConditionSpec` as dialogue/package/recipe gates). Perk-relevant functions:
  `GetBaseActorValue`/`GetActorValue` (need `actorValue`), `HasKeyword`/`WornHasKeyword`/`HasPerk`/
  `GetIsID`/`GetIsRace`/`GetItemCount`/`IsSpellTarget` (need a `param` ref), `GetEquippedItemType`
  (`itemType` = `Left`/`Right`/`Voice`/`Instant`), `GetRandomPercent`, `GetLevel`. Each takes a
  `comparison` (`EqualTo`/`GreaterThanOrEqualTo`/… or the symbol forms) vs `value`, an optional
  `runOn` (`Subject` default / `Target`), and `or` (OR with the next condition).
- **Attach to an NPC** via `npcs[].perks: ["MF_IronHidePerk", …]` — the actor gains the perk(s)
  passively at game start (each placement carries the perk's `numRanks`). **Granting a perk to the
  PLAYER needs a Papyrus `AddPerk` call** (`scripts` + a quest fragment) — there is no record-only way
  to put a perk on the player at game start; that's a CK/script route, documented honestly here.
- **In-game caveat:** structurally these emit exactly like vanilla perks (verify with `dump` /
  `perkdiag`), but whether an entry-point modifier actually changes combat numbers, or an ability
  perk's SPEL applies, can only be confirmed by a real Skyrim launch. Worked example:
  `examples/perk_spec.json`.

### external assets — your own meshes / textures / sounds (`model`, `sounds`, `assets`)
Instead of cloning a vanilla record's mesh via `template`, bring your OWN assets. ModForge
**references** them (writes the Data-relative path into the record) and **bundles** them (copies the
files next to the `.esp` on `package`). It does NOT author meshes/sounds — full contract +
path rules in **[external_assets.md](external_assets.md)**.
```jsonc
"assets": "my_assets",          // source dir; package copies its Meshes/Textures/Sound/… into the mod
"sounds": [ { "editorId": "MFChimeSD", "files": [ "Sound\\fx\\mymod\\chime.wav" ] } ],
"statics":    [ { "editorId": "MFStone",  "model": "MyMod\\stone.nif" } ],
"furniture":  [ { "editorId": "MFThrone", "name": "Throne", "model": "MyMod\\throne.nif" } ],
"activators": [ { "editorId": "MFBell", "name": "Bell", "model": "MyMod\\bell.nif",
                  "activationSound": "MFChimeSD" } ]
```
- **`model`** (on statics/activators/furniture/miscItems/weapons) is a Data-relative `.nif` path
  rooted at `Meshes\` — so **omit the `Meshes\` prefix** (write `MyMod\bell.nif`, not
  `Meshes\MyMod\bell.nif`). `validate` enforces this. On a `miscItem`, `model` overrides `template`
  (warns); on a `weapon`, pair `model` WITH a `template` (a model-less/template-less weapon CRASHES
  on equip).
- **`sounds`** emit Sound Descriptors (SNDR). A record points at one by *ref* (in-spec `editorId` or
  vanilla `<master>:0xFORMID`): activator `activationSound`/`loopingSound`, misc/weapon
  `pickUpSound`/`putDownSound`. `category`/`outputModel` default to the vanilla SFX category/output.
- **`assets`** names a source dir laid out like `Data/` (`Meshes/`, `Textures/`, `Sound/`, `Music/`,
  `Seq/`); `package` copies those sub-trees into the output mod folder. Override per-run with
  `package <spec> <outDir> --assets <dir>`. Worked example: `../examples/custom_asset_spec.json`.

### textureSets (TXST) — retexture without a new mesh
A huge class of mods just **swaps the textures** of an existing mesh (a recolored sword, a reskinned
creature, a Markarth-painted banner reusing the Jorrvaskr banner `.nif`) without authoring a new
`.nif`. That's a **TextureSet (TXST)** record: a set of texture-map paths plus a consumer that points
a named material on a base mesh at it.

A TXST has up to eight optional slots; set only the ones you replace (an omitted slot keeps the
mesh's original map for that channel). Every path is **relative to `Data\Textures\`** — exactly like
a `model` path is relative to `Data\Meshes\` — so you **omit** the leading `Textures\`:

```jsonc
"textureSets": [
  { "editorId": "MF_GildedRubbleTexture",
    "diffuse": "ModForge\\rubble\\gilded_rubble_d.dds",   // slot 0 — color/albedo (_d)
    "normal":  "ModForge\\rubble\\gilded_rubble_n.dds",   // slot 1 — normal + gloss (_n)
    // mask(_m)/glow(_g)/height(_p)/environment(_e)/multilayer/backlight also available — all optional
    "flags": [ "NoSpecularMap" ] }                         // NoSpecularMap|FaceGenTextures|HasModelSpaceNormalMap
]
```

Wire it into a consumer with `alternateTextures` on a `statics` or `activators` record (any record
with a `model`). Each entry overrides one **named material/sub-mesh** inside the base `.nif`:

```jsonc
"statics": [
  { "editorId": "MF_GildedRubble",
    "model": "Dungeons\\Nordic\\Rubble\\NorRubblePiece03.nif",   // a VANILLA mesh, reused as-is
    "alternateTextures": [
      { "name": "NorRubblePiece03:0",        // MUST match a material/3D-name in the .nif (CK "AltTex" dialog)
        "index": 0,                           // 3D sub-mesh index (the trailing number in `name`)
        "textureSet": "MF_GildedRubbleTexture" } ] }              // ref → a TXST (in-spec or <master>:0xFORMID)
]
```

The `name`/`index` convention (`<MeshName>:<index>`) mirrors vanilla — inspect a real one with
`txstdiag` (a TXST's slots) or `dump` (a record's `altTexture` lines), e.g. vanilla STAT
`NorExtRubblePiece03_HeavySN` uses `name="NorRubblePiece03:0" index=0`. Get the material names from
the CK's *Model Data → Edit → 3D Name* list (NifSkope shows them as `BSLightingShaderProperty`
names); a wrong `name` silently swaps nothing.

**Honest limit:** ModForge writes the TXST record + the `alternateTextures` references only. The
`.dds` files themselves are **user-authored** — ModForge cannot create or render texture content, and
the headless toolchain cannot verify that a swap looks right in-game. Put your authored `.dds` files
under `Data/Textures/<your path>/` in the packaged mod folder. See `examples/texture_set_spec.json`
(with a placeholder `examples/textures/ModForge/rubble/` tree) and the cookbook recipe.

### packages — AI Packages (what an NPC DOES)
A `packages` entry is an AI package. Skyrim's PACK record is **template-driven**: you reference a
vanilla "procedure template" form via `template`, and that template defines the data input schema
(slot indices + types). Our package fills in the inputs for the slots the template defines.

ModForge currently implements seven templates — **Sandbox** (`Skyrim.esm:0x01C254`), **Sleep**
(`Skyrim.esm:0x019717`), **Travel** (`Skyrim.esm:0x016FAA`), **UseMagic** (`Skyrim.esm:0x0504F5`),
**Patrol** (`Skyrim.esm:0x017723`), **Follow** (`Skyrim.esm:0x019B2C`), and **Escort**
(`Skyrim.esm:0x023B73`). Author the matching subobject (`sandbox` / `sleep` / `travel` / `useMagic`
/ `patrol` / `follow` / `escort`) and the build will fill that template's Data slots. To target a
template ModForge doesn't yet handle (UseWeapon / …), still set `template`; the package emits
structurally valid but with no Data overrides (template defaults apply) and a warning. Use
`packagediag <Skyrim.esm> <0xFORMID>` to discover any template's named slot schema before adding support.

**Sandbox at a specific ref vs Travel:** Sandbox's `location` ref makes the NPC wander/eat/sit
**around** that ref (radius covers nearby furniture). Travel's `place` ref makes the NPC actually
**walk to** that ref and stop within `radius` of it. Common chain: a Travel package + a Sandbox
package on the same NPC's `packages` list (Travel first) — Travel runs until the NPC arrives,
then Sandbox takes over.

```jsonc
{ "editorId": "MF_HangAtSpotPackage",
  "template": "Skyrim.esm:0x01C254",        // Sandbox procedure template (find by EditorID "Sandbox")
  "preferredSpeed": "Walk",
  "interruptFlags": [                        // the lifelike-NPC switches — leave most ON
    "HellosToPlayer", "RandomConversations", "ObserveCombatBehavior",
    "GreetCorpseBehavior", "ReactionToPlayerActions", "FriendlyFireComments",
    "AggroRadiusBehavior", "AllowIdleChatter", "WorldInteractions" ],
  "schedule": { "hour": -1, "minute": -1, "durationInMinutes": 0, "dayOfWeek": "Any" },
  "sandbox": {
    "radius": 1024,                          // wander distance from the anchor
    "location": "",                           // empty -> LocationFallback (NPC's editor location);
                                              // a ref -> LocationTarget anchored at that placed ref
    "allowEating": true,  "allowSleeping": false,  "allowConversation": true,
    "allowIdleMarkers": true, "allowSitting": true, "allowWandering": true,
    "allowSpecialFurniture": true, "energy": 50.0 } }
```
Then attach to an NPC: `"npcs": [{ ..., "packages": [ "MF_HangAtSpotPackage" ] }]`.

**Why these inputs:** the Sandbox template names them (see `packagediag <Skyrim.esm> 0x01C254`).
`location: ""` is the safest default — the engine anchors the sandbox at wherever the NPC was placed.
A specific `location` ref (an REFR/ACHR FormID) anchors the sandbox at that reference's position.
`Allow Sleeping = false` keeps the NPC active 24/7 (good for visible-in-game testing); leave it true
for a normal day/night cycle. `Energy = 50` is the vanilla default (higher = more wandering).

**Travel template (`Skyrim.esm:0x016FAA`) — `travel` subobject:**
```jsonc
{ "editorId": "MF_GoToWhiterun",
  "template": "Skyrim.esm:0x016FAA",       // Travel
  "preferredSpeed": "Walk",
  "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
  "travel": {
    "place": "Skyrim.esm:0x0567F7",        // a ref to a placed REFR/ACHR (the destination)
    "radius": 256,                          // arrive within this many units (0 = exact point)
    "rideHorse": false,                     // template default
    "preferPath": false } }                 // template default
```
Travel has just 3 slots: `Place to Travel` / `Ride Horse if possible?` / `Prefer Preferred Path?`.
**Without a `place` ref the NPC won't actually travel** — the engine falls back to NearSelf
(degenerate: travel to where you already are) and the package no-ops. Chain a Sandbox package after
it (lower priority in the NPC's `packages` list) so the NPC has something to do on arrival.

**UseMagic template (`Skyrim.esm:0x0504F5`) — `useMagic` subobject:**
```jsonc
{ "editorId": "MF_AltarRitual",
  "template": "Skyrim.esm:0x0504F5",       // UseMagic
  "preferredSpeed": "Walk",
  "interruptFlags": [ "HellosToPlayer", "AllowIdleChatter" ],
  // For CONTINUOUS casting BOTH knobs are required (see "It.18 gotchas" below):
  "schedule": { "hour": -1, "minute": -1, "durationInMinutes": 1440, "dayOfWeek": "Any" },
  "useMagic": {
    "spell":           "Skyrim.esm:0x043324",   // REQUIRED — FormLink to a SPEL record (Candlelight)
    "location":        "",                       // optional placed-ref (where to stand); empty -> NearSelf
    "radius":          256,                      // location radius (template default 500)
    "target":          "",                       // optional placed-ref (who to cast on); empty -> PackageTargetSelf
    "holdWhenBlocked": true,
    "castTimeMin":     1.5, "castTimeMax":     2.5,
    "cooldownTimeMin": 8.0, "cooldownTimeMax": 12.0,
    "numToCastMin":    1, "numToCastMax":    1000,
    "dualCast":        false } }
```
UseMagic has 11 active slots (2-12). The **"Spell" slot is a `PackageTargetObjectID` FormLink to
a specific SPEL record** — NOT a category enum. (`Spell` implements `IObjectId`.) Build writes
slot 4 (Target) as `PackageTargetSelf` when `target` is empty, matching vanilla self-cast packages
like `WCollegePracticeCastWard`; set `target` to a placed-ref for cast-at-X (vanilla
`WCollegeOnmundPracticeFlames12x4` points at a target dummy).

**It.18 gotchas (learned the hard way — 3 in-game rounds):**
1. **Slot 3 (Spell) must be `PackageTargetObjectID`, not `PackageTargetObjectType`.** The template
   default shows `PackageTargetObjectType` (a category enum), but all 46 vanilla UseMagic packages
   override it with `PackageTargetObjectID` (FormLink). The enum form builds, dumps fine, no-ops in-game.
2. **Slot 4 (Target) must be set** — `PackageTargetSelf` for self-cast, otherwise
   `PackageTargetSpecificReference`. Leaving it as the template's `PackageTargetLinkedReference`
   fallback also no-ops in practice.
3. **`numToCastMax` is total package-lifetime casts**, NOT per-cycle. With `schedule.durationInMinutes=0`
   (the default) the package completes the moment its quota's hit. For continuous casting use BOTH
   a high upper bound (1000 like vanilla Onmund) AND a non-zero `schedule.durationInMinutes`
   (e.g. 1440 = 24h).
4. **Combat preempts UseMagic.** Vanilla — for an idle ritual caster this is correct (NPC switches
   to attacking instead of standing & casting Candlelight). To force casting to continue (e.g. a
   boss ritual), add `flags: [ "IgnoreCombat" ]` like vanilla `SprigganCallOverride`.
5. **Use `pkgsbytemplate <plugin> <0xFORMID>`** to scan a master for all packages using a given
   template. Necessary because `find` matches EditorIDs only, and many template-based packages
   (e.g. `WhiterunTempleCastHealingSpellSoldier`) don't carry the template name in their EditorID.

**Flags (Package.Flag):** `OffersServices`, `MustComplete`, `MaintainSpeedAtGoal`, `ContinueIfPcNear`,
`OncePerDay`, `PreferredSpeed`, `AlwaysSneak`, `AllowSwimming`, `IgnoreCombat`, `WeaponsUnequipped`,
`WeaponDrawn`, `NoCombatAlert`, `WearSleepOutfit`.

**Interrupt flags (Package.InterruptFlag):** `HellosToPlayer`, `RandomConversations`,
`ObserveCombatBehavior`, `GreetCorpseBehavior`, `ReactionToPlayerActions`, `FriendlyFireComments`,
`AggroRadiusBehavior`, `AllowIdleChatter`, `WorldInteractions`. **These are the difference between
a silent statue and a lifelike NPC.** Vanilla DefaultSandbox enables all of them.

### weathers & climates — custom skies (WTHR) + weather cycles (CLMT)

A **weather** (`WTHR`) is one *sky*: cloud layers, per-time-of-day colours for the
sky/fog/clouds/sun, precipitation, wind, fog distances. A **climate** (`CLMT`) is a
*cycle*: which weathers occur (each with a relative `chance` weight) plus sunrise/sunset
timing and the sun/moon textures. A climate references weathers; together they give a
worldspace or region its atmosphere.

```jsonc
"weathers": [{
  "editorId": "MF_EerieFog",
  "flags": ["Cloudy", "Rainy"],          // default ["Pleasant"]
  "skyUpperColor": {                      // each colour: sunrise/day/sunset/night, RGB 0–255
    "day":   { "r": 46, "g": 92, "b": 58 },
    "night": { "r": 8,  "g": 20, "b": 14 }   // omitted times-of-day fall back to `day`
  },
  "fogNearColor": { "day": { "r": 60, "g": 120, "b": 70 } },
  "sunlightColor": { "day": { "r": 120, "g": 170, "b": 110 } },  // directional light on the world
  "clouds": [{ "index": 0, "texture": "Sky\\SkyrimCloudsUpper04.dds",
               "xSpeed": 0.012, "ySpeed": -0.006, "alphaDay": 1.0, "alphaNight": 0.8 }],
  "precipitation": "Skyrim.esm:0x10780F",  // a rain SPGD (find one via weatherdiag on a vanilla rainy WTHR)
  "windSpeed": 0.35, "windDirection": 210,  // speed 0–1 (or 0–100); direction in degrees
  "fogDayNear": 256, "fogDayFar": 9000
}],
"climates": [{
  "editorId": "MF_EerieClimate",
  "weathers": [ { "weather": "MF_EerieFog", "chance": 75 },
                { "weather": "MF_PlainClear", "chance": 25 } ],   // chances are relative weights
  "sunriseBegin": "06:00", "sunriseEnd": "09:30",
  "sunsetBegin": "17:00",  "sunsetEnd": "20:00",
  "moons": ["Masser", "Secunda"], "volatility": 40
}]
```

- **Minimal is valid.** A weather with just an `editorId` is a vanilla-sane clear-day sky;
  a climate needs only an `editorId` + at least one `weather`. Everything else defaults.
- **Colours** are 8-bit RGB (0–255). Any omitted time-of-day is seeded from `day`, so a
  partial colour is still valid. Validate flags out-of-range components.
- **Wind direction** is authored in **degrees** (0–360); it's stored on disk as a fraction
  of a full circle. **Wind speed** accepts a 0–1 fraction or a 0–100 percentage.
- **`precipitation`** is a *ref* to a shader-particle-geometry (`SPGD`). Discover a vanilla
  rain one with `weatherdiag <Skyrim.esm> <a-rainy-WTHR-formid>` (e.g. `SkyrimStormRain`
  → `Skyrim.esm:0x10780F`). The `Rainy`/`Snow` flags drive the engine's precip systems.
- **Inspect** a generated or vanilla record with `weatherdiag <esp> <0xFORMID>` /
  `climatediag <esp> <0xFORMID>`, or `dump` (which prints both).

> **Assigning the climate is a separate step.** Emitting a `WTHR`+`CLMT` does **not** by
> itself change any in-game sky. A vanilla game applies a climate via a **worldspace**
> (`WRLD` `Climate` field) or a **region** (`REGN` weather-data) record — neither is built
> here (worldspace/region authoring is out of scope). The records this produces are valid
> targets to point such a record at; doing so by hand (or via a future WRLD/REGN feature)
> is the hook. **IN-GAME CONFIRMED (It.36, 2026-06-02):** force weather via console `sw <XX>000800`
> where `XX` = plugin's load order slot in hex (see MO2 right panel). The `build` command prints
> the `sw` commands for all WTHR records after a successful build.

## Workflow

```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json          # check first
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp   # just the plugin
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir # esp + compiled scripts -> MO2 folder
```
`package` lays out `OutModDir/<pluginName>` + `Scripts/*.pex` + `Scripts/Source/*.psc`.

**NL → spec:** describe what you want to an AI agent (Claude Code); the agent emits a spec
conforming to this doc / `../examples/spec.schema.json` (per `for_agent.md`), runs `validate`
(self-correcting on problems), then `build`/`package`. This agent-driven loop **is** the
NL→spec layer — there is no in-tool LLM API (the once-planned `describe` command is dropped),
so there's no API key/provider to configure.

## Not yet covered (extend in `ModForge.Core` `Generator.Build` + a spec class)
World placement now covers new interior cells, vanilla interior cells, **and exterior/worldspace
cells** (via `worldspace` + world position), and ModForge can now **create** new worldspaces (WRLD)
+ regions (REGN) — see *worldspaces & regions* above (record layer only; terrain/LOD/navmesh stay
CK-side). Refs (in-spec or `<master>:0xFORMID`) and the `find` command are the building blocks for
the external ones. Remaining gaps are long-tail record types/fields and the CK-side terrain/LOD/
navmesh authoring — the record-side pattern is the same: add a spec class + a loop in `Build`.

See `../examples/sample_spec.json` for a complete working example.
