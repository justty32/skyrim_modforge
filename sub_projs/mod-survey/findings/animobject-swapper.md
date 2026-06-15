# Mod Survey — AnimObject Swapper (Nexus 75167, v1.1.0)

> ModForge 取向：把這個框架拆成「config 格式全集」+ 「ModForge 可生成 ini 輸出 / 需新支援 / 純參考」。
> 分析對象：`AnimObject Swapper-75167-1-1-0-1666410165.7z`（SKSE DLL + FOMOD）+ 原始碼（GitHub powerof3/AnimObjectSwapper）。
> 注意：AnimObject Swapper 本身不包含任何 `_ANIO.ini` 範例；真實 consumer mod ini 未能在本機 unzip 目錄找到，以下格式說明從原始碼逆向推導。

## 一、這個工具做什麼 + 工作原理

AnimObject Swapper（AOS）是一個 SKSE plugin（`po3_AnimObjectSwapper.dll`），讓 mod 作者**在 runtime 替換 idle animation 使用的 AnimObject（ANIO record）**，完全不需要 ESP patch。

**背景知識 — AnimObject 是什麼**：Skyrim 的 idle animation 可以讓 actor 手持一個「動畫物件」（AnimObject，record type `ANIO`），例如一個酒杯、書本、樂器、道具。這個 ANIO 是 base form，掛在 idle animation 的 animation object slot 上。不同 idle 可指定不同 ANIO；換 ANIO 就能在相同動作下讓不同角色「拿著不同東西」。

**工作原理**：

1. 遊戲啟動時，AOS 掃描 `Data\` 目錄下所有 suffix 為 `_ANIO` 的 `.ini` 檔。
2. 讀取各 ini，建立「base ANIO formID → [conditional swap 列表]」的對應表。
3. 當某個 actor 準備播放 idle（或由 engine 查詢 AnimObject）時，AOS hook 檢查當前 actor 是否符合任何條件，若符合則替換 ANIO。
4. 替換是 runtime only，不修改存檔或 ESP。

**適用範圍**：ANIO（AnimObject）record 的 runtime swap；只影響「拿東西」的視覺表現，不改變 idle animation 本身（idle 動作仍由 OAR / DAR / base idle 決定）。

## 二、config 格式語法全集

### 命名規則

放在 `Data\` 目錄下（或其子資料夾），檔名必須以 `_ANIO.ini` 結尾：

```
Data\MyMod_ANIO.ini
Data\SKSE\Plugins\SomeMod_ANIO.ini
Data\Sofia_Companion_ANIO.ini
```

### Section 格式

Section 名稱格式：

```
[BaseANIO|FILTERS|TRAITS]
```

用 `|` 分為三段：

| 段 | 說明 |
|---|---|
| `BaseANIO` | 要被替換的原始 ANIO（formID 或 editorID，見下） |
| `FILTERS` | 篩選條件（逗號分隔，可選） |
| `TRAITS` | actor 特徵篩選（sex/child，可選） |

無條件替換（最簡單形式）：

```ini
[OriginalANIOEditorID]
OriginalANIOEditorID|Swap1EditorID,Swap2EditorID
```

或含 filter 與 traits：

```ini
[OriginalANIOEditorID|+SofiaFaction,-ChildExclude|F]
OriginalANIOEditorID|FemaleSpecificANIO
```

### Entry 行格式

Section 內的 key-value 行：

```
baseANIO|swap1,swap2,swap3
```

| 欄位 | 說明 |
|---|---|
| `baseANIO` | 原始 ANIO 的 formID（`0xID~Plugin.esp`）或 editorID |
| `swap1,swap2,...` | 替換目標 ANIO，逗號分隔；多個時在 runtime **隨機選一個** |

### FormID 格式

與 BOS 相同：

```
0xFormID~Plugin.esp
EditorIDString
```

### Filter 條件語法（第二段 FILTERS）

條件是逗號分隔的 filter 項，各有前綴決定語意：

| 前綴 | 語意 | 說明 |
|---|---|---|
| `+` | ALL | 這些條件**全部**必須符合（AND 邏輯） |
| `-` | NOT | 這些條件**都不能**符合（排除邏輯） |
| `*` | ANY | 字串匹配（含有此子字串即符合，用於 .nif model path 或 keyword string） |
| 無前綴 | MATCH | 至少一個符合即生效（OR 邏輯） |

**可識別的 filter form 類型**：

| 類型 | 說明 |
|---|---|
| NPC（Actor base） | 匹配特定 NPC 的 base form |
| Faction | actor 是否在特定 faction |
| Race | actor 的種族 |
| Keyword | actor 或其 inventory 有此 keyword |
| Location | 當前 location（含 parent location） |
| Spell | actor 是否有特定 spell |
| FormList | 遞迴匹配 FormList 中的 form |
| Inventory objects | actor 的 weapon 或 bound object |
| String / model path | `*` 前綴的字串，匹配 .nif 路徑或 keyword 名 |

**評估邏輯**：

```
結果 = (ALL 全部符合) AND (NOT 都不符合) AND (MATCH 至少一個符合，若有) AND (ANY 至少一個字串含有，若有)
```

### Traits 語法（第三段 TRAITS）

控制 actor 的 sex / child 特徵：

| Traits 值 | 語意 |
|---|---|
| `M` 或 `-F` | 只對男性有效 |
| `F` 或 `-M` | 只對女性有效 |
| `C` | 只對小孩有效 |
| `-C` | 只對非小孩有效 |

可以組合：例如 `F,-C` = 女性且非小孩。

### 隨機替換邏輯

若 swap 端有多個逗號分隔的 ANIO：

```ini
[DrinkingCupANIO]
DrinkingCupANIO|MeadCupANIO,WineCupANIO,HornCupANIO
```

AOS 在 runtime 用 singleton RNG 隨機從列表中選一個。每次 actor 觸發此 idle 都可能選到不同 ANIO（真正的 random pool）。

### 條件 swap 與無條件 swap 的區分

- **無條件**（section 只有 `[BaseANIO]` 無 filter/traits）：進 `_animObjects` map，對所有持有此 idle 的 actor 生效。
- **有條件**（`[BaseANIO|FILTERS|TRAITS]`）：進 `_animObjectsConditional` map，runtime 對每個 actor 各自評估條件。

## 三、filter / 條件語法詳解

### 完整評估流程

```
Actor 即將使用 ANIO "X"
  → 查 _animObjectsConditional["X"] 的條件列表
  → 逐一 PassFilter(actor, conditions)
    → 評估 ALL / NOT / MATCH / ANY
    → 評估 Traits（sex / child）
  → 找到第一個符合條件的 ConditionalSwap → 從其 swappedAnimObjects 隨機選一
  → 若無條件符合 → 查 _animObjects["X"] 無條件替換（若存在）
  → 若仍無 → 用原始 ANIO
```

### Filter 查找方式

當 filter 字串為 EditorID：
1. 先嘗試 `GetFormEditorID` 解析為 FormID
2. 解析失敗則當作 string（用於 `*` ANY 模式）

BOS 的 filter 支援 cell/worldspace/region；AOS 的 filter 更偏向 **actor 本身的屬性**（faction, race, keyword, spell, npc base），不是地點。AOS 沒有 location/worldspace filter（那是 BOS 的強項）。

## 四、真實範例（從原始碼逆向的格式示意）

> 注意：以下範例是根據原始碼語法構造的示意，非從真實 `_ANIO.ini` 文件直接讀取。

**範例一：無條件隨機替換**

```ini
[DrinkingCupANIO]
; 持有 DrinkingCup idle 的 actor 隨機拿 3 種杯子之一
DrinkingCupANIO|WoodCupANIO,MeadHornANIO,GlassCupANIO
```

解釋：所有 actor 使用「喝飲料」idle 時，拿什麼杯子隨機從三種中選一個。

**範例二：依 NPC 身份替換**

```ini
[DrinkingCupANIO|SofiaFollower]
; 只對 Sofia 這個 NPC（base form）生效
DrinkingCupANIO|SofiaSpecialMugANIO
```

解釋：Sofia 拿她的專屬杯子，其他人拿原本的 cup。

**範例三：依 Faction + 性別替換**

```ini
[BookReadingANIO|+ThievesGuildFaction|F]
; 盜賊公會的女性成員拿特定書本
BookReadingANIO|ThievesGuildLedgerANIO
```

**範例四：排除小孩 + 依種族**

```ini
[DrinkingCupANIO|ArgonianRace|-C]
; 亞龍人（非小孩）拿亞龍人風格杯子
DrinkingCupANIO|ArgonianDrinkingVesselANIO
```

**範例五：FormID 格式（更精確，避免 editorID 衝突）**

```ini
[0x12345~Skyrim.esm|0xABC~CompanionsMod.esp]
0x12345~Skyrim.esm|0xDEF~CompanionsMod.esp,0xDEF2~CompanionsMod.esp
```

## 五、對 ModForge 的參考價值

### 純參考（目前無直接生成需求）

AOS 的對應 record 類型是 ANIO，它是 animation 管線的一部分，不是 ESP 的主力生成項。ModForge 目前的工作重心在 NPC、Dialogue、Quest、Scene 等 record；ANIO 替換屬於更精細的角色化視覺演出層。

純參考的原因：
- 生成 `_ANIO.ini` 需要知道 base ANIO 的 formID/editorID，這依賴對 vanilla idle animation 與 ANIO 配對的深度調查。
- 使用場景是「特定 follower 拿特定道具」，通常是手工設計決策，不是程序化生成的強項。

### 有潛力的支援點（推斷，需 code 驗證）

若 ModForge 的 follower spec 包含「道具/手持物品風格化」欄位，可以後期加入 `_ANIO.ini` 生成器：

- 輸入：`character.anio_profile` → `{ base_anio: "DrinkingCupANIO", swaps: ["SofiaWineBottleANIO"] }`
- 輸出：`<character>_ANIO.ini` 的對應行

Filter 條件中 `NPC base form` 是最精確的角色化方式（指定 Sofia 的 base formID），這需要 ModForge 在生成時知道對應 NPC 的 formID（通常是 spec 已知的）。

### 搭配使用模式（OAR + AOS）

最有力的演出技法：
- OAR（Open Animation Replacer）：負責換動作（idle animation hkx）
- AOS（AnimObject Swapper）：負責換動作中拿的物件（ANIO）

兩者都不需要 ESP patch，都是 config 驅動，可以各自獨立疊加。ModForge 若要支援「角色化演出包」輸出，OAR config 生成（已在 roadmap）搭配 AOS ini 生成是自然的配對。

### 小結

AOS 是「讓不同角色在相同 idle 裡拿不同道具」的最輕量工具。它比複製整個 idle animation record 更乾淨，兼容性更好。對 ModForge 目前的短期 roadmap 是**純參考**；中期若要做「follower 角色化道具演出包」，AOS ini 生成器是值得考慮的低成本輸出。

## 參考來源

- GitHub: [powerof3/AnimObjectSwapper](https://github.com/powerof3/AnimObjectSwapper)（原始碼：Manager.cpp, Manager.h, LookupFilters.cpp）
- Nexus: [AnimObject Swapper - Nexus 75167](https://www.nexusmods.com/skyrimspecialedition/mods/75167)
