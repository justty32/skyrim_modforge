# wait_user — 等待使用者的事

← [CLAUDE.md](CLAUDE.md)｜[INDEX](INDEX.md)

需要**你（justty32）親自做 / 驗證**才能繼續的事——不只遊戲實機，也包含 **bash 指令、環境變數設定、權限測試、外部工具實跑**等。我能做結構性驗證 + 打包；跨不過去的那一關記這裡等你。

**活項只列還沒做的**。做完或作廢的項移到該分類檔底部的「封存」節（原文保留、開頭註明狀態與證據）；功能類確認後濃縮句進 [workflows/feature-dev/landed.md](workflows/feature-dev/landed/README.md)。

> 本檔是**精簡入口**，待你項按類別放在 [`wait_todo/`](wait_todo/)；新增項放對應類別檔。整個工作區的待使用者總表在母 repo `WAIT_USER.md`，本 repo 的項目尚未併入那邊（2026-10-17）。

## 待你項分類（`wait_todo/`）

- **[roadmap-features.md](wait_todo/roadmap-features.md)** — 離線實作完、待 runtime／byte 驗收的後端功能：scene catalog runtime、Scene SetStage、MCM→GLOB、聚落作息、filtered encounter 冷卻、互動 perk byte、instanceGlobals。
- **[worldspace-editor.md](wait_todo/worldspace-editor.md)** — Godot worldspace editor（Idea #19）：VTXT「刷哪長哪」目視、物件 rotation 軸實機校準。
- **[ingame-tests.md](wait_todo/ingame-tests.md)** — 純遊戲實機測試（含**怎麼測通用流程** + MO2 鐵律）。**scene-capture-bridge 剩三條**：`gh0` 可見性、🔴 `sc ed` numpad 回歸、登記簿制野外匯出。其餘：MESG 多按鈕、EFSH、座標 profile cube、Sofia × VIGILANT Act 1–4、blacksmith 場景。
- **[nexus-and-env.md](wait_todo/nexus-and-env.md)** — 不屬任何功能的雜項：SkyLink 真 CTD 後的復原迴圈、主力機重生 quest nodes。
