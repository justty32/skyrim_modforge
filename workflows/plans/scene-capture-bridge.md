# 遊戲內編輯器（scene-capture-bridge）— Implementation Plan

← [plans](README.md)｜spec：[ingame-scene-export-design.md](../specs/ingame-scene-export-design.md)（共用）｜idea：[#24](../idea/tools/24-ingame-editor.md)（核心宗旨見其頂部 2026-07-10 重述）｜子專案：[scene-capture-bridge](../../sub_projs/scene-capture-bridge/README.md)

**（2026-07-10 細摳收斂後從頭重寫；取代同日早先的 M6–M10 版本，舊 M 編號在「路線圖」表保留對照）**

**Goal:** 把 Skyrim 自身變成 Creation Kit 的第一步。遊戲內 plugin＝**薄記錄器**（施法/快捷鍵 → 記座標/標籤/意圖 → 吐 json）；實際的生成工作全歸 ModForge / AI agent。MVP＝**統一 marker 系統**：玩家在世界裡標「這裡放什麼」，agent 讀 json 拿座標＋標籤去 authoring。

---

## 已定調的裁決（全部出自使用者，2026-07-10 細摳）

| 議題 | 裁決 |
|---|---|
| **MVP 骨幹** | **統一 marker 流**：指向性法術打 marker → 互動/GUI 改標籤 → 匯出 json → ModForge 讓 AI 在 marker 上擺東西。NPC/生物/leveled encounter/動態物件**初版全走這套**，額外操作之後再說 |
| 地形高度 | 遊戲內**不做**雕刻（runtime 無法變形 LAND，也不太需要）。走離線路（Godot editor / PNG heightmap → ModForge，生成端已落地）。遊戲內只放標註 marker 給 agent 下指令 |
| leveled encounter | 要。走 marker 流（`placeatme` 會立即抽選，故不生成、只記座標＋標籤，base 由 agent 在 spec 指定 LVLN） |
| area / trigger / event | 以後再說 |
| 靜態物件富路徑 | UX 已定稿（見附錄・細摳②）：新增/修改/刪除、輪廓確認流、numpad 編輯模式。**排在 marker MVP 之後** |
| 動態物件富路徑 | 同靜態＋物理凍結（見附錄・細摳③）。初版先走 marker 流 |
| 刪除的狀態模型 | 記憶體（session）＋面板 `Adopt disabled refs` 掃描鍵（明示優於推導） |
| 擦到外部 mod 的 ref | 允許，面板醒目標示「會讓 patch 依賴 X.esp」 |
| 真刪除語意 | 刪掉自己新增的＝無痕跡（不進 removals[]、不留登記簿、檢視法術不顯示） |
| **PROTEUS 相關** | **凍結**——使用者將重新規劃，本 plan 一律不涵蓋、不假設 |

## 已落地的地基（實機確認 2026-07-10）

- `SceneCaptureBridge.dll`：clang-cl 跨編譯、遊戲載入正常；F10 匯出、F1 ImGui 面板（SKSE Menu Framework 3，軟相依）。
- **vanilla diff**：authored ref 跳過、玩家 `PlaceAtMe` 的 emit。Bannered Mare 717 refs 全 pre-existing、`placeatme` 兩個後 placements=2。
- 座標契約全條目結案；`scene.json` → `build` → patch esp 整鏈閉環。
- 生成端既有能力：`placements[]`（含 ACHR）、`removals[]`、mapMarker/hazard/keyword、npcRoles macro、heightmap→LAND、programmatic navmesh。

## 路線圖

| Phase | 內容 | 舊編號對照 | 狀態 |
|---|---|---|---|
| **P1** | **統一 marker 系統（MVP 骨幹）** | 原 M9（升格） | **本 plan 的 Task 層級主體** |
| P2 | 靜態物件富路徑：刪除／新增／修改編輯模式 | 原 M6／M7a／M7c | UX 定稿（附錄・細摳②），等 P1 |
| P3 | 動態物件富路徑（物理凍結）＋檢視法術 | 原 M7d | UX 定稿（附錄・細摳③），等 P2 |
| P4 | 範圍吸取 | 原 M8 | 概要 |
| 後排 | role tag（原 M10；其 PROTEUS 路徑凍結）、navmesh 記點、area/trigger/event、滴管移動既有 ref（原 M7b，等 override 契約） | — | — |

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

## Task 4：指向性放置（依 Task 0 裁決）

- [ ] A 案：工具 esp 的符文式法術（爆炸 PlacedObject＝我們的 MarkerACTI）→ dynamic proxy 出現在命中點 → DLL 認領進登記簿（TESActivateEvent 或輪詢新 ref of our base）。
- [ ] B 案：concentration 法術當模式開關 + DLL 射線取命中點 `PlaceAtMe`。
- [ ] 法術美學是使用者願景語彙——就算 B 案，spell 殼還是要有（工具 esp）。

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

- **P2 刪除**：原 M6 plan 全文有效（資料流/Task 0–5/驗收見 git 歷史 `62b9724` 前版本；開工時搬回）。補細摳②的紅輪廓確認流與 GUI 列表路徑（自己加的單獨一掛、最新在前）。
- **P2 新增**：palette（GUI 選物）＋指向性落點＋綠輪廓確認；持續施法變體（法術＝模式開關、`CrosshairPickData` 每幀輪詢——不需 projectile hook、繞開 STAT 不吃魔法效果）。
- **P2 修改**：numpad 編輯模式（`2468` 位移/`1379` 旋轉/`+−` 縮放/`*/`+`5` 選屬性/`Enter`/`0` 結束）；編輯模式吞按鍵（框架 `AddInputEvent` 回傳 bool block）；進場快照 transform 供 cancel。**開工前拍板 override 契約形狀**（`overrideOf` vs `overrides[]`，spec 已載兩案）——偵測已由「選中且動過＝明示登記」天然解決。
- **P3 動態物件**：選中凍結物理（`SetMotionType(Keyframed)` 待驗）→ 編輯 → 回復。誠實邊界：匯出 authored 位置，載入後 havok 自行沉降（與 CK 慣例一致）。檢視法術（新增/修改/刪除/全部四種；顯示被刪的 vanilla ref 需暫時 enable 或臨時紅 marker，實作時定）。
- **P4 範圍吸取**：`ForEachReferenceInRange` bound 半徑；面板先預覽「會吸到 N 個」再確認。

---

# 附錄：細摳記錄（需求原文，2026-07-10）

## 細摳②：靜態物件 UX 定稿

「實現這些功能的程式碼就是那樣，**重點是操作方式**。」新增／修改（transform＋屬性：火把燃燒、門開關…）／刪除。

- **新增**：GUI 選物→指向法術落點（最簡）；或先放 marker→GUI 選「在那生成」。生成先出**綠色半透明輪廓**→幾秒或再施法→選單確認→OK 才實際生成。
- **刪除**：法術點選，或 GUI 列出 cell 內靜態物件（**我們新增的單獨一掛、最新在前**）。先變**紅色半透明輪廓**→確認。**色盲**：提供色彩調整（工具 esp 多套 EffectShader，面板切換）。
- **持續施法變體**（新增/刪除共用）：指哪打哪即時輪廓，收法選定最後命中者。
- **修改**：法術選中→**泛光**（勿半透明，與刪除區隔）→numpad 編輯＋屬性 GUI→`Enter`/`0` 結束。
- 設計後果：①「選中且動過」＝明示登記，解掉移動偵測問題；②屬性列表**只列能存活到 esp 的**（`PlacementSpec` 已有 Lock/Ownership/Count/InitiallyDisabled/EnableParent/LinkedRefs/Teleport；門「預設開啟」與火把燃燒待查，可能 enable-parent 對偶）；③輪廓/泛光 shader 機制待驗。

## 細摳③：動態物件＋檢視法術＋真刪除

- 動態物件同靜態流程，選中時**喪失物理**（光照渲染保留，細節實作時議）、結束回復。
- 檢視法術：持續施展顯示編輯痕跡，**四種：新增/修改/刪除/全部**；結束編輯後平時不顯示。
- **真刪除**：刪自己新增的＝徹底無痕（不進 removals、不留登記簿、檢視法術不顯示）。

## 細摳④：NPC/生物/leveled——全走 marker 流

「npc、生物這些暫時先不用弄太細，剩下都可以用同一套『指向性法術打 marker → 互動/GUI 改標籤 → 輸出 json → ModForge 讓 AI 在 marker 上擺東西』完成，甚至動態物件都可以先用這套。」leveled encounter 要（marker 流下 `placeatme` 抽選坑不存在；palette 直擺路徑的坑記錄於 git 歷史，日後富路徑再取用）。

---

# 驗證清單（彙總，未驗不寫進碼）

| 項 | 影響 | 驗法 |
|---|---|---|
| proxy 可見 base 的模型路徑 | P1 Task 0 | houseCARL/`find` 對 Skyrim.esm |
| `ExtraTextDisplayData` 可寫 | label 跨 session 免費復原 | grep header + 實機 |
| 各 hotkey scancode | P1/P2 | `GetIDCode()` log 實測 |
| 符文放置面限制 vs `bhkPickData` | P1 Task 4 | 工具 esp 試 + grep header |
| `TESObjectREFR::Delete()` 存在性 | 真刪除實作 | grep header（`Disable`/`Enable`/`IsDisabled` 已確認） |
| `SetMotionType(Keyframed)` C++ 對應 | P3 物理凍結 | grep header |
| EffectShader 綠/紅半透明＋泛光 | P2/P3 視覺 | 工具 esp 試 |
| 門「預設開啟」flag、火把燃燒的記錄表示 | P2 屬性列表 | houseCARL 讀 vanilla 記錄 |
| base=LVLN 的 placement 生成 | 富路徑 | grep `Generator.Build.Placements` |
| `data.location` 註解（authored vs live） | `SceneExporter.cpp` 待修註解 | 已知 `GetPosition()`≡`data.location`（`TESObjectREFR.h:405`） |
