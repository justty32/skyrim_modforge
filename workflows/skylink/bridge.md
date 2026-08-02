# SkyLink — 橋的架構與用法

← [README](README.md)

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
scripts/skylink/skylink-bridge.sh build     # 首次 / 改了 relay.c 或 sendkey.c（需 mingw-w64-gcc）
scripts/skylink/skylink-bridge.sh up        # 遊戲已啟動並載入存檔後才跑
scripts/skylink/skylink-bridge.sh status
scripts/skylink/skylink-bridge.sh key 44    # 在遊戲裡按一個鍵（hex DirectInput scancode，44 = F10）
scripts/skylink/skylink-bridge.sh down
```

### `key` — agent 唯一能按鍵的方式（2026-07-10 加）

SkyLink 的 tool 都在**遊戲行程內**執行，繞過輸入層，所以查狀態、跑 console 都沒問題。但**按鍵不行**，而有些 SKSE plugin（例如 [scene-capture-bridge](../../../scene-capture-bridge/README.md)）的觸發器只有 hotkey。

Linux 這側完全按不動：Wayland 合成器擋掉 XTest（`xdotool mousemove` 連指標都不會動，`getmouselocation` 可自證），而 Skyrim 讀 raw input，`xdotool key --window` 的合成 X 事件也被忽略。**連主選單的 CONTINUE 都按不了。**

`sendkey.c` 走跟 `relay.c` 同一條路：mingw 靜態編成 PE，用 `protontricks-launch --appid 489830` 塞進**遊戲既有的** wineserver。在那個行程裡呼叫 `SendInput()`，送進的就是 dinput8 讀的那條佇列。`key` 子命令會先 `xdotool windowactivate` 把遊戲拉到前景（視窗操作走 X 協定，不是 XTest，這個有效），再注入。

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
