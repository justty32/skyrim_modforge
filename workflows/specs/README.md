# specs — 設計方案（工作流入口）

← [INDEX](../../INDEX.md)｜[CLAUDE.md](../../CLAUDE.md)

一個 idea / roadmap 項**認真討論後產出的設計方案**：目標、架構、資料流、權衡、取捨。這是本工作流的 **入口**。

階梯：[idea](../idea/ideas.md)（不確定要不要做）→ [roadmap](../roadmap.md)（會做、不確定何時）→ **spec（討論後的方案）** → [plan](../plans/README.md)（動工前的詳細實作規劃）→ build。

- 本夾 `*.md` = 各功能的設計方案（檔名 **`<功能>-design.md`，不含日期**——日期記在下方 index 表）。
- 對應的逐步實作在 [plans/](../plans/README.md)。
- 設計脈絡的 idea 雛形在 [ideas](../idea/ideas.md)。
- 設計涉及 code 結構/慣例時參考 [common/conventions](../common/conventions.md)；橫向通則見 [DEV-GUIDE](../../DEV-GUIDE.md)。

> **archive**：已落地、被取代的舊設計方案封存進 [archive/](archive/README.md)（保留脈絡、不在維護鏈）。本入口檔若膨脹，照 [DEV-GUIDE「結構整理原則」](../../DEV-GUIDE.md) 拆。

## 現役設計方案

**目前無現役設計方案**——已落地的 design 一律移進 [archive/](archive/README.md)（凍結、不在維護鏈、不套拆檔門檻）。下一份待「身份系統 ③ 聲望/行為追蹤」開始討論時才產出（見 [roadmap](../roadmap.md)）。

新設計命名 **`<功能>-design.md`（不含日期）**，日期記在現役 index 表的一欄；對應 [plans/](../plans/README.md) 同名 plan，落地後即移 [archive/](archive/README.md)。（archived 舊檔仍保留歷史日期前綴、凍結不動。）
