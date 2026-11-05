# scene-capture-bridge backlog — 仍未做（2026-07-14 兩個新方向）

← [backlog](../backlog.md)（入口）

## 仍未做

### 🆕 使用者 2026-07-14 提的兩個新方向（**先做 ②**；① 已於 2026-08-20 解凍，見上面 C，[three-lines-bc](three-lines-bc.md)）

- **① `sc ed <xx>` —— 編輯既有物件的「狀態屬性」**（火把燃燒/熄滅、門開/關…）。現在 `sc ed` 只能動 **transform**（位移/旋轉/縮放）；使用者要的是同一個編輯模式裡改**別的 REFR 屬性**，指令形狀比照 `sc ed ax`（`sc ed` ＋ 一個兩字母子模式）。
  - **形狀已清楚**：一本新 registry（同 Eraser/Overrides/Referrer 的 co-save 模式）記「哪個 ref 的哪個屬性被改成什麼」 → 匯出成 `overrides[]` 的**新欄位** → ModForge 端寫進 REFR。UI 走既有 bound-field 面板。
  - **難的不是架構、是每個屬性的引擎真相**，得逐個解碼：門 open/locked（REFR record flag vs lock data）、**火把亮/滅（多半不是一個 flag——vanilla 牆上火把常是「另一顆 base」或帶 light child，要先 decode 才知道能不能 runtime toggle）**、initially-disabled、ownership、enable-parent、linked-ref、count。
  - **排序**：使用者拍板**先做 ②**，本條之後再開工（動工前先挑「第一批支援哪幾個屬性」）。

- **② 物件目錄瀏覽器 ＋ 預覽 —— ✅ 主體已做（2026-07-14，DLL `ba3e2089`，已部署待實機，見 [phases](../phases.md)）**。「借用物品欄 UI」**已否決**（只吃可攜帶 form type，山脈/樹/家具進不了 inventory ⇒ 最需要的那類正好不支援）；改成 **Browser 面板頁 ＋ 世界內 ghost 預覽**。剩下的**加分項**：
  - **面板內真 3D 預覽（spike）**：`RE::Inventory3DManager`（物品欄那顆會轉的 3D 模型）介面是 `LoadInventoryItem(TESBoundObject*, ExtraDataList*)`——**STAT 也是 TESBoundObject**，所以預覽技術也許能脫離物品欄單獨用。不確定（綁 item-3D render layer），**ghost 已經夠用，這是錦上添花**。**→ 2026-08-20 升格為上面 A1**（[three-lines-a](three-lines-a.md)）（使用者第二次提出同一個訴求，這顆 spike 就是它的核心）。
  - **離線 catalog json：✅ ModForge 產生端已完成（2026-08-12），DLL 消費端待主力機**：runtime **拿不到 EditorID**（SSE 不留）；ModForge `catalog build` 現保存明示 load order、EDID/FULL/model path/來源，`catalog export-json` 依 FormKey 只吐 winner，v1 schema + atomic write 已離線測完。剩 bridge DLL 讀檔、以 durable id 合併到 runtime catalog search，需另題實作並實機驗收。
  - **清單改用 ImGui ListClipper**：現在是「上限 500 筆＋明講截斷」（不想賭 wrapper 的 `ImGuiListClipper` struct layout）。clipper 能一路捲完三萬筆——實機證明頁面沒問題後再換。
  - **離線批次縮圖**：`nifexport`（Godot 編輯器那套）→ PNG → 面板 `LoadTexture` ⇒ CK 對等的 2D 縮圖牆。**幾萬張太重**，只有在 ghost 不夠用時、且只對特定類別批次生。

- **ModForge 端要不要過濾玩家的「管線 perk」（2026-07-13，接在拍板 (b) 之後）**：橋端已改成**全收**（base 12 顆 ＋ addedPerks，去重取高 rank，DLL `e19ad4ca`）——依使用者拍板「完全複製優先，到時候讓 modforge 處理」。所以**取捨在消費端**：`AllowShoutingPerk`／`VampireFeed`／`AlchemySkillBoosts`／`DBWellFitted` 這些是 vanilla **Player 記錄專用**的管線 perk，鑄到一個 NPC 分身身上多半是死資料（但 `AllowShoutingPerk` 之類若要讓分身用吼聲就需要）。**候選**：(i) 照抄不動（現況）；(ii) build 時印一行 INFO 點名這幾顆；(iii) spec 給個 opt-out。**還沒做，等有實際困擾再動。**

- **`sc cap` 物件類 vs `sc pk` 分工（使用者再想，先照舊）**：`sc cap` 記 NPC/player 含全身物品＋extra data（v7 已落地）；物件類 capture 與 `sc pk` 滴管感覺功能重複，使用者還要想想——**傾向仍記錄**，暫不動。
- **📌 導航網格（navmesh）——「超重要，之後得開始考慮」（使用者 2026-07-11 晚）**：編輯器流程目前完全沒碰 navmesh——擺出的建築/障礙物會擋住 vanilla navmesh 但 NPC 照原網格走（穿模/卡住），marker 生的 NPC 若落在無網格處也不會動。ModForge 已有程式化 navmesh 能力可接（custom worldspace NAVM＋NAVI additive override Skyrim.esm:0x12FB4 in-game 驗過，見 idea/asset-pipelines/map-scene/geometry.md 一帶＋Vigilant.esm 解碼參考）；難點在**編輯 vanilla cell**：要 override 既有 NAVM（cut/finalize 語意）而不只是新建。方向未定（DLL 端記錄擺放物 footprint → ModForge 端裁切？或先只處理「新增小平台補網格」？），需要時開獨立 plan。
  - **✅ 已開獨立 plan（2026-07-12）：[plans/navmesh.md](../../navmesh.md)——兩個結論同日皆已 🎮 實機 PASS，不再只是離線推論**：① **「擺的東西擋住 NPC」根本不必改 navmesh，已結案**：用 vanilla 的 **L_NAVCUT 碰撞體積**（`CollisionMarker` 0x000021 ＋ `CollisionLayer=49` ＋ Primitive box，**HearthFires 蓋房子用了 1220 筆**）就能 runtime 裁切，純 Mutagen 一筆 REFR——白漫大街 TEST/CONTROL 對照實驗實機證明有效（TEST 繞開、CONTROL 直穿），`autoNavCuts` 已預設開（⚠️ 光加 Obstacle flag 無效——L_STATIC 不是 NavmeshObstacle 層）。② **「NPC 走上新平台」非寫 NAVM 不可，而 override vanilla NAVM 的地基已實機驗證可行**（no-op override 裝上後白漫 NPC 一切正常；離線 byte-diff 早已 IDENTICAL；USSEP 807 筆真的這麼幹；NAVI 是加法式 merge 不是地雷）；鐵律＝**永不重新編號 triangle**（鄰居的 EdgeLink 存的是你的 triangle index）。**現況（2026-08-20 更新）：P1/T2.0/P0/P3 全部做完且 🎮 過關**（P3 內裝 edge-to-edge 2026-08-11 實機 PASS；原訂的 P2 NAVM-cut 備案因 T2.0 PASS 而整段作廢）——**唯一還沒動的就是 P4 遊戲內採集，已開成上面的 B**（[three-lines-bc](three-lines-bc.md)）。
- **外部 mod 依賴**：(a) 可見性＋(b) 宣告式 `requires:` 已完成（見 [phases](../phases.md)）。(d) **直接 master 依賴檢查已於 2026-09-14 完成**：`check-dependencies <plugin> --plugins <plugins.txt> [--implicit-plugins <implicit.txt>]` 只讀 TES4 header，以明示清單區分啟用／停用／缺項；不猜官方主檔，不驗磁碟存在、版本或遞迴依賴。專項 11 PASS（含真 CLI exit 0/1/2），完整離線 1270 PASS／1 SKIP；[用法](../../../../docs/for_agent_cli.md#檢查成品的直接-master-依賴)。(c) **modlist／load order 快照仍未做**，要保存「當時在哪個 load order 上吸的」仍需家用 MO2 檔案；沒有把快照或完整安裝閉環算成此次完成。
- **紅/綠半透明輪廓高亮**（使用者第二輪：`sc del dp1` 被刪物件紅框、`sc pl dp1` 新增物件綠框，顏色/透明度 Settings 可調）——**較難、非必做**（需 render/shader 或 highlight 效果）。
- marker 編輯視窗下拉：寶石種類 ＋ 發光開關（需 SceneCaptureTools.esp 多個 ACTI 變體或動態換 model，較大工程）。
