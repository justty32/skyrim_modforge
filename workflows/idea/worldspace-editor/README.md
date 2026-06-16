# Idea #19：Godot Worldspace Editor

← [ideas 索引](../ideas.md)｜附屬：[座標系與換算 coord-system.md](coord-system.md)

用 **Godot 4** 離線畫地形 → 匯出 → ModForge 生 LAND/REFR → 進遊戲微調。定位是 **Creation Kit 地形/場景編輯的替代前端**（CK 在 Wine/Proton 不穩、難腳本化）。

---

## 三階段 pipeline

1. **Godot 粗畫**（本 idea）：HTerrain 刷高度 + 散佈物件 → 匯出 PNG heightmap + placements JSON
2. **ModForge 生成**：讀 PNG → 切 cell → LAND records；讀 placements → REFR → ESP + BSA
3. **遊戲內微調**（另一專案）：擴展 SkyrimIngameEditor（C++/SKSE）細調高度/物件位置

> **分工邊界**：Godot 離線粗稿＝本 idea；遊戲內 fine-tune＝獨立 C++/SKSE repo，**不在 ModForge 範圍**。

---

## 專案定位

前期作為 sub_proj（`sub_projs/godot-worldspace-editor/`，輸出 JSON spec 對接 ModForge）→ 體量大了再移成獨立 repo，對接方式不變。

---

## 決策記錄（2026-06-16 brainstorm）

### 地形 / 整體架構

| 主題 | 決策 | 演進路線 |
|---|---|---|
| Godot 地形系統 | **Godot 4 + HTerrain plugin**（內建筆刷 + PNG 匯出） | — |
| Heightmap 格式 | **16-bit grayscale PNG**，spec 寫路徑 | — |
| Heightmap 切割 | **單張大 PNG，ModForge 切**（非 per-cell）：N 格寬→PNG 寬 `N×32+1` px，第 c 格取 `[c×32 .. c×32+32]` 共 33 px，相鄰格共用第 32 欄 → **seam 零誤差** | — |
| MVP scope | **單格 PNG → LAND → COW 進入、地形有起伏**；物件擺放不含 | — |
| 物件 metadata | 薄 script + `@export var skyrim_base`（base form ref）+ `@export var instance_id`（選填，空=匿名） | — |
| 擺放座標 | **Godot 原生座標（公尺 + Y-up），ModForge 轉**成 game units + Z-up；轉換唯一真相在 ModForge，Godot plugin 不做 Skyrim-specific 計算 | — |
| 物件 scale | **鎖 uniform**：Godot script 限制等比縮放（或匯出取主軸並 warn），對齊 Skyrim REFR 只支援單一 XSCL float | 後做：非等比 → 預變形 NIF 變體（遠超 MVP） |
| 物件預覽 | PyNifly 批量轉 glTF 作視覺代理 | B 先 → C 完整即時預覽 |
| VTXT 紋理 | 全格套預設 dirt | A 先 → B splatmap→VTXT 映射 → C BSA 預覽 |
| Navmesh | MVP 跳過，只生 LAND（COW 進入） | C 先 → B Godot 三角化地形匯頂點→NAVM |

### Godot Placements 格式（2026-06-16 brainstorm）

| 主題 | 決策 | 演進路線 |
|---|---|---|
| 出圖檔案格式 | **header 包一層**：`{"version":1, "coordinate_system":"godot4_y_up", "placements":[...]}` — 明確標示座標系，未來換工具不破壞 ModForge 解析 | — |
| 掛進 spec 方式 | **`godotPlacements` 巢狀在 worldspace 節點下**（JSON Schema `$ref` 抽獨立 schema）；ModForge build 時轉換後合流進一般 `placements[]` pipeline | — |
| Rotation 單位 | **JSON 寫 radians**（Godot 原生），**ModForge 負責轉 degrees**；文件明標「`rotation` 欄位單位為 radians」 | — |
| Instance editorId | **β 方案**：`instanceId` 選填，**可完全省略**（≠ 空字串）；省略 = 匿名 REFR；有值 = 此 REFR 的 editorId（供 linkedRef / quest alias 指向）。`linkedRefs` 等複雜關係仍走手寫 `placements[]` | γ 後排：`linkedRefs` 也進 Godot |

---

JSON 格式、欄位表、座標換算公式 → [placements-format.md](placements-format.md)

---

## ModForge 後端

**已有（平坦地形）**：`WorldspaceCellSpec.Height` 單一 float → VHGT 全格同高；`Height/8` 編碼；VNML 全朝上；SubCells block tree、flat-quad navmesh、水位預設。

**需新增（非平坦地形）**：
- spec 加 `"heightmap": "terrain.png"`，讀 PNG → 採樣每 cell 33×33
- VNML 從鄰格高度差重算（目前固定朝上）
- 多 cell 邊界接縫（seam：相鄰 cell LAND 邊緣行需一致）
- VTXT 紋理層（初期全 dirt，後期 splatmap 映射）

**不需動**：水位 / 氣候天氣（REGN）/ flat-quad navmesh（暫夠）/ LOD（→ shell-out xLODGen，roadmap #11）。

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
- **Cell seam**：高度已靠單張 PNG 共用邊緣欄自動對齊（見決策表）；殘留風險是 **VNML 法線**——邊緣頂點重算法線需參考鄰格高度，切割後要保留 1px overlap 才能算對
- **NIF 預覽**：Godot 不讀 NIF，靠 glTF proxy（PyNifly 批量轉換前置）
- **Navmesh**：複雜地形 flat-quad 會卡 NPC，精確 navmesh 後排（B 路線或 CK 補）

---

## 座標系（摘要）

| 確認值（codebase） | |
|---|---|
| 1 cell | 4096 × 4096 game units |
| LAND grid | 33×33 頂點，間距 128 units |
| VHGT 編碼 | `Offset = Height / 8` |

**Godot 比例**：採社群共識 **1 unit ≈ 1.43cm** → 1 cell ≈ **58.5m**、頂點間距 ≈ 1.83m。

完整推導、HTerrain 設定、**blocking 驗證項**見 → [coord-system.md](coord-system.md)。

---

## 與現有結構的關係

| 現有項目 | 關係 |
|---|---|
| Roadmap generation.md #2「LAND 高度圖」 | 本 idea 的 ModForge 後端（flat LAND → PNG heightmap LAND）|
| Roadmap generation.md #3「擴展 SkyrimIngameEditor」 | 本 idea 第 3 階段（遊戲內微調），歸另一個 C++ repo |
| Idea #15「Unity/Blender 視覺場景編輯器」 | 重疊但焦點不同：#15 物件擺放為主；本 idea 地形為主、工具選 Godot |
| Idea #4「異世界冒險」、#11「M&B 世界」 | 本 idea 是這些的地形製作工具 |
| `Spec.Worldspace.cs` / `Generator.Build.ExteriorCells.cs` | 已有平坦 LAND 生成，本 idea 往非平坦延伸 |

---

## Open / Blocking

- ✅ ~~**Blocking：VHGT delta signed/unsigned + 累積方式**~~ **已解（2026-06-16）**：背景 agent 查 Mutagen 0.53.1 原碼 + UESP + xEdit → signed int8（ModForge 自做二補數）、row-wise 累積、delta ×8 game units。設計＋逆推演算法見 [worldspace-editor-design.md](../../specs/worldspace-editor-design.md)。**降級為待主力機收尾**：對真實 Tamriel 斜坡格反解比對（演算法已高信心，純經驗性收尾，不擋實作）。
- **物件預覽 NIF → glTF（B 路線前置，已查證 2026-06-16）**：工具選 **fo76utils/nifskope**——真實存在、**Linux+Windows 雙平台**（主力機 Manjaro 可直接跑，不必 Windows 分區）、geometry-only glTF export 正好夠視覺代理。**`nif2gltf` Rust CLI 經查證不存在（Gemini 捏造），勿規劃**。PyNifly 真實但 Windows-only（高保真備案）。原 Gemini note 多處錯誤（見該檔頂部修正 banner）：[`sub_projs/gemini-research/worldspace-editor/nif-gltf-conversion.md`](../../../sub_projs/gemini-research/worldspace-editor/nif-gltf-conversion.md)。
- **UESP 確認 game unit → 公尺比例**（主力機；目前採社群共識，不擋 MVP——高度換算 spec 內自洽即可）
