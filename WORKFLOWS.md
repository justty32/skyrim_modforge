# WORKFLOWS — 工作流派發器

← [CLAUDE.md](CLAUDE.md)｜專案地圖 [INDEX.md](INDEX.md)

你（使用者）說要做某件事 → **從這張表選對應工作流 → 讀它的「入口檔」→ 就知道要做什麼**。每個工作流的細節都在它自己的入口檔，不在這裡。

## 你想做什麼 → 用哪個工作流

| 觸發（你說…）| 工作流 | 入口檔（先讀這個）|
|--------------|--------|-------------------|
| 「我想用 / 設定某個外部工具」「查 env var / 依賴」 | **tooling** | [workflows/tooling/README.md](workflows/tooling/README.md) |
| 「我想開發 / 修改某個 feature」 | **feature-dev** | [workflows/feature-dev/README.md](workflows/feature-dev/README.md) |
| 「重構 / 拆檔 / 整理結構」 | **refactor** | [workflows/refactor/README.md](workflows/refactor/README.md) |
| 「解碼 vanilla / 某個 mod」「可行性調查」 | **investigation** | [workflows/investigation/README.md](workflows/investigation/README.md) |
| 「把一個 idea 討論成設計方案」 | **spec** | [workflows/specs/README.md](workflows/specs/README.md) |
| 「把設計方案展開成動工計畫」 | **plan** | [workflows/plans/README.md](workflows/plans/README.md) |
| 「記一個奇思妙想」（不確定要不要做）| **idea** | [workflows/idea/ideas.md](workflows/idea/ideas.md) |
| 「記一件確定會做、不確定何時的事」 | **roadmap** | [workflows/roadmap/](workflows/roadmap/README.md) |
| 「跑測試」 | **testing** | [workflows/testing.md](workflows/testing.md) |
| 「設定 / 了解開發環境」「fresh clone 後要做什麼」「這台機器能做什麼」 | **dev-env** | [workflows/dev-env.md](workflows/dev-env.md) |

**規劃管線**（一個想法的成熟過程）：idea（要不要做？）→ roadmap（會做，何時？）→ spec（討論後方案）→ plan（動工前詳規）→ build（feature-dev）。

## 工作流的統一形式（規範）

所有工作流照同一套形式（細則見 [DEV-GUIDE](DEV-GUIDE.md)）：

**檔名規範**：
- **README** = 初入一個資料夾**先讀的入口／導引**（這資料夾在幹嘛、怎麼用）。
- **INDEX** = **描述該資料夾頂層結構**的索引（有哪些子項、各放什麼）。
- 小資料夾兩者可合一（README 兼述結構）；大到結構複雜時才分出獨立 INDEX。

形式：
- **資料夾型工作流**（feature-dev / refactor / investigation / specs / plans / idea / common）：
  - 一個 **入口 README**（或主檔，如 idea 的 `ideas.md`）——先讀它就知道這工作流在幹嘛、有哪些檔。
  - **`archive/`**：過時 / 被取代的文檔封存於此（保留脈絡、不在維護鏈）。
  - 視需要的 `gotchas.md`（踩坑）、`session-log.md`（本工作流 open 進度）。
- **單檔工作流**（tooling / roadmap / testing）：一個 `.md` 同時是入口與內容；撐大了就照「[結構整理原則](DEV-GUIDE.md)」升級成資料夾型。
- 入口檔本身膨脹 → 一樣照結構整理原則拆。

## 跨工作流的活狀態（repo 根）

- **進度**（還沒完成的 in-flight / open）→ [SESSION-LOG.md](SESSION-LOG.md)
- **待你親自做 / 驗證的**（實機 / 外部工具 / env / 權限 / Nexus 下載）→ [WAIT_USER.md](WAIT_USER.md)
