# SkyLink — 踩坑

← [README](README.md)

探索實測出來的陷阱，多數是**靜默失敗**（不報錯、結果騙人）——新發現往這裡加。

- **`up` 會在遊戲沒跑時拒絕動作**，這是刻意的。在 Steam 背後對那顆 prefix 起一個 wineserver，會讓下次啟動卡死（Proton reaper 永遠等不到）。收拾方式一律 `wineserver -k`。
- 橋斷了（遊戲關掉、relay 死掉）時，tool call 回 `An error occurred invoking '<tool>'`，不會 hang。先 `status`。
- **遊戲內的 MessageBox 不擋 SkyLink**。載入存檔時 vanilla 會跳 Survival Mode 詢問（`ccQDRSSE001-SurvivalMode.esl`），此時 `get_menu_state` 回 `openMenus:["MessageBoxMenu"]`、`gameIsPaused:true`，但 pipe 照常回話，讀寫 tool（`get_cell_info` / `search_forms` / `save_game`）全部可用。**沒有任何 tool 能回答 MessageBox**（`call_papyrus_function` 也不行——它 blocking 在 UI 輸入上），只能人點。所以看到 `isPaused:true` 不代表橋掛了。
- **`search_forms` 的 `type` 會靜默退回**：不支援的值（如 `"food"`）不報錯，直接當 `all` 搜——結果看起來「有回傳」其實 filter 沒生效。食物是 ALCH → `type:"potion"`。另外 `type:"all"` **不含 GLOBAL**，要撈 GLOBAL 必須明寫 `type:"global"`。
- **`call_papyrus_function` 只吃 string/int/float/bool，傳不了 Form**。所以以 Form 為 key 的 `StorageUtil.*Value(form, …)` 一律回 `result:null`（靜默失敗，不報錯）；`JsonUtil.SetIntValue(file,key,val)` 這種全 string/int 簽章可用，實測寫 42 讀回 42。`Game.GetPlayer()` 回 `"complex_type"`，無法再餵回去當參數。
- **沒有任何移動控制**。`teleport` / console `player.setpos` / `player.moveto` 都是瞬移（不吃碰撞與 navmesh），`play_idle` 只播動畫。要真的走路只能對遊戲視窗送鍵盤輸入——受 [README「Agent 使用範圍」](README.md#agent-使用範圍)的桌面輸入規矩約束。
- **`add_item` 認 FormID 不認名字**（`"Sweetroll"` 直接失敗）。先 `search_forms` 反查再用 FormID，**不要憑記憶填**：`00064B3F` 是 Cabbage，Sweet Roll 是 `00064B3D`。
- 依賴：`x86_64-w64-mingw32-gcc`、`socat`、`protontricks`、`dotnet` ≥10。
- prefix 用 Proton 9.0 (Beta)（`compatdata/489830/config_info`）。`protontricks-launch` 會自動挑對版本並掛進既有 wineserver。
- **`relay.exe` 不進 git**（gitignore），fresh clone 後跑 `build`。

## agent 按不了鍵（2026-07-10）

`xdotool` 在這台機器上**完全驅動不了遊戲**，而且失敗得很安靜：

- Wayland 合成器不允許 XTest 指標 warp。`xdotool mousemove 900 500` 回傳成功，但 `xdotool getmouselocation` 顯示指標紋風不動。**MO2 的 Run 按鈕點不下去**（點擊「成功」，MO2 的 log 卻沒有任何新行）。
- Skyrim 讀 raw input，所以 `xdotool key`（XTest）和 `xdotool key --window`（XSendEvent）都被忽略。主選單的 CONTINUE 也按不動。

`xdotool windowactivate` / `windowclose` / `search` **有效**——那些走 X 協定訊息，不是 XTest。所以「找視窗、拉前景、截圖」可以，「送輸入」不行。

解法是 `skylink-bridge.sh key <hexscan>`（見 [bridge.md](bridge.md)）。另外兩條繞道：

- 主選單進遊戲：不要試著按 CONTINUE，用 `execute_console_command "coc <CellEditorID>"`（console 在遊戲行程內執行）。
- 啟動遊戲：不要點 MO2 的 Run，用 `skylink-bridge.sh game-restart`（`protontricks-launch` + `moshortcut://:SKSE`，會轉交給已在跑的 MO2 instance）。

## `pkill -f` / `ps | grep` 會匹配到自己的指令字串（2026-07-10）

`pkill -9 -f 'ModOrganizer.exe'` 把發出指令的那個 shell 一起殺了——因為 shell 的 cmdline 裡就含 `ModOrganizer.exe` 這串字。同理 `ps -eo cmd | grep -qi 'SkyrimSE.exe'` 永遠為真。

本檔其他腳本用 `pgrep -f 'SkyrimSE\.ex[e]'` 的字元類技巧正是為此。臨時要殺行程時**用 PID**，不要用 pattern。
