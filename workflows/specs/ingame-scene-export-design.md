# 遊戲內場景匯出 — 設計方案（in-game 蓋城鎮 → scene JSON → ModForge patch）

← [specs 入口](README.md)｜idea：[#24 遊戲內編輯器](../idea/tools/24-ingame-editor.md)｜藍本：[Tundra Defense](../../sub_projs/mod-survey/findings/tundra-defense.md)・[PROTEUS](../../sub_projs/mod-survey/findings/proteus.md)

本 spec 涵蓋 **[Idea #24](../idea/tools/24-ingame-editor.md) 北極星「遊戲內蓋城鎮並匯出」的 ModForge 側契約 + 最小垂直切片**。核心發現（grep `src/ModForge.Core/` 驗證 2026-07-08）：**ModForge 的生成端幾乎全部已具備**（`PlacementSpec` 已含 position/rotation/scale/enableParent/ownership…、map marker/hazard/keyword/身份系統都已實機）。真正 net-new 的是**兩個 runtime 元件**（採集橋 SKSE DLL + placement-controller `.pex`）＋一座**「採集 → spec」的橋**。本 spec 定義那座橋的**契約**與最小切片；runtime 元件的內部實作各自成子專案（同 Tundra controller 之於 ModForge 的關係），不在本 spec 逐行設計。

---

## 目標 / 成功判準

北極星最小切片（**在遊戲內**做，**ModForge build 出**）：

1. **擺**：喝一瓶「Plans: 木屋」→ 定位模式 → 房子落地（placement-controller `.pex`，照 Tundra）。
2. **拓印**：用 PROTEUS 把當前玩家 clone 成一個站在原地的獨立 NPC（外貌/裝備/perk 由 PROTEUS 現成搞定），採集橋記下他的**穩定 ActorRef** + 玩家標的 **identity=blacksmith**。
3. **標註**：村口放 1 個地圖 marker、廣場放 1 個特效錨點（採集橋記成語意標記）。
4. **匯出**：施法**快照整片區域** → 採集橋吐一份 **scene JSON**。
5. **生成**：`dotnet run -- build scene.json` → patch esp：房子在該 cell、鐵匠站著且**講 ModForge 生成的鐵匠對話**、marker 上地圖可快旅、特效在廣場。

**成功判準**：進遊戲載入該 patch → 城鎮就位、鐵匠有問候/服務對話、marker 可快旅、特效可見。**行為不變保證**：不帶 scene-import 的既有 spec 生成結果**位元不變**（scene JSON 只是既有 `placements[]`/`npcs[]`/`mapMarkers[]`/`hazards[]` 的一種來源，不改既有路徑）。

## 範圍邊界（YAGNI）

| 納入 MVP | 排除（後排） |
|---|---|
| 採集橋 → scene JSON 契約定義（本 spec 的核心產物）| 採集橋 DLL 內部實作（子專案，本 spec 只定 output 契約）|
| scene JSON → 既有 `placements[]`/NPC-ref/marker/hazard/keyword 的映射 | placement-controller `.pex` 內部（與 [settlements P2](../roadmap/mod-survey-gaps/settlements-phase2.md) 合流，另 design）|
| §D 身份 tag → ModForge 灌對話/行為的 macro（1 個 archetype：blacksmith）| 多 archetype 全集、AI 生成對話文本（接 #17，後排）|
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
  │  走訪 cell、讀 transform │        │  tags[] / identities[]             │
  │  /enable、收 §B 語意標記 │        └────────────────┬───────────────────┘
  │  /記 clone ActorRef+身份 │                         │
  └─────────────────────────┘                         ▼
                                        ┌─────────────────────────────────┐
                                        │ ④ ModForge（本命，幾乎全已具備） │
                                        │  scene.json → 既有生成鏈 → patch │
                                        └─────────────────────────────────┘
```

- **① placement-controller `.pex`**：irreducibly bespoke Papyrus（Tundra §3.3 的 `aaaFortMainQuestScript` 等價）。ModForge 有「隨附 reusable `.pex` + `scriptAttach`」先例（MCM-Helper/dispatcher/PapyrusUtil）。**與 settlements P2 的 `buildables:` controller 是同一支，兩線合流**——本 spec 不重複設計，指向 settlements P2 design。
- **② PROTEUS**：消費、不改（native 閉源）。**crux 已拍板：clone 穩定、可被 esp 引用**（2026-07-08 使用者確認）→ 路徑 A 成立，facegen GAP 繞過。
- **③ 採集橋 SKSE DLL**：**唯一 net-new 的重工程**。走訪目標 cell 的 placed refs、讀每個 base+transform+enable、記 §B 語意標記與 §D 身份、收 PROTEUS clone 的 ActorRef，序列化成 scene.json。**本 spec 定義它的 output 契約**（下節）；內部實作（SKSE API、UI 走 [SKSE Menu Framework 3](../../sub_projs/mod-survey/findings/skse-menu-framework-3.md) ImGui）另立子專案。
- **④ ModForge**：讀 scene.json → 既有生成鏈。**幾乎零 net-new 生成碼**（見「落點」）。

---

## 契約：scene.json（採集橋 output ↔ ModForge input）

**設計原則：scene.json 就是一份 ModForge spec**（或其片段）。採集橋吐的每個欄位都**直接對映既有 spec 型別**，不發明新結構——這樣 ModForge 側 net-new 趨近於零。逐段對映（右欄 = 既有型別，grep 驗證 2026-07-08）：

| scene.json 段 | 採集橋放什麼 | 對映的既有 ModForge 型別 | 證據 |
|---|---|---|---|
| `placements[]` | 每個擺放 ref 的 base + cell/worldspace + position/rotation/scale + enable state | **`PlacementSpec`**（已含全部欄位：Base/Cell/Worldspace/Position/Rotation/Scale/Persistent/InitiallyDisabled/EnableParent/Ownership/Lock/LinkedRefs）| `Spec.World.cs` PlacementSpec |
| `npcRefs[]` | PROTEUS clone 的穩定 ActorRef（`<plugin>.esp:0xFORMID`）+ 位置 + **identity tag** | **`PlacementSpec`（base = 外部 ActorRef）** + §D 身份 macro（見下）| PlacementSpec base 支援 `<master>:0xFORMID`（It.7d）|
| `mapMarkers[]` | 座標 + Name + Type（Town/City…）+ flags（Visible\|CanTravelTo）| **`MapMarkerSpec`** | `Spec.MapMarkers.cs`（實機 [[worldspace-override-map-render-fields]]）|
| `hazards[]` | 特效錨點座標 + model/light/spell/imad | **`HazardSpec`** + `LightSpec` | `Spec.Lights.cs`/`Generator.Build.Hazards.cs` |
| `tags[]` | 功能/身份標籤 → 掛到 ref/cell 的 keyword | 既有 KYWD 生成 + FormListInject | `Spec.FormListInject.cs` 等 |
| `identities[]` | `{ actorRef, archetype, backstory }`（§D 的核心新欄）| **§D 身份 macro**（下節，唯一 net-new schema）| #1c [[identity-system-confirmed]] |
| `cell` / `worldspace` | 快照的目標 cell（override 目標）| **`CellSpec`** override + worldspace override | `Spec.World.cs`/[[worldspace-override-must-carry-topcell]] |

→ **落點裁決**：`placements`/`mapMarkers`/`hazards`/`tags`/`cell` 段 **ModForge 今天就能吃**（採集橋只要吐對形狀）。**唯一 net-new 的 ModForge schema = `identities[]` 這一段的身份 macro**（下節）。

### 座標契約（採集橋 must-honor）

- interior：`cell` = 目標 cell 的 `<master>:0xFORMID`，`position` = **cell-local**。
- exterior：`worldspace` = worldspace ref，`position` = **world-space**（ModForge 自動找 `floor(x/4096),floor(y/4096)` 的 cell 並 override 加 ref，It.7d-p3）。
- `rotation` 度數。採集橋讀遊戲內 ref 的 world transform，**須與此約定一致**（若遊戲內拿到的是弧度/象限差，採集橋負責轉換，不是 ModForge）。

---

## §D 身份 macro（唯一 net-new ModForge schema）

scene.json 的 `identities[]` 每筆 = `{ actorRef, archetype, backstory }`。ModForge build 時吃 archetype → **macro-expand 成既有生成型別**（對話 INFO + package + faction/service），全部已實機（[[identity-system-confirmed]]/[[conditioned-hello-one-topic-many-infos]]/[[radiant-alias-package-byte-truths]]），**macro 只是把它們串起來**：

```
identity: { actorRef: "SkyrimTown.esp:0x001234", archetype: "blacksmith",
            backstory: "曾是帝國軍鐵匠，戰後在此開鋪" }
   │  build-time macro-expand（1 個 archetype = 一包既有型別的組合）：
   ├─▶ 對話：conditioned Hello 問候 INFO（GetIsID actorRef）+ 服務 topic  ← 既有
   ├─▶ 行為：blacksmith sandbox package（綁鐵匠鋪 furniture/anvil）        ← 既有
   ├─▶ 服務：vendor faction + merchant container（賣鐵匠貨）                ← 既有
   └─▶ backstory → 對話文本（切片內手填；後續接 #17 AI 生成）
```

- **對話仍 build-time 由 ModForge 生**（使用者定調）——遊戲內只**貼 identity tag**，不在遊戲內生對話。
- **切片只做 1 個 archetype（blacksmith）**證明管線；archetype 全集（守衛/商人/冒險者…）沿用 [#23 living-adventurers 的 archetype 框架](../idea/living-adventurers.md)，一個 archetype = 一包資料（對話池/package/service），引擎不變。
- **net-new schema 極小**：`IdentitySpec { ActorRef, Archetype, Backstory }` + 一張 archetype→（package/service/對話模板）對照表（先只填 blacksmith）。生成器把對照表展開成既有 `dialogue`/`packages`/`vendors`/`npcPatches` 呼叫。

---

## ModForge 落點（generable-today / net-new，grep `src/ModForge.Core/` 驗證）

| 環節 | 狀態 | 說明 |
|---|---|---|
| `placements[]`（含 transform/enable/scale/ownership）| ✅ **已具備** | `PlacementSpec` 欄位全齊，零改動 |
| `npcRefs[]`（引用 PROTEUS clone ActorRef）| ✅ **已具備** | PlacementSpec base = 外部 `.esp:0xFORMID`（跨 master 引用熟路）|
| `mapMarkers[]` / `hazards[]` / `tags[]` / `cell` override | ✅ **已具備** | MapMarkerSpec/HazardSpec/LightSpec/keyword/CellSpec override |
| **`identities[]` 身份 macro** | 🔨 **net-new（小）** | `IdentitySpec` + archetype→型別對照表（切片只填 blacksmith）；展開全走既有生成 |
| scene.json 讀取 / 併入 spec | 🔨 **net-new（小）** | 一支 `SceneImport`：讀 scene.json → 填進既有 `Spec` 物件的對應 list，再走原 build。不改既有生成路徑（行為不變）|
| ① placement-controller `.pex` | 🔨 **net-new（runtime，合流 settlements P2）** | 隨附 reusable `.pex` + `scriptAttach`；與 `buildables:` 同一支 |
| ③ 採集橋 SKSE DLL | 🔨 **net-new（runtime，獨立子專案）** | 唯一重工程；本 spec 只定 output 契約 |
| ② PROTEUS facegen | ✅ **外部補位** | 消費，路徑 A |

**一句話**：ModForge 側 net-new 只有**兩小塊**（`identities[]` macro + `SceneImport` 讀檔併入），其餘生成全已具備；**重工程在兩個 runtime 元件**（採集橋 DLL 獨立、controller 與 settlements P2 合流）。

---

## 最小垂直切片（驗證管線）

**里程碑序（每步可獨立驗）**：

1. **M0 契約凍結**：手寫一份 scene.json（不經採集橋）含 1 house placement + 1 npcRef（指向任一既有 standalone follower ActorRef 當 clone 替身）+ 1 mapMarker + 1 hazard + 1 identity=blacksmith → 定案 schema。
2. **M1 ModForge 側**：實作 `SceneImport` + `IdentitySpec`(blacksmith) → `build` M0 的 scene.json → patch esp。**離線可驗**（`Category!=RequiresSkyrim`：斷言生成的 records = 房子 REFR + NPC ref + XMRK + HAZD + 鐵匠 dialogue INFO + package + vendor）。**行為不變測**：不帶 scene.json 的既有 spec 生成位元不變。
3. **M2 實機（主力機）**：載入 M1 的 patch → 房子在、marker 可快旅、特效可見、鐵匠有問候/服務對話。**此步不需採集橋/controller/PROTEUS**——用手寫 scene.json + 既有 follower ActorRef 替身，先證 ModForge 側管線通。
4. **M3 controller**：接 settlements P2 的 placement-controller → 遊戲內喝瓶擺 1 棟房子。
5. **M4 採集橋 spike**：最小 DLL 走訪 cell 吐 placements → 餵回 M1 → 閉環。
6. **M5 PROTEUS 拓印**：遊戲內 clone 玩家 → 採集橋記 ActorRef+身份 → build → 鐵匠是玩家本人的臉。

→ **M0–M2 純 ModForge，可立刻動工且離線可測**；M3–M6 依賴 runtime 元件，逐步接。**先做 M0–M2**（本 spec 的可立即落地部分）。

## 測試策略

- **離線單元（`Category!=RequiresSkyrim`）**：
  - scene.json round-trip：手造 scene.json → `SceneImport` → 斷言填進 `Spec` 的 list 內容正確。
  - identity macro：blacksmith archetype → 斷言展開出 dialogue INFO（GetIsID condition）+ sandbox package + vendor faction。
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
