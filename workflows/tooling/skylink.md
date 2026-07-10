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

## Gotchas

- **`up` 會在遊戲沒跑時拒絕動作**，這是刻意的。在 Steam 背後對那顆 prefix 起一個 wineserver，會讓下次啟動卡死（Proton reaper 永遠等不到）。收拾方式一律 `wineserver -k`。
- 橋斷了（遊戲關掉、relay 死掉）時，tool call 回 `An error occurred invoking '<tool>'`，不會 hang。先 `status`。
- 依賴：`x86_64-w64-mingw32-gcc`、`socat`、`protontricks`、`dotnet` ≥10。
- prefix 用 Proton 9.0 (Beta)（`compatdata/489830/config_info`）。`protontricks-launch` 會自動挑對版本並掛進既有 wineserver。
- **`relay.exe` 不進 git**（gitignore），fresh clone 後跑 `build`。

## Agent 使用範圍

使用者授權**讀寫全開**：查詢類（`get_*` / `search_forms` / `poll_events`）與變更類（`set_quest_stage` / `add_item` / `execute_console` / `teleport` / `kill_npc`）都可自行判斷使用。

實機**體感**仍然只有人能判——動畫對不對、嘴型有沒有動、崩不崩、觀感如何。這條橋補的是**狀態事實**：esp 有沒有載入、record 在不在、stage 有沒有推進、FormID 解析成什麼。兩者不互相取代，測試流程見 [testing.md](../testing.md)。
