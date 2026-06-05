# CODE_MAP — 物品・法術・附魔・特技・喊聲

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：weapons、armor、books/misc、magic effects、spells、potions、enchantments、perks、shouts、ingredients、ammunition、scrolls、soul gems、keys、texture sets、long-tail records。

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `EnchantmentTests.cs` | ENCH scalar fields + effect ref 接線 |
| `ExternalAssetTests.cs` | external asset 打包（Meshes/Textures/Sounds 複製）|
| `PerkTests.cs` | Perk trunk + entry-point modifier + ability-spell grant |
| `ShoutTests.cs` | Shout word-of-power + spell tier + cooldown build |
| `SpellTomeTests.cs` | spell tome teaching mechanics |
| `TextureSetTests.cs` | TXST 8 槽路徑 build |
| `TextureSetValidateTests.cs` | texture-path / model-path sanity validate |

---

---

## Weapons / Armor / Books / Misc
→ **說明文件**：[for_agent.md § 限制](for_agent.md#limits--be-honest-do-not-over-claim)（物品屬性確認清單）· [SPEC-intro.md](SPEC-intro.md)（頂層欄位表）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `WeaponSpec`, `ArmorSpec`, `BookSpec`, `MiscSpec` |
| Build P1 | `Generator.Build.Items.cs` | 建 Misc/Book/Weapon record（template cloning + base property defaults）|
| Validate | `Generator.Validate.Items.cs` | template ref、keyword ref、enchantment ref |
| Diag | `Diagnostics.Records.cs` | Weapon/Armor 詳細欄位 dump |

---

## Magic Effects / Spells / Potions
→ **說明文件**：[SPEC-magic.md § effects](SPEC-magic.md#effects-spells--potions) · [SPEC-magic.md § magicEffects](SPEC-magic.md#magiceffects-custom-mgef)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Magic.cs` | `SpellSpec`, `MagicEffectSpec`, `PotionSpec` |
| Build P1 | `Generator.Build.Magic.cs` | 建 MagicEffect scalar fields + Spell + Potion/Ingestible |
| Validate | `Generator.Validate.Items.cs` | ingredient/potion/spell effects ref + magnitude/duration 範圍 |
| Validate | `Generator.Validate.Helpers.cs` | `CheckEffects`（共用）|
| Diag | `Diagnostics.Records.cs` | Spell / MagicEffect 欄位 dump |
| Diag | `Diagnostics.SpellTomes.cs` | spell ref + teaching mechanics |

---

## Enchantments（ENCH）
→ **說明文件**：[SPEC-magic.md § enchantments](SPEC-magic.md#enchantments-ench--object-effect)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Enchantments.cs` | `EnchantmentSpec`（type/cast/target/cost/effects）|
| Build P1 | `Generator.Build.Enchantments.cs` | 建 Enchantment scalar fields（effect refs pass 2 接）|
| Validate | `Generator.Validate.Items.cs` | enchantment ref |
| Diag | `Diagnostics.Enchantments.cs` | 附魔類型 / effects / cost / charge pool dump |

---

## Perks（特技 PERK）
→ **說明文件**：[SPEC-items.md § perks](SPEC-items.md#perks-perk) · [engine-internals.md § Perk entry points](engine-internals.md#perk-entry-points-carry-a-hidden-tab-count-byte)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Perks.cs` | `PerkSpec`（ranks / entry-point modifiers / ability spells）|
| Build P1 | `Generator.Build.Perks.cs` | 建 Perk trunk（name/description/ranks/playable flag）|
| Build P2 | `Generator.Build.Perks.EntryPoints.cs` | entry-point modifier 接線 + ability-spell grant（含隱藏 tab-count byte）|
| Validate | `Generator.Validate.Items.cs` | perk entry-point enum、ability spell ref |
| Diag | `Diagnostics.Perks.cs` | entry-point effects / ability spell / ranks / conditions dump |

---

## Shouts（喊聲 SHOU / WOOP）
→ **說明文件**：[for_agent.md § 限制](for_agent.md#limits--be-honest-do-not-over-claim)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Shouts.cs` | `ShoutSpec`（三段 word-of-power + spell + cooldown）|
| Build P1 | `Generator.Build.Shouts.cs` | 建 Shout scalar fields（word/spell refs pass 2 接）|
| Diag | `Diagnostics.Shouts.cs` | word list / spell tiers / cooldown dump |

---

## Long-tail Records（長尾雜項）
→ **說明文件**：[for_agent.md § 限制](for_agent.md#limits--be-honest-do-not-over-claim)（支援記錄類型列表）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `IngredientSpec`, `AmmunitionSpec`, `ScrollSpec`, `SoulGemSpec`, `KeySpec`, `FurnitureSpec`, `SoundSpec` |
| Spec | `Spec.cs` | `keywords`, `outfits`, `statics`, `activators`（頂層欄位）|
| Build P1 | `Generator.Build.LongTail.cs` | 建上述所有小 record |
| Build P2 | `Generator.Build.LongTail.Wire.cs` | keyword 陣列 + sound ref + alternate-texture ref 接線 |
| Validate | `Generator.Validate.Items2.cs` | texture-path / model-path / sound-file sanity |

---

## Texture Sets（TXST）
→ **說明文件**：[SPEC-items.md § textureSets](SPEC-items.md#texturesets-txst--retexture-without-a-new-mesh)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `TextureSetSpec`（8 槽路徑）|
| Build P1 | `Generator.Build.TextureSets.cs` | 建 TextureSet（diffuse/normal/mask/glow/height/env/multilayer/backlight）|
| Validate | `Generator.Validate.Helpers.cs` | `CheckTexPath` |
| Diag | `Diagnostics.TextureSets.cs` | 8 槽路徑 + alternate-texture consumer dump |

---

## External Assets（外部資源）
→ **說明文件**：[SPEC-items.md § external assets](SPEC-items.md#external-assets--your-own-meshes--textures--sounds-model-sounds-assets) · [external_assets.md](external_assets.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Build | `Assets.cs` | 複製 Meshes/Textures/Sounds 樹到輸出目錄 |
| Validate | `Generator.Validate.Helpers.cs` | `CheckModelPath` / `CheckSoundFile` |
