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
