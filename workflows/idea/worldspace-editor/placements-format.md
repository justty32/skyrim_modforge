# Godot Placements 格式詳述

← [README](README.md)

## Godot 出圖檔案（`godot_export/placements.json`）

```json
{
  "version": 1,
  "coordinate_system": "godot4_y_up",
  "placements": [
    {
      "base": "Skyrim.esm:0x000D4B52",
      "position": { "x": 12.5, "y": 3.2, "z": -8.1 },
      "rotation": { "x": 0.0, "y": 0.785, "z": 0.0 },
      "scale": 1.5
    },
    {
      "base": "Skyrim.esm:0x000034",
      "instanceId": "MyPatrolMarker01",
      "position": { "x": 5.0, "y": 0.1, "z": 2.0 },
      "rotation": { "x": 0.0, "y": 1.571, "z": 0.0 },
      "scale": 1.0
    }
  ]
}
```

| 欄位 | 型別 | 說明 |
|------|------|------|
| `version` | int | 格式版本（目前 1） |
| `coordinate_system` | string | `"godot4_y_up"` — Godot 4 標準座標（X=東、Y=上、Z=南） |
| `base` | string | Skyrim base form ref（`<master>:0xFORMID` 或 in-spec editorId） |
| `position` | Vec3 | **公尺**，Godot 原生座標 |
| `rotation` | Vec3 | **radians**，Godot 原生 Euler；ModForge 讀時轉 degrees |
| `scale` | float | uniform 縮放；Godot plugin 限制等比，非等比取主軸並 warn |
| `instanceId` | string? | **選填，可省略**（省略 ≠ `""`）；省略 = 匿名 REFR；有值 = 此 REFR 的 editorId |

## 掛進 worldspace spec

```json
{
  "worldspaces": [
    {
      "editorId": "MyWorld",
      "heightmap": { "path": "terrain.png", "originX": 0, "originY": 0, "minHeight": 0, "maxHeight": 8192 },
      "godotPlacements": { "$include": "godot_export/placements.json" }
    }
  ]
}
```

`godotPlacements` 巢狀在 worldspace 節點下 → ModForge 從同一節點取 `OriginX/OriginY` 做座標換算，無需額外關聯欄位。

## ModForge 座標換算（`coordinate_system: "godot4_y_up"`）

```
skyrim_x = OriginX * 4096  +  godot_pos.x / 0.014286
skyrim_y = OriginY * 4096  -  godot_pos.z / 0.014286   ← Godot +Z 朝南，Skyrim +Y 朝北，方向相反
skyrim_z =                     godot_pos.y / 0.014286
```

比例常數 `0.014286 m/unit`（1 unit ≈ 1.4286cm，社群共識；待主力機 UESP 確認，不擋 MVP）。

rotation：各軸 `deg = rad × (180 / π)`；軸對映（Godot XYZ → Skyrim XYZ）待 Godot plugin 實作時對齊確認。
