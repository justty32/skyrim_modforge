# SkyLink — 實機狀態查詢橋（Manjaro 專屬）

← [WORKFLOWS](../../WORKFLOWS.md)｜開發環境見 [dev-env.md](../dev-env.md)

[SkyLink AI](https://www.nexusmods.com/skyrimspecialedition/mods/175682) 是一個 SKSE plugin + MCP server，把**執行中的 Skyrim** 以 87 個 MCP tool 暴露出來。接上之後，agent 可以直接查遊戲當下的 load order、cell、quest stage、NPC、FormID，不必每件事都靠人回報。

> 這條橋只在 **Manjaro 主力機**成立（需要 Skyrim + Proton + MO2）。離線機沒有遊戲，這裡的一切都跳過。腳本在 `scripts/skylink/`（照 repo 慣例，可執行的東西住 `scripts/`）。

## 內容

| 檔 | 涵蓋 |
|----|------|
| [bridge.md](bridge.md) | 為什麼需要橋（Win32 pipe ↔ Linux 的 relay 架構）＋用法（`skylink-bridge.sh` / MCP 註冊 / 免註冊 driver）|
| [crash-recovery.md](crash-recovery.md) | 崩潰復原迴圈：CTD 後自己讀 crash log、經 MO2 重啟遊戲、載回最新存檔（真 CTD 清場**未驗**）|
| [gotchas.md](gotchas.md) | 踩坑（多為靜默失敗：`search_forms` type 退回、`call_papyrus` 傳不了 Form、`add_item` 認 FormID…）|

## Agent 使用範圍

**SkyLink 的 tool 授權讀寫全開**：查詢類（`get_*` / `search_forms` / `poll_events`）與變更類（`set_quest_stage` / `add_item` / `execute_console` / `teleport` / `kill_npc`）都可自行判斷使用。

**任何會動到桌面的操作另有規矩**——滑鼠移動／點擊、鍵盤輸入、**以及視窗焦點切換（`xdotool windowactivate` / `windowfocus` / `windowraise`）**：只有在**使用者不在電腦前**時才可以做（他出門、健身、洗澡，且明講了人不在）。人在電腦前一律不碰，這些都會當場打斷他。遊戲內的對話框（例如載入存檔時 vanilla 的 Survival Mode 詢問）留給使用者自己按；agent 只負責看螢幕、講清楚那是什麼。

> **唯讀的觀察不受此限**：`import -window <id>`（`DISPLAY=:1`，Wayland 下 Proton 走 XWayland）截圖、`xdotool search --name` / `getwindowgeometry` / `xprop` 查詢都不改變任何狀態。MO2 與遊戲的 `WM_CLASS` 都是 `steam_app_489830`，靠 `getwindowpid` 分辨。

實機**體感**仍然只有人能判——動畫對不對、嘴型有沒有動、崩不崩、觀感如何。這條橋補的是**狀態事實**：esp 有沒有載入、record 在不在、stage 有沒有推進、FormID 解析成什麼。兩者不互相取代，測試流程見 [testing.md](../testing.md)。
