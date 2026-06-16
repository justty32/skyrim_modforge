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

## Open

- **待主力機收尾**：對真實 Tamriel 斜坡格反解比對（演算法已高信心，純驗證性，不擋實作）
- **待確認**：UESP 驗證 game unit → 公尺精確比例（目前採社群共識 1 unit ≈ 1.4286cm，不擋 MVP）
