<!-- 遊戲數值、法術效果、附魔 -->
# ModForge 規格說明 — 遊戲數值、法術與附魔

← [目錄](SPEC-index.md)

### 遊戲數值
- **武器：** 提供 `damage`（通常也提供 `value`/`weight`）。設定任何數值時，`speed` 和 `reach` 預設為 `1.0`，使武器可揮舞；可覆蓋以製作更慢/更快或更長/更短的武器。
- **盔甲：** `armorType` 為 `light` / `heavy` / `clothing`（預設 `clothing`）；`slots` 以 `BipedObjectFlag` 名稱列出其佔用的雙足槽位。`armorRating` 為防禦值。

### effects（法術與藥水）
法術或藥水**沒有至少一個 effect 將不起作用**。每個 effect 為：
```jsonc
{ "magicEffect": "Skyrim.esm:0x03EB15",  // a MagicEffect *ref* (usually vanilla)
  "magnitude": 25, "area": 0, "duration": 0 }   // duration in seconds; 0 = instant
```
`magicEffect` 為 *ref* — 一個 vanilla 的（`find <Skyrim.esm> <query> MagicEffect`，例如 `AlchRestoreHealth = Skyrim.esm:0x03EB15`、`AlchDamageHealth = Skyrim.esm:0x03EB42`）**或** spec 內部 `magicEffects` 條目的 `editorId`（見下方）。一個藥水有一個 effect 即可完全運作；法術還需要施法/法術類型調整，但 effect 是核心。

### magicEffects（自訂 MGEF）
定義你**自己的** effect，而非重用 vanilla 的。
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

**旗標很重要 — 必須配合 effect 的時序（這是第一大陷阱）：**
- **瞬間**恢復/傷害（`duration` 為 0）→ `["NoDuration", "NoArea"]`，傷害時加上 `"Detrimental"`（+`"Hostile"`）。**不要**設定 `Recover` — `Recover` 在 effect *結束*時還原數值，而瞬間 effect 立即結束，因此變更會被撤銷（治療套用 +N 後立即移除 → **淨零，看起來像「施法但什麼都沒做」**）。
- **計時**強化（`duration` > 0，例如 +50 生命值持續 60 秒）→ `["Recover", "NoArea"]`：`Recover` 在計時器到期時乾淨地移除加成。這是 `Recover` 的正確用法。
保持 `baseCost` 低（vanilla 恢復/傷害 effect 使用約 0.5–3）；法術的魔力消耗由 `baseCost` × `magnitude` 自動計算，因此大的 `baseCost` 會使法術極其昂貴。使用 `mgefdiag <Skyrim.esm> <0xFORMID>` 將任何 effect 與 vanilla 進行比較。

### enchantments（ENCH / 物品效果）
一個**物品效果**將一個或多個基於 MGEF 的 `effects` 打包成一個可重用的附魔，供**武器**或**盔甲**透過其 `enchantment` 欄位參照。`enchantType` 選擇行為類別及其 vanilla 預設施法/目標（已對照 `Skyrim.esm` 驗證）：

| `enchantType` | EnchantType | 預設 castType / targetType | 充能 | 用途 |
|---------------|-------------|-------------------------------|--------|-----|
| `weapon`  | `Enchantment`      | `FireAndForget` / `Touch` | 武器攜帶充能池（`enchantmentAmount`） | 攻擊時觸發（冰霜/火焰/吸取武器） |
| `apparel` | `Enchantment`      | `ConstantEffect` / `Self` | 無 — 穿戴時始終啟用 | 強化/抗性/恢復盔甲 |
| `staff`   | `StaffEnchantment` | `FireAndForget` / `Aimed` | 法杖攜帶充能池 | 法杖「使用時施放」 |

```jsonc
"enchantments": [
  { "editorId": "MF_FrostWeaponEnch", "name": "Frost Damage",
    "enchantType": "weapon",
    "enchantmentCost": 15,
    "effects": [ { "magicEffect": "MF_FrostDamageEnchEffect", "magnitude": 10 } ] }
],
"weapons": [
  { "editorId": "MF_FrostIronSword", "name": "Frostbite Iron Sword",
    "template": "Skyrim.esm:0x012EB7",   // clone a vanilla weapon for the model (else CRASH on equip)
    "damage": 8,
    "enchantment": "MF_FrostWeaponEnch",
    "enchantmentAmount": 1500 }
]
```
`apparel`（常效型）附魔以相同方式套用到**盔甲**上（無 `enchantmentAmount` — 裝備為被動）。使用 `enchdiag <in.esp> <0xFORMID>` 檢查已建置或 vanilla 的 ENCH。*（結構已驗證；附魔在遊戲中實際觸發尚未確認 — 見食譜備注。）*
