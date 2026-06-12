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
| `VoiceTests.cs` | `VoiceFileName` CK 命名格式、`WriteFuz` header（含無 lip 情形）|
| `VoiceSpeakerTests.cs` | `voicelines` speaker 偵測（GetIsID / alias / faction 條件解析）|
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
| CLI | `Program.Build.cs` | `build` / `validate` / `package` / `compile` / `voicelines` / `extract-voices` |
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

## 語音克隆（TTS → .fuz）
→ **說明文件**：[SPEC-workflow.md § Voice](SPEC-workflow.md#voice-tts-voice-cloning--fuz)

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Spec | `Spec.Voice.cs` | `VoiceTemplateSpec`（`engine` f5\|fish-s2\|chatterbox\|gptsovits\|xtts；`fish`/`fishspeech`/`fish-speech` 為 Fish S2 alias；`referenceWav`/`referenceText` zero-shot reference、`modelPath` 微調模型、`rvcModel`、`seed`、`speed`、`exaggeration`、`language`）+ `VoiceLineSpec` 全域輸出設定（`format` fuz\|wav\|xwm、`skipLip`）；`NpcSpec.voiceTemplate`（→ template id）在 `Spec.Actors.cs` |
| Core | `Voice.cs` | 呼外部 TTS（`MODFORGE_TTS_BIN`；engine/ref/model/seed/**speed/exaggeration/language** 全數傳給 TTS process）；`voicegen.py` 的 Fish S2 分支再呼 `MODFORGE_FISH_SPEECH_BIN`；xWMAEncode（`MODFORGE_XWMAENCODE`）與 FaceFXWrapper lip 生成（`MODFORGE_FACEFX` + `MODFORGE_FONIXDATA`）走 Wine |
| Core | `Fuz.cs` | `.fuz` 容器拆解（FUZE header → lip + audio；audio ext 自動偵測 xwm/wav）|
| Core | `Generator.Build.Voice.cs` | `WriteFuz`（lip + audio 打包成 .fuz）+ `VoiceFileName` CK 命名（`quest10_topic15_formid8_n.fuz`：quest EditorID 前 10 字 + topic EditorID 前 15 字 + INFO FormID hex8 + response 序號）|
| Core | `Generator.Build.Voice.Speakers.cs` | `ResolveVoiceSpeakers`：從建好 esp 的 INFO 條件解 speaker（GetIsID / GetIsAliasRef / GetInFaction / scene Dialog action）→ `VoiceSpeaker`(Npc + voiceType)；一個 INFO 可對多 speaker（faction），`SelectVoiceTargets` 去重成每個 distinct voiceType 一份。解不出 → `VoiceSpeakerResolution.Reason`（CLI 必須大聲報）。在 Core 故可對 in-memory built mod 單測 |
| Core | `Archives.cs` | Mutagen 讀 BSA/BA2（extract + path filter；`extract-voices` 用）|
| Validate | `Generator.Validate.Voice.cs` | template id 非空 / engine 枚舉 / `npc.voiceTemplate` ref 存在 / `voiceLine.format` 枚舉；已掛進 `Validate` |
| CLI | `Program.Build.Voice.cs` | `voicelines <spec> <esp>`：走訪建好 esp 的 INFO → 從條件找 speaker（GetIsID + **alias / faction 條件**；解不出 speaker → **loud warning**，不靜默 skip）→ WAV→xwm→fuz 寫到 `Sound/Voice/<plugin>/<voiceType>/`（已存在的檔 skip，無 hash cache）；**xwm 編碼失敗且 format=fuz → 改寫 loose `.wav` + warning（不把裸 WAV 包進 .fuz）**。`extract-voices <bsa> <voiceType> <outDir>`：抽 vanilla .fuz → ffmpeg 轉 wav（做 reference clip）|

環境變數：`MODFORGE_TTS_BIN`（TTS wrapper，必要）、`MODFORGE_FISH_SPEECH_BIN`（僅 Fish S2 engine 需要）、`MODFORGE_XWMAENCODE`、`MODFORGE_FACEFX`、`MODFORGE_FONIXDATA`（後三者 Wine 路徑，缺則退化：無 xwm / 無 lip）。

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
| CLI | `Diagnostics.Identity.cs` | `identitydiag <esp>`：從建好的 plugin 還原身份系統 wiring——讀 controller VMAD 還原 faction↔code registry、default-grant quest（factions/grants/perks）、**auto-grant trigger（faction ← GetActorValue(av) >= threshold）**、acquire books（MFIdentityBook props：faction/grant/perk/scene/toggle）、兩個控制 GLOB（純讀 plugin record/VMAD，無需 spec/CK）|

各領域 Diagnostics 見對應子 index。

---

## Demo & 測試輔助

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| Core | `Demo.cs` | 手建 demo plugin（sanity check 用，`gen` 命令呼叫）|
