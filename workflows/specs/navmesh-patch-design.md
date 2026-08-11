# navmesh patch design — P3 interior edge-to-edge MVP

← [specs](README.md) ｜ 來源計畫：[navmesh](../plans/navmesh.md)

## Done when

P3 第一版的輸入契約、幾何邊界、失敗語意與驗收方式都唯一確定，可直接實作；外景、跨 cell EdgeLink、既有 triangle 切分、自動體素化與遊戲內採集不在本 MVP。

## 裁決

2026-08-11 依現有 plan 的保守路徑收旂：

- **內裝先行**：只 patch vanilla interior CELL 中的既有 NAVM。外景有跨 cell EdgeLink 與 WRLD override 額外風險，等內裝實機通過再開。
- **不引 DotRecast**：MVP 輸入就是使用者／採集器給的可走多邊形，不是 NIF 三角湯。凸多邊形用 fan triangulation；之後的遊戲內射線採樣才負責貼地。
- **只做 edge-to-edge 縫合**：新 polygon 的外邊必須與舊 triangle 的完整邊重合（容差內）才能縫。不把新點插入舊邊中間、不切舊 triangle；這樣才能保證既有 triangle index 永不重排。
- **沒縫上就失敗，不靜默生孤島**：MVP 的目標是「走上新平台」；`linkTo:auto` 若找不到唯一合法舊邊，該 patch 不寫入並給 warning。孤島語意日後若需要，另加明示 `linkTo:none`，不在 MVP 偷渡。

## Spec 契約

```jsonc
"navPatches": [
  {
    "cell": "Skyrim.esm:0x01605E",
    "navmesh": "Skyrim.esm:0x0C9064",
    "polygon": [
      {"x": -120, "y": 40, "z": 96},
      {"x":  120, "y": 40, "z": 96},
      {"x":  120, "y": 240, "z": 96},
      {"x": -120, "y": 240, "z": 96}
    ],
    "linkTo": "auto",
    "epsilon": 8
  }
]
```

| 欄位 | MVP 語意 |
|---|---|
| `cell` | 必填；vanilla interior CELL 的 `<master>:0xFORMID`。不接受 in-spec cell 或 worldspace。 |
| `navmesh` | 必填；該 CELL 內要 patch 的 vanilla NAVM。不猜多張 mesh 中的目標。 |
| `polygon` | 3–32 點，坐標與內裝 `placements[].position` 相同（CELL local game units）。點依周長排列；可 CW 或 CCW，build 正規化為從 +Z 看的 CCW。 |
| `linkTo` | MVP 唯一合法值 `auto`（預設）。 |
| `epsilon` | 邊端點三維歐氏距離容差，預設 8 game units，必須 `>0 && <=64`。 |

## 驗證與幾何規則

build 前可純 spec 驗的都視為 validation error：

1. `cell` / `navmesh` 必須是 external ref；`polygon` 點數 3–32。
2. XY 投影不得有零長邊、自交、共線或凹角；MVP 只收凸多邊形。Z 可不同，以支援斜坡。
3. 點數、既有頂點數與 triangle 數加總不得超過 signed/unsigned 16-bit index 可表示範圍；超過就拒絕該 patch。
4. 同一 `(cell, navmesh)` 有多筆 patch 時依 spec 順序套用，後一筆可縫到前一筆新增的邊；每筆仍必須唯一命中。

需 master 才能判斷的錯誤在 build 時 warning 並跳過該 patch：CELL/NAVM 不存在、NAVM 不屬該 CELL、找不到可縫邊、同時命中多條舊邊。無 Skyrim.esm 的離線機維持現行降級規則：不產出 NAVM，不把「這台機器沒 master」誤報為 spec 錯誤。

## 輸出不變量

- target NAVM 沿用原 FormKey 做 whole-record override；NAVI 不動。
- 既有 vertices/triangles 保持原順序，新頂點與新 triangle 只 append 到尾端。
- 舊 triangle 只允許改一個被縫邊的 neighbour index；flags、vertices、其他 neighbour、DoorTriangles 與 cross-mesh `EdgeLinks[]` 不動。
- 新 triangle 的內部共邊必須雙向相連；與舊網格的縫也必須雙向。
- `Min` / `Max` / `MaxDistanceX` / `MaxDistanceY` 依全部頂點重算；`NavmeshGridDivisor=1`，grid 重建為單桶 `[triangleCount:int32][0..N-1:uint16]`。
- 延用原 `CrcHash`、parent、cover table、door table、record flags與其他 opaque 欄位。
- 只要任一 guard 失敗，該 patch 對 NAVM 零修改；不留半套頂點。

## 驗收

離線／RequiresSkyrim 結構驗收：

- triangle fan 的數量為 `N-2`，內部共邊雙向相連。
- 與 Bannered Mare 既有邊縫合後，新舊 triangle 互為 neighbour。
- 既有 triangle index 不變；除命中邊的一個 neighbour 欄外逐欄相等。
- 無縫、多縫、凹多邊形、超界輸入都不產生部分修改。
- grid 含所有 triangle index 且 bounds 包含新頂點。

實機驗收（寫入 WAIT_USER）：在既有內裝 NAVM 邊旁放一塊新平台，NPC 能雙向上下；平台上 spawn 的 NPC 能 sandbox 並跟隨離開。這一步同時驗 U3/U4。

## 非目標

- exterior/worldspace 與跨 cell EdgeLink。
- 切分、刪除或重排既有 triangle。
- door triangle、cover、preferred path、NVPP、NAVI 改寫。
- 凹多邊形三角化、自動貼地、NIF/havok 體素化、DotRecast。
- scene-capture-bridge 的 `sc nav` 輸入採集（P4）。
