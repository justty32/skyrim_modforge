# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

**只放還沒完成的活狀態**，每條一行＋連結；完成即刪（濃縮句進 [landed](workflows/feature-dev/landed/README.md)，過程留 git log）。待使用者實機／操作的另見 [WAIT_USER](WAIT_USER.md)。2026-10-17 前的長版快照已封存（見 [archive 索引](archive/README.md)）。

## Open（2026-10-17 查證）

- **採集橋待實機三條**：`sc ed` numpad 回歸、`gh0` 可見性、登記簿制野外匯出 → [wait_todo/ingame-tests](wait_todo/ingame-tests.md)。程式碼在 [`../scene-capture-bridge`](../scene-capture-bridge/README.md)；現役 DLL 已非文件寫的 `c07dd174`，以母 repo `wait-user/feature-runtime/scene-ghost.md` 為準。
- **roadmap 功能 runtime 收尾六項**（Scene SetStage、MCM→GLOB、聚落作息、filtered encounter、互動 perk、instanceGlobals）→ [wait_todo/roadmap-features](wait_todo/roadmap-features.md)；逐張驗收單在工作區 `agentctl/handoffs/home-2026-09-18/modforge/ingame-checks/`。
- **Idea #19 Godot 編輯器小尾巴**：VTXT position 序、物件 rotation 實機目視 → [wait_todo/worldspace-editor](wait_todo/worldspace-editor.md)。
- **Idea #23 living-adventurers**：2026-09-18 裁示今天不做；日後入口 A／B／C 見 [README](sub_projs/living-adventurers/README.md)。
- **Idea #24 新想法未核准**：`sc ed <xx>` 改物件狀態等 → [backlog](workflows/plans/scene-capture-bridge/backlog.md)。
- **Phase 2 烘焙臉（未排）** → [plans/captured-npcs-consumption](workflows/plans/captured-npcs-consumption.md)。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [feature-dev/session-log](workflows/feature-dev/session-log.md) | 身份系統 ③ 冷凍 |
| 重構整理 | [refactor/session-log](workflows/refactor/session-log.md) | 無（拆檔與覆蓋率收尾已完成） |
| 調查／解碼 | [investigation/session-log](workflows/investigation/session-log.md) | 無 |
