# scene-capture-bridge — 遊戲內採集橋 SKSE DLL（Idea #24 元件③）

← [sub_projs](../README.md)｜契約權威：[ingame-scene-export-design.md](../../workflows/specs/ingame-scene-export-design.md)｜idea：[#24 遊戲內編輯器](../../workflows/idea/tools/24-ingame-editor.md)

**唯一 net-new 的重工程**：一支 SKSE C++ DLL，在遊戲內走訪目標 cell 的 placed refs、讀每個 base + world transform + enable state、把 runtime FormID 反解成耐久 `<plugin>:0xLOCALID`，序列化成 **scene.json** → 餵 ModForge（`dotnet run -- build scene.json`）生成 patch esp。

- **類型**：基石聯動（它的 output 契約 = ModForge 的 input；兩者靠 scene.json 協議接，不整合）
- **契約權威**：scene.json 的每個欄位對映**既有 ModForge spec 型別**，本子專案**只擁有 output 形狀**，生成端全在 ModForge。契約定義見 [spec §契約](../../workflows/specs/ingame-scene-export-design.md)。
- **建置**：[BUILD.md](BUILD.md)（C++23 + CommonLibSSE-NG + vcpkg + CMake presets；靜態 CRT standalone DLL）
- **狀態**：🟡 **骨架 + SceneExporter 實作 stub 離線落地（2026-07-09）**，未編譯、未實機。

## 建置架構來源

改編自 [justty32/my_skyrim_plugin_1](https://github.com/justty32/my_skyrim_plugin_1) 的**建置骨架**（CMake/vcpkg/presets/CI/triplet overlay/靜態 CRT/clang-cl 跨編譯），**只借建置架構，plugin 邏輯全自寫**（依契約寫，不照抄內部程式碼）。關鍵沿用：
- `commonlibsse-ng-fork`（Monitor221hz vcpkg registry）+ **`nlohmann-json`**（scene.json 序列化）。
- `build-release-clang-cl-linux` preset → **主力機 Manjaro 可 compile-verify**（不必等 Windows）。
- GitHub Actions（windows-latest 出 DLL + 靜態 CRT 驗證 + MO2 zip）。

## 現在有什麼（`src/`）

| 檔 | 內容 |
|---|---|
| `plugin.cpp` | SKSE 入口 + message handler；`kDataLoaded` 就緒（export 觸發器待 M4 接 hotkey/console/ImGui）|
| `SceneExporter.{h,cpp}` | **核心**：`ExportCell` 走訪 cell → `placements[]`（static+transform+scale+enable）/`npcRefs[]`（actor）；`ResolveDurableId` FormID→`<plugin>:0xLOCALID`（TESFile origin + ESL/full 遮罩）；`WriteSceneFile` 吐 json |
| `PCH.h` / `log.h` | CommonLibSSE PCH（含 nlohmann）＋ spdlog file logger |

## 尚未做（依 spec 里程碑）

- **編譯驗證**：離線機無 MSVC/vcpkg → 待主力機 clang-cl 或 CI。見 [WAIT_USER](../../WAIT_USER.md)。
- **`ResolveDurableId` 的 ESL 局部 ID 寬度**、`data.location` 對 exterior 的座標語意、`InitiallyDisabled` flag 讀法 → 皆標 `TODO(runtime-verify)`，需實機對真實 ref 核。
- **§B 語意標記 / §D role tag / §E 滴管·範圍吸取·橡皮擦 removals[]**：這些不是裸 cell sweep 能產出的，要遊戲內編輯 UI（ImGui / SKSE Menu Framework 3），M4 之後接。
- **export 觸發器**（hotkey → `ExportPlayerCellToFile()`）＝ M4 spike 的最小可驗面。

## 里程碑對位（spec §最小垂直切片）

本子專案負責 **M4（採集橋 spike）→ M6**。M0–M2（ModForge 側 `SceneImport` + `SceneNpcRoleSpec`，手寫 scene.json 即可驗）是 ModForge 本命工作、離線可測，**不依賴本 DLL**——兩線並行。
