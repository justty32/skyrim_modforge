# External data / asset dependencies (read at runtime, not in repo)

← [tooling README](README.md)

| Dependency | Needed by | Located via | Test gate |
|------------|-----------|-------------|-----------|
| `Skyrim.esm` (+ other masters) | resolving vanilla FormKeys: templated weapons, npcPatches, map-marker WRLD override, lighting templates, SM roots, … | `MODFORGE_SKYRIM_DATA` → default Steam Data | `[Trait("Category","RequiresSkyrim")]` (VoiceTests, NpcPatchTests, MapMarkerTests, LightingTests, WeaponTests). Offline run excludes them (`scripts/test-offline.sh`) |
| `Skyrim - Interface.bsa` (vanilla STRINGS) | headless resolution of a **localized** master's Name/Description (npcPatches, `booktext`/quest/spelltome diags) — extracts `<master>_english.*` to a temp `Strings/` named in **ModKey case** | under `skyrimData` | same `MODFORGE_SKYRIM_DATA`; `RequiresSkyrim` where used. See memory `headless-vanilla-strings-provision` |
| Papyrus header/source cache `~/.cache/modforge/papyrus/Source/Scripts` | Wine compiler base scripts + flags (default `MODFORGE_PAPYRUS_BASE`) | default in `Papyrus.cs` | not a unit-test dep (compile → ExitCode 2 if missing) |
| Native papyrus headers (Steam `Data/Scripts/Source`) | native compiler `-h` (default `MODFORGE_PAPYRUS_HEADERS`) | default in `Papyrus.cs` | — |
| F5-TTS venv (py 3.11, `f5_tts`, torch cu128) | the `MODFORGE_TTS_BIN` wrapper's F5 path | wrapper script | runtime only (`voicelines`) |
| vanilla / follower voice BSAs (e.g. `SofiaFollower.bsa`) | `extract-voices`/`voice-annotate` — pull reference `.fuz` clips → ffmpeg → WAV | CLI argument (`bsaPath`), filter `sound/voice/<plugin>/<voiceType>/` | not gated (user-supplied path, `File.Exists` checked) |
| spec-referenced assets (`assets/`, `examples/refs/*.wav`, `$ref` preset JSON) | voice reference WAVs/models (spec-relative), `$ref` preset includes | spec-relative paths / `SpecRefs.cs` | `examples/refs/` is gitignored (vanilla audio not committed) |
