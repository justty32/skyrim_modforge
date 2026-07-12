# ModForge spec — 職業、對話、閒談與場景

← [index](SPEC-index.md) · 任務與 Story Manager → [SPEC-quests](SPEC-quests.md) · 身分系統 → [SPEC-identities](SPEC-identities.md)

### 職業 (CLAS)
NPC 的「職業」——把某個 npc 的 `class` ref 設成其中一個。它驅動該 actor 的屬性
分配與偏好技能（而對訓練師 NPC 來說，還決定它 `teaches` 什麼）。
```jsonc
{ "editorId": "MF_Battlemage", "name": "ModForge Battlemage",
  "teaches": "Destruction",        // a Skill the class can train (trainers); optional
  "maxTrainingLevel": 50,
  "healthWeight": 30, "magickaWeight": 50, "staminaWeight": 20,   // attribute split (~sum 100)
  "skillWeights": { "Destruction": 100, "Restoration": 75, "OneHanded": 50 } }  // Skill -> 0–255 favour
```
技能名稱：`OneHanded`、`TwoHanded`、`Archery`、`Block`、`Smithing`、`HeavyArmor`、`LightArmor`、
`Pickpocket`、`Lockpicking`、`Sneak`、`Alchemy`、`Speech`、`Alteration`、`Conjuration`、
`Destruction`、`Illusion`、`Restoration`、`Enchanting`。只有當某 npc 具備
**`level` > 0 且 `autoCalcStats: true`** 時，職業才會真正驅動該 NPC 實際的
屬性／技能數值——否則引擎使用平直預設值（一個裸 NPC 不論職業都讀作 50/50/50）。要看出差異：
生成一個重 magicka 與一個重 health 的 NPC（兩者同等級且皆 `autoCalcStats`）並比較
`getav magicka`／`getav health`。

### 對話
一個 `dialogue` 條目是顯示在某任務分支下的玩家話題，可選擇性地限制
為單一說話 NPC（一個 `GetIsID` 條件）。`questEditorId` 必須指名本 spec 中的某個任務；
`speakerNpcEditorId` 若設定，必須指名某個 npc。`prompt` 是玩家的台詞；
`responses` 是 NPC 所說的台詞。

從一個 `dialogue` 條目，build 會發出**整條原版鏈**，讓該話題
在遊戲內真正出現（已確認 It.23, SSE 1.6.1170）：
- **Topic**（`Custom`、`SNAM='CUST'`——null subtype 會在載入時當機）+ **Branch**
  （`TopLevel`, Player）+ 一個攜帶 responses 的 **INFO**。每個 INFO 都會取得 `ENAM`
  （flags）+ `CNAM`（favor level）——**沒有 `ENAM` 的 INFO 被視為無效，
  其 topic 會被靜默地從選單中移除**；
- 每個任務一個 **DialogView (DLVW)**，把它的 branch 綁到該任務（沒有它，
  該任務的玩家對話永遠不會被提供）；
- 每個說話 NPC 一個 **Hello** info（`Misc`/`Hello`/`SNAM='HELO'`），讓該 NPC
  *可被交談*——用 `npc.greeting` 設定其台詞。

**Result fragment（選到台詞時做某件事）。** 一個對話選項只能透過 Papyrus fragment
來*動作*（收取金幣、加入隨從系統、設定某階段）——JSON 只保存靜態資料，絕不放控制流程。設定 `resultScript`（fragment 的 Scriptname，必須
`Extends TopicInfo` 並定義 `Function Fragment_0(ObjectReference akSpeakerRef)`）、
`resultScriptSource`（`.psc`，由 `package` 編譯）以及 `resultProperties`（綁定它的
`Auto` properties——形狀與 `scripts[]` 條目的 properties 相同：`int`/`float`/`bool`/
`string`/`object`）。build 會掛上該 INFO 的 `OnBegin` fragment VMAD（在玩家
選到台詞時觸發；只有當效果必須跟在完整配音回應之後時才用 `OnEnd`）。設定 `goodbye: true`
讓台詞之後關閉選單（原版的招募／解雇台詞全都這樣做）。招募付費隨從的範例見
`examples/follower_paid_spec.json` + `MFHirePaidRecruit.psc`。

**INFO (ENAM) 行為旗標**（全部預設 false）：`sayOnce`（一輩子至多說一次——VIGILANT
最常用的 INFO 旗標，用於一次性劇情節拍）、`walkAway`（NPC 在台詞後走開）、`random`
（引擎在條件通過的同層 INFO 之間隨機挑選，用於台詞變化）、`invisibleContinue`
（不關閉選單，繼續鏈中的下一個 INFO）、`forceSubtitle`（即使字幕關閉也永遠顯示
字幕）。它們同時適用於玩家話題與 `hello` 問候語。

**對話樹（分支對話）。** 預設情況下每個 `dialogue` 條目都是一個**頂層**
玩家選項，在你跟 NPC 交談的當下全部顯示。要建一棵*樹*——選一個話題、NPC
回答、然後*新的*選項出現——使用：
- **`linkTo`**（ENAM，一個 list）：在這條台詞播完後，把這些對話話題作為下一批
  選項浮現。每個條目是另一個 `dialogue` 的 `editorId`（解析為其 TOPIC）或一個原版
  `<master>:0xFORMID` 話題。VIGILANT 的 #1 樹狀技巧。
- 在*目標*條目上設 **`topLevel: false`**：把它們標記為**子話題**，只有當某物
  `linkTo` 它們時才出現（否則它們也會顯示在初始選單）。預設 `true`。
- **`previousDialog`**（PNAM）：把這個 INFO 接在另一個之後（其值是一個 `dialogue` 的 `editorId`，
  解析為那個 INFO）——用於在一個流程內排序回應。

```jsonc
{ "editorId": "AskAboutCave", "questEditorId": "Q", "speakerNpcEditorId": "Hideko",
  "prompt": "What's in the cave?", "responses": ["Bandits. And worse."],
  "linkTo": ["AskHowMany", "AskReward"] },            // → two follow-up options appear
{ "editorId": "AskHowMany", "questEditorId": "Q", "speakerNpcEditorId": "Hideko",
  "prompt": "How many bandits?", "responses": ["A dozen, maybe."],
  "topLevel": false }                                  // a sub-topic — only via the linkTo above
```

> **三個執行期需求（不是記錄 bug）：** (1) 對話只在**遊戲 LOAD** 時註冊
> ——用真正的新遊戲測試，或在任務啟動後 `save`+`load`；
> 主選單的 `coc` 或對話進行中的 `startquest` 會讓 NPC 啞口無言，即使
> plugin 完美無瑕。(2) 把說話者放在真實的室內座標——一個無套件的 NPC 在 cell
> 原點 **(0,0,0)** 會落在 navmesh 外、無法被接近。(3) 無配音的台詞會一閃而過；
> 安裝 **Fuz Ro D-oh**（或附帶靜音 `.fuz`）並啟用字幕。見 `lifelike/gotchas.md`。

**INFO 陣列批次 (`variants`)。** 要在**一個 topic 下生成多條同層台詞**——對旅途／地點／時間／天氣
／玩家狀態反應的 ambient commentary——把它們全宣告在**一個** `dialogue` 條目的 `variants` 陣列裡，
不必為每條重複 topic、說話者與閘。每個 `variants[]` 條目成為自己的 INFO（帶 **`random`** 旗標，引擎
在當下條件通過的同層 INFO 之間隨機挑選），並**共用** parent 條目的說話者閘、`conditions`、
`useConditionTemplates` 與 `identity`——再加上它自己的額外 `conditions` 與 `responses`。這就是 FCO 式
265 條共用一組閘的 commentary 的生成器。一個 `variants[]` 條目是
`{ responses, conditions?, emotion?, emotionValue?, sayOnce? }`（`emotion`/`emotionValue` 未設時繼承
parent）。當 `variants` 已設且 parent `responses` 為**空**時，不會發出 parent INFO（該條目是純批次
header）；非空的 parent `responses` 會作為多一條同層台詞播放。Variants 僅供台詞變化——結果片段／
`setStage`／`linkTo` 留在 parent 條目上，且 `variants` 不支援於 `hello` 行。搭配 `conditionTemplates`
可跨**多個**批次共用同一組閘。

```jsonc
{ "editorId": "LydiaTravelBanter", "questEditorId": "Q", "speakerNpcEditorId": "Lydia",
  "prompt": "", "useConditionTemplates": ["Following"],     // 共用閘：僅在跟隨時
  "variants": [
    { "responses": ["Lovely day for it."], "conditions": [{ "function": "GetCurrentTime", "comparison": "<", "value": 18 }] },
    { "responses": ["Getting dark. We should make camp."], "conditions": [{ "function": "GetCurrentTime", "comparison": ">=", "value": 18 }] },
    { "responses": ["I used to dream of adventure. Be careful what you wish for."], "sayOnce": true }
  ] }
```

**對選取做出反應——`persist` / `syncPerks` / `storageWrites`。** 一個對話行被選取時可以記錄狀態：
`persist`/`syncPerks`（JContainers JFormDB 巢狀的每個 Form 狀態，Idea #20）與 `storageWrites`
（PapyrusUtil StorageUtil 扁平的每個 Form KV——隨從記憶、冷卻、旗標）。兩者都產生進該行的 TIF 結果片段。
形狀與在任務階段上相同——見 SPEC-quests 的
[persist/syncPerks](SPEC-quests.md#persist--syncperks--jcontainers-jformdb-per-form-state-idea-20-skill-tree-phase-0)
與 [storageWrites](SPEC-quests.md#storagewrites--papyrusutil-storageutil-per-form-kv-j-group)
（在對話行上 `target`/`key` 可為 `"speaker"`）。

### 閒談 (banter) — 主動（未經提示）的 NPC 台詞
一個 `banter` 條目是 NPC **自行**說出的台詞，沒有玩家選單——即原版
隨從評論模式（`HirelingIdles`）。形狀：`editorId`（選填）、`questEditorId`、
`speakerNpcEditorId`、`responses`（所說的台詞——一條評論）、`emotion`/`emotionValue`、
`conditions`（情境閘）。所有共用同一組（speaker, quest）的 banter 條目會塌縮成
**一個環境話題**——Category=Misc、SNAM=`IDLE`、無 branch——每個條目一個 **Random**
旗標的 INFO；引擎隨機挑一個其 `conditions` 當下通過的並播放它。**觸發
需求：** 說話者必須**啟用閒置閒聊**——一個帶有 `AllowIdleChatter` 中斷旗標的
AI 套件（一個 `Sandbox` 套件，或原版的跟隨套件）。用 `conditions`
讓它情境化（例如夜晚的 `GetCurrentTime`、`IsInInterior`、表示「我受傷了」的 `GetActorValuePercent`，
以及限隨從的 `GetInFaction CurrentFollowerFaction==1`）。這是玩家主動提問的 `dialogue`
台詞的*未經提示*對應物。注意：僅限環境／閒置——真正的
戰鬥咆哮使用不同的 subtype（Taunt/Attack），尚未支援。見 `examples/follower_vanilla_spec.json`。

### 場景 (scenes) — 兩個 NPC 互相交談 (SCEN)
一個 `scene` 是 NPC 之間（不含玩家）的腳本化對話——即原版 **Scene** 記錄。
一個場景由某任務**主持**，其參與者是該任務的**別名**（不是直接的 NPC ref），
它播放一串有序的**階段 (phases)**，每階段一條台詞。
```jsonc
{ "editorId": "MF_TavernArgument",
  "questEditorId": "MF_SceneQuest",     // a StartGameEnabled quest in this spec (the scene runs while it does)
  "beginOnQuestStart": true,            // play the moment the host quest starts (= on game load); default true
  "stopQuestOnEnd": false,              // stop the host quest when the scene finishes (vanilla one-shots set true)
  "actors": [                            // each actor = an alias INDEX + the NPC that fills it
    { "aliasId": 0, "npc": "MF_Borin", "name": "Borin" },
    { "aliasId": 1, "npc": "MF_Hilda", "name": "Hilda" } ],
  "phases": [                            // played in order; `speaker` is one of the actors' aliasId
    { "speaker": 0, "emotion": "Anger",   "lines": [ "You still owe me for the ale, Hilda." ] },
    { "speaker": 1, "emotion": "Disgust", "lines": [ "Owe you? That swill wasn't worth a clipped septim." ] },
    { "speaker": 0, "emotion": "Anger",   "lines": [ "Watch your tongue, or there'll be trouble." ] },
    { "speaker": 1, "emotion": "Happy",   "lines": [ "Ha! Buy me a drink and we're even." ] } ] }
```
從這一個條目，build 會發出**整條原版鏈**（鏡像 `dunIronbindBeemJaMourningScene` 上的
`scenediag`）：
- 主持任務上每個 actor 一個 **QuestAlias**，各以 `UniqueActor` 綁到指名的 NPC（使該
  別名以那個特定 actor 填充）；
- **Scene (SCEN)**：其 `SceneActors` 參照**別名索引**（不是 NPC FormKeys）；其
  `Phases` 是有序的節拍；每階段一個 **Dialog `SceneAction`** 把（說話別名、階段）
  綁到該台詞的 topic，並以*另一個* actor 作為 headtrack 目標，讓他們面對彼此；
  - **每階段視線覆寫**（選填）：一個階段可設 `headtrackActor`（要注視的 actor `aliasId`；
    `-1` = 不看任何人；預設 = 另一個 actor）、`headtrackPlayer: true`（轉向面對
    玩家——與非預設的 `headtrackActor` 互斥），以及 `faceTarget`（預設 true）。
    用於某個 NPC 轉身直接對你說話的節拍。三者全部省略 = 行為不變。
  - **條件**（選填的 CTDA 閘，共用 `ConditionSpec`，在 pass 2 接線）：場景層級的
    `conditions`（整個場景只在全部通過時才 STARTS）以及每階段的 `startConditions`（該階段
    只在全部通過時才播放）／`completionConditions`（該階段在全部通過時結束）。一個沒有
    條件的場景與先前位元組相同。見 `examples/scene-conditions.json`。
    - **重要——場景層級的 `conditions` 只閘住引擎啟動的場景**（`beginOnQuestStart`，或
      非強制的引擎啟動）。一個 **`autoStart`（在場閘控）的場景由
      控制器腳本（`Scene.Start()`）強制啟動，這會繞過場景啟動條件**——所以場景層級的
      `conditions` 對 autoStart 場景**毫無作用**。要閘控在場觸發的場景，請改用
      `autoStart.gateGlobal`（控制器在啟動前檢查的一個 GLOB）。每階段的
      `startConditions`／`completionConditions` 在播放期間兩種情況下仍會被評估。
    - **`completionConditions` 推進一個階段。** 標準的「在所說台詞結束後推進」
      閘是 **`IsSceneActionComplete`**——在場景條件上，`scene` 預設為所屬場景，
      所以你只需給 `sceneActionIndex`（該 action 在已建 SCEN 中的索引；用 `scenediag` 找出來）。
      你也可以以玩家位置作閘來掌控節拍——`GetDistance`（≤ N units）、`GetInCell`、
      `GetInCurrentLoc`、`GetInWorldspace`。（⚠️ 階段推進行為**已離線建出但尚未
      在遊戲內驗證**——待測的遊戲內測試追蹤於 repo 根目錄的 `WAIT_USER.md`。）
- 每階段一個 **Scene-subtype DialogTopic**（Category=Scene、SNAM=`SCEN`）+ **INFO**，攜帶
  所說的 `lines` + `emotion`。

> **執行期需求（不是記錄 bug）：** (1) 兩個 NPC 必須**彼此放在附近**——
> 為每個 NPC 加一個 `placements[]` 條目進**同一個 cell**（他們必須同處一地才能交談）。
> (2) 與所有任務對話一樣，場景只在**遊戲 LOAD** 時載入——測試新遊戲，或在主持任務啟動後
> `save`+`load`（build 會自動寫入 `.seq` 條目）。(3) 無配音的台詞會一閃而過；
> 安裝 **Fuz Ro D-oh** 並啟用字幕。**狀態：僅結構性**——`build`/`validate`/`dump`
> 已對照原版場景形狀驗證；**尚未在遊戲內確認。** 見 `examples/scene_spec.json`
> 與 `lifelike/cookbook-advanced.md`。

#### autoStart — 在場閘控的重複 Scene (隨從在場偵測 + 互動 Scene)
場景可以不在遊戲載入時播放一次（`beginOnQuestStart`），而是**每當
玩家與兩個 actor 同處一地**時自行播放，按冷卻時間重新觸發——即可用形式的「隨從
閒談」（隨從待在玩家附近，所以它會在旅途中觸發）。加一個 `autoStart` 區塊：
```jsonc
{ "editorId": "MF_TravelBanter", "questEditorId": "MF_BanterQuest",   // host quest MUST be StartGameEnabled
  "autoStart": {
    "triggerDistance": 1024.0,        // max distance (units) from the player to EACH actor; default 2048
    "requireLineOfSight": false,      // also require the player HasLOS both actors; default false
    "cooldownSeconds": 15.0,          // min REAL seconds between plays (timescale-independent); default 60
    "pollSeconds": 4.0,               // RegisterForSingleUpdate poll interval; default 5
    "brawlOnEnd": true },             // when the dialogue finishes, the two actors fight each other; default false
  "actors": [ /* ≥2, UniqueActor-bound as above */ ],
  "phases": [ /* … */ ] }
```
當 `autoStart` 存在時，build 會**清除**場景的 `beginOnQuestStart`，並把
可重用的 **`MFSceneBanterController`**（extends Quest）掛到主持任務上，將它接線到這個場景 +
前兩個 actor 的別名索引 + 調校參數。控制器輪詢（鏈式 `RegisterForSingleUpdate`），
並在兩個 actor 都已載入、在範圍內、未死亡／未在戰鬥（選擇性 LOS）、
且冷卻已過時呼叫 `Scene.Start()`。有 **`brawlOnEnd`** 時，它會偵測場景結束並使兩個 actor
開打（雙向 `StartCombat`）——他們在爭吵後動手；把 actor 標記為 **`essential`**
（NpcSpec flag）以進行非致命的扭打。`package` 會自動把 `MFSceneBanterController.pex` 出貨到 `Scripts/`。見 `examples/scene-presence-banter.json`。**範圍外（之後）：** 動態的「掃描
當前隊友」填充（這一切片使用指名的、`UniqueActor` 綁定的 actor）。

##### 重播策略 — 控制何時／多頻繁地重新觸發
預設情況下，在場閘每當玩家同處一地且 `cooldownSeconds` 已
過時就重新觸發（一個無盡迴圈）。把下列任一項加進 `autoStart` 來控制重播（全部與
冷卻 **AND** 在一起）：
```jsonc
"autoStart": {
  "triggerDistance": 1024.0, "cooldownSeconds": 15.0, "pollSeconds": 4.0,
  "playOnce": true,                 // play AT MOST ONCE ever; the controller stops polling afterwards
  "playHour": 12.0,                 // only fire within +/- playHourTolerance of this in-game hour (0..24,
  "playHourTolerance": 2.0,         //   circular); -1 (default) = any time. e.g. 12 +/- 2 = 10:00..14:00
  "gateGlobal": "MF_BanterDone"     // a ref → a GLOB used as a re-arm TOKEN (see globals)
}
```
- **`playOnce`**——最簡單的「不迴圈」：在單次播放後控制器取消註冊其輪詢
  （存檔膨脹衛生）。最適合一次性的遭遇。
- **`playHour` / `playHourTolerance`**——一個時段視窗（控制器讀取遊戲內小時）。
  與即時冷卻無關——用於「只在正午」、「只在夜晚」等。
- **`gateGlobal`**——通用機制：場景只在全域變數 `== 0` 時播放，且
  控制器在之後立刻 `SetValue(1)`。它接著保持關閉，直到某個**其他**生成的
  內容 `SetValue(0)` 它（一個對話 result script、一個任務階段 fragment、一個別名腳本、另一個
  事件）。這是「播放一次**直到某物重新武裝它**」。在
  [`globals`](SPEC-items.md#globals-glob--shared-flags--counters--constants) 中建出該 GLOB；把它重設為 0 是
  另外撰寫的（Papyrus）。見 `examples/scene-replay-policy.json`。

> 改動 `MFSceneBanterController.psc` 需要重新編譯它的 `.pex`（原生
> `~/tools/papyrus-compiler` 搭配指向 source cache 的 `MODFORGE_PAPYRUS_HEADERS`，或 Wine+CK）。

#### actions — 非對話的演出節拍 (NPC 劇情演出)
場景能做的不只交談。加一個 `actions[]` list，場景就變成一齣小型演出——
*走到某處 → 等待 → 交談*。每個 action 是一個原版的非 Dialog `SceneAction`（從
`dunTolvaldsCaveCrownScene` /`BardSongs*` 場景透過 `scnscan` 解碼），跑在一個**階段索引
視窗**之上。一個只被某 action 參照的階段可以有**空的 `lines`**——一個純*節拍階段*。
```jsonc
{ "editorId": "MF_AltarRite", "questEditorId": "MF_RiteQuest",
  "actors": [ {"aliasId":0,"npc":"MF_Priest"}, {"aliasId":1,"npc":"MF_Acolyte"} ],
  "phases": [
    {},                                              // phase 0: a BEAT (no lines) — window for the walk
    {"speaker":0, "lines":["Approach the altar."]},  // phase 1: spoken
    {"speaker":1, "lines":["As you say."]} ],
  "actions": [
    {"actor":0, "package":"MF_WalkToAltar", "startPhase":0, "endPhase":0},  // PACKAGE: actor runs a PACK
    {"actor":0, "timerSeconds":2.0,         "startPhase":0, "endPhase":0} ] // TIMER: pace the beat 2s
}
```
每個 action 設定**恰好一個**：
- **`idle`**——一個指向 `IDLE`（IdleAnimation）記錄的 ref（`<master>:0xFORMID`；用
  `find <master> <keyword> idle` 探索）。當 `startPhase` 開始時，actor **播放那個閒置動畫**
  （跪下／祈禱／手勢…），然後自然地回到 AI。動畫透過 SCEN 上每階段的
  `SceneAdapter` **OnStart fragment** 執行——一個 `SF_<scene>.Fragment_<phase>`，它呼叫
  `<alias>.GetActorRef().PlayIdle(<idle>)`（從原版 `SF_BardSongsBallad01Scene` 解碼）。該
  fragment 由 **`package`** 編譯 + 掛上（純粹的 `build`/`validate` 不掛任何 VMAD）。兩個
  陷阱，都已為你處理：(1) 引擎只**運行**有 `SceneAction` 的階段，所以一個
  idle action 也會發出一個 **Timer**（每個原版 fragment 階段都帶一個）——那個 Timer 使該
  階段觸發其 fragment 並**保持姿勢**；設 `timerSeconds` 控制保持時間（預設 2s）。
  (2) actor 必須**站立**——一個坐著／sandboxing 的 NPC 會忽略 `PlayIdle`，所以給他一個
  讓他留在原地的套件（一個 `allowSitting:false` 的 Sandbox），就像原版
  套件控制的場景 actor。idle 的 `<master>` 必須是真實的 IDLE——錯誤的 FormID 什麼都不播
  （無錯誤），所以要驗證它。
- **`package`**——一個指向 AI 套件的 ref（本 spec 中的一個 `packages[]` 條目，或一個外部
  `<master>:0xFORMID`）。actor 在階段視窗內運行那個 PACK。**移動** = 一個目的地為已放置標記的
  **Travel** 套件；**環境活動** = 一個 **Sandbox** 套件；等等
  （任何 `packages[]` 能建出來的東西）。build 發出一個 `Type=Package` SceneAction，其 `Packages`
  保存已解析的 PACK FormKey（在 pass 2 解析，就像 actor 別名一樣）。
- **`timerSeconds`**（> 0）——一個 `Type=Timer` SceneAction：場景在視窗內等待這麼多秒
  （原版吟遊詩人場景就是這樣掌控節拍）。在同一個節拍階段把 Timer 與移動 Package 配對，
  讓該階段在走動後可靠地推進（引擎在視窗的 action 完成時推進）。

**PlayIdle 組合**（idle = 動畫 + 它自己的保持 Timer；把 fragment 放在階段 ≥1，絕不放 0）：
```jsonc
"phases": [ {}, {"speaker":0,"lines":["By the Eight, I pledge my blade."]}, {"speaker":0,"lines":["It is done."]} ],
"actions": [
  {"actor":0, "startPhase":0, "timerSeconds":1.5},                                 // a standing beat (no fragment on phase 0)
  {"actor":0, "startPhase":1, "idle":"Skyrim.esm:0x0F11EE", "timerSeconds":4.0},   // IdleBlessingKneelEnter — kneel + pray (hold 4s)
  {"actor":0, "startPhase":2, "idle":"Skyrim.esm:0x0F11EF", "timerSeconds":2.0} ]  // IdleBlessingKneelExit — rise
```

`startPhase`/`endPhase` 是 `phases[]` 的索引；`endPhase` -1 = `startPhase`。驗證：actor
必須是場景 actor，階段視窗必須在範圍內，一個節拍（無台詞）階段必須被某個
action 覆蓋。見 `examples/scene-action-performance.json`（Borin 走過 Sleeping Giant Inn 到
原版的 `RiverwoodInnCenterMarker`，等 8s，然後兩人爭吵）以及 `examples/scene-playidle.json`
（一位懇求者跪下 → 喃喃祈禱 → 起身）。**範圍外（之後）：** sit / use-furniture（需要一個
UseItemAt PACK 模板——`MQ306EsbernSit` 形狀已解碼；以 `sittarget` PACK 模板提供）
以及 idle **event-name**（字串）變體而非 IDLE 記錄 ref。

### 條件 (conditions) — CTDA 閘（在 `dialogue` INFO、`banter` INFO 或 `package` 上）
一個條件是**靜態閘資料**，所以它存在於 spec 中（邏輯仍歸 Papyrus 所有）。
`dialogue[].conditions` 與 `packages[].conditions` 都採用相同形狀：
```jsonc
{ "function": "GetItemCount",          // form-arg: HasPerk | GetInFaction | GetItemCount | GetGlobalValue | GetStage | GetIsID | GetRelationshipRank
  //                                    //   GetQuestCompleted(quest) | GetDistance(ref; value=units) | GetIsCurrentPackage(pack) | GetIsVoiceType(VTYP/list)
  //                                    //   GetQuestRunning(quest) | GetInCell(cell) | GetInWorldspace(wrld) | GetEquipped(item/list) | GetDeadCount(npc base) | GetInCurrentLoc(location)
  //                                    // two-param: GetStageDone(param=quest, stage=N) — 1 if that exact stage was set
  //                                    //   IsSceneActionComplete(scene=<owning by default>, sceneActionIndex=N) — scene phase "advance when action N done"
  //                                    // actorValue-arg: GetActorValue | GetActorValuePercent (0..1 fraction)
  //                                    // alias-arg: GetIsAliasRef (use "alias", NOT "param" — names an alias on the OWNING quest)
  //                                    // no-arg situational: GetCurrentTime (hour 0..24) | IsInInterior | IsInCombat | GetRandomPercent (0..99) | TemperIsEnchanted (recipe temper guard)
  //                                    //   GetSitting (sit-state; ==3 sitting, ==4 sleeping) | GetGold (run-on actor's gold) | GetMapMarkerVisible (runOn=Reference to a map marker)
  "comparison": ">=",                  // == != > >= < <=
  "value": 500,
  "param": "Skyrim.esm:0x00000F",      // the function's form arg (faction/item/global/quest/npc) as a ref
  "actorValue": "",                    // for GetActorValue/GetActorValuePercent instead of param — e.g. "Health", "WaitingForPlayer"
  "alias": "",                         // for GetIsAliasRef instead of param — an alias NAME on the owning quest (resolved to its index)
  "runOn": "Reference",                // whose value: Subject (default) | Reference | Target | CombatTarget | ...
  "reference": "Skyrim.esm:0x000014",  // the ref read when runOn=Reference (here, the player)
  "or": false }                        // OR with the NEXT condition (default AND)
```
一個 `dialogue` INFO 已自帶一個自動的 `GetIsID` 說話者閘；這些是附加上去的。典型的隨從
用法：除非 `GetItemCount Gold >= 500`（在玩家身上）**且**
`GetInFaction CurrentFollowerFaction == 0`，否則隱藏付費招募台詞；以 `GetInFaction
CurrentFollowerFaction == 1` 閘控一個 Follow 套件，使它只在招募後才運行。見 `examples/follower_paid_spec.json`。

**`param` / `reference` 可以填什麼。**兩者都是**任意 ref**，而且都在 placements 與 `references[]`
**建好之後**才解析——所以除了 base record（faction/item/global/quest/NPC base）與原版
`<master>:0xFORMID` 之外，兩者都可以指一個**已擺放的 ref（placed ref）**：一個**檔內 `placements[]`
的 editorId**，或一個 **`references[]` label**。這正是「不寫 Papyrus 也能表達世界錨定的條件」的關鍵——
`{ "function": "GetDistance", "param": "the chair", "comparison": "<=", "value": 512 }`（玩家靠近**那個**
物件），或 `{ "function": "GetMapMarkerVisible", "runOn": "Reference", "reference": "my marker" }`。
這條規則在**所有用到這個共用 condition 形狀的地方**都成立：`dialogue`（inline ＋ `conditionTemplates`
＋ variants）、`banter`、`packages`、`perks`（perk 層與 effect 層）、`quests[].storyEvent.conditions`、
quest `aliases[].conditions`（`findMatching*` 的 match 過濾）、`scenes[].conditions` 與
`phases[].startConditions` / `completionConditions`、`stages[].conditions`、
`objectives[].targets[].conditions`，以及 recipe 的 conditions。解不到上述任一者會**警告並丟棄該
condition**（閘門於是什麼都沒測——把這個警告當成錯誤看待）。

**`GetIsAliasRef`** 以**run-on actor 填充了哪個任務別名**作閘（VIGILANT 最常用的
對話技巧——以角色閘控一條台詞，例如「Victim 別名」，而非硬寫的 NPC FormID）。以
**`alias`**（其在**所屬任務**上的名稱）給出別名，而非 `param`；build 把它解析為
別名索引。只有在有所屬任務的地方有效：`dialogue` / `banter` / `scene`（場景層級與
每階段）/ 任務 `stages[].conditions` / `objectives[].targets[].conditions`。在一個 `package` /
`perk` / recipe 條件上（無所屬任務），它會被丟棄並發出警告。
