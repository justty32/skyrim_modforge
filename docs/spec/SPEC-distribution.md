# ModForge spec — SKSE distributor configs

← [spec index](SPEC-index.md)

These sections emit **loose config files** (no ESP record) for SKSE distributor frameworks. They
let a mod attach records to other mods' NPCs/items **without an ESP patch** — the standard
compatibility layer for follower/NPC packs. ModForge writes the config; the framework's `.dll`
(player-supplied) does the runtime work.

Currently implemented: **SPID**, **MCM Helper**, **FLM**, **KID**, **BOS**, **AOS**, **SkyPatcher**
(roadmap D-group, see `workflows/roadmap/all-findings-gaps.md`).

---

## `spidDistributions` — SPID (Spell Perk Item Distributor)

SPID scans `Data/` for every `*_DISTR.ini` at start-up and, as each NPC loads, attaches matching
records to its actorbase by filter. Equivalent to editing the NPC in the CK, but with **no ESP
patch** and **no conflict** with other mods touching the same NPC.

Format + field semantics are verified against the SPID 7.3 reference and real ini files
(`../../analysis/mod-survey/findings/spid.md`).

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
**Mod Configuration Menu** page from a JSON file. **Requires MCM Helper + SkyUI** at runtime. Each config
emits two loose files **plus an ESP-side registration quest** (auto-generated — see below):

- `MCM/Config/<plugin-stem>/config.json` — the menu layout (**required**)
- `MCM/Config/<plugin-stem>/settings.ini` — the mod's default values

> ⚠️ **The folder name is the host plugin's filename stem, NOT the spec `modName`.** MCM Helper's DLL keys
> the config folder on `FormUtil::GetModName(quest)` = `path(pluginFilename).stem()` — it never reads the
> Papyrus `ModName` property for the lookup. A `MyMod.esp` plugin → `MCM/Config/MyMod/`. The spec `modName`
> only feeds the menu's `displayName` fallback; the emitted config.json `modName` field is set to the
> plugin stem (a self plugin-requirement). Getting this wrong makes MCM Helper read the wrong folder and
> show *"unknown error: check json syntax"* in-game (confirmed 2026-06-20).

**A loose config.json alone does NOT register a menu** — that was an earlier wrong assumption (it researched
the config.json *format* but not the *registration* step). MCM Helper requires, at minimum: a
Start-Game-Enabled `QUST` whose attached script extends `MCM_ConfigBase`, plus a player-forced
`PlayerAlias` carrying `SKI_PlayerLoadGameAlias`. **ModForge generates this automatically** for every
`mcmConfigs` entry. Ini-only menus carry the reusable `ModForgeMCM` script; a menu with a `global`
binding gets a generated per-menu subclass, and `package` compiles/ships it. The base path is ini-backed (`sourceType` =
`ModSettingBool`/`Int`/`Float`/`String`) — the player's edits persist to `MCM/Settings/<plugin-stem>.ini`
at runtime. A bool toggle may additionally set `global` to a GLOB ref: ModForge emits an
`action.CallFunction`, a Papyrus setter, and a VMAD GlobalVariable property so each edit mirrors 0/1 into
that GLOB. Raw `PropertyValue*` and hand-authored actions remain out of scope. The ini-only path was
verified in-game 2026-06-20; the new GLOB bridge is offline-verified and awaits one runtime check.

Because the directory is derived from the host plugin, one plugin can contain exactly one
`mcmConfigs` entry. `validate` and `package` reject additional entries instead of silently overwriting
the same `config.json` and `settings.ini`. A `global` bridge is required runtime code: if its generated
Papyrus subclass cannot compile, `package` fails before writing the ESP.

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

→ for a host plugin `MyMod.esp`: `MCM/Config/MyMod/config.json` (the layout, with `name`→`pageDisplayName`
and the value fields nested under `valueOptions`) + `MCM/Config/MyMod/settings.ini` + an auto-generated
`MF_MCM_*` registration quest in the ESP + `Scripts/ModForgeMCM.pex`:

```ini
[General]
bEnable=1
fMult=1.0
iDetail=1
```

### `mcmConfigs[]`
| Field | Required | Meaning |
|---|---|---|
| `modName` | ✅ | The menu's `displayName` fallback. **Does NOT name the folder** — the folder is the host plugin's filename stem (MCM Helper keys on the plugin name, not this field). |
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
| `global` | Optional GLOB ref for a `toggle` + `ModSettingBool`. The setting remains ini-persisted; a generated `CallFunction` setter mirrors each edit into the GLOB. `package` needs the normal MCM Helper + SkyUI Papyrus headers to compile the per-menu script. |
| `groupControl` | Int id — marks this control as a group toggle. |
| `groupCondition` | Int id (or `groupConditionNot:true` → `{"NOT": id}`) — show/hide driven by that group toggle. |
| `groupBehavior` | `disable` (grey out) or `skip` (hide) the dependent control. |
| `position` | Two-column forced column: `0` left / `1` right. |

### Offline-validation note
`validate` checks structure only: control `type` and `sourceType` are in the allowed sets, value
controls have a `"key:Section"` id, sliders have `min`+`max`, `stepper`/`enum` have `options`. The
**live menu can only be confirmed in-game** — ModForge writes the files; MCM Helper + SkyUI render them.

### Worked example: MCM switch → GLOB → perk condition

[`examples/mcm_global_perk.json`](../../examples/mcm_global_perk.json) declares `MF_BarterEnabled` once,
binds a bool toggle to it with `global`, then gates a `ModBuyPrices` perk effect on
`GetGlobalValue(MF_BarterEnabled) == 1`. `package` produces the persisted MCM setting, generated bridge
script, VMAD GLOB property, and perk CTDA as one coherent mod. Keep the GLOB non-constant: the setter must
be allowed to change it.

---

## `formListInjects` — FormList Manipulator (FLM, D-4)

[FormList Manipulator](https://www.nexusmods.com/skyrimspecialedition/mods/74037) (FLM) appends forms
to **any already-loaded FormList** — vanilla or another mod's — at runtime, with **no ESP override**, so
there is **zero conflict**. This is the no-conflict way to add your spell/item/NPC into someone else's
FLST pool (a Spellforge spell list, a SPID target list, an adoption-gift list…).

> **When NOT to use it:** for a FLST you *own*, build it ESP-side with `formLists[]` — that's
> deterministic and inspectable. FLM's leverage is purely "append to *someone else's* FLST without a
> patch." Format verified against FLM v1.8.1 (`../../analysis/mod-survey/findings/formlist-manipulator-*.md`).

Each config emits `<file>_FLM.ini` at the **mod folder root** (= `Data/`). Definitions
(`filters`/`aliases`/`groups`/`collections`) are emitted before the `entries` (the `FormList =` lines)
that reference them.

```json
{
  "formListInjects": [
    {
      "file": "MyFlmPatch",
      "filters": [ { "name": "HFFilter", "conditions": ["+HearthFires.esm"] } ],
      "aliases": [ { "name": "GiftLists", "items": ["BYOH...GiftChildMale", "BYOH...GiftChildFemale"] } ],
      "groups":  [ { "name": "Dolls", "items": ["BYOHChefDoll", "BYOHDBDoll"] } ],
      "collections": [ { "name": "IronWarAxes", "formType": "Weapon", "keywords": ["WeapTypeWarAxe", "WeapMaterialIron"] } ],
      "entries": [
        { "target": "#GiftLists", "forms": ["#Dolls"], "filter": "HFFilter" },
        { "target": "0x000800~SomeSpellMod.esp", "forms": ["0x000D62~MyFlmPatch.esp"] }
      ]
    }
  ]
}
```

→ writes `MyFlmPatch_FLM.ini` (no section header — a leading `[General]` makes FLM v1.8.1 log
`Config file is empty` and skip the whole file; verified in-game 2026-06-20):

```ini
Filter = HFFilter|+HearthFires.esm
Alias = GiftLists|BYOH...GiftChildMale, BYOH...GiftChildFemale
Group = Dolls|BYOHChefDoll, BYOHDBDoll
Collection = IronWarAxes|Weapon|WeapTypeWarAxe, WeapMaterialIron
FormList = #GiftLists|#Dolls|#HFFilter
FormList = 0x000800~SomeSpellMod.esp|0x000D62~MyFlmPatch.esp
```

### `formListInjects[]`
| Field | Required | Meaning |
|---|---|---|
| `file` | ✅ | Output stem; `_FLM.ini` suffix is added on emit (FLM scans `Data/` for `*_FLM.ini`). |
| `entries` | | The `FormList =` operation lines (see below). |
| `filters` / `aliases` / `groups` / `collections` | | Reusable definitions referenced by `entries`. |

### `entries[]` — the operations (`FormList = <FList>|<forms>|<Filter>`)
| Field | Meaning |
|---|---|
| `target` | The FormList to append to: an EditorID, `0xFormID~Plugin.esp`, or `#Alias` (a defined alias of several FLSTs). |
| `forms` | Tokens to add: a form ref, `*FormList` (expand its contents), `#Group`, or `#Collection`. |
| `filter` | Optional — a filter name (a leading `#` is added if absent); the line only applies when the filter passes. |

### Definitions
| Block | Shape | Meaning |
|---|---|---|
| `filters[]` | `{ name, conditions[] }` | `conditions` are OR'd; each is `+Plugin.esp` (must be active), `-Plugin.esp` (must be inactive), or `+A.esp&-B.esp` (AND in one). |
| `aliases[]` | `{ name, items[] }` | Bind several **target FormLists** into one name; reference as `#name` in an entry `target`. |
| `groups[]` | `{ name, items[] }` | A reusable **form set**; reference as `#name` in an entry `forms`. Items may themselves be refs / `*FormList` / `#Collection`. |
| `collections[]` | `{ name, formType, keywords[], filter? }` | Batch-select forms of one `formType` carrying **all** listed keywords (`-kw` excludes). `formType` ∈ Armor/Weapon/Ammo/MagicEffect/AlchemyItem/Scroll/Location/Ingredient/Book/Misc/Key/Soulgem/Activator/Flora/Furniture/Race/TalkingActivator/Enchantment/NPC/Spell. |

**Out of scope (MVP):** the `ModEvent =` runtime-dynamic line (needs a Papyrus sender) and the
specialized shortcut lines (`Plant`/`BToys`/`GToys`/`HairColors`/`AtronachForge`/…).

### Offline-validation note
`validate` checks structure only: `file`/`target` non-empty, each entry has `forms`, collection
`formType` is in the allowed set, filters have conditions. FLM resolves the target FLST / form refs
against the **player's load order** at runtime — ModForge can't verify they exist offline.

---

## `kidDistributions` — Keyword Item Distributor (KID, D-5)

[KID](https://www.nexusmods.com/skyrimspecialedition/mods/55728) attaches a **Keyword** to matching
records (Weapon/Armor/MagicEffect/…) by filter at start-up, with no ESP patch — for batch item tagging
(quality classes, keywords other frameworks then read). If the keyword's EditorID isn't found in any
loaded plugin, **KID creates a new KYWD** on the fly. Emits `<file>_KID.ini` at the mod root.

```json
{ "kidDistributions": [ { "file": "MyKidPatch", "entries": [
  { "keyword": "MysticismSpells", "type": "Magic Effect", "filters": ["MysticismMagic.esp"] },
  { "keyword": "0x1234~MyKidPatch.esp", "type": "Armor", "filters": ["*Iron"], "traits": "ArmorTypeHeavy+ArmorGauntlet,-E" },
  { "keyword": "MysticalAmmo", "type": "Ammo", "filters": ["*Bound"], "chance": 50 }
] } ] }
```

→ `Keyword = <keyword>|<type>|<strings_or_formIDs>|<traits>|<chance>` (trailing `NONE` trimmed, like SPID):
```ini
Keyword = MysticismSpells|Magic Effect|MysticismMagic.esp
Keyword = 0x1234~MyKidPatch.esp|Armor|*Iron|ArmorTypeHeavy+ArmorGauntlet,-E
Keyword = MysticalAmmo|Ammo|*Bound|NONE|50
```

| Field | Pos | Meaning |
|---|---|---|
| `keyword` | 1 | **Required.** The KYWD to distribute — EditorID / `0xFormID~Plugin.esp`. Unknown EditorID → KID creates a new KYWD. |
| `type` | 2 | **Required.** `Weapon` `Armor` `Ammo` `Magic Effect` `Potion` `Scroll` `Location` `Ingredient` `Book` `Misc Item` `Key` `Soul Gem` `Spell` `Activator` `Flora` `Furniture` `Race` `Talking Activator` `Enchantment`. |
| `filters` | 3 | Array (OR'd): name/EditorID/keyword; `+x` AND-requires, `-x` excludes, `*x` wildcard, `0x…~esp` FormID, `*x.nif` model path. Empty → all of that type. |
| `traits` | 4 | Raw string — type-specific (Armor `AR(10/50)`, Weapon `OneHandSword`, MGEF `20(0/25)`, Book `S,20`). |
| `chance` | 5 | `0.0`–`100.0` (omit → 100). |

---

## `objectSwaps` — Base Object Swapper (BOS, D-6)

[Base Object Swapper](https://www.nexusmods.com/skyrimspecialedition/mods/49669) replaces a base object
with another when a reference loads, with no ESP override (zero conflict) — for scene dressing (swap a
vanilla clutter form for a richer one, gate by location). MVP = the `[Forms]` section. Emits
`<file>_SWAP.ini` at the mod root.

```json
{ "objectSwaps": [ { "file": "MySwapPatch", "groups": [
  { "entries": [
    { "base": "0x10C0E3~Skyrim.esm", "swaps": ["0x806~MySwapPatch.esp"] },
    { "base": "0x10ACC2~Skyrim.esm", "swaps": ["0x81F~MySwapPatch.esp", "0x820~MySwapPatch.esp"], "properties": "scale(1.2/1.2)", "chance": 50 }
  ] },
  { "conditions": ["WhiterunLocation", "-AzuraShrineLocation"], "entries": [
    { "base": "0x8E48~Dawnguard.esm", "swaps": ["0x897~MySwapPatch.esp"] }
  ] }
] } ] }
```

→ `[Forms]` (or `[Forms|cond,…]`) then `baseFormID|swapFormID[|properties][|chance]` (a gap stays `||`):
```ini
[Forms]
0x10C0E3~Skyrim.esm|0x806~MySwapPatch.esp
0x10ACC2~Skyrim.esm|0x81F~MySwapPatch.esp,0x820~MySwapPatch.esp|scale(1.2/1.2)|50

[Forms|WhiterunLocation,-AzuraShrineLocation]
0x8E48~Dawnguard.esm|0x897~MySwapPatch.esp
```

| Field | Meaning |
|---|---|
| `groups[].conditions` | Optional `[Forms|c1,c2]` filter — Location/Region/Keyword/Cell/Worldspace by EditorID/FormID; `-x` excludes. Empty → unconditional `[Forms]`. |
| `entries[].base` | **Required.** The base form to replace (`0xFormID~Plugin.esp` / EditorID). |
| `entries[].swaps` | **Required.** Replacement form(s); several → BOS picks one at random per reference. |
| `entries[].properties` | Optional raw transform string, e.g. `scale(1.2/1.2),rot(0/0,0/0,45/45)`. |
| `entries[].chance` | `0.0`–`100.0` (omit → always). |

> **Out of scope (MVP):** the standalone `[Properties]` (transform without a swap) and `[References]`
> (per-reference instance) sections.

---

## `animObjectSwaps` — AnimObject Swapper (AOS, D-7)

AnimObject Swapper swaps the **prop an actor holds during an idle** (a mug, a book) by condition, with
no ESP override — for follower/role characterization. Swaps the *held object*, not the animation (that's
OAR). Emits `<file>_ANIO.ini` at the mod root.

```json
{ "animObjectSwaps": [ { "file": "MyAnioPatch", "entries": [
  { "base": "DrinkingCupANIO", "swaps": ["WoodCupANIO", "MeadHornANIO", "GlassCupANIO"] },
  { "base": "DrinkingCupANIO", "swaps": ["SofiaSpecialMugANIO"], "filters": ["SofiaFollower"] },
  { "base": "BookReadingANIO", "swaps": ["ThievesGuildLedgerANIO"], "filters": ["+ThievesGuildFaction"], "traits": "F" }
] } ] }
```

→ `[BaseANIO|FILTERS|TRAITS]` header then `baseANIO|swap1,swap2,…` (several swaps → random pick):
```ini
[DrinkingCupANIO]
DrinkingCupANIO|WoodCupANIO,MeadHornANIO,GlassCupANIO
[DrinkingCupANIO|SofiaFollower]
DrinkingCupANIO|SofiaSpecialMugANIO
[BookReadingANIO|+ThievesGuildFaction|F]
BookReadingANIO|ThievesGuildLedgerANIO
```

| Field | Meaning |
|---|---|
| `base` | **Required.** The original ANIO to swap (`0xFormID~Plugin.esp` / EditorID). |
| `swaps` | **Required.** Replacement ANIO(s); several → AOS picks one at random per idle. |
| `filters` | Optional FILTERS header segment: NPC base/Faction/Race/Keyword/Spell/Location ref; `+x` AND, `-x` excludes, `*x` string-match. |
| `traits` | Optional TRAITS segment: `M`/`F` sex, `C`/`-C` child. A traits-only header holds the filter slot open (`[Base||F]`). |

---

## `skyPatchers` — SkyPatcher (D-3)

[SkyPatcher](https://www.nexusmods.com/skyrimspecialedition/mods/108591) applies **in-memory runtime
edits to records by filter**, with no ESP override — the no-conflict way to mass-edit vanilla
NPCs/armor/weapons or inject into leveled lists. Emits `SKSE/Plugins/SkyPatcher/<recordType>/<file>.ini`
with **flat lines, no `[section]` headers**.

```json
{ "skyPatchers": [
  { "file": "npc-patch", "recordType": "npc", "patches": [
    { "filters": [ { "key": "filterByRaces", "value": "NordRace" } ],
      "mods": [ { "key": "spellsToAdd", "value": "MagicResistance50" }, { "key": "perksToAdd", "value": "HalfCostSpells" } ] }
  ] },
  { "file": "loot-inject", "recordType": "leveledList", "patches": [
    { "filters": [ { "key": "filterByLists", "value": "DeathItemBanditMelee" } ],
      "mods": [ { "key": "objectsToAdd", "value": "0x000800~MySkyPatch.esp" } ] }
  ] }
] }
```

→ `filterKey=value:…:modKey=value:…` (filters AND'd first, then mods; colon-delimited, no spaces):
```ini
; SkyPatcher/npc/npc-patch.ini
filterByRaces=NordRace:spellsToAdd=MagicResistance50:perksToAdd=HalfCostSpells
; SkyPatcher/leveledList/loot-inject.ini
filterByLists=DeathItemBanditMelee:objectsToAdd=0x000800~MySkyPatch.esp
```

| Field | Meaning |
|---|---|
| `file` | **Required.** Output stem → `SkyPatcher/<recordType>/<file>.ini`. |
| `recordType` | **Required.** The record-type folder: `npc` `armor` `weapon` `ammo` `leveledList` `formList` `race` `container`. |
| `patches[].filters` | `{ key, value }` pairs (all must match). Common keys: `filterByRaces`, `filterByKeywords`, `filterByNpcs`, `filterByLists`. A key may repeat (list it twice). |
| `patches[].mods` | `{ key, value }` pairs — the edits. Common keys: `spellsToAdd`, `perksToAdd`, `keywordsToAdd`, `objectsToAdd`/`objectsToRemove`. **At least one required.** |

> **MVP note:** ModForge emits whatever `key`/`value` pairs you give verbatim — it does **not** whitelist
> the (very large) SkyPatcher field set. See the SkyPatcher docs for every filter/field per record type.

### Offline-validation note (KID / BOS / AOS / SkyPatcher)
All four are checked for structure only (required names, known type/recordType, non-empty swap/mod
lists). Every framework resolves its form refs against the **player's load order** at runtime, so the
actual effect is a play-time concern — confirm in-game (SkyPatcher/BOS/AOS have debug logs).
