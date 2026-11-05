# CODE_MAP.world — Examples・Tests

← [CODE_MAP.world](../CODE_MAP.world.md)

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
| `PlacementSpecFieldsTests.cs` | Scale(XSCL) / InitiallyDisabled(flag) / **FullLod(flag 0x10000, REFR only)** / **NoHavokSettle(flag 0x20000000)** / EnableParent(XESP) / Lock(XLOC) / Ownership(XOWN) / Count(XCNT) build + validate |
| `LightTests.cs` | 自訂 Light（LIGT）color/radius/fade/flags build + validate |
| `LightingTests.cs` | LGTM/IMGS build + CELL XCLL inherit + validate guardrails |
| `CellWaterTests.cs` | interior 與 own-worldspace exterior CELL 的 `waterHeight` (XCLW) / `water` (XCWT) / `acousticSpace` (XCAS) build + ref validate；template 與 spec 都有水位時由 spec 覆寫 |
| `SkillTreeTests.cs` | `skillTrees:` macro-expansion（points/rank GLOB、node+line ACTI、垂直堆疊 placement、line 中點+rot+scale、gating 鏈 prereq/downLine、root 無 prereq、idempotent guard）+ build（temp refs、node 掛 MFSkillNode）+ validate（id 唯一/cell/name/ability 必填）|
| `SettlementTests.cs` | `settlements:` macro-expansion（ACHR spawn 座標/fallback、Sleep/Work/Wander package + wrap-midnight 時長 + 錨點、npc.Packages 排序、routine 覆寫、auto/explicit faction、crimeFaction、vendor FACT/chest/gold、friendlyResidents RELA、idempotent）+ build（**Sleep location 解析到 in-spec 床錨**的 deferred 修回歸測）+ validate（npc 非 in-spec、缺 spawn、sleep 無 home、未知錨、重複住民、vendor 時數、缺 cell/residents）|
