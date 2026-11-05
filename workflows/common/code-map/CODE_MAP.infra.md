# CODE_MAP — 基礎設施・CLI・驗證・打包・Papyrus・翻譯

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：CLI 命令進入點、build orchestrator、BuildContext 狀態機、spec 驗證 pipeline、Papyrus 編譯、asset 打包、翻譯 extract/apply、plugin I/O。

各段已按主題拆到 [infra/](infra/)（子 index 一律在該資料夾）；本檔是索引。

<!-- wf-nav -->
| 檔案 | 內容 |
|------|------|
| [examples-tests.md](infra/examples-tests.md) | Examples & Schema；Tests |
| [spec-cli-mo2.md](infra/spec-cli-mo2.md) | Spec 根（頂層 DTO）；CLI 命令進入點；MO2 Profile Snapshot / Diff |
| [build-validate-package.md](infra/build-validate-package.md) | Build Orchestrator（兩段 Pipeline）；驗證 Pipeline；Papyrus 編譯；打包（Package）& 外部資產 |
| [action-system.md](infra/action-system.md) | 動作系統 asset/config 生成（OAR / BDI / PIE，loose files、非-esp） |
| [skse-distributors.md](infra/skse-distributors.md) | SKSE 分發器 config 生成（SPID / MCM / FLM / KID / BOS / AOS / SkyPatcher，loose、非-esp） |
| [skse-distributors-notes.md](infra/skse-distributors-notes.md) | SKSE 分發器 config 生成（續：各分發器說明） |
| [voice.md](infra/voice.md) | 語音克隆（TTS → .fuz） |
| [voice-cli.md](infra/voice-cli.md) | 語音克隆（續：Validate・CLI・環境變數） |
| [scripts-translate-io-diag.md](infra/scripts-translate-io-diag.md) | 工作流腳本（`scripts/`，bash）；翻譯 Extract / Apply；Plugin I/O；Diagnostics 基礎；Demo & 測試輔助 |

## 語音克隆（TTS → .fuz）

→ 已移至 [infra/voice.md](infra/voice.md#語音克隆tts--fuz)。
