# ModForge spec — workflow & gaps

← [index](SPEC-index.md)

## Workflow

```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json          # check first
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp   # just the plugin
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir # esp + compiled scripts -> MO2 folder
```
`package` lays out `OutModDir/<pluginName>` + `Scripts/*.pex` + `Scripts/Source/*.psc`.

**NL → spec:** describe what you want to an AI agent (Claude Code); the agent emits a spec
conforming to this doc / `../examples/spec.schema.json` (per `for_agent.md`), runs `validate`
(self-correcting on problems), then `build`/`package`. This agent-driven loop **is** the
NL→spec layer — there is no in-tool LLM API (the once-planned `describe` command is dropped),
so there's no API key/provider to configure.

## Voice (TTS voice cloning → .fuz)

Optional post-build pipeline that synthesizes voiced audio (+ lipsync) for every dialogue
line in a built plugin. External tools only — nothing is bundled.
**In-game confirmed 2026-06-13** with real F5-TTS (cloned voice plays on a custom NPC). Real-model
setup notes (Blackwell→torch cu128, F5 auto-transcribes ref when `ref_text=""`, xWMAEncode eats F5's
24 kHz PCM directly, empty custom cell drops NPCs → use a vanilla interior) live in CLAUDE.md.

**Spec fields**

- `voiceTemplates[]` — named cloning recipes, referenced by NPCs:
  - `id` — unique template name.
  - `engine` — `f5` | `fish-s2` | `chatterbox` | `gptsovits` | `xtts`. `f5` is handled by the
    bundled local `voicegen.py`; `fish-s2` is routed through `MODFORGE_FISH_SPEECH_BIN`, a local
    Fish Speech wrapper that writes a WAV. The other names are reserved until their wrappers exist.
  - `referenceWav` + `referenceText` — zero-shot reference clip and its transcript
    (paths relative to the spec file; f5 requires the transcript).
  - `modelPath` — optional fine-tuned model directory (relative to the spec).
  - `rvcModel` — optional RVC model for timbre stabilization.
  - `seed` — deterministic output.
  - `speed` / `exaggeration` / `language` — generation tuning; all three are passed
    through to the TTS process along with the rest.
- `npcs[].voiceTemplate` — ref → a `voiceTemplates` id; routes that NPC's lines to the
  cloning engine. Distinct from `npcs[].voiceType`, which is the in-game VTYP record ref
  (you still need a voiceType — it decides the output folder, see below).
- `voiceLine` (global, optional) — output settings: `format` (`fuz` | `wav` | `xwm`,
  default `fuz`) and `skipLip` (true = skip .lip generation, static mouth).

**Environment variables**

| Var | Tool | Needed for |
|-----|------|-----------|
| `MODFORGE_TTS_BIN` | TTS wrapper script/binary (e.g. an f5 venv wrapper) | required — `voicelines` errors out without it |
| `MODFORGE_FISH_SPEECH_BIN` | Fish Speech S2 wrapper script/binary | required only when a template uses `engine: "fish-s2"` |
| `MODFORGE_XWMAENCODE` | `xWMAEncode.exe` (run under Wine) | WAV → xwm encoding (`format: xwm`/`fuz`) |
| `MODFORGE_FACEFX` | `FaceFXWrapper.exe` (run under Wine) | .lip lipsync generation |
| `MODFORGE_FONIXDATA` | `FonixData.cdf` | required by FaceFXWrapper |

**Workflow**

```bash
dotnet run --project src/ModForge.Cli -- build      myspec.json out.esp   # 1. build (all dialogue/banter/scene INFOs get EditorIDs for the filenames)
dotnet run --project src/ModForge.Cli -- voicediag myspec.json out.esp    # 2. check speaker/template/path without TTS
dotnet run --project src/ModForge.Cli -- voicelines myspec.json out.esp   # 2. walk INFOs, synth WAV → xwm → .fuz next to the esp
dotnet run --project src/ModForge.Cli -- package    myspec.json OutModDir # 3. package as usual (the Sound/ tree travels with the mod)

# helper: harvest reference clips from a vanilla archive
dotnet run --project src/ModForge.Cli -- extract-voices "<path>/Skyrim - Voices_en0.bsa" FemaleYoungEager refclips/
```

Fish S2 template example:

```json
{
  "voiceTemplates": [
    {
      "id": "SeranaFish",
      "engine": "fish-s2",
      "referenceWav": "refs/serana_ref.wav",
      "referenceText": "Keep your eyes open.",
      "modelPath": "models/fish-s2-pro",
      "language": "en",
      "seed": 1234
    }
  ]
}
```

**File layout** — `Sound/Voice/<plugin>/<voiceType>/<quest10>_<topic15>_<formid8>_<n>.fuz`
(the CK naming scheme: first 10 chars of the quest EditorID, first 15 of the topic
EditorID, 8-digit hex INFO FormID, 1-based response index). The engine looks the file up
by the **speaker's voiceType**, so one generated file per voiceType serves every NPC of
that voiceType saying that line. Files that already exist are skipped on re-runs (no hash
cache yet — delete to regenerate).

Voice files are loose Skyrim assets, not records embedded inside the plugin. Packaging options:

- run `package` first, then run `voicelines <spec> <OutModDir>/<plugin>` so files are written
  directly into the final mod folder; or
- run `build` + `voicelines` in a staging directory, then `package <spec> <OutModDir> --assets <stagingDir>`
  so the generated `Sound/` tree is copied.

On Linux/Wine, Windows tools (`xWMAEncode.exe`, `FaceFXWrapper.exe`) need Windows-style paths.
ModForge converts temp paths through `winepath -w`; if conversion/tool execution fails, `format:fuz`
downgrades to loose `.wav` instead of writing a likely-silent raw-PCM `.fuz`.

**Failure behavior**

- An INFO whose speaker can't be resolved from its conditions (GetIsID, alias, or faction
  conditions are understood) → skipped with a **loud warning**, never silently.
- xwm encoding fails with `format: fuz` → the line is written as a **loose `.wav`** with a
  warning instead of packing a bare WAV into the .fuz (engine acceptance of WAV-in-fuz is
  unverified).
- Missing FaceFX env vars → no .lip (same effect as `skipLip`); subtitles still work
  (Fuz Ro D'oh not needed once real .fuz files exist).
- `engine: "fish-s2"` with no `MODFORGE_FISH_SPEECH_BIN` → the wrapper exits with a clear error.

## Not yet covered (extend in `ModForge.Core` `Generator.Build` + a spec class)
World placement now covers new interior cells, vanilla interior cells, **and exterior/worldspace
cells** (via `worldspace` + world position), and ModForge can now **create** new worldspaces (WRLD)
+ regions (REGN) — see [SPEC-world](SPEC-world.md) (record layer only; terrain/LOD/navmesh stay
CK-side). Refs (in-spec or `<master>:0xFORMID`) and the `find` command are the building blocks for
the external ones. Remaining gaps are long-tail record types/fields and the CK-side terrain/LOD/
navmesh authoring — the record-side pattern is the same: add a spec class + a loop in `Build`.
