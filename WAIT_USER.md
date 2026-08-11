# wait_user — 等待使用者的事

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

需要**你（justty32）親自做 / 驗證**才能繼續的事——不只遊戲實機，也包含 **bash 指令、環境變數設定、權限測試、Nexus 下載 mod、外部工具實跑**等。我能做結構性驗證 + 打包；跨不過去的那一關記這裡等你。

**只列還沒做的**——做完即移除（不留已完成清單）；功能類確認後濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)，歷史看 git log。

> 本檔是**精簡入口**。待你項已按類別拆進 [`wait_todo/`](wait_todo/)（2026-06-18，照 [DEV-GUIDE「結構整理原則」](DEV-GUIDE.md)）；本檔只留導航到 `wait_todo/` 分類檔。新增待你項放對應類別檔；某類膨脹再於 `wait_todo/` 內續拆。

## 待你項分類（`wait_todo/`）

- **[roadmap-features.md](wait_todo/roadmap-features.md)** — 離線實作完、待主力機 byte/runtime 驗收的後端功能：MCM→GLOB setter、動態生怪 SM、互動式 perk、instanceGlobals、Idea #20 技能樹（JContainers + Campfire U1–U3）。
- **[worldspace-editor.md](wait_todo/worldspace-editor.md)** — Godot worldspace editor（Idea #19）整鏈：VTXT「刷哪長哪」最終目視、model-converter nif→glTF 對真實檔驗。
- **[ingame-tests.md](wait_todo/ingame-tests.md)** — 純遊戲實機測試（含**怎麼測通用流程** + MO2 鐵律）。**scene-capture-bridge 剩三條**（2026-07-14 那輪，同一顆 DLL `c07dd174`）：**`gh0` 可見性**、🔴 **`sc ed` numpad 回歸**、**匯出登記簿制的野外驗證**。其餘是舊帳：darksouls-port P1、living-adventurers、blacksmith 場景、VNML、Sofia × VIGILANT Act 1 / Act 2-4。
- **[nexus-and-env.md](wait_todo/nexus-and-env.md)** — 不屬任何功能的雜項：Nexus 下載清單、env、bash。
