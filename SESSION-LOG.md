# SESSION-LOG — 進度日誌（hub）

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

**只放「還沒完成」的活狀態**（in-flight / open）。完成的不留這裡——濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，過程細節留 git log。待**你**親自驗證／做的另見 [WAIT_USER.md](WAIT_USER.md)。

> **膨脹就拆**：本檔若過大，就在 repo 頂層新立 **`session_logs/`** 資料夾，按工作流／類別**拆檔 + 一個 index 導航**（照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）。

本檔同時 ① 連到各工作流自己的 session-log，② 收**不屬任何工作流**的進度——後者堆太多時就是拆進 `session_logs/` 的觸發。

> **條目格式**：每條只留**一行 open 狀態 + 指向細節的連結**（設計決策/修了什麼落到該工作流或 sub_proj 文件、已落地功能進 [landed](workflows/feature-dev/landed/README.md)、待你驗的進 [WAIT_USER](WAIT_USER.md)）。完成即整條刪除。

## 最新進度

- 目前無跨工作流的 open 項；各工作流的 open 狀態見下表，不屬任何工作流的 open 項見最末節。

## 各工作流 session-log

| 工作流 | session-log | open 摘要 |
|--------|-------------|----------|
| 功能開發 | [workflows/feature-dev/session-log](workflows/feature-dev/session-log.md) | 🧊 身份系統 ③ 聲望/行為追蹤（2026-06-22 冷凍，等很有空再做）|
| 重構整理 | [workflows/refactor/session-log](workflows/refactor/session-log.md) | 無 |
| 調查／解碼 | [workflows/investigation/session-log](workflows/investigation/session-log.md) | 無 |

## 不屬任何工作流的進度（堆太多 → 拆進 `session_logs/`）

- **Idea #23 living-adventurers**：剩主力機 `package`（編全部 .pex：spike/P1/canonical + 互動 setGlobal TIF）+ 實機驗收（P0–P3 整鏈第一次 runtime，共同 acceptance gate）——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/ingame-tests.md](wait_todo/ingame-tests.md)。設計/進度 → idea [#23](workflows/idea/living-adventurers.md)、sub_proj [README](sub_projs/living-adventurers/README.md) + [design.md](sub_projs/living-adventurers/design.md)。
- **Idea #20 in-world 技能樹**：Phase 0 離線完備 + .pex 已編交付，剩實機驗收——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/roadmap-features.md](wait_todo/roadmap-features.md)。sub_proj [inworld-skill-tree](sub_projs/inworld-skill-tree/README.md)。
- **darksouls-port（DS1 北方不死院 → Skyrim worldspace）**：sub_proj 已開、規劃完成，待 P0 spike（單塊 map piece FLVER→NIF 端到端 + 碰撞路線定案）——[sub_projs/darksouls-port/plan.md](sub_projs/darksouls-port/plan.md)。
- **Idea #19 Godot Worldspace Editor**：整鏈已落地（[landed/world](workflows/feature-dev/landed/world.md) +「Godot 編輯器 WYSIWYG」條 / [godot-editor](workflows/feature-dev/landed/godot-editor.md)），剩非阻塞小尾巴——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/worldspace-editor.md](wait_todo/worldspace-editor.md)。
