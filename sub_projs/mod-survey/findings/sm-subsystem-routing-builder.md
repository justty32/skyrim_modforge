# 事件路由機制 + ModForge 現有 builder/缺口

← [sm-subsystem](sm-subsystem.md)

## 三、事件路由機制

### 3a. 引擎走樹的順序

1. **事件觸發**：引擎偵測到（例如某 Actor 死亡）→ 找到對應 SMEN root（`KillActorEvent`）
2. **走 SMBN 子樹**：從 root 的所有子 SMBN 開始，逐一評估各自的 `Conditions[]`
   - 兄弟 SMBN 之間是**互斥的**（mutual exclusive handlers）：引擎命中一個就不再走其他兄弟
   - 所以如要「多個 branch 都可能命中」，要用**多個 SMQN** 掛在同一 branch 下（而非多個 branch）
3. **走 SMQN 鏈**：在命中的 SMBN 下，按 `PreviousSibling` 鏈**順序走所有 SMQN**（兄弟 SMQN 不互斥）
4. **選候選 quest**：每個 SMQN 的 `Quests[]` 裡可有多個候選，引擎依各自條件/權重選一個
5. **啟動 Quest**：選中的 quest 填充 aliases（`fromEvent` slot 對應此次事件的 victim/killer/location 等）→ quest 開始跑

### 3b. 各事件的 root FormID 與槽位（StoryManagerEvents.cs 的硬編碼表）

| 事件名（spec 用） | SMEN root（Skyrim.esm） | Quest.Event 碼 | 槽位（slot） |
|-------------------|------------------------|----------------|-------------|
| `KillActor` | `0x013010` | `KILL` | `victim`=R1、`killer`=R2 |
| `ChangeLocation` | `0x01320E` | `CLOC` | `oldLocation`=L1、`newLocation`=L2 |
| `CastMagic` | `0x046829` | `CAST` | `caster`=R1、`target`=R2、`location`=L1 |
| `AddItem` | `0x02C439` | `AIPL` | `owner`=R1、`location`=L1 |
| `Assault` | `0x02C494` | `ASSU` | `victim`=R1、`attacker`=R2、`location`=L1 |
| `CraftItem` | `0x039D86` | `CRFT` | `workbench`=R1 |
| `PlayerRemoveItem` | `0x02C6AC` | `REMP` | `owner`=R1、`item`=R2 |
| `Arrest` | `0x06B369` | `ARRT` | `guard`=R1、`criminal`=R2 |
| `IncreaseLevel` | `0x05BD79` | `LEVL` | （無 ref 槽，靠 conditions gate） |
| `ScriptEvent` | `0x01379A` | `SCPT` | `ref1`=R1、`ref2`=R2、`loc`=L1 |

槽位 encoding：R1=`52 31 00 00`、R2=`52 32 00 00`、L1=`4C 31 00 00`、L2=`4C 32 00 00`（ASCII "R1"/"R2"/"L1"/"L2" + NUL×2）。

### 3c. ScriptEvent 的 keyword 過濾

`ScriptEvent` 是唯一一個「由 Papyrus 主動觸發」的自訂事件，使用 `SendStoryEvent(KYWD, akRef1, akRef2, akLoc)` API。因為所有 Papyrus SendStoryEvent 都走同一個 SMEN root，SM 必須過濾出「屬於我這個 branch 的呼叫」。ModForge 的做法：

- branch 帶條件 `GetEventData Function=GetIsID Member=Keyword Record=<KYWD> == 1`
- 此 KYWD 必須在 `spec.keywords[]` 裡宣告（validator 校驗）
- 不同 keyword → 不同 branch（互斥）；相同 keyword 的 quest 共用一個 branch

---

## 四、ModForge 現有 SM builder 能做什麼（有 code 為據）＋確認缺口

### 4a. 現有能力（`Generator.Build.StoryManager.cs`）

**`BuildStoryManager()` 的完整流程（code 為據）**：

1. 遍歷 `spec.Quests`，凡帶 `StoryEvent` 區塊的 quest：
   - 設 `Quest.Event = def.Code`（4 字元碼）
   - 清除 `Quest.Flags & StartGameEnabled`
   - 把 `se.Conditions[]` 填進 `Quest.EventConditions`
   - 呼叫 `BuildQuestAliases()`
2. 按 `root|keyword` 計算 branch key，**同一 key 共用一個 SMBN**（只建一次）
   - 若為 ScriptEvent，加 `GetEventData/GetIsID Keyword` 條件到 branch
   - `branch.Parent.SetTo(def.Root)` → additive 接到 vanilla root
3. 每個 quest 建一個 `SMQN`（`mod.StoryManagerQuestNodes.AddNew()`）：
   - `qnode.Parent.SetTo(branch)`
   - 以 `PreviousSibling` 鏈串同 branch 下所有 quest node（`lastQNodeByBranch` dict 追蹤末尾）
   - `qnode.Quests.Add(entry)` 一個 quest node 掛一個 quest

**`BuildQuestAliases()` 的五種 fill 模式**（共用於 SM quest 與 standalone quest）：
`fromEvent` / `forced` / `uniqueActor` / `createObject` / `findMatching`（詳見二.e）。

**`smtree` diagnostic verb**（`Diagnostics.StoryManager.cs`）：用 overlay 枚舉任何 esp/esm 的 SMEN record，印出 FormKey/EditorID/Type/MaxConcurrentQuests，供離線辨識 vanilla event root FormID。

### 4b. 確認缺口（已在 roadmap 驗證，直接引用）

引用自 [roadmap/mod-survey-gaps.md](../../workflows/roadmap/mod-survey-gaps.md) 的「降級為 partial」條目：

> **SM branch/quest-node 子樹 + keyword 路由（降級為 partial）**
> `BuildStoryManager()` 已建 SMBN+SMQN、以 `PreviousSibling` 串同層 quest node、按 `root|keyword` 一分支路由（帶 `GetEventData/GetIsID Keyword` 條件）。**真缺**：只建 vanilla event root 下**單層**分支（兄弟 = quest node），不支援**任意深度/巢狀 SMBN 子樹**或非 vanilla event root。scope 收窄為「多層分支巢狀」。

⚠️ **缺口 #2（roadmap 序號）**：SM builder 不支援在 SMBN 下再建子 SMBN，亦無法讓 spec 宣告「我要當另一個 SMBN 的子節點」——目前 parent 固定是 vanilla event root。

---

