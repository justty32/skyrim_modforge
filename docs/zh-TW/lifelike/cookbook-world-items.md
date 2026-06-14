<!-- World & items patterns -->
# 食譜手冊 — 世界與物品

← [cookbook index](cookbook-index.md) | [lifelike hub](README.md)

## 「可用的室內 cell」（光照 + 地板，而非黑色虛空）

一個全新的室內 cell 需要三樣東西，否則就是一個會讓你墜落的全黑虛空：

```jsonc
{ "cells": [
    { "editorId": "MF_Hall", "name": "Forged Hall",
      "template": "Skyrim.esm:0x0165A8" }   // Breezehome — inherits interior lighting via CopyCellEnv
  ],
  "statics": [ { "editorId": "MF_Floor", "model": "..." } ],  // or place vanilla WRIntFloorSTMid01Large 0x1044AA
  "placements": [
    // a 3×3 floor grid at 256 spacing, a non-PortalStrict omni key light, wall pieces
    { "base": "Skyrim.esm:0x1044AA", "cell": "MF_Hall", "position": { "x": 0,   "y": 0, "z": 0 } },
    { "base": "Skyrim.esm:0x0C82AE", "cell": "MF_Hall", "position": { "x": 0,   "y": 0, "z": 200 } } // WRShadowOmni key light
  ] }
```

光照來自 `template`（程式碼路徑 `CopyCellEnv`）；地板 + 燈光只是 placement。
使用非 PortalStrict 的 omni 燈光（`WRShadowOmni 0x0C82AE`）——在沒有 portal 的 cell 裡，`PortalStrict` 燈光照不亮任何東西。

## 「以等級調整的敵人填滿地下城」（encounter zone + 等級化生成）

把**符合等級**的敵人放進一個區域：encounter zone（ECZN）設定等級範圍 +
重生；每個生成物的 `base` 都是一個 **LeveledNpc list**，引擎會在載入時擲出一個依等級調整的 actor。

```jsonc
{ "encounterZones": [
    { "editorId": "MF_BanditDenZone", "minLevel": 4, "maxLevel": 0,   // max 0 = uncapped (scales w/ player)
      "flags": [ "MatchPcBelowMinimumLevel" ] }
  ],
  "cells": [
    { "editorId": "MF_BanditDen", "name": "Bandit Den",
      "template": "Skyrim.esm:0x0165A8",          // lighting (else black) — see "Usable interior cell"
      "encounterZone": "MF_BanditDenZone" }       // the whole cell's level scaling/respawn
  ],
  "placements": [
    // ... a floor grid (WRIntFloorSTMid01Large 0x1044AA) so you don't fall into the void ...
    { "base": "Skyrim.esm:0x01E79C", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditMeleeAny (NPC_)
      "position": { "x": -180, "y": 120, "z": 0 } },
    { "base": "Skyrim.esm:0x01B0D5", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditMissileNordM (NPC_ archer)
      "position": { "x":  180, "y": 120, "z": 0 } },
    { "base": "Skyrim.esm:0x01B0E1", "cell": "MF_BanditDen", "kind": "npc",   // LvlBanditBossNordM (NPC_ boss)
      "position": { "x": 0, "y": -120, "z": 0 }, "encounterZone": "MF_BanditDenZone" }  // per-ref XEZN (optional)
  ] }
```

- **CRITICAL — confirmed CTD（It.36, 2026-06-02）：** `LChar*` formid（例如 `0x03DECD` `LCharBanditMeleeAny`）
  是 **LVLN 記錄**，而把一個原始 LVLN 當作 ACHR base 會**讓 Skyrim 在載入時崩潰**。請改放 `LvlBandit*`
  **NPC_ wrapper**（`Lvl…` 前綴 = NPC_，可安全放置；`LChar…` 前綴 = LVLN，絕不要直接放置）。一個 **in-spec**
  的 `leveledNpcs` base 會自動偵測為 actor（build 會為此發出警告）。
- `maxLevel 0` = uncapped（vanilla 慣用法；`HelgenZone` 是 min 6 / max 0）。`MatchPcBelowMinimumLevel`
  讓低等級玩家獲得依玩家等級調整的生成物，而非被夾在 `minLevel`；`NeverResets` 讓已清除的巢穴維持清除狀態。
- 驗證：`validate` → `build` → `dump`（cell 的 `encZone ->`、每個 `placed npc -> base …`、ECZN 的
  `levels [min..max]`）以及 `eczndiag <plugin> <0xFORMID>`。可用的 spec：`examples/encounter_spec.json`。
- **Navmesh：** 一個全新的 cell **沒有 navmesh**，所以生成物會站在放置的位置、無法尋路/追擊，
  直到你在 CK 裡為該 cell 製作 navmesh。Actor 會吸附到地板，所以 placement 座標很寬容，但
  移動/戰鬥 AI 在製作 navmesh 之前不會啟動（在那之前僅限結構性）。

## 「可製作的物品」（COBJ recipe）

比看起來簡單：workbench 是一個單純的 keyword FormLink（預設為 forge），**不是**
CTDA condition；components 重用 container 的 item/count 形狀；perk/skill 閘控（`conditions`）是
可選的，一個基本 recipe 不需要任何閘控。

```jsonc
{ "recipes": [
    { "editorId": "MF_ForgeSword", "createdObject": "<MF_MySword>", "count": 1,
      // "workbench": "forge",   // named selector — forge is the default, can omit
      "components": [ { "item": "Skyrim.esm:0x05ACE4", "count": 3 },    // IngotIron
                      { "item": "Skyrim.esm:0x0800E4", "count": 1 } ] } // LeatherStrips
  ] }
```

## 「不用新 mesh 的重新貼圖」（TXST + alternateTextures）

重用一個 vanilla 物件的 `.nif`，並把它的其中一個材質指向你自己的
**TextureSet（TXST）**，藉此為它重新貼皮。不需要製作 mesh——只需要 texture 路徑。

```jsonc
{ "textureSets": [
    { "editorId": "MF_GildedRubbleTexture",
      "diffuse": "ModForge\\rubble\\gilded_rubble_d.dds",   // relative to Data\Textures\ — OMIT the "Textures\" prefix
      "normal":  "ModForge\\rubble\\gilded_rubble_n.dds",
      "flags": [ "NoSpecularMap" ] }
  ],
  "statics": [
    { "editorId": "MF_GildedRubble",
      "model": "Dungeons\\Nordic\\Rubble\\NorRubblePiece03.nif",   // a VANILLA mesh, reused as-is
      "alternateTextures": [
        { "name": "NorRubblePiece03:0", "index": 0,                 // material/3D-name inside the .nif
          "textureSet": "MF_GildedRubbleTexture" } ] }
  ] }
```

陷阱：
- **路徑根目錄。** TXST 槽位路徑相對於 `Data\Textures\`，所以它們會省略開頭的
  `Textures\`（與 `model` 省略 `Meshes\` 一致）。Validate 會拒絕多餘的 `Textures\` 前綴。
- **`name` 必須與 mesh 相符。** 它是來自 `.nif` 之 shader properties 的 `<3DName>:<index>`
  （CK *Model Data → AltTex*，或 NifSkope 的 `BSLightingShaderProperty` 名稱）。錯誤的名稱
  什麼都不會替換——而且是無聲地。對照一個 vanilla 範例：`txstdiag <Skyrim.esm>` 列出每個 TXST，`dump`
  印出一筆記錄的 `altTexture` 行，而 vanilla STAT `NorExtRubblePiece03_HeavySN` 展示了本食譜所複製的
  `NorRubblePiece03:0` / index 0 模式。
- **`textureSet` 是一個 ref**——一個 in-spec 的 TXST editorId，或一個 vanilla `<master>:0xFORMID`。
- **`.dds` 由你製作。** ModForge 寫入記錄 + 參照；它無法建立或算繪
  texture 內容，而 headless 工具鏈也無法確認替換看起來是否正確——只有
  啟動 Skyrim 才行。請把你製作的 `.dds` 檔放進 mod 資料夾的 `Data/Textures/<your path>/` 之下。

進行結構性驗證：`validate` → `build` → `txstdiag <out.esp>`（已寫入的槽位）以及
`dump <out.esp>`（`altTexture` 接線 + 它的 `-> <TXST>` 目標）。

## 「可製作 + 可強化的武器」（perk 閘控的 forge + grindstone 強化 + 冶煉）

一條完整的鍛造鏈：forge 出武器（perk 閘控，所以只有在你取得
SteelSmithing 之後才會出現）、在 grindstone 改良它、並把礦石冶煉成它所花費的 ingot。`workbench` 是
一個 **named selector**（`forge`/`sharpeningWheel`/`armorTable`/`smelter`/`tanningRack`/`skyforge`）；
recipe 的 `kind`（`craft`/`temper`/`smelt`/`breakdown`）會設定一個合理的預設工作台，所以你常常可以
省略它。一個 `temper` recipe 的 `createdObject` **就是**武器本身，並透過在 smithing 的 `HasPerk` 之前
加上 `TemperIsEnchanted`(`or: true`) 守衛來對齊 vanilla。Conditions 是共用的
CTDA `ConditionSpec`（`function`/`param`/`comparison`/`value`/`or`）。用
`find Skyrim.esm SteelSmithing Perk` 探查 perk/ingredient 的 FormID；用 `cobjdiag <esp> <0xID>` 檢視任一 recipe。
一個完整可執行的版本是 [`examples/smithing_spec.json`](../../../examples/smithing_spec.json)。

```jsonc
{ "recipes": [
    // FORGE — perk-gated craft (SteelSmithing perk = Skyrim.esm:0x0CB40D)
    { "editorId": "MF_ForgeBlade", "kind": "craft", "createdObject": "<MF_MyBlade>",
      "workbench": "forge",
      "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 2 },     // SteelIngot
                      { "item": "Skyrim.esm:0x0800E4", "count": 1 } ],   // LeatherStrips
      "conditions": [ { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] },

    // GRINDSTONE — temper (createdObject = the blade; enchant-guard + perk, exactly like vanilla)
    { "editorId": "MF_TemperBlade", "kind": "temper", "createdObject": "<MF_MyBlade>",
      "workbench": "sharpeningWheel",
      "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 1 } ],   // SteelIngot
      "conditions": [
        { "function": "TemperIsEnchanted", "comparison": "!=", "value": 1, "or": true },
        { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] },

    // SMELTER — ore -> ingot (no conditions)
    { "editorId": "MF_SmeltIron", "kind": "smelt", "createdObject": "Skyrim.esm:0x05ACE4",
      "components": [ { "item": "Skyrim.esm:0x071CF3", "count": 1 } ] }   // IronOre -> IronIngot
  ] }
```

已通過結構性驗證（`dump`/`cobjdiag` 顯示 temper recipe 除了 target/perk 之外，與 vanilla
`TemperWeaponSteelSword` 逐位元組相符）。**遊戲內尚未確認**——recipe
是否真的出現在工作台 / 強化是否生效，需要實際執行遊戲才能確認。
