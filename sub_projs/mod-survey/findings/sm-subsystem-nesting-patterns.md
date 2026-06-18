# 多層巢狀 SMBN 設計 + 設計模式

← [sm-subsystem](sm-subsystem.md)

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
