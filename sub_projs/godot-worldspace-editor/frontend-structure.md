# 前端結構（`godot/`）

← [README](README.md)

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
