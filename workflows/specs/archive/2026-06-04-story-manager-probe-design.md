# Story Manager 最小驗證探針 — 設計

> 日期：2026-06-04 · 狀態：已核可，待寫 plan
> 背景全文見 `docs/minor/ideas.md` 第 9 節「量產的關鍵槓桿：Story Manager + 條件式 Alias」

## 目的

驗證 Skyrim 引擎的 Story Manager（SM）動態選角機制能被 ModForge 產出的記錄正確驅動。
這是大量劇情自動生成（IDEAS 第 9 節）的關鍵槓桿——若 SM + 條件式 Alias 通了，
「模板任務 + 條件填充」能讓同一個 ESP 的劇情變化量放大一個數量級。

實驗刻意設計成**最小變數**，把 ModForge 缺的記錄欄位與引擎 quirks 一次撞出來。

## 核可的決策（2026-06-04 brainstorm）

1. **觸發方式**：原版內建事件節點（零 Papyrus）。不走自訂 Keyword + `SendStoryEvent`，
   避免引入 Papyrus 編譯這個額外變數。
2. **事件 + alias 填充**：**Kill Actor** 事件 + **From Event Data** 填充
   （Mutagen 型別 `QuestAlias.FindMatchingRefFromEvent`，拿事件帶來的「被殺 ref」填 alias）。
3. **成功訊號**：console `sqv MFSM_AvengeQuest` 看任務啟動（stage 設了）且 alias "Victim"
   填上被殺 actor 的 FormID。純結構驗證，零可見行為／零 objective／零 Papyrus。
4. **實作路徑**：探針優先，兩階段（見下）。先用最少程式碼撞引擎，再投資正式 spec 管線。

## 工程前提（已查證）

- Mutagen 0.53.1 有完整 typed API，**不需手刻 raw bytes**：
  - `StoryManagerEventNode`(SMEN) / `StoryManagerBranchNode`(SMBN) / `StoryManagerQuestNode`(SMQN)，
    皆繼承 `AStoryManagerNode`（具 `Conditions` / `Parent`(=PNAM) / `PreviousSibling`）。
  - `Quest.Event` + `Quest.EventConditions`（標記「可被 SM 此事件啟動」）。
  - `QuestAlias.FindMatchingRefFromEvent`（From Event Data 填充）；其他填充型別
    （`CreateReferenceToObject` / `FindMatchingRefNearAlias` …）留待階段二。
- ModForge 既有支援：Keyword / Quest / Alias / Conditions / QuestStages（已實機確認）。
  缺的就是 SM 三種記錄、Quest 的 Event 旗標、FromEvent alias 填充。

## 架構：兩階段

### 階段一 — 探針（本設計範圍）

寫一個小 C# harness，**繞過 spec→build 管線**，直接用 Mutagen typed API 拼出
`ModForgeStoryManager.esp`，走既有 package/zip 流程進遊戲。

**產出記錄：**

1. 模板 Quest `MFSM_AvengeQuest`
   - `Quest.Event` = Kill Actor 事件碼（綁定可被 SM 此事件啟動）
   - 一條 `ReferenceAlias` "Victim"，fill = `FindMatchingRefFromEvent`
   - startup stage 10（log entry，讓 `sqv` 看得到啟動）
   - **不**加 objective、**不**加 Papyrus（YAGNI）
   - 旗標：`StartGameEnabled=false`（靠 SM 啟動）；允許多重實例（radiant 慣例）

2. SM 節點樹（PNAM 父連結）
   ```
   原版 Kill Actor SM event 根 (Skyrim.esm 既有 SMEN)
     └─ SMBN (我們的分支, Parent → 原版 Kill Actor SMEN)
          └─ SMQN (Parent → SMBN, Quests=[MFSM_AvengeQuest])
               設 Num Quests to Run / Shares Event / 冷卻，避免連發
   ```

**前置步驟**：decode Skyrim.esm 的 SM 樹，找出原版 **Kill Actor** event 根的 FormID
（PNAM 哲學是「往既有節點下加分支」，我們的 SMBN 的 Parent 很可能要指向原版既有 SMEN，
而非自建 SMEN——就像 navmesh 的 NAVI 必須 override `Skyrim.esm:0x00012FB4`）。把找到的 FormID
記進筆記。

**關鍵未知（探針要撞的）：**
- Kill Actor 的 SM event 根：自建 SMEN vs. override 原版既有根（傾向後者）
- `Quest.Event` 欄位的確切 4-byte 事件碼
- FromEvent alias 填充是否需要額外 event-data 索引設定

### 階段二 — 正式 spec 管線（**不在本設計範圍**，另走 brainstorm/plan）

只在階段一遊戲內看到 `sqv` alias 填上 FormID 之後才開：
- spec schema 的 `storyManager` 段落 + Quest event 欄位 + FromEvent alias 填充型別
- `Generator.Build.StoryManager.cs`（typed record 產出 + PNAM 連結）
- validator（IDEAS 列的防呆：ESL 能否裝 SM 記錄、Find Matching Reference 條件太苛靜默失敗）

## 資料流

```
遊戲內：玩家殺死任一 actor
  → 引擎發 Kill Actor SM 事件，帶事件資料（被殺 ref + location）
    → 走訪原版 Kill Actor SMEN → 我們的 SMBN（評估條件）→ SMQN
      → 嘗試啟動 MFSM_AvengeQuest
        → Victim alias 用 FindMatchingRefFromEvent 拿「被殺 ref」填充
          → 填充成功 → 任務啟動、stage 10 set
驗證：console `sqv MFSM_AvengeQuest`
  → pass：stage=10 且 Victim alias = 被殺 actor 的 FormID
  → fail：任務 not running / alias 空 → 進除錯
```

## 除錯路徑（SM 靜默失敗）

- 開 Story Manager log：`Documents/My Games/Skyrim Special Edition/Logs/Story Manager.log`
  （SkyrimCustom.ini Papyrus logging）——記錄 SM 走訪哪些節點、哪個條件擋掉、哪個 alias 填不出。
  這是 SM 除錯的唯一眼睛。
- 隔離變數：若 FromEvent 填不出，先把 alias 換成 forced specific reference（ModForge 已支援）
  確認任務本身能被 SM 啟動，分離「SM 啟動」與「alias 填充」兩個失敗源。

## 在遊戲裏的測試流程

1. harness 產插件 → package → zip → `~/skyrim_mods`，裝進 MO2
2. 進遊戲（新檔或既有檔皆可，SM 節點即時生效；`.seq` 不需要——非開局對話）
3. 殺任意一個 actor（雞／兔／盜賊）
4. console：`sqv MFSM_AvengeQuest`
5. 判定：stage=10 且 Victim alias 有被殺 actor 的 FormID → pass

## 階段一產出物

- C# harness（throwaway test 或 `tools/` 下小程式，直接 Mutagen 拼插件 + 既有 package/zip）
- decode 出的原版 Kill Actor SM event 根 FormID（記進筆記，比照 navmesh 的 0x00012FB4）
- 一份 in-game 測試結果記錄

## 範圍外（YAGNI）

- 自訂 Keyword + `SendStoryEvent`（Script Event 入口）——量產真正的入口，但留待引擎機制確認後
- objective / QOBJ / 可見行為 / Papyrus
- Find Matching Reference（條件式搜索選角）/ Location Alias 等其他填充型別
- spec schema、build step、validator（全屬階段二）
