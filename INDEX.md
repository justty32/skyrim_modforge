# INDEX — ModForge 專案地圖

整個專案的頂層導航。ModForge = **JSON spec → Skyrim `.esp` 生成工具**（AI-agent 友善）。CLAUDE.md 只放主工作流 + 指向本檔；細節從這裡分流出去。

程式碼導航另見 [CODE_MAP](workflows/common/code-map/CODE_MAP.md)，spec 欄位語意另見 [SPEC-index](docs/spec/SPEC-index.md)，在 repo 怎麼工作見 [DEV-GUIDE](DEV-GUIDE.md)。

---

## Repo 佈局

| 路徑 | 內容 |
|------|------|
| `src/` | C# 原始碼（`ModForge.Core` 生成器 + `ModForge.Cli` 命令列）。導航見 [CODE_MAP](workflows/common/code-map/CODE_MAP.md) |
| `examples/` | spec `.json` 範例 + `spec.schema.json` + `scripts/*.psc` + `assets/`——視為**源碼** |
| `assets/papyrus/` | embed 進 CLI 的 `.psc`（dispatcher / controller / 身份系統）|
| `scripts/` | 工作流 bash（bootstrap-pex / ship / ship-voice / test-offline / extract-skyrim-masters）|
| `sub_projs/` | 用 ModForge 當工具的**獨立專案**（[sofia-patch](sub_projs/sofia-patch/README.md) 消費者、[skyrim-voicegen](sub_projs/skyrim-voicegen/README.md) 語音合成基石）|
| `tests/` | xUnit（`Category=RequiresSkyrim` 為需本機 Skyrim.esm 者）|
| `docs/` | ModForge 使用手冊（cookbook/cheatsheet/spec；見下）|

## 三個頂層 index

| index | 涵蓋 |
|-------|------|
| **INDEX**（本檔）| 專案佈局 + 所有子項目入口 |
| [CODE_MAP](workflows/common/code-map/CODE_MAP.md) | 程式碼導航（5 子 index 按領域）|
| [SPEC-index](docs/spec/SPEC-index.md) | spec 欄位語意（SPEC-* 家族）|

## 開發工作流

工作流的**選擇與入口**見 **[WORKFLOWS.md](WORKFLOWS.md)**——依「你想做什麼」派發到 feature-dev / refactor / investigation / specs / plans / idea / roadmap / tooling / testing。每個工作流的 durable 知識歸在 `workflows/<該工作流>/`（入口＝該夾 README 或主檔，含 `archive/` 封存過時文檔），具體流程在各自 README。

[DEV-GUIDE](DEV-GUIDE.md) 是**被動的結構整理參考**（結構整理原則 + 四級成長軌跡）——**只在要重構/整理結構時取用**，不貫穿日常每個動作（類 zh-tw/html）。always-on 的**鐵律**在 [CLAUDE.md](CLAUDE.md)；碰原始碼的**程式碼慣例 + CODE_MAP 維護鏈**在 [common/conventions](workflows/common/conventions.md)。

## 通用（跨工作流共享）

| 路徑 | 內容 |
|------|------|
| [common/README](workflows/common/README.md) | 跨工作流共通：[gotchas](workflows/common/gotchas.md) 踩坑 + [code-map/](workflows/common/code-map/CODE_MAP.md) 程式碼導航 |

## docs/ — ModForge 使用手冊

使用文檔（如何**使用** ModForge）在 [docs/](docs/)：spec 欄位參考 [SPEC-index](docs/spec/SPEC-index.md) · NPC cookbook/cheatsheet [lifelike](docs/lifelike/README.md) · agent CLI 指南 [for_agent](docs/for_agent.md) · 外部資產打包 [external_assets](docs/external_assets.md) · 引擎背景 [engine-internals](docs/engine-internals.md) · vanilla 抽取 [local-skyrim-extraction](docs/local-skyrim-extraction.md) · 繁中鏡像 `zh-TW/` · 本機 Mutagen/Synthesis 鏡像 + Skyrim.esm 解碼 dump `reference/`（**gitignore**）。

> 注：外部工具的**已建好整合**走 spec（[SPEC-workflow](docs/spec/SPEC-workflow.md) Voice 段、external_assets）；**未建的管線可行性研究**屬 idea-research，在 [workflows/idea/asset-pipelines](workflows/idea/asset-pipelines/README.md)。

## 活狀態（只列還沒完成的）

| 檔案 | 用途 |
|------|------|
| [SESSION-LOG](SESSION-LOG.md) | 進度 hub（repo 根）→ 各工作流 session-log（open-only）|
| [wait_user](WAIT_USER.md) | 待**你**親自做/驗證的（repo 根；實機 / 外部工具 / env / 權限 / Nexus 下載）|
