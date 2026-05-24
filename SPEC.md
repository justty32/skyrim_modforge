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

## Top-level shape

```jsonc
{
  "pluginName": "MyMod.esp",   // output filename / ModKey
  "esl": true,                  // light-master flag (default true)

  "miscItems": [...], "books": [...], "weapons": [...], "npcs": [...],
  "quests": [...], "dialogue": [...], "spells": [...], "potions": [...],
  "armors": [...], "factions": [...], "messages": [...],
  "scripts": [...]              // Papyrus attachments (see below)
}
```

## Record types

| section | fields |
|---------|--------|
| `miscItems` | `editorId`, `name`, `value` (int≥0), `weight` (number) |
| `books` | `editorId`, `name`, `text` (book body) |
| `weapons` | `editorId`, `name` |
| `npcs` | `editorId`, `name`, `factions` (array of faction `editorId`s) |
| `quests` | `editorId`, `name`, `objectives` (array of `{ index (int), text }`) |
| `dialogue` | `editorId`, `questEditorId`, `speakerNpcEditorId` (optional), `prompt`, `responses` (array of strings) |
| `spells` | `editorId`, `name` |
| `potions` | `editorId`, `name`, `value`, `weight` |
| `armors` | `editorId`, `name`, `value`, `weight`, `armorRating` (number) |
| `factions` | `editorId`, `name` |
| `messages` | `editorId`, `name`, `description` (body text) |

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
Effects on spells/potions, armor slots/keywords, container contents, leveled lists,
cells/placement, npc race/class/outfit (an npc currently needs those to be a fully
functional actor in-game). Same patterns as the existing types.

See `examples/sample_spec.json` for a complete working example.
