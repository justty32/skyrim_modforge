# wait_todo — Godot worldspace editor（Idea #19 + model-converter）

← [WAIT_USER](../WAIT_USER.md)（總入口）

離線地形/紋理/物件編輯前端（[`sub_projs/godot-worldspace-editor/`](../sub_projs/godot-worldspace-editor/README.md)）整鏈相關的待主力機項。

- **【Idea #19 紋理 + 物件 — Godot 整鏈剩「刷哪長哪」最終目視（2026-06-18）】**：✅ **整鏈 in-game 確認**（`GodotEditorDemo.zip`）：Godot GUI 刷高度+紋理+擺物件 → 匯出 → ModForge build → 地形/紋理/物件實機全顯示（[landed/world.md](../workflows/feature-dev/landed/world.md) + memory [[land-texture-render-requirements]]，紋理 3 真因已修、695 測綠）。⚠️ **唯一還沒目視確認的尾巴**：VTXT 每點 position 的 **row/col 編碼順序**（layer 號/flags/texture FormID 已對 vanilla byte-verify，但 position 序是照文件慣例推的，離線無法對 vanilla 比）。**待你做**：在 Godot 只刷地圖**某一角/一條**草（不要刷滿）→ build 進遊戲，確認草長在**你刷的同一位置**（不是鏡像/旋轉/錯格）。若位置對＝position 序確認；若鏡像或偏移，回報「刷在哪、實機長在哪」我修 `Vtxt.cs` 的 row/col 對應。
- **【model-converter MVP】nif→glTF 載體已自寫，剩對真實 vanilla 檔驗證 — 待主力機**（物件 WYSIWYG 的下一步：base FormID→model NIF→glTF）：
  ✅ 已**離線自寫** Python 載體 `nif2gltf`（[sub_projs/model-converter/](../sub_projs/model-converter/README.md)）：Skyrim NIF 靜態 mesh→glTF，**LE**（NiTriShape/Strips+Data，全 float）+**SSE**（BSTriShape，BSVertexDesc offset 表解碼）、NiNode transform、Z-up→Y-up、含 skin→exit 3、batch manifest，**23 測綠**。不再依賴 NifSkope（原「測有沒有 CLI」那關已用自寫繞過）。⚠️ 勿用 `amPerl/nif`、`SkyMeshGLTF`（幻覺）。**待你做（主力機，有遊戲素材）**：① 解出幾個真實 vanilla `.nif`（LE BSA 一個、SSE BSA 一個，例如某 rock/clutter static），跑 `python -m nif2gltf --in X.nif --out X.gltf --flat`；② 把 `.gltf` 拖進 Blender 或 Godot 看形狀對不對（**SSE 半精度 offset 解碼是最需驗的點**——若 SSE 出來變形/錯位/空，回報該 nif 的 BSVertexDesc）；③ LE 若也有就一併驗。離線只證了「reader 讀回它照 nif.xml 編的合成 fixture」，真檔 byte 對齊跨不過去。收尾驗證，不擋使用。
