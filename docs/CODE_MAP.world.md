# CODE_MAP — 世界・放置・地區・等級列表・遭遇區域

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：interior cells、exterior placements、worldspaces、regions、leveled lists、containers、recipes、encounter zones、vendors。

---

## 1. Spec（資料定義）

| 檔案 | 主要型別 |
|-----|---------|
| `src/ModForge.Core/Spec.World.cs` | `CellSpec`, `Vec3`, `PlacementSpec`, `LinkedRefSpec`, `EncounterZoneSpec` |
| `src/ModForge.Core/Spec.Worldspace.cs` | `WorldspaceSpec`, `WorldspaceCellSpec`, `WorldMapDataSpec`, `RegionSpec`, `RegionWeatherEntrySpec`, `PointSpec` |
| `src/ModForge.Core/Spec.Items.cs` | `LeveledItemSpec`, `LeveledNpcSpec`, `ContainerSpec`, `RecipeSpec`（含 workbench/components）|

---

## 2. Build Pass 1

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Generator.Build.Cells.cs` | 建 interior cell record + encounter zone 連結 |
| `src/ModForge.Core/Generator.Build.Worldspace.cs` | 建 worldspace record（climate/water/map bounds）+ cell grid 骨架 + flat navmesh 初始化 |
| `src/ModForge.Core/Generator.Build.Regions.cs` | 建 region record（polygon/weather table/priority/map color）|
| `src/ModForge.Core/Generator.Build.Lists.cs` | 建 leveled-item / leveled-NPC list record（chance-none flags）|
| `src/ModForge.Core/Generator.Build.LongTail.cs` | 建 static / activator / furniture（作為 placement base）|

## 3. Build Pass 2

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Generator.Build.Placements.cs` | 室內/室外/vanilla-override 放置，position/rotation，persistent flag，cell 錨定 |
| `src/ModForge.Core/Generator.Build.PlacementRefs.cs` | linked-ref 對 + teleport-door XTEL 接線（deferred target/location）|
| `src/ModForge.Core/Generator.Build.ExteriorCells.cs` | 室外 worldspace cell group tree（block/sub-block 嵌套按 grid 坐標）|
| `src/ModForge.Core/Generator.Build.Navmesh.cs` | NAVM 生成（4 頂點平面 quad per cell）+ NAVI 索引 |
| `src/ModForge.Core/Generator.Build.Lists.Wire.cs` | leveled-item / leveled-NPC entry FormLink 接線 |
| `src/ModForge.Core/Generator.Build.Vendor.cs` | vendor faction 設定（hours/buy-stolen/sell-list/merchant container）+ JobMerchantFaction 指派 |
| `src/ModForge.Core/Generator.Recipes.cs` | COBJ crafting recipe（workbench dispatch + component 接線）|

---

## 4. Validate

| 檔案 | 檢查什麼 |
|-----|---------|
| `src/ModForge.Core/Generator.Validate.World.cs` | cell ref、leveled-list base、worldspace ref、linked-ref target、teleport pairs、worldspace boundary |
| `src/ModForge.Core/Generator.Validate.World2.cs` | encounter zone / region / outfit content ref；multi-stage placement constraint |
| `src/ModForge.Core/Generator.Validate.Items.cs` | leveled list entries、container contents、recipe workbench/components |

---

## 5. Diagnostics

| 檔案 | dump 哪些 |
|-----|---------|
| `src/ModForge.Cli/Diagnostics.Dump.World.cs` | placements / cells / linked-refs / encounter zones / worldspace terrain / navmesh |
| `src/ModForge.Cli/Diagnostics.Worldspace.cs` | worldspace climate/water/map bounds / cell grid / region overlay |
| `src/ModForge.Cli/Diagnostics.Encounters.cs` | encounter zone level range / respawn / owner / location |
| `src/ModForge.Cli/Diagnostics.Recipes.cs` | workbench / input components / output item / condition gates |

---

## 6. Docs

| 連結 | 內容 |
|-----|-----|
| `docs/SPEC-world.md` | 完整 spec 欄位參考（EN）|
| `docs/zh-TW/SPEC-world.md` | 完整 spec 欄位參考（zh-TW）|
