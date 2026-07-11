# scene-capture-bridge — 遊戲內採集橋 SKSE DLL（Idea #24 元件③）

← [sub_projs](../README.md)｜契約權威：[ingame-scene-export-design.md](../../workflows/specs/ingame-scene-export-design.md)｜idea：[#24 遊戲內編輯器](../../workflows/idea/tools/24-ingame-editor.md)

**唯一 net-new 的重工程**：一支 SKSE C++ DLL，在遊戲內走訪目標 cell 的 placed refs、讀每個 base + world transform + enable state、把 runtime FormID 反解成耐久 `<plugin>:0xLOCALID`，序列化成 **scene.json** → 餵 ModForge（`dotnet run -- build scene.json`）生成 patch esp。

- **類型**：基石聯動（它的 output 契約 = ModForge 的 input；兩者靠 scene.json 協議接，不整合）
- **契約權威**：scene.json 的每個欄位對映**既有 ModForge spec 型別**，本子專案**只擁有 output 形狀**，生成端全在 ModForge。契約定義見 [spec §契約](../../workflows/specs/ingame-scene-export-design.md)。
- **建置**：[BUILD.md](BUILD.md)（C++23 + CommonLibSSE-NG + vcpkg + CMake presets；靜態 CRT standalone DLL）
- **狀態**：✅ **P1–P3 主線實機全過**（2026-07-11；前情 M4 spike＋P1 marker 閉環 2026-07-10）；**P5 模式制已實作待實機**（同日）——`sc` console 指令＋每模式鍵位＋SKSE co-save，**F 直達鍵全數移除**。面板：F1 → `Scene Capture Bridge`。驗收明細見 [landed/world.md](../../workflows/feature-dev/landed/world.md)；殘項見 [wait_todo/ingame-tests.md](../../wait_todo/ingame-tests.md)。

## 建置架構來源

改編自 [justty32/my_skyrim_plugin_1](https://github.com/justty32/my_skyrim_plugin_1) 的**建置骨架**（CMake/vcpkg/presets/CI/triplet overlay/靜態 CRT/clang-cl 跨編譯），**只借建置架構，plugin 邏輯全自寫**（依契約寫，不照抄內部程式碼）。關鍵沿用：
- `commonlibsse-ng-fork`（Monitor221hz vcpkg registry）+ **`nlohmann-json`**（scene.json 序列化）。
- `build-release-clang-cl-linux` preset → **主力機 Manjaro 可 compile-verify**（不必等 Windows）。
- GitHub Actions（windows-latest 出 DLL + 靜態 CRT 驗證 + MO2 zip）。

## 操作模型（P5 模式制，2026-07-11 全數拍板）

**一次一個模式，console 切換；每個模式一格動作鍵（面板可改綁、允許重複、預設全 F11）。** F6/F7/F8/F10 直達鍵**不存在**（使用者拍板：整個移除，非預設關）；export 只走面板 Export 鈕。

```
sc mk    打標記模式（動作鍵＝準星處放 marker）
sc del   刪除模式（動作鍵＝擦準星目標）
sc pk    滴管吸取模式（動作鍵＝吸準星目標進 palette）
sc pl    擺放模式（動作鍵＝把選中插槽擺在準星處）
sc ed    編輯模式（動作鍵＝選中準星目標進 numpad 微調）
sc off   啥都不做
sc       印當前模式＋用法
sc mk dp0 / dp1        隱藏／顯示所有 marker 光球（純視覺，登記簿與匯出不受影響）
sc del|pk|ed er0 / er1  該模式動作鍵用準星／物理射線（樹、純裝飾 static）
sc ed ax                進編輯「純旋轉子模式」（見下）；回普通模式打 sc ed
sc delc                 擦除 console 滑鼠點選的 ref（先只做物件，非 actor）
sc cap / sc cap r       擷取準星／射線目標的附魔＋效果（附魔武防、藥水、材料）進 capturedItems[]
```

**編輯純旋轉子模式**：`sc ed ax` 進入，**`sc ed` 退回**普通移動模式。ON 時 numpad 方向鍵改成旋轉——**4/6＝yaw、1/3＝pitch、7/9＝roll、8/2＝角度歸零**（位置/縮放不動），numpad 5 也＝角度歸零；OFF（預設）時 8/2 前後、4/6 左右、1/3 升降、7/9 yaw，numpad 5＝復原到編輯前姿態。

編輯模式的目標若是 **marker 光球**：numpad 微調＋0 commit＝**移動該 marker**（更新登記簿座標，不進 overrides）。`er` 切換、旋轉子模式、編輯步長全部**存進存檔**（co-save SETT v3）。

Export 頁有 **Export player cell** 與 **Export all (loaded cells)** 兩鈕：registries（marker/擦除/override）本就全 cell 全域，`all` 只多掃**已載入的其他 cell** 的 placements（未載入 cell 的 placements 撈不到，log 會講）。Palette 頁的具名檔可 **load from file（追加）／ save to file**。

模式內操作不算佔鍵：numpad 編輯（8/2/4/6/1/3 位移、7/9 yaw、+/− 縮放、0 commit、`.` cancel、**5＝復原到編輯前姿態並續留編輯**）與 **numpad \*＝射線選取**照舊；位移/yaw/縮放**步長在 Settings 頁可調**（存 co-save）。當前模式/dp 狀態＋三個步長＋三本登記簿全部**存進存檔**（SKSE co-save，使用者的無 ini 原則）。動作鍵目前固定 F11（rebind 暫時隱藏，捕捉流程待重作）。`sc` 指令的實作＝劫持一個 retail 無作用的 vanilla console 指令（候選鏈首個命中者，2026-07-11 實機 donor＝`ClearAchievement`；全滅時面板 Settings 頁照樣能切模式）。

## 現在有什麼（`src/`）

| 檔 | 內容 |
|---|---|
| `plugin.cpp` | SKSE 入口 + message handler；input sink 三層：rebind 捕捉 → 編輯模式內部鍵 → 當前模式動作鍵（sink 形狀抄 my_skyrim_plugin_1 的 `FollowLight::HotkeySink`）|
| `Modes.{h,cpp}` | P5 模式管理：一次一模式、每模式鍵位（預設 F11、允許重複）、動作分派、rebind 流程（下一鍵完成、Esc 取消）|
| `Console.{h,cpp}` | `sc` console 指令（ObScript 劫持：`LocateConsoleCommand` 改寫 inert donor 的 name/params/executeFunction）|
| `CoSave.{h,cpp}` | SKSE SerializationInterface（UID `'SCBR'`）：設定＋Markers/Eraser/Overrides/Captures 四本登記簿隨存檔走；revert 只清登記不碰世界；FormID 經 `ResolveFormID` 重解析（Captures 只存耐久 id，無 handle）|
| `UI.Settings.cpp` | Settings 頁：模式切換鈕、每模式鍵位表＋rebind、marker 光球開關；`UI::ModeLine()` 各頁頂部常駐當前模式 |
| `SceneExporter.{h,cpp}` | **核心**：`ExportCell` 走訪 cell → **vanilla diff**（ref 解得出耐久 id ⇒ 既有 ⇒ 跳過；解不出 ⇒ 玩家 `PlaceAtMe` 擺的 ⇒ emit）→ `placements[]`（actor 與物件同一個 list，因 ModSpec 沒有 `npcRefs` 成員）；`ResolveDurableId` FormID→`<plugin>:0xLOCALID`；`WriteSceneFile` 吐 json |
| `UI.{h,cpp}` | 遊戲內面板（[SKSE Menu Framework 3](../mod-survey/findings/skse-menu-framework-3.md) / Dear ImGui）：顯示所在 cell、Export 按鈕、上次匯出統計；Eraser/Palette/Editor 各頁帶 **`… by ray` 明示射線鈕**與 **this cell only 過濾**。**軟相依**——`IsInstalled()` 是 `GetModuleHandleW` 探測，沒裝框架就只有 hotkey |
| `UI.Markers.cpp` | Markers 頁（this-cell 過濾、每列 `edit` 鈕）＋ **marker 編輯視窗**（E 按 marker 開啟：label／kind／**note 多行**／delete；`AddWindow` 獨立視窗，開著會暫停遊戲收輸入）|
| `extern/SKSEMenuFramework/` | vendored 消費者 header（LGPL-2.1，`GetProcAddress` shim，不連結 DLL）|
| `Aim.{h,cpp}` | 共用視角射線＋**兩種選取入口**：`CrosshairRef()`（互動準星，老手感）與 `RayRef()`（物理射線→反查 ref，樹/純裝飾 static 用）。**射線絕不做自動 fallback**（使用者拍板 2026-07-11）——牆/地板都是 ref，自動 fallback 會把「按空」變誤抓；射線只走明示按鈕/專用鍵 |
| `Eraser.{h,cpp}` | 橡皮擦（`sc del` 模式動作）：authored→disable＋登記→`removals[]`；自己的 dynamic→真刪除無痕；entry 記 name/座標/cell（面板逐列顯示＋過濾）；undo 逐列/逐 cell/最近一筆；`erase by ray` 明示射線入口。（`scan disabled` 跨存檔救援已移除——co-save 持久化耐久 id 後冗餘）|
| `Palette.{h,cpp}` | 滴管（`sc pk` 吸、`sc pl` 擺；runtime-only base 拒收）；`pick by ray` 明示入口；**插槽落盤 `scene-capture-palette.json`（跨存檔跨 session）**，base 解析不回（plugin 移除）標 unavailable 不炸 |
| `Captures.{h,cpp}` | 定義擷取器（Palette 的姊妹：吸「沒有耐久 base 可引用」的內容）。`sc cap`／面板讀 live form 的語意內容 → ModForge **鑄新記錄**。**①物品**：附魔武防（實例 ExtraEnchantment 優先，否則 base formEnchanting）＋藥水/材料效果 → `capturedItems[]`（效果 shape = EffectSpec）。**②NPC**：`TESNPC` 外貌（race/sex/weight/height＋headParts/tintLayers/faceMorphs+parts/hair/skin/FTST/outfit）＋**base perks（id+rank）**＋**當前 buff（active-effect 快照：source spell＋MGEF＋mag/dur/elapsed）**＋**旗標（unique/dead/essential/protected）**＋擺位 → `capturedNpcs[]`。**唯一 NPC 也收**（2026-07-11 使用者反轉，帶 `unique` 旗標給 ModForge 判斷）。登記簿隨 co-save（record `'SCCP'` v3，只存耐久 id）。**⚠️ NPC 待驗/未涵蓋**：(a) PROTEUS 若用 NiNode live override 不寫 TESNPC，擷到的臉是 base 的非套用後的；(b) **身形/臉部「mesh」本身不收**——只收「定義」（headParts+morphs+race+weight，臉/身是由這些＋facegen 烘焙生成的），baked FaceGeom nif 與 RaceMenu/NiOverride 雕塑不在 TESNPC，需 facegen 烘焙＝ModForge 下游活 |
| `Editor.{h,cpp}` | 編輯模式（`sc ed` 動作鍵選中準星目標；**numpad \* ＝射線選取**）→ numpad 微調（8/2/4/6/1/3 位移、7/9 yaw、+/− 縮放、0 commit、. cancel、**5＝復原續編**）；步長 runtime 可調（Settings/co-save）；havok-movable 類型編輯期物理凍結；自己的 ref＝live pose 直接匯出（不進 overrides 列，正常），**authored ref＝commit 時登記進 Overrides**（2026-07-11 契約拍板）|
| `Overrides.{h,cpp}` | authored ref 被編輯 commit 後的登記簿（比照 Eraser：明示、不 diff——havok 噪音）→ 匯出頂層 `overrides[]`（ref/position/rotation°/scale；actor 不帶 scale）；Editor 面板頁逐筆/全部 revert 回 baseline |
| `PCH.h` / `log.h` | CommonLibSSE PCH（含 nlohmann）＋ spdlog file logger |

## 尚未做（依 spec 里程碑）

- **PROTEUS clone 的 ref 是 dynamic**：`npcRoles[].actorRef` 需要耐久 ref id，dynamic ref 沒有。PROTEUS 已降為**可選**（預設走 ModForge 直接生的「大眾臉」NPC，ref 耐久），故不阻塞；見 [spec](../../workflows/specs/ingame-scene-export-design.md)「NPC 來源」。
- **§B 語意標記 / §D role tag / §E 滴管·範圍吸取·橡皮擦**：UI 骨架（`src/UI.cpp`）已接上 SKSE Menu Framework，剩下的是把這些工具畫進面板。

## 使用流程：marker → agent → 世界改變（P1，實機閉環 2026-07-10）

玩家側：console `sc mk` 進標記模式 → **動作鍵（預設 F11）**在準星處放 marker（無命中落腳下；面板 `place marker here` 為備援）→ 對著 marker **按 E 開編輯視窗**（改 label/kind、寫 **note** 給 agent 的補充指示、刪除）或 **F1 → Markers** → **F1 → Export 鈕**匯出。登記簿隨存檔走（co-save）；跨存檔撿孤兒才用 `adopt this cell`。
marker 的樣子＝**鐵匕首**（`Weapons\Iron\IronDagger.nif`，houseCARL 對 WEAP 01397E 驗過）——換掉靈魂石是因為 marker 現在會**記錄＋可編輯完整朝向與大小**，匕首的**劍尖方向**剛好把朝向視覺化。有碰撞才能被 E/準星選到；weapon clutter havok 會掉 → 放置當下 `SetMotionType(kKeyframed)` 凍住。marker 匯出的 `annotations[]` 現在帶 `rotation{x,y,z}`＋`scale`（`angleZ` 仍在＝`rotation.z`，向後相容）。

**agent 對接配方**（拿到需求如「在 goat 放一隻山羊」時照做）：

1. 讀 `.../compatdata/489830/pfx/drive_c/users/steamuser/Documents/My Games/Skyrim Special Edition/SKSE/scene-export.json` 的 `annotations[]`——每筆有 seq/label/kind/position/angleZ/cell 或 worldspace。
2. 查 base：houseCARL `cross_plugin_query`（如 `editorid_contains=EncGoat`）。
3. author spec：`placements[]` 帶 marker 的 position/angleZ（rotation.z）＋歸屬欄位。**⚠️ 外部 NPC base 必須 `"kind": "npc"`**——isNpc 自動判定只認 in-spec base，漏了會生成 REFR（不生怪、無報錯，dump 看到 `PlacedObject` 而非 `PlacedNpc` 即中招）。
4. `build` → `dump` 驗座標與記錄型別 → 產物放 `<MO2>/mods/<新資料夾>/`。
5. 提醒使用者：**MO2 F5 refresh 後新 mod 預設不勾**，要手動勾 mod＋plugin。

先例：`mods/SCB Goat Demo/`（本 README 同日的實機驗收產物）。

## 持久化與 adopt 語意（P5 co-save 後全面升級）

DLL 有兩層狀態，**P5 起兩層都隨存檔走**：

1. **存檔（savegame）**：所有實際操作本來就持久——擺的動態 ref（`0xFF......`）連同 transform、擦除的 `Disable()` 狀態、marker proxy 連同顯示名。2026-07-11 實機確認。
2. **登記簿 → SKSE co-save（`CoSave.cpp`）**：Markers（**含 note**）/Eraser/Overrides 三本登記簿＋設定（模式/鍵位/dp 狀態）以 UID `'SCBR'` 掛在**每個存檔**旁，讀檔自動跟回來——**關遊戲重開不再歸零**。讀一個沒有我們記錄的存檔＝乾淨預設（revert 先清）。

匯出的 vanilla diff 判別依舊無狀態（ref 解得出來源檔 ⇒ authored 跳過；解不出 ⇒ 玩家放的）。co-save 後的持久化對照：

| 東西 | 誰記得 | 重開遊戲後匯出 |
|---|---|---|
| 新增物件（擺的、丟在地上的裝備） | 存檔（動態 ref） | **自動**——身份證在 ref 自己身上，從來不需要登記 |
| 真刪除的自家物件 | 存檔（disabled 動態 ref） | 自動跳過（無痕） |
| marker（位置/label/kind/**note**） | **co-save 登記簿**＋存檔裡的 proxy | **自動**——proxy 是動態 ref、FormID 過完整重啟未必重解析，故讀檔時 co-save 認不回的那筆改**自動 adopt**（`kPostLoadGame` 掃當前 cell），並用**座標配對**把 co-save 的 note/kind 貼回撿到的光球；別的 cell 走過去仍靠 `adopt this cell` |
| 擦除 vanilla/mod 物件 | **co-save 登記簿**＋存檔 disable 狀態 | **自動**進 `removals[]` |
| 移動 authored ref（overrides） | **co-save 登記簿**（baseline＋commit pose）＋存檔 live pose | **自動**進 `overrides[]`，revert 也還能回 baseline |
| Palette 插槽 | **磁碟**（`scene-capture-palette.json`） | 天生跨存檔；plugin 移出 load order 的槽標 unavailable |
| Captures 擷取定義（物品附魔/效果、NPC 外貌） | **co-save 登記簿**（record `'SCCP'` v2，純耐久 id） | **自動**進 `capturedItems[]`／`capturedNpcs[]`；無 handle，讀檔即回 |
| 模式/鍵位/dp 狀態 | **co-save** | 隨存檔還原 |

**adopt 降級為救援機制**：marker 的 `adopt this cell` 現在讀檔會**自動跑一次**（掃當前 cell），只有跨到別的 cell 才需手動按。擦除的 `scan disabled refs` 已整個移除——co-save 存的是耐久 id，重解析穩定，跨存檔救援冗餘。真要**換一個存檔**撿另一條時間線的 marker，走 Markers 頁 `adopt this cell`。

**2026-07-11 實機驗收**：F11 準星放置（pitch 正確）、F8 擦除/undo、F6/F7 滴管（含姿態）、numpad 編輯（5 選/3 升/0 commit/. 取消還原；編輯中 log 出現的 unmapped `0x11`/`0x1F`/`0x20`/`0x38` 是 WASD/Alt，非 numpad 問題）、物理凍結→commit→沉降（匯出為沉降後姿態）、F10 匯出→ModForge build→esp 閉環（removals 深埋 Z-30000、Tamriel override 自動帶 TopCell、ESL）。

## 建置踩坑（2026-07-10 首編）

- **`ports/` overlay 必須存在**。`CMakePresets.json` 的 `vcpkg-clang-linux` 指向 `${sourceDir}/ports`；`commonlibsse-ng-fork/fix-clang-delete.patch` 是 clang-cl 編 CommonLibSSE-NG 的**必要**修補，`directxtk` 也得走 overlay（registry 版在 `x64-windows-skse-clang` 下編不過）。從 `my_skyrim_plugin_1/ports/` 整包搬。
- `CMakeLists.txt` 需 `find_package(directxtk CONFIG REQUIRED)`——CommonLibSSE 的 export target 在 link interface 裡具名 `Microsoft::DirectXTK`。
- **改過 preset / vcpkg.json 後必須 `rm -rf build/release-clang-cl-linux`**。stale `CMakeCache.txt` 會讓 vcpkg.cmake 跳過 chainload toolchain，clang-cl 就不帶 `/winsysroot`，錯誤訊息長得像「編譯器壞了」。
- `ForEachReference` 的 callback 收 `TESObjectREFR*`（指標），不收 reference。

## 里程碑對位（spec §最小垂直切片）

本子專案負責 **M4（採集橋 spike）→ M6**。M0–M2（ModForge 側 `SceneImport` + `SceneNpcRoleSpec`，手寫 scene.json 即可驗）是 ModForge 本命工作、離線可測，**不依賴本 DLL**——兩線並行。
