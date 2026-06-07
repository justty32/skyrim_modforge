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
| `Helpers.cs` | 共用測試 helper（非 test class，供其他 *Tests.cs 使用）|

---

---

## Spec 根（頂層 DTO）
→ **說明文件**：[SPEC-intro.md](SPEC-intro.md)（cross-reference & IDs、完整 record 類型表）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.cs` | `ModSpec`（頂層 DTO，所有 record family 的清單欄位）|

---

## CLI 命令進入點
→ **說明文件**：[for_agent_cli.md](for_agent_cli.md)（命令速查 + 常見陷阱）

| 層次 | 檔案 | 命令 |
|-----|-----|-----|
| CLI | `Program.cs` | `gen` / `find` / diagnostic dispatcher；`ReadSpec` JSON 反序列化 |
| CLI | `Program.Build.cs` | `build` / `validate` / `package` / `compile` |
| CLI | `Program.Translate.cs` | `extract` / `apply` / `applyloc` |
| CLI | `Package.cs` | `package` 完整流程：Papyrus 編譯 + Assets 複製 + MO2 資料夾組裝 |

---

## Build Orchestrator（兩段 Pipeline）
→ **說明文件**：[SPEC-workflow.md § Workflow](SPEC-workflow.md#workflow)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Entry | `Generator.cs` | 外部入口 `Generator.Build(spec, opts)`；`BuildResult` / `BuildStats` |
| Pipeline | `Generator.Build.cs` | pass 1（建所有 record）→ pass 2（接 FormLink）完整呼叫序列 |
| State | `Generator.BuildContext.cs` | 狀態容器：mod handle / warnings / editorId-formKey 對照表 / placement-package-vendor tracking |
| Helpers | `Generator.BuildContext.Utilities.cs` | master link-cache 管理；`PackageDataLocation` slot 建構 |
| Helpers | `Generator.Helpers.cs` | 靜態 helpers：armor/enchantment/grid-coord 解析；ref resolver（in-spec vs external）|

---

## 驗證 Pipeline
→ **說明文件**：[SPEC-workflow.md § Workflow](SPEC-workflow.md#workflow)（validate → fix → build 流程）· [for_agent_cli.md](for_agent_cli.md)（validate 指令）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Entry | `Generator.Validate.cs` | 進入點 `Validate(spec)`；`ValidateContext`；`RegisterAll` / `Reg` / `CheckRef` |
| Helpers | `Generator.Validate.Helpers.cs` | `CheckEnum` / `CheckEffects` / `ValidComparison` / `CheckCondition` / `CheckModelPath` / `CheckTexPath` / `CheckSoundFile` |

各領域 Validate 檔案見對應子 index（dialogue-quests / world / items-magic / npcs-packages）。

---

## Papyrus 編譯
→ **說明文件**：[for_agent_cli.md § compile](for_agent_cli.md)（`compile` 命令 + 環境需求）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Papyrus.cs` | Wine/CK PapyrusCompiler.exe 包裝 + native Linux 整合 + error capture（exit code 0 bug 處理）|
| CLI | `Package.cs` | `package` 時自動編譯 script + 夾帶 MFStoryEventDispatch.pex（§5b，ScriptEvent quest）+ MFSceneBanterController.pex（§5c，autoStart scene）；編 user script 時把 embed 的 dispatcher `.psc` 解到 temp 當 sibling header（`Fire()` 才解析得到）|
| Asset | `assets/papyrus/MFSceneBanterController.psc` | 在場偵測 Scene controller（extends Quest）；`.pex` embed 進 CLI，前置編譯同 dispatcher |

---

## 打包（Package）& 外部資產
→ **說明文件**：[SPEC-items.md § external assets](SPEC-items.md#external-assets--your-own-meshes--textures--sounds-model-sounds-assets) · [external_assets.md](external_assets.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Assets.cs` | 複製 Meshes/Textures/Sounds 樹到輸出目錄 |
| CLI | `Package.cs` | 完整 MO2 資料夾組裝（plugin + assets + scripts + seq）|

---

## 翻譯 Extract / Apply
→ **說明文件**：[for_agent_cli.md § 翻譯工作流程](for_agent_cli.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Translator.cs` | 字串 extract（→ JSON source/target）+ apply（inline or Localized .STRINGS）|
| Core | `Support.cs` | UTF-8 provider（CJK localization）+ string-entry translation slot helpers |
| CLI | `Program.Translate.cs` | `extract` / `apply` / `applyloc` 命令 |
| CLI | `Program.Schema.cs` | `CheckUnknownFields`：遞迴比對 JSON key 與 C# 型別屬性，抓拼字錯誤；跳過 `_*` / `//*` 注釋慣例 |

---

## Plugin I/O
→ **說明文件**：[for_agent_cli.md § 環境需求](for_agent_cli.md)（ESL 限制說明）· [engine-internals.md](engine-internals.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `PluginIo.cs` | Mutagen load/write 包裝；ESL 2048 record 安全檢查 |
| Core | `SeqFile.cs` | 寫 `.seq` manifest（StartGameEnabled quest 強制啟動）|

---

## Diagnostics 基礎
→ **說明文件**：[for_agent_cli.md § dump](for_agent_cli.md)（`dump` / `find` 命令）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| CLI | `Diagnostics.cs` | `dump` / `find` 命令 dispatcher（`find` 型別經反射 `I<Type>Getter`，含短別名 `idle`→`IdleAnimation` 供 PlayIdle 探查）|
| CLI | `Diagnostics.Dump.cs` | 全 record 列舉（name/editorId/key）|
| CLI | `Diagnostics.Dump.More.cs` | 擴充 dump（icons/flags/nested sub-records）|
| CLI | `Diagnostics.Identity.cs` | `identitydiag <esp>`：從建好的 plugin 還原身份系統 wiring——讀 controller VMAD 還原 faction↔code registry、default-grant quest（factions/grants/perks）、acquire books（MFIdentityBook props：faction/grant/**perk**/scene/toggle）、兩個控制 GLOB（純讀 plugin record/VMAD，無需 spec/CK）|

各領域 Diagnostics 見對應子 index。

---

## Demo & 測試輔助

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Demo.cs` | 手建 demo plugin（sanity check 用，`gen` 命令呼叫）|
