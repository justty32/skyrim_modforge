# config 格式語法全集

← [base-object-swapper](base-object-swapper.md)

## 二、config 格式語法全集

### 命名規則

放在 `Data\` 目錄下（或其子資料夾），檔名必須以 `_SWAP.ini` 結尾（大小寫不拘）：

```
Data\MyMod_SWAP.ini
Data\SKSE\Plugins\SomeMod_SWAP.ini
Data\MyMod_Interiors_SWAP.ini
```

### Section 種類

BOS ini 可有四種 section：

| Section | 用途 |
|---|---|
| `[Forms]` | base form → swap form 的替換（最常用） |
| `[Properties]` 或 `[Transforms]` | reference-level 屬性覆蓋（位置/旋轉/縮放/flags），不換 base form |
| `[References]` | 針對**特定 reference**（而非 base form）的替換 |

**條件 section**：在 section 名稱後用 `|` 加條件，讓這一段只在符合條件時生效：

```ini
[Forms|Condition1,Condition2]
[Properties|LocationEditorID]
[References|-ExcludedEditorID]
```

### `[Forms]` 行格式

```
baseFormID|swapFormID[|properties][|chance]
```

| 欄位 | 必要 | 說明 |
|---|---|---|
| `baseFormID` | 必要 | 要被替換的原始 base form（見 FormID 格式一節） |
| `swapFormID` | 必要 | 替換目標。可為單一 form，也可為逗號分隔的 set（隨機選一） |
| `properties` | 選用 | 屬性覆蓋字串（見 Properties 語法一節） |
| `chance` | 選用 | 機率字串（見 Chance 語法一節） |

### `[Properties]` / `[Transforms]` 行格式

```
baseFormID|transformString|traitsString
```

對應 `ObjectData::GetProperties()` 解析邏輯；用於覆蓋 reference 的位置/旋轉/縮放/record flags，不換 base。

### `[References]` 行格式

與 `[Forms]` 格式相同，但 `baseFormID` 欄位填的是**reference 的 formID 或 editorID**，只替換那一個特定的 reference 實例。

### FormID 格式

BOS 支援兩種方式指定 form：

**方式一：十六進位 FormID + 插件名**
```
0xFormID~Plugin.esp
```
範例：
```
0x10C0E3~Skyrim.esm
0x8E48~Dawnguard.esm
0x806~Dynamic Things Alternative.esp
```

**方式二：EditorID 字串**
```
MyEditorID
```

**多值 set（用於 base 端的批次或 swap 端的隨機池）**：
```
0xFormID1~Plugin.esp,0xFormID2~Plugin.esp,0xFormID3~Plugin.esp
```
- 若 swap 端是單值：每個 base 都換成同一個 swap。
- 若 swap 端 size == 1 且 base 端是 set：全部 base 都換成同一個 swap。
- 若 swap 端 size ≥ base 端 size：每個 base 隨機被分配一個唯一 swap（不重複，從 swap pool 中抽）。

### Properties 語法（屬性覆蓋）

Properties 字串是以逗號分隔的多個指令，每個指令 `key(value)` 格式：

| 指令 | 格式 | 說明 |
|---|---|---|
| 位置 | `pos(x/x, y/y, z/z)` | 偏移座標（相對或絕對）；`x/x` 是 min/max 範圍 |
| 旋轉 | `rot(x/x, y/y, z/z)` | 旋轉角度（度），自動轉 radians；`x/x` 是 min/max 範圍 |
| 縮放 | `scale(min/max)` | 相對縮放倍率；`scaleA(min/max)` 為絕對縮放 |
| 設 flags | `flags(0xFLAG1,0xFLAG2)` | bitwise OR 設定 record flags |
| 清 flags | `flagsC(0xFLAG1,0xFLAG2)` | bitwise NOT 清除 record flags |

`min/max` 相同時為固定值（例如 `scale(1.5/1.5)` 固定 1.5x）；不同時在 runtime 隨機取一個 float 值。

### Chance 語法

第四個 `|` 欄位控制此次 swap 的機率（0.0–100.0）：

```
0x10C0E3~Skyrim.esm|0x806~MyMod.esp||75
```

（空的第三欄 `||` 代表無 properties）

- 預設不填 = 100%（必定替換）。
- `75` = 75% 機率替換，剩下 25% 維持原樣。
- 機率以 reference 為單位評估（每個 ref 各自隨機），不是全局一次。

### 條件 Section 過濾語法

Section 名稱後加 `|conditions`，conditions 是逗號分隔的過濾項：

| 前綴 | 語意 |
|---|---|
| 無前綴 | MATCH（至少符合一個，OR 邏輯） |
| `-` | NOT（這些都不能符合，排除邏輯） |

可識別的 filter form 類型（BOS 自動判斷 formID 屬於哪類）：

| 類型 | 說明 |
|---|---|
| Location | 當前 Location form（支援 parent location 遞迴） |
| Region | 當前 Region（REGN） |
| Keyword | Location 或 Reference 上的 keyword |
| Cell | 當前 Cell（formID 或 editorID） |
| WorldSpace | 當前 WorldSpace（支援 parent worldspace） |

若 filter 無法解析為 form，BOS 嘗試當成 EditorID（cell 或 keyword 字串）處理，並在 log 中記錄 INFO。

**範例：**

```ini
[Forms|WhiterunLocation,-AzuraShrineLocation]
; 只在 Whiterun Location 且不在 Azura Shrine Location 時替換
0xABCD~Skyrim.esm|0x100~MyMod.esp
```

