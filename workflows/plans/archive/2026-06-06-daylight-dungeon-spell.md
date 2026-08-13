# 白晝地城法術 + IMAD builder — 實作計畫

> 對照 spec：`workflows/specs/archive/2026-06-06-daylight-dungeon-spell-design.md`

**Goal:** 新增可重用的 IMAD (ImageSpace Modifier) record builder，並用它組出一個開關型「白晝地城」法術（imagespace 提亮 + Light-archetype 跟隨光），打包成 `ModForgeDaylight.zip` 供 in-game 測試。

**Architecture:** 真正的新程式碼只有 IMAD builder（spec 型別 + build + validate）。法術本體全靠既有能力組裝（spells / magicEffects / lights / books / scripts 都是現成 builder + pass-2 ref 解析）。兩支小腳本由 `package` 編譯掛載。

**Tech Stack:** C#/.NET 10、Mutagen.Bethesda.Skyrim 0.53.1、Papyrus。

Mutagen IMAD 事實（反射確認）：`ImageSpaceAdapter` / `IImageSpaceAdapterGetter`；提亮=`CinematicBrightnessMult`、對比=`CinematicContrastMult`、飽和=`CinematicSaturationMult`（皆 `ExtendedList<KeyFrame>{Time,Value}`，設一個 keyframe）；tint=`TintColor` `ExtendedList<ColorFrame>{Time,Color}`（amount=Color 的 alpha）；`Duration` float 純量、`Animatable` bool。建構：`new ImageSpaceAdapter(formKey, SkyrimRelease.SkyrimSE)`，`using Mutagen.Bethesda.Skyrim;` 即涵蓋 KeyFrame/ColorFrame。

---

## Task 1：Spec — ImageSpaceModifierSpec + 頂層 list

**Files:** Modify `src/ModForge.Core/Spec/Spec.MagicFx.cs`、`src/ModForge.Core/Spec/Spec.cs`、`src/ModForge.Core/Build/Generator.BuildContext.cs:149`

- ImageSpaceModifierSpec 欄位：`EditorId` / `BrightnessMultiplier`(float=1.6) / `Contrast`(float=1) / `Saturation`(float=1) / `TintColor`(ColorSpec?) / `TintAmount`(float 0..1=0) / `Duration`(float=1) / `Animatable`(bool=false)。
- Spec.cs 加 `public List<ImageSpaceModifierSpec> ImageSpaceModifiers { get; set; } = new();`（放 Explosions 後）。
- ToResult `total` 加 `+ spec.ImageSpaceModifiers.Count`。

## Task 2：Builder — BuildImageSpaceModifiers

**Files:** Create `src/ModForge.Core/Build/Generator.Build.ImageSpace.cs`；Modify `src/ModForge.Core/Build/Generator.Build.cs:34`（orchestrator）

- 仿 `Generator.Build.Projectiles.cs` 的 BuildContext instance 方法。建 `mod.ImageSpaceAdapters.AddNew()`，設 EditorID/Duration/Animatable，brightness/contrast/saturation 各 add 一個 `KeyFrame{Time=0,Value=…}`，tint 有色時 add `ColorFrame{Time=0,Color=FromArgb(alpha, r,g,b)}`。
- orchestrator 在 `ctx.BuildProjectiles();` 後加 `ctx.BuildImageSpaceModifiers();`。
- 無需 pass-2（IMAD 不持有對外 ref）；它被 MGEF 腳本 property 反向 ref，靠 BuildFormKeyTable 自動登記。

## Task 3：Validate

**Files:** Modify `src/ModForge.Core/Validate/Generator.Validate.MagicFx.cs`、`Generator.Validate.cs:101`

- `ValidateImageSpaceModifiers()`：brightness/contrast/saturation/duration >= 0；tintAmount 0..1（warn 不擋）。
- Validate.cs 加 `foreach (var im in spec.ImageSpaceModifiers) Reg(im.EditorId, "imageSpaceModifier");` 與 `ctx.ValidateImageSpaceModifiers();`。

## Task 4：單元測試

**Files:** Create `tests/ModForge.Core.Tests/Build/DaylightSpellTests.cs`

- IMAD builder：brightness/contrast/saturation 的 keyframe value、tint color+alpha、Duration 正確。
- 法術組裝：Active spell 有 2 effect、Toggle 為 FireAndForget/Self、Active 為 Ability/ConstantEffect、Light-archetype MGEF 的 Association 解到 LIGT。

## Task 5：Example + 腳本

**Files:** Create `examples/daylight_spell_spec.json`、`examples/scripts/MFDaylightToggle.psc`、`examples/scripts/MFDaylightVisionEffect.psc`

- spec：1 IMAD + 1 LIGT + 4 MGEF（toggle script / vision script / light / —）+ 2 SPEL（Toggle / Active）+ 1 BOOK（tome）+ 2 script attach（含 Source 指向 .psc、object property 接 Active/IMAD）。

## Task 6：文檔同步

**Files:** Modify `examples/spec.schema.json`、`docs/CODE_MAP.items-magic.md`、`docs/SPEC-magic.md`

## Task 7：Build → test → package → zip

- `dotnet test`（259/260 基線）。
- `package examples/daylight_spell_spec.json <out> --assets …` → 編譯 2 腳本 → zip 攤平 → `~/skyrim_mods/ModForgeDaylight.zip`。
