# 已落地 — scene-capture-bridge 遊戲內採集橋：部署鐵律／M4／P1 marker／P2–P3 編輯器

← [landed index](README.md)｜DLL 在 [scene-capture-bridge](../../../../scene-capture-bridge/README.md)，C# 消費端對應 [CODE_MAP.world](../../common/code-map/CODE_MAP.world.md)｜採集橋：[scene-capture-bridge.md](scene-capture-bridge.md)（含 🔴 部署事故，先讀）· [editor](scene-capture-bridge-editor.md) · [referrer](scene-capture-bridge-referrer.md) · [flags-deps](scene-capture-bridge-flags-deps.md)

## 🔴 部署事故：絕不 `cp` 覆寫執行中的 DLL（2026-07-12，實際炸掉一次遊戲）

background agent 編完新 DLL 後用 `cp` 就地覆寫 `mods/SceneCaptureBridge/SKSE/Plugins/`，**當場把使用者正在玩的遊戲弄死**——而且**沒有產生任何 crash log**（CrashLoggerSSE 有裝且正常）。成因：Windows 會鎖住載入中的 DLL，**Linux/Proton 不會**；`cp` ＝ `open(O_TRUNC)` 寫回**同一個 inode**，而已載入 DLL 的程式碼頁是從該檔 **demand-page** 進來的 → 檔案在腳下被換掉 → 下次 page-in 從新檔同一 offset 讀到的是別的東西 → 指令流變垃圾（crash handler 自己可能也還沒 fault-in，所以連 log 都寫不出來）。

修法（已落地 `scripts/deploy.sh`）：`cp new target.tmp && mv target.tmp target`（`rename(2)` 原子換 inode，執行中的遊戲繼續指向舊的已 unlink inode）＋ 部署前 `pgrep -f SkyrimSE.exe`，**遊戲在跑就直接拒絕**。細節寫進 [dev-env](../../dev-env.md)「部署 SKSE DLL 到 MO2」節。

## scene-capture-bridge M4 spike — 遊戲內採集橋（Idea #24 元件③）· IN-GAME 2026-07-10

一支 SKSE C++ DLL（`../scene-capture-bridge/`）走訪玩家所在 cell，把玩家擺放的 ref 序列化成 `scene.json`（＝一份合法 `ModSpec`）→ ModForge `build` 出 patch esp。實機驗收：

- **clang-cl 跨編譯的 SKSE DLL 可直接實機**：`skse64.log` → `plugin SceneCaptureBridge.dll (...) loaded correctly (handle 53)`。import 表只有系統 DLL、靜態 CRT。`BUILD.md` 原本假設此路徑僅供編譯驗證——有反例了（出貨仍走 Windows CI，因未驗 address-library 跨版本行為）。
- **vanilla diff**：`ResolveDurableId(&ref)` 解得出 ⇒ 既有 ref ⇒ 跳過；解不出（dynamic `0xFF......`，`GetFile(0)==nullptr`）⇒ 玩家 `PlaceAtMe` 擺的 ⇒ emit。The Bannered Mare (`01605E`) 按 F10：`0 placements, 717 pre-existing`。`placeatme` 兩個之後：`2 placements, 717 pre-existing`。
- **座標契約結案**：plugin 存弧度、Papyrus `GetAngle*` 回度數、C++ `GetAngle()` 回弧度、`scene.json` 一律度數；`interior` 的 `data.location` 就是 cell-local（匯出座標與 `get_cell_info` 玩家座標一字不差）；vanilla `scale==1.0` 時省略 XSCL。round-trip 誤差 2.3e-7 rad（float32 捨入）。
- **整鏈**：真實 `scene.json` → `validate` 零問題 → `build` → 2 placement 掛在 vanilla interior cell 的加法 override，master 僅 `Skyrim.esm`。

已知取捨：玩家**移動/縮放過的 vanilla ref** 不採（需 emit 既有 ref 的 override，`scene.json` 尚未建模）。NPC 來源預設走 ModForge 直接生的「大眾臉」；PROTEUS 拓印為可選。

**遊戲內面板（2026-07-10 IN-GAME）**：`src/UI.cpp` 消費 [SKSE Menu Framework 3](../../../../../analysis/mod-survey/findings/frameworks-tools/skse-menu-framework-3.md)，F1 面板出現 `Scene Capture Bridge` section，`Export player cell` 按鈕與 F10 走同一支 `ExportPlayerCellToFile()`。軟相依（`GetModuleHandleW` 探測，import 表無框架），沒裝框架只剩 hotkey。⚠️ `sse-imgui` 在 AE 不能用，理由見 finding。

## P1 統一 marker 系統（Idea #24 遊戲內編輯器 MVP）· IN-GAME 2026-07-10

「玩家在遊戲裡指一個地方說要什麼 → AI 把它變成 mod」端到端閉環，含最後一哩目視確認：

1. **標記**：hotkey 在玩家腳下放發光召喚圈 proxy（`SCB_MarkerACTI`＝vanilla `Magic\SummonTargetFX.nif`，工具 esp 缺席時 fallback vanilla base）；F1 面板改名/改 kind/刪。使用者實機標 `goat`（Tamriel）。
2. **匯出**：F10 → `scene.json` 的 `annotations[]`（advisory 段，ModForge build 永不生成、只 log）。實測 3 筆全對：label/kind 編輯生效、angleZ 度數、worldspace 歸位、proxy 排除數=3（editor chrome 不漏進 placements）。
3. **agent authoring**：agent 讀 annotations → spec（`EncGoatDomestic` + marker 座標/朝向）→ build → `PlacedNpc @ (116031.1, 111485.6, -7744)` cell(28,27) 自動歸位，master 僅 Skyrim.esm。
4. **目視**：使用者進遊戲在標記處看到山羊。

實戰教訓（皆已記 plan）：**F9 是 vanilla 快速讀檔**（sink 只觀察不吞鍵，遊戲同時處理→讀檔抹掉剛放的 proxy）→ 改 F11 + 面板放置鈕；**外部 NPC base 必須明示 `kind:"npc"`**（isNpc 自動判定只認 in-spec base，落成 REFR 不生怪——dump 看到 PlacedObject 才抓到）；kPostLoadGame → PruneDeadProxies。MO2 refresh 後新 mod 資料夾預設不勾。

## P2 編輯器三件套 + P3 物理凍結 · IN-GAME 2026-07-11（Winterhold）

一輪實測全過，含匯出→build→patch 實機生效的完整閉環：

1. **F11 準星放 marker**：兩發都落在射線命中點——`Aim.cpp` pitch 符號正確（先前唯一未驗的射線疑點結案）。
2. **F8 橡皮擦**：vanilla 長凳 ref 標記→面板 undo 復原→再標記；匯出進 `removals[]`。
3. **F6/F7 滴管**：吸 `NobleBench01`（含 60.2° 旋轉）→ F7 擺在準星處，姿態跟著走。
4. **numpad 編輯**：5 選中、3 升高（+55 units）、0 commit、`.` 取消還原全部實證；**無任何未映射 numpad 鍵**——log 裡 unmapped `0x11/0x1F/0x20/0x38` 是編輯中按的 WASD/Alt（無害噪音，之後可限縮 log 範圍）。
5. **物理凍結**（P3）：選中掉落的 `StaffMagelight`（HavokMovable）→ `physics frozen` → 抬升 → commit → `physics restored` → 沉降；**匯出的是沉降後姿態**（rotation x=146.6° 躺平），符合「live pose 匯出」語意。
6. **閉環**：`scene-export.json`（2 placements + 2 annotations + 1 removal）→ `build` → 7-record esp（removal override 深埋 Z−30000、Tamriel override 自動帶 TopCell 0xD74、ESL、master 僅 Skyrim.esm）→ 使用者實機：**擦的長凳永久消失、擺的長凳＋躺平法杖出現**。
7. **持久化**：遊戲內 save→load 後擦除維持、markers 原地——disable 狀態與動態 ref 都住在存檔（語意詳見 [../scene-capture-bridge/README](../../../../scene-capture-bridge/README.md)「持久化與 adopt 語意」）。

意外收穫：被抬的法杖根本不是 DLL 擺的（玩家丟在地上的裝備）照樣進 placements——vanilla diff 的無狀態判別（動態 ref＝玩家所為）涵蓋所有玩家操作，不限本工具。

殘項（open-only 慣例，列在 [wait_todo/ingame-tests.md](../../../wait_todo/ingame-tests.md)）：跨行程 adopt（關遊戲重開）＋三個零星未實證小項。
