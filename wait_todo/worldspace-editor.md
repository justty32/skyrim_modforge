# wait_todo — Godot worldspace editor（Idea #19 + model-converter）

← [WAIT_USER](../WAIT_USER.md)（總入口）

離線地形/紋理/物件編輯前端（[`sub_projs/godot-worldspace-editor/`](../sub_projs/godot-worldspace-editor/README.md)）整鏈相關的待主力機項。

- **【Idea #19 紋理 + 物件 — Godot 整鏈剩「刷哪長哪」最終目視（2026-06-18）】**：✅ **整鏈 in-game 確認**（`GodotEditorDemo.zip`）：Godot GUI 刷高度+紋理+擺物件 → 匯出 → ModForge build → 地形/紋理/物件實機全顯示（[landed/world.md](../workflows/feature-dev/landed/world.md) + memory [[land-texture-render-requirements]]，紋理 3 真因已修、695 測綠）。⚠️ **唯一還沒目視確認的尾巴**：VTXT 每點 position 的 **row/col 編碼順序**（layer 號/flags/texture FormID 已對 vanilla byte-verify，但 position 序是照文件慣例推的，離線無法對 vanilla 比）。**待你做**：在 Godot 只刷地圖**某一角/一條**草（不要刷滿）→ build 進遊戲，確認草長在**你刷的同一位置**（不是鏡像/旋轉/錯格）。若位置對＝position 序確認；若鏡像或偏移，回報「刷在哪、實機長在哪」我修 `Vtxt.cs` 的 row/col 對應。
- **【物件 WYSIWYG — 已整合進編輯器，剩 GUI 目視 + 模型貼圖（2026-06-18）】**：✅ nif2gltf 已對**真實 vanilla SSE nif 驗證+修復**（RockL01/TreePineForest01：extract→convert→`GLTFDocument` 載入 1/2 mesh OK；修了 full-precision-flag 不可靠 bug，commit `bb9fe14`，24 測綠）。✅ 已串成編輯器物件 pipeline（CLI `nifexport` + `model_fetch.gd` + placement `set_model`，commit `3cad792`）。**待你做（主力機開 Godot Place Mode）**：① 擺幾個物件 → 按 **「Load real models (WYSIWYG)」** → 看 box 有沒有換成真實 RockL01 石頭/松樹模型、大小/位置對不對（scale=game-unit×METERS_PER_UNIT，若太大/太小回報）；② 試填別的 base ref（STAT/TREE FormID）按鈕重抽看能不能顯示。⚠️ **已知缺**：glTF 目前**無貼圖**（灰模），形狀對即算過——貼圖是下一步（nif→TXST→DDS，同 texexport pattern）。③ LE-format nif（舊版）還沒拿真檔驗（vanilla 多為 SSE，遇到 LE 再說）。收尾驗證，不擋使用。
