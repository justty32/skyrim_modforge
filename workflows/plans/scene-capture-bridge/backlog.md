# scene-capture-bridge — 之後再做（backlog）

← [README](README.md)（現況導航）｜[phases](phases.md)（已落地實作記錄）｜[appendix](appendix.md)（細摳原文＋驗證清單）

**活躍成長區**：新想法都記這。做完就搬進 [phases.md](phases.md)（已落地實作記錄，標日期/DLL crc），從這裡刪除。

---

## ❌ 已否決（別再做第三次）

- **遊戲內 rebind（面板抓鍵改動作鍵）——放棄，改走 `.ini`**（使用者 2026-07-12 實機後拍板：「這太麻煩了，先隱藏掉這個功能吧，我們之後把他擺進 .ini 設定」）。**為什麼不再試**：面板（SKSE Menu Framework）**不暫停遊戲**，所以「抓玩家想綁的那顆鍵」永遠在跟**玩家手上還按著的鍵**搶同一條輸入串流——人剛用滑鼠點完 `Rebind`，手多半還在 WASD 上。**兩次嘗試、兩種設計都在實機失敗**：① P5（2026-07-11）armed 後來者不拒 → 綁成 W；② 重作（`ddf6324`，2026-07-12）加了保留鍵黑名單＋按下再放開才 commit → **使用者實機回報仍失敗**。現況＝**`SceneCaptureBridge.ini`**（SKSE 資料夾、缺檔自動生成、寫鍵名不寫 scancode、面板一顆 `reload keys from ini`）——檔案沒有那條賽道可輸：沒有 armed 狀態、沒有 input sink、沒有時序。實作與完整驗屍見 [phases.md](phases.md) 該兩節；抓鍵狀態機已從 `Modes`／`plugin.cpp` 移除（要看舊碼去 git `ddf6324`）。

## 仍未做

### 🆕 使用者 2026-07-14 提的兩個新方向（**先做 ②**）

- **① `sc ed <xx>` —— 編輯既有物件的「狀態屬性」**（火把燃燒/熄滅、門開/關…）。現在 `sc ed` 只能動 **transform**（位移/旋轉/縮放）；使用者要的是同一個編輯模式裡改**別的 REFR 屬性**，指令形狀比照 `sc ed ax`（`sc ed` ＋ 一個兩字母子模式）。
  - **形狀已清楚**：一本新 registry（同 Eraser/Overrides/Referrer 的 co-save 模式）記「哪個 ref 的哪個屬性被改成什麼」 → 匯出成 `overrides[]` 的**新欄位** → ModForge 端寫進 REFR。UI 走既有 bound-field 面板。
  - **難的不是架構、是每個屬性的引擎真相**，得逐個解碼：門 open/locked（REFR record flag vs lock data）、**火把亮/滅（多半不是一個 flag——vanilla 牆上火把常是「另一顆 base」或帶 light child，要先 decode 才知道能不能 runtime toggle）**、initially-disabled、ownership、enable-parent、linked-ref、count。
  - **排序**：使用者拍板**先做 ②**，本條之後再開工（動工前先挑「第一批支援哪幾個屬性」）。

- **② 物件目錄瀏覽器 ＋ 預覽 —— ✅ 主體已做（2026-07-14，DLL `ba3e2089`，已部署待實機，見 [phases](phases.md)）**。「借用物品欄 UI」**已否決**（只吃可攜帶 form type，山脈/樹/家具進不了 inventory ⇒ 最需要的那類正好不支援）；改成 **Browser 面板頁 ＋ 世界內 ghost 預覽**。剩下的**加分項**：
  - **面板內真 3D 預覽（spike）**：`RE::Inventory3DManager`（物品欄那顆會轉的 3D 模型）介面是 `LoadInventoryItem(TESBoundObject*, ExtraDataList*)`——**STAT 也是 TESBoundObject**，所以預覽技術也許能脫離物品欄單獨用。不確定（綁 item-3D render layer），**ghost 已經夠用，這是錦上添花**。
  - **離線 catalog json（使用者拍板的「之後再補」）**：runtime **拿不到 EditorID**（SSE 不留），所以目錄只能靠 model path／name／FormID 搜。ModForge 用 Mutagen 掃 load order 產一份 catalog json（EDID＋FULL name 全齊）給 DLL 讀，就能**照 CK 的 EditorID 搜**。等真的被「沒 EDID」卡到再做。
  - **清單改用 ImGui ListClipper**：現在是「上限 500 筆＋明講截斷」（不想賭 wrapper 的 `ImGuiListClipper` struct layout）。clipper 能一路捲完三萬筆——實機證明頁面沒問題後再換。
  - **離線批次縮圖**：`nifexport`（Godot 編輯器那套）→ PNG → 面板 `LoadTexture` ⇒ CK 對等的 2D 縮圖牆。**幾萬張太重**，只有在 ghost 不夠用時、且只對特定類別批次生。

- **ModForge 端要不要過濾玩家的「管線 perk」（2026-07-13，接在拍板 (b) 之後）**：橋端已改成**全收**（base 12 顆 ＋ addedPerks，去重取高 rank，DLL `e19ad4ca`）——依使用者拍板「完全複製優先，到時候讓 modforge 處理」。所以**取捨在消費端**：`AllowShoutingPerk`／`VampireFeed`／`AlchemySkillBoosts`／`DBWellFitted` 這些是 vanilla **Player 記錄專用**的管線 perk，鑄到一個 NPC 分身身上多半是死資料（但 `AllowShoutingPerk` 之類若要讓分身用吼聲就需要）。**候選**：(i) 照抄不動（現況）；(ii) build 時印一行 INFO 點名這幾顆；(iii) spec 給個 opt-out。**還沒做，等有實際困擾再動。**

- **`sc cap` 物件類 vs `sc pk` 分工（使用者再想，先照舊）**：`sc cap` 記 NPC/player 含全身物品＋extra data（v7 已落地）；物件類 capture 與 `sc pk` 滴管感覺功能重複，使用者還要想想——**傾向仍記錄**，暫不動。
- **📌 導航網格（navmesh）——「超重要，之後得開始考慮」（使用者 2026-07-11 晚）**：編輯器流程目前完全沒碰 navmesh——擺出的建築/障礙物會擋住 vanilla navmesh 但 NPC 照原網格走（穿模/卡住），marker 生的 NPC 若落在無網格處也不會動。ModForge 已有程式化 navmesh 能力可接（custom worldspace NAVM＋NAVI additive override Skyrim.esm:0x12FB4 in-game 驗過，見 idea/asset-pipelines/map-scene/geometry.md 一帶＋Vigilant.esm 解碼參考）；難點在**編輯 vanilla cell**：要 override 既有 NAVM（cut/finalize 語意）而不只是新建。方向未定（DLL 端記錄擺放物 footprint → ModForge 端裁切？或先只處理「新增小平台補網格」？），需要時開獨立 plan。
  - **✅ 已開獨立 plan（2026-07-12）：[plans/navmesh.md](../navmesh.md)——兩個結論同日皆已 🎮 實機 PASS，不再只是離線推論**：① **「擺的東西擋住 NPC」根本不必改 navmesh，已結案**：用 vanilla 的 **L_NAVCUT 碰撞體積**（`CollisionMarker` 0x000021 ＋ `CollisionLayer=49` ＋ Primitive box，**HearthFires 蓋房子用了 1220 筆**）就能 runtime 裁切，純 Mutagen 一筆 REFR——白漫大街 TEST/CONTROL 對照實驗實機證明有效（TEST 繞開、CONTROL 直穿），`autoNavCuts` 已預設開（⚠️ 光加 Obstacle flag 無效——L_STATIC 不是 NavmeshObstacle 層）。② **「NPC 走上新平台」非寫 NAVM 不可，而 override vanilla NAVM 的地基已實機驗證可行**（no-op override 裝上後白漫 NPC 一切正常；離線 byte-diff 早已 IDENTICAL；USSEP 807 筆真的這麼幹；NAVI 是加法式 merge 不是地雷）；鐵律＝**永不重新編號 triangle**（鄰居的 EdgeLink 存的是你的 triangle index）。**現況：P1/T2.0/P0 全部做完且過關，下一步是 P3 add+link**（原訂的 P2 NAVM-cut 備案因 T2.0 PASS 而整段作廢）；P4 遊戲內採集殿後。
- **外部 mod 依賴——剩下的兩個候選**（(a) 可見性＋(b) 宣告式 `requires:` 契約已做，見 [phases](phases.md)）：**(c) modlist / load order 快照**（把當下 MO2 `plugins.txt` 存進 spec 旁，之後能重現「當時是在哪個 load order 上吸的」）；**(d)「依賴檢查」指令**（給一個 esp ＋ 一份 load order → 回報缺什麼；(b) 檢查的是 spec↔build，這個檢查的是 build↔**玩家的實際安裝**，是出貨前最後一道）。兩者都要讀 MO2/遊戲側檔案，離線機做不了完整迴圈，優先度不高。
- **紅/綠半透明輪廓高亮**（使用者第二輪：`sc del dp1` 被刪物件紅框、`sc pl dp1` 新增物件綠框，顏色/透明度 Settings 可調）——**較難、非必做**（需 render/shader 或 highlight 效果）。
- marker 編輯視窗下拉：寶石種類 ＋ 發光開關（需 SceneCaptureTools.esp 多個 ACTI 變體或動態換 model，較大工程）。
