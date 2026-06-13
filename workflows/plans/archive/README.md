# plans/archive — 已落地的實作計畫草稿

← [plans/](../README.md)

描述的功能皆已落地（多數實機驗證過）的舊**實作計畫**。保留作脈絡，**不在維護鏈上**（現況以 git log / [CODE_MAP](../../common/code-map/CODE_MAP.md) / [docs/spec](../../../docs/spec/SPEC-index.md) 為準）。對應的舊設計方案在 [specs/archive/](../../specs/archive/README.md)。

| 封存計畫 | 一句話 |
|----------|--------|
| [2026-06-04-script-event-entry](2026-06-04-script-event-entry.md) | 自訂 story event 入口（內容自己發帶 ref payload 的事件，SM 接到啟動模板 quest）。 |
| [2026-06-04-story-manager-probe](2026-06-04-story-manager-probe.md) | SM 探針插件（階段一）：掛原版 Kill Actor 根，遊戲內 `sqv` 驗證 SM 啟動。 |
| [2026-06-04-story-manager-spec-pipeline](2026-06-04-story-manager-spec-pipeline.md) | SM spec 管線（階段二）：quest 宣告 `storyEvent`+`aliases` 自動生 SMBN→SMQN 樹。 |
| [2026-06-06-daylight-dungeon-spell](2026-06-06-daylight-dungeon-spell.md) | IMAD builder + 開關型「白晝地城」法術，打包 in-game 測試。 |
