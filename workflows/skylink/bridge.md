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
