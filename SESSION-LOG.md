# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

> **條目格式**：每條只留**一行 open 狀態 + 指向細節的連結**（設計決策/修了什麼落到該工作流或 sub_proj 文件、已落地功能進 [landed](workflows/feature-dev/landed/README.md)、待你驗的進 [WAIT_USER](WAIT_USER.md)）。完成即整條刪除。

## 最新進度

- 目前無跨工作流的 open 項；各工作流的 open 狀態見下表，不屬任何工作流的 open 項見最末節。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [workflows/feature-dev/session-log](workflows/feature-dev/session-log.md) | 🧊 身份系統 ③ 聲望/行為追蹤（2026-06-22 冷凍，等很有空再做）|
| 重構整理 | [workflows/refactor/session-log](workflows/refactor/session-log.md) | 無 |
| 調查／解碼 | [workflows/investigation/session-log](workflows/investigation/session-log.md) | 無 |

## 不屬任何工作流的進度（堆太多 → 拆進 `session_logs/`）

- **⚠️ 離線測試套件在真離線機（無 Skyrim.esm）是紅的——11 個測試漏標 `RequiresSkyrim`**（2026-07-09 發現，**待決定修法**）。`Category!=RequiresSkyrim` 跑 861 個 → **850 過、11 敗**：10 個 `LivingNpcTests` + 1 個 `SettlementTests.Build_SleepLocationResolvesToInSpecBedAnchor`。根因：這些測試把 ACHR/marker build 進 **vanilla cell**（Riverwood/Whiterun 旅館等），`TestBuild.Ok`（`tests/…/Helpers.cs:18`）把「master 'Skyrim.esm' not found／vanilla cell unresolved」warning 當失敗。**主力機有 Skyrim.esm → 零 warning → 一直是綠的**，所以從沒被發現；這台 fresh clone 是首次在真離線機上跑才暴露。**影響**：CLAUDE.md 鐵律①「改完跑離線測試」在離線機**目前無法達成**，會擋掉在公司碰原始碼的工作。**修法選項**：①（推薦、convention-correct）照兄弟測試（`NpcPatchTests`/`RemovalsTests`/`MapMarkerTests` 已標）給這 11 個補 `[Trait("Category","RequiresSkyrim")]` → 離線套件轉綠，但損失這些 macro 展開的離線覆蓋；②外科修 `TestBuild.Ok`（偵測 Skyrim.esm 缺席時寬容 master-not-found warning）保留覆蓋，但部分測試斷言可能仍需 Skyrim.esm、且動共用 helper。使用者 2026-07-09 選「先只記錄、回家再決定」。
- **Idea #23 living-adventurers**：剩主力機 `package`（編全部 .pex：spike/P1/canonical + 互動 setGlobal TIF）+ 實機驗收（P0–P3 整鏈第一次 runtime，共同 acceptance gate）——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/ingame-tests.md](wait_todo/ingame-tests.md)。設計/進度 → idea [#23](workflows/idea/living-adventurers.md)、sub_proj [README](sub_projs/living-adventurers/README.md) + [design.md](sub_projs/living-adventurers/design.md)。
- **Idea #20 in-world 技能樹**：Phase 0 離線完備 + .pex 已編交付，剩實機驗收——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/roadmap-features.md](wait_todo/roadmap-features.md)。sub_proj [inworld-skill-tree](sub_projs/inworld-skill-tree/README.md)。
- **darksouls-port（DS1 北方不死院 → Skyrim worldspace）**：P1「空殼院」離線完成、`DSPortP1.zip` 已交付（2026-07-06），**剩實機驗收**（進場指令與三段驗收見 [wait_todo/ingame-tests.md](wait_todo/ingame-tests.md)）；規劃與 P1 結論 [plan.md](sub_projs/darksouls-port/plan.md)。
- **Idea #19 Godot Worldspace Editor**：整鏈已落地（[landed/world](workflows/feature-dev/landed/world.md) +「Godot 編輯器 WYSIWYG」條 / [godot-editor](workflows/feature-dev/landed/godot-editor.md)），剩非阻塞小尾巴——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/worldspace-editor.md](wait_todo/worldspace-editor.md)。
- **Idea #24 遊戲內編輯器**：**ModForge 生成端已完備**（scene.json → patch：placements 含 rotation/scale/位移、mapMarker、hazard、keyword、role macro〔blacksmith 問候+行為+交易，實機確認〕、`removals[]` 橡皮擦）——落地見 [landed/dialogue-quests](workflows/feature-dev/landed/dialogue-quests.md) +「§D/§E」、[landed/world](workflows/feature-dev/landed/world.md)「removals」、設計 [spec](workflows/specs/ingame-scene-export-design.md) / plan [ingame-scene-export](workflows/plans/ingame-scene-export.md)。**下一大塊＝runtime 採集橋 SKSE DLL**（走訪 cell/滴管取樣/FormID 反解/吐 scene.json，C++ 獨立子專案，scene.json 契約＝它的 output 目標）——**子專案骨架 + `SceneExporter` 實作 stub 已離線落地**（2026-07-09，`sub_projs/scene-capture-bridge/`；建置架構改編自使用者提供的參考 repo my_skyrim_plugin_1，只借 build stack、邏輯自寫）：cell 走訪→placements/npcRefs、FormID→`<plugin>:0xLOCALID` 反解、scene.json 序列化（nlohmann）皆成形，多處 `TODO(runtime-verify)`。**首編已過**（2026-07-10 主力機 clang-cl 跨編譯，產物 `build/release-clang-cl-linux/SceneCaptureBridge.dll`；缺的是 preset 依賴的 `ports/` overlay，非 MSVC。ESL local-id 與 vanilla diff 兩個 TODO 已拆，見 spec）。**P1 統一 marker 系統 MVP 實機閉環（2026-07-10，含目視山羊——玩家標記→json→agent authoring→build→世界改變）**；plan 重寫為 P1–P4：ModForge `annotations[]` 落地（advisory only，865 測試綠）；DLL Markers 模組（F9 腳下放 marker、面板改名/kind/刪、匯出 annotations、ExportCell 排除 proxy）；工具 esp `SceneCaptureTools.esp`（dogfood，`SCB_MarkerACTI`＝vanilla 召喚圈模型；DLL 無 esp 時 fallback vanilla base）。殘項僅 F11 複核＋讀檔 prune 複核（[wait_todo/ingame-tests.md](wait_todo/ingame-tests.md)）；下一步＝P1 Task 4 指向性放置 或 P2 靜態富路徑。細摳全記錄於 [plan](workflows/plans/scene-capture-bridge.md)；PROTEUS 相關凍結待使用者重規劃。順帶發現 `examples/spec.schema.json` 既有漂移（`removals`/`npcRoles` 缺，本次只補 `annotations`）——待補。前情：M4 spike 實機全過（2026-07-10）：DLL 載入、vanilla diff（Bannered Mare 717 refs 全跳過、placements=0）、玩家擺放只採到玩家擺的、interior 座標確認 cell-local、`scene.json` → `build` 整鏈閉環。**clang-cl 跨編譯產物可直接實機**。剩 §B/§D/§E 遊戲內編輯 UI，以及路徑 A（PROTEUS clone）的 ref/base 耐久性——PROTEUS 已降為可選，預設走 ModForge 直接生 NPC 的「大眾臉」路徑。
