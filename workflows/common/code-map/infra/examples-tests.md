# CODE_MAP.infra — Examples & Schema・Tests

← [CODE_MAP.infra](../CODE_MAP.infra.md)

## Examples & Schema

| 檔案 | 性質 | 職責 |
|-----|-----|-----|
| `examples/spec.schema.json` | **源碼** | JSON Schema for spec；IDE IntelliSense 用；`Spec.*.cs` 欄位有增減時必須同步 |
| `examples/sample_spec.json` | **源碼** | 完整示範 spec；`for_agent_cli.md` 直接引用，是 agent 最先參考的範例 |
| `examples/proof_spec.json` | 煙霧測試 | 基礎功能 e2e 驗證用 spec |
| `examples/showcase_spec.json` | 煙霧測試 | 完整功能展示 spec |
| `schemas/modlist-snapshot.schema.json` | **源碼** | MO2 profile snapshot v1 JSON Schema |
| `fixtures/modlist-snapshot/{lf,crlf}/` | 人工 fixture | 每組三檔、每檔六行；LF 與 CRLF 對照，含空格／非 ASCII／停用／separator |

---

## Tests

| 測試檔案 | 涵蓋 |
|---------|-----|
| `ValidateTests.cs` | 跨領域 validate（editorId 唯一性、ref 合法性等通用規則）|
| `SeqFileTests.cs` | `.seq` manifest 生成（StartGameEnabled quest 列表）|
| `VoiceTests.cs` / `VoiceLiveContractTests.cs` | `BuildTtsArgs`/`BuildLipGenArgs` 命令列、`VoiceFileName` CK 命名格式、`WriteFuz` header（含無 lip 情形）、`GenerateLip` 官方 LipGenerator 端到端（`RequiresSkyrim`，env-gated 自跳過）；live contract 由 production `GenerateWav` 經跨平台 test wrapper 真 exec sibling production `voicegen.py`，以 deterministic fake Fish engine 驗全參數、空白路徑、合法 WAV 與非零/缺檔/header-only/截斷 fail-closed（缺 sibling/Python 明示 skip）|
| `VoiceSpeakerTests.cs` | `voicelines` speaker 偵測（GetIsID / alias / faction 條件解析）|
| `VoiceAnnotateTests.cs` | `voice-annotate`：clip 檔名→INFO FormKey 解析 + 從 INFO 讀 emotion/intensity/text 建 manifest entry |
| `SpecRefsTests.cs` | `$ref` 三形態（string / array 鏈式 / long-form `{from,pointer}`）、`$env`（value / default / 缺報錯）、`$ref`+`$env` 衝突、cycle、sibling deep-merge、`ResolveFile` disk round-trip |
| `DependencyTests.cs` | 外部 master 可見性：純 vanilla spec **什麼都不印**（negative case）／capture 與手寫 spec 都列出 mod master ＋歸因到**作者寫的**欄位（含「巨集展開後仍報 `capturedNpcs[]` 不報 `npcs[]`」）／CC 分類／摘要＋`requires.txt` 內容／**釘死「分析不改 esp 一個 byte」**|
| `Commands/CheckDependenciesTests.cs` | `check-dependencies`：TES4 header direct masters 對明示 `plugins.txt`／implicit 清單比對；enabled/disabled/missing、BOM/comment/case、衝突重複、空/全 disabled、壞輸入，以及真 CLI process exit 0/1/2 且不改輸入 bytes |
| `RequiresTests.cs` | 宣告式 `requires[]` 雙向檢查：**沒有 requires 段＝完全不檢查**（negative case，向後相容）／用到沒宣告→錯誤且訊息指出**是哪一行 spec 欄位**／宣告沒用到→警告／空 `[]`＝只准 vanilla／`name` 條目（無 plugin 的 SKSE 相依）永不檢查但進旁檔／`version` 只是標籤（旁檔標 NOT verified）／**玩家面向 shipped 形式**（`forShippedMod`）保留安裝清單＋reason/version/連結、拿掉 spec 欄位歸因與 rebuild 指示／`SyncRequires` 加新丟舊保留 metadata＋同步後檢查通過／`validate` 形狀檢查／JSON 字串簡寫與缺段＝null／**釘死「requires[] 不改 esp 一個 byte」**|
| `Helpers.cs` | 共用測試 helper（非 test class，供其他 *Tests.cs 使用）|
| `CatalogTests.cs` | 不需 Skyrim.esm 的 synthetic plugin round-trip：multiple source、FTS name/EditorID + type/plugin filter、exact FormKey/source lookup、load-order winner JSON（含 `--placeable` 同款 record-type filter、保留無 direct model 的合法 base；實際 exporter bytes 通過正式 Draft 2020-12 schema）、model path、source hash/path provenance、atomic failure、rerun replace 不重複。|
| `scene_catalog_schema_test.py` | scene-capture catalog v1 JSON Schema 正反例。|
| `GameDataInputTests.cs` | `gamedata`/`questnodes` 共用 localized input：`--strings` 與 synthetic SSE BSA 的 QUST stage-log roundtrip、metadata-driven archive refresh，以及 cache key 的 data-source 隔離。|
| `SafeOutputPathTests.cs` | package 輸出路徑 containment：合法嵌套路徑通過，`..` traversal 拒絕 |
| `PackageSafetyTests.cs` | CLI package safety gates：多 `mcmConfigs` 拒絕；required global bridge 編譯失敗時不進入 build/write |
| `Commands/ModlistSnapshotTests.cs` | 20 個測試案例：mod 啟用／優先序方向／separator／名稱；CRLF-LF 與重跑 byte-identical；既有 plugin parser 與 fail-closed；清單落差／不推論官方主檔；diff 各類變更；來源 hash／行數；JSON 與正式 schema；真 CLI process exit 0/1/2、保護輸入及失敗時保留舊輸出 |
| `VoiceEngineRegistryTests.cs` | 五個 engine 的 wired/reserved registry、Fish alias 大小寫、unknown error／reserved stderr warning 與 wrapper env var、完整 ordered TTS argv 回歸、生成前 reserved skip 與 plan/exit 3 決策、混合／空白／unresolved 計數（29 cases） |
| `VoiceReferenceLibraryTests.cs` | referenceLibrary：legacy args 逐字回歸、manifest 路徑/文字接線、emotion 優先/大小寫不敏感、最近 intensity、FormID/clip/text 確定性平手、override/intensityOverride（含零）、Neutral→固定 reference 退路、空陣列、缺檔/壞 JSON/null/無 clip fail-closed、缺省 targets、selector 不修改輸入；18 cases |
