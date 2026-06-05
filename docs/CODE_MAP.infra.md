# CODE_MAP — 基礎設施・CLI・驗證・打包・Papyrus・翻譯

← [CODE_MAP.md](CODE_MAP.md)

涵蓋：CLI 命令進入點、build orchestrator、BuildContext 狀態機、spec 驗證 pipeline、Papyrus 編譯、asset 打包、翻譯 extract/apply、plugin I/O。

---

## 1. Spec 根（資料定義）

| 檔案 | 主要型別 |
|-----|---------|
| `src/ModForge.Core/Spec.cs` | `ModSpec`（頂層 DTO，所有 record family 的清單欄位）|
| `src/ModForge.Core/GlobalUsings.cs` | 全域 using 別名 |

---

## 2. CLI 進入點

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Cli/Program.cs` | argv dispatcher；`gen`/diagnostic 命令；`ReadSpec` JSON 反序列化 |
| `src/ModForge.Cli/Program.Build.cs` | `build`/`validate`/`package`/`compile` 命令實作 |
| `src/ModForge.Cli/Program.Translate.cs` | `extract`/`apply`/`applyloc` 命令 |
| `src/ModForge.Cli/Package.cs` | `package` 完整流程：Papyrus 編譯 + Assets 複製 + MO2 資料夾組裝 |

---

## 3. Build Orchestrator

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Generator.cs` | 外部入口 `Generator.Build(spec, opts)`；`BuildResult`/`BuildStats` |
| `src/ModForge.Core/Generator.Build.cs` | 兩段 pipeline：pass 1（建所有 record）→ pass 2（接 FormLink）完整呼叫序列 |
| `src/ModForge.Core/Generator.BuildContext.cs` | 狀態容器：mod handle、warnings、editorId/formKey 對照表、placement/package/vendor tracking |
| `src/ModForge.Core/Generator.BuildContext.Utilities.cs` | master link-cache 管理；`PackageDataLocation` slot 建構 |
| `src/ModForge.Core/Generator.Helpers.cs` | 靜態 helpers：armor/enchantment/grid-coord 解析、flag 解析、ref resolver（in-spec vs external）|

---

## 4. 驗證 Pipeline

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Generator.Validate.cs` | 進入點 `Validate(spec)`；`ValidateContext`；`RegisterAll`/`Reg`/`CheckRef` |
| `src/ModForge.Core/Generator.Validate.Helpers.cs` | 共用 helpers：`CheckEnum`、`CheckEffects`、`ValidComparison`、`CheckCondition`、`CheckModelPath`/`CheckTexPath`/`CheckSoundFile` |

各領域 Validate 檔案見對應子 index（dialogue-quests / world / items-magic / npcs-packages）。

---

## 5. 打包・Papyrus・資產

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/Papyrus.cs` | Papyrus 編譯：Wine/CK PapyrusCompiler.exe 包裝 + native Linux 整合 + error capture |
| `src/ModForge.Core/Assets.cs` | 外部資產打包：複製 Meshes/Textures/Sounds 樹到輸出目錄 |
| `src/ModForge.Core/SeqFile.cs` | 寫 `.seq` manifest（StartGameEnabled quest 強制啟動）|
| `src/ModForge.Core/Demo.cs` | 手建 demo plugin（sanity check 用）|

---

## 6. Plugin I/O・翻譯・工具

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Core/PluginIo.cs` | Mutagen load/write 包裝；ESL 2048 record 安全檢查 |
| `src/ModForge.Core/Translator.cs` | 字串 extract（→ JSON source/target）+ apply（inline or Localized .STRINGS）|
| `src/ModForge.Core/Support.cs` | UTF-8 provider（CJK localization）+ string-entry translation slot helpers |

---

## 7. Diagnostics（dump 基礎）

| 檔案 | 做什麼 |
|-----|-------|
| `src/ModForge.Cli/Diagnostics.cs` | `dump`/`find` 命令 dispatcher |
| `src/ModForge.Cli/Diagnostics.Dump.cs` | 全 record 列舉（name/editorId/key）|
| `src/ModForge.Cli/Diagnostics.Dump.More.cs` | 擴充 dump（icons/flags/nested sub-records）|

各領域 Diagnostics 見對應子 index。

---

## 8. Docs

| 連結 | 內容 |
|-----|-----|
| `docs/SPEC-workflow.md` | CLI workflow + validate/build/package 流程（EN）|
| `docs/zh-TW/SPEC-workflow.md` | （zh-TW）|
| `docs/for_agent_cli.md` | CLI 命令速查 + 常見陷阱（EN）|
| `docs/for_agent_lib.md` | Library API 使用方式 |
