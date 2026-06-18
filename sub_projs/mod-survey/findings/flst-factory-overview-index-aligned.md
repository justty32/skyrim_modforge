# FLST 資料池概覽 + 索引對齊模式

← [flst-factory](flst-factory.md)

## 一、FormList 作為資料池的設計模式概覽

`FormList`（FLST）是 Skyrim 的**通用容器 record**：一個有序的 FormID 清單，成員可以是任意型別的 record（SPEL、NPC_、WEAP、KYWD、LVLN 等，甚至混型）。FLST 本身不帶語意，語意完全由使用方決定。

在 mod 設計中，FLST 被當作「資料池」來用時，大致落在三種模式：

| 模式 | 代表 mod | 核心語意 | Papyrus 讀法 |
|---|---|---|---|
| **索引對齊池** | Spellforge | 多個 FLST 相同 index 位置的 item 彼此對應 | `GetAt(i)` 交集 |
| **分類容器** | Missives | FLST 代表一個集合，用成員身份做條件判斷 | `HasForm(form)` |
| **runtime 追加** | FLM（FormList Manipulator） | 遊戲載入後動態把 form 加進 FLST | DLL hook |

這三種模式各自有不同的生成需求，ModForge 的支援程度也不同。

---

## 二、索引對齊模式（Spellforge 風格）

### 結構

Spellforge 為每個「法術分類軸」建一組平行 FLST：

```
FLST "SFM_DeliveryAimed"     [SPEL_A,  SPEL_B,  SPEL_C,  ...]
FLST "SFM_Level0Novice"      [SPEL_A,  SPEL_B,  SPEL_C,  ...]
FLST "SFM_MethodFireForget"  [SPEL_A,  SPEL_B,  SPEL_C,  ...]
FLST "SFM_Principle03"       [SPEL_A,  SPEL_B,  SPEL_C,  ...]
```

關鍵：**index i 在所有清單中指向同一個 SPEL**（Spellforge 稱此警告 *"Missing level/method/delivery flist for spell at index N"*）。清單內容是預先寫死的法術集合，不是 runtime 產生。

### Papyrus 用法

```papyrus
; 找出符合所有條件的 spell（所有清單在該 index 都包含 spell）
Function find_all_spells_for_definition(...)
    Int i = 0
    While i < DeliveryList.GetSize()
        Form spell = DeliveryList.GetAt(i)
        If LevelList.GetAt(i) == spell && MethodList.GetAt(i) == spell && ...
            ; 此 spell 在所有軸上都符合 → 加入結果集
        EndIf
        i += 1
    EndWhile
EndFunction
```

核心 API：
- `FormList.GetAt(int index)` → `Form`
- `FormList.GetSize()` → `int`
- `FormList.HasForm(Form akForm)` → `bool`

### 注意事項

1. **清單必須嚴格對齊**：任何一個法術若遺漏某一軸的 FLST 條目，`GetAt(i)` 就會取到錯誤的 form → 分類錯亂。這是此模式最脆弱的點。
2. **library merge 機制**（Spellforge 的跨 plugin 擴充）：library esp 各自持有「補充列表」，在遊戲載入時由 `sfm_librarytransferscript` 把它們 append 進核心 esp 的 base lists——這是 ESP-side 無法無衝突解決的部分，Spellforge 用 Papyrus `AddForm()` 在 runtime 合併。
3. **索引對齊池的替代方案**：若不需跨 plugin 合併，直接在一個 esp 內建齊所有平行清單，index 對齊只是 spec 撰寫的紀律問題，不需要特別機制。

---

