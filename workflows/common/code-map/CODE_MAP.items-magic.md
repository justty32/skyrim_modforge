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
| `examples/mcm_global_perk.json` | MCM bool toggle → GLOB → `ModBuyPrices` perk CTDA 完整接線 |
| `examples/interactive_perk_spec.json` | **#1 互動式 perk：addActivateChoice（[E] 選項 + spell/fragmentBody）+ setText（改活化提示）；PerkAdapter fragment（package 路徑）** |
| `examples/shout_spec.json` | 三段 shout + word of power |
| `examples/texture_set_spec.json` | TXST 8 槽路徑 |
| `examples/custom_asset_spec.json` | 外部網格 / 貼圖 / 音效打包 |
| `examples/assets/customasset/` | 配套示範資產（.nif / .dds / .wav）|
| `examples/textures/` | 貼圖集示範資產（.dds）|
| `examples/projectile-explosion.json` | 自訂法術飛行彈 + 爆炸（PROJ/EXPL，完整可施法 firebolt 鏈）|
| `examples/effect_shader.json` | EFSH membrane + sprite particles，並接 MGEF hit/enchant shader |
| `examples/message_menu.json` | MESG 多按鈕 menu（按鈕順序＝Papyrus `Message.Show()` 回傳 index）|
| `examples/showcase-multi2.json` | 多功能 showcase #2（firebolt PROJ/EXPL + NPC 庫存武器 + scene 條件閘）|

---

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `WeaponTests.cs` | templated weapon 傷害：未給傷害保留 template 值 / 顯式傷害覆寫 |
| `MagicFxTests.cs` | Projectile + Explosion build（scalar + ref 解析 + enum/flag + validate）|
| `EffectShaderTests.cs` | EFSH texture/scalar/key build、palette warning、MGEF shader wiring、validate |
| `MessageTests.cs` | MESG notification 相容、1–10 個有序按鈕 build、空 label/超量 validate |
| `EnchantmentTests.cs` | ENCH scalar fields + effect ref 接線 |
| `ExternalAssetTests.cs` | external asset 打包（Meshes/Textures/Sounds 複製）|
| `PerkTests.cs` | Perk trunk + entry-point modifier + ability-spell grant |
| `McmGlobalWiringTests.cs` | MCM QUST VMAD property 與 perk `GetGlobalValue` 指向同一顆 GLOB FormKey |
| `PerkActivateChoiceTests.cs` | **#1 addActivateChoice（EntryType.Activate/ButtonLabel/Spell/conditions/tab-count）+ setText（Text）+ fragment source（Fragment_N body）+ PerkAdapter VMAD 綁定[fake .pex]（IndexedScriptFragment + Flags.RunImmediately/FragmentIndex）+ 無 .pex 不掛 + validate（空 label / do-nothing / 空 text）** |
| `ShoutTests.cs` | Shout word-of-power + spell tier + cooldown build |
| `SpellTomeTests.cs` | spell tome teaching mechanics |
| `TextureSetTests.cs` | TXST 8 槽路徑 build |
| `TextureSetValidateTests.cs` | texture-path / model-path sanity validate |

---

---

## Weapons / Armor / Books / Misc
→ **說明文件**：[for_agent.md § 限制](../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)（物品屬性確認清單）· [SPEC-intro.md](../../../docs/spec/SPEC-intro.md)（頂層欄位表）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `WeaponSpec`, `ArmorSpec`, `BookSpec`, `MiscSpec`, `MessageSpec`（MESG notification + `buttons[]` menu）|
| Build P1 | `Generator.Build.Items.cs` | 建 Misc/Book/Weapon record（template cloning + base property defaults）|
| Build P1 | `Generator.Build.Messages.cs` | 建 Message (MESG) record（0 buttons＝notification；1–10 buttons＝Papyrus `Show()` 有序 menu；無 FormLink、純 pass-1）|
| Validate | `Generator.Validate.Items.cs` | template/ref + MESG 空 button label / 10-button 上限 |
| Diag | `Diagnostics.Records.cs` | Weapon/Armor 詳細欄位 dump |

---

## Magic Effects / Spells / Potions
→ **說明文件**：[SPEC-magic.md § effects](../../../docs/spec/SPEC-magic.md#effects-spells--potions) · [SPEC-magic.md § magicEffects](../../../docs/spec/SPEC-magic.md#magiceffects-custom-mgef)

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
→ **說明文件**：[SPEC-magic.md § projectiles & explosions](../../../docs/spec/SPEC-magic.md#projectiles-proj--explosions-expl--custom-spell-bolts--booms)

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
→ **說明文件**：[SPEC-magic.md § effectShaders](../../../docs/spec/SPEC-magic.md#effectshaders-efsh--texture-only-membrane-and-particle-vfx)

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
→ **說明文件**：[SPEC-magic.md § enchantments](../../../docs/spec/SPEC-magic.md#enchantments-ench--object-effect)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Enchantments.cs` | `EnchantmentSpec`（type/cast/target/cost/effects）|
| Build P1 | `Generator.Build.Enchantments.cs` | 建 Enchantment scalar fields（effect refs pass 2 接）|
| Validate | `Generator.Validate.Items.cs` | enchantment ref |
| Diag | `Diagnostics.Enchantments.cs` | 附魔類型 / effects / cost / charge pool dump |

---

## Captured items（capturedItems[] 遊戲內「定義滴管」消費，Idea #24）
scene-capture-bridge DLL `sc cap` 匯出的 `capturedItems[]` → macro 展開成既有 WEAP/ARMO(+新鑄 ENCH)/ALCH/INGR。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.CapturedItems.cs` | `CapturedItemSpec`（kind/name/editorId?/base?/enchantment?/effects）＋`CapturedEnchantSpec`（target/base?/amount/effects）|
| Expand P0 | `Generator.CapturedItems.cs` `ExpandCapturedItems`（接進 `Generator.SceneNpcRoles.cs` `ExpandMacros`）| weapon/armor→WeaponSpec/ArmorSpec `Template`=base clone；附魔：durable `enchantment.base` 直接引用／否則從 effects 新鑄 in-spec ENCH（weapon vs apparel）；potion/ingredient→Effects 直填。editorId `MFCap_<name>_<i>`（1-based 解同名重複）|
| Validate | `Generator.Validate.SceneNpcRoles.cs` `ValidateCapturedItems` | kind 白名單、base/enchant.base external ref 格式、gear 無 base 且無附魔／consumable 無 effects／effect 缺 magicEffect |
| Tests | `CapturedItemsTests.cs` | validate＋expand（新鑄／引用／potion／ingredient／同名唯一／idempotent）離線；模板複製 1 RequiresSkyrim |

---

## Perks（特技 PERK）
→ **說明文件**：[SPEC-items.md § perks](../../../docs/spec/SPEC-items.md#perks-perk) · [engine-internals.md § Perk entry points](../../../docs/engine-internals.md#perk-entry-points-carry-a-hidden-tab-count-byte)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Perks.cs` | `PerkSpec`、`PerkEffectSpec`（kind=ability / entryPoint / **addActivateChoice / setText**；後二含 buttonLabel/text/spell/fragmentBody/replaceDefault）|
| Build P1 | `Generator.Build.Perks.cs` `BuildPerks` | 建 Perk trunk（name/description/ranks/playable flag）|
| Build P2 | `Generator.Build.Perks.cs` `WirePerks` | 所有 effect 接線：ability/entryPoint modify-value/**`PerkEntryPointAddActivateChoice`（EntryType.Activate + ButtonLabel + Spell + Flags）/`PerkEntryPointSetText`（Text）**；npcs[].perks。**perk/effect CTDA 不在此建**——`WirePerks` 跑在 placements 之前，而 perk 條件合法指 placed ref（`GetDistance <那張椅子>`），故一律 `DeferCondition` 排隊，由 `WireDeferredConditions`（`Generator.Build.Conditions.Wire.cs`）補建；effect 的 `PerkCondition` tab 走 `DeferConditionFinalizer`（條件全建不出來就不掛空 tab）。**`AttachPerkFragments`**：有 fragmentBody 的 choice → `PerkAdapter` VMAD（`Scripts`+`ScriptFragments.IndexedScriptFragment` 綁 `Fragment_<i>`、choice `Flags=RunImmediately\|FragmentIndex`），gated on `.pex`。`ParsePerkEntry`（預設 Activate）|
| Build P2 | `Generator.PerkFragments.cs` | perk fragment 純產生器：`<perk>_Frags extends Perk`、`Fragment_<i>(ObjectReference akTargetRef, Actor akActor)` 含 fragmentBody；`PerkNeedsFragmentScript`/`PerkFragmentScriptName`/`PerkFragmentChoices` |
| EntryPoints | `Generator.Build.Perks.EntryPoints.cs` | `EntryPointTabCount` vanilla 表（含 `Activate`=2）：每 EntryType 的隱藏 tab-count byte（防 CTD）|
| Validate | `Generator.Validate.Npcs.cs` | perk entry-point enum、ability spell ref、**addActivateChoice（buttonLabel 非空、spell/fragmentBody 至少一）/setText（text 非空）** |
| Package | `src/ModForge.Cli/Commands/Package.cs` | 任一 perk 有 fragmentBody → 編 `<perk>_Frags.psc`（與 quest/dialogue/scene fragment 同段），Build 時掛 PerkAdapter |
| Diag | `Diagnostics.Perks.cs` | entry-point effects / ability spell / ranks / conditions dump |

---

## Shouts（喊聲 SHOU / WOOP）
→ **說明文件**：[for_agent.md § 限制](../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Shouts.cs` | `ShoutSpec`（三段 word-of-power + spell + cooldown）|
| Build P1 | `Generator.Build.Shouts.cs` | 建 Shout scalar fields（word/spell refs pass 2 接）|
| Diag | `Diagnostics.Shouts.cs` | word list / spell tiers / cooldown dump |

---

## Long-tail Records（長尾雜項）
→ **說明文件**：[for_agent.md § 限制](../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)（支援記錄類型列表）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `IngredientSpec`, `AmmunitionSpec`, `ScrollSpec`, `SoulGemSpec`, `KeySpec`, `FurnitureSpec`, `SoundSpec` |
| Spec | `Spec.cs` | `keywords`, `outfits`, `statics`, `activators`（頂層欄位）|
| Build P1 | `Generator.Build.LongTail.cs` | 建上述所有小 record |
| Build P2 | `Generator.Build.LongTail.Wire.cs` | keyword 陣列 + sound ref + alternate-texture ref 接線 |
| Validate | `Generator.Validate.Items.More.cs` | texture-path / model-path / sound-file sanity |

---

## Global Variables（GLOB）— 共享旗標 / 計數器 / 常數
→ **說明文件**：[SPEC-items.md § globals](../../../docs/spec/SPEC-items.md#globals-glob--shared-flags--counters--constants)

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
→ **說明文件**：[SPEC-items.md § textureSets](../../../docs/spec/SPEC-items.md#texturesets-txst--retexture-without-a-new-mesh)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `TextureSetSpec`（8 槽路徑）|
| Build P1 | `Generator.Build.TextureSets.cs` | 建 TextureSet（diffuse/normal/mask/glow/height/env/multilayer/backlight）|
| Validate | `Generator.Validate.Helpers.cs` | `CheckTexPath` |
| Diag | `Diagnostics.TextureSets.cs` | 8 槽路徑 + alternate-texture consumer dump |

---

## External Assets（外部資源）
→ **說明文件**：[SPEC-items.md § external assets](../../../docs/spec/SPEC-items.md#external-assets--your-own-meshes--textures--sounds-model-sounds-assets) · [external_assets.md](../../../docs/external_assets.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Build | `Assets.cs` | 複製 Meshes/Textures/Sounds 樹到輸出目錄 |
| Validate | `Generator.Validate.Helpers.cs` | `CheckModelPath` / `CheckSoundFile` |
