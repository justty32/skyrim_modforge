# Voice emotion-annotation index (A) — design

**Date:** 2026-06-13
**Goal:** Produce a JSON **annotation manifest** for a voice type's clips, seeded deterministically from
the vanilla INFO **Emotion** field, that the user reviews/corrects by ear — later used (phase B) to pick
an emotion-matched reference clip for F5-TTS so cloned lines carry the right tone.

This is **sub-feature A** (the index). Sub-feature **B** (emotion-aware reference selection in
`voicelines`) is a separate later spec → plan; sketched at the end.

## Why (the key insight)

F5-TTS clones the **prosody/emotion** of its few-second reference clip, not just timbre — so an angry
reference → angry output. Today a `voiceTemplate` has ONE fixed `referenceWav`, so every line from that
NPC sounds the same regardless of content. To vary tone by line, we need a *library* of reference clips
tagged by emotion.

**The first-pass annotation is free and authoritative:** every Skyrim INFO already carries an `Emotion`
(Neutral/Anger/Disgust/Fear/Sad/Happy/Surprise) + `EmotionValue` (0–100 intensity) on its response. A
clip's filename encodes its source INFO FormID, so we can map clip → INFO → game-assigned emotion
deterministically — no LLM needed for the base pass. The user only corrects the cases the coarse game
label gets wrong (e.g. labelled Neutral but actually sarcastic).

## The taxonomy

Skyrim's native 7 emotions + intensity, 1:1 with the INFO `Emotion`/`EmotionValue` fields:
`Neutral | Anger | Disgust | Fear | Sad | Happy | Surprise`, intensity 0–100.

## A. The CLI: `voice-annotate`

```
voice-annotate <esm> <voiceType> <VoicesBSA> <outDir>
```
- `<esm>` — the plugin whose INFOs label the clips: `Skyrim.esm` for vanilla voice types, or a mod
  (`SofiaFollower.esp`, `Vigilant.esm`) for that mod's character voices.
- `<voiceType>` — e.g. `MaleNord`, `FemaleYoungEager`, or a mod VTYP editorId.
- `<VoicesBSA>` — the archive holding the clips (`Skyrim - Voices_en0.bsa`, `SofiaFollower.bsa`).
- `<outDir>` — output folder.

**What it does** (mirrors / extends the existing `extract-voices`):
1. Enumerate the `Sound/Voice/<plugin>/<voiceType>/*.fuz|wav|xwm` clips in the BSA.
2. For each clip, parse the **INFO FormID** out of the filename (the CK name encodes
   `<quest>_<topic>_<infoFormId>_<n>`), look that INFO up in `<esm>`, and read its response
   `Emotion` + `EmotionValue` + the spoken line `Text` + the quest/topic context.
3. Convert the clip to WAV (existing `extract-voices` xWMA→WAV path) into `<outDir>`.
4. Write `<outDir>/voice-annotations.json` — the manifest (below).

**Manifest format** (JSON array, one entry per clip):
```jsonc
[
  {
    "clip": "MaleNord/00043F2A_1.wav",   // path under outDir
    "voiceType": "MaleNord",
    "text": "You'll regret crossing me.",
    "emotion": "Anger",                  // from the INFO (game-assigned; the base pass)
    "intensity": 80,                     // EmotionValue 0–100
    "quest": "MQ101", "topic": "...", "infoFormId": "0x00043F2A",
    "override": "",                      // YOU fill after listening: a 7-emotion name to replace `emotion`
    "intensityOverride": null,           // optional corrected intensity
    "note": ""                           // free text (e.g. "sarcastic, not angry")
  }
]
```
- `emotion`/`intensity` are deterministic (read from the INFO) — never hand-edited; you correct via
  `override`/`intensityOverride`/`note`. The *effective* emotion of a clip is `override ?? emotion`.
- An optional `aiSuggested` field MAY be added later by an LLM batch pass (reads `text` + context,
  flags likely-wrong base labels) — out of scope for the deterministic CLI.

**Clip → INFO mapping:** the filename FormID is the authoritative key (the same FormID-in-filename
`voicediag`/`voicelines` already parse). If a clip's INFO can't be found in `<esm>` (e.g. wrong esm),
emit the entry with `emotion: "Neutral"`, `note: "INFO not found in <esm>"` so nothing is silently
dropped.

## Memory / cost note
Reading a **mod** esm (Sofia 635KB, Vigilant 21MB) is trivial. Reading **Skyrim.esm** (for vanilla
voice types) uses the same lazy binary-overlay master read ModForge already does for `find` / template
clones — resolving specific INFO FormKeys is lazy (does not load the whole 250MB into the heap). No
masters of the esm are loaded; the BSA is read for clip bytes only.

## Testing
- Offline-friendly: run against `SofiaFollower.esp` (635KB) + (if available) its BSA — assert the
  manifest maps clips → INFO emotion/text correctly. The INFO-lookup + filename-FormID parse +
  manifest serialization are unit-testable with a small in-memory plugin (no master, no BSA): build a
  tiny plugin with one voiced INFO carrying an Emotion, feed its FormID-named clip, assert the manifest
  entry. Gate any test that needs the real Voices BSA / Skyrim.esm behind `RequiresSkyrim`.

## Maintenance chain
Code: `Voice.cs` (or a new `Voice.Annotate.cs`) + `Program.Build.Voice.cs` (the CLI command, beside
`extract-voices`/`voicelines`) → `CODE_MAP.infra.md` (voice section) → `SPEC-workflow.md § Voice`
(document `voice-annotate` + the manifest) → `spec.schema.json` (n/a — the manifest is a CLI artifact,
not part of the spec; but phase B will add `voiceTemplates[].referenceLibrary`).

---

## Phase B (later, separate spec): emotion-aware reference selection
- `voiceTemplates[]` gains `referenceLibrary` (path to a corrected `voice-annotations.json`) as an
  alternative to the single `referenceWav` + `referenceText`.
- When `voicelines` generates a line, it reads the target line's emotion (the built INFO's `Emotion`,
  which it already walks) and picks the manifest clip whose effective emotion matches and whose
  intensity is nearest → uses that clip's WAV as the F5 `--ref-wav` and its `text` as `--ref-text`.
- Fallback order: exact emotion + nearest intensity → same emotion any intensity → Neutral → the
  template's default `referenceWav` (backward compatible).
