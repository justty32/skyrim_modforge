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
  "packages": [...]              // AI Packages — what an NPC DOES (sandbox/travel/use furniture)
}
```

## Record types

| section | fields |
|---------|--------|
| `miscItems` | `editorId`, `name`, `value` (int≥0), `weight` (number), `keywords` (array of *refs*) |
| `books` | `editorId`, `name`, `text` (book body) |
| `weapons` | `editorId`, `name`, `value`, `weight`, `damage` (int≥0), `speed` (number), `reach` (number), `keywords` (array of *refs*) |
| `npcs` | `editorId`, `name`, `factions` (array of *refs*), `race` (*ref*), `class` (*ref*), `outfit` (*ref* → DefaultOutfit), `level` (int), `autoCalcStats` (bool — derive H/M/S + skills from level + class), `packages` (array of *refs* → PACK; the NPC's AI package list, evaluated in order), `voiceType` (*ref* → VTYP), `crimeFaction` (*ref* → FACT; city-citizen identity, required for cross-cell Travel), `unique` (bool — one-off actor, helps engine AI tracking), `combatStyle` (*ref* → CSTY; HOW the AI fights), `spells` (array of *refs* → SPEL; the AI's spell list), `greeting` (string — the Hello line; when this NPC has custom `dialogue`, a Hello info is auto-emitted so it's conversable. Empty ⇒ a default line) |
| `quests` | `editorId`, `name`, `objectives` (array of `{ index (int), text }`) |
| `dialogue` | `editorId`, `questEditorId`, `speakerNpcEditorId` (optional), `prompt`, `responses` (array of strings), `emotion` (optional — `Neutral`\|`Anger`\|`Disgust`\|`Fear`\|`Sad`\|`Happy`\|`Surprise`), `emotionValue` (0–100). Optional **result fragment** (runs when the line is picked): `resultScript` (Scriptname, `Extends TopicInfo`, `Fragment_0`), `resultScriptSource` (`.psc`), `resultProperties` (bound props), `goodbye` (bool — close menu after). Build wires the full chain (Quest→DialogView→Branch→Topic→INFO + a Hello) — see the dialogue section below |
| `banter` | `editorId` (optional), `questEditorId`, `speakerNpcEditorId`, `responses` (array of strings — one unprompted comment), `emotion`/`emotionValue`, `conditions` (situational CTDA gates). Proactive (NPC-initiated) lines; entries sharing a (speaker, quest) merge into one ambient Misc/`IDLE` topic with Random INFOs. Needs the speaker to have idle chatter enabled (a Sandbox/follow package). See the *banter* section below |
| `spells` | `editorId`, `name`, `effects` (array of *effects*), `spellType`, `castType`, `targetType`, `baseCost` (int), `chargeTime` (number) |
| `magicEffects` | `editorId`, `name`, `description`, `archetype`, `actorValue`, `magicSkill`, `resistValue`, `castType`, `targetType`, `baseCost` (number), `flags` (array), `association` (*ref*) — a custom MGEF an `effect` can point at |
| `potions` | `editorId`, `name`, `value`, `weight`, `effects` (array of *effects*) |
| `armors` | `editorId`, `name`, `value`, `weight`, `armorRating` (number), `armorType` (`light`\|`heavy`\|`clothing`), `slots` (array of biped-slot names), `keywords` (array of *refs*) |
| `factions` | `editorId`, `name` |
| `classes` | `editorId`, `name`, `description`, `teaches` (Skill), `maxTrainingLevel`, `healthWeight`/`magickaWeight`/`staminaWeight` (attribute distribution), `skillWeights` (`{ Skill: 0–255 }`) — an npc `class` can point at one |
| `messages` | `editorId`, `name`, `description` (body text) |
| `cells` | `editorId`, `name`, `template` (vanilla interior cell `<master>:0xFORMID` to copy lighting from — else the new cell is black) |
| `placements` | `base` (*ref*); **interior:** `cell` (in-spec editorId **or** vanilla interior cell `<master>:0xFORMID`) **or exterior:** `worldspace` (`<master>:0xFORMID`, position is world coords); `kind` (`npc`\|`object`), `position` (`{x,y,z}`), `rotation` (`{x,y,z}` degrees), `persistent` (bool) |
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
| `statics` | `editorId`, `model` (a `.nif` path — reference a vanilla mesh; a placement base, no name) |
| `activators` | `editorId`, `name`, `model` (`.nif` path), `keywords` (array of *refs*); attach behaviour via `scripts` |
| `recipes` | `editorId`, `createdObject` (*ref*), `count` (int), `workbench` (keyword *ref*; defaults to the forge), `components` (array of `{ item (*ref*), count (int) }`) — a crafting recipe (COBJ) |
| `packages` | `editorId`, `template` (*ref* → a vanilla procedure template, e.g. Sandbox = `Skyrim.esm:0x01C254`, Travel = `Skyrim.esm:0x016FAA`, UseMagic = `Skyrim.esm:0x0504F5`), `flags` (array — `Package.Flag` names), `interruptFlags` (array — `HellosToPlayer`/`AllowIdleChatter`/`WorldInteractions`/…), `preferredSpeed` (`Walk`\|`Jog`\|`Run`\|`FastWalk`), `combatStyle` (*ref*, optional), `ownerQuest` (*ref*, optional), `schedule` (`{ month, dayOfWeek, date, hour, minute, durationInMinutes }`), `sandbox` / `travel` / `useMagic` / `follow` / `escort` (template-input subobjects — see below), `conditions` (array of CTDA gates — see *conditions* below; the engine runs the first package whose conditions pass, so e.g. a Follow package gated on `GetInFaction CurrentFollowerFaction==1` only activates after recruitment). An AI package; assign it to one or more NPCs via `npcs[].packages`. |
| `combatStyles` | `editorId`, `offensiveMult`/`defensiveMult`/`groupOffensiveMult` (~aggression/blocking/group-boldness), `equipMultMelee`/`equipMultMagic`/`equipMultRanged`/`equipMultShout`/`equipMultUnarmed`/`equipMultStaff` (AI weapon-preference scores; for a mage NPC, push Magic high relative to the others — vanilla `csVampireMagic` uses 8.1/2.15/0.51), `avoidThreatChance` (0..1), `flags` (array — `Dueling`\|`Flanking`\|`AllowDualWielding`). An npc's `combatStyle` ref can point at one. |

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
`string`/`object`). The build attaches the INFO's `OnEnd` fragment VMAD. Set `goodbye: true`
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

### conditions — CTDA gates (on a `dialogue` INFO, a `banter` INFO, or a `package`)
A condition is **static gate data**, so it lives in the spec (logic still belongs in Papyrus). Both
`dialogue[].conditions` and `packages[].conditions` take the same shape:
```jsonc
{ "function": "GetItemCount",          // form-arg: GetInFaction | GetItemCount | GetGlobalValue | GetStage | GetIsID | GetRelationshipRank
  //                                    // actorValue-arg: GetActorValue | GetActorValuePercent (0..1 fraction)
  //                                    // no-arg situational: GetCurrentTime (hour 0..24) | IsInInterior | IsInCombat | GetRandomPercent (0..99)
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

### recipes (crafting / COBJ)
Make an item craftable at a workbench:
```jsonc
{ "editorId": "MF_ForgedBladeRecipe",
  "createdObject": "MF_ForgedBlade",   // a ref — usually an in-spec weapon/armor
  "count": 1,
  "workbench": "Skyrim.esm:0x088105",   // bench keyword ref; OMIT to default to the forge
  "components": [                        // consumed on craft (ref + count)
    { "item": "Skyrim.esm:0x05ACE4", "count": 3 },   // IngotIron
    { "item": "Skyrim.esm:0x0800E4", "count": 1 } ] }  // LeatherStrips
```
Common bench keywords: `0x088105` forge (new weapons/armor), `0x0ADB78` armor table (temper armor),
`0x088108` sharpening wheel (temper weapons), `0x0F46CE` Skyforge. Perk/skill gating (conditions)
is not yet a spec field — a recipe shows whenever you have the components.

### packages — AI Packages (what an NPC DOES)
A `packages` entry is an AI package. Skyrim's PACK record is **template-driven**: you reference a
vanilla "procedure template" form via `template`, and that template defines the data input schema
(slot indices + types). Our package fills in the inputs for the slots the template defines.

ModForge currently implements three templates — **Sandbox** (`Skyrim.esm:0x01C254`), **Travel**
(`Skyrim.esm:0x016FAA`), and **UseMagic** (`Skyrim.esm:0x0504F5`). Author the matching subobject
(`sandbox` / `travel` / `useMagic`) and the build will fill that template's Data slots. To target
a template ModForge doesn't yet handle (Patrol / Follow / Escort / UseWeapon / …), still set
`template`; the package emits structurally valid but with no Data overrides (template defaults
apply) and a warning. Use `packagediag <Skyrim.esm> <0xFORMID>` to discover any template's named
slot schema before adding support.

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
cells** (via `worldspace` + world position). Refs (in-spec or `<master>:0xFORMID`) and the `find`
command are the building blocks for the external ones. Remaining gaps are long-tail record
types/fields — the same pattern: add a spec class + a loop in `Build`.

See `../examples/sample_spec.json` for a complete working example.
