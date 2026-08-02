# 22. 漂泊開拓慢活（移動基地 → 程序生成異域 → 開拓經營）

← index: [README.md](README.md) · [ideas 索引](../ideas.md)

**統整原本散開的 [#3](03-caravans-fleets.md)（商隊船隊）+ [#4](04-alien-worldspace.md)（異世界 worldspace）+ [#8](08-procedural-world.md)（程序生成）成一條玩法企劃**——核心爽點＝**漂泊 + 開拓 + 慢活**（明確排除 #11 的戰爭/征服那一味）。玩家帶著移動基地（商隊／船／空艇）在世界間漂，抵達一個（離線預生成的）新異域，在那裡開拓、蓋據點、經營生活、跟 NPC 養關係，逐步把荒地變成自己的聚落。

**可行性總判（2026-06-24 盤點）**：對引擎比 #11 友善太多——剛好避開 #11 最致命的兩難（波次會戰 AI 上限、攻城尋路）。這是「**單人/小規模、慢節奏、狀態驅動**」玩法，正是 Skyrim + ModForge 現有能力的甜蜜點。五根柱子（worldspace／人／任務／養成／存檔）皆已 in-game 立起，缺口幾乎全是「**寫 Papyrus 腳本 + 加 spec section**」，且每個關鍵 pattern 都已單獨 in-game 驗證過。**沒有「引擎做不到」的死路。**

**已拍板決策（2026-06-24，使用者確認）**：

- **A. 移動基地＝事件化，不真駕駛**——Skyrim 無 moving-platform 物理，「站甲板看海岸滑過」做不到（所有船 mod 都是假動會抖會掉）。務實解：基地＝一個 interior cell 當家（內部體驗滿分），「移動」＝地圖選目的地→淡出→抵達（cell + 傳送 + map marker，全已驗）。移動過程只演出、不真開。**使用者已接受此犧牲。**
- **B. 程序生成＝離線批量多套，不 runtime 無限**——Skyrim 不能 runtime 生新 worldspace record，「每次開檔世界都不同」的無限隨機做不到。務實解：ModForge Generator 離線批量生**幾十套「手氣不同」的預生成世界**（隨機種子驅動多樣性），「探索新島」＝切到某套預生成 worldspace。**使用者已接受此犧牲。**

**五柱現狀 vs 缺口**（✅現成可生成｜🔧要寫但積木已驗｜⚠️引擎硬限／體驗瓶頸）：

| 子系統 | 狀態 | 說明 |
|--------|------|------|
| ① 移動基地 | ⚠️→✅務實解 | interior cell 家 + 事件化移動（決策 A） |
| ② 抵達新異域（worldspace） | ✅骨架／⚠️「程序」 | 地形/貼圖/navmesh/天氣光照/Godot 擺物件全 in-game；多樣性靠離線批量（決策 B） |
| ③ 開拓/建設據點 | 🔧 | Hearthfire 式 enable-parent marker 翻轉（placement 欄位已落地）+ 採集→消耗→解鎖（同 in-world 技能樹 #20 的扣點 gate pattern）；缺「聚落量產 spec section」（macro-expand，類比 `skillTrees:`） |
| ④ 經營/生活循環 | 🔧 | 作物/工作台＝vanilla record；作息/收成/補貨＝常駐 quest `RegisterForSingleUpdateGameTime` 每日 tick；純腳本織，無引擎牆 |
| ⑤ NPC 關係/好感度 | ✅+🔧 | per-actor StorageUtil KV + 條件對話 gate + Scene + F5 語音 + 隨從，全已驗 |
| ⑥ 系統 UI（經營面板/選目的地） | ⚠️體驗瓶頸 | MCM 可做但醜卡；最佳解 CEF 網頁 UI（#7）未做；短期保底 message box + 書本 UI。是「順不順手」非「能不能」 |

**真正要繞的硬限只有兩條**：① 移動基地不能真動（→決策 A 事件化）；② 程序生成非 runtime 無限（→決策 B 離線預生成多套）。其餘全是工程量 + UI 體驗，**唯一未驗的未知是 ⑥ 的 CEF**，但它只影響好不好用、不影響核心玩法成立。

**第一個垂直切片建議＝③ 據點建設**——最能展示「開拓→經營」核心循環、技術積木全驗過、對 ModForge 需求最小（placement enable-parent + StorageUtil 計數 gate + 一座小聚落的 placed refs）。是整個企劃的試金石。

**與其他想法交集**：吸收 [#3](03-caravans-fleets.md)/[#4](04-alien-worldspace.md)/[#8](08-procedural-world.md)；UI 靠 #7（CEF）；建設/解鎖重用 #20（in-world 技能樹）的 macro-expand + 扣點 gate pattern；NPC/對話/Scene/語音重用既有隨從管線；worldspace 重用既有生成全鏈。

**待深挖**：(a) 聚落量產 spec section 設計（一座聚落 → placed refs，參數化／macro-expand）——**人口類 mod 調查已坐實此缺口（2026-06-24）**：Populated 系（純靜態 base+package+cell-override 擺人，無 controller）、Immersive Citizens AI Overhaul（alias-ALPS 掛 bespoke 日程包 + Flee-template 防禦/逃跑）、Immersive Wenches（XMarker+LeveledNpc 腳本生怪 + 時段 package + SM 觸發環境 scene）三者**機制全已可生成**，唯一共通缺口＝一個 `settlementPopulation:`/`wildernessPopulation:`/`spawnPoints[]` 的 macro-expansion 便利層（照 `skillTrees:` pass-0 展開模式），外加一個小 record 缺口 **`flee` PACK template**（受襲時平民逃跑/守衛迎戰，慢活聚落要有反應）。藍本見 [mod-survey findings](../../../../../analysis/mod-survey/findings/populated-skyrim-family.md)（+ immersive-citizens-ai-overhaul / immersive-wenches）。借鏡配方：日程 package 要綁實際擺放的床/攤位/工作站 ref（純抽象 sandbox 會讓 NPC 呆站）。(b) 生活循環資料模型（資源/體力/收成/好感怎麼用 StorageUtil 存、每日 tick 規則）；(c) 移動基地事件化的具體接法（家 cell + 地圖目的地選單 + 抵達演出）；(d) 程序生成多樣性的種子化生成器設計（一個 spec 模板 → N 套手氣不同的 worldspace）；(e) ⑥ UI：CEF 可行性 vs 書本保底的取捨原型；(f) **大圖分塊工作流**（Godot 分塊編輯 + 手動對齊 → stitch 合成大 PNG；GDScript 程序化擺放）——決策已定，見 [godot-editor stitching.md](../../../../godot-worldspace-editor/design/stitching.md)；缺 LCTN 可發現地點記錄生成。
