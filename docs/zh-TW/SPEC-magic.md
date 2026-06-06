<!-- 遊戲數值、法術效果、附魔 -->
# ModForge 規格說明 — 遊戲數值、法術與附魔

← [目錄](SPEC-index.md)

### 遊戲數值
- **武器：** 提供 `damage`（通常也提供 `value`/`weight`）。設定任何數值時，`speed` 和 `reach` 預設為 `1.0`，使武器可揮舞；可覆蓋以製作更慢/更快或更長/更短的武器。沒有任何數值的武器是惰性物品（可裝備但毫無作用）。
- **盔甲：** `armorType` 為 `light` / `heavy` / `clothing`（預設 `clothing`）；`slots` 以 `BipedObjectFlag` 名稱列出其佔用的雙足槽位 — `Body`、`Head`、`Hands`、`Feet`、`Forearms`、`Calves`、`Shield`、`Amulet`、`Ring`、`Circlet`…（多個槽位以 OR 結合）。`armorRating` 為防禦值。

### effects（法術與藥水）
法術或藥水**沒有至少一個 effect 將不起作用**。每個 effect 為：
```jsonc
{ "magicEffect": "Skyrim.esm:0x03EB15",  // a MagicEffect *ref* (usually vanilla)
  "magnitude": 25, "area": 0, "duration": 0 }   // duration in seconds; 0 = instant
```
`magicEffect` 為 *ref* — 一個 vanilla 的（`find <Skyrim.esm> <query> MagicEffect`，例如 `AlchRestoreHealth = Skyrim.esm:0x03EB15`、`AlchDamageHealth = Skyrim.esm:0x03EB42`）**或** spec 內部 `magicEffects` 條目的 `editorId`（見下方）。一個藥水有一個 effect 即可完全運作；法術還需要施法/法術類型調整，但 effect 是核心。

### magicEffects（自訂 MGEF）
定義你**自己的** effect，而非重用 vanilla 的；接著法術/藥水/材料/卷軸的 `effect` 透過 `editorId` 指向它（每次施放的 `magnitude`/`area`/`duration` 仍留在該 effect 上）。
```jsonc
{ "editorId": "MF_RestoreHealthEffect", "name": "ModForge Restore Health",
  "archetype": "ValueModifier",   // ValueModifier (damage/heal/fortify) | SummonCreature | Bound | Light | Paralysis | …
  "actorValue": "Health",          // what it acts on: Health | Magicka | Stamina | …
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
一個純 `ValueModifier` 的 MGEF（無視覺特效/投射物）仍會套用其數值——適用於 Self/Touch 與藥水。一個會*飛行*的傷害法術（`targetType: Aimed`）需要一個 `projectile`（+ 通常還有 `castingArt`）；可用 `mgefdiag <Skyrim.esm> <0xFORMID>` 取得 vanilla 的範本（例如火焰效果 `FireDamageFFAimed75 0x10F7F1` 使用 projectile `0x10FBEA` + castingArt `0x01B211`）。

**旗標很重要 — 必須配合 effect 的時序（這是第一大陷阱）：**
- **瞬間**恢復/傷害（`duration` 為 0）→ `["NoDuration", "NoArea"]`，傷害時加上 `"Detrimental"`（+`"Hostile"`）。**不要**設定 `Recover` — `Recover` 在 effect *結束*時還原數值，而瞬間 effect 立即結束，因此變更會被撤銷（治療套用 +N 後立即移除 → **淨零，看起來像「施法但什麼都沒做」**）。
- **計時**強化（`duration` > 0，例如 +50 生命值持續 60 秒）→ `["Recover", "NoArea"]`：`Recover` 在計時器到期時乾淨地移除加成。這是 `Recover` 的正確用法。
保持 `baseCost` 低（vanilla 恢復/傷害 effect 使用約 0.5–3）；法術的魔力消耗由 `baseCost` × `magnitude` 自動計算，因此大的 `baseCost` 會使法術極其昂貴。使用 `mgefdiag <Skyrim.esm> <0xFORMID>` 將任何 effect 與 vanilla 進行比較。

### projectiles（PROJ）與 explosions（EXPL）— 自訂法術飛行彈與爆炸
給自訂的毀滅系法術一個**自己的**飛行彈與撞擊爆炸（而非重用 vanilla 的）。由下而上建構的鏈：**EXPL** ← **PROJ**（參照 EXPL）← **MGEF**（`projectile` = 該 PROJ）← **SPEL**（Aimed / FireAndForget）。
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
接著讓一個 MGEF 指向該飛行彈：在自訂的 `magicEffects` 條目上設 `"projectile": "MF_Bolt"`，並將該 effect 放到一個 Aimed 的 `spells` 條目上（完整可施放鏈見 `examples/projectile-explosion.json`）。**務必驗證 nif/art 路徑**是否存在於 Skyrim.esm（錯誤的 `model` = 隱形投射物，無報錯）——用 Mutagen 解碼一個 vanilla 的 PROJ/EXPL 並複製其 model/light/sound/imagespace。爆炸在投射物之前建構，因此 PROJ 能以 editorId 解析其 `explosion`。兩者都是一般的基礎記錄；`ImpactDataSet`/`ObjectEffect`（AoE MGEF）為可選的 refs。

### imageSpaceModifiers（IMAD）— 螢幕空間後製

頂層的 `imageSpaceModifiers: []` 是一組螢幕後製記錄（亮度/對比/色調），由 `explosions[].imageSpaceModifier` ref 使用，或從 Papyrus 的 `ImageSpaceModifier` property 套用/移除（`ApplyCrossFade()` / `Remove()`）。

```jsonc
"imageSpaceModifiers": [
  { "editorId": "MFDaylightIMAD",
    "brightnessMultiplier": 1.6,   // CinematicBrightnessMult (1=neutral, >1 brighter)
    "contrast": 1.05, "saturation": 0.92,
    "tintColor": { "r": 255, "g": 250, "b": 235 }, "tintAmount": 0.15,  // amount -> colour alpha
    "duration": 1.0, "animatable": false }
]
```

Mutagen 將每個 IMAD 欄位都建模為可動畫的曲線；builder 為每個欄位寫入一個關鍵影格（tint = 一個 ColorFrame）。見 `examples/daylight_spell_spec.json`。注意：該範例的*執行期*「白晝」效果最終被移到一個 SKSE 外掛中（真正的跟隨光源 + 即時 cell 環境光，這兩者——不像螢幕濾鏡——不會洗掉低反照率物件）；IMAD builder 仍保留為一個通用的 ESP 端能力。

### enchantments（ENCH / 物品效果）
一個**物品效果**將一個或多個基於 MGEF 的 `effects`（與法術/藥水 effect 相同的 `{ magicEffect, magnitude, area, duration }` 結構）打包成一個可重用的附魔，供**武器**或**盔甲**透過其 `enchantment` 欄位參照。`enchantType` 選擇行為類別及其 vanilla 預設施法/目標（已對照 `Skyrim.esm` 驗證）：

| `enchantType` | EnchantType | 預設 castType / targetType | 充能 | 用途 |
|---------------|-------------|-------------------------------|--------|-----|
| `weapon`  | `Enchantment`      | `FireAndForget` / `Touch` | 武器攜帶充能池（`enchantmentAmount`） | 攻擊時觸發（冰霜/火焰/吸取武器） |
| `apparel` | `Enchantment`      | `ConstantEffect` / `Self` | 無 — 穿戴時始終啟用 | 強化/抗性/恢復盔甲 |
| `staff`   | `StaffEnchantment` | `FireAndForget` / `Aimed` | 法杖攜帶充能池 | 法杖「使用時施放」（vanilla 法杖將 `chargeTime` 設為 ~0.5） |

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
`apparel`（常效型）附魔以相同方式套用到**盔甲**上（無 `enchantmentAmount` — 裝備為被動）。`enchantment` ref 也可以是一個 **vanilla** 的 ObjectEffect（`find <Skyrim.esm> Ench... ObjectEffect`，例如 `EnchWeaponFrostDamageBase = Skyrim.esm:0x10FB96`）。使用 `enchdiag <in.esp> <0xFORMID>` 檢查已建置或 vanilla 的 ENCH。可參考範例：[`examples/enchantment_spec.json`](../examples/enchantment_spec.json)。*（結構已驗證；附魔在遊戲中實際觸發尚未確認 — 見食譜備注。）*
