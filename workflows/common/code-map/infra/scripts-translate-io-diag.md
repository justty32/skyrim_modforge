# CODE_MAP.infra — 工作流腳本・翻譯・Plugin I/O・Diagnostics

← [CODE_MAP.infra](../CODE_MAP.infra.md)

## 工作流腳本（`scripts/`，bash）
→ **說明文件**：[tooling](../../../tooling/README.md)（外部工具 / env var / 依賴）；CLAUDE.md「前置步驟」「出貨腳本」

| 檔案 | 職責 |
|-----|-----|
| `scripts/bootstrap-pex.sh` | fresh-clone 一鍵編譯 `assets/papyrus/*.psc`→`.pex`（embed 用，glob 全部、自動納入新增的）。需 Papyrus toolchain；任一失敗 → exit 非零 |
| `scripts/ship.sh` | 一般 mod 出貨：`package`→FLAT zip（plugin 在根，防 stale-ESP）→`$MODFORGE_SHIP_DIR`（預設 `~/skyrim_mods/mine`）。plugin 名讀自建好的 `.esp`（耐 `$ref`/`$env`）；`--clean-prefix` 清同前綴舊 zip（防 MO2 裝到 stale），否則只 warn |
| `scripts/ship-voice.sh` | 語音 mod 出貨：`package`→`voicelines`(對 packaged esp 生 `Sound/Voice/<plugin>/…`)→`voicediag`(planned vs shipped)→FLAT zip。需 `MODFORGE_TTS_BIN`，否則 abort |
| `scripts/test-offline.sh` | `dotnet test --filter "Category!=RequiresSkyrim"`（透傳額外 args）|
| `scripts/extract-skyrim-masters.sh` | 抽 vanilla master 到 `docs/reference/`（`MODFORGE_SKYRIM_MASTERS`/`MODFORGE_REFERENCE_OUT`）|

---

## 翻譯 Extract / Apply
→ **說明文件**：[for_agent_cli.md § 翻譯工作流程](../../../../docs/for_agent_cli.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Translator.cs` | 字串 extract（→ JSON source/target）+ apply（inline or Localized .STRINGS）|
| Core | `Support.cs` | UTF-8 provider（CJK localization）+ string-entry translation slot helpers |
| CLI | `Program.Translate.cs` | `extract` / `apply` / `applyloc` 命令 |
| CLI | `Program.Schema.cs` | `CheckUnknownFields`：遞迴比對 JSON key 與 C# 型別屬性，抓拼字錯誤；跳過 `_*` / `//*` 注釋慣例 |

---

## Plugin I/O
→ **說明文件**：[for_agent_cli.md § 環境需求](../../../../docs/for_agent_cli.md)（ESL 限制說明）· [engine-internals.md](../../../../docs/engine-internals.md)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `PluginIo.cs` | Mutagen load/write 包裝；ESL 2048 record 安全檢查；**masterless 防呆**：write 前掃 FormLinks，若零外部 ref（會 masterless → 遊戲靜默丟棄整個 esp，help/setstage not-found 但 MO2 顯示啟用，in-game 2026-06-20）自動補 Skyrim.esm 為唯一 master + `MastersListContent=NoCheck`（Mutagen 仍按 FormKey 正確映射 master index，FormID 不變、byte 同天生有 master 的 esp）；有外部 ref 維持 `Iterate`。測 `RelationshipAndEslTests`(masterless→補 master、有 ref→不重複) |
| Core | `SeqFile.cs` | 寫 `.seq` manifest（StartGameEnabled quest 強制啟動）|

---

## Diagnostics 基礎
→ **說明文件**：[for_agent_cli.md § dump](../../../../docs/for_agent_cli.md)（`dump` / `find` 命令）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| CLI | `Diagnostics.cs` | `dump` / `find` 命令 dispatcher（`find` 型別經反射 `I<Type>Getter`，含短別名 `idle`→`IdleAnimation` 供 PlayIdle 探查）|
| CLI | `Diagnostics.Dump.cs` | 全 record 列舉（name/editorId/key）|
| CLI | `Diagnostics.Dump.More.cs` | 擴充 dump（icons/flags/nested sub-records）|
| CLI | `Diagnostics.Identity.cs` | `identitydiag <esp>`：從建好的 plugin 還原身份系統 wiring——讀 controller VMAD 還原 faction↔code registry、default-grant quest（factions/grants/perks）、**auto-grant trigger（faction ← GetActorValue(av) >= threshold）**、acquire books（MFIdentityBook props：faction/grant/perk/scene/toggle）、兩個控制 GLOB（純讀 plugin record/VMAD，無需 spec/CK）|
| CLI | `Diagnostics.Voice.cs` | 兩個 `voicediag`：spec-aware `VoiceDiagCmd(spec, esp)`（從 `Program.Build.Voice.cs` 搬來）重用 `PrintVoicePlan`，額外列出 reserved template 數與受影響的非空白 line target 數，有 reserved-skipped 或 speaker 未解析即 exit 3/1；純 plugin-only `VoiceDiag(esp)` 保留原樣——走訪所有 dialogue INFO，印出每個 response 期望的 `.fuz` 路徑（`Sound/Voice/<plugin>/<voiceType>/<quest>_<topic>_<formId>_<n>.fuz`）與 speaker/voiceType（reuse Core 的 `ResolveVoiceSpeakers`；無需 spec/Skyrim.esm/TTS，也不含 reserved 資訊）。Tests: `VoiceEngineRegistryTests.cs`（reserved plan 決策），既有 `VoiceSpeakerTests.cs`（speaker/plan）|
| CLI | `Diagnostics.Records.cs` | targeted 單記錄 diag（lazy overlay，不 materialize 250MB master）：`cellblk`/`mgefdiag`/`lightdiag`/`refpos`/`packagediag` 等 |
| CLI | `Diagnostics.CellRefs.cs` | **`cellrefs <esp> <0xFORMID>`**：dump 單一 interior cell 的所有 placed REFR/ACHR（base FormKey + cell-local pos + rotation **RADIANS** + scale）成 CSV——逆向 vanilla cell 成 `placements[]`。記憶體安全：lazily 走 CELL block tree，命中 target FormID 後只處理那顆 cell 的 child group（Temporary+Persistent，數百 ref）就 return，絕不列舉所有 cell 的 children。rotation 是 esm 原生 radian，轉成 ModForge spec 的 degree 需 `*180/pi`。範例見 `docs/investigation/decode/sleeping-giant-inn-reverse-2026-06-13.md` + `examples/sleeping_giant_inn.json`。|
| CLI | `Diagnostics.GameData.cs` | **`gamedata <plugin> <outDir> [--strings <dir>]`**：streamed overlay 一趟 major-record pass，把 books/dialogue/quests/npcs/items/locations/magic 批次匯出成資料夾（給 agent 當參考；不 full-materialize、不 `.ToList` record group，跑得動 250MB master）。|
| CLI | `Diagnostics.GameData.Input.cs` | `gamedata` / `questnodes` 共用 localized plugin reader；`ProvisionEnglishStringsAnyBsa` 從任一 BSA 抽 English STRINGS，cache key 依 data source 與 BSA path/size/mtime 分流。|
| CLI | `Diagnostics.BookText.cs` | **`booktext <esm> <0xFORMID>`**：印一本 BOOK 的 localized Name + 全文 BookText（lore prose）；`ProvisionEnglishStrings` 從 master BSA 抽 English STRINGS |

各領域 Diagnostics 見對應子 index。

---

## Demo & 測試輔助

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Demo.cs` | 手建 demo plugin（sanity check 用，`gen` 命令呼叫）|
