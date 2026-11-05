# 遊戲內場景匯出 — 目標、範圍、ModForge 落點、歷史切片、測試與開放項

← [ingame-scene-export-design](../ingame-scene-export-design.md)（入口）

## 目標 / 成功判準

> ⚠️ **歷史切片（已被 2026-07-12 的場景／captures 分流與 `sc capp` 取代）**：以下 PROTEUS clone + role 路線保留作決策脈絡，不是現行待做項；現況見「場景匯出不含 NPC」（[captures](captures.md)）與「`sc capp` 直接吸玩家」。

北極星最小切片（**歷史方案**；在遊戲內做、ModForge build 出）：

1. **擺**：喝一瓶「Plans: 木屋」→ 定位模式 → 房子落地（placement-controller `.pex`，照 Tundra）。
2. **拓印**：用 PROTEUS 把當前玩家 clone 成一個站在原地的獨立 NPC（外貌/裝備/perk 由 PROTEUS 現成搞定），採集橋記下他的**穩定 ActorRef** + 玩家標的 **role=blacksmith**。
3. **標註**：村口放 1 個地圖 marker、廣場放 1 個特效錨點（採集橋記成語意標記）。
4. **匯出**：施法**快照整片區域** → 採集橋吐一份 **scene JSON**。
5. **生成**：`dotnet run -- build scene.json` → patch esp：房子在該 cell、鐵匠站著且**講 ModForge 生成的鐵匠對話**、marker 上地圖可快旅、特效在廣場。

**成功判準**：進遊戲載入該 patch → 城鎮就位、鐵匠有問候/服務對話、marker 可快旅、特效可見。**行為不變保證**：不帶場景採集內容的既有 spec 生成結果**位元不變**（scene JSON 只是既有 `placements[]`/`npcs[]`/`mapMarkers[]`/`hazards[]` 的一種來源，不改既有路徑）。

## 範圍邊界（YAGNI）

| 納入 MVP | 排除（後排） |
|---|---|
| 採集橋 → scene JSON 契約定義（本 spec 的核心產物）| 採集橋 DLL 內部實作（子專案，本 spec 只定 output 契約）|
| scene JSON → 既有 `placements[]`/NPC-ref/marker/hazard/keyword 的映射 | placement-controller `.pex` 內部（與 [settlements P2](../../roadmap/mod-survey-gaps/settlements-phase2.md) 合流，另 design）|
| §D role tag → ModForge 灌對話/行為的 macro（1 個 role：blacksmith）| 多 archetype 全集、AI 生成對話文本（接 #17，後排）|
| PROTEUS clone 的**引用**（路徑 A，clone 穩定已拍板）| 路徑 B（ModForge 自建 facegen 生成獨立 NPC）——未來「可散布」才做 |
| 單 cell / 小片區域快照 | 整片 worldspace + 即時 navmesh 採集（硬項，後排）|
| import 既有 record 型別（ModForge 已能生者）| 新記錄型別（無——切片內不需要）|

---

## ModForge 落點（generable-today / net-new，grep `src/ModForge.Core/` 驗證）

| 環節 | 狀態 | 說明 |
|---|---|---|
| `placements[]`（含 transform/enable/scale/ownership）| ✅ **已具備** | `PlacementSpec` 欄位全齊，零改動 |
| `npcRefs[]`（引用 PROTEUS clone ActorRef）| ✅ **已具備** | PlacementSpec base = 外部 `.esp:0xFORMID`（跨 master 引用熟路）|
| `mapMarkers[]` / `hazards[]` / `tags[]` / `cell` override | ✅ **已具備** | MapMarkerSpec/HazardSpec/LightSpec/keyword/CellSpec override |
| **`npcRoles[]` 角色 macro** | ✅ **已落地** | `SceneNpcRoleSpec` + `ExpandNpcRoles`；blacksmith 展開 greeting/package，並在有 companion placement 時生成 vendor chest/FACT/trade topic/factions；已有測試 |
| **`removals[]` 移除既有 vanilla ref**（橡皮擦法術）| ✅ **已落地 2026-07-08** | `BuildRemovals`：master link cache `TryResolveContext<IPlaced>` → `GetOrAddAsOverride` → InitiallyDisabled + 深埋 Z−30000。RequiresSkyrim |
| §E 滴管/範圍吸取（base+rot+scale→placements）| ✅ **已具備** | 進既有 `placements[]`（Position/Rotation/Scale 全有）；外部 ref 自動加 master |
| scene.json 讀取 / 併入 spec | ❌ **已取消（不需要）** | scene.json 本身就是合法 ModSpec；CLI `ReadSpec` 直接 deserialize。`SceneImport.cs` 從未建立；若要把 scene 與 captures 合併，目前人工合併 JSON |
| ① placement-controller `.pex` | ♻️ **已被取代** | 現行 runtime 路徑是 scene-capture-bridge 的 C++ `Palette`／`Preview`（place action → `Preview::Commit` 或 `Palette::PlaceSelected`），repo 沒有 Papyrus controller |
| ③ 採集橋 SKSE DLL | ✅ **核心已落地（獨立子專案）** | 現行 scene-capture-bridge 以 C++ 實作採集、Palette／Preview 與輸出契約 |
| ② PROTEUS facegen | ♻️ **歷史路線** | 已被 `sc capp` 直接讀玩家 base TESNPC 取代 |

**現況一句話**：`npcRoles[]` 角色 macro 已落地；`SceneImport` 已取消，CLI 直接 deserialize scene.json；Papyrus controller 與 PROTEUS 路線都已被 C++ Palette/Preview 與 `sc capp` 取代。

---

## 歷史最小垂直切片（驗證管線；非現行待辦）

**里程碑序（每步可獨立驗）**：

1. **M0 契約凍結（已完成）**：手寫一份 scene.json（不經採集橋）含 1 house placement + 1 npcRef（指向任一既有 standalone follower ActorRef 當 clone 替身）+ 1 mapMarker + 1 hazard + 1 npcRole=blacksmith → 定案 schema。
2. **M1 ModForge 側（已完成，但 `SceneImport` 取消）**：`SceneNpcRoleSpec`(blacksmith) 已落地；scene.json 本身就是 ModSpec，由 CLI 直接 deserialize 後 build，不存在 `SceneImport.cs`。
3. **M2 實機（主力機）**：載入 M1 的 patch → 房子在、marker 可快旅、特效可見、鐵匠有問候/服務對話。**此步不需採集橋/controller/PROTEUS**——用手寫 scene.json + 既有 follower ActorRef 替身，先證 ModForge 側管線通。
4. **M3 controller（架構已更換）**：不做 placement-controller `.pex`；現行由 C++ `Palette`／`Preview` 負責擺放。
5. **M4 採集橋 spike（已完成）**：DLL 走訪 cell 吐 placements → CLI 直接讀 scene.json → 閉環。
6. **M5 PROTEUS 拓印（路線已取消）**：現行 `sc capp` 直接擷取玩家，不經 PROTEUS clone。

→ 此里程碑序只保留歷史脈絡：M0–M2 與採集橋核心已落地，`SceneImport`／Papyrus controller／PROTEUS clone 三項則分別取消或被取代，不應再依本節重派。

## 測試策略

- **離線單元（`Category!=RequiresSkyrim`）**：
  - scene.json 直接讀取：scene.json 作為 ModSpec 由 CLI `ReadSpec` deserialize；沒有 `SceneImport` round-trip。
  - role macro：blacksmith role → 斷言展開出 dialogue INFO（GetIsID condition）+ sandbox package + vendor faction。
  - **行為不變**：既有 spec 的生成輸出不因 scene.json 直接讀取路徑而改變。
  - 座標映射：interior local vs exterior world 兩路各一 placement，斷言落在對的 cell。
- **實機（主力機，`RequiresSkyrim` / WAIT_USER）**：M2 起的城鎮就位 + 對話 + marker + 特效；M5 的玩家臉拓印。

## 開放 / 後續（非本 MVP）

- **採集橋 DLL 內部設計**：SKSE cell 走訪 API、ImGui 面板（SKSE Menu Framework 3）、語意標記的遊戲內下標 UX → 獨立子專案 spec。
- **placement-controller（歷史項，已被取代）**：原擬與 settlements P2 合流；現行擺放由 scene-capture-bridge 的 C++ Palette／Preview 負責。
- **archetype 全集**：blacksmith 之外的守衛/商人/冒險者…（接 #23 框架）；對話文本 AI 生成（接 #17）。
- **路徑 B（ModForge 自建 facegen）**：讓產物不依賴玩家端 PROTEUS（可散布獨立 NPC）——接 asset-pipelines headless facegen 研究。
- **即時 navmesh 採集**：能完美尋路的城鎮（硬項）；MVP 先出可造訪版。
- **整片 worldspace 快照**：超出單 cell 的大範圍匯出。
</content>
