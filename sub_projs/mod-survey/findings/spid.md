# SPID：Spell Perk Item Distributor 深挖 Finding

- 版本：7.3.0（本地解壓：`Spell Perk Item Distributor-36869-7-3-0-1778353486`）
- 作者：powerofthree
- Nexus：[36869](https://www.nexusmods.com/skyrimspecialedition/mods/36869)
- 類型：SKSE plugin（`.dll`），SSE / AE 各有一份

---

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

## 四、Filter 語法詳解

### 4.1 StringFilters（第 2 欄）

接受：**Keyword EditorID**、**NPC actorbase EditorID**、**NPC 顯示名稱（Name）**。

多個字串用逗號 `,` 分隔。

**修飾子（modifier）**——每個「expression」只能用一種：

| 修飾子 | 語義 | 範例 |
|---|---|---|
| （無） | 精確比對（exact match）| `Lydia` — 只對名字 = Lydia 的 NPC |
| `-` 前綴 | 排除（exclusion）— NPC 不符合才通過 | `-Nazeem` — 排除 Nazeem |
| `*` 前綴 | 前綴/部分比對（partial match）| `*Guard` — 名字含 Guard 的 NPC |
| `+` 連接 | 複合條件（AND）— NPC 必須同時符合全部 | `ActorTypeGhost+Vampire` — 必須是 ghost 且 vampire |

**範例**：
```ini
; 所有 ActorTypeNPC（精確 keyword）
Perk = 0xCF788~Skyrim.esm|ActorTypeNPC

; 分發給 ActorTypeNPC，但排除 Nazeem
Item = 0xF~Skyrim.esm|ActorTypeNPC,-Nazeem

; 名字含 Bandit 的 NPC
Spell = 0x1A6CC~Skyrim.esm|*Bandit

; 同時是 ghost 且是 vampire 的 NPC
Shout = 0x13E07~Skyrim.esm|ActorTypeGhost+Vampire
```

> **注意**：`*` 只在 StringFilter 有效，FormFilter 不支援萬用字元。

### 4.2 FormFilters（第 3 欄）

接受：特定 form 的 FormID 或 EditorID，以逗號 `,` 分隔。

**可接受的 form 型別**：
- `Faction`（FACT）
- `Race`（RACE）
- `Class`（CLAS）
- `CombatStyle`（CSTY）
- `Outfit`（OTFT）
- `NPC_`（NPC，指定特定 actorbase）
- `Spell`（SPEL）
- `VoiceType`（VTYP）
- `FormList`（FLST）
- `EditorLocation`（位置）

**排除**：同樣用 `-` 前綴排除某個 form：`-0x101~MyMod.esp`。

**範例**：
```ini
; 分發給屬於 BanditFaction 的 NPC
Perk = MyPerk|NONE|BanditFaction

; 分發給 Nord 種族 NPC
Spell = MySpell|NONE|NordRace

; 分發給某 FormList 內的 NPC
Spell = MySpell|NONE|0x123~MyMod.esp

; 排除某 faction
Faction = MyFact|NONE|-EnemyFaction
```

### 4.3 LevelFilters（第 4 欄）

兩種子語法：**Actor 等級**與**技能等級**。

#### Actor 等級範圍

```
min/max      ; 等級在 min 到 max 之間（含端點）
min/         ; 等級 >= min（開放上限）
/max         ; 等級 <= max（開放下限）
```

範例：
```ini
; 等級 25-50 的 NPC
Spell = 0x12FCD~Skyrim.esm|NONE|NONE|25/50
; 等級 10 以上
Spell = MySpell|NONE|NONE|10/
```

> 玩家等級動態 NPC（leveled NPCs）的等級在 load 時才確定，可能被跳過。

#### 技能等級

格式：`SkillIndex(min/max)`

技能索引（0-17）：

| Index | 技能 | Index | 技能 |
|---|---|---|---|
| 0 | One-Handed | 9 | Sneak |
| 1 | Two-Handed | 10 | Alchemy |
| 2 | Archery | 11 | Speech |
| 3 | Block | 12 | Alteration |
| 4 | Smithing | 13 | Conjuration |
| 5 | Heavy Armor | 14 | Destruction |
| 6 | Light Armor | 15 | Illusion |
| 7 | Pickpocket | 16 | Restoration |
| 8 | Lockpicking | 17 | Enchanting |

範例：
```ini
; Destruction 技能 50-100 的 NPC
Perk = MyPerk|NONE|NONE|14(50/100)

; 一個括號語法：Actor 等級 14 以上，且 Destruction 技能 10 以上
Spell = 0x12FCD~Skyrim.esm|NONE|NONE|14(10)|M/U
```

### 4.4 Traits（第 5 欄）

特質字母以 `/` 分隔組合，代表 AND（同時符合）：

| 字母 | 語義 |
|---|---|
| `M` | 男性 NPC |
| `F` | 女性 NPC |
| `U` | Unique NPC（actorbase 標記 unique） |
| `S` | 可召喚（Summonable） |
| `C` | 兒童（IsChild） |
| `L` | Leveled NPC |
| `T` | 玩家隊友（Player Teammate） |

**否定**：在字母前加 `-`，代表「不符合此特質」：
- `-U` = 非 unique NPC
- `-C` = 非兒童

**組合範例**：
```ini
; 所有女性 NPC
Spell = MySpell|NONE|NONE|NONE|F

; 所有男性 unique 且可召喚的 NPC
Perk = MyPerk|NONE|NONE|NONE|M/U/S

; 非 unique 的 NPC
Item = MyItem|NONE|NONE|NONE|-U
```

> Chance（機率）只對非 unique（`-U`）NPC 生效；unique NPC 忽略 chance，永遠分發（或不分發）。

### 4.5 Chance（第 7 欄）

```
0-100    ; 百分比，0=永不，100=必定（預設）
```

- 只對 **非 unique** NPC 有效。
- Unique NPC 忽略 chance 值，一律視為 100%（只要其他 filter 符合）。
- 省略、留空、或寫 `NONE` 等同 `100`。

### 4.6 多個 Filter 之間的關係

- 同一行的所有 filter 欄位是 **AND**（全部都要通過）。
- 同一欄位內的多個條目（逗號分隔）是 **OR**（任一符合即可），但帶 `-` 前綴的條目是排除（NPC 必須不符合那個值）。
- `+` 連接的多個 StringFilter 條目是 **AND**（NPC 必須同時有全部）。

範例說明：
```ini
; 以下分發給：同時符合 ActorTypeNPC 且不是 Nazeem，且在 BanditFaction 或 EnemyFaction 中
Item = 0xF~Skyrim.esm|ActorTypeNPC,-Nazeem|BanditFaction,EnemyFaction|NONE|NONE|3000
```

---

## 五、真實 ini 範例（附中文解釋）

來源：本地 `~/skyrim_mods/unzip/` 的真實 mod 檔。

```ini
; === nwsFF_SkillBoostsPerks_DISTR.ini ===
; 給所有帶 ActorTypeNPC keyword 的 NPC 加兩個 perk：
; 讓 NPC 也能受 alchemy/skill boost perk 效果影響（原本只有玩家能用）
Perk = 0xCF788~Skyrim.esm|ActorTypeNPC
Perk = 0xA725C~Skyrim.esm|ActorTypeNPC
```

```ini
; === nwsFF_SpellMag_DISTR.ini ===
; 給所有 NPC 加法術威力 perk（NFF 自訂 perk，讓 NPC 法術隨等級縮放）
Perk = 0x4F9D6D~nwsFollowerFramework.esp|ActorTypeNPC
```

```ini
; === nwsFF_FriendlyFire_DISTR.ini ===
; 給所有 NPC 加友軍傷害 perk（NFF 的友軍傷害控制系統）
Perk = 0x4F9D6C~nwsFollowerFramework.esp|ActorTypeNPC
```

```ini
; === ImGladYoureHere_DISTR.ini ===
; 給特定 NPC（JJSofiaFollower、PumpkinTheFoxActor）加入 GYH 的 faction，
; 讓 GYH 能識別這些 follower 並執行擁抱/互動場景
Faction = WW42GYHSofialikeFollowerDialogueFixFaction|JJSofiaFollower|NONE|NONE|NONE|NONE|NONE
Faction = WW42GYHPetPatchFaction|PumpkinTheFoxActor|NONE|NONE|NONE|NONE|NONE
```

**其他常見模式（源自文件範例）**：

```ini
; 給等級 25-50 的女性 NPC 加 Flames 法術
Spell = 0x12FCD~Skyrim.esm|NONE|NONE|25/50|F

; 給男性 unique NPC（Destruction 技能 >= 10）加 Flames
Spell = 0x12FCD~Skyrim.esm|NONE|NONE|14(10)|M/U

; 給某 NPC（除 Nazeem 外）的 inventory 加 3000 個金幣
Item = 0xF~Skyrim.esm|ActorTypeNPC,-Nazeem|NONE|NONE|NONE|3000

; 給 ActorTypeGhost 且 Vampire 的 NPC 加龍吼
Shout = 0x13E07~Skyrim.esm|ActorTypeGhost+Vampire

; 給貧民 NPC 加 ActorTypePoor keyword（按 EditorID 指定目標 NPC）
Keyword = ActorTypePoor|Brenuin

; 給 BanditFaction 中的 NPC 加一個 perk，機率 50%（非 unique）
Perk = 0x9DE80~test.esp|NONE|0x1BCC0~test.esp|NONE|NONE|NONE|50
```

---

## 六、對 ModForge 的參考價值

### 可生成的輸出：`_DISTR.ini`

SPID 的 config 是純文字 `.ini`，**無需 ESP**。ModForge 理論上可以輸出 `<ModName>_DISTR.ini` 作為 mod 的一部分。

對 ModForge 最直接的使用場景：

| 場景 | SPID 行格式 | 說明 |
|---|---|---|
| 給特定 NPC 加 faction（dialogue condition 用）| `Faction = MyFaction\|TargetNPCEditorID\|NONE\|NONE\|NONE\|NONE\|NONE` | 最輕量，不需 patch ESP |
| 給一批 NPC 加 keyword | `Keyword = MyKeyword\|ActorTypeNPC` | OAR 或 dialogue condition 用 |
| 給 follower 加 ability（invisible spell）| `Spell = MyAbility\|FollowerEditorID` | 狀態 hook / buff 注入 |
| 給 NPC 分發 perk（戰鬥/技能用）| `Perk = 0xFormID~Plugin.esp\|ActorTypeNPC` | 廣泛分發 |
| 給 NPC 加入一個 outfit | `Outfit = MyOutfit\|TargetRace\|NONE\|NONE\|NONE\|NONE\|100` | 替換外觀 |
| 給死亡 NPC 加額外掉落 | `DeathItem = 0xLVLI~Plugin.esp\|ActorTypeNPC` | 戰利品分配 |

### ⚠️ ModForge 需要新支援的項目（推斷，未查 src/）

- **`_DISTR.ini` 輸出器**：目前 ModForge 產出 `.esp`，若要支援 SPID config，需要新增一個輸出模組，能把 JSON spec 裡的 distribution 設定翻譯成 `_DISTR.ini` 行格式。
- **FormID cross-plugin 解析**：SPID config 的 `0xFormID~Plugin.esp` 格式要求知道目標 form 的 FormID 和所在 plugin，ModForge 的 form 建立流程需要能在 spec 層記錄這個 reference。
- **EditorID 穩定性管理**：SPID 強烈建議用 EditorID 而非 FormID（merge 穩定），ModForge 生成記錄時給每個 form 一個穩定 EditorID 是前提。
- **Outfit / SleepOutfit / Skin 分發**：這三種 type 涉及 per-actor 追蹤（7.2+），若要支援需了解 Outfit 分發的「第一條優先」語義，避免多條 config 衝突。

### 現有能力（不需新支援）

- ModForge 已可建立 FACT / SPEL / PERK / KYWD 等記錄 → 可以在 ESP 裡直接做；SPID 只是讓你**不需要 patch 其他 mod 的 NPC record**。
- 若 ModForge 的目標 NPC 是自己 ESP 內的 NPC（不是 vanilla / 第三方 NPC），直接在 NPC record 裡加 faction/spell 即可，不需要 SPID。
- **SPID 的槓桿點在「跨 mod 無 patch 分發」**——自家 mod 的 NPC 不需要它。

---

## 七、與 KID / SkyPatcher 的分工

### KID（Keyword Item Distributor）

- **目標**：把 keyword 分發到**道具（item）記錄**（武器、盔甲、藥水、書、彈藥、材料、魔法效果等）。
- **不能做**：給 NPC 加 spell / perk / faction。
- **與 SPID 的關係**：SPID 運行在 KID 之後（如果都安裝了），兩者互補——KID 負責標記道具，SPID 負責標記 NPC。
- **使用場景**：給武器加 `WeapTypeSword` keyword、給藥水加自訂分類 keyword、讓 OAR/BFCO 能用 keyword 條件識別裝備。

### SkyPatcher

- **目標**：更廣泛的 runtime record patch——可以修改 LVLN（leveled list）、容器、種族、武器、NPC 屬性等，不限於「分發」。
- **能力**：可增加/修改道具到 leveled list、容器，可以直接修改 NPC 的屬性欄位，不只是 attach/add。
- **與 SPID 的關係**：互補，不互斥。複雜修改（直接 override NPC 欄位）用 SkyPatcher；單純 attach（加 spell/perk/faction/keyword）用 SPID。SPID 更輕量，兼容性更好。
- **使用場景**：修改 leveled list 讓新武器出現在商人、修改 NPC 戰鬥風格、直接 patch 種族記錄。

### 三者分工一覽

| 工具 | 主要目標 | 主要操作 | 典型 ini 後綴 |
|---|---|---|---|
| SPID | NPC actorbase | 加 spell / perk / item / faction / keyword / outfit / package 到 NPC | `_DISTR.ini` |
| KID | 道具記錄（ARMO/WEAP/ALCH 等） | 加 keyword 到道具 | `_KID.ini` |
| SkyPatcher | 廣泛 record（LVLN/CONT/NPC/RACE 等）| 修改記錄欄位、加入 leveled list | 自定義 `.ini` in `SkyPatcher/` |

> **待補**：KID 和 SkyPatcher 的深挖 survey 完成後，補充各自完整語法與更多使用場景對比。

---

## 參考來源

- Nexus 文章「SPID: The Complete Reference」：`https://www.nexusmods.com/skyrimspecialedition/articles/6617`（Nexus 會員限定）
- Nexus 文章「How To Use」：`https://www.nexusmods.com/skyrimspecialedition/articles/4022`
- aqxaromods 鏡像 v6.6.2 文件：`https://aqxaromods.com/skyrim-special-edition/utilities-skyrimse/12728-spell-perk-item-distributor-spid-v662.html`
- moddingskyrim.com SPID and KID 比較：`https://moddingskyrim.com/spid-and-kid/`
- 本地真實 ini 範例：`ImGladYoureHere_DISTR.ini`、`nwsFF_*_DISTR.ini`
