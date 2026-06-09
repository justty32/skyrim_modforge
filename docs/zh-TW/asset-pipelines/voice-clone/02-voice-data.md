# 02 — 語音資料（抽取 vanilla 音檔、建 reference/資料集）

← [README](README.md) · 上一份：[01-engine-setup.md](01-engine-setup.md) · 下一份：[03-lip-and-audio-encoding.md](03-lip-and-audio-encoding.md)

沒有的聲音克隆不來。本檔帶你從「硬碟上的 Skyrim BSA」到「一個乾淨 reference clip（零訓練）或乾淨資料集（GPT-SoVITS）」，針對一個選定的 voiceType。全部步驟 Linux 可做（native 或 Wine）。

---

## 1. 選目標語音

- **Vanilla voiceType**（如 `MaleNord`、`FemaleEvenToned`、`MaleEvenToned`）—— 訓練資料已在 Voices BSA 裡，是 MVP 目標。NPC 的 `voiceType` EditorID 同時也是 `.fuz` 路徑的**第二段**（[04]），所以早點選就釘死輸出路徑。
- **自訂 follower voiceType** —— 有自己語音類型的 follower mod 是理想素材：乾淨、一致、單一講者、常為鬆散檔。適合做有辨識度的 NPC。

MVP 先用 `MaleNord`（行數多、好判斷品質）。

---

## 2. 在 Linux 抽取音檔

Vanilla 語音行是 `.fuz`，打包在 `Skyrim - Voices_en0.bsa`（與同伴）裡。兩條路：

**路線 A — Lazy Voice Finder（組集最快）。** Nexus SSE #8619。可依**文字或語音類型**搜尋 vanilla/mod 語音檔，播放/抽取，並**自動把 FUZ/XWM/MP3/OGG → WAV 並抽出 `.lip`**，*不必*先解 BSA 或 fuz。Windows GUI → 走 Wine。這是「給我 20 個乾淨 `MaleNord` 行的 WAV」最省力的路。

**路線 B — 自己解包 + 解碼（可腳本/批量）。**
1. **解 BSA：** B.A.E.（Bethesda Archive Extractor，Nexus #974，.NET GUI 走 Wine）或 BSArch（xEdit 工具組 CLI，走 Wine）。抽 `Sound/Voice/.../<VoiceType>/*.fuz`。
2. **解碼 `.fuz` → `.wav`：** fuz layout 已驗證（見 [04] / 上層 §1），所以能 native 切。選項：
   - **`pwsh`（PowerShell Core，Linux native）** 跑 suglasp 的 fuz-split 腳本 —— 在程式碼裡讀 12-byte header，免 Windows 工具。
   - **Yakitori Audio Converter**（Nexus #17765，附 `xWMAEncode.exe`）走 Wine —— fuz↔xwm↔wav GUI/批量。
   - **ffmpeg** 負責 `.xwm` → `.wav` 的解碼那半（ffmpeg *解* xWMA 沒問題；只是不能*編*合法 xWMA —— 那限制只在 [03] 咬人，這裡不會）。

任一路線都會給你一資料夾的 `MaleNord/*.wav`。抽取在 Linux **不是 blocker** —— 每塊都是 native-或-Wine。

---

## 3a. 零訓練 reference clip（F5 / Chatterbox）

你需要的*很少*：一個或數個**乾淨、單一講者**的 clip。

- **F5-TTS：** 5–15 秒 clip **+ 其中所說的逐字稿**。挑語氣中性、清楚的（避開吼叫/低語/戰鬥呻吟）。逐字稿準確度直接影響克隆品質。
- **Chatterbox：** ~10 秒+ clip、24 kHz+、無嚴格要逐字稿。同樣「乾淨、中性、單一講者」原則。

**挑選撇步：** 瀏覽抽出的行，挑 2–3 個候選 clip，各生同一句測試行，留克隆最好的那個 ref。修掉靜音與任何音樂/SFX 尾巴（Audacity 走 Wine，或 native `sox`/`ffmpeg`）。*reference* 除了去削波外不需做響度正規化。

> reference clip 品質對零訓練結果的主導程度遠大於台詞本身。花那 10 分鐘挑個好 clip。

---

## 3b. 微調資料集（GPT-SoVITS）

資料更多、更有結構、一次性：

- **數量：** GPT-SoVITS 約 ~1 分鐘就能訓，但更多乾淨音檔 → 更好。vanilla voiceType 你有大把；幾分鐘多樣乾淨的行是好目標。避開戰鬥 bark / 重疊 SFX。
- **格式期望（經典 xVATrainer 風，也是 GPT-SoVITS 安全目標）：** mono WAV、~22050 Hz、每段 ≤ ~10 秒、**附逐字稿**。GPT-SoVITS 的 WebUI 附 **切片 + ASR** 工具會切長音檔並自動轉錄 —— 所以你能餵較長的串接音檔，讓它建出 clip+逐字稿配對。檢查/清理 ASR 逐字稿；爛逐字稿會毒化訓練。
- **正規化：** 統一取樣率、mono、peak-normalize、去靜音。`sox`/`ffmpeg` 批次：
  ```bash
  for f in raw/*.wav; do
    ffmpeg -i "$f" -ac 1 -ar 22050 -af "silenceremove=1:0:-50dB,loudnorm" "clean/$(basename "$f")"
  done
  ```
  （調 filter chain；重點是 mono + 固定率 + 去靜音 + 調平。）

---

## 4. 取樣率速查（什麼用什麼率）

你會碰到好幾種取樣率；理清（細節在 [03]）：

| 用途 | 率 / 格式 |
|---------|---------------|
| 零訓練 **reference** clip | 引擎原生即可（F5 ~24 kHz、Chatterbox 24 kHz+）；乾淨且 mono |
| GPT-SoVITS **訓練** clip | 22050 Hz mono WAV + 逐字稿 |
| TTS **輸出**行 | 引擎吐什麼（如 24 kHz） |
| **遊戲內**語音 WAV/xwm | PCM 16-bit mono；44.1 kHz/16-bit mono 是安全假設 |
| **`.lip` 生成**輸入 | **16 kHz、16-bit、mono**（FaceFXWrapper 要求） |

所以每行通常產出：引擎原生 WAV → 給遊戲的 44.1 kHz/16-bit mono 副本 →（*只在*生 lip 時）16 kHz/16-bit mono 副本。（[03] 把這正式化。）

---

## 5. 資料放哪

保持整齊的本機 layout（永不 commit —— 資產依法務規則留本機）：
```
~/voice-work/
  refs/        MaleNord_ref.wav  + MaleNord_ref.txt   （零訓練）
  datasets/    MaleNord/clips/*.wav + 逐字稿           （GPT-SoVITS）
  models/      MaleNord_gptsovits/...                 （微調輸出）
  out/         每行生成的 WAV/xwm/lip/fuz
```
ModForge 之後的 `voicelines` step（[05]）透過 `voiceTemplate` spec 欄位引用 `refs/` 或 `models/`。

---

## 6.「完成」長什麼樣

- **零訓練路：** `refs/MaleNord_ref.wav`（+ `.txt`）就緒 → 直接餵進 [01] 的 `voicegen.py`。
- **保真路：** `models/MaleNord_gptsovits/` 訓練好 → `--engine gptsovits --ref models/...`。

任一都解鎖在 [06] 生成 3 句 MVP 行。

---

### 來源
抽取/格式：[CK Wiki「generate voice files by batch」](https://ck.uesp.net/wiki/How_to_generate_voice_files_by_batch)、[Beyond Skyrim Voice Line Implementation](https://wiki.beyondskyrim.org/wiki/Arcane_University:Voice_Line_Implementation)、Lazy Voice Finder（Nexus SSE #8619）、B.A.E.（Nexus #974）、Yakitori（Nexus #17765）。資料集/訓練：[RVC-Boss/GPT-SoVITS](https://github.com/RVC-Boss/GPT-SoVITS)（附切片/ASR）、[Beyond Skyrim xVASynth](https://wiki.beyondskyrim.org/wiki/Arcane_University:XVASynth)（22050 mono ≤10 秒 + 逐字稿慣例）。
