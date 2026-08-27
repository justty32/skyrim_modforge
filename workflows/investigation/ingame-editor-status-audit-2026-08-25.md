# 遊戲內編輯器現況盤點（2026-08-25）

> ⚠️ **2026-08-27 起，這份是「待複驗清單」，不是事實。** 本檔記載的 16 處文件與實體不符，
> 有一部分在寫完之後已經被修掉（`839a787` 就處理了好幾條），另有至少一條**當初的判定本身就錯**
> （§3 卡點 8 的 Godot binary，見該處批註）。**引用本檔任何一條之前先自己驗一次**；
> 驗過的請就地加上 `【2026-08-27 複驗】` 批註，不要只在別處寫結論。
> 已複驗：§3 卡點 8、§4 第 15 條。

## 結論先行

這條線已有一條可用但不是全自動的閉環：`scene-capture-bridge` 能在遊戲內以 console／模式快捷鍵記錄玩家擺放、刪除、移動、標記、引用與 capture，輸出合法 ModSpec JSON；ModForge 能直接把其中的具體 record 段生成 ESP。真正尚未接通的是兩處：

1. 「施法即編輯」尚無 `TESSpellCastEvent`／SPEL 觸發實作，現況是 console、F11 與 F1 面板。
2. `annotations[]` 只是給人／agent 讀的 advisory marker，ModForge 明確不生成它；遊戲內 NAVM 採集更只有 `kind="navmesh"` 字串，沒有 `navPatches[]` producer。

納入四個 subproject：ModForge、scene-capture-bridge、my_skyrim_plugin_1、godot-worldspace-editor。Godot editor 屬於本線，因它負責離線地形／粗場景，README 明列最後以 scene-capture-bridge 在遊戲內微調並回餵 ModForge（`projects/godot-worldspace-editor/README.md:7-19`）。`my_skyrim_plugin_1` 依交辦納入核對，但它不是「薄記錄器」；它只是 scene-capture-bridge 的建置骨架來源。

---

## 1. 實體產物清單

### 1.1 ModForge

- **能跑的東西**：`ModForge.Cli`（spec JSON → ESP/ESL、catalog 等）與 `ModForge.Core` class library。CLI 的 `build` 直接 `ReadSpec` → `Generator.Build` → `PluginIo.Write`（`src/ModForge.Cli/Commands/Program.Build.cs:14-28`）。
- **語言／建置**：C#、.NET 10、MSBuild；`src/ModForge.Cli/ModForge.Cli.csproj:1-16` 與 `src/ModForge.Core/ModForge.Core.csproj:1-18`。
- **HEAD／分支／工作樹**：
  - HEAD `6feef86efa259d2570fa7fac18b4248ef2fdb76c`
  - branch `feat/placement-drift`
  - upstream `origin/feat/placement-drift`，ahead/behind `0/0`
  - `git status --short` 無輸出，repo 內部乾淨。
- **實體產物**：本次測試生成 `src/ModForge.Cli/bin/Debug/net10.0/ModForge.Cli.dll`，311,808 bytes；它是 .NET console assembly。
- **測試怎麼跑**：`workflows/testing.md:11-22` 指定離線 suite：

  ```bash
  dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"
  ```

- **本次真實結果**（為禁止下載加 `--no-restore`；耗時命令已套 `nice -n 19 taskset -c 6-15`）：

  ```text
  Passed! - Failed: 0, Passed: 1190, Skipped: 0, Total: 1190, Duration: 2 s
  exit 0
  ```

### 1.2 scene-capture-bridge

- **能跑的東西**：`SceneCaptureBridge.dll`，真正的遊戲內薄記錄器。SKSE 入口、input sink、console `sc`、UI、co-save 都在 `src/plugin.cpp:60-178`；輸出器在 `src/SceneExporter.cpp:657-876`。
- **語言／建置**：C++23、CommonLibSSE-NG、nlohmann-json、CMake/vcpkg/Ninja，以 Linux clang-cl + lld-link + xwin 交叉編 Windows DLL；`CMakeLists.txt:1-33`、`BUILD.md:1-24`。
- **HEAD／分支／工作樹**：
  - HEAD `a17e460ebdc1fea8da65b00feb8fcc8ecba1dde2`
  - branch `feat/ghost-camera-ray-2026-08-22`
  - upstream `origin/feat/ghost-camera-ray-2026-08-22`，ahead/behind `0/0`
  - `git status --short` 無輸出，乾淨。
- **實體產物**：`build/release-clang-cl-linux/SceneCaptureBridge.dll`，1,906,176 bytes，PE32+ Windows x86-64 DLL，mtime `2026-08-22 09:52:35 +0800`，SHA-256 `dccc10e04f74e46b72d322e6e92e3c5d000eb0d495f4acdb9c057d93ed43fd67`。
- **建置／測試怎麼跑**：DLL 用 `cmake --preset build-release-clang-cl-linux && cmake --build build/release-clang-cl-linux`（`BUILD.md:99-104`）；portable catalog tests 的正式流程見 `BUILD.md:24-58` 與 `tests/CMakeLists.txt:1-62`。
- **本次真實結果**：

  ```text
  cmake --build build/release-clang-cl-linux
  ninja: no work to do.
  exit 0

  ctest --test-dir build/release-clang-cl-linux --output-on-failure
  No tests were found!!!
  exit 0
  ```

  主 DLL build tree 沒登記 CTest，故另以 `/tmp` build、系統 GNU C++ 16.2.1、既有 nlohmann-json 3.11.2 cache 跑 repo 自帶的 portable suite；沒有下載，完後已清掉 `/tmp` 產物：

  ```text
  catalog_file_tests ............... Passed
  modforge_catalog_contract ........ Passed
  100% tests passed, 0 tests failed out of 2
  Total Test time (real) = 11.39 sec
  exit 0
  ```

### 1.3 my_skyrim_plugin_1

- **能跑的東西**：`DaylightDungeon.dll`，功能是 FollowLight、AmbientBoost、NpcGenerator，不是 scene recorder。實際 build source list 只有 `plugin/hook/NpcGenerator/FollowLight/AmbientBoost`（`cmake/sourcelist.cmake:1-7`），初始化也只呼叫這三個功能（`src/plugin.cpp:7-13`）。
- **它與本線的關係**：scene-capture-bridge README 明說只借它的 CMake/vcpkg/preset/CRT 建置骨架，plugin 邏輯全自寫（`projects/scene-capture-bridge/README.md:15-21`）。所以它是祖先模板／旁支，不在 scene JSON 資料流上。
- **語言／建置**：C++23、CommonLibSSE-NG、CMake/vcpkg；project name `DaylightDungeon`（`CMakeLists.txt:1-46`）。
- **HEAD／分支／工作樹**：
  - HEAD `129ca7f1d33ad4bfff1ae6be017591eff3f30dad`
  - branch `main`
  - upstream `origin/main`，ahead/behind `0/0`
  - `git status --short` 無輸出，乾淨。
- **實體產物**：已有 `build/release-clang-cl-linux/DaylightDungeon.dll`，1,093,632 bytes，PE32+ Windows x86-64 DLL，mtime `2026-08-02 17:34:24 +0800`，SHA-256 `9f19924142ae9944dbd637d638a12513eccc93b8f26a70742fa1327992084ddf`。它早於目前 HEAD，不能當作本次 HEAD build 成功證據。
- **測試怎麼跑**：README 指定 `scripts/test_packaging.ps1`（`README.md:127-133`）與 `scripts/test_quest_prf.ps1`（`README.md:135-152`）；repo 明說沒有 C++ test suite（`CLAUDE.md:34-36`）。
- **本次真實結果**：

  ```text
  cmake --build build/release-clang-cl-linux
  CMake Error: Could not find toolchain file:
    /home/lorkhan/vcpkg/scripts/buildsystems/vcpkg.cmake
  ninja: error: rebuilding 'build.ninja': subcommand failed
  exit 1
  ```

  真因是既有 `CMakeCache.txt` 仍釘舊路徑 `/home/lorkhan/vcpkg/...`，而本機 `VCPKG_ROOT` 是 `/home/lorkhan/dev/vcpkg`；依交辦不修。`command -v pwsh` 為 `MISSING`，所以兩支正式 PowerShell 測試均跑不動；沒有把腳本改寫成別的測試，也沒有安裝 PowerShell。

### 1.4 godot-worldspace-editor

- **能跑的東西**：Godot 4.6 project `Worldspace Editor`，main scene `res://main.tscn`（`godot/project.godot:9-16`）；可編 heightmap/splatmap、擺物件並匯出 `placements.json`。Exporter 的 v1 `godot4_y_up` contract 在 `godot/placements_io.gd:1-64`。
- **語言／建置**：GDScript / Godot 4.6；沒有 standalone build artifact。測試 harness 是 Python stdlib `unittest`。
- **HEAD／分支／工作樹**：
  - HEAD `5b0abb1af61996563074eef4ac24d1efc9817ddf`
  - **detached HEAD**；該 commit 同時是 `origin/main`
  - 無 branch upstream（`HEAD does not point to a branch`）
  - `git status --short` 無輸出，乾淨。
- **測試怎麼跑**：README `:82-100` 指定兩支：

  ```bash
  python tests/test_placements_contract.py
  python tests/test_model_fetch_contract.py
  ```

- **本次真實結果**（兩支都套指定 CPU/priority）：

  ```text
  test_placements_contract.py:
    source contract: ok
    runtime producer/E2E: skipped 'Godot not found; set GODOT_BIN...'
    Ran 1 test; OK (skipped=1); exit 0

  test_model_fetch_contract.py:
    source contract: ok
    live converter/ModelFetch: skipped 'Godot not found...'
    Ran 1 test; OK (skipped=1); exit 0
  ```

  `godot4`、`godot` 皆不在 PATH；依限制沒有下載或開 GUI。兩支 harness 本來就把缺 Godot 明示為 skip（`tests/test_placements_contract.py:91-103`、`tests/test_model_fetch_contract.py:113-120`）。

---

## 2. 資料流接通到哪

| 段 | 判定 | 實體證據 |
|---|---|---|
| **施法觸發** | **空的** | 全 `scene-capture-bridge/src` 無 `TESSpellCastEvent`；backlog 把 A2 明列為待做（`workflows/plans/scene-capture-bridge/backlog.md:21-54`）。 |
| **console／快捷鍵／面板觸發** | **已接通** | `OnDataLoaded` 註冊 HotkeySink、console、UI（`projects/scene-capture-bridge/src/plugin.cpp:105-126`）；模式 action key 分派 marker/delete/pick/place/edit/capture/referrer（`src/Modes.cpp:106-125`）；`sc` parser 與操作面在 `src/Console.cpp:39-57,95-230`。 |
| **記錄** | **已接通** | marker 登記 `Markers::PlaceAimed/PlaceAtPlayer`（`src/Markers.cpp:91-125`）；placement ownership registry 擋掉魚／critter（`src/SceneExporter.cpp:217-235`）；eraser/override/annotation registries 出口（`src/SceneExporter.cpp:303-370`）；capture/referrer 同樣有 registry。SKSE co-save 正式註冊 save/load/revert callback（`src/CoSave.cpp:808-820`）。 |
| **匯出 JSON** | **已接通，但只掃 loaded cells** | `ExportCell`/`ExportAll` 組 placements + registries + references + minted items（`src/SceneExporter.cpp:657-703`），`ExportCaptures` 分檔（`:712-722`），`WriteSceneFile` 與 timestamp/never-clobber path（`:743-876`）。`ExportAll` 明確只掃 attached cells，已 unload 的物件取不到（`:683-702`）。 |
| **ModForge 讀取 scene JSON** | **已接通** | scene JSON 本身就是 ModSpec；CLI `ReadSpec` 直接 deserialize，並對未知欄位報告（`src/ModForge.Cli/Program.cs:51-72`）。原先規劃的 `SceneImport` 沒有實作，也不需要（`workflows/plans/ingame-scene-export.md:17-29,97-102`）。 |
| **具體 record 消化**（placements/removals/overrides/references/captures/npcRoles） | **已接通** | build 順序實際呼叫 `BuildPlacements`、`BuildReferences`、`BuildOverrides`、`BuildRemovals`、`BuildNavPatches`（`src/ModForge.Core/Build/Generator.Build.cs:121-139`）；`npcRoles[]` 在 pass 0 展開（`src/ModForge.Core/Macros/Generator.SceneNpcRoles.cs:18-23,44-79`）。 |
| **semantic marker 消化**（annotations → 真 record） | **部分** | JSON 能 deserialize，但 `AnnotationSpec` 明寫 advisory-only、build 永不生成（`src/ModForge.Core/Spec/Spec.Annotations.cs:3-26`），CLI 也印 `not built`（`src/ModForge.Cli/Commands/Program.Build.cs:34-36`）。所以 marker → XMRK/HAZD/NPC/tag 仍需人／agent 把意圖改寫成真 spec 段。 |
| **ESP 生成** | **已接通**（限真 spec 段） | `Generator.Build` 後 `PluginIo.Write`（`Program.Build.cs:14-28`）；本次 1190 離線測全綠。先前 M2 blacksmith 問候已有實機證據（`workflows/plans/ingame-scene-export.md:17-29`）。 |
| **NAVM 生成端** | **已接通** | `NavPatchSpec` 已定義 interior polygon contract（`src/ModForge.Core/Spec/Spec.NavPatches.cs:3-20`）；`BuildNavPatches` clone、append/stitch、成功後才落 CELL/NAVM（`src/ModForge.Core/Build/Generator.Build.NavPatches.cs:12-59`）。P3 先前實機雙向通行 PASS（`workflows/feature-dev/landed/world.md:136-152`）。 |
| **遊戲內 NAVM 採集 → navPatches[]** | **空的** | bridge 只有 `Markers::Entry.kind = "navmesh"` 的字串／seq（`projects/scene-capture-bridge/src/Markers.h:23-39`）；它匯出成 inert `annotations[]`。repo 沒有 `sc nav` mode 或 `navPatches` exporter。現行 plan 也明寫「DLL 端一個字都沒寫」（`workflows/plans/scene-capture-bridge/README.md:58-62`）。 |
| **Godot 離線地形旁路** | **已接通（本機 runtime 未重跑）** | Godot 匯 `heightmap/splatmap/placements.json`，ModForge 以 `GodotPlacements.Load` 合流 placements（`src/ModForge.Core/Build/Generator.Build.Worldspace.cs:258-263`）。本次只有兩個 source gate 通過，Godot live cases 因缺 binary skip。 |

因此，指定鏈路可精確寫成：

```text
施法（空）
  └─ console/F11/F1（已接通）
       → registries/co-save（已接通）
       → scene/captures JSON（已接通；loaded cells only）
       → ModForge 真 spec 段（已接通）→ ESP（已接通）
       → annotations 語意段（只讀不生；部分）
       → navPatches producer（空）→ NAVM backend（已接通但吃不到遊戲端資料）
```

---

## 3. 卡點

1. **「施法即編輯」缺實作，不是缺一顆設定。** 目前 plugin 只註冊 input/activate/message sinks，沒有 spell cast sink；A2 尚在 backlog（`backlog.md:27-32,49-54`）。要完成北極星，需 SPEL/MGEF 與 `TESSpellCastEvent` 到既有 mode/ghost 入口。

2. **semantic marker 尚未變成 build-time authoring。** Bridge 只吐 `annotations[]`；ModForge 刻意 inert。這使「標這裡放 NPC/map marker/VFX」必須由下一輪 agent 手動翻成 `npcs[]`/`placements[]`/`mapMarkers[]`/`hazards[]`，沒有 deterministic 自動層。證據是 `Spec.Annotations.cs:3-26` 與 `Program.Build.cs:34-36`。

3. **遊戲內 NAVM producer 完全缺席。** ModForge P3 backend 只吃精確、凸、3+ 點、完整邊唯一 matching 的 interior polygon；bridge 沒讀 live `RE::NavMesh`、沒吸附、沒 `sc nav` registry、沒吐 `navPatches[]`。具體前置是 B1 live navmesh vertex/triangle spike；契約與理由在 `backlog.md:58-93`。

4. **場景與 captures 是兩個 JSON，沒有 merge command。** 2026-07-12 刻意拆檔；要同一 ESP 目前只能人工合併兩份 JSON，或分別 build。原 `SceneImport --scene` 被取消且 class 不存在（`ingame-scene-export.md:26-29,97-102`）。這會卡「一次 export → 一個含場景與拓印 NPC 的 patch」自動化。

5. **全區快照只涵蓋已載入 cell。** `TES::ForEachCell` 還要 `cell->IsAttached()`，unloaded cell 的 placement 無法復原；code 明說需拜訪或逐 cell export（`SceneExporter.cpp:683-703`）。大區域 authoring 仍不是一次完整 snapshot。

6. **目前 scene-capture-bridge HEAD 有兩個尚無實機驗收記錄的新修正。** `75308c9` 保持 py0 placement 經 editor 後仍 frozen；`a17e460` 改用 rendered NiCamera ray，支援第三人稱／SmoothCam 且略過玩家。兩 commit 已 build，但既有 landed/WAIT_USER 沒記這兩項的 runtime PASS；本任務硬性禁止啟動 Skyrim，故不能補證。

7. **物件狀態屬性編輯仍卡在逐欄 engine truth。** 架構雖定為 registry → `overrides[]` 新欄位，但門 open/locked、火把亮滅、ownership、enable-parent 等每個都要先解碼，尤其火把可能不是 REFR flag（`backlog.md:97-114`）。這不是「再接一個 UI field」即可完成。

8. **本機附屬驗證環境的缺口——三項裡有一項是誤判。** ~~Godot binary 缺失，故兩個 live contract skip~~；PowerShell 缺失，故 my_skyrim_plugin_1 的兩支正式測試不能跑；該 repo build cache 又釘舊 vcpkg path。這些不擋 ModForge/bridge 主線 build，但擋本機把所有旁路重跑到 runtime 層。

    **【2026-08-27 複驗】**

    - ❌ **「Godot binary 缺失」不成立——是探測名單漏了，不是機器沒裝。** 本機有
      `/usr/bin/godot-mono`（`4.7.2.stable.mono.arch_linux.ed1daf0bf`，headless 可用）。
      當初兩支 contract 測試的探測名單只寫了 `("godot4", "godot")`，所以 runtime 層一直被 skip，
      而 skip 訊息（`Godot not found; set GODOT_BIN...`）讀起來像是機器沒有 Godot。
      補上名單後 `godot-worldspace-editor` 的 `python3 -m unittest discover -s tests` 是
      **Ran 7 tests, OK**（補之前只有 2 條真的執行）。依據：`godot-worldspace-editor` commit `26d3cab`，
      由 `opus-godot` 修、`dispatcher` 獨立複驗。
      ⚠️ 教訓：**「工具不在 PATH」與「本機沒有這個工具」是兩件事**，skip 訊息不該被當成後者的證據。
    - ✅ **PowerShell 確實仍缺**（`command -v pwsh` → MISSING，2026-08-27 複驗）。
    - ➡️ **vcpkg 舊路徑的 build cache 歸 `projects/my_skyrim_plugin_1`**，不在 ModForge 領地；
      本檔只記錄，修不修由該 repo 的負責線決定。

---

## 4. 文件與實體不符之處（全面檢查）

本輪逐份查：idea `24-ingame-editor.md`、現行 spec、plans index、`ingame-scene-export.md`、`scene-capture-bridge/{README,phases,backlog,appendix}.md`、captured/player/navmesh plans、landed/world、wait_todo/ingame-tests，以及四 repo 的 README/build/test 文件；再對四 repo HEAD、build manifests、producer/consumer/function symbols、tests 與實際命令結果。找到下列不符；未列出的主契約（座標為度、durable FormKey、scene/captures 分檔、Godot v1 placements）與實體一致。

### A. 會直接誤派工作的高風險不符

1. **交接書稱 `my_skyrim_plugin_1` 是薄記錄器；實體不是。**
   - 文件側：本次 HANDOFF 權威入口把它寫成「遊戲內『薄記錄器』plugin」。
   - 實體側：source list 只有 DaylightDungeon 的 NpcGenerator/FollowLight/AmbientBoost（`projects/my_skyrim_plugin_1/cmake/sourcelist.cmake:1-7`），無 scene/capture/export。真正 recorder 是 scene-capture-bridge（`projects/scene-capture-bridge/src/plugin.cpp:105-178`、`SceneExporter.cpp:657-876`）。

2. **現行 spec 尾段仍把已完成的 `npcRoles[]` 寫成 net-new。**
   - 文件側：`ingame-scene-export-design.md:307-347` 仍標 `SceneNpcRoleSpec`／macro 為待做。
   - 實體側：DTO 在 `src/ModForge.Core/Spec/Spec.SceneExport.cs:21-34`；`ExpandNpcRoles` 在 `Macros/Generator.SceneNpcRoles.cs:44-79`；本次 suite 1190/1190 通過，含 `SceneNpcRolesTests.cs`。

3. **現行 spec 尾段仍把 `SceneImport` 寫成 net-new；實際決策是取消且不需要。**
   - 文件側：spec `:342-347,355-369` 規劃 `SceneImport` 與 round-trip test。
   - 實體側：plan 的現況段明說 scene.json 本身是 ModSpec、`SceneImport.cs` 從未建立（`workflows/plans/ingame-scene-export.md:17-29,97-102`）；CLI 直接 deserialize（`Program.cs:61-72`）。

4. **backlog 說 catalog consumer 尚未做；實體已完成且本次 2/2 通過。**
   - 文件側：`scene-capture-bridge/backlog.md:116-120` 寫「ModForge 產生端完成、DLL 消費端待主力機」。
   - 實體側：bridge README `:12,170` 已描述 consumer；`src/Catalog.cpp:122-181` 載入／compatibility gate／merge，`CatalogFile.cpp` 是 portable parser；本次 `catalog_file_tests` 與 `modforge_catalog_contract` 2/2 PASS。

5. **plan 摘要說 A 只剩 A1，但 spell trigger 與 favorite UI 仍沒有實體。**
   - 文件側：`scene-capture-bridge/README.md:58-61` 寫「只剩 A1」；同一 backlog `:49-54` 又把 A2/A3 列為任務。
   - 實體側：全 `scene-capture-bridge/src` 無 `TESSpellCastEvent`；Palette 有保存/選取，但沒有 backlog 所述「加入最愛」改名與星號置頂。結論：A1 是唯一**未知技術 spike**，不是唯一未做工作。

6. **bridge README 的 exporter 職責仍寫 dynamic ref 即 emit；實體多了一道 ownership registry gate。**
   - 文件側：`projects/scene-capture-bridge/README.md:169` 仍概述「解不出 durable id ⇒ 玩家 PlaceAtMe ⇒ emit」。
   - 實體側：`SceneExporter.cpp:217-235` 明確拒絕 registry 沒有的 dynamic ref，避免魚／critter 混入；只有 `Palette::PlacedInfoFor` 命中才 emit。這是行為級差異，不只是補充細節。

7. **bridge 的 status 摘要彼此不一致。**
   - `projects/scene-capture-bridge/README.md:13` 只寫 P1–P3 全過、P5 待實機。
   - ModForge plan `workflows/plans/scene-capture-bridge/README.md:52` 寫 P1–P6 主線全過。
   - `wait_todo/ingame-tests.md:44-63` 又保留 ghost/numpad 與 ownership exporter 回歸項。
   - 實體結論：P1–P6 核心功能已存在；部分 post-P6/ghost 回歸仍 open；bridge README 頂部 status 過時。

### B. spec 內部被後文推翻、但前文仍像現況

8. **PROTEUS clone／穩定 ActorRef 是舊路線。**
   - 舊宣稱：spec `:11-19,34-59` 把 PROTEUS clone + role 當北極星切片，且說 clone 穩定可引用。
   - 現況修正：同檔 `:241-282` 改成場景不含 actor、captures 分檔、`sc capp` 直接讀玩家；`:286-299` 明說 PROTEUS 已被取代且 dynamic ref 有耐久性問題。實體 `Console.cpp:165-178` 與 `Captures::CapturePlayer` 已走直接 capture。

9. **「override 尚未建模」的舊話仍殘留。**
   - 舊宣稱：spec `:105-115` 說 scene.json 只有 removals、移動既有 ref 未建模；bridge production comment `SceneExporter.cpp:180-184` 也仍這樣寫。
   - 現況：spec `:140-168` 隨後定案 `overrides[]`；實體 exporter `SceneExporter.cpp:329-350` 會吐，ModForge `Generator.Build.cs:130-131` 會 build。

10. **interior 座標同段同時寫「尚未驗」與「已結案」。**
    - spec `:99-103` 保留「尚未驗」。
    - spec `:116-130` 記錄 runtime 已驗、全部條目結案。前者應明示為歷史。

11. **placement-controller `.pex` 仍被寫成待做 runtime 元件，但現行主路徑已改成 C++ Palette/Preview。**
    - 舊宣稱：spec `:34-59,343-347,351-362` 把 `.pex` 列 M3。
    - 實體：bridge `Modes::RunAction(kPlace)` → `Preview::Commit`／`Palette::PlaceSelected`（`src/Modes.cpp:106-125`），`Palette::PlaceSlot` 直接 PlaceAtMe；repo 沒有 Papyrus controller。這是被更換的架構，不應仍列為 current blocker。

12. **`SceneNpcRoleSpec` 註解說 vendor 尚未展開，實作其實已有 vendor。**
    - 舊註解：`src/ModForge.Core/Spec/Spec.SceneExport.cs:17-20` 與 `Macros/Generator.SceneNpcRoles.cs:39-41`。
    - 實體：`ExpandBlacksmith` 在 companion placement 存在時生成 merchant chest、vendor FACT、trade topic，並掛 factions（`Macros/Generator.SceneNpcRoles.cs:111-189`）；plan 也記 vendor 後補完成（`workflows/plans/ingame-scene-export.md:26-29`）。

### C. 其他 repo／現行 branch 的不符

13. **scene-capture-bridge HEAD 的兩個新修正沒有進現役文件。**
    - 實體 commit：`75308c9 fix: preserve frozen palette placements`、`a17e460 fix: aim placements from rendered camera`；後者改 `Aim::RenderedCameraHit` 並讓 Palette/Preview 共用。
    - 文件側：全 ModForge workflows 與 bridge README 搜不到 rendered-camera／placement-drift/frozen-placement 的現況或驗收。README `:174` 仍只描述 player-facing look ray 與 Crosshair/RayRef。

14. **Godot README 頂部說 box proxy 還待換，後文與實體說已完成。**
    - 舊摘要：`projects/godot-worldspace-editor/README.md:23-25` 把 box proxy 換真實 glTF 列為剩項。
    - 現況：同檔 `:106` 明列 2026-06-18 GUI 確認真實 vanilla geometry；`godot/model_fetch.gd` 與 `tests/test_model_fetch_contract.py` 存在，本次 source gate PASS。

15. ~~**Godot placement 的 producer 註解仍稱 HTerrain plugin，repo 實際是自製 terrain。**~~ **已修，本條作廢。**
    - 舊註解：`src/ModForge.Core/Spec/Spec.Worldspace.cs:65-74`。
    - 現況：Godot README `:5-9` 明說自製 terrain、不靠 HTerrain；實際 `godot/terrain.gd:1-17` 是自有 height grid。JSON contract 沒因此壞，但註解會誤導依賴判斷。
    - **【2026-08-27 複驗】已不成立**：`grep -rn HTerrain` 掃過整個 ModForge repo，
      命中的只剩本檔這兩行；`Spec.Worldspace.cs` 現在的註解寫的是「Godot 自製 terrain editor」。
      是本檔寫完之後的 `839a787`（docs: align in-game editor docs with implementation）修掉的，
      也就是**這一條在被讀到之前就已經過期**。

16. **ModForge testing 文件的 suite 規模已落後。**
    - 文件：`workflows/testing.md:27-29` 仍以 1107 test methods 為敘述基準。
    - 本次命令：1190 passed、0 skipped。這不影響指令正確性，但不能再用舊數字判斷是否漏跑。

### D. 查過且一致

- scene/captures 分檔與 timestamp 檔名：spec `:241-259`、bridge README `:101-106`、`SceneExporter.cpp:712-876` 一致。
- annotation 為 advisory、不直接生記錄：bridge exporter、ModForge DTO、CLI summary 一致。
- Godot placements v1 的 version/coordinate system/base/position/rotation/scale/instanceId：`placements_io.gd:47-64`、Python source gate、ModForge `GodotPlacements` consumer 一致。
- NAVM P3 為 vanilla interior、append/stitch、保留舊 triangle index：spec、`Spec.NavPatches.cs`、`Build.NavPatches.cs` 與 landed runtime 記錄一致。

---

## 5. 三個可立即執行的下一步

### 1. 先做現役文件真相收斂

- **Done when:** spec 的舊 PROTEUS/SceneImport/controller/override 段明示為歷史或改成現況；scene plan/README/backlog 統一 P1–P6、catalog consumer、A1/A2/A3、ownership gate 與目前兩個 HEAD fixes；Godot 頂部移除 box-proxy 舊尾巴。調度者從任一權威入口都不會再把已完成項重派。
- **規模**：小。
- **排序理由**：已經發生「差點重做一晚」；這是最便宜、且能立即阻止重工的風險消除。
- **需使用者在場**：否；純文件，無 GUI／實機／下載。

### 2. 穩定並驗收目前 placement 主線，再補 A2 施法入口

- **Done when:** `75308c9` 的 py0 frozen-preservation 與 `a17e460` 的 first-person/third-person/SmoothCam rendered-camera placement 都在遊戲內 PASS；同一輪加入 SPEL/MGEF + `TESSpellCastEvent`，施法走既有 place/ghost mode，console/F11 保持相容，scene export 不混入 spell FX/dynamic junk。
- **規模**：中。
- **排序理由**：目前 HEAD 已越過已驗收基線；先證它再疊功能。同時 A2 是北極星「施法即編輯」最明顯、最小的缺口。
- **需使用者在場**：是；需 Skyrim/MO2/第一第三人稱與 SmoothCam 實機，無需下載。

### 3. 做 B 線 `sc nav` 的 interior 垂直切片

- **Done when:** bridge 能讀玩家附近 live `RE::NavMesh` 頂點/三角形、顯示並吸附角點；`sc nav` 可收一個凸 polygon、co-save、匯出真正的 `navPatches[]`；ModForge build 後 `navdiag` 證新舊三角形互為鄰居且舊 index 不變，兩名相反方向 Travel actor 都跨 seam。
- **規模**：大。
- **排序理由**：ModForge backend 與 runtime proof 已完成，這是目前資料流唯一完全空白、且對 NPC 可走性價值最高的 producer 段；比再加一個外觀 UI 更接近「Skyrim = CK」。
- **需使用者在場**：是；B1/B2 可離線開發，但最終需 Skyrim/MO2 實機，無需下載。
