# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[workflows/INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

## 最新進度（幾句話）

- 目前無跨工作流的 open 項；各工作流的 open 狀態見下表。
- **近期已落地（細節在 git log）**：docs/workflows 大重構（CLAUDE.md 瘦成路由器、開發流程移到頂層 `workflows/`）→ 拆檔門檻定案（docs 300 行 / workflows 8192 bytes）→ DEV-GUIDE 改被動參考、plans/specs 命名去日期 → docs/ SPEC 拆分 → **zh-TW + html 全面重譯重組（1:1 鏡像 EN，原 deferred 的 re-sync 已補完）**。語音合成已解耦為 `sub_projs/skyrim-voicegen/`、Sofia 擴充為 `sub_projs/sofia-patch/`；`gamedata` CLI + `sub_projs/game-data/` 供並行 agent 取用。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [workflows/feature-dev/session-log](workflows/feature-dev/session-log.md) | 身份系統 ③ 聲望/行為追蹤（待設計）|
| 重構整理 | [workflows/refactor/session-log](workflows/refactor/session-log.md) | 無 |
| 調查／解碼 | [workflows/investigation/session-log](workflows/investigation/session-log.md) | 無 |

## 不屬任何工作流的進度（堆太多 → 拆進 `session_logs/`）

- **Idea #19 Godot Worldspace Editor** → [worldspace-editor/README.md](workflows/idea/worldspace-editor/README.md)。**後端 MVP design 已完成**（2026-06-16）→ [workflows/specs/worldspace-editor-design.md](workflows/specs/worldspace-editor-design.md)：heightmap PNG（自動衍生 cell grid、min/max 線性高度）→ 逆推 offset+signed delta（VHGT 格式已查證：signed int8、row-wise 累積、delta ×8 game units——**修正了 coord-system.md 舊的 1/8 錯誤**）→ seam 靠 PNG 共用欄零誤差。VHGT blocking 已降級為「待主力機對真實 LAND 反解收尾」（不擋實作）。NIF→glTF 已查證：選 **fo76utils/nifskope**（雙平台，主力機可跑），`nif2gltf` Rust CLI 是 Gemini 捏的。**下一步**：出 implementation plan（writing-plans）；placements.json 格式另一輪 brainstorm。
