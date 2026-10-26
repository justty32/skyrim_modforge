# External binaries

← [tooling README](README.md)

ModForge 呼叫的外部 binary：用途、定位方式、是否走 Wine、缺席時的行為。

已抽到 [binaries.json](binaries.json)（10 列）。欄位：

- `Tool`：binary 名稱。
- `Used for`：用途。
- `Located via`：定位方式（PATH、env var、預設路徑）。
- `Wine?`：是否在 Wine 下跑（`yes`／`no`／`native`／`—`）。
- `Missing → behavior`：缺席時實際發生什麼。
- `Source ref`：原表附在列尾的程式碼位置（只有 `winepath`、`ffmpeg` 兩列有）。

統計：10 列；`Wine?` 為 yes 的 4 個（PapyrusCompiler、xWMAEncode、LipGenerator、FaceFXWrapper），`wine`／`winepath` 是 Wine 端本身的工具。

Notes / gotchas:
- **F5-TTS** runs **in-process inside `voicegen.py`** (`from f5_tts.api import F5TTS`), not as a separate binary — it needs the F5 venv the `MODFORGE_TTS_BIN` wrapper activates (Python 3.11, torch **cu128** on Blackwell). `chatterbox/gptsovits/xtts` are reserved engine names, not wired.
- **`winepath` is a real but undocumented-elsewhere dependency.** Its silent-fallback-to-Unix-path is the actual mechanism behind the "xWMAEncode reports missing filenames → voice degrades to loose .wav" symptom. Both the `<wav>` and `-OutputFileName` args to `LipGenerator`, and the cdf/wav/lip args to FaceFX, are winepath-converted.
- **`ffmpeg` has no presence check** — if absent, `extract-voices`/`voice-annotate` just produce zero converted WAVs with no error. Only the two extract subcommands need it; the generation pipeline does not.
