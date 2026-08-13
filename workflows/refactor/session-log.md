# refactor — session log（重構整理）

← [SESSION-LOG hub](../../SESSION-LOG.md)｜拆法見 [DEV-GUIDE「膨脹即拆」](../../DEV-GUIDE.md)

**只放本工作流還沒完成的 in-flight / open 狀態**；完成即移除（→ git log）。

---

## 進行中 / open

### `src/` 拆檔分層 + hub 解耦 — 計畫已定，尚未動工

計畫全文：[src-layout-plan.md](src-layout-plan.md)（2026-08-13 於離線機 Windows 擬定）。

- **狀態**：Batch 0（golden hash 護欄）都還沒開始，`src/` 一個 byte 都還沒動。
- **已量測的基準**（動工前重跑確認沒漂移）：offline 測試 1122 passed / 1 skipped / 0 failed；**143/143 支 `examples/*.json` build 輸出 byte-deterministic**（每支連建兩次 SHA256 相同）→ golden hash 護欄可行。
- **⚠ 跨機**：這件事必須**在一台機器上連續做完**，期間另一台不要碰 `src/`——Batch 1 是一次巨型 rename，撞上就很難合。開工前先確認另一台沒有未推的 `src/` 改動。
