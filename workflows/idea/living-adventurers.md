# Idea #23 — 活世界人口框架：給每張捏臉一條命（living-adventurers）

← [ideas 索引](ideas.md)｜相關：[#17 任務節點圖](followers.md#17-skyrim-原版任務節點圖--批量隨從反應生成2026-06-15) · [#18 隨從記憶系統](followers.md#18-隨從記憶系統任務經歷追蹤與對話更新2026-06-15) · [#3 商隊與船隊生活](world-building.md) · [#1 擴充停更隨從](followers.md#1-擴充停止更新的隨從模組) · `settlements:` macro

**狀態**：idea（模擬迴圈未驗,屬可行性待證）。候選獨立子專案 `sub_projs/living-adventurers/`。子名沿用 living-adventurers,但**範圍已不限冒險者**（見下）。

## 緣起（這是專案的靈魂）

Nexus 上一堆**純好看的 standalone NPC**——精美的臉模、語音、裝束——卻只是站在某個酒館角落等玩家招募。**浪費了。他們應該有自己的生活。**

而真正的使用情境是:**某天在 Nexus 看到一張捏臉,心裡就開始浮現他是誰、什麼來歷、過著怎樣的日子——然後想幾分鐘內就把他放進自己的天際省、讓他活起來。** 所以這個專案的**核心交付物不是那批 cast,是那條「快速 enroll 的 on-ramp」**：每多一個活 NPC = 一份**極小的 spec**（指向那個 NPC 的 ActorRef + 選一個 archetype + 幾行背景）,`build` 出 patch,他就入世了——不是寫程式,是填一張卡。

這把專案定為一個 **patch 生成器 + 不斷成長的名冊**（ModForge 本命,比照 `sofia-patch` / `followers-patch`）。**這是框架,不是會做完的 mod——天際省的人口永遠可以再 +1,沒有天花板。**

## Idea

不只是冒險者:一群**有名字、有故事的 NPC** 在天際省過著各自規律的離場生活,玩家在酒館 / 領主廳 / 市集 / 神殿 / 野外各處撞見他們,酒館傳唱他們的事蹟。有人是接 missive 的冒險者,有人是城市↔城市跑商的**行商團**,有人是駐城的**藥草師**,有人是神殿間巡禮的**行走祭司**……玩家可**互動**——搶任務、雇用、資助或破壞。

## 不只是冒險者：archetype 框架（核心抽象）

**模擬引擎完全不在乎這個 NPC 是誰。** 同一套「抽象幽靈模擬 + 就地實體化」對任何「過規律離場生活的 NPC」都成立。差別只在一張**行為側寫（archetype）＝一包資料**：

| archetype | 離場任務循環 | 錨點 | 現身時在幹嘛 | 傳唱 |
|---|---|---|---|---|
| 冒險者 | 接 missive → 清地牢/打強盜 | 旅館/board | 旅館喝酒、領主廳領賞、地牢巡邏 | 戰功 |
| 行商團 | 城市↔城市跑商（buy low sell high） | 各城市集 | 路上駝隊行進、市集擺攤 | 行情/見聞 |
| 駐城藥草師 | 野外採集 → 回城煉藥販售 | 某城的鋪子 | 鋪子煉藥、野外採花 | 藥方/八卦 |
| 行走祭司 | 神殿間巡禮 | 各神殿 | 祭壇祈禱、路上佈道 | 神諭/傳道 |
| **強盜/逃兵（敵對）** | 攔路打劫、突襲、回營窩藏 | 強盜營/藏身點 | 營地巡邏、路上埋伏 | 惡名/懸賞/真相 |
| **魔法學徒** | 研究/取試劑/煉法器、練法術 | 冬堡學院 | Arcanaeum 看書、中庭練法、冬堡閒晃 | 學術突破/醜聞/失敗實驗 |

引擎（timer 推進 + MoveTo 現身）一行不改,新 archetype 就是新增一包 `{任務池, 錨點清單, 現身 package, 傳唱文本}`。天然吃下 #3 商隊船隊,與 `settlements:` macro 的住民作息同源。

### alignment 是一個軸（友善 / 中立 / 敵對）＋ 道德選擇分岔

archetype 帶一個 **alignment** 值。敵對 NPC 不只是「打」——撞見時有**分岔**：

- **敵對**：直接戰鬥（他過著敵對的生活：窩營地、攔路、跟同夥混）。
- **理解 / parley**：放下武器接近 / 過 Speech / 讀到關於他的傳聞 → 解鎖他的 backstory（也許是逃兵在養活村子）→ 玩家可放他走、收編、或揭穿。

**幽靈模擬記得選擇**（playerRel KV 本就在資料模型）：放過他 → 下次再遇是中立/友好；殺了他同夥 → 見你拔刀。每個敵對 NPC ＝一段可走向不同結局的小關係。這把「可互動」從友善（搶任務/雇用/資助）延伸到敵對（敵對/理解/收編/放生），共用同一份 per-NPC KV。

## 外部 mod 協同（出身即關係）

關係的**初始值可由玩家出身決定**,不必都從零撞見。**Alternate Start - Live Another Life（LAL）** 讓玩家選出身（強盜/吸血鬼/派系成員…）——若偵測到玩家**出生在某強盜窩**,就 seed 玩家跟該窩的特別強盜的 playerRel（同夥/友好/共 faction）：玩家**一開始就認識他、甚至是他的人**。同一份 per-NPC KV,只是初始值由出身寫入。

> ⚠ **待驗（mod 事實,照 repo 規矩查證,勿臆測）**：LAL 如何把「出身選擇 / 起點」暴露給 condition 或 script（globals? quest stage? 起點 cell?）——進 mod-survey 查 LAL 再定接法。其他出身（吸血鬼氏族、各派系）同理可 seed 對應 NPC 關係。

## 快速 enroll 管線（產品的核心）

每個活 NPC 的輸入要壓到最小:
```
{ ref: "<followerMod>.esp:0xFORMID",   // 指向既有捏臉的 ActorRef
  name, archetype,                      // 選一個側寫
  backstory: "...",                     // 幾行背景（驅動傳唱/對話文本）
  homeHolds: [...], tier }              // 少量錨點/強度
```
→ `build` → patch esp（把該 ref 接進 sim controller + 生傳唱對話 + 錨點）。**「想他的故事」那步可讓 AI 起草**（接 #17 批量生成管線：看圖/讀 mod 頁 → 生 backstory + archetype 建議 → spec）。腦補 → brief → spec → 入世。

## 拍板的設計決策（2026-06-27）

| 岔路 | 選擇 | 帶來的後果 |
|---|---|---|
| 產品形態 | **enroll 框架 + 成長名冊**（非固定 mod） | 核心是 on-ramp（極小 per-NPC spec）;cast 永遠可 +1,無天花板 |
| 卡司來源 | **既有 standalone follower / NPC mod 為首選**,可手作可 AI 生 backstory | 白賺臉模/語音/裝束;follower 的 unique ActorRef 本就是 persistent actor |
| 規模分層 | **具名錨點層 + 環境群像層** | 重要角色 = 一人一 persistent ref + MoveTo 進出（乾淨無 churn）;背景人口（無名行商隊/采藥人）量大 → 回到 pooled ref / LVLN-spawn。兩層共用同一 sim 引擎,只差實體化策略 |
| 玩家關係 | **可互動**（搶任務 / 雇用 / 資助破壞） | 需處理玩家↔幽靈模擬的雙向同步 |
| 模擬保真度 | **抽象幽靈模擬**（唯一務實解） | 離場 NPC＝純資料,timer 推進,玩家同地點才現身 |

## 鐵律：為什麼一定是「抽象幽靈模擬 + 就地實體化」

Skyrim 只跑玩家附近載入格的 AI,離場 NPC 是凍結的——不可能讓冒險者「真的」走三天路去清怪。所有活世界 mod（Immersive Patrols、Populated、Missives 逃犯追捕）走同一條路。兩層架構：

### 1. 抽象幽靈模擬（mod 的引擎,看不見）
每個具名冒險者是一包資料（JFormDB / StorageUtil KV,memory `storage-writes-ingame-confirmed`）：
```
{ id, name, archetype, level,
  task: {type: kill|gather|retrieve|courier|banditcamp, targetLoc, tier, progress, deadline},
  state: traveling | atTask | resting(inn) | reporting(jarlHall) | injured,
  abstractLoc,                       // 現在「在」哪個 hold/POI
  storyLog: [完成的戰功…],            // 酒館傳唱 + 自己對話的素材
  playerRel: {rivalry, favor} }
```
一個 controller quest 用 `RegisterForUpdateGameTime`（鏈式,勿用 OnUpdate 持續循環——存檔膨脹,見 followers.md §1）每數遊戲時推進狀態機：挑一個 missive 任務 → 設 ETA「旅行」→ N 時後擲成敗骰（權重＝level vs tier）→ 成功則 append storyLog + 可能升級 + 回 inn 休息。**全程純資料,不需 actor。**

### 2. 就地實體化（看得見的部分）
玩家進某地點 → controller 檢查有無冒險者 abstractLoc==此地且 state 對得上（resting→inn / reporting→jarl hall / atTask→dungeon / traveling→路上 encounter）→ 有就把那個**常駐唯一 actor** `MoveTo` 進場（+128 Z free-fall,memory `dynamic-spawn-debugging`）、ForceRefTo 進 forced alias、掛對應 package（sandbox@inn / sit@hall / patrol@dungeon / travel@road,memory `radiant-alias-package-byte-truths`）。玩家離開 → MoveTo 回 holding cell 冷凍。
> **具名路線紅利**：每個冒險者是**一個 persistent unique actor**,只是被 MoveTo 進出,不是 LVLN spawn。無 spawn/despawn churn、無殭屍 actor、無重複身分——比程序大軍乾淨太多。

### 3. 酒館傳唱（傳唱）
storyLog 條目 → 條件化的 Rumors topic INFO（bard / innkeeper / 酒客）："聽說 X 一個人清了白漫龍臨?" 走 #18 的 StorageUtil/GLOB 經歷集 + condition 對話,文本可走 #17 批量 AI 生成管線。

### 4. 玩家互動（選了可互動）
全部讀寫同一份 per-adventurer KV：
- **搶 / 競爭**：玩家接同一張 missive,先完成 → 該冒險者任務失敗 → 反應（rivalry++）。
- **雇用**：把具名冒險者招為（臨時）隨從 → 接 `sub_projs/followers-patch` / vanilla SetFollower（memory `hirefollower-paid-gold-bug`）。
- **資助 / 破壞**：給金/裝（成敗骰 +）或通風報信給強盜（成敗骰 −）。

## ModForge 已有的基礎 vs 缺口

**已有（memory 全有確認配方）**：dynamic spawn / MoveTo、radiant alias+package fill、StorageUtil KV、SM 觸發、對話 INFO+condition、faction、語音生成。任務層的 quest stage/objective/fragment、FLST、GLOB 量產、AI package、board container/activator —— 見 `sub_projs/mod-survey/findings/missives-modforge.md`,**ModForge 都能生**。

**缺口（前置工程,gating）**：
- **roadmap #7 LocationAlias fill / #8 nested ReferenceAlias fill / #9 `UpdateCurrentInstanceGlobal`** —— 隨機選任務目標地點的命脈,做任務層前必補。見 [mod-survey-gaps.md](../roadmap/mod-survey-gaps.md)。
- **幽靈模擬 controller**（狀態機 + game-time timer）：手寫 .psc,比照 dispatcher/controller embed,生成器只負責 wire（memory `dispatcher-magic-trigger`）。
- **就地實體化 controller**（玩家進地點 → MoveTo 對的冒險者 + package）。

## 待證的可行性風險（idea 階段要先 spike）

1. **MoveTo 進出 churn**：玩家快速穿梭 cell 時的效能 + actor cleanup;persistent unique actor 的 package 在 MoveTo 後是否乾淨重掛。
2. **成敗骰平衡**：sim roll 的權重曲線（太強搶光玩家任務 / 太弱沒存在感）。
3. **玩家搶任務的雙向同步**：missive 是玩家也能接的真 quest,還是冒險者專屬的抽象任務?兩者如何對帳。
4. **storyLog → 傳唱對話**的條件爆炸（戰功組合 × NPC 類型）。
5. **與 follower 原 mod 共存**：standalone follower 自帶招募 quest + 常駐在某 cell。把他接進模擬＝劫持他的 ActorRef 去過冒險生活,但**不能破壞 vanilla 招募對話**。設計：玩家未雇用時過冒險者人生,雇用時回歸正常 follower（hire 互動本就在設計裡,天然對接）。follower mod 當 master（依賴它的 ActorRef FormID）—— ModForge 跨 master 引用 vanilla/外部 record 已是熟路（memory `vanilla-nif-paths-must-be-verified` / `esm-formid-access`）。

## 建議的下一步

idea → **先做一個最小 spike 證明模擬迴圈**（1 個具名冒險者：timer 推進抽象任務 + 玩家進酒館時 MoveTo 現身 + 一條傳唱對話），跑通再進 roadmap / spec。任務層的隨機地點要等 roadmap #7–9 補完,但**模擬迴圈本身不依賴它們**,可先獨立驗證。
