# ModForge 現有 builder 與缺口

← [perk-entry-points](perk-entry-points.md)

## 六、ModForge 現有 Perks builder 能做什麼 + 缺口

### 6-1 現有能力（有 code 為據）

`Generator.Build.Perks.cs:WirePerks()` + `Spec.Perks.cs:PerkEffectSpec`：

- **`kind="ability"`**：emit `PerkAbilityEffect`，`Spell` → FormLink → SPEL（✅）
- **`kind="entryPoint"`**：只 emit `PerkEntryPointModifyValue`，function 支援 Set/Add/Multiply，`Value` 為 float（✅ 已支援 ModifyValue 類）
- **PerkConditionTabCount**：自動查 `EntryPointTabCount` 表，unlisted fallback = 2（✅ 有保護）
- **effect-level conditions**：`PerkCondition { RunOnTabIndex = 0 }` + `BuildCondition()`，支援 HasKeyword/GetIsID 等（✅）
- **perk-level conditions**：plain `Condition` list（✅）
- **診斷工具**：`perkdiag` 可印出 perk 所有 effects；`perkdiag entrypoints` 列出所有 EntryType 名稱（✅）

### 6-2 確認缺口（直接引自 mod-survey-gaps.md 缺口 #1）

> **Perk entry-point `AddActivateChoice` / `SetText` + fragment 膠水**——（最高價值，#1）`Generator.Build.Perks.cs:WirePerks()` 只 emit `PerkEntryPointModifyValue`（+ `ability`），`EntryPointTabCount` 表也沒列這兩個。Immersive Interactions 需 29× AddActivateChoice + 4× SetText。scope：新增 `entrypoint` 子類 emit `PerkEntryPointAddActivateChoice`（帶 GetIsID/keyword/FLST 條件）與 `PerkEntryPointSetText` + Perk-fragment dispatcher（VMAD `Extends Perk`、`Fragment_N`→quest-script call，仿 `Generator.Build.Scripts.cs` 既有 dialogue/scene fragment 膠水）。注意 `perk-conditiontabcount-ctd`。

具體缺口：

| 缺口 | 現狀 | 需補 |
|---|---|---|
| `PerkEntryPointAddActivateChoice` emit | 不支援（WirePerks 只 emit ModifyValue） | 新 emit 路徑 |
| `PerkEntryPointSetText` emit | 不支援 | 新 emit 路徑 |
| `ButtonLabel`（LString）設定 | PerkEffectSpec 無此欄位 | 新 spec 欄位 |
| `Flags.FragmentIndex` 設定 | PerkEffectSpec 無此欄位 | 新 spec 欄位 |
| `Flags.RunImmediately / ReplaceDefault` | 無 | 新 spec 欄位 |
| `PerkAdapter` + `PerkScriptFragments` VMAD | Perk 完全無 VMAD 生成 | 新 pass（仿 AttachSceneFragments） |
| `PF_<perk>.psc` fragment 腳本生成 | 無 perk fragment 生成器 | 新 GeneratePerkFragmentSource() |
| `AddActivateChoice` tab-count | FilterActivation(74) 已在 EntryPointTabCount 表（= 2）✅ | SetActivateLabel(81) 未在表 ⚠️ |

---

