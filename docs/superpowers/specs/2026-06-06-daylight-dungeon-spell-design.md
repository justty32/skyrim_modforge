# 白晝地城法術 + IMAD builder — 設計 spec

日期：2026-06-06
狀態：設計定稿，待 review → writing-plans

## 1. 目標

一個可施放的法術：**開關型**，開啟後讓整座地城「像 worldspace 白天那樣明亮」；再施放一次關閉。採**混合**做法，**零外部依賴**（純 vanilla 引擎機制 + ModForge 生成）：

- **整體白晝觀感** → 一個自訂 ImageSpace Modifier（IMAD），由腳本套用/移除（全畫面提亮，涵蓋「整座地城」）。
- **近處真實光影** → 一個跟隨施法者的大半徑光（vanilla Light archetype，同燭光術機制）。

附帶產出（這次的主菜）：給 ModForge 新增**可重用的 IMAD record builder**，對齊 CLAUDE.md「之後可做：Imagespace」。

### 非目標（YAGNI）

- 不真正改 cell ambient（需 SKSE，違反零依賴）。
- 不做「進任何地城自動點亮」的 cell 監聽（本版只做手動開關）。
- 不做 interior-only 閘門（開著時在戶外只是短暫過曝，可接受；文檔註明為地城設計）。
- 不寫存檔保險：採 AddSpell 常駐 ability，狀態天然隨存檔走、讀檔後 `OnEffectStart` 自動重套，無需額外工程（見 §5）。

## 2. 全新能力：IMAD (ImageSpace Modifier) record builder

ModForge 目前無 IMAD builder（`ExplosionSpec.imageSpaceModifier` 只能 ref 既有 IMAD）。本版新增：

- **`Spec.MagicFx.cs` 新增 `ImageSpaceModifierSpec`**，v1 核心子集欄位：
  - `editorId`
  - `brightnessMultiplier`（HDR 提亮，預設效果 ×1.6）
  - `contrast`
  - `saturation`（白晝可略降飽和）
  - `tintColor`（RGB）+ `tintAmount`（微暖 tint）
  - `fadeDuration`（cross-fade 秒數，0.5s）
- **build**：新增 `Generator.Build.ImageSpace.cs`（或併入 `Generator.Build.Projectiles.cs`），建 IMAD record，映射上述子集到 Mutagen `ImageSpaceAdapter`；未映射欄位留 Mutagen 預設。
- **頂層 spec 欄位**：`imageSpaceModifiers: []`（與 `lights`/`projectiles` 等並列）。
- **白賺**：建好後 `ExplosionSpec.imageSpaceModifier` 也能 ref 自訂 IMAD（既有接線，無需改動）。

> 風險：Mutagen `ImageSpaceAdapter` 欄位多、部分可動畫化（陣列/曲線）。v1 只映射上列純量子集，實作時對 Mutagen API 核對確切欄位名與型別；不確定的欄位寧可留預設、不亂塞。

## 3. Record 鏈（開關型，雙 SPEL）

```
SPEL  MFDaylightToggle   (SpellType=Spell / CastType=FireAndForget / TargetType=Self / Alteration)
 └─ MGEF  MFDaylightToggleEffect  (Archetype=Script, dur=0, Self)
       └─ 掛 MFDaylightToggle.psc

SPEL  MFDaylightActive   (SpellType=Ability / CastType=ConstantEffect / TargetType=Self / 無 cost)
 ├─ MGEF#1  MFDaylightLight    (Archetype=Light, Association→LIGT, Self)
 │      └─ LIGT  MFDaylightLite   半徑 4096 / 暖白(255,250,235) / 無閃爍
 └─ MGEF#2  MFDaylightVision   (Archetype=Script, Self)
        ├─ 掛 MFDaylightVisionEffect.psc（持 ImageSpaceModifier property → MFDaylightIMAD）
        └─ IMAD  MFDaylightIMAD   ← §2 新 builder 產出

SpellTome  MFDaylightTome   教 MFDaylightToggle（取得管道；現有 SpellTome builder）
```

承重假設（已驗證）：
- `MagicEffectSpec.Archetype="Light"` + `Association`（assoc form）已支援 → 跟隨光用現有 builder（`Generator.Build.Magic.cs` pass-2 `WireMagicFxRefs` 解 association）。
- `SpellSpec.SpellType/CastType/TargetType` 支援 Ability/ConstantEffect/Self → 常駐載體 OK。
- 掛自訂腳本到 MGEF 已是解決能力（既有 magic-effect trigger `MFSE_SpellTrigger.psc` + 通用 `ScriptAttachSpec`）。

## 4. 兩支腳本（手寫，ModForge 編譯 + 掛載）

### MFDaylightToggle.psc（~6 行）
```papyrus
Scriptname MFDaylightToggle extends ActiveMagicEffect
Spell Property MFDaylightActive Auto
Event OnEffectStart(Actor akTarget, Actor akCaster)
    if akCaster.HasSpell(MFDaylightActive)
        akCaster.RemoveSpell(MFDaylightActive)   ; 已開 → 關
    else
        akCaster.AddSpell(MFDaylightActive, false) ; 已關 → 開
    endif
EndEvent
```

### MFDaylightVisionEffect.psc（~12 行）
```papyrus
Scriptname MFDaylightVisionEffect extends ActiveMagicEffect
ImageSpaceModifier Property MFDaylightIMAD Auto
Event OnEffectStart(Actor akTarget, Actor akCaster)
    MFDaylightIMAD.ApplyCrossFade()   ; 套白晝提亮
EndEvent
Event OnEffectFinish(Actor akTarget, Actor akCaster)
    MFDaylightIMAD.PopTo(MFDaylightIMAD) ; 或 .Remove()，移除提亮
EndEvent
```
（`PopTo`/`Remove` 的正確 API 實作時對照 vanilla 夜視腳本確認。）

property 由 `ScriptAttachSpec.properties` 接到對應生成 record（toggle→Active spell、vision→IMAD）。

## 5. 存檔行為（已拍板：採 AddSpell）

- `AddSpell/RemoveSpell` 的 ability 待在玩家法術列表 → **隨存檔走**。
- 讀檔後常駐 ability 的 `OnEffectStart` 自動重跑 → imagespace 重新套上、跟隨光重生。
- 結論：**開著的狀態讀檔後自動恢復，無需任何存檔保險程式碼**。這滿足「不需要為存檔特別處理」。

## 6. 預設參數（可調，第一版）

| 項 | 值 |
|---|---|
| Toggle 法術 | Spell / FireAndForget / Self / Alteration / Adept / magicka cost ~150 |
| Active 載體 | Ability / ConstantEffect / Self / 無 cost |
| LIGT | 半徑 4096 / 暖白 (255,250,235) / 無閃爍 / 無陰影（omni fill） |
| IMAD | brightnessMultiplier ×1.6 / 微暖 tint / saturation 略降 / fade 0.5s |

數值（尤其 IMAD 提亮量與 LIGT 半徑/色）以 in-game 觀感迭代為準，第一版必再調。

## 7. 測試 / 驗證

- **單元測試**（`tests/ModForge.Core.Tests`，新增 `DaylightSpellTests.cs` 或併入 magic-fx 測試）：
  - IMAD builder 產出 IMAD，brightness/tint/fade 等映射欄位值正確。
  - `MFDaylightActive` 有 2 個 effect；MGEF#1 Light archetype 的 association 解析到 LIGT。
  - MGEF#2 / toggle MGEF 各掛上對應腳本，且 property 接到正確生成 record（IMAD / Active spell）。
  - `MFDaylightToggle` 為 FireAndForget/Self；`MFDaylightActive` 為 Ability/ConstantEffect。
- **結構驗證**：CLI `dump` 檢視；可選新 diag `imaddiag <in.esp> <0xFORMID>` 印 IMAD 欄位（對齊既有 `lightdiag`/`mgefdiag` 慣例）。
- **in-game**（使用者執行）：`package`→zip→`~/skyrim_mods`（獨立 `ModForgeDaylight.zip`），確認：白晝觀感是否足夠/會否過曝、跟隨光是否到位、開關是否正常、讀檔後是否自動恢復。→ 迭代調 §6 數值。

## 8. 維護鏈（commit 前對齊，依 ModForge CLAUDE.md 優先級）

- 程式碼：`Spec.MagicFx.cs`(新 `ImageSpaceModifierSpec`) + `Generator.Build.ImageSpace.cs`(新) + validate（IMAD 欄位範圍）+ 可選 `Diagnostics`(imaddiag)。
- examples：新增 `examples/daylight_spell_spec.json`（含 IMAD + 雙 SPEL + 雙 MGEF + LIGT + tome）+ `examples/scripts/MFDaylightToggle.psc` / `MFDaylightVisionEffect.psc`。
- `examples/spec.schema.json`：補 `imageSpaceModifiers` 頂層與 `ImageSpaceModifierSpec` 欄位。
- CODE_MAP：`docs/CODE_MAP.items-magic.md` 加 IMAD builder 行（含 Tests）。
- 文檔：`docs/SPEC-magic.md` 加 IMAD 段。
- HTML：不要求。

## 9. 開放問題（實作期解決，非阻塞）

1. Mutagen `ImageSpaceAdapter` 確切欄位名/型別與 HDR 提亮對應欄位 → 實作首步先寫一支最小 build + dump 對照 vanilla IMAD 驗證映射。
2. `ImageSpaceModifier` 移除的正確 Papyrus API（`PopTo` vs `Remove` vs 記錄 apply id）→ 對照 vanilla 夜視/吸血鬼腳本。
3. Light archetype 在 ConstantEffect/Self ability 下是否如燭光術般跟隨（vanilla `CandlelightFFSelf` 為 FireAndForget+dur；常駐 ability 版需 in-game 確認光是否持續跟隨；若不跟隨，退路為 toggle 腳本 `PlaceAtMe` 一個 LIGT 並 `MoveTo` 跟隨，或改用既有 follow 機制）。
