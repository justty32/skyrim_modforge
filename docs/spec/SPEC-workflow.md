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

## `requires[]` — declare the mods this plugin needs (build enforces it)

Naming `"PROTEUS.esp:0x08073D"` anywhere in a spec makes **PROTEUS.esp a master** of the output, and
**Skyrim silently refuses to load a plugin whose masters are missing** — no error, no log line, the
records just aren't there. `build` always *reports* the masters it linked (and writes
`<plugin>.requires.txt`). `requires[]` goes further: it is what the author **declares**, and build
**fails** when the two disagree.

```json
{
  "pluginName": "MyMod.esp",
  "requires": [
    "XPMSE.esp",
    { "plugin": "PROTEUS.esp", "version": "3.4+", "reason": "the captured player's spells",
      "url": "https://www.nexusmods.com/skyrimspecialedition/mods/62934" },
    { "name": "PapyrusUtil SE", "reason": "storageWrites (SKSE plugin — has no .esp)" }
  ]
}
```

| Field | Meaning |
|-------|---------|
| `plugin` | A master the build is expected to link (`.esp`/`.esm`/`.esl`). **Checked, both ways.** A bare string in the list is shorthand for this. |
| `name` | A requirement with **no plugin of its own** (an SKSE DLL, a loose-file framework). It can never be a master, so it is **documentation only** and is never checked — but it does go into the sidecar, which is the requirements list a player reads. |
| `version` | **Documentation only — ModForge cannot verify it.** A Skyrim plugin carries no mod version (see below). Printed for humans, never enforced. |
| `reason` | Why the mod is needed. `--sync-requires` auto-fills it with the spec field that pulled the master in. |
| `url` | Where to get it — goes into the sidecar. |

**The two checks:**

- **linked but not declared → ERROR, and nothing is written.** This is the drift the feature exists to
  catch: the plugin just acquired an install requirement nobody signed up for. The message names the
  exact spec field (`capturedNpcs[0].spells[2] = PROTEUS.esp:0x08073D`), so you can either delete that
  line or declare the master.
- **declared but never linked → warning.** A stale/copy-pasted line. (If the mod is needed at *runtime*
  but no record references it, it is not a master: declare it with `name` instead.)

**Backward compatible by construction:** a spec with **no `requires` section** is not checked at all —
writing one is how you opt in. `"requires": []` is an opt-in too: it declares *"this mod stays
vanilla-only"*, so any mod ref then fails the build.

**`build --sync-requires`** writes the masters the build actually links back into the spec's
`requires[]` (creating the section if absent, dropping stale entries, keeping the `reason`/`version`/
`url` you authored, never touching `name` entries):

```bash
dotnet run --project src/ModForge.Cli -- build myspec.json out.esp --sync-requires
```
A capture (`sc cap` / `sc capp`) drags in a dependency for every mod-given spell/perk/item, so
hand-maintaining the list would make the contract not worth having. The point of syncing rather than
staying silent: the dependency set becomes a **line in the spec diff** — a change to what your mod
needs shows up in `git diff` like any other change.

**Why there is no version *check*:** an `.esp` has no mod version. Its `TES4`/`HEDR` "version" is the
file **format** version (1.70/1.71 — identical for PROTEUS 3.4 and for a two-record test plugin);
`CNAM`/`SNAM` (author/description) are free text, usually `DEFAULT` or empty. The only place a real
version lives is the **mod manager's** metadata (MO2 `meta.ini` `version=`, from Nexus), which is not
in the plugin and not visible to a build. So `version` is a label for humans and is marked as such in
`<plugin>.requires.txt`; ModForge will not pretend to verify it.

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
- `voiceSpeakers[]` — voice lines whose speaker is an **EXTERNAL** NPC (from another master the
  built plugin can't resolve — e.g. an existing follower like Sofia, gated via a manual
  `GetIsID(<master>:0xFORMID)` condition). Each entry `{ speaker, voiceType, template }` binds that
  NPC ref → its `voiceType` (folder name, e.g. `JJSofiaVoiceType`) → a `voiceTemplates` id. Without
  it the speaker is unresolvable (mod-only cache) and the line gets no voice. **Extract a clone ref
  from the follower's own BSA** with `extract-voices <Follower.bsa> <VoiceType> <outDir> <Follower.esp>`
  (the optional 4th arg keys the BSA voice path off that plugin, not Skyrim.esm). This is how you make
  an existing fully-voiced follower comment on new content in their own voice.
- `voiceLine` (global, optional) — output settings: `format` (`fuz` | `wav` | `xwm`,
  default `fuz`) and `skipLip` (true = skip .lip generation, static mouth).

**Environment variables**

| Var | Tool | Needed for |
|-----|------|-----------|
| `MODFORGE_TTS_BIN` | TTS wrapper script/binary (e.g. an f5 venv wrapper) | required — `voicelines` errors out without it |
| `MODFORGE_FISH_SPEECH_BIN` | Fish Speech S2 wrapper script/binary | required only when a template uses `engine: "fish-s2"` |
| `MODFORGE_XWMAENCODE` | `xWMAEncode.exe` (run under Wine) | WAV → xwm encoding (`format: xwm`/`fuz`) |
| `MODFORGE_LIPGEN` | CK official `LipGenerator.exe` (run under Wine) | **preferred** .lip lipsync generation; ships with the Creation Kit at `Tools/LipGen/LipGenerator/` and auto-finds `FonixData.cdf` next to its own exe (no separate cdf var needed) |
| `MODFORGE_FACEFX` | community `FaceFXWrapper.exe` (run under Wine) | fallback .lip generation when `MODFORGE_LIPGEN` is unset |
| `MODFORGE_FONIXDATA` | `FonixData.cdf` | required by the `MODFORGE_FACEFX` fallback only |

> Lip sync runs automatically when `format: fuz` and `skipLip` is false. With `MODFORGE_LIPGEN` pointed at the
> CK `LipGenerator.exe`, `voicelines` packs a real `.lip` into each `.fuz` so NPC mouths move — **confirmed in-game
> 2026-06-13** (the NPC's mouth animates with the syllables). With no lip tool configured, `voicelines` prints a
> one-time warning and the `.fuz` ships without lip data (static mouth) — subtitles still work.
>
> **Folder-name trap:** `voicelines` writes to `Sound/Voice/<PluginName>/…`, so run it on the *packaged* plugin
> (package first, then voicelines on the packaged esp) — otherwise the voice folder won't match the shipped
> plugin name and the engine won't find the audio.

**Workflow**

```bash
dotnet run --project src/ModForge.Cli -- build      myspec.json out.esp   # 1. build (all dialogue/banter/scene INFOs get EditorIDs for the filenames)
dotnet run --project src/ModForge.Cli -- voicediag myspec.json out.esp    # 2. check speaker/template/path without TTS
dotnet run --project src/ModForge.Cli -- voicelines myspec.json out.esp   # 2. walk INFOs, synth WAV → xwm → .fuz next to the esp
dotnet run --project src/ModForge.Cli -- package    myspec.json OutModDir # 3. package as usual (the Sound/ tree travels with the mod)

# helper: harvest reference clips from a vanilla archive
dotnet run --project src/ModForge.Cli -- extract-voices "<path>/Skyrim - Voices_en0.bsa" FemaleYoungEager refclips/

# helper: harvest reference clips AND tag each with its source INFO emotion → annotation manifest
dotnet run --project src/ModForge.Cli -- voice-annotate <esm> <voiceType> <VoicesBSA> <outDir>
```

**`voice-annotate`** — like `extract-voices`, but for every clip it also looks up the source INFO (the
8-hex FormID is in the clip filename) in `<esm>` and writes `<outDir>/voice-annotations.json`: one entry
per clip with `clip` / `text` / `emotion` (the 7 Skyrim emotions: Neutral/Anger/Disgust/Fear/Sad/Happy/
Surprise) / `intensity` (0–100) / `infoFormId`, plus blank `override` / `intensityOverride` / `note`
fields for you to fill after listening. `emotion`/`intensity` come straight from the INFO's
`Emotion`/`EmotionValue` (the game already labelled every line — a free, authoritative first pass); you
only correct what the coarse label gets wrong (e.g. labelled Neutral but actually sarcastic — set
`override`). `<esm>` is `Skyrim.esm` for vanilla voice types, or a mod (`SofiaFollower.esp`, `Vigilant.esm`)
for that mod's character voices. *(Phase B — `voiceTemplates[].referenceLibrary` consuming the corrected
manifest to pick an emotion-matched reference clip per line — is a separate later feature.)*

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
- No lip tool configured (neither `MODFORGE_LIPGEN` nor `MODFORGE_FACEFX`) → no .lip (same effect
  as `skipLip`) plus a one-time warning; subtitles still work (Fuz Ro D'oh not needed once real
  .fuz files exist).
- `engine: "fish-s2"` with no `MODFORGE_FISH_SPEECH_BIN` → the wrapper exits with a clear error.

## Not yet covered (extend in `ModForge.Core` `Generator.Build` + a spec class)
World placement now covers new interior cells, vanilla interior cells, **and exterior/worldspace
cells** (via `worldspace` + world position), and ModForge can now **create** new worldspaces (WRLD)
+ regions (REGN) — see [SPEC-worldspaces](SPEC-worldspaces.md) (record layer only; terrain/LOD/navmesh stay
CK-side). Refs (in-spec or `<master>:0xFORMID`) and the `find` command are the building blocks for
the external ones. Remaining gaps are long-tail record types/fields and the CK-side terrain/LOD/
navmesh authoring — the record-side pattern is the same: add a spec class + a loop in `Build`.
