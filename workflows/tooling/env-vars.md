# Environment variables (`MODFORGE_*`)

← [tooling README](README.md)

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
