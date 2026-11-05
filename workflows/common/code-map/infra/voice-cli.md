# CODE_MAP.infra — 語音克隆（Validate・CLI・環境變數）

← [CODE_MAP.infra](../CODE_MAP.infra.md)

## 語音克隆（續：Validate・CLI・環境變數）

| 層次 | 檔案 | 職責 |
|-----|-----|-----|
| CLI | `Program.Build.Voice.Extract.cs` `VoiceAnnotateCmd` | `voice-annotate <esm> <voiceType> <bsa> <outDir>`：抽 clip→WAV(Archives.Extract+Fuz+ffmpeg)+ 對每 clip 從 `<esm>` 查 INFO emotion → 寫 `voice-annotations.json`。打底確定性(讀 INFO Emotion);人聽完改 manifest。產出的 corrected manifest 由 `Voice.ReferenceLibrary.cs` 消費 |
| Validate | `Generator.Validate.Voice.cs` | template id 非空 / `npc.voiceTemplate` ref 存在 / `voiceLine.format` 枚舉；已掛進 `Validate`；`engine` 改查 registry，unknown 回傳 fatal error，reserved（`chatterbox`/`gptsovits`/`xtts`）明示 stderr WARNING（含未來 wrapper 契約），不列入 fatal errors。Tests: `VoiceEngineRegistryTests.cs` |
| CLI | `Program.Build.Voice.cs` | `voicelines <spec> <esp> [--dry-run\|--plan]`：走訪建好 esp 的 INFO → 從條件找 speaker（GetIsID + **alias / faction 條件**；解不出 speaker → **loud warning**，不靜默 skip）→ WAV→xwm→fuz 寫到 `Sound/Voice/<plugin>/<voiceType>/`。只在 matching deterministic fingerprint sidecar + metadata 列出的 artifact 的 bytes+SHA-256 都相符時 skip；舊檔、壞 sidecar 或被改動的 output 一律重建。一次 `voicelines` run 共用 source-identity memoizer，避免同 template 對每句反覆 hash。**xwm 編碼失敗且 format=fuz → 改寫 loose `.wav` + warning（不把裸 WAV 包進 .fuz），sidecar 綁定實際 wav/lip 輸出**。含 `BuildNpcVoiceTemplateMap`/`BuildNpcVoiceTypeMap`/`BuildExternalVoiceMap`(voiceSpeakers[]→FormKey)/`PrintVoicePlan`/`GenerateVoiceLine` 的生成側 helpers。**F5 踩坑：ref clip 要短（~2-3s），太長/太密 F5 會估錯時長把輸出截斷**。`GenerateVoiceLine` 在 cache/檔案/外部行程之前先查 reserved engine，reserved 的目標 return -2、獨立累計不併入 TTS failure，命令（含 `--dry-run`/`--plan`）只要有目標被跳過就 exit 3；spec-aware `voicediag <spec> <esp>` 已搬到 `Diagnostics.Voice.cs`（此檔不再含它）。Tests: `VoiceEngineRegistryTests.cs`（reserved gate 純決策），既有 `VoiceTests.cs`／`VoiceLiveContractTests.cs`（工具契約）|
| CLI | `Program.Build.Voice.Extract.cs` | `extract-voices <bsa> <voiceType> <outDir> [plugin]`：抽 .fuz → ffmpeg 轉 wav（做 reference clip）；`[plugin]` 預設 Skyrim.esm，給 `SofiaFollower.esp` 等可抽既有隨從 BSA 的嗓音 ref。`VoiceAnnotateCmd`(`voice-annotate`，見上)亦在此。純抽取/轉檔側（Archives.Extract+Fuz.Split+ffmpeg），與生成側分檔以守 300 行上限 |

環境變數：`MODFORGE_TTS_BIN`（TTS wrapper，必要）、`MODFORGE_FISH_SPEECH_BIN`（僅 Fish S2 engine 需要）、`MODFORGE_XWMAENCODE`、`MODFORGE_LIPGEN`（官方 CK LipGenerator.exe，lip 首選）、`MODFORGE_FACEFX` + `MODFORGE_FONIXDATA`（社群 FaceFXWrapper，lip 退化路徑）——皆 Wine 路徑，缺則退化：無 xwm / 無 lip（嘴不動）。`format=fuz` 且未配任何 lip 工具時 `voicelines` 開頭發一次 loud warning。
