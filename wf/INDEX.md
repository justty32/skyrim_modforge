# INDEX — ModForge `wf/` 骨架地圖

ModForge = **JSON spec → Skyrim `.esp` 生成工具**（AI-agent 友善）。**整個 repo 的頂層地圖是根 [INDEX.md](../INDEX.md)**；本檔只描述 `wf/`（2026-10-26 非侵入式導入的 wf kernel）裡有什麼，每列一句話＋連結。

## `wf/` 佈局

| 路徑 | 內容 |
|------|------|
| `workflows/` | kernel 工作流：[tidy](workflows/tidy/README.md)（文件整理）、[decisions](workflows/decisions.md)、[TEMPLATE](workflows/TEMPLATE.workflow.md)；共享區 [workflows/common/](workflows/common/README.md)（user、reply-style、data-files）。開發工作流不在這裡，在根 [`workflows/`](../workflows/README.md) |
| `tools/` | kernel 工具：`wf-lint.sh`（檢查）、`tabledb.py`（資料檔 CRUD／連結）、`find_big_lists.py`、`fix_moved_links.py`；資料檔契約見 [workflows/common/data-files.md](workflows/common/data-files.md) |
| 根 `.claude/commands/` | slash 指令適配層（`/wf-lint`）；Claude Code 只讀專案根的這層，所以留在根 |
<!-- wf-insert:INDEX -->

## 頂層文件

| 檔案 | 角色 |
|------|------|
| [WORKFLOWS.md](WORKFLOWS.md) | kernel 派發器；開發工作流導回根 [WORKFLOWS.md](../WORKFLOWS.md) |
| [STRUCTURE.md](STRUCTURE.md) | 結構整理參考（被動）；含 ModForge 補充（原根目錄 DEV-GUIDE 併入） |
| 根 [SESSION-LOG.md](../SESSION-LOG.md) | open 進度（在 repo 根，不在 `wf/`）|
| 根 [WAIT_USER.md](../WAIT_USER.md) | 等使用者親自做 / 驗證的事（在 repo 根，細項在 [`wait_todo/`](../wait_todo/)）|
