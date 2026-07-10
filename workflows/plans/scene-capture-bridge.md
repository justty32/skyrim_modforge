# 採集橋 runtime 工具 — Implementation Plan（Idea #24 §B/§D/§E）

← [plans](README.md)｜spec：[ingame-scene-export-design.md](../specs/ingame-scene-export-design.md)（共用）｜idea：[#24](../idea/tools/24-ingame-editor.md)｜子專案：[scene-capture-bridge](../../sub_projs/scene-capture-bridge/README.md)

**Goal:** 把採集橋從「一顆 Export 按鈕」長成**遊戲內編輯器**——橡皮擦、滴管、範圍吸取、語意標記、role tag。全部落在 runtime C++ 側；`scene.json` 契約不變或只增段，**ModForge 生成端幾乎零改動**。

**前置（皆已落地）**：[ingame-scene-export.md](ingame-scene-export.md) 的 M0–M2（ModForge 側 scene.json → patch）；本子專案的 M4 spike（cell 走訪 + vanilla diff + scene.json，IN-GAME 2026-07-10）；SKSE Menu Framework 面板（IN-GAME 2026-07-10）。

---

## 現況（2026-07-10）

- `SceneCaptureBridge.dll` clang-cl 跨編譯，**實機載入正常**。F10 hotkey + F1 面板（`Scene Capture Bridge` → `Export`）。
- **vanilla diff 成立**：`ResolveDurableId(&ref)` 解得出 ⇒ authored ⇒ 跳過（計 `preexisting`）；解不出（dynamic `0xFF......`、`GetFile(0)==nullptr`）⇒ 玩家 `PlaceAtMe` 擺的 ⇒ emit 進 `placements[]`。Bannered Mare：`0 placements / 717 pre-existing`；`placeatme` 兩個後 `2 placements / 717 pre-existing`。
- 座標契約全條目結案（旋轉單位、ESL 遮罩、scale 省略、interior cell-local、round-trip）。
- 面板走 **SKSE Menu Framework 3**，軟相依（`GetModuleHandleW` 探測，import 表無框架）。⚠️ `sse-imgui` 在 AE 不能用，見 [finding](../../sub_projs/mod-survey/findings/skse-menu-framework-3.md)。

## 已定調的決策

| 議題 | 決定 | 出處 |
|---|---|---|
| NPC 來源 | 預設「大眾臉」——ModForge 直接生 `NpcSpec`（有 `Race`，無 headpart/tint/facegen → 引擎用種族預設頭）。**PROTEUS 拓印降為可選** | 使用者 2026-07-10 |
| 橡皮擦的 removals 狀態 | **記憶體（session）+ 面板的 `Adopt disabled refs` 掃描鍵**（推導只在按下去時發生，不靜默誤判任務關掉的雜物） | 使用者 2026-07-10 |
| 擦到外部 mod 的 ref | **允許，但面板醒目標示**（「這會讓你的 patch 依賴 `X.esp`」），匯出時 log 列出新增的 master | 使用者 2026-07-10 |
| 地形高度 | **遊戲內不做地形雕刻**——runtime 無法變形 LAND，且「地形高度不太需要在遊戲中修改」。走既有離線路：Godot worldspace editor（idea #19）或 PNG heightmap → ModForge（heightmap→非平坦 LAND **已落地**）。遊戲內只做**輔助**：標註 marker（見 M9） | 使用者 2026-07-10 |
| 標註 marker 的定位 | 指向性法術/射線在命中點放 marker → 可改名 → 匯出 json → **給 AI agent 下指令的座標錨點**（「這個 marker 所在座標的地形要抬升…」）。是玩家→agent 的通訊通道 | 使用者 2026-07-10 |
| 玩家移動/縮放過的 vanilla ref | **不採**（需 `scene.json` 長出「既有 ref 的 override」形狀）。橡皮擦繞過它；**滴管會撞上**——見下方技術債 | spec §契約 |

---

## 細摳②：靜態物件——使用者 UX 定稿（2026-07-10）

「實現這些功能的程式碼就是那樣，**重點是操作方式**。」分三種：**新增、修改、刪除**；修改再分 transform（位置/大小/角度）與**屬性**（火把燃燒與否、門開關與否…）。

### 新增

- **路徑 A（最簡）**：GUI 選單先選一個靜態物件 → 施放指向性法術 → 物件出現在命中點。
- **路徑 B**：先施法在命中點放 marker → 再到 GUI 選「在那個 marker 生成」。
- **確認流程**：生成時先出現**綠色半透明輪廓**預覽 → 過幾秒（或玩家再施一法）→ 跳選單問「這樣可不可以」→ 確定 OK 才實際生成。

### 刪除

- **路徑 A**：指向性法術擊中目標靜態物件。
- **路徑 B**：GUI 直接列出指定 cell 的靜態物件選擇刪除——**我們新增的單獨放一掛、最新生成的在最前面**（採集橋本來就有這份資料）。
- **確認流程**：先變**紅色半透明輪廓** → 幾秒或再施法 → 確認選單。
- **色盲考量（使用者自provisions）**：紅綠對色盲不友善 → 提供色彩調整選項。實作：編輯器工具 esp 自帶多套 EffectShader 記錄，面板切換。

### 持續施法變體（新增與刪除共用）

指向性法術多做一個 **concentration 版**：指哪打哪，指到的物件即時套上半透明輪廓，**結束施法時選定最後被擊中者**。

→ **實作洞見**：法術＝**模式開關＋美學**；實際選取＝DLL 每幀輪詢 `CrosshairPickData`（API 已確認）。這樣**不需要 projectile impact hook**，也繞開「STAT 靜物不吃魔法效果」的限制——spell 只負責告訴 DLL「現在在選取模式」。

### 修改（快捷鍵編輯模式）

- 指向性法術選中 → 物件套**泛光**效果（**不要**半透明——與刪除預覽視覺區隔）。
- **numpad 操作**：`2468` 位移、`1379` 旋轉、`+ −` 縮放、`* /` ＋ `5` 在屬性 GUI 中選擇要改哪個屬性、`Enter` 或 `0` 結束編輯。
- 同時跳出 GUI 視窗列出該物件的屬性。
- **編輯模式中按鍵必須吞掉**、不給遊戲——SKSE Menu Framework 的 `AddInputEvent` callback 回傳 bool 即 block（[finding](../../sub_projs/mod-survey/findings/skse-menu-framework-3.md) 已載明此能力）。
- scancode 全部**實測取得**（Task 0 慣例），不假設 DIK 值。
- 進入編輯模式時**快照原 transform** → cancel 可還原（per-edit undo）。

### 三個設計後果

1. **「修改既有 ref」的流程本身就是明示登記**。被法術選中且實際動過的既有 ref，自然進 override 清單——先前「不能用 diff 偵測移動」（havok 假陽性）的問題被這個 UX 直接解掉，M7b 不必另造登記機制。剩下的只有契約形狀（`overrideOf` vs `overrides[]`）要拍板。
2. **屬性清單必須映射到 record 欄位，否則匯出說謊**。`PlacementSpec` 已有：`Lock`(XLOC)/`Ownership`(XOWN)/`Count`(XCNT)/`InitiallyDisabled`/`EnableParent`/`LinkedRefs`/`Teleport`。**待查**：門的「預設開啟」flag、火把燃燒狀態（火把/火盆常見是 enable-parent 對偶或 lit/unlit 兩個 base——可能得先標 advisory）。GUI 屬性列表**只列能存活到 esp 的屬性**，或明確標示「僅本次遊戲、不會匯出」。
3. **預覽輪廓機制待驗**：EffectShader（vanilla ghost/ethereal 類）能否給綠/紅半透明；預覽 ghost 要不要關碰撞（`SetMotionType`？待驗）；泛光用哪個 shader。編輯器工具 esp 自帶自訂 shader 記錄，順帶承載色盲選項。

### 細摳③：動態物件（2026-07-10）

走靜態物件同一條路（新增/修改/刪除、同一套確認流程），差別只在**物理**：

- 被指向性法術選中後 → **凍結物理**（tcl 效果——喪失物理特性；光照/渲染等保留，細節實作時再議）→ 編輯 → **結束後回復物理**。
- 機制候選：`SetMotionType(Keyframed)`（Papyrus 同名函式的 C++ 對應；**待驗**）。
- **誠實註記（WYSIWYG 邊界）**：物理回復後 havok 會重新模擬——擺懸空的杯子會掉、穿模的會彈開。匯出的是 **authored 位置**（你編輯時定的），遊戲載入後物件自行沉降——這與 CK modder 擺 clutter 的行為**一致**（vanilla 本來就這樣），不是 bug，但要在文件/面板講清楚。
- 契約**零改動**：動態物件（MSTT/misc…）跟靜態一樣進 `placements[]`（base + transform）；物理凍結純屬編輯期 runtime 行為。

### 細摳③附：檢視法術（編輯痕跡的可視化）

選中的輪廓/泛光效果在**結束編輯後消失**。另提供一支**持續施展**的檢視法術，施展期間把編輯痕跡重新顯示出來，方便玩家知道自己改了啥。**可能做四種：新增／修改／刪除／全部**。

- 實作：DLL 迭代 session 登記簿（新增清單／修改清單／removals 清單——三者本來就存在於設計中），對各套上對應 shader；法術一樣只是模式開關。
- **待驗**：顯示「被刪除的」有個坑——被擦掉的 vanilla ref 是 disabled，**不會被渲染**。要嘛暫時 enable + 紅 shader、收法再 disable；要嘛在原位放臨時紅色 marker。實作時定。

### 細摳③附：真刪除語意（使用者定調）

**被刪除的物件如果是我們先前新增的，刪了就是真的刪了**——不進 removals[]（原本就如此）、**也不留在任何登記簿**，檢視法術（含「刪除」模式）不會顯示紅輪廓。無痕跡。

- 與 M6 資料流一致並延伸：dynamic ref 刪除 = 從「新增」登記簿移除 + 世界中銷毀（`Delete()` 是否存在仍是 Task 0 驗證項；Papyrus 慣例是 `Disable()`+`Delete()` 標記引擎回收）。

### 細摳④（stub）：NPC／生物擺放——含 leveled encounter（使用者 2026-07-10 確認要）

待使用者細摳。先標一個**採集側的關鍵坑**：

- **`placeatme` 一個 LVLN 會立刻抽選**——引擎當場生出一個具體的 NPC（抽中的那隻土匪），dynamic ref 的 base 是**抽選結果的 NPC_**，不是 LVLN。所以「靠採集 dynamic ref 拿 base」這條路（靜態物件的做法）**對 leveled encounter 不成立**。
- **解法＝palette 授權**：玩家在 GUI 選單選的是 LVLN → 登記簿記下**選單裡選的 LVLN id**＋落點 transform；遊戲內生出來的那隻只是**視覺代理**（讓你看位置對不對）。匯出時 `placements[].base` 寫 LVLN id，不看世界裡站的是誰。
- vanilla 先例：野外/地城的 leveled 遭遇就是 ACHR 指向 LCharBandit 之類的 LVLN base。
- **ModForge 側待驗**：placement builder 對 base=LVLN 是否正確生 ACHR（Mutagen 的 PlacedNpc base 應涵蓋 LeveledNpc，需 grep 驗證）；LVLN 記錄本身的生成已存在（`WireLeveledNpcs`）。

### 對既有 milestone 的映射

**M6＝刪除**（原橡皮擦，補上紅輪廓確認流程與 GUI 列表路徑）、**M7a＝新增**（palette＋指向性放置＋綠輪廓確認）、**新增 M7c＝修改編輯模式**（numpad transform＋屬性 GUI）、**新增 M7d＝動態物件物理凍結＋檢視法術**（細摳③）。M7b（override 形狀）的**偵測**由修改流程天然提供，只剩契約拍板。

---

## M6：橡皮擦（§E ③）

**為何先做**：生成端**零改動**。`BuildRemovals`（`Generator.Build.Removals.cs:21`，已讀原始碼）吃 `<master>:0xFORMID` 的既有 placed ref → master link cache 解 `IPlaced` → `GetOrAddAsOverride`（連帶 override parent cell/worldspace）→ 設 `InitiallyDisabled`(0x800) → Z 埋 −30000 避 havok 殘留。而 `ResolveDurableId(&ref)` 吐的正是那個格式。採集橋只要把字串塞進 `removals[]`。

它也是第一個讓玩家碰**既有 vanilla ref** 的功能：vanilla diff 目前把這類 ref 一律算進 `preexisting`，橡皮擦要在其中鑿一個洞。

### 資料流

```
準星指著 ref → 按 hotkey
  ├─ authored（ResolveDurableId(&ref) 成功）
  │     → ref.Disable()（當場消失，視覺回饋）
  │     → 耐久 id 進 session 清單
  └─ dynamic（解不出 ⇒ 玩家這次擺的）
        → 就地移除；**不進 removals[]**（BuildRemovals 的 TryExternalRef 會 warn+skip）

匯出 → scene["removals"] = 清單全部 id
     → cell 走訪中，id 在清單裡的 authored ref 不計入 preexisting
```

清單是**全域**的、不是 per-cell——`BuildRemovals` 靠 master link cache 解析，與當下所在 cell 無關，所以跨房間擦東西也對。

### Task 0：前置確認（只讀，不改碼）

- [ ] **Step 1**：`RE::TESObjectREFR` 有沒有 `Delete()`？（grep vcpkg headers）已確認有 `Disable()`（virtual 89）、`Enable(bool)`、`IsDisabled()`；**`Delete()` 沒找到**。若無，玩家自擺 ref 只能 `Disable()`（仍在存檔裡，只是看不見）——面板必須誠實說明，不能寫「刪除」。
- [ ] **Step 2**：`Delete` 鍵的 DirectInput scancode。CommonLibSSE **沒有** DIK 表。先在 `HotkeySink` 裡對任意按鍵 log 一次 `GetIDCode()`，實測取得，**不要假設 `0xD3`**。（先例：`ForEachReference` 的簽名憑印象寫錯，首編才發現。）
- [ ] **Step 3**：`RE::CrosshairPickData::GetSingleton()->target` 型別為 `ObjectRefHandle`（= `BSPointerHandle<TESObjectREFR>`，`.get()` 取回）。**已確認**。

### Task 1：`src/Eraser.{h,cpp}` — session 狀態

**Files:** 新增 `src/Eraser.h` / `src/Eraser.cpp`；`cmake/{source,header}list.cmake` 各加一行。

清單元素存 **`ObjectRefHandle` 而非裸指標**——undo 時 ref 可能已被卸載，handle 才能安全地判斷與 `Enable()`。

```
struct Entry {
    std::string id;        // "Skyrim.esm:0x0D1991"
    std::string plugin;    // "Skyrim.esm"（切 id 的前半）
    bool addsMaster;       // plugin 不在 base-game 集合 ⇒ 會給 patch 加 master
    RE::ObjectRefHandle handle;
};
```

- [ ] **Step 1**：`MarkCrosshair()` — 取準星 ref；`ResolveDurableId(&ref)` 成功 ⇒ `Disable()` + push；失敗 ⇒ dynamic 分支（依 Task 0 Step 1 的結論）。重複標記同一個 id 要 no-op。
- [ ] **Step 2**：`Undo()` 彈出最後一筆並 `Enable(false)`；`Clear()` 全部 `Enable(false)` 後清空。handle 失效時只從清單移除、log 一行。
- [ ] **Step 3**：`MarkedIds()` 回一個 `unordered_set<string>` 供匯出查表。
- [ ] **Step 4**：`addsMaster` 判定——base-game 集合 = `Skyrim.esm` / `Update.esm` / `Dawnguard.esm` / `HearthFires.esm` / `Dragonborn.esm`。**CC 的 `cc*.esl` 不在集合內**（它們確實會加 master），這點要在面板說清楚。

### Task 2：hotkey 綁定

**Files:** `src/plugin.cpp`（`HotkeySink` 加一個 scancode 分支）。

- [ ] **Step 1**：先做 Task 0 Step 2 的 log 探測，確定 scancode 再寫死。
- [ ] **Step 2**：與 F10 共用同一個 sink、同一個 200ms debounce。

### Task 3：匯出整合

**Files:** `src/SceneExporter.cpp`。

- [ ] **Step 1**：`ExportCell` 走訪時，authored ref 的 id 若在 `Eraser::MarkedIds()` 內 ⇒ **不計入 `preexisting`**（它不是「保持原狀的既有 ref」）。
- [ ] **Step 2**：走訪結束後，若清單非空 ⇒ `scene["removals"] = [清單全部 id]`。**注意 `removals` 是 `ModSpec` 的合法成員**（`Spec.cs:45`），不會被靜默忽略。
- [ ] **Step 3**：`Stats` 加一個 `removals` 計數 + `addsMasters` 的 plugin 名集合，log 出來。

### Task 4：面板

**Files:** `src/UI.cpp`（新增 `Eraser` 頁）。

橡皮擦**不能**做成面板按鈕：面板一開，準星早已不在目標上。所以 hotkey 標記、面板管理。

- [ ] **Step 1**：清單每筆一行（耐久 id）。`addsMaster` 為真的用警告色 + 「這會讓你的 patch 依賴 `X.esp`」。
- [ ] **Step 2**：`Undo` / `Clear` 按鈕。
- [ ] **Step 3**：`Adopt disabled refs in this cell` 掃描鍵——掃當前 cell，列出「authored ref 且 `IsDisabled()` 為真、但 record 上不是 `InitiallyDisabled`」的候選，**逐筆確認再加入**。推導只在按下去時發生，避開任務關掉的雜物、腳本清掉的屍體那類誤判。

### Task 5：驗收

- [ ] **Step 1（實機）**：進 Bannered Mare，對一張 vanilla 椅子按鍵 → 消失。F1 見清單一筆 `Skyrim.esm:0x…`。`Undo` → 椅子回來。
- [ ] **Step 2（實機）**：再擦掉 → Export。
- [ ] **Step 3（離線）**：`validate` 零問題；`build`；`dump` → 該 REFR 的 override 帶 `0x800`、Z 被埋、master 僅 `Skyrim.esm`。

> Step 3 完全離線可做。實機只需 Step 1–2。

---

## M7：滴管（§E ①）—— 拆成 a/b，b 擋在契約決定後面

**規模**：runtime 側中等；ModForge **零改動**（吸來的 base 進 `placements[].base`，`PluginIo.cs:35` 會自動把來源 mod 加為 master）。

準星吸取一個 ref 的 **base + 當前 rotation + scale** 進一個**具名插槽**；之後選插槽 `PlaceAtMe(base)` 落地並回填 rot/scale。等於玩家在遊戲內即時建自己的開放式調色盤——想擺什麼就吸什麼，取代 Tundra Defense 那種設計期寫死的 REFR 目錄。

### M7a：吸取 + 擺放 + 微調**新** ref（契約零改動，可立即做）

- 吸取用 `CrosshairPickData`（同 M6，已確認）。**不要**用「投射物命中」——STAT 靜物不吃魔法效果。
- 吸中回饋：`EffectShader.Play(ref, ~1.5s)`（vanilla 有現成發光 shader）。純 runtime，不進 `scene.json`。
- 插槽存哪：idea §E 原本寫 StorageUtil KV（需 PapyrusUtil）。**但我們現在有 C++ 面板**——直接存 DLL 記憶體 + 一個 sidecar json 即可，不必拉 PapyrusUtil 相依。命名走 ImGui `InputText`，不必 UILib。
- 微調：`SetPosition` / `SetAngle` / `SetScale`（`TESObjectREFR.h:458/464/466`，皆已確認存在）。**只作用在自己剛擺的 dynamic ref 上**，所以 vanilla diff 照常把它們 emit 進 `placements[]`，契約零改動。

### M7b：移動**既有** ref（擋在「override 形狀」拍板之後）

吸一面牆擺下去，你自然會想把它對齊既有的牆——那面既有的牆被移動了，而 `scene.json` 沒有「既有 ref 的 override」形狀。**必須明示登記**（不能用 diff，`GetPosition()` 就是 `data.location`，且 havok 會自己移動東西）。詳見「技術債 / 未決」與 [spec](../specs/ingame-scene-export-design.md)。

> **M7a 先做，M7b 等 M6 累積實感後再拍契約。** 這樣滴管的主體價值（開放調色盤）不必等契約決定。

### ⚠️ placement controller：C++ 還是 `.pex`？——**兩者都要，但不是同一支**

idea #24 §② 寫「本 idea 的施法擺設與 settlements Phase-2 的 `buildables:` **共用同一支 controller**——兩線合流，是最強的協同」。**這句話是錯的**（2026-07-10 修正）。兩者的**部署限制不同**：

| | 編輯器 controller（idea #24 §②） | 出貨 controller（settlements P2 `buildables:`） |
|---|---|---|
| 何時跑 | **作者的編輯 session** | **玩家的遊戲**裡，在 ModForge 生的 mod 中 |
| 載體 | 我們的 `SceneCaptureBridge.dll`（作者機器上一定有） | **必須是 `.pex`**——ModForge 生的 mod 夾帶不了 DLL |
| 實作 | C++，每幀、直接 `SetPosition`/`SetAngle`/`SetScale`，共用 ImGui 面板 | Papyrus 狀態機（Tundra `aaaFortMainQuestScript` 等價），`scriptAttach` 掛上 |

所以**共用的是設計（模式/軸/輸入語彙），不是程式碼**。M7a 的微調直接寫 C++，不必等 settlements P2 的 `.pex`，也不必為它寫 Papyrus。反之 settlements P2 仍需要那支 `.pex`（`.pex` 那條 [settlements P2](../roadmap/mod-survey-gaps/settlements-phase2.md) 的判定不受影響）。

## M8：範圍吸取（§E ②）

一次吸半徑內所有 ref 進捕獲集。`cell->ForEachReferenceInRange(playerPos, radius, ...)`（SkyLink 的 `get_nearby_objects` 用的就是它，`WorldManager.cpp:190`）。每個 ref 取 base + transform + scale。ModForge **零改動**（一樣是一批 `placements[]`，只是來源是範圍不是單點）。

面板要能預覽「這次會吸到 N 個」再確認——半徑掃描很容易一口氣吸進整個房間。

## M9：marker／標註系統（§B 語意標記 ＋ 地形/任意標註，合併為一個系統）

**（2026-07-10 細摳後重定義）**原本 §B 只涵蓋「會被 ModForge 展開成記錄」的語意標記。使用者的地形細摳揭示了一個更通用的原語：**具名座標 marker**，一個 `kind` 欄位決定它是**生成性**還是**建議性**：

| kind | 匯出到 | ModForge 行為 |
|---|---|---|
| `mapMarker` / `vfx` / `tag`（§B 原有） | `mapMarkers[]` / `hazards[]` / `tags[]` | **展開成真記錄**（生成端已全備） |
| `note`（自由文字，例：「這裡地形抬升」） | 標註段（形狀見下） | **不生成**——給人/AI agent 讀的座標錨點 |
| `navmesh`（有序點列；idea 的 navmesh 記點願景） | 標註段 | 未來餵 navmesh 生成（三角化在 ModForge 側；`programmatic-navmesh` 已實機） |

**放置機制（兩案，皆待驗）**：

- **A. 符文式法術（rune-style）**：引擎原生機制——符文法術（如 Fire Rune）本來就把一個物件放在**瞄準的表面命中點**上，純 esp、零 SKSE 碼，且放出來的是 dynamic ref → **vanilla diff 自動採到**。待驗：符文的放置面限制（地板/牆？）、`iMaxAttachedRunes` 上限、以及編輯器自用 esp 怎麼來（**編輯器工具 esp ≠ 出貨產物**，手做一次或 ModForge 生一次都行，不挑剔）。
- **B. C++ 射線（bhkPickData raycast）**：從鏡頭射線取任意命中點 → `PlaceAtMe` marker。更自由（不受符文面限制），但 raycast API 待驗（CommonLibSSE 的 `bhkPickData`；**不憑印象寫**——`ForEachReference` 前車之鑑）。

法術美學（使用者的願景語彙是「施放法術」）偏 A；技術自由度偏 B。可以 A 起步、B 補位。

**改名 UX**：marker 是 dynamic ref，名字存 DLL 記憶體（同 M6 橡皮擦的清單模型）；面板列出所有 marker + ImGui `InputText` 改名。不需要 UILib/activate 對話框。

**標註段的形狀（三案，待使用者拍板）**：

| | 去處 | 優點 | 缺點 |
|---|---|---|---|
| a. `_annotations`（底線鍵） | scene.json 內 | 一個檔；**已驗**：`Program.Schema.cs:13` 刻意放行 `_`/`//` 前綴（註解慣例），validate 不叫 | 語意上是「註解空間」，工具鏈可能視為可剝除；deserialize 一樣靜默忽略 |
| b. 一等公民 `Annotations` 欄位 | `ModSpec` 加 ~5 行 | validate 安全、build 可 log「N annotations (advisory)」、agent 讀一個檔 | ModSpec 混入一個「不生成」的欄位，語意要寫清楚 |
| c. sidecar（`scene-annotations.json`） | 獨立檔 | ModSpec 純淨 | agent 要讀兩個檔；檔案配對靠命名慣例 |

傾向 **b**（明確、一個檔、有 no-op log），但等使用者審。

**navmesh 記點（願景項，遠期）**：同一個 marker 系統，`kind=navmesh` + 序號。已知先例：**Debug Menu mod 能在遊戲內視覺化 navmesh**（見 [gemini-research 報告](../../sub_projs/gemini-research/2026-07-10-ingame-scene-editor-prior-art.md)），「能不能顯示出來」答案是能，讀它怎麼畫。點怎麼連（三角化）歸 ModForge 側，不在遊戲內做。

## M10：role tag（§D）

給 `placements[]` 裡的某個 actor 貼 `{ actorRef, role, backstory }` → `npcRoles[]`。ModForge 側的 role macro **已落地並實機確認**（blacksmith：conditioned-Hello + sandbox package + vendor）。

⚠️ `npcRoles[].actorRef` 需要**耐久的 ref id**。走「大眾臉」路徑時，NPC 的 placement 是 in-spec authored ⇒ ref 耐久 ⇒ 沒問題。走 PROTEUS 路徑時 clone 的 ref 必為 dynamic ⇒ **指不到**。這是 PROTEUS 降為可選的直接後果，M10 只支援前者。

---

## 技術債 / 未決

**`scene.json` 需要「既有 ref 的 override」形狀。** 兩案（`placements[].overrideOf` vs 新開 `overrides[]`）與取捨已寫進 [spec](../specs/ingame-scene-export-design.md)「既有 ref 的 override 形狀」。ModForge 側成本低（`BuildRemovals` 已有整套 `GetOrAddAsOverride` 機件，約 30 行）。**先做 M6 累積實感再拍板。**

- ~~「怎麼知道 ref 被移動過」~~ **已解（2026-07-10 細摳②）**：使用者的修改 UX（法術選中→numpad 編輯）本身就是明示登記——被選中且動過的才進清單，havok 假陽性天然排除。剩契約形狀（`overrideOf` vs `overrides[]`）待拍板。
- ⚠️ `SceneExporter.cpp` 裡「authored transform, not live physics pose」是 stub 留下的**未驗證宣稱，待修**。

**`TESObjectREFR::Delete()` 是否存在。** 未確認。影響「玩家自擺物件能不能真的刪掉，還是只能隱藏」。

**`Delete` 鍵的 scancode。** 未確認。實測取得，不假設。

**PROTEUS 路徑 A 的 clone ref/base 耐久性。** 可選項，不阻塞。見 [spec](../specs/ingame-scene-export-design.md)「NPC 來源」。
