# CODE_MAP.items-magic — 魔法效果・投射物・特效・附魔

← [CODE_MAP.items-magic](../CODE_MAP.items-magic.md)

## Magic Effects / Spells / Potions
→ **說明文件**：[SPEC-magic.md § effects](../../../../docs/spec/SPEC-magic.md#effects-spells--potions) · [SPEC-magic.md § magicEffects](../../../../docs/spec/SPEC-magic.md#magiceffects-custom-mgef)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Magic.cs` | `SpellSpec`, `MagicEffectSpec`（含 `SecondActorValue`/`SecondActorValueWeight` for DualValueModifier）, `PotionSpec` |
| Build P1 | `Generator.Build.Magic.cs` | 建 MagicEffect scalar fields（archetype/AV；DualValueModifier 的 `SecondActorValue`+weight 在給時才設）+ Spell + Potion/Ingestible。**Script-archetype MGEF 掛 Papyrus 走通用 `scripts[]`**（`AttachScripts` 反射任何有 writable VMAD 的 record，MGEF 已在 `recordsByEd`，無需 MGEF 專屬接線）|
| Validate | `Generator.Validate.Items.cs` | ingredient/potion/spell effects ref + magnitude/duration 範圍 |
| Validate | `Generator.Validate.Helpers.cs` | `CheckEffects`（共用）|
| Diag | `Diagnostics.Records.cs` | Spell / MagicEffect 欄位 dump |
| Diag | `Diagnostics.SpellTomes.cs` | spell ref + teaching mechanics |

---

## Projectiles / Explosions（PROJ / EXPL）— 法術視覺：飛行彈 + 爆炸
→ **說明文件**：[SPEC-magic.md § projectiles & explosions](../../../../docs/spec/SPEC-magic.md#projectiles-proj--explosions-expl--custom-spell-bolts--booms)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.MagicFx.cs` | `ProjectileSpec`（type/speed/gravity/range/flags/model/light/sound/explosion…）, `ExplosionSpec`（damage/force/radius/light/sound/impactDataSet/imageSpaceModifier/objectEffect…）|
| Build P1 | `Generator.Build.Explosions.cs` | 建 EXPL scalar fields（Projectiles 之前建，故 PROJ 可按 editorId 引用 EXPL）|
| Build P1 | `Generator.Build.Projectiles.cs` | 建 PROJ scalar fields；pass-2 `WireMagicFxRefs` 解析 light/sound/explosion/objectEffect/imageSpaceModifier FormLink |
| Spec | `Spec.MagicFx.cs` | `ImageSpaceModifierSpec`（brightnessMultiplier/contrast/saturation/tintColor+tintAmount/duration/animatable）|
| Build P1 | `Generator.Build.ImageSpace.cs` | 建 IMAD record（每欄寫一個 KeyFrame；tint→ColorFrame，amount=alpha）；無 pass-2（無對外 ref，被 Explosion.imageSpaceModifier 或 Papyrus property 反向引用）|
| Spec | `Spec.Hazards.cs` | `HazardSpec`（model/radius/lifetime/targetInterval/limit/spell/flags + light/sound/imad/impactDataSet）|
| Build P1 | `Generator.Build.Hazards.cs` | `BuildHazards`（HAZD scalar/model/flags，BuildFormKeyTable 前建，故 MGEF association / placement base 可引用）；pass-2 `WireHazards` 解析 spell/light/sound/imad/impactDataSet FormLink |
| Validate | `Generator.Validate.MagicFx.cs` | enum/flag 合法、radius/speed 正數、ref 完整、editorId 唯一；IMAD brightness/contrast/saturation/duration ≥ 0；**`ValidateHazards`：無 model/無 spell 警告、ref 完整、Hazard.Flag 合法** |
| Spec | `Spec.Music.cs` | `MusicTrackSpec`（type/file/fadeOut/loopBegins+loopEnds+loopCount/tracks）, `MusicTypeSpec`（flags/priority/duckingDecibel(0–655 正 dB)/fadeDuration/tracks）|
| Build P1 | `Generator.Build.Music.cs` | `BuildMusicTracks`(MUST：type/TrackFilename(字串→AssetLink)/FadeOut/Duration/LoopData) + `BuildMusicTypes`(MUSC：Flags/Data{Priority,DuckingDecibel}/FadeDuration)，BuildFormKeyTable 前建；pass-2 `WireMusic`(MUSC→MUST + Palette MUST→子 MUST，**Tracks 清單 null 需先 materialize**) + `WireCellMusic`(`cells[].music`→cell.Music) |
| Validate | `Generator.Validate.Music.cs` | `ValidateMusic`：track type 合法、SingleTrack 無 file/Palette 無子軌警告、MUSC 無 tracks 警告、duckingDecibel 0–655、flag 合法、ref 完整 |

**Music（MUSC + MUST）**：MUST 音軌(SingleTrack→`.xwm` 檔 / Palette→子軌池 / SilentTrack)；MUSC 容器引用 MUST。掛載：`cells[].music`(pass-2 `WireCellMusic`) + `worldspaces[].music`(**沿用既有 `WorldspaceSpec.Music` 的 pass-2 wire，零 worldspace 改動**)。音檔是 loose asset(`Data/Music/...`)，builder 只寫路徑。`Tests/MusicTests.cs`（record/loop/MUSC-tracks、Palette 子軌、cell+worldspace 掛載、validate）。

法術飛行彈鏈：自訂 EXPL ← PROJ（flag Explosion，explosion=EXPL）← MGEF（projectile=PROJ）← SPEL（Aimed）。MGEF 的 `projectile`/`explosion` ref 欄位沿用既有（無需改 MGEF builder）。`Tests/MagicFxTests.cs`、`Tests/DaylightSpellTests.cs`（IMAD builder + 開關型法術組裝）。

**Hazard（HAZD）兩種用法**：①法術噴出——`magicEffects[].archetype:"SpawnHazard"` + `association:<hazard>`（沿用既有 MGEF archetype/association wiring，無需改 MGEF builder）；②放置——`placements[].base` 是 in-spec HAZD（或 `kind:"hazard"`）→ `Generator.Build.Placements.cs` 建 `PlacedHazard`（見 `CODE_MAP.world.md`）。`Tests/HazardTests.cs`（record/flags/spell wiring、SpawnHazard association、PlacedHazard、validate）。

IMAD（ImageSpace Modifier）：螢幕後處理 record，由 Explosion `imageSpaceModifier` 或 Papyrus `ImageSpaceModifier` property（腳本 `ApplyCrossFade`/`Remove`）套用。頂層 `imageSpaceModifiers[]`。範例 `examples/daylight_spell_spec.json`（開關型「白晝」法術：雙 SPEL Toggle/Active + Light-archetype 跟隨光 + Script-archetype imagespace；註：runtime 端後改走 SKSE plugin 的真實光/cell ambient，imagespace 版為 ESP-only 參考）。`examples/daylight_lights_spec.json` 為配套 SKSE plugin 用的 4 階 LIGH 燈泡。

---

## Effect Shader（EFSH）— 純貼圖特效
→ **說明文件**：[SPEC-magic.md § effectShaders](../../../../docs/spec/SPEC-magic.md#effectshaders-efsh--texture-only-membrane-and-particle-vfx)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.EffectShaders.cs` | `EffectShaderSpec` + membrane/particle/key DTO；貼圖皆相對 `Data/Textures` |
| Build P1 | `Generator.Build.EffectShaders.cs` | EFSH textures/blend/fade/particle scalar + 2 scale keys/3 color keys；缺 particle palette loud warning |
| Build P2 | `Generator.Build.Magic.cs` | `magicEffects[].hitShader` / `enchantShader` → EFSH FormLink |
| Validate | `Generator.Validate.EffectShaders.cs` + `Generator.Validate.Items.cs` | texture path、enum/flag、ratio/key-count、MGEF shader refs |
| Tests | `EffectShaderTests.cs` | record shape、palette fallback/warning、MGEF wiring、invalid shape |

EFSH 不生成 particle NIF；sprite particle 由 Actor 發射。offline record 結構已驗，真 `.dds` 外觀待實機。

---

## Enchantments（ENCH）
→ **說明文件**：[SPEC-magic.md § enchantments](../../../../docs/spec/SPEC-magic.md#enchantments-ench--object-effect)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Enchantments.cs` | `EnchantmentSpec`（type/cast/target/cost/effects）|
| Build P1 | `Generator.Build.Enchantments.cs` | 建 Enchantment scalar fields（effect refs pass 2 接）|
| Validate | `Generator.Validate.Items.cs` | enchantment ref |
| Diag | `Diagnostics.Enchantments.cs` | 附魔類型 / effects / cost / charge pool dump |
