# scene-capture-bridge — 之後再做（backlog）

← [README](README.md)（現況導航）｜[phases](phases.md)（P1–P6 落地）｜[appendix](appendix.md)（細摳原文＋驗證清單）

**活躍成長區**：新想法都記這。做完就從「仍未做」移到「已做」並標日期/DLL crc。

---

## ✅ 已做（P7 backlog，2026-07-11，DLL `79e611e8`→`a46ed0b2`，待實機）
- `sc del/pk/ed er0/er1`：該模式動作鍵準星↔物理射線切換（`Modes::UseRay` per-mode，co-save SETT v3）。取代「numpad * 專用鍵才能射線」的需求。
- **`sc ed ax`（純旋轉子模式，使用者第二輪定案取代 ax0/1/2）**：ON 時 numpad 4/6＝yaw、1/3＝pitch、7/9＝roll、8/2＝角度歸零；OFF 時照舊位移。`Editor::g_rotateMode`，co-save。
- `sc delc`：擦除 `RE::Console::GetSelectedRef()` 選中的 ref，走 `Eraser::MarkConsoleRef`；actor 拒絕（先只做物件）。
- 編輯指向靈魂石 marker → numpad 0 commit 更新該 marker 登記簿座標（`Markers::SetTransform`），不進 overrides；orphan proxy 就地 adopt。
- palette「load from file」鈕（append）＋**「save to file」鈕**（`Palette::LoadFromFile`/`SaveToFile`，讀寫 SKSE 夾下具名檔）。
- **Export「Export all (loaded cells)」鈕**：`SceneExporter::ExportAll` 走訪全部已載入 cell 收 placements＋registries 一次（registries 本就全域；未載入 cell 的 placements 撈不到，log 說明）。重構出 `AppendPlacements`/`AppendRegistries`/`RecordStats`。
- Settings 頁顯示 aim source／旋轉子模式現況（console 設定的可視化）。
- **numpad 5 改 per-mode**（使用者第三輪）：純旋轉模式下 5＝角度歸零（同 8/2），移動模式下 5＝復原編輯前——不再兩模式共用。
- **marker 記錄完整朝向＋大小**（使用者第三輪）：Entry `angleZDeg`→`angleDeg{x,y,z}`＋`scale`；匯出 `annotations[]` 帶 `rotation`＋`scale`（ModForge `AnnotationSpec.Rotation/Scale`，869 測綠）；co-save MKRS v2（舊 v1 只有 angleZ→補 0）。**marker 模型改鐵匕首**（`Weapons\Iron\IronDagger.nif`，劍尖視覺化朝向；tools-spec.json 改 model 重建 esp，houseCARL 驗 WEAP 01397E）。

## 仍未做
- **📌 pointer/referrer 原語——標示「既有物件」而非座標（使用者 2026-07-11 晚）**：目前只有 marker（標**空的座標**：「這裡放東西」）。新增一個 pointer：指向一個**已存在的 ref**（vanilla/他 mod 擺的椅子、桌子、門…）→ `sc` 記下它的**耐久 FormID**＋自由標籤（例：指椅子→標「sofia的椅子」）→ export → AI 配合 ModForge 寫該物件相關行為（例：給 Sofia 的 AI package 一個 Sit/Sandbox 錨點，讓她常坐這張椅子）。與現有原語的分工：marker＝新建 proxy 記座標；pointer＝**不新建**、只記既有 ref 身份供下游引用（≠ `sc pk` 滴管——pk 吸的是 base 定義用來**複製擺放**，pointer 記的是**特定 instance 的身份**用來被引用）。
  - **技術基礎已有**：記既有 authored ref 的耐久 id ＝ Eraser（removals）/Overrides 那套（`Eraser::MarkConsoleRef` 的解析路），pointer 只是「記 id＋label、不改它、不 disable」。
  - **🔴 核心坑＝persistent vs temporary ref**：AI package 的「sit at / sandbox at 指定 ref」需要目標 ref 能被 quest alias 以 specific-reference 填充，這通常要求該 ref 是 **persistent**；vanilla 場景物件多為 non-persistent → 可能無法直接當 alias 目標。消費端拍板方向（動工時定）：(a) ref 本身 persistent → spec 直接 specific-reference alias；(b) 非 persistent → 退而記其**座標＋base**，ModForge 端在該點放一支自己的 persistent furniture/idle marker 當錨點（等於 pointer 退化成「帶 base 提示的 marker」）。DLL 端兩者都先照記（ref id＋座標＋base＋label），把選擇權留給 ModForge。
  - **export 形狀（待拍板）**：新頂層段 `references[]`，每筆 `{ref, label, base?, position?, cell/worldspace}`；ModForge 端**不生成**該 ref（已存在），當可引用錨點消費。
  - **指令（使用者 2026-07-11 晚定名 referrer）**：`sc ref`＝進 referrer 模式（動作鍵記準星/射線指的既有 ref）；`sc ref XXX`＝記下當前指的 ref 並直接打標籤 XXX；`sc refc [XXX]`＝console 選取版（aim-free，同 delc/capc/pkc）＋選用標號。⚠️ 標號 XXX 用未 `Lower()` 的 raw 參數（保留大小寫，同 pkc/label 坑）。
  - **面板頁（使用者 2026-07-11 晚）**：新增 `References` 頁——列出已記的 referrer（label 就地改名、顯示 ref id/base/所在 cell、逐列刪除；比照 Markers/Palette 頁最新在前）。
- **Export 檔名帶場景＋日期（使用者 2026-07-11 晚）**：目前 ExportCell / ExportAll 都固定寫 `scene-export.json`（SceneExporter.cpp L478/L491）→ 連續 export 互相覆蓋。改成 `scene-export_<cell名或所在>_<YYYYMMDD-HHMM>.json`（interior 用 cell EditorID；exterior 用 worldspace＋grid；名稱要 sanitize 成檔名安全字元）。
- **Captures 獨立 Export 鈕（使用者 2026-07-11 晚）**：`sc cap` 記下的東西（capturedItems/capturedNpcs）要有**自己的 export 按鈕、輸出獨立檔案**（如 `captures_<日期時間>.json`），和 cell 場景 export 分開——現在混在同一份 scene-export.json 裡。
- **📌 Scope 反轉拍板（使用者 2026-07-11 晚，推翻先前指示）**：cell export **不再涵蓋我們新增的 NPC 等**——太麻煩；cell export ＝純場景/物件 placements ＋ marker（annotations），**NPC 這類交給 ModForge 按 marker 去擺**。實作＝ExportCell 掃描時排除 actor refs（現在 actor 會以 `kind:"npc"` 進 placements[]，SceneExporter.cpp L201-208）。⚠️ 動工時同步更新 [spec](../../specs/ingame-scene-export-design.md) 契約節。
- **`sc cap` 物件類 vs `sc pk` 分工（使用者再想，先照舊）**：`sc cap` 記 NPC/player 含全身物品＋extra data（v7 已落地）；物件類 capture 與 `sc pk` 滴管感覺功能重複，使用者還要想想——**傾向仍記錄**，暫不動。
- **`sc pk ed0/ed1`＋`sc pl ed0/ed1`（使用者 2026-07-11 晚）**：滴管/擺放的 extra-data 開關。現況＝`sc pk` 只吸 durable base、不吸實例附魔（Palette.cpp 只取 GetBaseObject）。`ed1` ＝吸取時連 ExtraEnchantment 等 extra data 一起記（palette 條目要能帶實例資料，擺放/匯出時走 capturedItems 式鑄造＋引用）；`sc pl ed1` ＝擺放時帶上 extra data。per-mode 設定、進 co-save SETT（同 er0/er1 模式）。
- **`sc pl py0/py1`（使用者 2026-07-11 晚）**：擺放模式的物理開關。`py1`（**預設**）＝擺出的物件保留完整物理；`py0` ＝擺出的物件關閉物理性質——主要目的＝避免擺好的東西被 Skyrim 神奇物理引擎弄到亂飛。動態物件為主，靜態物件是否也要（clutter 類其實都是 havok 物件）實作時一併看。per-mode 設定、進 co-save SETT（同 er0/er1）。⚠️ 實作要分兩層：(a) DLL 擺放當下的即時凍結（已有 P3 物理凍結機制可複用），(b) **持久到 esp**——placement 要把這個狀態帶進 export，ModForge 端 REFR 用哪個機制（Don't Havok Settle 記錄旗標 vs script SetMotionType keyframed）動工時查證拍板，`PlacementSpec` 可能加欄位。先只考慮物理性質，不擴及其他屬性。
- **`sc ed py0/py1`（使用者 2026-07-11 晚）**：編輯模式（含 `ax` 旋轉子模式）下被控制物件的物理開關。**預設 `py0` ＝控制期間停止物理**（＝現行 P3 物理凍結行為，細摳③「選中時喪失物理」），`sc ed py1` 切回控制期間保留物理。也就是把既有凍結行為做成可切換設定；per-mode、進 co-save SETT。
- **📌 導航網格（navmesh）——「超重要，之後得開始考慮」（使用者 2026-07-11 晚）**：編輯器流程目前完全沒碰 navmesh——擺出的建築/障礙物會擋住 vanilla navmesh 但 NPC 照原網格走（穿模/卡住），marker 生的 NPC 若落在無網格處也不會動。ModForge 已有程式化 navmesh 能力可接（custom worldspace NAVM＋NAVI additive override Skyrim.esm:0x12FB4 in-game 驗過，見 idea/asset-pipelines/map-scene/geometry.md 一帶＋Vigilant.esm 解碼參考）；難點在**編輯 vanilla cell**：要 override 既有 NAVM（cut/finalize 語意）而不只是新建。方向未定（DLL 端記錄擺放物 footprint → ModForge 端裁切？或先只處理「新增小平台補網格」？），需要時開獨立 plan。
- **F1 面板清掉冗餘動作鈕（使用者 2026-07-11 晚）**：各頁面上諸如 "place marker here" / "erase by ray" / "pick by ray"… 這類動作觸發鈕都刪掉——現在這些動作全走 `sc` console 指令＋鍵位（P5 模式制之後 UI 觸發已多餘）。面板保留設定/檢視/清單類（改名、kind、逐列 undo、步長、palette 列表…），只砍「在面板按一下就執行世界動作」那批。動工時逐頁盤點哪些是動作鈕、哪些是設定項。
- **`sc pkc [XXX]`（使用者 2026-07-11 晚）**：滴管吸取的 console-selected 版——console 點選 ref 後 `sc pkc` 吸進 palette（同 `delc`/`capc` 的 aim-free 模式）；帶選用標號 `sc pkc XXX` ＝吸取當下直接把該 palette 條目改名為 XXX（識別用）。⚠️ 標號要用未 `Lower()` 的 raw 參數（保留大小寫，同 [player-capture-capp](../player-capture-capp.md) 的 label 坑）。
- **紅/綠半透明輪廓高亮**（使用者第二輪：`sc del dp1` 被刪物件紅框、`sc pl dp1` 新增物件綠框，顏色/透明度 Settings 可調）——**較難、非必做**（需 render/shader 或 highlight 效果）。
- marker 編輯視窗下拉：寶石種類 ＋ 發光開關（需 SceneCaptureTools.esp 多個 ACTI 變體或動態換 model，較大工程）。
- rebind 重作（找出 in-game 抓錯鍵主因：可能是 rebind armed 當幀把移動鍵也吃進去；目前 Settings 隱藏、固定 F11）。
