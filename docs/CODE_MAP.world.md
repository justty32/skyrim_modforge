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
| Spec | `Spec.World.cs` | `PlacementSpec`, `LinkedRefSpec`, `Vec3` |
| Build P2 | `Generator.Build.Placements.cs` | 室內/室外/vanilla-override 放置，position/rotation，persistent flag，cell 錨定 |
| Build P2 | `Generator.Build.PlacementRefs.cs` | linked-ref 對 + teleport-door XTEL 接線（deferred）|
| Build P2 | `Generator.Build.ExteriorCells.cs` | 室外 worldspace cell group tree（block/sub-block 按 grid 坐標）|
| Validate | `Generator.Validate.World.cs` | linked-ref target、teleport pairs、worldspace boundary |
| Diag | `Diagnostics.Dump.World.cs` | placements / cells / linked-refs / navmesh dump |

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
