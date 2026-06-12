# 語音克隆 → Skyrim SE `.fuz` 語音對話管線

← 索引：[README.md](README.md) · 相關：[IDEAS.md §1](../IDEAS.md)（語音前置需求）、memory `voice-gen-interface-future`

**研究日期：** 2026-06-08。目標：Manjaro Linux 上的 Skyrim SE、Proton 下的 MO2、有 CUDA GPU、Wine/Proton 可用，**僅供個人單人遊戲使用**。

**信心度說明：** 檔案格式事實（§1、§5、§6）以及 FaceFXWrapper / xVASynth 工具事實（§3、§4）都有充分佐證；`.fuz` 佈局已從原始碼確認。最大的*不確定性*是 **Linux-native 唇形（lip）生成** — FaceFXWrapper 是 Windows-only，而 `.lip` 步驟正是管線撞牆之處（§4）。外推之處會在內文標註。

**狀態更新 2026-06-12：** ModForge 核心端已結構性落地。現有程式碼已有
`voiceTemplates[]`、`npcs[].voiceTemplate`、`voiceLine`、`voicediag`、`voicelines --plan`、本機
TTS wrapper 契約、Wine `xWMAEncode.exe` path conversion，以及 native `.fuz` writer。fake TTS +
真 xWMAEncode 已在本機產出 `.fuz`。真模型設定、lip 生成、QA、Skyrim/Proton 實機播放仍待處理。

---

## 1. `.fuz` 格式與 Skyrim 每句語音行所需的內容

**`.fuz` 是一個容器：`.xwm` 音訊 + 一個選用的 `.lip` 唇形同步 blob。** 已驗證的二進位佈局（讀自 suglasp 的 `convert_fuz_to_xwm.ps1`，它在程式碼中解析 fuz — 具權威性且可重新實作）：

| Offset | Bytes | Field |
|--------|-------|-------|
| 0 | 4 | Magic `FUZE`（ASCII） |
| 4 | 4 | Version / unknown |
| 8 | 4 | `FuzLipSize` — uint32，lip 區段的大小 |
| 12 | `FuzLipSize` | `.lip` 資料（若 size == 0 則省略） |
| 12 + FuzLipSize | rest | `.xwm` 音訊串流 |

所以 `xwmDataLen = fileLength − 12 − FuzLipSize`。**若 `FuzLipSize == 0`，xWMA 資料就緊接在 12-byte header 之後** — 一個無 lip 的 `.fuz` 可輕易構造（12-byte header + 原始 xwm）。對 MVP 最重要的事實：你可以在*沒有唇形同步*的情況下交付語音對話，只要寫入 12 bytes + xwm。

**磁碟路徑慣例：** `Data/Sound/Voice/<PluginName.esp>/<VoiceType>/<filename>.fuz`。第一層子資料夾 = **plugin 檔名完全一致**（例如 `MyMod.esp`）；第二層 = **voice type EditorID**（例如 `MaleNord`）。

**檔名慣例**（已確認）：`(Quest)_(Topic)_(HexBaseID)_(LineNumber)` — 例如 `MyQuest_MyTopic_000113C9_1.fuz`。它編碼了父 quest 的 EditorID、topic/INFO 上下文、**INFO/response 記錄的 8 位十六進位 FormID**，以及 INFO 內**從 1 起算的 response index**。**CK 會自動產生這些名稱且無法更改** — 音訊檔案必須完全相符，否則引擎不會播放它。

**對 ModForge 的意涵：** 因為 ModForge *本身就是*透過 Mutagen 發出 QUST/DIAL/INFO 記錄的生成器，它已經知道 quest EditorID、它指派的 INFO FormID 以及 response index — 所以**它能在不依賴 Creation Kit 的情況下，確定性地計算出精確的目標檔名**。手動工作流程中最困難的部分（CK 比對的檔名）對你來說是免費的。

*不確定性：* quest/topic 字串片段的精確格式（截斷、大小寫、空白 topic 行為）並未經位元組驗證。安全做法：**先抽取幾個原版 `.fuz` 檔名並逆向工程出精確規則**，再去信任 ModForge 的名稱生成。

---

## 2. 來源素材抽取（原版 + mod 隨從語音作為訓練資料）

原版語音行本身就是 `.fuz`，打包在 BSA 內（`Skyrim - Voices_en0.bsa` 等）。

**BSA 抽取：**
- **B.A.E. — Bethesda Archive Extractor**（Nexus SSE #974）— 抽取 `.bsa`/`.ba2`。Windows .NET GUI；**可在 Wine 下執行**。（archive.org 有封存副本。）
- **較偏 Linux 的替代方案：** **BSArch**（xEdit 工具集）CLI，在 Wine 下執行；也存在純 Python 的 BSA 讀取器。（`b2a` 無法確認為現行工具 — 視為未驗證。）

**依 voice type 定位語音行：** **Lazy Voice Finder**（Nexus SSE #8619）列出原版/mod 語音檔案，可依**文字或 voice type** 搜尋，能播放/抽取它們，並**自動將 FUZ/XWM/MP3/OGG → WAV 並抽出 `.lip`**，*無需*事先解包 BSA 或 fuz。這是為單一 voice type 組裝訓練集最快的方法。Windows GUI → Wine。

**批次 `.fuz` → `.wav`：**
- **Yakitori Audio Converter**（Nexus SSE #17765）— fuz↔xwm↔wav；**內含 `xWMAEncode.exe`**；搭配 ffmpeg 可涵蓋更多格式。GUI，可 Wine。
- **fuz_extractor** + **xWMAEncode** — 經典的兩步驟。
- **suglasp 的 PowerShell 腳本** — 在程式碼中原生讀取 fuz，無需第三方工具；可在 Linux 上以 **PowerShell Core（`pwsh`）原生執行**，且夠小，可在 ModForge 內以 C# 重新實作。**BmlFuzDecode** / **unfuzer** 也能批次解碼（Windows）。

**Mod 隨從語音：** 機制相同 — 該隨從要嘛以散落形式提供 `Data/Sound/Voice/<Plugin>/<VoiceType>/*.fuz`（直接複製），要嘛提供 BSA（先用 BAE 解包），然後批次解碼為 WAV。一個帶有**自訂 voice type** 的自訂隨從是理想的訓練資料。

**Linux 路徑：** 所有解碼皆可透過 `pwsh`（原生、fuz 切分）+ ffmpeg（xwm→wav）完成，或在 Wine 下使用 BAE/Yakitori/LazyVoiceFinder。不是阻礙。

---

## 3. 語音克隆 / TTS 引擎（2026 技術現況）

請區分 **TTS**（文字→語音，這是你需要的，因為 ModForge 產出的是*文字*）與 **VC**（RVC：音訊→音訊，需要來源音訊）。

| Engine | 能否從幾分鐘克隆？ | Linux/GPU | TTS vs VC | 角色語音品質 | Notes |
|--------|--------------------|-----------|-----------|------------------------|-------|
| **xVASynth v3 / SKVA Synth**（Nexus #44184；GH DanRuta/xVA-Synth，GPL-3.0） | 否（每角色預訓練；新語音需 xVATrainer + dataset） | Backend = **Python + ffmpeg，CUDA 開關**；Electron 前端是 Windows，但 **Python backend 可在 Linux 執行** | TTS（+VC mode） | 專為遊戲語音打造；逐字 pitch/duration/energy/emotion | **Skyrim 原生選擇。** |
| **xVATrainer** | Fine-tune FastPitch1.1 + HiFi-GAN；dataset = **22050 Hz mono WAV ≤~10 s + 逐字稿** | Python, GPU | training | — | 你用抽取的原版音訊*製作*新 xVASynth 語音的方法。需要的不只「幾分鐘」。 |
| **RVC** | ~10 分鐘–1 小時乾淨音訊 | **Linux + CUDA**，成熟 | **僅 VC**（需要來源音訊） | 音色轉換極佳 | **克隆主力。** 模式：先 TTS，再用 RVC 重新上色。 |
| **XTTS v2（Coqui）** | **可 — zero/few-shot，~6–30 s 參考** | Linux + GPU（~2–3 GB） | 帶克隆的 TTS | 良好、多語言 | 最簡單的一步式 文字→克隆語音。公司已關閉，weights/forks 仍在。 |
| **F5-TTS** | **可 — zero-shot 短參考** | Linux + GPU | TTS（DiT） | 高、自然 | 強力的現代選項，乾淨的 Python。 |
| **GPT-SoVITS**（GH RVC-Boss） | **可 — 「1 分鐘」few-shot**，亦支援 zero-shot 音色 | Linux + CUDA | TTS（+ASR/分段工具） | 相似度非常高 | **每分鐘相似度最佳；** 內含 dataset 準備工具。 |
| **StyleTTS2** | Few-shot | Linux + GPU | TTS | 自然度高 | 設定比 F5/XTTS 更挑剔。 |
| **Bark** | 不太行 | Linux + GPU | TTS | 表現力強但不穩定 | **不推薦**（非確定性）。 |
| **Piper** | 否（固定語音） | **Linux-native，CPU 快** | TTS | 機械感 | 用來驗證管線接線的良好**佔位/MVP**。 |
| **ElevenLabs** | 可，極佳 | Cloud（HTTP） | TTS+克隆 | 同類最佳 | 雲端、無需 GPU；有成本+ToS，個人使用沒問題。**後備方案。** |

**建議：**
- **首選：GPT-SoVITS**（或 **F5-TTS**），在 Linux/CUDA 上一步完成 文字→克隆語音，以抽取的原版 voice-type WAV 作條件。
- **Skyrim 正統路徑：xVASynth v3**（Linux 上的 Python backend），當你想要一個本身*就是*某個已知角色的模型時，透過 xVATrainer 微調。
- **最高保真度：TTS → RVC** 兩階段。這是 Skyrim 社群收斂到的做法。
- **後備：** ElevenLabs（雲端）或 Piper（即時佔位）。

*不確定性：* 沒有 2026 年的來源確認過有文件化的 headless-Linux xVASynth 配方；架構上可執行，但你可能得直接驅動 Python backend。GPT-SoVITS/F5/XTTS 是較安全的 Linux-native 賭注。

---

## 4. `.lip` 唇形同步問題 — **這就是那道牆**

**生成器：** **FaceFXWrapper**（GH Nukem9/FaceFXWrapper）。CLI：
```
FaceFXWrapper Skyrim USEnglish FonixData.cdf in.wav resampled.wav out.lip "the spoken text"
```
預期（若已預先重採樣）**16 kHz、16-bit、mono WAV**，並**需要 `FonixData.cdf`**（不隨附 — 從 CK 的 `Data/Sound/Voice/Processing/FonixData.cdf` 複製，Nexus 上也有鏡像）。

**Linux/headless 狀態 — 阻礙：** FaceFXWrapper 是 **Windows-only**（VS solution，透過 MemoryModule 在記憶體中載入 CK DLL，沒有 CMake/Wine 測試的文件）。最佳賭注：**在 Wine 下執行** — 一個做純運算的小型 console exe，*很有機會*能運作，但**未經確認**；必須測試。捆綁它並自動打包 fuz 的高階封裝：**xVASynth `.lip`/`.fuz` plugin**（Nexus #55605）與 **Runalip**（Nexus #98931，從 `.wav`/`.fuz` 批次產生 `.lip`）。同樣有 Wine 的注意事項。

**缺失/通用的 `.lip`：** 一個 `FuzLipSize == 0` 的 `.fuz`（或一個散落的 `.wav`）**音訊照常播放 — 只是嘴巴不動。** 完全可聽/可用；唇形同步只是表面裝飾。這就是為什麼 MVP 可以完全延後處理 lip。（若透過 CK 生成 lip，另見 Nexus #40971「SSE CK Fonixdata Lip Sync Fix」。）

**結論：** 唇形同步是唯一沒有乾淨 Linux-native 工具的步驟。Plan A = 在 Wine 下用 FaceFXWrapper/Runalip（需驗證）；Plan B = 跳過 lip（zero-size，嘴巴靜止）；Plan C = 在一台 Windows/CK 機器上生成一次 lip 並重複使用。

---

## 5. `.xwm` 編碼

- **`xWMAEncode.exe`**（DirectX SDK / CK tools）— 規範的 `.wav`↔`.xwm`。小型 console exe → **在 Wine 下可靠執行**，且**內含於 Yakitori Audio Converter**。
- **ffmpeg + xWMA：** 能*解碼*，但其*編碼器*對於 Bethesda 有效的 xWMA 並不可靠/不存在。**不要依賴 ffmpeg 產出遊戲內可用的 xwm。** 使用 xWMAEncode（Wine）。
- **純 `.wav` 在遊戲中可用嗎？** **可以** — WAV/XWM/FUZ 都能作為語音檔案播放。放在正確路徑的散落 `.wav` 可運作（無 lip → 嘴巴不動），只是在磁碟上較大。**MVP 捷徑：跳過 xwm 編碼與 fuz 打包 — 直接把 WAV 放到正確路徑。** 使用 PCM 16-bit（44.1 kHz/16-bit mono 是安全的假設）。

---

## 6. 組裝 `.fuz`

- **工具：** Yakitori（Wine，內含 xWMAEncode）、Unfuzer、BmlFuzEncoder/Decode、xVASynth fuz plugin、Runalip。全為 Windows → Wine。
- **可在 Linux 上腳本化嗎？可以 — 完全跳過這些工具。** 鑑於已驗證的佈局，打包非常簡單：`"FUZE"` + 4 個 version bytes + uint32 lip size + （lip bytes）+ xwm bytes。**ModForge 可用 C# 原生發出 `.fuz`，約 20 行** — 就像它透過 Mutagen 處理二進位記錄一樣。無 lip 的 MVP：`FUZE` + version + `0x00000000` + xwm。這移除了對 Wine fuz 工具的依賴。

---

## 7. 提議的端到端管線

從 **「ModForge 為 NPC X（voiceType Y）發出了 N 句對話行」** 開始：

1. **（自動）** ModForge 每筆 INFO 都知道：quest EditorID、INFO FormID、response index、行的**文字**、NPC 的 **voiceType** → 計算目標檔名 + 路徑。*（先對照一次原版抽取以驗證精確的名稱規則。）*
2. **（一次性、半手動）** 為 **voiceType Y** 建立一個**語音模型**：抽取原版（或隨從）音訊（BAE/Lazy Voice Finder → WAV）、準備 dataset、訓練/條件化 GPT-SoVITS（或微調 xVASynth，或註冊一段 XTTS/F5 參考片段）。**可跨該語音未來所有行重複使用。**
3. **（自動、GPU）** 每行：TTS 文字 → `line.wav`。（選用的 RVC 第二輪。）
4. **（自動）** 正規化（mono、16-bit；為 lip 重採樣複製一份成 16 kHz）。
5. **（自動、Wine）** 透過 xWMAEncode 將 `.wav`→`.xwm`。*（MVP：跳過 — 保留 WAV。）*
6. **（半自動 — 那道牆）** 在 Wine 下透過 FaceFXWrapper/Runalip 產生 `.lip`。*（MVP：跳過 — zero lip。）*
7. **（自動、原生 C#）** 將 `.xwm`（+選用 `.lip`）打包成 `.fuz`。*（MVP：zero-lip fuz，或放置 WAV。）*
8. **（自動）** 放到步驟 1 的路徑。Voice files 是 loose assets，不嵌入 plugin：
   可以對最終 mod folder 內的 plugin 直接跑 `voicelines`，或把已生成的 staging directory
   交給 `package --assets <dir>`。

**完全可自動化：** 1、3、4、5、7、8。**一次性人工：** 2（建立語音模型）。**那道牆：** 6（`.lip`，依賴 Wine，可跳過）。今天在 Linux 上，一條端到端*可聽*的管線是完全可自動化的；*唇形同步*是唯一的權變部分。

---

## 8. ModForge 整合點

對齊既有的 `sounds[]`（SNDR + 複製 wav/xwm）、`package`（複製資源目錄）、shell-out（Papyrus compiler、xLODGen）：

- **Spec 概念 `voiceTemplate`**（每 voiceType 或每角色）：目前已實作的 engine 包含 `f5` 與透過本機 `voicegen.py` 契約轉呼的 `fish-s2`；`chatterbox`/`gptsovits`/`xtts` 仍是可接受/保留名稱，等各自 wrapper 補上。
- **`NpcSpec.voiceTemplate`**（或全域 map `voiceType → voiceTemplate`），讓每個 NPC 的行路由到正確的模型。
- **INFO 行已帶有文字** — 不需新增字串欄位；選用的 `voiceLine: { skipLip, format: "wav"|"xwm"|"fuz" }`。
- **新增 CLI 步驟 `voicelines`**（與 `compile`/`package` 並列）：走訪發出的 INFO 記錄、計算檔名（確定性 — ModForge 擁有那些 FormID，*相對於 CK 的最大優勢*）、shell out 到 TTS 引擎（venv 二進位路徑，類似 `~/tools/papyrus-compiler`），然後**透過既有 Wine 接線在 Wine 下**執行 xWMAEncode/FaceFXWrapper，再用一個**原生 C# fuz writer**（新檔 `Generator.Build.Voice.cs`）打包 `.fuz`，把檔案放到 `Data/Sound/Voice/<plugin>/<voicetype>/` 下。
- **`package`** 會在 `--assets` 或 `spec.assets` 提供 `Sound/...` 時複製它；不會自動發現另一個 build directory 裡的 voice output。無 `.seq` 互動。
- **工具設定：** `MODFORGE_TTS_BIN`、`MODFORGE_FACEFX`（+ `FonixData.cdf`）、`MODFORGE_XWMAENCODE`。xWMA/lip 工具缺時降級到 `.wav` 或 no lip；TTS 缺時不能生成，但 `voicediag` 與 `voicelines --plan` 仍可用。

---

## 9. 風險、坑、MVP

**風險 / 坑：**
- **Linux 上的 `.lip` 未經驗證** — 別把功能押在它上面。
- **檔名規則必須經實證釘死** — 一個字元不符 = 靜默無音訊。抽取 2–3 個原版 `.fuz` 名稱，確認 ModForge 完全重現各片段。
- **ffmpeg ≠ 有效 xWMA** — 用 xWMAEncode（Wine）或交付 WAV。
- **FonixData.cdf** — 從你自己的 CK 複製，不要再散布。
- **xVASynth headless-on-Linux** 未經確認；偏好 GPT-SoVITS/F5/XTTS。
- **語音一致性** — few-shot 克隆會飄移；預留一個正規化/QA 流程；RVC-on-top 可穩定音色。
- **MO2 重新安裝會還原手動修補的檔案**（memory `mo2-reinstall-reverts-manual-pex`）— 永遠重新建置進 zip。

**MVP — 最小的驗證切片：**
1. 一個既有的測試 NPC、原版 voiceType（例如 `MaleNord`）、3 句 ModForge 已能發出的短行。
2. 抽取 ~10–20 句 `MaleNord` 原版行（Lazy Voice Finder → WAV）作為克隆參考。
3. 用 GPT-SoVITS（或 XTTS v2）克隆 → 3 個 WAV。
4. **跳過 xwm、跳過 lip：** 把 3 個純 `.wav` 放到 `Data/Sound/Voice/<plugin>.esp/MaleNord/<完全比照 CK 風格的名稱>.wav`，捆綁進扁平 zip。
5. 遊戲中（手動 MO2/Proton）：確認音訊播放且有字幕、嘴巴靜止。**證明了 檔名對應 + 克隆品質 + 打包 — 整條脊椎 — 且零 Wine 依賴。**
6. **然後加入 fuz：** 把 WAV 換成原生 C# 寫出的 zero-lip `.fuz`。
7. **然後加入 lip：** 讓 FaceFXWrapper/Runalip 在 Wine 下運作。這是唯一可能逼你退回 Windows/CK 的步驟。

**法律：** 僅供個人、單人、不再散布。不要發布克隆語音資源（語音演員/Bethesda 權利；`FonixData.cdf` 是 Bethesda 財產）。把生成的資源保留在本機。

---

### 已驗證的關鍵來源
- `.fuz` 佈局：suglasp `convert_fuz_to_xwm.ps1`；Fallout Wiki FUZ File。
- 檔名/路徑/WAV 可用：CK Wiki「How to generate voice files by batch」；Beyond Skyrim「Voice Line Implementation」。
- Lip：GH Nukem9/FaceFXWrapper；Runalip（Nexus #98931）；xVASynth lip/fuz plugin（#55605）；CK Fonixdata Lip Sync Fix（#40971）。
- xwm/fuz：Yakitori（#17765）、recursive wav→xwm（#16763）、Xwm Ninja。
- 抽取：BAE（#974）、Lazy Voice Finder（#8619）。
- TTS/VC：GH DanRuta/xVA-Synth + xva-trainer；xVASynth v3（#44184）；GH RVC-Boss/GPT-SoVITS；F5-TTS、XTTS v2/Coqui、RVC；Mantella docs（Piper/xVASynth/XTTS）。
