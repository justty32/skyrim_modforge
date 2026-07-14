# scene-capture-bridge — P1–P6 Task 層級與已落地記錄

← [README](README.md)（現況導航）｜[backlog](backlog.md)（未做項）｜[appendix](appendix.md)（細摳原文＋驗證清單）

歷史實作細節，大而少動。新想法記 [backlog.md](backlog.md)。

---

# P1：統一 marker 系統 — Task 層級

**一句話**：玩家在世界裡放**具名座標 marker**（標籤自由文字），面板管理，匯出成標註段；AI agent 讀座標＋標籤做 authoring。一次解鎖：NPC/生物/leveled 擺放指令、地形指令、動態物件初版、未來 navmesh 記點。

## 資料模型

```
MarkerEntry {
    std::uint32_t seq;          // 遞增序號（navmesh 記點要有序）
    std::string   label;        // 自由文字，玩家改
    std::string   kind;         // "note"(default) | "mapMarker" | "vfx" | "tag" | "navmesh" | ...
    RE::NiPoint3  position;     // 放置當下取定（不追 proxy 的物理位置）
    float         angleZ;       // 玩家放置時的朝向（給「面向這邊」類指令）
    std::string   cellOrWs;     // 耐久 id，interior=cell / exterior=worldspace（重用 ExportCell 邏輯）
    RE::ObjectRefHandle proxy;  // 世界裡的可見代理 ref
}
```

- **kind 的語意**：`note`/`navmesh`＝建議性（不生成，給 agent 讀）；`mapMarker`/`vfx`/`tag`＝生成性（**P1 先不自動展開**——一律進標註段，由 agent 翻成 `mapMarkers[]`/`hazards[]`/`tags[]`；自動展開是之後的最佳化，避免 P1 碰生成端）。
- 座標在**放置當下取定**並存進登記簿——proxy 之後被物理推動也不影響匯出值。

## 🔴 拍板點（Task 1 的前置）：標註段的形狀

| | 去處 | 優點 | 缺點 |
|---|---|---|---|
| a | `_annotations`（底線鍵，scene.json 內） | 一個檔；已驗 `Program.Schema.cs:13` 放行 `_`/`//` 前綴 | 語意上是註解空間；deserialize 靜默忽略 |
| **b（✅ 拍板 2026-07-10）** | 一等公民 `ModSpec.Annotations` | validate 安全；build log「N annotations (advisory)」；agent 讀一個檔 | ModSpec 混入不生成的欄位，語意要寫明 |
| c | sidecar `scene-annotations.json` | ModSpec 純淨 | agent 讀兩個檔；配對靠命名慣例 |

## Task 0：前置驗證（只讀，不改碼；`ForEachReference` 簽名憑印象寫錯的前車之鑑）

- [x] **proxy 的 base**（✅ 2026-07-10）：工具 esp `SCB_MarkerACTI`（model=`Magic\SummonTargetFX.nif`，vanilla 召喚圈，路徑讀自 `07CD55:Skyrim.esm`——保證有效）；**DLL 在 esp 缺席時 fallback 到 vanilla `SummonTargetFXActivator`**，hotkey 路徑不依賴工具 esp。原候選評估：挑一個**看得見**的載體（vanilla XMarker 遊戲內不渲染）。候選：發光小物（wisp/燭火類 ACTI 或 MSTT）。模型路徑**必須用 houseCARL/`find` 對 Skyrim.esm 驗**（[[vanilla-nif-paths-must-be-verified]]：錯路徑＝隱形物件無報錯）。裁決同時定「用 vanilla base」vs「工具 esp 自帶 MarkerACTI」（後者辨識乾淨——base 來自我們的 esp，adopt 掃描零誤判；工具 esp ≠ 出貨產物，ModForge 生一次即可，dogfood）。
- [x] **放置 hotkey 的 scancode**：F9=0x43 實機確認 sink 有收到——**但 F9 是 vanilla 快速讀檔**，遊戲同時處理（sink 只觀察不吞鍵）→ 改 **F11=0x57**（DIK 表在 F10 後跳號，非連續；vanilla 未綁），另在面板加 `place marker here` 鈕（零衝突路徑）。**教訓：選 hotkey 先查 vanilla 綁定表。**
- [x] **label 持久化 trick**（✅ `TESObjectREFR::SetDisplayName(BSFixedString,bool)` 存在，`TESObjectREFR.h:460`；已接上——改名同步寫 proxy 顯示名）：`ExtraTextDisplayData`（SetDisplayName 等價）改 proxy 顯示名——顯示名**存進存檔**，若可行則 save/reload 後 adopt 掃描能**連標籤一起**復原，登記簿的跨 session 問題免費解決。驗 CommonLibSSE API 是否存在可寫路徑。
- [ ] **（Task 4 用）指向性放置**：A 案符文式（引擎原生把物件放在瞄準命中點；驗放置面限制、`iMaxAttachedRunes`）vs B 案 `bhkPickData` 射線（驗 API 形狀）。
- [x] **（Task 4 裁決，2026-07-10）指向性放置走 B 案射線**——A 案符文**不成立**：讀 vanilla `FireRune`（05DB90）→ MGEF `RuneFireFFLocation`（TargetLocation + FXPersist + Projectile 05DB91），符文機制是 **projectile 黏表面＋近接引爆**，沒有任何 placed ref 可認領。B 案已實作：`bhkPickData` + `bhkWorld::PickObject`（`GetWorldScale` 縮放、`BSReadLockGuard` 上鎖、eye+120 起點、range 4096、無命中 fallback 腳下）。**pitch 正負號未實機驗**——若 marker 落點詭異（身後/天上），翻符號即可（`Markers.cpp` `LookHit` 有註記）。
- [x]（存在，暫未使用）**（選配）`TESActivateEvent` sink**：玩家 E 鍵啟動 marker → 面板跳到該筆。沒有也不擋——面板列表夠用。

## Task 1：ModForge 側標註段（等形狀拍板；若 b 案 ≈ 20 行）

**Files:** `Spec.cs`（或新 `Spec.Annotations.cs`）、`Program.Build.cs`（log）、測試、`examples/`。

- [x] `AnnotationSpec { Label, Kind, Position, AngleZ, Cell, Worldspace, Seq }` + `ModSpec.Annotations`。
- [x] build：**不生成任何記錄**，log 一行 `N annotation(s) (advisory, not built)`。
- [x] validate：欄位型別檢查即可（`CheckUnknownFields` 自動涵蓋）。
- [x] 離線測試 + 一個 example json。**行為不變**：無 annotations 的既有 spec 生成位元不變（鐵律①）。

## Task 2：DLL marker 登記簿 + 腳下放置

**Files:** 新 `src/Markers.{h,cpp}`；`plugin.cpp`（hotkey 分支）；`SceneExporter.cpp`（整合）。

- [x] 登記簿（session 記憶體；模型同 M6 橡皮擦清單）＋ `Place()`：hotkey → **玩家腳下**放 proxy（`PlaceAtMe`，零新 API——使用者的 navmesh 願景本來就是「記錄玩家腳下位置」）＋ 記 position/angleZ/cellOrWs。
- [x] **`ExportCell` 排除 proxy**：proxy 是 dynamic ref，不排除就會被 vanilla diff 當成玩家擺的物件收進 `placements[]`——**這是正確性問題**，不是最佳化。登記簿 handle 查表跳過，另計 `markers` 統計。
- [x] 匯出：登記簿 → 標註段（依拍板形狀）。跨 cell 的 marker 也全部匯出（登記簿是全域的，同 removals 理由）。
- [x] 刪 marker：銷毀 proxy + 移出登記簿（真刪除語意，無痕）。

## Task 3：面板 Markers 頁

**Files:** `src/UI.cpp`。

- [x] 列表：seq、label（`InputText` 就地改名）、kind（下拉或自由文字）、所在 cell、刪除鈕。最新在前。
- [x] `Stats` 加 marker 計數；Export 頁顯示「N markers → annotations」。

## Task 4：指向性放置（✅ 2026-07-10 離線實作，B 案射線）

- [x] ~~A 案符文~~ **不成立**（vanilla 符文＝projectile 黏表面＋近接引爆，無 placed ref——見 Task 0 裁決記錄）。
- [x] B 案：F11 → `PlaceAimed()`——射線命中點放 marker，無命中 fallback 腳下（一鍵兩用，零新 scancode 風險）。pitch 符號待實機驗。
- [x] **`AdoptOrphans()` 順帶落地**：面板 `adopt this cell` 鈕——上一 session 的 proxy 和顯示名都活在存檔裡，掃當前 cell 認領回登記簿（label 從顯示名復原）。跨 session 復原閉環（`SetDisplayName` trick 的消費端）。
- [ ] 法術殼（美學）後補——工具 esp 加一支 self 法術當模式開關即可，不擋功能。

## Task 5：工具 esp（ModForge dogfood）

**Files:** `examples/` 或 `sub_projs/scene-capture-bridge/tools-spec.json`。

- [x] ModForge 生：MarkerACTI（驗過的可見模型）+ 放置法術（+ 之後的檢視法術、選取法術殼）。**編輯器工具 esp ≠ 出貨產物**。
- [ ] 部署：跟 DLL 一起進 `mods/SceneCaptureBridge/`。

## Task 6：端到端驗收（就是使用者的工作流）

- [ ] **實機**：放 3 個 marker（含一個 exterior）、面板改名（`camp-1`／`raise-terrain-here`／`goat`）→ Export。
- [ ] **離線**：json 的標註段座標與 `get_cell_info`/houseCARL 對照；`validate` 零問題。
- [x] **agent 工作流 demo**（✅ 2026-07-10）：使用者實機標 `goat`（Tamriel (116031, 111486, -7744)）→ 匯出 → agent 從 annotations author spec（`EncGoatDomestic` 04359C + marker 座標/朝向）→ build → dump 驗 `PlacedNpc @ (116031.1, 111485.6, -7744)`，cell (28,27) 自動歸位。**MVP 價值主張閉環。**產物 `mods/SCB Goat Demo/`，勾起來進遊戲看羊。
  - **⚠️ agent authoring 陷阱（實測撞到）**：`isNpc` 自動判定只認 in-spec base（`recordsByEd`），**外部 NPC base 必須明示 `kind: "npc"`**，否則落成 REFR（NPC base 的 REFR 不生怪）。`Generator.Build.Placements.cs:79-83`。

---

# P2–P4：概要（開工前再展開成 Task）

- **P2 刪除（✅ 核心離線落地 2026-07-10）**：`src/Eraser.{h,cpp}`——**F8** 擦準星目標（authored→`Disable()`+登記耐久 id；自己的 dynamic→真刪除無痕；marker proxy→轉交 Markers::Remove）；面板 `Eraser` 頁（undo／clear／外部 master 警告色／`scan disabled refs`→逐筆確認 adopt，明示不推導）；匯出寫 `removals[]`（全域），marked 不計入 preexisting，disabled dynamic ref 不匯出。消費端離線驗過（removals→build→REFR flags=0x800、Z=−34603）。**待實機**（wait_todo）。NG 坑：`CrosshairPickData` 成員是 per-VR-device 陣列，flat 取 `[0]`。**未做（富路徑）**：紅輪廓確認流、GUI 列出 cell 內物件直選、持續施法變體。
- **P2 新增（✅ 滴管核心離線落地 2026-07-10）**：`src/Palette.{h,cpp}`——**F6** 滴管吸準星目標的 base+rot+scale 進具名插槽（runtime-only base 拒收並 warn：進了 palette 也 build 不出來）；**F7** 把選中插槽擺在準星處（回填姿態；actor 不回填 scale——XSCL 無效）；面板 `Palette` 頁（use/改名/del、外部 master 警告色）。擺出來是普通 dynamic ref → vanilla diff 自動匯出，契約零改動。**順手把 `kind:"npc"` 陷阱從源頭殺掉**：`ExportCell` 對 actor placement 自動 stamp `kind:"npc"`。**未做（富路徑）**：綠輪廓確認流、持續施法變體、GUI 全域搜物。
- **P2 修改（✅ 2026-07-10 MVP 實機過；✅ 2026-07-11 authored ref 解鎖——overrides[] 契約拍板 B 案並全鏈落地）**：`src/Editor.{h,cpp}`——numpad 5 選準星目標進編輯模式、8/2 前後、4/6 左右、**1/3 高低（與細摳②的 1379 全旋轉不同——spec 沒給高度軸，家具擺放對 Z 的需求遠大於第二旋轉軸；每鍵一個常數，要改隨時改）**、7/9 yaw、+/− 縮放（actor 略過）、0 commit、`.` cancel（還原快照）。5/3/0/. 實機實證（2026-07-11），無未映射 numpad。**authored ref**：commit 時明示登記進 `src/Overrides.{h,cpp}`（比照 Eraser；重複編輯更新既有筆、baseline 不動）→ 匯出頂層 `overrides[]`（live pose；actor 無 scale）→ ModForge `BuildOverrides`（`Spec.Overrides.cs`，同 removals 解析機件）。Editor 面板頁列 overrides、逐筆/全部 revert。契約拍板理由全文在 [spec](../../specs/ingame-scene-export-design.md)「拍板」節。**未做**：泛光選中效果、屬性 GUI（`*/`+`5`，需 record-representable 映射）、持續按鍵重複、跨行程 override 重登記（重編輯一次即可，MVP 接受）。
- **P3 動態物件**：選中凍結物理（`SetMotionType(Keyframed)` 待驗）→ 編輯 → 回復。誠實邊界：匯出 authored 位置，載入後 havok 自行沉降（與 CK 慣例一致）。檢視法術（新增/修改/刪除/全部四種；顯示被刪的 vanilla ref 需暫時 enable 或臨時紅 marker，實作時定）。
- **P4 範圍吸取**：`ForEachReferenceInRange` bound 半徑；面板先預覽「會吸到 N 個」再確認。
- **QoL 四件套（✅ 2026-07-11 離線落地，使用者實測後需求）**：① Markers/Eraser 頁 `this cell only` 過濾（Eraser entry 補記 cell）；② Palette 落盤 `scene-capture-palette.json` 跨存檔（unavailable 槽不炸）；③ **射線選取＝明示入口**（使用者拍板：準星手感不變，`select/erase/pick by ray` 鈕＋numpad \*——牆/地板都是 ref，自動 fallback 會把按空變誤抓，理由在 `Aim.h`）；④ marker 模型改**懸浮發光靈魂石**（`SoulGemGrand01.nif`，讀自 Skyrim.esm STAT 10D18B；舊召喚圈**無碰撞**故 E/準星選不到＋特效播完隱形；clutter havok 用放置即凍解）＋ **E 開編輯視窗**（AddWindow；label/kind/**note**/delete）＋ note 全鏈進 `annotations[].note`（AnnotationSpec.Note，881 測綠）。

# P5：console 指令集＋模式制（✅ 2026-07-11 同日實作＋部署，待實機——commit e4f8fb7；下方為規劃記錄）

**動機（使用者原話意旨）**：快捷鍵太多了（F6/F7/F8/F10/F11＋整排 numpad）。改成**模式制**：console 切模式，之後**同一顆動作鍵（F11）按當前模式做事**。例：`mode_marker` 後 F11=放 marker；`mode_delete` 後 F11=擦除。**指令越短越好**（使用者明示）。

**設計草案**：

- **模式**：`marker` / `del`（擦除）/ `pick`（滴管吸）/ `place`（擺放）/ `edit`（進 numpad 編輯）/ `off`。**export 不佔鍵（使用者拍板 2026-07-11）——一律走 F1 面板 Export 鈕**（已存在，與 F10 同一支函式）；numpad 編輯內部鍵不變（那是模式內操作，不是入口）。
- **指令形狀：✅ 拍板 b 案（使用者 2026-07-11）——一字前綴**：`sc mk` / `sc del` / `sc pk`（滴管吸）/ `sc pl`（擺放）/ `sc ed` / `sc off`。短且零撞名風險。（未選：a 無前綴極短——撞名要逐驗；c `mode_marker`——太長。）
- **指令文法第二層（使用者 2026-07-11 定調）**：`sc <工具> <短參數>`——無參數＝切模式；帶參數＝工具子指令，參數走**極簡縮寫**風格。首例：`sc mk dp0`＝所有 marker 發光體暫時隱形（只是看不到，登記簿與匯出不受影響——座標放置當下已取定，且 proxy 在 exporter 是先於 disabled 判斷被排除的）、`sc mk dp1`＝顯回來。同型模式日後複用（如 `sc del dp0` 隱藏擦除高亮之類）。
- **回饋**：切模式時 `DebugNotification`「SCB mode: marker」＋面板頂部常駐顯示當前模式；`F11` 在 `off` 模式提示先切模式。
- **實作路徑（待研究定案）**：SKSE 自訂 console 指令的成熟做法＝**劫持 vanilla 冷門 ObScript 指令**（改 name/handler，如慣例犯 `ClearAchievement`）或 console 輸入 hook；也可考慮軟依賴 ConsoleUtilSSE。研究時查 CommonLibSSE-NG 的 `SCRIPT_FUNCTION` 改寫先例。
- **遷移：✅ 拍板（使用者 2026-07-11）——不留 classic 開關**。F6/F7/F8/F10 直達與 numpad 5 入口**直接刪除**；輸入面只剩各模式綁定鍵＋numpad 編輯模式內部鍵＋numpad * 射線選取。
- **✅ 設定哲學拍板（使用者 2026-07-11）：不用 ini**。所有設定用 console 指令調；需要持久化的設定**直接放存檔**（SKSE co-save / SerializationInterface）。
- **✅ 快捷鍵哲學拍板（使用者 2026-07-11）：佔用越少越好，且具體鍵位在 F1 面板可設**。**鍵位模型（使用者 2026-07-11 定稿）：每個模式一格鍵位、允許重複**——新增/刪除/修改/打標記/滴管吸取各自在 F1 面板 Keybinds 區設定動作鍵（點擊→按下一鍵改綁，存 co-save），**預設全部 F11**；因為一次只啟用一個模式，同鍵不衝突。這同時支援兩種用法：全設同鍵（靠 `sc` 切模式，佔鍵最少）或各配不同鍵（免切模式直達，自己選擇多佔鍵）。F1 是 SKSE Menu Framework 自己的開關鍵（框架共用，非我們綁的）；F10 砍掉（export 走面板鈕）。
- **co-save 的副作用（提案，implementation 時評估）**：既然引入 SKSE co-save 存設定，**Markers/Eraser/Overrides 登記簿也可以一起進 co-save**——登記簿隨存檔走，關遊戲重開不再歸零，adopt 從主要機制降級為救援機制（撿別的存檔/舊 session 的孤兒用）。這會把「持久化與 adopt 語意」表的三個「要 adopt」格子大部分變成「自動」。
- **✅ 拍板（實作定案）**：`edit` 入口＝動作鍵（F11）準星選中；numpad * 保留為射線選取入口；numpad 5 退役為「復原到編輯前姿態、續留編輯」（見 P6）。

**P5 實機（2026-07-11）**：`sc` 劫持成功（donor `ClearAchievement`），模式切換＋F11 動作鍵行為、`sc mk dp0/dp1`、co-save 三登記簿全數確認。唯 rebind 捕捉到錯鍵（W）→ P6 暫時隱藏。

---

# P6：實機 polish 一輪（✅ 2026-07-11 晚實作＋部署，待實機——DLL `ec88c2b2`；esp 不動）

使用者第一輪實測（P4/QoL/P5）後即時反饋，全部當場落地：

- **#2 靈魂石被踢走**：放置當下 3D 未載入 → `SetMotionType` 靜默失敗（log `Target does not have 3D`）。改**延後凍結**：SKSE task 佇列重試至 `Get3D()` 就緒才凍（`FreezeDeferred`，上限 60 幀）。placement/adopt/prune/dp1 全改走它。
- **#10 marker 讀檔要手動 adopt**：光球是 dynamic ref，FormID 過完整重啟不保證重解析 → co-save 認不回。改 **kPostLoadGame 自動 adopt**（延後一幀掃當前 cell）；並把 co-save 讀到、proxy 解不出的那筆 note/kind/label 存進 `g_pending`，adopt 時用**座標配對**（≤16 units）貼回 → **note 不再因重啟而掉**。
- **#6a numpad 5**：從「commit＋再選」改為**復原到編輯前姿態、續留編輯**；commit（0）/ ray-commit（*）/ cancel（.）各補 `DebugNotification`。
- **#6b 自家 ref commit「沒反應」**：確認**設計正確**——dynamic ref 以 live pose 直接匯出、不進 overrides 列（那是給 authored ref 的）。只補提示消除疑惑。
- **#9 匯出計數**：Export 頁分列 added/modified/removed（placements/overrides/removals，`Stats` 早分開算）＋玩家 XYZ ＋ worldspace 名。
- **Eraser 頁**：逐列 `undo`＋顯示 name＋原座標；`this cell only` 時上方 undo 只退本 cell 最後一筆（`UndoInCell`/`UndoEntry`）。**移除 `scan disabled refs`＋整套 Candidate 機制**（co-save 已持久化耐久 id，跨存檔救援冗餘）。
- **Editor overrides 列**：加名稱＋新座標。
- **Palette 頁**：最新吸取排最上；名稱欄加寬、可自由改名。
- **#8 rebind 隱藏**：捕捉流程 in-game 抓到錯鍵（W），Settings 頁隱藏 rebind、顯示固定 F11；co-save 讀鍵位的 byte 照讀但**不套用**（清掉存進去的壞綁定）。`Modes::BeginRebind` 保留待日後重作。
- **編輯步長可調**：`Editor` 三個步長常數改 runtime 變數＋Settings 頁 `InputFloat`＋co-save **SETT v2** 持久化。
- **co-save 版本**：改 per-record 版本號（`kVerSett=2`/`kVerErsr=2`，其餘 1），OnLoad 傳 version 給各 loader → 舊存檔各記錄按自身 layout 讀（不再全表跳過）。

---

# 加碼一輪：backlog 已落地記錄（2026-07-11–2026-07-12）

P6 之後在 [backlog.md](backlog.md) 累積、現已完工的功能——原記在 backlog.md「已做」區，2026-07-12 backlog 拆分時搬移至此（內容不變，只搬家；日期/DLL crc 見各段標題）。

## ✅ 已做（F1 面板清掉冗餘動作鈕，2026-07-12，DLL crc `6498c57b`，**已部署**，待實機）
- **逐頁盤點後砍掉 7 顆「按一下就在世界裡執行一個動作」的按鈕**（P5 `sc` console 指令＋每模式鍵位落地後，這些面板觸發鈕與 mode+action key 完全重複）：
  - Markers 頁 `place marker here`（`Markers::PlaceAtPlayer`，只放腳下）→ 已被 `sc mk` 模式的動作鍵取代（`Markers::PlaceAimed`，準星瞄準＋無命中自動落腳下，一鍵蓋兩種情境）。
  - Eraser 頁 `erase by ray` → `sc del er1`（切射線）＋動作鍵，同一個 `Eraser::MarkByRay()`。
  - Captures 頁 `capture crosshair` / `capture by ray` → `sc cap` 模式（預設 er0=準星）／`sc cap er1`＋動作鍵，同一組 `Captures::CaptureCrosshair/CaptureByRay()`。
  - Palette 頁 `pick by ray` → `sc pk er1`＋動作鍵，同一個 `Palette::PickByRay()`。
  - Editor 頁 `select by ray` → `sc ed er1`＋動作鍵，或永遠可用的 numpad `*`（`Editor.h` 原文即標「numpad * is the key equivalent」），同一個 `Editor::SelectByRay()`。
  - Editor 頁 `cancel (restore)`（編輯中）→ numpad `.`／Del（`Editor::Cancel()`，面板文字本來就寫著「. cancel」）。
  - 每顆都核對過 `Console.cpp` 指令表＋`Modes.cpp` 的 `RunAction`／`Editor::HandleKey` 派送，確認底層函式仍被 mode+key 路徑呼叫（**只拔 UI 觸發，不動邏輯**）；`Markers::PlaceAtPlayer()` 拔完按鈕後在 DLL 內已無其他呼叫者，**保留原函式**（宣告在 `Markers.h`，非 static，無 unused-function 警告，之後若要救援回來成本為零）。
- **留下的**：Export 三鈕（Export player cell / Export all / Export captures，產出檔案不是改世界，且面板是**唯一**入口，`sc` 沒有 export 指令）；各頁的逐列 undo/revert/del（Eraser 的 `undo`/`clear (re-enable all)`、Captures 的 `undo`/`clear`、Editor 的 `revert`/`revert all`——這些是**復原**既有動作，不是「觸發新世界動作」，且都**沒有 `sc` 替代路徑**）；Markers 頁 `adopt this cell`（跨 cell 孤兒救援，無 `sc` 對應指令，co-save 只在同 cell 自動跑）；marker 編輯視窗 `delete marker`（`sc del` 明文拒收 marker gem，唯一刪除入口）；Palette 的 `[use]`／rename／`del`／三顆檔案 I/O 鈕（`load from file`/`replace from file`/`save to file`，無 `sc` 對應）；References/Markers 的改名/kind/note/apply/del；Settings 頁模式切換鈕與各項顯示。
- **邊界檢查**：backlog 原文提醒的「palette『place slot N』」類按鈕在目前面板**不存在**（每列只有 `[use]` 選取＋`del`，沒有逐列「place」鈕），故無需請示。
- 純 C++（`sub_projs/scene-capture-bridge/src/UI.cpp`、`UI.Markers.cpp`），未碰 C#；`build-release-clang-cl-linux` 編過（撞到 stale cache，`rm -rf build/release-clang-cl-linux` 後重編成功）。

## ✅ 已做（模式開關套件：`py`／`ed`／`pkc` 五項，2026-07-12，DLL crc `5434abd4`，**已部署**，待實機）
- **`sc pl py0/py1`（擺放物理，`py1` 預設）＋ `sc ed py0/py1`（編輯期物理，`py0` 預設）**。DLL 端凍結複用 P3 機制（抽成新的 `src/Physics.{h,cpp}`：`HavokMovable` 判定 ＋ `FreezeDeferred` 延後到 3D 載入才凍；Markers/Editor 原本各抄一份，現三處共用，**行為不變**）。`sc ed py0` ＝把現行凍結行為做成可切；`py1` ＝控制期間 havok 照跑。
- **🔑 持久到 esp ＝「Don't Havok Settle」記錄旗標（查證後拍板，不走 script `SetMotionType`）**：`PlacementSpec.NoHavokSettle` → REFR header flag **`0x20000000`**。**決定性證據**（Mutagen 直接掃 Skyrim.esm，非推論）：693,333 個 PlacedObject 裡 **3,791 個帶此旗標**，base 型別分佈正是雜物類——MoveableStatic 995／MiscItem 724／Activator 564／Weapon 321／**Static 247**／Ingestible 245／Armor 159／Ammo 141／Flora 102／Ingredient 96／Book 87／Container 56…（樣本 `GlazedPot02Nordic`、`RuinsFloorCandleStandSmall`、`CrateOpen`）⇒ **Bethesda 自己擺完雜物就是靠它定住的**。它跳過的正是 **cell 載入時的 havok settle pass**（把手擺物件彈飛的元兇，物件與桌面稍有交疊時尤烈）。成本：純 record header 旗標、與 `Persistent 0x400`／`InitiallyDisabled 0x800` 同一條 code path，**零 script／零 VMAD／零 pex／不多 master**；script 路要給每顆 ref 掛 Papyrus（VMAD＋pex＋OnCellAttach），工程量爆炸只換來「連玩家撞都推不動」的邊際差異，且那是 runtime 狀態、沒 script 跑就不存在。
- **靜態物件要不要吃 `py0`？→ 匯出旗標不按型別過濾**（vanilla 連 247 個 STAT 都帶它；clutter 類本來就都是 havok 物件）。**只有 runtime 凍結過濾**（`HavokMovable`：keyframe 一個 STAT 沒有意義，而且把 STAT/FURN 放回 `kDynamic` 會把牆震鬆——這是 P3 當初就寫死的邊界）。ACHR 不寫（actor 無此語意）。
- **`sc pk ed0/ed1` ＋ `sc pl ed0/ed1`（extra data）**：現況只取 `GetBaseObject()` ⇒ 玩家自己附魔的劍吸進來是**白鐵劍**（附魔活在 ref 的 `ExtraEnchantment`，不在 base）。`pk ed1` ＝插槽連實例附魔一起記（durable ENCH → 引用；runtime ENCH → 記 MGEF effects 待鑄造）。`pl ed1` ＝匯出走**鑄造＋引用**：同一份 scene 檔吐 `capturedItems[]`（`editorId: MFPal_<插槽名>_<seq>`，`base`＝實體模板）＋ placement 的 **`base` 指那個 editorId** ＝ **檔內相依**（同 referrer 那招；**必須同檔**，capturedItems 落到另一份 json 的話 build 解不到 base 會丟掉 placement）。**C# 零改動**（`ExpandCapturedItems` pass 0 → `WeaponSpec/ArmorSpec.EditorId` → `formKeyByEd` → `BuildPlacements` 的 `TryResolveRef` 解得到）。⚠️ **runtime ENCH 的指標絕不快取在插槽上**——插槽是**落盤跨存檔**的，而 runtime ENCH 是存檔綁定的 form（快取＝懸空指標）；世界裡的物件只在 **durable ENCH** 時真的帶上附魔，匯出則照樣鑄造（ship 出去的不受影響）。
- **`sc pkc [XXX]`**：滴管的 console 選取版（同 `delc`/`capc`/`refc` 的 aim-free 路），`XXX` ＝吸取當下直接改插槽名。標號走**未 `Lower()` 的 raw 參數**（大小寫保留，照 `sc capp` 的坑）。
- **co-save**：SETT **v6**（+place/edit 物理 +pick/place extra data；v≤5 舊存檔降級讀 ⇒ 落回預設 place py1／edit py0／extra 全關＝與以前完全一致）＋新 record **`'PLEX'` v1**（我們擺出去、匯出要多講一句話的 ref：`noHavokSettle`／鑄造附魔。**一般擺放不建列**——vanilla diff 本來就吐得完美。handle 跨重啟死掉 → 匯出掃 cell 時按 **base+座標**就地撿回，不必 kPostLoadGame hook）。Settings 頁顯示四個開關現況。
- **端到端自驗（離線閉環）**：手寫 DLL 形狀 json（一筆 `noHavokSettle` placement ＋ 一筆 `base` 指 `MFPal_…` 的 placement ＋ 對應 `capturedItems[]`）→ `build` → esp 裡兩個 REFR 都 **flags=0x20000000（DontHavokSettle=True）**、鑄出 `MFPal_Ebony_Sword_of_Fire_2` WEAP（enchantmentAmount 1500）＋ `…_Ench` ENCH，第二個 REFR 的 base **解到那把鑄造的劍**。C# **932 測綠**（928 + 4 新，含「舊 json 不帶欄位 → 旗標不設」的向後相容釘子 ＋「ACHR 不寫」）。

## ✅ 已做（pointer/referrer 原語**全鏈完工**，2026-07-12，DLL crc `112be269`，**已部署**，待實機）
- **DLL 端補齊**（C# 消費端 `adc419b` 已先落地）：新模組 `src/Referrer.{h,cpp}`（登記簿，**不新建/不改/不 disable** 目標——與 Eraser 的唯一差別）＋ `src/UI.References.cpp`（面板頁）＋ exporter 吐 `references[]`。
- **指令**：`sc ref`＝進 referrer 模式（動作鍵標準星/射線指的既有 ref，`sc ref er0/er1` 切 aim source，SETT **v5**）；`sc ref <Label>`＝一次到位（標下當前指的 ref ＋打標籤）；`sc refc [Label]`＝console 選取版（aim-free）。**標籤走未 `Lower()` 的 raw 參數**（大小寫保留，同 `sc capp` 的坑）。
- **🔑 (乙) 檔內相依關聯（最關鍵，一次做對）**：referrer 指到**我們自己 `sc pl` 擺的物件**（dynamic ref、無耐久 FormID）時——`AppendPlacements` 掃到那筆 ref 就在它的 placement 上**蓋一個穩定 editorId**（`Referrer::EditorIdOf` ＝ `MFRef_<sanitize(label)>_<seq>`，seq 隨 co-save 故跨匯出穩定），`references[].ref` 指那個 editorId。**identity ＝ handle**（沒有耐久 id 可用）。`AppendReferences` **必須跑在 `AppendPlacements` 之後**，且**只吐「這次匯出真的有出 placement」的那些**（cell 沒掃到／物件被擦掉／跨重啟 handle 死掉 ⇒ 跳過＋log warn＋面板顯示 skipped 數，不吐一個 build 對不上的 editorId）。
- **(甲) 外部既有 ref**：照記耐久 `<plugin>:0xLOCALID` ＋ base ＋座標/rotation/scale ＋ cell/worldspace。**`anchor` 欄位 DLL 一律不填**（留白＝`none`，persistent 逃生門的選擇權在 ModForge/agent）。
- **拒收三類**：① **marker proxy**（editor chrome，`ExportCell` 本來就排除它 → 檔內 reference 永遠解不到；而且 marker 本來就有 label/note 走 `annotations[]`）；② **我們自己生的 actor**（cell 匯出不含 actor ⇒ 沒有 placement 可指；要複製走 `sc cap`）；③ **重複 label**（label 在 ModForge 是**全域名字空間**＝可解析的 id，撞名 validate 會炸整份 spec）。**authored actor（vanilla NPC 的 ACHR）可以指**——它有耐久 id，走 (甲)。
- **面板 References 頁**：最新在前、label／note 就地改名（**擋重複 label**，撞名當場橘字說「not renamed」）、顯示 ref id（檔內顯示**將寫進 json 的 editorId**，綠字）／base／cell／座標、逐列刪除（**只刪登記列，世界不動**）。Export 頁多兩行統計（`N named (references[])` ＋ skipped 數）。
- **co-save 新 record `'RFRR'` v1**；in-file 列的 handle 跨完整重啟會死 → `Referrer::ReacquireOrphans()`（`kPostLoadGame` 自動跑，按 base+座標在玩家 cell 重新綁回，同 marker 的 adopt 救援）；撿不回的列**保留**（面板標 `TARGET LOST`、匯出跳過），不靜默丟。
- **端到端自驗（離線閉環）**：手寫一份 DLL 形狀的 json（placement 帶 `MFRef_sofia_s_chair_1` ＋ `references[]` 指它 ＋ Sofia 的 sandbox package 指 label）→ `build` → esp 裡該 REFR **record flag ＝ 0x400**、落在 cell 的 **Persistent group**（`dump`：`persistent=1 temporary=0`），build 摘要印 `1 reference(s) — labels bound to existing refs: 'sofia's chair'`。C# **928 測綠**（消費端零改動）。
- **🎮 價值主張 IN-GAME PASS（2026-07-12）**：`examples/referrer-chair-anchor.json` → `ModForgeReferrerChair.zip`（vanilla `WhiterunBreezehome` override，對照組佈局——誘餌椅 `807` 未命名、更近、擋在路上；命名椅 `808` 用 `sitTarget.target` 走 SingleRef 槽）。使用者實機回報「坐在 808」：Sofia 走過誘餌坐上被命名的那張，趕起來後也會自己走回去。過程插曲（第一版命名椅貼北牆卡住坐不下，反而證明她非那張不可，`y=400→330` 修正見 commit `287f755`）＋ 挖出的 slot-kind 教訓（SingleRef vs Location 槽決定「鎖定那一個」還是「一塊區域隨便挑」，已加 `PackageRefSlots.cs` 護欄）詳見落地句 [landed/world](../../feature-dev/landed/world.md)「referrer 原語」節。**剩 open＝DLL 端**（`sc ref`/`sc refc`/面板頁/跨重開撿回檔內目標，這次測的是手寫 spec 不是遊戲內標記流程），見 [wait_todo](../../../wait_todo/ingame-tests.md)。

## ✅ 已做（referrer 下游修正**第五隻＝最後一隻，家族結案**：共用 `BuildCondition` 的 CTDA `param`／`reference`，2026-07-12，**純 C#**）
- **家族全貌（五隻，全部同一顆種子）**：referrer（`references[]` label）落地後才發現，**「eager 解析一個可能是 placed ref 的欄位」**這個錯誤在 repo 裡散落五處。placement 與 label 要到 `BuildPlacements`(pass-2 ~115)／`BuildMapMarkers`(116)／`BuildReferences`(117) 才進 ref 表；**任何更早解析的欄位，永遠只看得到 base record**。五隻：① `eat.location` ② `useMagic.location`／`useMagic.target`（① ② 同一 commit，`BuildPackageData`）③ `placements[].enableParent.ref`（`BuildPlacements` 迴圈內）④ ——⑤ **本次：共用 `BuildCondition()` 的 `param`／`reference`**。
- **這隻為什麼最大**：前四隻都是**單一欄位**；這隻是**共用 helper 的兩個欄位**，被 **10 個呼叫點**共用，其中 **4 個跑在 placements 之前**。而且**當初的開發者知道條件能指 placed ref**——dialogue／banter／package 的條件早就被刻意 defer 到 `Build.cs` 150/154/155（placements 之後）。同一條規則**只用「排序」表達、沒寫下來**，於是另外四個呼叫點漏了。
- **`BuildCondition` 呼叫點地圖**（Build.cs pass-2 行號；`BuildPlacements`＝115、`BuildReferences`＝117）：

  | 呼叫點 | 步驟（行） | 修前 | 現況 |
  |---|---|---|---|
  | `BuildStoryManager`（storyEvent 條件＋`locationFilter`） | 87 | ❌ eager | ✅ `DeferCondition` |
  | `BuildQuestAliases` → `WireAliasMatchConditions`（`findMatching*` match filter） | 87＋88（SM 與 standalone **共用**） | ❌ eager | ✅ `DeferCondition` |
  | `WireScenes`（scene／phase start／completion 條件） | 93（`sceneConditionWires` **只 defer 到這一步**，不是到 placements 之後） | ❌ eager | ✅ `DeferCondition` |
  | `WirePerks`（perk trunk 條件＋effect 條件） | 103 | ❌ eager | ✅ `DeferCondition` |
  | `WireRecipes` | 141 | ✅ 本來就在 placements 之後 | 不動 |
  | `WireDialogueConditions`（inline＋templates＋identity＋variants） | 151 | ✅ **本來就刻意 defer** | 不動 |
  | `WireQuestStages` | 153 | ✅ | 不動 |
  | `WireObjectiveTargets` | 154 | ✅ | 不動 |
  | `WireBanterConditions` | 155 | ✅ **本來就刻意 defer** | 不動 |
  | `WirePackageConditions` | 156 | ✅ **本來就刻意 defer** | 不動 |

- **修法的形狀＝「只在那四個呼叫點補 defer」，不動 `BuildCondition` 本身的簽章**。理由：(a) **爆炸半徑最小**——已經正確的 6 個呼叫點一行不改，產物逐位元不變是**由構造保證**的，不是靠測試碰運氣；(b) 把 `BuildCondition` 改成「回傳 deferred wire」做不到——它回傳一個 `ConditionFloat`，10 個呼叫點把它塞進**形狀不同的容器**，而且有呼叫點**依賴回傳值是不是 null** 來決策（perk effect 的 `if (pcond.Conditions.Count > 0)`）；(c) 仍需要「當場建」的能力（未來若有跑在 placements 之後的新呼叫點）。
- **落實**：`Generator.Build.Conditions.Wire.cs` 新增 `DeferCondition()`（把 target `IList<Condition>`＋`ConditionSpec`＋label＋`aliasIdx`＋`owningScene` 排進 `deferredConditionWires`）／`DeferConditionFinalizer()`／`WireDeferredConditions()`（Build.cs 排在 `WirePackageConditions` 之後、`WireDeferredScriptObjectProps` 之前）。**依排隊順序排空** ⇒ 每個 target list 的 CTDA 順序與 eager 版**完全相同**（`EventConditions` 仍是 [storyEvent…, locationFilter…]；alias 仍是 [fill-shape 條件…, match 條件…]）。perk effect 的 `PerkCondition` tab 走 **finalizer**：條件**全部**建不出來時**不掛空 tab**（vanilla 不發空 PRKC，＝舊 `Count > 0` 的位元等價）。
- **🔑 鐵律（寫死在 `BuildCondition` 檔頭 ＋ CODE_MAP）**：**CTDA 的 `param`／`reference` 是任意 ref，可以合法指 placed ref ⇒ 任何跑在 `BuildReferences` 之前的步驟都不准直接呼叫 `BuildCondition()`，必須 `DeferCondition()`**。並且**不只是註解**——新增 `refsIndexed` 旗標（`BuildReferences` 開頭設起），`BuildCondition` 早於它被呼叫就吐 `BUILD-ORDER BUG` **警告**。第六隻要是哪天長出來，會**當場自己叫**，不會再靜默。
- **產物影響＝零**：`examples/` **全部 139 份 spec** 改前改後 `build`，**esp md5 一份不差**；139 份 build log（警告＋`linksWired`/`extLinks` 統計）正規化後**零差異**。既有路徑（條件指 vanilla ref／base record／不指任何 ref）**一位元不變**。
- **驗證（新通的路）**：一份四個呼叫點各指「檔內 placement editorId」與「`references[]` label」的 spec —— **修前**：6 條 `! … param ref … unresolved` 警告，**條件全被丟棄**（`perkdiag` `Conditions = 0` / effect `conds=0`、`scenediag` `startConds=0`）；**修後**：**0 警告**，`Conditions = 1` / `conds=1` / `startConds=1`。C# **1021 測綠**（1005 ＋ 16 新，新增 `ConditionPlacedRefTests.cs`；把 src 改動 stash 掉重跑 ⇒ 16 中 **11 紅**，證明測試真的釘到 bug，另 5 條是本來就該兩邊都綠的負向釘）。
- **家族結案掃描**：逐一過完 `BuildPlacements` 之前的**每一個** pass-2 步驟的 ref 解析——`BuildQuestSpawns`(`spawn.form`＝要 `PlaceAtMe` 的 **base** form)、`WireNpcs`(race/class/outfit/voice/crime/combatStyle/spells/factions/items/headParts)、`WireVendors`(`sellBuyList`＝FLST；merchant chest ＋ VendorLocation **早已 deferred**)、`WireRelationships`(NPC **base**)、`WireScenes`(uniqueActor＝NPC base／action PACK／gateGlobal GLOB)、`WireKeywords`/`WireSounds`/`WireAlternateTextures`/`WireEffects`/`WireEnchantments`/`WireMagicEffectRefs`/`WireMagicFxRefs`/`WireHazards`/`WireMusic`/`WireWeatherAndClimateLinks`/`WireShouts`(全是 base record)、`WirePerks`(nextPerk/ability spell＝base)、`BuildPackageData`(base 槽；12 個 ref 槽早已 deferred)、`WireNpcPackages`/`WireNpcPatch*`/`WireOutfits`(base)、`BuildWorldspacesAndRegions`(climate/water/lodWater/interiorLighting/location/music/encounterZone/baseTexture/region worldspace/weather/global —— **全是 base record，沒有一個吃 placed ref**)。`mapMarkers[]` 除了 `worldspace` 之外**沒有 ref 欄位**。**結論：沒有第六隻。**這個家族到此為止。

## ✅ 已做（referrer 下游修正：三個 package 槽解不到 label／檔內 editorId，2026-07-12，**純 C#**）
- **既有 bug（referrer 挖出來的）**：`eat.location`／`useMagic.location`／`useMagic.target` 這三個槽在 `BuildPackageData` **當場解析**——但那一步跑在 `BuildPlacements`／`BuildReferences` **之前**，ref 表這時只有 base record ⇒ 填 `references[]` label 或**檔內 placement editorId** 一律解不到，無聲掉回 `NearSelf`／`PackageTargetSelf`（只留一行 `! package 'X' eat location '…' unresolved` 警告）。其餘 9 個同類槽早就走 deferred wire（程式碼註解甚至寫著 "DEFERRED (like Travel's Place)"），**這三個是漏網的**。
- **修法**：三個槽改丟 `deferredTargetWires`／`deferredLocationWires`，由 `WireDeferredTargets`／`WireDeferredLocations` 在 placements＋labels 都在了之後填。`useMagic.target` 的 slot 4 仍**先寫** `PackageTargetSelf`（self-cast 預設＋解不到時的 fallback），ref 走 `DeferTarget(selfOnUnresolved: true)` ⇒ 解不到時警告文字照舊講「defaulting to PackageTargetSelf」，不會被通用的「package will no-op」蓋掉。
- **🔑 鐵律（已寫進 `PackageRefSlots.cs` ＋ `BuildPackageData` ＋ CODE_MAP）**：`PackageRefSlots` 的 **12 個 SingleRef/Location 槽一律不准在 `BuildPackageData` 解析**——新 template 加 ref 槽時同理。base form 槽（template/combatStyle/ownerQuest/`useMagic.spell`）不受此限。
- **產物影響**：填 **vanilla FormID** 的既有路徑**一位元不變**（`examples/usemagic_spec.json`／`examples/package-eat.json` ＋一份三槽全填 vanilla ref 的 spec，改前改後 esp md5 相同；`Package.Data` 是 key-sorted 寫出，deferred 不動 byte）。repo 內**沒有任何 spec 用這三個槽填 label／editorId**（那條路本來就不通），所以**沒有既有 spec 的 esp 會變**。
- **驗證**：`packagediag` 顯示三個槽都指到 label 的目標 REFR（`LocationTarget(000804)`／`PackageTargetSpecificReference(000804)` ＝ `MFChair` 那筆 placement），且 label 落在 location 槽時 area-anchor info 提示照印。C# **1002 測綠**（996 + 6 新：三槽 label／檔內 editorId 正面測試 ＋ vanilla FormID 回歸 ＋ 解不到仍警告 ＋ 空 target 仍 `PackageTargetSelf`）。

## ✅ 已做（referrer 下游修正第三隻：`placements[].enableParent.ref` 解不到後面的 placement／label，2026-07-12，**純 C#**）
- **既有 bug（同一家族，第三隻）**：`enableParent.ref` 在 `BuildPlacements` **迴圈裡當場解析**——ref 表這時只裝得下「這個迴圈已經跑過的更早 placement」，`references[]` label 更是完全還沒生（`BuildReferences` 整段跑在 `BuildPlacements` 之後）。結果：`enableParent` 只能指列表中**更早**的 placement（順序敏感，這本身就很怪），指不到後面的 placement 或任何 label——都無聲 fallback（掉回**不寫 XESP**，只留一行 `unresolved` 警告；比前兩隻輕，因為現況本來就會警告，不是靜默）。「這扇門開了才顯示裡面的箱子」這種箱子寫在門前面的自然寫法會踩到。
- **修法**：`ep.Ref`＋`ep.Flag` 連同 `placedRec` 一起丟進新的 `deferredEnableParentWires`，由 `Generator.Build.PlacementRefs.cs` 新增的 `WireDeferredEnableParents`（跑在 `WireTeleportDoors` 之後、`WireDeferredTargets` 之前，即 placements＋references[] 都在了之後）補建 XESP（`Reference`＋`Flags` 一起寫，解不到就完全不建——沒有「self」之類的 fallback，跟舊的 eager-resolve 行為一致）。**沒有**把 `ep.Ref` 併進 `deferredAnchorEds`（不強制 target persistent）——舊的兩條會過的路徑（vanilla ref／指更早的 placement）本來就沒有這條 persistence 副作用，併進去會讓既有 esp 產物變。
- **產物影響**：`git stash` 對照改前改後——**vanilla ref**與**指列表中更早的 placement**兩條既有路徑 esp **md5 完全相同**（`enableParent.ref` 指向的 target 不受影響，`linksWired`/`extLinks` 統計不算 esp bytes）。repo 內沒有任何 spec 用 `enableParent` 指 label 或後面的 placement（那條路本來就不通），所以沒有既有 spec 的 esp 會變。
- **驗證**：CLI `build` 手測——指列表**後面**的 placement 從「印 `unresolved` 警告＋0 cross-ref link」變成「0 警告＋1 cross-ref link」；指 `references[]` label 同樣乾淨過（2 links＝label 綁定本身 1 ＋ XESP 解析 1）。C# **1005 測綠**（1002 ＋ 3 新：forward-reference 正面測試／references[] label 正面測試／解不到仍警告的回歸釘子）。
- **順手掃過的「這個家族還有幾隻」**：`linkedRefs`（`WireLinkedRefs`）、objective target（`WireObjectiveTargets`）、script Form property（`AttachScripts`＋`deferredScriptObjectProps`/`WireDeferredScriptObjectProps`）、`forced:` alias（`deferredForcedAliases`/`WireDeferredForcedAliases`）——**這四個都已經正確 deferred 到 placements/references 都建好之後**，不是這隻。但掃描過程中發現**共用的 `BuildCondition()`（CTDA 的 `reference`/`param` 可以指向任何 ref，包括 placement/label）在好幾個呼叫點跑得比 `BuildPlacements`(pass2 line 115)／`BuildReferences`(117) 早**：`WirePerks`（perk/perk-effect 條件，@103）、`BuildStoryManager`（storyEvent 條件／`locationFilter`／alias `findMatching*` 系列的 match 條件，@87）、`BuildStandaloneQuestAliases`（同款 alias match 條件，@88）、`WireScenes`（scene/phase 條件，@93，`sceneConditionWires` 只 defer 到這一步，不是到 placements 之後）——**dialogue／banter／package 的條件已經被刻意 defer 到 150/154/155（placements 之後）**，證明開發者知道條件能指placed-ref；上面這幾個呼叫點看起來是**同一顆漏網的種子，但範圍是「BuildCondition 的 reference/param」而不是單一欄位**，牽動面比前三隻大很多（perk 距離/場景觸發指定物件等authoring情境是合理的）。**當時未動手修**——只把範圍記下來；`WireQuestStages`(152)／`WireObjectiveTargets`(153)／`WireRecipes`(140) 的條件已經在 placements 之後，沒事。→ **已於同日修完＝第五隻，見本檔最上方「家族結案」節**（含完整呼叫點地圖 ＋ 寫死的鐵律 ＋ 「沒有第六隻」的掃描結論）。

## ✅ 已做（旋轉 per-axis 還原 ＋ palette replace，2026-07-12，DLL `9cae7ff1`→部署為 `c5049c78`，**🎮 實機 PASS 2026-07-12**）
- **旋轉子模式的歸零鍵改 per-axis（使用者實機後提）**：`sc ed ax` 下**每組的中間鍵只管自己那一組軸**——**2＝還原 pitch（1/3）、5＝還原 yaw（4/6）、8＝還原 roll（7/9）**。語意＝**revert 回進編輯前的該軸原值**（`g.origAngle.<axis>`），**不是設成 0**（物件本來就可能有角度）。原本三鍵都是「整個角度還原」（全軸 `origAngle`）。移動模式的 numpad 5（＝復原整個編輯）**不動**（P7 的 per-mode 行為）。`Editor.cpp` 的 `kBack`/`kSelect`/`kFwd` 三個 case；每鍵各自的 DebugNotification。
- **palette 檔案 I/O 兩改**：① **檔內順序＝面板順序**（最上面那筆排 json 第一筆）——`SlotsJson()` 反向寫、`ParseSlots()`＋`Adopt()` 反向插；`load from file (append)` 的新項因此**落在列表最上面**且保留檔內順序（面板最新排頂的既有慣例）。② 新鈕 **`replace from file`**（`Palette::ReplaceFromFile`）＝**清空現有插槽再載入**；檔案不存在／不可讀／無可用插槽 ⇒ **不清**（不會誤把磁碟持久的 palette 清光）。三鈕並列：`load from file (append)` / `replace from file` / `save to file` ＋一行說明。
- ⚠️ 舊 `scene-capture-palette.json`（舊格式＝反序）讀進來順序會**上下顛倒一次**，之後穩定；欄位完全相容。

## ✅ 已做（`capturedNpcs[].isPlayer` 標示，2026-07-12，DLL crc `e37ad0e1`，**已部署**，待實機）

> 🐞 **這一版的 `isPlayer` 是壞的**——下面寫的 `actor->As<PlayerCharacter>()` **對任何 actor 都必定回傳 nullptr**，所以 `isPlayer` 永遠 false、玩家 perk 一顆都吸不到（實機釘死：`captures_20260712-2250.json` 只有 12 個 base perk）。**已修**（commit `eb6ae75` 改單例指標比對，DLL `dd7afd82` 已部署）；驗屍與可推廣判準見 [plans/player-capture-capp.md](../player-capture-capp.md) 末節，驗收錨點見 [wait_todo](../../../wait_todo/ingame-tests.md)。

- 實機發現玩家 base TESNPC **沒有 `voiceType`**（分身啞巴）；使用者拍板**照實輸出，不加 fallback**——但補一個「這筆是玩家」的標示。`NpcData.isPlayer`（當時寫成 `actor->As<PlayerCharacter>()`，**＝上面那顆 bug**，已改單例比對）；`SceneExporter` 只在 true 時吐 `"isPlayer": true`。co-save **SCCP v9**（v≤8 缺省 `false`）。
- C# 消費：`CapturedNpcSpec.IsPlayer` → `NpcSpec.IsPlayer`（純可見性，不寫任何 Mutagen 記錄欄，行為不變）→ `BuildNpcs` 只在「`IsPlayer` 且無 `VoiceType`」時 `Warn`（措辭「this is expected, not a bug」，不是錯誤）。舊 json 缺欄位＝`false`＝完全相容。詳見 [plans/player-capture-capp.md](../player-capture-capp.md)。C# 928 測綠（5 個新測試）。

## ✅ 已做（`sc capp` 直接吸玩家，2026-07-12，DLL `f8afc170`，待實機）
- **`sc capp [Label]` ＝直接吸玩家**（去 PROTEUS 化）：玩家 chargen 就在 base TESNPC（`Skyrim.esm:0x000007`），DLL 直讀 → `capturedNpcs[]`。**PROTEUS 中介整條移除**（clone 自報 L1／50-50-50、不寫 tintLayers、outfit 空殼＝裸體，三個缺陷一次解掉）。玩家 perk 讀 `PlayerCharacter::addedPerks`（玩家 base 的 perk array 是空的）。
- **顯式數值（所有 actor，不只玩家）**：`GetBaseActorValue` 取 H/M/S ＋ AV 6..23 的 18 技能（＝Mutagen `Skill` enum 序）→ 匯出 `health/magicka/stamina/skills[18]`。ModForge 消費**優先序＝顯式 ＞ class autocalc**（有顯式值就寫 DNAM、`autoCalcStats` 關；沒有才走舊路 → **舊 capture json 原樣相容**）。
- **`sc capc [Label]` ／ `sc capp [Label]` 標號**：→ `editorId: "MFCap_<label>"`，「顯式 editorId 優先」即身份機制（同 label 再吸＝同一筆）。⚠️ label 走**未 `Lower()` 的 raw 參數**（大小寫保留）——`pkc`/referrer 動工時照抄這條。
- co-save **SCCP v8**（+label +H/M/S +skills；v≤7 照讀）。C# 端 923 測綠。詳見 [plans/player-capture-capp.md](../player-capture-capp.md)。
- **🔴 部署鐵律（血的教訓）**：遊戲跑著時 `cp` 就地覆寫 DLL ＝ **無聲暴斃、無 crash log**（`cp` 寫穿同一個 inode，而 DLL 程式碼頁是 demand-paged from that file）。一律走 `scripts/deploy.sh`（`pgrep SkyrimSE.exe` 在跑就拒絕 ＋ tmp+rename 換 inode）。

## ✅ 已做（匯出三改，2026-07-12，DLL `65f53a93`，待實機）
- **Export 檔名帶場景＋時間**：`scene-export_<cell EditorID 或 worldspace_x<X>y<Y>>_<YYYYMMDD-HHMM>.json`（`Export all` ＝ `scene-export_all-<玩家所在>_…`）。名稱 sanitize 成 `[A-Za-z0-9._-]`、截 48 字；同分鐘同場景再匯出加 `-2`/`-3`，**永不覆蓋**。⚠️ 下游 agent 別再寫死 `scene-export.json`，取資料夾裡最新一份。
- **Captures 獨立 Export 鈕**：Export 頁／Captures 頁各一顆 `Export captures` → `captures_<YYYYMMDD-HHMM>.json`，只含 `capturedItems[]`＋`capturedNpcs[]`；**場景匯出檔不再帶這兩段**。兩者都是 `ModSpec` 成員故單獨 `build` 吃得下（**ModForge C# 端零改動**）。
- **📌 Scope 反轉（NPC 移出 cell 匯出）**：`ExportCell`/`ExportAll` 掃到 actor ref 直接跳過（計 `actorsExcluded`，只進 log/面板），`placements[]` 不再有 `kind:"npc"`。NPC 交給 ModForge 按 `annotations[]`（marker）擺；真要複製某 NPC 走 `sc cap` → `capturedNpcs[]`。[spec](../../specs/ingame-scene-export-design.md) 契約節已同步（新增「2026-07-12 拍板」節，並標註推翻 2026-07-10 那條）。

## ✅ 已做（P7 backlog，2026-07-11，DLL `79e611e8`→`a46ed0b2`，待實機）
- `sc del/pk/ed er0/er1`：該模式動作鍵準星↔物理射線切換（`Modes::UseRay` per-mode，co-save SETT v3）。取代「numpad * 專用鍵才能射線」的需求。
- **`sc ed ax`（純旋轉子模式，使用者第二輪定案取代 ax0/1/2）**：ON 時 numpad 4/6＝yaw、1/3＝pitch、7/9＝roll、8/2＝角度歸零（**歸零鍵已被 2026-07-12 的 per-axis 還原取代**，見上）；OFF 時照舊位移。`Editor::g_rotateMode`，co-save。
- `sc delc`：擦除 `RE::Console::GetSelectedRef()` 選中的 ref，走 `Eraser::MarkConsoleRef`；actor 拒絕（先只做物件）。
- 編輯指向靈魂石 marker → numpad 0 commit 更新該 marker 登記簿座標（`Markers::SetTransform`），不進 overrides；orphan proxy 就地 adopt。
- palette「load from file」鈕（append）＋**「save to file」鈕**（`Palette::LoadFromFile`/`SaveToFile`，讀寫 SKSE 夾下具名檔）。（**2026-07-12 續改**：append 排最上＋新增 `replace from file`，見上。）
- **Export「Export all (loaded cells)」鈕**：`SceneExporter::ExportAll` 走訪全部已載入 cell 收 placements＋registries 一次（registries 本就全域；未載入 cell 的 placements 撈不到，log 說明）。重構出 `AppendPlacements`/`AppendRegistries`/`RecordStats`。
- Settings 頁顯示 aim source／旋轉子模式現況（console 設定的可視化）。
- **numpad 5 改 per-mode**（使用者第三輪）：純旋轉模式下 5＝角度歸零（同 8/2），移動模式下 5＝復原編輯前——不再兩模式共用。（**2026-07-12**：旋轉模式下的 5 進一步收斂成**只還原 yaw**，見上；移動模式的 5 不變。）
- **marker 記錄完整朝向＋大小**（使用者第三輪）：Entry `angleZDeg`→`angleDeg{x,y,z}`＋`scale`；匯出 `annotations[]` 帶 `rotation`＋`scale`（ModForge `AnnotationSpec.Rotation/Scale`，869 測綠）；co-save MKRS v2（舊 v1 只有 angleZ→補 0）。**marker 模型改鐵匕首**（`Weapons\Iron\IronDagger.nif`，劍尖視覺化朝向；tools-spec.json 改 model 重建 esp，houseCARL 驗 WEAP 01397E）。

## ✅ 已做（外部 mod 依賴的**可見性**＝候選 (a)，2026-07-12，**純 C#、零 DLL 改動**，離線閉環驗完）
- **問題**：`sc capp`/`sc cap` 吸到的 spell/perk/item/effect 只要來自 mod，生成的 esp 就把那些 mod 變成 **master**；缺 master → Skyrim **靜默不載**（不說為什麼），而 `build` 過去**零可見性**。**範圍不只 capture**：任何手寫 spec 寫 `PROTEUS.esp:0x123` 都一樣 ⇒ 處置做在 **ModForge 通用層**（`src/ModForge.Core/Generator.Dependencies.cs`）。
- **🔑 使用者已拍板：不過濾——「完全複製」優先**（分身的價值在「就是你」；要可攜就手寫 spec）。所以這條**純粹是可見性**：`build` 產物**一個 byte 都不變**（`MFCapHatak.esp` md5 改前改後同為 `638aae3c…`；另有測試釘死「跑不跑分析，寫出來的 bytes 相同」）。
- **`build` 摘要印非 vanilla masters ＋ 逐筆歸因**。歸因粒度＝**作者親手寫的那個 spec 欄位**（不是只到 record）。拿使用者 2026-07-12 那份**真的** capture 跑，7 個 master 一個不差，逐行講是誰拉進來的：
  - `PROTEUS.esp (1 link) ← capturedNpcs[0].spells[17] = PROTEUS.esp:0x08073D`
  - `Conditional Expressions.esp ← capturedNpcs[0].activeEffects[3].magicEffect = …:0x00081A`（活性效果也照抓）
- **兩個來源，各司其職**：**master 清單以「建好的 mod」為準**（掃 record FormKey ＋ `EnumerateFormLinks`——抓得到 spec 字串沒寫、被 deep-copy／template clone 帶進來的 master）；**歸因以 spec 為準**（reflection walk 出 JSON 路徑）。⚠️ 歸因快照必須在 `ExpandMacros` **展開前**取（`ModSpec.AuthoredRefSources`，internal）——否則 captured NPC 會報成巨集生出來的 `npcs[0].spells[…]`，那是**使用者檔案裡根本不存在的欄位**。
- **旁檔 `<plugin>.requires.txt`**（寫在 esp 旁；沒有非原版依賴時刪掉舊檔）＝解掉後果②「沒有任何地方記著這個 esp 依賴誰」。**`package` 只印摘要、不寫旁檔**（它的輸出夾就是要出貨的 mod，不該多塞檔案）。
- **CC 不算 vanilla**（`ccXXXSSE###` / `_ResourcePack`）：按帳號購買，沒買的玩家一樣靜默不載——照列，只是標註原因。vanilla ＝ Skyrim/Update/Dawnguard/HearthFires/Dragonborn 五個。
- **語氣＝資訊、不是錯誤**（使用者拍板要完全複製）；純 vanilla spec **一個字都不印**（negative-case 測試釘住，免得變背景噪音）。C# **971 測綠**（951 + 20 新）。docs：[for_agent_cli](../../../docs/for_agent_cli.md)（＋zh-TW 鏡像）。
- **另三個候選**：(b) spec 宣告式 `requires:` 段 → **✅ 同日補上，見下節**；(c) modlist / load order 快照（MO2 `plugins.txt`）、(d)「依賴檢查」指令（給 esp ＋ load order，回報缺什麼）**仍未做**（收進 backlog）。

## ✅ 已做（外部 mod 依賴的**宣告式契約**＝候選 (b)，2026-07-12，**純 C#、零 DLL 改動**，離線 **987 測綠**）
- **(a) 只解決一半**：它**記錄**依賴，卻不**驗證**。真後果是**漂移**——哪天移除 PROTEUS／重吸一次 capture／刪掉一行，esp 的 master 清單就悄悄變了，而缺 master ＝ Skyrim **靜默不載**。(b) ＝ spec 宣告「這個 mod 需要這些 plugin」，**build 對不上就報錯**。
- **形狀**（`Spec.Requires.cs`；`ModSpec.Requires` 是 **nullable**）：
  ```json
  "requires": [
    "XPMSE.esp",
    { "plugin": "PROTEUS.esp", "version": "3.4+", "reason": "被吸的玩家法術", "url": "https://nexus…" },
    { "name": "PapyrusUtil SE", "reason": "storageWrites（SKSE，沒有自己的 esp）" }
  ]
  ```
  `plugin` ＝會被雙向檢查的 master（裸字串是它的簡寫）；**`name` ＝沒有 plugin 的相依**（SKSE DLL／loose-file framework——永遠不可能是 master ⇒ **純文件、永不檢查**，但照樣進旁檔＝玩家看的需求清單）；`reason`/`url` 給人看。
- **雙向檢查，不對稱是刻意的**：**用到但沒宣告 → 錯誤，且 esp 一個 byte 都不寫**（`RequiresOk` 擋在 `PluginIo.Write` 前面，`package` 走同一個閘門——沒宣告的 master 就是「玩家不知道要裝什麼」那個失敗，不能讓它出貨；訊息直接點出**是哪一行 spec 欄位**拉它進來）；**宣告了但沒 link → 只警告**（陳舊行不致命；訊息順便教你：runtime 才需要、沒 master 的東西改用 `name`）。
- **🔑 向後相容（硬要求）**：**spec 沒寫 `requires` 段（null）＝完全不檢查**——repo 幾十個 example spec ＋已出貨 spec 零行為改變（negative-case 測試釘死）。寫這段＝**opt-in**。**空陣列 `[]` 也是一種宣告**：「這個 mod 只用 vanilla」，之後任何 mod ref 都會擋下。
- **自動補宣告：`build <spec> <out.esp> --sync-requires`**（`SyncRequiresFile`；純函式 `Generator.SyncRequires`）——capture **大量**自動引入依賴，手動補宣告會煩到讓契約不值得存在。sync 會：補上實際 link 的 master（`reason` 自動填**拉它進來的那個 spec 欄位**）、丟掉陳舊項、**保留作者手寫的 `reason`/`version`/`url`**、`name` 條目不動。真正的回報：**依賴變動變成 spec diff 的一行**，在 git 裡可審。（`requires` 來自 `$ref` include 時拒絕改寫，免得宣告分叉成兩份。）
- **版本檢查＝查證後確認做不到，所以不做假的**。掃了本機 40+ 個真 mod 的 TES4 header：`HEDR` 的 version 是**檔案格式版本**（清一色 1.70/1.71——PROTEUS 3.4 跟兩筆記錄的測試 esp 完全一樣）；`CNAM`/`SNAM`（作者／描述）是自由文字（多半 `DEFAULT`／空／行銷文案；少數人塞版本如 `SkyUI SE 6.0`、`Vigilant SNAM=181`，不可依賴）。**真版本只活在 mod manager 的 metadata**（MO2 `meta.ini` `version=`，來自 Nexus），不在 plugin 裡、build 也看不到 ⇒ `version` 欄位**只是給人看的標籤**，旁檔明寫 `NOT verified`。
- **複用 (a)、不重算**：`CheckRequires(spec, deps)` 直接吃 `AnalyzeDependencies` 的結果 → `BuildResult.Requires`。旁檔 `RequiresFileText` 升級成折進 `requires[]` 的中繼資料（含沒有 plugin 的相依）。**`requires[]` 不進 esp**（測試釘死 byte 相同）。
- 檔案：`Spec.Requires.cs`／`Generator.Requires.cs`／`Generator.Dependencies.Report.cs`（把 (a) 的輸出文字拆出來，守 300 行門檻）＋ `Program.Build.cs`（`RequiresOk`／`SyncRequiresFile`）。docs：[SPEC-workflow](../../../docs/spec/SPEC-workflow.md)、[for_agent_cli](../../../docs/for_agent_cli.md)（＋zh-TW 鏡像）、`examples/spec.schema.json`。

## ✅ 已做＋**已驗收**（依賴可見性的**遊戲內版**：Export 頁 `Export requires` 鈕，2026-07-12，DLL `6498c57b`→`008aba47`；驗收 2026-07-13）
- **價值＝時機**。上面那條 C# 版是 **build 後**才知道——那時你已經退出遊戲了；這顆鈕把同一個問題**提前到匯出當下**：人還站在那間房，覺得那顆 PROTEUS 法術不值得讓整個 mod 變成硬相依，**重吸一次就好**。兩者互補、格式對齊（路徑前綴 `scene.` / `captures.` 指出該去改哪個檔），輸出可互相比對。
- **`Requires.{h,cpp}`**（新）：走訪**匯出後的 json**（`SceneExporter::ScanAll`/`ScanCaptures` ＝匯出減掉寫檔與統計副作用），收每個 `<plugin>:0xLOCALID` ＋其 JSON 路徑 → `requires_<YYYYMMDD-HHMM>.txt`（沿用既有 `UniquePath` 的**永不覆蓋** `-2` 機制，`ext` 參數化）。掃描範圍＝`placements[]`(base/cell/ws)＋`removals[]`＋`overrides[]`＋`references[]`＋`annotations[]`＋整本 Captures。vanilla 五個＋CC 不算 vanilla，與 C# 同一套。
- **⚠️ 關鍵：只列真的會變成依賴的東西——而「假依賴」不只 `activeEffects` 一個。** 逐一讀 C# 消費端（`Generator.CapturedNpcs.cs`/`CapturedItems.cs`/`Build.*.cs`）確認出**六類**寫著 mod FormID 但 build 根本不 link 的欄位，全部排除（列進去＝說謊，刪掉它並不會拿掉那個依賴），並在 SKSE log 逐欄位交代排除筆數：① `capturedNpcs[].activeEffects[].magicEffect` **＋ `.source`**（同一個物件上的**第二個** ref 欄位，C# 註解只點名了前者）；② `capturedNpcs[].base`（一律鑄新、從不 override）；③ `capturedNpcs[].defaultOutfit`**當有 `worn` 裝備時**（穿的甲會鑄 OTFT 蓋掉它）；④ `capturedItems[].base` **當 `kind: ingredient`**（`IngredientSpec` 沒有 Template 欄位＝死資料）；⑤ `annotations[].cell`/`.worldspace`（inert，不生記錄）；⑥ `references[].base`/`.cell`/`.worldspace`（DLL 刻意不寫 `anchor` ⇒ 預設 `none` ⇒ build 連讀都不讀）。
- **兩種限定條件的相依加註記**：`[template clone]`（`capturedItems[].base`、附魔的 `inventory[].item`——ModForge **deep-copy** 那筆記錄，form 本身不是 master link，但複本拖著來源的 sub-link＋要用它的 mesh，實務上仍需要那個 mod）；`[named only]`（`references[].ref`——只有 spec 真的有東西指向那個 label 才成為 master）。
- **跟 C# 版的差異（各有得失）**：C# 以「建好的 mod」為 master 權威 ⇒ 連 deep-copy 拖進來的 master 都抓得到，但**歸因會掉**（`Causal()` 過濾掉 FormKey 沒被 link 的 spec 來源 ⇒ template clone 只剩 `record Weapon:MFCap_…`，講不出要刪哪一行）；DLL 沒有建好的 mod 可掃，改用**規則表** ⇒ 遇到全新欄位可能誤判（預設當作「會 link」＝寧可多報不漏報），但 **template clone 也講得出是哪一行**。
- **驗證**：`Analyze(scene, captures)` 刻意拆成**純函式**（不碰遊戲狀態），離線用 stub header 編**真的 `Requires.cpp`** 跑一份含 9 個 mod 的擬真測資 → 六類排除全部命中（`Conditional Expressions.esp`／`Immersive Armors.esp`／`Wyrmstooth.esp` 只出現在假依賴欄位 ⇒ **報告裡一個字都沒有**），而 `PROTEUS.esp`／`Ordinator.esp` 照樣列出、**歸因到真正拉它進來的那一行**（`spells[1]`／`perks[0].perk`，不是 activeEffects）。
- **C# 端零改動**（這是純 DLL 側的新輸出）。
- **🎮/🔬 驗收完成（2026-07-13）**——四項全過，這條結案：① 鈕在遊戲內跑出 `requires_20260712-2244.txt`（使用者 2026-07-12）；② **假依賴沒有污染名單**：那份 .txt 裡 7 個 mod **每一個都只由 `captures.capturedNpcs[].spells[]` 拉進來**，沒有一行是 `activeEffects`／`base`（尾註明講另有 138 筆「記錄但不 link」被刻意排除）；③ **跨端對帳一致**：把同一支角色的 `captures_20260712-2250.json` 餵 C# `build` → `<plugin>.requires.txt` 吐**同樣 7 個 mod**，link 數也對得上（DLL 24 links ＝ 兩個 capture × 12；C# 單 capture 12）；④ **C# 端也不說謊（一變數實驗）**：把 `Conditional Expressions.esp` 的那顆 spell 從 spells[] 刪掉、**保留它的 14 筆 activeEffects** → 該 mod 同時從 `requires.txt`（7→6）**和 esp 的 master list** 上消失 ⇒ activeEffects 確實零 link，兩端規則一致。（差異僅在**呈現**：C# 會把 activeEffects 行印在該 mod 底下當佐證，但不計 link、也不會讓只出現在那裡的 mod 上榜；DLL 則整個不印。無需修正。）

## ✅ 已做＋🎮 PASS（`isPlayer` ＋玩家 perk：從 bug 到「全收」，2026-07-12→13，DLL `dd7afd82`→`e19ad4ca`）

- **bug**（commit `eb6ae75`）：`Captures.cpp` 用 `actor->As<RE::PlayerCharacter>()` 判玩家身份——**該 cast 對任何 actor（含玩家）都必定回傳 nullptr**。`TESForm::As<T>()` 不是 `dynamic_cast`，是 `switch (GetFormType())`（CommonLibSSE `FormTraits.h`），每個 case 只肯從 FORM_TYPE 的具體類別**往 base 轉**；玩家 ref 的 form type 是 `kCharacter` → 具體類別 `Character`，而 switch 裡**沒有 `PlayerCharacter` case**（它沒有自己的 FORM_TYPE）⇒ 這是**向下轉型**、`is_convertible` false ⇒ 靜默 null，**編譯期就決定**（換 MSVC 也一樣）。⇒ `isPlayer` 永遠 false、玩家點的 perk 一顆都吸不到。改為**單例指標比對** `actor == RE::PlayerCharacter::GetSingleton()`。全 DLL 其餘 4 處 `As<>` 皆為 upcast／formtype 精確命中，安全。**可推廣的判準**：`As<T>()` 只在 T 是該 form type 具體類別的 **base** 時才有效——凡是想「往下」轉（`PlayerCharacter`、以及任何沒有專屬 FORM_TYPE 的類別）都會靜默失敗，必須改用單例／旗標／`Is*()` 判斷。
- **🎮 PASS（2026-07-13）**：`captures_20260713-2138.json` → `"isPlayer": true`、perk **12 → 26**，內容是**真的玩家點下去的單手樹 perk**（`Armsman00`/`Armsman20`/`FightingStance`/`Bladesman30`/`HackAndSlash30`/`BoneBreaker30` ＋ CC/DLC/mod 的 20 顆）——base 記錄不可能有這些。（原訂的 `0x0F2CAA` 錨點失效：使用者換了存檔，這隻角色是純單手戰士，restoration 沒點；單手樹那組是更硬的證據。）
- **隨即拍板 (b)「全收」（2026-07-13，DLL `e19ad4ca`）**：修好後那段是 **if/else**——玩家走 `addedPerks` 就**不再讀 base 的 12 顆管線 perk**（`AllowShoutingPerk`／`VampireFeed`／`AlchemySkillBoosts`／`DBWellFitted`＝ vanilla **Player 記錄專用**）。使用者拍板「玩家走 (b)，到時候讓 modforge 處理」⇒ 改成**兩個陣列都收、依 durable id 去重、同 perk 取高 rank**，與 masters 那條同一個原則（**橋端完全複製、不過濾；取捨留給消費端**）。消費端要不要過濾＝ [backlog](backlog.md)「仍未做」。**🎮 PASS 同日**（`captures_20260713-2222.json`）：base 12 顆全在（含 `AllowShoutingPerk`）、總數 **32 ＝ 12 ＋ 20 且零重疊** ⇒ 去重取高 rank 的合併正確。（該份少了單手樹那 6 顆＝**存檔換成 level 1 白紙角色**，沒 perk 點數可花，非退化。）


## ❌ 已撤回（rebind 重作，2026-07-12，DLL crc `378d3c6c`）——**實機仍失敗，遊戲內 rebind 整個放棄，改走 .ini**（見下一節）

> **這一節保留當歷史**：它記的是**第二次**（也是最後一次）想在遊戲內抓鍵的嘗試。診斷（面板不暫停遊戲 ⇒ 抓鍵在跟玩家還按著的移動鍵搶輸入）是對的，**但兩道防線加完，使用者實機回報「rebind 仍失敗」** ⇒ 使用者拍板：**這功能先拿掉，改用 .ini 設定檔**。UI 與 `Modes` 的抓鍵狀態機（`BeginRebind`/`HandleKeyUp`/`RebindArmed`…）與 `plugin.cpp` 的 key-up 轉發**全部移除**（不留死碼；要回頭看實作去 git `ddf6324`）。
>
> **留下來的兩個修正是對的、繼續活著**（它們跟 rebind UI 無關，是獨立的正確性 bug）：① SETT v7 讓鍵位**真的套用**（過去寫進 co-save 卻在讀取時丟棄）；② `kCapture`／`kReferrer` 兩個模式的鍵位 P5 之後**從沒進過 co-save**（漏寫），v7 補上。**鍵位的持久化機制是活的**，只是「在遊戲內改鍵」這個入口沒了。

- **真正的根因（不是「同一幀」）**：P5 實機（2026-07-11，見上「唯 rebind 捕捉到錯鍵（W）」）撞到的不是 backlog 猜的「armed 那一幀」同批次事件——`BeginRebind()` 由面板滑鼠點擊觸發（ImGui render pass），跟鍵盤 input-poll 批次不是同一次呼叫，物理上不可能同幀撞在一起。真正原因是**舊實作對 armed 後收到的第一個鍵盤 down-event 完全不設防**：不篩鍵、不等放開，來什麼綁什麼。而面板本身**不暫停遊戲**（`Editor.cpp` 的獨立證據：編輯模式當下 WASD/Alt 的 down-event 照樣灌進同一支 sink，見該檔 `0x11/0x1F/0x20/0x38` 那段 log 註記）——玩家點「Rebind」的手多半還在 WASD 上，下一個鍵盤事件十之八九是移動鍵的殘餘按壓，不是玩家真正要按的目標鍵。`UI.Settings.cpp` 原本的隱藏註解（"grabbed the wrong keys in-game (e.g. movement W)"）與此診斷一致——backlog 的直覺方向對，但機制描述（同幀）不準確。
- **修法**（`Modes.{h,cpp}`）：
  - **黑名單**（`Modes::IsBindable`）：armed 狀態下永遠不接受 WASD／Space／LShift／RShift／LCtrl／Tab／Enter／console 反引號──這批鍵碰到直接吞掉、不消耗 armed 狀態，並丟一個 `DebugNotification`「that key is reserved, press another」。這是主因的直接解藥：不管手有沒有還在 WASD 上，這些鍵永遠進不了候選。
  - **等放開才 commit**：符合條件的第一個 down 只記成候選（`g_rebindCandidate`），**必須同一顆鍵的 up-event** 才真正 `SetBind`——順手也堵死「armed 當下已經按住的鍵」這個理論路徑（`ButtonEvent::IsDown()` 只在 up→down 那一幀為真，早就按住的鍵本來就不會產生新的 down-event，這點驗證後 backlog 猜測在字面上不成立，但等放開仍是額外一層防呆，也讓面板能顯示「放開 X 確認」）。
  - Esc 取消（**沿用既有**——舊碼已經有，只是被隱藏而已，不是新做的）；`plugin.cpp` 的 `HotkeySink` 補收 key-UP 並轉給新的 `Modes::HandleKeyUp`。
  - `UI.Settings.cpp`：restore 每模式一顆 `Rebind##<mode>` 鈕＋目前鍵位文字；armed 時顯示「Rebinding <mode> -- press a key」／「-- release <key> to confirm」的黃字狀態列＋ `Cancel rebind` 鈕；rebind 中其餘模式的 Rebind 鈕 disable（`igBeginDisabled`），避免同時兩個 rebind 打架。
  - `CoSave.cpp`：SETT **v6→v7**。① 過去 5 個模式的鍵位欄位是**寫進去但讀出來直接丟掉**（防呆舊 bug——現在改成真的套用，並用 `Modes::IsBindable` 二次驗證，遇到保留鍵字節就當壞資料丟掉、退回 F11，不盲信存檔內容）；② `kCapture`／`kReferrer` 兩個模式的鍵位 P5 之後**從來沒進過 co-save**（漏寫，不是這次才有的 bug），v7 補上。
- **實機結果（同日）：仍失敗**。使用者回報「rebind 仍失敗」，並拍板「這太麻煩了，先隱藏掉這個功能吧，我們之後把他擺進 .ini 設定」。**兩次嘗試、兩種設計（來者不拒／黑名單＋按放開）都輸給同一件事**：面板不暫停遊戲，抓鍵永遠在跟玩家手上的鍵搶。**結論：遊戲內抓鍵這條路封掉，不再嘗試第三次。**

## ✅ 已做（動作鍵改走 `.ini` ＋ palette clear 鈕，2026-07-12，commit `1fffb15`，**已部署**〔DLL `dd7afd82`，含 `eb6ae75` 的 isPlayer 修正〕，待實機）

> 部署時序：編完當下使用者正在遊戲中 → `deploy.sh` 依設計拒絕（`pgrep SkyrimSE.exe`）；使用者關遊戲後已部署完成，**下次啟動遊戲就會吃到**。`SceneCaptureBridge.ini` 要**跑過一次遊戲**才會自動生成（收工時尚未生成，屬預期）。

**① 動作鍵＝ `SceneCaptureBridge.ini`（新檔 `src/KeyIni.{h,cpp}`）**

- **為什麼 ini 一定會贏過遊戲內抓鍵**：ini **沒有那條賽道可輸**——沒有 armed 狀態、沒有 input sink、沒有時序、沒有「玩家手還按在 WASD 上」。上一節那個根因在檔案面前不存在。
- **位置＝ SKSE 資料夾**（`…/My Games/Skyrim Special Edition/SKSE/SceneCaptureBridge.ini`），跟 palette store／所有匯出檔同一個資料夾。**刻意不放 `Data/SKSE/Plugins/`**（SKSE 慣例位置）：那裡在 MO2 mod 資料夾內，**重裝 zip 會把使用者的設定默默還原掉**（本 repo 的舊傷：memory「MO2 reinstall reverts manual pex」）。
- **缺檔自動生成**：帶完整鍵名表＋保留鍵說明的註解模板 → 功能自我說明，不必先讀文件。
- **值寫鍵名不寫 scancode**：`Modes` 內新增**單一 DIK 表**，`KeyName(code)`／`KeyCode(name)` 雙向（面板顯示、ini 讀寫共用同一份詞彙，round-trip 無損）。大小寫/空白不計（`NumPad 5`＝`numpad5`＝`num5`），另留 `0x57`／`87` 原始 code 逃生門。離線用同一份表跑過 round-trip＋人類拼法測試（全表 name→code→name、`numpad -`、`num5`、hex/dec、拒收不存在的鍵）。
- **保留鍵黑名單留著、換了身份**：從「抓鍵時的過濾器」變成**驗證器**——ini 寫了 WASD/Space/Shift/Ctrl/Esc/Tab/Enter/console 會被拒（log 講原因、該模式維持原鍵）。**舊存檔裡 rebind 時代綁壞的鍵也照樣過這關**（那正是 bug 過去能存活的路徑）。
- **`reload keys from ini` 鈕**（Settings 頁）：改完不必重開遊戲。重讀＝純重新套用（先 `ClearIniBinds` 再 parse，所以**刪掉一行真的會讓那個模式退回舊行為**，不是 merge 殘留）。
- **ini vs co-save 優先序：ini 贏**（`Modes::ApplyCoSaveBind` 一處決策）。理由：ini 是**使用者的設定**、co-save 只是**這個存檔的狀態**；「我改了 ini 卻沒生效」是不可接受的失敗模式。**ini 沒提到的模式**才吃存檔的值（→ 舊存檔優雅降級，不會被硬蓋成 F11），再沒有就 F11。`ResetDefaults()`（OnRevert）也會**重新套回 ini 的鍵**——設定不該被「載入一個沒有我們記錄的存檔」洗掉。
- **移除**：`Modes` 的 rebind 狀態機、`plugin.cpp` 的 key-up 轉發（input sink 從三層變**兩層**）、Settings 頁的 Rebind 鈕（改成唯讀鍵位表＋來源標記 `(ini)`／`(save / default)`）。

**② Palette `clear all slots` 鈕（使用者要求）**

- 防呆**兩道**：① 按一下先變 `really clear all N slot(s)?` ＋ `yes, clear`／`cancel`，**要再按一次**；② 清完出現 **`undo clear`**（session 內有效，整批回來並重寫回磁碟）。
- **為什麼比 `replace from file` 更需要防呆**：插槽是**落盤跨存檔**的（不像 eraser/override 是可 revert 的存檔狀態），清掉＝丟掉別的 playthrough 攢的東西；而且 clear **沒有**「載入的新檔」當補償——`replace` 至少換來一份新插槽，clear 是純損失。（`replace` 既有的防呆＝檔案不存在/無可用插槽就完全不動，照舊。）

## ✅ 已做（面板欄位一致化：bound field 重構 ＋ 四頁補齊 label／note ＋ 匯出，2026-07-14，commits `2c8705b`／`3898fe9`／…，**待實機**）

**起點是那個 🐞**：面板 buffer 與 registry 靜默分叉（2026-07-13 使用者實機發現）。修法沒有停在「補一個提交路徑」，而是把契約收成單一擁有者——因為六個面板各抄一份同樣的錯，是**結構**在漏，不是六個獨立疏忽。

**① `UI.Fields.{h,cpp}`（重構，commit `2c8705b`）**——一個 bound text field，兩條規則：
- **RULE 1**：buffer 只有在「那一格正被打字」時才准跟 registry 不同，其餘每一幀都從 entry 回種。靠的是 **ImGui 的不變式：同一時間只有一個 active item**——所以「正在編輯的那格」是**單一 id，不是集合**，一個 `g_active` 就夠。⇒ 面板在結構上**不可能**顯示 registry 沒有的值。
- **RULE 2**：Enter **與** deactivate-after-edit（點走／tab 出去）都提交（`ImGuiMCP::IsItemDeactivatedAfterEdit()`；已查證那不是空殼，是直呼 cimgui 的 `igIsItemDeactivatedAfterEdit`，同步進真 ImGui context）。
- **連帶**：buffer 失效（invalidation）這個**概念整個退場**——刪列／改名被拒／索引位移都自癒，`g_rows.erase()`／`g_slotBufs.clear()` 是**被刪掉不是搬家**。順帶治好同源的另一個謊：References 撞名被拒時，欄位現在會**彈回**已存的 label。
- **唯一例外**：Palette 的 slot **沒有身份**（無 seq，只能用索引定位），列表重排時進行中的編輯會落到別的 slot 上 ⇒ 保留一個明確的 `UI::ForgetEdits()`。Eraser／Overrides 則改用**耐久 id 的 hash** 當列 key，所以 undo/revert 上面一列不會讓下面全部錯位。
- marker 編輯視窗**不動**：它有明確 save／cancel，本來就不會靜默分叉。

**② 四本 registry ＋ 面板補齊 label／note（commit `3898fe9`）**——Eraser／Overrides 的 `Entry` 加 label/note（setter 鍵入耐久 id）；Captures 加 note（label 早有，但以前只能從 console `sc capp <label>` 設，現在面板開欄，打錯字不必重擷一次 NPC）；Palette::Slot 加 note——**它跟其他三本不同，是落盤到 palette json 的磁碟狀態**，所以 `save to file` 會把筆記一起帶走、活得比存檔久。co-save 升版：`'ERSR'` v2→v3、`'OVRD'` v1→v2、`'SCCP'` v9→v10（舊存檔讀不到＝空字串，照舊）。

**③ 匯出＋消費端（使用者拍板 (b)「加欄位、非破壞」）**——`removals[]` 的關鍵設計：**沒有 label/note 的移除仍是一個裸字串**，只有真的寫了東西才變成 `{ref, label?, note?}` 物件。⇒ 一般匯出**與以前逐位元相同**、所有舊 spec 照讀（跟 `requires[]` 同一條「沒話說就塌回簡寫」規則）。`overrides[]`／`capturedItems[]`／`capturedNpcs[]` 各加選配 note。ModForge 端 `RemovalSpec` ＋ converter（字串｜物件聯集，抄 `RequirementConverter`）。
- **`Requires` 掃描器不用改**：它的分類表 default 是「看起來像 ref 就算硬相依」，所以 `removals[].ref` 天然落在對的一格；筆記文字不像 ref，會被自然忽略。

## ✅ 已做（`sc ed` numpad **長按持續作用**，2026-07-14，DLL `c4460315`，**待實機**）

**訴求**（使用者 2026-07-13）：微調位置/角度時 numpad 按著不放就一直動，不用狂點。

**根因確認**：`plugin.cpp` 的 `if (!btn->IsDown()) continue;`——CommonLibSSE 的 `IsDown()` 定義就是 `IsPressed() && heldDownSecs == 0`，**只有按下的那一幀為真**。引擎其實每一幀都在派送該鍵的 ButtonEvent、`heldDownSecs` 一直累加（`ButtonEvent.h:25-29` 確認 `IsHeld()`／`HeldDuration()` 都在）——**料一直都在，是被那一行丟掉的**。

**做法（採 (b) 連續位移）**：
- sink 分流：`IsDown` → `Editor::HandleKey` / `Modes::HandleKey`（照舊，單按一步）；`IsHeld` → `Editor::HandleHold`。**只有編輯模式看得到 held 事件**——動作鍵絕不能因為手指多按了一秒就連發 60 次。
- **只有 nudge 鍵重複**（位移/旋轉/縮放）。`commit`(0)／`cancel`(.)／`select`(5/*)／per-axis revert **維持單發**。⚠️ **8/2 在移動模式是 nudge、在 rotate 模式是 per-axis revert**——同一個 scancode 兩種性質，所以 `IsNudgeKey()` 在 rotate 模式**拒絕**重複 8/2。
- 手感：**0.35s 死區**（所以單點仍是精準的一步，不會飄）→ 之後 `step × frameDelta × rate`，rate 從 8 steps/s 在 1.5s 內滾升到 40 steps/s。
- **frameDelta 不用跟引擎要**：兩幀之間 `heldDownSecs` 的差值就是它。`> 0.25s` 的跳躍（暫停/讀取/卡頓）直接丟棄，否則會把整段空窗一次補成位移＝物件瞬移。
- 重構副產品：一個 `Nudge(ref, code, steps)` 吃 **float 步數**，單點傳 `1.0`、長按每幀傳分數步——**tap 與 hold 走同一條路，不可能各自漂移**。scale 順手 clamp 到 [0.05, 10]（單點永遠碰不到 0，長按一秒就穿過去了，負 scale ＝ 壞掉的隱形物件）。

## ✅ 已做（**Browser 目錄 ＋ 世界內 ghost 預覽**＝CK 的 Object Window，2026-07-14，DLL `ba3e2089`，**已部署、待實機**）

**訴求**（使用者 2026-07-14）：「要編輯場景，那些山脈之類我都得先去用滴管吸取，不像 CK 直接給你一個列表、還能預覽你要擺的是啥。我想用 skyrim 的物品欄 ui，那也可以預覽物品。」

**🔴 可行性調查：「借用物品欄 UI」否決**（結論在 [backlog](backlog.md) 有完整版）。物品欄只吃**可攜帶**的 form type；**STAT/TREE/FURN/ACTI/MSTT（山脈、岩石、樹、建築、家具）進不了 inventory**——連 FULL name 都沒有（STAT 記錄＝EDID/OBND/MODL），沒 icon、沒重量價值，ItemCard/SkyUI 的資料源整套對不上。**最需要的那一類正好不支援 ⇒ 死路**。改用面板（本來就是完整 Dear ImGui）＋**世界本身當預覽窗**。

**使用者拍板**：目錄**先 runtime**（之後再考慮補離線 catalog）、預覽**先做 ghost**（面板內 3D 之後再 spike）。

**🔴 架構級的坑：runtime 沒有 EditorID**。`TESForm::GetFormEditorID()` 預設 `{ return ""; }`——SSE 不把 EDID 留在記憶體。CELL/WRLD 有留（所以 `SceneExporter.cpp` 那幾處能用），**STAT/ACTI/FURN 全是空字串**。⇒ 索引改建在**永遠拿得到**的 `TESModel::GetModel()` 上，而模型路徑其實是**更好的鍵**：搜 "mountain" 直接命中 `Landscape\Mountains\*.nif`，不必知道任何 EditorID。

**做了什麼**

- **`Catalog`（新）**：`TESDataHandler::GetFormArray(FormType)` 掃 21 種可擺放的 form type（**不含 actor**——cell 匯出本來就不帶 actor，NPC 走 marker／`sc cap`），建成 {base, type, durable id, plugin, name, model} 索引，**懶建**（第一次開頁才掃）。兩種**主動剔除**：① 無 durable id（runtime-only base，擺了也匯不出去，跟滴管同樣的拒收）；② **無模型路徑**（會擺出一個**隱形物件**——那個經典的 wrong-nif 陷阱）。搜尋＝空白分隔 AND 詞組，比對 name＋model＋id。
- **`Preview`（新）＝ghost**：選中的 base 直接**生在你的瞄準點**（真尺寸、真光照、真位置——比 CK 那個小預覽窗更接近成品）。非碰撞（`SetCollisionLayer(kNonCollidable)`，延後到 3D 載入後，同 `Physics::FreezeDeferred` 那招）——**這不是美觀問題**：瞄準是物理射線，實心 ghost 會擋住自己的射線，瞄準點每幀往玩家爬。凍 havok、跟著準心走（可關）、面板可調 yaw/scale。
- **`UI.Browser`（新頁）**：搜尋框＋type／plugin 兩個下拉＋清單（**上限 500 筆並明講截斷**，不用 ImGui clipper——不想賭 wrapper 的 struct layout）。按鈕：`preview here`／`clear preview`／`add to palette`／`place here (real)`。
- **擺放路徑收成單一入口**：`Palette::PlaceSelected` 抽出 `Palette::PlaceSlot(slot, posOverride)`，`sc pl` 與 ghost commit **走同一條**——physics（`py0/py1`）、extra data（`ed0/ed1`）、placed 登記簿、匯出契約**結構上不可能分叉**。commit 用 ghost 的**當下姿態**（不重新瞄準：你看到的就是你放的），且 **ghost 留著** ⇒ 種一排樹＝同一顆鍵按五下。
- **`sc pl` 動作鍵**：有 ghost 就 commit ghost，沒有才擺 palette 選中的 slot（面板顯示什麼，鍵就做什麼）。
- **`add to palette`**：目錄是**全部**，palette 是**你留下來的那幾個**（磁碟持久、跨存檔）——新增 `Palette::AddSlot`。

**🔴 唯一不能出錯的地方：ghost 絕不能被匯出**。匯出器的判準是「dynamic ref ＝ 玩家放的 ⇒ 匯出」（`SceneExporter.cpp:174`），而 ghost 正是 dynamic ref。**兩層防護，第二層才是關鍵**：
1. **live handle**——便宜、精準，但一離開 session 就沒了。
2. **ref 自帶的哨兵**（`ExtraTextDisplayData`，savegame 會連著 created ref 一起序列化）。玩家開著 ghost 快存、明天讀回來 ⇒ 我們的 registry 是空的、handle 是死的，**但 `IsGhost()` 還是認得出來**，因為證據長在 ref 上、不在我們的記憶裡。**能從世界重建的狀態，勝過必須記住的狀態。**

同一道閘接進**所有**會抓 ref 的地方（Palette 滴管／Eraser／Editor／Captures／Referrer／SceneExporter），語氣一致：「那是預覽，它不在那裡」。另外兩道清理：**離開 cell 就即刻銷毀 ghost**（Update 每幀比對 parent cell——否則舊 cell 會留一座孤兒山）、**kPostLoadGame 掃掉存檔裡的孤兒 ghost**（`SweepOrphans`，靠哨兵認）。co-save **零改動、零版本 bump**（哨兵在 ref 上，不需要我們存任何東西）。

**ModForge C# 端零改動**——沒有新 spec 欄位，ghost commit 走既有 `placements[]`。
