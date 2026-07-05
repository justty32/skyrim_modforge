# ModForge spec — 魔法與屬性

← [index](SPEC-index.md)

### 遊戲性屬性
- **武器：** 給一個 `damage`（通常還有 `value`/`weight`）。當設定任何屬性時，`speed` 與 `reach`
  預設為 `1.0`，讓武器可以揮動；若要更慢/更快或更長/更短的武器則覆寫這些值。完全沒有屬性的武器是
  惰性物品（會裝備，但不會有任何實際作用）。
- **護甲：** `armorType` 為 `light` / `heavy` / `clothing`（預設 `clothing`）；`slots`
  以 `BipedObjectFlag` 名稱列出它佔用的雙足部位 — `Body`、`Head`、`Hands`、
  `Feet`、`Forearms`、`Calves`、`Shield`、`Amulet`、`Ring`、`Circlet`…（多個部位
  以 OR 結合）。`armorRating` 是防護值。

### effects（spells 與 potions）
一個 spell 或 potion **若沒有至少一個 effect，就什麼也不會做**。每個 effect 是：
```jsonc
{ "magicEffect": "Skyrim.esm:0x03EB15",  // a MagicEffect *ref* (usually vanilla)
  "magnitude": 25, "area": 0, "duration": 0 }   // duration in seconds; 0 = instant
```
`magicEffect` 是一個 *ref* — 可以是原版的（`find <Skyrim.esm> <query> MagicEffect`，例如
`AlchRestoreHealth = Skyrim.esm:0x03EB15`、`AlchDamageHealth = Skyrim.esm:0x03EB42`），**或**是
spec 內某個 `magicEffects` 項目的 `editorId`（見下文）。一個 potion 只要有一個 effect 就完全可用；
一個 spell 還會想要 cast/spell-type 的調校，但 effect 才是核心。

### magicEffects（自訂 MGEF）
定義你自己的 effect，而不是重用原版的；一個 spell/potion/ingredient/scroll 的 `effect`
就以 `editorId` 指向它（而每次施放的 `magnitude`/`area`/`duration` 仍留在該 effect 上）。
```jsonc
{ "editorId": "MF_RestoreHealthEffect", "name": "ModForge Restore Health",
  "archetype": "ValueModifier",   // ValueModifier (damage/heal/fortify) | DualValueModifier | SummonCreature | Bound | Light | Script | …
  "actorValue": "Health",          // what it acts on: Health | Magicka | Stamina | …
  "secondActorValue": "Magicka",   // DualValueModifier only: a 2nd affected AV (omit otherwise)
  "secondActorValueWeight": 0.5,   // DualValueModifier only: how the magnitude splits to the 2nd AV (0 = all to primary)
  "magicSkill": "Restoration",     // school: Alteration|Conjuration|Destruction|Illusion|Restoration
  "resistValue": "ResistFire",     // AV that resists it (optional): ResistFire | ResistFrost | PoisonResist | …
  "castType": "FireAndForget",     // FireAndForget | Concentration | ConstantEffect
  "targetType": "Self",            // Self | Touch | Aimed | TargetActor | TargetLocation
  "baseCost": 8.0,
  "flags": ["Recover"],            // Hostile | Detrimental | Recover | NoArea | NoDuration | NoMagnitude | …
  "association": "<ref>",          // summoned/bound form (only for Summon/Bound archetypes)
  "projectile": "<ref>",           // PROJ — the bolt that travels (needed for Aimed spells)
  "castingArt": "<ref>",           // ARTO — FX at the caster's hands
  "hitEffectArt": "<ref>",         // ARTO — FX at the impact point
  "explosion": "<ref>" }           // EXPL — AoE explosion on impact
```
一個裸的 `ValueModifier` MGEF（沒有視覺 art/projectile）仍會套用其數值 — 適用於 Self/Touch
與 potions。一個會*飛行*的傷害 spell（`targetType: Aimed`）需要一個 `projectile`（+通常還要
`castingArt`）；用 `mgefdiag <Skyrim.esm> <0xFORMID>` 收割一個原版的（例如火焰 effect
`FireDamageFFAimed75 0x10F7F1` 使用 projectile `0x10FBEA` + castingArt `0x01B211`）。

- **`DualValueModifier`** 以一個 magnitude 影響**兩個** actor value — 設定 `archetype:
  "DualValueModifier"`、主要的 `actorValue`，加上 `secondActorValue` 與 `secondActorValueWeight`
  （導向第二個 AV 的 magnitude 比例）。吸收/轉移類型的 effect（傷害一個屬性、餵養另一個）
  就是這樣建構的。
- **`Script`-archetype MGEF**（boss 法術邏輯、自訂的施加時行為）執行 **Papyrus**：設定
  `archetype: "Script"` 並附加一個 script。兩種等效寫法：**inline** 的 `magicEffects[].scripts[]`
  項目（`targetEditorId` 是隱含的 — 讓 script 貼著 effect 放），或一個頂層 **`scripts[]`** 項目，
  其 `targetEditorId` 是此 MGEF 的 `editorId`。兩者形狀相同（見
  [SPEC-quests](SPEC-quests.md) § scripts），`package` 都會編譯各項目的 `source`。`.psc`
  extends `ActiveMagicEffect`。為求可讀性優先用 inline 寫法。

**Flags 很重要 — 要對上 effect 的時序（這是第一名的陷阱）：**
- **瞬間** restore/damage（`duration` 0）→ `["NoDuration", "NoArea"]`，傷害再加上 `"Detrimental"`
  （+`"Hostile"`）。**不要**設 `Recover` — `Recover` 會在 effect *結束*時把數值還原，而瞬間 effect
  立刻結束，所以變更會被撤銷（一次治療套用 +N 然後立刻移除它 → **淨值為零，看起來像「有施放但什麼都沒做」**）。
- **限時** fortify（`duration` > 0，例如 +50 Health 持續 60s）→ `["Recover", "NoArea"]`：`Recover`
  會在計時器到期時乾淨地移除加成。這是 `Recover` 的正確用途。
保持 `baseCost` 偏低（原版 restore/damage effect 用 ~0.5–3）；spell 的 magicka cost 由
`baseCost` × `magnitude` 自動計算，所以過大的 `baseCost` 會讓 spell 貴得離譜。
用 `mgefdiag <Skyrim.esm> <0xFORMID>` 把任何 effect 和原版的比較。

### projectiles（PROJ）與 explosions（EXPL）— 自訂的法術飛彈與爆炸
給自訂的破壞系 spell 它自己的飛行飛彈與撞擊爆炸（而不是重用原版的）。這條鏈，由下而上建構：
**EXPL** ← **PROJ**（參照 EXPL）← **MGEF**（`projectile` = 該 PROJ）← **SPEL**（Aimed / FireAndForget）。
```jsonc
"explosions": [
  { "editorId": "MF_Boom", "name": "Forged Blast",
    "model": "Effects\\FXEmptyExplosionArt.nif",   // a VERIFIED vanilla nif (wrong path = invisible)
    "damage": 15, "force": 7, "radius": 256, "isRadius": 1280,
    "sound": "Skyrim.esm:0x02518F",                // vanilla fire-impact sound
    "imageSpaceModifier": "Skyrim.esm:0x0010FBE8", // vanilla fire-blast screen FX
    "flags": [ "IgnoreLosCheck" ] } ],
"projectiles": [
  { "editorId": "MF_Bolt", "name": "Forged Bolt",
    "type": "Missile",                             // Missile|Lobber|Beam|Flame|Cone|Barrier|Arrow
    "speed": 2500, "gravity": 0, "range": 10000, "lifetime": 10, "impactForce": 1,
    "flags": [ "Explosion" ],                      // trigger the explosion on impact
    "model": "Magic\\FireBoltProjectile.nif",      // the REAL vanilla firebolt nif → visible bolt
    "light": "Skyrim.esm:0x0001CBB3", "sound": "Skyrim.esm:0x0003C8FE",
    "explosion": "MF_Boom" } ],                     // ref → the in-spec EXPL (built first)
```
然後讓一個 MGEF 指向該飛彈：在自訂的 `magicEffects` 項目上設 `"projectile": "MF_Bolt"`，並把那個
effect 放到一個 Aimed `spells` 項目上（完整可施放鏈見 `examples/projectile-explosion.json`）。
**永遠要驗證 nif/art 路徑**對照 Skyrim.esm（錯誤的 `model` = 看不見的 projectile，沒有錯誤）—
用 Mutagen 解碼一個原版 PROJ/EXPL 並複製它的 model/light/sound/imagespace。Explosions 在 projectiles
之前建構，所以 PROJ 會以 editorId 解析它的 `explosion`。兩者都是一般的 base record；
`ImpactDataSet`/`ObjectEffect`（AoE MGEF）是可選的 ref。

### imageSpaceModifiers（IMAD）— 螢幕空間後處理

一個頂層的 `imageSpaceModifiers: []`，是螢幕後處理 record（亮度/對比/色調），由
`explosions[].imageSpaceModifier` ref 使用，或從 Papyrus `ImageSpaceModifier`
property 套用/移除（`ApplyCrossFade()` / `Remove()`）。

```jsonc
"imageSpaceModifiers": [
  { "editorId": "MFDaylightIMAD",
    "brightnessMultiplier": 1.6,   // CinematicBrightnessMult (1=neutral, >1 brighter)
    "contrast": 1.05, "saturation": 0.92,
    "tintColor": { "r": 255, "g": 250, "b": 235 }, "tintAmount": 0.15,  // amount -> colour alpha
    "duration": 1.0, "animatable": false }
]
```

Mutagen 把每個 IMAD 欄位都建模成可動畫的曲線；builder 為每個欄位寫入一個關鍵影格（tint = 一個
ColorFrame）。見 `examples/daylight_spell_spec.json`。注意：該範例的*執行時*「daylight」effect 最終被
移到一個 SKSE plugin（真正的跟隨光源 + 即時 cell-ambient，這些 — 不像螢幕濾鏡 — 不會把低反照率物件
洗白）；IMAD builder 仍是一個通用的 ESP 端能力。

### hazards（HAZD）— 半徑效果 / 放置陷阱

一個頂層的 `hazards: []`，是環境危害 — 一塊火焰/冰霜/毒物區域，會週期性地對其半徑內的 actor 套用一個
spell（引擎的火焰陷阱 / 持續 AoE 機制）。

```jsonc
"hazards": [
  { "editorId": "MFHZ_Fire", "name": "Flames",
    "model": "Meshes/Traps/PressurePlateFire/NorTrapFirePlateFX.nif",  // visual nif (verify vs Skyrim.esm)
    "radius": 150,            // effect radius
    "lifetime": 8,           // seconds it persists (0 = inherit from the spawning spell / permanent)
    "targetInterval": 1,     // seconds between applying `spell` to actors in radius
    "limit": 0,              // max simultaneous instances (0 = unlimited)
    "spell": "MFHZ_BurnSpell", // ref -> the SPEL applied periodically (the actual effect)
    "flags": [ "DropToGround" ], // AffectsPlayerOnly | InheritDurationFromSpawnSpell | AlignToImpactNormal | InheritRadiusFromSpawnSpell | DropToGround
    "light": "...", "sound": "Skyrim.esm:0x000F57E6", // optional refs (LIGT / SNDR)
    "imageSpaceModifier": "...", "impactDataSet": "..." } // optional refs (IMAD / IPDS)
]
```

**使用 hazard 的兩種方式**（兩者皆已出貨）：
1. **Spell-spawn** — 一個 `magicEffects[]` 項目，帶 `"archetype": "SpawnHazard"` 與
   `"association": "MFHZ_Fire"`，放到一個 `TargetLocation` spell 上 → 一個可施放的 spell，會把
   hazard 落在地上。重用既有的 MGEF archetype/association 接線（無特殊欄位）。
2. **放置陷阱** — 一個 `placements[]` 項目，其 `base` 是 hazard 的 editorId（或 `"kind": "hazard"`）
   → cell 中一個靜態的 `PlacedHazard`（一個地城火焰陷阱）。見 `SPEC-world.md`。

沒有 `spell` 的 hazard 不會套用任何東西（validate 會警告）；沒有 model 的 hazard 看不見（對照
Skyrim.esm 驗證 nif 路徑 — 見 vanilla-nif-paths-must-be-verified）。完整實作範例（兩條路徑）：
`examples/hazard.json`。

### enchantments（ENCH / Object Effect）
一個 **Object Effect** 把一個或多個基於 MGEF 的 `effects`（與 spell/potion effect 相同的
`{ magicEffect, magnitude, area, duration }` 形狀）綑成一個可重用的附魔，供 **weapon** 或
**armor** 透過其 `enchantment` 欄位參照。`enchantType` 挑選行為家族及其
原版預設的 cast/target（已對照 `Skyrim.esm` 驗證）：

| `enchantType` | EnchantType | default castType / targetType | charge | use |
|---------------|-------------|-------------------------------|--------|-----|
| `weapon`  | `Enchantment`      | `FireAndForget` / `Touch` | weapon carries the pool (`enchantmentAmount`) | cast-on-strike (frost/fire/absorb weapon) |
| `apparel` | `Enchantment`      | `ConstantEffect` / `Self` | none — always-on while worn | fortify/resist/regen apparel |
| `staff`   | `StaffEnchantment` | `FireAndForget` / `Aimed` | staff carries the pool | staff "cast on use" (vanilla staves set `chargeTime` ~0.5) |

```jsonc
"enchantments": [
  { "editorId": "MF_FrostWeaponEnch", "name": "Frost Damage",
    "enchantType": "weapon",          // weapon | apparel | staff
    "enchantmentCost": 15,            // per-cast charge cost drained from the weapon's pool
    // "castType": "...", "targetType": "...",  // optional — override the family defaults
    "effects": [ { "magicEffect": "MF_FrostDamageEnchEffect", "magnitude": 10 } ] }
],
"weapons": [
  { "editorId": "MF_FrostIronSword", "name": "Frostbite Iron Sword",
    "template": "Skyrim.esm:0x012EB7",   // clone a vanilla weapon for the model (else CRASH on equip)
    "damage": 8,
    "enchantment": "MF_FrostWeaponEnch", // ref → in-spec ENCH or vanilla <master>:0xFORMID
    "enchantmentAmount": 1500 }          // the weapon's charge pool (casts before recharge)
]
```
一個 `apparel`（constant-effect）附魔以相同方式放到一件 **armor** 上（沒有 `enchantmentAmount` —
apparel 是被動的）。`enchantment` ref 也可以是一個**原版** ObjectEffect
（`find <Skyrim.esm> Ench... ObjectEffect`，例如 `EnchWeaponFrostDamageBase = Skyrim.esm:0x10FB96`）。
用 `enchdiag <in.esp> <0xFORMID>` 檢視已建構或原版的 ENCH。實作範例：
[`examples/enchantment_spec.json`](../../../examples/enchantment_spec.json)。*（已做結構驗證；附魔
在遊戲內實際發動尚未確認 — 見 cookbook recipe 的註記。）*
