# CODE_MAP — 物品・法術・附魔・特技・喊聲

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：weapons、armor、books/misc、magic effects、spells、potions、enchantments、perks、shouts、ingredients、ammunition、scrolls、soul gems、keys、texture sets、long-tail records。

---

## 1. Spec（資料定義）

| 檔案 | 主要型別 |
|-----|---------|
| `src/ModForge.Core/Spec.Items.cs` | `WeaponSpec`, `ArmorSpec`, `BookSpec`, `MiscSpec`（+長尾：`IngredientSpec`, `AmmunitionSpec`, `ScrollSpec`, `SoulGemSpec`, `KeySpec`, `FurnitureSpec`, `SoundSpec`）, `ContainerSpec`, `RecipeSpec`, `LeveledItemSpec` |
| `src/ModForge.Core/Spec.Magic.cs` | `SpellSpec`, `MagicEffectSpec`, `PotionSpec`, `ClassSpec`, `CombatStyleSpec` |
| `src/ModForge.Core/Spec.Enchantments.cs` | `EnchantmentSpec`（ENCH/ObjectEffect：type/cast/target/cost/effects）|
| `src/ModForge.Core/Spec.Perks.cs` | `PerkSpec`（ranks / entry-point modifiers / ability spells）|
| `src/ModForge.Core/Spec.Shouts.cs` | `ShoutSpec`（三段 word-of-power + spell + cooldown）|
| `src/ModForge.Core/Spec.cs` 頂層 | `keywords`, `outfits`, `statics`, `activators`, `textureSets` 亦在此 |

---

## 2. Build Pass 1

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Generator.Build.Items.cs` | 建 Misc/Book/Weapon record（template cloning + base property defaults）|
| `src/ModForge.Core/Generator.Build.Magic.cs` | 建 MagicEffect scalar fields + Spell record + Potion/Ingestible |
| `src/ModForge.Core/Generator.Build.Enchantments.cs` | 建 Enchantment scalar fields（effect refs pass 2 接）|
| `src/ModForge.Core/Generator.Build.Perks.cs` | 建 Perk trunk（name/description/ranks/playable flag）|
| `src/ModForge.Core/Generator.Build.Shouts.cs` | 建 Shout scalar fields（word/spell refs pass 2 接）|
| `src/ModForge.Core/Generator.Build.LongTail.cs` | 建 ingredient/ammunition/scroll/soul gem/key/keyword/outfit/static/activator/furniture/sound/texture set |
| `src/ModForge.Core/Generator.Build.TextureSets.cs` | 建 TextureSet 8 槽路徑（diffuse/normal/mask/glow/height/env/multilayer/backlight）|

## 3. Build Pass 2

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Generator.Build.Perks.EntryPoints.cs` | Perk entry-point modifier 接線 + ability-spell grant（rank/priority/condition 順序）|
| `src/ModForge.Core/Generator.Build.LongTail.Wire.cs` | keyword 陣列填入 + sound ref + static/activator alternate-texture ref |
| `src/ModForge.Core/Generator.Recipes.cs` | COBJ crafting recipe（workbench dispatch + component 接線）|

---

## 4. Validate

| 檔案 | 檢查什麼 |
|-----|---------|
| `src/ModForge.Core/Generator.Validate.Items.cs` | template ref、keyword ref、ingredient/potion/spell effects、leveled list entries、enchantment ref |
| `src/ModForge.Core/Generator.Validate.Items2.cs` | container contents、recipe workbench/components、texture-path/model-path/sound-file sanity |
| `src/ModForge.Core/Generator.Validate.Helpers.cs` | `CheckEffects`（魔法效果 ref + magnitude/duration 範圍）、`CheckModelPath`、`CheckTexPath`、`CheckSoundFile` |

---

## 5. Diagnostics

| 檔案 | dump 哪些 |
|-----|---------|
| `src/ModForge.Cli/Diagnostics.Records.cs` | Weapon/Armor/Perk/Spell/Enchantment/Quest record 詳細欄位 |
| `src/ModForge.Cli/Diagnostics.Enchantments.cs` | 附魔類型 / effects / cost / charge pool |
| `src/ModForge.Cli/Diagnostics.Perks.cs` | entry-point effects / ability spell / ranks / conditions / next-perk chain |
| `src/ModForge.Cli/Diagnostics.Shouts.cs` | word list + spell tiers + cooldown |
| `src/ModForge.Cli/Diagnostics.SpellTomes.cs` | spell ref + teaching mechanics |
| `src/ModForge.Cli/Diagnostics.TextureSets.cs` | 8 槽路徑 + alternate-texture consumer |
| `src/ModForge.Cli/Diagnostics.Recipes.cs` | workbench / input components / output item |

---

## 6. Docs

| 連結 | 內容 |
|-----|-----|
| `docs/SPEC-items.md` | items/perks/external assets 欄位（EN）|
| `docs/SPEC-magic.md` | spells/magic effects/enchantments 欄位（EN）|
| `docs/zh-TW/SPEC-items.md` | （zh-TW）|
| `docs/zh-TW/SPEC-magic.md` | （zh-TW）|
