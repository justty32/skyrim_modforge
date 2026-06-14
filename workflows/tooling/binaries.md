# External binaries

← [tooling README](README.md)

| Tool | Used for | Located via | Wine? | Missing → behavior |
|------|----------|-------------|-------|--------------------|
| `wine` | launcher for all CK `.exe` tools | PATH | — | Process.Start throws if `wine` can't start (only reached on the Wine compile/voice paths) |
| `winepath` | Unix → `Z:\…` path conversion for Wine tools | PATH | — | best-effort: on failure **silently returns the un-converted Unix path** (try/catch), which then makes the Wine exe fail with "Must specify input and output filenames" — see note below | `Voice.cs:188` |
| `PapyrusCompiler.exe` (CK) | `.psc → .pex` (Wine) | env → Steam path | yes | ExitCode 2 (prereq missing) |
| `papyrus-compiler` (native) | `.psc → .pex` (no Wine) | env → `~/tools/papyrus-compiler` | no | preferred when file exists; absent → ExitCode 2 |
| TTS bin (`voicegen-f5.sh` → `voicegen.py`, in `sub_projs/skyrim-voicegen/`) | text+emotion+ref → WAV (decoupled bedrock project; contract = `sub_projs/skyrim-voicegen/PROTOCOL.md`) | `MODFORGE_TTS_BIN`, exec'd directly | native | `voicelines` exit 1 / `GenerateWav` null |
| `xWMAEncode.exe` (CK) | WAV → xWMA for `.fuz`/`.xwm` | `MODFORGE_XWMAENCODE` | yes (needs winepath) | loose `.wav` downgrade + warning |
| `LipGenerator.exe` (CK, official) | WAV+text → `.lip` (preferred) | `MODFORGE_LIPGEN` | yes | FaceFX fallback, else no-lip |
| `FaceFXWrapper.exe` (community) | WAV+text → `.lip` (legacy) | `MODFORGE_FACEFX` (+`MODFORGE_FONIXDATA`) | yes | no-lip |
| `ffmpeg` | xWMA → WAV when extracting **vanilla** voice clips (`extract-voices`, `voice-annotate`) | **bare PATH, no env var, no presence check** | no | clip simply isn't converted (silent) — see note below | `Program.Build.Voice.cs:293,354` |
| Fish Speech wrapper | TTS via fish-s2 engine | `MODFORGE_FISH_SPEECH_BIN` | no | Python-side exit 1 (spawned by `voicegen.py`, not C#) |

Notes / gotchas:
- **F5-TTS** runs **in-process inside `voicegen.py`** (`from f5_tts.api import F5TTS`), not as a separate binary — it needs the F5 venv the `MODFORGE_TTS_BIN` wrapper activates (Python 3.11, torch **cu128** on Blackwell). `chatterbox/gptsovits/xtts` are reserved engine names, not wired.
- **`winepath` is a real but undocumented-elsewhere dependency.** Its silent-fallback-to-Unix-path is the actual mechanism behind the "xWMAEncode reports missing filenames → voice degrades to loose .wav" symptom. Both the `<wav>` and `-OutputFileName` args to `LipGenerator`, and the cdf/wav/lip args to FaceFX, are winepath-converted.
- **`ffmpeg` has no presence check** — if absent, `extract-voices`/`voice-annotate` just produce zero converted WAVs with no error. Only the two extract subcommands need it; the generation pipeline does not.
