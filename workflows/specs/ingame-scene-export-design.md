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
| `overrides[]` | numpad 編輯器 commit 過的**既有 ref 新 transform** | **✅ `OverrideSpec` / `BuildOverrides`** | 已落地 2026-07-11（下面「既有 ref 的 override 形狀」節）|
| `references[]` | referrer（`sc ref`）標記的**既有 ref 身份＋自由 label**（檔內 placement editorId，或外部 `<master>:0xFORMID`）| **✅ `ReferenceSpec` / `BuildReferences`**——label 註冊成可解析的名字，任何 ref 欄位可指它；檔內目標強制 persistent；外部 temporary → 警告＋`anchor` 逃生門 | 已落地 2026-07-12（下面「referrer 的形狀」節）|
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
- `ResolveDurableId(&ref)` **失敗** → 玩家擺的 → emit 進 `placements[]`（其 `base` 仍解得出耐久 id）。**⚠️ 2026-07-12 起 actor 除外**：玩家擺的 actor 也不出（見下「場景匯出不含 NPC」拍板）。

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

- ❌ `npcRefs[]` 不是 ModSpec 成員（ModSpec 只有一個 `Placements` list）→ 整段被丟掉。**當時的修正：actor 與物件一律進 `placements[]`**（actor base 會讓 ModForge 生 ACHR；XSCL 對 actor 無效故不帶 scale）。本節開頭契約表的 `npcRefs[]` 那列**是概念分段，不是 JSON 鍵名**。**🔴 已被 2026-07-12 拍板推翻**——cell 掃描根本不出 actor（見下「場景匯出不含 NPC」），NPC 走 marker/captures。
- ❌ 頂層 `cell` / `worldspace` 不是 ModSpec 成員 → 被丟掉。**歸屬欄位在每一筆 `PlacementSpec` 上**（`Spec.World.cs:6-7`）。
- ❌ 兩者皆空的 placement 會被 `Generator.Build.Placements.cs:48` 以 `cell '' not found in spec — skipped` 丟棄。採集橋現在在解不出 cell/worldspace 時直接中止並 warn。

### ✅ 拍板（2026-07-11）：既有 ref 的 override 形狀＝**B 案，頂層 `overrides[]`**

**問題**（原文）：`scene.json` 只有 `removals[]` 碰既有 ref。玩家**移動/縮放過的 vanilla ref** 一律被 vanilla diff 跳過——吸一面牆擺下去，玩家自然會想把既有的牆對齊挪動。

**定案形狀**（`Spec.Overrides.cs` / `Generator.Build.Overrides.cs`，2026-07-11 全鏈落地）：

```json
"overrides": [
  { "ref": "Skyrim.esm:0x0D1991",
    "position": {"x": 19265.9, "y": -12816.5, "z": -4539.0},
    "rotation": {"x": 0, "y": 0, "z": 90.0},
    "scale": 1.5 }
]
```

position/rotation（度）必填＝新的完整 transform（不是 delta）；`scale` 選填——**省略＝不碰原記錄的 XSCL、1.0＝清掉 XSCL 回引擎預設**（採集橋一律明寫 live scale；actor 不帶 scale）。

**為什麼推翻原「傾向 A」**（當時說「先做 M6 累積實感再拍板」——實感累積完，一致指向 B）：

1. **PlacementSpec 已經長胖**：teleport/lock/ownership/linkedRefs/enableParent/count/persistent… 全部在 override 語境下無意義。A 案要 validate 排除的非法組合面隨 PlacementSpec 每次成長而變大；B 案的新型別只有 4 個欄位，永遠不會長錯東西。
2. **生成路徑本質不同**：AddNew 需要 cell/worldspace 歸屬（`BuildPlacements` 對空 cell 直接丟棄），GetOrAddAsOverride 不需要（resolved context 自帶 parent chain）。A 案得在 BuildPlacements 迴圈前面加特判繞過歸屬檢查——耦合進最熱的一條路徑。
3. **removals 前例**：碰既有 ref 的操作已住頂層段；overrides 是它的兄弟（remove existing / move existing 並列）。DLL 端同構：Eraser→`removals[]`、Overrides 登記→`overrides[]`。
4. **placements 語意已被實機固化**（2026-07-11 Winterhold 驗收）：「placements[] = 玩家新增的動態 ref」這條不變式是 vanilla diff 的根基，混入 override entry 會弄髒它。

與 removals 撞名＝validate 警告（矛盾）；build 讓 removal 後蓋而贏（`Build.cs` 順序：Overrides → Removals）。

#### 「怎麼知道 ref 被移動過」——不能用 diff，維持明示模型（已照此實作）

`TESObjectREFR::GetPosition()` 的定義**就是** `return data.location;`（`TESObjectREFR.h:405`）——引擎回報的是**當前**位置，不是 authored 值，且 havok 會自己移動東西（杯子從桌上滾下來），純 diff 會吐出一堆假的 override。所以與 removals 決策同構走**明示**：只有**經過 numpad 編輯器 commit** 的 authored ref 才進 `Overrides` 登記簿（`src/Overrides.{h,cpp}`，比照 Eraser），匯出時 emit live pose（commit 後物理沉降照實）。登記簿在 RAM：**關遊戲重開後，移動過的 pose 活在存檔裡但不會自動重新登記**——重新編輯一次（numpad 5 → 微調 → 0）即可，MVP 接受此限制（README「持久化與 adopt」表有列）。revert 按鈕回到 first-select baseline（havok 已滾動過的物件，baseline 是滾動後的 pose——不影響匯出，只影響 revert 落點）。

---

### ✅ 拍板（2026-07-12）：referrer 的形狀＝頂層 `references[]`（ModForge 消費端已落地）

**問題**（backlog「📌 pointer/referrer 原語」）：marker 標的是**空座標**（「這裡放東西」）；referrer 標的是**一個已存在 ref 的身份**（vanilla 椅子，或玩家自己 `sc pl` 擺的椅子）＋自由標籤，好讓下游 spec 拿標籤去引用它（例：給 Sofia 的 sandbox package 一個「sofia 的椅子」錨點）。

**定案形狀**（`Spec.References.cs` / `Generator.Build.References.cs` / `Generator.Validate.References.cs`，2026-07-12 C# 端全鏈落地）：

```json
"references": [
  { "ref": "MFRef_SofiaChair",          // (乙) 檔內 placements[] 的 editorId
    "label": "sofia's chair",           // 必填、全 spec 唯一
    "base": "Skyrim.esm:0x0B9C04",
    "worldspace": "Skyrim.esm:0x00003C",
    "position": { "x": 18700, "y": -12700, "z": -4590 },
    "rotation": { "x": 0, "y": 0, "z": 180 },
    "note": "she should always come back to this one" },

  { "ref": "Skyrim.esm:0x0D1991",       // (甲) 外部既有 vanilla ref
    "label": "skulvar's hoe",
    "base": "Skyrim.esm:0x02F2F4",
    "worldspace": "Skyrim.esm:0x00003C",
    "position": { "x": 19265.9, "y": -12816.5, "z": -4603.4 },
    "anchor": "replace" }               // persistent 逃生門（見下）
]
```

**核心語意＝`label` 是一個「可解析的名字」**：build 把它註冊進 pass-2 的 editorId→FormKey 表（`formKeyByEd`），所以 **spec 裡任何一個 ref 欄位都能直接寫這個 label** —— package 的 `sandbox.location` / `travel.place`、quest alias 的 `forced:`、`linkedRefs.target`、`enableParent.ref`、objective target、script Form property。消費站點**零改動**。ModForge **不生成**該 ref（唯一例外＝下面的 anchor）。三兄弟並列：`removals[]` 擦掉既有、`overrides[]` 移動既有、`references[]` **命名**既有。

**兩類目標（backlog 🔑 洞察，語意不同）：**

| | (乙) 檔內相依 | (甲) 外部既有 ref |
|---|---|---|
| `ref` | 同檔 `placements[]` 的 **editorId** | `<master>:0xFORMID` |
| 誰擁有它 | **我們**（玩家 `sc pl` 擺的，exporter 寫進 placements[]） | vanilla / 他 mod |
| persistent | build **強制** persistent（0x400 ＋ cell 的 Persistent group；機制同 linkedRefs target / package anchor） | 看它自己 |
| 🔴 坑 | 無——**乾淨路徑** | vanilla 場景物件多為 **temporary** → 不是可靠的 specific-reference 目標 |
| 離線可測 | ✅ | ❌（要 master link cache 查 0x400） |

**🔴 persistent 坑的處置（甲路徑）**：build 用 master link cache 解出該 ref，檢查 record header 的 **0x400 persistent flag**：

- 有 → 無警告，label 直接指它。
- 沒有 → **明確警告**（temporary ref；quest alias 的 specific-reference fill / package SingleRef target 可能撐不過 save/load），label 仍指它（不靜默丟）。
- `anchor` ＝逃生門，**ModForge 在該點放自己的 persistent 錨點**：
  - `"marker"` → 該座標生一支 **persistent XMarkerHeading**（0x400），**label 綁到 marker**。適用「只需要一個*地點*」（sandbox / travel / patrol 錨點）。
  - `"replace"` → 用 `base` 在該點生**我們自己的 persistent 複製品**（同 base/transform），**label 綁到複製品**，並把 vanilla 原件**自動加進 removals**（disable＋深埋，避免兩張椅子疊在一起）。適用「錨點必須*就是那個物件*」（坐**那張**椅子）。
  - 乙路徑上設 `anchor` ＝ validate 報錯（本來就是我們的、本來就 persistent）。

**Build 順序**（`Build.cs`）：`BuildPlacements` → `BuildMapMarkers` → **`BuildReferences`** → `BuildOverrides` → `BuildRemovals` → 全部 wire 步驟。必須在 placements 之後（檔內目標要先存在）、在 wire 之前（label 才解析得到）。`references[]` 為空時**不生任何記錄**（行為不變）。

**DLL 端（scene-capture-bridge）還缺的**：`sc ref` / `sc refc` 指令＋ References 面板頁；exporter 要偵測 referrer 目標 handle **∈ 本次匯出的 placements** → 給該 placement 發一個穩定 editorId、`references[].ref` 指那個 editorId（**否則 dynamic FormID 不可攜、build 後對不上**）；目標是外部既有 ref 時照記耐久 `<plugin>:0xLOCALID` ＋ `base` ＋座標＋（可得的話）rotation/scale，`anchor` 的選擇權留給 ModForge/agent。

**範例**：`examples/scene-references.json`（乙路徑端到端：椅子 placement → reference label → Sofia 的 sandbox package 以 label 當 location；build 出的 esp 裡 package slot 0 ＝ `LocationTarget(該椅子 REFR)`，椅子落在 cell 的 Persistent group 且帶 0x400）。測試：`tests/ModForge.Core.Tests/ReferencesTests.cs`。

### ✅ 拍板（2026-07-12，使用者）：**場景匯出不含 NPC** ＋ **captures 拆成獨立檔**

推翻 2026-07-10「actor 與物件一律進 `placements[]`」那條（上面 §「⚠️ 採集橋輸出必須是合法 ModSpec」第一點）。**DLL 端已落地**（`SceneExporter.cpp`，DLL crc `65f53a93`）。三條契約變更：

**① cell 匯出＝純場景/物件，actor 一律不出。** `ExportCell`/`ExportAll` 掃描時遇到 actor ref 直接跳過（計入 `actorsExcluded`，只進 log/面板統計），**`placements[]` 不再出現 `kind:"npc"` 的條目**。NPC 交給 ModForge 按 **`annotations[]`（marker）** 去擺——marker 帶 position/rotation/scale/label/kind/note，足以指定「這裡放一隻山羊，面向這邊」。理由：把 NPC 塞進 cell 掃描要處理 dynamic actor base、PROTEUS 外貌、role 標記…太麻煩，而 marker＋ModForge `NpcSpec` 這條路本來就已實機驗過。
- ⚠️ 這**不影響** `capturedNpcs[]`：真的要「複製這個 NPC」時走 `sc cap`（擷取器，帶完整外貌/perk/inventory/身份），那是**明示**採集，不是掃描產物。
- ⚠️ ModForge 端**零改動**：少一種輸入條目而已（`PlacementSpec.Kind = "npc"` 路徑照舊存在，手寫 spec 仍可用）。

**② `capturedItems[]` / `capturedNpcs[]` 從場景檔移出，走自己的檔。** 面板 Export 頁／Captures 頁各有一顆 **`Export captures`** 鈕 → 寫 `captures_<YYYYMMDD-HHMM>.json`（只含這兩段）。場景匯出檔**不再帶**這兩段。
- **仍是合法 ModSpec**：`CapturedItems`/`CapturedNpcs` 都是 `ModSpec` 成員（`Spec.cs:107`、`Spec.CapturedNpcs.cs`），所以 `build captures_20260712-1830.json out.esp` 單獨吃得下，**ModForge 端零改動**。要一次生成場景＋擷取物，就 build 兩份、或人工把兩個 json 併起來。
- 理由：擷取到的定義是**跨 cell 的資料庫**（一把附魔劍不屬於你站的那間房），混在場景檔裡會讓「這份檔＝這個地方」的語意變髒；而且每次 export 場景都重覆吐一整包 NPC 外貌資料。

**③ 匯出檔名帶場景＋時間戳**（連續 export 不再互相覆蓋）：
```
scene-export_<cell EditorID 或 worldspace+grid>_<YYYYMMDD-HHMM>.json   # Export player cell
scene-export_all-<玩家所在>_<YYYYMMDD-HHMM>.json                        # Export all (loaded cells)
captures_<YYYYMMDD-HHMM>.json                                          # Export captures
```
interior 用 cell 的 EditorID（如 `WhiterunBanneredMare`）；exterior 用 worldspace EditorID ＋ cell grid（如 `Tamriel_x5y-3`）。名稱 sanitize 成 `[A-Za-z0-9._-]`（其餘字元→`_`，截 48 字）；同分鐘同場景再匯出＝加 `-2`/`-3` 後綴，**永不覆蓋**。⚠️ 下游 agent 別再寫死 `scene-export.json`——**取該資料夾最新一份**（或使用者指定的那份）。

---

### ✅ 拍板＋落地（2026-07-12）：`sc capp` **直接吸玩家**（去 PROTEUS 化）＋ 顯式數值＋label→editorId

推翻下面「NPC 來源」節的**路徑 A（拓印玩家＝消費 PROTEUS clone）**：PROTEUS 能複製玩家的臉，只因引擎把 chargen 寫在**玩家的 base TESNPC**（`Skyrim.esm:0x000007`）上——DLL 直讀同一處即可，**不需要中介**。DLL 端已落地（crc `f8afc170`，co-save SCCP v8）；計畫全文＋落地摘要見 [plans/player-capture-capp.md](../plans/player-capture-capp.md)。契約變更三條：

**① `sc capp [Label]` ＝把玩家當一般 actor 擷取** → 一筆 `capturedNpcs[]`（外貌/perk/裝備/擺位全帶）。`base` 出來是 `Skyrim.esm:0x000007`（advisory，ModForge 只 MINT 不 override）。順手解掉 PROTEUS 路線三個缺陷：clone 自報 level 1／50-50-50、不寫 tintLayers、defaultOutfit 是空殼 → 裸體。玩家 perk 讀 `PlayerCharacter::addedPerks`（玩家 base 的 perk array 是空的），一般 NPC 照舊讀 base。

**② `capturedNpcs[]` 新增顯式數值欄（所有 actor 都吐，不只玩家）**：
```jsonc
"health": 320.0, "magicka": 150.0, "stamina": 210.0,   // base actor values（引擎真正跑的數字）
"skills": [41, 42, ...]                                // 18 個，引擎 AV 6..23 序
```
skills 的順序 ＝ `OneHanded, TwoHanded, Archery, Block, Smithing, HeavyArmor, LightArmor, Pickpocket, Lockpicking, Sneak, Alchemy, Speech, Alteration, Conjuration, Destruction, Illusion, Restoration, Enchanting` ＝ **Mutagen `Skill` enum 序**，index 即映射。**ModForge 消費優先序＝顯式數值 ＞ class autocalc**：有顯式值就寫 DNAM 且 `autoCalcStats` 關（autocalc 只是拿 class+level 估，載入時還會覆蓋掉寫死的值）；沒有才走舊的 class-autocalc 路（**舊 capture json 因此原樣相容**，欄位缺省 0）。

**③ `capturedItems[]` / `capturedNpcs[]` 可帶 `editorId`（label 機制）**：`sc capp <Label>` / `sc capc <Label>` 的標籤 → `editorId: "MFCap_<sanitised label>"`（非 alnum → `_`）。ModForge 既有的「顯式 editorId 優先」規則即為身份機制——**同一個 label 再吸一次＝同一筆記錄**（不會多生一個）。⚠️ label 走**未 `Lower()` 的 raw 參數**（`sc` 的參數解析會全轉小寫；`pkc`/referrer 的標號同此坑）。

---

### NPC 來源：PROTEUS 是**可選**，預設走「大眾臉」（2026-07-10 使用者定調）

> 🔴 **路徑 A（PROTEUS）已被 2026-07-12 拍板取代**——見上節 `sc capp`：直接吸玩家 base TESNPC，不需要 PROTEUS。以下保留為歷史脈絡。

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
