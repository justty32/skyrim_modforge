# ModForge ↔ voicegen 協議

ModForge 與語音合成工具之間的**唯一**接點。ModForge 透過 `MODFORGE_TTS_BIN` 把這支當黑盒 exec;這支只看 args、只交一個 `.wav`。互不知道對方內部。

## 呼叫

ModForge 對 `MODFORGE_TTS_BIN`(本 repo 的 `voicegen-f5.sh` → `voicegen.py`)做一次 process exec,**每條台詞一次**,參數如下(C# 端在 `Voice.BuildTtsArgs`,純函式可單測):

| arg | 必填 | 型別 | 意義 |
|-----|:--:|------|------|
| `--engine` | ✔ | string | `f5` / `fish-s2`(別名 `fish`/`fishspeech`/`fish-speech`)/ 保留:`chatterbox`/`gptsovits`/`xtts` |
| `--text` | ✔ | string | 要合成的台詞 |
| `--out` | ✔ | path | 要寫出的 `.wav` 絕對路徑 |
| `--ref-wav` | | path | zero-shot 參考嗓音 clip(由 ModForge `extract-voices` 從 vanilla BSA 抽) |
| `--ref-text` | | string | 參考 clip 的轉寫;F5 留空會自動 Whisper 轉寫 |
| `--model` | | path | 微調模型 |
| `--rvc` | | path | RVC 模型 |
| `--seed` | | int | 決定性 seed |
| `--speed` | | float | 語速 |
| `--exaggeration` | | float | 表情誇張度(engine 無對應則 note+ignore) |
| `--language` | | string | 語言碼(F5 從 ref 推、忽略) |
| `--emotion` | | string | **情緒**,來自對話 INFO 記錄,Skyrim 八種:`Neutral`/`Anger`/`Disgust`/`Fear`/`Sad`/`Happy`/`Surprise`/`Puzzled` |
| `--intensity` | | int | 情緒強度 0–100,同樣來自 INFO(`EmotionValue`) |

只有有值的 optional 才會出現(沒給就讓 engine 用預設)。`--emotion`/`--intensity` 是**從 INFO 記錄**取的(不是 voiceTemplate spec 欄位):ModForge 走訪 built plugin 的 INFO，每個 response 的 `Emotion`/`EmotionValue` 隨台詞一起交出。

## 回傳合約

- **成功**:`--out` 路徑出現一個合法 `.wav`,process **exit 0**。
- **失敗**:**exit 非 0**,診斷寫 **stderr**。ModForge 看到非 0 或 `--out` 不存在 → 該行記 TTS 失敗、跳過(不會塞半成品)。
- stdout 僅供人看,ModForge 不解析。

## 引擎對情緒/表情的處理(engine 內政)

協議**永遠**把 `--emotion`/`--intensity`/`--exaggeration`/`--language` 交出去;engine 自己決定用或不用:
- **f5**:無表情/情緒控制 → 收到就在 stderr `NOTE: ... ignoring` 後忽略(語言從 ref 推)。**不報錯**。
- **fish-s2**:原樣轉呼 `MODFORGE_FISH_SPEECH_BIN`,由該外部 wrapper 決定怎麼用;wrapper **必須**輸出 WAV。
- 未來 engine 可把 emotion/intensity 映成自己的表情參數——**那是 voice 專案的事,ModForge 不變**。

## 邊界:誰做什麼

| ModForge(包裝,留 repo 本體) | voicegen(合成,本專案) |
|---|---|
| 走訪 INFO、解 speaker→voiceType、算 plan/`voicediag` | text+emotion+ref → `.wav` |
| `.wav` → xWMA → `.fuz`、lip、擺進 `Sound/Voice/<plugin>/` | engine 選擇、venv、ref clip、model |
| 透過 `MODFORGE_TTS_BIN` 呼叫本協議 | 實作本協議 |

> 改協議(加 arg / 改回傳)= 改本檔 + ModForge 的 `Voice.BuildTtsArgs` + `voicegen.py`,三者一起。其餘都不該因為某個 engine 而動。
