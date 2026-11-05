# CODE_MAP.infra — 動作系統 asset/config 生成

← [CODE_MAP.infra](../CODE_MAP.infra.md)

## 動作系統 asset/config 生成（OAR / BDI / PIE，loose files、非-esp）
→ **說明文件**：[SPEC-animation.md](../../../../docs/spec/SPEC-animation.md) · 調查 [mod-survey/action-system/](../../../../../../analysis/mod-survey/action-system/)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.AnimationReplacer.cs` | DTO：OAR replacer/submod/condition + OAR 2.2+ `conditionPresets`/`replacementAnimations` variant metadata + OAR 3.0 typed function/multifunction、`NpcMovesetSpec`/BDI/PIE（ModSpec 三個 list 在 `Spec.cs`）|
| Core | `OarConditions.cs` | `OarConditionSpec`→OAR JSON（`Emit`，含 PRESET 2.2.0）、`NpcMovesetSpec`→條件束（`Expand`）、武器型 enum（`WeaponType`）、form-ref 解析（`ParseForm`）|
| Core | `OarFunctions.cs` | OAR 3.0 typed built-ins：`CONDITION`/`RANDOM`/`ONE` multifunction + `PlaySound`、trigger JSON（`Emit`）|
| Core | `OarGen.cs` | OAR 資料夾樹 + root/submod `config.json`（含 `conditionPresets`/`replacementAnimDatas`/function sets；`Generate`）+ `.hkx` 擺放清單（`HkxPlacements`）|
| Core | `BdiGen.cs` | BDI flat-array JSON（`BdiGen.Generate`）+ PIE `.ini` 巨集表（`PieGen.Generate`）|
| Validate | `Generator.Validate.AnimationReplacer.cs` | priority/條件名/武器名/form/BDI type + OAR 2.2+/3.0 重名、PRESET/variant filename 引用、weight、typed functions 校驗（`.hkx` 存在性在 `Package.cs` 查）|
| Tests | `OarConditionsTests.cs` / `OarGenTests.cs` / `ValidateAnimationReplacerTests.cs` | OAR condition JSON、folder/config tree（含 OAR 2.2+/3.0 metadata/preset/function contract）、語義 validation |

`.hkx` 動畫本體**不生成**（使用者自備，經 `assets`/spec 目錄）；Blender→hkx、Pandora、SCAR AI 不在範圍。
