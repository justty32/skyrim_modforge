# Story Manager 子系統拆解

> 來源：ModForge `src/ModForge.Core/Generator.Build.StoryManager.cs`、`Generator.Validate.StoryManager.cs`、`Spec.StoryManager.cs`、`StoryManagerEvents.cs`、`Diagnostics.StoryManager.cs`；mod-survey findings `extended-encounters.md`、`immersive-world-encounters.md`；roadmap `mod-survey-gaps.md`。

---

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

## 五、多層巢狀 SMBN 設計

### 5a. 需求場景

EE 與 IWE 都使用「依 location keyword 路由」的中間層 SMBN，例：

```
SMEN（WEQuests, ScriptEvent root）
  └─ EE_ChangeLocation（SMBN，ChangeLocation 加法子）
       ├─ EE_WI_LocTypeTown（SMQN，條件：HasKeyword LocTypeTown）→ 一堆城市遭遇 quest
       ├─ EE_WI_LocTypeBanditCamp（SMQN）→ 一堆土匪營遭遇 quest
       └─ ...（30 個 quest node）
```

這種「SMBN → 多個 SMQN（各自帶 keyword 條件）→ 多個候選 quest」的兩層結構，今天的 ModForge **無法直接生成**。

### 5b. Record 層面的需求

要實現任意深度 SMBN 子樹，需要：

1. **spec 層面**：新增 `StoryManagerBranchNodeSpec`（帶 EditorID、parent 指向 vanilla root 或另一個 SMBN、conditions[]、子 SMQN / 子 SMBN 清單）。目前 `QuestStoryEventSpec` 只讓 quest 宣告「我要掛到某事件」，沒有獨立的 SMBN spec 類型。

2. **builder 層面**：修改 `BuildStoryManager()` 的兩個地方：
   - **SMBN parent** 改為可指向另一個 SMBN（當前硬寫為 `def.Root`）
   - **SMQN parent** 改為可指向 user-declared 的中間層 SMBN（而非只能是 auto-generated branch）

3. **PreviousSibling 鏈問題**：
   - 同層兄弟 SMBN（互斥路由）：須鏈
   - 同層兄弟 SMQN（並列評估）：須鏈
   - **兩者可同時存在**：一個 SMBN 可有子 SMBN（互斥）也可有子 SMQN（並列），engine 規則：先走子 SMBN（互斥命中一個）OR 走 SMQN（全部走）——兩者類型不同、獨立計算

4. **Quest.Event 的 code 必須對應正確的 event 種類**：即使 quest 被一個中間層 SMBN 隔著，它的 `Quest.Event` RecordType code 仍必須對應最上層 SMEN 的 `Type`（否則引擎無法建立 event data → alias fromEvent 填充失敗）。

### 5c. PreviousSibling 鏈 vs Children tree

Skyrim 的 record 結構沒有直接的「children 列表」欄位——節點關係靠兩個機制組合：

| 機制 | 方向 | 誰持有 | 說明 |
|------|------|--------|------|
| `Parent` | 子→父 | 子節點持有 | 宣告「我屬於哪個節點」 |
| `PreviousSibling` | 後→前 | 後加入的節點持有 | 把同層節點串成鏈；引擎依此走所有兄弟 |

**兩者同時使用**：所有同層節點都需設 `Parent`（相同父節點），且除第一個外都需設 `PreviousSibling` 指向前一個兄弟。ModForge `BuildStoryManager()` 已用 `lastQNodeByBranch` dict 正確實作 SMQN 的 `PreviousSibling` 鏈——缺的是 SMBN 也做同樣的鏈（當有多個 SMBN 接同一 root 時）。

### 5d. `additiveParent`（加法父節點）的角色

「additive」不是一個 record 欄位名，而是 **Mutagen 的 override 機制**：把 vanilla record FormKey 加入 mod 的 override 集合，只改想改的欄位，其他走 fall-through 讀 vanilla 值。SM 的 additive 接法具體是：

- 建新 SMBN，`branch.Parent.SetTo(def.Root)` 指向 vanilla SMEN root
- 引擎載入時，所有 `Parent == [vanillaRootFormKey]` 的 SMBN 都成為該 root 的子節點
- 不需要 override vanilla SMEN record 本身 → 零衝突

這就是 memory `story-manager-kill-recipe` 的「SMBN additive-parents the vanilla root」——**additive 的意思就是這個：把子節點的 Parent 指向 vanilla record，不動 vanilla record 自身**。

---

## 六、設計模式（從真實 mod finding 抽出的 SM 慣用模式）

### 模式 A：純 SM 路由骨架 quest（Extended Encounters 風格）

用途：不需要日誌/目標的路邊遭遇容器。

```
SM 事件 → SMBN（依 location 類型分流）→ SMQN（一個遭遇種類）→ Quest
Quest：
  - type=None, event=SCPT/CLOC/KILL
  - Stages: 0(StartUp) / 10(演出) / 255(ShutDown)，log 全空
  - Aliases: XMarker trigger + scene center + 演員 alias（含 navmesh tester）
  - 無 objective，純當「alias 容器 + QF fragment 狀態機」
```

特徵：30 個 SMQN 依 LocType keyword 路由（`EE_WI_LocTypeTown`、`EE_WI_LocTypeBanditCamp`…），每個 SMQN 底下一批候選 quest + `GlobalShort` 權重控制開關。

### 模式 B：多候選 SMQN + 條件多樣化（Immersive World Encounters 風格）

用途：讓同一觸發事件有多種遭遇選擇（37 個 quest node，7 個 branch）。

```
SMEN root
  └─ SMBN（主分流，條件：如 isWilderness）
       ├─ SMQN（EE_SetteRoads，條件：LocType 道路）
       │    └─ Quests[]: [WE_押送隊, WE_雙人決鬥, WE_賞金獵人, ...]（各帶條件/權重）
       └─ SMQN（EE_SetteFactions，條件：陣營衝突）
            └─ Quests[]: [...]
```

特徵：SMQN 級別的分桶（按遭遇種類命名），每個 SMQN 底下多個候選 quest，引擎在命中 SMQN 後再從候選 quest 裡抽一個——形成「兩階段隨機」。EditorID 命名即路由表（`WE_Sette*`、`WI_Sette*`）。

### 模式 C：ScriptEvent 自訂觸發（ModForge dispatcher 風格）

用途：Papyrus 腳本主動丟事件觸發 SM，而不等引擎自然觸發（見 memory `dispatcher-magic-trigger`）。

```
Papyrus: Keyword.SendStoryEvent(myKYWD, ref1, ref2, loc)
  → SM ScriptEvent root（0x01379A）
  → SMBN（條件 GetEventData/GetIsID Keyword == myKYWD）
  → SMQN → Quest（alias fromEvent:ref1/ref2/loc 拿到 Papyrus 傳來的 ref）
```

特徵：branch 的 keyword 過濾是「只有用 myKYWD 發的 SendStoryEvent 才命中此 branch」，不同 keyword 走不同 branch（互斥），ModForge 自動按 `root|keyword` key 做分 branch。

### 模式 D：SM quest 無需 journal（普遍反模式破除）

- **不要**給 SM-driven radiant quest 加 `startUpStage`（有的話 quest 的第一個 objective 會出現在玩家 journal）
- stage log 文字全留空（純狀態機）
- 這是 EE（330 個遭遇 quest）和 IWE（148 個遭遇 quest）的共同做法
- 對比：只有涉及玩家主動目標的 quest（如 IWE 的招募跟隨者 quest）才設 objective

### 模式 E：alias fromEvent 的型別對齊

- 槽位 `L1`/`L2`（首字節 `'L'`）→ `alias.Type = Location`
- 槽位 `R1`/`R2`（首字節 `'R'`）→ `alias.Type = Reference`
- 型別不對齊 → 引擎填充 null → quest 無法啟動（in-game 驗證過的陷阱，已在 `BuildQuestAliases` 修正）
