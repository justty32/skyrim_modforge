# Story Manager spec 管線（階段二）— 設計

> 日期：2026-06-04 · 狀態：已核可（使用者授權自主定案），待寫 plan
> 前置：階段一探針已實機 PASS（見 `docs/minor/ideas.md` 第 9 節 + `docs/superpowers/archive/plans/2026-06-04-story-manager-probe.md`）
> 硬知識：[[story-manager-kill-recipe]]（根 0x013010、Event "KILL"、slot R1/R2、SimpleActor quirk）

## 目的

把階段一探針驗證過的 Story Manager 能力，從 throwaway builder 提升成 ModForge **spec→build 管線**的一等公民。
使用者在 spec.json 宣告「一個被事件觸發、會從事件資料選角的模板任務」，ModForge 自動產出
SMBN→SMQN 節點樹（additive 掛在原版事件根下）+ 帶條件式 alias 的 Quest。這是大量劇情自動生成
（IDEAS 第 9 節）的可量產底座。

## 核可的決策（2026-06-04 brainstorm；第二節起使用者授權自主定案）

1. **抽象層級＝意圖導向**：使用者不直接寫 SMEN/SMBN/SMQN。在 Quest 上宣告 `storyEvent` + `aliases`，
   ModForge 自動生節點樹並接到內建「事件名→原版根」表。藏掉記錄機制，貼合量產模板願景。
2. **alias 填充型別＝`fromEvent` + `forced`**。`findMatching` 及其他延後（YAGNI；避免靜默失敗除錯）。
3. **事件覆蓋＝只做 KillActor**。事件表設計成一行一事件，之後加事件是純資料工。
4. **`fill` 用字串語法**（`fromEvent:victim` / `forced:<ref>`），與既有 spec 的緊湊風格一致。
5. 探針 `StoryManagerProbe.cs` 與 CLI `smprobe` 在 spec 路徑通後**刪除**；CLI `smtree` **保留**（解事件根仍有用）。

## Schema（spec 新增）

`QuestSpec` 新增兩個可選欄位（不影響既有 quest；無 `storyEvent` 時行為完全不變）：

```jsonc
"quests": [{
  "editorId": "MFSM_Avenge",
  "name": "復仇",
  "stages": [{ "index": 10 }],
  "storyEvent": {                  // 有此塊 = 可被 SM 啟動
    "event": "KillActor",          // 友善事件名（階段二只認 KillActor）
    "conditions": []               // 可選：事件條件（沿用既有 ConditionSpec）
  },
  "aliases": [
    { "name": "Victim", "fill": "fromEvent:victim" },                 // R1 = 被殺者
    { "name": "Killer", "fill": "fromEvent:killer", "optional": true },// R2 = 兇手
    { "name": "Boss",   "fill": "forced:SomeBossEditorId" }           // 寫死特定 ref
  ]
}]
```

新型別（新檔 `src/ModForge.Core/Spec/Spec.StoryManager.cs`）：

```csharp
public sealed class QuestStoryEventSpec
{
    public string Event { get; set; } = "";                  // 事件名，查事件表
    public List<ConditionSpec> Conditions { get; set; } = new();
}
public sealed class QuestAliasSpec
{
    public string Name { get; set; } = "";
    public string Fill { get; set; } = "";                   // "fromEvent:<slot>" | "forced:<ref>"
    public bool Optional { get; set; }                       // → QuestAlias optional flag
}
```

`QuestSpec` 加：
```csharp
public QuestStoryEventSpec? StoryEvent { get; set; }
public List<QuestAliasSpec> Aliases { get; set; } = new();
```

`fill` 字串語法：
- `fromEvent:<slot>` — slot 是事件相關友善名。KillActor：`victim`(R1)、`killer`(R2)。
- `forced:<ref>` — ref 是 EditorId 或 `Plugin.esm:0xFORMID`（沿用既有 ref 解析助手）。

## 事件表（新檔 `src/ModForge.Core/StoryManagerEvents.cs`）

純資料 + 解析助手。一個事件一筆：

```
KillActor → {
   Root  = Skyrim.esm:0x013010,
   Code  = RecordType("KILL"),
   Slots = { "victim" → [0x52,0x31,0x00,0x00] /*R1*/, "killer" → [0x52,0x32,0x00,0x00] /*R2*/ }
}
```

API：`bool TryGet(string eventName, out StoryEventDef def)`；def 提供 Root/Code/Slots。
fill 解析助手：給 `(QuestAliasSpec, StoryEventDef)` 回傳「要在 QuestAlias 上設什麼」（FromEvent 資料或 forced ref）。

## Build step（新檔 `src/ModForge.Core/Build/Generator.Build.StoryManager.cs`）

Orchestrator（`Generator.Build.cs`）在 `ctx.BuildQuests()` 之後插一行 `ctx.BuildStoryManager()`。
（quests 已建好並存在 `questsByEd`；SM 節點在 quest 之後建，FormID 順序安全。）

每個有 `storyEvent` 的 quest：
1. 從 `questsByEd[editorId]` 取已建 Quest 記錄。查事件表得 def（查不到 = build 前已被 validator 擋）。
2. `Quest.Event = def.Code`；`Quest.EventConditions` 由 `storyEvent.conditions` 經既有 `BuildCondition` 產生；
   **強制清 `Quest.Flag.StartGameEnabled`**（SM 啟動的 quest 不能開局自跑）。
3. 依序為每個 `QuestAliasSpec` 建 `QuestAlias`（ID 連續、設 `NextAliasID`）：
   - `fromEvent:<slot>` → `FindMatchingRefFromEvent { FromEvent = def.Code, EventData = def.Slots[slot] }`
   - `forced:<ref>` → `ForcedReference.SetTo(解析後的 FormKey)`
   - `optional:true` → 設 alias 的 optional flag
4. 生 **一條 SMBN**（`mod.StoryManagerBranchNodes.AddNew`，`Parent.SetTo(def.Root)`，無條件）
   + **一條 SMQN**（`Parent.SetTo(branch)`，`Quests=[StoryManagerQuest{Quest→本 quest}]`）。
   一 quest 一組節點，PNAM-additive，多 quest 互不干擾。

## Validator（新檔 `src/ModForge.Core/Validate/Generator.Validate.StoryManager.cs`，掛進既有 validate 派發）

- `storyEvent.event` 必須是已知事件名 → 否則 **錯誤**（列出支援清單，目前 `KillActor`）。
- 每個 alias `fill` 語法合法：`fromEvent:` 的 slot 對該事件存在；`forced:` 的 ref 可解析 → 否則 **錯誤**。
- `storyEvent` 同時顯式 `startGameEnabled:true` → **警告**（會被強制 false）。
- 含 `storyEvent` 的 quest 在 ESL 插件裏 → **警告**（ESL+SM 尚未實機驗，比照 worldspace 保守處置，不硬擋）。

## 資料流

```
spec.json (quest.storyEvent=KillActor + aliases)
  → validate（事件名/fill/ESL 檢查）
    → build：BuildQuests 建 Quest → BuildStoryManager 套 Event/EventConditions/aliases + 生 SMBN/SMQN
      → package/build 寫 esp（既有 PluginIo.Write）
遊戲內：殺一個完整 actor（非 SimpleActor）
  → 引擎發 KILL 事件 → 走原版根 0x013010 → 我們的 SMBN（無條件）→ SMQN → 啟動 quest
    → Victim alias 用 FromEvent 填被殺者
驗證：sqv <quest> → 任務 running + Victim = 被殺者 FormID
```

## 測試

**單元（不需 Skyrim.esm，結構斷言）**：
- build：含 storyEvent 的 quest → `Quest.Event` 設為 "KILL"、`StartGameEnabled` 被清、EventConditions 數量正確；
  `fromEvent:victim` alias → FindMatchingRefFromEvent{FromEvent="KILL", EventData=R1}；`forced:` alias → ForcedReference 對；
  生成恰好一條 SMBN（Parent=0x013010）+ 一條 SMQN（Parent=branch、Quests 連到本 quest）。
- build：無 storyEvent 的既有 quest 行為不變（回歸保護）。
- validator：未知事件名→錯誤；壞 fill（未知 slot / 不可解析 forced ref）→錯誤；storyEvent+startGameEnabled→警告。

**實機（沿用探針已驗配方，但改走完整 spec 管線）**：
手寫一個 `examples/` spec.json（一個 KillActor storyEvent quest + Victim fromEvent alias）→ `package` → FLAT zip →
MO2 → 殺一頭牛（非 SimpleActor）→ `sqv` 看 Victim 填上。PASS = spec 管線端到端通。

## 檔案

- 新：`src/ModForge.Core/Spec/Spec.StoryManager.cs`（QuestStoryEventSpec / QuestAliasSpec），`QuestSpec` 加兩欄位
- 新：`src/ModForge.Core/StoryManagerEvents.cs`（事件表 + fill 解析）
- 新：`src/ModForge.Core/Build/Generator.Build.StoryManager.cs`（build step）+ orchestrator 插一行
- 新：`src/ModForge.Core/Validate/Generator.Validate.StoryManager.cs`（validation）+ validate 派發掛接
- 新：`examples/story-manager-kill.json`（實機樣本 spec）
- 改/刪：spec 路徑通後刪 `StoryManagerProbe.cs` + CLI `smprobe` dispatch/Usage；保留 `smtree`
- 測試：`tests/ModForge.Core.Tests/Build/StoryManagerBuildTests.cs` + `StoryManagerValidateTests.cs`；
  探針的 `StoryManagerProbeTests.cs` 隨 builder 一起刪

## 範圍外（YAGNI）

- 自訂 Keyword + `SendStoryEvent`（Script Event 入口）——量產最終入口，但需 Papyrus，另立題目
- `findMatching` / location / createRef / uniqueActor 等填充型別
- KillActor 以外的事件（表可擴充，逐個離線解碼 + 實機驗後再加）
- 共享 SMBN 優化（目前一 quest 一組節點；夠用）
