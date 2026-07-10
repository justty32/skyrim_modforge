# scene-capture-bridge — 遊戲內採集橋 SKSE DLL（Idea #24 元件③）

← [sub_projs](../README.md)｜契約權威：[ingame-scene-export-design.md](../../workflows/specs/ingame-scene-export-design.md)｜idea：[#24 遊戲內編輯器](../../workflows/idea/tools/24-ingame-editor.md)

**唯一 net-new 的重工程**：一支 SKSE C++ DLL，在遊戲內走訪目標 cell 的 placed refs、讀每個 base + world transform + enable state、把 runtime FormID 反解成耐久 `<plugin>:0xLOCALID`，序列化成 **scene.json** → 餵 ModForge（`dotnet run -- build scene.json`）生成 patch esp。

- **類型**：基石聯動（它的 output 契約 = ModForge 的 input；兩者靠 scene.json 協議接，不整合）
- **契約權威**：scene.json 的每個欄位對映**既有 ModForge spec 型別**，本子專案**只擁有 output 形狀**，生成端全在 ModForge。契約定義見 [spec §契約](../../workflows/specs/ingame-scene-export-design.md)。
- **建置**：[BUILD.md](BUILD.md)（C++23 + CommonLibSSE-NG + vcpkg + CMake presets；靜態 CRT standalone DLL）
- **狀態**：✅ **M4 spike 實機全過**（2026-07-10）。clang-cl 跨編譯產物直接載入遊戲、vanilla diff 成立、`scene.json` → ModForge `build` 整鏈閉環。遊戲內 ImGui 面板亦已實機（F1 → `Scene Capture Bridge` → `Export player cell`）。驗收明細見 [landed/world.md](../../workflows/feature-dev/landed/world.md)。

## 建置架構來源

改編自 [justty32/my_skyrim_plugin_1](https://github.com/justty32/my_skyrim_plugin_1) 的**建置骨架**（CMake/vcpkg/presets/CI/triplet overlay/靜態 CRT/clang-cl 跨編譯），**只借建置架構，plugin 邏輯全自寫**（依契約寫，不照抄內部程式碼）。關鍵沿用：
- `commonlibsse-ng-fork`（Monitor221hz vcpkg registry）+ **`nlohmann-json`**（scene.json 序列化）。
- `build-release-clang-cl-linux` preset → **主力機 Manjaro 可 compile-verify**（不必等 Windows）。
- GitHub Actions（windows-latest 出 DLL + 靜態 CRT 驗證 + MO2 zip）。

## 現在有什麼（`src/`）

| 檔 | 內容 |
|---|---|
| `plugin.cpp` | SKSE 入口 + message handler；`kDataLoaded` 註冊 **F10（scancode 0x44）export hotkey**（sink 形狀抄 my_skyrim_plugin_1 的 `FollowLight::HotkeySink`）|
| `SceneExporter.{h,cpp}` | **核心**：`ExportCell` 走訪 cell → **vanilla diff**（ref 解得出耐久 id ⇒ 既有 ⇒ 跳過；解不出 ⇒ 玩家 `PlaceAtMe` 擺的 ⇒ emit）→ `placements[]`（actor 與物件同一個 list，因 ModSpec 沒有 `npcRefs` 成員）；`ResolveDurableId` FormID→`<plugin>:0xLOCALID`；`WriteSceneFile` 吐 json |
| `UI.{h,cpp}` | 遊戲內面板（[SKSE Menu Framework 3](../mod-survey/findings/skse-menu-framework-3.md) / Dear ImGui）：顯示所在 cell、Export 按鈕、上次匯出的 placements / pre-existing 統計。**軟相依**——`IsInstalled()` 是 `GetModuleHandleW` 探測，沒裝框架就只有 F10 |
| `extern/SKSEMenuFramework/` | vendored 消費者 header（LGPL-2.1，`GetProcAddress` shim，不連結 DLL）|
| `PCH.h` / `log.h` | CommonLibSSE PCH（含 nlohmann）＋ spdlog file logger |

## 尚未做（依 spec 里程碑）

- **PROTEUS clone 的 ref 是 dynamic**：`npcRoles[].actorRef` 需要耐久 ref id，dynamic ref 沒有。PROTEUS 已降為**可選**（預設走 ModForge 直接生的「大眾臉」NPC，ref 耐久），故不阻塞；見 [spec](../../workflows/specs/ingame-scene-export-design.md)「NPC 來源」。
- **§B 語意標記 / §D role tag / §E 滴管·範圍吸取·橡皮擦**：UI 骨架（`src/UI.cpp`）已接上 SKSE Menu Framework，剩下的是把這些工具畫進面板。

## 使用流程：marker → agent → 世界改變（P1，實機閉環 2026-07-10）

玩家側：遊戲內 **F11** 在準星處放 marker（無命中落腳下；面板 `place marker here` 為備援）→ **F1 → Markers** 改 label/kind → **F10** 匯出。存檔重載後按 `adopt this cell` 連名字認領回來。

**agent 對接配方**（拿到需求如「在 goat 放一隻山羊」時照做）：

1. 讀 `.../compatdata/489830/pfx/drive_c/users/steamuser/Documents/My Games/Skyrim Special Edition/SKSE/scene-export.json` 的 `annotations[]`——每筆有 seq/label/kind/position/angleZ/cell 或 worldspace。
2. 查 base：houseCARL `cross_plugin_query`（如 `editorid_contains=EncGoat`）。
3. author spec：`placements[]` 帶 marker 的 position/angleZ（rotation.z）＋歸屬欄位。**⚠️ 外部 NPC base 必須 `"kind": "npc"`**——isNpc 自動判定只認 in-spec base，漏了會生成 REFR（不生怪、無報錯，dump 看到 `PlacedObject` 而非 `PlacedNpc` 即中招）。
4. `build` → `dump` 驗座標與記錄型別 → 產物放 `<MO2>/mods/<新資料夾>/`。
5. 提醒使用者：**MO2 F5 refresh 後新 mod 預設不勾**，要手動勾 mod＋plugin。

先例：`mods/SCB Goat Demo/`（本 README 同日的實機驗收產物）。

## 建置踩坑（2026-07-10 首編）

- **`ports/` overlay 必須存在**。`CMakePresets.json` 的 `vcpkg-clang-linux` 指向 `${sourceDir}/ports`；`commonlibsse-ng-fork/fix-clang-delete.patch` 是 clang-cl 編 CommonLibSSE-NG 的**必要**修補，`directxtk` 也得走 overlay（registry 版在 `x64-windows-skse-clang` 下編不過）。從 `my_skyrim_plugin_1/ports/` 整包搬。
- `CMakeLists.txt` 需 `find_package(directxtk CONFIG REQUIRED)`——CommonLibSSE 的 export target 在 link interface 裡具名 `Microsoft::DirectXTK`。
- **改過 preset / vcpkg.json 後必須 `rm -rf build/release-clang-cl-linux`**。stale `CMakeCache.txt` 會讓 vcpkg.cmake 跳過 chainload toolchain，clang-cl 就不帶 `/winsysroot`，錯誤訊息長得像「編譯器壞了」。
- `ForEachReference` 的 callback 收 `TESObjectREFR*`（指標），不收 reference。

## 里程碑對位（spec §最小垂直切片）

本子專案負責 **M4（採集橋 spike）→ M6**。M0–M2（ModForge 側 `SceneImport` + `SceneNpcRoleSpec`，手寫 scene.json 即可驗）是 ModForge 本命工作、離線可測，**不依賴本 DLL**——兩線並行。
