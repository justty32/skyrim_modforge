# ModForge spec — SKSE distributor configs

← [spec index](SPEC-index.md)

These sections emit **loose config files** (no ESP record) for SKSE distributor frameworks. They
let a mod attach records to other mods' NPCs/items **without an ESP patch** — the standard
compatibility layer for follower/NPC packs. ModForge writes the config; the framework's `.dll`
(player-supplied) does the runtime work.

Currently implemented: **SPID**, **MCM Helper**. KID / SkyPatcher / FLM follow the same loose-file
pattern (roadmap D-group, see `workflows/roadmap/all-findings-gaps.md`).

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

---

## `mcmConfigs` — MCM Helper settings menu (D-2)

[MCM Helper](https://www.nexusmods.com/skyrimspecialedition/mods/53000) (Parapets) renders an in-game
**Mod Configuration Menu** page from a JSON file — no Papyrus, no SkyUI scripting. Each config emits two
loose files:

- `MCM/Config/<modName>/config.json` — the menu layout (**required**)
- `MCM/Config/<modName>/settings.ini` — the mod's default values

**MVP = the ini-backed path.** Controls whose `sourceType` is `ModSettingBool`/`Int`/`Float`/`String`
are fully handled by `MCMHelper.dll` with **no Quest record and no Papyrus** — the player's edits persist
to `MCM/Settings/<modName>.ini` at runtime. (The advanced `PropertyValue*` / `action.CallFunction` path
needs a Quest script extending `MCM_ConfigBase` and is intentionally **out of scope** — `validate`
rejects those sourceTypes.) Format verified against `sub_projs/mod-survey/findings/mcm-helper-config-json.md`.

```json
{
  "mcmConfigs": [
    {
      "modName": "MyMod",
      "displayName": "My Mod",
      "pages": [
        { "name": "General", "content": [
          { "type": "header", "text": "Features" },
          { "type": "toggle", "text": "Enable", "id": "bEnable:General",
            "sourceType": "ModSettingBool", "defaultBool": true },
          { "type": "slider", "text": "Multiplier", "id": "fMult:General",
            "sourceType": "ModSettingFloat", "min": 0.5, "max": 3.0, "step": 0.1, "defaultNumber": 1.0 },
          { "type": "enum", "text": "Detail", "id": "iDetail:General",
            "sourceType": "ModSettingInt", "options": ["Low","Medium","High"], "defaultNumber": 1 }
        ] }
      ]
    }
  ]
}
```

→ `MCM/Config/MyMod/config.json` (the layout, with `name`→`pageDisplayName` and the value fields nested
under `valueOptions`) + `MCM/Config/MyMod/settings.ini`:

```ini
[General]
bEnable=1
fMult=1.0
iDetail=1
```

### `mcmConfigs[]`
| Field | Required | Meaning |
|---|---|---|
| `modName` | ✅ | Names the `MCM/Config/<modName>/` folder and the MCM identity key. |
| `displayName` | | Left-list label. Supports a `$TranslationKey`. |
| `pages` | ✅ | The menu tabs. |

### `mcmConfigs[].pages[]`
| Field | Required | Meaning |
|---|---|---|
| `name` | ✅ | Tab label (emitted as `pageDisplayName`). Supports a `$TranslationKey`. |
| `cursorFillMode` | | `topToBottom` (default) or `leftToRight` (two-column). |
| `content` | | The control list. |

### `mcmConfigs[].pages[].content[]`
| Field | Meaning |
|---|---|
| `type` | `toggle` `hiddenToggle` `slider` `stepper` `enum` `keymap` `header` `empty`. `header`/`empty` carry no value. |
| `id` | `"key:Section"` — the ini key + `[Section]` the value is stored under. **Required for any control with a `sourceType`.** |
| `text` | Display label. Supports `$Key` and `{value}` interpolation. |
| `help` | Hover tooltip. |
| `sourceType` | `ModSettingBool` \| `ModSettingInt` \| `ModSettingFloat` \| `ModSettingString` (the ini-backed set). |
| `min`/`max`/`step` | Slider range/step. A `slider` needs both `min` and `max`. |
| `formatString` | Slider display, e.g. `"{0} s"` (int) / `"{1}"` (float). |
| `options` | `stepper`/`enum` option labels (the int value is an index into this). Required for those types. |
| `shortNames` | `enum` short display names. |
| `defaultBool` / `defaultNumber` / `defaultString` | The default; which is read is decided by `sourceType` (Bool→`defaultBool`, Int/Float→`defaultNumber`, String→`defaultString`). Drives both `config.json` `defaultValue` and the `settings.ini` line. |
| `groupControl` | Int id — marks this control as a group toggle. |
| `groupCondition` | Int id (or `groupConditionNot:true` → `{"NOT": id}`) — show/hide driven by that group toggle. |
| `groupBehavior` | `disable` (grey out) or `skip` (hide) the dependent control. |
| `position` | Two-column forced column: `0` left / `1` right. |

### Offline-validation note
`validate` checks structure only: control `type` and `sourceType` are in the allowed sets, value
controls have a `"key:Section"` id, sliders have `min`+`max`, `stepper`/`enum` have `options`. The
**live menu can only be confirmed in-game** — ModForge writes the files; MCM Helper + SkyUI render them.
