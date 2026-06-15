# Mod Survey — Base Object Swapper (Nexus 60805, v3.4.1)

> ModForge 取向：把這個框架拆成「config 格式全集」+ 「ModForge 可生成 ini 輸出 / 需新支援 / 純參考」。
> 分析對象：`Base Object Swapper-60805-3-4-1-1752606013.7z`（SKSE DLL + FOMOD）+ 原始碼（GitHub powerof3/BaseObjectSwapper）+ `Dynamic Things Alternative - Base Object Swapper-60741-0-5-1777404773.7z`（consumer mod，抽取真實 ini 範例）。

## 一、這個工具做什麼 + 工作原理

Base Object Swapper（BOS）是一個 SKSE plugin（`po3_BaseObjectSwapper.dll`），讓 mod 作者**在 runtime 把某個 base form 替換成另一個**，完全不需要 ESP patch 也不需要修改 cell reference。

**工作原理**：

1. 遊戲啟動時，BOS 掃描 `Data\` 目錄下所有檔名 suffix 為 `_SWAP` 的 `.ini` 檔。
2. 讀取各 ini 的 section/key，建立「base formID → swap formID + 屬性覆蓋 + 條件」的對應表。
3. 遊戲運行中，當引擎要實例化某個 reference 時，BOS 的 hook 攔截並在記憶體中把 base form 替換成指定的目標 form。
4. 替換是 runtime only，不修改任何存檔或 ESP，可以被隨時移除（移除後恢復原貌）。

**適用 record 類型**：任何繼承自 `TESBoundObject` 的 form type，包含 STAT（Static）、FURN（Furniture）、CONT（Container）、ACTI（Activator）、MSTT（MovableStatic）、LIGH（Light）、TREE（Tree）等。**不適用**：NPC / Actor（那是 NPCSwap 或 SPID 的工作）。

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

## 三、filter / 條件語法詳解

### 複合條件評估邏輯

```
結果 = (MATCH 至少一個符合) AND (NOT 的全部都不符合)
```

無 MATCH 條件 + 有 NOT 條件：只要 NOT 都不符合即生效（即白名單全開，只排除特定地點）。

### 條件 section 對應的 map

- `[Forms|...]` → `swapFormsConditional` map
- `[Properties|...]` / `[Transforms|...]` → `refPropertiesConditional` map

這兩個 map 在每次 reference 實例化時查詢，檢查當前 cell/worldspace/location/region/keyword 是否符合。

### 無條件 section

不帶 `|` 的 section（`[Forms]`, `[Properties]`, `[References]`）進 `swapForms` / `refProperties` / `swapRefs` map，對所有 reference 生效。

## 四、真實範例（DynamicThingsAlternative_SWAP.ini）

```ini
[Forms]

; Barrel02Static → dtaBarrel02（DTA 自訂版桶子，可互動）
; 原物：Skyrim.esm 0x10C0E3，替換目標：DTA.esp 0x806
0x10C0E3~Skyrim.esm|0x806~Dynamic Things Alternative.esp

; FirewoodPileHuge1 → dtaFirewoodPileHuge1（可撿柴的版本）
0x10ACC2~Skyrim.esm|0x81F~Dynamic Things Alternative.esp

; DLC Dawnguard 資源也可以被替換
; DLC01BucketBlood → dtaBucketBlood
0x8E48~Dawnguard.esm|0x897~Dynamic Things Alternative.esp

; MeadBarrel_BloodDrip01（Dawnguard）→ dtaMeadBarrel_BloodDrip01
0x482D~Dawnguard.esm|0x895~Dynamic Things Alternative.esp
```

**解讀**：
- 所有替換都在 `[Forms]` section，無條件對應（全遊戲所有這個 base 的 reference 都被換）。
- 格式為 `0xFormID~Plugin|0xFormID~Plugin`，注意 `~` 而非 `|` 分隔 FormID 和插件名。
- 主插件換 Skyrim.esm 物件；DLC 插件（`DynamicThingsAlternative_Fishing_SWAP.ini`）在另一個 ini 換 DLC 資源，做到 optional DLC 支援。
- 每對 `;` 注解行只是說明（非必要），格式為原 EditorID|新 EditorID。

**帶 properties 的假設範例**（根據語法推導，DTA 未使用此功能）：

```ini
[Forms]
; 替換桶子，同時縮放到 1.2x、有 50% 機率
0x10C0E3~Skyrim.esm|0x806~MyMod.esp|scale(1.2/1.2)|50
```

## 五、對 ModForge 的參考價值

### 可生成 ini 輸出（推斷）

ModForge 目前生成 ESP；加入 `_SWAP.ini` 輸出器相對低成本：

- **基本 `[Forms]` 替換**：已知全部格式（FormID + 插件名語法完整）。可生成如 `FollowerHomeSwap_SWAP.ini` 之類的輸出，讓 follower home 根據進度替換家具/擺設。
- **多 DLC 拆分**：把 DLC-dependent 替換放另一個 `_SWAP.ini`，主 ini 只依賴 Skyrim.esm，pattern 清晰。

### 需新支援（推斷）

- **Properties / Transform 覆蓋的生成**：`pos()` / `rot()` / `scale()` 語法需要 ModForge 的 placement/transform 層介接，目前不確定是否已有對應 spec。
- **條件 section 生成**：`[Forms|LocationEditorID,-ExcludeLocation]` 語法需要 ModForge 知道 Location form 的 editorID；可能需新增 filter 條件 spec。

### 純參考

- **Chance 機率替換**：場景佈置隨機性（例如「有 60% 機率這個箱子換成舊版本」）是 pure ini 層，對 ModForge spec 沒有對應——適合在生成的 ini 裡直接寫死機率值。
- **`[References]` 特定 ref 替換**：這需要事先知道 ref formID，適合手寫微調，不是生成器的主力場景。

### 小結

BOS 是「無 ESP 場景佈置工具」的最輕量選項。ModForge 若要支援它，最高價值的第一步是**生成基本 `[Forms]` ini**（FormID 查表 + 插件名組合），Properties/條件 section 可作為後期擴展。

## 參考來源

- GitHub: [powerof3/BaseObjectSwapper](https://github.com/powerof3/BaseObjectSwapper)（原始碼：SwapData.cpp, ObjectProperties.cpp, ConditionalData.cpp, Manager.cpp）
- Nexus: [Base Object Swapper - Nexus 60805](https://www.nexusmods.com/skyrimspecialedition/mods/60805)
- Consumer mod 範例: Dynamic Things Alternative - Base Object Swapper（Nexus 60741, v0.5）
