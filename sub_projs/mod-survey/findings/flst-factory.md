# FLST 工廠模式

> 為 ModForge（JSON spec → `.esp` 生成器）整理的 FormList 設計模式筆記。Record 名 / 函式名 / Mutagen 型別名留英文；散文繁中。
> 主要來源：spellforge.md finding、missives.md finding、`Generator.Build.Lists.cs`、`Generator.Build.Lists.Wire.cs`、`Spec.Items.cs`。

---

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

## 三、分類容器模式（Missives 風格）

### HasForm condition 與分類邏輯

Missives 用 FLST 當**集合成員判斷門**，不取值、不走 index，只問「這個 form 在不在清單裡」：

```
FLST "_M_ListLocationsForbidden"  → 禁止的地點清單
FLST "_M_ListPeopleForbidden"     → 禁止的 actor 清單
FLST "_M_ListQuests<Hold>Low"     → Whiterun hold、Low tier 的 quest 池
FLST "_M_ListQuests<Hold>Med"     → ...
```

**使用方式分兩層**：

1. **quest alias fill 條件（engine-side）**：Quest alias 的 fill conditions 可以用 `HasForm(FLST)` 或 `NOT HasForm(FLST)` 過濾——例如 `Alias_Dungeon` 的 fill condition 說「`NOT HasForm(_M_ListLocationsForbidden)`」，引擎就不會挑選禁止地點。

2. **Papyrus 遍歷池**（`_M_ActivatorScript.UpdateQuests`）：

```papyrus
Function UpdateQuests(Int chance, FormList akQuestPool)
    Int i = 0
    While i < akQuestPool.GetSize()
        Quest q = akQuestPool.GetAt(i) as Quest
        If Utility.RandomInt(0, 100) < chance
            q.Start()
        EndIf
        i += 1
    EndWhile
EndFunction
```

此處 FLST 當「容器」被遍歷，而非當「索引表」取對應值。

### HasForm vs GetAt 的語義差異

| API | 語義 | 典型用法 |
|---|---|---|
| `HasForm(form)` | 集合成員測試（O(n) 掃描，但 FLST 通常小） | condition gate；分類判斷 |
| `GetAt(index)` | 按位置取值 | 索引對齊交集；遍歷池 |
| `AddForm(form)` | runtime 追加（需 .pex 呼叫） | library merge；FLM 模式 |

**在 quest alias fill conditions 裡**，engine-side 原生支援「`HasForm(FLST)` 作為 fill 過濾條件」，這是 Papyrus 以外的第二個 FLST 使用場景。

---

## 四、其他模式

### FLST as SPID target

SPID（po3 SpellPerkItemDistributor）的 ini 可以把 FLST 當分發目標（type=Spell 時 value 可以是 FLST，分發清單內的所有 SPEL）或作為 FormFilter（`Form = 0x123~mod.esp` 可以是 FLST，SPID 展開其成員）。這讓 FLST 成為**無衝突 NPC 分發的彙整點**：各 mod 把自己的 spell 加到同一個 FLST，SPID 的 ini 只需指向該 FLST 就能一次分發所有成員。

此模式的 FLST 本身是靜態（編譯期填入），追加由 FLM 或 SPID ini 層處理，不需 runtime Papyrus。

### FLM runtime 追加模式

FormList Manipulator（FLM DLL）的 `_FLM.ini` 讓外部 plugin 在 `kDataLoaded` 時動態把 form 追加進任意 FLST（包括 vanilla 或他人 mod 的 FLST），**不衝突**（不需要 override 那個 esp）。

```ini
; _FLM.ini 語法（FormListManipulator finding）
[FormList]
; 把 MySpell 加進 SomeOtherMod 的 FLST
Form = 0x123456~SomeOtherMod.esp
Target = 0xABCDEF~TargetMod.esp
```

此模式是 ESP-side 無法達成的缺口補完（esp override FLST 會覆蓋其他 mod 的追加）。

---

## 五、ModForge FLST builder 現有能力 + 未覆蓋模式

### 現有能力（有 code 為據）

**`BuildFormLists()`**（`Generator.Build.Lists.cs`）：

```csharp
public void BuildFormLists()
{
    foreach (var fl in spec.FormLists)
    {
        var r = mod.FormLists.AddNew();
        r.EditorID = fl.EditorId;
        // 只設 EditorID，items 留空給 pass 2 填
    }
}
```

**`WireFormLists()`**（`Generator.Build.Lists.Wire.cs`）：

```csharp
public void WireFormLists()
{
    foreach (var fl in spec.FormLists)
    {
        if (!recordsByEd.TryGetValue(fl.EditorId, out var rec) || rec is not IFormList list) continue;
        foreach (var item in fl.Items)
            Resolve($"formList '{fl.EditorId}' item", item, fk =>
                list.Items.Add(new FormLink<ISkyrimMajorRecordGetter>(fk)));
    }
}
```

**`FormListSpec`**（`Spec.Items.cs`）：

```csharp
// FormList (FLST): an ordered list of FormIDs of ANY type. `items` are refs (in-spec editorId or
// vanilla "<master>:0xFORMID"). Use it as the param of a list-taking condition (GetItemCount /
// GetEquipped / GetIsVoiceType / GetInWorldspace all accept a FormList), as a keyword/clothing set
// for dialogue/quest gating, or anywhere the game wants a grouped set of forms.
public sealed class FormListSpec
{
    public string EditorId { get; set; } = "";
    public List<string> Items { get; set; } = new();
}
```

**ModForge 今天可以做的**：
- 建立新 FLST record（`BuildFormLists()`）
- 填入任意 form（in-spec editorId 或 vanilla ref）作為成員（`WireFormLists()`）
- 同一 FLST 可混合 in-spec 與 vanilla form
- 建立多個互相平行的 FLST（索引對齊模式的基礎）
- 在 condition 中引用 FLST（`ConditionSpec` 的 `HasForm` / `GetItemCount` 等）

### 未覆蓋模式（推斷，⚠️）

| 缺口 | 說明 | 影響模式 |
|---|---|---|
| ⚠️ **平行 FLST 自動展開**（高階） | 要從一批 spec-defined SPEL 自動沿分類軸（school/level/delivery）建出多條對齊清單，需要 spec 層語法支援（如 `indexedLists[]`）；目前只能手動逐一列出每條 FLST 的成員 | 索引對齊池（大規模法術族生成） |
| ⚠️ **runtime AddForm（Papyrus 側）** | ModForge 生成的是靜態 ESP，不生成 `AddForm()` 呼叫的 script 邏輯；跨 plugin 動態追加需靠 FLM 或手寫 Papyrus | FLM runtime 追加 |
| ⚠️ **生成 FLM ini 輸出** | ModForge 的 `build` 輸出是 `.esp`，不輸出 `_FLM.ini`；若要利用 FLM 無衝突追加外部 FLST，ini 需手寫 | FLST as SPID target（外部追加） |

**結論**：FLST 的**靜態建立 + 填入**（分類容器模式、手工索引對齊池、SPID target）ModForge 今天就能完整生成；**跨 plugin 動態追加**和**自動化索引對齊展開**是兩個合理的未來擴充點，不是緊急缺口（Missives/Spellforge 的核心功能不需要它們）。
