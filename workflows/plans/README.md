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
| [worldspace-editor.md](worldspace-editor.md)（heightmap → 非平坦 LAND，7 task） | 2026-06-16 | [specs/worldspace-editor-design.md](../specs/worldspace-editor-design.md) | Task 1–6 落地（552 測試綠）；Task 7 待主力機 |
| [ingame-scene-export.md](ingame-scene-export.md)（scene.json → patch；ModForge 側 M0–M2，7 task） | 2026-07-08 | [specs/ingame-scene-export-design.md](../specs/ingame-scene-export-design.md) | **M0–M2 落地（IN-GAME 確認 2026-07-08）**——npcRoles macro + 外部-speaker Hello，白漫 Carlotta 講鐵匠問候；落地記錄 [landed/dialogue-quests](../feature-dev/landed/dialogue-quests.md)。後續：vendor faction-add / `removals[]` / M3–M5 runtime |
| [scene-capture-bridge.md](scene-capture-bridge.md)（採集橋 runtime 工具：橡皮擦／滴管／範圍吸取／語意標記／role tag，M6–M10） | 2026-07-10 | [specs/ingame-scene-export-design.md](../specs/ingame-scene-export-design.md)（共用） | **規劃中**——M4 spike + 面板已 IN-GAME（[landed/world](../feature-dev/landed/world.md)）；M6 橡皮擦待審 |

action-system asset/config 生成（OAR/BDI/PIE）7 個 task 全落地（2026-06-14，547 測試綠燈），計畫已移 [archive/](archive/README.md)；落地記錄見 [feature-dev/landed/infra.md](../feature-dev/landed/infra.md)。

新計畫命名 **`<功能>.md`（不含日期）**，日期記在現役 index 表的一欄；落地後即移 [archive/](archive/README.md)。（archived 舊檔仍保留歷史日期前綴、凍結不動。）

> 已落地、被取代的舊實作計畫見 [archive/](archive/README.md)（保留脈絡、不在維護鏈）。本入口檔若膨脹，照 [DEV-GUIDE「結構整理原則」](../../DEV-GUIDE.md) 拆。
