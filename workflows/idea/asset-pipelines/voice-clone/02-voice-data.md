# 02 — Voice data (extract vanilla audio, build the reference/dataset)

← [README](README.md) · prev: [01-engine-setup.md](engine-setup/README.md) · next: [03-lip-and-audio-encoding.md](03-lip-and-audio-encoding.md)

You can't clone a voice you don't have. This file gets you from "Skyrim BSAs on disk" to "a clean reference clip (zero-shot) or a clean dataset (GPT-SoVITS)" for one chosen voiceType. All steps are Linux-doable (native or Wine).

---

## 1. Pick the target voice

- **Vanilla voiceType** (e.g. `MaleNord`, `FemaleEvenToned`, `MaleEvenToned`) — abundant training data already on disk in the Voices BSAs; the MVP target. The NPC's `voiceType` EditorID is also the **second path segment** of the `.fuz` location ([04]), so picking it early pins your output paths.
- **Custom-follower voiceType** — a follower mod with its own voice type is ideal: clean, consistent, one speaker, often loose files. Great for a distinctive NPC.

Start with `MaleNord` for the MVP (lots of lines, easy to judge quality).

---

## 2. Extract audio on Linux

Vanilla voice lines are `.fuz` packed inside `Skyrim - Voices_en0.bsa` (and friends). Two routes:

**Route A — Lazy Voice Finder (fastest for assembling a set).** Nexus SSE #8619. Searches vanilla/mod voice files by **text or voice type**, plays/extracts them, and **auto-converts FUZ/XWM/MP3/OGG → WAV and extracts the `.lip`** *without* pre-unpacking BSA or fuz. Windows GUI → run under Wine. This is the path of least resistance to "give me 20 clean `MaleNord` lines as WAV".

**Route B — unpack + decode yourself (scriptable/bulk).**
1. **Unpack BSA:** B.A.E. (Bethesda Archive Extractor, Nexus #974, .NET GUI under Wine) or BSArch (xEdit toolset CLI, under Wine). Extract `Sound/Voice/.../<VoiceType>/*.fuz`.
2. **Decode `.fuz` → `.wav`:** the fuz layout is verified (see [04] / parent §1), so you can split natively. Options:
   - **`pwsh` (PowerShell Core, native on Linux)** running suglasp's fuz-split scripts — reads the 12-byte header in code, no Windows tools.
   - **Yakitori Audio Converter** (Nexus #17765, bundles `xWMAEncode.exe`) under Wine — fuz↔xwm↔wav GUI/batch.
   - **ffmpeg** for the `.xwm` → `.wav` decode half (ffmpeg *decodes* xWMA fine; it just can't *encode* valid xWMA — that limitation only bites in [03], not here).

Either route lands you a folder of `MaleNord/*.wav`. Memory: extraction is **not a blocker on Linux** — every piece is native-or-Wine.

---

## 3a. Zero-shot reference clip (F5 / Chatterbox)

You need *little*: one or a few **clean, single-speaker** clips.

- **F5-TTS:** 5–15 s clip **+ an exact transcript** of what's spoken in it. Pick a clip with neutral, clear delivery (avoid shouts/whispers/combat grunts). The transcript accuracy directly affects clone quality.
- **Chatterbox:** ~10 s+ clip, 24 kHz+, no transcript strictly required. Same "clean, neutral, one speaker" rule.

**Selection tips:** browse extracted lines, pick 2–3 candidate clips, generate the same test line from each, keep the ref that clones best. Trim silence and any music/SFX tails (Audacity under Wine, or `sox`/`ffmpeg` natively). No loudness normalization needed for the *reference* beyond removing clipping.

> Reference-clip quality dominates zero-shot results far more than line text does. Spend the 10 minutes to pick a good clip.

---

## 3b. Fine-tune dataset (GPT-SoVITS)

More data, more structure, one-time:

- **Quantity:** GPT-SoVITS can train on as little as ~1 min, but more clean audio → better. For a vanilla voiceType you have plenty; a few minutes of varied, clean lines is a good target. Avoid combat barks / overlapping SFX.
- **Format expectation (classic xVATrainer-style, also a safe GPT-SoVITS target):** mono WAV, ~22050 Hz, each clip ≤ ~10 s, **with a transcript**. GPT-SoVITS's WebUI bundles **slicing + ASR** tools that will segment long audio and auto-transcribe — so you can feed it longer concatenated audio and let it build the clip+transcript pairs. Verify/clean the ASR transcripts; bad transcripts poison training.
- **Normalization:** consistent sample rate, mono, peak-normalized, silence-trimmed. `sox`/`ffmpeg` batch:
  ```bash
  for f in raw/*.wav; do
    ffmpeg -i "$f" -ac 1 -ar 22050 -af "silenceremove=1:0:-50dB,loudnorm" "clean/$(basename "$f")"
  done
  ```
  (Tune the filter chain; the point is mono + fixed rate + trimmed + leveled.)

---

## 4. Audio rate cheat-sheet (which rate for what)

You will touch several sample rates; keep them straight (details in [03]):

| Purpose | Rate / format |
|---------|---------------|
| Zero-shot **reference** clip | engine-native is fine (F5 ~24 kHz, Chatterbox 24 kHz+); clean & mono |
| GPT-SoVITS **training** clips | 22050 Hz mono WAV + transcript |
| TTS **output** line | whatever the engine emits (e.g. 24 kHz) |
| **In-game** voice WAV/xwm | PCM 16-bit mono; 44.1 kHz/16-bit mono is the safe assumption |
| **`.lip` generation** input | **16 kHz, 16-bit, mono** (FaceFXWrapper requirement) |

So per line you typically produce: the engine's native WAV → a 44.1 kHz/16-bit mono copy for the game → a 16 kHz/16-bit mono copy *only if* generating lip. ([03] formalizes this.)

---

## 5. Where the data lives

Keep a tidy local layout (never committed — assets stay local per the legal rule):
```
~/voice-work/
  refs/        MaleNord_ref.wav  + MaleNord_ref.txt   (zero-shot)
  datasets/    MaleNord/clips/*.wav + transcripts     (GPT-SoVITS)
  models/      MaleNord_gptsovits/...                 (fine-tuned output)
  out/         per-line generated WAV/xwm/lip/fuz
```
ModForge's later `voicelines` step ([05]) references `refs/` or `models/` via the `voiceTemplate` spec field.

---

## 6. What "done" looks like

- **Zero-shot path:** `refs/MaleNord_ref.wav` (+ `.txt`) ready → feed straight into [01]'s `voicegen.py`.
- **Fidelity path:** `models/MaleNord_gptsovits/` trained → `--engine gptsovits --ref models/...`.

Either unblocks generating the 3 MVP lines in [06].

---

### Sources
Extraction/format: [CK Wiki "generate voice files by batch"](https://ck.uesp.net/wiki/How_to_generate_voice_files_by_batch), [Beyond Skyrim Voice Line Implementation](https://wiki.beyondskyrim.org/wiki/Arcane_University:Voice_Line_Implementation), Lazy Voice Finder (Nexus SSE #8619), B.A.E. (Nexus #974), Yakitori (Nexus #17765). Dataset/training: [RVC-Boss/GPT-SoVITS](https://github.com/RVC-Boss/GPT-SoVITS) (bundled slicing/ASR), [Beyond Skyrim xVASynth](https://wiki.beyondskyrim.org/wiki/Arcane_University:XVASynth) (22050 mono ≤10 s + transcript convention).
