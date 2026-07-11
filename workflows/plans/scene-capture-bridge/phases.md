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
