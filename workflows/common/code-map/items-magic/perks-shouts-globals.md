# CODE_MAP.items-magic — 特技・喊聲・Global

← [CODE_MAP.items-magic](../CODE_MAP.items-magic.md)

## Perks（特技 PERK）
→ **說明文件**：[SPEC-items.md § perks](../../../../docs/spec/SPEC-items.md#perks-perk) · [engine-internals.md § Perk entry points](../../../../docs/engine-internals.md#perk-entry-points-carry-a-hidden-tab-count-byte)

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
→ **說明文件**：[for_agent.md § 限制](../../../../docs/for_agent.md#limits--be-honest-do-not-over-claim)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Shouts.cs` | `ShoutSpec`（三段 word-of-power + spell + cooldown）|
| Build P1 | `Generator.Build.Shouts.cs` | 建 Shout scalar fields（word/spell refs pass 2 接）|
| Diag | `Diagnostics.Shouts.cs` | word list / spell tiers / cooldown dump |

---

## Global Variables（GLOB）— 共享旗標 / 計數器 / 常數
→ **說明文件**：[SPEC-items.md § globals](../../../../docs/spec/SPEC-items.md#globals-glob--shared-flags--counters--constants)

全域共享的單一數字，存檔保存，**condition 零腳本可讀**（`GetGlobalValue`）。三子型：short / long(int) / float；`constant` 旗標 = 唯讀調參。當旗標 / re-arm token（與 quest stage 互補，見 [CODE_MAP.dialogue-quests.md § Story Manager / conditions]）。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Globals.cs` | `GlobalSpec`（editorId/type/value/constant）；`Spec.cs` 頂層 `globals` |
| Build P1 | `Generator.Build.Globals.cs` | 建 GlobalShort/Int/Float（`AddNewShort/Int/Float`）+ 初值 + Constant major flag；在 BuildFormKeyTable 前，故 condition/region 可按 editorId 引用 |
| Validate | `Generator.Validate.cs` | `ValidateGlobals`（type ∈ short\|long\|float）+ editorId 唯一 |
| 範例 | `examples/globals.json` | 三型 + constant + dialogue 條件讀旗標 |
| Tests | `GlobalTests.cs` | 三子型 + 初值 + constant flag + condition 引用 + validate |
