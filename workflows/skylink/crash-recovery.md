# SkyLink — 崩潰復原迴圈

← [README](README.md)

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
- **未驗**：真 CTD 時若 `SkyrimSE.exe` 滯留（使用者回報還會多一個 wine 對話框），MO2 會**停在鎖定狀態**、`game_running()` 也會誤判成「還在跑」，`game-restart` 兩邊都過不去。解鎖那顆按鈕只能靠人點（見 [README「Agent 使用範圍」](README.md#agent-使用範圍)的桌面輸入規矩），所以清場邏輯大概得走「先確認行程真死 → 殺掉殘骸」而不是點 UI。下次真崩了才驗得到，見 [wait_todo/nexus-and-env.md](../../wait_todo/nexus-and-env.md)。目前 `game-restart` 只在乾淨退出後驗證過。
