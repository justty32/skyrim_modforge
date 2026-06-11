# CODE_MAP — 物品・法術・附魔・特技・喊聲

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：weapons、armor、books/misc、magic effects、spells、potions、enchantments、perks、shouts、ingredients、ammunition、scrolls、soul gems、keys、texture sets、global variables、long-tail records。

## Examples

| 檔案 | 對應功能 |
|-----|---------|
| `examples/customspell_spec.json` | 自訂法術 + magic effect |
| `examples/mgef_spec.json` | 自訂 MGEF（archetype / actorValue）|
| `examples/spell_tome_spec.json` | spell tome 教學 |
| `examples/enchantment_spec.json` | weapon / apparel enchantment |
| `examples/perk_spec.json` | perk entry-point + ability spell |
| `examples/shout_spec.json` | 三段 shout + word of power |
| `examples/texture_set_spec.json` | TXST 8 槽路徑 |
| `examples/custom_asset_spec.json` | 外部網格 / 貼圖 / 音效打包 |
| `examples/assets/customasset/` | 配套示範資產（.nif / .dds / .wav）|
| `examples/textures/` | 貼圖集示範資產（.dds）|
| `examples/projectile-explosion.json` | 自訂法術飛行彈 + 爆炸（PROJ/EXPL，完整可施法 firebolt 鏈）|
| `examples/showcase-multi2.json` | 多功能 showcase #2（firebolt PROJ/EXPL + NPC 庫存武器 + scene 條件閘）|

---

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `WeaponTests.cs` | templated weapon 傷害：未給傷害保留 template 值 / 顯式傷害覆寫 |
| `MagicFxTests.cs` | Projectile + Explosion build（scalar + ref 解析 + enum/flag + validate）|
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
| Spec | `Spec.Items.cs` | `WeaponSpec`, `ArmorSpec`, `BookSpec`, `MiscSpec`, `MessageSpec`（MESG：editorId/name/description）|
| Build P1 | `Generator.Build.Items.cs` | 建 Misc/Book/Weapon record（template cloning + base property defaults）|
| Build P1 | `Generator.Build.Messages.cs` | 建 Message (MESG) record（player-facing message box / notification；無 FormLink、純 pass-1；可被 perk/script 以 editorId 引用，pass-2 解）|
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

## Projectiles / Explosions（PROJ / EXPL）— 法術視覺：飛行彈 + 爆炸
→ **說明文件**：[SPEC-magic.md § projectiles & explosions](SPEC-magic.md#projectiles-proj--explosions-expl--custom-spell-bolts--booms)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.MagicFx.cs` | `ProjectileSpec`（type/speed/gravity/range/flags/model/light/sound/explosion…）, `ExplosionSpec`（damage/force/radius/light/sound/impactDataSet/imageSpaceModifier/objectEffect…）|
| Build P1 | `Generator.Build.Explosions.cs` | 建 EXPL scalar fields（Projectiles 之前建，故 PROJ 可按 editorId 引用 EXPL）|
| Build P1 | `Generator.Build.Projectiles.cs` | 建 PROJ scalar fields；pass-2 `WireMagicFxRefs` 解析 light/sound/explosion/objectEffect/imageSpaceModifier FormLink |
| Spec | `Spec.MagicFx.cs` | `ImageSpaceModifierSpec`（brightnessMultiplier/contrast/saturation/tintColor+tintAmount/duration/animatable）|
| Build P1 | `Generator.Build.ImageSpace.cs` | 建 IMAD record（每欄寫一個 KeyFrame；tint→ColorFrame，amount=alpha）；無 pass-2（無對外 ref，被 Explosion.imageSpaceModifier 或 Papyrus property 反向引用）|
| Validate | `Generator.Validate.MagicFx.cs` | enum/flag 合法、radius/speed 正數、ref 完整、editorId 唯一；IMAD brightness/contrast/saturation/duration ≥ 0 |

法術飛行彈鏈：自訂 EXPL ← PROJ（flag Explosion，explosion=EXPL）← MGEF（projectile=PROJ）← SPEL（Aimed）。MGEF 的 `projectile`/`explosion` ref 欄位沿用既有（無需改 MGEF builder）。`Tests/MagicFxTests.cs`、`Tests/DaylightSpellTests.cs`（IMAD builder + 開關型法術組裝）。

IMAD（ImageSpace Modifier）：螢幕後處理 record，由 Explosion `imageSpaceModifier` 或 Papyrus `ImageSpaceModifier` property（腳本 `ApplyCrossFade`/`Remove`）套用。頂層 `imageSpaceModifiers[]`。範例 `examples/daylight_spell_spec.json`（開關型「白晝」法術：雙 SPEL Toggle/Active + Light-archetype 跟隨光 + Script-archetype imagespace；註：runtime 端後改走 SKSE plugin 的真實光/cell ambient，imagespace 版為 ESP-only 參考）。`examples/daylight_lights_spec.json` 為配套 SKSE plugin 用的 4 階 LIGH 燈泡。

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

## Global Variables（GLOB）— 共享旗標 / 計數器 / 常數
→ **說明文件**：[SPEC-items.md § globals](SPEC-items.md#globals-glob--shared-flags--counters--constants)

全域共享的單一數字，存檔保存，**condition 零腳本可讀**（`GetGlobalValue`）。三子型：short / long(int) / float；`constant` 旗標 = 唯讀調參。當旗標 / re-arm token（與 quest stage 互補，見 [CODE_MAP.dialogue-quests.md § Story Manager / conditions]）。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Globals.cs` | `GlobalSpec`（editorId/type/value/constant）；`Spec.cs` 頂層 `globals` |
| Build P1 | `Generator.Build.Globals.cs` | 建 GlobalShort/Int/Float（`AddNewShort/Int/Float`）+ 初值 + Constant major flag；在 BuildFormKeyTable 前，故 condition/region 可按 editorId 引用 |
| Validate | `Generator.Validate.cs` | `ValidateGlobals`（type ∈ short\|long\|float）+ editorId 唯一 |
| 範例 | `examples/globals.json` | 三型 + constant + dialogue 條件讀旗標 |
| Tests | `GlobalTests.cs` | 三子型 + 初值 + constant flag + condition 引用 + validate |

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
