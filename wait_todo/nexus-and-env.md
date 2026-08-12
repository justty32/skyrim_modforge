# wait_todo — 外部資源 / 環境 / bash（不屬任何功能的雜項）

← [WAIT_USER](../WAIT_USER.md)（總入口）

- **環境設定**（env var / 權限 / 本機工具安裝）：（無）
- **等下次真 CTD 才驗得到**：
  - **SkyLink 崩潰復原迴圈的最後一段**（見 [skylink/crash-recovery](../workflows/skylink/crash-recovery.md)）：真 CTD 後 wine 會彈一個對話框（使用者回報，2026-07-10）。已知 MO2 在它啟動的程式執行期間會鎖住自己並**拒絕再啟動 executable**（乾淨退出會自動解鎖，所以 `game-restart` 在那條路徑上可行）。未知 ①該 wine 對話框的行程叫什麼、②崩掉的 `SkyrimSE.exe` 會不會滯留——若滯留則 MO2 停在鎖定、`skylink-bridge.sh game_running()` 也誤判成「還在跑」，`game-restart` 兩邊都過不去。下次真的崩了，先別關視窗，把 `pgrep -a -f '\.ex[e]'` 貼給我，我再把 `game-restart` 的清場邏輯補上（走殺殘骸行程，不走點 UI——桌面輸入受限）。受控 `kill -SEGV` 模擬未做（使用者決定之後遇到再說）。
- **需你跑的 bash / 指令**：
  - **主力機重生真實 quest nodes**：本離線機已用自製 plugin 驗過 `spec → esp → questnodes → JSON Schema`；但沒有 Skyrim Data。回主力機在 `../game-data/` 跑 `./extract.sh`，生成 gitignored `catalog/quest-nodes/<plugin>/`，確認 Skyrim.esm 節點數非零後即可交 AI semantic pass。
  - ~~**編譯 `../scene-capture-bridge/`**~~ **✅ 2026-07-10 完成**（主力機 clang-cl 跨編譯）。真正的缺口不是 MSVC（`~/vcpkg`、`~/.xwin-cache` 早就在）而是**離線落地時只搬了 CMakePresets、沒搬它依賴的 `ports/` overlay**（`commonlibsse-ng-fork/fix-clang-delete.patch` 是 clang-cl 必要修補；`directxtk` 也得走 overlay）。另補 `find_package(directxtk)`。程式碼錯誤僅一個：`ForEachReference` 的 callback 收 `TESObjectREFR*` 不收 reference（本條原本就預測到了）。**實機部分移至 [ingame-tests.md](ingame-tests.md)**。
- **Nexus 下載（美化/body/工具，掃完 ~/skyrim_mods 確認缺）**：
  - **CBBE 3BA**（30174）— OBody 必需的 body framework，現有 CBBE 是舊版
  - **OBody NG**（77016）— 每個 NPC 自動隨機 body preset + ORefit 服裝貼合
  - **AutoBody AE**（61321）— OBody 的輕量替代（zero config randomize）
  - **Modpocalypse NPCs**（54422）或 **Nordic Faces**（40658）— 通用 NPC 美化底座擇一
  - **EasyNPC**（52313）— NPC appearance 合併工具（避免暗臉衝突）
