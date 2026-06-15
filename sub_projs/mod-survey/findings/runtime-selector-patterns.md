# Runtime Selector Patterns — Finding

> 本文是兩種「執行期資料模式」的合一拆解：**Global-as-selector**（用 GlobalVariable 當跨系統共享狀態）與 **linkedRef 節點鏈**（用 XLKR 連接 placed ref 組成路線/觸發序列/目標集合）。材料來源：Immersive Interactions、Conditional Expressions、Animated Carriage 的 mod survey finding，以及 ModForge `src/` 實際 builder。
> 繁體中文行文；record 名/函數名/欄位名保留英文。

---

## 第一部分：Global-as-selector 模式

### 一、概念說明

GlobalVariable（GLOB record）是 Skyrim 中**唯一能被多種不同系統在執行期同時讀取**的有名數值：

- Papyrus 腳本可 `SetValue()` / `GetValue()` / `Mod()`
- CTDA 條件可用 `GetGlobalValue` 無需 Papyrus
- OAR / DAR 條件 JSON 可直接引用 GLOB 的 FormKey
- SPID `_DISTR.ini` 的 filter 可讀 global
- SkyPatcher ini 也可作 global condition

這代表同一個 GlobalVariable 可以當**多個異質系統的共享信號**：Papyrus 負責**寫**（設狀態），外部 framework 負責**讀**（決定動畫/分發/條件）。這就是 "Global-as-selector" 的核心意義：Global 不是單向開關，而是一個**執行期狀態匯流排**，讓沒有直接 API 相依的系統能間接協作。

**典型實例**：
- **Immersive Interactions**：`AR_DogUp`（GlobalShort）在 Papyrus 播動畫前 `SetValue(N)`（1–7），DAR 的 `_conditions.txt` 用 `ValueEqualTo(...|0x0000AA13, N)` 選對應變體 `.hkx`，播完 `SetValue(0)` 清值。Global 是唯一的狀態橋。
- **Conditional Expressions**：`CondiExp_CurrentlyBusy`（GlobalShort）作 mutex：busy=1 時其他 magic effect 跳過、busy=0 才搶佔執行。`CondiExp_PlayerIsDrunk` 等多個 global 被 dialogue INFO condition `GetGlobalValue` 直接讀取，不走 Papyrus。

---

### 二、各消費方的語法

#### DAR `_conditions.txt`（舊，以 Immersive Interactions 為例）
```
ValueEqualTo("ImmersiveInteractions.esp"|0x0000AA13, 1.0)
```
- 第一參數：GLOB 的 FormLink（`"Plugin.esp"|0xFormID`）
- 第二參數：比對的 static float 值
- 多資料夾分別對應不同 N，DAR 選最高優先通過的那層

#### OAR `config.json` — `CompareValues`
OAR 把 DAR 的 `ValueEqualTo` 通則化為 `CompareValues`，值的型別可以是 static/global/AV/graph variable 四選一：

```json
{
  "condition": "CompareValues",
  "requiredVersion": "1.0.0.0",
  "Value A": {
    "globalVariable": "Plugin.esp|0x0000AA13",
    "type": "Global"
  },
  "Comparison": "==",
  "Value B": { "value": 1.0 }
}
```

或用 BFCO 示範的 behavior graph variable 版本：

```json
{
  "condition": "CompareValues",
  "requiredVersion": "1.0.0.0",
  "Value A": { "graphVariable": "BFCO_iAttackVariants", "graphVariableType": "Int" },
  "Comparison": "==",
  "Value B": { "value": 1.0 }
}
```

兩種 "Value A" 可混搭：`"type": "Global"` 引用 GLOB，`"type": "ActorValue"` 引用 AV，`"type": "FloatValue"` 是 static 常數。

#### SPID `_DISTR.ini` filter
SPID（Spell Perk Item Distributor）的 filter 段支援 global：
```ini
Spell = MySpell|...|...|...|G(MyMod_AnimState == 1)|...
```
`G(EditorID == value)` 語法在 kDataLoaded 後求值；global 為 0/1 常見用法（啟用/停用分發）。

#### Papyrus `GlobalVariable` 直接讀寫
```papyrus
GlobalVariable Property MyMod_AnimState Auto

; 設值（selector 進入某狀態）
MyMod_AnimState.SetValue(2.0)

; 讀值（條件判斷）
if MyMod_AnimState.GetValue() == 0.0
    ; …
EndIf

; 增量（busy counter、reputation）
MyMod_AnimState.Mod(1.0)
```

#### CTDA condition（dialogue INFO / perk / package）
在 CK / Mutagen 層：`GetGlobalValue` function，參數指向 GLOB FormKey，比對值設在 condition 欄位。Dialogue INFO 可用此條件讀玩家狀態，完全不需要腳本。

---

### 三、生命週期模式

#### 模式 A：設值→動作→清值（一次性動畫選擇器，Immersive Interactions 原型）

```
Papyrus 播動畫前：
  AR_DogUp.SetValue(3.0)   ← 選擇器進入狀態 3

  DAR/OAR 即時讀 Global，選對應 .hkx 播放

動畫結束後：
  AR_DogUp.SetValue(0.0)   ← 清值，退回中性狀態（DAR/OAR 回到 priority 0）
```

**設計原則**：
- 0 = 中性/無選擇（DAR/OAR 沒有 N=0 的資料夾，等同關閉）
- 1..N = 各個變體
- 清值務必在所有 branch（成功、被打斷、等待超時）都執行

#### 模式 B：Busy gate（互斥鎖，Conditional Expressions 原型）

```
Effect A 執行前：
  if CondiExp_CurrentlyBusy.GetValue() != 0.0
      return   ← 被佔用，跳過
  EndIf
  CondiExp_CurrentlyBusy.SetValue(1.0)   ← 上鎖

  … 執行表情/動作 …

OnEffectFinish（或計時結束）：
  CondiExp_CurrentlyBusy.SetValue(0.0)   ← 解鎖
```

**設計原則**：
- 高優先動作（Pain/Drunk/Skooma）設 busy=1
- 低優先動作（Random idle 表情）在 `OnUpdate`/`OnEffectStart` 開頭查 busy
- 不參與優先序的背景動作（Sneaking/Angry）**不用** busy gate

#### 模式 C：狀態中介（跨系統通訊，Conditional Expressions 通用）

```
ReferenceAlias 腳本（監聽事件）：
  OnObjectEquipped event：
    if item in DrinksList
      CondiExp_PlayerIsDrunk.SetValue(1.0)
    EndIf

Magic effect（消費者）：
  OnUpdate：
    if CondiExp_PlayerIsDrunk.GetValue() == 1.0
      … 啟動醉酒表情 …
    EndIf

Dialogue INFO condition（另一個消費者，零 Papyrus）：
  CTDA: GetGlobalValue(CondiExp_PlayerIsDrunk) == 1.0
  → 玩家喝醉時 NPC 有特殊台詞，不需要任何腳本
```

---

### 四、ModForge 生成評估

#### Global record 生成：**完整支援**

`Spec.Globals.cs:GlobalSpec` 有 `EditorId`/`Type`（short|long|float）/`Value`/`Constant`。`Generator.Build.Globals.cs:BuildGlobals()` 在 pass 1 建好 GLOB，讓後續的 conditions/regions 可引用其 editorId。

**可立即用於**：
- 動畫選擇器 global（`type: "short"`, `value: 0`）
- busy gate global（`type: "short"`, `value: 0`）
- MCM 開關 flag（`constant: false`）
- 只讀調參常數（`constant: true`）

#### script 中讀寫 Global：**支援（透過 setGlobal + 通用 scripts[]）**

`Spec.Dialogue.cs:DialogueSetGlobalSpec`（`dialogue[].setGlobal.global` + `value`/`delta`）讓對話選項在 TIF fragment 裡呼叫 `TargetGlobal.SetValue(...)` 或 `TargetGlobal.Mod(...)`。`Generator.Build.Scripts.cs:AttachDialogueResultScripts()` 在 pass 2 把 global property 綁進去。

通用 `scripts[]`（`Spec.Scripts.cs`）可用 `type: "object"` / `objectEditorId: "<globEditorId>"` 把任意 GLOB 綁為 script property，再由手寫 `.psc` 的 `SetValue()`/`GetValue()` 消費。

**當前缺口**：沒有「非對話觸發時直接在 spec 裡描述 SetValue 呼叫」的一等支援（例如 quest 腳本層的 global 寫值），仍需手寫 `.psc` + 透過 `scripts[]` 綁 property。

#### OAR condition 生成（CompareValues + Global）：**待接線**

OAR 生成器本身是 roadmap 項，尚未落地。但技術上：
- `CompareValues` condition 的 `"type": "Global"` + `"globalVariable": "Plugin.esp|0xFormID"` 是純 JSON 序列化
- ModForge 已在 pass 1 建好 GLOB 並有 formKey；emit OAR condition 時可從 `formKeyByEd` 查 FormKey 後格式化為 `"Plugin|0xFormID"` 字串
- 與既有 CTDA condition 模型對映（OAR 指南 §10 已說明：`CTDA → OAR condition` 一對一映射是最高槓桿接線點）

**建議**：OAR 生成器的 condition 序列化器優先支援 `CompareValues(Global, static)` 組合，即可覆蓋 Global-as-selector 的核心用例。

---

## 第二部分：linkedRef 節點鏈模式

### 五、概念說明

linkedRef（在 ESP 中為 `XLKR` subrecord）是放置物件（placed ref，REFR 或 ACHR）之間的有名指標。一個 ref 可帶**多條** XLKR，每條由兩個欄位組成：**target ref**（指向什麼）+ **keyword**（這條連結叫什麼名字，可為 null）。

這個機制可以實現三種截然不同的功能：

1. **路線鏈（Route Chain）**：marker → marker → … → end，NPC Patrol package 沿 null keyword link 一格一格走，也可用 `TranslateToRef` 讓 Activator 沿鏈移動（Animated Carriage）
2. **觸發序列（Trigger Sequence）**：事件腳本呼叫 `GetLinkedRef(kwEvent1)` 取到下一個觸發點，再 `GetLinkedRef(kwEvent2)` 取分岔——實作無座標的「事件流水線」
3. **目標集合（Target Pool）**：多個 ref 的 linkedRef 都指向同一個「列表頭」，讓腳本輕鬆 scan；Extended Encounters + Immersive Interactions 用 FLST + `FormList.HasForm(ref)` 做類似事，而 linkedRef 版本是純 placement 層（無需 FLST record）

---

### 六、record 層結構

#### XLKR subrecord 格式

每條 `LinkedReference` 結構（Mutagen 型別 `LinkedReferences`）：

| 欄位 | 型別 | 說明 |
|------|------|------|
| `Reference` | `FormLink<IPlacedGetter>` | 目標 placed ref 的 FormKey（必填） |
| `KeywordOrReference` | `FormLink<IKeywordLinkedReferenceGetter>` | keyword KYWD 的 FormKey（可為 null = default link） |

一個 placed ref 可有**多條** XLKR，只要 keyword 不同就是不同的具名連結（named link）。

#### 單向鏈 vs 雙向鏈

**單向鏈**（Carriage 路線）：
```
Start → Node1 → Node2 → Node3 → End
```
每個 ref 只有一條 `null keyword` XLKR 指向下一個。Patrol package 從 Start 出發，`GetLinkedRef()` 每次取 null link 前進；`kwAlternativePath` 是第二條具名 link，讓腳本可以 50% 機率走分岔路。

**雙向鏈**（罕見）：
```
A ←→ B（A 有 XLKR→B，B 也有 XLKR→A）
```
通常用於「對話觸發後可以雙向追溯」或特定 package 需要「回頭走」。Animated Carriage 的路線是純單向（前進 + 到達終點後靜態化）。

#### keyword-typed link（具名連結）的用途

同一個物件上掛多條不同 keyword 的 XLKR，讓同一個 ref 同時參與**不同語義的鏈**：

```
MarkerRef:
  XLKR[keyword=null]              → 下一個巡邏點（Patrol 預設跟的路線）
  XLKR[keyword=kwAlternativePath] → 分岔路線的第一個點（腳本 50% 機率走這條）
  XLKR[keyword=kwBossSpawn]       → Boss 生成點
```

Papyrus 取具名連結：
```papyrus
ObjectReference nextPatrol = MarkerRef.GetLinkedRef()                    ; null keyword
ObjectReference altPath    = MarkerRef.GetLinkedRef(kwAlternativePath)   ; 具名 keyword
```

---

### 七、ModForge WireLinkedRefs() 現有能力

`Generator.Build.PlacementRefs.cs:WireLinkedRefs()` 在 pass 2 執行（在所有 placement 建好後），逐一讀取 `PlacementSpec.LinkedRefs`（型別 `List<LinkedRefSpec>`）：

```csharp
// 對每個有 LinkedRefs 的 placement：
foreach (var lr in pl.LinkedRefs)
{
    // 1. 解析 target：in-spec editorId 或 vanilla "Plugin.esm:0xID" 外部 ref
    if (!TryResolveRef(lr.Target, formKeyByEd, out var tgtFk)) { Warn(...); continue; }

    var link = new LinkedReferences();
    link.Reference.SetTo(new FormLink<IPlacedGetter>(tgtFk));

    // 2. keyword 可選：有就解析，空字串 = null keyword（= default Patrol link）
    if (!string.IsNullOrWhiteSpace(lr.Keyword) && TryResolveRef(lr.Keyword, formKeyByEd, out var kwFk))
        link.KeywordOrReference.SetTo(new FormLink<IKeywordLinkedReferenceGetter>(kwFk));

    list.Add(link);
}
```

**已支援**：
- null keyword link（省略 `keyword` 欄位）→ Patrol 預設路線
- 具名 keyword link（填 `keyword: "MyKeywordEditorId"` 或 `"Plugin.esm:0xID"`）→ 具名連結
- 跨 plugin 外部 ref target（`"Skyrim.esm:0x..."` 語法）
- target 可以是 in-spec placement（by editorId）或 vanilla ref
- 帶 `linkedRefs` 的 placement 自動設 persistent flag（`Generator.Build.Placements.cs` 行 124）

**spec 層欄位**（`Spec.World.cs:LinkedRefSpec`）：
```csharp
public sealed class LinkedRefSpec
{
    public string Target  { get; set; } = "";   // 必填：目標 placement editorId 或 "Plugin:0xID"
    public string Keyword { get; set; } = "";   // 可選：keyword editorId 或 "Plugin:0xID"；空 = default link
}
```

**驗證**（`Generator.Validate.World.cs`）：`Target` 不能為空、`Target`/`Keyword` 都經 `CheckRef` 解析驗證；帶 `linkedRefs` 的 placement 必須有 `editorId`（否則 validation error）。

**注意**：`WireLinkedRefs()` 同時支援 `IPlacedObject`（REFR）和 `IPlacedNpc`（ACHR），`src as IPlacedObject ?? src as IPlacedNpc` 選正確的 `LinkedReferences` 集合。

---

### 八、設計模式

#### 模式 A：Patrol 路線鏈

最直接的用法，即 Animated Carriage 的路徑節點鏈：

```json
// spec 片段（示意）
{
  "placements": [
    { "editorId": "MyRoute_Start",   "base": "MyCartMarker", "cell": "...", "position": {...},
      "linkedRefs": [{ "target": "MyRoute_Node1" }] },
    { "editorId": "MyRoute_Node1",   "base": "MyCartMarker", "cell": "...", "position": {...},
      "linkedRefs": [{ "target": "MyRoute_Node2" }] },
    { "editorId": "MyRoute_Node2",   "base": "MyCartMarker", "cell": "...", "position": {...},
      "linkedRefs": [{ "target": "MyRoute_End" }] },
    { "editorId": "MyRoute_End",     "base": "MyCartMarker", "cell": "...", "position": {...} }
  ],
  "packages": [
    { "editorId": "MyNpcPatrol", "type": "Patrol", "patrol": { "start": "MyRoute_Start" } }
  ]
}
```

Patrol package 的 `start` 指向鏈頭，NPC 跟著 null keyword XLKR 一格一格走。**閉環**（回到起點循環）：最後一個節點的 `linkedRefs` 指回 `MyRoute_Start`，`patrol.repeatable: true`（預設即 true）。

#### 模式 B：具名連結（分岔路線 / 多語義）

```json
{ "editorId": "BranchPoint",
  "linkedRefs": [
    { "target": "MainPath_Next" },
    { "target": "AltPath_Start", "keyword": "kwAlternativePath" }
  ]
}
```

腳本可以 `GetLinkedRef()` 取 null link（主路）或 `GetLinkedRef(kwAlternativePath)` 取分岔，再依隨機值決定走哪條。ModForge 今天完整支援此模式（`WireLinkedRefs()` 的 keyword 路徑）。

#### 模式 C：目標池（FLST 替代方案）

多個物件都帶 XLKR 指向同一個「列表頭 marker」，讓腳本遍歷：

```
Ref_A → [linkedRef null] → PoolHead
Ref_B → [linkedRef null] → PoolHead
Ref_C → [linkedRef null] → PoolHead
```

腳本不用 FormList，直接 scan 附近所有帶某 keyword link 的 ref；或反過來：`PoolHead` 的 XLKR 指向第一個目標，目標的 XLKR 再指向下一個，形成遍歷鏈。

#### 模式 D：與 alias 間接的關係（真缺口 #2）

mod-survey-gaps.md 的確認真缺 **#2**：package `target`/`location` 目前只解到 placed ref 或 NearSelf，無 `PackageTargetAlias`/alias-index location。

這個缺口與 linkedRef 鏈在 **radiant 演出** 場景下直接相關：

- **理想流程**：Quest alias 填入一個動態 actor（`findMatching` 或 `createObject`），package 的 target 指向那個 alias index → actor 沿 linkedRef 鏈巡邏
- **現況**：`WireDeferredTargets()` 只能輸出 `PackageTargetSpecificReference`（指向固定 placed ref）
- **可作的繞過**：將 linked-ref 鏈頭設為 persistent placed marker，package target 直接指向那個 marker 的 editorId → NPC 從鏈頭開始巡邏，但 NPC 本身仍需是 placed ref 而非動態填入的 alias。完全的 alias-driven radiant patrol 需等 #2 補上 `PackageTargetAlias` 支援。

---

## 附錄：命名規則建議

### Global-as-selector 命名

| 用途 | 建議格式 | 範例 |
|------|----------|------|
| 動畫選擇器 | `<ModPrefix>_AnimState` | `MF_AnimState` |
| Busy gate | `<ModPrefix>_Busy` | `MF_Busy` |
| 玩家狀態中介 | `<ModPrefix>_Player<State>` | `MF_PlayerDrunk` |
| MCM 開關 | `<ModPrefix>_Enable<Feature>` | `MF_EnableGreetAnim` |
| 計數器/聲望 | `<ModPrefix>_<Counter>` | `MF_ReputationScore` |

### linkedRef 路線命名

| 用途 | 建議格式 | 範例 |
|------|----------|------|
| 路線整體 | `<ModPrefix>_Route_<Name>_<N>` | `MF_Route_TavernPatrol_01` |
| 路線起點 | 同上，後綴 `Start` | `MF_Route_TavernPatrol_Start` |
| 路線終點 | 同上，後綴 `End` | `MF_Route_TavernPatrol_End` |
| 具名 keyword | `kw<ModPrefix>_<LinkName>` | `kwMF_AltPath` |
