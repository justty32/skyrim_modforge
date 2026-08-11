# plans — 實作規劃（工作流入口）

← [INDEX](../../INDEX.md)｜[CLAUDE.md](../../CLAUDE.md)

**真的要動工前**的詳細實作規劃：精確到哪個檔、什麼 code、什麼測試步驟（bite-sized task + 驗證）。這是本工作流的 **入口**。

階梯：[idea](../idea/ideas.md) → [roadmap](../roadmap/README.md) → [spec（討論後方案）](../specs/README.md) → **plan（動工前詳規）** → build。

- 本夾 `*.md` = 各功能的逐步實作計畫（檔名 **`<功能>.md`，不含日期**——日期記在下方 index 表）。
- 對應的設計方案在 [specs/](../specs/README.md)（命名對應：`<功能>.md` ↔ `specs/<功能>-design.md`）。
- 計畫要遵守的**程式碼慣例 + CODE_MAP 維護鏈**見 [common/conventions](../common/conventions.md)；橫向通則見 [DEV-GUIDE](../../DEV-GUIDE.md)。

## 現役計畫

| 計畫 | 出計畫日期 | 對應 spec | 狀態 |
|---|---|---|---|
| [worldspace-editor.md](worldspace-editor.md)（heightmap → 非平坦 LAND，7 task） | 2026-06-16 | [specs/worldspace-editor-design.md](../specs/worldspace-editor-design.md) | **Task 1–7 皆完成**；2026-08-11 重跑 Tamriel LAND round-trip PASS |
| [ingame-scene-export.md](ingame-scene-export.md)（scene.json → patch；ModForge 側 M0–M2，7 task） | 2026-07-08 | [specs/ingame-scene-export-design.md](../specs/ingame-scene-export-design.md) | **M0–M2 落地（IN-GAME 確認 2026-07-08）**——npcRoles macro + 外部-speaker Hello，白漫 Carlotta 講鐵匠問候；落地記錄 [landed/dialogue-quests](../feature-dev/landed/dialogue-quests.md)。後續：vendor faction-add / `removals[]` / M3–M5 runtime |
| [scene-capture-bridge/](scene-capture-bridge/README.md)（遊戲內編輯器：P1 統一 marker MVP → P2 靜態富路徑 → P3 動態＋檢視 → P4 範圍吸取；2026-07-11 膨脹拆成 README/phases/backlog/appendix 四檔） | 2026-07-10（同日細摳收斂後重寫） | [specs/ingame-scene-export-design.md](../specs/ingame-scene-export-design.md)（共用） | **P1–P6 主線 2026-07-11 實機全過**（[landed/world](../feature-dev/landed/world.md)）；未做項與新想法見 [backlog](scene-capture-bridge/backlog.md)；PROTEUS 相關凍結待使用者重規劃 |
| [captured-npcs-consumption.md](captured-npcs-consumption.md)（擷取器 ② NPC 外貌：capturedNpcs[] → NPC_ 記錄＋placement） | 2026-07-11（同日細化至動工級 T1–T6） | [specs/ingame-scene-export-design.md](../specs/ingame-scene-export-design.md)（共用） | **Phase 1 IN-GAME 確認（2026-07-11，Mirabelle 分身原地出現；[landed/npcs](../feature-dev/landed/npcs.md)）**——Phase 2＝烘焙臉（未排，要動工開新 plan；界線與 faceMorph 映射表留本檔）|
| [player-capture-capp.md](player-capture-capp.md)（`sc capp <label>` 直接吸玩家＋顯式數值/技能，去 PROTEUS 化；SCCP v9） | 2026-07-11 | [specs/ingame-scene-export-design.md](../specs/ingame-scene-export-design.md)（共用） | **已實作，外貌實機 PASS**；數值與玩家 perk 待練過的角色實機對帳 |
| [navmesh.md](navmesh.md)（編輯 vanilla cell 的導航網格） / [P3 實作](navmesh-p3.md) | 2026-07-12 / 2026-08-11 | [navmesh-patch-design](../specs/navmesh-patch-design.md) | P0/P1/T2.0/P3 皆實機 PASS；P3 兩名相反方向 Travel actor 均跨過新增↔vanilla seam |

action-system asset/config 生成（OAR/BDI/PIE）7 個 task 全落地（2026-06-14，547 測試綠燈），計畫已移 [archive/](archive/README.md)；落地記錄見 [feature-dev/landed/infra.md](../feature-dev/landed/infra.md)。

新計畫命名 **`<功能>.md`（不含日期）**，日期記在現役 index 表的一欄；落地後即移 [archive/](archive/README.md)。（archived 舊檔仍保留歷史日期前綴、凍結不動。）

> 已落地、被取代的舊實作計畫見 [archive/](archive/README.md)（保留脈絡、不在維護鏈）。本入口檔若膨脹，照 [DEV-GUIDE「結構整理原則」](../../DEV-GUIDE.md) 拆。
