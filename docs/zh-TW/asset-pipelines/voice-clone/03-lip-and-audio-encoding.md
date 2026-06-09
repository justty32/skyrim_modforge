# 03 — 嘴型同步 + 音訊編碼

← [README](README.md) · 上一份：[02-voice-data.md](02-voice-data.md) · 下一份：[04-fuz-and-filenames.md](04-fuz-and-filenames.md)

兩件事：(1) 選擇性讓嘴會動，(2) 選擇性把 `.wav` → `.xwm`。兩者都*選用* —— 一個鬆散 44.1 kHz/16-bit mono WAV 放對路徑，遊戲內就會播（靜止嘴）。本檔講的是加上嘴動與縮小檔案。

你的決策：**「嘴型隨便亂動就好」。** 所以目標是*任何*嘴動，不是 phoneme 準確度。這改變了下面的建議順序。

---

## 1. 音訊正規化（永遠做、便宜、native）

在 lip 或 xwm 之前，產出 canonical 遊戲 WAV 與（若做 lip）16 kHz lip 輸入副本：

```bash
# canonical 遊戲內 WAV：PCM 16-bit mono、44.1 kHz
ffmpeg -i out/line_raw.wav -ac 1 -ar 44100 -sample_fmt s16 out/line.wav
# lip 輸入副本，只在生 .lip 時：16 kHz 16-bit mono
ffmpeg -i out/line_raw.wav -ac 1 -ar 16000 -sample_fmt s16 out/line_16k.wav
```
引擎輸出常是 24 kHz；降到 44.1 kHz 是無害的上採或直通。**16 kHz mono** 副本正是 FaceFXWrapper 要的，所以現在就生，lip 步驟免重採樣。

---

## 2. `.lip` —— 分層計劃（挑能給嘴動的最低 tier）

### Tier 0 — 無 lip（基準、永遠可行、**靜止嘴**）
出 WAV，或打包 `FuzLipSize == 0` 的 `.fuz`。音訊 + 字幕完美播放；嘴不動。這是 MVP 預設與保證正確的 fallback。*不滿足「嘴會動」，但它是一切的地板。*

### Tier 1 — FaceFXWrapper / Runalip 走 Wine（**建議先試，為了嘴動**）
**若 Wine 配合**，這幾乎免費給你*正確*的嘴動。既然你不需要準確度，也就不必在意它略偏 —— 你只要它能跑起來。

```
FaceFXWrapper Skyrim USEnglish FonixData.cdf in.wav line_16k.wav out.lip "the spoken text"
```
- **參數：** `[Type] [Lang] [FonixDataPath] [WavPath] [ResampledWavPath] [LipPath] [Text]`。Type = `Skyrim`、Lang = `USEnglish`。若以已是 16 kHz/16-bit/mono 的 WAV 當 `ResampledWavPath`，它跳過自己的重採樣。
- **`FonixData.cdf`：** 工具不附 —— 從自己的 CK 安裝 `Data/Sound/Voice/Processing/FonixData.cdf` 複製（Nexus 亦有鏡像）。Bethesda 財產；留本機。
- **Wine 風險：** FaceFXWrapper 只有 Windows，用 **MemoryModule** 在記憶體載入 CK DLL —— 那是 Wine 下最可能爆的部分（無 Wine 測試文件）。**設 ~15 分鐘時限。** 跑得起來就贏了 —— 包成 shell-out 然後往下走。
- **批量替代：** **Runalip**（Nexus SSE #98931）—— console `Runalip.exe` 從 `.wav`/`.fuz` + 文字 CSV 量產 `.lip`（並選擇性 `.fuz`）。同 Wine 警告；批量更順。**xVASynth `.lip`/`.fuz` plugin**（Nexus #55605）附同能力並自動打包 fuz。

### Tier 2 — 原生 C# 寫的合成 envelope-driven `.lip`（**Wine 失敗時的後備**）
因為你只要*嘴動*，就不需要真 phoneme。計劃：算音訊振幅 envelope，產出一個 `.lip`，其 phoneme keyframe 在**張口**形（能量峰）與**閉/中性**形（谷）間交替，跨整段排布。嘴隨語速拍動，準確度無關。這 100% Linux-native（無 Wine、無 FonixData、無 CK），與 fuz writer 折進同一個 `Generator.Build.Voice.cs`。

實作前提是釘死 `.lip` byte layout（見 §3）。工夫：格式確認後幾小時。只在 Tier 1 不乖時才做。

> 關於 **Silent Voice Generator**（Nexus SSE #9124）：它為*無聲/未配音*對話產出時長相符的 `.lip` —— 即**空白** lip（無嘴動）。當成「寫出長度 N 的結構合法 `.lip`」的參考實作有用，但其輸出是靜止嘴，所以是 Tier 2 的程式碼參考，本身不是嘴動解方。

### 決策流程
```
要嘴動嗎？
  否  → Tier 0（無 lip）
  是  → 試 Tier 1（FaceFXWrapper/Runalip 走 Wine，15 分鐘時限）
          可跑 → 完成（準確嘴動，免費）
          失敗 → Tier 2（合成 envelope .lip，native C#）   [或接受 Tier 0]
```

---

## 3. `.lip` 格式 —— 已知的，與如何釘死其餘

**已捕捉事實（足以開工；尚非完整 byte map）：**
- `.lip` 是 **FaceFX** 臉部動畫 blob。FaceFX 認 **42 個 phoneme**；動畫是隨時間的 bone-pose/morph-target 曲線。
- 由 **16 kHz/16-bit/mono WAV + 所說文字**生成（FaceFXWrapper 整個工作就是 WAV+文字 → phoneme 曲線）。
- **offset/timing 慣例：** 某 phoneme 的位置碼 ≈ `timestamp_seconds × 4 × sampleRate`（sampleRate ~22050）。例如 2.13 秒的音 → `2.13 × 4 × 22050 ≈ 0x00031A38`。這是合成 lip 排 keyframe 的關鍵。
- 它放在 `.fuz` 裡當 lip section（[04]）；`FuzLipSize == 0` 代表「無 lip」。

**寫 Tier 2 前要釘死**（回家做 —— 兩來源都 403 擋自動抓取但瀏覽器開得開）：
1. 讀 **`fallout.wiki/wiki/LIP_File_Format`**（與 `falloutmods.fandom.com` 鏡像）拿確切 header/magic/version 與 keyframe record layout。
2. **Hex-diff 2–3 個抽出的 vanilla `.lip`**（Lazy Voice Finder 會抽，[02]）對照 wiki spec 確認 Skyrim-SE 細節。
3. 拿 **Runalip** / **xVASynth fuz plugin** 對某已知良好 Skyrim `.lip` 的輸出交叉驗證你的 writer round-trip。

釘死後把確認的 byte map 記回本節（本檔是它的家）。

---

## 4. `.xwm` 編碼（選用，縮小硬碟）

- **正統工具：`xWMAEncode.exe`**（DirectX SDK / CK tools；**Yakitori 內附**）。小 console exe → **Wine 下穩定可跑**（不像 FaceFXWrapper，這支在 Wine 下廣被證實）。
  ```
  wine xWMAEncode.exe out/line.wav out/line.xwm
  ```
- **ffmpeg 產不出 Bethesda 合法 xWMA。** 它解 xWMA 沒問題但其編碼器對遊戲內合法輸出不可靠/缺。**絕不**用 ffmpeg *製*遊戲內 `.xwm`。
- **可完全跳過 xwm** —— 鬆散 WAV（或 WAV-在-fuz 內）遊戲內會播，只是硬碟較大。MVP 跳過；Wine 路徑證實後再加（與 lip 步驟可能共用同一 Wine plumbing）。

---

## 5. 把每行輸出湊起來

一行依 tier 會得到其中一些：
```
out/line.wav        # 44.1k/16/mono —— 可直接播（Tier 0、無 fuz）
out/line_16k.wav    # 16k/16/mono —— 僅 lip 輸入
out/line.lip        # 選用嘴動（Tier 1 Wine，或 Tier 2 合成）
out/line.xwm        # 選用，xWMAEncode/Wine
```
[04] 把 `(.xwm 或 .wav) + 選用 .lip` 打包成 `line.fuz`，或你出鬆散 `line.wav`。

---

## 6.「完成」長什麼樣

- 最低：`line.wav` 正規化到 44.1 kHz/16-bit mono —— 足夠 Tier-0 MVP。
- 嘴動：一個引擎接受、且讓嘴會動的 `.lip`（Tier 1 或 2 遊戲內確認）。
- 選用：`.xwm` 走 Wine。

---

### 來源
Lip：[Nukem9/FaceFXWrapper](https://github.com/Nukem9/FaceFXWrapper)、[Runalip（Nexus SSE #98931）](https://www.nexusmods.com/skyrimspecialedition/mods/98931)、[.lip/.fuz xVASynth plugin（Nexus SSE #55605）](https://www.nexusmods.com/skyrimspecialedition/mods/55605)、[Silent Voice Generator（Nexus SSE #9124）](https://www.nexusmods.com/skyrimspecialedition/mods/9124)、[FaceFX（Wikipedia —— 42 phoneme、曲線模型）](https://en.wikipedia.org/wiki/FaceFX)、LIP File Format wiki（fallout.wiki / falloutmods.fandom.com —— 瀏覽器讀）。xwm：[Yakitori（Nexus #17765）](https://www.nexusmods.com/skyrimspecialedition/mods/17765)、[Beyond Skyrim Voice Line Implementation](https://wiki.beyondskyrim.org/wiki/Arcane_University:Voice_Line_Implementation)。
