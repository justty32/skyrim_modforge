<!-- Cells、世界空間、等級列表、遭遇區域、商販 -->
# ModForge 規格說明 — 世界、Cells 與商販

← [目錄](SPEC-index.md)

### cells 與 placements — 將物件放入世界
```jsonc
"cells": [
  { "editorId": "MF_TestRoom", "name": "ModForge Test Room",
    "template": "Skyrim.esm:0x0165A8" }                          // 從 Breezehome 複製燈光（否則全黑）
],
"placements": [
  { "base": "MF_Smith", "cell": "MF_TestRoom",
    "position": { "x": 0, "y": 0, "z": 0 },
    "rotation": { "x": 0, "y": 0, "z": 0 } },                    // 旋轉以度為單位
  { "base": "MF_Chest", "cell": "Skyrim.esm:0x01605E",           // 放入原版室內 cell
    "position": { "x": 100, "y": 0, "z": 0 } },
  { "base": "MF_Coin", "worldspace": "Skyrim.esm:0x00003C",      // 放入開放世界（Tamriel）
    "position": { "x": 22528, "y": 22528, "z": 200 } }
]
```
- 一個 `placement` 目標為**室內** `cell` **或**室外 `worldspace` 其中之一：
  - **室內** — `cell` 為 spec 內新室內 cell 的 `editorId`，**或**外部/原版室內 cell 的 `"<master>:0xFORMID"`。沒有 `template` 的新 cell 會呈現**全黑**且**沒有地板**：將 cell 的 `template` 設為原版室內並在其中放置地板靜態物件。`position` 相對於 cell。
  - **室外** — `worldspace` 為世界空間 ref `"<master>:0xFORMID"`（Tamriel = `Skyrim.esm:0x00003C`）。`position` 為**世界**座標；若 `worldspace` 和 `cell` 同時設定，以 `worldspace` 為準。
- `base` 為 *ref*；NPC 會變為 `PlacedNpc`，其他則為 `PlacedObject`（`kind` 可覆寫）。`rotation` 的單位為**度**。`persistent: true` 將其放入 cell 的永久清單中。
- **原版 placement** 會覆寫 cell/worldspace 以*加入*你的 reference（原版內容不受影響）。需要遊戲的 `Data` 資料夾——若不在預設的 Steam 路徑，請設定 `MODFORGE_SKYRIM_DATA`。

### worldspaces（WRLD）與 regions（REGN）— 室外世界與天氣
建立一個**新的**室外世界空間並附加氣候，並定義 **regions**（世界空間內的區域），其**天氣表**決定該處播放哪種天氣：
```jsonc
"worldspaces": [
  { "editorId": "MFTestWorld", "name": "ModForge Test Vale",
    "climate": "Skyrim.esm:0x000812",      // CLMT — 天空/光照週期（實際上為必要）
    "water":   "Skyrim.esm:0x000018",      // WATR — DefaultWater（可選）
    "parent":  "Skyrim.esm:0x00003C",      // 上層 WRLD = Tamriel（可選）
    "flags":   ["SmallWorld", "CannotFastTravel"],
    "defaultLandHeight":  -27000,          // 防浸水修正：省略這些會使水面預設為 0，
    "defaultWaterHeight": -14000,          //   令海平面以下的地形被淹沒
    "map": { "northwestX": -4, "northwestY": 4, "southeastX": 4, "southeastY": -4,
             "cameraInitialPitch": 50, "cameraMinHeight": 50000, "cameraMaxHeight": 80000 } }
],
"regions": [
  { "editorId": "MFTestWorldWeather", "worldspace": "MFTestWorld",
    "edgeFallOff": 1024, "mapColor": "0x3CA0F0", "weatherPriority": 60,
    "weather": [
      { "weather": "Skyrim.esm:0x10E1F2", "chance": 60 },           // SkyrimClear（相對權重）
      { "weather": "Skyrim.esm:0x10E1F1", "chance": 30 },           // SkyrimCloudy
      { "weather": "Skyrim.esm:0x10E1F0", "chance": 10 } ],         // SkyrimClearSN
    "area": [ { "x": -16384, "y": -16384 }, { "x": 16384, "y": -16384 },
              { "x": 16384, "y": 16384 }, { "x": -16384, "y": 16384 } ] }   // >=3 個世界座標點
  ]
```
- **worldspaces**（WRLD）：一個新的室外世界。`climate` 為 CLMT *ref*（原版預設 = `Skyrim.esm:0x000812`）——若無此設定，世界將**沒有天空/光照週期**。`defaultLandHeight`/`defaultWaterHeight` 預設為 Tamriel 的值（-27000 / -14000）——**保留這些值**，因為水面預設為 0 會淹沒整個世界。
- **regions**（REGN）：`worldspace` 內的一個區域。`area` 為**至少 3 個**世界座標點組成的多邊形（非 cell 格子）。`weather` 為選取當前天氣的表——每個條目為一個 WTHR *ref* 加上相對 `chance`。
- 警告 **僅限記錄層——非可遊玩的世界。** ModForge 會輸出 WRLD/REGN 記錄，但真正可步行的室外地區還需要**地形（LAND 高度圖）、LOD 網格與尋路網格**，這些必須在 **Creation Kit** 中製作。此功能**在結構上已驗證**但**尚未在遊戲中確認**。

### 等級列表與容器
```jsonc
"leveledItems": [
  { "editorId": "MF_LootList", "chanceNone": 25,
    "flags": ["CalculateFromAllLevelsLessThanOrEqualPlayer"],
    "entries": [ { "reference": "MF_Blade", "level": 1, "count": 1 },
                 { "reference": "MF_Coin",  "level": 1, "count": 5 } ] }
],
"containers": [
  { "editorId": "MF_Chest", "name": "Forged Chest",
    "items": [ { "item": "MF_Coin", "count": 10 }, { "item": "MF_Apron", "count": 1 } ] }
]
```
- `leveledItems`（LVLI）和 `leveledNpcs`（LVLN）是受等級限制的加權清單：每個 `entry` 的 `reference` 為 *ref*，以 `level` 作為門檻，重複 `count` 次。`chanceNone`（0–100）為清單不產出任何東西的機率；`flags` 名稱來自 LVLI/LVLN 旗標集合。
- `containers`（CONT）持有 `items`，每項為一個物品 *ref* 加上 `count`。（若要讓容器出現在世界中，需用 `placement` 放置，與其他物件相同。）

### 遭遇區與等級演員生成——在區域中填入等比例的敵人
兩個部分協同運作，以在區域中投放**適合等級**的敵人：

**1. 等級演員生成**使用 **NPC_ 包裝器**作為 `base`——一個 NPC_，其 TEMPLATE 鏈參照 LeveledNpc 清單（LVLN），讓引擎在生成時滾出一個適合等級的演員。

> **嚴重注意事項——已確認 CTD（It.36，2026-06-02）：** `LChar*` formid（例如 `0x03DECD` `LCharBanditMeleeAny`）是 **LVLN 記錄**，而將原始 LVLN 作為 ACHR 基底**會導致 Skyrim 在載入時崩潰**。請改用 `LvlBandit*` NPC_ 包裝器。命名規則：`Lvl…` 前綴 = NPC_（可安全放置）；`LChar…` 前綴 = LVLN（永遠不要直接放置）。

```jsonc
{ "base": "Skyrim.esm:0x01E79C", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditMeleeAny (NPC_)
  "position": { "x": -180, "y": 120, "z": 0 } }
```
- 使用 `find <Skyrim.esm> Lvl<…> Npc` 查找 NPC_ 包裝器（例如 `LvlBanditMeleeAny` `0x01E79C`）。其底層的 LVLN 清單**不是**有效的 placement 基底。
- 對於 spec 內用作 placement 基底的 `leveledNpcs` 清單，請加入 `"kind": "npc"` 以讓建置發出警告，而非靜默產生會崩潰的外掛。

**2. 遭遇區**（`encounterZones`，ECZN）設定生成所滾動的**等級範圍與重生**規則。
```jsonc
"encounterZones": [
  { "editorId": "MF_BanditDenZone",
    "minLevel": 4, "maxLevel": 0,            // 最低等級 4；maxLevel 0 = 無上限（隨玩家縮放）
    "flags": ["MatchPcBelowMinimumLevel"] }
],
"cells": [
  { "editorId": "MF_BanditDen", "template": "Skyrim.esm:0x0165A8",
    "encounterZone": "MF_BanditDenZone" }    // 連結 cell 的等級縮放與重生設定
]
```
- `maxLevel 0` 表示**無上限**——原版地下城的慣例（例如 `HelgenZone` 為最低 6 / 最高 0）。
- `flags`：`NeverResets`（已清除的地下城不重生），`MatchPcBelowMinimumLevel`（生成等級符合低等級玩家），`DisableCombatBoundary`（演員可在區域外追逐）。
- 使用 `eczndiag <plugin> <0xFORMID>` 檢查任何區域。
- **尋路網格注意事項：** 全新的 spec 內 cell **沒有尋路網格**，因此生成的演員在 CK 為其建立尋路網格之前無法移動。
- **已在遊戲中確認（It.36，2026-06-02）：** `coc MF_BanditDen`——cell 載入、強盜生成、無崩潰。完整流程：遭遇區、cell 範本、NPC_ 放置均已在 SSE 1.6.1170 中驗證。

### 商販／商人——一個可運作的店主
透過給予一個**派系** `vendor` 子物件，並讓 NPC 成為其成員，即可將 NPC 轉變為可運作的商店（買入 + 賣出）。
```jsonc
"factions": [
  { "editorId": "MF_ShopFaction", "name": "ModForge General Goods",
    "vendor": {
      "startHour": 8, "endHour": 20,
      "radius": 0,
      "buysStolen": false,
      "sellBuyList": "Skyrim.esm:0x06CB48",    // VendorItem 關鍵字的 FormList（交易類別）
      "notSellBuyList": true,                  // true ⇒ sellBuyList 為 NOT-sell 清單（交易除此之外的一切）
      "merchantContainer": "MF_ShopChestRef"   // 參照一個 PLACEMENT 的 editorId：已放置的商人箱子
    } }
],
"containers": [
  { "editorId": "MF_ShopChest", "name": "Merchant Chest",
    "items": [ { "item": "Skyrim.esm:0x072AE7", "count": 1 },    // VendorGoldMisc（商販的金幣池）
               { "item": "Skyrim.esm:0x09AF0A", "count": 10 } ] }  // 庫存等級清單
],
"placements": [
  { "editorId": "MF_ShopChestRef", "base": "MF_ShopChest", "cell": "MF_Shop", "persistent": true,
    "position": { "x": 0, "y": 256, "z": 0 } }
],
"npcs": [
  { "editorId": "MF_Shopkeeper", "name": "...", "race": "Skyrim.esm:0x013746",
    "factions": [ "MF_ShopFaction" ],
    "greeting": "Looking to buy?" }            // 問候語使其可對話——提示框所需的必要條件
]
```
- **`merchantContainer`** 必須參照一個 **placement** 的 `editorId`（已放置的箱子 REFR），而非裸容器——只有*已放置*的 ref 才持有引擎讀取的金幣/庫存。在箱子中放入 `VendorGoldMisc`（`Skyrim.esm:0x072AE7`）讓商販有錢購物。
- **成員資格即店主。** 建置會**自動加入** `JobMerchantFaction`（`Skyrim.esm:0x051596`）至該 NPC，因為原版通用的「我想交易」主題需要滿足 `GetInFaction JobMerchantFaction` + `GetOffersServicesNow`。
- **可對話。** 與所有自訂 NPC 相同的規則：交易提示只有在 NPC 開啟對話選單後才會出現，而這需要一句 `greeting` 或自訂的 `dialogue[]`。
- 使用 `factdiag <plugin> <0xFORMID>` 檢查；與原版商人比較，例如 `factdiag <Skyrim.esm> 0x09CAF5`（Belethor's General Goods）。
- **已在遊戲中確認 2026-05-31：** FACT/箱子/成員資格已於 SSE 1.6.1170 中驗證——「我想交易」提示正確開啟以物易物選單。
