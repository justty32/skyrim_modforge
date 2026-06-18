# 架構概覽 + 各 Record 欄位結構

← [sm-subsystem](sm-subsystem.md)

## 一、SM 子系統做什麼 + 架構概覽

**Story Manager（SM）** 是 Skyrim 引擎的「事件→quest 路由器」：當玩家觸發特定引擎事件（殺人、換地點、施法、升級、Papyrus 腳本事件…），引擎走一棵 record 樹，依條件與權重選出一個（或多個）候選 quest 啟動。這是所有「路邊遭遇」（World Encounter）、「境況任務」、「情境觸發」的底層骨架。

### Record 樹結構

```
SMEN（Story Manager Event Node）
  └─ 事件根（vanilla Skyrim.esm 定義，如 KillActor root 0x013010）
       └─ SMBN（Story Manager Branch Node）
            ├─ 可帶條件（CTDA）做分流
            ├─ 可巢狀子 SMBN（多層分桶路由）
            └─ SMQN（Story Manager Quest Node）
                 ├─ 可帶條件 + 權重（max concurrent quests / random chance）
                 └─ Quests[]：一到多個候選 Quest
                      └─ Quest（含 Event/EventConditions、ReferenceAlias[]、Papyrus）
```

三種 record 類型對比：

| 類型 | 完整名稱 | 角色 | 關鍵欄位 |
|------|----------|------|----------|
| `SMEN` | StoryManagerEventNode | 事件根（vanilla 已有，mod 不新建） | `Type`（KillActorEvent / ChangeLocationEvent / ScriptEvent…）、`MaxConcurrentQuests` |
| `SMBN` | StoryManagerBranchNode | 分流節點；children 可為 SMBN 或 SMQN | `Parent`（指向上層 SMEN 或 SMBN）、`Conditions[]`（CTDA）、`PreviousSibling`（同層兄弟鏈） |
| `SMQN` | StoryManagerQuestNode | 葉節點，掛實際 quest | `Parent`（指向 SMBN）、`Quests[]`（候選 quest list，各有權重/條件）、`MaxConcurrentQuests`、`PreviousSibling` |

### Additive（加法）掛載模式

Mod 不替換 vanilla SMEN root，而是以 **additive override** 把自己的 SMBN 接到 vanilla root 的 `Parent` 欄位，形成「附加子節點」。引擎在走完 vanilla 樹後也走 additive 子樹。vanilla SMEN root 的 FormID 由 `StoryManagerEvents.cs` 離線解出並硬編碼（見第三節）。

---

## 二、各 Record 類型的欄位結構

### 2a. SMEN（StoryManagerEventNode）

由 vanilla Skyrim.esm 定義，mod 不新建；以 `smtree` CLI verb（`Diagnostics.StoryManager.cs`）枚舉已知根節點。

欄位（從 Mutagen 介面推斷）：
- `EditorID`：vanilla 事件名（例 `WEQuests`、`WIKillActor`）
- `Type`：`IStoryManagerEventNodeGetter.Type`，對應引擎內建事件種類（如 `KillActorEvent`、`ChangeLocationEvent`、`ScriptEvent`）
- `MaxConcurrentQuests`：同時可跑幾個此事件觸發的 quest
- `Flags`：（vanilla 有的欄位，內容省略）

### 2b. SMBN（StoryManagerBranchNode）

由 mod 新建並接到 vanilla SMEN root（或另一個 SMBN）：

| 欄位 | 型別 | 說明 |
|------|------|------|
| `EditorID` | string | 命名慣例：`MFSM_{Event}_SMBranch` 或 `MFSM_{Event}_{Keyword}_SMBranch` |
| `Parent` | FormLink | 指向父節點（SMEN root 或上層 SMBN）；**additive 接法** |
| `Conditions[]` | CTDA | 此分支的過濾條件（如 ScriptEvent 的 keyword 過濾：`GetEventData/GetIsID Keyword == 1`） |
| `PreviousSibling` | FormLink? | 同層兄弟鏈（兄弟 SMBN 必須鏈否則引擎只走最後一個） |
| （Flags） | — | vanilla 有 Chance/Random/AllBelow 等，ModForge 目前未設 |

### 2c. SMQN（StoryManagerQuestNode）

| 欄位 | 型別 | 說明 |
|------|------|------|
| `EditorID` | string | 命名慣例：`{QuestEditorId}_SMQuestNode` |
| `Parent` | FormLink | 指向父 SMBN |
| `PreviousSibling` | FormLink? | 同層兄弟鏈，**必須鏈，否則引擎只命中最後一個 quest node** |
| `MaxConcurrentQuests` | int | 同時允許幾個（通常 1） |
| `Quests[]` | List\<StoryManagerQuest\> | 每條含 `Quest` FormLink（+ 原版還有 `Count`/條件等欄位） |

### 2d. Quest 端的 SM 欄位

`Quest` record 本身要配合 SM 設定：

| 欄位 | 說明 |
|------|------|
| `Event` | RecordType（4 字元碼，如 `KILL`/`CLOC`/`SCPT`），對應觸發它的 SMEN 類型 |
| `EventConditions[]` | CTDA 條件，引擎在命中此 quest node 前先評估 |
| `Flags` 中 `StartGameEnabled` | SM quest 必須**清除**此旗標（否則 quest 在遊戲開始時就常駐啟動，不等 SM 觸發） |
| `Aliases[]` | `QuestAlias`（含 fill 策略，見二.e） |

### 2e. QuestAlias 填充模式

別名（alias）是 SM 觸發時「事件帶來的 ref」的接收容器，fill 策略：

| fill 語法 | 行為 | 對應 QuestAlias 欄位 |
|-----------|------|---------------------|
| `fromEvent:<slot>` | 拿事件攜帶的 ref（如 victim/killer/newLocation） | `FindMatchingRefFromEvent`（含 `FromEvent` code + `EventData` 4-byte slot） |
| `forced:<ref>` | 寫死一個特定 ref（vanilla 或 in-spec） | `ForcedReference` |
| `uniqueActor:<ref>` | 指向唯一 NPC base（自動帶 `AllowReserved`） | `UniqueActor` |
| `createObject:<ref>@<alias>` | 在另一個 alias 的位置生成一個新 ref | `CreateReferenceToObject`（含 `AliasID`、`Object`、`Create=At`） |
| `findMatching:closest\|any` | 在已載入區域找一個符合 Conditions 的現有 ref | `Flags |= MatchingRefInLoadedArea[+MatchingRefClosest]` |

---

