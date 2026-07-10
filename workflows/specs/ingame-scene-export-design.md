# 遊戲內場景匯出 — 設計方案（in-game 蓋城鎮 → scene JSON → ModForge patch）

← [specs 入口](README.md)｜idea：[#24 遊戲內編輯器](../idea/tools/24-ingame-editor.md)｜藍本：[Tundra Defense](../../sub_projs/mod-survey/findings/tundra-defense.md)・[PROTEUS](../../sub_projs/mod-survey/findings/proteus.md)

本 spec 涵蓋 **[Idea #24](../idea/tools/24-ingame-editor.md) 北極星「遊戲內蓋城鎮並匯出」的 ModForge 側契約 + 最小垂直切片**。核心發現（grep `src/ModForge.Core/` 驗證 2026-07-08）：**ModForge 的生成端幾乎全部已具備**（`PlacementSpec` 已含 position/rotation/scale/enableParent/ownership…、map marker/hazard/keyword/身份系統都已實機）。真正 net-new 的是**兩個 runtime 元件**（採集橋 SKSE DLL + placement-controller `.pex`）＋一座**「採集 → spec」的橋**。本 spec 定義那座橋的**契約**與最小切片；runtime 元件的內部實作各自成子專案（同 Tundra controller 之於 ModForge 的關係），不在本 spec 逐行設計。

---

## 目標 / 成功判準

北極星最小切片（**在遊戲內**做，**ModForge build 出**）：

1. **擺**：喝一瓶「Plans: 木屋」→ 定位模式 → 房子落地（placement-controller `.pex`，照 Tundra）。
2. **拓印**：用 PROTEUS 把當前玩家 clone 成一個站在原地的獨立 NPC（外貌/裝備/perk 由 PROTEUS 現成搞定），採集橋記下他的**穩定 ActorRef** + 玩家標的 **role=blacksmith**。
3. **標註**：村口放 1 個地圖 marker、廣場放 1 個特效錨點（採集橋記成語意標記）。
4. **匯出**：施法**快照整片區域** → 採集橋吐一份 **scene JSON**。
5. **生成**：`dotnet run -- build scene.json` → patch esp：房子在該 cell、鐵匠站著且**講 ModForge 生成的鐵匠對話**、marker 上地圖可快旅、特效在廣場。

**成功判準**：進遊戲載入該 patch → 城鎮就位、鐵匠有問候/服務對話、marker 可快旅、特效可見。**行為不變保證**：不帶 scene-import 的既有 spec 生成結果**位元不變**（scene JSON 只是既有 `placements[]`/`npcs[]`/`mapMarkers[]`/`hazards[]` 的一種來源，不改既有路徑）。

## 範圍邊界（YAGNI）

| 納入 MVP | 排除（後排） |
|---|---|
| 採集橋 → scene JSON 契約定義（本 spec 的核心產物）| 採集橋 DLL 內部實作（子專案，本 spec 只定 output 契約）|
| scene JSON → 既有 `placements[]`/NPC-ref/marker/hazard/keyword 的映射 | placement-controller `.pex` 內部（與 [settlements P2](../roadmap/mod-survey-gaps/settlements-phase2.md) 合流，另 design）|
| §D role tag → ModForge 灌對話/行為的 macro（1 個 role：blacksmith）| 多 archetype 全集、AI 生成對話文本（接 #17，後排）|
| PROTEUS clone 的**引用**（路徑 A，clone 穩定已拍板）| 路徑 B（ModForge 自建 facegen 生成獨立 NPC）——未來「可散布」才做 |
| 單 cell / 小片區域快照 | 整片 worldspace + 即時 navmesh 採集（硬項，後排）|
| import 既有 record 型別（ModForge 已能生者）| 新記錄型別（無——切片內不需要）|

---

## 架構：四個元件 + 一座橋

```
  [遊戲內 runtime]                          [build-time]
  ┌─────────────────────────┐
  │ ① placement-controller  │  喝瓶→定位→落地（照 Tundra，與 settlements P2 共用）
  │    .pex（隨附 reusable） │
  ├─────────────────────────┤
  │ ② PROTEUS（外部，消費）  │  玩家 → 穩定可引用的 clone NPC（facegen 白賺）
  ├─────────────────────────┤        ┌──────────── scene.json ───────────┐
  │ ③ 採集橋 SKSE DLL        │───────▶│  placements[] / npcRefs[] /        │
  │    （net-new 子專案）    │  匯出   │  mapMarkers[] / hazards[] /        │
  │  走訪 cell、讀 transform │        │  tags[] / npcRoles[]               │
  │  /enable、收 §B 語意標記 │        └────────────────┬───────────────────┘
  │  /記 clone ActorRef+身份 │                         │
  └─────────────────────────┘                         ▼
                                        ┌─────────────────────────────────┐
                                        │ ④ ModForge（本命，幾乎全已具備） │
                                        │  scene.json → 既有生成鏈 → patch │
                                        └─────────────────────────────────┘
```

- **① placement-controller `.pex`**：irreducibly bespoke Papyrus（Tundra §3.3 的 `aaaFortMainQuestScript` 等價）。ModForge 有「隨附 reusable `.pex` + `scriptAttach`」先例（MCM-Helper/dispatcher/PapyrusUtil）。**與 settlements P2 的 `buildables:` controller 是同一支，兩線合流**——本 spec 不重複設計，指向 settlements P2 design。**定位軸**：Tundra 已有三軸**旋轉**（`MODE_ROTATE_X/Y/Z`+`ChangeRotationAxis`）+**距離**（`MODE_DISTANCE`，沿視線推拉），**但無縮放、也無自由三軸位移**——本控制器要**多加兩個 mode**：`MODE_SCALE`（`SetScale`）、`MODE_TRANSLATE`（`GetPositionX/Y/Z`+delta→`SetPosition`，沿軸精確 nudge，如貼齊牆面），皆 vanilla Papyrus、共用 `AXIS`+Plus/Minus 輸入。ModForge 側 `PlacementSpec.Position`+`Rotation`+`Scale`(XSCL) **均已支援**，scene.json 契約已帶（見下 `placements[]`），故**匯出/生成零改動**，只差 controller 補這兩個 mode。⚠️ **XSCL 不作用於 actor**（`Spec.World.cs:43`）→ 縮放只對靜物/家具/光，拓印 NPC 不可縮放（位移/旋轉對 actor 正常）。
- **② PROTEUS**：消費、不改（native 閉源）。**crux 已拍板：clone 穩定、可被 esp 引用**（2026-07-08 使用者確認）→ 路徑 A 成立，facegen GAP 繞過。
- **③ 採集橋 SKSE DLL**：**唯一 net-new 的重工程**。走訪目標 cell 的 placed refs、讀每個 base+transform+enable、記 §B 語意標記與 §D 身份、收 PROTEUS clone 的 ActorRef，序列化成 scene.json。**本 spec 定義它的 output 契約**（下節）；內部實作（SKSE API、UI 走 [SKSE Menu Framework 3](../../sub_projs/mod-survey/findings/skse-menu-framework-3.md) ImGui）另立子專案。**含「滴管取樣」子能力**（見下）——採集橋負責的一個關鍵 runtime 責任是 **FormID → 耐久 `<plugin>:0xLOCALID` 反解**（`TESDataHandler`），因為 runtime FormID 高位元組是 load-order index、跨載入順序不穩，匯出前必須反解成插件相對 ID。
- **④ ModForge**：讀 scene.json → 既有生成鏈。**幾乎零 net-new 生成碼**（見「落點」）。

### ③附：滴管取樣 + 具名插槽（開放式調色盤，idea #24 §E）

Tundra 的可擺清單是設計期寫死的 FormID；本系統改用**遊戲內編輯法術組**（idea #24 §E，3 支）：

- **① 滴管（單點）**：讀準星 ref 的 **base + rot + scale**（`GetCurrentCrosshairRef`→`GetBaseObject`/`GetAngle`/`GetScale`）→ 存進 **StorageUtil 具名插槽**（命名走 UILib/ImGui）→ 選插槽 `PlaceAtMe` + 回填 rot/scale → placement-controller 微調。吸中瞬間在被吸 ref 播**成功特效**（`EffectShader.Play`，純 runtime 回饋、不進 scene.json）。
- **② 範圍吸取**：一次吸半徑內所有 ref（SKSE PO3 `FindAllReferences*` 或重用採集橋 cell 走訪 bound 半徑）→ 每個取 base+transform+scale。
- **③ 移除物件（橡皮擦）**：標記移除；session 內自擺 dynamic ref 直接 `Delete()`，**既有 vanilla ref → scene.json `removals[]`**。

**①② 對 ModForge 衝擊＝零**：吸來的 base 一樣以 `<plugin>:0xLOCALID` 進 `placements[].base`，**ModForge 對外部 ref 自動加來源 mod 為 master（已驗，`PluginIo.cs:35`）**；連帶只是**產物 master 清單隨吸過的物件增長**（匯出宜提示依賴哪些 mod）。採集橋的 **FormID→`<plugin>:0xLOCALID` 反解**（`TESDataHandler`）是讓「當次 session 的 runtime Form」變「耐久可 build 的插件相對 ref」的橋。

**③ 是唯一 net-new 生成項（`removals[]`，GAP 已 grep 驗證）**：ModForge placements 一律 `AddNew`、只能 disable **新** ref，**無「抓既有 vanilla REFR by FormID → disable/delete」路徑**。補法（小）：`removals: ["<master>:0xFORMID", …]` → 生成器在該 ref 所屬 cell 的 override（`VanillaCellOverride` 地基現成）裡把該子 ref 拿成 override 記錄 + 設 `InitiallyDisabled`(0x800)＋深埋（Z −30000，避 disabled havok）或 Delete flag。標準「disable vanilla clutter」patch，Mutagen 易做。→ 非 M0–M2 必需（切片不含移除），列為 §E 隨採集橋（M4）一起補的小生成項。

---

## 契約：scene.json（採集橋 output ↔ ModForge input）

**設計原則：scene.json 就是一份 ModForge spec**（或其片段）。採集橋吐的每個欄位都**直接對映既有 spec 型別**，不發明新結構——這樣 ModForge 側 net-new 趨近於零。逐段對映（右欄 = 既有型別，grep 驗證 2026-07-08）：

| scene.json 段 | 採集橋放什麼 | 對映的既有 ModForge 型別 | 證據 |
|---|---|---|---|
| `placements[]` | 每個擺放 ref 的 base + cell/worldspace + position/rotation/scale + enable state | **`PlacementSpec`**（已含全部欄位：Base/Cell/Worldspace/Position/Rotation/Scale/Persistent/InitiallyDisabled/EnableParent/Ownership/Lock/LinkedRefs）| `Spec.World.cs` PlacementSpec |
| `npcRefs[]` | PROTEUS clone 的穩定 ActorRef（`<plugin>.esp:0xFORMID`）+ 位置 + **role tag** | **`PlacementSpec`（base = 外部 ActorRef）** + §D 角色 macro（見下）| PlacementSpec base 支援 `<master>:0xFORMID`（It.7d）|
| `mapMarkers[]` | 座標 + Name + Type（Town/City…）+ flags（Visible\|CanTravelTo）| **`MapMarkerSpec`** | `Spec.MapMarkers.cs`（實機 [[worldspace-override-map-render-fields]]）|
| `hazards[]` | 特效錨點座標 + model/light/spell/imad | **`HazardSpec`** + `LightSpec` | `Spec.Lights.cs`/`Generator.Build.Hazards.cs` |
| `tags[]` | 功能/身份標籤 → 掛到 ref/cell 的 keyword | 既有 KYWD 生成 + FormListInject | `Spec.FormListInject.cs` 等 |
| `npcRoles[]` | `{ actorRef, role, backstory }`（§D 的核心新欄）| **§D `SceneNpcRoleSpec` macro**（下節，唯一 net-new schema）| 重用 SettlementVendorSpec/package/conditioned-Hello |
| `removals[]` | 橡皮擦法術標記要移除的**既有 vanilla ref**（`<master>:0xFORMID`）| **✅ `BuildRemovals`**（GetOrAddAsOverride + InitiallyDisabled + 深埋）| 已落地 2026-07-08 |
| `cell` / `worldspace` | 快照的目標 cell（override 目標）| **`CellSpec`** override + worldspace override | `Spec.World.cs`/[[worldspace-override-must-carry-topcell]] |

→ **落點裁決**：`placements`/`mapMarkers`/`hazards`/`tags`/`cell` 段 **ModForge 今天就能吃**（採集橋只要吐對形狀）。**唯一 net-new 的 ModForge schema = `npcRoles[]` 這一段的角色 macro**（下節）。

### 座標契約（採集橋 must-honor）

- interior：`cell` = 目標 cell 的 `<master>:0xFORMID`，`position` = **cell-local**。
- exterior：`worldspace` = worldspace ref，`position` = **world-space**（ModForge 自動找 `floor(x/4096),floor(y/4096)` 的 cell 並 override 加 ref，It.7d-p3）。
- `rotation` 度數。採集橋讀遊戲內 ref 的 world transform，**須與此約定一致**（若遊戲內拿到的是弧度/象限差，採集橋負責轉換，不是 ModForge）。
- **✅ 已用 houseCARL 離線核對 vanilla 基準真相（2026-07-10，不需開遊戲）**：
  - **旋轉單位**：`01605E:Skyrim.esm`（WhiterunBanneredMare）的 `Temporary[15]`（YsoldasChairREF）`Placement.Rotation = 0,-0,2.2730167` → **plugin 記錄存弧度**。ModForge 已在生成端轉換（`Generator.Helpers.cs:15` `Deg2Rad`，套用於 `Generator.Build.Placements.cs:68/275`、`Generator.Build.PlacementRefs.cs:53`）。→ **scene.json 一律吐度數**。⚠️ 兩條路線的來源單位不同，別搞混：**Papyrus** `GetAngleX/Y/Z()` 回**度數**（直接吐）；**C++ native** `TESObjectREFR::GetAngle()` 回**弧度**（必須轉，`SceneExporter.cpp:8,93-96` 的 `kRadToDeg` 已處理）。
  - **Scale 省略**：同一 ref `Scale = (absent)`；`Spec.World.cs:74` 註解 `// XSCL; omitted in record if 1.0`。→ 採集橋**不要無條件寫 `scale: 1.0`**（會與 vanilla 位元組不同）；ModForge 側已處理。
  - **尚未驗**：interior 的 runtime 座標是否即 cell-local（需實機比對 `getpos` vs 此處 `Placement.Position = -453.7385,-965.83203,67.837296`）。
  - **規模參考**：該 cell 有 **662 個 `Temporary` ref**。逐個 `execute_console_command` 取 transform 只適合驗證，真正採集需要 batch tool（見「採集橋 vs SkyLink」）。

### vanilla diff：採集橋只吐「玩家加的東西」（2026-07-10 定調）

cell 走訪看得到**cell 裡的每一個 ref**。若全部吐出來，ModForge 會把整個 vanilla 房間在原地再擺一份（Bannered Mare 662 個 ref，每張椅子疊兩張）。所以採集橋必須自己做 diff。

**判別式是免費的**：任何在某個 plugin 裡被 authored 的 ref 都解得出耐久 id；玩家在遊戲內 `PlaceAtMe` 生出來的 ref 活在 **dynamic `0xFF......` 範圍、`GetFile(0) == nullptr`**。

- `ResolveDurableId(&ref)` **成功** → 既有 ref → **跳過**（計入 `preexisting`）。
- `ResolveDurableId(&ref)` **失敗** → 玩家擺的 → emit 進 `placements[]` / `npcRefs[]`（其 `base` 仍解得出耐久 id）。

**⚠️ MVP 取捨（刻意，非疏漏）**：玩家**移動/縮放過的 vanilla ref** 會被跳過。要採到它，得 emit 一份既有 ref 的 **override**，而不是一筆新 placement —— 那是 scene.json 目前沒建模的形狀（只有 `removals[]` 碰既有 ref）。要做的話得先擴契約。

### ✅ 座標契約 round-trip（2026-07-10 離線閉環，不開遊戲）

拿 houseCARL 讀到的 vanilla 真值造一份採集橋形狀的 scene.json → `build` → 拆產出 esp 的 REFR `DATA` 子記錄：

```
vanilla plugin   rot.z = 2.2730167 rad   （01605E:Skyrim.esm Temporary[15]）
  ↓ 採集橋 kRadToDeg
scene.json       rot.z = 130.23426°
  ↓ ModForge Deg2Rad
產出 plugin      rot.z = 2.2730169 rad   差 2.3e-7 rad（float32 捨入，可忽略）
```

position 三分量完全一致。`dump` 確認 vanilla cell 是**加法式 override**（`temporary=1`），master 僅 `Skyrim.esm`。

**✅ runtime 端也驗完了（2026-07-10 實機）**：在 `01605E`（The Bannered Mare）`player.placeatme` 後匯出，`position = (48.99, 259.99, 321.48)`，與 SkyLink `get_cell_info` 回報的玩家座標**一字不差**，且與同 cell 的 vanilla ref `(-453.7, -965.8, 67.8)` 同一空間 → **interior 的 `ref.data.location` 就是 cell-local**。座標契約全部條目結案。

### ⚠️ 採集橋輸出必須是合法 ModSpec（2026-07-10 修正）

`scene.json` **就是一份 ModSpec**，而 `ReadOpts`（`Program.cs:145`）**沒設 `UnmappedMemberHandling`** → System.Text.Json 預設**靜默忽略未知鍵**。所以採集橋吐的每一個鍵都必須是真的 ModSpec 成員，否則無聲消失。首編後發現三處不合：

- ❌ `npcRefs[]` 不是 ModSpec 成員（ModSpec 只有一個 `Placements` list）→ 整段被丟掉。**修正：actor 與物件一律進 `placements[]`**（actor base 會讓 ModForge 生 ACHR；XSCL 對 actor 無效故不帶 scale）。本節開頭契約表的 `npcRefs[]` 那列**是概念分段，不是 JSON 鍵名**。
- ❌ 頂層 `cell` / `worldspace` 不是 ModSpec 成員 → 被丟掉。**歸屬欄位在每一筆 `PlacementSpec` 上**（`Spec.World.cs:6-7`）。
- ❌ 兩者皆空的 placement 會被 `Generator.Build.Placements.cs:48` 以 `cell '' not found in spec — skipped` 丟棄。採集橋現在在解不出 cell/worldspace 時直接中止並 warn。

### 🟡 未定案：既有 ref 的 override 形狀（擋著 M7 滴管）

**問題**：`scene.json` 目前只有 `removals[]` 碰既有 ref。玩家**移動/縮放過的 vanilla ref** 一律被 vanilla diff 跳過。M6 橡皮擦繞得過；**M7 滴管繞不過**——吸一面牆擺下去，玩家自然會想把它對齊既有的牆，那面既有的牆就被移動了。

#### ModForge 側成本：低

`BuildRemovals`（`Generator.Build.Removals.cs:21`）**已經有整套機件**：`cache.TryResolveContext<IPlaced, IPlacedGetter>(fk, out ctx)` → `ctx.GetOrAddAsOverride(mod)`（連帶 override parent cell/worldspace）→ 直接改 `ov.Placement.Position`。改 rotation/scale 是同一個物件上的欄位。所以生成端大概 30 行。

#### 契約形狀：兩案

| | A. `placements[].overrideOf` | B. 新開頂層 `overrides[]` |
|---|---|---|
| net-new schema | `PlacementSpec` 加一個 `string OverrideOf` | 新型別 + 新頂層 list |
| 採集橋輸出 | 同一個 entry 形狀多一個欄位 | 另一段 |
| 生成端分支 | `if (overrideOf != "") 走 GetOrAddAsOverride，否則 AddNew` | 獨立一段 |
| 語意風險 | `base` 欄位在 override 時無意義（必須留空或與既有 base 相符），需驗證 | 乾淨，`base` 不出現 |

**傾向 A**：`PlacementSpec` 已經帶 position/rotation/scale，採集橋吐的 entry 只多一個欄位，`scene.json` 不長新段。代價是要在 validate 加一條「`overrideOf` 與 `base` 互斥」的檢查。**先做 M6 累積實感再拍板**，不要現在定。

#### ⚠️ 「怎麼知道 ref 被移動過」——不能用 diff

`TESObjectREFR::GetPosition()` 的定義**就是** `return data.location;`（`TESObjectREFR.h:405`）。所以 `data.location` 是引擎回報的**當前**位置，不是 authored 值。`SceneExporter.cpp` 裡「authored transform, not live physics pose」那句註解是 stub 留下的**未經驗證宣稱，待修**。

後果：採集橋手上**沒有 authored 基準**可比對。而且 havok 會自己移動東西（杯子從桌上滾下來），純 diff 會吐出一堆假的 override。

兩條路，與 removals 的決策同構：

- **推導（不推薦）**：session 開始時快照全 cell 的 authored ref transform，匯出時 diff。717 個 ref 的記憶體成本可忽略，但 **havok 造成的位移無法與玩家的刻意移動區分**，且存檔重載後基準遺失。
- **明示（推薦）**：只有**經過編輯器移動**的 ref 才被登記。與 M6 橡皮擦「記憶體清單 + 明示 adopt」同一個模型——**明示優於推導**，因為推導的誤判在這裡有物理來源（havok），比 removals 那邊的任務腳本更難排除。

明示模型也意味著 **M7 需要一支「抓取/移動既有 ref」的工具**（Tundra 式的 placement controller，idea §② / settlements P2 合流），而不只是「吸取 + 擺放」。這是 M7 真正的範圍，比 idea 原本寫的大。

---

### NPC 來源：PROTEUS 是**可選**，預設走「大眾臉」（2026-07-10 使用者定調）

原設計把 §A 拓印玩家（PROTEUS）當成 NPC 的唯一來源。改為兩條並列，**預設是後者**：

| 路徑 | 外貌 | ref 耐久性 | 產物自足 |
|---|---|---|---|
| **C. 大眾臉（預設）** — ModForge 直接生 `NpcSpec` | 種族預設（`NpcSpec` 有 `Race`，**無** headpart/tint/facegen 欄位 → 引擎用預設頭） | **耐久**（in-spec authored placement，`npcRoles[].actorRef` 指得到） | ✅ 玩家端不需裝任何東西 |
| **A. 拓印玩家（可選）** — 消費 PROTEUS clone | 玩家本人的臉（PROTEUS native facegen） | ⚠️ 見下 | ❌ 依賴玩家端裝 PROTEUS |

大眾臉路徑**今天就能跑**：`NpcSpec` 生出來的 NPC 已在實機出貨過（vendor / hireable follower / identity 系統）。要「一群沒名字的村民」時它才是對的工具——facegen GAP 根本不在關鍵路徑上。

**⚠️ 路徑 A 的未解風險**（降為可選後不再阻塞 MVP）：vanilla diff 用「ref 解不出耐久 id ⇒ 玩家擺的」。PROTEUS clone 的 actor **ref 必然是 dynamic**（`PlaceAtMe`），會被正確判為玩家擺的——但 `npcRoles[].actorRef` 需要**耐久** ref id 才指得到它。若 clone 的 **NPC_ base 本身也是 runtime 生成**，`ResolveDurableId(base)` 一併失敗，該 actor 直接落進 `skipped`。idea §A「crux 已拍板：PROTEUS clone 是穩定、可引用的」須釐清指的是 base 還是 ref——**實機待驗**。

### ESL local-id 寬度（2026-07-10 以 houseCARL 離線核對，已拆 TODO）

`file->IsLight() ? (rawId & 0xFFF) : (rawId & 0xFFFFFF)` **正確**：`ccBGSSSE037-Curios.esl` 的 local id 最大 `0x88E`（全 < 0x1000，12 位元）；`Skyrim.esm` 的 `01605E` 是 24 位元。兩者都能 round-trip 成 ModForge 要的 `<plugin>:0xLOCALID`（6 位 hex 補零）。

---

## §D NPC 角色 macro（唯一 net-new ModForge schema）

scene.json 的 `npcRoles[]` 每筆 = `{ actorRef, role, backstory }`。ModForge build 時吃 role → **macro-expand 成既有生成型別**（對話 INFO + package + faction/service），底層零件全已實機——**macro 只是把它們串起來**：

```
npcRole: { actorRef: "SkyrimTown.esp:0x001234", role: "blacksmith",
           backstory: "曾是帝國軍鐵匠，戰後在此開鋪" }
   │  build-time macro-expand（1 個 role = 一包既有型別的組合）：
   ├─▶ 對話：conditioned Hello 問候 INFO（GetIsID actorRef）+ 服務 topic  ← [[conditioned-hello-one-topic-many-infos]]
   ├─▶ 行為：blacksmith sandbox package（綁鐵匠鋪 furniture/anvil）        ← [[radiant-alias-package-byte-truths]]
   ├─▶ 服務：vendor faction + merchant container（賣鐵匠貨）                ← 既有 Build.Vendor / SettlementVendorSpec
   └─▶ backstory → 對話文本（切片內手填；後續接 #17 AI 生成）
```

**⚠️ 用對既有機件（recon 2026-07-08 校正）**——兩個看似相關的既有型別都**不是**直接載體，別誤用：
- 既有 **`IdentitySpec`（[[identity-system-confirmed]]）是玩家向**：一個玩家加入的 FACT，`identity`/`primaryIdentity` tag 展開成**玩家**對話的 GetInFaction gate。**與 §D 無關**（§D 是給某個 NPC 一個職業角色，不是給玩家一個身份）。
- 既有 **`SettlementSpec.ResidentSpec` 最接近**，但它的 `Npc` 欄指向**in-spec** NpcSpec editorId、且以「住滿聚落」為框；§D 要的是**掛在外部 captured ActorRef 上**、且要**帶 conditioned-Hello 對話**。

→ **net-new = 一個小 sibling 型別 `SceneNpcRoleSpec { ActorRef, Role, Backstory }` + 一張 role→(package/vendor/對話模板) 對照表**（先只填 blacksmith）。生成器把對照表展開，**vendor 段重用 `SettlementVendorSpec`/Build.Vendor、package 段重用既有 package attach、對話段重用 conditioned-Hello（GetIsID 外部 ActorRef）**——零件全現成，只差這層 role→零件的薄 macro + 「keyed on 外部 ActorRef」這一點與 ResidentSpec 的差異。

- **對話仍 build-time 由 ModForge 生**（使用者定調）——遊戲內只**貼 role tag**，不在遊戲內生對話。
- **切片只做 1 個 role（blacksmith）**證明管線；role 全集（守衛/商人/冒險者…）沿用 [#23 living-adventurers 的 archetype 框架](../idea/living-adventurers.md)，一個 role = 一包資料（對話池/package/service），引擎不變。

---

## ModForge 落點（generable-today / net-new，grep `src/ModForge.Core/` 驗證）

| 環節 | 狀態 | 說明 |
|---|---|---|
| `placements[]`（含 transform/enable/scale/ownership）| ✅ **已具備** | `PlacementSpec` 欄位全齊，零改動 |
| `npcRefs[]`（引用 PROTEUS clone ActorRef）| ✅ **已具備** | PlacementSpec base = 外部 `.esp:0xFORMID`（跨 master 引用熟路）|
| `mapMarkers[]` / `hazards[]` / `tags[]` / `cell` override | ✅ **已具備** | MapMarkerSpec/HazardSpec/LightSpec/keyword/CellSpec override |
| **`npcRoles[]` 角色 macro** | 🔨 **net-new（小）** | `SceneNpcRoleSpec` + role→型別對照表（切片只填 blacksmith）；展開重用既有 vendor/package/conditioned-Hello 零件 |
| **`removals[]` 移除既有 vanilla ref**（橡皮擦法術）| ✅ **已落地 2026-07-08** | `BuildRemovals`：master link cache `TryResolveContext<IPlaced>` → `GetOrAddAsOverride` → InitiallyDisabled + 深埋 Z−30000。RequiresSkyrim |
| §E 滴管/範圍吸取（base+rot+scale→placements）| ✅ **已具備** | 進既有 `placements[]`（Position/Rotation/Scale 全有）；外部 ref 自動加 master |
| scene.json 讀取 / 併入 spec | 🔨 **net-new（小，有先例）** | `SceneImport` = **推廣既有 `GodotPlacements.Load()`**（已在做「外部 JSON → `spec.Placements.AddRange()`」，見 `Generator.Build.Worldspace.cs:255`）：讀 scene.json → AddRange 進 `Placements`/`MapMarkers`/`Hazards`/`npcRoles`，再走原 build。不改既有生成路徑（行為不變）|
| ① placement-controller `.pex` | 🔨 **net-new（runtime，合流 settlements P2）** | 隨附 reusable `.pex` + `scriptAttach`；與 `buildables:` 同一支 |
| ③ 採集橋 SKSE DLL | 🔨 **net-new（runtime，獨立子專案）** | 唯一重工程；本 spec 只定 output 契約 |
| ② PROTEUS facegen | ✅ **外部補位** | 消費，路徑 A |

**一句話**：ModForge 側 net-new 只有**兩小塊**（`npcRoles[]` 角色 macro + `SceneImport` 讀檔併入），其餘生成全已具備；**重工程在兩個 runtime 元件**（採集橋 DLL 獨立、controller 與 settlements P2 合流）。

---

## 最小垂直切片（驗證管線）

**里程碑序（每步可獨立驗）**：

1. **M0 契約凍結**：手寫一份 scene.json（不經採集橋）含 1 house placement + 1 npcRef（指向任一既有 standalone follower ActorRef 當 clone 替身）+ 1 mapMarker + 1 hazard + 1 npcRole=blacksmith → 定案 schema。
2. **M1 ModForge 側**：實作 `SceneImport` + `SceneNpcRoleSpec`(blacksmith) → `build` M0 的 scene.json → patch esp。**離線可驗**（`Category!=RequiresSkyrim`：斷言生成的 records = 房子 REFR + NPC ref + XMRK + HAZD + 鐵匠 dialogue INFO + package + vendor）。**行為不變測**：不帶 scene.json 的既有 spec 生成位元不變。
3. **M2 實機（主力機）**：載入 M1 的 patch → 房子在、marker 可快旅、特效可見、鐵匠有問候/服務對話。**此步不需採集橋/controller/PROTEUS**——用手寫 scene.json + 既有 follower ActorRef 替身，先證 ModForge 側管線通。
4. **M3 controller**：接 settlements P2 的 placement-controller → 遊戲內喝瓶擺 1 棟房子。
5. **M4 採集橋 spike**：最小 DLL 走訪 cell 吐 placements → 餵回 M1 → 閉環。
6. **M5 PROTEUS 拓印**：遊戲內 clone 玩家 → 採集橋記 ActorRef+身份 → build → 鐵匠是玩家本人的臉。

→ **M0–M2 純 ModForge，可立刻動工且離線可測**；M3–M6 依賴 runtime 元件，逐步接。**先做 M0–M2**（本 spec 的可立即落地部分）。

## 測試策略

- **離線單元（`Category!=RequiresSkyrim`）**：
  - scene.json round-trip：手造 scene.json → `SceneImport` → 斷言填進 `Spec` 的 list 內容正確。
  - role macro：blacksmith role → 斷言展開出 dialogue INFO（GetIsID condition）+ sandbox package + vendor faction。
  - **行為不變**：既有無 scene-import 的 spec → 生成位元不變（scene 只是另一資料來源）。
  - 座標映射：interior local vs exterior world 兩路各一 placement，斷言落在對的 cell。
- **實機（主力機，`RequiresSkyrim` / WAIT_USER）**：M2 起的城鎮就位 + 對話 + marker + 特效；M5 的玩家臉拓印。

## 開放 / 後續（非本 MVP）

- **採集橋 DLL 內部設計**：SKSE cell 走訪 API、ImGui 面板（SKSE Menu Framework 3）、語意標記的遊戲內下標 UX → 獨立子專案 spec。
- **placement-controller**：與 [settlements P2](../roadmap/mod-survey-gaps/settlements-phase2.md) 合流設計（喝瓶→定位狀態機）。
- **archetype 全集**：blacksmith 之外的守衛/商人/冒險者…（接 #23 框架）；對話文本 AI 生成（接 #17）。
- **路徑 B（ModForge 自建 facegen）**：讓產物不依賴玩家端 PROTEUS（可散布獨立 NPC）——接 asset-pipelines headless facegen 研究。
- **即時 navmesh 採集**：能完美尋路的城鎮（硬項）；MVP 先出可造訪版。
- **整片 worldspace 快照**：超出單 cell 的大範圍匯出。
</content>
