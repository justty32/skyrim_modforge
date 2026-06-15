# Perk Entry-Point 機制深挖

← [findings index](../index.md)

> **動機**：Perk 的 entry-point 是 Skyrim 引擎的「hook 插槽」——mod 在這裡插入「當 X 發生時做 Y」。ModForge 目前只支援 `ModifyValue`（數值型 entry-point）與 `ability`（SPEL 載具），`AddActivateChoice`（互動啟動選單）與 `SetText`（覆寫啟動按鈕文字）兩個互動型完全未支援，是 mod-survey-gaps 缺口 #1。本 finding 把完整機制拆清楚，作為 builder 擴充的設計基礎。

---

## 一、Perk entry-point 機制概覽

### 概念：引擎 hook 插槽

Perk 是玩家（或 NPC）身上的「被動增益」載具。一個 Perk record（PERK）可以帶多個 effect，每個 effect 選擇一種「觸發時機」（entry-point）並攔截引擎的計算或動作。引擎在執行對應動作時，會遍歷持有者身上所有 Perk 的 effect、找到匹配的 entry-point，依其 FunctionType 與條件決定是否介入。

```
PERK record
  ├── effect [ability]       → SPEL（常駐能力，始終生效）
  └── effect [entryPoint]    → 選定 EntryType hook
        ├── EntryPoint  = EntryType enum（哪個引擎事件）
        ├── FunctionType（效果的「動作種類」：SetValue / AddActivateChoice / SetText…）
        ├── Rank / Priority
        ├── ButtonLabel（LString，互動型才有意義）
        ├── Flags（PerkScriptFlag：FragmentIndex + RunImmediately/ReplaceDefault）
        ├── PerkConditionTabCount（byte，引擎內建 tab 數，不可為 0）
        └── Conditions（PerkCondition 列表，effect-level CTDA）
              └── PerkCondition { RunOnTabIndex, Conditions[] }
```

### record 層次

- **Perk 層條件**（`perk.Conditions`）：決定 Perk 整體是否對持有者生效（例如 `GetLevel >= 30`）。這是 plain `Condition` list，不是 `PerkCondition`。
- **Effect 層條件**（`effect.Conditions`，型別 `PerkCondition`）：決定「此次事件觸發時，這個 effect 是否出手」（例如「目標是門且 crosshair 在門上」）。`PerkCondition` 多了 `RunOnTabIndex`，對應該 entry-point 內建的語意 tab（不同 tab 的條件 run-on target 不同）。
- **PerkConditionTabCount**：entry-point 固有的 tab 數量，引擎用它分配 per-tab 條件陣列，**必須非 0 且與 vanilla 一致**（見第三節）。

---

## 二、Entry-point 種類全表

資料來源：`perkdiag entrypoints` 實測（Skyrim.esm 2026-06-15） + Mutagen `APerkEntryPointEffect.EntryType` enum + `APerkEntryPointEffect.FunctionType` enum 反射。

### 2-1 EntryType：引擎 hook 點（91 個）

下表精選與 ModForge 生成相關度較高的項目，附 vanilla tab-count（從 `Generator.Build.Perks.EntryPoints.cs` EntryPointTabCount 表讀出，unlisted = 預設 2）。

| ID | EntryType 名稱 | Mutagen 類別 | 典型場景 | tab-count |
|----|---|---|---|---|
| 0 | CalculateWeaponDamage | `PerkEntryPointModifyValue` | 武器傷害計算 | 3 |
| 26 | ModBashingDamage | `PerkEntryPointModifyValue` | 格擋攻擊傷害 | 2 |
| 28 | ModPowerAttackDamage | `PerkEntryPointModifyValue` | 力攻傷害 | 3 |
| 29 | ModSpellMagnitude | `PerkEntryPointModifyValue` | 法術強度 | 3 |
| 35 | ModAttackDamage | `PerkEntryPointModifyValue` | 普攻傷害 | 3 |
| 36 | ModIncomingDamage | `PerkEntryPointModifyValue` | 受到傷害（可設 0 擋下）| 3 |
| 38 | ModSpellCost | `PerkEntryPointModifyValue` | 法術消耗 | 2 |
| 51 | ApplyCombatHitSpell | `PerkEntryPointSelectSpell` | 命中時附加 spell | 3 |
| 52 | ApplyBashingSpell | `PerkEntryPointSelectSpell` | 格擋命中附加 spell | 2 |
| 53 | ApplyReanimateSpell | `PerkEntryPointSelectSpell` | 重生時附加 spell | 3 |
| 61 | CanPickpocketEquippedItem | `PerkEntryPointAbsoluteValue` | 可扒穿戴物品 | 3 |
| 67 | ApplyWeaponSwingSpell | `PerkEntryPointSelectSpell` | 揮武器時附加 spell | 3 |
| 74 | **FilterActivation** | `PerkEntryPointAddActivateChoice` | 過濾/攔截啟動動作 | 2 |
| 81 | **SetActivateLabel** | `PerkEntryPointSetText` | 覆寫啟動按鈕文字 | 2（預設）|

完整 91 個 EntryType 見 `perkdiag entrypoints` 輸出或 Mutagen `APerkEntryPointEffect.EntryType` enum。

> 注意：`FilterActivation` (74) 是「啟動攔截」的 hook 點，對應 Mutagen 類別 `PerkEntryPointAddActivateChoice`（FunctionType = `AddActivateChoice`）；`SetActivateLabel` (81) 對應 `PerkEntryPointSetText`（FunctionType = `SetText`）。EntryType 名稱（Skyrim 引擎用語）與 Mutagen 類別名稱不同，容易混淆。

### 2-2 FunctionType：effect 的「動作種類」

`APerkEntryPointEffect.FunctionType` enum（用來決定 Mutagen 序列化的具體類別）：

| FunctionType | Mutagen 類別 | 額外欄位 |
|---|---|---|
| SetValue / AddValue / MultiplyValue | `PerkEntryPointModifyValue` | `Value` (float) + `Modification` |
| AddRangeToValue | `PerkEntryPointAddRangeToValue` | `Value` + `Value2` |
| AddActorValueMult | `PerkEntryPointModifyActorValue` | AV + 修改方式 |
| AbsoluteValue / NegativeAbsoluteValue | `PerkEntryPointAbsoluteValue` | 無 |
| AddLeveledList | `PerkEntryPointAddLeveledItem` | `Item`（LVLI ref） |
| **AddActivateChoice** | `PerkEntryPointAddActivateChoice` | `Spell`（SPEL ref，fragment 派發用） |
| **SelectSpell** | `PerkEntryPointSelectSpell` | `Spell` |
| **SelectText** | `PerkEntryPointSelectText` | 文字字串 |
| **SetText** | `PerkEntryPointSetText` | `Text`（LString） |

---

## 三、AddActivateChoice 深挖

### 3-1 機制概覽

`FilterActivation` (EntryType 74) + FunctionType `AddActivateChoice` = **「在玩家準備啟動物件時，插入一個額外的選單選項（或攔截原本的啟動動作）」**。

- 引擎在玩家對 crosshair 目標按 Activate 前，遍歷所有擁有此 entry-point 的 Perk effect。
- 若 effect-level conditions 通過（例如目標 `GetIsID` 是某扇門、或 `HasKeyword` 是某道具），引擎把這個 effect 的選項加進「選單」。
- 玩家選擇後，引擎呼叫對應的 **Perk fragment**（`Fragment_N`）。

### 3-2 Record layout（Mutagen 欄位）

`PerkEntryPointAddActivateChoice` 繼承鏈：

```
APerkEffect
  ├── Rank          (byte)
  ├── Priority      (byte)
  ├── Conditions    (List<PerkCondition>)  ← effect-level CTDA
  ├── ButtonLabel   (LString)              ← 選單顯示文字
  └── Flags         (PerkScriptFlag)
        ├── FragmentIndex  (ushort)        ← 對應哪個 Fragment_N
        └── Flags          (RunImmediately | ReplaceDefault)
APerkEntryPointEffect（extends APerkEffect）
  ├── EntryPoint          (EntryType = FilterActivation = 74)
  └── PerkConditionTabCount (byte)         ← 必須非 0，vanilla = 2
PerkEntryPointAddActivateChoice（extends APerkEntryPointEffect）
  └── Spell               (FormLink<SPEL>) ← 通常是 perk fragment 的 dispatcher spell
```

關鍵欄位說明：

- **ButtonLabel**：玩家看到的選單文字（例如「打開箱子（動畫）」、「撿起（動畫）」）。
- **Flags.FragmentIndex**：對應 VMAD `PerkScriptFragments.Fragments[N].FragmentIndex` —— 引擎觸發時會呼叫 fragment script 的 `Fragment_N` 函式。每個 `AddActivateChoice` effect 有一個獨立的 fragment index。
- **Flags.RunImmediately**：不等玩家選擇直接執行。
- **Flags.ReplaceDefault**：取代原本的 vanilla Activate 行為（不在選單追加，而是覆蓋）。
- **Spell**：在 Immersive Interactions 中幾乎是空的（fragment 本身才是真正動作），但引擎允許在 fragment 觸發後再施放一個附加 spell。

### 3-3 Conditions 掛在 effect 層

每個 `AddActivateChoice` effect 帶自己的 effect-level conditions（`PerkCondition` list），決定「這個選項何時出現在選單」。Immersive Interactions 用：
- `GetIsID(Target, <formId>)` 判斷 crosshair 目標是否為特定 NPC/物件
- `HasKeyword(Target, <keyword>)` 判斷目標關鍵字
- `FormList.HasForm(Target, <FLST>)` 判斷目標是否在分類清單內

Tab 分組（`RunOnTabIndex`）：通常 0 = 啟動者（玩家），tab-count = 2 = 有「啟動者」與「目標」兩個語意 tab。

### 3-4 PerkConditionTabCount 必要值

`FilterActivation` 的 vanilla canonical tab-count = **2**（已在 `EntryPointTabCount` 表中）。

⚠️ **若 `PerkConditionTabCount = 0` 且有 `PerkCondition`，引擎在載入時分配 0 大小的 tab 陣列，接著對 tab 0 寫入 → 陣列越界 → 讀到 garbage FormID → 硬 CTD（Loading Files 階段）**。參見記憶筆記 `perk-conditiontabcount-ctd`，根因即此。

`SetActivateLabel` (81) 目前**不在 `EntryPointTabCount` 表中**，會使用預設值 2（`GetValueOrDefault(entry, 2)`）——在沒有 effect-level conditions 的情況下可接受（0 條件不會觸發越界），但若要加條件應明確加入表中並設為正確值。

---

## 四、SetText 深挖

### 4-1 機制

`SetActivateLabel` (EntryType 81) + FunctionType `SetText` = **「覆寫 crosshair 啟動按鈕的文字標籤」**。

引擎在玩家對準目標時顯示啟動提示（例如「[E] Open」），此 entry-point 可把文字換成自訂字串（例如「[E] (動畫) 開啟」）。與 `AddActivateChoice` 搭配：`SetText` 改按鈕文字，`AddActivateChoice` 在按下後插入選項 / 攔截動作。

### 4-2 Record layout

`PerkEntryPointSetText` 的欄位：

```
APerkEffect（共有）
  ├── Rank / Priority / Conditions / ButtonLabel / Flags
APerkEntryPointEffect
  ├── EntryPoint          = SetActivateLabel (81)
  └── PerkConditionTabCount                   ← vanilla 未在 EntryPointTabCount 表，預設 2
PerkEntryPointSetText
  └── Text                (TranslatedString)  ← 要顯示的自訂標籤文字
```

- **Text**：LString，支援多語言本地化。
- **ButtonLabel**（來自 APerkEffect）：在某些 CK 版本中 ButtonLabel 與 Text 都存在，通常設一樣的字串。
- effect-level conditions 用來控制「哪種目標類型才套用此文字」（例如「只對門套用」）。

### 4-3 與 AddActivateChoice 的搭配模式

Immersive Interactions 的典型模式：

1. **SetText** effect（`SetActivateLabel=81`）：匹配特定目標時，把按鈕文字改成「(動畫) 開門」。
2. **AddActivateChoice** effect（`FilterActivation=74`）：匹配同樣目標時，攔截 Activate、呼叫對應 fragment。

兩個 effect 用幾乎相同的 conditions，但各自獨立。`SetText` 只改 UI 文字，`AddActivateChoice` 才觸發實際動作。

---

## 五、Perk-fragment 膠水

### 5-1 VMAD on Perk（PerkAdapter）

Perk record 的 VMAD 是 `PerkAdapter`（不是 QuestAdapter 也不是 VirtualMachineAdapter）。Mutagen 型別：

```csharp
perk.VirtualMachineAdapter = new PerkAdapter
{
    Version = 5,
    ObjectFormat = 2,
    ScriptFragments = new PerkScriptFragments
    {
        ExtraBindDataVersion = 2,
        FileName = "AnimimationsReborn_Fragments",  // .psc scriptname（不含 .pex）
        Fragments = new List<PerkScriptFlag>
        {
            new PerkScriptFlag
            {
                FragmentIndex = 0,               // 對應 Fragment_0
                Flags = PerkScriptFlag.Flag.RunImmediately,
            },
            new PerkScriptFlag
            {
                FragmentIndex = 1,               // 對應 Fragment_1
                Flags = PerkScriptFlag.Flag.RunImmediately,
            },
            // ... 每個 AddActivateChoice effect 一個
        }
    }
};
```

`PerkScriptFragments` 欄位：
- `ExtraBindDataVersion`：2（vanilla 一致值）
- `FileName`：fragment script 的 Papyrus Scriptname（CK 規範是 `AnimimationsReborn_Fragments`；ModForge 可用 `PF_<PerkEditorId>` 格式）
- `Fragments`：每個 `PerkScriptFlag` 對應一個 effect；`FragmentIndex` 對應該 effect 的 `Flags.FragmentIndex` 欄位

### 5-2 `Extends Perk` script 格式

Fragment script 的 Papyrus 格式（Immersive Interactions 真實結構）：

```papyrus
Scriptname AnimimationsReborn_Fragments extends Perk Hidden

; Properties: 通常掛一個 Quest（中央 quest script）
ObjectReference Property Activate Auto   ; AR_QuestScript

Function Fragment_0(Actor akActor, ObjectReference akTargetRef)
    Activate.fOpen(akActor, akTargetRef)    ; → 中央 quest script 的函式
EndFunction

Function Fragment_1(Actor akActor, ObjectReference akTargetRef)
    Activate.fTake(akActor, akTargetRef)
EndFunction

; ... Fragment_N 對應第 N 個 AddActivateChoice effect
```

**固定簽名**：`Fragment_N(Actor akActor, ObjectReference akTargetRef)`——引擎呼叫時傳入啟動者與目標 ref，與 TIF（`Fragment_0(ObjectReference akSpeakerRef)`）不同。

### 5-3 `Fragment_N` 命名規則

- Fragment index 從 0 開始，每個 `AddActivateChoice` effect 各佔一個。
- Index 值由 effect 的 `Flags.FragmentIndex` 欄位指定（與 ScenePhaseFragment 的 `Index` 欄位同義）。
- 同一個 Perk 的多個 effect 不能有重複的 FragmentIndex。
- `PerkScriptFragments.Fragments` 列表長度 ≥ 最大 FragmentIndex + 1；unlisted index 留空（引擎跳過）。

### 5-4 Dispatcher 模式

Immersive Interactions 示範的生成樣板：

```
PerkSpec.Effects[]
  effect[N]: AddActivateChoice
    EntryPoint = FilterActivation
    ButtonLabel = "（動畫）開門"
    Flags.FragmentIndex = N
    Flags.RunImmediately = true
    Conditions = [GetIsID(target, DoorFormId)]

Perk.VirtualMachineAdapter (PerkAdapter)
  ScriptFragments.FileName = "PF_MyPerk"
  Fragments[N].FragmentIndex = N

PF_MyPerk.psc (extends Perk)
  Quest Property CentralScript Auto   ← 綁定到 quest script FormKey
  Function Fragment_N(Actor a, ObjectReference t)
      CentralScript.HandleActivate(a, t, N)   ; 或直接展開動作
  EndFunction
```

比較既有 ModForge fragment 家族：

| fragment 家族 | 目標 record | script extends | 函式簽名 | adapter 型別 |
|---|---|---|---|---|
| TIF（dialogue） | DialogResponses | `TopicInfo` | `Fragment_0(ObjectReference akSpeakerRef)` | `DialogResponsesAdapter` |
| QF（quest stage） | Quest | `Quest` | `Fragment_Stage_XXXX_Item00000()` | `QuestAdapter` |
| SF（scene phase） | Scene | `Scene` | `Fragment_N()` | `SceneAdapter` |
| **PF（perk effect）** | **Perk** | **`Perk`** | **`Fragment_N(Actor, ObjectReference)`** | **`PerkAdapter`** |

---

## 六、ModForge 現有 Perks builder 能做什麼 + 缺口

### 6-1 現有能力（有 code 為據）

`Generator.Build.Perks.cs:WirePerks()` + `Spec.Perks.cs:PerkEffectSpec`：

- **`kind="ability"`**：emit `PerkAbilityEffect`，`Spell` → FormLink → SPEL（✅）
- **`kind="entryPoint"`**：只 emit `PerkEntryPointModifyValue`，function 支援 Set/Add/Multiply，`Value` 為 float（✅ 已支援 ModifyValue 類）
- **PerkConditionTabCount**：自動查 `EntryPointTabCount` 表，unlisted fallback = 2（✅ 有保護）
- **effect-level conditions**：`PerkCondition { RunOnTabIndex = 0 }` + `BuildCondition()`，支援 HasKeyword/GetIsID 等（✅）
- **perk-level conditions**：plain `Condition` list（✅）
- **診斷工具**：`perkdiag` 可印出 perk 所有 effects；`perkdiag entrypoints` 列出所有 EntryType 名稱（✅）

### 6-2 確認缺口（直接引自 mod-survey-gaps.md 缺口 #1）

> **Perk entry-point `AddActivateChoice` / `SetText` + fragment 膠水**——（最高價值，#1）`Generator.Build.Perks.cs:WirePerks()` 只 emit `PerkEntryPointModifyValue`（+ `ability`），`EntryPointTabCount` 表也沒列這兩個。Immersive Interactions 需 29× AddActivateChoice + 4× SetText。scope：新增 `entrypoint` 子類 emit `PerkEntryPointAddActivateChoice`（帶 GetIsID/keyword/FLST 條件）與 `PerkEntryPointSetText` + Perk-fragment dispatcher（VMAD `Extends Perk`、`Fragment_N`→quest-script call，仿 `Generator.Build.Scripts.cs` 既有 dialogue/scene fragment 膠水）。注意 `perk-conditiontabcount-ctd`。

具體缺口：

| 缺口 | 現狀 | 需補 |
|---|---|---|
| `PerkEntryPointAddActivateChoice` emit | 不支援（WirePerks 只 emit ModifyValue） | 新 emit 路徑 |
| `PerkEntryPointSetText` emit | 不支援 | 新 emit 路徑 |
| `ButtonLabel`（LString）設定 | PerkEffectSpec 無此欄位 | 新 spec 欄位 |
| `Flags.FragmentIndex` 設定 | PerkEffectSpec 無此欄位 | 新 spec 欄位 |
| `Flags.RunImmediately / ReplaceDefault` | 無 | 新 spec 欄位 |
| `PerkAdapter` + `PerkScriptFragments` VMAD | Perk 完全無 VMAD 生成 | 新 pass（仿 AttachSceneFragments） |
| `PF_<perk>.psc` fragment 腳本生成 | 無 perk fragment 生成器 | 新 GeneratePerkFragmentSource() |
| `AddActivateChoice` tab-count | FilterActivation(74) 已在 EntryPointTabCount 表（= 2）✅ | SetActivateLabel(81) 未在表 ⚠️ |

---

## 七、實作建議

以下推斷 builder 需要的改動，標 ⚠️ 表示未驗 code 細節。

### 7-1 PerkEffectSpec 擴充

```csharp
public sealed class PerkEffectSpec
{
    // ... 現有欄位 ...

    // --- AddActivateChoice / SetText 新欄位 ---
    public string ButtonLabel { get; set; } = "";    // ⚠️ LString（選單/按鈕文字）
    public int FragmentIndex { get; set; } = -1;     // ⚠️ >= 0 代表此 effect 需 fragment
    public bool RunImmediately { get; set; }         // ⚠️ PerkScriptFlag.Flag
    public bool ReplaceDefault { get; set; }         // ⚠️ PerkScriptFlag.Flag

    // --- SetText 新欄位 ---
    public string LabelText { get; set; } = "";      // ⚠️ PerkEntryPointSetText.Text
}
```

或用 `kind` 細分：`"addActivateChoice"` / `"setText"` / `"selectSpell"` / `"addLeveledItem"` 各自映射不同 Mutagen 類別。

### 7-2 WirePerks() 新 emit 路徑

```csharp
case "addactivatechoice":
{
    var ef = new PerkEntryPointAddActivateChoice
    {
        EntryPoint = entry,                          // 通常 FilterActivation
        Rank = ..., Priority = ...,
        ButtonLabel = es.ButtonLabel,
        PerkConditionTabCount = EntryPointTabCount.GetValueOrDefault(entry, (byte)2),
    };
    if (es.FragmentIndex >= 0)
        ef.Flags = new PerkScriptFlag
        {
            FragmentIndex = (ushort)es.FragmentIndex,
            Flags = (es.RunImmediately ? PerkScriptFlag.Flag.RunImmediately : 0) | ...,
        };
    if (!string.IsNullOrEmpty(es.Spell))
        Resolve(..., fk => ef.Spell.SetTo(fk));
    effect = ef;
    break;
}
case "settext":
{
    var ef = new PerkEntryPointSetText
    {
        EntryPoint = entry,                          // 通常 SetActivateLabel
        PerkConditionTabCount = EntryPointTabCount.GetValueOrDefault(entry, (byte)2),
        Text = es.LabelText,
    };
    effect = ef;
    break;
}
```

⚠️ `SetActivateLabel` (81) 需加入 `EntryPointTabCount` 表（建議值 = 2，與 vanilla 其他互動型 entry-point 一致）。

### 7-3 PerkAdapter VMAD 生成（新 pass）

仿 `AttachSceneFragments()`，新增 `AttachPerkFragments()`：

```
條件：PerkSpec 至少有一個 effect FragmentIndex >= 0
輸出：Perk.VirtualMachineAdapter = PerkAdapter
       { ScriptFragments = { FileName = "PF_<EditorId>",
                              ExtraBindDataVersion = 2,
                              Fragments = [per-effect PerkScriptFlag] } }
```

⚠️ 需確認 `PerkAdapter.Version` / `ObjectFormat` 的 vanilla canonical 值（預估 5/2，與其他 adapter 一致）。
⚠️ 需在 `Generator.Build.cs` 的 build pass 序列中安排：在 `WirePerks()` 之後執行（因為需要 effect 的 FragmentIndex 已設好）。

### 7-4 Perk fragment 腳本生成（GeneratePerkFragmentSource）

新增純函式（仿 `GenerateSceneFragmentSource`）：

```papyrus
Scriptname PF_<EditorId> extends Perk Hidden
; AUTO-GENERATED by ModForge — perk activate-choice fragment dispatcher.

Quest Property CentralScript Auto     ; ← bound to quest FormKey

Function Fragment_0(Actor akActor, ObjectReference akTargetRef)
    ; effect[0]: <ButtonLabel>
    CentralScript.<handler>(akActor, akTargetRef)
EndFunction

Function Fragment_1(Actor akActor, ObjectReference akTargetRef)
    ; ...
EndFunction
```

⚠️ 函式簽名固定為 `(Actor, ObjectReference)`，與 TIF / QF / SF 不同——務必不要混用。
⚠️ `Quest Property CentralScript Auto` 由 `package` 綁 FormKey（仿 `TIF` 的 OwningQuest 綁定模式）。
⚠️ `compile` → `.pex` → `AttachPerkFragments()` 需要 `.pex` 存在才掛 VMAD（仿 `AttachSceneFragments` 的 `File.Exists` 檢查）。

### 7-5 EntryPointTabCount 表補充

```csharp
// ⚠️ 待補：
[APerkEntryPointEffect.EntryType.SetActivateLabel] = 2,
// FilterActivation (74) = 2 已在表中 ✅
```

### 7-6 perkdiag 擴充

`Diagnostics.Perks.cs:PerkDiag()` 現在對非 `IPerkEntryPointModifyValueGetter` 的 effect 只印 `({e.GetType().Name})`——實作後建議補印 `ButtonLabel`、`FragmentIndex`、`Text` 等欄位，方便驗證生成結果。

---

## 相關筆記連結

- `perk-conditiontabcount-ctd`（記憶）：tab-count byte 必須非 0，否則 load CTD；FilterActivation=2 已修；SetActivateLabel 未補
- `immersive-interactions.md`：Immersive Interactions 完整機制拆解，真實 AddActivateChoice 實例（29 effects + 4 SetText）
- `arrowblock.md`：ModIncomingDamage Set 0 的 entry-point 模式，現有 builder 可生成的代表性案例
- `scene-playidle-recipe.md`：SceneAdapter fragment 膠水，PerkAdapter 可仿此模式
- `dispatcher-magic-trigger.md`：perk fragment → quest script dispatcher 模式的同源概念
- `mod-survey-gaps.md`（roadmap）：缺口 #1 正式定義
