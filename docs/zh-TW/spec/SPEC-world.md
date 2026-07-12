# ModForge spec — 室內 cell、放置與地圖標記

← [index](SPEC-index.md) · 光照 → [SPEC-lighting](SPEC-lighting.md) · in-world macro →
[SPEC-world-macros](SPEC-world-macros.md) · 室外世界、清單、生怪與商販 →
[SPEC-worldspaces](SPEC-worldspaces.md)

室內 cell、物件放置（室內與室外），以及世界地圖標記。自訂光源與室內／室外光照
（LGTM/IMGS/DALC）已移至 [SPEC-lighting](SPEC-lighting.md)；高階人口 macro（技能樹、聚落、
活世界 NPC）已移至 [SPEC-world-macros](SPEC-world-macros.md)。室外世界空間、等級清單、遭遇區
與商販請見 [SPEC-worldspaces](SPEC-worldspaces.md)。

### cells 與 placements — 把東西放進世界
```jsonc
"cells": [
  { "editorId": "MF_TestRoom", "name": "ModForge Test Room",     // a new interior cell
    "template": "Skyrim.esm:0x0165A8" }                          //   copy lighting from Breezehome (else BLACK)
],
"placements": [
  { "base": "MF_Smith", "cell": "MF_TestRoom",                   // an in-spec NPC ...
    "position": { "x": 0, "y": 0, "z": 0 },
    "rotation": { "x": 0, "y": 0, "z": 0 } },                    //   rotation in degrees
  { "base": "MF_Chest", "cell": "Skyrim.esm:0x01605E",          // ... into a VANILLA INTERIOR cell
    "position": { "x": 100, "y": 0, "z": 0 } },                  //   (Skyrim.esm WhiterunBanneredMare)
  { "base": "MF_Coin", "worldspace": "Skyrim.esm:0x00003C",     // ... into the OPEN WORLD (Tamriel);
    "position": { "x": 22528, "y": 22528, "z": 200 } }           //   position is WORLD coords
]
```
- 一個 `placement` 鎖定的目標**不是**室內 `cell` **就是**室外 `worldspace`（擇一設定）：
  - **室內** — `cell` 是一個 in-spec 新室內 cell 的 `editorId`，**或**一個外部／原版
    室內 cell `"<master>:0xFORMID"`（用 `find <Skyrim.esm> <name> Cell` 找）。一個沒有
    `template` 的新 cell 會渲染成**全黑**且**沒有地板**（你會掉進虛空）：把該
    cell 的 `template` 設為某個原版室內（複製其光照），並在裡面放一個地板 static。
    `position` 是相對於該 cell 的局部座標。
  - **室外** — `worldspace` 是一個世界空間 ref `"<master>:0xFORMID"`（Tamriel =
    `Skyrim.esm:0x00003C`；用 `find <Skyrim.esm> <name> Worldspace` 找）。`position` 是
    **世界**座標；位於 `floor(x/4096), floor(y/4096)` 的室外 cell 會在 master 中被找到
    並被覆寫以加入你的 ref。若該格子沒有 master cell，會在那裡建立一個新的室外 cell
    （僅結構性 — 未經遊戲內驗證）。若 `worldspace` 與 `cell` 同時設定，`worldspace` 勝出。
- `base` 是一個 *ref*（in-spec 或外部）；NPC 會變成 `PlacedNpc`，其他任何東西都是 `PlacedObject`
  （`kind` 會覆寫這個猜測）。`rotation` 是**角度**。`persistent: true` 會把它放進該
  cell 的 persistent 清單（若某個 quest/script 引用它則需要）。
- **放置 hazard：** 當 `base` 是一個 in-spec `hazards[]` editorId（或 `kind: "hazard"`）時，該 ref
  是一個 `PlacedHazard` — 一個 static 的環境陷阱（火/霜/毒區域）。見
  `SPEC-magic.md § hazards` 中的 HAZD record。
- **`kind: "xmarker"` / `"xmarkerHeading"`** — 用於放置一個**不可見錨點**的輔助器。當
  `base` 為空時，預設為原版 XMarker（`Skyrim.esm:0x0000003B`）／ XMarkerHeading
  （`0x00000034`）static，且該 ref 會**強制 persistent**（一個 quest 目標錨點必須 persist，否則
  `forced:` 別名會解析到一個被丟棄的暫時 ref）。給它一個 `editorId`，用一個
  `forced:<editorId>` 別名綁定它，並讓某個 `objectives[].targets[]` 指向該別名，就能在一個沒有
  NPC 的固定點放上一個 quest 標記。
- **原版 placement**（室內 cell 或室外 worldspace）會覆寫該 cell/worldspace 以*加入*你的
  reference（原版內容不受影響 — 它們來自 master）。需要遊戲的 `Data` 資料夾 —
  若它不在預設 Steam 路徑，設定 `MODFORGE_SKYRIM_DATA`。（放入一個原版 worldspace 時
  也會附加帶上它的 persistent cell，所以原版的地圖標記與世界地圖會保持完整。）

#### placement 額外欄位
```jsonc
"placements": [
  { "base": "MF_GoldCoins", "cell": "MF_Room",
    "position": { "x": 0, "y": 50, "z": 80 },
    "count": 50 },                                      // XCNT: 50 金幣疊放

  { "base": "MF_LockedChest", "cell": "MF_Room",
    "position": { "x": 200, "y": 0, "z": 0 },
    "lock": { "level": "master" },                      // XLOC: 大師級鎖
    "ownership": { "owner": "MF_BanditFaction" } },     // XOWN: 屬於此幫派

  { "base": "MF_Trophy", "cell": "MF_Room",
    "position": { "x": -100, "y": 0, "z": 100 },
    "scale": 1.5 },                                     // XSCL: 放大 1.5 倍

  { "base": "MF_SecretDoor", "cell": "MF_Room",        // 任務觸發前隱藏
    "editorId": "MF_SecretDoorRef",
    "initiallyDisabled": true,                          // 不可見 + 無碰撞
    "enableParent": {                                   // XESP: 跟隨任務觸發標記
      "ref": "MF_QuestTrigger",
      "flag": "SetEnable" } }                           //   觸發器啟用時一併出現
]
```
- **`scale`**（XSCL）：等比例縮放倍率。`1.0` = 預設（不寫 XSCL 子記錄）。適用於 static、
  家具、燈光；actor 在遊戲中會忽略它。必須 > 0。
- **`initiallyDisabled`**（record flag `0x800`）：ref 存在於 cell 中但不可見、無碰撞，
  直到被明確啟用（透過 script、任務階段、或 `enableParent`）。常見模式：隱藏物件 +
  `enableParent` 指向任務觸發 XMarker。
- **`enableParent`**（XESP）：此 ref 的啟用狀態跟隨另一個放置 ref（`ref` =
  placement editorId、`references[]` label、或外部 ref）。在**每個** `placements[]` 項目與
  `references[]` label 都存在之後才解析，所以 `ref` 可以指向列表中**更早或更後面**的
  placement——順序無所謂。
  - `flag`：`SetEnable`（父啟用時我也啟用 — 預設）、`SetDisable`（反轉）、
    `PopIn`（出現時不淡入，避免閃爍）。
- **`lock`**（XLOC）：鎖住門或容器（僅 `PlacedObject`）。
  - `level`：`novice` | `apprentice` | `adept` | `expert` | `master` | `requiresKey` |
    `inaccessible`，或原始 byte 值字串（如 `"50"`）。
  - `key`（選填）：可繞過鎖的物品 ref。
- **`ownership`**（XOWN）：誰擁有此物件 — 拿走會算偷竊。
  - `owner`：FACT 或 NPC ref。
  - `rank`（選填，int ≥ 0）：所需幫派等級（對 NPC 擁有者無效；`0` = 任何成員）。
- **`count`**（XCNT）：放置物品的堆疊數量（如 50 枚金幣）。`0` = 單個（不寫子記錄）。
  對 actor 或 static 無意義。

### map markers (XMRK) — 永久的世界地圖圖示

`mapMarkers[]` 把可發現／可快速旅行的**地點標記**加到世界地圖 — 與任何
quest 無關：

```jsonc
"mapMarkers": [
  { "editorId": "MF_HiddenCamp", "name": "Hidden Camp",
    "worldspace": "Skyrim.esm:0x00003C",                 // Tamriel
    "position": { "x": 0, "y": -9000, "z": 0 },
    "type": "Camp",                                       // MarkerType: City/Town/Settlement/Cave/Camp/Fort/Landmark/…
    "flags": ["Visible", "CanTravelTo"] }                 // empty = hidden until the player discovers it
]
```

- 每一項都在原版 **MapMarker** static（`0x10`）上建立一個 `PlacedObject`，帶上一個 XMRK
  `MapMarker`（name + type + flags），加進該 worldspace 的 **persistent cell**，與
  原版標記並列。`type` 是一個 `MapMarker.MarkerType` 名稱；`flags` 是 `Visible | CanTravelTo |
  ShowAllIsHidden`。
- 因為它是一個 persistent 的具名 ref，一個地圖標記**也能**是一個 `objectives[].targets[]` 來源
  （用一個 `forced:<editorId>` 別名綁定它）— 一個指向某地圖位置的 quest 箭頭。結合目標標記
  + 一個 xmarker 錨點 + 一個地圖標記的完整範例：`examples/quest-markers.json`。

