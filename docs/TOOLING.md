# ModForge — External Tooling & Environment Dependencies

ModForge is a Skyrim mod **generator**: record-building (the ESP/ESM itself) is pure Mutagen and needs no external tools. External binaries and data files only come into play for **Papyrus compilation**, the **voice pipeline**, and **vanilla-master resolution**. Everything here is optional in the sense that a missing tool degrades or skips one feature — the core build never hard-crashes on a missing dependency (the one exception is a spec `$env` with no default, which is a deliberate hard error).

Conventions in the tables:
- **Required?** — required *for the feature that reads it*, not for ModForge as a whole.
- **Missing → behavior** — what actually happens when the tool/var is absent. ModForge prefers *warn-and-degrade* over throwing.

---

## 1. Environment variables (`MODFORGE_*`)

| Var | Points to | Required? | Missing → behavior | Read at |
|-----|-----------|-----------|--------------------|---------|
| `MODFORGE_SKYRIM_DATA` | Skyrim SE `Data/` dir (masters, BSAs) | for any build that resolves vanilla records | falls back to default Steam path; if the needed master isn't there, the dependent record is **warned + skipped** (e.g. npcPatch) | `Generator.BuildContext.cs:103` |
| `MODFORGE_PAPYRUS_COMPILER` | CK `PapyrusCompiler.exe` (Wine) | for Wine-backend compile | fallback to hard-coded Steam path; absent → `CompileResult` ExitCode 2 `PapyrusCompiler not found` (never throws) | `Papyrus.cs:13` |
| `MODFORGE_PAPYRUS_BASE` | dir w/ base `.psc` + `TESV_Papyrus_Flags.flg` (Wine) | for Wine-backend compile | fallback `~/.cache/modforge/papyrus/Source/Scripts`; missing flags → ExitCode 2 | `Papyrus.cs:17` |
| `MODFORGE_PAPYRUS_COMPILER_BIN` | Linux-native `papyrus-compiler` | preferred when present (no Wine) | fallback `~/tools/papyrus-compiler`; `CompileBest` uses native only if the file exists, else Wine; absent in native path → ExitCode 2 | `Papyrus.cs:34` |
| `MODFORGE_PAPYRUS_HEADERS` | base-game `.psc` header sources (native) | for native compile (esp. `extends ReferenceAlias`) | fallback to Steam `Data/Scripts/Source`; missing → ExitCode 2 | `Papyrus.cs:38` |
| `MODFORGE_TTS_BIN` | TTS wrapper (`voicegen.py` venv launcher); **exec'd directly** | **for `voicelines`** | CLI prints `ERROR: MODFORGE_TTS_BIN not set. Voice generation skipped.` exit 1; lib `GenerateWav` → null. `--dry-run`/`--plan`/`voicediag` don't need it | `Voice.cs:31`, CLI `Program.Build.Voice.cs:40` |
| `MODFORGE_XWMAENCODE` | CK `xWMAEncode.exe` (Wine) | optional | null/fail → **downgrade fuz/xwm to loose `.wav`** + `!!` warning (never raw PCM in `.fuz`); any `.lip` shipped loose | `Voice.cs:32`, downgrade `Generator.Build.Voice.cs:25` |
| `MODFORGE_LIPGEN` | CK official `LipGenerator.exe` (Wine) — **preferred lip backend** | optional | unset → fall through to FaceFX; both unset (fuz, skipLip=false) → one warning + **no-lip / static mouth**. Auto-finds `FonixData.cdf` beside its own exe | `Voice.cs:35`, dispatch `Voice.Lip.cs:35` |
| `MODFORGE_FACEFX` | community `FaceFXWrapper.exe` (Wine) — **legacy lip fallback** | optional | only used when `MODFORGE_LIPGEN` absent; null → no lip | `Voice.cs:33`, `Voice.Lip.cs:80` |
| `MODFORGE_FONIXDATA` | `FonixData.cdf` (FaceFX **only**) | only for the FaceFX fallback | null → FaceFX path yields no lip. **Not needed** for the preferred LipGenerator path | `Voice.cs:34`, `Voice.Lip.cs:81` |
| `MODFORGE_FISH_SPEECH_BIN` | local Fish Speech wrapper | only for `--engine fish-s2` | **read in `voicegen.py` (Python), not C#**: missing → `sys.exit(1)`. The `f5` engine never reads it | `voicegen.py:61` |
| `MODFORGE_DEBUG` | flag (any value) | optional | unset → top-level CLI catch prints only `ERROR: <Type>: <Message>`; set → full stack trace | `Program.cs:71` |
| `MODFORGE_SKYRIM_MASTERS` | space-separated master list | shell only | `scripts/extract-skyrim-masters.sh` only; unset → built-in default list | `scripts/extract-skyrim-masters.sh:10` |
| `MODFORGE_REFERENCE_OUT` | extract output dir | shell only | `scripts/extract-skyrim-masters.sh` only; unset → `$repo/reference/skyrim-masters-local` | `scripts/extract-skyrim-masters.sh:8` |
| *(arbitrary)* | any var named in a spec `$env` directive | contextual | resolved by `SpecRefs.cs`; **no value and no `default` → hard error** (`SpecRefException`) — the one deliberate non-degrading failure | `SpecRefs.cs:34` |

---

## 2. External binaries

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

---

## 3. External data / asset dependencies (read at runtime, not in repo)

| Dependency | Needed by | Located via | Test gate |
|------------|-----------|-------------|-----------|
| `Skyrim.esm` (+ other masters) | resolving vanilla FormKeys: templated weapons, npcPatches, map-marker WRLD override, lighting templates, SM roots, … | `MODFORGE_SKYRIM_DATA` → default Steam Data | `[Trait("Category","RequiresSkyrim")]` (VoiceTests, NpcPatchTests, MapMarkerTests, LightingTests, WeaponTests). Offline run excludes them (`scripts/test-offline.sh`) |
| `Skyrim - Interface.bsa` (vanilla STRINGS) | headless resolution of a **localized** master's Name/Description (npcPatches, `booktext`/quest/spelltome diags) — extracts `<master>_english.*` to a temp `Strings/` named in **ModKey case** | under `skyrimData` | same `MODFORGE_SKYRIM_DATA`; `RequiresSkyrim` where used. See memory `headless-vanilla-strings-provision` |
| Papyrus header/source cache `~/.cache/modforge/papyrus/Source/Scripts` | Wine compiler base scripts + flags (default `MODFORGE_PAPYRUS_BASE`) | default in `Papyrus.cs` | not a unit-test dep (compile → ExitCode 2 if missing) |
| Native papyrus headers (Steam `Data/Scripts/Source`) | native compiler `-h` (default `MODFORGE_PAPYRUS_HEADERS`) | default in `Papyrus.cs` | — |
| F5-TTS venv (py 3.11, `f5_tts`, torch cu128) | the `MODFORGE_TTS_BIN` wrapper's F5 path | wrapper script | runtime only (`voicelines`) |
| vanilla / follower voice BSAs (e.g. `SofiaFollower.bsa`) | `extract-voices`/`voice-annotate` — pull reference `.fuz` clips → ffmpeg → WAV | CLI argument (`bsaPath`), filter `sound/voice/<plugin>/<voiceType>/` | not gated (user-supplied path, `File.Exists` checked) |
| spec-referenced assets (`assets/`, `examples/refs/*.wav`, `$ref` preset JSON) | voice reference WAVs/models (spec-relative), `$ref` preset includes | spec-relative paths / `SpecRefs.cs` | `examples/refs/` is gitignored (vanilla audio not committed) |

---

## Fresh-clone prerequisite

Before the first `dotnet build`, the six dispatcher/controller `.psc` must be compiled to `.pex` (they're embedded as conditional `EmbeddedResource` but `.pex` is gitignored). See CLAUDE.md「前置步驟」for the exact `compile` commands. Missing `.pex` only *warns* at build time — the relevant Fire()-routed trigger / identity feature won't work at runtime until the `.pex` exists locally.
