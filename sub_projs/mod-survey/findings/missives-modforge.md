# 對 ModForge 的參考價值

← [missives](missives.md)

## 3. 對 ModForge 的參考價值

Missives 是「**radiant quest 工廠**」的純粹樣本——它把本輪多份 survey 反覆指出的同一個缺口推到最高優先：**ModForge 目前不能生成 quest 的 alias 與 fill 模式**，而那正是所有 radiant 內容的命脈。

### ModForge 可生成的部分（表格）

| 機制 | 狀態 | 備註 |
|---|---|---|
| Quest stage + fragment（stage flags / StartUpStage / CompleteQuest / FailQuest） | ✅ 可生成 | `Generator.Build.QuestStages.cs` + `Generator.Build.Actors.cs:BuildQuests()` |
| Quest objective（QOBJ / DisplayText / QSTA target alias） | ✅ 可生成 | `DisplayText = o.Text`（pass-through，`<Alias=...>/<Global=...>` token 直接寫入即可）；QSTA 由 `WireObjectiveTargets()` 接 alias index |
| Quest alias（forced / uniqueActor / createObject / findMatching:loaded-area） | ✅ 可生成 | `BuildQuestAliases()`；four fill modes already wired |
| FLST 建立 + 填 item | ✅ 可生成 | `Generator.Build.Lists.cs:BuildFormLists()` / `WireFormLists()` |
| GlobalVariable 量產（Short/Float/Int type） | ✅ 可生成 | `Generator.Build.Globals.cs:BuildGlobals()` |
| LVLN（追捕目標）+ createObject alias fill | ✅ partial | createObject 帶 LVLN base 可生成；一等 LVLN fill mode 仍是 partial |
| BOOK（告示）+ Model（template clone） | ✅ 可生成 | `Generator.Build.Items.cs`；需 `template` 否則 crash |
| Message（MESG，`<Alias.ShortName=...><BaseName>` 動態命名） | ✅ 可生成 | `Generator.Build.Messages.cs`；token 直寫 Description 欄 |
| Container（CONT，裝 missive 的板子） | ✅ 可生成 | `BuildContainers()` |
| Activator（ACTI，觸發刷新的隱形 trigger box） | ✅ 可生成 | Placement + script attach |
| AI Package（追捕逃犯的 travel package） | ✅ 可生成 | `Generator.Build.Packages.cs` |
| LocationAlias fill（Find Matching Location by keyword） | ❌ 缺 | **roadmap #7**；所有 radiant 地點隨機化的核心，無此不能做 hold/dungeon/inn 隨機選 |
| nested ReferenceAlias（findNearAlias：在指定 location alias 範圍內找 ref） | ❌ 缺 | **roadmap #8**；Missives 的 boss/chest/questgiver alias 全靠此；不同於 findMatching loaded-area |
| UpdateCurrentInstanceGlobal fragment codegen | ❌ 缺 | **roadmap #9**；gather 計數 `<Global=Count>/<Global=Total>` 顯示的必要呼叫 |
| RegisterForUpdateGameTime alias script（時限任務計時器） | ⚠️ partial | 腳本本身可手寫交 package，但 fragment 生成器無此模式 |

### 新缺口（附 evidence）

**缺口 A：QuestAlias `findMatchingLocation` fill（LocationAlias 型）**

Evidence：`_M_QuestKillScript.psc` 第 7 行 `LocationAlias Property Alias_Hold Auto`；`_M_QuestCourierScript.psc` 第 41–43 行 `Alias_Destination`（LocationAlias）；ESP strings 中 `_M_QuestWhiterunKillBandit` 等均有 `Hold` + `Dungeon` 兩個 LocationAlias。目前 `QuestAliasSpec.Fill` 只支援 `fromEvent/forced/uniqueActor/createObject/findMatching`，沒有任何 `findMatchingLocation` 路徑，`QuestAlias.Type` 從未設為 `Location`（除了 fromEvent L-slot）。

**缺口 B：QuestAlias `findMatchingRefNearAlias`（ALNA，nested Reference alias）**

Evidence：`_M_QuestGatherScript.psc` `Alias_Item`（在 QuestGiver hold 裡找 LVLI spawn point）；`_M_QuestKillScript.psc` `Alias_target`（在 `Dungeon` 裡找 boss）；`_M_QuestRetrieveScript.psc` `Alias_chest`（在 `Dungeon` 裡找容器）；`_M_QuestTrackThiefScript.psc` `Alias_InnMarker1`（在 `Inn1` 裡找 marker）。現有 `findMatching:closest/any` 用 `MatchingRefInLoadedArea`，只在整個 loaded area 搜索，不限地點範圍。FindMatchingRefNearAlias（CK 術語）/ Mutagen `QuestAlias.FindMatchingRefNearAlias` (ALNA) 完全不同。

**缺口 C：`UpdateCurrentInstanceGlobal` fragment codegen**

Evidence：`_M_QuestGatherScript.psc` Fragment_5（StartUpStage）第 39–41 行：
```
ItemTotal.SetValue(Utility.RandomInt(ItemTotalMin.GetValue() as int, ItemTotalMax.GetValue() as int))
UpdateCurrentInstanceGlobal(ItemTotal)
```
`_M_AliasPlayerGather.psc` OnItemAdded 第 18/30 行亦有。此 call 讓同一模板 quest 多次跑時各自擁有獨立的 ItemCount/ItemTotal 數值，objective text `<Global=_M_GlobalCountWhiterunOreVeryHigh>/<Global=_M_GlobalTotalWhiterunOreVeryHigh>` 才能正確顯示。ModForge `Generator.Build.QuestStages.cs` 和 `Generator.SceneFragments.cs` 無此生成路徑。

### 標記：可生成 / 需新支援 / 純參考

**需新支援（高優先，Missives 強烈強化）**
- **QuestAlias LocationAlias fill + nested ReferenceAlias fill**：缺口 A、B，radiant 任何隨機地點選擇的核心。
- **`UpdateCurrentInstanceGlobal` fragment snippet**：缺口 C，gather/計量型 radiant quest 的計數顯示必要。
- **GlobalVariable 量產 + script property 連線**：249 個 GlobalShort 當計數器/獎勵額；ModForge 需要能批次建 global 並接到 quest fragment script property 與 objective token。

**可生成 / 接近可生成（用 ModForge 既有能力）**
- **Quest stage + fragment 腳本骨架**：stage flags（StartUp/Complete/Fail）、objective display/complete、`AddItem` 發獎——這層膠水 Papyrus 跟既有 dispatcher/fragment 模式同類，可生成。參見 memory `sm-quest-journal-progression`。
- **Objective display text token**：`<Alias=...>/<Global=...>/<Global.Day=...>/<Alias.BaseName=...>` 直接寫進 `ObjectiveSpec.text` 即 pass-through。
- **Book / Message / LeveledItem / Container / Activator** record：都是 ModForge 已能造的基本型。
- **Tiered FormList 池**：`BuildFormLists()` + `WireFormLists()` 已支援。

**純參考（架構啟發，不必照抄）**
- **「不用 SM、用 Activator 觸發 + 預生 quest 池」是一條替代路線**：Missives 證明 **radiant 不一定要 SM**——若目標地點固定（板子在城裡）、靠玩家走近觸發，純 Activator+FormList+`Quest.Start()` 更簡單。ModForge 兩條路都該支援，視內容型態選。
- **笛卡兒積生成法**（hold × job × tier 把 265 顆模板鋪開）對 JSON-spec 生成器是天作之合：**spec 寫一個 job-family 模板 + 一張 hold/tier 矩陣，生成器展開成 N 顆 quest + 對應 FormList**，正是 ModForge「JSON → 大量 record」想要的形態。
- **Courier 時限失敗模式**（`RegisterForUpdateGameTime` alias + `DeliveryDate` global + `GameDaysPassed` 比對）：可作為時限任務的 Papyrus 腳本模板參考。
- **BlockActivation(true/false) 鎖容器防並發**：刷 quest 時暫鎖 board 的模式，對任何「觸發器刷新」設計都適用。
- **`<Global.Day=...>/<Global.MonthWord=...>` objective token**：CK 內建的 GlobalVariable → 日期格式化，純寫法，不需引擎特別支援。

### 跟既有 memory / 筆記的連結
- `story-manager-kill-recipe`、`sm-quest-journal-progression`：Missives 是「**不走 SM**」的對照組；但 quest stage/objective 推進的 journal 規則共用。
- `programmatic-navmesh`：Missives 的目標投放靠引擎在既有 vanilla 地點的 alias fill，**不自造 navmesh**（跟 extended-encounters 的 `MoveTo`+NavmeshTester 不同）——若 ModForge 生成的 radiant 任務目標落在 vanilla 地點，navmesh 不是問題；落在自製 worldspace 才需 programmatic navmesh。
- 共通缺口彙整（跨本輪 survey）：**FLST 建立（已支援）、LVLN alias fill（partial）、alias fill 系統（三條新缺口）** ——Missives 把「quest alias fill 系統（尤其 LocationAlias + nested ReferenceAlias）」確立為 radiant 生成的第一順位前置工程。
- roadmap：新缺口已登錄 [mod-survey-gaps.md](../../../workflows/roadmap/mod-survey-gaps.md) #7–9。
