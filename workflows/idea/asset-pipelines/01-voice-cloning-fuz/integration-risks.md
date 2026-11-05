# Voice-Cloning → Skyrim SE `.fuz` Voiced-Dialogue Pipeline — §8–9 ModForge 整合、風險與 MVP

← [入口](../01-voice-cloning-fuz.md)

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
