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
| `XMarkerTests.cs` | XMarker 放置（特殊 placement base）|
| `XMarkerKindTests.cs` | `kind:xmarker/xmarkerHeading` helper（空 base→0x3B/0x34 + persistent）+ `forced:` alias 解析到 xmarker 錨點 |
| `MapMarkerTests.cs` | mapMarker → MapMarker static + XMRK（type/flags）；持久 TopCell 加性帶上（⚠️ 需本機 Skyrim.esm）+ validate（type/flag）|
| `LightTests.cs` | 自訂 Light（LIGT）color/radius/fade/flags build + validate |
| `LightingTests.cs` | LGTM/IMGS build + CELL XCLL inherit + validate guardrails |

---

---

## Interior Cells 室內空間
→ **說明文件**：[SPEC-world.md § cells & placements](SPEC-world.md#cells--placements--putting-things-in-the-world)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `CellSpec`（name / template / encounterZone）|
| Build P1 | `Generator.Build.Cells.cs` | 建 interior cell record + encounter zone 連結 |
| Validate | `Generator.Validate.World.cs` | cell ref、encounter zone ref |

---

## Placements 放置（NPC / Object）
→ **說明文件**：[SPEC-world.md § cells & placements](SPEC-world.md#cells--placements--putting-things-in-the-world) · [engine-internals.md § Cell GRUP](engine-internals.md#cell-grup-placement-is-keyed-by-formidgrid)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `PlacementSpec`（含 `kind:"xmarker"/"xmarkerHeading"` helper）, `LinkedRefSpec`, `Vec3` |
| Spec | `Spec.MapMarkers.cs` | `MapMarkerSpec`（editorId/name/worldspace/position/`type`(MarkerType)/`flags`(Visible/CanTravelTo/ShowAllIsHidden)）|
| Build P2 | `Generator.Build.Placements.cs` | 室內/室外/vanilla-override 放置，position/rotation，persistent flag，cell 錨定；**`kind:xmarker/xmarkerHeading` → 空 base 自動填 `Skyrim.esm:0x3B`/`0x34` STAT + 強制 persistent**（quest-target 錨點）|
| Build P2 | `Generator.Build.MapMarkers.cs` | **`BuildMapMarkers`**：每筆 → MapMarker static（`0x10`）上的 `PlacedObject` + XMRK `MapMarker`(Name/Type/Flags)，放進 worldspace **持久 TopCell**（`WorldspacePersistentCell`），registered 進 formKeyByEd 故可被 `forced:` alias 抓 |
| Build P2 | `Generator.Build.PlacementRefs.cs` | linked-ref 對 + teleport-door XTEL 接線（deferred）|
| Build P2 | `Generator.Build.ExteriorCells.cs` | 室外 worldspace cell group tree（block/sub-block 按 grid 坐標）；**`WorldspaceOverride` 加性帶上 master 持久 TopCell（`CopyCellEnv`、不重述 vanilla ref）否則 vanilla 地圖標記全消失+大地圖空白**；**`WorldspacePersistentCell`** 回 worldspace 持久 cell 給地圖標記 |
| Validate | `Generator.Validate.World.cs` | linked-ref target、teleport pairs、worldspace boundary |
| Diag | `Diagnostics.Dump.World.cs` | placements / cells / linked-refs / navmesh dump |

---

## Lights 自訂光源（LIGT）
→ **說明文件**：[SPEC-world.md § lights](SPEC-world.md#lights--custom-light-sources-ligt)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Lights.cs` | `LightSpec`（editorId/name/color/radius/fadeValue/falloff/fov/flags/value/weight）|
| Build P1 | `Generator.Build.Lights.cs` | `BuildLights`（`mod.Lights.AddNew`；color R/G/B、radius/fade、`Light.Flag` 解析；BuildFormKeyTable 前建，故 placement 可按 editorId 放置）|
| Validate | `Generator.Validate.Lights.cs` | `ValidateLights`（flag 名合法、color 0..255、radius>0、editorId 唯一）|
| Diag | `Diagnostics.Records.cs` | `lightdiag`（radius/color/fade/flags dump）|

自訂 Light 是一般 base record——用既有 `placements[]`（base=light editorId）放進任意 cell，無需動 placement 程式碼。

---

## Lighting Templates + ImageSpaces 室內光照（LGTM / IMGS / CELL XCLL）
→ **說明文件**：[SPEC-world.md § lighting](SPEC-world.md#lighting)

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
→ **說明文件**：[SPEC-world.md § worldspaces & regions](SPEC-world.md#worldspaces-wrld--regions-regn--exterior-worlds--weather)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Worldspace.cs` | `WorldspaceSpec`, `WorldspaceCellSpec`, `WorldMapDataSpec`, `RegionSpec`, `RegionWeatherEntrySpec`, `PointSpec` |
| Build P1 | `Generator.Build.Worldspace.cs` | 建 worldspace record（climate/water/map bounds）+ cell grid 骨架 |
| Build P1 | `Generator.Build.Regions.cs` | 建 region record（polygon / weather table / priority / map color）|
| Build P2 | `Generator.Build.ExteriorCells.cs` | cell group tree 生成（外層結構）|
| Build P2 | `Generator.Build.Navmesh.cs` | NAVM 4 頂點平面 quad + NAVI 索引（[engine-internals § navmesh](engine-internals.md#programmatic-navmesh-navm--navi--in-game-confirmed-2026-06-03)）|
| Validate | `Generator.Validate.World.cs` | worldspace ref、boundary、climate ref |
| Validate | `Generator.Validate.World2.cs` | region / encounter zone / outfit content ref |
| Diag | `Diagnostics.Worldspace.cs` | worldspace climate/water/map / cell grid / region overlay |

---

## Leveled Lists 等級列表
→ **說明文件**：[SPEC-world.md § leveled lists & containers](SPEC-world.md#leveled-lists--containers)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `LeveledItemSpec`, `LeveledNpcSpec` |
| Build P1 | `Generator.Build.Lists.cs` | 建 leveled-item / leveled-NPC list record（chance-none flags）|
| Build P2 | `Generator.Build.Lists.Wire.cs` | entry FormLink 接線 |
| Validate | `Generator.Validate.Items.cs` | leveled list entries ref |

---

## Containers 容器
→ **說明文件**：[SPEC-world.md § leveled lists & containers](SPEC-world.md#leveled-lists--containers)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `ContainerSpec`（items + counts）|
| Build P2 | `Generator.Build.Lists.Wire.cs` | container item FormLink 接線 |
| Validate | `Generator.Validate.Items2.cs` | container contents ref |

---

## Recipes 合成配方（COBJ）
→ **說明文件**：[SPEC-items.md § recipes](SPEC-items.md#recipes-crafting--cobj)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `RecipeSpec`（workbench / components / result）|
| Build | `Generator.Recipes.cs` | COBJ 建立（workbench dispatch + component 接線）|
| Validate | `Generator.Validate.Items2.cs` | workbench keyword、component ref、output ref |
| Diag | `Diagnostics.Recipes.cs` | workbench / components / output dump |

---

## Encounter Zones 遭遇區域
→ **說明文件**：[SPEC-world.md § encounter zones](SPEC-world.md#encounter-zones--leveled-actor-spawns--populating-an-area-with-scaled-enemies)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.World.cs` | `EncounterZoneSpec`（level range / respawn / owner）|
| Build P1 | `Generator.Build.Lists.cs` | `BuildEncounterZones` 入口 |
| Build P2 | `Generator.Build.Lists.Wire.cs` | `WireEncounterZones` / `WireCellZones` |
| Validate | `Generator.Validate.World2.cs` | encounter zone / owner ref |
| Diag | `Diagnostics.Encounters.cs` | level range / respawn / owner / location dump |

---

## Vendors 商人
→ **說明文件**：[SPEC-world.md § vendors](SPEC-world.md#vendors--merchants--a-working-shopkeeper)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Actors.cs` | `VendorSpec`（hours / buy-stolen / sell-list / chest）|
| Build P2 | `Generator.Build.Vendor.cs` | vendor faction config + merchant container + JobMerchantFaction 指派 |
| Validate | `Generator.Validate.Npcs.cs` | faction ref、vendor chest ref |
| Diag | `Diagnostics.Factions.cs` | vendor config / crime data dump |
