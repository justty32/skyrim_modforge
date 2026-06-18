# AddActivateChoice + SetText 深挖

← [perk-entry-points](perk-entry-points.md)

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

