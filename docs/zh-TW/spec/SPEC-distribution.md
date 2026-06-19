# ModForge spec — SKSE 分發器設定

← [spec index](SPEC-index.md)

這些區塊會發出**鬆散的設定檔**（沒有 ESP 記錄）供 SKSE 分發器框架使用。它們
讓一個 mod 能把記錄附加到其他 mod 的 NPC/物品上，而**無須 ESP patch**——這是
follower/NPC 包的標準相容層。ModForge 寫出設定檔；框架的 `.dll`（玩家自備）負責執行期的工作。

目前已實作：**SPID**、**MCM Helper**、**FLM**。KID / SkyPatcher 遵循同樣的鬆散檔
模式（roadmap D 組，見 `workflows/roadmap/all-findings-gaps.md`）。

---

## `spidDistributions` — SPID (Spell Perk Item Distributor)

SPID 在啟動時掃描 `Data/` 中每一個 `*_DISTR.ini`，並在每個 NPC 載入時，依過濾器把相符的
記錄附加到它的 actorbase 上。等同於在 CK 中編輯該 NPC，但**沒有 ESP
patch**，也**不會**與其他更動同一 NPC 的 mod **衝突**。

格式與欄位語意已對照 SPID 7.3 參考文件與真實的 ini 檔驗證
（`sub_projs/mod-survey/findings/spid.md`）。

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

→ 在**mod 資料夾根目錄**（＝ `Data/`，*而非* 在 `SKSE/Plugins` 下）寫出 `MyFollowerPatch_DISTR.ini`：

```ini
Faction = 0x000800~MyFollowerPatch.esp|JJSofiaFollower
Perk = 0xCF788~Skyrim.esm|ActorTypeNPC
Spell = 0x12FCD~Skyrim.esm|NONE|NONE|25/50|F|NONE|50
Item = 0xF~Skyrim.esm|ActorTypeNPC,-Nazeem|NONE|NONE|NONE|3000
```

### `spidDistributions[]`
| Field | Required | Meaning |
|---|---|---|
| `file` | ✅ | 輸出檔名主幹；`_DISTR.ini` 後綴會在發出時加上（SPID 要求它）。 |
| `entries` | | 各分發行。 |

### `spidDistributions[].entries[]`
每一個 entry 是一行：`Type = RecordID│StringFilters│FormFilters│LevelFilters│Traits│TypeParam│Chance`。
**結尾的 `NONE` 欄位會被裁掉**；若某個較後的欄位前有空缺，則以 `NONE` 撐開保留。

| Field | Line pos | Meaning |
|---|---|---|
| `type` | keyword | `Spell` `Perk` `Item` `Shout` `LevSpell` `Package` `Outfit` `SleepOutfit` `Keyword` `DeathItem` `Faction` `Skin`。**`SleepOutfit`/`Skin` 必須明確指定**——SPID 無法從 form type 推斷它們。 |
| `record` | 1 | **必填。** `0xFormID~Plugin.esp` 或一個 EditorID。Skyrim/DLC 可省略 `~plugin` 後綴。EditorID 在 merge 時較穩定；偏好它。不可為 `NONE`。 |
| `stringFilters` | 2 | 陣列（OR）。Keyword／actorbase EditorID／顯示名稱。`-x` 排除、`*x` 部分比對、`a+b` 兩者皆須（一個運算式內的 AND）。 |
| `formFilters` | 3 | 陣列（OR）。以 FormID 或 EditorID 指定的 Faction/Race/Class/CombatStyle/Outfit/NPC_/Spell/VoiceType/FormList。`-x` 排除。此處無萬用字元。 |
| `levelFilters` | 4 | 原始字串。Actor 範圍 `25/50`、`10/`、`/40`；或技能 `SkillIndex(min/max)` 例如 `14(50/100)`（索引 0-17，見 finding §4.3）。 |
| `traits` | 5 | 原始字串。以 `/` 連接的字母：`M` `F` `U`(unique) `S`(summonable) `C`(child) `L`(leveled) `T`(teammate)。`-` 否定，例如 `M/U`、`-C`。 |
| `count` | 6 | **僅 Item** ——物品數量。其他類型忽略。 |
| `packageIndex` | 6 | **僅 Package** ——package 堆疊插入索引（0 = 最上）。其他類型忽略。 |
| `chance` | 7 | `0`-`100` 分發機率。**僅限非 unique NPC**（unique NPC 一律 100）。省略 → SPID 預設為 100。 |

### 過濾器邏輯回顧
- 同一行上的所有過濾欄位之間是 **AND**（每個欄位都必須通過）。
- 一個欄位內以逗號分隔的項目之間是 **OR**；以 `-` 為前綴的項目是排除。
- `stringFilters` 中以 `+` 連接的項目之間是 **AND**（NPC 必須擁有它們全部）。

### 你*不*需要 SPID 的時候
若目標 NPC 定義在**你自己的** ESP 中，直接在該 NPC 記錄上設定 faction/spell
即可——SPID 的價值純粹在於「在不用 ESP override 的情況下 patch *別人的* NPC」。

### 離線驗證注意事項
`validate` 只檢查結構：`type` 在允許集合中、`record` 非空、`chance`
在 0-100 之間。SPID 在執行期才會對**玩家的載入順序**解析 `RecordID`/`EditorID`，所以
ModForge 無法驗證該 form 是否真的存在——那是遊玩時的事，不是 build 錯誤。

---

## `mcmConfigs` — MCM Helper 設定選單 (D-2)

[MCM Helper](https://www.nexusmods.com/skyrimspecialedition/mods/53000)（Parapets）從一個 JSON 檔
渲染一個遊戲內的 **Mod Configuration Menu** 頁面——無須 Papyrus、無須 SkyUI 腳本。每個 config 發出兩個
鬆散檔：

- `MCM/Config/<modName>/config.json` — 選單版面（**必填**）
- `MCM/Config/<modName>/settings.ini` — 該 mod 的預設值

**MVP ＝ ini-backed 路徑。** 那些 `sourceType` 為 `ModSettingBool`/`Int`/`Float`/`String`
的控制項，完全由 `MCMHelper.dll` 處理，**無須 Quest 記錄、無須 Papyrus**——玩家的編輯會在執行期
持久化到 `MCM/Settings/<modName>.ini`。（進階的 `PropertyValue*` / `action.CallFunction` 路徑
需要一個衍生自 `MCM_ConfigBase` 的 Quest 腳本，刻意**不在範圍內**——`validate`
會拒絕那些 sourceType。）格式已對照 `sub_projs/mod-survey/findings/mcm-helper-config-json.md` 驗證。

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

→ `MCM/Config/MyMod/config.json`（版面，其中 `name`→`pageDisplayName`，值欄位巢狀
在 `valueOptions` 之下）＋ `MCM/Config/MyMod/settings.ini`：

```ini
[General]
bEnable=1
fMult=1.0
iDetail=1
```

### `mcmConfigs[]`
| Field | Required | Meaning |
|---|---|---|
| `modName` | ✅ | 命名 `MCM/Config/<modName>/` 資料夾與 MCM 身分鍵。 |
| `displayName` | | 左側清單標籤。支援 `$TranslationKey`。 |
| `pages` | ✅ | 各選單分頁。 |

### `mcmConfigs[].pages[]`
| Field | Required | Meaning |
|---|---|---|
| `name` | ✅ | 分頁標籤（發出為 `pageDisplayName`）。支援 `$TranslationKey`。 |
| `cursorFillMode` | | `topToBottom`（預設）或 `leftToRight`（雙欄）。 |
| `content` | | 控制項清單。 |

### `mcmConfigs[].pages[].content[]`
| Field | Meaning |
|---|---|
| `type` | `toggle` `hiddenToggle` `slider` `stepper` `enum` `keymap` `header` `empty`。`header`/`empty` 不帶值。 |
| `id` | `"key:Section"` ——ini 鍵＋值所存放的 `[Section]`。**任何帶 `sourceType` 的控制項皆必填。** |
| `text` | 顯示標籤。支援 `$Key` 與 `{value}` 插值。 |
| `help` | 滑鼠懸停提示。 |
| `sourceType` | `ModSettingBool` \| `ModSettingInt` \| `ModSettingFloat` \| `ModSettingString`（ini-backed 集合）。 |
| `min`/`max`/`step` | Slider 範圍／步進。一個 `slider` 需要 `min` 與 `max` 兩者。 |
| `formatString` | Slider 顯示，例如 `"{0} s"`（int）／ `"{1}"`（float）。 |
| `options` | `stepper`/`enum` 的選項標籤（int 值是進入此清單的索引）。這兩種類型必填。 |
| `shortNames` | `enum` 的短顯示名稱。 |
| `defaultBool` / `defaultNumber` / `defaultString` | 預設值；讀取哪一個由 `sourceType` 決定（Bool→`defaultBool`、Int/Float→`defaultNumber`、String→`defaultString`）。同時驅動 `config.json` 的 `defaultValue` 與 `settings.ini` 的那一行。 |
| `groupControl` | Int id ——標記此控制項為一個群組開關。 |
| `groupCondition` | Int id（或 `groupConditionNot:true` → `{"NOT": id}`）——顯示／隱藏由該群組開關驅動。 |
| `groupBehavior` | `disable`（灰掉）或 `skip`（隱藏）相依的控制項。 |
| `position` | 雙欄強制欄位：`0` 左／ `1` 右。 |

### 離線驗證注意事項
`validate` 只檢查結構：控制項的 `type` 與 `sourceType` 在允許集合中、值
控制項有一個 `"key:Section"` id、slider 有 `min`＋`max`、`stepper`/`enum` 有 `options`。
**實際選單只能在遊戲內確認**——ModForge 寫出檔案；MCM Helper ＋ SkyUI 負責渲染它們。

---

## `formListInjects` — FormList Manipulator (FLM, D-4)

[FormList Manipulator](https://www.nexusmods.com/skyrimspecialedition/mods/74037)（FLM）在執行期把 form
追加進**任何已載入的 FormList**——vanilla 或別的 mod 的——且**不需要 ESP override**，因此**零衝突**。
這是把你自己的 spell/item/NPC 加進別人 FLST 池（Spellforge 法術清單、SPID 分發目標清單、領養禮物
清單…）的零衝突方式。

> **何時*不該*用：** 對於你*自己擁有*的 FLST，用 esp-side 的 `formLists[]` 建——那是確定且可檢視的。
> FLM 的價值純粹在於「在不用 patch 的情況下追加到*別人的* FLST」。格式已對照 FLM v1.8.1 驗證
> （`sub_projs/mod-survey/findings/formlist-manipulator-*.md`）。

每個 config 在 **mod 資料夾根目錄**（＝ `Data/`）發出 `<file>_FLM.ini`。定義
（`filters`/`aliases`/`groups`/`collections`）會在引用它們的 `entries`（`FormList =` 行）之前發出。

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

→ 寫出 `MyFlmPatch_FLM.ini`：

```ini
[General]
Filter = HFFilter|+HearthFires.esm
Alias = GiftLists|BYOH...GiftChildMale, BYOH...GiftChildFemale
Group = Dolls|BYOHChefDoll, BYOHDBDoll
Collection = IronWarAxes|Weapon|WeapTypeWarAxe, WeapMaterialIron
FormList = #GiftLists|#Dolls|#HFFilter
FormList = 0x000800~SomeSpellMod.esp|0x000D62~MyFlmPatch.esp
```

### `formListInjects[]`
| 欄位 | 必填 | 意義 |
|---|---|---|
| `file` | ✅ | 輸出檔名主幹；`_FLM.ini` 後綴會在發出時加上（FLM 掃描 `Data/` 找 `*_FLM.ini`）。 |
| `entries` | | `FormList =` 操作行（見下）。 |
| `filters` / `aliases` / `groups` / `collections` | | 供 `entries` 引用的可重用定義。 |

### `entries[]` — 操作（`FormList = <FList>|<forms>|<Filter>`）
| 欄位 | 意義 |
|---|---|
| `target` | 要追加進的 FormList：EditorID、`0xFormID~Plugin.esp`、或 `#Alias`（多個 FLST 的別名）。 |
| `forms` | 要加入的 token：form ref、`*FormList`（展開其內容）、`#Group`、或 `#Collection`。 |
| `filter` | 可選——filter 名稱（缺前導 `#` 會自動補上）；此行僅在 filter 通過時套用。 |

### 定義
| 區塊 | 形狀 | 意義 |
|---|---|---|
| `filters[]` | `{ name, conditions[] }` | `conditions` 為 OR；每個是 `+Plugin.esp`（須啟用）、`-Plugin.esp`（須未啟用）、或 `+A.esp&-B.esp`（單一條件內 AND）。 |
| `aliases[]` | `{ name, items[] }` | 把多個**目標 FormList** 綁成一個名稱；在 entry 的 `target` 用 `#name` 引用。 |
| `groups[]` | `{ name, items[] }` | 可重用的**form 集合**；在 entry 的 `forms` 用 `#name` 引用。items 本身可為 ref / `*FormList` / `#Collection`。 |
| `collections[]` | `{ name, formType, keywords[], filter? }` | 批次選取單一 `formType` 且帶有**全部**列出 keyword 的 form（`-kw` 排除）。`formType` ∈ Armor/Weapon/Ammo/MagicEffect/AlchemyItem/Scroll/Location/Ingredient/Book/Misc/Key/Soulgem/Activator/Flora/Furniture/Race/TalkingActivator/Enchantment/NPC/Spell。 |

**範圍外（MVP）：** `ModEvent =` 執行期動態行（需 Papyrus 發送端）與特化快捷行
（`Plant`/`BToys`/`GToys`/`HairColors`/`AtronachForge`/…）。

### 離線驗證注意事項
`validate` 只檢查結構：`file`/`target` 非空、每個 entry 有 `forms`、collection 的
`formType` 在允許集合中、filter 有 conditions。FLM 在執行期才對**玩家的載入順序**解析目標 FLST／
form ref——ModForge 離線無法驗證它們是否存在。
