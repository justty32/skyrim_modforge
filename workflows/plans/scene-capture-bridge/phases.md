# scene-capture-bridge — P1–P6 現行實作索引

← [README](README.md)（現況導航）｜[backlog](backlog.md)（未做項）｜[appendix](appendix.md)（需求原文＋驗證清單）

本索引只導航已落地能力；歷史實作過程由 git 保存。新想法與未完成項只記在 [backlog.md](backlog.md)。

## Phase 文件

| Phase | 現行內容 |
|---|---|
| [P1 — marker 與 annotations](phase-p1-markers.md) | marker 登記、放置、編輯、匯出與工具 esp |
| [P2–P4 — 場景編輯原語](phases-p2-p4-editor.md) | Eraser、Palette、Editor、Overrides、物理凍結與範圍採集邊界 |
| [P5 — console 模式與設定](phase-p5-modes.md) | `sc` 指令、模式制、動作鍵 `.ini` 與 co-save |
| [P6＋後續 — UI、匯出與 Browser](phase-p6-and-later.md) | UI polish、registry、referrer、captures、ghost、catalog 與匯出所有權 |

## 跨 Phase 不變式

- scene-capture-bridge 是薄記錄器；輸出必須是合法 `ModSpec`，生成工作交給 ModForge。
- editor chrome（marker proxy、ghost）不得進 `placements[]`。
- authored ref 的刪除／修改／命名分別走 `removals[]`／`overrides[]`／`references[]`。
- dynamic ref 只有在 `Placed` 登記簿中才算本工具所有；不能以 `0xFF......` 推導所有權。
- 場景匯出排除 actor；NPC／物品的明示擷取走獨立 `captures_*.json`。
- 執行中的 DLL 不得就地覆寫；部署走 `scripts/deploy.sh` 的原子換檔與程序 guard。
