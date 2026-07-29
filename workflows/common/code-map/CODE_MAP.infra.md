# CODE_MAP — 基礎設施・CLI・驗證・打包・Papyrus・翻譯

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：CLI 命令進入點、build orchestrator、BuildContext 狀態機、spec 驗證 pipeline、Papyrus 編譯、asset 打包、翻譯 extract/apply、plugin I/O。

## Examples & Schema

| 檔案 | 性質 | 職責 |
|-----|-----|-----|
| `examples/spec.schema.json` | **源碼** | JSON Schema for spec；IDE IntelliSense 用；`Spec.*.cs` 欄位有增減時必須同步 |
| `examples/sample_spec.json` | **源碼** | 完整示範 spec；`for_agent_cli.md` 直接引用，是 agent 最先參考的範例 |
| `examples/proof_spec.json` | 煙霧測試 | 基礎功能 e2e 驗證用 spec |
| `examples/showcase_spec.json` | 煙霧測試 | 完整功能展示 spec |

---

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `ValidateTests.cs` | 跨領域 validate（editorId 唯一性、ref 合法性等通用規則）|
| `SeqFileTests.cs` | `.seq` manifest 生成（StartGameEnabled quest 列表）|
| `VoiceTests.cs` | `BuildTtsArgs`/`BuildLipGenArgs` 命令列、`VoiceFileName` CK 命名格式、`WriteFuz` header（含無 lip 情形）、`GenerateLip` 官方 LipGenerator 端到端（`RequiresSkyrim`，env-gated 自跳過）|
| `VoiceSpeakerTests.cs` | `voicelines` speaker 偵測（GetIsID / alias / faction 條件解析）|
| `VoiceAnnotateTests.cs` | `voice-annotate`：clip 檔名→INFO FormKey 解析 + 從 INFO 讀 emotion/intensity/text 建 manifest entry |
| `SpecRefsTests.cs` | `$ref` 三形態（string / array 鏈式 / long-form `{from,pointer}`）、`$env`（value / default / 缺報錯）、`$ref`+`$env` 衝突、cycle、sibling deep-merge、`ResolveFile` disk round-trip |
| `DependencyTests.cs` | 外部 master 可見性：純 vanilla spec **什麼都不印**（negative case）／capture 與手寫 spec 都列出 mod master ＋歸因到**作者寫的**欄位（含「巨集展開後仍報 `capturedNpcs[]` 不報 `npcs[]`」）／CC 分類／摘要＋`requires.txt` 內容／**釘死「分析不改 esp 一個 byte」**|
| `RequiresTests.cs` | 宣告式 `requires[]` 雙向檢查：**沒有 requires 段＝完全不檢查**（negative case，向後相容）／用到沒宣告→錯誤且訊息指出**是哪一行 spec 欄位**／宣告沒用到→警告／空 `[]`＝只准 vanilla／`name` 條目（無 plugin 的 SKSE 相依）永不檢查但進旁檔／`version` 只是標籤（旁檔標 NOT verified）／**玩家面向 shipped 形式**（`forShippedMod`）保留安裝清單＋reason/version/連結、拿掉 spec 欄位歸因與 rebuild 指示／`SyncRequires` 加新丟舊保留 metadata＋同步後檢查通過／`validate` 形狀檢查／JSON 字串簡寫與缺段＝null／**釘死「requires[] 不改 esp 一個 byte」**|
| `Helpers.cs` | 共用測試 helper（非 test class，供其他 *Tests.cs 使用）|

---

---

## Spec 根（頂層 DTO）
→ **說明文件**：[SPEC-intro.md](../../../docs/spec/SPEC-intro.md)（cross-reference & IDs、完整 record 類型表）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.cs` | `ModSpec`（頂層 DTO，所有 record family 的清單欄位）|
| Spec | `Spec.Requires.cs` | `RequirementSpec`（`ModSpec.Requires`，**nullable**：null＝spec 沒寫這段＝完全不檢查）＋ `RequirementConverter`（`"PROTEUS.esp"` 字串簡寫 ↔ 物件形態）。`plugin`＝會被雙向檢查的 master；`name`＝**沒有 plugin 的相依**（SKSE DLL / loose files，純文件永不檢查）；`version`＝**只是標籤，無法驗證**（esp 沒有 mod 版本欄位）。檢查邏輯在 `Generator.Requires.cs` |
| Spec | `SpecRefs.cs` | `$ref`/`$env` 反序列化前預處理器（純 `JsonNode` resolver + `ResolveFile` disk 入口；注入 file/env lookup 以可測）；CLI 在 deserialize 前跑。見 [SPEC-refs.md](../../../docs/spec/SPEC-refs.md) |

---

## CLI 命令進入點
→ **說明文件**：[for_agent_cli.md](../../../docs/for_agent_cli.md)（命令速查 + 常見陷阱）

| 層次 | 檔案 | 命令 |
|-----|-----|-----|
| CLI | `Program.cs` | `gen` / `find` / diagnostic dispatcher；`ResolveSpecJson`（單一 chokepoint，跑 `SpecRefs.ResolveFile`）→ `ReadSpec` JSON 反序列化 |
| CLI | `Program.Build.cs` | `build` / `validate` / `package` / `compile` / `voicelines` / `extract-voices`；`validate` 的 `CheckUnknownFields` + deserialize 都跑在 `$ref`/`$env` **解析後**的 JSON；`build` 後另印 `annotations`（advisory，不生記錄）與 `references`（label→既有 ref 綁定清單）兩行摘要；`ReportDependencies` 印非 vanilla master ＋寫 `<plugin>.requires.txt` 旁檔（作者面向）；`package` 也印，並另把**玩家面向** `REQUIREMENTS.txt`（`RequiresFileText forShippedMod:true`）寫進出貨夾——玩家最需要「先裝哪些前置」；**`RequiresOk` ＝ `requires[]` 的閘門**（用到沒宣告 → 印錯誤、**在 `PluginIo.Write` 之前 return 1，esp 完全不寫**；`package` 走同一個閘門），**`SyncRequiresFile` ＝ `build --sync-requires`**（用 `JsonNode` 就地改寫 spec 檔的 `requires[]`；requires 來自 `$ref` include 時拒絕改寫，免得宣告分叉）|
| CLI | `Program.Translate.cs` | `extract` / `apply` / `applyloc` |
| CLI | `Package.cs` | `package` 完整流程：Papyrus 編譯 + Assets 複製 + MO2 資料夾組裝 |
| CLI | `Package.Compile.cs` | `Package.cs` 的 static helper：生成片段編譯（`CompileGeneratedFragment`）、embedded `.pex` 出貨（`ShipEmbeddedPex`）、action-system loose file 寫出（`WriteLooseFile`）|

---

## Build Orchestrator（兩段 Pipeline）
→ **說明文件**：[SPEC-workflow.md § Workflow](../../../docs/spec/SPEC-workflow.md#workflow)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Entry | `Generator.cs` | 外部入口 `Generator.Build(spec, opts)`；`BuildResult` / `BuildStats`。`BuildResult.Notes` ＝**advisory INFO 頻道**（`BuildContext.Note()`，與 `Warnings` 分離：note 不該讓乾淨的 build 變黃、也不該弄髒「零警告」這個斷言）——目前唯一來源＝`references[]` label 掉進 package location 槽的 area-anchor 護欄。CLI `build`/`package` 在 warnings 之後照印 |
| Pipeline | `Generator.Build.cs` | pass 1（建所有 record）→ pass 2（接 FormLink）完整呼叫序列 |
| State | `Generator.BuildContext.cs` | 狀態容器：mod handle / warnings / editorId-formKey 對照表 / placement-package-vendor tracking |
| Helpers | `Generator.BuildContext.Utilities.cs` | master link-cache 管理；`PackageDataLocation` slot 建構 |
| Helpers | `Generator.Helpers.cs` | 靜態 helpers：armor/enchantment/grid-coord 解析；ref resolver（in-spec vs external）|
| Deps | `Generator.Dependencies.cs` | **外部 master 可見性**（純資訊，不動產物）：`AnalyzeDependencies(mod, spec)` → `BuildResult.Dependencies`。**master 清單以「建好的 mod」為準**（掃每筆 record 的 FormKey ＋ `EnumerateFormLinks`——抓得到 spec 字串沒寫、由 deep-copy 帶進來的 master）；**歸因以 spec 為準**（reflection walk → `capturedNpcs[0].spells[17] = PROTEUS.esp:0x08073D`）。歸因快照在 `ExpandMacros` **展開前**取（`ModSpec.AuthoredRefSources`，internal），否則 captured NPC 會報成巨集生出來的 `npcs[0]`。vanilla ＝ Skyrim/Update/Dawnguard/HearthFires/Dragonborn；**CC（`ccXXXSSE###` / `_ResourcePack`）不算 vanilla**（按帳號購買，缺了照樣靜默不載）|
| Deps | `Generator.Dependencies.Report.cs` | 上面那份分析的**輸出文字**：`DependencySummary`（build 摘要行，純 vanilla spec 一個字都不印）＋ `RequiresFileText`。會把 spec 的 `requires[]` 中繼資料折進來（`reason`/`version`/`url`＋**沒有 plugin 的相依**如 PapyrusUtil）；`version` 印出來但標明 **NOT verified**。**兩種形式**（`forShippedMod` 旗標）：預設＝**作者面向**，`build` 寫 `<plugin>.requires.txt` 旁檔（含 spec 欄位歸因＋「刪 spec 行後 rebuild」指示）；`forShippedMod:true`＝**玩家面向**，`package` 寫進出貨夾 `REQUIREMENTS.txt`（拿掉 spec 內部＋rebuild 指示，只留「先裝哪些 mod＋各自 reason/version/連結」——拿到 mod 的玩家既沒 spec 也沒 ModForge）|
| Deps | `Generator.Requires.cs` | **宣告式 `requires[]` 的雙向檢查**（候選 (b)；DTO 在 `Spec.Requires.cs`）：`CheckRequires(spec, deps)` → `BuildResult.Requires`。**build 有 link 但沒宣告＝錯誤**（CLI 直接不寫 esp——缺 master ＝ Skyrim 靜默不載，那正是要擋的漂移）；**宣告了但從沒 link＝警告**（陳舊行；runtime-only 相依請用 `name` 條目）。**spec 沒有 `requires` 段（null）＝完全不檢查**（向後相容硬要求）；**空陣列 `[]` 也是宣告**＝「只用 vanilla」。`SyncRequires(declared, deps)`（純函式）＝ `build --sync-requires` 的合併邏輯（保留作者寫的 metadata、丟掉陳舊項、`name` 條目不動）。`ValidateRequires` 只做**形狀**檢查（`validate` 沒有建好的 mod，做不了實質比對）。**`requires[]` 不進 esp**（測試釘死）|

---

## 驗證 Pipeline
→ **說明文件**：[SPEC-workflow.md § Workflow](../../../docs/spec/SPEC-workflow.md#workflow)（validate → fix → build 流程）· [for_agent_cli.md](../../../docs/for_agent_cli.md)（validate 指令）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Entry | `Generator.Validate.cs` | 進入點 `Validate(spec)`；`ValidateContext`；`RegisterAll` / `Reg` / `CheckRef` |
| Helpers | `Generator.Validate.Helpers.cs` | `CheckEnum` / `CheckEffects` / `ValidComparison` / `CheckCondition` / `CheckModelPath` / `CheckTexPath` / `CheckSoundFile` |

各領域 Validate 檔案見對應子 index（dialogue-quests / world / items-magic / npcs-packages）。

---

## Papyrus 編譯
→ **說明文件**：[for_agent_cli.md § compile](../../../docs/for_agent_cli.md)（`compile` 命令 + 環境需求）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Papyrus.cs` | Wine/CK PapyrusCompiler.exe 包裝 + native Linux 整合 + error capture（exit code 0 bug 處理）|
| CLI | `Package.cs` | `package` 時自動編譯 script + 夾帶 MFStoryEventDispatch.pex（§5b，ScriptEvent quest）+ MFSceneBanterController.pex（§5c，autoStart scene）；編 user script 時把 embed 的 dispatcher `.psc` 解到 temp 當 sibling header（`Fire()` 才解析得到）|
| Asset | `assets/papyrus/MFSceneBanterController.psc` | 在場偵測 Scene controller（extends Quest）；`.pex` embed 進 CLI，前置編譯同 dispatcher |

---

## 打包（Package）& 外部資產
→ **說明文件**：[SPEC-items.md § external assets](../../../docs/spec/SPEC-items.md#external-assets--your-own-meshes--textures--sounds-model-sounds-assets) · [external_assets.md](../../../docs/external_assets.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Assets.cs` | 複製 Meshes/Textures/Sounds 樹到輸出目錄 |
| CLI | `Package.cs` | 完整 MO2 資料夾組裝（plugin + assets + scripts + seq + action-system loose files）|

---

## 動作系統 asset/config 生成（OAR / BDI / PIE，loose files、非-esp）
→ **說明文件**：[SPEC-animation.md](../../../docs/spec/SPEC-animation.md) · 設計 [specs/archive/2026-06-14-action-system-asset-generation-design.md](../../specs/archive/2026-06-14-action-system-asset-generation-design.md) · 調查 [mod-survey/action-system/](../../../sub_projs/mod-survey/action-system/)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.AnimationReplacer.cs` | DTO：`AnimationReplacerSpec`/`OarSubmodSpec`/`OarConditionSpec`/`NpcMovesetSpec`/`BehaviorDataSpec`/`BdiEntrySpec`/`PayloadMacroSpec`/`PieMacroSpec`（ModSpec 三個 list 在 `Spec.cs`）|
| Core | `OarConditions.cs` | `OarConditionSpec`→OAR JSON（`Emit`）、`NpcMovesetSpec`→條件束（`Expand`）、武器型 enum（`WeaponType`）、form-ref 解析（`ParseForm`）|
| Core | `OarGen.cs` | OAR 資料夾樹 + root/submod `config.json`（`Generate`）+ `.hkx` 擺放清單（`HkxPlacements`）|
| Core | `BdiGen.cs` | BDI flat-array JSON（`BdiGen.Generate`）+ PIE `.ini` 巨集表（`PieGen.Generate`）|
| Validate | `Generator.Validate.AnimationReplacer.cs` | priority/條件名/武器名/form/BDI type 校驗（`.hkx` 存在性在 `Package.cs` 查）|

`.hkx` 動畫本體**不生成**（使用者自備，經 `assets`/spec 目錄）；Blender→hkx、Pandora、SCAR AI 不在範圍。

---

## SKSE 分發器 config 生成（SPID / MCM / FLM / KID / BOS / AOS / SkyPatcher，loose、非-esp）
→ **說明文件**：[SPEC-distribution.md](../../../docs/spec/SPEC-distribution.md) · 格式調查 findings：[spid](../../../sub_projs/mod-survey/findings/spid.md)、[mcm-helper-config-json](../../../sub_projs/mod-survey/findings/mcm-helper-config-json.md)、[formlist-manipulator-config-core](../../../sub_projs/mod-survey/findings/formlist-manipulator-config-core.md)、[keyword-item-distributor-config-1](../../../sub_projs/mod-survey/findings/keyword-item-distributor-config-1.md)、[base-object-swapper-config](../../../sub_projs/mod-survey/findings/base-object-swapper-config.md)、[animobject-swapper-overview-config](../../../sub_projs/mod-survey/findings/animobject-swapper-overview-config.md)、[skypatcher-records-and-config](../../../sub_projs/mod-survey/findings/skypatcher-records-and-config.md)（D 組 ini pipeline，見 [roadmap/all-findings-gaps.md](../../roadmap/all-findings-gaps.md)）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.SpidDistribution.cs` | DTO：`SpidDistributionSpec`/`SpidEntrySpec`（ModSpec list `spidDistributions` 在 `Spec.cs`）|
| Core | `SpidGen.cs` | `SpidDistributionSpec`→`<file>_DISTR.ini`（`Generate`）、單行欄位組裝＋尾段 NONE 修剪（`Line`）|
| Validate | `Generator.Validate.Spid.cs` | type 白名單 / record 必填 / chance 0–100（`ValidateSpidDistributions`）|
| Spec | `Spec.Mcm.cs` | DTO：`McmSpec`/`McmPageSpec`/`McmControlSpec`（ModSpec list `mcmConfigs` 在 `Spec.cs`）|
| Core | `McmGen.cs` | `McmSpec`+`identity`→`MCM/Config/<identity>/config.json`（`System.Text.Json` JsonObject）+ `settings.ini`（`Generate`/`BuildConfigJson`/`BuildSettingsIni`/`SplitId`）。**`identity`＝宿主插件檔名 stem**（MCM Helper 走 `path(plugin).stem()` 找資料夾，非 spec modName）；config.json `modName` 欄位＝identity（self required-plugin），`displayName`＝spec modName/displayName |
| Core | `Generator.Build.Mcm.cs` | `BuildMcmQuests`：每個 `mcmConfigs` 生 Start-Game-Enabled `MF_MCM_*` QUST，掛 `ModForgeMCM`(extends MCM_ConfigBase, property `ModName`) + `PlayerAlias`(ALFR=0x14, 掛 `SKI_PlayerLoadGameAlias`)。**MCM Helper 沒這個 quest 不出選單**（仿 `BuildDefaultIdentityQuest`+`AttachAliasScript`）|
| Asset | `assets/papyrus/ModForgeMCM.psc` | 可重用空子類 `extends MCM_ConfigBase`；一顆 `.pex` 服務所有 MCM 選單（差別只在 VMAD `ModName`）。**編譯需 MCM Helper+SkyUI SDK headers**（非香草）：供 `MCM_ConfigBase.psc`/`SKI_*.psc` 進 header cache 再編；`.pex` embed 進 CLI、gitignore |
| Validate | `Generator.Validate.Mcm.cs` | control type / sourceType 白名單、value control 需 `key:Section` id、slider 需 min+max、stepper/enum 需 options；**PropertyValue\*/action 為 MVP 範圍外，擋掉**（`ValidateMcmConfigs`）|
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

`_DISTR.ini` 寫在 mod 資料夾**根目錄**（≠ SKSE/Plugins）；RecordID/EditorID 由玩家 load order 解析，ModForge 離線不驗。example：`examples/spid_distribution_spec.json`。
**MCM Helper**（Idea D-2，**in-game 確認 2026-06-20**）：MVP＝ini-backed（`ModSettingBool/Int/Float/String`），DLL 把玩家改動存到 `MCM/Settings/<plugin-stem>.ini`；`config.json` 用 `name`→`pageDisplayName`、value 欄位收進 `valueOptions`。⚠️ **光丟 config.json 不出選單**（早期「零 Quest/Papyrus」假設是錯的）——ModForge 自動生 Start-Game-Enabled 註冊 QUST（`ModForgeMCM` + `PlayerAlias`/`SKI_PlayerLoadGameAlias`），需 MCM Helper+SkyUI。⚠️ **資料夾名＝插件 stem，非 spec modName**（MCM Helper `FormUtil::GetModName`＝`path(plugin).stem()`，不讀 Papyrus ModName property；錯了會 in-game 跳「check json syntax」）。`PropertyValue*`/`action.CallFunction`（需 per-mod 子類）為範圍外。example：`examples/mcm_config_spec.json`。
**FLM**（Idea D-4）：`_FLM.ini` 寫在 mod 根，runtime 把 form 追加進**任意既有 FLST**（vanilla/他 mod）零 override 零衝突；自建 FLST 仍走 esp-side `formLists[]`。MVP 涵蓋 FormList 操作行 + Filter/Alias/Group/Collection 定義；`ModEvent`（需 Papyrus 發送）+ 特化快捷（Plant/BToys/…）為範圍外。example：`examples/formlist_inject_spec.json`。
**KID**（D-5，**in-game 確認 2026-06-20**）：`_KID.ini`，把 Keyword 依 filter 掛到 record（unknown EditorID → KID 自建 KYWD）。syntax example：`examples/kid_distribution_spec.json`；known-good log-test：`examples/kid_keyword_test_spec.json`（新 KYWD→鐵武器，`po3_KeywordItemDistributor.log` 看 `added to N`）。
**BOS**（D-6，**in-game 確認 2026-06-20**）：`_SWAP.ini`，reference 載入時把 base object 換成另一個（可帶 location 條件/transform/chance），MVP 限 `[Forms]` section。syntax example：`examples/object_swap_spec.json`；known-good visible-test：`examples/bos_treeswap_visible_spec.json`（全松樹→白楊；遠景 LOD 不換、只看近處）。
**AOS**（D-7，**in-game 確認 2026-06-20**）：`_ANIO.ini`，換 idle 時手持的 ANIO 道具（隨機池 + NPC/faction/trait 條件）。syntax example：`examples/anim_object_swap_spec.json`；known-good visible-test：`examples/aos_bucket_test_spec.json`（酒館酒杯→水桶）。
**SkyPatcher**（D-3，**in-game 確認 2026-06-20**）：`SkyPatcher/<recordType>/<file>.ini`，runtime 依 filter 改 record（NPC 加 spell/perk、leveled list 注入）。MVP 不白名單欄位，verbatim emit。syntax example：`examples/skypatcher_spec.json`；known-good visible-test：`examples/skypatcher_scale_test_spec.json`（Nord `height=1.5`；尺寸 key 是 `height` 非 `setScale`）。
> **全 D-group 七個分發器（SPID/MCM/FLM/KID/BOS/AOS/SkyPatcher）皆 IN-GAME CONFIRMED 2026-06-20**。離線只驗結構、runtime 由 DLL 對玩家 load order 解析 ref；各框架的 known-good 測試 + SKSE log 路徑見 memory `dll-loose-ini-distributors-confirmed`。

---

## 工作流腳本（`scripts/`，bash）
→ **說明文件**：[tooling](../../tooling/README.md)（外部工具 / env var / 依賴）；CLAUDE.md「前置步驟」「出貨腳本」

| 檔案 | 職責 |
|-----|-----|
| `scripts/bootstrap-pex.sh` | fresh-clone 一鍵編譯 `assets/papyrus/*.psc`→`.pex`（embed 用，glob 全部、自動納入新增的）。需 Papyrus toolchain；任一失敗 → exit 非零 |
| `scripts/ship.sh` | 一般 mod 出貨：`package`→FLAT zip（plugin 在根，防 stale-ESP）→`$MODFORGE_SHIP_DIR`（預設 `~/skyrim_mods/mine`）。plugin 名讀自建好的 `.esp`（耐 `$ref`/`$env`）；`--clean-prefix` 清同前綴舊 zip（防 MO2 裝到 stale），否則只 warn |
| `scripts/ship-voice.sh` | 語音 mod 出貨：`package`→`voicelines`(對 packaged esp 生 `Sound/Voice/<plugin>/…`)→`voicediag`(planned vs shipped)→FLAT zip。需 `MODFORGE_TTS_BIN`，否則 abort |
| `scripts/test-offline.sh` | `dotnet test --filter "Category!=RequiresSkyrim"`（透傳額外 args）|
| `scripts/extract-skyrim-masters.sh` | 抽 vanilla master 到 `docs/reference/`（`MODFORGE_SKYRIM_MASTERS`/`MODFORGE_REFERENCE_OUT`）|

---

## 語音克隆（TTS → .fuz）
→ **說明文件**：[SPEC-workflow.md § Voice](../../../docs/spec/SPEC-workflow.md#voice-tts-voice-cloning--fuz)
→ **TTS 合成已解耦**：`text+emotion+ref→.wav` 在獨立基石專案 `sub_projs/skyrim-voicegen/`（`voicegen.py` + wrappers），靠 `MODFORGE_TTS_BIN` 協議連，合約見該夾 `PROTOCOL.md`。**下表全是 ModForge 端的「包裝」職責**（plan/解析/.wav→fuz/lip/擺位），不含合成。

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Voice.cs` | `VoiceTemplateSpec`（`engine` f5\|fish-s2\|chatterbox\|gptsovits\|xtts；`fish`/`fishspeech`/`fish-speech` 為 Fish S2 alias；`referenceWav`/`referenceText` zero-shot reference、`modelPath` 微調模型、`rvcModel`、`seed`、`speed`、`exaggeration`、`language`）+ `VoiceLineSpec` 全域輸出設定（`format` fuz\|wav\|xwm、`skipLip`）；`NpcSpec.voiceTemplate`（→ template id）在 `Spec.Actors.cs` |
| Core | `Voice.cs` | 呼外部 TTS（`MODFORGE_TTS_BIN`；`BuildTtsArgs` pure 組 engine/ref/model/seed/speed/exaggeration/language + **emotion/intensity**（從 INFO 記錄取，非 spec 欄位）全數傳給 TTS process，協議規格見 `sub_projs/skyrim-voicegen/PROTOCOL.md`）；`EncodeXwma`（`MODFORGE_XWMAENCODE`）走 Wine；`WinePath`（Unix→`Z:\` 轉換，xwma/lip 共用）|
| Core | `Voice.Lip.cs` | `.lip` lip-sync 生成（`GenerateLip` 一個入口、兩後端）：**優先**官方 CK `LipGenerator.exe`（`MODFORGE_LIPGEN`，簽名 `<wav> <text> -Language:<lang> -OutputFileName:<lip>`，FonixData.cdf 自 exe 同夾找、免給 cdf 路徑、**已在本機 Wine 實跑產出合法 .lip 2026-06-13**）；**退化**社群 FaceFXWrapper（`MODFORGE_FACEFX` + `MODFORGE_FONIXDATA`）。`BuildLipGenArgs` pure 可單測 |
| Core | `Fuz.cs` | `.fuz` 容器拆解（FUZE header → lip + audio；audio ext 自動偵測 xwm/wav）|
| Core | `Generator.Build.Voice.cs` | `WriteFuz`（lip + audio 打包成 .fuz）+ `VoiceFileName` CK 命名（`quest10_topic15_formid8_n.fuz`：quest EditorID 前 10 字 + topic EditorID 前 15 字 + INFO FormID hex8 + response 序號）|
| Core | `Generator.Build.Voice.Plan.cs` | `VoiceLinePlanEntry` record + `BuildVoiceLinePlan`（每個 INFO response → speaker(s)/voiceType 資料夾/引擎檔名的可交付計畫，供 CLI dry-run/`voicediag` 用）+ `VoiceTypeFolderName`（spec voiceType ref → `Sound/Voice` 資料夾名，含 `SkyrimVoiceTypeFolders` 表）+ `NormalizeVoiceFormat`/`IsSafeVoiceFolder` |
| Core | `Generator.Build.Voice.Speakers.cs` | `ResolveVoiceSpeakers`：從建好 esp 的 INFO 條件解 speaker（GetIsID / GetIsAliasRef / GetInFaction / scene Dialog action）→ `VoiceSpeaker`(Npc + voiceType)；一個 INFO 可對多 speaker（faction），`SelectVoiceTargets` 去重成每個 distinct voiceType 一份。解不出 → `VoiceSpeakerResolution.Reason`（CLI 必須大聲報）。**`ResolveExternalSpeakerVoice`**：INFO gated on GetIsID(Subject) 的**外部 master NPC**（mod-only cache 解不了，如既有隨從 Sofia）→ 用 `voiceSpeakers[]`（`Spec.Voice.cs` `VoiceSpeakerSpec`：speaker ref→voiceType+template）直接給，繞過 NPC 解析；`BuildVoiceLinePlan` 與 voicelines loop 都先查它。在 Core 故可對 in-memory built mod 單測 |
| Core | `Archives.cs` | Mutagen 讀 BSA/BA2（extract + path filter；`extract-voices` / `voice-annotate` 用）|
| Core | `Voice.Annotate.cs` | 情緒標注 index：`VoiceAnnotation`(clip/text/emotion/intensity/infoFormId + 人填 override/intensityOverride/note) model + `VoiceAnnotate.TryParseInfoFormKey`(從 clip 檔名解 INFO FormKey;high byte→master)/`BuildEntry`(從 resolved INFO 讀 Emotion/EmotionValue/Text)。純函式可單測 (`VoiceAnnotateTests.cs`)|
| CLI | `Program.Build.Voice.Extract.cs` `VoiceAnnotateCmd` | `voice-annotate <esm> <voiceType> <bsa> <outDir>`：抽 clip→WAV(Archives.Extract+Fuz+ffmpeg)+ 對每 clip 從 `<esm>` 查 INFO emotion → 寫 `voice-annotations.json`。打底確定性(讀 INFO Emotion);人聽完改 manifest。Phase B(`voiceTemplates[].referenceLibrary` 情緒選 ref)另開 |
| Validate | `Generator.Validate.Voice.cs` | template id 非空 / engine 枚舉 / `npc.voiceTemplate` ref 存在 / `voiceLine.format` 枚舉；已掛進 `Validate` |
| CLI | `Program.Build.Voice.cs` | `voicelines <spec> <esp> [--dry-run\|--plan]` + `voicediag`：走訪建好 esp 的 INFO → 從條件找 speaker（GetIsID + **alias / faction 條件**；解不出 speaker → **loud warning**，不靜默 skip）→ WAV→xwm→fuz 寫到 `Sound/Voice/<plugin>/<voiceType>/`（已存在的檔 skip，無 hash cache）；**xwm 編碼失敗且 format=fuz → 改寫 loose `.wav` + warning（不把裸 WAV 包進 .fuz）**。含 `BuildNpcVoiceTemplateMap`/`BuildNpcVoiceTypeMap`/`BuildExternalVoiceMap`(voiceSpeakers[]→FormKey)/`PrintVoicePlan`/`GenerateVoiceLine` 的生成側 helpers。**F5 踩坑：ref clip 要短（~2-3s），太長/太密 F5 會估錯時長把輸出截斷**|
| CLI | `Program.Build.Voice.Extract.cs` | `extract-voices <bsa> <voiceType> <outDir> [plugin]`：抽 .fuz → ffmpeg 轉 wav（做 reference clip）；`[plugin]` 預設 Skyrim.esm，給 `SofiaFollower.esp` 等可抽既有隨從 BSA 的嗓音 ref。`VoiceAnnotateCmd`(`voice-annotate`，見上)亦在此。純抽取/轉檔側（Archives.Extract+Fuz.Split+ffmpeg），與生成側分檔以守 300 行上限 |

環境變數：`MODFORGE_TTS_BIN`（TTS wrapper，必要）、`MODFORGE_FISH_SPEECH_BIN`（僅 Fish S2 engine 需要）、`MODFORGE_XWMAENCODE`、`MODFORGE_LIPGEN`（官方 CK LipGenerator.exe，lip 首選）、`MODFORGE_FACEFX` + `MODFORGE_FONIXDATA`（社群 FaceFXWrapper，lip 退化路徑）——皆 Wine 路徑，缺則退化：無 xwm / 無 lip（嘴不動）。`format=fuz` 且未配任何 lip 工具時 `voicelines` 開頭發一次 loud warning。

---

## 翻譯 Extract / Apply
→ **說明文件**：[for_agent_cli.md § 翻譯工作流程](../../../docs/for_agent_cli.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Translator.cs` | 字串 extract（→ JSON source/target）+ apply（inline or Localized .STRINGS）|
| Core | `Support.cs` | UTF-8 provider（CJK localization）+ string-entry translation slot helpers |
| CLI | `Program.Translate.cs` | `extract` / `apply` / `applyloc` 命令 |
| CLI | `Program.Schema.cs` | `CheckUnknownFields`：遞迴比對 JSON key 與 C# 型別屬性，抓拼字錯誤；跳過 `_*` / `//*` 注釋慣例 |

---

## Plugin I/O
→ **說明文件**：[for_agent_cli.md § 環境需求](../../../docs/for_agent_cli.md)（ESL 限制說明）· [engine-internals.md](../../../docs/engine-internals.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `PluginIo.cs` | Mutagen load/write 包裝；ESL 2048 record 安全檢查；**masterless 防呆**：write 前掃 FormLinks，若零外部 ref（會 masterless → 遊戲靜默丟棄整個 esp，help/setstage not-found 但 MO2 顯示啟用，in-game 2026-06-20）自動補 Skyrim.esm 為唯一 master + `MastersListContent=NoCheck`（Mutagen 仍按 FormKey 正確映射 master index，FormID 不變、byte 同天生有 master 的 esp）；有外部 ref 維持 `Iterate`。測 `RelationshipAndEslTests`(masterless→補 master、有 ref→不重複) |
| Core | `SeqFile.cs` | 寫 `.seq` manifest（StartGameEnabled quest 強制啟動）|

---

## Diagnostics 基礎
→ **說明文件**：[for_agent_cli.md § dump](../../../docs/for_agent_cli.md)（`dump` / `find` 命令）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| CLI | `Diagnostics.cs` | `dump` / `find` 命令 dispatcher（`find` 型別經反射 `I<Type>Getter`，含短別名 `idle`→`IdleAnimation` 供 PlayIdle 探查）|
| CLI | `Diagnostics.Dump.cs` | 全 record 列舉（name/editorId/key）|
| CLI | `Diagnostics.Dump.More.cs` | 擴充 dump（icons/flags/nested sub-records）|
| CLI | `Diagnostics.Identity.cs` | `identitydiag <esp>`：從建好的 plugin 還原身份系統 wiring——讀 controller VMAD 還原 faction↔code registry、default-grant quest（factions/grants/perks）、**auto-grant trigger（faction ← GetActorValue(av) >= threshold）**、acquire books（MFIdentityBook props：faction/grant/perk/scene/toggle）、兩個控制 GLOB（純讀 plugin record/VMAD，無需 spec/CK）|
| CLI | `Diagnostics.Voice.cs` | `voicediag <esp>`：走訪所有 dialogue INFO，印出每個 response 期望的 `.fuz` 路徑（`Sound/Voice/<plugin>/<voiceType>/<quest>_<topic>_<formId>_<n>.fuz`）與 speaker/voiceType；reuse Core 的 `ResolveVoiceSpeakers`；無需 spec/Skyrim.esm/TTS|
| CLI | `Diagnostics.Records.cs` | targeted 單記錄 diag（lazy overlay，不 materialize 250MB master）：`cellblk`/`mgefdiag`/`lightdiag`/`refpos`/`packagediag` 等 |
| CLI | `Diagnostics.CellRefs.cs` | **`cellrefs <esp> <0xFORMID>`**：dump 單一 interior cell 的所有 placed REFR/ACHR（base FormKey + cell-local pos + rotation **RADIANS** + scale）成 CSV——逆向 vanilla cell 成 `placements[]`。記憶體安全：lazily 走 CELL block tree，命中 target FormID 後只處理那顆 cell 的 child group（Temporary+Persistent，數百 ref）就 return，絕不列舉所有 cell 的 children。rotation 是 esm 原生 radian，轉成 ModForge spec 的 degree 需 `*180/pi`。範例見 `docs/investigation/decode/sleeping-giant-inn-reverse-2026-06-13.md` + `examples/sleeping_giant_inn.json`。|
| CLI | `Diagnostics.GameData.cs` | **`gamedata <plugin> <outDir> [--strings <dir>]`**：streamed overlay 一趟 major-record pass，把 books/dialogue/quests/npcs/items/locations/magic 批次匯出成資料夾（給 agent 當參考；不 full-materialize、不 `.ToList` record group，跑得動 250MB master）。`ProvisionEnglishStringsAnyBsa` 從任一 BSA 抽 English STRINGS 解 localized Name |
| CLI | `Diagnostics.BookText.cs` | **`booktext <esm> <0xFORMID>`**：印一本 BOOK 的 localized Name + 全文 BookText（lore prose）；`ProvisionEnglishStrings` 從 master BSA 抽 English STRINGS |

各領域 Diagnostics 見對應子 index。

---

## Demo & 測試輔助

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Demo.cs` | 手建 demo plugin（sanity check 用，`gen` 命令呼叫）|
