# navmesh — 讓編輯器擺出來的東西被 NPC 走得到（plan）

← [plans](README.md)｜採集端：[scene-capture-bridge/backlog](scene-capture-bridge/backlog.md)｜契約：[navmesh-patch-design](../specs/navmesh-patch-design.md)｜任務：[navmesh-p3](navmesh-p3.md)｜背景：[engine-internals](../../docs/engine-internals.md#programmatic-navmesh)

## 現況

| 階段 | 狀態 |
|---|---|
| P0 navdiag＋vanilla NAVM no-op override | ✅ 10/10 NVNM byte-identical；白漫內外景 runtime PASS |
| P1 診斷警告 | ✅ Generator.Build.NavmeshIndex.cs＋Generator.Build.NavmeshCheck.cs |
| T2.0 L_NAVCUT | ✅ 白漫 TEST 繞行、CONTROL 直穿；autoNavCuts 預設 true |
| P2 NAVM Deleted-flag cut | 不做；L_NAVCUT 已解掉阻擋問題 |
| P3 navPatches[] | ✅ vanilla 內裝 edge-to-edge MVP runtime PASS |
| P4 sc nav 遊戲內採集 | 未動；工作拆在 [backlog B 線](scene-capture-bridge/backlog.md) |

## 問題分流

| 症狀 | 正解 |
|---|---|
| NPC 走進新牆／房屋／大石頭 | L_NAVCUT runtime 裁切；不改 NAVM |
| NPC 站在新平台上不動 | navPatches[] append＋link；必須寫 NAVM |
| 移除樓梯／橋後 NPC 走在空中 | 只做診斷，低優先 |

## 已定的引擎契約

### NAVM override

- Mutagen 0.53.1 對 NVNM no-op 讀寫無損；navdiag 的 master 側直接讀 ESM raw NVNM，不經 Mutagen。
- plugin 內的 NAVM override 會被引擎採用；NAVM 是整筆 last-wins，不會加法式 merge。
- NAVI/NVMI 清單由引擎加法式 merge；override 既有 NAVM 時可不 authored NAVI。
- triangle index 是跨 mesh EdgeLink.TriangleIndex 的位置契約：**永不重新編號**。新增只 append；不得刪除陣列元素。
- 修改幾何後須更新 Min／Max 並重建 NavmeshGrid；divisor=1、全部 triangle 單桶已 runtime 驗證。
- interior 頂點是 cell-local；exterior 頂點是 world-space，與 scene JSON 座標契約一致。
- 第一版 navPatches[] 只支援 vanilla interior；外景跨 cell EdgeLink 尚未開放。

### L_NAVCUT

- 正確 base 是 Skyrim.esm:0x000021 CollisionMarker，碰撞層為 49（L_NAVCUT），形狀用 REFR Primitive box。
- XPRM.Bounds 是全尺寸，不是半徑；padding 是每一側額外外擴，預設 32。
- navcut 一律 temporary。HearthFires 的 persistent navcut 是 enable-parent 用例，不是靜態裁切的一般要求。
- 只加 base record Obstacle flag 不足；碰撞層還必須帶 NavmeshObstacle。L_STATIC(1) 不帶，L_NAVCUT(49) 才是可靠路徑。
- navcut 要外擴半個 actor 寬，避免零體積 actor 從細縫穿過；只影響玩家所在 cell，且已開始的 path 不一定重算。

## 現行 spec

### 明示 navCuts[]

~~~jsonc
"navCuts": [
  {
    "editorId": "MF_CutUnderHouse",
    "worldspace": "Skyrim.esm:0x00003C",
    "position": {"x": 100, "y": 200, "z": -3510},
    "size": {"x": 520, "y": 140, "z": 220},
    "rotationZ": 45,
    "padding": 32
  },
  { "placement": "MF_MyHouse" }
]
~~~

### placement 三態

~~~jsonc
{
  "editorId": "MF_MyHouse",
  "base": "...",
  "worldspace": "...",
  // 省略：依門檻自動裁
  // "navCut": false：明示不裁
  // "navCut": true：不看門檻，明示要裁
  // "navCut": { "size": {...}, "offset": {...}, "padding": 48 }：手調
}
~~~

### 全域設定

~~~jsonc
"navmesh": {
  "autoNavCuts": true,
  "minFootprint": 10000,
  "minHeight": 100,
  "padding": 32,
  "warnings": true,
  "warnEmptyCells": false,
  "warnNavmeshClobber": true
}
~~~

自動 navcut 的三道 guard：

1. 只處理非 ACHR／hazard 的物件。
2. 只處理有 vanilla 網格可裁的 cell/worldspace。
3. 只有 footprint 真正覆蓋至少一個 live triangle 才生成。

無 Skyrim.esm 的離線機無法判定第 2、3 項，因此零產出、零警告。

### navmeshOverrides[]

~~~jsonc
"navmeshOverrides": [
  { "cell": "Skyrim.esm:0x01605E" },
  { "worldspace": "Skyrim.esm:0x01A26F", "x": 5, "y": -2 },
  {
    "worldspace": "Skyrim.esm:0x01A26F",
    "position": {"x": 21750, "y": -7625, "z": 0}
  },
  {
    "cell": "Skyrim.esm:0x01605E",
    "navmesh": "Skyrim.esm:0x0C9064"
  }
]
~~~

- 同 FormKey 表示 override，不建新 NAVM。
- in-spec cell 沒有 vanilla 網格可 override，validate 直接拒絕。
- parent chain 走既有 ExteriorCell()／WorldspaceOverride／CopyWorldspaceEnv，確保 EDID、RNAM、TopCell flags 正確且不帶 OFST。

### navPatches[]

~~~jsonc
"navPatches": [
  {
    "cell": "Skyrim.esm:0x01605E",
    "polygon": [
      {"x": 0, "y": 0, "z": 100},
      {"x": 64, "y": 0, "z": 100},
      {"x": 64, "y": 64, "z": 100}
    ],
    "linkTo": "auto"
  }
]
~~~

- polygon 至少 3 點、順序為周長、必須凸；以 fan triangulation 生成。
- 頂點與 triangle append 到原陣列尾端；新 triangle 互設鄰居。
- linkTo:"auto" 只接受唯一的完整邊匹配：新舊邊兩端都在 epsilon 內才雙向縫合；不能把「靠近」當成功。
- 若縫合／驗證失敗，整筆 patch 不落地；不得留下半套 CELL/NAVM override。
- 舊 triangle index 不變；只有命中 seam 的 EdgeLink 欄位可改。

## P1 診斷

- ACHR 不在任何 live triangle 2D 投影內，或與腳下網格高度差 >200（上）／>400（下）時警告。
- 大物件 footprint 面積 ≥10000 units² 且高度 ≥100、覆蓋 vanilla triangle 卻沒有 navcut 時警告。
- removals[]／overrides[] 動到頂面承載 navmesh 的大型物件時，提示可能留下懸空網格。
- exterior 查 3×3 cell 鄰域；帶 Deleted flag 的 triangle 跳過。
- navmesh.warnings:false 關閉總警告；placements[].navmeshCheck:false 只供刻意停在網格外的 actor。
- 無 Skyrim.esm 時完全沉默；「不知道」不能冒充「有問題」。

## P3 已驗收邊界

Bannered Mare NAVM 0x0C9064 fixture 從 298→302 vertices、318→320 triangles，新平台深 64 units。兩名一次性 Travel actor 分別從 seam 兩側出發：

- 新→舊到 vanilla marker 最短距離 3.3 units。
- 舊→新在 6 秒抵達平台 marker，距離 0.0。

這同時驗證雙向 seam、divisor=1 grid 與「改幾何但不 authored NAVI」。repeatable Patrol 不能區分 seam 失敗與 loop 語意，因此驗收固定使用兩個方向各一名 Travel actor。

## P4 待做

1. **T4.1 前置 spike**：DLL 讀 live RE::NavMesh 的 vertices／triangles／portals，顯示附近網格與腳下狀態。
2. **頂點吸附**：讓角點吸附到既有 vertex；這是 linkTo:"auto" 能否成功的核心。
3. **sc nav 模式**：登記簿、co-save、UI 點列表、收口、逐點刪除、polygon revert。
4. **匯出**：直接寫既有頂層 navPatches[]，C# 契約不變。
5. **遊戲內 guard**：凸性、自交、共線、零長邊當場拒絕。

navmesh 不是 waypoint graph：兩點連線不產生可走面。操作手勢是走一圈、每個轉角記一點、最後收口成 3+ 點 polygon。第一版仍只允許 vanilla interior。

## 保留未知數

- triangle Deleted flag 是否被 runtime 尋路尊重：未驗；目前沒有排程依賴它。
- 外景只改本 mesh、不動鄰居是否在 P3 幾何變更後仍安全：P0 支持，外景 P3 尚未驗。
- override／新建 NAVM 在 ESL 的邊界：新建 NAVM 保守強制 esl:false；override 路徑仍待專門驗證。
- 孤島 navmesh 是否能讓 NPC sandbox：未驗，不屬 edge-to-edge MVP。
- 外景 WRLD override 的地圖與名稱回歸仍需專門驗證。
- NAVM clobber 無法自動合併；CheckNavmeshOverrideClobbers 在 Data 夾可見其他 override 時點名 plugin＋mesh，無 Data 夾時沉默。

## 不做

- 不引入 Recast/DotRecast 的離線體素化；它需要 NIF/Havok 幾何產業鏈。
- 不碰 cover／preferred／NVPP；新 triangle 無 cover。
- 不刪除或 disable 整張 NAVM。
- 不把已作廢的 NAVM Deleted-flag cut 重新列成現役待辦。
