# ModForge spec — intro & record-type table

← [index](SPEC-index.md)

The **spec** is a JSON file describing the content of one Skyrim plugin. It is the
contract between intent (natural language, turned into a spec by an AI agent) and the
deterministic generator (Mutagen). You write/produce a spec, `validate` it, then `build`
or `package` it.

```
NL / idea ──(AI agent: Claude Code)──▶ spec.json ──(validate)──▶ ──(build | package)──▶ .esp [+ .pex]
                                               └─(optional voicelines)──▶ Sound/Voice/<plugin>/<voiceType>/*.fuz
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
  "encounterZones": [...],       // ECZN — level scaling / respawn for an area (a cell/spawn points at one)
  "voiceTemplates": [...],       // TTS/voice-clone recipes used by npcs[].voiceTemplate
  "voiceLine": { "format": "fuz", "skipLip": false }, // global voice output settings
  "presets": {                   // non-emitting cookbook fragments; expand into arrays above to build
    "lighting": {}, "weather": {}, "packages": {}, "identities": {}
  }
}
```

## Record types

| section | fields |
|---------|--------|
| `miscItems` | `editorId`, `name`, `value` (int≥0), `weight` (number), `keywords` (array of *refs*), `template` (vanilla MISC ref to clone a model from), `model` (custom `.nif` path — overrides `template`'s mesh), `pickUpSound`/`putDownSound` (SNDR *refs*) — see [external_assets.md](../external_assets.md) |
| `books` | `editorId`, `name`, `text` (book body), `template` (*ref* → a vanilla BOOK to clone a model from — a takeable/readable book NEEDS one or it CRASHES on 3D-read), `value` (int; 0 ⇒ keep template's), `weight` (number; 0 ⇒ keep template's), `flags` (array of `Book.Flag` names, e.g. `CantBeTaken`), `teaches` (optional — a *teaching* book; see below) |
| `books[].teaches` | `{ "kind": "spell", "spell": <ref> }` — a **spell tome** that grants a SPEL on first read (`spell` is an in-spec spell editorId OR a vanilla `<master>:0xFORMID`); OR `{ "kind": "skill", "skill": <name> }` — a **skill book** that raises a `Skill` (e.g. `Destruction`, `OneHanded`, `Smithing`) on first read; OR omit ⇒ a plain book (teaches nothing). A teaching book must have a `template`. |
| `weapons` | `editorId`, `name`, `value`, `weight`, `damage` (int≥0), `speed` (number), `reach` (number), `keywords` (array of *refs*), `enchantment` (*ref* → ENCH, in-spec or vanilla `<master>:0xFORMID`), `enchantmentAmount` (int — the weapon's charge pool, e.g. 1500–3000; 0 = engine auto-calc), `template` (vanilla WEAP ref — clones model/anim/equip; needed to avoid an equip CRASH), `model` (custom world-mesh `.nif` path — pair WITH `template`), `pickUpSound`/`putDownSound` (SNDR *refs*) |
| `npcs` | `editorId`, `name`, `factions` (array of *refs*), `race` (*ref*), `class` (*ref*), `outfit` (*ref* → DefaultOutfit), `level` (int), `autoCalcStats` (bool — derive H/M/S + skills from level + class), `packages` (array of *refs* → PACK; the NPC's AI package list, evaluated in order), `voiceType` (*ref* → VTYP; also determines the `Sound/Voice/<plugin>/<voiceType>/` folder for generated dialogue audio), `voiceTemplate` (ref → `voiceTemplates[].id`, optional TTS route), `crimeFaction` (*ref* → FACT; city-citizen identity, required for cross-cell Travel), `unique` (bool — one-off actor, helps engine AI tracking), `combatStyle` (*ref* → CSTY; HOW the AI fights), `spells` (array of *refs* → SPEL; the AI's spell list), `perks` (array of *refs* → PERK; granted to the actor as passive ability/entry-point perks at game start), `greeting` (string — the Hello line; when this NPC has custom `dialogue`, a Hello info is auto-emitted so it's conversable. Empty ⇒ a default line) |
| `quests` | `editorId`, `name`, `startGameEnabled` (bool, default true), `priority` (0–255), `objectives` (array of `{ index (int), text, showStage?, completeStage? }`), `stages` (array of `{ index (int), logEntry?, completeQuest?, failQuest?, conditions? }`) — see *Quest stages* in [SPEC-quests](SPEC-quests.md) |
| `dialogue` | `editorId`, `questEditorId`, `speakerNpcEditorId` (optional), `prompt`, `responses` (array of strings), `emotion` (optional — `Neutral`\|`Anger`\|`Disgust`\|`Fear`\|`Sad`\|`Happy`\|`Surprise`), `emotionValue` (0–100). `setStage` (int — advance the quest to this stage when the line is picked; `package` auto-compiles + VMAD-attaches the TIF fragment and auto-adds a `GetStage < N` condition so the line won't repeat). Optional **custom result fragment** (overrides the auto TIF): `resultScript` (Scriptname, `Extends TopicInfo`, `Fragment_0`), `resultScriptSource` (`.psc`), `resultProperties` (bound props), `goodbye` (bool — close menu after). Build wires the full chain (Quest→DialogView→Branch→Topic→INFO + a Hello) — see [SPEC-dialogue](SPEC-dialogue.md) |
| `banter` | `editorId` (optional), `questEditorId`, `speakerNpcEditorId`, `responses` (array of strings — one unprompted comment), `emotion`/`emotionValue`, `conditions` (situational CTDA gates). Proactive (NPC-initiated) lines; entries sharing a (speaker, quest) merge into one ambient Misc/`IDLE` topic with Random INFOs. Needs the speaker to have idle chatter enabled (a Sandbox/follow package). See [SPEC-dialogue](SPEC-dialogue.md) |
| `scenes` | `editorId`, `questEditorId` (host quest), `actors` (array of `{ aliasId (int), npc (*ref*), name }`), `phases` (ordered array of `{ speaker (an aliasId), lines (array of strings), emotion, emotionValue }`), `beginOnQuestStart` (bool, default true), `stopQuestOnEnd` (bool). A **SCEN** — two NPCs talking to EACH OTHER. See [SPEC-dialogue](SPEC-dialogue.md) |
| `spells` | `editorId`, `name`, `effects` (array of *effects*), `spellType`, `castType`, `targetType`, `baseCost` (int), `chargeTime` (number), `equipType` (EQUP *ref*). **Castable types (Spell/Voice/Power/LesserPower) auto-default to EitherHand `Skyrim.esm:0x00013F44` when omitted** — a Voice/shout spell with no EQUP is learned but **can't be shouted**; set only to override |
| `magicEffects` | `editorId`, `name`, `description`, `archetype`, `actorValue`, `magicSkill`, `resistValue`, `castType`, `targetType`, `baseCost` (number), `flags` (array), `association` (*ref*), `projectile`/`castingArt`/`hitEffectArt`/`explosion` (*refs* — the visible bolt + cast/impact FX; an Aimed spell/shout needs a `projectile` or it fires invisibly/silently), `sounds` (array of `{ type (default `Release`), sound (SNDR *ref*) }` — `Release` is the cast-out/effect sound; a shout's spoken-word *voice* is a recorded voice asset, not settable here) — a custom MGEF an `effect` can point at |
| `enchantments` | `editorId`, `name`, `enchantType` (`weapon`\|`apparel`\|`staff`), `castType`/`targetType` (optional overrides), `enchantmentCost` (int — per-cast charge cost / worn cost), `chargeTime` (number — staff charge-up), `effects` (array of *effects*) — an Object Effect (ENCH) a weapon/armor `enchantment` field points at |
| `potions` | `editorId`, `name`, `value`, `weight`, `effects` (array of *effects*) |
| `armors` | `editorId`, `name`, `value`, `weight`, `armorRating` (number), `armorType` (`light`\|`heavy`\|`clothing`), `slots` (array of biped-slot names), `keywords` (array of *refs*), `enchantment` (*ref* → ENCH, normally an `apparel` constant-effect one), `template` (vanilla ARMO *ref* — clones its **Armature** (worn mesh) + WorldModel; **required or the armor equips INVISIBLE**, e.g. `Skyrim.esm:0x00012E49` ArmorIronCuirass), `model` (custom ground-mesh `.nif` path — pair WITH `template`) |
| `factions` | `editorId`, `name`, `vendor` (optional sub-object — turns this into a MERCHANT faction; see [SPEC-worldspaces](SPEC-worldspaces.md)) |
| `classes` | `editorId`, `name`, `description`, `teaches` (Skill), `maxTrainingLevel`, `healthWeight`/`magickaWeight`/`staminaWeight` (attribute distribution), `skillWeights` (`{ Skill: 0–255 }`) — an npc `class` can point at one |
| `messages` | `editorId`, `name`, `description` (body text) |
| `cells` | `editorId`, `name`, `template` (vanilla interior cell `<master>:0xFORMID` to copy lighting from — else the new cell is black), `encounterZone` (*ref* → ECZN — level scaling/respawn for the whole cell) |
| `placements` | `base` (*ref* — a concrete NPC_ actor or object form; **never a raw LeveledNpc list (LVLN)** — LVLN as ACHR base CTDs at load, see [SPEC-world](SPEC-world.md)); **interior:** `cell` (in-spec editorId **or** vanilla interior cell `<master>:0xFORMID`) **or exterior:** `worldspace` (`<master>:0xFORMID`, position is world coords); `kind` (`npc`\|`object`), `position` (`{x,y,z}`), `rotation` (`{x,y,z}` degrees), `persistent` (bool), `encounterZone` (*ref* → ECZN — per-ref override of the cell's zone) |
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
| `statics` | `editorId`, `model` (a `.nif` path — vanilla OR custom mesh; a placement base, no name), `alternateTextures` (array — swap the mesh's textures to a TXST; see [SPEC-items](SPEC-items.md)) |
| `activators` | `editorId`, `name`, `model` (`.nif` path), `keywords` (array of *refs*), `alternateTextures` (array — same as `statics`), `activationSound`/`loopingSound` (SNDR *refs*); attach behaviour via `scripts` |
| `furniture` | `editorId`, `name`, `model` (`.nif` path — vanilla OR custom mesh), `keywords` (array of *refs*) — a placeable interactive object (chair/bed/bench/idle marker); place it with a `placement` |
| `sounds` | `editorId`, `files` (array of Data-relative `Sound\...` `.wav`/`.xwm` paths), `category` (SNCT *ref*, default AudioCategorySFX), `outputModel` (SOPM *ref*, default vanilla SFX), `priority` (0–255), `staticAttenuation` (dB) — a Sound Descriptor (SNDR) a record's sound field points at. See [external_assets.md](../external_assets.md) |
| `voiceTemplates` | `id`, `engine` (`f5`\|`fish-s2`\|`chatterbox`\|`gptsovits`\|`xtts`; only configured wrappers can actually synthesize), `referenceWav`, `referenceText`, `modelPath`, `rvcModel`, `language`, `seed`, `speed`, `exaggeration`. Referenced by `npcs[].voiceTemplate`; used only by `voicelines`, not by `build` itself. See [SPEC-workflow § Voice](SPEC-workflow.md#voice-tts-voice-cloning--fuz) |
| `voiceLine` | global post-build output settings: `format` (`fuz`\|`wav`\|`xwm`, default `fuz`) and `skipLip` (true = static mouth/no `.lip`). Voice assets are loose files under `Sound/Voice/<plugin>/<voiceType>/`, not embedded plugin records. |
| `recipes` | `editorId`, `kind` (`craft`/`temper`/`smelt`/`breakdown`), `createdObject` (*ref*), `count` (int), `workbench` (named selector `forge`/`sharpeningWheel`/`armorTable`/`smelter`/`tanningRack`/`skyforge` OR a keyword *ref*; defaults by kind), `components` (array of `{ item (*ref*), count (int) }`), `conditions` (array of shared CTDA `{ function, param (*ref*), comparison, value, or }` — perk/item/skill gating) — a crafting/tempering/smelting recipe (COBJ) |
| `packages` | `editorId`, `template` (*ref* → a vanilla procedure template), `flags`, `interruptFlags`, `preferredSpeed`, `combatStyle`, `ownerQuest`, `schedule`, `sandbox`/`sleep`/`travel`/`useMagic`/`patrol`/`follow`/`escort` (template subobjects), `conditions` — see [SPEC-packages](SPEC-packages.md) |
| `combatStyles` | `editorId`, `offensiveMult`/`defensiveMult`/`groupOffensiveMult`, `equipMultMelee`/`equipMultMagic`/`equipMultRanged`/`equipMultShout`/`equipMultUnarmed`/`equipMultStaff`, `avoidThreatChance`, `flags` (`Dueling`\|`Flanking`\|`AllowDualWielding`) |
| `encounterZones` | `editorId`, `minLevel` (0–255), `maxLevel` (0–255; **0 = uncapped**), `rank`, `owner`, `location`, `flags` (`NeverResets`\|`MatchPcBelowMinimumLevel`\|`DisableCombatBoundary`) — see [SPEC-worldspaces](SPEC-worldspaces.md) |
| `perks` | `editorId`, `name`, `description`, `playable`/`hidden`/`trait`, `level`, `numRanks` (≥1), `nextPerk`, `conditions`, `effects` (array — `ability` or `entryPoint`) — see [SPEC-items](SPEC-items.md) |
| `wordsOfPower` | `editorId`, `name` (dragon-script glyph), `translation` (English gloss) — one Word of Power (WOOP) |
| `shouts` | `editorId`, `name`, `description`, `menuDisplayObject`, `words` (array of up to 3 `{ word, spell, recoveryTime }`) — a SHOU |
| `wordWalls` | `editorId`, `name`, `shout`, `wordIndex` (1\|2\|3), `word`, `scriptName`, `triggerEditorId`/`triggerBase`, placement (`cell`/`worldspace` + `position`/`rotation`) |
| `textureSets` | `editorId`, eight optional `.dds` slot paths (`diffuse`, `normal`, `mask`, `glow`, `height`, `environment`, `multilayer`, `backlight`) relative to `Data\Textures\`, `flags` — see [SPEC-items](SPEC-items.md) |
| `weathers` | `editorId`, `template` (optional vanilla WTHR to copy clouds/colours from), `flags`, per-time-of-day colours, `clouds`, `precipitation`, `windSpeed`/`windDirection`, `fogDayNear`/`fogDayFar`/`fogNightNear`/`fogNightFar`, `transitionDelta` — see [SPEC-packages](SPEC-packages.md) |
| `climates` | `editorId`, `weathers` (array of `{ weather, chance }`), sunrise/sunset times, `sunTexture`/`sunGlareTexture`, `moons`, `phaseLength`, `volatility` — see [SPEC-packages](SPEC-packages.md) |
| `presets` | non-emitting cookbook fragments grouped under `lighting`, `weather`, `packages`, and `identities`. The builder ignores this section; copy/expand fragments into the normal arrays to emit records. See [cookbook-presets](../lifelike/cookbook-presets.md) and `examples/presets-cookbook.json` |

A field marked *ref* takes an in-spec `editorId` **or** `"<master>:0xFORMID"` (see
*References to vanilla / external forms* above). A standing NPC needs at least `race` +
`class` to behave as a real actor in-game; `outfit` gives it clothing/gear.
