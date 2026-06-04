# 模組創作想法隨記

個人構想備忘，未必有明確優先順序，隨時增補。

---

## 1. 擴充停止更新的隨從模組

許多高品質的隨從模組已停止維護，想在此基礎上做後續擴充：

- 補充更多日常對話與情境反應（旅行中的閒聊、遭遇特定地點或事件的台詞）
- 深化與玩家的互動（任務完成後的感想、好感度觸發的特殊對話）
- 多隨從同行時彼此之間的互動對話（隨從 A 評論隨從 B、爭執、互相調侃）

### 隨從在場偵測方案

不依賴任何多隨從框架（NFF/EFF 等），用 vanilla Papyrus 三層判斷：

```papyrus
; 1. 已載入、可參與 Scene —— 同 Cell（注意：室內 Cell 經常是一整層地城，
;    不等於「同一個房間」；這層只保證引擎已載入對方）
bool loaded  = followerRef.GetParentCell() == Game.GetPlayer().GetParentCell()

; 2. 夠近 —— 室外 Cell 粒度太細（一格地形），用距離補
bool nearby  = followerRef.GetDistance(Game.GetPlayer()) < 2048.0

; 3. 真的「看得見」—— 引擎原生視線檢查，這才是不穿牆的判斷
bool visible = Game.GetPlayer().HasLOS(followerRef)
```

第 3 層按需使用：兩個隨從在背後閒聊，被柱子擋住也無妨；「評論玩家正在做的事」這類需要真的看見的觸發才加 LOS。

「是否在隊伍」若有需要，可分層追加：
- 大多數隨從模組走標準系統 → `followerRef.IsPlayerTeammate()` 夠用
- 有自訂跟隨機制的模組 → 才去讀該模組的 Quest Stage / 專屬變數

### Scene 觸發骨架

**硬性前提：Scene 的 Actor 必須是同一個 Quest 的 Alias。** 不是偵測到兩個 NPC 就能開演——要先 `ForceRefTo()` 把他們填進自己 Quest 的 Alias，Scene 才能引用。常駐 Quest 同時是偵測器與 Alias 容器，Alias 的填入/釋放時機（隨從死亡、被解散、跨模組 NPC 未載入）都要處理。

```
常駐 Quest
├── Alias: FollowerA（ForceRefTo 填入，注意釋放時機）
├── Alias: FollowerB（同上）
└── Papyrus Script
    RegisterForSingleUpdate(N) 鏈式輪詢（不要用 OnUpdate 持續循環——存檔膨脹的經典來源）：
      if A 在場 && B 在場 && !冷卻中 && 玩家不在對話/戰鬥中
          StartScene(InteractionScene)
          設冷卻 flag
      endif
      RegisterForSingleUpdate(N)   ; 重新註冊下一輪
```

### 語音問題（所有對話類想法共同的前提）

Skyrim 的對話沒有語音檔幾乎不能用——無聲台詞的字幕一閃而過（字幕停留時間由音檔長度決定）。

- **預設假設：玩家都裝了 Fuz Ro D'oh**（SKSE 插件，讓無聲台詞字幕正常停留）——自己玩、開發迭代期都以此為前提
- **生成靜音 .fuz** 與 **AI 語音合成**（xVASynth 有 Skyrim 聲庫可做 voice cloning、或 ElevenLabs 等）仍然需要——擴充既有隨從要讓新台詞像「她本人」說的，唯一正解是語音合成。這屬於之後的工作流，不在 ModForge 本體範圍

---

## 1b. NPC 劇情演出（Scene 驅動）

不只是對話，想在特定時機（如玩家選了某個選項後）讓 NPC 進行完整演出：

- 走到指定地點（Scene Phase + XMarker 目的地，依賴 navmesh）
- 播放指定動畫（`PlayAnimation` event name，自訂動畫需 DAR/OAR）
- 使用場景物件（FURN — 椅子、工作台、祭壇等，NPC 自動走過去互動）
- NPC 之間的對話（多 Actor 輪流說台詞，每句是一個 action）
- 可選：附帶鏡頭（Camera Shot record），簡單演出不做也行

### 觸發流程骨架

```
玩家選對話 (INFO Result Script)
  └─ SetStage(MyQuest, N)  或直接 MyScene.Start()

Scene
  Phase 1: NPC 移動到 XMarker
  Phase 2: NPC 播放動畫 / 使用 FURN
  Phase 3: NPC 之間對話
  Phase 4: 結束，回正常 AI
```

### ModForge 待補

SCEN 有基本支援（phases、actors、dialogue actions）；「移動到指定位置」「播放動畫」「使用物件」這幾種 action type 目前 spec 還沒對應欄位，之後有需要再擴充。

---

## 2. 喜愛劇情模組的遺憾分支改版

有些劇情模組在關鍵節點缺少想要的選擇，想自己補上：

- 製作平行的劇情分支，讓玩家能走「作者當初沒寫的那條路」
- 保留原模組的人設與世界觀，只擴充分支，盡量以 Patch 形式存在
- 可能涉及新的 INFO/對話樹、條件觸發、任務階段

---

## 3. 商隊與船隊生活

想體驗上古卷軸中的流浪商人視角：

- 加入或組建陸路商隊，沿固定路線移動、進行交易
- 船隊：海路版本，港口停靠、貨物買賣
- **空艇冒險**：利用空艇（Airship）作為移動基地，穿越各地甚至異域
- 可能需要自訂 AI Package（商隊巡邏路線）和配套的商業系統 UI

---

## 4. 異世界冒險（另開 Worldspace）

- 開闢一個全新的 Worldspace，設定迥異於泰姆瑞爾的世界觀
- 以「穿越」或「傳送門」作為進入手段，有對應的劇情驅動
- 可以是奇幻異界、蒸汽龐克城市、廢土等任何主題

---

## 5. 其他遊戲資源移植 / 引擎復現

- 將電腦上其他遊戲的場景、角色、玩法概念「翻譯」進 Skyrim 引擎
- 不是完整移植，而是用 Skyrim 的敘事與互動語言重現那個遊戲的精髓
- 需要評估資源格式轉換（模型/材質）以及遊戲規則的系統化對應

---

## 6. 在 SkyUI 基礎上擴充 UI

- 已有先例：如快捷欄擴充模組（iEquip、Wheeler 等），先研究其實作方式
- 想添加的元素：技能槽（可快速切換施法序列）、任務追蹤懸浮框、小地圖增強
- 核心挑戰：SkyUI 以 ActionScript/Flash 實作，擴充需要 AS3 或 Scaleform 知識

---

## 7. 遊戲內嵌入網頁 UI

- 概念：在 Skyrim 視角內顯示一塊可互動的「瀏覽器」面板
- 可能方向：利用 CEF（Chromium Embedded Framework）或類似技術搭配 SKSE 插件
- 應用場景：遊戲內查閱攻略、顯示 AI 代理回傳的資訊、即時地圖服務
- 技術難度高，需要 SKSE/C++ 層面的介入

---

## 8. 程序生成的世界

- 更生動、更具隨機性的世界：地形、地城、NPC 組成、事件都有程序生成的成分
- 參考：Requiem 的縮放系統 + Radiant Story 的延伸 + 自訂世界生成邏輯
- ModForge 本身的 Generator 或許可以作為批次生成「骨架 ESP」的起點，再疊上程序邏輯
- 長期目標：每次開新檔都有不同的世界佈局

---

## 9. 大量劇情自動生成（獨立工作流）

想要體驗遠超現有模組數量的劇情內容，靠手工寫規格無法擴展，需要一套 LLM 驅動的生成管線。

### 分層架構

```
故事生成系統（獨立工作流）
  ├─ LLM 構思劇情概要、人物弧線、對話
  ├─ 展開成 ModForge spec JSON
  └─ 呼叫 ModForge build → .esp

ModForge（下游工具，負責記錄層）
  └─ spec → 合法 ESP，不參與敘事設計
```

ModForge 對這條管線的貢獻是：spec 格式夠清楚讓 LLM 可靠填寫，build 出來的 ESP 不出錯。`for_agent.md` 已經是為此設計的。

### 真正的難題（故事生成系統自己要解）

- 跨任務的 NPC 狀態記憶（A 任務結果影響 B 任務）
- 人物個性一致性（不同 NPC 說話風格要有區別）
- 大量劇情之間不重複、不單調
- 語音：必須把 TTS 排進管線（見 1 節的語音問題），不然產出的是啞巴劇情

### 引擎層的規模天花板（量產前必須面對）

- **載入順序上限**：完整 ESP 約 254 個、ESL 約 4096 個——「一個任務一個 ESP」走不遠；生成系統要嘛合併輸出成大 ESP，要嘛設計插件回收機制
- **ESL FormID 預算**：一個 ESL 只有 2048（舊版）/ 4096（1.6.1130+）個新 record；一條有對話的任務輕易吃掉幾十到幾百個
- **存檔膨脹**：每個有腳本的 Quest 都進存檔；幾百個生成任務同時 running 會拖垮存檔——需要「完成即 Stop + 清 Alias」的紀律

### 量產的關鍵槓桿：Story Manager + 條件式 Alias

Skyrim 原生的 Story Manager + 條件式 Quest Alias 就是設計來做「動態選角、動態選地點」的（Radiant 系統的底層）。生成系統若輸出「模板任務 + 條件 Alias」而非「寫死 NPC 的任務」，同一個 ESP 能產生的劇情變化量放大一個數量級。

**核心循環**：

```
遊戲內事件（殺人、進入地點、升級、合成…）
  → 引擎帶事件資料走訪 SM 節點樹（SMEN 事件根 → SMBN 分支 → SMQN 任務節點）
    → 逐層評估條件 → 嘗試啟動 Quest
      → Alias 用事件資料 + 條件動態填充（Find Matching Reference / Location Alias / From Event Data）
        → 全部填充成功才啟動；任一失敗 → 換下一個候選（靜默）
```

**對量產最重要的入口：Script Event**

```papyrus
; 對著自訂 Keyword 發射 SM 事件，可帶兩個 ref + 兩個數值
MyStoryKeyword.SendStoryEvent(akLoc, akRef1, akRef2, aiValue1, aiValue2)
```

策略：生成系統供應「模板任務池」掛在自訂 Keyword 的 Script Event Node 下；一個輕量常駐腳本在恰當時機發射事件；SM 按條件挑出此刻最合適的模板並自動選角。調度權在引擎（原生條件評估、零 Papyrus 負擔），生成系統只管供應模板。

**工程上的好性質**：SM 節點靠 PNAM（指向父節點）連結，不是父節點持有子清單——多個 ESP 可同時往同一個事件節點下加分支，互不衝突、不用 override 原版記錄。對大量生成的插件共存非常友善。

**已知的坑**（原版設計就有）：
- Alias 填充失敗 = 任務靜默不啟動，無錯誤訊息（CK 有 SM 日誌 ini 開關可救）
- 條件只在啟動時評估一次；啟動後世界變化（選中的 NPC 死亡）要靠 Alias 的 Death/Disable flag 處理
- Quest Node 要設 `Num Quests to Run` / `Shares Event` / 冷卻，否則同一事件連發多個任務
- Find Matching Reference 受已載入區域 / Location 範圍限制，條件太苛刻會永遠填不出來

**ModForge 現況缺口（落地前要補的）**：
- SMEN / SMBN / SMQN 三種記錄 spec 完全沒支援
- Quest 的 Event 欄位（標記「可被 SM 啟動」）
- Alias 的條件式填充類型（Find Matching Reference / Location Alias / From Event Data——目前只有 forced ref 一類）

**最小驗證實驗（第一步就做這個）**：
手寫一個 spec → 一個 Script Event Keyword + 一個帶 Find Matching Reference Alias 的模板任務 → 遊戲內 `SendStoryEvent` → 看 SM 能否正確選角。這個實驗會把 ModForge 缺的欄位全部暴露出來。

**✅ 階段一探針結果（2026-06-04，實機 PASS）**：
改用更精簡的路徑驗證——原版 **Kill Actor** 事件節點（零 Papyrus）+ **From Event Data** 填充，而非自訂 Keyword + `SendStoryEvent`。`StoryManagerProbe.BuildProbe`（`src/ModForge.Core/StoryManagerProbe.cs`）直接用 Mutagen 拼 SMBN→SMQN（additive 掛在原版 Kill Actor SMEN `Skyrim.esm:0x013010` 下）+ 帶 `FindMatchingRefFromEvent` 的模板 Quest。CLI：`smtree`（解事件根）、`smprobe`（寫 esp）。
- **結果**：殺一個完整 actor 後 `sqv MFSM_AvengeQuest` → 任務啟動、Victim alias 填上被殺者 FormID。**SM 動態選角在 ModForge 產出的記錄上跑通了。**
- **離線解出的真值**（記進 [[story-manager-kill-recipe]]）：`Quest.Event="KILL"`；alias `FindMatchingRefFromEvent{FromEvent="KILL", EventData="R1"=52 31 00 00}` = 事件被殺者槽；SMBN 零條件 = 每次擊殺都嘗試。
- **暴露的引擎 quirk**：Kill Actor story event **不對 `SimpleActor` 旗標的環境 critter 發送**（雞、兔…）——殺雞無觸發，殺牛/盜賊才有。量產 radiant 擊殺內容時須以完整 actor 為對象。
- **階段二 spec 管線：✅ 實機驗證通過（2026-06-04）**。意圖導向落地：`QuestSpec.storyEvent`(event+conditions) + `QuestSpec.aliases`(fill `fromEvent:<slot>`|`forced:<ref>`)；事件表 `StoryManagerEvents`（只 KillActor）；`Generator.Build.StoryManager.cs`（pass 2，自動生 SMBN→SMQN + 清 StartGameEnabled）；`Generator.Validate.StoryManager.cs`。探針 builder/smprobe 退役、`smtree` 保留。樣本 `examples/story-manager-kill.json`。278 測試綠。設計/計畫見 `docs/superpowers/specs/2026-06-04-story-manager-spec-pipeline-design.md` + `docs/superpowers/plans/2026-06-04-story-manager-spec-pipeline.md`。
  - **實機發現（2026-06-04）**：(a) **ESL 插件能裝並啟動 SM 記錄**（`MFSM_Esl` 殺牛後 stopped→running）——解決 IDEAS 未解問題，不需 ESL 守衛。(b) **多任務的 SM 樹結構**（三輪實機才釘對）：vanilla = 事件根 → **一條**分支 → **多個** quest node（彼此 PreviousSibling 串鏈）。兩條鐵律：① 同父 sibling 節點必須 PreviousSibling 串鏈，否則引擎漏掉除一個外的全部；② **事件根下的多條分支是互斥處理器，引擎只跑一條**——所以「每 quest 一條分支」只有 head 那條的任務啟動。已修（`Generator.Build.StoryManager.cs`）：每事件根**共用一條分支**，每 quest 一個 qnode 掛其下、qnode 串 PreviousSibling。實機軌跡：4 條未串分支→全不啟動；4 條串鏈分支→只 head(Basic)；1 分支+4 串鏈 qnode→仍只 head。
  - (c) **「一事件只啟動一個最先符合的 quest」是引擎的正確 radiant 行為**（量產時引擎按條件挑一個模板，非每個都觸發），不是 bug——所以同事件多個無條件 quest 只有 head 啟動是對的。要逐一驗證變體就一個 quest 一個插件。
  - **最終實機全綠（單一-quest 插件逐一測）**：basic（Victim=被殺者）、victim+killer（**Killer R2=玩家**）、forced（Boss=指定 ref 0x14）、condition（不破壞觸發）、ESL 五個變體全部 `sqv` running + alias 正確填充。階段二完成。
- **階段二+ 擴充：✅ 實機驗證通過（2026-06-04）**——三條軌並行（worktree subagent）整合上 master，291 測試綠。
  - **實機抓到並修掉的兩個 fill bug（都離線解碼 vanilla 修正）**：① **location 槽填 null**——location-填充 alias 必須 `QuestAlias.Type=Location`（vanilla KingOlafs/CRHoldExpansion 的 NewLoc 都是），我們建成預設 Reference → 引擎不把 location 塞進 reference alias。已修：fromEvent 槽 'L' 開頭自動設 Type=Location。② **殺被保留的 NPC 不啟動**——Sven 被常駐 `FreeformRiverwood01` 用 `ReservesLocationOrReference` 鎖住，沒 `AllowReserved` 的 Victim 搶不到他 → 必填失敗 → quest 不啟動（Alvor 的保留者已完成所以可）。已加 opt-in `allowReserved`（QuestAliasSpec，預設關；uniqueActor 強制開）。**鐵律補充**：alias Type 要對上事件槽 payload 種類否則填 null；任一必填 alias 填不上 → quest 靜默不啟動；reserved ref 需 AllowReserved。另修 latent bug：`QuestAlias.Flags` nullable、`|=` 對 null 是 no-op（連 Optional 以前都沒寫進去），改 `GetValueOrDefault()` 起底。實機全綠：ChgLoc NewLocation 填上 LCTN、殺 Sven 也 running（Victim+烏弗瑞克）。
  - **(A) 事件表 +4**（離線從 Skyrim.esm 解碼，純資料、build/validate 不變）：`ChangeLocation`（根 `0x01320E`/碼 `CLOC`，槽 oldLocation=L1、newLocation=L2）、`CastMagic`（`0x046829`/`CAST`，caster=R1、target=R2、location=L1）、`AddItem`（`0x02C439`/`AIPL`，owner=R1、location=L1）、`Assault`（`0x02C494`/`ASSU`，victim=R1、attacker=R2、location=L1）。R1/R2/L1/L2 = `52 31`/`52 32`/`4C 31`/`4C 32 00 00`。被拒事件：ScriptEvent(需 Papyrus)、ActorDialogue/Hello(太吵)、CraftItem/RemoveItem(無乾淨 ref 槽)、ArrestEvent 等(vanilla 0 範例無法解)。樣本 `examples/story-manager-{changelocation,assault}.json`。
  - **(B) 新填充型別 `uniqueActor:<ref>`** → `QuestAlias.UniqueActor`（指定唯一 NPC，不靠事件帶 ref，語法同 forced）。`createObject`(`CreateReferenceToObject`)、`findMatching`(`Conditions`/`FindMatchingRefNearAlias`)評估後**緩做**——需先解碼 vanilla 範例落地，不盲猜。樣本 `examples/story-manager-uniqueactor.json`。
  - **(C) Script Event 入口 — ✅ 實機驗證通過（2026-06-04）**（`MFSE_Target` running + Target alias=玩家，整條自訂入口鏈通）：研究尖兵 `docs/superpowers/specs/2026-06-04-script-event-entry-spike.md`、計畫 `docs/superpowers/plans/2026-06-04-script-event-entry.md`。這是「**ModForge 內容自己發任意 story event**」的最終通用入口（之前所有觸發都綁引擎既有事件）。落地：事件表加 `ScriptEvent`（根 0x01379A/碼 SCPT/槽 ref1·ref2·loc）；`storyEvent.keyword` 宣告自建 KYWD，build 在共用分支加 keyword 過濾條件 `GetEventData/GetIsID Member=Keyword Record=<KYWD> ==1`（Mutagen `GetEventDataConditionData` 原生；同 keyword 共用分支、不同 keyword 不同分支）；validate 要求 keyword 已宣告。通用派發器 `assets/papyrus/MFStoryEventDispatch.{psc,pex}`（Global `Fire(kw,ref1,ref2,loc)`→`SendStoryEvent`）編一次、embed 進 CLI、package 遇 ScriptEvent quest 自動丟進 `Scripts/`——一份 byte 服務所有 mod，per-mod build 不碰 Papyrus。**Papyrus 編譯**：Wine/`mono`+CK PapyrusCompiler 用 cache 全 source set（`~/.cache/modforge/papyrus/Source/Scripts`，14301 .psc）；native 編 user 腳本時 headers 不全可設 `MODFORGE_PAPYRUS_HEADERS` 指向該 cache。端到端範例 `examples/story-manager-scriptevent.json` + 觸發腳本 `examples/MFSE_TestTrigger.psc`（OnInit `SendStoryEvent`）；測試包 `~/skyrim_mods/MFSE_Test.zip`：reload 後 `sqv MFSE_Target` 應 running、Target alias=玩家。**之後**：把派發器接到實際觸發場景（dialogue fragment / magic effect / alias script）做量產接線。

### ModForge 可以貢獻的：資源索引

故事生成系統需要知道「我要一隻狼 / 一條麵包用哪個 FormKey」，ModForge 可以擴充一個 `catalog` 指令，把 Skyrim.esm（或任意 ESP）的資源批次匯出成索引供查詢：

```
modforge catalog Skyrim.esm --types Npc,Weapon,Armor,Food,Creature,Location,...
→ 可查詢的索引（SQLite / 分類分片 / 查詢 API）
```

**形態注意**：Skyrim.esm 有幾十萬筆 record，單一大 JSON 是 LLM 讀不完的——catalog 應該是**可查詢的索引**，讓生成系統按需檢索（按類型/關鍵字/名稱查），而不是一次性大檔。

索引包含兩層：

**資料層**
- FormKey、EditorID、顯示名稱、記錄類型
- 關鍵屬性（NPC 的種族/等級、食物的回復量、武器的傷害值…）

**美術層**
- NPC 外型：種族、性別、臉部預設、髮型、眼睛顏色
- 物品模型路徑（`.nif`）、貼圖路徑（`.dds`）、物品欄圖示
- 可讓生成系統在撰寫人物描述或選配外型時有真實依據
- 聲音資源：語音類型（Voice Type）、環境音效、音樂路徑（`.fuz` / `.wav`），讓生成系統知道哪些聲音實際存在可用
- 動作資源：可用的 idle 動畫 event name、paired 動畫、furniture 互動動畫，供 Scene 演出設計時引用
- 地點資源：Location / Cell 清單，標註類型（地城、城鎮、廢墟、戶外等），讓生成系統在安排事件發生地點時有真實場景可選
- 劇情與對話內容：QUST（任務結構、階段、目標）、DIAL/INFO（對話樹、台詞、條件）— 不限於原版，第三方模組也要能匯出；故事生成系統擴充別人的劇情時，需要先讀懂原模組說過什麼，才不會產生衝突或重複
- 派系（FACT）：現有派系清單、階級、派系間敵友關係——陣營衝突與角色歸屬的依據
- 書籍／文本（BOOK）：典籍、信件、日記等現成的世界觀素材，生成系統可引用或延伸
- 種族（RACE）：可用種族及其特性——角色設計的基礎
- 關鍵字（KYWD）：分類標籤系統，物品/NPC/技能到處都在用，生成時需要正確引用
- 天氣／氣候（WTHR/CLMT）：場景氛圍與環境設定
- 原則上涵蓋所有記錄類型；catalog 指令接受任意 ESP，不限原版

現有的診斷指令（`npcdiag`、`dump`、`find` 等）已能拉這些欄位，批次化即可產出。

---

## 10. 翻譯 + 插件合併

- **翻譯工作流**：ModForge 已有 `extract` / `apply` / `applyloc`（含 UTF-8 `_chinese.STRINGS` 本地化輸出）；想對喜歡的英文模組做中文化時直接用
- **ESP/ESL 合併**：把多個小插件合併成一個，釋放載入順序空位——對「大量生成劇情」尤其重要（見 9 節的載入順序上限）。合併要處理 FormID 重映射 + 所有引用（含腳本屬性、SEQ）的同步改寫，是個不小的工程，但 Mutagen 有對應的基礎能力

---

## 11. 騎馬與砍殺 in Skyrim（機制復刻）

要的是 **Mount & Blade 的玩法機制**，不是它的世界或素材——募兵、帶兵、會戰、攻城、封地經營、逐步征服，整套 M&B 循環用上古卷軸引擎跑起來。再加上**三國志的精隨**：城池換領主、勢力興衰變遷、武將（被俘/招降/倒戈）——不只是打仗，而是一個活的戰略格局。

**舞台：架空的自訂 worldspace**（2026-06-04 決定），不一定要在天際省。理由：
- 動天際省城市歸屬會跟所有 vanilla quest / 城市 mod 打架，lore 也綁手綁腳；架空世界全部歸零
- **自訂 worldspace ≠ 自訂美術**——擺的全是 vanilla 資產（白漫建築 kit、農舍、城牆 statics），零新資產
- 地圖可以為戰略玩法設計：城市間距、隘口要塞、糧倉村莊按行軍/遭遇節奏排；攻城戰的城也能設計成「適合打的」
- ModForge 已能生 worldspace + 平坦地形 + navmesh，直接踩在現有能力上；缺的是非平坦地形（短期：平原 + statics 堆地貌）和聚落級 placed-ref 量產

### M&B 的核心循環拆解

- **募兵與部隊管理**：招募村民/傭兵 → 訓練升級兵種樹 → 部隊跟著你走
- **野戰**：兩軍對衝的大規模戰鬥，玩家既是指揮官也是參戰者（衝鋒、下令）
- **攻城戰**：攻防城鎮要塞
- **戰略層**：大地圖移動、領主外交、封地經濟、王國征服
- **騎戰**：馬上揮砍/騎射（Skyrim 1.6+ 原生支援騎乘戰鬥，這塊有底子）

### 城池換領主——引擎有現成先例

- **vanilla 內戰系統就是「城市換勢力」的完整實作**（白漫陷落後衛兵/旗幟/領主/crime faction 全換），模式可一般化：每座城 × 每個勢力 = 一組 Enable Parent Marker（旗幟 + 衛兵 spawn + 領主宮廷），換主時舊組 `Disable()`、新組 `Enable()`
- vanilla 只硬編 2 勢力；N 勢力 = marker 組數 ×N——「8 城 × 5 勢力 × 每組 30 refs」的組合爆炸正是 ModForge spec 量產的甜蜜點
- **動態外交原生支援**：`Faction.SetEnemy()` / `SetAlly()` 是 vanilla Papyrus 函數，執行期改敵友——同盟破裂、倒戈不用 SKSE

### 戰略模擬層（三國志的部分）

拆成「模擬」與「演出」兩層：

- **模擬層**：常駐 quest script，`RegisterForSingleUpdateGameTime(24)` 每遊戲日 tick——勢力 AI 決策（攻/守/徵兵）、**玩家不在場的戰役數值結算**（兵力×質量×城防 → 勝負，即三國志 autocalc，引擎不渲染）、武將資料（忠誠/能力/被俘/招降）純資料層存放
- **演出層**：玩家在場的戰役才實打（受 actor 上限約束，波次增援）；換主 = marker 翻轉；「XX 城被攻陷」的傳聞/信使/告示由想法 9 的生成管線餵
- **武將是靈魂**：每個 lord 是真 NPC record（ModForge 生成），戰略狀態（位置/兵力/忠誠）活在模擬層；招降 = 改 faction + AI package + 解鎖對話分支，全是現有機制

風險點：(1) 守軍必須 spawn-on-demand（LvlN），絕不能人人 persistent ref——存檔膨脹鐵律；(2) 每日 tick 的 Papyrus 算力夠用，但每小時級精細模擬要考慮 SKSE native；(3) 攻城戰仍是最難的演出，架空世界「把城設計成適合打的」是相對天際省的優勢

### 引擎的硬限制（決定設計上限)

- **同屏 Actor 數量**：Skyrim 引擎超過 ~30-50 個活躍戰鬥 AI 就明顯掉幀、AI 品質崩壞——M&B 的「百人會戰」不能硬做，要靠分波增援、戰場區隔、或士兵以小隊為單位抽象化
- **先行者可參考**：Open Civil War、Immersive Patrols 等大規模戰鬥模組已經踩過這些坑，先研究它們怎麼處理規模
- **戰略層**：Skyrim 沒有「大地圖」概念——可以用世界地圖 + 快速旅行事件化，或乾脆用選單/書本 UI 抽象處理外交與封地

### 與其他想法的交集

- 部隊跟隨 = 想法 1 的多隨從管理放大版（Alias 池 + AI Package）
- 商隊護衛/劫掠 = 想法 3 的商隊生活直接共用系統
- 募兵對話/領主外交 = 想法 9 的對話生成管線可以餵內容
- 大規模事件調度 = Story Manager（9 節）正好是戰略層事件的天然載體
- 架空 worldspace = 想法 4 / 8 的直接應用場景；聚落量產會倒逼 ModForge 的 worldspace 能力（地形、批量 placed refs）

### 技術難題盤點（按致命度）

**致命級（設計必須繞著走）**：
1. **戰鬥 AI 上限**：超過 ~20 個 actor 同時戰鬥，AI 決策品質劣化（發呆、不揮刀）；Havok/ragdoll 隨人數暴漲；大量 `OnDeath` handler 會塞爆 Papyrus VM（每幀 ~1.2ms 預算）→ **會戰必須是 20v20 波次制**（陣亡補位 + 後台增援池計數），這是設計前提不是優化選項
2. **攻城戰尋路**：navmesh 靜態、攻城梯路徑做不出來、大量 AI 擠隘口會卡死 → 城在設計期就預埋突破口 + 預鋪攻城動線 navmesh，攻城戰 = 預設動線上的波次戰（M&B 本質上也是這樣做的）

**困難級（有路但都是硬仗）**：
3. **非平坦地形 + LOD**：heightmap 程式生成不難（難在跨 cell 接縫/normals）；LTEX 地表貼圖層；**LOD 是真硬點**——沒有 terrain LOD 遠景直接虛空，務實解是 shell out 給 xLODGen 而非自造；短期折衷 = 小世界 + 霧遮遠景
4. **聚落 navmesh**：建築 footprint → 網格法挖洞 → 三角化（簡化版 recast），純演算法工作量
5. **戰略層 UI**：Scaleform/SWF 痛苦（想法 6）、CEF 網頁 UI 是最好試驗場（想法 7，回合制不要求即時）、原型期用 message box + 書本保底
6. **Papyrus 資料天花板**：陣列上限 128 元素 → JContainers 必須；模擬規模大了終點是 SKSE native plugin

**工程量級**：部隊跟隨（照抄 EFF/NFF 的 catch-up teleport / 門口排隊方案）；NPC 騎乘戰鬥 AI 很爛（騎兵衝鋒大概率做成腳本化位移+撞擊判定的假騎兵）；聚落量產（量大但都是 ModForge 已有/近似已有能力）

### 已拍板的決策（2026-06-04）

- **A. 玩家定位：混合**——M&B 式傭兵起步，後期解鎖三國志式君主玩法；最小可玩版先做 M&B 前段（募兵+野戰），君主層等模擬層成熟後補
- **B. 時間與行軍：即時派**——行軍 = 真的帶兵走，敵軍是世界內真實移動的部隊（AI package 巡邏）；「野外撞見敵軍」是 M&B 靈魂體驗，不事件化
- **C. 依賴基線**：SKSE + SkyUI + JContainers + po3 Papyrus Extender / Tweaks + Fuz Ro D'oh + **Nemesis**（behavior 引擎，動畫類 mod 的事實標準）+ **Community Shaders**（含 Light Limit Fix 等 feature 模組；2026-06-04 補）——**這些視為所有玩家都裝的標配**（此基線適用所有想法，不只本節）；自寫 SKSE plugin 留作後期選項，前期把模擬邏輯隔離好方便日後搬遷
  - **ModForge 也應該配合這個基線**：(1) Papyrus 編譯管線要認得第三方腳本源（`PO3_SKSEFunctions.psc`、JContainers 的 `JValue/JMap/JArray.psc`、SKSE 基礎腳本、SkyUI 的 `SKI_*.psc`）——加 import path 設定讓生成的腳本能直接呼叫這些函數；(2) spec 可考慮支援 **MCM 設定選單**的鷹架生成（quest + 繼承 `SKI_ConfigBase` 的腳本，SkyUI 標配功能）；(3) 文件/for_agent 註明哪些函數庫可假設存在；(4) **Nemesis**：自訂動畫內容的接點——`package` 打包要能輸出 Nemesis 認得的動畫/behavior patch 目錄結構；(5) **Community Shaders**：美術方向可假設 Light Limit Fix（室內擺燈不再受每 mesh 4 盞限制，直接放大 §12 的操作空間），CS 的卡通渲染類 feature 若成熟也是 §13 二次元路線的渲染端解
- **D. 世界規模：先小後大**——~8×8 cells、3-5 座城起步，霧遮遠景躲 LOD；「世界是 spec 生成的」保證日後重生成大世界不是重做
- **E. 勢力/武將規模**：資料模型按「N 勢力 M 武將」參數化設計，原型 3×5、成品 5-8 勢力 × 30+ 武將，不用現在拍死
- **F. 兵種樹：架空設計**——各勢力自己的兵種樹，全部用 vanilla 裝備模型拼（ModForge NPC + LvlN 現有能力）
- **G. 第一個垂直切片：波次會戰原型**——一塊平地 + 兩隊 spawn + 波次增援腳本，驗證難題 1 的手感；它是整個企劃的試金石（手感不行其他都白搭），且對 ModForge 需求最小（平地 worldspace 已會生）

### 待深挖的三個方向

- (a) 戰略層資料模型：城/武將/勢力狀態怎麼存（JContainers）、AI 決策規則
- (b) 聚落量產：從「一座城的 spec」生出 placed refs + N 勢力 marker 組
- (c) 玩家循環：募兵 → 帶兵 → 受封 → 自立的具體機制接點

## 12. 明亮美術基調 / 光照管線（2026-06-04）

Skyrim 本身的光照氛圍太陰暗——偏好原神、薩爾達那種明亮的感覺。那種感覺 vanilla 只有白天 worldspace 才有；地下城、洞窟一律陰暗，偏偏玩家大部分時間在裡面。

**核心認知：暗是美術方向，不是引擎限制**——光照幾乎全是記錄層的事，正是 ModForge 主場：

- **室外**：Weather 記錄內建完整調色盤（日光/環境光/霧/天空色 × 黎明/白天/黃昏/夜晚），vanilla 故意調灰冷低飽和；每個 weather 還掛 **IMGS（ImageSpace）**——HDR 眼適應、bloom、cinematic 飽和/亮度/對比，「亮、乾淨、高飽和」很大部分是 IMGS 參數
- **室內**：CELL Lighting 欄位（環境光/方向光/霧色 + DALC 六方向環境光）、**LGTM（Lighting Template）**（地城 90% 用 DefaultDungeon 系暗模板）、擺放 LIGH 光源刻意稀疏——全都是「選擇」。Zelda 神廟也是封閉空間但它亮
- 引擎真限制：沒有 GI（環境光拉太高會平/塑膠感，正解是高一點環境光打底 + 少量光源做層次）；每 mesh 最多 4 盞光——**Community Shaders + Light Limit Fix 已入依賴基線（§11 決策 C），此限制可視為解除**；卡通渲染做不到（要 shader 級工作，CS feature 是可能出路）；最後一哩是玩家側 ENB/CS preset，但**記錄層能把底子打到八成**

**與 §11 天作之合**：架空世界的 climate/weather/室內模板全部自己定，明亮基調從 spec 一路貫穿。

**ModForge 缺口（純記錄工作，難度低）**：
1. CELL 光照欄位直接進 spec（ambient/directional/fog 色 + DALC）——現在室內只能 `template` 整包抄 vanilla cell
2. LGTM 生成——自製一套明亮模板（BrightShrine、BrightCave…）全 mod 共用
3. IMGS 生成——自訂 imagespace 掛 weather 和 cell，控制飽和/亮度/bloom
4. 補 `lgtmdiag` / `imgsdiag` 診斷命令

## 13. 通用 NPC 美化：morph 空間轉換規則（2026-06-04）

**核心想法：不是「換成哪種美術」，而是一個轉換規則（morph 空間 → morph 空間的函數）**——讀每個 NPC 原版的滑條數值，按規則轉換成另一個模型/骨架系統的滑條數值。原版滑條編碼了 NPC 的個性，轉換把個性帶進新美術——全 load order（含 mod 新增 NPC）自動套用，且「這個 NPC 在新美術下還認得出是她」。二次元（原神型動漫模型）只是其中一個資產包；同一管線換寫實高模頭、COtR 頭都通。

**為什麼現有美化做不到**：替換包是**硬覆蓋每個 NPC 的記錄** + 按 FormID 預烘焙 FaceGen 檔（`FaceGenData\FaceGeom\<plugin>\00XXXXXX.nif`）——mod 新增 NPC 不在它的 FormID 清單裡就漏網；記錄被改但 FaceGen 沒配對 → 黑臉 bug（「美化了卻怪怪的」多半是這個）。身體沒這問題是因為 CBBE/UNP 是 race 級替換；病根在「臉是 per-NPC 烘焙」。

**原版滑條存在哪（轉換的輸入端）**：NPC_ 記錄的 Face Morph（19 個 float：鼻長/顎寬/顴骨…）+ Face Parts 離散預設（鼻型 N 號）+ Head Parts（HDPT）+ tint layers。注意：CK 手雕過的 NPC 個性有一部分只在烘焙 nif 頂點裡、滑條讀不回來——轉換只能近似，但目標是風格化美術，近似可接受。

**轉換規則本身就是 spec**：兩邊都是 blendshape 係數空間，規則可做成宣告式對照表（`NoseLength → target.nose_long × 0.7`；離散 Face Parts 查表展開成 morph 組合）。每個目標模型系統手寫一份規則（一次性工作），之後全 NPC 自動轉。規則表是可審閱 JSON、patcher 是確定性變換、輸出可 diff——完全是 ModForge 的形狀，跟翻譯支柱同構（讀任意插件 → 確定性變換 → 輸出 patch）。

**身體側此模式已被驗證**：OBody/AutoBody = 按規則給每個 NPC 套 BodySlide 滑條、SKEE 執行期應用（vanilla 單根 weight 滑條展開成多滑條）。臉側沒有對應物（SynthEBD 只到貼圖/資產分配層級）——**「OBody 的臉版 + 跨模型系統滑條轉換」是空白**。

**執行落點兩條路**：
- **執行期路線（可能更順）**：patch 記錄換 head parts 指向目標頭模，轉換後的 per-NPC morph 值由 SKEE/RaceMenu 執行期套——完全繞開 FaceGen 烘焙、不碰 nif。Project Proteus 走過執行期換外觀這條路（相容性/穩定性是難點）
- **離線烘焙路線**：套 blendshape 權重算頂點是純數學，理論上可不靠 CK 直接寫 nif——但屬資產層、超出 Mutagen 範圍；或 shell out 給 CK 命令列 `-ExportFaceGenData`（同 xLODGen 態度：不自造）

**二次元終局的真實成本不在臉**：動漫頭通常整顆 mesh + 少量 morph，反而繞開 FaceGen 烘焙；貴的是 **vanilla 裝備不貼合動漫身形（全裝備 refit）** 和比例差異的動畫適配。務實順序：先用寫實美化資產驗證轉換管線，二次元化 = 同管線換資產包 + 裝備 refit 的後續

## 14. 資產格式轉換管線（glTF/FBX → NIF）（2026-06-04）

主流 3D 格式和 Skyrim 格式的全自動轉換可行性——**「網格」可以，「全套」不行**，卡點很集中：

| 內容 | Skyrim 格式 | 自動化可行性 |
|---|---|---|
| 網格/材質 | `.nif`（SSE BSTriShape 變體） | 高（PyNifly / ck-cmd 已解） |
| 貼圖 | `.dds`（BC 壓縮 + mipmaps） | 完全自動（純轉碼） |
| 表情/morph | `.tri` | 高（兩邊都是頂點 delta） |
| 動畫/骨架/物理 | `.hkx`（Havok 二進位） | **這就是那道牆** |

- **靜態物件最接近全自動**：要補 (1) 碰撞——NIF 的 `bhk*` 塊也是 Havok 資料，但簡單形狀（凸包/box）可程式生成；(2) 材質映射有損——glTF PBR 語義對不上 `BSLightingShaderProperty`，要一份映射規則（寫一次、批次套用）
- **蒙皮網格半自動**：綁 Skyrim 骨架（`NPC Spine [Spn1]` 命名體系）、每頂點 ≤4 骨權重、裝備切 `BSDismemberSkinInstance` 分區——「來源骨架 → Skyrim 骨架」的 retarget 映射每個來源體系寫一次，和 §13 滑條轉換表同一哲學：**一次性規則 + 批次套用**
- **動畫是真正的牆**：Havok SDK 不公開，社群靠 ck-cmd/hkxcmd 包舊版 SDK 做 FBX→hkx（能用但版本敏感）；behavior graph 完全沒有自動轉換可能（Nemesis/Pandora 領域）→ 帶全套自訂動畫的模型目前做不到無人值守轉換
- **對其他想法的意義**：§13 二次元路線（VRoid/MMD 模型 = 主流格式出身，頭/身網格轉 NIF 可管線化，卡在動畫——與「真實成本在裝備 refit 和動畫」吻合）；想法 5 資源移植（靜態場景物件是甜蜜點，一條批次管線灌整包場景資產）。⚠️ 法律面：其他遊戲資產轉了不能發布，只適用自製/CC 授權資產
- **ModForge 視角**：這是**資產層管線**，與記錄層（Mutagen）平行的另一條軸；`package` 已會打包 Meshes/Textures，上游接轉換步驟是自然延伸；PyNifly 可腳本化，shell-out 候選（同 xLODGen 態度：不自造）

---

*最後更新：2026-06-04*
