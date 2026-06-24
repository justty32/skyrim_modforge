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

| 設計方案 | 討論日期 | 對應 idea/roadmap | 狀態 |
|---|---|---|---|
| [worldspace-editor-design.md](worldspace-editor-design.md)（heightmap → 非平坦 LAND，後端 MVP） | 2026-06-16 | [Idea #19](../idea/worldspace-editor/README.md) | 設計完成，VHGT 格式已查證；待出 plan |
| [settlement-population-design.md](settlement-population-design.md)（`settlements:` 聚落量產 macro，人口填充） | 2026-06-24 | [Idea #22](../idea/world-building.md#22-漂泊開拓慢活移動基地--程序生成異域--開拓經營) · [roadmap 🏘️](../roadmap/mod-survey-gaps.md) | 設計完成（MVP=具名住民+靜態 ACHR+綁錨點作息+vendor）；待討論開放問題→出 plan |

action-system asset/config 生成（OAR/BDI/PIE）MVP 已落地（2026-06-14），design 已移 [archive/](archive/README.md)。

新設計命名 **`<功能>-design.md`（不含日期）**，日期記在現役 index 表的一欄；對應 [plans/](../plans/README.md) 同名 plan，落地後即移 [archive/](archive/README.md)。（archived 舊檔仍保留歷史日期前綴、凍結不動。）
