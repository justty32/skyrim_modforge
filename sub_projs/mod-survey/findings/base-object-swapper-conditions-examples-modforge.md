# 條件語法詳解 + 真實範例 + 對 ModForge 的參考價值

← [base-object-swapper](base-object-swapper.md)

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

