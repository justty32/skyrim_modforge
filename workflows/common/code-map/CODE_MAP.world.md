# CODE_MAP — 世界・放置・地區・等級列表・遭遇區域

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：interior cells、exterior placements、worldspaces、regions、leveled lists、containers、recipes、encounter zones、vendors。

## Examples

| 檔案 | 對應功能 |
|-----|---------|
| `examples/place_spec.json` | NPC / object 放置（室內）|
| `examples/interior_spec.json` | 新建 interior cell |
| `examples/newcell_spec.json` | cell + placement 完整範例 |
| `examples/teleport_doors_spec.json` | teleport door linked-ref pair |
| `examples/encounter_spec.json` | encounter zone + leveled spawns |
| `examples/recipe_spec.json` | COBJ crafting recipe |
| `examples/smithing_spec.json` | 鍛造台 recipe |
| `examples/vendor_spec.json` | 商人 faction + merchant chest |
| `examples/worldspace_spec.json` | 自訂 worldspace |
| `examples/worldspace_navmesh_test_spec.json` | worldspace + navmesh |
| `examples/lights.json` | 自訂 Light（LIGT）：color/radius/flicker + 放置進 cell |
| `examples/showcase-multi.json` | 多功能 showcase（Light + scene headtrack + SitTarget，一包一次測）|
| `examples/lighting.json` | 明亮室內：自訂 LGTM + IMGS + CELL 逐欄光照（含 DALC）|
| `examples/weather_bright.json` | 室外天氣 IMGS 調色：自訂 IMGS + Weather `imageSpaces.default` 填全 ToD |
| `examples/skill_tree_spec.json` | **in-world 技能樹 generator**（`skillTrees:` 高階 macro，**這是正規用法**）IN-GAME CONFIRMED |
| `examples/inworld_skill_tree_standalone_spec.json` | 同結果**手刻低階版**（直接寫 activators/placements/scripts；Phase 1 實機驗證範本，可對照 generator 展開出什麼）|
| `examples/settlement_spec.json` | **聚落人口 generator**（`settlements:` 高階 macro）：2 住民（鐵匠+vendor / 廚子+作息覆寫）+ 錨點作息 + 自動 faction |
| `examples/inworld_skill_tree_spec.json` | Campfire-radial-menu 路線設計範本（**不交付、需裝 Campfire**；留作未來 radial 版參考）|
| `examples/assets/skilltree/` | 技能樹美術 kit：Campfire 星/線 nif（loose）+ 9 個 vanilla 貼圖；spec `assets` 帶上 |

---

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `EncounterTests.cs` | encounter zone build + validate |
| `RecipeTests.cs` | COBJ workbench dispatch + component 接線 |
| `TeleportDoorTests.cs` | teleport-door XTEL 接線（linked-ref pair build）|
| `TeleportValidateTests.cs` | teleport pair validate（ref 存在、配對完整性）|
| `VendorTests.cs` | vendor faction config + merchant container build |
| `WorldspaceRegionTests.cs` | worldspace record + region polygon/weather build |
| `WorldspaceBaseTextureTests.cs` | `worldspace.baseTexture`（LTEX）→ 每格 LAND 四象限 BTXT base 層（quadrant 全覆蓋、**LayerNumber 0xFFFF**＝vanilla base 標記、texture FormID）；omit = 無紋理 |
| `WorldspaceSplatmapTests.cs` | `worldspace.textureLayers`（多紋理混合）：`Vtxt.BuildLayers` 純函式（quadrant 切分、position=localRow×17+localCol、稀疏、opacity clamp、共用中央頂點）+ 端到端 PNG splatmap（每格四象限 ATXT/VTXT 層、**ATXT 0-indexed**、cell 落在圖外不生層）|
| `HeightmapTests.cs` | PNG load、Y-flip、min/max 映射、33×33 seam 零誤差 |
| `WorldspaceHeightmapTests.cs` | PNG→cell grid 尺寸推導、VHGT delta 非零、flat PNG = flat cell path、**相鄰 cell 邊界重建高度完全一致（seam stitching）**、validate（min<max / empty path / ESL 不相容） |
| `VhgtTests.cs` | encode（全零 flat、round-trip ±4 units、過陡 clamp+warn）、**RequiresSkyrim：Tamriel 20 格 decode→encode delta bytes 完全一致（主力機驗演算法）** |
| `VnmlTests.cs` | Vnml.Compute：平地全朝上、均勻東坡 X<128、均勻北坡 Y<128、Z=255 flat、對角等比坡 X=Y |
| `GodotPlacementsTests.cs` | Godot placements JSON 座標換算（origin offset、Z 翻轉、m→units）、rotation rad→deg、scale passthrough、instanceId→editorId、error cases |
| `XMarkerTests.cs` | XMarker 放置（特殊 placement base）|
| `XMarkerKindTests.cs` | `kind:xmarker/xmarkerHeading` helper（空 base→0x3B/0x34 + persistent）+ `forced:` alias 解析到 xmarker 錨點 |
| `MapMarkerTests.cs` | mapMarker → MapMarker static + XMRK（type/flags）；持久 TopCell 加性帶上（⚠️ 需本機 Skyrim.esm）+ validate（type/flag）|
| `PlacementSpecFieldsTests.cs` | Scale(XSCL) / InitiallyDisabled(flag) / EnableParent(XESP) / Lock(XLOC) / Ownership(XOWN) / Count(XCNT) build + validate |
| `LightTests.cs` | 自訂 Light（LIGT）color/radius/fade/flags build + validate |
| `LightingTests.cs` | LGTM/IMGS build + CELL XCLL inherit + validate guardrails |
| `SkillTreeTests.cs` | `skillTrees:` macro-expansion（points/rank GLOB、node+line ACTI、垂直堆疊 placement、line 中點+rot+scale、gating 鏈 prereq/downLine、root 無 prereq、idempotent guard）+ build（temp refs、node 掛 MFSkillNode）+ validate（id 唯一/cell/name/ability 必填）|
| `SettlementTests.cs` | `settlements:` macro-expansion（ACHR spawn 座標/fallback、Sleep/Work/Wander package + wrap-midnight 時長 + 錨點、npc.Packages 排序、routine 覆寫、auto/explicit faction、crimeFaction、vendor FACT/chest/gold、friendlyResidents RELA、idempotent）+ build（**Sleep location 解析到 in-spec 床錨**的 deferred 修回歸測）+ validate（npc 非 in-spec、缺 spawn、sleep 無 home、未知錨、重複住民、vendor 時數、缺 cell/residents）|

---

---

## Interior Cells 室內空間
→ **說明文件**：[SPEC-world.md § cells & placements](../../../docs/spec/SPEC-world.md#cells--placements--putting-things-in-the-world)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `CellSpec`（name / template / encounterZone / **`music`→MUSC**）|
| Build P1 | `Generator.Build.Cells.cs` | 建 interior cell record + encounter zone 連結（`cells[].music` 由 pass-2 `WireCellMusic` 接到 cell.Music，見 `CODE_MAP.items-magic.md` Music）|
| Validate | `Generator.Validate.World.cs` | cell ref、encounter zone ref |

---

## Placements 放置（NPC / Object）
→ **說明文件**：[SPEC-world.md § cells & placements](../../../docs/spec/SPEC-world.md#cells--placements--putting-things-in-the-world) · [engine-internals.md § Cell GRUP](../../../docs/engine-internals.md#cell-grup-placement-is-keyed-by-formidgrid)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `PlacementSpec`（含 `kind:"xmarker"/"xmarkerHeading"` helper；**Scale/InitiallyDisabled/EnableParent(`EnableParentSpec`)/Lock(`LockSpec`)/Ownership(`OwnershipSpec`)/Count**）, `LinkedRefSpec`, `Vec3` |
| Spec | `Spec.MapMarkers.cs` | `MapMarkerSpec`（editorId/name/worldspace/position/`type`(MarkerType)/`flags`(Visible/CanTravelTo/ShowAllIsHidden)）|
| Build P2 | `Generator.Build.Placements.cs` | 室內/室外/vanilla-override 放置，position/rotation，persistent flag，cell 錨定；**`kind:xmarker/xmarkerHeading` → 空 base 自動填 `Skyrim.esm:0x3B`/`0x34` STAT + 強制 persistent**（quest-target 錨點）；**base 是 in-spec HAZD（或 `kind:"hazard"`）→ 建 `PlacedHazard`（`.Hazard` 而非 `.Base`）**；**Scale(XSCL) / InitiallyDisabled(0x800) / EnableParent(XESP) / Lock(XLOC) / Ownership(XOWN) / Count(ItemCount XCNT)**；`ParseLockLevel` helper 在 `Generator.Helpers.cs` |
| Build P2 | `Generator.Build.MapMarkers.cs` | **`BuildMapMarkers`**：每筆 → MapMarker static（`0x10`）上的 `PlacedObject` + XMRK `MapMarker`(Name/Type/Flags)，放進 worldspace **持久 TopCell**（`WorldspacePersistentCell`），registered 進 formKeyByEd 故可被 `forced:` alias 抓 |
| Build P2 | `Generator.Build.PlacementRefs.cs` | linked-ref 對 + teleport-door XTEL 接線（deferred）|
| Build P2 | `Generator.Build.ExteriorCells.cs` | 室外 worldspace cell group tree（block/sub-block 按 grid 坐標）；**`WorldspaceOverride` 加性帶上 master 持久 TopCell（`CopyCellEnv`、不重述 vanilla ref）否則 vanilla 地圖標記全消失+大地圖空白**；**`WorldspacePersistentCell`** 回 worldspace 持久 cell 給地圖標記 |
| Validate | `Generator.Validate.World.cs` | linked-ref target、teleport pairs、worldspace boundary |
| Diag | `Diagnostics.Dump.World.cs` | placements / cells / linked-refs / navmesh dump |

---

## In-world Skill Trees 技能樹（Idea #20）
→ **說明文件**：[SPEC-world.md § in-world skill trees](../../../docs/spec/SPEC-world.md#in-world-skill-trees-skilltrees) · memory `inworld-skill-tree-standalone-confirmed`

零外部依賴的可點 in-world 養成樹。**macro-expansion** 架構：在 `Build()` pass-0 把高階 `skillTrees:` 展開成既有低階記錄（globals / node+line ACTI / placements / `scripts:` 的 MFSkillNode 掛載），重用所有既有 pass。IN-GAME CONFIRMED 2026-06-21（手刻版）。MVP = 垂直線性鏈。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.SkillTree.cs` | `SkillTreeSpec`（editorId/name/cell/origin/spacing/pointsGlobal/startingPoints/nodeModel/lineModel）+ `SkillNodeSpec`（editorId/name/ability）|
| Spec | `Spec.cs` | `ModSpec.SkillTrees` + `SkillTreesExpanded` 守衛旗標 |
| Expand P0 | `Generator.SkillTrees.cs` | **`ExpandSkillTrees`**：每 tree → points GLOB + 每 node 的 rank GLOB / star ACTI / placement（垂直堆疊 origin+i*spacing）/ MFSkillNode script-attach（ability/rank/points/name + i>0 的 prereq+downLine）；每邊 → line ACTI + placement（中點、rot 90/0/180、scale=spacing/65）。常數 `SkillNodeScript`/`DefaultSkillNodeModel`/`DefaultSkillLineModel` |
| Build | `Generator.Build.cs` | pass-0 呼叫 `ExpandSkillTrees(spec)`（在 `new BuildContext` 前）|
| Validate | `Generator.Validate.SkillTrees.cs` | `ValidateSkillTrees`：tree/node editorId 唯一、cell/name/ability 必填、ability CheckRef、spacing>0 |
| Papyrus | `assets/papyrus/MFSkillNode.psc` | 節點行為（extends ObjectReference）：OnActivate gate（prereq rank + points）→ AddSpell + PlayAnimation("OwnedWild") + downLine "Unlock" + 扣點；OnLoad 重播亮起（持久）。嵌入 CLI、`package` 在有 skillTree 時 `ShipEmbeddedPex("MFSkillNode.pex")` |
| Art | `examples/assets/skilltree/` | Campfire 星/線 nif（loose 打包，非 master）+ 9 個 vanilla 貼圖；作者經 spec `assets` 帶上 |

---

## Populated Settlements 聚落人口（Idea #22）
→ **說明文件**：[SPEC-world.md § populated settlements](../../../docs/spec/SPEC-world.md#populated-settlements-settlements) · 設計 [settlement-population-design](../../specs/archive/README.md)

把「住滿活人的聚落」一鍵展開成既有低階記錄的 **macro**（同 skillTrees 架構，`Build()` pass-0）。MVP = 具名住民 + 靜態 ACHR + 綁錨點作息 + 可選 vendor + faction 三件套；**零新 record 型別、零 runtime 腳本**，純資料展開、離線完全可驗。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Settlement.cs` | `SettlementSpec`（editorId/cell/settlementFaction/crimeFaction/friendlyResidents/dailyRoutine/residents）+ `ResidentSpec`（npc/home/work/spawnAt/spawnPosition/vendor/routine）+ `RoutineSpec`（sleep/work）+ `RoutineWindowSpec`（from/to）+ `SettlementVendorSpec`（sellBuyList/notSellBuyList/startHour/endHour/gold）|
| Spec | `Spec.cs` | `ModSpec.Settlements` + `SettlementsExpanded` 守衛旗標 |
| Expand P0 | `Generator.Settlements.cs` | **`ExpandSettlements`**：每聚落 → settlement FACT（空則自建）；每住民 → ACHR placement（spawn marker 座標 / spawnPosition fallback）+ Sleep/Work/Wander package（綁 home/work/spawnAt 錨點，schedule 帶 wrap-midnight 時長）+ npc.Packages（排程 by hour、wander 最後）+ faction 三件套 + 可選 vendor FACT/chest（gold）/JobMerchantFaction；`friendlyResidents` → 兩兩 Friend RELA。常數 `SandboxTemplateRef`/`SleepTemplateRef`/`GoldRef`/`JobMerchantFactionRef` |
| Build | `Generator.Build.cs` | pass-0 呼叫 `ExpandSettlements(spec)`（在 `ExpandSkillTrees` 後、`new BuildContext` 前）|
| Validate | `Generator.Validate.Settlements.cs` | `ValidateSettlements`：settlement/resident id 唯一、cell 必填、residents 非空、npc 必為 in-spec npcs[]、需 spawnAt 或 spawnPosition、sleep window 需 home 錨、home/work/spawnAt 須為 placement/external、vendor 時數 0..24/gold≥0、routine 時數合法 |
| **錨點解析修** | `Generator.Build.Packages.cs` | `ApplySandboxData`/`ApplySleepData` 的 location slot 改 **deferred**（加進 `deferredLocationWires`，仿 Travel/Escort）——原本 eager 解析時 placement 還沒註冊 → in-spec 錨點一律 fallback NearSelf；現在 placement loop 後才解析，in-spec marker/bed 錨點正確解析（且自動被強制 persistent）|

---

## Living-world NPCs 活世界 NPC（Idea #23）
→ **說明文件**：[SPEC-world.md § living-world NPCs](../../../docs/spec/SPEC-world.md#living-world-npcs-livingnpcs) · 設計 [sub_projs/living-adventurers/](../../../sub_projs/living-adventurers/README.md)（idea #23 + design.md）

一小撮**具名持久 NPC 過自己的離場人生**（抽象幽靈模擬 + 就地實體化）的 **macro**（同 settlements pass-0），但**會掛 runtime 腳本**（兩個可重用 .pex）。每多一個既有 archetype 的 NPC = 一個 entry（純資料）。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Controller .pex | `assets/papyrus/MFLivingWorldController.psc` | host quest，單一 roster 迴圈：`OnUpdateGameTime` 推進每個 alias 的抽象 sim、`OnUpdate` 跑每個 alias 的在場 poll（chained，不隨 N 線性膨脹）。props `SimIntervalHours`/`PollInterval`/`AliasCount` |
| Alias .pex | `assets/papyrus/MFLivingNpcAlias.psc` | per-NPC（extends ReferenceAlias）：`AdvanceSim`（deed global +1 + 輪替 anchorIdx）、`Presence`（玩家同 cell → `MoveTo` 進場 + `EvaluatePackage`；離開 → 回 HoldMarker）。props `Archetype`(int 分支)/`HoldMarker`/`Anchors`(FLST)/`DeedCount` |
| Spec | `Spec.LivingNpc.cs` | `LivingNpcsSpec`（simIntervalHours/pollInterval/rumorSpeaker/npcs）+ `LivingNpcSpec`（ref/name/archetype/alignment/backstory/anchors/rumors/**interactions**）+ `LivingAnchorSpec`（cell/position/kind）|
| Spec | `Spec.cs` | `ModSpec.LivingNpcs`（nullable）+ `LivingNpcsExpanded` 守衛旗標 |
| Expand P0 | `Generator.LivingNpcs.cs` | **`ExpandLivingNpcs`**：共享 hold marker + sandbox package + host controller quest（掛 world-controller script）；每 NPC → deed GLOB + 每 anchor 一個 xmarker + Anchors FLST + alias（in-spec=place ACHR+`forced:`／external=`uniqueActor:`，掛 alias script + Archetype/HoldMarker/Anchors/DeedCount props，in-spec 補 sandbox package）+ 可選 rumor dialogue（gated on deed GLOB）。**P3**：`interactions` → per-NPC favor GLOB + 互動 dialogue（`setGlobal` favor，`LivingInteraction` fund/praise/parley copy，praise gate deed）；`alignment=hostile` 的 in-spec NPC 設 `Aggression=Aggressive`。`LivingArchetypeCode` 名→int |
| Build | `Generator.Build.cs` | pass-0 呼叫 `ExpandLivingNpcs(spec)`（在 `ExpandSettlements` 後）|
| Validate | `Generator.Validate.LivingNpcs.cs` | `ValidateLivingNpcs`：simInterval/poll>0、rumorSpeaker 須 in-spec/external、ref 必填且唯一、in-spec ref 須為 npcs[]、archetype 須已知、anchors≥1、anchor 須有 cell、有 rumors 無 speaker 警告、**alignment/interaction 種類須已知、external+hostile 警告** |
| Ship-gate | `Package.cs` + `ModForge.Cli.csproj` | `spec.LivingNpcs.Npcs.Count>0` → ship 兩個 .pex（EmbeddedResource，Exists 條件）|
| Example | `examples/living_npcs_spec.json` | Kjeld(adventurer，2 旅館輪替) + Falas(mageApprentice) + Bjorn rumor |
| Tests | `LivingNpcTests.cs` | macro 展開（controller quest/alias/archetype int/object props 解析/deed GLOB/anchor marker/FLST/persistent ref/external uniqueActor/rumor gated/no-speaker drop）+ validate |
| **core 前置修** | `Generator.Build.Scripts.cs` + `Generator.Build.Placements.cs` | ① alias scriptProperties object prop 改 deferred 解析（`deferredScriptObjectProps` + `WireDeferredScriptObjectProps`）→ 可指向 later-built placement；② `deferredForcedAliases.Ref` 併入 `deferredAnchorEds` → forced-alias ACHR 自動 persistent。回歸測 `AliasScriptObjectPropTests` |

---

## 遊戲內場景匯出 · NPC 角色 macro（Idea #24 §D）
→ **設計**：[specs/ingame-scene-export-design.md](../../specs/ingame-scene-export-design.md) · plan [plans/ingame-scene-export.md](../../plans/ingame-scene-export.md) · idea [tools/24-ingame-editor.md](../../idea/tools/24-ingame-editor.md)

給一個**外部 captured NPC**（PROTEUS clone / follower base）一個職業 `role`，pass-0 macro 展開成該 NPC 的 conditioned 問候 + 行為。**非玩家向 `IdentitySpec`**（那是玩家加入 FACT gate 玩家對話）；與 `ResidentSpec` 差別＝keyed on **外部 base NPC ref** 且自帶對話。切片只做 `blacksmith`。scene.json ＝一份 `ModSpec` 片段（`placements`/`mapMarkers`/`hazards`/`npcRoles`），生成端 placements/marker/hazard **全已具備**。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.SceneExport.cs` | `SceneNpcRoleSpec`（npc=外部 base ref／role／backstory）|
| Spec | `Spec.Annotations.cs` | `AnnotationSpec`（seq/label/kind/**note**/position/angleZ/cell/worldspace）——遊戲內編輯器 marker 錨點（Idea #24 P1），**advisory only**：build 永不生記錄，僅 `Program.Build.cs` log 數量；agent 讀它 author 下一輪 spec；note＝自由文字補充指示（DLL 的 E 編輯視窗填，2026-07-11） |
| Spec | `Spec.cs` | `ModSpec.NpcRoles` + `NpcRolesExpanded` 守衛旗標；`ModSpec.References`（referrer，見下 References 列）|
| Expand P0 | `Generator.SceneNpcRoles.cs` | **`ExpandNpcRoles`**：每 role → 共享 StartGameEnabled host quest `MF_SceneNpcRolesQ`；blacksmith → Hello `DialogueSpec`（GetIsID npc）+ sandbox `PackageSpec`（無 location＝editor-location fallback）+ `NpcPatchSpec`（overrideOf npc, append）。**vendor**：有 companion placement（base==npc，`RefsMatch`）時 → Vendor FACT（`VendorItemsBlacksmith` 0x066333/8-20 時）+ merchant chest（gold）共置 placement + `patch.Factions` 加 vendor FACT + `JobMerchantFaction`（vanilla 交易對話）；無 placement 跳過 vendor。未知 role 不展開（Validate 警告）。`SanitizeEd` 把外部 ref 轉成合法 editorId 片段。常數 `BlacksmithGold`/`BlacksmithVendorList` |
| Build | `Generator.Build.cs` | pass-0 呼叫 `ExpandNpcRoles(spec)`（在 `ExpandLivingNpcs` 後）|
| Removals P1 | `Generator.Build.Removals.cs` | **`BuildRemovals`**（Idea #24 §E 橡皮擦）：`removals[]` 每個 `<master>:0xFORMID` → `MasterCache.TryResolveContext<IPlaced>` → `GetOrAddAsOverride(mod)`（自動 override parent cell/worldspace）→ `MajorRecordFlagsRaw\|=0x800`(InitiallyDisabled) + 深埋 Z−30000。Build.cs 在 BuildMapMarkers 後呼叫。RequiresSkyrim（要 master link cache）|
| Overrides | `Spec.Overrides.cs` + `Generator.Build.Overrides.cs` | **`OverrideSpec`**（ref/position/rotation°/scale?）＋**`BuildOverrides`**（Idea #24 numpad 編輯器）：頂層 `overrides[]`（拍板 B 案，理由在 spec）——同 removals 解析機件 → `GetOrAddAsOverride` → 重蓋 `Placement`（度→弧度）；`scale` null=不碰、1.0=清 XSCL、其他=寫。Build.cs 在 **BuildRemovals 前**呼叫（撞名時 removal 後蓋、贏）。RequiresSkyrim |
| References | `Spec.References.cs` + `Generator.Build.References.cs` | **`ReferenceSpec`**（ref/label/base?/position?/rotation°?/scale?/cell?/worldspace?/anchor?/note?）＋**`BuildReferences`**（Idea #24 referrer `sc ref`）：頂層 `references[]` ＝**命名一個既有 placed ref**（removals=擦、overrides=移、references=**命名**，三兄弟）。**`label` 註冊進 `formKeyByEd`** → spec 任何 ref 欄位（package sandbox.location/travel.place、alias `forced:`、linkedRefs、enableParent、objective target、script prop）都能寫這個 label，消費站點零改動。**(乙) 檔內路徑**：`ref`＝同檔 placements[] editorId → `BuildPlacements` 的 anchor 集合強制它 persistent ＋ `BuildReferences` 補 0x400 → **乾淨、離線可測**。**(甲) 外部路徑**：`ref`＝`<master>:0xFORMID` → 查 master link cache 的 0x400 flag，temporary 就**警告**；`anchor:"marker"` 生 persistent XMarkerHeading、`anchor:"replace"` 生自家 persistent 複製品＋把原件推進 `referenceRemovals`（`BuildRemovals` 一併 disable+bury）。空 list ＝不生任何記錄（行為不變）。Build.cs 在 **BuildMapMarkers 後、BuildOverrides 前**呼叫（必在 placements 後、所有 wire 前）|
| Validate | `Generator.Validate.References.cs` | `RegisterReferenceLabels`（`Validate()` 開頭呼叫，把 label 加進 `Ids` → 其他 CheckRef 認得）＋`ValidateReferences`：ref/label 必填、label 唯一且不可長得像外部 ref、非外部 ref 必須是 placements[] editorId、anchor∈{none,marker,replace}、anchor 不可用在檔內 ref、anchor 需 cell/worldspace、`replace` 需 base、cell/worldspace 互斥、scale>0 |
| **core 前置修** | `Generator.Build.Dialogue.cs` | **外部 speaker Hello 支援**：conditioned-hello 建構 + `AddSpeakerGate` 兩處，in-spec `npcsByEd` 失敗時 fallback `TryResolveRef` 解析 `<plugin>:0xID` → GetIsID FormKey；`MakeHello` 改吃 `FormKey`（非 `INpcGetter`）並在 hello 材質化迴圈解析外部 speaker（否則外部 NPC 的 Hello topic 永不生成）。回歸測全綠 |
| Validate | `Generator.Validate.SceneNpcRoles.cs` | `ValidateNpcRoles`：npc 必填、role 必填且須已知（`KnownRoles`={blacksmith}）；`ValidateRemovals`：removals 須 well-formed 外部 ref；`ValidateOverrides`：同 ref 形狀規則＋與 removals 撞名＝矛盾（警告，build 讓 removal 贏）|
| Example | `examples/scene-export-blacksmith.scene.json` | M0 契約 fixture：farmhouse(0x00084A)+Carlotta(0x013B99 as 替身)+town marker+campfire hazard+blacksmith role |
| Example | `examples/scene-references.json` | referrer 端到端 fixture（乙路徑）：椅子 placement(CommonChair02 0x0B9C04) → `references[].label="sofia's chair"` → Sofia 的 sandbox package `location`＝該 label（radius 128/allowSitting）；再帶一筆甲路徑（Skulvar 的鋤頭 0x0D1991）示範 temporary 警告 |
| Tests | `SceneNpcRolesTests.cs` | validate（缺 npc／未知 role）+ expand（host quest/greeting/package/patch、idempotent、未知 role emit 空、無 npcRoles no-op）+ build（Hello INFO GetIsID gate 到外部 NPC FormKey）|
| Tests | `ReferencesTests.cs` | 乙路徑（強制 persistent 0x400、label 解析成 package Location target、不多生記錄、無 references[] 時椅子仍是 temporary＝行為不變）+ validate 9 條 + 甲路徑（無 link cache 仍綁 label；RequiresSkyrim：temporary 警告、`anchor:"marker"` 生 XMarkerHeading、`anchor:"replace"` 生自家複製品且原件 disable+深埋）|

---

## Lights 自訂光源（LIGT）
→ **說明文件**：[SPEC-world.md § lights](../../../docs/spec/SPEC-world.md#lights--custom-light-sources-ligt)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Lights.cs` | `LightSpec`（editorId/name/color/radius/fadeValue/falloff/fov/flags/value/weight）|
| Build P1 | `Generator.Build.Lights.cs` | `BuildLights`（`mod.Lights.AddNew`；color R/G/B、radius/fade、`Light.Flag` 解析；BuildFormKeyTable 前建，故 placement 可按 editorId 放置）|
| Validate | `Generator.Validate.Lights.cs` | `ValidateLights`（flag 名合法、color 0..255、radius>0、editorId 唯一）|
| Diag | `Diagnostics.Records.cs` | `lightdiag`（radius/color/fade/flags dump）|

自訂 Light 是一般 base record——用既有 `placements[]`（base=light editorId）放進任意 cell，無需動 placement 程式碼。

---

## Lighting Templates + ImageSpaces 室內光照（LGTM / IMGS / CELL XCLL）
→ **說明文件**：[SPEC-world.md § lighting](../../../docs/spec/SPEC-world.md#lighting)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Lighting.cs` | `LightingTemplateSpec`(LGTM) / `ImageSpaceSpec`(IMGS) / `CellLightingSpec`(inline XCLL) / `AmbientColorsSpec`(DALC) |
| Spec | `Spec.World.cs` | `CellSpec.LightingTemplate/ImageSpace/Lighting` |
| Build P1 | `Generator.Build.Lighting.cs` | `BuildLightingTemplates` + `BuildImageSpaces`（模板抄+覆寫；DALC LGTM→DirectionalAmbientColors、XCLL→AmbientColors；BuildCells 前建，lgtmByEd/imgsByEd 供 cell 解析）|
| Build P1 | `Generator.Build.Cells.cs` | cell 掛 LGTM/IMGS link（`ResolveLightingRef`）+ `ApplyCellLighting`（inline XCLL + inherit flags；無 inline 且有 template → 全繼承）|
| Validate | `Generator.Validate.Lighting.cs` | color 0..255、template/cell-ref 可解（cross-type）、inherit flag 名合法 |
| Diag | `Diagnostics.Records.cs` | `lgtmdiag` / `imgsdiag` |

> 註：`ImageSpaceSpec`(IMGS base) 與既有 `ImageSpaceModifierSpec`(IMAD, `Generator.Build.ImageSpace.cs`) 是兩個不同 record。

---

## Worldspaces + Regions
→ **說明文件**：[SPEC-worldspaces.md](../../../docs/spec/SPEC-worldspaces.md)（worldspace 細節已從 SPEC-world.md 移來此檔）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Worldspace.cs` | `WorldspaceSpec`（含 `Heightmap`/`GodotPlacements`/**`BaseTexture`**(LTEX ref)/**`TextureLayers`**(多紋理混合)）, `HeightmapSpec`, **`TerrainTextureLayerSpec`**(texture+splatmap), **`SplatmapSpec`**(path/originX/Y), `GodotPlacementsSpec`, `WorldspaceCellSpec`, `WorldMapDataSpec`, `RegionSpec`, `RegionWeatherEntrySpec`, `PointSpec` |
| Build P1 | `Generator.Build.Worldspace.cs` | 建 worldspace record（climate/water/map bounds）+ cell grid 骨架；**PNG heightmap 路徑**（`Heightmap.Load` → `Vhgt.Encode` per cell）；**單層地形貼圖 `baseTexture`**（resolve LTEX 一次 → `EmitCell` 每格四象限加 `BaseLayer`{`LayerHeader.Texture`/`Quadrant`}，**`LayerNumber=BaseLayerNumber`=0xFFFF**）；**多紋理 `textureLayers`**（每層 resolve LTEX + `Splatmap.Load`；`EmitCell` 每格 `TrySampleCell` → `Vtxt.BuildLayers` → 加 `AlphaLayer`，**LayerNumber=層序 0-indexed**）；**有紋理時 `LAND.Flags |= Layers`(0x04)**（否則引擎跳過紋理層＝無紋理，byte-verified vs vanilla）；**Godot placements 展開**（`GodotPlacements.Load` → `spec.Placements.AddRange`，在 `BuildPlacements` 前注入）|
| Build P1 | `Generator.Build.Regions.cs` | 建 region record（polygon / weather table / priority / map color）|
| Build P2 | `Generator.Build.ExteriorCells.cs` | cell group tree 生成（外層結構）|
| Build P2 | `Generator.Build.Navmesh.cs` | NAVM 4 頂點平面 quad + NAVI 索引（[engine-internals § navmesh](../../../docs/engine-internals.md#programmatic-navmesh-navm--navi--in-game-confirmed-2026-06-03)）|
| Util | `Heightmap.cs` | 16-bit grayscale PNG → 全域高度網格；`SampleCell` 切 33×33（相鄰格共用邊緣欄）；`SampleCellExtended` 切 35×35（+1px 邊框，供 VNML 中心差分）；Y-flip（影像頂=北）|
| Util | `Vhgt.cs` | VHGT 編解碼：絕對高度 → float offset + 33×33 signed-int8 delta（row-wise 累積，×8 game units）；`Decode` 接受 `IReadOnlyArray2d<byte>`（相容 Mutagen getter）|
| Util | `Vnml.cs` | VNML 法線計算：從 35×35 高度格（SampleCellExtended 輸出）以中心差分算切線，E×N cross product 得 Skyrim(X=東,Y=北,Z=上) 法線，encode `P3UInt8` **signed byte=round(n×127)**（無 +128 偏移；up=(0,0,127)）|
| Util | `Splatmap.cs` | 8-bit grayscale PNG → per-vertex alpha 網格（同 heightmap 網格約定，Y-flip）；`TrySampleCell(globalCellX,Y)` 切 33×33 alpha 0..1，cell 落在圖涵蓋範圍外回 false |
| Util | `Vtxt.cs` | ATXT+VTXT 純建構：33×33 alpha 格 + Quadrant → `AlphaLayer`{`LayerHeader`+稀疏`AlphaLayerData`(position/opacity)}；quadrant 切分（Bottom=南/低row、Left=西/低col）、position=localRow×17+localCol、0-indexed、空象限不生層。✅ LayerNumber/flags/texFormID 已對 vanilla byte-verify（2026-06-18 in-game OK）；⚠️ 僅 VTXT position 的 row/col 序待最終目視 |
| Seam | `Generator.Build.Worldspace.cs` | heightmap 迴圈內 **seam stitching**（VHGT）+ **VNML 重算**：每格 encode 後 decode 取 east/north edge 注入下格，`Vnml.Compute(SampleCellExtended)` 填法線（邊緣頂點帶 1px overlap 無需特判）|
| Util | `GodotPlacements.cs` | Godot `godot4_y_up` placements JSON → `List<PlacementSpec>`；座標換算（Z 翻轉、m→units）+ rotation rad→deg |
| Validate | `Generator.Validate.World.cs` | worldspace ref、boundary、climate ref |
| Validate | `Generator.Validate.World2.cs` | region / encounter zone / outfit content ref |
| Diag | `Diagnostics.Worldspace.cs` | worldspace climate/water/map / cell grid / region overlay |
| Diag | `Diagnostics.Landscape.cs` | `landdiag <plugin> [ws] [n]` — dump LAND 紋理層（BTXT/ATXT quad+LayerNumber+tex、VTXT pts、Flags）對 vanilla byte-verify（Mutagen `AlphaLayer:BaseLayer` 故判型先 IAlphaLayerGetter）；`find <plugin> 0xFORMID` 反查 FormID 型別（`Diagnostics.cs`）|
| Tool | `TexExport.cs` (CLI) | `texexport <dataDir> <outDir> <master:0xLTEX>[,…]` — LTEX→TextureSet→diffuse .dds 從遊戲 BSA（`Archives`）抽出 → `magick` 轉 PNG；餵 Godot worldspace-editor 的 WYSIWYG 地形 shader |
| Tool | `NifExport.cs` (CLI) | `nifexport <dataDir> <outDir> <master:0xFORMID>[,…]` — 可放置 base（STAT/TREE/MSTT…）的 `IModeledGetter.Model.File`→model .nif 從 mesh BSA 抽出；Godot 端再走 `nif2gltf`（[sub_projs/model-converter](../../../sub_projs/model-converter/README.md)）轉 glTF 當 WYSIWYG 物件代理 |
| Tool | `TexExport.cs` `TexPath` (CLI) | `texpath <dataDir> <outDir> <texPath>[,…]` — 抽任意貼圖路徑（NIF shader 引用的 .dds）→ `<basename>.png`；給物件 glTF 上 diffuse 貼圖（nif2gltf 寫 `<stem>.textures.json` sidecar 列要抽的 dds，model_fetch 據此呼叫）|

---

## Leveled Lists 等級列表 + FormList（FLST）
→ **說明文件**：[SPEC-world.md § leveled lists & containers](../../../docs/spec/SPEC-world.md#leveled-lists--containers) · [SPEC-world.md § formLists](../../../docs/spec/SPEC-world.md#formlists--flst)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `LeveledItemSpec`, `LeveledNpcSpec`, `FormListSpec`（editorId + items 任意 record ref）|
| Build P1 | `Generator.Build.Lists.cs` | 建 leveled-item / leveled-NPC list record（chance-none flags）；`BuildFormLists`（空 FLST，items pass 2）|
| Build P2 | `Generator.Build.Lists.Wire.cs` | entry FormLink 接線；`WireFormLists`（items → `FormLink<ISkyrimMajorRecordGetter>`，順序保留）|
| Validate | `Generator.Validate.Items.cs` | leveled list entries ref |
| Validate | `Generator.Validate.World.cs` / `.cs` | FLST editorId 唯一 + 每個 item ref 可解 |

FLST 用途：當吃 list 的 condition 的 param（`GetItemCount`/`GetEquipped`/`GetIsVoiceType`/`GetInWorldspace` 都收 FormList）、keyword/穿著清單、或任何要「一組 form」的地方。**`GetIsInList` 在 Mutagen 0.49 無對應 ConditionData——FLST 走既有 `*OrList` param，不是獨立函式。**

---

## Containers 容器
→ **說明文件**：[SPEC-world.md § leveled lists & containers](../../../docs/spec/SPEC-world.md#leveled-lists--containers)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `ContainerSpec`（items + counts）|
| Build P2 | `Generator.Build.Lists.Wire.cs` | container item FormLink 接線 |
| Validate | `Generator.Validate.Items2.cs` | container contents ref |

---

## Recipes 合成配方（COBJ）
→ **說明文件**：[SPEC-items.md § recipes](../../../docs/spec/SPEC-items.md#recipes-crafting--cobj)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `RecipeSpec`（workbench / components / result）|
| Build | `Generator.Recipes.cs` | COBJ 建立（workbench dispatch + component 接線）|
| Validate | `Generator.Validate.Items2.cs` | workbench keyword、component ref、output ref |
| Diag | `Diagnostics.Recipes.cs` | workbench / components / output dump |

---

## Encounter Zones 遭遇區域
→ **說明文件**：[SPEC-world.md § encounter zones](../../../docs/spec/SPEC-world.md#encounter-zones--leveled-actor-spawns--populating-an-area-with-scaled-enemies)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `EncounterZoneSpec`（level range / respawn / owner）|
| Build P1 | `Generator.Build.Lists.cs` | `BuildEncounterZones` 入口 |
| Build P2 | `Generator.Build.Lists.Wire.cs` | `WireEncounterZones` / `WireCellZones` |
| Validate | `Generator.Validate.World2.cs` | encounter zone / owner ref |
| Diag | `Diagnostics.Encounters.cs` | level range / respawn / owner / location dump |

---

## Vendors 商人
→ **說明文件**：[SPEC-world.md § vendors](../../../docs/spec/SPEC-world.md#vendors--merchants--a-working-shopkeeper)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Actors.cs` | `VendorSpec`（hours / buy-stolen / sell-list / chest）|
| Build P2 | `Generator.Build.Vendor.cs` | vendor faction config + merchant container + JobMerchantFaction 指派 |
| Validate | `Generator.Validate.Npcs.cs` | faction ref、vendor chest ref |
| Diag | `Diagnostics.Factions.cs` | vendor config / crime data dump |
