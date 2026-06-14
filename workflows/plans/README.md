# plans — 實作規劃（工作流入口）

← [INDEX](../../INDEX.md)｜[CLAUDE.md](../../CLAUDE.md)

**真的要動工前**的詳細實作規劃：精確到哪個檔、什麼 code、什麼測試步驟（bite-sized task + 驗證）。這是本工作流的 **入口**。

階梯：[idea](../idea/ideas.md) → [roadmap](../roadmap.md) → [spec（討論後方案）](../specs/README.md) → **plan（動工前詳規）** → build。

- 本夾 `*.md` = 各功能的逐步實作計畫（檔名 `YYYY-MM-DD-<功能>.md`）。
- 對應的設計方案在 [specs/](../specs/README.md)（命名對應：`<功能>.md` ↔ `specs/<功能>-design.md`）。
- 計畫要遵守的**程式碼慣例 + CODE_MAP 維護鏈**見 [common/conventions](../common/conventions.md)；橫向通則見 [DEV-GUIDE](../../DEV-GUIDE.md)。

## 現役計畫

**目前無現役計畫**——已落地的計畫一律移進 [archive/](archive/README.md)（凍結、不在維護鏈、不套拆檔門檻）。下一個計畫待「身份系統 ③ 聲望/行為追蹤」定設計後才寫（見 [roadmap](../roadmap.md)）。

新計畫命名 `YYYY-MM-DD-<功能>.md`，落地後即移 archive。

> 已落地、被取代的舊實作計畫見 [archive/](archive/README.md)（保留脈絡、不在維護鏈）。本入口檔若膨脹，照 [DEV-GUIDE「結構整理原則」](../../DEV-GUIDE.md) 拆。
