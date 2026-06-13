# skyrim-voicegen — 語音合成基石專案

**ModForge 的基石工具,不是 ModForge 的一部分。** 它的唯一職責:**收 ModForge 給的「臺詞 + 情緒 + 參考嗓音」,吐一個 `.wav`**。怎麼合成(F5-TTS / Fish Speech / 未來別的)、用哪個 venv、ref clip 怎麼來——全是這個專案的內政,ModForge 不該知道。

兩者只透過一條協議連接(見 [`PROTOCOL.md`](PROTOCOL.md)),掛勾就是 `MODFORGE_TTS_BIN`。ModForge 把它當黑盒 exec;這個專案把 ModForge 當黑盒(只看 args、只交 wav)。**互不整合。**

```
ModForge  ──(text+emotion+ref, --out path)──►  voicegen.py  ──► writes .wav
   ▲                                                                  │
   └────────────── reads the .wav, packs xwm/.fuz/lip ◄──────────────┘
        (.wav→.fuz、lip、擺進 Sound/Voice/ 是 mod 格式,留在 ModForge)
```

## 檔案

| 檔 | 用途 |
|---|---|
| `voicegen.py` | 協議實作:解析 args、跑選定 engine、寫 wav。F5 在 venv 內 `import f5_tts`;fish-s2 轉呼 `MODFORGE_FISH_SPEECH_BIN` |
| `voicegen-f5.sh` | **正式** wrapper:在 `.venvs/f5`(py3.11)內跑 voicegen.py。`MODFORGE_TTS_BIN` 指這支 |
| `voicegen.sh` | 舊 wrapper(`venv_voice` py3.12),已被 f5 取代,留參考 |
| `PROTOCOL.md` | ModForge ↔ voicegen 的合約規格(args / wav / exit code / engine 行為) |

## 使用

```bash
export MODFORGE_TTS_BIN=/home/lorkhan/repo/ModForge/sub_projs/skyrim-voicegen/voicegen-f5.sh
# venv 在 repo root .venvs/f5(gitignore);放別處用 MODFORGE_VOICEGEN_VENV 覆寫
```
之後 ModForge 的 `voicelines` / `scripts/ship-voice.sh` 會自動透過 `MODFORGE_TTS_BIN` 呼到這裡。

## 本機環境(不進 repo)

venv、ref clip、訓練資料都 gitignore 留本機:
- `.venvs/f5/`(repo root)— F5 venv,**Blackwell 要 torch cu128**、python 3.11
- `examples/refs/`(repo root)— vanilla ref clip,由 ModForge `extract-voices <Voices BSA> <VoiceType>` 抽出
- ref-clip 細節與踩坑見 ModForge memory `voice-gen-interface-future`

> venv 內含絕對路徑,**不能直接搬**;要換位置請重建,或用 `MODFORGE_VOICEGEN_VENV` 指。
