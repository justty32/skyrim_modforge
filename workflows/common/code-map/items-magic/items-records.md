# CODE_MAP.items-magic — 物品・captured items・長尾・貼圖・外部資產

← [CODE_MAP.items-magic](../CODE_MAP.items-magic.md)

## Weapons / Armor / Books / Misc
→ **說明文件**：[for_agent.md § 限制](../../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)（物品屬性確認清單）· [SPEC-intro.md](../../../../docs/spec/SPEC-intro.md)（頂層欄位表）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `WeaponSpec`, `ArmorSpec`, `BookSpec`, `MiscSpec`, `MessageSpec`（MESG notification + `buttons[]` menu）|
| Build P1 | `Generator.Build.Items.cs` | 建 Misc/Book/Weapon record（template cloning + base property defaults）|
| Build P1 | `Generator.Build.Messages.cs` | 建 Message (MESG) record（0 buttons＝notification；1–10 buttons＝Papyrus `Show()` 有序 menu；無 FormLink、純 pass-1）|
| Validate | `Generator.Validate.Items.cs` | template/ref + MESG 空 button label / 10-button 上限 |
| Diag | `Diagnostics.Records.cs` | Weapon/Armor 詳細欄位 dump |

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

## Long-tail Records（長尾雜項）
→ **說明文件**：[for_agent.md § 限制](../../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)（支援記錄類型列表）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `IngredientSpec`, `AmmunitionSpec`, `ScrollSpec`, `SoulGemSpec`, `KeySpec`, `FurnitureSpec`, `SoundSpec`, `ActivatorSpec`, `StaticSpec`, `MovableStaticSpec`（含可選 OBND min/max） |
| Spec | `Spec.Items.cs` | `keywords`, `outfits`, `statics`, `movableStatics`, `activators`（頂層欄位）|
| Build P1 | `Generator.Build.LongTail.cs` | 建上述所有小 record；STAT / MSTT / ACTI 可寫顯式 OBND |
| Build P2 | `Generator.Build.LongTail.Wire.cs` | keyword 陣列 + sound ref + alternate-texture ref 接線 |
| Validate | `Generator.Validate.Items.More.cs` | texture-path / model-path / sound-file sanity |
| Validate / Tests | `Generator.Validate.Items.cs` / `ExternalAssetTests.cs` | OBND 成對、有限整數、int16 範圍及 min ≤ max；ESP 序列化與 MSTT placement 回歸 |

---

## Texture Sets（TXST）
→ **說明文件**：[SPEC-items.md § textureSets](../../../../docs/spec/SPEC-items.md#texturesets-txst--retexture-without-a-new-mesh)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Items.cs` | `TextureSetSpec`（8 槽路徑）|
| Build P1 | `Generator.Build.TextureSets.cs` | 建 TextureSet（diffuse/normal/mask/glow/height/env/multilayer/backlight）|
| Validate | `Generator.Validate.Helpers.cs` | `CheckTexPath` |
| Diag | `Diagnostics.TextureSets.cs` | 8 槽路徑 + alternate-texture consumer dump |

---

## External Assets（外部資源）
→ **說明文件**：[SPEC-items.md § external assets](../../../../docs/spec/SPEC-items.md#external-assets--your-own-meshes--textures--sounds-model-sounds-assets) · [external_assets.md](../../../../docs/external_assets.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Build | `Assets.cs` | 複製 Meshes/Textures/Sounds 樹到輸出目錄 |
| Validate | `Generator.Validate.Helpers.cs` | `CheckModelPath` / `CheckSoundFile` |
