<!-- Part 1/5 — Introduction through Enchantments -->
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
