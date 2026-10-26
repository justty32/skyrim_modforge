# WORKFLOWS — 工作流派發器

[INDEX](INDEX.md)｜結構 [STRUCTURE](STRUCTURE.md)

使用者要做事 → **從派發表選工作流 → 讀它的入口檔**；細節都在入口檔。

**可以跳流程**：單行或小範圍、低風險、不跨 session 的修正；純查詢或一次性回答，不留 durable 知識；使用者明確要求快速處理；既有工作流只會增加同步成本而不降低風險。跳流程不等於跳過工程規矩——仍要讀必要上下文、不破壞使用者改動、能測就測。

## 派發表

### 本專案既有工作流（開發 flavor）

ModForge 的開發工作流（feature-dev、testing、refactor、investigation、dev-env、tooling、規劃管線 idea → roadmap → spec → plan、skylink、zh-tw-sync）早於 `wf/` 骨架，**一律以根 [WORKFLOWS.md](../WORKFLOWS.md) 的派發表為準**，入口都在根 [`workflows/`](../workflows/README.md)。`wf/` 不另放 dev 包的同名工作流，避免兩份規矩；碰原始碼前的慣例與 code map 也走根那份（[conventions](../workflows/common/conventions.md)、[CODE_MAP](../workflows/common/code-map/CODE_MAP.md)）。

<!-- wf-insert:WORKFLOWS -->

### kernel 內建

| 觸發（你說…）| 工作流 | 入口檔 |
|--------------|--------|--------|
| 「記 / 查踩坑」 | **gotchas** | 根 [workflows/common/gotchas.md](../workflows/common/gotchas.md)（ModForge 既有那份）|
| 「整理 X」「封存過時的」「檔案太多／太雜」「太大要拆」「只留結論」（**文件**整理）| **tidy** | [workflows/tidy/README.md](workflows/tidy/README.md) |
| 「記個想法」「以後要做」「排進 roadmap」「幫我規劃」 | **planning** | 根 [WORKFLOWS.md](../WORKFLOWS.md) 的規劃管線（idea → roadmap → spec → plan）|
| 「記個決定」「為什麼選 A 不選 B」 | **decisions** | [workflows/decisions.md](workflows/decisions.md) |
| 「我的偏好是…」「以後直接做 / 先問」 | **user** | [workflows/common/user.md](workflows/common/user.md) |

**tidy 與根 refactor 的分界**：動到程式碼、要求行為不變的重構／拆檔走根 [refactor](../workflows/refactor/README.md)；只動文件的整理、封存、抽資料檔走 tidy。結構整理原則統一在 [STRUCTURE.md](STRUCTURE.md)（ModForge 原根目錄 DEV-GUIDE 已於 2026-10-26 併入其 local 區塊）。

**都不符 → 看 [INDEX.md](INDEX.md)**。新開工作流 → 複製 [workflows/TEMPLATE.workflow.md](workflows/TEMPLATE.workflow.md) 到根 `workflows/`，並在根 [WORKFLOWS.md](../WORKFLOWS.md) 派發表加一列（ModForge 只維護根那張派發表）。

## 活狀態記哪裡（只列 open，完成即刪）

| 在等誰 | 記哪裡 |
|--------|--------|
| 等**使用者**做 / 驗證 / 決定 | ModForge 根 [WAIT_USER.md](../WAIT_USER.md)（細項在 [`wait_todo/`](../wait_todo/)；`wf/` 不另開一份）|
| 等**同 repo 另一個 session / fork** | ModForge 根 [SESSION-LOG.md](../SESSION-LOG.md) 一行 open |
| 等**別資料夾的 agent** | 工作區 `agentctl/` 的 inbox（本 repo 不裝 multi-agent 包）|
