# Voice-Cloning → Skyrim SE `.fuz` Voiced-Dialogue Pipeline — §1–3 格式、素材、TTS 引擎

← [入口](../01-voice-cloning-fuz.md)

## 1. The `.fuz` format and what Skyrim needs per voiced line

**`.fuz` is a container: `.xwm` audio + an optional `.lip` lipsync blob.** Verified binary layout (read from suglasp's `convert_fuz_to_xwm.ps1`, which parses fuz in code — authoritative and reimplementable):

| Offset | Bytes | Field |
|--------|-------|-------|
| 0 | 4 | Magic `FUZE` (ASCII) |
| 4 | 4 | Version / unknown |
| 8 | 4 | `FuzLipSize` — uint32, size of the lip section |
| 12 | `FuzLipSize` | `.lip` data (omitted if size == 0) |
| 12 + FuzLipSize | rest | `.xwm` audio stream |

So `xwmDataLen = fileLength − 12 − FuzLipSize`. **If `FuzLipSize == 0`, the xWMA data sits immediately after the 12-byte header** — a lip-less `.fuz` is trivially constructible (12-byte header + raw xwm). Most important fact for the MVP: you can ship voiced dialogue *without lipsync* by writing 12 bytes + the xwm.

**On-disk path convention:** `Data/Sound/Voice/<PluginName.esp>/<VoiceType>/<filename>.fuz`. First sub-folder = the **plugin filename exactly** (e.g. `MyMod.esp`); second = the **voice type EditorID** (e.g. `MaleNord`).

**Filename convention** (confirmed): `(Quest)_(Topic)_(HexBaseID)_(LineNumber)` — e.g. `MyQuest_MyTopic_000113C9_1.fuz`. It encodes the parent quest EditorID, the topic/INFO context, the **8-digit hex FormID of the INFO/response record**, and a **1-based response index** within the INFO. **CK generates these names automatically and they cannot be changed** — the audio file must match exactly or the engine won't play it.

**Implication for ModForge:** Because ModForge *is* the generator emitting the QUST/DIAL/INFO records via Mutagen, it already knows the quest EditorID, the INFO FormID it assigned, and the response index — so **it can compute the exact target filename deterministically without the Creation Kit**. The hardest part of the manual workflow (CK-matched filenames) is free for you.

*Uncertainty:* the precise quest/topic string-segment formatting (truncation, casing, blank-topic behavior) is not byte-verified. Safe move: **extract a few vanilla `.fuz` filenames and reverse-engineer the exact rule** before trusting ModForge's name generation.

---

## 2. Source material extraction (vanilla + mod-follower voices as training data)

Vanilla voice lines are themselves `.fuz`, packed in BSAs (`Skyrim - Voices_en0.bsa` etc.).

**BSA extraction:**
- **B.A.E. — Bethesda Archive Extractor** (Nexus SSE #974) — extracts `.bsa`/`.ba2`. Windows .NET GUI; **runs under Wine**. (Archived copy on archive.org.)
- **Linux-ish alternative:** **BSArch** (xEdit toolset) CLI, runs under Wine; pure-Python BSA readers exist. (`b2a` could not be confirmed as a current tool — treat as unverified.)

**Locating lines by voice type:** **Lazy Voice Finder** (Nexus SSE #8619) lists vanilla/mod voice files searchable by **text or voice type**, plays/extracts them, and **converts FUZ/XWM/MP3/OGG → WAV automatically and extracts the `.lip`**, *without* pre-unpacking BSA or fuz. Fastest way to assemble a training set for one voice type. Windows GUI → Wine.

**Batch `.fuz` → `.wav`:**
- **Yakitori Audio Converter** (Nexus SSE #17765) — fuz↔xwm↔wav; **bundles `xWMAEncode.exe`**; with ffmpeg covers more formats. GUI, Wine-able.
- **fuz_extractor** + **xWMAEncode** — classic two-step.
- **suglasp's PowerShell scripts** — read fuz natively in code, no third-party tools; run under **PowerShell Core (`pwsh`) natively on Linux**, small enough to reimplement in C# inside ModForge. **BmlFuzDecode** / **unfuzer** also bulk-decode (Windows).

**Mod-follower voices:** same mechanics — the follower either ships loose `Data/Sound/Voice/<Plugin>/<VoiceType>/*.fuz` (copy directly) or a BSA (unpack with BAE first), then batch-decode to WAV. A custom follower with a **custom voice type** is ideal training data.

**Linux paths:** all decoding doable via `pwsh` (native, fuz-split) + ffmpeg (xwm→wav), or BAE/Yakitori/LazyVoiceFinder under Wine. Not a blocker.

---

## 3. Voice-cloning / TTS engines (2026 state of the art)

Separate **TTS** (text→speech, what you need since ModForge produces *text*) from **VC** (RVC: audio→audio, needs source audio).

| Engine | Clone from few min? | Linux/GPU | TTS vs VC | Char-voice quality | Notes |
|--------|--------------------|-----------|-----------|------------------------|-------|
| **xVASynth v3 / SKVA Synth** (Nexus #44184; GH DanRuta/xVA-Synth, GPL-3.0) | No (pre-trained per-character; new voice needs xVATrainer + dataset) | Backend = **Python + ffmpeg, CUDA toggle**; Electron front-end is Windows but **Python backend is Linux-runnable** | TTS (+VC mode) | Purpose-built for game voices; per-letter pitch/duration/energy/emotion | **The Skyrim-native choice.** |
| **xVATrainer** | Fine-tunes FastPitch1.1 + HiFi-GAN; dataset = **22050 Hz mono WAV ≤~10 s + transcript** | Python, GPU | training | — | How you *make* a new xVASynth voice from extracted vanilla audio. Needs more than "a few minutes." |
| **RVC** | ~10 min–1 hr clean audio | **Linux + CUDA**, mature | **VC only** (needs source audio) | Excellent timbre transfer | **Cloning workhorse.** Pattern: TTS first, then RVC recolors. |
| **XTTS v2 (Coqui)** | **Yes — zero/few-shot, ~6–30 s ref** | Linux + GPU (~2–3 GB) | TTS w/ cloning | Good, multilingual | Easiest text→cloned-voice in one step. Company gone, weights/forks remain. |
| **F5-TTS** | **Yes — zero-shot short ref** | Linux + GPU | TTS (DiT) | High, natural | Strong modern option, clean Python. |
| **GPT-SoVITS** (GH RVC-Boss) | **Yes — "1 min" few-shot**, zero-shot timbre too | Linux + CUDA | TTS (+ASR/segmentation tooling) | Very high similarity | **Best similarity-per-minute;** bundles dataset-prep tools. |
| **StyleTTS2** | Few-shot | Linux + GPU | TTS | High naturalness | Finickier setup than F5/XTTS. |
| **Bark** | Not really | Linux + GPU | TTS | Expressive but unstable | **Not recommended** (non-deterministic). |
| **Piper** | No (fixed voices) | **Linux-native, CPU-fast** | TTS | Robotic | Good **placeholder/MVP** to validate plumbing. |
| **ElevenLabs** | Yes, excellent | Cloud (HTTP) | TTS+cloning | Best-in-class | Cloud, no GPU; cost+ToS, fine personal. **Fallback.** |

**Recommendation:**
- **Primary: GPT-SoVITS** (or **F5-TTS**) for text→cloned-voice in one step on Linux/CUDA, conditioned on extracted vanilla voice-type WAVs.
- **Skyrim-canonical path: xVASynth v3** (Python backend on Linux) when you want a model that already *is* a known character, fine-tuned via xVATrainer.
- **Max fidelity: TTS → RVC** two-stage. What the Skyrim community converged on.
- **Fallback:** ElevenLabs (cloud) or Piper (instant placeholder).

*Uncertainty:* no 2026 source confirms a documented headless-Linux xVASynth recipe; architecturally runnable but you may drive the Python backend directly. GPT-SoVITS/F5/XTTS are the safer Linux-native bets.
