# CODE_MAP.infra — SKSE 分發器 config 生成

← [CODE_MAP.infra](../CODE_MAP.infra.md)

## SKSE 分發器 config 生成（SPID / MCM / FLM / KID / BOS / AOS / SkyPatcher，loose、非-esp）
→ **說明文件**：[SPEC-distribution.md](../../../../docs/spec/SPEC-distribution.md) · 格式調查 findings：[spid](../../../../../../analysis/mod-survey/findings/frameworks-tools/spid.md)、[mcm-helper-config-json](../../../../../../analysis/mod-survey/findings/frameworks-tools/mcm-helper-config-json.md)、[formlist-manipulator-config-core](../../../../../../analysis/mod-survey/findings/frameworks-tools/formlist-manipulator-config-core.md)、[keyword-item-distributor-config-1](../../../../../../analysis/mod-survey/findings/frameworks-tools/keyword-item-distributor-config-1.md)、[base-object-swapper-config](../../../../../../analysis/mod-survey/findings/frameworks-tools/base-object-swapper-config.md)、[animobject-swapper-overview-config](../../../../../../analysis/mod-survey/findings/frameworks-tools/animobject-swapper-overview-config.md)、[skypatcher-records-and-config](../../../../../../analysis/mod-survey/findings/frameworks-tools/skypatcher-records-and-config.md)（D 組 ini pipeline，見 [roadmap/all-findings-gaps.md](../../../roadmap/all-findings-gaps.md)）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.SpidDistribution.cs` | DTO：`SpidDistributionSpec`/`SpidEntrySpec`（ModSpec list `spidDistributions` 在 `Spec.cs`）|
| Core | `SpidGen.cs` | `SpidDistributionSpec`→`<file>_DISTR.ini`（`Generate`）、單行欄位組裝＋尾段 NONE 修剪（`Line`）|
| Validate | `Generator.Validate.Spid.cs` | type 白名單 / record 必填 / chance 0–100（`ValidateSpidDistributions`）|
| Spec | `Spec.Mcm.cs` | DTO：`McmSpec`/`McmPageSpec`/`McmControlSpec`（含 bool toggle 的 `global` GLOB bridge；ModSpec list `mcmConfigs` 在 `Spec.cs`）|
| Core | `McmGen.cs` | `McmSpec`+`identity`→`MCM/Config/<identity>/config.json` + `settings.ini`；`global` control 加 `action.CallFunction`。**`identity`＝宿主插件檔名 stem**；config.json `modName`＝identity，`displayName`＝spec modName/displayName |
| Core | `SafeOutputPath.cs` | 生成路徑的 containment/reparse guard：正規化後必須仍在輸出根目錄內，並可拒絕 output file 或 parent symlink/junction；擋住 OAR/loose-file `..` traversal 與跨根目錄寫入 |
| Core | `Generator.McmScripts.cs` | `global` controls → per-menu `MCM_ConfigBase` Papyrus source（bool setter 把 0/1 寫 GLOB）、穩定 script/property/function 命名 |
| Core | `Generator.Build.Mcm.cs` | `BuildMcmQuests`：每個 config 生 Start-Game-Enabled `MF_MCM_*` QUST；ini-only 掛 `ModForgeMCM`，有 `global` 時掛 generated subclass + VMAD GLOB properties；另有 player-forced alias + `SKI_PlayerLoadGameAlias` |
| Asset | `assets/papyrus/ModForgeMCM.psc` | ini-only 選單共用的空子類 `extends MCM_ConfigBase`；VMAD `ModName` 只是註冊／錯誤頁顯示 fallback，config 查找依宿主 plugin stem。有 `global` 的選單改用生成子類。**編譯需 MCM Helper+SkyUI SDK headers**（非香草）：供 `MCM_ConfigBase.psc`/`SKI_*.psc` 進 header cache 再編；`.pex` embed 進 CLI、gitignore |
| Validate | `Generator.Validate.Mcm.cs` | control/sourceType/id/range/options；`global` 只准 toggle+ModSettingBool 且 ref 必須可解；raw PropertyValue\*/action 仍擋掉 |
| Spec | `Spec.FormListInject.cs` | DTO：`FormListInjectSpec`/`FlmFilterSpec`/`FlmNamedListSpec`/`FlmCollectionSpec`/`FlmEntrySpec`（ModSpec list `formListInjects` 在 `Spec.cs`）|
| Core | `FlmGen.cs` | `FormListInjectSpec`→`<file>_FLM.ini`（`Generate`）；**無區段頭**（檔首 `[General]` 會讓 FLM v1.8.1 判 `Config file is empty` 跳過整檔，IN-GAME 2026-06-20）；先 emit Filter/Alias/Group/Collection 定義、再 FormList 操作行；filter ref 自動補 `#`|
| Validate | `Generator.Validate.Flm.cs` | file/target 非空、entry 需 forms、collection formType 白名單、filter 需 conditions（`ValidateFormListInjects`）|
| Spec | `Spec.KidDistribution.cs` | DTO：`KidDistributionSpec`/`KidEntrySpec`（`kidDistributions`）|
| Core | `KidGen.cs` | `→<file>_KID.ini`；`Keyword = kw\|type\|filters\|traits\|chance` 尾段 NONE 修剪（仿 SPID）|
| Validate | `Generator.Validate.Kid.cs` | keyword 必填、type 19 白名單、chance 0–100（`ValidateKidDistributions`）|
| Spec | `Spec.ObjectSwap.cs` | DTO：`ObjectSwapSpec`/`ObjectSwapGroupSpec`/`ObjectSwapEntrySpec`（`objectSwaps`）|
| Core | `BosGen.cs` | `→<file>_SWAP.ini`；`[Forms]`/`[Forms\|cond]` + `base\|swaps\|properties\|chance`（gap 留 `\|\|`）|
| Validate | `Generator.Validate.Bos.cs` | base/swaps 必填、chance 0–100（`ValidateObjectSwaps`）|
| Spec | `Spec.AnimObjectSwap.cs` | DTO：`AnimObjectSwapSpec`/`AnimObjectSwapEntrySpec`（`animObjectSwaps`）|
| Core | `AosGen.cs` | `→<file>_ANIO.ini`；`[Base\|FILTERS\|TRAITS]` header + `base\|swaps`（尾段空 segment 修剪）|
| Validate | `Generator.Validate.Aos.cs` | base/swaps 必填（`ValidateAnimObjectSwaps`）|
| Spec | `Spec.SkyPatcher.cs` | DTO：`SkyPatcherSpec`/`SkyPatcherLineSpec`/`SkyPatcherFieldSpec`（`skyPatchers`）|
| Core | `SkyPatcherGen.cs` | `→SKSE/Plugins/SkyPatcher/<recordType>/<file>.ini`；flat `filterK=v:modK=v`（無 section header）|
| Validate | `Generator.Validate.SkyPatcher.cs` | recordType 8 白名單、每行需 mods、key 非空（`ValidateSkyPatchers`）|
| CLI | `Package.cs` | loose-file 寫出（與 OAR/BDI/PIE/SPID/MCM/FLM 同一段；多數→mod 根＝`Data/`、MCM→`MCM/Config/<plugin-stem>/`＋`ShipEmbeddedPex("ModForgeMCM.pex")` gated on `McmConfigs.Count>0`、SkyPatcher→`SKSE/Plugins/SkyPatcher/<recordType>/`）|
