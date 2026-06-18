# 決策與查證

← [README](README.md)

## 決策（已鎖定）

| 主題 | 決策 |
|---|---|
| 地形系統 | **Godot 4 自製 terrain**（ArrayMesh + 自寫高度/splat 筆刷 + 自寫 PNG codec）。原評估 HTerrain plugin，實作時棄用改自製 |
| Heightmap 格式 | 16-bit grayscale PNG，spec 寫路徑 |
| Heightmap 切割 | 單張大 PNG，ModForge 切；N 格寬 → PNG 寬 `N×32+1` px，相鄰格共用邊緣欄 → seam 零誤差 |
| 物件 metadata | 薄 script + `@export var skyrim_base`（base form ref）+ `@export var instance_id`（選填） |
| 座標轉換 | Godot 原生座標（公尺 + Y-up），ModForge 轉 game units + Z-up；換算唯一真相在 ModForge |
| 物件 scale | 鎖 uniform；Godot script 限制等比縮放，非等比取主軸並 warn |
| 物件預覽 | fo76utils / NifSkope 批量轉 glTF 作視覺代理 |
| 紋理 | 兩段：單層 `baseTexture`（全格 BTXT）+ 多層 `textureLayers`（每層 LTEX + 灰階 splatmap PNG → VTXT/ATXT alpha 層）。已實作 |
| Navmesh | MVP 跳過，只生 LAND |
| placements.json | header 包一層（`version` + `coordinate_system: "godot4_y_up"`），rotation 單位 radians |
| instanceId | 選填可省略（≠ 空字串）；省略 = 匿名 REFR；有值 = 此 REFR 的 editorId |
| 掛進 spec | `godotPlacements` 巢狀在 worldspace 節點下，ModForge 轉換後合流進 `placements[]` pipeline |

## 已查證

- ✅ **VHGT 編碼**：signed int8 delta、row-wise 累積、每 delta = 8 game units（Mutagen 0.53.1 + UESP + xEdit，2026-06-16）——詳見 [worldspace-editor-design.md](../../workflows/specs/worldspace-editor-design.md)
- ✅ **NIF → glTF**：fo76utils / NifSkope（Linux+Windows 雙平台）；`nif2gltf` Rust CLI 不存在（Gemini 捏造）——見 [`gemini-research/worldspace-editor/nif-gltf-conversion.md`](../gemini-research/worldspace-editor/nif-gltf-conversion.md)
