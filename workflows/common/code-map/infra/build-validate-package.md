# CODE_MAP.infra — Build Orchestrator・驗證・Papyrus 編譯・打包

← [CODE_MAP.infra](../CODE_MAP.infra.md)

## Build Orchestrator（兩段 Pipeline）
→ **說明文件**：[SPEC-workflow.md § Workflow](../../../../docs/spec/SPEC-workflow.md#workflow)

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
| Deps | `InstalledDependencies.cs` | 以 Mutagen TES4 header reader 取得 direct masters；跨平台一致解析 MO2 星號式 `plugins.txt` 與明示 implicit plugin 清單，產 enabled/implicit/disabled/missing 結果。不查 instance、檔案存在、版本或遞迴依賴 |

---

## 驗證 Pipeline
→ **說明文件**：[SPEC-workflow.md § Workflow](../../../../docs/spec/SPEC-workflow.md#workflow)（validate → fix → build 流程）· [for_agent_cli.md](../../../../docs/for_agent_cli.md)（validate 指令）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Entry | `Generator.Validate.cs` | 進入點 `Validate(spec)`；`ValidateContext`；`RegisterAll` / `Reg` / `CheckRef` |
| Helpers | `Generator.Validate.Helpers.cs` | `CheckEnum` / `CheckEffects` / `ValidComparison` / `CheckCondition` / `CheckModelPath` / `CheckTexPath` / `CheckSoundFile` |

各領域 Validate 檔案見對應子 index（dialogue-quests / world / items-magic / npcs-packages）。

---

## Papyrus 編譯
→ **說明文件**：[for_agent_cli.md § compile](../../../../docs/for_agent_cli.md)（`compile` 命令 + 環境需求）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Papyrus.cs` | Wine/CK PapyrusCompiler.exe 包裝 + native Linux 整合 + error capture（exit code 0 bug 處理）；`CompileBest` 優先 native，編譯失敗（含 headers 不全）時自動 fallback Wine/CK |
| CLI | `Package.cs` | `package` 時自動編譯 script + 夾帶 MFStoryEventDispatch.pex（§5b，ScriptEvent quest）+ MFSceneBanterController.pex（§5c，autoStart scene）；編 user script 時把 embed 的 dispatcher `.psc` 解到 temp 當 sibling header（`Fire()` 才解析得到）|
| Asset | `assets/papyrus/MFSceneBanterController.psc` | 在場偵測 Scene controller（extends Quest）；`.pex` embed 進 CLI，前置編譯同 dispatcher |

---

## 打包（Package）& 外部資產
→ **說明文件**：[SPEC-items.md § external assets](../../../../docs/spec/SPEC-items.md#external-assets--your-own-meshes--textures--sounds-model-sounds-assets) · [external_assets.md](../../../../docs/external_assets.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Assets.cs` | 複製 Meshes/Textures/Sounds 樹到輸出目錄 |
| CLI | `Package.cs` | 完整 MO2 資料夾組裝（plugin + assets + scripts + seq + action-system loose files）|
