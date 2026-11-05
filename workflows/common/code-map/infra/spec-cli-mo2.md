# CODE_MAP.infra — Spec 根・CLI 命令・MO2 Profile

← [CODE_MAP.infra](../CODE_MAP.infra.md)

## Spec 根（頂層 DTO）
→ **說明文件**：[SPEC-intro.md](../../../../docs/spec/SPEC-intro.md)（cross-reference & IDs、完整 record 類型表）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.cs` | `ModSpec`（頂層 DTO，所有 record family 的清單欄位）|
| Spec | `Spec.Requires.cs` | `RequirementSpec`（`ModSpec.Requires`，**nullable**：null＝spec 沒寫這段＝完全不檢查）＋ `RequirementConverter`（`"PROTEUS.esp"` 字串簡寫 ↔ 物件形態）。`plugin`＝會被雙向檢查的 master；`name`＝**沒有 plugin 的相依**（SKSE DLL / loose files，純文件永不檢查）；`version`＝**只是標籤，無法驗證**（esp 沒有 mod 版本欄位）。檢查邏輯在 `Generator.Requires.cs` |
| Spec | `SpecRefs.cs` | `$ref`/`$env` 反序列化前預處理器（純 `JsonNode` resolver + `ResolveFile` disk 入口；注入 file/env lookup 以可測）；CLI 在 deserialize 前跑。見 [SPEC-refs.md](../../../../docs/spec/SPEC-refs.md) |

---

## CLI 命令進入點
→ **說明文件**：[for_agent_cli.md](../../../../docs/for_agent_cli.md)（命令速查 + 常見陷阱）

| 層次 | 檔案 | 命令 |
|-----|-----|-----|
| CLI | `Program.cs` | `Main` 入口，依序試 `DispatchCore`／`DispatchDiagnostics`，都不吃就落到 `Usage()`；`ResolveSpecJson`（單一 chokepoint，跑 `SpecRefs.ResolveFile`）→ `ReadSpec` JSON 反序列化 |
| CLI | `Program.Dispatch.cs` | `DispatchCore`：`build`／`package`／`voicelines`／`voicediag`／`extract-voices`／`voice-annotate`／`compile`／`catalog`／`gamedata`／`questnodes`／`modlist`／`check-dependencies` 等核心命令的 argv 形態比對 + `CoreUsage` 說明文字（`modlist snapshot/diff` 由此線加入） |
| Catalog | `Catalog.cs` / `Catalog.Schema.cs` / `Catalog.Export.cs` | 離線 SQLite/FTS5 catalog：generic major record（FormKey/plugin/type/EditorID/name/model path）+ source SHA-256/path/load-order provenance；DB schema/version gate 對舊 cache 明示 rebuild；保留 override occurrences，`export-json` 依明示 low→high order 只吐 winner，可選 21 種 scene Browser base 的 record-type filter（不以 model path 誤刪 ARMO）、拒絕覆寫輸入 DB，flushed temporary→atomic replace，契約為 `schemas/scene-catalog.schema.json`。未做 record-specific schema。|
| CLI | `CatalogCmd.cs` | `catalog build/query/get/sources/export-json` facade；query/get/sources 可輸出人讀 TSV 或 camelCase JSON，`export-json --placeable` 產 scene-capture Browser 精簡檔。|
| CLI | `Program.Build.cs` | `build` / `validate` / `package` / `compile` / `voicelines` / `extract-voices`；`validate` 的 `CheckUnknownFields` + deserialize 都跑在 `$ref`/`$env` **解析後**的 JSON；`build` 後另印 `annotations`（advisory，不生記錄）與 `references`（label→既有 ref 綁定清單）兩行摘要；`ReportDependencies` 印非 vanilla master ＋寫 `<plugin>.requires.txt` 旁檔（作者面向）；`package` 也印，並另把**玩家面向** `REQUIREMENTS.txt`（`RequiresFileText forShippedMod:true`）寫進出貨夾——玩家最需要「先裝哪些前置」；**`RequiresOk` ＝ `requires[]` 的閘門**（用到沒宣告 → 印錯誤、**在 `PluginIo.Write` 之前 return 1，esp 完全不寫**；`package` 走同一個閘門），**`SyncRequiresFile` ＝ `build --sync-requires`**（用 `JsonNode` 就地改寫 spec 檔的 `requires[]`；requires 來自 `$ref` include 時拒絕改寫，免得宣告分叉）|
| CLI | `CheckDependenciesCmd.cs` | `check-dependencies <plugin> --plugins <plugins.txt> [--implicit-plugins <implicit.txt>]`：argv/header 編排、結果格式化與 exit 0/1；壞參數或輸入由頂層轉 exit 2 |
| CLI | `Program.Translate.cs` | `extract` / `apply` / `applyloc` |
| CLI | `Package.cs` | `package` **編排器**（2026-08-27 拆檔後只剩 94 行）：讀 spec → 前置編譯生成片段 → `Generator.Build` + 寫 esp/seq → 編使用者腳本 → 出貨 `.pex` → 複製 assets → 寫 loose files |
| CLI | `Package.Fragments.cs` | 步驟 1：**ModForge 自己生成**的片段前置編譯（quest stage / dialogue set-stage / scene / perk / MCM bridge）。必須早於 `Generator.Build`，VMAD 只在 `.pex` 確認存在時才掛 |
| CLI | `Package.Scripts.cs` | 步驟 4–5：使用者 `.psc` 編譯（含 dispatcher sibling-header 機制）＋ word-wall 片段；步驟 5b–5g：依 spec 用到什麼就出貨哪顆內嵌 `.pex` |
| CLI | `Package.LooseFiles.cs` | 步驟 7：action-system loose files（OAR / BDI / PIE / SPID / MCM / FLM / KID / BOS / AOS / SkyPatcher）＋ `.hkx` 解析與複製 |
| CLI | `Package.Compile.cs` | `Package.cs` 的 static helper：生成片段編譯（`CompileGeneratedFragment`）、MCM count/required-bridge safety gates、embedded `.pex` 出貨（`ShipEmbeddedPex`）、action-system loose file 寫出（`WriteLooseFile`）|

---

## MO2 Profile Snapshot / Diff
→ **說明文件**：[for_agent_cli.md § 保存與比較 MO2 profile 快照](../../../../docs/for_agent_cli.md)

| 層次 | 檔案 | 職責 | Tests |
|---|---|---|---|
| Core | `ModlistSnapshot.cs` | v1 snapshot/source/mod/plugin records；三份 MO2 profile 文字解析、正規化雜湊與行數、nullable 來源缺席狀態；plugins parser 重用 `PluginListing.Parse` | `Commands/ModlistSnapshotTests.cs` |
| Core | `ModlistSnapshot.Json.cs` | 確定性 camelCase JSON、嚴格讀入、版本／來源／名稱／index 驗證 | `Commands/ModlistSnapshotTests.cs` |
| Core | `ModlistSnapshot.Diff.cs` | 純函式 diff；mod/plugin 新增移除、啟用與位置變更、来源 metadata 變更與 JSON 輸出 | `Commands/ModlistSnapshotTests.cs` |
| CLI | `ModlistSnapshotCmd.cs` | `modlist snapshot` 原子寫檔、保護 profile 輸入；`modlist diff [--json]`；exit 0/1/2 | `Commands/ModlistSnapshotTests.cs` |

`schemas/modlist-snapshot.schema.json`（見 Examples & Schema 表）為此契約的 JSON Schema 來源。
