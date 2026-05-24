# ModForge spec — authoring reference

The **spec** is a JSON file describing the content of one Skyrim plugin. It is the
contract between intent (natural language / an LLM) and the deterministic generator
(Mutagen). You write/produce a spec, `validate` it, then `build` or `package` it.

```
NL / idea ──(LLM)──▶ spec.json ──(validate)──▶ ──(build | package)──▶ .esp [+ .pex]
```

Property names are **case-insensitive** (`editorId` == `EditorId`); examples use camelCase.

## Cross-references & IDs

- Every record has an **`editorId`** — a stable, unique name you choose. It is how
  records reference each other *within the spec* (an npc joins a faction by its
  `editorId`; a dialogue names its quest by `editorId`). It is **not** a FormID:
  Mutagen assigns FormIDs and masters automatically.
- `editorId` must be **non-empty and unique** across the whole spec (`validate` enforces).
- `esl: true` (default) flags the plugin as a light master — keep new records **≤ 4096**.

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
  "esl": true,                  // light-master flag (default true)

  "miscItems": [...], "books": [...], "weapons": [...], "npcs": [...],
  "quests": [...], "dialogue": [...], "spells": [...], "potions": [...],
  "armors": [...], "factions": [...], "messages": [...],
  "scripts": [...],             // Papyrus attachments (see below)
  "cells": [...], "placements": [...],  // new interior cells + placing forms in them
  "leveledItems": [...], "leveledNpcs": [...], "containers": [...]
}
```

## Record types

| section | fields |
|---------|--------|
| `miscItems` | `editorId`, `name`, `value` (int≥0), `weight` (number), `keywords` (array of *refs*) |
| `books` | `editorId`, `name`, `text` (book body) |
| `weapons` | `editorId`, `name`, `value`, `weight`, `damage` (int≥0), `speed` (number), `reach` (number), `keywords` (array of *refs*) |
| `npcs` | `editorId`, `name`, `factions` (array of *refs*), `race` (*ref*), `class` (*ref*), `outfit` (*ref* → DefaultOutfit) |
| `quests` | `editorId`, `name`, `objectives` (array of `{ index (int), text }`) |
| `dialogue` | `editorId`, `questEditorId`, `speakerNpcEditorId` (optional), `prompt`, `responses` (array of strings) |
| `spells` | `editorId`, `name`, `effects` (array of *effects*), `spellType`, `castType`, `targetType`, `baseCost` (int), `chargeTime` (number) |
| `potions` | `editorId`, `name`, `value`, `weight`, `effects` (array of *effects*) |
| `armors` | `editorId`, `name`, `value`, `weight`, `armorRating` (number), `armorType` (`light`\|`heavy`\|`clothing`), `slots` (array of biped-slot names), `keywords` (array of *refs*) |
| `factions` | `editorId`, `name` |
| `messages` | `editorId`, `name`, `description` (body text) |
| `cells` | `editorId`, `name` (a new interior cell) |
| `placements` | `base` (*ref*), `cell` (in-spec cell editorId), `kind` (`npc`\|`object`), `position` (`{x,y,z}`), `rotation` (`{x,y,z}` degrees), `persistent` (bool) |
| `leveledItems` | `editorId`, `chanceNone` (0–100), `flags` (array), `entries` (array of `{ reference (*ref*), level (int), count (int) }`) |
| `leveledNpcs` | same shape as `leveledItems`, but `reference` is an npc/leveled-npc |
| `containers` | `editorId`, `name`, `weight`, `items` (array of `{ item (*ref*), count (int) }`) |

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
The `magicEffect` is a *ref* — find a vanilla one with `find <Skyrim.esm> <query> MagicEffect`
(e.g. `AlchRestoreHealth = Skyrim.esm:0x03EB15`, `AlchDamageHealth = Skyrim.esm:0x03EB42`).
A potion is fully functional with one effect; a spell also wants cast/spell-type tuning
(not yet a spec field — defaults are written) but the effect is the core.

### dialogue
A `dialogue` entry is a player topic shown under a quest's branch, optionally limited
to one speaker NPC (a `GetIsID` condition). `questEditorId` must name a quest in this
spec; `speakerNpcEditorId`, if set, must name an npc. `prompt` is the player's line;
`responses` are the NPC's spoken lines.

> **In-game caveat:** the generator writes *structurally valid* dialogue records, but
> making a line actually surface in conversation can need quest-flag/branch tuning and
> in-game (Proton) testing — that is content/runtime tuning, not a Mutagen limitation.

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
  { "editorId": "MF_TestRoom", "name": "ModForge Test Room" }   // a new interior cell
],
"placements": [
  { "base": "MF_Smith", "cell": "MF_TestRoom",                   // an in-spec NPC ...
    "position": { "x": 0, "y": 0, "z": 0 },
    "rotation": { "x": 0, "y": 0, "z": 0 } },                    //   rotation in degrees
  { "base": "Skyrim.esm:0x0001397E", "cell": "MF_TestRoom",      // ... or a vanilla form (ref)
    "position": { "x": 60, "y": 0, "z": 0 }, "kind": "object" }
]
```
- A `cell` is a **new interior cell**; reach it in-game with `coc <editorId>` (it has no
  lighting template, so it’ll be dark — fine for testing that the placed forms are there).
- A `placement` puts a `base` form into a `cell`. `base` is a *ref* (in-spec or external);
  NPCs become `PlacedNpc`, anything else `PlacedObject` (`kind` overrides the guess).
  `position` is world units, `rotation` is **degrees**. `persistent: true` puts it in the
  cell’s persistent list (needed if a quest/script references it).
- **Only in-spec cells** are supported. Placing into a **vanilla cell** (e.g. dropping an
  NPC into Whiterun) needs a cell *override* and is not implemented yet — `validate` flags it.

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

## Workflow

```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json          # check first
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp   # just the plugin
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir # esp + compiled scripts -> MO2 folder
```
`package` lays out `OutModDir/<pluginName>` + `Scripts/*.pex` + `Scripts/Source/*.psc`.

**NL → spec:** describe what you want; an LLM emits a spec conforming to this doc /
`examples/spec.schema.json`; run `validate` (self-correct on problems); then `package`.
A live `describe` command (LLM API) is planned (It.6c) — until then the LLM step is done
interactively.

## Not yet covered (extend in `Program.cs` `Build` + a spec class)
Mainly **placement into a vanilla cell** (an NPC/object dropped into Whiterun etc. — needs a
cell *override*; new in-spec interior cells already work via `cells`/`placements`). Refs
(in-spec or `<master>:0xFORMID`) and the `find` command are the building blocks for the
external ones. Other record types/fields are the same pattern — add a spec class + a loop.

See `examples/sample_spec.json` for a complete working example.
