# 做什麼 + _DISTR.ini 完整語法 + Type 全集表

← [spid](spid.md)

## 一、SPID 做什麼 + 工作原理

SPID 是「**無 ESP patch、用 config 分發記錄到 NPC**」的 SKSE plugin。

### 工作原理

1. **讀取時機**：遊戲啟動、載入 SKSE 時（非 runtime），SPID 掃描 `Data/` 資料夾，找所有後綴為 `_DISTR` 的 `.ini` 檔（如 `MyMod_DISTR.ini`）。
2. **排序**：依檔名字母 A→Z 排序後逐一讀取。
3. **分發**：遊戲開始載入時，SPID 對每個 NPC 的 actorbase 逐條評估 ini，符合條件的記錄就直接分發到那個 NPC——等同在 CK 裡對 NPC 加上 spell / perk / faction 等，但不需要 ESP。
4. **運行一次**：SPID 在 start-up 時分發，之後不會再掃描（除非重啟遊戲）。Outfit 分發在 7.2+ 引入 per-actor 追蹤，有動態更新機制（見下）。

### 命名慣例

- 檔案必須放在 `Data/` 或 MO2 mod 資料夾的根目錄（安裝後對應 `Data/`）。
- 後綴必須是 `_DISTR.ini`，例如 `MyMod_DISTR.ini`、`nwsFF_SkillBoostsPerks_DISTR.ini`。
- 檔名不需要對應任何 ESP；一個 mod 可以有多個 `_DISTR.ini`。

---

## 二、`_DISTR.ini` 完整語法

### 2.1 通用行格式

```
Type = RecordID|StringFilters|FormFilters|LevelFilters|Traits|NONE|Chance
```

**Item** 與 **Package** 有第六欄位專用值：

```
Item    = RecordID|StringFilters|FormFilters|LevelFilters|Traits|ItemCount|Chance
Package = RecordID|StringFilters|FormFilters|LevelFilters|Traits|PackageIdx|Chance
```

所有其他 type 的第六欄位是 `NONE`（可省略，寫 `NONE` 或留空效果相同）。

### 2.2 各欄位說明

| 欄位 | 位置 | 說明 | 允許值 / 格式 |
|---|---|---|---|
| `Type` | 行首關鍵字 | 分發的記錄類型 | 見「三、type 全集」 |
| `RecordID` | 第 1 欄 | 要分發的 form | `0xFormID~Plugin.esp` 或 EditorID |
| `StringFilters` | 第 2 欄 | 字串篩選（NPC 名、EditorID、Keyword） | 字串列表；NONE=跳過 |
| `FormFilters` | 第 3 欄 | Form 篩選（Faction、Race、Class 等） | FormID 或 EditorID 列表；NONE=跳過 |
| `LevelFilters` | 第 4 欄 | 等級或技能篩選 | 等級範圍或技能語法；NONE=跳過 |
| `Traits` | 第 5 欄 | 特質篩選（性別、唯一、召喚等） | 字母組合；NONE=跳過 |
| 第 6 欄 | 型別參數 | 依 Type 而異：一般為 NONE，Item=數量，Package=插入位置 | `NONE` / 正整數 |
| `Chance` | 第 7 欄 | 分發機率 0-100（只對非 unique NPC 生效） | 整數；省略或 NONE=100 |

### 2.3 NONE 的語義

- `NONE` 代表「**跳過這個篩選條件**」，即不限制。
- 任何欄位都可以寫 `NONE` 或留空（兩者等效）。
- 例：`Perk = ActorTypeNPC|NONE|NONE|NONE|NONE|NONE|NONE` 等同 `Perk = ActorTypeNPC`（只用 EditorID 做 StringFilter，其他不限）。
- **注意**：RecordID 欄不能是 NONE（必須指定要分發的 form）。

### 2.4 RecordID 格式

**FormID 格式**：
```
0x12345~MyPlugin.esp
0x12345~MyPlugin.esl
0x12345~MyPlugin.esm
```
- `0x` 前綴的十六進位 FormID，去掉 load-order prefix（最高兩位）。
- `~` 後接 plugin 檔名（含副檔名）。
- Skyrim 原生 / DLC 記錄不需要加 `~Skyrim.esm`（可省略 plugin suffix）。

**EditorID 格式**：
```
ActorTypeNPC
BalgruufTheGreater
WW42GYHSofialikeFollowerDialogueFixFaction
```
- 直接寫 EditorID，不加引號。
- EditorID 在 mod merge 後不會改變，比 FormID 更穩定。
- 若 EditorID 不唯一（多 mod 同名），SPID 會記錄警告。

---

## 三、Type 全集表

| Type 關鍵字 | 分發到 | 分發的記錄型別 | 備註 |
|---|---|---|---|
| `Spell` | NPC actorbase | SPEL（Spell）| 含 ability、power、lesser power |
| `Perk` | NPC actorbase | PERK | NPC 的被動能力 / 行為修改 |
| `Item` | NPC inventory | ARMO / WEAP / MISC / ALCH / BOOK / AMMO / INGR / SLGM / SCRL | 第 6 欄 = 數量（預設 1）|
| `Shout` | NPC actorbase | SHOU | 龍吼 |
| `LevSpell` | NPC actorbase | LVSP（Leveled Spell）| 分發一個 leveled spell list |
| `Package` | NPC actorbase package stack | PACK | 第 6 欄 = PackageIdx：插入位置（0=最頂，預設 0）；若 RecordID 是 FormList 則 0-4 對應 override list 類別 |
| `Outfit` | NPC default outfit | OTFT | 7.2+ per-actor 追蹤；第一條符合的 config 行優先，其餘跳過 |
| `SleepOutfit` | NPC sleep outfit | OTFT | 同 Outfit 型別，但替換 sleep outfit；**必須寫 `SleepOutfit`，不能讓 SPID 自動推斷**（否則會誤判為 Outfit） |
| `Keyword` | NPC actorbase | KYWD | 給 NPC 本身加 keyword（非給裝備）|
| `DeathItem` | NPC 死亡掉落 | LVLI（Leveled Item）| NPC 死時額外掉落 |
| `Faction` | NPC faction 清單 | FACT | 讓 NPC 加入一個派系 |
| `Skin` | NPC skin override | ARMO | 替換 NPC 的 skin（視覺模型）；**必須明確寫 `Skin`**，否則 SPID 會誤判為 Item |

> `SleepOutfit` 和 `Skin` 因為底層 form 型別與 `Outfit` 和 `Item` 相同，**必須明確指定 Type**，SPID 無法自動推斷。

---

