# wait_todo — 外部資源 / 環境 / bash（不屬任何功能的雜項）

← [WAIT_USER](../WAIT_USER.md)（總入口）

- **環境設定**（env var / 權限 / 本機工具安裝）：（無）
- **需你跑的 bash / 指令**：
  - **編譯 `sub_projs/scene-capture-bridge/`（採集橋 SKSE DLL，Idea #24 元件③）**：離線機無 MSVC/vcpkg/ninja → 骨架與 stub 已離線落地（2026-07-09）但**從未編譯**。主力機 Manjaro 走 clang-cl 跨編譯驗證：先 `xwin --accept-license splat --output ~/.xwin-cache`（首次）+ vcpkg（`VCPKG_ROOT`），再 `cd sub_projs/scene-capture-bridge && cmake --preset build-release-clang-cl-linux && cmake --build build/release-clang-cl-linux`；或 push 觸發 GitHub CI（windows-latest 出 DLL）。首編大概率有 CommonLibSSE API 名稱要修（`ForEachReference` 簽名、`TESFile::IsLight`、`data.location`/`GetFormFlags` 等 stub 內 `TODO(runtime-verify)` 標的點）——編譯錯誤貼回來我修。詳見 [BUILD.md](../sub_projs/scene-capture-bridge/BUILD.md)。
- **Nexus 下載（美化/body/工具，掃完 ~/skyrim_mods 確認缺）**：
  - **CBBE 3BA**（30174）— OBody 必需的 body framework，現有 CBBE 是舊版
  - **OBody NG**（77016）— 每個 NPC 自動隨機 body preset + ORefit 服裝貼合
  - **AutoBody AE**（61321）— OBody 的輕量替代（zero config randomize）
  - **Modpocalypse NPCs**（54422）或 **Nordic Faces**（40658）— 通用 NPC 美化底座擇一
  - **EasyNPC**（52313）— NPC appearance 合併工具（避免暗臉衝突）
