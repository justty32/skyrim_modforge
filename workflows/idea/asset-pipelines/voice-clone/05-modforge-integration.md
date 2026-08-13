# 05 — ModForge integration design

← [README](README.md) · prev: [04-fuz-and-filenames.md](04-fuz-and-filenames.md) · next: [06-standalone-runbook.md](06-standalone-runbook.md)

How the proven hand pipeline ([06]) folds into the generator. This started as design, but the core
structure landed by 2026-06-12: `voicelines`, `voicediag`, `voiceTemplates`, `voiceLine`, TTS wrapper
shell-out, Wine xWMA path conversion, and native `.fuz` writing now exist. Treat this file as the
historical design/cross-check; use `SPEC-workflow.md` and `CODE_MAP.infra.md` for current commands
and code locations.

**Mirror these existing conventions exactly** (don't invent new patterns):
- **Shell-out with env-var fallback:** `Papyrus.cs` is the template — `PapyrusOptions` fields fall back `null → MODFORGE_* env → default`, drive an exe under Wine *or* native. Voice tooling copies this shape.
- **Conditional EmbeddedResource:** the six `MF*.psc/.pex` embed into `ModForge.Cli.csproj` conditionally (missing file still builds; runtime warns). Any embedded helper (e.g. a synthetic-lip data table) follows this.
- **Asset copy + MO2 assembly:** `Assets.cs` copies the `Meshes/Textures/Sounds` tree; `Package.cs` assembles the flat MO2 folder. Voice output is just more `Sound/Voice/...` for these to pick up.
- **Two-pass build:** `Generator.Build.cs` = pass 1 (build records) → pass 2 (link). Voice generation runs *after* records exist (it needs final INFO FormIDs), so it's a **post-build step**, not a record builder.

---

## 1. Spec design (new fields)

Additive only — no breaking changes (per CLAUDE.md: new optional fields are safe; existing examples unaffected). After adding, update `examples/spec.schema.json` and `sample_spec.json`.

**`VoiceTemplate`** — a named voice recipe (top-level list on `ModSpec`, like other record families in `Spec.cs`):
```jsonc
"voiceTemplates": [{
  "id": "MaleNordCloned",
  "engine": "f5",                         // f5 | chatterbox | gptsovits
  "referenceWav": "refs/MaleNord_ref.wav",// zero-shot ref (f5/chatterbox)
  "referenceText": "transcript ...",      // f5 needs it
  "modelPath": null,                       // gptsovits fine-tuned dir (instead of ref)
  "rvcModel": null,                        // optional RVC recolor post-pass
  "language": "en",
  "seed": 12345,
  "exaggeration": 0.5                       // chatterbox emotion knob (optional)
}]
```
This mirrors the deferred plan in memory [[voice-gen-interface-future]].

**`NpcSpec.voiceTemplate`** — routes an NPC's lines to a template. NPC already has `VoiceType` (ref → VTYP; line 16 of `Spec.Actors.cs`) which supplies the **path segment**; `voiceTemplate` supplies the **engine/voice**. Alternatively a global `voiceType → voiceTemplate` map so vanilla-voiced NPCs auto-route.

**`voiceLine` (optional, per-INFO or per-build):** `{ skipLip: bool, format: "wav"|"xwm"|"fuz" }`. INFO lines already carry their **text** — no new field for words.

> Field hygiene: removing/renaming a field later requires `grep -r "field" examples/` and updating all hits in the same commit (CLAUDE.md rule). Adding is free.

---

## 2. New CLI step `voicelines` (sibling to `compile`/`package`)

Lives alongside `build`/`validate`/`package`/`compile` in `Program.Build.cs`. Runs **after** `build` (needs final INFO FormIDs). Pipeline per emitted INFO response:

```
voicelines <spec.json> <built.esp>
  1. walk INFO records (Mutagen read of the built esp, like Diagnostics does)
  2. for each response: resolve text + NPC voiceType + voiceTemplate
  3. compute CK-matched filename + path        ← deterministic, ModForge's superpower ([04])
  4. cache check: skip if (text+template+seed) unchanged and file exists
  5. shell out to voicegen (MODFORGE_TTS_BIN) → line_raw.wav
  6. normalize (mono/16-bit; 16k copy if lip)  → native or ffmpeg
  7. (opt) lip: FaceFXWrapper/Runalip under Wine, OR native synthetic ([03])
  8. (opt) xwm: xWMAEncode under Wine ([03])
  9. pack .fuz natively (Generator.Build.Voice.cs) OR emit loose wav ([04])
 10. write to Data/Sound/Voice/<plugin>/<voicetype>/<name>.<ext>
```
Steps 1, 3–6, 8–10 are fully automatable; 7 is the contingent lip step. `package` then sweeps the output into the zip.

**Why a separate step (not inside `build`):** voice gen is slow (GPU per line), needs the final esp, and has heavy optional external deps. Keeping it separate means `build` stays fast and dependency-free; `voicelines` is opt-in. Same reasoning as `compile` being separate from `build`.

---

## 3. New core file `Generator.Build.Voice.cs`

Native, no Wine, unit-testable (like `Generator.SceneFragments.cs` / `QuestFragments.cs` which are pure and Wine-free):
- `WriteFuz(byte[] audio, byte[]? lip)` — the ~20-line fuz writer ([04] §1).
- `VoiceFileName(quest, infoFormId, responseIndex, topic)` — the deterministic filename rule ([04] §2), with a test asserting it reproduces extracted vanilla names.
- (Tier 2 only) `SyntheticLip(float[] envelope, double durationSec)` — envelope → phoneme-keyframe `.lip` ([03] §2/§3).

Kept ≤300 lines (CLAUDE.md). The shell-out orchestration (TTS/Wine) lives in a separate `Voice.cs` (Core) mirroring `Papyrus.cs` — options class with `null → MODFORGE_* → default` fallback.

---

## 4. Tooling config (env vars, conditional)

Following `Papyrus.cs`/`PapyrusOptions`:

| Env var | Points at | Absent → |
|---------|-----------|----------|
| `MODFORGE_TTS_BIN` | the `voicegen.py` venv wrapper ([01]) | no TTS generation; planning/diag still works |
| `MODFORGE_XWMAENCODE` | `xWMAEncode.exe` (run under Wine) | skip xwm, ship WAV |
| `MODFORGE_FACEFX` | `FaceFXWrapper.exe` (Wine) | skip lip (Tier 0) or use synthetic (Tier 2) |
| `MODFORGE_FONIXDATA` | `FonixData.cdf` | required only if `MODFORGE_FACEFX` set |

Missing xWMA/lip tools gracefully degrade to a lower tier (`.wav` or no lip). Missing TTS blocks actual
generation but `voicediag` / `voicelines --plan` still provide the offline map.

---

## 5. Package + build-pipeline wiring

- `Assets.cs` already copies the `Sounds` tree — ensure `Sound/Voice/<plugin>/<voicetype>/` is included (it should fall out of the existing `Sound/...` copy; verify the glob reaches `Voice/`).
- `Package.cs` flat MO2 assembly handles `Sound/...` when that tree is provided through `--assets`
  or `spec.assets`. It does not discover voice output sitting beside an unrelated build by itself.
  **No `.seq` interaction** (voice ≠ StartGameEnabled quests).
- Reliable orders: `package` to the final mod folder, then run `voicelines` against that plugin; or
  `build` + `voicelines` in a staging dir, then `package --assets <stagingDir>`.

---

## 6. Maintenance-chain placement (when this lands)

Per CLAUDE.md Workflow 1, these were the landing points; most are now present:
- **Code:** `Spec.cs` (+`Spec.Actors.cs`), `Generator.Build.Voice.cs`, `Voice.cs`, `Program.Build.cs`, `examples/spec.schema.json` + `sample_spec.json`.
- **CODE_MAP:** add `Generator.Build.Voice.cs` / `Voice.cs` rows to `CODE_MAP.infra.md`; the `voicelines` command to the CLI table; the spec fields cross-ref into `CODE_MAP.dialogue-quests.md` (INFO/voiceType live there). Add a Tests row (`VoiceFileNameTests`, `FuzWriterTests`).
- **Docs:** `voiceLine`/`voiceTemplate` fields in SPEC docs; `voicelines` / `voicediag` in workflow docs.
- `voicediag <spec> <built.esp>` now verifies emitted filenames/paths against the esp's INFO records
  without running TTS or the game.

---

## 7. What "done" looks like

`modforge voicelines spec.json built.esp` walks the INFOs, generates a WAV per line via `voicegen.py`, writes them to the correct deterministic paths, and `package` bundles them — with `MODFORGE_TTS_BIN` set and everything else (xwm/lip) degrading gracefully when unset. The hand runbook ([06]) is the spec for what this step automates.

---

### Sources
Internal conventions read from `workflows/common/code-map/CODE_MAP.infra.md`, `src/ModForge.Core/Papyrus/Papyrus.cs`, `src/ModForge.Core/Spec/Spec.Actors.cs`, `src/ModForge.Cli/ModForge.Cli.csproj`. Engine/format facts: see [01]–[04].
