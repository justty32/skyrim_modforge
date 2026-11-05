# Idea #23 — 活世界人口框架：給每張捏臉一條命（living-adventurers） — 拍板決策與鐵律

← [入口](../living-adventurers.md)

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
- **雇用**：把具名冒險者招為（臨時）隨從 → 接 `../../analysis/followers-patch` / vanilla SetFollower（memory `hirefollower-paid-gold-bug`）。
- **資助 / 破壞**：給金/裝（成敗骰 +）或通風報信給強盜（成敗骰 −）。
