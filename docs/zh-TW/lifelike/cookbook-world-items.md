<!-- World & items patterns -->
# 食譜手冊 — 世界建構與物品

← [目錄](cookbook-index.md) | [lifelike 主頁](README.md)

## 「可用的室內空間」（光照 + 地板，而非漆黑的虛空）

全新的室內空間需要三樣東西，否則就會變成漆黑一片、腳下空無一物、會讓你直接墜落的虛空：

```jsonc
{ "cells": [
    { "editorId": "MF_Hall", "name": "Forged Hall",
      "template": "Skyrim.esm:0x0165A8" }   // Breezehome — 透過 CopyCellEnv 繼承室內光照
  ],
  "statics": [ { "editorId": "MF_Floor", "model": "..." } ],  // 或放置原版 WRIntFloorSTMid01Large 0x1044AA
  "placements": [
    // 間距 256 的 3×3 地板網格、一個非 PortalStrict 的全向主光源、牆面元件
    { "base": "Skyrim.esm:0x1044AA", "cell": "MF_Hall", "position": { "x": 0,   "y": 0, "z": 0 } },
    { "base": "Skyrim.esm:0x0C82AE", "cell": "MF_Hall", "position": { "x": 0,   "y": 0, "z": 200 } } // WRShadowOmni 主光源
  ] }
```

光照來自 `template`（程式碼路徑 `CopyCellEnv`）；地板與光源只是放置物件。
使用非 PortalStrict 的全向光源（`WRShadowOmni 0x0C82AE`）——`PortalStrict` 光源在沒有入口（portal）的空間裡什麼都照不到。

## 「填充地城（帶等級縮放的敵人）」（遭遇區域 + 等級化重生點）

在區域內放置**與等級相符**的敵人：遭遇區域（ECZN）設定等級範圍與重生規則；每個重生點的 `base` 為 **LeveledNpc 列表**，讓引擎在載入時隨機生成一個依等級縮放的角色。

```jsonc
{ "encounterZones": [
    { "editorId": "MF_BanditDenZone", "minLevel": 4, "maxLevel": 0,   // max 0 = 無上限（隨玩家等級縮放）
      "flags": [ "MatchPcBelowMinimumLevel" ] }
  ],
  "cells": [
    { "editorId": "MF_BanditDen", "name": "Bandit Den",
      "template": "Skyrim.esm:0x0165A8",          // 光照（否則漆黑）— 參見「可用的室內空間」
      "encounterZone": "MF_BanditDenZone" }       // 整個空間的等級縮放/重生設定
  ],
  "placements": [
    // ... 地板網格（WRIntFloorSTMid01Large 0x1044AA），以免掉入虛空 ...
    { "base": "Skyrim.esm:0x01E79C", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditMeleeAny（NPC_）
      "position": { "x": -180, "y": 120, "z": 0 } },
    { "base": "Skyrim.esm:0x01B0D5", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditMissileNordM（NPC_ 弓手）
      "position": { "x":  180, "y": 120, "z": 0 } },
    { "base": "Skyrim.esm:0x01B0E1", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditBossNordM（NPC_ 首領）
      "position": { "x": 0, "y": -120, "z": 0 }, "encounterZone": "MF_BanditDenZone" }  // 單一引用的 XEZN（可選）
  ] }
```

- **嚴重——已確認 CTD（It.36，2026-06-02）：** `LChar*` formid（例如 `0x03DECD` `LCharBanditMeleeAny`）是 **LVLN 記錄**，而將原始 LVLN 作為 ACHR 基底**會導致 Skyrim 在載入時崩潰**。請改放置 `LvlBandit*` **NPC_ 包裝器**（`Lvl…` 前綴 = NPC_，可安全放置；`LChar…` 前綴 = LVLN，永遠不要直接放置）。**規格內**的 `leveledNpcs` 基底會自動辨識為角色（建置會為其發出警告）。
- `maxLevel 0` = 無上限（原版慣例；`HelgenZone` 為最低 6 / 最高 0）。`MatchPcBelowMinimumLevel` 讓低等級玩家獲得依其等級縮放的重生點，而非夾擠到 `minLevel`；`NeverResets` 讓清空的巢穴維持清空狀態。
- 驗證方式：`validate` → `build` → `dump`（cell 的 `encZone ->`、每個 `placed npc -> base …`、ECZN 的 `levels [min..max]`）以及 `eczndiag <plugin> <0xFORMID>`。可運作的規格範例：`examples/encounter_spec.json`。
- **導航網格：** 全新空間**沒有導航網格**，因此重生點會站在放置的位置，在你於 CK 中為該空間建立導航網格之前無法移動/追擊。角色會貼齊地板，所以放置座標的容錯度很高，但在建立導航網格之前移動/戰鬥 AI 都不會啟動（在那之前僅限結構層面）。

## 「可製作的物品」（COBJ 配方）

比看起來簡單：工作台是一個普通的關鍵字 FormLink（預設為鍛造爐），**而非** CTDA 條件；材料重複使用容器物品/數量的格式；技能樹限制（`conditions`）是可選的，基本配方不需要。

```jsonc
{ "recipes": [
    { "editorId": "MF_ForgeSword", "createdObject": "<MF_MySword>", "count": 1,
      // "workbench": "forge",   // 具名選擇器——鍛造爐為預設值，可省略
      "components": [ { "item": "Skyrim.esm:0x05ACE4", "count": 3 },    // IngotIron
                      { "item": "Skyrim.esm:0x0800E4", "count": 1 } ] } // LeatherStrips
  ] }
```

## 「無需新網格的重新貼圖」（TXST + alternateTextures）

透過重複使用原版物件的 `.nif`，並將其中一個材質指向你自己的**材質集（TextureSet / TXST）**，來為原版物件換裝。無需製作網格——只需填入貼圖路徑。

```jsonc
{ "textureSets": [
    { "editorId": "MF_GildedRubbleTexture",
      "diffuse": "ModForge\\rubble\\gilded_rubble_d.dds",   // 相對於 Data\Textures\ — 省略 "Textures\" 前綴
      "normal":  "ModForge\\rubble\\gilded_rubble_n.dds",
      "flags": [ "NoSpecularMap" ] }
  ],
  "statics": [
    { "editorId": "MF_GildedRubble",
      "model": "Dungeons\\Nordic\\Rubble\\NorRubblePiece03.nif",   // 原版網格，直接重複使用
      "alternateTextures": [
        { "name": "NorRubblePiece03:0", "index": 0,                 // .nif 內部的材質/3D 名稱
          "textureSet": "MF_GildedRubbleTexture" } ] }
  ] }
```

注意事項：
- **路徑根目錄。** TXST 插槽路徑相對於 `Data\Textures\`，因此須**省略**開頭的 `Textures\`（與 `model` 省略 `Meshes\` 的方式一致）。驗證程序會拒絕多餘的 `Textures\` 前綴。
- **`name` 必須與網格吻合。** 格式為 `.nif` 著色器屬性中的 `<3DName>:<index>`（CK *Model Data → AltTex*，或 NifSkope 的 `BSLightingShaderProperty` 名稱）。名稱錯誤時什麼都不會替換——且不會有任何提示。可比對原版範例：`txstdiag <Skyrim.esm>` 會列出每個 TXST，`dump` 會印出一筆記錄的 `altTexture` 行，原版 STAT `NorExtRubblePiece03_HeavySN` 即展示了本食譜複製的 `NorRubblePiece03:0` / index 0 模式。
- **`textureSet` 是一個引用**——規格內的 TXST editorId，或原版的 `<master>:0xFORMID`。
- **你需要自行製作 `.dds`。** ModForge 負責寫入記錄與引用；它無法建立或渲染貼圖內容，而且無頭工具鏈也無法確認替換看起來是否正確——只有實際啟動 Skyrim 才能。請將你製作好的 `.dds` 檔案放到 mod 資料夾的 `Data/Textures/<你的路徑>/` 之下。

結構驗證：`validate` → `build` → `txstdiag <out.esp>`（已寫入的插槽）以及 `dump <out.esp>`（`altTexture` 接線 + 其 `-> <TXST>` 目標）。

## 「可製作與鍛造的武器」（技能樹限制的鍛造 + 磨刀石淬鍊 + 熔煉）

完整的鍛造流程：鍛造爐製作武器（技能樹限制，唯有取得 SteelSmithing 後才顯示）、磨刀石上強化、礦石熔煉成它所需的礦錠。`workbench` 為**具名選擇器**（`forge`/`sharpeningWheel`/`armorTable`/`smelter`/`tanningRack`/`skyforge`）；配方的 `kind`（`craft`/`temper`/`smelt`/`breakdown`）會設定一個合理的預設工作台，所以通常可以省略。`temper` 配方的 `createdObject` **就是**武器本身，並依原版慣例在 smithing 的 `HasPerk` 之前加入 `TemperIsEnchanted`（`or: true`）防護。Conditions 為共用的 CTDA `ConditionSpec`（`function`/`param`/`comparison`/`value`/`or`）。用 `find Skyrim.esm SteelSmithing Perk` 探查技能樹/材料的 FormID；用 `cobjdiag <esp> <0xID>` 檢視任一配方。完整可執行版本請見 [`examples/smithing_spec.json`](../../examples/smithing_spec.json)。

```jsonc
{ "recipes": [
    // FORGE — 技能樹限制的製作（SteelSmithing perk = Skyrim.esm:0x0CB40D）
    { "editorId": "MF_ForgeBlade", "kind": "craft", "createdObject": "<MF_MyBlade>",
      "workbench": "forge",
      "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 2 },     // SteelIngot
                      { "item": "Skyrim.esm:0x0800E4", "count": 1 } ],   // LeatherStrips
      "conditions": [ { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] },

    // GRINDSTONE — 淬鍊（createdObject = 刀刃；附魔防護 + 技能樹，與原版完全相同）
    { "editorId": "MF_TemperBlade", "kind": "temper", "createdObject": "<MF_MyBlade>",
      "workbench": "sharpeningWheel",
      "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 1 } ],   // SteelIngot
      "conditions": [
        { "function": "TemperIsEnchanted", "comparison": "!=", "value": 1, "or": true },
        { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] },

    // SMELTER — 礦石 -> 礦錠（無條件）
    { "editorId": "MF_SmeltIron", "kind": "smelt", "createdObject": "Skyrim.esm:0x05ACE4",
      "components": [ { "item": "Skyrim.esm:0x071CF3", "count": 1 } ] }   // IronOre -> IronIngot
  ] }
```

已通過結構驗證（`dump`/`cobjdiag` 顯示 temper 配方除了目標/技能樹之外，與原版 `TemperWeaponSteelSword` 逐位元組吻合）。**尚未在遊戲中確認**——配方是否真的出現在工作台/淬鍊是否生效，需要實際執行遊戲才能驗證。
