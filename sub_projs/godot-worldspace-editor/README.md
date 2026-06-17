# Godot Worldspace Editor

← [idea #19](../../workflows/idea/ideas.md)｜附屬：[座標系](coord-system.md)　[placements 格式](placements-format.md)

用 **Godot 4 + HTerrain** 離線做地形編輯 → 匯出 → ModForge 生 LAND/REFR → 進遊戲微調。定位是 **Creation Kit 地形/場景編輯的替代前端**（CK 在 Wine/Proton 不穩、難腳本化）。

前期作為 sub_proj（輸出 JSON spec 對接 ModForge）→ 體量大了再移成獨立 repo，對接方式不變。

---

## 三階段 pipeline

1. **Godot 粗畫**（本 sub_proj）：HTerrain 刷高度 + 散佈物件 → 匯出 PNG heightmap + `placements.json`
2. **ModForge 生成**：讀 PNG → 切 cell → LAND records；讀 placements → REFR → ESP + BSA
3. **遊戲內微調**（獨立 C++/SKSE repo）：SkyrimIngameEditor 細調，不在 ModForge 範圍

---

## MVP scope

**單格 PNG → LAND → COW 進入、地形有起伏**；物件擺放不含。

---

## 資料流

```
Godot（HTerrain）                    ModForge
  HeightMap  → terrain.png ─────┐
  SplatMap   → splatmap.png ────┼──→ 按 cell grid 切割（每 cell 4096 units）
  物件 Node3D → placements.json ┘     → 每 cell 採樣 33×33 → VHGT + 重算 VNML → LAND
  （editorID + transform）            → placements → REFR
                                      → ESP + BSA
```

**已知難點**：
- **Cell seam**：高度已靠單張 PNG 共用邊緣欄自動對齊；殘留風險是 **VNML 法線**——邊緣頂點重算法線需參考鄰格高度，切割後要保留 1px overlap
- **NIF 預覽**：Godot 不讀 NIF，靠 glTF proxy（fo76utils / NifSkope 批量轉換前置，Linux+Windows 雙平台可用）
- **Navmesh**：複雜地形 flat-quad 會卡 NPC，精確 navmesh 後排

---

## 決策（已鎖定）

| 主題 | 決策 |
|---|---|
| 地形系統 | Godot 4 + HTerrain plugin（內建筆刷 + PNG 匯出） |
| Heightmap 格式 | 16-bit grayscale PNG，spec 寫路徑 |
| Heightmap 切割 | 單張大 PNG，ModForge 切；N 格寬 → PNG 寬 `N×32+1` px，相鄰格共用邊緣欄 → seam 零誤差 |
| 物件 metadata | 薄 script + `@export var skyrim_base`（base form ref）+ `@export var instance_id`（選填） |
| 座標轉換 | Godot 原生座標（公尺 + Y-up），ModForge 轉 game units + Z-up；換算唯一真相在 ModForge |
| 物件 scale | 鎖 uniform；Godot script 限制等比縮放，非等比取主軸並 warn |
| 物件預覽 | fo76utils / NifSkope 批量轉 glTF 作視覺代理 |
| VTXT 紋理 | 全格預設 dirt → 後期 splatmap 映射 |
| Navmesh | MVP 跳過，只生 LAND |
| placements.json | header 包一層（`version` + `coordinate_system: "godot4_y_up"`），rotation 單位 radians |
| instanceId | 選填可省略（≠ 空字串）；省略 = 匿名 REFR；有值 = 此 REFR 的 editorId |
| 掛進 spec | `godotPlacements` 巢狀在 worldspace 節點下，ModForge 轉換後合流進 `placements[]` pipeline |

---

## ModForge 後端需新增

**已有（平坦地形）**：`WorldspaceCellSpec.Height` → VHGT 全格同高；SubCells block tree、flat-quad navmesh、水位預設。**不需動**：水位 / REGN / navmesh（暫夠）。

| 項目 | 說明 |
|---|---|
| spec `heightmap` 欄位 | `{ "path": "terrain.png", "originX": 0, "originY": 0, "minHeight": 0, "maxHeight": 8192 }` |
| PNG → VHGT | 讀 PNG，採樣每 cell 33×33，row-wise delta 編碼（signed int8，×8 game units） |
| VNML 重算 | 從高度差算法線；邊緣頂點須 1px overlap 參考鄰格高度 |
| `godotPlacements` 讀取 | 解 JSON，座標換算（`godot4_y_up` → Skyrim），合流進 `placements[]` |
| Cell seam 驗證 | 相鄰 cell 邊緣行需一致（PNG 共用欄設計已自動保證，實作時驗證） |

---

## 已查證

- ✅ **VHGT 編碼**：signed int8 delta、row-wise 累積、每 delta = 8 game units（Mutagen 0.53.1 + UESP + xEdit，2026-06-16）——詳見 [worldspace-editor-design.md](../../workflows/specs/worldspace-editor-design.md)
- ✅ **NIF → glTF**：fo76utils / NifSkope（Linux+Windows 雙平台）；`nif2gltf` Rust CLI 不存在（Gemini 捏造）——見 [`gemini-research/worldspace-editor/nif-gltf-conversion.md`](../gemini-research/worldspace-editor/nif-gltf-conversion.md)

---

## 前端結構（`godot/`）

所有 `.gd` 維持 ~100 行，分層拆檔：

| 檔 | 職責 |
|---|---|
| `main.gd` | 根場景：組裝、輸入派發、editor/walk 模式切換、display 同步 |
| `terrain.gd` | 高度資料 + 座標換算 + 碰撞 body（`TerrainGrid`） |
| `terrain_mesh.gd` | 從高度建 ArrayMesh（vertex/normal/uv + 高度漸層頂點色） |
| `terrain_brush.gd` | 4 brush 模式（raise/lower/flatten/smooth） |
| `camera_rig.gd` | orbit 編輯相機（middle orbit / scroll zoom / right pan） |
| `player_controller.gd` | Walk Mode 人形 `CharacterBody3D`（第一人稱 + WASD/跳/ESC） |
| `scene_builder.gd` | env / 編輯相機 / cursor / 格線 工廠 |
| `world_ui.gd` | 側欄（ScrollContainer + slider/spinbox + 模式/筆刷/匯出按鈕） |
| `io_dialog.gd` | export/import FileDialog |
| `png16.gd` / `png16_codec.gd` | 16-bit PNG encode/decode + chunk/CRC |

**顯示縮放**：`vis_height_scale`（Y）與 `vis_surface_scale`（X/Z）只影響顯示，資料恆為 game units；`Y=(h-min)·MPU·scale` 讓地板固定 Y=0。**高度著色**：以中間高度為基準，下沉→淺藍→深藍（水），上升→草綠→岩石→雪。

## Open

- **物件擺放（Godot 前端）**：放置物件 UI + `@export skyrim_base` 薄 script + 匯出 `placements.json`。**後端已 ready 等這個檔**——前端只能匯出高度 PNG，產不出 placements，所以「Godot 擺物件 → 進遊戲」這半條鏈還串不起來。
  - **前置依賴**：物件要能在 Godot 裡看到，需 vanilla `.nif` → glTF 視覺代理 → 此能力收斂到 [model-converter](../model-converter/README.md) sub_proj（nif→glTF 反向轉換，目前無已驗證批量 pipeline）。
- **紋理圖（splatmap → VTXT）**：前後端皆缺。前端無 splat 筆刷（目前全格預設 dirt）、後端無 VTXT/ATXT 紋理層生成。地形可走但只有單一土質貼圖。見下方「資料流」的 SplatMap 規劃（尚未實作）。

~~godotPlacements 讀取（後端）~~ ✅ 2026-06-16（`GodotPlacements.cs` + test：解 JSON、godot4_y_up→Skyrim 座標換算、rad→deg、合流 `placements[]`，已接進 `Generator.Build.Worldspace.cs:198`）
~~Godot 前端骨架~~ ✅ 2026-06-16（自製 terrain，不靠 HTerrain）
~~前端拆檔 + display scale + Walk Mode + 高度著色~~ ✅ 2026-06-16
~~Godot 專案開啟測試~~ ✅ 2026-06-16（使用者主力機驗證：terrain/筆刷/PNG 匯出可用）
~~VNML 重算（後端）~~ ✅ 2026-06-16（`Vnml.cs` + `SampleCellExtended`，35×35 中心差分）
~~待主力機收尾：Tamriel VHGT 反解比對~~ ✅ 2026-06-16（20 格 delta 完全一致）
~~VNML axis/編碼驗證~~ ✅ 2026-06-16（對 vanilla Tamriel VNML 逐 byte 比對，修了 3 bug：轉置 / StepUnits 8→128 / signed-byte up=(0,0,127)；見 [landed/world.md](../../workflows/feature-dev/landed/world.md)）
