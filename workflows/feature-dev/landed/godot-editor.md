# 已落地 — Godot worldspace 編輯器 + model-converter（子專案工具）

← [landed index](README.md)｜durable 見 [godot-worldspace-editor](../../../sub_projs/godot-worldspace-editor/README.md) / [model-converter](../../../sub_projs/model-converter/README.md)

這裡收 Idea #19 Godot worldspace 編輯器與其 nif→glTF 資產轉換器的落地記錄（編輯器工具，非 ModForge 生成的 world record；生成的 world record 見 [world.md](world.md)）。

**地形紋理 + Godot placement 整鏈**（in-game 確認 2026-06-18，`GodotEditorDemo.zip`）：Godot worldspace editor（`sub_projs/godot-worldspace-editor/`）刷高度+紋理+擺物件 → 匯出 heightmap PNG / splatmap PNG / `placements.json` → ModForge build LAND（VHGT/VNML）+ BTXT/ATXT/VTXT 紋理層 + REFR，**地形/紋理/物件實機全顯示**。**LAND 紋理三鐵律（byte-verified vs vanilla Tamriel，初版三項全錯過）**：① `LAND.Flags` 必含 `Layers`(0x04)，否則引擎跳過所有紋理層＝無紋理；② BTXT base 的 `LayerNumber`=`0xFFFF`(-1) 非 0；③ ATXT alpha 層 0-indexed（非接續 base）。診斷 `landdiag <plugin> [ws] [n]`（dump 每層 quad/layer/tex + VTXT 點數，對 vanilla 比；Mutagen `AlphaLayer:BaseLayer` 故判型先 IAlphaLayerGetter）。placement 的 `base` 必須是 base FORM（STAT/TREE…）**不能是 REFR**（填 REFR=隱形物件）；`find <plugin> 0xFORMID` 反查 FormID 型別。詳見 memory [[land-texture-render-requirements]]。

**Godot 編輯器 WYSIWYG（真實貼圖 + 真實物件，GUI 確認 2026-06-18）**：編輯器直接顯示遊戲真實素材，非平色/方塊/灰模。同款 **BSA 抽取 pattern**（CLI 從遊戲 BSA 抽 → 轉檔 → Godot 快取載入）：
- **地形紋理**：CLI `texexport`（LTEX→TextureSet→diffuse .dds → `magick` 轉 PNG）+ `tex_fetch.gd`（OS.execute 呼叫、快取 res://texcache/）+ `terrain_material.gd`（ShaderMaterial：base + ≤4 層 per-vertex alpha 混合，無貼圖 fallback 頂點高度色）。預覽 alpha 格＝匯出 splatmap 同份資料 → 所見＝VTXT 烘出。
- **物件模型+貼圖**：CLI `nifexport`（base→model .nif from mesh BSA）→ `nif2gltf` 轉 glTF（解 shape→BSLightingShaderProperty→BSShaderTextureSet diffuse，寫 `.textures.json` sidecar）→ CLI `texpath` 抽貼圖 → `model_fetch.gd` GLTFDocument runtime 載入、`placement.set_model` 換 box（game-unit×METERS_PER_UNIT→顯示公尺）。
- 工具：`landdiag`（LAND 紋理層 byte-verify）、`find 0xFORMID`（反查記錄型別）。durable 見 [sub_projs/godot-worldspace-editor](../../../sub_projs/godot-worldspace-editor/README.md) + memory [[godot-editor-wysiwyg-textures]]。

**model-converter `nif2gltf`（真實 vanilla nif 驗證+修復 2026-06-18）**：Skyrim NIF 靜態 mesh→glTF（LE NiTriShape/Strips、SSE BSTriShape/NiTriShape、NiNode transform、Z-up→Y-up、含 skin→exit 3）。**SSE position 精度 bug 修復**：真實 vanilla static 存 full-precision float3 卻不設 Full_Precision flag → 改從頂點佈局推精度（first attr offset≥12=float3，否則 half3），避免誤讀成 NaN（commit `bb9fe14`）。diffuse 貼圖解析（shader→textureset）+ sidecar。24 測綠。durable [sub_projs/model-converter](../../../sub_projs/model-converter/README.md) + memory [[godot-editor-wysiwyg-textures]]。⚠ 剩小尾巴（WAIT_USER）：物件只取 diffuse（無 normal/spec）、LE-format 真檔未驗、VTXT position row/col 序最終目視。
