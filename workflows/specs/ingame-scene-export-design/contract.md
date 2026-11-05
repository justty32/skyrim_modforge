# 遊戲內場景匯出 — 契約 scene.json、座標契約、vanilla diff

← [ingame-scene-export-design](../ingame-scene-export-design.md)（入口）

## 契約：scene.json（採集橋 output ↔ ModForge input）

**設計原則：scene.json 就是一份 ModForge spec**（或其片段）。採集橋吐的每個欄位都**直接對映既有 spec 型別**，不發明新結構——這樣 ModForge 側 net-new 趨近於零。逐段對映（右欄 = 既有型別，grep 驗證 2026-07-08）：

| scene.json 段 | 採集橋放什麼 | 對映的既有 ModForge 型別 | 證據 |
|---|---|---|---|
| `placements[]` | 每個擺放 ref 的 base + cell/worldspace + position/rotation/scale + enable state | **`PlacementSpec`**（已含全部欄位：Base/Cell/Worldspace/Position/Rotation/Scale/Persistent/InitiallyDisabled/EnableParent/Ownership/Lock/LinkedRefs）| `Spec.World.cs` PlacementSpec |
| `npcRefs[]` | PROTEUS clone 的穩定 ActorRef（`<plugin>.esp:0xFORMID`）+ 位置 + **role tag** | **`PlacementSpec`（base = 外部 ActorRef）** + §D 角色 macro（見 [npc-source](npc-source.md)）| PlacementSpec base 支援 `<master>:0xFORMID`（It.7d）|
| `mapMarkers[]` | 座標 + Name + Type（Town/City…）+ flags（Visible\|CanTravelTo）| **`MapMarkerSpec`** | `Spec.MapMarkers.cs`（實機 [[worldspace-override-map-render-fields]]）|
| `hazards[]` | 特效錨點座標 + model/light/spell/imad | **`HazardSpec`** + `LightSpec` | `Spec.Lights.cs`/`Generator.Build.Hazards.cs` |
| `tags[]` | 功能/身份標籤 → 掛到 ref/cell 的 keyword | 既有 KYWD 生成 + FormListInject | `Spec.FormListInject.cs` 等 |
| `npcRoles[]` | `{ actorRef, role, backstory }`（§D 的核心新欄）| **§D `SceneNpcRoleSpec` macro**（下節，唯一 net-new schema）| 重用 SettlementVendorSpec/package/conditioned-Hello |
| `removals[]` | 橡皮擦法術標記要移除的**既有 vanilla ref**（`<master>:0xFORMID`）| **✅ `BuildRemovals`**（GetOrAddAsOverride + InitiallyDisabled + 深埋）| 已落地 2026-07-08 |
| `overrides[]` | numpad 編輯器 commit 過的**既有 ref 新 transform** | **✅ `OverrideSpec` / `BuildOverrides`** | 已落地 2026-07-11（下面「既有 ref 的 override 形狀」節（[contract-overrides](contract-overrides.md)））|
| `references[]` | referrer（`sc ref`）標記的**既有 ref 身份＋自由 label**（檔內 placement editorId，或外部 `<master>:0xFORMID`）| **✅ `ReferenceSpec` / `BuildReferences`**——label 註冊成可解析的名字，任何 ref 欄位可指它；檔內目標強制 persistent；外部 temporary → 警告＋`anchor` 逃生門 | 已落地 2026-07-12（下面「referrer 的形狀」節（[references](references.md)））|
| `cell` / `worldspace` | 快照的目標 cell（override 目標）| **`CellSpec`** override + worldspace override | `Spec.World.cs`/[[worldspace-override-must-carry-topcell]] |

→ **落點裁決**：`placements`/`mapMarkers`/`hazards`/`tags`/`cell` 段 **ModForge 今天就能吃**（採集橋只要吐對形狀）。**唯一 net-new 的 ModForge schema = `npcRoles[]` 這一段的角色 macro**（下節）。

### 座標契約（採集橋 must-honor）

- interior：`cell` = 目標 cell 的 `<master>:0xFORMID`，`position` = **cell-local**。
- exterior：`worldspace` = worldspace ref，`position` = **world-space**（ModForge 自動找 `floor(x/4096),floor(y/4096)` 的 cell 並 override 加 ref，It.7d-p3）。
- `rotation` 度數。採集橋讀遊戲內 ref 的 world transform，**須與此約定一致**（若遊戲內拿到的是弧度/象限差，採集橋負責轉換，不是 ModForge）。
- **✅ 已用 houseCARL 離線核對 vanilla 基準真相（2026-07-10，不需開遊戲）**：
  - **旋轉單位**：`01605E:Skyrim.esm`（WhiterunBanneredMare）的 `Temporary[15]`（YsoldasChairREF）`Placement.Rotation = 0,-0,2.2730167` → **plugin 記錄存弧度**。ModForge 已在生成端轉換（`Generator.Helpers.cs:15` `Deg2Rad`，套用於 `Generator.Build.Placements.cs:68/275`、`Generator.Build.PlacementRefs.cs:53`）。→ **scene.json 一律吐度數**。⚠️ 兩條路線的來源單位不同，別搞混：**Papyrus** `GetAngleX/Y/Z()` 回**度數**（直接吐）；**C++ native** `TESObjectREFR::GetAngle()` 回**弧度**（必須轉，`SceneExporter.cpp:8,93-96` 的 `kRadToDeg` 已處理）。
  - **Scale 省略**：同一 ref `Scale = (absent)`；`Spec.World.cs:74` 註解 `// XSCL; omitted in record if 1.0`。→ 採集橋**不要無條件寫 `scale: 1.0`**（會與 vanilla 位元組不同）；ModForge 側已處理。
  - **歷史待驗（已結案）**：當時尚未確認 interior runtime 座標是否即 cell-local；2026-07-10 的 runtime 結果已在下方「座標契約 round-trip」（[contract-overrides](contract-overrides.md)）確認為 cell-local。
  - **規模參考**：該 cell 有 **662 個 `Temporary` ref**。逐個 `execute_console_command` 取 transform 只適合驗證，真正採集需要 batch tool（見「採集橋 vs SkyLink」）。

### vanilla diff：採集橋只吐「玩家加的東西」（2026-07-10 歷史判定）

> ⚠️ 以下是 `overrides[]` 落地前的舊判定。2026-07-11 已定案並實作 `OverrideSpec`／`BuildOverrides`；現況見下一節（[contract-overrides](contract-overrides.md)）。

cell 走訪看得到**cell 裡的每一個 ref**。若全部吐出來，ModForge 會把整個 vanilla 房間在原地再擺一份（Bannered Mare 662 個 ref，每張椅子疊兩張）。所以採集橋必須自己做 diff。

**判別式是免費的**：任何在某個 plugin 裡被 authored 的 ref 都解得出耐久 id；玩家在遊戲內 `PlaceAtMe` 生出來的 ref 活在 **dynamic `0xFF......` 範圍、`GetFile(0) == nullptr`**。

- `ResolveDurableId(&ref)` **成功** → 既有 ref → **跳過**（計入 `preexisting`）。
- `ResolveDurableId(&ref)` **失敗** → 玩家擺的 → emit 進 `placements[]`（其 `base` 仍解得出耐久 id）。**⚠️ 2026-07-12 起 actor 除外**：玩家擺的 actor 也不出（見下「場景匯出不含 NPC」（[captures](captures.md)）拍板）。

**歷史限制（現已解除）**：當時玩家**移動/縮放過的 vanilla ref** 會被跳過；2026-07-11 起已由頂層 `overrides[]` 建模，採集橋會輸出、ModForge 會 build（見下一節，[contract-overrides](contract-overrides.md)）。
