# Godot Worldspace Editor

← [idea #19](../../workflows/idea/ideas.md)｜附屬：[座標系](coord-system.md)　[placements 格式](placements-format.md)

用 **Godot 4**（**自製 terrain**，不靠 HTerrain plugin）離線做地形編輯 → 匯出 → ModForge 生 LAND/REFR → 進遊戲微調。定位是 **Creation Kit 地形/場景編輯的替代前端**（CK 在 Wine/Proton 不穩、難腳本化）。

前期作為 sub_proj（輸出 JSON spec 對接 ModForge）→ 體量大了再移成獨立 repo，對接方式不變。

---

## 三階段 pipeline

1. **Godot 粗畫**（本 sub_proj）：刷高度 + 刷紋理 + 散佈物件 → 匯出 PNG heightmap + splatmap PNG + `placements.json`
2. **ModForge 生成**：讀 heightmap → 切 cell → LAND（VHGT/VNML）；讀 splatmap → BTXT/VTXT 紋理層；讀 placements → REFR → ESP + BSA
3. **遊戲內微調**（獨立 C++/SKSE repo）：SkyrimIngameEditor 細調，不在 ModForge 範圍

---

## MVP scope（已達成並超出）

原始 MVP＝**單格 PNG → LAND → COW 進入、地形有起伏**。現況已遠超：**多格 heightmap**（VHGT/VNML/seam）、**物件擺放 → REFR**、**地形紋理**（單層 baseTexture + 多層 splatmap → BTXT/VTXT）整鏈離線完成；剩主力機驗收（Godot GUI 實跑、xEdit byte-verify）與 box proxy 換真實 glTF。

---

## 資料流

```
Godot（自製 terrain）               ModForge
  HeightMap  → terrain.png ─────┐
  SplatMap   → splatmap.png ────┼──→ 按 cell grid 切割（每 cell 4096 units）
  物件 Node3D → placements.json ┘     → 每 cell 採樣 33×33 → VHGT + 重算 VNML → LAND
  （editorID + transform）            → splatmap → BTXT/VTXT 紋理層
                                      → placements → REFR → ESP + BSA
```

**已知難點**：
- **Cell seam**：高度已靠單張 PNG 共用邊緣欄自動對齊；殘留風險是 **VNML 法線**——邊緣頂點重算法線需參考鄰格高度，切割後要保留 1px overlap
- **NIF 預覽**：Godot 不讀 NIF，靠 glTF proxy（fo76utils / NifSkope 批量轉換前置，Linux+Windows 雙平台可用）
- **Navmesh**：複雜地形 flat-quad 會卡 NPC，精確 navmesh 後排

---

## 決策（已鎖定）

| 主題 | 決策 |
|---|---|
| 地形系統 | **Godot 4 自製 terrain**（ArrayMesh + 自寫高度/splat 筆刷 + 自寫 PNG codec）。原評估 HTerrain plugin，實作時棄用改自製（見下方 landed） |
| Heightmap 格式 | 16-bit grayscale PNG，spec 寫路徑 |
| Heightmap 切割 | 單張大 PNG，ModForge 切；N 格寬 → PNG 寬 `N×32+1` px，相鄰格共用邊緣欄 → seam 零誤差 |
| 物件 metadata | 薄 script + `@export var skyrim_base`（base form ref）+ `@export var instance_id`（選填） |
| 座標轉換 | Godot 原生座標（公尺 + Y-up），ModForge 轉 game units + Z-up；換算唯一真相在 ModForge |
| 物件 scale | 鎖 uniform；Godot script 限制等比縮放，非等比取主軸並 warn |
| 物件預覽 | fo76utils / NifSkope 批量轉 glTF 作視覺代理 |
| 紋理 | 兩段：單層 `baseTexture`（全格 BTXT）+ 多層 `textureLayers`（每層 LTEX + 灰階 splatmap PNG → VTXT/ATXT alpha 層）。已實作（見下方 landed） |
| Navmesh | MVP 跳過，只生 LAND |
| placements.json | header 包一層（`version` + `coordinate_system: "godot4_y_up"`），rotation 單位 radians |
| instanceId | 選填可省略（≠ 空字串）；省略 = 匿名 REFR；有值 = 此 REFR 的 editorId |
| 掛進 spec | `godotPlacements` 巢狀在 worldspace 節點下，ModForge 轉換後合流進 `placements[]` pipeline |

---

## ModForge 後端對接（全部已實作）

對接細節真相在 [SPEC-worldspaces](../../docs/spec/SPEC-worldspaces.md) + [CODE_MAP.world](../../workflows/common/code-map/CODE_MAP.world.md)；下表只留「哪個前端輸出 → 哪個後端入口」的對照。

| 前端輸出 | 後端入口 | 狀態 |
|---|---|---|
| heightmap PNG | spec `heightmap` → `Heightmap.cs` → `Vhgt.Encode`（33×33 row-wise signed-int8 delta）+ seam stitching | ✅ |
| — | `Vnml.Compute`（35×35 中心差分重算法線，1px overlap） | ✅ |
| splatmap PNG | spec `baseTexture`（BTXT）/ `textureLayers[].splatmap`（`Splatmap.cs`→`Vtxt.cs` ATXT/VTXT） | ✅ 待 xEdit byte-verify |
| `placements.json` | spec `godotPlacements` → `GodotPlacements.cs`（`godot4_y_up`→Skyrim 換算）→ 合流 `placements[]` | ✅ |

平坦地形（`WorldspaceCellSpec.Height` → VHGT 全格同高）、SubCells block tree、flat-quad navmesh、水位預設皆已有；**不需動**：水位 / REGN / navmesh（暫夠）。

---

## 已查證

- ✅ **VHGT 編碼**：signed int8 delta、row-wise 累積、每 delta = 8 game units（Mutagen 0.53.1 + UESP + xEdit，2026-06-16）——詳見 [worldspace-editor-design.md](../../workflows/specs/worldspace-editor-design.md)
- ✅ **NIF → glTF**：fo76utils / NifSkope（Linux+Windows 雙平台）；`nif2gltf` Rust CLI 不存在（Gemini 捏造）——見 [`gemini-research/worldspace-editor/nif-gltf-conversion.md`](../gemini-research/worldspace-editor/nif-gltf-conversion.md)

---

## 前端結構（`godot/`）

所有 `.gd` 維持 ~100 行，分層拆檔：

| 檔 | 職責 |
|---|---|
| `main.gd` | 根場景：組裝、輸入派發（height brush / Place / Splat 三模式互斥路由）、editor/walk 模式切換、display 同步 |
| `terrain.gd` | 高度資料 + 座標換算 + 碰撞 body（`TerrainGrid`） |
| `terrain_mesh.gd` | 從高度建 ArrayMesh（vertex/normal/uv + 高度漸層頂點色 + active splat 層 alpha overlay blend） |
| `terrain_brush.gd` | 4 brush 模式（raise/lower/flatten/smooth） |
| `camera_rig.gd` | orbit 編輯相機（middle orbit / scroll zoom / right pan） |
| `player_controller.gd` | Walk Mode 人形 `CharacterBody3D`（第一人稱 + WASD/跳/ESC） |
| `scene_builder.gd` | env / 編輯相機 / cursor / 格線 工廠 |
| `world_ui.gd` | 側欄（ScrollContainer + slider/spinbox + 模式/筆刷/匯出按鈕） |
| `io_dialog.gd` | 高度 PNG export/import FileDialog |
| `png16.gd` / `png16_codec.gd` | 16-bit PNG encode/decode + chunk/CRC |
| `placement.gd` | `PlacedObject` 薄節點：metadata（base/instanceId/uniform_scale）+ box proxy 視覺 |
| `placement_tool.gd` | `PlacementTool`：placement 筆狀態 + 物件 list（place 吸地表 / restore / undo / clear）|
| `placement_ui.gd` | 側欄 PLACEMENT 段（mode 切換、base/instance 欄、rotationY/scale、count、JSON I/O 按鈕）|
| `placements_io.gd` | `placements.json` 匯出/匯入（顯示 scale 除掉還原 canonical 公尺）|
| `splat_tool.gd` | `SplatTool`：紋理 alpha 筆（多層，每層 LTEX ref + alpha 格；paint 吸 active 層、radius/strength/erase 帶 falloff）+ 推 overlay 給地形上色 |
| `splat_ui.gd` | 側欄 TEXTURE 段（Splat Mode 切換、層選擇+新增、LTEX ref、Paint/Erase、radius/strength、清層、splatmap PNG I/O）|
| `splatmap_io.gd` | splatmap 8-bit 灰階 PNG 匯出/匯入（Y-flip 頂=北，同 Png16/Heightmap 約定）+ 印出可貼進 spec 的 `textureLayers` 片段 |

**顯示縮放**：`vis_height_scale`（Y）與 `vis_surface_scale`（X/Z）只影響顯示，資料恆為 game units；`Y=(h-min)·MPU·scale` 讓地板固定 Y=0。**高度著色**：以中間高度為基準，下沉→淺藍→深藍（水），上升→草綠→岩石→雪。

## Open

- **box proxy → 真實 glTF**：目前擺放代理是彩色方塊（不擋擺放/匯出鏈）。換真實外觀需 vanilla `.nif` → glTF 視覺代理 → 收斂到 [model-converter](../model-converter/README.md)（nif→glTF，批量 pipeline 待主力機驗）。

> 主力機驗收項（Godot GUI 實跑、xEdit byte-verify）列在 [WAIT_USER](../../WAIT_USER.md)，不擋離線開發。

~~紋理 B 路線（單層 BTXT + 多層 VTXT splatmap + 前端 splat 筆刷）~~ ✅ 2026-06-17（離線實作，**待主力機 Godot GUI + xEdit byte-verify**）：① spec `worldspace.baseTexture` → 每格四象限 BTXT base 層（測 `WorldspaceBaseTextureTests`）；② spec `worldspace.textureLayers`（LTEX + 灰階 splatmap PNG）→ 每格四象限稀疏 ATXT+VTXT alpha 層（`Splatmap.cs`/`Vtxt.cs`，測 `WorldspaceSplatmapTests`）；③ Godot 前端 Splat Mode 多層 alpha 筆 + active 層即時上色 + 8-bit 灰階 splatmap PNG 匯出（含可貼 spec 片段）：`splat_tool.gd`/`splat_ui.gd`/`splatmap_io.gd` + `terrain`/`terrain_mesh` overlay + `main.gd` 路由（與 Place Mode 互斥）。PNG Y-flip/網格約定前後端一致。

~~物件擺放（Godot 前端）~~ ✅ 2026-06-17（離線實作，**待主力機 Godot GUI 跑一次**）：Place Mode 切換 + placement 筆（base ref / instanceId / rotationY / scale）+ box proxy（hash 配色，Y 吸地表）+ `placements.json` 匯出/匯入。檔：`placement.gd`（PlacedObject 薄節點）/ `placement_tool.gd`（list + place/undo/clear）/ `placements_io.gd`（JSON I/O，顯示 scale 除掉還原 canonical 公尺）/ `placement_ui.gd`（側欄 PLACEMENT 段）；`terrain.gd` 加 `world_to_canonical_meters`/`canonical_meters_to_world`/`surface_display_y`；`main.gd` 接 Place Mode 輸入路由。**離線已核對**前端輸出欄位 + 座標換算與後端 `GodotPlacements.cs` 逐欄一致（round-trip 自洽）。

~~godotPlacements 讀取（後端）~~ ✅ 2026-06-16（`GodotPlacements.cs` + test：解 JSON、godot4_y_up→Skyrim 座標換算、rad→deg、合流 `placements[]`，已接進 `Generator.Build.Worldspace.cs:198`）
~~Godot 前端骨架~~ ✅ 2026-06-16（自製 terrain，不靠 HTerrain）
~~前端拆檔 + display scale + Walk Mode + 高度著色~~ ✅ 2026-06-16
~~Godot 專案開啟測試~~ ✅ 2026-06-16（使用者主力機驗證：terrain/筆刷/PNG 匯出可用）
~~VNML 重算（後端）~~ ✅ 2026-06-16（`Vnml.cs` + `SampleCellExtended`，35×35 中心差分）
~~待主力機收尾：Tamriel VHGT 反解比對~~ ✅ 2026-06-16（20 格 delta 完全一致）
~~VNML axis/編碼驗證~~ ✅ 2026-06-16（對 vanilla Tamriel VNML 逐 byte 比對，修了 3 bug：轉置 / StepUnits 8→128 / signed-byte up=(0,0,127)；見 [landed/world.md](../../workflows/feature-dev/landed/world.md)）
