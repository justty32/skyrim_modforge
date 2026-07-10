# SkyLink AI — 實機狀態查詢橋（Manjaro 專屬）

← [tooling](README.md)｜開發環境見 [dev-env.md](../dev-env.md)

[SkyLink AI](https://www.nexusmods.com/skyrimspecialedition/mods/175682) 是一個 SKSE plugin + MCP server，把**執行中的 Skyrim** 以 87 個 MCP tool 暴露出來。接上之後，agent 可以直接查遊戲當下的 load order、cell、quest stage、NPC、FormID，不必每件事都靠人回報。

> 這條橋只在 **Manjaro 主力機**成立（需要 Skyrim + Proton + MO2）。離線機沒有遊戲，這裡的一切都跳過。

## 為什麼需要橋

Mod 假設 Windows：SKSE plugin 在遊戲行程內開一條 Win32 named pipe `\\.\pipe\SkyrimMCP`，`.NET` MCP server 用 `NamedPipeClientStream` 去接。在 Linux 上這兩端接不起來——

- pipe 只存在於**遊戲那顆 Proton prefix 的 wineserver** 命名空間裡，Linux 行程看不到；
- .NET on Unix 把 named pipe 實作成 unix domain socket `/tmp/CoreFxPipe_<name>`，跟 Win32 pipe 無關。

所以補一段 relay，兩邊各自用自己原生的東西講話：

```
Skyrim (Proton prefix)                                       Linux
SKSE plugin ──\\.\pipe\SkyrimMCP──> relay.exe ──TCP:8770──> socat ──> /tmp/CoreFxPipe_SkyrimMCP ──> SkyrimMCP.dll ──MCP/stdio──> agent
```

`SkyrimMCP.dll` **一行都沒改**，在 Linux 原生 dotnet 10 下跑。pipe 上是換行分隔的 JSON 位元組流（`{"id":uuid,"action":"get_cell_info","params":{}}` → `{"success":true,"data":{…}}`），沒有 Windows message-mode，所以 relay 只要盲轉位元組。

`relay.c` 用 mingw 靜態編成無依賴的 PE，靠 `protontricks-launch --appid 489830` 塞進**遊戲既有的** wineserver。

## 用法

```bash
scripts/skylink/skylink-bridge.sh build     # 首次 / 改了 relay.c（需 mingw-w64-gcc）
scripts/skylink/skylink-bridge.sh up        # 遊戲已啟動並載入存檔後才跑
scripts/skylink/skylink-bridge.sh status
scripts/skylink/skylink-bridge.sh down
```

MCP server 以 `local` scope 註冊（存 `~/.claude.json`，不進 repo，離線機不受影響）：

```bash
claude mcp add --scope local skyrim -- dotnet <MO2>/mods/SkyLinkAI/SKSE/Plugins/SkyLinkAI_Server/SkyrimMCP.dll
```

註冊後**新開的 session** 才會拿到那 87 個 tool。當下 session 要臨時打一發，用免註冊的 driver：

```bash
scripts/skylink/skylink-call.py --list
scripts/skylink/skylink-call.py get_cell_info
scripts/skylink/skylink-call.py get_nearby_np_cs '{"radius":4000}'
```

## 崩潰復原迴圈

遊戲 CTD 之後 agent 可以自己爬起來，**全程不碰 Steam、不 `wineserver -k`**——關鍵在行程樹：

```
reaper → proton → SkyrimSELauncher.exe(stub) → ModOrganizer.exe → SkyrimSE.exe
```

**MO2 不會跟著遊戲一起死**，而它的 `ModOrganizer.ini` 裡第一個 configured executable 標題是 `SKSE`。所以對還活著的 MO2 送一發 `moshortcut://:SKSE` 就能重拉遊戲。relay 與 socat 也活過 CTD（獨立行程），pipe 一回來就自動接上，**橋會自癒**。

```bash
scripts/skylink/skylink-bridge.sh crashlog           # 最新 crash-*.log 路徑
scripts/skylink/skylink-bridge.sh game-restart       # moshortcut://:SKSE，等到 pipe 活過來
scripts/skylink/skylink-bridge.sh game-load-latest   # 挑最新 .ess，load_save
```

- crash log（CrashLogger）在 prefix 的 `Documents/My Games/Skyrim Special Edition/SKSE/crash-*.log`，Linux 端直接讀。
- **`load_most_recent_save` 是壞的**（無論如何都回 `{"loading":false}`），所以 `game-load-latest` 自己按 mtime 挑最新 `.ess`，再用 `load_save` 指名載入。主選單下 `get_game_safety` 可用、`get_cell_info` 會 `isError`——後者正好拿來判斷存檔載完沒。
- **MO2 lock**：MO2 啟動 executable 期間會把自己鎖住並顯示 `Mod Organizer is locked while the application is running / SkyrimSE.exe (<pid>) / [Unlock]`，**鎖住時拒絕再啟動任何 executable**。遊戲乾淨退出 → MO2 自動解鎖 → `moshortcut` 可用（`game-restart` 就是靠這個）。
- **未驗**：真 CTD 時若 `SkyrimSE.exe` 滯留（使用者回報還會多一個 wine 對話框），MO2 會**停在鎖定狀態**、`game_running()` 也會誤判成「還在跑」，`game-restart` 兩邊都過不去。解鎖那顆按鈕只能靠人點（見上面「Agent 使用範圍」的桌面輸入規矩），所以清場邏輯大概得走「先確認行程真死 → 殺掉殘骸」而不是點 UI。下次真崩了才驗得到，見 [wait_todo/nexus-and-env.md](../../wait_todo/nexus-and-env.md)。目前 `game-restart` 只在乾淨退出後驗證過。

## Gotchas

- **`up` 會在遊戲沒跑時拒絕動作**，這是刻意的。在 Steam 背後對那顆 prefix 起一個 wineserver，會讓下次啟動卡死（Proton reaper 永遠等不到）。收拾方式一律 `wineserver -k`。
- 橋斷了（遊戲關掉、relay 死掉）時，tool call 回 `An error occurred invoking '<tool>'`，不會 hang。先 `status`。
- **遊戲內的 MessageBox 不擋 SkyLink**。載入存檔時 vanilla 會跳 Survival Mode 詢問（`ccQDRSSE001-SurvivalMode.esl`），此時 `get_menu_state` 回 `openMenus:["MessageBoxMenu"]`、`gameIsPaused:true`，但 pipe 照常回話，讀寫 tool（`get_cell_info` / `search_forms` / `save_game`）全部可用。**沒有任何 tool 能回答 MessageBox**（`call_papyrus_function` 也不行——它 blocking 在 UI 輸入上），只能人點。所以看到 `isPaused:true` 不代表橋掛了。
- 依賴：`x86_64-w64-mingw32-gcc`、`socat`、`protontricks`、`dotnet` ≥10。
- prefix 用 Proton 9.0 (Beta)（`compatdata/489830/config_info`）。`protontricks-launch` 會自動挑對版本並掛進既有 wineserver。
- **`relay.exe` 不進 git**（gitignore），fresh clone 後跑 `build`。

## Agent 使用範圍

**SkyLink 的 tool 授權讀寫全開**：查詢類（`get_*` / `search_forms` / `poll_events`）與變更類（`set_quest_stage` / `add_item` / `execute_console` / `teleport` / `kill_npc`）都可自行判斷使用。

**任何會動到桌面的操作另有規矩**——滑鼠移動／點擊、鍵盤輸入、**以及視窗焦點切換（`xdotool windowactivate` / `windowfocus` / `windowraise`）**：只有在**使用者不在電腦前**時才可以做（他出門、健身、洗澡，且明講了人不在）。人在電腦前一律不碰，這些都會當場打斷他。遊戲內的對話框（例如載入存檔時 vanilla 的 Survival Mode 詢問）留給使用者自己按；agent 只負責看螢幕、講清楚那是什麼。

> **唯讀的觀察不受此限**：`import -window <id>`（`DISPLAY=:1`，Wayland 下 Proton 走 XWayland）截圖、`xdotool search --name` / `getwindowgeometry` / `xprop` 查詢都不改變任何狀態。MO2 與遊戲的 `WM_CLASS` 都是 `steam_app_489830`，靠 `getwindowpid` 分辨。

實機**體感**仍然只有人能判——動畫對不對、嘴型有沒有動、崩不崩、觀感如何。這條橋補的是**狀態事實**：esp 有沒有載入、record 在不在、stage 有沒有推進、FormID 解析成什麼。兩者不互相取代，測試流程見 [testing.md](../testing.md)。
