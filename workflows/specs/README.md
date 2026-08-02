# specs — 設計方案（工作流入口）

← [INDEX](../../INDEX.md)｜[CLAUDE.md](../../CLAUDE.md)

一個 idea / roadmap 項**認真討論後產出的設計方案**：目標、架構、資料流、權衡、取捨。這是本工作流的 **入口**。

階梯：[idea](../idea/ideas.md)（不確定要不要做）→ [roadmap](../roadmap/README.md)（會做、不確定何時）→ **spec（討論後的方案）** → [plan](../plans/README.md)（動工前的詳細實作規劃）→ build。

- 本夾 `*.md` = 各功能的設計方案（檔名 **`<功能>-design.md`，不含日期**——日期記在下方 index 表）。
- 對應的逐步實作在 [plans/](../plans/README.md)。
- 設計脈絡的 idea 雛形在 [ideas](../idea/ideas.md)。
- 設計涉及 code 結構/慣例時參考 [common/conventions](../common/conventions.md)；橫向通則見 [DEV-GUIDE](../../DEV-GUIDE.md)。

> **archive**：已落地、被取代的舊設計方案封存進 [archive/](archive/README.md)（保留脈絡、不在維護鏈）。本入口檔若膨脹，照 [DEV-GUIDE「結構整理原則」](../../DEV-GUIDE.md) 拆。

## 現役設計方案

> **狀態不記在這**：一份 design「在本夾＝現役、在 [archive/](archive/README.md)＝落地」由位置隱含；已出 plan 與否／落地進度以 [plans/README.md](../plans/README.md) 的表為唯一 source of truth。

| 設計方案 | 討論日期 | 對應 idea/roadmap |
|---|---|---|
| [worldspace-editor-design.md](worldspace-editor-design.md)（heightmap → 非平坦 LAND，後端 MVP） | 2026-06-16 | [Idea #19](../../../godot-worldspace-editor/README.md) |
| [ingame-scene-export-design.md](ingame-scene-export-design.md)（遊戲內蓋城鎮 → scene JSON → patch；ModForge 側契約 + 最小切片 M0–M2） | 2026-07-08 | [Idea #24](../idea/tools/24-ingame-editor.md) |

action-system asset/config 生成（OAR/BDI/PIE）MVP 已落地（2026-06-14），design 已移 [archive/](archive/README.md)。

> **命名不含日期 + 落地即 archive** 的完整規則見 [plans/README.md](../plans/README.md)（兩夾共用；spec 檔名對應 `<功能>-design.md` ↔ plan `<功能>.md`）。
