# ModForge spec — SKSE distributor configs

← [spec index](SPEC-index.md)

These sections emit **loose config files** (no ESP record) for SKSE distributor frameworks. They
let a mod attach records to other mods' NPCs/items **without an ESP patch** — the standard
compatibility layer for follower/NPC packs. ModForge writes the config; the framework's `.dll`
(player-supplied) does the runtime work.

Currently implemented: **SPID**. KID / SkyPatcher / FLM follow the same loose-file pattern (roadmap
D-group, see `workflows/roadmap/all-findings-gaps.md`).

---

## `spidDistributions` — SPID (Spell Perk Item Distributor)

SPID scans `Data/` for every `*_DISTR.ini` at start-up and, as each NPC loads, attaches matching
records to its actorbase by filter. Equivalent to editing the NPC in the CK, but with **no ESP
patch** and **no conflict** with other mods touching the same NPC.

Format + field semantics are verified against the SPID 7.3 reference and real ini files
(`sub_projs/mod-survey/findings/spid.md`).

```json
{
  "spidDistributions": [
    {
      "file": "MyFollowerPatch",
      "entries": [
        { "type": "Faction", "record": "0x000800~MyFollowerPatch.esp", "stringFilters": ["JJSofiaFollower"] },
        { "type": "Perk",    "record": "0xCF788~Skyrim.esm", "stringFilters": ["ActorTypeNPC"] },
        { "type": "Spell",   "record": "0x12FCD~Skyrim.esm", "levelFilters": "25/50", "traits": "F", "chance": 50 },
        { "type": "Item",    "record": "0xF~Skyrim.esm", "stringFilters": ["ActorTypeNPC", "-Nazeem"], "count": 3000 }
      ]
    }
  ]
}
```

→ writes `MyFollowerPatch_DISTR.ini` at the **mod folder root** (= `Data/`, *not* under `SKSE/Plugins`):

```ini
Faction = 0x000800~MyFollowerPatch.esp|JJSofiaFollower
Perk = 0xCF788~Skyrim.esm|ActorTypeNPC
Spell = 0x12FCD~Skyrim.esm|NONE|NONE|25/50|F|NONE|50
Item = 0xF~Skyrim.esm|ActorTypeNPC,-Nazeem|NONE|NONE|NONE|3000
```

### `spidDistributions[]`
| Field | Required | Meaning |
|---|---|---|
| `file` | ✅ | Output stem; `_DISTR.ini` suffix is added on emit (SPID requires it). |
| `entries` | | The distribution lines. |

### `spidDistributions[].entries[]`
Each entry is one line: `Type = RecordID│StringFilters│FormFilters│LevelFilters│Traits│TypeParam│Chance`.
**Trailing `NONE` fields are trimmed**; a gap before a later field is held open with `NONE`.

| Field | Line pos | Meaning |
|---|---|---|
| `type` | keyword | `Spell` `Perk` `Item` `Shout` `LevSpell` `Package` `Outfit` `SleepOutfit` `Keyword` `DeathItem` `Faction` `Skin`. **`SleepOutfit`/`Skin` must be explicit** — SPID can't infer them from the form type. |
| `record` | 1 | **Required.** `0xFormID~Plugin.esp` or an EditorID. Skyrim/DLC may drop the `~plugin` suffix. EditorID is merge-stable; prefer it. Cannot be `NONE`. |
| `stringFilters` | 2 | Array (OR'd). Keyword / actorbase EditorID / display name. `-x` excludes, `*x` partial-match, `a+b` requires both (AND in one expression). |
| `formFilters` | 3 | Array (OR'd). Faction/Race/Class/CombatStyle/Outfit/NPC_/Spell/VoiceType/FormList by FormID or EditorID. `-x` excludes. No wildcards here. |
| `levelFilters` | 4 | Raw string. Actor range `25/50`, `10/`, `/40`; or skill `SkillIndex(min/max)` e.g. `14(50/100)` (indices 0-17, see finding §4.3). |
| `traits` | 5 | Raw string. Letters joined by `/`: `M` `F` `U`(unique) `S`(summonable) `C`(child) `L`(leveled) `T`(teammate). `-` negates, e.g. `M/U`, `-C`. |
| `count` | 6 | **Item only** — item count. Ignored for other types. |
| `packageIndex` | 6 | **Package only** — package-stack insert index (0 = top). Ignored for other types. |
| `chance` | 7 | `0`-`100` distribution chance. **Non-unique NPCs only** (unique NPCs always 100). Omit → SPID defaults to 100. |

### Filter logic recap
- All filter fields on one line are **AND** (every field must pass).
- Comma-separated items within a field are **OR**; a `-`-prefixed item is an exclusion.
- `+`-joined `stringFilters` items are **AND** (the NPC must have all of them).

### When you do *not* need SPID
If the target NPC is defined in **your own** ESP, just set the faction/spell on the NPC record
directly — SPID's leverage is purely "patch *someone else's* NPC without an ESP override."

### Offline-validation note
`validate` checks structure only: `type` is in the allowed set, `record` is non-empty, `chance`
is 0-100. SPID resolves `RecordID`/`EditorID` against the **player's load order** at runtime, so
ModForge can't verify the form actually exists — that's a play-time concern, not a build error.
