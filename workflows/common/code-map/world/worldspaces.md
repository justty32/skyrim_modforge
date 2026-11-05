# CODE_MAP.world — Worldspaces + Regions

← [CODE_MAP.world](../CODE_MAP.world.md)

## Worldspaces + Regions
→ **說明文件**：[SPEC-worldspaces.md](../../../../docs/spec/SPEC-worldspaces.md)（worldspace 細節已從 SPEC-world.md 移來此檔）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Worldspace.cs` | `WorldspaceSpec`（含 `Heightmap`/`GodotPlacements`/**`BaseTexture`**(LTEX ref)/**`TextureLayers`**(多紋理混合)）, `HeightmapSpec`, **`TerrainTextureLayerSpec`**(texture+splatmap), **`SplatmapSpec`**(path/originX/Y), `GodotPlacementsSpec`, `WorldspaceCellSpec`（含 XCLW/XCWT/XCAS 與選用 LGTM/IMGS）, `WorldMapDataSpec`, `RegionSpec`, `RegionWeatherEntrySpec`, `PointSpec` |
| Build P1 | `Generator.Build.Worldspace.cs` / `Generator.Build.GodotPlacements.cs` | 建 worldspace record（climate/water/map bounds）+ cell grid骨架；flat cell 的選用 XCLW／XCWT／XCAS／LGTM／IMGS 在 `EmitCell` 寫入（ref 走既有 `Wire`；空值不寫）；**PNG heightmap 路徑**（`Heightmap.Load` → `Vhgt.Encode` per cell）；**單層地形貼圖 `baseTexture`**（resolve LTEX 一次 → `EmitCell` 每格四象限加 `BaseLayer`{`LayerHeader.Texture`/`Quadrant`}，**`LayerNumber=BaseLayerNumber`=0xFFFF**）；**多紋理 `textureLayers`**（每層 resolve LTEX + `Splatmap.Load`；`EmitCell` 每格 `TrySampleCell` → `Vtxt.BuildLayers` → 加 `AlphaLayer`，**LayerNumber=層序 0-indexed**）；**有紋理時 `LAND.Flags |= Layers`(0x04)**；**Godot placements per-build 展開 + base/global editorId fail-closed gate**（在 `BuildPlacements` 前注入、不修改 caller spec）|
| Build P1 | `Generator.Build.Regions.cs` | 建 region record（polygon / weather table / priority / map color）|
| Build P2 | `Generator.Build.ExteriorCells.cs` | cell group tree 生成（外層結構）|
| Build P2 | `Generator.Build.Navmesh.cs` | NAVM 4 頂點平面 quad + NAVI 索引；全部 quad 建完後，同 worldspace 正交相鄰且兩側皆啟用 navmesh 的 cell 互建 external `EdgeLinks[]`，triangle edge 欄存本表 index 並設對應 flag（[engine-internals § navmesh](../../../../docs/engine-internals.md#programmatic-navmesh-navm--navi--in-game-confirmed-2026-06-03)）|
| Build P2 | `Generator.Build.Navmesh.Custom.cs` | `navmeshGeometry` 頂點/三角形原樣寫入；處理 spec 明示的跨格 links（不重排 triangle）|
| Build P2 | `Generator.Build.Navmesh.Seams.cs` | 只補至少一側是自訂幾何的未連接邊界；投影區間重疊 + `CustomNavmeshSeamMaxZDelta=128` 才雙向配對，配不上出警告；flat↔flat 明確略過，不碰既有路徑 |
| Util | `Heightmap.cs` | 16-bit grayscale PNG → 全域高度網格；`SampleCell` 切 33×33（相鄰格共用邊緣欄）；`SampleCellExtended` 切 35×35（+1px 邊框，供 VNML 中心差分）；Y-flip（影像頂=北）|
| Util | `Vhgt.cs` | VHGT 編解碼：絕對高度 → float offset + 33×33 signed-int8 delta（row-wise 累積，×8 game units）；`Decode` 接受 `IReadOnlyArray2d<byte>`（相容 Mutagen getter）|
| Util | `Vnml.cs` | VNML 法線計算：從 35×35 高度格（SampleCellExtended 輸出）以中心差分算切線，E×N cross product 得 Skyrim(X=東,Y=北,Z=上) 法線，encode `P3UInt8` **signed byte=round(n×127)**（無 +128 偏移；up=(0,0,127)）|
| Util | `Splatmap.cs` | 8-bit grayscale PNG → per-vertex alpha 網格（同 heightmap 網格約定，Y-flip）；`TrySampleCell(globalCellX,Y)` 切 33×33 alpha 0..1，cell 落在圖涵蓋範圍外回 false |
| Util | `Vtxt.cs` | ATXT+VTXT 純建構：33×33 alpha 格 + Quadrant → `AlphaLayer`{`LayerHeader`+稀疏`AlphaLayerData`(position/opacity)}；quadrant 切分（Bottom=南/低row、Left=西/低col）、position=localRow×17+localCol、0-indexed、空象限不生層。✅ LayerNumber/flags/texFormID 已對 vanilla byte-verify（2026-06-18 in-game OK）；⚠️ 僅 VTXT position 的 row/col 序待最終目視 |
| Seam | `Generator.Build.Worldspace.cs` | heightmap 迴圈內 **seam stitching**（VHGT）+ **VNML 重算**：每格 encode 後 decode 取 east/north edge 注入下格，`Vnml.Compute(SampleCellExtended)` 填法線（邊緣頂點帶 1px overlap 無需特判）|
| Util | `GodotPlacements.cs` | Godot placements format v1 / `godot4_y_up` JSON → `List<PlacementSpec>`；拒絕缺漏或不支援的 format version；座標換算（Z 翻轉、m→units）+ rotation rad→deg |
| Util | `SceneCoordinates.cs` | Pure transform API：source position/quaternion/scale3 → Skyrim position/Euler XYZ/scale；明確 Unity LH Y-up、Unreal LH Z-up 與 custom basis，完整 `B*R*B⁻¹`；非均勻 scale diagnostic |
| Tests | `SceneCoordinatesTests.cs` | identity、axis/handedness、basis rotation、unit/fudge scale、non-uniform diagnostic |
| Tests | `WorldspaceNavmeshGeometryTests.cs` / `WorldspaceNavmeshSeamTests.cs` | 自訂 NAVM 原樣寫入/明示 links；自訂幾何↔平面在 Z 容差 128 內雙向連接，超過時不連且警告 |
| Validate | `Generator.Validate.World.cs` | worldspace ref、boundary、climate ref |
| Validate | `Generator.Validate.World.More.cs` | region / encounter zone / outfit content ref |
| Diag | `Diagnostics.Worldspace.cs` | worldspace climate/water/map / cell grid / region overlay |
| Diag | `Diagnostics.Landscape.cs` | `landdiag <plugin> [ws] [n]` — dump LAND 紋理層（BTXT/ATXT quad+LayerNumber+tex、VTXT pts、Flags）對 vanilla byte-verify（Mutagen `AlphaLayer:BaseLayer` 故判型先 IAlphaLayerGetter）；`find <plugin> 0xFORMID` 反查 FormID 型別（`Diagnostics.cs`）|
| Tool | `TexExport.cs` (CLI) | `texexport <dataDir> <outDir> <master:0xLTEX>[,…]` — LTEX→TextureSet→diffuse .dds 從遊戲 BSA（`Archives`）抽出 → `magick` 轉 PNG；餵 Godot worldspace-editor 的 WYSIWYG 地形 shader |
| Tool | `NifExport.cs` (CLI) | `nifexport <dataDir> <outDir> <master:0xFORMID>[,…]` — 可放置 base（STAT/TREE/MSTT…）的 `IModeledGetter.Model.File`→model .nif 從 mesh BSA 抽出；Godot 端再走 `nif2gltf`（[../model-converter](../../../../../model-converter/README.md)）轉 glTF 當 WYSIWYG 物件代理 |
| Tool | `TexExport.cs` `TexPath` (CLI) | `texpath <dataDir> <outDir> <texPath>[,…]` — 抽任意貼圖路徑（NIF shader 引用的 .dds）→ `<basename>.png`；給物件 glTF 上 diffuse 貼圖（nif2gltf 寫 `<stem>.textures.json` sidecar 列要抽的 dds，model_fetch 據此呼叫）|
