<!-- Part 3/4 — World building and items -->
## 「可用的室內空間」（光照 + 地板，而非漆黑的虛空）

全新的室內空間需要三樣東西，否則就會變成漆黑一片、腳下空無一物的虛空：

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
    { "base": "Skyrim.esm:0x03DECD", "cell": "MF_BanditDen", "kind": "npc",   // LCharBanditMeleeAny
      "position": { "x": -180, "y": 120, "z": 0 } },
    { "base": "Skyrim.esm:0x01A348", "cell": "MF_BanditDen", "kind": "npc",   // LCharBanditMissileNordM（弓手）
      "position": { "x":  180, "y": 120, "z": 0 } },
    { "base": "Skyrim.esm:0x01A341", "cell": "MF_BanditDen", "kind": "npc",   // LCharBanditBossNordM（首領）
      "position": { "x": 0, "y": -120, "z": 0 }, "encounterZone": "MF_BanditDenZone" }  // 單一引用的 XEZN（可選）
  ] }
```

- **原版** LVLN 基底（`Skyrim.esm:0x…`）需要 `"kind": "npc"`——建置程序無法在無介面模式下讀取主檔的記錄類型。**規格內**的 `leveledNpcs` 基底會自動辨識為角色。
- `maxLevel 0` = 無上限（原版慣例；`HelgenZone` 為最低 6 / 最高 0）。`MatchPcBelowMinimumLevel` 讓低等級玩家獲得依其等級縮放的重生點；`NeverResets` 讓清空的巢穴維持清空狀態。
- 驗證方式：`validate` → `build` → `dump` 以及 `eczndiag <plugin> <0xFORMID>`。可運作的規格範例：`examples/encounter_spec.json`。
- **導航網格：** 全新空間**沒有導航網格**，因此重生點在 CK 中建立導航網格前無法移動或追擊。

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

透過重複使用原版物件的 `.nif`，並將其中一個材質指向你自己的**材質集（TXST）**，來為原版物件換裝。無需製作網格——只需填入貼圖路徑。

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
- **路徑根目錄。** TXST 插槽路徑相對於 `Data\Textures\`，因此須**省略**開頭的 `Textures\`。驗證程序會拒絕多餘的 `Textures\` 前綴。
- **`name` 必須與網格吻合。** 格式為 `.nif` 著色器屬性中的 `<3DName>:<index>`（CK 的 *Model Data → AltTex*，或 NifSkope 中的 `BSLightingShaderProperty` 名稱）。名稱錯誤時替換不會生效——且不會有任何提示。
- **`textureSet` 是一個引用**——規格內的 TXST editorId，或原版的 `<master>:0xFORMID`。
- **你需要自行製作 `.dds`。** ModForge 負責寫入記錄與引用；它無法建立或渲染貼圖內容。

驗證：`validate` → `build` → `txstdiag <out.esp>` 以及 `dump <out.esp>`。

## 「可製作與鍛造的武器」（技能樹限制的鍛造 + 磨刀石淬鍊 + 熔煉）

完整的鍛造流程：鍛造爐製作武器（需要 SteelSmithing 技能樹）、磨刀石上強化、礦石熔煉成礦錠。`workbench` 為**具名選擇器**（`forge`/`sharpeningWheel`/`armorTable`/`smelter`/`tanningRack`/`skyforge`）。`temper` 配方的 `createdObject` **就是**武器本身，並依原版慣例在 `HasPerk` 之前加入 `TemperIsEnchanted`（`or: true`）防護。完整可執行版本請見 [`examples/smithing_spec.json`](../../examples/smithing_spec.json)。

```jsonc
{ "recipes": [
    // 鍛造爐 — 技能樹限制的製作（SteelSmithing 技能樹 = Skyrim.esm:0x0CB40D）
    { "editorId": "MF_ForgeBlade", "kind": "craft", "createdObject": "<MF_MyBlade>",
      "workbench": "forge",
      "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 2 },     // SteelIngot
                      { "item": "Skyrim.esm:0x0800E4", "count": 1 } ],   // LeatherStrips
      "conditions": [ { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] },

    // 磨刀石 — 淬鍊（createdObject = 刀刃；附魔防護 + 技能樹，與原版完全相同）
    { "editorId": "MF_TemperBlade", "kind": "temper", "createdObject": "<MF_MyBlade>",
      "workbench": "sharpeningWheel",
      "components": [ { "item": "Skyrim.esm:0x05ACE5", "count": 1 } ],   // SteelIngot
      "conditions": [
        { "function": "TemperIsEnchanted", "comparison": "!=", "value": 1, "or": true },
        { "function": "HasPerk", "param": "Skyrim.esm:0x0CB40D", "comparison": "==", "value": 1 } ] },

    // 熔煉爐 — 礦石 -> 礦錠（無條件）
    { "editorId": "MF_SmeltIron", "kind": "smelt", "createdObject": "Skyrim.esm:0x05ACE4",
      "components": [ { "item": "Skyrim.esm:0x071CF3", "count": 1 } ] }   // IronOre -> IronIngot
  ] }
```

已通過結構驗證。**尚未在遊戲中確認**——需要實際執行遊戲才能驗證。

## 「自訂瞄準戰鬥法術」（MGEF + 彈體 + SPEL）

```jsonc
{ "magicEffects": [
    { "editorId": "MF_Firebolt", "archetype": "ValueModifier", "actorValue": "Health",
      "magicSkill": "Destruction", "resistValue": "ResistFire",
      "castType": "FireAndForget", "targetType": "Aimed", "baseCost": 12.0,
      "flags": [ "Hostile", "Detrimental", "NoArea" ],   // 不加 Recover（這是即時效果）
      "projectile": "Skyrim.esm:0x10FBEA",               // 重複使用原版火焰箭彈體（可見光束 + 命中效果）
      "castingArt": "Skyrim.esm:0x01B211" }              // 雙手特效
  ],
  "spells": [
    { "editorId": "MF_FireboltSpell", "name": "Forged Firebolt",
      "spellType": "Spell", "castType": "FireAndForget", "targetType": "Aimed",
      "equipType": "Skyrim.esm:0x013F44",                // EitherHand — 必填，否則 NPC 無法裝備/施放
      "effects": [ { "magicEffect": "MF_Firebolt", "magnitude": 25, "area": 0, "duration": 0 } ] }
  ] }
```

重複使用原版的 `projectile` 與 `castingArt`，才能讓光束可見並傳遞命中效果。若缺少 `equipType`，NPC 會改用近戰攻擊——這是生成戰鬥法術時最常見的無聲失敗原因。

## 「為自訂效果製作附魔武器」（MGEF + ENCH + WEAP + COBJ）

三個層次：自訂 **MGEF**（命中時觸發的效果）→ **附魔** / ENCH（`enchantType: weapon`）→ 引用它並帶有充能槽的**武器**。加入 COBJ 讓玩家可以製作。（若為被動**裝備**附魔，使用 `enchantType: apparel` 放在 `armor` 上——無需 `enchantmentAmount`，穿著時持續生效。）

> **盔甲必須帶有 `template`，否則裝備後會隱形**（已於 2026-06-01 在遊戲中確認）。ARMO 穿著時的網格位於其 Armature（ARMA 附加記錄）上，而非 ARMO 本身。Set `template` to a vanilla armor of the same slot, e.g. `"template": "Skyrim.esm:0x00012E49"` (ArmorIronCuirass)。Build 現在會在護甲沒有 `template` 時發出警告。

```jsonc
{ "magicEffects": [
    { "editorId": "MF_FrostDamageEnchEffect", "name": "Frost Damage",
      "archetype": "ValueModifier", "actorValue": "Health",
      "magicSkill": "Destruction", "resistValue": "ResistFrost",
      "castType": "FireAndForget", "targetType": "Touch", "baseCost": 1.5,
      "flags": [ "Hostile", "Detrimental", "NoArea" ] }
  ],
  "enchantments": [
    { "editorId": "MF_FrostWeaponEnch", "name": "Frost Damage",
      "enchantType": "weapon",
      "enchantmentCost": 15,            // 每次攻擊從武器充能槽消耗的量
      "effects": [ { "magicEffect": "MF_FrostDamageEnchEffect", "magnitude": 10 } ] }
  ],
  "weapons": [
    { "editorId": "MF_FrostIronSword", "name": "Frostbite Iron Sword",
      "template": "Skyrim.esm:0x012EB7", "damage": 8,
      "enchantment": "MF_FrostWeaponEnch", "enchantmentAmount": 1500 }
  ],
  "recipes": [
    { "editorId": "MF_FrostIronSwordRecipe", "createdObject": "MF_FrostIronSword",
      "components": [ { "item": "Skyrim.esm:0x05ACE4", "count": 2 },
                      { "item": "Skyrim.esm:0x02E4FC", "count": 1 } ] }
  ] }
```

完整檔案：[`examples/enchantment_spec.json`](../../examples/enchantment_spec.json)。**注意——僅通過結構驗證：** 附魔在遊戲中實際*觸發*尚未確認。
