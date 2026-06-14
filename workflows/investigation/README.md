# 調查／解碼（investigation）— 工作流入口

← [INDEX](../../INDEX.md)｜[CLAUDE.md](../../CLAUDE.md)

這是本工作流的 **入口**。要整理本工作流結構時參考 [DEV-GUIDE](../../DEV-GUIDE.md)（被動）；always-on 鐵律見 [CLAUDE.md](../../CLAUDE.md)。

## 流程

離線解碼 vanilla / 既有 mod（**esp-only、記憶體安全**——絕不載 Skyrim.esm 250MB、不 `.ToList()` 整個 record group）對照 ModForge 可實現性 → 產出進 [decode/](decode/README.md)。浮現的待補項進 [roadmap](../roadmap.md)，踩坑進 [gotchas](gotchas.md)。

## 內容

| 路徑 | 內容 |
|------|------|
| [decode/](decode/README.md) | **解碼參考檔**（VIGILANT / AI Overhaul / Sofia / blender 等；index 在該夾 README）|
| [esm-formid-access.md](esm-formid-access.md) | **agent 工具參考**：怎麼從 esm/esp 抽內容、查 FormID（`gamedata`/`find`/各 `*diag`）|
| [mod-survey-guide.md](mod-survey-guide.md) | **agent 操作手冊**：調查 `~/skyrim_mods/` 那批 mod；工作區在 [`sub_projs/mod-survey/`](../../sub_projs/mod-survey/README.md) |
| [gotchas.md](gotchas.md) | 解碼踩坑（vanilla nif 驗證 / WRLD 覆寫等）|
| [session-log.md](session-log.md) | 本工作流 open / in-flight 調查（hub 在 repo 根 [SESSION-LOG](../../SESSION-LOG.md)）|

> **archive**：過時的解碼文檔封存進 `investigation/archive/`（學 [specs/](../specs/README.md)）。本入口檔若膨脹，照結構整理原則拆。
