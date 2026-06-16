# Risks, open questions & legal guardrail — voice-clone

← [README](README.md)

## Risks, open questions, things to verify empirically at home

These are the points the plan *cannot* settle from a Windows desk — flagged so you test them deliberately rather than assume.

- **Filename rule is implemented but still deserves in-game confirmation.** ModForge uses
  `<quest10>_<topic15>_<infoFormId8>_<responseIndex>`. A one-character mismatch = silent line, no
  error. Extract 2–3 vanilla `.fuz` filenames and confirm the generator reproduces them exactly
  before trusting large batches. ([04](04-fuz-and-filenames.md) §"Pinning the rule").
- **FaceFXWrapper under Wine is unconfirmed.** It loads CK DLLs in-memory via MemoryModule — the part most likely to break under Wine. Treat Tier 1 as "try it, ~15 min timebox"; fall straight to Tier 2 (synthetic) or Tier 0 (no lip) if it misbehaves. ([03](03-lip-and-audio-encoding.md))
- **`.lip` exact byte layout is not yet captured here** (the two authoritative wikis 403 automated fetches). If you go Tier 2, pin it at implementation by reading `fallout.wiki/wiki/LIP_File_Format` in a browser **and** hex-diffing a couple of extracted vanilla `.lip`. Known facts are recorded in [03](03-lip-and-audio-encoding.md) §"`.lip` format".
- **ffmpeg cannot produce Bethesda-valid `.xwm`.** Use `xWMAEncode.exe` (Wine) or ship loose WAV. Do not trust ffmpeg's xWMA *encoder*. ([03](03-lip-and-audio-encoding.md))
- **Zero-shot clones drift** across many lines / long lines. Budget a normalization + QA pass; the GPT-SoVITS fine-tune track exists precisely for voices where drift is unacceptable. ([01](engine-setup/README.md))
- **`FonixData.cdf`** (needed only if you ever use the CK/FaceFX path) is Bethesda property — copy from your own CK install, never redistribute.
- **xVASynth headless-on-Linux** stays *not chosen* (Linux headless recipe undocumented). Kept only as a "canonical character voice" escape hatch. ([01](engine-setup/README.md) §"Rejected/deferred").
- **MO2 reinstall reverts hand-patched files** — always rebuild into the zip, never hand-edit inside the MO2 mod folder. (Memory [[mo2-reinstall-reverts-manual-pex]].)
- **In-game test is manual** — you run the game yourself (memory [[ingame-test-workflow]]); the plan's structural checks (`*diag`, path/filename verification) come first, real MO2/Proton second.

---

## Legal / ethics guardrail

Personal, single-player, non-redistributed only. Do **not** publish cloned-voice assets — voice-actor and Bethesda rights apply, and `FonixData.cdf` is Bethesda property. Keep all generated audio local. This constraint is consistent across the whole asset-pipelines folder.
