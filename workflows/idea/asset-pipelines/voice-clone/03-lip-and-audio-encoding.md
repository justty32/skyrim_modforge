# 03 — Lip-sync + audio encoding

← [README](README.md) · prev: [02-voice-data.md](02-voice-data.md) · next: [04-fuz-and-filenames.md](04-fuz-and-filenames.md)

Two jobs here: (1) optionally make the mouth move, (2) optionally encode `.wav` → `.xwm`. Both are *optional* — a loose 44.1 kHz/16-bit mono WAV at the right path plays in-game with a static mouth. This file is about adding movement and shrinking files.

Your decision: **"mouth moves anyhow is fine."** So the target is *any* movement, not phoneme accuracy. That changes the recommended order below.

---

## 1. Audio normalization (always, cheap, native)

Before lip or xwm, produce the canonical game WAV and (if doing lip) the 16 kHz lip-input copy:

```bash
# canonical in-game WAV: PCM 16-bit mono, 44.1 kHz
ffmpeg -i out/line_raw.wav -ac 1 -ar 44100 -sample_fmt s16 out/line.wav
# lip-input copy ONLY if generating .lip: 16 kHz 16-bit mono
ffmpeg -i out/line_raw.wav -ac 1 -ar 16000 -sample_fmt s16 out/line_16k.wav
```
Engine output is often 24 kHz; downsampling to 44.1 kHz is an upsample-or-passthrough that's harmless. The **16 kHz mono** copy is exactly what FaceFXWrapper expects, so generating it now means the lip step needs no resample.

---

## 2. `.lip` — tiered plan (pick the lowest tier that gives movement)

### Tier 0 — no lip (baseline, always works, **static mouth**)
Ship the WAV, or pack a `.fuz` with `FuzLipSize == 0`. Audio + subtitles play perfectly; the mouth doesn't move. This is the MVP default and the guaranteed-correct fallback. *Doesn't satisfy "mouth moves," but it's the floor everything builds on.*

### Tier 1 — FaceFXWrapper / Runalip under Wine (**recommended first try for movement**)
This gives *correct* movement essentially for free **if Wine cooperates**. Since you don't need accuracy, you also don't need to care if it's slightly off — you just need it to run.

```
FaceFXWrapper Skyrim USEnglish FonixData.cdf in.wav line_16k.wav out.lip "the spoken text"
```
- **Args:** `[Type] [Lang] [FonixDataPath] [WavPath] [ResampledWavPath] [LipPath] [Text]`. Type = `Skyrim`, Lang = `USEnglish`. If you pass an already-16 kHz/16-bit/mono WAV as `ResampledWavPath`, it skips its own resample.
- **`FonixData.cdf`:** not shipped with the tool — copy from your own CK install at `Data/Sound/Voice/Processing/FonixData.cdf` (also mirrored on Nexus). Bethesda property; keep local.
- **Wine risk:** FaceFXWrapper is Windows-only and loads CK DLLs in-memory via **MemoryModule** — that's the part most likely to fail under Wine (no documented Wine testing). **Timebox it ~15 min.** If it runs, you're done — wrap it as a shell-out and move on.
- **Batch alternative:** **Runalip** (Nexus SSE #98931) — console `Runalip.exe` mass-generates `.lip` (and optionally `.fuz`) from `.wav`/`.fuz` + a CSV of text. Same Wine caveat; nicer for bulk. The **xVASynth `.lip`/`.fuz` plugin** (Nexus #55605) bundles the same capability and auto-packs fuz.

### Tier 2 — synthetic envelope-driven `.lip` in native C# (**backstop if Wine fails**)
Because you only need *movement*, you don't need real phonemes. Plan: compute the audio's amplitude envelope, and emit a `.lip` whose phoneme keyframes alternate between an **open** mouth shape (on energy peaks) and a **closed/neutral** shape (on valleys), timed across the clip. The mouth flaps with the speech rhythm — visually "talking," accuracy irrelevant. This is 100% Linux-native (no Wine, no FonixData, no CK), and folds into the same `Generator.Build.Voice.cs` as the fuz writer.

Implementation is gated on pinning the `.lip` byte layout (see §3). Effort: a few hours once the format is confirmed. Only build this if Tier 1 misbehaves.

> Note on **Silent Voice Generator** (Nexus SSE #9124): it produces duration-matched `.lip` for *silent/unvoiced* dialogue — i.e. a **blank** lip (no movement). Useful as a reference implementation of "write a structurally-valid `.lip` of length N", but its output is a static mouth, so it's a code reference for Tier 2, not a movement solution itself.

### Decision flow
```
Want movement?
  no  → Tier 0 (no lip)
  yes → try Tier 1 (FaceFXWrapper/Runalip under Wine, 15-min timebox)
          works → done (accurate movement, free)
          fails → Tier 2 (synthetic envelope .lip, native C#)   [or accept Tier 0]
```

---

## 3. `.lip` format — what's known, and how to pin the rest

**Captured facts (enough to start; not yet a full byte map):**
- `.lip` is a **FaceFX** facial-animation blob. FaceFX recognizes **42 phonemes**; the animation is bone-pose/morph-target curves driven over time.
- Generated from a **16 kHz/16-bit/mono WAV + the spoken text** (FaceFXWrapper's whole job is WAV+text → phoneme curves).
- **Offset/timing convention:** a phoneme's position code ≈ `timestamp_seconds × 4 × sampleRate` (sampleRate ~22050). e.g. a sound at 2.13 s → `2.13 × 4 × 22050 ≈ 0x00031A38`. This is the key to placing keyframes for a synthetic lip.
- It lives inside `.fuz` as the lip section ([04]); `FuzLipSize == 0` means "no lip".

**To pin before writing Tier 2** (do at home — both sources 403 automated fetches but open fine in a browser):
1. Read **`fallout.wiki/wiki/LIP_File_Format`** (and the `falloutmods.fandom.com` mirror) for the exact header/magic/version and keyframe record layout.
2. **Hex-diff 2–3 extracted vanilla `.lip`** (Lazy Voice Finder extracts them, [02]) against the wiki spec to confirm Skyrim-SE specifics.
3. Cross-check **Runalip** / the **xVASynth fuz plugin** output for a known-good Skyrim `.lip` to validate your writer round-trips.

Record the confirmed byte map back into this section when you pin it (this doc is the home for it).

---

## 4. `.xwm` encoding (optional, shrinks disk)

- **Canonical tool: `xWMAEncode.exe`** (DirectX SDK / CK tools; **bundled inside Yakitori**). Tiny console exe → **runs reliably under Wine** (unlike FaceFXWrapper, this one is well-attested under Wine).
  ```
  wine xWMAEncode.exe out/line.wav out/line.xwm
  ```
- **ffmpeg does NOT produce Bethesda-valid xWMA.** It decodes xWMA fine but its encoder is unreliable/absent for in-game-valid output. **Never** use ffmpeg to *make* the in-game `.xwm`.
- **You can skip xwm entirely** — loose WAV (or WAV-inside-fuz) plays in-game, just larger on disk. MVP skips it; add it once the Wine path is proven (it's the same Wine plumbing the lip step may use).

---

## 5. Putting the per-line outputs together

For one line, depending on tier you end up with some of:
```
out/line.wav        # 44.1k/16/mono — playable as-is (Tier 0, no fuz)
out/line_16k.wav    # 16k/16/mono — lip input only
out/line.lip        # optional movement (Tier 1 Wine, or Tier 2 synthetic)
out/line.xwm        # optional, xWMAEncode/Wine
```
[04] packs `(.xwm or .wav) + optional .lip` into `line.fuz`, or you ship `line.wav` loose.

---

## 6. What "done" looks like

- Minimum: `line.wav` normalized to 44.1 kHz/16-bit mono — enough for the Tier-0 MVP.
- Movement: a `.lip` that the engine accepts and that makes the mouth move (Tier 1 or 2 confirmed in-game).
- Optional: `.xwm` via Wine.

---

### Sources
Lip: [Nukem9/FaceFXWrapper](https://github.com/Nukem9/FaceFXWrapper), [Runalip (Nexus SSE #98931)](https://www.nexusmods.com/skyrimspecialedition/mods/98931), [.lip/.fuz xVASynth plugin (Nexus SSE #55605)](https://www.nexusmods.com/skyrimspecialedition/mods/55605), [Silent Voice Generator (Nexus SSE #9124)](https://www.nexusmods.com/skyrimspecialedition/mods/9124), [FaceFX (Wikipedia — 42 phonemes, curve model)](https://en.wikipedia.org/wiki/FaceFX), LIP File Format wikis (fallout.wiki / falloutmods.fandom.com — read in browser). xwm: [Yakitori (Nexus #17765)](https://www.nexusmods.com/skyrimspecialedition/mods/17765), [Beyond Skyrim Voice Line Implementation](https://wiki.beyondskyrim.org/wiki/Arcane_University:Voice_Line_Implementation).
