# wait_todo — 外部資源 / 環境 / bash（不屬任何功能的雜項）

← [WAIT_USER](../WAIT_USER.md)（總入口）

- **環境設定**（env var / 權限 / 本機工具安裝）：（無）
- **等下次真 CTD 才驗得到**：
  - **SkyLink 崩潰復原迴圈的最後一段**（見 [tooling/skylink.md](../workflows/tooling/skylink.md)）：真 CTD 後 wine 會彈一個對話框（使用者回報，2026-07-10）。已知 MO2 在它啟動的程式執行期間會鎖住自己並**拒絕再啟動 executable**（乾淨退出會自動解鎖，所以 `game-restart` 在那條路徑上可行）。未知 ①該 wine 對話框的行程叫什麼、②崩掉的 `SkyrimSE.exe` 會不會滯留——若滯留則 MO2 停在鎖定、`skylink-bridge.sh game_running()` 也誤判成「還在跑」，`game-restart` 兩邊都過不去。下次真的崩了，先別關視窗，把 `pgrep -a -f '\.ex[e]'` 貼給我，我再把 `game-restart` 的清場邏輯補上（走殺殘骸行程，不走點 UI——桌面輸入受限）。受控 `kill -SEGV` 模擬未做（使用者決定之後遇到再說）。
- **需你跑的 bash / 指令**：
  - **編譯 `sub_projs/scene-capture-bridge/`（採集橋 SKSE DLL，Idea #24 元件③）**：離線機無 MSVC/vcpkg/ninja → 骨架與 stub 已離線落地（2026-07-09）但**從未編譯**。主力機 Manjaro 走 clang-cl 跨編譯驗證：先 `xwin --accept-license splat --output ~/.xwin-cache`（首次）+ vcpkg（`VCPKG_ROOT`），再 `cd sub_projs/scene-capture-bridge && cmake --preset build-release-clang-cl-linux && cmake --build build/release-clang-cl-linux`；或 push 觸發 GitHub CI（windows-latest 出 DLL）。首編大概率有 CommonLibSSE API 名稱要修（`ForEachReference` 簽名、`TESFile::IsLight`、`data.location`/`GetFormFlags` 等 stub 內 `TODO(runtime-verify)` 標的點）——編譯錯誤貼回來我修。詳見 [BUILD.md](../sub_projs/scene-capture-bridge/BUILD.md)。
- **Nexus 下載（美化/body/工具，掃完 ~/skyrim_mods 確認缺）**：
  - **CBBE 3BA**（30174）— OBody 必需的 body framework，現有 CBBE 是舊版
  - **OBody NG**（77016）— 每個 NPC 自動隨機 body preset + ORefit 服裝貼合
  - **AutoBody AE**（61321）— OBody 的輕量替代（zero config randomize）
  - **Modpocalypse NPCs**（54422）或 **Nordic Faces**（40658）— 通用 NPC 美化底座擇一
  - **EasyNPC**（52313）— NPC appearance 合併工具（避免暗臉衝突）
