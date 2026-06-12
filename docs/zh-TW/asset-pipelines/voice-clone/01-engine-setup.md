# 01 — 引擎安裝（Manjaro、16 GB VRAM）

← [README](README.md) · 下一份：[02-voice-data.md](02-voice-data.md)

本檔目標：讓**一個**引擎能把*文字 + 參考聲音*變成克隆 WAV，並藏在一個之後可換引擎的契約後面。三個引擎的**推論**都吃得下 16 GB VRAM；GPT-SoVITS 的*微調*也吃得下（batch size 留意即可）。

---

## 0. 可切換的引擎契約

每個引擎都化簡成同一個 shell 可呼叫的形狀。把它們全都規劃成滿足這個契約，這樣 ModForge（與你的手跑腳本）除了引擎名 + 幾個旋鈕外，永遠不必為某引擎開特例：

```
voicegen  --engine {f5|fish-s2|chatterbox|gptsovits}
          --ref     <reference.wav>          # 零訓練 ref clip，或微調好的 model 目錄
          --ref-text "<ref 的逐字稿>"        # F5 需要；Chatterbox/GPT-SoVITS 選用
          --text    "<要說的台詞>"
          --out     <line.wav>
          [--seed N] [--exaggeration F] [--speed F] [--lang en]
```

具體上這就是 venv 裡一個薄薄的 Python wrapper（`voicegen.py`），依 `--engine` 分派。ModForge 之後就 shell-out 到這支（`MODFORGE_TTS_BIN`），跟它 shell-out Papyrus 編譯器一模一樣。wrapper 保持引擎無關；各引擎怪癖藏在裡面。`fish-s2` 目前再轉呼 `MODFORGE_FISH_SPEECH_BIN`，讓 Fish 官方 CLI / HTTP API / SGLang server 的變化留在外部 wrapper。

**決定性：** 三者都吃 seed。釘死它，讓同一行重跑能重現同一段音訊（[05] 的快取會用到 —— 沒變的行別重生）。

---

## 1. Manjaro 前置（一次）

在 Manjaro 上最乾淨的路是**不**靠系統 CUDA 去配每個專案；用 per-project venv + PyTorch 自帶的 CUDA wheel。

- **驅動：** `nvidia` / `nvidia-dkms`（你幾乎一定已有）。用 `nvidia-smi` 確認看得到顯卡與 CUDA runtime 版本。
- **Python：** Manjaro 內建近期 Python；隔離用 **`uv`**（快、2026 標準）或 **miniconda**。建議 per-engine 用 `uv` —— `uv venv && uv pip install ...`。偏好 conda 也行（GPT-SoVITS 的安裝腳本假設 conda）。
- **常需的系統庫：** `ffmpeg`（音訊 I/O / 重採樣）、`sox`（選用，正規化好用）、`git`、`git-lfs`（模型權重）。`sudo pacman -S ffmpeg sox git git-lfs`。
- **PyTorch：** 裝對應近期 toolkit 的 CUDA wheel，例如 `pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124`（`cu124` 換成當前版本；wheel 自帶 CUDA，所以系統 CUDA 版本無關）。驗證：`python -c "import torch; print(torch.cuda.is_available())"` → `True`。

> 每引擎一個 venv 避免依賴衝突（它們釘不同 transformers/torch 版本）。硬碟便宜，跨引擎衝突不便宜。

---

## 2. 引擎 A — F5-TTS（零訓練，MVP 主選）

Flow-matching DiT TTS；從 **5–15 秒**參考 clip 克隆，免訓練。英中雙語 base model。舒適速度約 ~8 GB VRAM —— 16 GB 綽綽有餘。乾淨 Python、一流 CLI。**這是建議的 MVP 引擎：設定最少、立刻可腳本化。**

**安裝：**
```bash
uv venv f5 && source f5/bin/activate
uv pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124
uv pip install f5-tts
```

**一行推論（CLI）：**
```bash
f5-tts_infer-cli \
  --model F5TTS_v1_Base \
  --ref_audio  ref_malenord.wav \
  --ref_text   "transcript of the reference clip exactly as spoken." \
  --gen_text   "Stop right there, criminal scum." \
  --output_dir out/
```

**Python API（`voicegen.py` 呼叫的）：**
```python
from f5_tts.api import F5TTS
f5 = F5TTS()                      # 預設載 F5TTS_v1_Base
wav, sr, _ = f5.infer(
    ref_file="ref_malenord.wav",
    ref_text="transcript of the reference clip ...",
    gen_text="Stop right there, criminal scum.",
    file_wave="out/line.wav",
    seed=12345,
)
```

**坑／旋鈕：**
- `ref_text` 必須貼近參考 clip 實際說的字 —— 逐字稿錯會劣化克隆。
- 它支援 **TOML config + 多聲音批次**模式（`-c custom.toml`、`[voices.*]` tag）—— 之後一次 process 載入批一整個 NPC 的行很有用，免去每行付一次 model-load。
- 參考 clip 保持乾淨、單一講者、無音樂/SFX（見 [02]）。

---

## 3. 引擎 B — Chatterbox（零訓練，情緒控制）

Resemble AI、**MIT 授權**、0.5B 參數、**5–7 GB VRAM**。從 ~10 秒 ref 零訓練克隆。特色：**情緒誇張控制**（`exaggeration`）與 `cfg_weight` 節奏旋鈕 —— 適合戲劇化的 NPC 演出。**Turbo** 變體（350M、1-step）快很多，支援像 `[laugh]`/`[cough]` 的 paralinguistic tag。盲測勝過 ElevenLabs。

**安裝：**
```bash
uv venv chatterbox && source chatterbox/bin/activate
uv pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124
uv pip install chatterbox-tts
```

**Python API：**
```python
import torchaudio as ta
from chatterbox.tts import ChatterboxTTS
model = ChatterboxTTS.from_pretrained(device="cuda")
wav = model.generate(
    "Stop right there, criminal scum.",
    audio_prompt_path="ref_malenord.wav",   # 要克隆的聲音
    exaggeration=0.5,                         # 0=平 … 越高越戲劇
    cfg_weight=0.5,                           # 節奏/貼合度
)
ta.save("out/line.wav", wav, model.sr)        # model.sr 是輸出取樣率
```
（Turbo：`from chatterbox.tts import ChatterboxTurboTTS; ChatterboxTurboTTS.from_pretrained(...)`。）

**參考 clip：** ≥10 秒、WAV、24 kHz+、單一講者、無背景噪。注意 Chatterbox 以 `model.sr`（24 kHz 級）輸出 —— 你會在 [03] 為遊戲/lip 步驟降採樣。

**何時選 Chatterbox 而非 F5：** 想要情緒誇張／paralinguistic tag，或想要最輕/最快的零訓練。兩者都是 MVP 等級；契約讓 A/B 很簡單 —— 同 3 句用兩個引擎各生一次，留克隆你選定 voiceType 比較好的那個。

---

## 4. 引擎 C — GPT-SoVITS（微調，保真度/一致性升級）

少樣本：~10–60 秒就能用，**每分鐘相似度最佳**、且是 Skyrim 社群的保真度選擇。相對零訓練的勝點是**跨多行的一致性** —— 微調模型不會像單一 ref clip 那樣漂移。代價：每個語音一次性的訓練步驟。

**VRAM：** 推論 ≥6 GB（建議 RTX 3060+）；DPO 訓練選項想要 ≥12 GB —— 在 16 GB 都行。系統 RAM ≥16 GB（訓練 32 GB 更好）；保留 ~20 GB 硬碟。若 GPT-stage 訓練 VRAM 緊，**先降 `batch_size`**（它是主要 VRAM 槓桿）。

**安裝：** clone `RVC-Boss/GPT-SoVITS`；它附安裝/conda 環境與一個 WebUI 跑完整 **dataset-prep → train → infer** 迴圈，加上 `api.py`/`api_v2.py` 做 headless HTTP 推論。
```bash
git clone https://github.com/RVC-Boss/GPT-SoVITS && cd GPT-SoVITS
# 照它的 install_*.sh / conda env；它附 ASR + 切片工具做資料集前處理
```

**兩階段：**
1. **訓練（每語音一次，WebUI）：** 餵清理過的 voiceType 資料集（[02]）；它切片、ASR 轉錄、微調出一對 GPT + SoVITS 權重。輸入≈短乾淨 clip + 逐字稿。OOM 就降 `batch_size`。
2. **推論（腳本化）：** 從 `voicegen.py` 走 HTTP 驅 `api_v2.py` —— POST `{text, ref_audio, ...}`，拿 WAV。這就是 `--engine gptsovits` 分支；`--ref` 指向微調權重 + 一段短 prompt clip，而非原始 reference。

**選用的最高保真階段：TTS → RVC。** RVC（audio→audio、Linux+CUDA、成熟）把任何 TTS 輸出往目標音色重上色並*穩定*它。社群頂級保真模式就是「TTS 後接 RVC」。當成任何引擎之上的後處理 pass（`--rvc-model`），不是主引擎。等到你判定 base 品質是瓶頸再做。

---

## 5. 引擎 D — Fish Speech S2（現代 open clone backend）

Fish Speech S2 是較新的 open TTS family，目標包含 voice cloning、long-form/multispeaker 與較高容量模型。ModForge 端刻意維持薄 wrapper：`voicegen.py --engine fish-s2` 只檢查 `MODFORGE_FISH_SPEECH_BIN`，然後把 `--text`、`--out`、`--ref-audio`、`--ref-text`、`--model`、`--seed`、`--speed`、`--exaggeration`、`--language` 轉交出去。外部 wrapper 必須輸出 WAV。

**安裝草圖：**
```bash
uv venv fish-speech && source fish-speech/bin/activate
uv pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124
uv pip install fish-speech
```

依 Fish Audio 文件下載 model/codec weights。若使用 S2 Pro，本機模型目錄可放在 `voiceTemplates[].modelPath`。

**wrapper 契約：**
```bash
fish-s2-wrapper \
  --text "Stop right there, criminal scum." \
  --out out/line.wav \
  --ref-audio refs/malenord.wav \
  --ref-text "reference transcript" \
  --model models/fish-s2-pro \
  --seed 12345 \
  --speed 1.0 \
  --language en
```

`fish-s2-wrapper` 可以自己選擇驅動 Fish 官方 CLI、HTTP API 或 SGLang server；這個細節不要塞進 ModForge，否則 Fish runtime layout 一改，`.fuz` pipeline 就會跟著壞。

**Spec 範例：**
```jsonc
"voiceTemplates": [{
  "id": "SeranaFish",
  "engine": "fish-s2",
  "referenceWav": "refs/serana_ref.wav",
  "referenceText": "I knew you would come.",
  "modelPath": "models/fish-s2-pro",
  "language": "en",
  "seed": 12345
}]
```

**何時選 Fish S2：** F5 聽起來太平或長段情緒台詞漂移時，用 Fish 做較高容量模型的比較。F5 保留為快速 baseline，直到 Fish 安裝與品質都確認。

---

## 6. VRAM / 適配總表（16 GB）

| 引擎 | 推論 VRAM | 要訓練？ | 跨多行漂移 | 設定工夫 | 角色 |
|--------|---------------|-----------|-------------------------|--------------|------|
| **F5-TTS** | ~8 GB | 無（零訓練） | 中等 | 低 | MVP 主選 |
| **Chatterbox** | 5–7 GB | 無（零訓練） | 中等；有情緒旋鈕 | 低 | MVP 替選（情緒/tag） |
| **GPT-SoVITS** | ≥6 GB（DPO ≥12 GB） | 是，吃得下 16 GB | 低（已微調） | 中（訓練步驟） | 保真度/一致性升級 |
| **Fish Speech S2** | 視模型而定；S2 Pro 較重 | 選用 | 預期低/中 | 中/高 | 現代 clone 比較 |
| RVC（後處理） | 小 | 是 | —（穩定化） | 中 | 選用最高保真重上色 |

全在 16 GB 內。沒有任何一項逼你上雲或量化。

---

## 7. 已拒絕／延後的引擎（與理由，別盲目重新考慮）

- **xVASynth / xVATrainer** —— Skyrim-native、「*就是*某個已知角色」的選擇，但無 **headless-Linux** 文件化 recipe（Electron 前端是 Windows；Python 後端*架構上*可跑但無被驅動成服務的文件）。當你特別想要 canonical vanilla 角色音色、且願意逆向其後端時的逃生口保留。非預設。
- **XTTS v2（Coqui）** —— 易上手零訓練，但 Coqui 已歇業（權重/fork 仍在）。F5/Chatterbox 是維護更健康的等價物。只在某 fork 更方便時用。
- **Qwen3-TTS** —— 2026 強勢新秀；若 F5/Chatterbox/Fish 都不滿意，可用同一 wrapper 契約接上。尚未 wired。
- **Piper** —— Linux-native、CPU 快、*機械音、不克隆*。當**即時占位**驗證 [03]/[04]/[06] plumbing，免等 GPU/克隆品質。
- **ElevenLabs** —— 業界最佳但雲端 + ToS + 成本。個人用後備而已；違背 local-on-Manjaro 的目標。

---

## 8. 本檔「完成」長什麼樣

你能跑 `voicegen.py --engine f5 --ref ref.wav --ref-text "..." --text "Hello." --out hello.wav` 並拿到可懂、對味的 WAV。光是這個能力就解鎖 [06] 整個 MVP。之後全是打包。

---

### 來源
F5-TTS：[SWivid/F5-TTS](https://github.com/SWivid/F5-TTS)、[CLI 文件（DeepWiki）](https://deepwiki.com/SWivid/F5-TTS/3.2-command-line-interface)、[f5-tts PyPI](https://pypi.org/project/f5-tts/)。Chatterbox：[resemble-ai/chatterbox](https://github.com/resemble-ai/chatterbox)、[chatterbox-tts PyPI](https://pypi.org/project/chatterbox-tts/)、[Chatterbox Turbo](https://www.resemble.ai/chatterbox-turbo/)。GPT-SoVITS：[RVC-Boss/GPT-SoVITS](https://github.com/RVC-Boss/GPT-SoVITS)。Fish Speech：[fishaudio/fish-speech](https://github.com/fishaudio/fish-speech)、[Fish Audio self-hosted inference](https://docs.fish.audio/developer-guide/self-hosting/running-inference)、[Fish Audio S2 technical report](https://arxiv.org/abs/2603.08823)。Landscape 比較：[BentoML 2026 OSS TTS](https://www.bentoml.com/blog/exploring-the-world-of-open-source-text-to-speech-models)、[SiliconFlow voice-cloning 2026](https://www.siliconflow.com/articles/en/best-open-source-models-for-voice-cloning)。
