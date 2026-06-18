# Global-as-selector：生命週期 + ModForge 評估

← [runtime-selector-patterns](runtime-selector-patterns.md)

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

