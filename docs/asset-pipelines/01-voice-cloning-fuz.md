# Voice-Cloning → Skyrim SE `.fuz` Voiced-Dialogue Pipeline

← index: [README.md](README.md) · related: [IDEAS.md §1](../IDEAS.md) (voice prereq), memory `voice-gen-interface-future`

**Research date:** 2026-06-08. Target: Skyrim SE on Manjaro Linux, MO2 under Proton, CUDA GPU available, Wine/Proton usable, **personal single-player use only**.

**Confidence note:** The file-format facts (§1, §5, §6) and the FaceFXWrapper / xVASynth tool facts (§3, §4) are well-corroborated; the `.fuz` layout is confirmed from source code. The biggest *uncertainty* is **Linux-native lip generation** — FaceFXWrapper is Windows-only and the `.lip` step is the one place the pipeline hits a wall (§4). Extrapolations are flagged inline.

**Status update 2026-06-12:** the core ModForge side has landed structurally. Current code has
`voiceTemplates[]`, `npcs[].voiceTemplate`, `voiceLine`, `voicediag`, `voicelines --plan`, a local
TTS wrapper contract, Wine `xWMAEncode.exe` path conversion, and a native `.fuz` writer. Fake TTS +
real xWMAEncode produced valid `.fuz` files locally. Real model setup, lip generation, QA, and
Skyrim/Proton in-game playback remain open.

---

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

---

## 4. The `.lip` lipsync problem — **this is the wall**

**Generator:** **FaceFXWrapper** (GH Nukem9/FaceFXWrapper). CLI:
```
FaceFXWrapper Skyrim USEnglish FonixData.cdf in.wav resampled.wav out.lip "the spoken text"
```
Expects (if pre-resampled) **16 kHz, 16-bit, mono WAV**, and **requires `FonixData.cdf`** (not shipped — copy from CK at `Data/Sound/Voice/Processing/FonixData.cdf`, also mirrored on Nexus).

**Linux/headless status — the blocker:** FaceFXWrapper is **Windows-only** (VS solution, loads CK DLLs in-memory via MemoryModule, no CMake/Wine testing documented). Best bet: **run under Wine** — small console exe doing pure computation, *good chance* it works, but **unconfirmed**; must be tested. Higher-level wrappers that bundle it and auto-pack fuz: the **xVASynth `.lip`/`.fuz` plugin** (Nexus #55605) and **Runalip** (Nexus #98931, batch `.lip` from `.wav`/`.fuz`). Same Wine caveat.

**Missing/generic `.lip`:** A `.fuz` with `FuzLipSize == 0` (or a loose `.wav`) **plays audio fine — the mouth just doesn't move.** Fully audible/functional; lipsync is cosmetic. This is why the MVP can defer lip entirely. (See also "SSE CK Fonixdata Lip Sync Fix" Nexus #40971 if generating lip via CK.)

**Bottom line:** lipsync is the only step with no clean Linux-native tool. Plan A = FaceFXWrapper/Runalip under Wine (verify); Plan B = skip lip (zero-size, mouth static); Plan C = generate lip on a Windows/CK box once and reuse.

---

## 5. `.xwm` encoding

- **`xWMAEncode.exe`** (DirectX SDK / CK tools) — canonical `.wav`↔`.xwm`. Tiny console exe → **runs reliably under Wine**, and is **bundled inside Yakitori Audio Converter**.
- **ffmpeg + xWMA:** can *decode* but its *encoder* is unreliable/absent for Bethesda-valid xWMA. **Do not rely on ffmpeg to produce in-game xwm.** Use xWMAEncode (Wine).
- **Does plain `.wav` work in-game?** **Yes** — WAV/XWM/FUZ all play as voice files. A loose `.wav` at the correct path works (no lip → no mouth movement), just larger on disk. **MVP shortcut: skip both xwm encoding and fuz packing — drop WAVs at the right paths.** Use PCM 16-bit (44.1 kHz/16-bit mono is the safe assumption).

---

## 6. Assembling `.fuz`

- **Tools:** Yakitori (Wine, bundles xWMAEncode), Unfuzer, BmlFuzEncoder/Decode, xVASynth fuz plugin, Runalip. All Windows → Wine.
- **Scriptable on Linux? Yes — skip the tools entirely.** Given the verified layout, packing is trivial: `"FUZE"` + 4 version bytes + uint32 lip size + (lip bytes) + xwm bytes. **ModForge can emit `.fuz` natively in C# in ~20 lines** — same as it handles binary records via Mutagen. Lip-less MVP: `FUZE` + version + `0x00000000` + xwm. This removes the Wine fuz-tool dependency.

---

## 7. End-to-end proposed pipeline

From **"ModForge emitted N dialogue lines for NPC X (voiceType Y)"**:

1. **(Auto)** ModForge knows per INFO: quest EditorID, INFO FormID, response index, line **text**, NPC **voiceType** → compute target filename + path. *(Verify exact name rule against a vanilla extraction first.)*
2. **(One-time, semi-manual)** Build a **voice model for voiceType Y**: extract vanilla (or follower) audio (BAE/Lazy Voice Finder → WAV), prep dataset, train/condition GPT-SoVITS (or fine-tune xVASynth, or register an XTTS/F5 reference clip). **Reusable across all future lines for that voice.**
3. **(Auto, GPU)** Per line: TTS text → `line.wav`. (Optional RVC second pass.)
4. **(Auto)** Normalize (mono, 16-bit; resample copy to 16 kHz for lip).
5. **(Auto, Wine)** `.wav`→`.xwm` via xWMAEncode. *(MVP: skip — keep WAV.)*
6. **(Semi — the wall)** `.lip` via FaceFXWrapper/Runalip under Wine. *(MVP: skip — zero lip.)*
7. **(Auto, native C#)** Pack `.xwm`(+optional `.lip`)→`.fuz`. *(MVP: zero-lip fuz, or place WAV.)*
8. **(Auto)** Place at path from step 1. Voice files are loose assets, not embedded in the plugin:
   either run `voicelines` against the plugin inside the final mod folder, or feed the generated
   staging directory to `package --assets <dir>`.

**Fully automatable:** 1, 3, 4, 5, 7, 8. **One-time human:** 2 (build the voice model). **The wall:** 6 (`.lip`, Wine-dependent, skippable). An end-to-end *audible* pipeline is fully automatable on Linux today; *lipsync* is the only contingent piece.

---

## 8. ModForge integration points

Aligned to existing `sounds[]` (SNDR + copy wav/xwm), `package` (copy asset dir), shell-out (Papyrus compiler, xLODGen):

- **Spec concept `voiceTemplate`** (per voiceType or per character): currently implemented with engines such as `f5` and `fish-s2` wired through the local `voicegen.py` contract; `chatterbox`/`gptsovits`/`xtts` remain accepted/reserved names until their wrappers are provided.
- **`NpcSpec.voiceTemplate`** (or map `voiceType → voiceTemplate` globally) so each NPC's lines route to the right model.
- **INFO lines already carry text** — no new field for words; optional `voiceLine: { skipLip, format: "wav"|"xwm"|"fuz" }`.
- **New CLI step `voicelines`** (sibling to `compile`/`package`): walk emitted INFO records, compute filenames (deterministic — ModForge owns the FormIDs, *the big advantage over CK*), shell out to the TTS engine (venv binary path, like `~/tools/papyrus-compiler`), then xWMAEncode/FaceFXWrapper **under Wine via the existing Wine plumbing**, then pack `.fuz` with a **native C# fuz writer** (new `Generator.Build.Voice.cs`), drop files under `Data/Sound/Voice/<plugin>/<voicetype>/`.
- **`package`** copies `Sound/...` when supplied through `--assets` or `spec.assets`; it does not
  automatically discover voice output from another build directory. No `.seq` interaction.
- **Tooling config:** `MODFORGE_TTS_BIN`, `MODFORGE_FACEFX` (+ `FonixData.cdf`), `MODFORGE_XWMAENCODE`.
  Missing xWMA/lip tools degrade to `.wav` or no lip; missing TTS blocks generation but `voicediag`
  and `voicelines --plan` still work.

---

## 9. Risks, gotchas, MVP

**Risks / gotchas:**
- **`.lip` on Linux is unverified** — don't gate the feature on it.
- **Filename rule must be empirically pinned** — a one-character mismatch = silent no audio. Extract 2–3 vanilla `.fuz` names, confirm ModForge reproduces segments exactly.
- **ffmpeg ≠ valid xWMA** — use xWMAEncode (Wine) or ship WAV.
- **FonixData.cdf** — copy from your own CK, don't redistribute.
- **xVASynth headless-on-Linux** unconfirmed; prefer GPT-SoVITS/F5/XTTS.
- **Voice consistency** — few-shot clones drift; budget a normalization/QA pass; RVC-on-top stabilizes timbre.
- **MO2 reinstall reverts manually-patched files** (memory `mo2-reinstall-reverts-manual-pex`) — always rebuild into the zip.

**MVP — smallest proving slice:**
1. One existing test NPC, vanilla voiceType (e.g. `MaleNord`), 3 short lines ModForge already emits.
2. Extract ~10–20 `MaleNord` vanilla lines (Lazy Voice Finder → WAV) as the clone reference.
3. Clone with GPT-SoVITS (or XTTS v2) → 3 WAVs.
4. **Skip xwm, skip lip:** drop 3 plain `.wav` at `Data/Sound/Voice/<plugin>.esp/MaleNord/<exact CK-style name>.wav`, bundle in the flat zip.
5. In-game (manual MO2/Proton): confirm audio plays with subtitles, mouth static. **Proves filename-mapping + clone quality + packaging — the whole spine — with zero Wine dependency.**
6. **Then add fuz:** swap WAV→native-C#-written zero-lip `.fuz`.
7. **Then add lip:** get FaceFXWrapper/Runalip working under Wine. Only step that might force a Windows/CK fallback.

**Legal:** personal, single-player, non-redistributed only. Do not publish cloned-voice assets (voice-actor/Bethesda rights; `FonixData.cdf` is Bethesda property). Keep generated assets local.

---

### Key verified sources
- `.fuz` layout: suglasp `convert_fuz_to_xwm.ps1`; Fallout Wiki FUZ File.
- Filenames/paths/WAV-works: CK Wiki "How to generate voice files by batch"; Beyond Skyrim "Voice Line Implementation".
- Lip: GH Nukem9/FaceFXWrapper; Runalip (Nexus #98931); xVASynth lip/fuz plugin (#55605); CK Fonixdata Lip Sync Fix (#40971).
- xwm/fuz: Yakitori (#17765), recursive wav→xwm (#16763), Xwm Ninja.
- Extraction: BAE (#974), Lazy Voice Finder (#8619).
- TTS/VC: GH DanRuta/xVA-Synth + xva-trainer; xVASynth v3 (#44184); GH RVC-Boss/GPT-SoVITS; F5-TTS, XTTS v2/Coqui, RVC; Mantella docs (Piper/xVASynth/XTTS).
