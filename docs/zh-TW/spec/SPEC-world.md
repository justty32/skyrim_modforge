# ModForge spec — 室內 cell、放置與光照

← [index](SPEC-index.md) · 室外世界、清單、生怪與商販 → [SPEC-worldspaces](SPEC-worldspaces.md)

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
  placement editorId 或外部 ref）。
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

### lights — 自訂光源 (LIGT)
定義一個自訂光源（顏色、半徑、閃爍）並像任何其他 base object 一樣放置它。ModForge
本來就能*放置*原版光源；`lights[]` 讓你能編寫新的。
```jsonc
"lights": [
  { "editorId": "MF_EerieLight", "name": "Eerie Glow",
    "color": { "r": 70, "g": 230, "b": 110 },   // RGB 0..255
    "radius": 420, "fadeValue": 1.0,            // radius in units; fade = brightness multiplier
    "flags": [ "Dynamic", "Flicker" ],          // Light.Flag names: Dynamic / Flicker / FlickerSlow /
                                                //   Pulse / PulseSlow / OffByDefault / SpotLight / CanBeCarried / …
    "falloffExponent": 1.0, "fov": 90.0,        // optional (spotlights)
    "value": 0, "weight": 0.0 } ]               // optional (only matter for a carriable light)
```
用一個普通的 `placements[]` 項目放置它，其 `base` 為該光源的 `editorId`：
```jsonc
"placements": [ { "base": "MF_EerieLight", "cell": "Skyrim.esm:0x0133C6",
                  "position": { "x": -650, "y": 100, "z": 140 } } ]
```
一個 LIGT base 半徑預設為 256，fade 為 1.0。用 `Dynamic` 讓它照亮穿過它的移動 actor，
用 `Flicker`/`Pulse` 做火把／蠟燭／魔法效果。Validation 會檢查 flag 名稱、顏色範圍
（0..255）與 radius > 0。（一個獨立光源沒有 model — 想要可見的*燈具*，也放一個原版
火把/燈籠 static，或把光源掛在一個火把 object 上。）

### lighting（光照）
Skyrim 室內之所以黑暗是出於*編寫的選擇*，不是引擎限制 — 光照幾乎完全是
record 層的事。三種 record 類型協同運作：

- **LGTM (LightingTemplate)** — 可重用的室內 ambient/directional/fog + DALC 設定。
- **IMGS (ImageSpace)** — 螢幕空間 HDR 眼睛適應、bloom、電影感色彩與 tint。
- **內嵌 XCLL** — 對特定光照欄位的逐 cell 覆寫（其餘從 LGTM 繼承）。

```jsonc
"lightingTemplates": [
  { "editorId": "MF_BrightCaveLGTM",
    "template": "Skyrim.esm:0x0300E2",         // DeepCopy DefaultLightingTemplate as base
    "ambientColor":     { "r": 150, "g": 155, "b": 170 },
    "directionalColor": { "r": 210, "g": 210, "b": 200 },
    "fogNear": 0, "fogFar": 8192,
    "directionalAmbient": {                    // DALC — six-direction hemisphere fill
      "scale": 1.0,
      "zPlus":  { "r": 200, "g": 205, "b": 215 },
      "zMinus": { "r": 120, "g": 122, "b": 130 },
      "xPlus":  { "r": 170, "g": 172, "b": 180 },
      "xMinus": { "r": 170, "g": 172, "b": 180 },
      "yPlus":  { "r": 170, "g": 172, "b": 180 },
      "yMinus": { "r": 170, "g": 172, "b": 180 } } }
],
"imageSpaces": [
  { "editorId": "MF_BrightIMGS",              // no template — start from engine defaults (see pitfall below)
    "brightness": 1.35, "saturation": 1.2, "contrast": 1.0,
    "bloomScale": 0.8, "sunlightScale": 1.2, "white": 1.5 }
],
"cells": [
  { "editorId": "MF_BrightRoom", "name": "Bright Test Room",
    "template": "Skyrim.esm:0x0165A8",         // copy Breezehome env as structural base
    "lightingTemplate": "MF_BrightCaveLGTM",  // in-spec LGTM editorId (or "Skyrim.esm:0xFORMID")
    "imageSpace": "MF_BrightIMGS" }            // in-spec IMGS editorId (or "Skyrim.esm:0xFORMID")
]
```

**編寫模型 — 模板複製 + 覆寫。** 在一個 LGTM 或 IMGS 上把 `template` 設為一個原版
record `"<master>:0xFORMID"`；它會被 DeepCopy 作為 base，接著只有你指定的欄位會覆寫它
（所有欄位都是選填；省略一個就保留原版值）。沒有 `template` →
引擎中性預設值（一個空白 IMGS 的 HDR 欄位全為零 — 見下方 pitfall）。

**LGTM 欄位**（`lightingTemplates[]`）：
`editorId`、`template`（原版 LGTM ref）；顏色 `ambientColor` / `directionalColor` /
`fogNearColor` / `fogFarColor`（RGB 0..255）；浮點 `directionalRotationXY` / `directionalRotationZ` /
`directionalFade` / `fogNear` / `fogFar` / `fogMax` / `fogClipDistance` / `fogPower` /
`lightFadeStart` / `lightFadeEnd`；`directionalAmbient`（DALC，見下方）。

**DALC — `directionalAmbient`**（`AmbientColorsSpec`）：六方向半球填充 —
`xPlus` / `xMinus` / `yPlus` / `yMinus` / `zPlus` / `zMinus` + `specular`（皆為 `ColorSpec`）
與 `scale`（float）。Skyrim 沒有全域照明；DALC 是從各方向照亮一個黑暗房間的
ambient 填充的實用替代方案。在一個 LGTM 上它對應到
`DirectionalAmbientColors`；在一個內嵌 CELL XCLL 上它對應到 `AmbientColors`（不同的
Mutagen 欄位，相同的資料）。

**IMGS 欄位**（`imageSpaces[]`）：
`editorId`、`template`（原版 IMGS ref）；
HDR：`eyeAdaptSpeed` / `eyeAdaptStrength` / `bloomBlurRadius` / `bloomThreshold` / `bloomScale` /
`receiveBloomThreshold` / `white` / `sunlightScale` / `skyScale`；
電影感（1 = 中性）：`brightness` / `contrast` / `saturation`；
Tint：`tintAmount` / `tintColor`（ColorSpec）。「明亮、乾淨、飽和」的觀感主要靠 IMGS
（提高 `brightness`、`saturation`，降低 `bloomThreshold`）。

**CELL 光照欄位**（在一個 `cells[]` 項目上）：
- `lightingTemplate` — in-spec LGTM `editorId` **或**原版 `"<master>:0xFORMID"` LGTM ref。
- `imageSpace` — in-spec IMGS `editorId` **或**原版 `"<master>:0xFORMID"` IMGS ref。
- `lighting` — 內嵌 `CellLightingSpec`：與 LGTM 相同的顏色/fog/fade 欄位（注意：
  CELL 用 `lightFadeBegin`/`lightFadeEnd`，不是 `lightFadeStart`/`lightFadeEnd`）加上
  `directionalAmbient`（DALC → `AmbientColors`）與 `inherit`（下方的 flag 名稱清單）。

**Inherit flags 規則。** 一個室內 CELL 必須帶有一個 XCLL record，否則它會渲染成全黑。
`lighting.inherit` 清單指名哪些欄位要從 `lightingTemplate` 拉取，而非從內嵌
XCLL。有效的 flag 名稱：`AmbientColor` / `DirectionalColor` / `FogColor` / `FogNear` /
`FogFar` / `DirectionalRotation` / `DirectionalFade` / `ClipDistance` / `FogPower` / `FogMax` /
`LightFadeDistances`。
特殊情況：
- 沒有內嵌 `lighting` **且**有設定 `lightingTemplate` → 該 cell 繼承**所有** flag
  （完全由模板驅動；build 會寫出一個設定了每個 inherit flag 的 XCLL）。
- 一個欄位同時內嵌設定且列在 `inherit` 中 → 模板勝出（會警告）。

**IMAD vs IMGS。** `imageSpaces[]` 產生 IMGS *base* records（HDR/電影感/tint 附加到一個
CELL）。既有的 `imageSpaceModifiers[]`（IMAD）是由 spell/script 觸發的螢幕後處理曲線 —
一種不同的 record 與不同的工作流。

**與 `cells[].template` 共存。** 既有的 `template` 欄位（複製一整個原版
室內的光照/水體 env 作為結構性 base）仍然有效；`lightingTemplate` / `imageSpace`
/ `lighting` 接著疊加在上面以精準覆寫你在意的那些欄位。

**Pitfall — 空白 IMGS。** 一個沒有 `template` 的全新 IMGS 從引擎零值的 HDR 數值起步
（`bloomThreshold`、`eyeAdaptSpeed`、`white` 全為 0）。結果是過亮或泛白的
觀感。為了正常的外觀，最好給該 IMGS 一個原版 `template`（例如
`Skyrim.esm:0x1A27E0` `DefaultImageSpace`）並只調整你想要的欄位，而非
從頭編寫 HDR。用 `imgsdiag <Skyrim.esm>` 列出原版 IMGS records 及其
數值。

**Diagnostics（診斷）。**
- `lgtmdiag <esp> [0xFORMID]` — 傾印一個 LightingTemplate 的 ambient/directional/fog 顏色 + DALC。
  沒有 FormID = 列出檔案中所有 LGTM。用於驗證 build 出的結果，或在把一個原版
  template 當作 `template` 使用前讀取其數值。
- `imgsdiag <esp> [0xFORMID]` — 傾印一個 ImageSpace 的 HDR / 電影感 / tint。沒有 FormID 時
  同樣是列出全部的行為。

完整範例：`examples/lighting.json`（明亮室內：自訂 LGTM + IMGS、模板驅動光照的 cell、
DALC 半球填充）。

**戶外 / 天氣 IMGS。** 上面的 LGTM + CELL XCLL 路徑是**僅限室內**的。在室外，
ambient 光照來自 Weather record 自身的 sky/sunlight/ambient 顏色通道
（`WeatherSpec` 的 `skyUpperColor` / `sunlightColor` / `ambientColor` 逐時段欄位 —
已支援）。室外的螢幕空間色彩分級使用另一套機制：Weather
record 的逐時段 **ImageSpace** 槽位。透過 `weathers[].imageSpaces` 設定它們：

```jsonc
"imageSpaces": [
  { "editorId": "MF_OutdoorBrightIMGS", "template": "Skyrim.esm:0x012F88",
    "brightness": 1.1, "saturation": 1.25, "bloomScale": 0.9, "sunlightScale": 1.2, "skyScale": 0.12 }
],
"weathers": [
  { "editorId": "MF_BrightWeather",
    "template": "Skyrim.esm:0x10E1F2",                       // SkyrimClear_A — inherit clouds + tuned sky
    "imageSpaces": { "default": "MF_OutdoorBrightIMGS" } }   // default fills all four ToD
]
```

`weathers[].imageSpaces` 欄位：`default`（填補任何未設定的時段）、`sunrise`、`day`、
`sunset`、`night`。每個值是一個 in-spec `imageSpaces[]` editorId **或**一個原版
`"<master>:0xFORMID"` IMGS ref。一個 `default` 就足以將全部四個
時段統一分級。

**Weather `template`（雲！）。** 一個**從頭建立的 weather 沒有雲**（且只有
基線 sky 顏色）— 天空是一片平坦的空漸層。把 `weathers[].template` 設為一個原版
weather `"<master>:0xFORMID"`（例如 `Skyrim.esm:0x10E1F2` = SkyrimClear_A）：複製品會繼承它的
雲層 + 雲貼圖 + 逐時段 sky/sunlight/ambient 顏色 + 大氣效果，
接著你**只**覆寫你設定的部分（留為 null 的顏色保留模板的；一個空的 `clouds`
清單保留模板的雲）。這是推薦的室外 base：複製一個原版晴朗
weather 以取得正常的多雲天空，再透過 `imageSpaces` 推動螢幕分級。兩個槓桿保持
獨立 — **天空亮度** = weather 的 `skyUpperColor`/`skyLowerColor` + IMGS 的
`skyScale`；**地面/場景** = `sunlightScale` + weather 的 `ambientColor`。

> **注意：** LGTM / CELL 路徑**不適用**於室外 cell — 不要把
> `lightingTemplate` 或 `imageSpace` 直接附加到一個 weather。weather 自身的顏色欄位
> 驅動室外 ambient；weather 上的 IMGS 驅動螢幕空間 HDR/bloom/飽和度。

**遊戲內測試（非侵入式）。** `fw <weatherFormID>`（ForceWeather）會立即啟用該 weather，
不需編輯任何氣候或 worldspace。用
`find <esp> MF_BrightWeather Weather` 找出 FormID，並在主控台把十六進位 FormID 傳給 `fw`
（例如 ESL 槽位用 `fw 0800`）。用
`weatherdiag <esp> <0xFormID>` 驗證 IMGS 是否接妥 — `ImageSpaces` 那一行必須在
全部四個時段都顯示自訂 IMGS FormKey。測試視覺結果不需要任何氣候/worldspace 指派。

完整範例：`examples/weather_bright.json`（透過 `imageSpaces.default` 的室外 IMGS 分級）。
交叉參照：見上方室內 [lighting](#lighting) 小節的 LGTM / CELL / XCLL。
