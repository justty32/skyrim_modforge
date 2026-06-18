# 這個工具做什麼 + config 格式語法全集

← [animobject-swapper](animobject-swapper.md)

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

