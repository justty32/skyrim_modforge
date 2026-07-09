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
- **darksouls-port（DS1 北方不死院 → Skyrim worldspace）**：P1「空殼院」離線完成、`DSPortP1.zip` 已交付（2026-07-06），**剩實機驗收**（進場指令與三段驗收見 [wait_todo/ingame-tests.md](wait_todo/ingame-tests.md)）；規劃與 P1 結論 [plan.md](sub_projs/darksouls-port/plan.md)。
- **Idea #19 Godot Worldspace Editor**：整鏈已落地（[landed/world](workflows/feature-dev/landed/world.md) +「Godot 編輯器 WYSIWYG」條 / [godot-editor](workflows/feature-dev/landed/godot-editor.md)），剩非阻塞小尾巴——見 [WAIT_USER](WAIT_USER.md) → [wait_todo/worldspace-editor.md](wait_todo/worldspace-editor.md)。
- **Idea #24 遊戲內編輯器**：**ModForge 生成端已完備**（scene.json → patch：placements 含 rotation/scale/位移、mapMarker、hazard、keyword、role macro〔blacksmith 問候+行為+交易，實機確認〕、`removals[]` 橡皮擦）——落地見 [landed/dialogue-quests](workflows/feature-dev/landed/dialogue-quests.md) +「§D/§E」、[landed/world](workflows/feature-dev/landed/world.md)「removals」、設計 [spec](workflows/specs/ingame-scene-export-design.md) / plan [ingame-scene-export](workflows/plans/ingame-scene-export.md)。**下一大塊＝runtime 採集橋 SKSE DLL**（走訪 cell/滴管取樣/FormID 反解/吐 scene.json，C++ 獨立子專案，scene.json 契約＝它的 output 目標）——**子專案骨架 + `SceneExporter` 實作 stub 已離線落地**（2026-07-09，`sub_projs/scene-capture-bridge/`；建置架構改編自使用者提供的參考 repo my_skyrim_plugin_1，只借 build stack、邏輯自寫）：cell 走訪→placements/npcRefs、FormID→`<plugin>:0xLOCALID` 反解、scene.json 序列化（nlohmann）皆成形，多處 `TODO(runtime-verify)`。**剩＝首編**（離線機無 MSVC，待主力機 clang-cl 或 CI，見 [wait_todo/nexus-and-env.md](wait_todo/nexus-and-env.md)）＋ §B/§D/§E 遊戲內編輯 UI（M4 後接）。實機待驗（blacksmith 場景 + vendor + 移除）見 [wait_todo/ingame-tests.md](wait_todo/ingame-tests.md)。
