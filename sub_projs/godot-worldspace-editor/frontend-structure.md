# 前端結構（`godot/`）

← [README](README.md)

所有 `.gd` 維持 ~100 行，分層拆檔：

| 檔 | 職責 |
|---|---|
| `main.gd` | 根場景 `WorldspaceEditor`：組裝、UI setup、display 同步、export/import + 模式切換 callback；輸入/walk 委派下兩檔 |
| `editor_input.gd` | `EditorInput`（static，吃 `WorldspaceEditor`）：鍵盤筆刷熱鍵、LMB place/paint 派發、逐幀 cursor + 高度讀數 + paint tick |
| `walk_mode.gd` | `WalkMode`（static）：第一人稱走地形預覽 enter/exit（生 PlayerController、暫停編輯輸入） |
| `terrain.gd` | 高度資料 + 碰撞 body（`TerrainGrid`）；座標換算委派 `terrain_coords` |
| `terrain_coords.gd` | `TerrainCoords`（static）：Skyrim↔Godot↔display↔vertex 座標換算 + 相機 ray 取點（純函式） |
| `terrain_mesh.gd` | 從高度建 ArrayMesh（vertex/normal/uv + 高度漸層頂點色 + active splat 層 alpha overlay blend） |
| `terrain_brush.gd` | 4 brush 模式（raise/lower/flatten/smooth） |
| `camera_rig.gd` | orbit 編輯相機（middle orbit / scroll zoom / right pan） |
| `player_controller.gd` | Walk Mode 人形 `CharacterBody3D`（第一人稱 + WASD/跳/ESC） |
| `scene_builder.gd` | env / 編輯相機 / cursor / 格線 工廠 |
| `world_ui.gd` | 側欄（ScrollContainer + slider/spinbox + 模式/筆刷/匯出按鈕） |
| `io_dialog.gd` | 高度 PNG export/import FileDialog |
| `png16.gd` / `png16_codec.gd` | 16-bit PNG encode/decode + chunk/CRC |
| `placement.gd` | `PlacedObject` 薄節點：metadata（base/instanceId/uniform_scale）+ box proxy 視覺；`set_model` 換成真實 glTF 模型（game-unit×METERS_PER_UNIT→顯示公尺，box 當 fallback）|
| `model_fetch.gd` | `ModelFetch`：base ref → 真實模型節點。兩段：CLI `nifexport`（抽 model .nif）→ `nif2gltf`（venv，`env PYTHONPATH=`）轉 glTF，快取 res://modelcache/，`GLTFDocument` runtime 載入。主力機限定，抽不到→留 box |
| `placement_tool.gd` | `PlacementTool`：placement 筆狀態 + 物件 list（place 吸地表 / restore / undo / clear）|
| `placement_ui.gd` | 側欄 PLACEMENT 段（mode 切換、base/instance 欄、rotationY/scale、count、JSON I/O 按鈕）|
| `placements_io.gd` | `placements.json` 匯出/匯入（顯示 scale 除掉還原 canonical 公尺）|
| `splat_tool.gd` | `SplatTool`：紋理 alpha 筆 — 層模型（多層 LTEX ref + alpha 格）+ paint（active 層、radius/strength/erase falloff）+ base 貼圖；地形視覺委派 `splat_render` |
| `splat_render.gd` | `SplatRender`（static，吃 `SplatTool`）：層資料 → 地形視覺橋——WYSIWYG 真實貼圖混合（`TerrainMaterial`）或頂點色 tint fallback |
| `splat_ui.gd` | 側欄 TEXTURE 段（Splat Mode 切換、base 貼圖欄、層選擇+新增、LTEX ref〔Enter 抓圖〕、Paint/Erase、radius/strength、清層、Load real textures、splatmap PNG I/O）|
| `splatmap_io.gd` | splatmap 8-bit 灰階 PNG 匯出/匯入（Y-flip 頂=北，同 Png16/Heightmap 約定）+ 印出可貼進 spec 的 `textureLayers` 片段 |
| `tex_fetch.gd` | `TexFetch`：LTEX ref → 真實貼圖。OS.execute 呼叫 ModForge CLI `texexport`（LTEX→diffuse .dds 從遊戲 BSA→PNG），快取 res://texcache/；CLI 路徑由 repo 結構推導、Data dir 自動偵測（可 texconfig.json 覆寫）。主力機限定，抽不到→退回 tint |
| `terrain_material.gd` | `TerrainMaterial`：地形 WYSIWYG ShaderMaterial — tiled base 貼圖 + 至多 4 層 alpha 混合（alpha 格→R-float 貼圖，UV 取樣）；無真實貼圖時 fallback 頂點高度漸層色 |

**顯示縮放**：`vis_height_scale`（Y）與 `vis_surface_scale`（X/Z）只影響顯示，資料恆為 game units；`Y=(h-min)·MPU·scale` 讓地板固定 Y=0。**高度著色**：以中間高度為基準，下沉→淺藍→深藍（水），上升→草綠→岩石→雪。
