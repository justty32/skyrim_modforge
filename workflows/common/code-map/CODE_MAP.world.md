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
| `examples/navcut_spike_spec.json` | **navcut 證偽實驗**（plan T2.0）：白漫大街 A/B 對照，只差一顆 L_NAVCUT box；亦為 `navCuts[]` 的用法範例 |

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
| `WorldspaceNavmeshConnectionsTests.cs` | 2×2 exterior flat NAVM 的雙向跨格 `EdgeLinks[]`／triangle edge flag+index；不同高度仍連且頂點高度正確 |
| `WorldspaceCellLightingTests.cs` | exterior `WorldspaceCellSpec` 的 spec 內 LGTM + external IMGS 接線、兩欄省略為 null、cross-type validate |
| `WorldspaceBaseTextureTests.cs` | `worldspace.baseTexture`（LTEX）→ 每格 LAND 四象限 BTXT base 層（quadrant 全覆蓋、**LayerNumber 0xFFFF**＝vanilla base 標記、texture FormID）；omit = 無紋理 |
| `WorldspaceSplatmapTests.cs` | `worldspace.textureLayers`（多紋理混合）：`Vtxt.BuildLayers` 純函式（quadrant 切分、position=localRow×17+localCol、稀疏、opacity clamp、共用中央頂點）+ 端到端 PNG splatmap（每格四象限 ATXT/VTXT 層、**ATXT 0-indexed**、cell 落在圖外不生層）|
| `HeightmapTests.cs` | PNG load、Y-flip、min/max 映射、33×33 seam 零誤差 |
| `WorldspaceHeightmapTests.cs` | PNG→cell grid 尺寸推導、VHGT delta 非零、flat PNG = flat cell path、**相鄰 cell 邊界重建高度完全一致（seam stitching）**、validate（min<max / empty path / ESL 不相容） |
| `VhgtTests.cs` | encode（全零 flat、round-trip ±4 units、過陡 clamp+warn）、**RequiresSkyrim：Tamriel 20 格 decode→encode delta bytes 完全一致（主力機驗演算法）** |
| `VnmlTests.cs` | Vnml.Compute：平地全朝上、均勻東坡 X<128、均勻北坡 Y<128、Z=255 flat、對角等比坡 X=Y |
| `GodotPlacementsTests.cs` | Godot placements JSON 座標換算（origin offset、Z 翻轉、m→units）、rotation rad→deg、scale passthrough、instanceId→editorId、format version/coordinate system error cases、重複 Build 不修改 spec/不重複匯入 |
| `GodotPlacementsValidationTests.cs` | Godot placements v1 malformed payload fail-closed：required 欄位/null/Vec3 axes、非有限或非正數值、轉換 overflow、重複 instanceId 都回報檔案路徑與 entry index |
| `GodotPlacementsBuildValidationTests.cs` | Godot imports 整合 fail-closed：instanceId 與手寫/其他 Godot 檔/後段生成的 CELL、MCM quest 撞名、base malformed/unresolved、in-spec LVLN base CTD 防線；diagnostic 含 source/worldspace/index |
| `XMarkerTests.cs` | XMarker 放置（特殊 placement base）|
| `XMarkerKindTests.cs` | `kind:xmarker/xmarkerHeading` helper（空 base→0x3B/0x34 + persistent）+ `forced:` alias 解析到 xmarker 錨點 |
| `MapMarkerTests.cs` | mapMarker → MapMarker static + XMRK（type/flags）；持久 TopCell 加性帶上（⚠️ 需本機 Skyrim.esm）+ validate（type/flag）|
| `PlacementSpecFieldsTests.cs` | Scale(XSCL) / InitiallyDisabled(flag) / **NoHavokSettle(flag 0x20000000)** / EnableParent(XESP) / Lock(XLOC) / Ownership(XOWN) / Count(XCNT) build + validate |
| `LightTests.cs` | 自訂 Light（LIGT）color/radius/fade/flags build + validate |
| `LightingTests.cs` | LGTM/IMGS build + CELL XCLL inherit + validate guardrails |
| `CellWaterTests.cs` | interior 與 own-worldspace exterior CELL 的 `waterHeight` (XCLW) / `water` (XCWT) / `acousticSpace` (XCAS) build + ref validate；template 與 spec 都有水位時由 spec 覆寫 |
| `SkillTreeTests.cs` | `skillTrees:` macro-expansion（points/rank GLOB、node+line ACTI、垂直堆疊 placement、line 中點+rot+scale、gating 鏈 prereq/downLine、root 無 prereq、idempotent guard）+ build（temp refs、node 掛 MFSkillNode）+ validate（id 唯一/cell/name/ability 必填）|
| `SettlementTests.cs` | `settlements:` macro-expansion（ACHR spawn 座標/fallback、Sleep/Work/Wander package + wrap-midnight 時長 + 錨點、npc.Packages 排序、routine 覆寫、auto/explicit faction、crimeFaction、vendor FACT/chest/gold、friendlyResidents RELA、idempotent）+ build（**Sleep location 解析到 in-spec 床錨**的 deferred 修回歸測）+ validate（npc 非 in-spec、缺 spawn、sleep 無 home、未知錨、重複住民、vendor 時數、缺 cell/residents）|

---

---

## Interior Cells 室內空間
→ **說明文件**：[SPEC-world.md § cells & placements](../../../docs/spec/SPEC-world.md#cells--placements--putting-things-in-the-world)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `CellSpec`（name / template / **`waterHeight`→XCLW / `water`→XCWT / `acousticSpace`→XCAS** / encounterZone / **`music`→MUSC**）|
| Build P1 | `Generator.Build.Cells.cs` | 建 interior cell record；template 環境複製後套用明示水位/水種/聲學空間，確保 spec 覆寫 template（`encounterZone` / `music` 由 pass 2 接線）|
| Validate | `Generator.Validate.World.More.cs` | cell encounterZone / water / acousticSpace ref |

---

## Placements 放置（NPC / Object）
→ **說明文件**：[SPEC-world.md § cells & placements](../../../docs/spec/SPEC-world.md#cells--placements--putting-things-in-the-world) · [engine-internals.md § Cell GRUP](../../../docs/engine-internals.md#cell-grup-placement-is-keyed-by-formidgrid)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `PlacementSpec`（含 `kind:"xmarker"/"xmarkerHeading"` helper；**Scale/InitiallyDisabled/NoHavokSettle/EnableParent(`EnableParentSpec`)/Lock(`LockSpec`)/Ownership(`OwnershipSpec`)/Count**）, `LinkedRefSpec`, `Vec3` |
| Spec | `Spec.Primitives.cs` | **`PrimitiveSpec`**（type＝box(預設)/sphere/portalBox/none 或生數字、bounds＝**全尺寸非半徑**、color/opacity＝**CK 編輯器外觀，遊戲內無效**）＋ `PlacementSpec.Primitive`／`PlacementSpec.CollisionLayer`（皆 **REFR only**）。**觸發箱的 vanilla 配方**（2026-09-05 對 Skyrim.esm 1.6.1170 實掃 13,668 筆 XPRM）：base 指 ACTIVATOR（`defaultActivateSelfTRIG`=`Skyrim.esm:0x048AC0` 486 筆／`defaultSetStageTRIG`=`0x033F50` 279 筆／`WordWallTrigger`=`0x05095E` 46 筆 Sphere），Box＋(204,76,51)＋0.15，**不設 collisionLayer**（觸發行為來自 ACTI base，XPRM 只說體積多大）|
| Spec | `Spec.MapMarkers.cs` | `MapMarkerSpec`（editorId/name/worldspace/position/`type`(MarkerType)/`flags`(Visible/CanTravelTo/ShowAllIsHidden)）|
| Build P2 | `Generator.Build.Placements.cs` | 室內/室外/vanilla-override 放置，position/rotation，persistent flag，cell 錨定；**`kind:xmarker/xmarkerHeading` → 空 base 自動填 `Skyrim.esm:0x3B`/`0x34` STAT + 強制 persistent**（quest-target 錨點）；**base 是 in-spec HAZD（或 `kind:"hazard"`）→ 建 `PlacedHazard`（`.Hazard` 而非 `.Base`）**；**Scale(XSCL) / InitiallyDisabled(0x800) / NoHavokSettle(0x20000000＝DontHavokSettle，跳過 cell 載入時的 havok settle pass，手擺的雜物才不會被彈飛；vanilla Skyrim.esm 有 3791 個 REFR 帶它；**REFR only**，ACHR 不寫) / Lock(XLOC) / Ownership(XOWN) / Count(ItemCount XCNT)**；`ParseLockLevel` helper 在 `Generator.Helpers.cs`；**EnableParent(XESP) 只丟進 `deferredEnableParentWires`**（同一迴圈裡 eager 解析只看得到列表中「更早」的 placement——`WireDeferredEnableParents` 在 `Generator.Build.PlacementRefs.cs` 才真正接線，見下） |
| Build P2 | `Generator.Build.MapMarkers.cs` | **`BuildMapMarkers`**：每筆 → MapMarker static（`0x10`）上的 `PlacedObject` + XMRK `MapMarker`(Name/Type/Flags)，放進 worldspace **持久 TopCell**（`WorldspacePersistentCell`），registered 進 formKeyByEd 故可被 `forced:` alias 抓 |
| Build P2 | `Generator.Build.PlacementRefs.cs` | linked-ref 對 + teleport-door XTEL 接線（deferred）；**`WireDeferredEnableParents`**：placement `enableParent.ref` 的 XESP 接線，跑在 `BuildPlacements`＋`BuildReferences` 都完成後——解得到**指向列表後面的 placement**與 `references[]` label（原本 eager 解析在同一迴圈當場做，只看得到「更早」的 placement，解不到就照樣印 `unresolved` 警告、不生 XESP）|
| Build P2 | `Generator.Build.ExteriorCells.cs` | 室外 worldspace cell group tree（block/sub-block 按 grid 坐標）；**`WorldspaceOverride` 加性帶上 master 持久 TopCell（`CopyCellEnv`、不重述 vanilla ref）否則 vanilla 地圖標記全消失+大地圖空白**；**帶 master 的 `Name`(FULL)**——override 少了它就把該 worldspace 的名字**清空**（本地地圖/存檔顯示不出「Whiterun」；`MasterCache` 已 provision 英文 STRINGS 所以現在讀得到，Tamriel 的硬編碼只留作 fallback）；**`WorldspacePersistentCell`** 回 worldspace 持久 cell 給地圖標記 |
| Build P2 | `Generator.Build.Primitives.cs` | **`MakePrimitive`**（唯一產 `PlacedPrimitive` 的地方，navCuts 與 `placements[].primitive` 共用）＋ **`BuildPrimitive`**（spec→記錄：缺 bounds 警告不寫、sphere 三軸自 X 補齊、`ParsePrimitiveType` 收名稱/生數字，不認得回退 Box）。寫入點在 `Generator.Build.Placements.Record.cs` 的 `ApplyPlacementAttributes`（ACHR 上給 primitive/collisionLayer 會**出聲警告**而非靜默丟掉——不然就是一個永遠不觸發又查不出原因的箱子）|
| Validate | `Generator.Validate.Primitives.cs` | **`ValidatePrimitives`**：primitive/collisionLayer 不可放 ACHR、bounds 必填且三軸為正（sphere 可只給 X）、type 名稱、opacity 0..1、base 不可空 |
| Validate | `Generator.Validate.World.cs` | linked-ref target、teleport pairs、worldspace boundary |
| Diag | `Diagnostics.Dump.World.cs` | placements / cells / linked-refs / navmesh dump |

---

## Navmesh — navCuts（避障）＋ P1 診斷 ＋ navmeshOverrides（P0）＋ navPatches（P3 add+link）
→ **說明文件**：[SPEC-world.md § navmesh](../../../docs/spec/SPEC-world.md#navmesh--why-npcs-walk-into-your-house-and-how-to-stop-them) · 計畫 [plans/navmesh.md](../../plans/navmesh.md)

Skyrim NPC **只走 navmesh**：腳下沒三角形＝完全不動（且**無任何錯誤訊息**）；vanilla navmesh 不知道你新蓋的房子＝NPC 直接穿牆。四件事：**runtime 裁切**（`navCuts[]`）＋ **build 時警告**（P1）＋ **no-op NAVM override**（`navmeshOverrides[]`，P0）＋ **append+link**（`navPatches[]`，P3 MVP：讓既有內裝網格延伸到新平台）。

**🔴 鐵律：永不重新編號 triangle。** 鄰居 cell 的 NAVM 的 EdgeLink 存的是**你這張網格的 triangle 陣列下標**——重排一次，整條 cell 邊界就錯位（CK Finalize 會重編號，所以它被迫連鄰居一起存；社群「navmesh 不能在 CK 外改」的真正成因）。no-op override 逐元素照抄，未來 P2/P3 只能**尾端 append ＋ Deleted flag**。

**🔴 兩段閘門（別搞錯）**：`Obstacle` record flag(bit 25) **單獨無效**——引擎是看**碰撞層**，vanilla 55 個 COLL 只有 6 層帶 `NavmeshObstacle`（L_ANIMSTATIC/CLUTTER/PROPS/DEBRIS_LARGE/TRANSPARENT_SMALL_ANIM/**L_NAVCUT(49)**），**L_STATIC(1) 不在內**（一般房子/牆/石頭正是 L_STATIC）→ 「複製 STAT ＋ 加 flag」完全無效。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.NavCuts.cs` | `NavCutSpec`（editorId/cell/worldspace/**placement**/position＝**box 中心**/size＝**全尺寸非半徑**/rotationZ°/padding）＋ `PlacementNavCutSpec`（enabled/size/offset/padding，帶 `PlacementNavCutConverter` → JSON 收 `false`/`true`/物件三形）＋ `NavmeshSpec`（warnings/autoNavCuts/minFootprint/minHeight/padding/warnEmptyCells）|
| Spec | `Spec.World.cs` | `PlacementSpec.NavCut`（**省略＝依體積自動**、`false`＝不裁、`true`＝硬裁、物件＝手調）＋ `PlacementSpec.NavmeshCheck`（false＝這筆 ACHR 是**刻意**擺在網格外，關掉診斷；`livingNpcs` 的 off-stage 停車位用）|
| Spec | `Spec.NavmeshOverrides.cs` | `NavmeshOverrideSpec`（cell＝vanilla 內裝／worldspace＋**x,y＝cell 格座標**（或 position 選格）／navmesh＝只挑一張）＋ 為什麼要 no-op、鐵律、NAVI 為何不碰 |
| Spec | `Spec.NavPatches.cs` | `NavPatchSpec`（vanilla interior cell + navmesh + 3–32 點凸 polygon + `linkTo:auto` + epsilon）；完整契約見 `workflows/specs/navmesh-patch-design.md` |
| Spec | `Spec.cs` | `ModSpec.NavCuts` + `ModSpec.NavmeshOverrides` + `ModSpec.NavPatches` + `ModSpec.Navmesh` |
| Build P2 | `Generator.Build.NavCuts.cs` | **`BuildNavCuts`**（`Build.cs` 在 `BuildRemovals` 後呼叫）：每個 box → `PlacedObject`{Base=**CollisionMarker `Skyrim.esm:0x000021`**、**`CollisionLayer=49`**、`Primitive`{Box, Bounds=size+2×padding, Color=255,255,0, Unknown=0.15}}，進 cell 的 **Temporary**（**不 persistent**——Skyrim.esm 自己的 441 個靜態 navcut 都是 temporary；exterior 走 persistent 會拖出 worldspace TopCell 的地圖地雷）。**auto**：placement 是 PlacedObject ＋ 在 **vanilla** cell/worldspace ＋ OBND 過門檻 ＋ **真的蓋到 ≥1 個 live 三角形** 才生（三個 guard 缺一不可；最後一條也讓**離線＝零產出＝位元不變**）。欄位已 byte-verify vs HearthFires 1003 筆 ＋ Skyrim.esm 441 筆 |
| Build P2 | `Generator.Build.NavmeshIndex.cs` | **navmesh 幾何讀取**（RequiresSkyrim 領域）：`NavTrisAt(cell, ws, pos, builtCell)` → vanilla 內裝（`ICellGetter.NavigationMeshes`，**cell-local**）/ vanilla 外景（`FindMasterExteriorCell` **3×3 鄰域**，**world**——邊界點常被鄰居網格蓋住）/ 自建 worldspace（我們自己的 flat quad）/ 自建內裝（`NoTris`＝**已知空**）。座標系天生對得上 `PlacementSpec.Position`，零轉換。跳過帶 `Deleted` flag 的三角形。**`null` ＝「不知道」（無 link cache）→ 呼叫端一律沉默**（鐵律①）。幾何：`InTri2D`/`TriZAt`/`DistToTri2D`/`NearestTri` |
| Build P2 | `Generator.Build.NavmeshCheck.cs` | **`CheckNavmesh`**（`Build.cs` **最後**呼叫，**只出警告、零記錄**）：**②** ACHR 不在任何三角形上／離地太高 → 「這個 NPC 不會動」；**①** blocking placement 蓋住 vanilla 三角形但沒被任何 navcut box 蓋掉 → 「NPC 會走進去」；**③** `removals[]`/`overrides[]` 動到的大物件**頂面有 navmesh** → 「NPC 會走在空氣上」。`navmesh.warnings:false` 全關；`placements[].navmeshCheck:false` 單筆關 |
| Build P2 | `Generator.Build.NavmeshOverrides.cs` | **`BuildNavmeshOverrides`**（`Build.cs` 在 `BuildNavCuts` 後呼叫）：解出 vanilla cell → `nm.DeepCopy()` 進**我們自己的 cell override**（`VanillaCellOverride` / `ExteriorCell`），**同 FormKey ＝ override**。**刻意不用 Mutagen 的 `GetOrAddAsOverride` parent chain**——那會讓 Mutagen 自己造 CELL/WRLD override，而我們的 `WorldspaceOverride`/`CopyWorldspaceEnv` 帶著兩顆地雷的疤（LandDefaults 不帶＝淹世界、EDID/RNAM 不帶＝地圖白/壞、TopCell 的 record flags 不帶＝CTD、OFST 帶了＝檔案錯位）。**NAVI 完全不碰**（FormID 沒變，vanilla NVMI 仍指得到＝U4）。**無 Skyrim.esm ＝零產出零警告**。**U10 `CheckNavmeshOverrideClobbers`**（`Build.cs` 在 `CheckNavmesh` 後、同為 warnings-only 零記錄）：NAVM ＝整筆後蓋前、無加法合併，所以掃 Data 夾裡「master 到我們 override 的 NAVM 所屬 master」的**非-vanilla** plugin，若它也 override 同一張 → 警告（無法知 load order 誰贏，只點名衝突，USSEP 是典型受害者）。**只在 `navmeshOverridden` 非空時掃**（一般 build 不付掃描成本）；無 Data 夾＝沉默；開關 `navmesh.warnNavmeshClobber`（`Spec.NavCuts.cs`，預設 true）|
| Build P3 | `Generator.Build.NavPatches.cs` + `NavmeshPatch.cs` | vanilla interior NAVM detached deep-copy → winding 正規化／凸 polygon fan／新三角互連／唯一完整 boundary seam 雙向縫合 → 重建 bounds + divisor=1 全 triangle grid；成功才 publish。舊 triangle/vertex 只 append 不重排，唯一舊修改是 seam EdgeLink；NAVI 不碰；同 mesh 多筆依序累加；失敗警告且零 partial mutation。`Generator.Build.NavmeshIndex.cs` 在 diagnostics 階段優先讀已 patch 的 built cell，避免把新平台上的 NPC 誤報 off-mesh |
| Validate | `Generator.Validate.NavCuts.cs` + `Generator.Validate.NavPatches.cs` | 前者驗 navcut/no-op override；後者驗 external cell/NAVM、3–32 finite points、零面積/重點/自交/嚴格凸、epsilon > 0、`linkTo=auto` |
| Diag | `Diagnostics.Navmesh.cs`（CLI） | **`navdiag`** ＝ P0 的 GO/NO-GO 閘。`navdiag <esp>` 列出每張 NAVM（頂點/三角/**跨 mesh EdgeLinks**/door tri/cover/grid divisor/min-max/record flags）**並把每張 override 的 NVNM 與 master 的原始位元組逐 byte 比對**（`IDENTICAL`/`DIFF`，DIFF ＝ exit 1）。vanilla 那一側**不經 Mutagen**——直接掃 Skyrim.esm 的 NAVM record header、zlib 解壓（vanilla NAVM 帶 Compressed 0x40000）、走子記錄找 NVNM（含 XXXX 超長度）——否則就是拿 Mutagen 的輸出比 Mutagen 的輸出，什麼都證不了。`navdiag <esm> <0xCELL>` / `navdiag <esm> <0xWRLD> <x> <y>` ＝ 偵察某個 vanilla cell 有哪些網格 |
| Example | `examples/navcut_spike_spec.json` | **T2.0 證偽實驗**（白漫大街 A/B 對照：同樣的 NPC/package/marker/告示牌，只差一顆 navcut box）。14 個座標全部**讀 Skyrim.esm 的 navmesh 挑出來**（marker 不會貼地，猜 z ＝ patrol 靜默失效）|
| Example | `examples/navmesh_noop_spike_spec.json` | **P0 證偽實驗**：整份 esp **只有** 10 張 vanilla NAVM 原樣搬過來（Bannered Mare 內裝 ＋ 白漫外景 (5,-2)/(5,-3)）。沒 NPC 沒擺放沒腳本＝失敗只有一個可能成因。`esl:false`（ESL 安全性是另一顆未知數 U7，不混進來）|
| Example | `examples/navmesh_patch.json` | **P3 實機 spike**：從 Bannered Mare 真實 boundary edge 推導 64-unit 平台，append 4 vertex/2 triangle，NPC 在新舊兩側 marker 間巡邏；連續跨 seam＝PASS |
| Tests | `PrimitiveTests.cs` | 20 條：預設＝vanilla 觸發箱配方、color/opacity 可寫、type 四名稱＋生數字、sphere 補三軸＋不等值警告、省略＝無 XPRM、collisionLayer 有寫/沒寫、ACHR 上 validate＋build 雙重擋、缺 bounds／零 bounds／未知 type／opacity 出界四條 validate、**以及 navcut 共用 helper 後仍吐原本的 CollisionMarker 黃箱配方**（重構行為不變的釘子）|
| Tests | `NavCutTests.cs` | 記錄形狀（base/layer 49/Box/黃色/0.15/temporary）、size＝全尺寸、padding 三軸外脹、`navCut` 的 bool/物件 JSON 三形、validate 5 條、**離線＝零 navcut**；RequiresSkyrim：auto 裁大牆（`autoNavCuts` **預設 `true`**，2026-07-12 T2.0 實機 PASS 後翻回）、跳過雜物、尊重 `false`、`autoNavCuts:false` 改成警告、`navCuts[].placement` 包 OBND |
| Tests | `NavmeshCheckTests.cs` | 自建內裝預設沉默＋`warnEmptyCells` 開了才講、每 cell 只警告一次、總開關、**無 master ＝ 完全沉默**；RequiresSkyrim：白漫街上站好＝沉默／離網格＝警告／浮空＝警告／刻意 off-stage 可 `navmeshCheck:false` 豁免／大牆未裁＝警告／有 navcut ＝閉嘴／雜物永不吵；**spike spec 本身必須零警告＋剛好 1 顆 navcut** |
| Tests | `NavmeshOverrideTests.cs` | **無 master ＝零記錄零警告**、validate 3 條；RequiresSkyrim：同 FormKey ＋ record flags(0x40000) ＋ 掛在 vanilla cell override 下、**逐 triangle/vertex 比對 vanilla（含 EdgeLink 三欄、grid blob、divisor、door tri、min/max）＝一個索引都沒動**、**零 NAVI 記錄**、外景 6 張全搬且 WRLD override 帶 EDID/RNAM/**FULL**/TopCell(0x40400)、**無 OFST**、`navmesh` 只挑一張＋同 cell 列兩次只搬一次、空 cell 要警告。**U10 clobber（合成 plugin，離線可跑）**：假 master 帶 cell+NAVM ＋ 假 patch override 同張 → 警告點名該 plugin＋mesh；只有 owner 在場＝不警告；`warnNavmeshClobber:false`＝閉嘴仍照 build |
| Tests | `NavmeshPatchTests.cs` | 純幾何：validate 凹形／錯 link mode、CW 正規化、fan 新新 adjacency、新舊 seam 雙向、grid/bounds、無 seam mutation-free、offline 零產出；RequiresSkyrim：Bannered Mare 真 NAVM append-only、298→302 vertex／318→320 triangle、舊 index/vertex 全不動、只一條舊 EdgeLink 改、NAVI 零記錄 |

---

## In-world Skill Trees 技能樹（Idea #20）
→ **說明文件**：[SPEC-world-macros.md § in-world skill trees](../../../docs/spec/SPEC-world-macros.md#in-world-skill-trees-skilltrees) · memory `inworld-skill-tree-standalone-confirmed`

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
→ **說明文件**：[SPEC-world-macros.md § populated settlements](../../../docs/spec/SPEC-world-macros.md#populated-settlements-settlements) · 設計 [settlement-population-design](../../specs/archive/README.md)

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
→ **說明文件**：[SPEC-world-macros.md § living-world NPCs](../../../docs/spec/SPEC-world-macros.md#living-world-npcs-livingnpcs) · 設計 [sub_projs/living-adventurers/](../../../sub_projs/living-adventurers/README.md)（idea #23 + design.md）

一小撮**具名持久 NPC 過自己的離場人生**（抽象幽靈模擬 + 就地實體化）的 **macro**（同 settlements pass-0），但**會掛 runtime 腳本**（兩個可重用 .pex）。每多一個既有 archetype 的 NPC = 一個 entry（純資料）。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Controller .pex | `assets/papyrus/MFLivingWorldController.psc` | host quest，單一 roster 迴圈：`OnUpdateGameTime` 推進每個 alias 的抽象 sim、`OnUpdate` 跑每個 alias 的在場 poll（chained，不隨 N 線性膨脹）。props `SimIntervalHours`/`PollInterval`/`AliasCount` |
| Alias .pex | `assets/papyrus/MFLivingNpcAlias.psc` | per-NPC（extends ReferenceAlias）：`AdvanceSim`（deed global +1 + 輪替 anchorIdx）、`Presence`（玩家同 cell → `MoveTo` 進場 + `EvaluatePackage`；離開 → 回 HoldMarker）；`IsPlayerTeammate()` 時停止 sim/MoveTo 並交還 follower system。dismiss 後保留原 follower package 的可見步行，只有玩家已無法跟上且 grace 到期才 off-screen 回 hold、恢復納管。props `Archetype`/`HoldMarker`/`Anchors`/`DeedCount`/`ReclaimDelaySeconds`（30）/`ReclaimDistance`（8192） |
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
→ **設計**：[specs/ingame-scene-export-design.md](../../specs/ingame-scene-export-design.md) · archived plan [2026-07-08-ingame-scene-export.md](../../plans/archive/2026-07-08-ingame-scene-export.md) · idea [tools/24-ingame-editor.md](../../idea/tools/24-ingame-editor.md)

給一個**外部 captured NPC**（PROTEUS clone / follower base）一個職業 `role`，pass-0 macro 展開成該 NPC 的 conditioned 問候 + 行為。**非玩家向 `IdentitySpec`**（那是玩家加入 FACT gate 玩家對話）；與 `ResidentSpec` 差別＝keyed on **外部 base NPC ref** 且自帶對話。切片只做 `blacksmith`。scene.json ＝一份 `ModSpec` 片段（`placements`/`mapMarkers`/`hazards`/`npcRoles`），生成端 placements/marker/hazard **全已具備**。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.SceneExport.cs` | `SceneNpcRoleSpec`（npc=外部 base ref／role／backstory）|
| Spec | `Spec.Annotations.cs` | `AnnotationSpec`（seq/label/kind/**note**/position/angleZ/cell/worldspace）——遊戲內編輯器 marker 錨點（Idea #24 P1），**advisory only**：build 永不生記錄，僅 `Program.Build.cs` log 數量；agent 讀它 author 下一輪 spec；note＝自由文字補充指示（DLL 的 E 編輯視窗填，2026-07-11） |
| Spec | `Spec.cs` | `ModSpec.NpcRoles` + `NpcRolesExpanded` 守衛旗標；`ModSpec.References`（referrer，見下 References 列）|
| Expand P0 | `Generator.SceneNpcRoles.cs` | **`ExpandNpcRoles`**：每 role → 共享 StartGameEnabled host quest `MF_SceneNpcRolesQ`；blacksmith → Hello `DialogueSpec`（GetIsID npc）+ sandbox `PackageSpec`（無 location＝editor-location fallback）+ `NpcPatchSpec`（overrideOf npc, append）。**vendor**：有 companion placement（base==npc，`RefsMatch`）時 → Vendor FACT（`VendorItemsBlacksmith` 0x066333/8-20 時）+ merchant chest（gold）共置 placement + `patch.Factions` 加 vendor FACT + `JobMerchantFaction`（vanilla 交易對話）；無 placement 跳過 vendor。未知 role 不展開（Validate 警告）。`SanitizeEd` 把外部 ref 轉成合法 editorId 片段。常數 `BlacksmithGold`/`BlacksmithVendorList` |
| Build | `Generator.Build.cs` | pass-0 呼叫 `ExpandNpcRoles(spec)`（在 `ExpandLivingNpcs` 後）|
| Removals P1 | `Spec.Removals.cs` + `Generator.Build.Removals.cs` | **`RemovalSpec`**（ref/label?/note?，`RemovalConverter`：bare string ⇄ `{"ref":...}`，同 `RequirementConverter` 套路，label/note 皆空時 Write 收斂回裸字串）＋**`BuildRemovals`**（Idea #24 §E 橡皮擦）：`removals[]` 每個 `.Ref`（`<master>:0xFORMID`）→ `MasterCache.TryResolveContext<IPlaced>` → `GetOrAddAsOverride(mod)`（自動 override parent cell/worldspace）→ `MajorRecordFlagsRaw\|=0x800`(InitiallyDisabled) + 深埋 Z−30000。`label`/`note` 純文件用途，build 從不讀。Build.cs 在 BuildMapMarkers 後呼叫。RequiresSkyrim（要 master link cache）|
| Overrides | `Spec.Overrides.cs` + `Generator.Build.Overrides.cs` | **`OverrideSpec`**（ref/position/rotation°/scale?/label?/note?，後兩者純文件用途、build 從不讀）＋**`BuildOverrides`**（Idea #24 numpad 編輯器）：頂層 `overrides[]`（拍板 B 案，理由在 spec）——同 removals 解析機件 → `GetOrAddAsOverride` → 重蓋 `Placement`（度→弧度）；`scale` null=不碰、1.0=清 XSCL、其他=寫。Build.cs 在 **BuildRemovals 前**呼叫（撞名時 removal 後蓋、贏）。RequiresSkyrim |
| References | `Spec.References.cs` + `Generator.Build.References.cs` | **`ReferenceSpec`**（ref/label/base?/position?/rotation°?/scale?/cell?/worldspace?/anchor?/note?）＋**`BuildReferences`**（Idea #24 referrer `sc ref`）：頂層 `references[]` ＝**命名一個既有 placed ref**（removals=擦、overrides=移、references=**命名**，三兄弟）。**`label` 註冊進 `formKeyByEd`** → spec 任何 ref 欄位（package sandbox.location/travel.place、alias `forced:`、linkedRefs、enableParent、objective target、script prop）都能寫這個 label，消費站點零改動。**(乙) 檔內路徑**：`ref`＝同檔 placements[] editorId → `BuildPlacements` 的 anchor 集合強制它 persistent ＋ `BuildReferences` 補 0x400 → **乾淨、離線可測**。**(甲) 外部路徑**：`ref`＝`<master>:0xFORMID` → 查 master link cache 的 0x400 flag，temporary 就**警告**；`anchor:"marker"` 生 persistent XMarkerHeading、`anchor:"replace"` 生自家 persistent 複製品＋把原件推進 `referenceRemovals`（`BuildRemovals` 一併 disable+bury）。空 list ＝不生任何記錄（行為不變）。Build.cs 在 **BuildMapMarkers 後、BuildOverrides 前**呼叫（必在 placements 後、所有 wire 前）。🔴 **area-anchor 護欄** `NoteLabelsUsedAsAreaAnchors`：label 掉進 **location 槽**（`PackageRefSlots` 的 `Location` 類）時印一行 **info**（`BuildResult.Notes`，**不是 warning**——「在那附近晃」合法）說明「錨的是區域、引擎會在 radius 內挑別的家具；要鎖定改用 SingleRef 槽」。只在 ref ＝ `references[]` **label** 時印（vanilla FormID / 檔內 placement editorId 放 location 槽＝一般用法，不吵）。**零產物變動**。**`area:<ref>` 前綴 opt-out**（helper `HasAreaPrefix`/`StripAreaPrefix` 在 `Generator.Build.Packages.AliasRefs.cs`）：作者在 location 槽寫 `"area:sofia's chair"` ＝明示「我就是要一塊區域」→ 護欄不印（意圖已答）；解析端 `MakeLocationSlot`（`Generator.BuildContext.Utilities.cs`，全 location 槽唯一 chokepoint）先剝前綴再解 label，**無前綴＝byte-identical**。前綴只在 location 槽合法，SingleRef 槽留給它 unresolved（正確） |
| Validate | `Generator.Validate.Npcs.cs` | package location 槽（sandbox/sleep/travel/escort.destination/useMagic.location/eat）走 `PkgLoc` local fn＝`PkgSlotRef(StripAreaPrefix(...))`，先剝 `area:` 前綴再驗；SingleRef 槽維持裸 `PkgSlotRef`（`area:` 在那留給它報 unresolved）|
| Validate | `Generator.Validate.References.cs` | `RegisterReferenceLabels`（`Validate()` 開頭呼叫，把 label 加進 `Ids` → 其他 CheckRef 認得）＋`ValidateReferences`：ref/label 必填、label 唯一且不可長得像外部 ref、非外部 ref 必須是 placements[] editorId、anchor∈{none,marker,replace}、anchor 不可用在檔內 ref、anchor 需 cell/worldspace、`replace` 需 base、cell/worldspace 互斥、scale>0 |
| **core 前置修** | `Generator.Build.Dialogue.Hello.cs`（2026-08-27 前在 `Generator.Build.Dialogue.cs`）| **外部 speaker Hello 支援**：conditioned-hello 建構 + `AddSpeakerGate` 兩處，in-spec `npcsByEd` 失敗時 fallback `TryResolveRef` 解析 `<plugin>:0xID` → GetIsID FormKey；`MakeHello` 改吃 `FormKey`（非 `INpcGetter`）並在 hello 材質化迴圈解析外部 speaker（否則外部 NPC 的 Hello topic 永不生成）。回歸測全綠 |
| Validate | `Generator.Validate.SceneNpcRoles.cs` | `ValidateNpcRoles`：npc 必填、role 必填且須已知（`KnownRoles`={blacksmith}）；`ValidateRemovals`：removals 須 well-formed 外部 ref；`ValidateOverrides`：同 ref 形狀規則＋與 removals 撞名＝矛盾（警告，build 讓 removal 贏）|
| Example | `examples/scene-export-blacksmith.scene.json` | M0 契約 fixture：farmhouse(0x00084A)+Carlotta(0x013B99 as 替身)+town marker+campfire hazard+blacksmith role |
| Example | **`examples/referrer-chair-anchor.json`** | **referrer 權威範例／實機價值證明**（zip `ModForgeReferrerChair`）：vanilla WhiterunBreezehome（0x0165A8，**要 navmesh ⇒ 必 vanilla cell**）擺**兩張同 base 的 `CommonChair01F`**，`references[]` 只命名其中一張 → Sofia 的 **SitTarget** package slot-16 SingleRef 指該 label。**對照組**＝未命名的誘餌椅（更近、擋路）⇒ 「坐了椅子」≠「坐了命名的那張」。命名的 `0x808` flag=0x400/Persistent、誘餌 `0x807` flag=0/Temporary |
| Example | `examples/scene-references.json` | **(甲) 外部路徑 ＋ `anchor` 逃生門** fixture：Skulvar 的鋤頭(0x0D1991) → temporary 警告；乙路徑那半（椅子 0x0B9C04 → label `"sofia's chair"`）**已從 `sandbox.location` 改為 `sitTarget.target`**——🔴 location 槽只錨定**區域**（`LocationTarget`＋radius，引擎在 radius 內自己挑家具），要鎖定**就是那一個 ref** 必須用 **SingleRef target 槽**（`PackageTargetSpecificReference`）。舊寫法 build 全綠卻證明不了任何事 |
| Tests | `SceneNpcRolesTests.cs` | validate（缺 npc／未知 role）+ expand（host quest/greeting/package/patch、idempotent、未知 role emit 空、無 npcRoles no-op）+ build（Hello INFO GetIsID gate 到外部 NPC FormKey）|
| Tests | `ReferencesTests.cs` | 乙路徑（強制 persistent 0x400、label 解析成 package Location target、不多生記錄、無 references[] 時椅子仍是 temporary＝行為不變）+ validate 9 條 + 甲路徑（無 link cache 仍綁 label；RequiresSkyrim：temporary 警告、`anchor:"marker"` 生 XMarkerHeading、`anchor:"replace"` 生自家複製品且原件 disable+深埋）|
| Tests | `ReferenceSlotKindTests.cs` | **area-anchor 護欄**：label→location 槽＝一行 info（含槽名/radius/AREA/該改用哪個 SingleRef 槽，前綴 `  i `，**Warnings 為空**）；六個 location 槽全會印、六個 SingleRef 槽全不印；**negative**：label 在 SingleRef 槽／location 槽放 vanilla FormID／location 槽放檔內 placement editorId／沒有 `references[]` ⇒ 完全不吵；產物不變（記錄數/槽 payload 照舊）；**反腐化**：reflection 掃 `PackageSpec` 全 string 欄位須在 `PackageRefSlots` 分類過 + 表的 accessor 真的讀到對應欄位；**`area:` opt-out** 三條：前綴讓護欄閉嘴、前綴仍解到與裸 label 同一 FormKey（產物不變）、前綴套在 vanilla FormID 也照解 |

---

## Lights 自訂光源（LIGT）
→ **說明文件**：[SPEC-lighting.md § lights](../../../docs/spec/SPEC-lighting.md#lights--custom-light-sources-ligt)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Lights.cs` | `LightSpec`（editorId/name/color/radius/fadeValue/falloff/fov/flags/value/weight）|
| Build P1 | `Generator.Build.Lights.cs` | `BuildLights`（`mod.Lights.AddNew`；color R/G/B、radius/fade、`Light.Flag` 解析；BuildFormKeyTable 前建，故 placement 可按 editorId 放置）|
| Validate | `Generator.Validate.Lights.cs` | `ValidateLights`（flag 名合法、color 0..255、radius>0、editorId 唯一）|
| Diag | `Diagnostics.Records.cs` | `lightdiag`（radius/color/fade/flags dump）|

自訂 Light 是一般 base record——用既有 `placements[]`（base=light editorId）放進任意 cell，無需動 placement 程式碼。

---

## Lighting Templates + ImageSpaces 室內光照（LGTM / IMGS / CELL XCLL）
→ **說明文件**：[SPEC-lighting.md § lighting](../../../docs/spec/SPEC-lighting.md#lighting)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Lighting.cs` | `LightingTemplateSpec`(LGTM) / `ImageSpaceSpec`(IMGS) / `CellLightingSpec`(inline XCLL) / `AmbientColorsSpec`(DALC) |
| Spec | `Spec.World.cs` | `CellSpec.LightingTemplate/ImageSpace/Lighting` |
| Spec | `Spec.Worldspace.cs` | `WorldspaceCellSpec` 的 `WaterHeight`(XCLW) / `Water`(XCWT) / `AcousticSpace`(XCAS) / `LightingTemplate` / `ImageSpace`（own-worldspace exterior CELL；ref 可接 spec 內或 external）|
| Build P1 | `Generator.Build.Lighting.cs` | `BuildLightingTemplates` + `BuildImageSpaces`（模板抄+覆寫；DALC LGTM→DirectionalAmbientColors、XCLL→AmbientColors；BuildCells 前建，lgtmByEd/imgsByEd 供 cell 解析）|
| Build P1 | `Generator.Build.Cells.cs` | cell 掛 LGTM/IMGS link（`ResolveLightingRef`）+ `ApplyCellLighting`（inline XCLL + inherit flags；無 inline 且有 template → 全繼承）|
| Build P2 | `Generator.Build.Worldspace.cs` | flat exterior CELL 建構時寫 `waterHeight`→XCLW，並用既有 `Wire` 掛 `water`→XCWT、`acousticSpace`→XCAS、LGTM／IMGS；空白欄位不寫 link |
| Validate | `Generator.Validate.Lighting.cs` | color 0..255、template/cell-ref 可解（cross-type）、inherit flag 名合法 |
| Diag | `Diagnostics.Records.cs` | `lgtmdiag` / `imgsdiag` |

> 註：`ImageSpaceSpec`(IMGS base) 與既有 `ImageSpaceModifierSpec`(IMAD, `Generator.Build.ImageSpace.cs`) 是兩個不同 record。

---

## Worldspaces + Regions
→ **說明文件**：[SPEC-worldspaces.md](../../../docs/spec/SPEC-worldspaces.md)（worldspace 細節已從 SPEC-world.md 移來此檔）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Worldspace.cs` | `WorldspaceSpec`（含 `Heightmap`/`GodotPlacements`/**`BaseTexture`**(LTEX ref)/**`TextureLayers`**(多紋理混合)）, `HeightmapSpec`, **`TerrainTextureLayerSpec`**(texture+splatmap), **`SplatmapSpec`**(path/originX/Y), `GodotPlacementsSpec`, `WorldspaceCellSpec`（含 XCLW/XCWT/XCAS 與選用 LGTM/IMGS）, `WorldMapDataSpec`, `RegionSpec`, `RegionWeatherEntrySpec`, `PointSpec` |
| Build P1 | `Generator.Build.Worldspace.cs` / `Generator.Build.GodotPlacements.cs` | 建 worldspace record（climate/water/map bounds）+ cell grid骨架；flat cell 的選用 XCLW／XCWT／XCAS／LGTM／IMGS 在 `EmitCell` 寫入（ref 走既有 `Wire`；空值不寫）；**PNG heightmap 路徑**（`Heightmap.Load` → `Vhgt.Encode` per cell）；**單層地形貼圖 `baseTexture`**（resolve LTEX 一次 → `EmitCell` 每格四象限加 `BaseLayer`{`LayerHeader.Texture`/`Quadrant`}，**`LayerNumber=BaseLayerNumber`=0xFFFF**）；**多紋理 `textureLayers`**（每層 resolve LTEX + `Splatmap.Load`；`EmitCell` 每格 `TrySampleCell` → `Vtxt.BuildLayers` → 加 `AlphaLayer`，**LayerNumber=層序 0-indexed**）；**有紋理時 `LAND.Flags |= Layers`(0x04)**；**Godot placements per-build 展開 + base/global editorId fail-closed gate**（在 `BuildPlacements` 前注入、不修改 caller spec）|
| Build P1 | `Generator.Build.Regions.cs` | 建 region record（polygon / weather table / priority / map color）|
| Build P2 | `Generator.Build.ExteriorCells.cs` | cell group tree 生成（外層結構）|
| Build P2 | `Generator.Build.Navmesh.cs` | NAVM 4 頂點平面 quad + NAVI 索引；全部 quad 建完後，同 worldspace 正交相鄰且兩側皆啟用 navmesh 的 cell 互建 external `EdgeLinks[]`，triangle edge 欄存本表 index 並設對應 flag（[engine-internals § navmesh](../../../docs/engine-internals.md#programmatic-navmesh-navm--navi--in-game-confirmed-2026-06-03)）|
| Build P2 | `Generator.Build.Navmesh.Custom.cs` | `navmeshGeometry` 頂點/三角形原樣寫入；處理 spec 明示的跨格 links（不重排 triangle）|
| Build P2 | `Generator.Build.Navmesh.Seams.cs` | 只補至少一側是自訂幾何的未連接邊界；投影區間重疊 + `CustomNavmeshSeamMaxZDelta=128` 才雙向配對，配不上出警告；flat↔flat 明確略過，不碰既有路徑 |
| Util | `Heightmap.cs` | 16-bit grayscale PNG → 全域高度網格；`SampleCell` 切 33×33（相鄰格共用邊緣欄）；`SampleCellExtended` 切 35×35（+1px 邊框，供 VNML 中心差分）；Y-flip（影像頂=北）|
| Util | `Vhgt.cs` | VHGT 編解碼：絕對高度 → float offset + 33×33 signed-int8 delta（row-wise 累積，×8 game units）；`Decode` 接受 `IReadOnlyArray2d<byte>`（相容 Mutagen getter）|
| Util | `Vnml.cs` | VNML 法線計算：從 35×35 高度格（SampleCellExtended 輸出）以中心差分算切線，E×N cross product 得 Skyrim(X=東,Y=北,Z=上) 法線，encode `P3UInt8` **signed byte=round(n×127)**（無 +128 偏移；up=(0,0,127)）|
| Util | `Splatmap.cs` | 8-bit grayscale PNG → per-vertex alpha 網格（同 heightmap 網格約定，Y-flip）；`TrySampleCell(globalCellX,Y)` 切 33×33 alpha 0..1，cell 落在圖涵蓋範圍外回 false |
| Util | `Vtxt.cs` | ATXT+VTXT 純建構：33×33 alpha 格 + Quadrant → `AlphaLayer`{`LayerHeader`+稀疏`AlphaLayerData`(position/opacity)}；quadrant 切分（Bottom=南/低row、Left=西/低col）、position=localRow×17+localCol、0-indexed、空象限不生層。✅ LayerNumber/flags/texFormID 已對 vanilla byte-verify（2026-06-18 in-game OK）；⚠️ 僅 VTXT position 的 row/col 序待最終目視 |
| Seam | `Generator.Build.Worldspace.cs` | heightmap 迴圈內 **seam stitching**（VHGT）+ **VNML 重算**：每格 encode 後 decode 取 east/north edge 注入下格，`Vnml.Compute(SampleCellExtended)` 填法線（邊緣頂點帶 1px overlap 無需特判）|
| Util | `GodotPlacements.cs` | Godot placements format v1 / `godot4_y_up` JSON → `List<PlacementSpec>`；拒絕缺漏或不支援的 format version；座標換算（Z 翻轉、m→units）+ rotation rad→deg |
| Util | `SceneCoordinates.cs` | Pure transform API：source position/quaternion/scale3 → Skyrim position/Euler XYZ/scale；明確 Unity LH Y-up、Unreal LH Z-up 與 custom basis，完整 `B*R*B⁻¹`；非均勻 scale diagnostic |
| Tests | `SceneCoordinatesTests.cs` | identity、axis/handedness、basis rotation、unit/fudge scale、non-uniform diagnostic |
| Tests | `WorldspaceNavmeshGeometryTests.cs` / `WorldspaceNavmeshSeamTests.cs` | 自訂 NAVM 原樣寫入/明示 links；自訂幾何↔平面在 Z 容差 128 內雙向連接，超過時不連且警告 |
| Validate | `Generator.Validate.World.cs` | worldspace ref、boundary、climate ref |
| Validate | `Generator.Validate.World.More.cs` | region / encounter zone / outfit content ref |
| Diag | `Diagnostics.Worldspace.cs` | worldspace climate/water/map / cell grid / region overlay |
| Diag | `Diagnostics.Landscape.cs` | `landdiag <plugin> [ws] [n]` — dump LAND 紋理層（BTXT/ATXT quad+LayerNumber+tex、VTXT pts、Flags）對 vanilla byte-verify（Mutagen `AlphaLayer:BaseLayer` 故判型先 IAlphaLayerGetter）；`find <plugin> 0xFORMID` 反查 FormID 型別（`Diagnostics.cs`）|
| Tool | `TexExport.cs` (CLI) | `texexport <dataDir> <outDir> <master:0xLTEX>[,…]` — LTEX→TextureSet→diffuse .dds 從遊戲 BSA（`Archives`）抽出 → `magick` 轉 PNG；餵 Godot worldspace-editor 的 WYSIWYG 地形 shader |
| Tool | `NifExport.cs` (CLI) | `nifexport <dataDir> <outDir> <master:0xFORMID>[,…]` — 可放置 base（STAT/TREE/MSTT…）的 `IModeledGetter.Model.File`→model .nif 從 mesh BSA 抽出；Godot 端再走 `nif2gltf`（[../model-converter](../../../../model-converter/README.md)）轉 glTF 當 WYSIWYG 物件代理 |
| Tool | `TexExport.cs` `TexPath` (CLI) | `texpath <dataDir> <outDir> <texPath>[,…]` — 抽任意貼圖路徑（NIF shader 引用的 .dds）→ `<basename>.png`；給物件 glTF 上 diffuse 貼圖（nif2gltf 寫 `<stem>.textures.json` sidecar 列要抽的 dds，model_fetch 據此呼叫）|

---

## Leveled Lists 等級列表 + FormList（FLST）
→ **說明文件**：[SPEC-worldspaces.md § leveled lists & containers](../../../docs/spec/SPEC-worldspaces.md#leveled-lists--containers) · [SPEC-worldspaces.md § formLists](../../../docs/spec/SPEC-worldspaces.md#formlists--flst)

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
→ **說明文件**：[SPEC-worldspaces.md § leveled lists & containers](../../../docs/spec/SPEC-worldspaces.md#leveled-lists--containers)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `ContainerSpec`（model + items + counts）|
| Build P1 | `Generator.Build.Lists.cs` | container 基本欄位 + model (MODL) |
| Build P2 | `Generator.Build.Lists.Wire.cs` | container item FormLink 接線 |
| Validate | `Generator.Validate.Items.More.cs` | container contents ref |

---

## Recipes 合成配方（COBJ）
→ **說明文件**：[SPEC-items.md § recipes](../../../docs/spec/SPEC-items.md#recipes-crafting--cobj)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `RecipeSpec`（workbench / components / result）|
| Build | `Generator.Recipes.cs` | COBJ 建立（workbench dispatch + component 接線）|
| Validate | `Generator.Validate.Items.More.cs` | workbench keyword、component ref、output ref |
| Diag | `Diagnostics.Recipes.cs` | workbench / components / output dump |

---

## Encounter Zones 遭遇區域
→ **說明文件**：[SPEC-worldspaces.md § encounter zones](../../../docs/spec/SPEC-worldspaces.md#encounter-zones--leveled-actor-spawns--populating-an-area-with-scaled-enemies)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `EncounterZoneSpec`（level range / respawn / owner）|
| Build P1 | `Generator.Build.Lists.cs` | `BuildEncounterZones` 入口 |
| Build P2 | `Generator.Build.Lists.Wire.cs` | `WireEncounterZones` / `WireCellZones` |
| Validate | `Generator.Validate.World.More.cs` | encounter zone / owner ref |
| Diag | `Diagnostics.Encounters.cs` | level range / respawn / owner / location dump |

---

## Vendors 商人
→ **說明文件**：[SPEC-worldspaces.md § vendors](../../../docs/spec/SPEC-worldspaces.md#vendors--merchants--a-working-shopkeeper)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Actors.cs` | `VendorSpec`（hours / buy-stolen / sell-list / chest）|
| Build P2 | `Generator.Build.Vendor.cs` | vendor faction config + merchant container + JobMerchantFaction 指派 |
| Validate | `Generator.Validate.Npcs.cs` | faction ref、vendor chest ref |
| Diag | `Diagnostics.Factions.cs` | vendor config / crime data dump |
