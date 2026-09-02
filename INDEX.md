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
| `scripts/` | 工作流 bash：出貨與環境（bootstrap-pex / ship / ship-voice / extract-skyrim-masters）＋**測試五支**（test-offline 跑測試；golden-hash / package-snapshot / cli-dispatch-snapshot 三支重構護欄；coverage 找沒測到的洞）——後者用法見 [testing.md](workflows/testing.md) |
| `sub_projs/` | 用 ModForge 當工具的**獨立專案**（消費者 / 基石工具）——見 [sub_projs/README](sub_projs/README.md)。**2026-08-02 大幅瘦身**：只剩 `gemini-research`、`inworld-skill-tree`、`living-adventurers` 三個實體；其餘 **8 個已移出**——有程式碼的成了 `projects/` 下的同層 repo（godot-worldspace-editor、scene-capture-bridge、model-converter、agent-bridge、darksouls-port、sofia-patch、skyrim-voicegen、game-data），純文檔的進工作區 `analysis/`（mod-survey、tool-survey、followers-patch）。**stub 也不留**——原名 → 現在在哪的對照表在 [sub_projs/README](sub_projs/README.md) |
| `tests/` | xUnit（`Category=RequiresSkyrim` 為需本機 Skyrim.esm 者）|
| `spikes/` | 實驗性 spike，不進主管線、不進 dotnet test。目前一個 `prefab_grammar`（seed 可固定的離線 prefab grammar，Python unittest 自測；見 [spikes/prefab_grammar/README.md](spikes/prefab_grammar/README.md)）|
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

使用文檔（如何**使用** ModForge）在 [docs/](docs/)：spec 欄位參考 [SPEC-index](docs/spec/SPEC-index.md) · NPC cookbook/cheatsheet [lifelike](docs/lifelike/README.md) · agent CLI 指南 [for_agent](docs/for_agent.md) · 外部資產打包 [external_assets](docs/external_assets.md) · 引擎背景 [engine-internals](docs/engine-internals.md) · vanilla 抽取 [local-skyrim-extraction](docs/local-skyrim-extraction.md) · [繁中鏡像 zh-TW](docs/zh-TW/spec/SPEC-index.md) · 本機 Mutagen/Synthesis 鏡像 + Skyrim.esm 解碼 dump [reference/](docs/reference/INDEX.md)（**素材本體 gitignore、`INDEX*.md` 地圖有進版控**——2026-08-02 修正，之前整包被吞，fresh clone 讀不到地圖）。

> 注：外部工具的**已建好整合**走 spec（[SPEC-workflow](docs/spec/SPEC-workflow.md) Voice 段、external_assets）；**未建的管線可行性研究**屬 idea-research，在 [workflows/idea/asset-pipelines](workflows/idea/asset-pipelines/README.md)。

## 活狀態（只列還沒完成的）

| 檔案 | 用途 |
|------|------|
| [SESSION-LOG](SESSION-LOG.md) | 進度 hub（repo 根）→ 各工作流 session-log（open-only）|
| [wait_user](WAIT_USER.md) | 待**你**親自做/驗證的精簡入口（repo 根）→ 細項按類別在 [`wait_todo/`](wait_todo/)（roadmap-features / worldspace-editor / ingame-tests / nexus-and-env）|
