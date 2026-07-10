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
| 玩家移動/縮放過的 vanilla ref | **不採**（需 `scene.json` 長出「既有 ref 的 override」形狀）。橡皮擦繞過它；**滴管會撞上**——見下方技術債 | spec §契約 |

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

## M7：滴管（§E ①）

**規模**：runtime 側中等；ModForge **零改動**（吸來的 base 進 `placements[].base`，`PluginIo.cs:35` 會自動把來源 mod 加為 master）。

準星吸取一個 ref 的 **base + 當前 rotation + scale** 進一個**具名插槽**；之後選插槽 `PlaceAtMe(base)` 落地並回填 rot/scale。等於玩家在遊戲內即時建自己的開放式調色盤——想擺什麼就吸什麼，取代 Tundra Defense 那種設計期寫死的 REFR 目錄。

- 吸取用 `CrosshairPickData`（同 M6，已確認）。**不要**用「投射物命中」——STAT 靜物不吃魔法效果。
- 吸中回饋：`EffectShader.Play(ref, ~1.5s)`（vanilla 有現成發光 shader）。純 runtime，不進 `scene.json`。
- 插槽存哪：idea §E 原本寫 StorageUtil KV（需 PapyrusUtil）。**但我們現在有 C++ 面板**——直接存 DLL 記憶體 + 一個 sidecar json 即可，不必拉 PapyrusUtil 相依。命名走 ImGui `InputText`，不必 UILib。
- ⚠️ **這一步會撞上技術債**：吸一面牆擺下去，你自然會想把它對齊既有的牆——那時 vanilla ref 被移動了，而 `scene.json` 沒有「既有 ref 的 override」形狀。見下。

## M8：範圍吸取（§E ②）

一次吸半徑內所有 ref 進捕獲集。`cell->ForEachReferenceInRange(playerPos, radius, ...)`（SkyLink 的 `get_nearby_objects` 用的就是它，`WorldManager.cpp:190`）。每個 ref 取 base + transform + scale。ModForge **零改動**（一樣是一批 `placements[]`，只是來源是範圍不是單點）。

面板要能預覽「這次會吸到 N 個」再確認——半徑掃描很容易一口氣吸進整個房間。

## M9：語意標記（§B）

在面板上下「意圖標記」而非實體物件：地圖 marker / 特效錨點 / 功能標籤。採集橋只要輸出 `{kind, at, params}`，展開成 `mapMarkers[]` / `hazards[]` / `tags[]`——**這三段 ModForge 今天就吃得下**（`Spec.MapMarkers.cs` / `Generator.Build.Hazards.cs` / KYWD 生成）。

UI 上大概是「站到那個位置 → 面板選 kind → 填參數 → Add」。座標取玩家當前位置即可。

## M10：role tag（§D）

給 `placements[]` 裡的某個 actor 貼 `{ actorRef, role, backstory }` → `npcRoles[]`。ModForge 側的 role macro **已落地並實機確認**（blacksmith：conditioned-Hello + sandbox package + vendor）。

⚠️ `npcRoles[].actorRef` 需要**耐久的 ref id**。走「大眾臉」路徑時，NPC 的 placement 是 in-spec authored ⇒ ref 耐久 ⇒ 沒問題。走 PROTEUS 路徑時 clone 的 ref 必為 dynamic ⇒ **指不到**。這是 PROTEUS 降為可選的直接後果，M10 只支援前者。

---

## 技術債 / 未決

**`scene.json` 需要「既有 ref 的 override」形狀。** 目前只有 `removals[]` 碰既有 ref。玩家移動/縮放過的 vanilla ref 一律被 vanilla diff 跳過。M6 繞得過，**M7 繞不過**（滴管擺完就想對齊既有的牆）。屆時要決定：`placements[]` 加一個 `overrideOf` 欄位？還是新開一段 `overrides[]`？——先做 M6 累積實感再決定，不要現在拍板。

**`TESObjectREFR::Delete()` 是否存在。** 未確認。影響「玩家自擺物件能不能真的刪掉，還是只能隱藏」。

**`Delete` 鍵的 scancode。** 未確認。實測取得，不假設。

**PROTEUS 路徑 A 的 clone ref/base 耐久性。** 可選項，不阻塞。見 [spec](../specs/ingame-scene-export-design.md)「NPC 來源」。
