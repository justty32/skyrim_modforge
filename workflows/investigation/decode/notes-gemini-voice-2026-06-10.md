# Gemini session 盤點（2026-06-10）

Gemini 留下兩塊：一個已 commit 的雜項 bugfix，加一大包未 commit 的 voice cloning 管線。
本檔是 Claude 審視後的快照；處理完可刪。

## 已 commit：`2d8ab96`「fix(generator): resolve multiple bugs」

六項小修（dialogue / armor / magic / vendor / placement / validate）。其中
Hello topic EditorID 改成 `{npc}_{quest}_Hello` 防撞名，**但沒更新測試**：

- ❌ `VendorTests.Shopkeeper_IsConversable_HasHello` FAIL —— 仍預期舊名
  `MF_Shopkeeper_Hello`，實際是 `MF_Shopkeeper_MF_Shopkeeper_GreetQuest_Hello`。
- 428/429 通過；這是 master 上唯一失敗（這台機器 WordWall 有過）。

## 未 commit：voice cloning 管線（TTS → .fuz）

即 IDEAS 裡的 voice-gen interface。build 過、`VoiceTests` 3 個都過。

### Spec / Schema
- `voiceTemplates[]`：`engine`（f5|chatterbox|gptsovits|xtts，只有 f5 有實作）、
  `referenceWav` / `referenceText`（zero-shot reference）、`modelPath`（微調模型）、
  `rvcModel`、`seed`、`speed`、`exaggeration`、`language`。
- `NpcSpec.voiceTemplate` → 指到 template id。
- 全域 `voiceLine`：`format`（fuz|wav|xwm）、`skipLip`。
- `spec.schema.json` 補了對應 defs（JSON 可 parse，但新增段縮排亂掉，要重排）。

### Core 新檔
| 檔案 | 職責 |
|---|---|
| `Voice.cs` | 呼外部 TTS（`MODFORGE_TTS_BIN`）；xWMAEncode、FaceFXWrapper 走 Wine（`MODFORGE_XWMAENCODE` / `MODFORGE_FACEFX` / `MODFORGE_FONIXDATA`） |
| `Fuz.cs` | 拆 .fuz（lip + audio） |
| `Generator.Build.Voice.cs` | `WriteFuz` + CK 命名 `quest10_topic15_formid8_n.fuz` |
| `Archives.cs` | Mutagen 讀 BSA（extract / list） |
| `Generator.Validate.Voice.cs` | template id / engine / format 驗證，已掛進 Validate |

`ModForge.Core.csproj` 多吃完整 `Mutagen.Bethesda` 套件（Archives 需要）。

### CLI 新指令
- `voicelines <spec> <esp>`：走訪建好 esp 的 INFO，用 GetIsID 條件找 speaker，
  生 WAV→xwm→fuz 到 `Sound/Voice/<plugin>/<voiceType>/`。已存在的檔 skip（無 hash cache）。
- `extract-voices <bsa> <voiceType> <outDir>`：抽 vanilla .fuz → ffmpeg 轉 wav（做 reference clip）。

### 其他改動
- dialogue / banter / scene 的 INFO 全補上 EditorID（voice 檔名用）。
- repo 根目錄實驗產物（未 ignore）：`voicegen.py`（f5 wrapper）、`voicegen.sh`（venv_voice）、
  `make_list.py` + `training_list.txt` + `transcripts/` + `voice-clones/`
  （FemaleYoungEager 微調資料集）、`test_out*.wav`、`young_test.json`、根目錄誤生的 `Seq/`。
  看起來實際跑過一輪 FemaleYoungEager clone 實驗。

## 待處理問題（皆未動手）

1. **master 測試壞掉**：上述 Vendor 測試（commit 進去的，非 working tree 的鍋）。
2. **死欄位**：`VoiceTemplateSpec.Speed/Exaggeration/Language` 有定義、voicegen.py 也吃
   `--speed`，但 `Voice.GenerateWav` 沒傳這三個參數——spec 填了被靜默忽略。
   schema 只列 `exaggeration` 沒列 `speed`，兩邊不一致。
3. **speaker 偵測只認 GetIsID**：banter/scene 用 alias / faction 條件 gate 的 INFO
   會被靜默 skip，不生語音。
4. xwm 編碼失敗且 format=fuz 時會把**裸 WAV 包進 .fuz**——引擎吃不吃未驗證。
5. 根目錄垃圾沒進 `.gitignore`；CODE_MAP / SPEC 文檔完全未同步（迭代期可接受）。

## 處理狀態（2026-06-11）

上列五項本 session 全數處理：

- ✅ **#1 Vendor 測試**：`Shopkeeper_IsConversable_HasHello` 對齊新 Hello EditorID 命名。
- ✅ **#2 死欄位**：`speed` / `exaggeration` / `language` 接通傳進 TTS 呼叫；schema 兩邊補齊。
- ✅ **#3 speaker 偵測**：GetIsID 之外加 alias / faction 條件解析；解不出 speaker 的 INFO 改為 loud warning（不再靜默 skip）。新測試 `VoiceSpeakerTests.cs`。
- ✅ **#4 fuz fallback**：xwm 編碼失敗且 format=fuz 時不再把裸 WAV 包進 .fuz，改寫 loose `.wav` + warning。
- ✅ **#5a gitignore**：根目錄實驗產物清理 / ignore。
- ✅ **#5b 文檔同步**：`CODE_MAP.infra.md`（語音克隆段 + Tests）、`CODE_MAP.dialogue-quests.md`（INFO EditorID 一行註）、`SPEC-workflow.md § Voice`（欄位/env/CLI 工作流/檔名規則/失敗行為）、`SPEC-index.md`、`IDEAS.md`（語音前提改標已落地）。
