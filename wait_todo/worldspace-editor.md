# wait_todo — Godot worldspace editor（Idea #19 + model-converter）

← [WAIT_USER](../WAIT_USER.md)（總入口）

離線地形/紋理/物件編輯前端（[`sub_projs/godot-worldspace-editor/`](../sub_projs/godot-worldspace-editor/README.md)）整鏈相關的待主力機項。

- **【Idea #19 紋理 + 物件 — Godot 整鏈剩「刷哪長哪」最終目視（2026-06-18）】**：✅ **整鏈 in-game 確認**（`GodotEditorDemo.zip`）：Godot GUI 刷高度+紋理+擺物件 → 匯出 → ModForge build → 地形/紋理/物件實機全顯示（[landed/world.md](../workflows/feature-dev/landed/world.md) + memory [[land-texture-render-requirements]]，紋理 3 真因已修、695 測綠）。⚠️ **唯一還沒目視確認的尾巴**：VTXT 每點 position 的 **row/col 編碼順序**（layer 號/flags/texture FormID 已對 vanilla byte-verify，但 position 序是照文件慣例推的，離線無法對 vanilla 比）。**待你做**：在 Godot 只刷地圖**某一角/一條**草（不要刷滿）→ build 進遊戲，確認草長在**你刷的同一位置**（不是鏡像/旋轉/錯格）。若位置對＝position 序確認；若鏡像或偏移，回報「刷在哪、實機長在哪」我修 `Vtxt.cs` 的 row/col 對應。
- **【物件 WYSIWYG — 形狀+貼圖都已實作，剩貼圖 GUI 目視（2026-06-18）】**：✅ **形狀 GUI 確認**（Place Mode 擺 RockL01/松樹，形狀正確）。✅ **貼圖已實作**（nif2gltf 解 BSLightingShaderProperty→BSShaderTextureSet diffuse → glTF material + sidecar；CLI `texpath` 抽貼圖；`model_fetch` 抽進 modelcache，GLTFDocument 自動套；headless 驗 rock 1/1、tree 2/2 面有貼圖，commit `b2e6f25`/`502a523`）。預載 modelcache 已含貼圖。**待你做（主力機開 Godot Place Mode）**：擺 RockL01/松樹（或按「Load real models」）→ **看模型有沒有真實貼圖**（石頭=MountainSlab02、松樹=bark/branch），非灰模即過。⚠️ 剩：① 只取了 **diffuse**（無 normal/specular，夠 WYSIWYG）；② LE-format nif（舊版）未拿真檔驗（vanilla 多 SSE，遇到再說）。不擋使用。
