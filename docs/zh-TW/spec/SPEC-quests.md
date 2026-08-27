# ModForge spec — 任務階段、Story Manager 與腳本

← [index](SPEC-index.md) · 對話／閒聊／場景與 CTDA 條件 → [SPEC-dialogue](SPEC-dialogue.md)

### 任務階段、日誌條目與目標串接
任務的 `stages[]` 是整數里程碑，任務可被**設定到**這些階段（10、20、30…）。每個階段
可選地寫入一條**日誌記錄條目**，並可攜帶一個任務狀態旗標。目標會隨階段被設定而顯示與
完成；`dialogue` 行被選取時可推進階段。

```jsonc
"quests": [{
  "editorId": "MF_ErrandQuest", "name": "A Forged Errand",
  "startGameEnabled": true, "priority": 60,
  "stages": [
    { "index": 10, "logEntry": "Joren asked me to retrieve his lost hammer." },
    { "index": 20, "logEntry": "I agreed to help. Time to search the riverbank.",
      "conditions": [ { "function": "GetStage", "comparison": "GreaterThanOrEqualTo",
                        "value": 10, "param": "MF_ErrandQuest" } ] },   // optional CTDA gate on the log entry
    { "index": 30, "logEntry": "I returned the hammer. Done.", "completeQuest": true }   // closes the quest
  ],
  "objectives": [
    { "index": 10, "text": "Agree to help Joren", "showStage": 10, "completeStage": 20 },
    { "index": 20, "text": "Find Joren's hammer",  "showStage": 20, "completeStage": 30 }
  ]
}]
```
- **`stages[]`** — `index`（唯一、**遞增**）、`logEntry`（日誌文字；省略則為無聲的
  里程碑）、`completeQuest` / `failQuest`（設定 QuestLogEntry 旗標，在達到此階段時關閉／失敗
  任務——最多一個）、`conditions`（日誌條目上的可選 CTDA 條件，以共用的
  **ConditionSpec** 建構：`function`（`GetStage`/`GetIsID`/… 名稱）、`comparison`
  （`==`/`>=`/… 或 `EqualTo`/`GreaterThanOrEqualTo`/…，預設 `>=`）、`value`、`param`（ref → 該
  函式的 form 參數，例如 `GetStage` 的任務））。
- **`stages[].startUpStage`** — 標記引擎在**任務啟動瞬間自動執行 `SetStage` 到的**階段
  （原版 QSDT「Start Up Stage」旗標）。這就是**由 Story Manager 觸發**的任務如何在
  **沒有外部 `SetStage`** 的情況下顯示其開場日誌條目／第一個目標——若沒有它，
  SM 啟動的任務會無聲地停留在階段 0。每個任務最多一個。**IN-GAME CONFIRMED 2026-06-05。**
- **`stages[].instanceGlobals[]`** — 在階段執行時，將 GLOB 綁定到**這個任務實例**
  （蒐集/計數**radiant** 任務）。階段片段會呼叫 `UpdateCurrentInstanceGlobal(<global>)`，
  使目標文字 `<Global=MF_ItemCount>/<Global=MF_ItemTotal>` 顯示**每個實例各自**的
  計數（一個模板、多份副本各帶不同計數——Missives 的手法）。每一項為：`{ "global":
  "<GLOB editorId>", "randomMin": N, "randomMax": M }`（種子 `SetValue(Utility.RandomInt(N,M))`），或
  `{ "global": "…", "value": V }`（種子 `SetValue(V)`），或 `{ "global": "…" }`（僅綁定）。在
  `globals[]` 中宣告該 GLOB；ModForge 會產出 `<quest>_Stages.psc` 供 `package` 編譯。把它放在
  `startUpStage` 上以在啟動時擲定目標值。示範 `examples/gather_quest_spec.json`。（撿取腳本由你提供。）
- **`objectives[].showStage` / `.completeStage`** — 將目標連結到階段：在 `showStage` 時
  `SetObjectiveDisplayed`，在 `completeStage` 時 `SetObjectiveCompleted`。`-1`（預設值）
  代表「不與階段連結」。
- **`objectives[].targets[]`** — 目標的羅盤／地圖**標記**（QSTA）。每個 target 為
  `{ "alias": "<aliasName>", "compassIgnoresLocks": false, "conditions": [...] }`。標記箭頭
  跟隨該**別名在執行階段所填入的內容**：以一名 actor 填入別名以標記**人物**，或以一個
  位置/ref（一扇門、一個 `kind:"xmarker"` 錨點，或一筆 `mapMarkers[]` 條目）來標記
  **地點**。多個 target = 多個標記（原版「殺死 X/Y/Z 任一」）。`alias` 必須
  指向**同一任務**上的別名。`compassIgnoresLocks` 讓羅盤標記穿透上鎖的門顯示；
  `conditions` 是每個 target 各自的 CTDA（標記僅在它們通過時顯示）。目標必須被
  **顯示**（透過 `showStage` 或腳本）其標記才會出現。要在沒有 NPC 的固定地點標記，
  放置一個 `kind:"xmarker"` 錨點並以 `forced:<editorId>` 別名綁定它。見
  `examples/quest-markers.json`。
- **`dialogue[].setStage`** — 選取該主題會將宿主任務推進到此階段。要從**非對話**動作
  （例如啟動一個執行階段生成的 ref）推進階段，附加一個**別名腳本**
  （`alias[].script`），其 `OnActivate` 呼叫 `GetOwningQuest().SetStage(N)`——可重用的
  `examples/MFSE_AdvanceStage.psc` 正是做這件事。端到端的日誌推進示範（start-up
  階段在 SM 啟動時顯示目標 → 別名 `OnActivate` 完成它並關閉任務）：
  `examples/story-manager-queststage.json`。

**哪些是純記錄、哪些需要 Papyrus：** 階段、日誌條目、`completeQuest`/`failQuest`
旗標與日誌條目條件都是**純記錄資料**——它們可乾淨地 build、`dump`/`questdiag`，
引擎也直接讀取。但在設定階段時*顯示*一個目標，以及從對話行*推進*一個
階段，需要 **Papyrus 片段**。`package` 指令端到端地處理這件事（**不需要 CK，
IN-GAME CONFIRMED It.36 2026-06-02**）：

1. 產生 `Scripts/Source/<quest>_Stages.psc`——每個階段一個 `Fragment_Stage_XXXX_Item00000()`
   函式以顯示/完成目標（CK 標準命名；引擎在 `SetStage()` 觸發時呼叫它）。
2. 產生 `Scripts/Source/TIF_<dialogue>.psc`——`extends TopicInfo Hidden`，帶一個明確的
   綁定到任務 FormKey 的 `Quest Property OwningQuest Auto`；`Fragment_0` 呼叫
   `OwningQuest.SetStage(N)`。使用 `OnBegin`（在玩家選取該行時觸發）。
   **不要使用 `GetOwningQuest()`——對 StartGameEnabled 任務而言，在遊戲載入時它回傳 None。**
3. 以 Linux 原生的 `papyrus-compiler` 將兩個 `.psc` 編譯成 `.pex`（退回到 Wine/CK）。
4. 將 VMAD 附加到 QUST（需要 `QuestScriptFragment.Unknown2=1`——啟用旗標；0
   即使 `SetStage()` 觸發也會略過片段），以及附加到 INFO（`DialogResponsesAdapter`、`OnBegin`）。
5. 在每一個 `setStage` 對話行上自動加入一個 `GetStage(quest) < setStage` 條件，使 NPC
   在玩家已選取後不會重複它。

以 `questdiag <plugin> <0xFORMID>` 檢視任何任務。對話仍只在遊戲
**LOAD** 時註冊（見上面的陷阱）。完整範例：`examples/quest_stages_spec.json`。

**其他可產生的結果動作**（同一個 TIF 片段可組合多個——不需要每個 mod 寫腳本）：

- **`hello: true`** — 將該行作為 NPC 自動說出的**問候語**發出（`Misc`/`Hello`），而非玩家
  選單選項。與 `identity`/`primaryIdentity`/`conditions` 組合以依狀態做不同問候；
  引擎播放優先度最高且符合的 Hello，否則播放 NPC 的純 `greeting`。（狀態變化的
  問候語放在一個 Hello 主題中作為多筆有序的 INFO——同一個說話者+任務的 `hello:true` 行
  會自動合併；具體條件化的行在前，純後備的在後。）`prompt` 被忽略。
- **`setPrimaryIdentity: "<id>"|"auto"`** — 覆寫玩家的主要身分（見 [SPEC-identities](SPEC-identities.md)）。
- **`openBarter: true`** — 與說話的商人 NPC 開啟交易選單（`Actor.ShowBarterMenu()`）。
- **`rewardItem`（一個 ref）+ `rewardCount`** — 給予玩家該物品/金幣（`Game.GetPlayer().AddItem`）。
- **`evaluateSpeakerPackages: true`** — 立即重新評估說話者的 AI 套件，讓由這一行的
  `setStage` 新啟用的套件（例如一個以 `GetStage==N` 把關的 Follow PACK）立即生效。

**護送/跟隨任務模式**（純記錄 + 上述動作）：一個帶階段 10/20 + 一個
目標的任務；一個 Follow PACK（`template` `0x019B2C`，target = 玩家）帶 `conditions:
[{ function: "GetStage", value: 10, param: "<quest>" }]`；NPC 攜帶 `[followPkg, standSandbox]`；
一個由 `identity` 把關的「我來護送你」行（`setStage: 10`、`evaluateSpeakerPackages: true`）與一個
「我們到了」行（`conditions: GetStage==10`、`setStage: 20`、`rewardItem`）。見
`examples/identity-paladin.json`（由 Adventurer 把關的 Wary Traveler 護送）。

<a id="persist--syncperks--jcontainers-jformdb-per-form-state-idea-20-skill-tree-phase-0"></a>

#### `persist` / `syncPerks`——JContainers JFormDB 每個 Form 的狀態（Idea #20 技能樹，Phase 0）

一個 `persist` 區塊會把**巢狀的每個 Form 狀態**寫入 [JContainers](https://www.nexusmods.com/skyrimspecialedition/mods/16495)
`JFormDB` 儲存，而一個 `syncPerks` 區塊則依該儲存狀態套用 perks——這是世界內技能樹的持久化層
（一名 NPC 從已存的技能等級「成長」出 perks，沒有 Campfire UI）。兩者都可掛在**兩種宿主**上：

- 一個**對話行**（`dialogue[].persist` / `.syncPerks`）——在該行被**選取**時，於該行的 TIF 結果片段中執行；
- 一個**任務階段**（`quest.stages[].persist` / `.syncPerks`）——在任務**達到**該階段時，於階段片段中執行
  （在一個里程碑上存入狀態，而非一次對話選擇）。

在兩者中，寫入都排在 perk 同步之前，使同步能看見剛剛存入的內容。

**由 Story Manager 驅動的觸發**（一個簡單的遊戲內掛點）：把一個階段 `persist`/`syncPerks` 放在一個
同時也帶 `storyEvent`（見 [Story Manager quests](#story-manager-quests--event-driven-start)）的任務上。一個
SM 啟動的任務永遠不會執行它的 startUpStage 片段（in-game 2026-06-19），所以產生器改把該
階段的 persist 路由到該任務的 `OnStory<Event>` handler 中——它會在**每一次** SM 投遞時執行，然後
`Stop()` 以重新待命。

> ⚠️ **不要用 `event: "CastMagic"` 做「施法練功」觸發。** 引擎被動的 Cast Magic SM 事件**不會**對玩家普通施法
> 觸發（in-game 2026-06-20：`OnStoryCastMagic` handler 從不執行；`package`/`build` 現在會在你接上它時警告）。
> 改用**有腳本的魔法效果**：一顆自施的自訂法術，其魔法效果掛 `MFSE_SpellTrigger`（`OnEffectStart → MFStoryEventDispatch.Fire(keyword, caster)`）
> → 一個 `ScriptEvent` 任務，其 `OnStoryScript` handler 存入狀態。這是遊戲內已確認的路徑（2026-06-20：persist +
> perk 同步 + 好感度 gate 全部有效，每次重施等級會累加）。完整範例：**`examples/skill_cast_spec.json`**（對自己施放
> 「Endurance Drill」訓練 Endurance → rank 2 取得 Adaptation perk）。`examples/npc_skill_persist_spec.json` 僅
> 保留作 persist/`syncPerks` **結構** + `OnStory` 路由的參考——它的 CastMagic 觸發不會 fire。

- **`persist`** — `{ storage, key?, set: [...] }`。`storage` 是 JFormDB 的 storageName（命名空間
  桶；成為路徑的第一個元件）。`key` 是狀態所掛附的 Form——見下面的 **Key**。
  每一個 `set` 項為 `{ path, <value>, delta? }`：
  - `path`——storage 之下的子路徑，例如 `".Endurance.nodes.Adaptation"`（發出的路徑為
    `".<storage><path>"`）。
  - 恰好一個值：`int` / `float` / `str`（→ `solveIntSetter`/`solveFltSetter`/`solveStrSetter`）
    或 `form`（一個 ref → `solveFormSetter`，綁定為一個 VMAD 屬性）。
  - `delta: true`（僅 int/float）——對目前已存的值做**累加**（讀-加-寫）而非
    取代，用於像累積 XP/比率這類計數器。
- **`syncPerks`** — `{ storage, key?, nodes: [{ path, perk, minRank? }] }`。對每個 node，讀取
  已存的 rank（`solveInt`），當 `rank >= minRank`（預設 1）時對 key actor 執行 **AddPerk**，否則
  **RemovePerk**。冪等——每次執行都安全。

**Key**——三種形式：
- `"speaker"`——對話 NPC（`akSpeakerRef`）。**僅限對話行**（任務階段沒有 speaker；
  validation 會拒絕階段上的 `"speaker"`）。這是預設值，所以階段 `persist` 必須設定 `key`。
- `"player"`——`Game.GetPlayer()`。
- **任何其他值**——一個任意 ref（spec 內的 editorId 或 `<master>:0xFORMID`），綁定為一個 Form
  屬性並用作 JFormDB 的 key（例如把所有狀態都掛在某個特定 NPC base form / 一塊代表某 NPC
  的石頭上）。對 `syncPerks` 而言，該 ref 在執行階段應為一個 actor 參考——`AddPerk` 受
  `If (key as Actor)` 保護，所以非 actor 的 key 只會無動作。

**好感度閘**（`persist` 與／或 `syncPerks` 上的可選 `gate`）——`{ global, atLeast?, atMost? }`。
一個關係/聲望計數器（Sofia F6 藍圖）：只有當綁定的 `GlobalVariable` 滿足閾值時，該區塊的寫入/同步
才會執行，於是成長可以以好感度把關，無須手寫 Papyrus。`atLeast` → `value >= n`；`atMost` → `value <= n`；
兩者並存 → 一個區間（`atLeast <= value <= atMost`）；皆無 → 以 GLOB 非零作為閘（一個布林旗標）。
`global` 必須解析到 spec 中的一個 GLOB。在別處（禮物、任務階段、一個對話 `setGlobal`）提升該計數器；
閘只負責讀取它。

屬性名稱會依階段加上命名空間（`S0010_PF_0`、`S0010_SyncPerk_0`、`S0010_PGate`…），這樣同一任務腳本中
數個階段永遠不會衝突；對話 TIF 則使用裸名稱（`PF_0`、`SyncPerk_0`、`PKey`、`SKey`、`PGate`、`SGate`）。

**生命週期**：只會產生 root-DB 路徑 API（`JFormDB.solveXxxSetter`/`solveInt`）。JContainers 擁有那些
root 並隨存檔一起持久化它們，所以**沒有** `JValue.object()`/`retain()`/`release()` 控制代碼需要平衡
——retain/release 的陷阱因設計而被規避（解決了設計未知數 U5）。

**執行階段/build 需求**：遊戲內必須安裝 JContainers SE；編譯產生的 `TIF_*.psc` /
`<quest>_Stages.psc` 需要 JContainers 自身的 `.psc` 在 Papyrus header 路徑上（`MODFORGE_PAPYRUS_BASE`）——
這是一個主力機步驟（見 WAIT_USER）。完整範例：`examples/npc_skill_persist_spec.json`（一名訓練師 NPC）。

<a id="storagewrites--papyrusutil-storageutil-per-form-kv-j-group"></a>

#### `storageWrites`——PapyrusUtil StorageUtil 每個 Form 的 KV（J 組）

`persist` 的輕量替代，用於**扁平、隨存檔自動管理的純量狀態**：`storageWrites` 把
[PapyrusUtil](https://www.nexusmods.com/skyrimspecialedition/mods/58705) 的
`StorageUtil.Set/Adjust{Int,Float,String}Value` 呼叫產生進相同的兩個 host——一個**對話行**
（`dialogue[].storageWrites`，被選取時在 TIF 片段中執行）與一個**任務階段**
（`quest.stages[].storageWrites`，達到階段時執行，對 SM 驅動的任務會路由到 `OnStory<Event>`
handler，與 `persist` 完全相同）。`persist`（JContainers JFormDB）是為巢狀路徑與 Form-as-key
而生，而 StorageUtil 是「簡單 + 自動管理」那半：隨從記憶、互動冷卻、每個 NPC 的旗標。值由存檔
管理，沒有要 retain/release 的東西。

每個條目是 `{ key, target?, <value>, delta?, fromJson? }`：
- `key`——StorageUtil 的字串鍵（例如 `"mymod_lastGreet"`）。
- `target`——值所掛的 Form：
  - `"speaker"`（對話 NPC，`akSpeakerRef`——**僅對話行**；預設）、
  - `"player"`、
  - `"none"`/`"global"`（不綁任何 Form 的行程全域 KV），**或**
  - **任何其他 token＝任意 ref**——一個 placed-ref editorId 或 `Master:0xFORMID`——讓值掛在**那個特定的
    actor/物件**上（per-NPC／per-container 記憶）。該 ref 會像 `persist` 的 key 一樣綁成片段 VMAD 裡的
    `Form` 屬性，且必須能解析。
  - 任務階段沒有說話者 → 用 `"player"`、`"none"` 或一個 ref（驗證會在階段上拒絕 `"speaker"`/預設）。
- 恰好一個值：`int` / `float`（→ `Set{Int,Float}Value`）或 `str`（→ `SetStringValue`）。
- `delta: true`（僅 int/float）——`Adjust{Int,Float}Value`，計數器用的原子 read-add-write。
- `fromJson: { file, key }`（選用）——**在 runtime 從外部 [PapyrusUtil JsonUtil] 檔讀取值**，而不用字面值。
  寫入的值變成 `JsonUtil.GetPath{Int,Float,String}Value("<file>", ".<key>", <int/float/str 字面值>)`，其中字面值
  作為 **missing default**（JSON 鍵不存在時回傳）。`file` 相對於 `data/skse/plugins/StorageUtilData/`
  （`"../"` 可往上跳）；`key` 是 JsonUtil **path**——裸 top-level key（`difficulty`）會自動補前導 `.`，或自己給
  巢狀點分路徑（`.tuning.spawnCount`、`.list[0]`）。這是「玩家可改／工具寫入的設定檔 → runtime 狀態」的橋接
  （例如從隨包 JSON 讀一個難度旋鈕進玩家 KV，供 mod 其餘部分讀）。
  > **為何用 Path API 而非 `GetIntValue`：** 純 `JsonUtil.GetIntValue(file, key)` 只讀 JsonUtil **自己**用
  > `SetIntValue` 寫的扁平命名空間，對**手寫的外部設定檔是空的**——會靜默回 default。`GetPath…Value` 系列才
  > 能巡覽任意外部 JSON。（2026-06-22 實機確認：`GetIntValue`→fallback、`GetPathIntValue`→真值。）

`speaker`/`player`/`none` 是純 Papyrus 表達式（不綁屬性）；**arbitrary-ref target** 每條綁一個 `Form` 屬性。
編譯產生的片段需要 PapyrusUtil 的 `.psc` 在 header 路徑上（主力機步驟；遊戲內須安裝 PapyrusUtil）——任何條目用
`fromJson` 時還需 **`JsonUtil.psc`**。

<a id="story-manager-quests--event-driven-start"></a>

### Story Manager 任務——事件驅動的啟動

任務可以**由 Story Manager（SM）自動啟動**以回應一個
遊戲內事件，而非在遊戲載入時或透過 `SetObjectiveDisplayed` 啟動。在任務中加入一個
`storyEvent` 區塊，build 就會自動串接好一切（SMBN→SMQN
在正確的原版事件根之下，`StartGameEnabled` 被清除）。

**遊戲內確認（2026-06-04）** 涵蓋全部五種變體模式（victim、killer、forced、
condition、ESL）。

```jsonc
// minimal — triggers on any actor kill, Victim alias = the killed actor
{
  "editorId": "MFSM_Avenge", "name": "Avenge the Fallen",
  "stages": [ { "index": 10 } ],
  "storyEvent": { "event": "KillActor" },
  "aliases": [ { "name": "Victim", "fill": "fromEvent:victim" } ]
}
```

#### `storyEvent` 欄位

| Field | Type | Notes |
|---|---|---|
| `event` | string | 事件名稱——見下表。**必填。** |
| `keyword` | string | 此 spec 中某 keyword 的 `editorId`。**僅 `ScriptEvent` 必填。** 多個任務可共用同一個 keyword（同一個過濾分支）。 |
| `conditions` | ConditionSpec[] | SM 分支上的額外 CTDA 條件（形狀與 `dialogue[].conditions` 相同）。把關 SM 是否嘗試啟動此任務。 |
| `locationFilter` | string[] | **位置感知的遭遇糖衣語法（#5）。** LocType keyword refs（`LocTypeBanditCamp`、`LocTypeDungeon`…）。build 會為每個 keyword 附加一個 `GetKeywordDataForCurrentLocation` 事件條件，彼此以 **OR** 串接——於是只有當玩家的新位置具有列出的任一 LocType 時，任務才會觸發。最適合搭配 `event: "ChangeLocation"`。純 CTDA（可離線驗證）。 |
| `cooldownHours` | float | **防洗版冷卻（#6，EE_WITimeout 模式）。** 兩次觸發之間的最小遊戲內小時數。build 會建立一個 `<quest>_LastFired` float GLOB + 附加可重用的 `MFEncounterCooldown` 任務腳本（`OnInit` 若任務在時間窗內重複觸發則 `Stop()` 它）。0 = 無。⚠ 需要執行階段檢查（預先編好的 `.pex` 在主力機編譯；見 `WAIT_USER.md`）。Hold 偵測 = 一個 `findMatchingLocation` 位置別名 + 一個 `LocAliasHasKeyword` 條件（新增的函式還有：`GetKeywordDataForCurrentLocation`、`LocationHasKeyword`）。 |

#### 支援的事件

| `event` | 由何觸發 | 可供 `fill` 的 slot |
|---|---|---|
| `KillActor` | 任何 actor 被殺 | `victim`、`killer`、`location` |
| `ChangeLocation` | actor 進入新位置 | `oldLocation`、`newLocation` |
| `CastMagic` | 施放法術 — ⚠️ **不會對玩家普通施法觸發**（in-game 2026-06-20；build 會警告）。施法觸發改用 `ScriptEvent` 經 `MFSE_SpellTrigger`（見 `examples/skill_cast_spec.json`）。 | `caster`、`target`、`location` |
| `AddItem` | 物品加入物品欄 | `owner`、`location` |
| `Assault` | actor 被攻擊 | `victim`、`attacker`、`location` |
| `CraftItem` | 玩家在工作台製作物品 | `workbench` |
| `PlayerRemoveItem` | 物品離開玩家物品欄（賣出/丟棄/給予） | `owner`、`item` |
| `Arrest` | 守衛逮捕一名 actor | `guard`、`criminal` |
| `IncreaseLevel` | 玩家升級 | *（無——透過 `storyEvent.conditions` 把關，例如 `GetLevel`）* |
| `ScriptEvent` | 透過 dispatcher 的 Papyrus `SendStoryEvent` | `ref1`、`ref2`、`location` |

#### `aliases`——動態別名填入

`aliases` 中的每一項在任務啟動時填入一個任務別名。若任何**必填**別名
無法被填入，任務會無聲地不啟動。

```jsonc
"aliases": [
  { "name": "Victim",    "fill": "fromEvent:victim" },        // slot from the event payload
  { "name": "Killer",    "fill": "fromEvent:killer" },
  { "name": "NewLoc",    "fill": "fromEvent:newLocation" },   // Location slot → alias Type=Location auto-set
  { "name": "TheBoss",   "fill": "uniqueActor:Skyrim.esm:0x01414D" },  // specific NPC (Ulfric)
  { "name": "TriggerRef","fill": "forced:Skyrim.esm:0x000014" },        // forced ref (player)
  { "name": "Spawned",   "fill": "createObject:Skyrim.esm:0x0010FE05@Caster" },  // spawn a wolf AT the Caster alias
  { "name": "Nearby",    "fill": "findMatching:closest",               // nearest ref in the loaded area…
    "conditions": [ { "function": "HasKeyword", "comparison": "==", "value": 1, "param": "Skyrim.esm:0x013794" } ] },  // …matching these gates (nearest NPC)
  { "name": "Hatch",     "fill": "createObject:Skyrim.esm:0x0BCD2D@Caster",     // spawn a chest, then…
    "script": "MFSE_AliasActivate", "scriptSource": "MFSE_AliasActivate.psc",   // …OnActivate on the spawned ref
    "scriptProperties": [ { "name": "TheKW", "type": "object", "objectEditorId": "MFSE_AliasKW" } ] }
]
```

| `fill` prefix | 別名種類 | Notes |
|---|---|---|
| `fromEvent:<slot>` | `FindMatchingRefFromEvent` | slot 名稱來自上面的事件表。Location slot（`newLocation`、`oldLocation`、`location`）會自動設定 `QuestAlias.Type = Location`。 |
| `uniqueActor:<ref>` | `UniqueActor` | 以 ref 釘定到一個特定 NPC；`AllowReserved` 強制開啟。 |
| `forced:<ref>` | `ForcedReference` | 靜態 ref（例如玩家 `Skyrim.esm:0x000014`）。 |
| `createObject:<ref>@<targetAlias>` | `CreateReferenceToObject` | 在任務啟動時，於 `<targetAlias>` 所持有的 ref 處**生成一個指向 `<ref>` 的新參考**（任何可放置的 base——NPC/container/static/物品）（`Create=At`、`Level=Easy`）。`<targetAlias>` 必須是同一任務中另一個 **ref 型**別名（不是 Location），且不能是它自己。例如施放法術 → 在施法者處生成一名守護者。遊戲內確認（2026-06-05）。 |
| `findMatching:closest` / `findMatching:any` | `QuestAlias.Flag.MatchingRefInLoadedArea`（`closest` 時加上 `MatchingRefClosest`） | 以**載入區域中一個已存在、符合此別名 `conditions` 的參考**填入——`closest` 取最近的符合者，`any` 取第一個。比對過濾器是一個 CTDA 清單（同樣的 `ConditionSpec` 形狀）串接到 `QuestAlias.Conditions`（例如 `HasKeyword ActorTypeNPC` = 最近的 NPC；`GetIsID <base>` = 某 base 的最近者）。**至少需要一個 condition。** 這是從原版 `MQGreybeardCall` Bystander 別名解碼出來的載入區域「Find Matching Reference」機制——**不是** `FindMatchingRefNearAlias`（那只找編輯器連結 ref 的子項）。別名是否填入取決於執行階段是否真的有符合的 ref 在載入區域內。 |
| `findMatchingLocation:<locTypeKeyword>[@<parentLocationAlias>]` | `QuestAlias.Type = Location` + 匹配 CTDA | **Radiant LocationAlias（#7）。** 以「Find Matching Location」填入一個 **Location** 型別名——挑選一個其 **LocType keyword** 相符（`<locTypeKeyword>` = spec 內的 KYWD editorId 或 `Plugin.esm:0xID`）的位置，可選地縮小到 `@<parentLocationAlias>`（本任務中另一個 Location 別名）**之內**的子位置。發出 `LocationHasKeyword == 1` 條件（LocType）+ 縮小時加 `GetInCurrentLocAlias == 1` 條件（`LocationAliasIndex` = 父）+ 設 `StoresText`（讓 `<Alias=Name>` token 顯示挑中位置名）。**已對 shipping Missives `_M_QuestWhiterunKillBandit` byte 驗證（2026-06-21）**——引擎在 Location 型別名上忽略 `LocationAliasReference.Keyword`，故是 conditions-based、**非** `LocationAliasReference`。Missives radiant 多樣性的核心：先一個 Hold 位置，再在其中一個 Dungeon/Inn 位置。 |
| `findInLocationAlias:<locationAlias>[#<refTypeLCRT>]` | `QuestAlias.Type = Reference` + `LocationAliasReference` | **Radiant 在位置中找 ref（#8）。** 以「Find Matching Reference」填入一個 **Reference** 型別名，範圍限定在 `<locationAlias>`（本任務中一個 Location 別名）所持有的位置——以一個可選的 **RefType** LCRT（`#<refTypeLCRT>`，例如地城的 `BossContainer`）與／或此別名的 `conditions` 縮小範圍。發出 `Location = {AliasID=<location index>, RefType=<LCRT>}`。**需要一個 refType 與／或至少一個 condition。** Missives 的 Alias_target/Alias_chest（地城內的 boss/戰利品）。使用 `LocationAliasReference`（**不是** `FindMatchingRefNearAlias`，後者已驗證僅限連結 ref 的子項）。 |

**額外的別名選項：**

| Field | Default | Notes |
|---|---|---|
| `allowReserved` | `false` | 若目標 NPC 可能被另一個任務保留（`ReservesLocationOrReference`）則設為 `true`。沒有它，別名會填入失敗而任務不啟動。`uniqueActor` 會強制開啟它。 |
| `packages` | `[]` | 當此別名被填入時**覆寫該別名 actor 的 AI** 的 package（`ReferenceAlias` 的「Packages」分頁 / ALPS，寫進 `QuestAlias.PackageData`），優先序高的在前。每項 = spec 內的 PACK editorId 或 `Plugin.esm:0xID`。**這才是真正驅動 radiant escort/travel 演出的東西**——光有 package record（即使 `PackageTargetAlias`/`LocationFallback` 都對）也不會跑，除非在這裡把它列進該 actor 的別名。byte 形狀對齊 vanilla `MS13` Camilla 別名。示範 `examples/radiant_package_spec.json`（VIP 別名 → `MFTravelToSafehouse` → Thoring（Windpeak Inn 酒保）走向 runtime 挑中的 Safehouse 地點）。 |
| `script` / `scriptSource` / `scriptProperties` | — | 將一個 Papyrus **別名腳本**附加到此別名（一個 `ReferenceAlias` 衍生的腳本，存放在任務的 `QuestAdapter.Aliases` VMAD 上，綁定到別名 ID）。它隨**填入該別名的任何 ref** 一同移動——包括 `createObject` 生成或 `findMatching` 比對到、任何 base-object 腳本都搆不到的 ref。經典用法是 `Event OnActivate(ObjectReference akActionRef)`：啟動該別名化的 ref 會執行它（例如呼叫 `MFStoryEventDispatch.Fire(...)` 以串接一個 story event）。`script` = Scriptname、`scriptSource` = 供 `package` 編譯的 `.psc`、`scriptProperties` 綁定它的 Auto 屬性（形狀同對話的 `resultProperties`）。你需自行提供編譯好的 `.pex`。遊戲內確認（2026-06-05）；可重用的輔助工具 `examples/MFSE_AliasActivate.psc`。 |

**一般任務（無 `storyEvent`）上的別名：** 同一個 `aliases[]` 區塊在一般
**StartGameEnabled** 任務上也適用——`forced`/`uniqueActor`/`createObject`/`findMatching` 填入與一個別名
`script` 全都適用（只有 `fromEvent` 無效——沒有事件可從中拉取；validator 會標記它）。別名
在任務啟動時填入（= 遊戲載入）。遊戲內確認（2026-06-05）；示範
`examples/quest-alias-standalone.json`（forced 玩家 → `createObject` 箱子 → `OnActivate` 推進）。

**Radiant 鏈（`findMatchingLocation` + `findInLocationAlias`）：** 這些可組合成 Missives 的
多樣性模式——`Hold`（`findMatchingLocation:<holdLocType>`）→ `Dungeon`
（`findMatchingLocation:<dungeonLocType>@Hold`）→ `BossChest`
（`findInLocationAlias:Dungeon#<bossLCRT>`）。示範 `examples/radiant_alias_spec.json`。✅ **已對 shipping
Missives byte 驗證（2026-06-21，`questdiag`）：** #7 = conditions-based（`LocationHasKeyword` +
`GetInCurrentLocAlias`）逐欄位對上 Missives `Dungeon` 別名；#8 = `LocationAliasReference{AliasID, RefType}`
完全對上 Missives `Target` 別名。剩最後一關 = 實機 alias fill（啟動後 `sqv MFRadiantBounty`；
`ModForgeRadiantAlias.zip`）。見 `WAIT_USER.md`。

#### `spawn`——動態的玩家附近生成（F組 #3）
任務的 `spawn` 區塊：在任務啟動時，於玩家周圍一個隨機 `minDistance`..`maxDistance` 偏移處放置
`count` 份 `form`（ActorBase / LeveledNpc）的副本，然後（`snapToNavmesh`，預設 true）切換
`EnableAI` 讓每一份吸附到最近的 navmesh 點——一個合法可行走的生成，且**沒有預先放置的
標記**（EE NavmeshTester 的手法），透過可重用的 `MFDynamicSpawn` 任務腳本。與
`ChangeLocation` + `locationFilter` + `cooldownHours` 搭配以做出一個受速率限制的位置感知遭遇
（`examples/location_encounter_spec.json`）。⚠ 執行階段（`PlaceAtMe`+`EnableAI` 吸附）需要遊戲內檢查；`.pex` 在主力機編譯。見 `WAIT_USER.md`。

#### SM 鐵律（引擎行為，非 bug）

- **一個事件 → 啟動一個任務** — 引擎按順序嘗試任務節點，並啟動
  第一個條件通過的。同一事件上的第二個無條件任務在同一次事件觸發中永遠不會啟動。
  使用條件來區分。
- **`SimpleActor` 小動物不會觸發 `KillActor`** — 殺死雞、兔子等不會產生
  任何 SM 事件。請以正規 actor 為目標（盜賊、狼、NPC）。
- **任何填入失敗的必填別名 → 任務無聲地不啟動。** 只有在任務缺了別名仍能運作時，
  才把別名設為選用。
- **ESL plugin 與 SM 記錄完全相容。** 不需要僅為了 SM 內容就使用 ESP。

#### ScriptEvent——發送你自己的 story event

`ScriptEvent` 讓 Papyrus 程式碼得以觸發 SM 任務，而無須依賴原版事件。
build 會自動把共用的 dispatcher（`MFStoryEventDispatch.pex`）嵌入到打包後的 mod 中
——你不需要為每個任務編譯任何東西。

```jsonc
// 1. declare the keyword that identifies your event channel
"keywords": [ { "editorId": "MY_StoryKW" } ],

// 2. the quest that responds to it
"quests": [{
  "editorId": "MY_QuestOnFire",
  "storyEvent": { "event": "ScriptEvent", "keyword": "MY_StoryKW" },
  "aliases": [ { "name": "Target", "fill": "fromEvent:ref1" } ]
}]
```

從 Papyrus（任何腳本），觸發該事件：
```papyrus
; MFStoryEventDispatch is the embedded global script
MFStoryEventDispatch.Fire(MY_StoryKW, akRef1, akRef2, akLocation)
```

dispatcher 呼叫 `MY_StoryKW.SendStoryEvent(...)`，引擎將其路由到每一個
符合的 SM 任務節點。一個 dispatcher `.pex` 服務所有 mod——只要有任何 ScriptEvent 任務存在，
`package` 就會自動把它複製到 `Scripts/`。

**把 `Fire()` 接到真正的觸發來源。** 任何 Papyrus 環境都能呼叫 dispatcher。一個可重用的
模式是 magic-effect 腳本：把它附加到一個自訂 MGEF、設定一個 keyword 屬性，施放
法術即以施法者作為 `ref1` 觸發 story event：

```papyrus
Scriptname MFSE_SpellTrigger extends ActiveMagicEffect
Keyword Property TheKW Auto
Event OnEffectStart(Actor akTarget, Actor akCaster)
    MFStoryEventDispatch.Fire(TheKW, akCaster, akTarget)
EndEvent
```

`package` 會自動把這類腳本與嵌入的 dispatcher 源碼一起編譯，因此 `Fire()`
無須任何本機 Papyrus 設定即可解析。同樣的形狀也適用於對話片段、別名
腳本或 activator——任何你能執行一行 Papyrus 的地方。

同樣的一行 `Fire()` 呼叫從任何入口點都能運作——一個小型可重用的觸發程式庫：

| 入口點 | Script（`extends …`） | Event | Example |
|-------------|----------------------|-------|---------|
| Magic effect（法術） | `ActiveMagicEffect` | `OnEffectStart` | `story-manager-magictrigger.json`（in-game ✓） |
| Magic effect（藥水） | `ActiveMagicEffect` | `OnEffectStart` | `story-manager-potiontrigger.json`（同腳本，飲用以觸發；in-game ✓） |
| Activator | `ObjectReference` | `OnActivate` | `story-manager-activatortrigger.json`（拉一根拉桿；in-game ✓） |
| Dialogue line | `TopicInfo` | `Fragment_0` | `story-manager-dialoguetrigger.json`（NPC 給予一個任務；in-game ✓） |

四者皆於 2026-06-05 遊戲內驗證。activator 注意事項：`model` 必須是一個在載入順序中
確實存在的 NIF 路徑——錯誤的路徑會生成一個不可見的物件且沒有錯誤。

前三者把一個帶 `Keyword` 屬性的腳本附加到一個記錄（MGEF / ACTI），透過
spec 的 `scripts[]` 設定。dialogue 觸發則把腳本串接為某行的 `resultScript` +
`resultScriptSource` + 一個 `TheKW` `resultProperty`。`package` 會把它們全部與
嵌入的 dispatcher 源碼一起編譯。

亦見 `examples/story-manager-scriptevent.json` + `examples/MFSE_TestTrigger.psc`（OnInit 測試）。

### scripts——Papyrus 附加
```jsonc
{
  "targetEditorId": "MF_Q1",          // record to attach to (any editorId in the spec)
  "scriptName": "MFDemoQuestScript",  // must match the .pex/.psc Scriptname
  "source": "scripts/MFDemoQuestScript.psc",  // optional: .psc path (rel. to this spec);
                                              //  `package` compiles it via Wine
  "properties": [
    { "name": "GreetingCount", "type": "int",    "int": 3 },
    { "name": "PlayerRef",     "type": "object", "objectEditorId": "MF_Smith" }
  ]
}
```
- 屬性 `type` ∈ `int | float | bool | string | object`。設定對應的值
  欄位：`int` / `float` / `bool` / `str`，或 `objectEditorId`（對 `object` 而言，解析
  為一個 FormLink）。屬性會被標記為 *Edited* 使遊戲讀取它們。
- 附加可作用於任何支援腳本的記錄（Quest、Npc、Activator、
  MagicEffect、Weapon、Armor、MiscItem、Book、Ingestible…）。腳本的 `Name` 必須
  與編譯後的 `.pex` 相符。
