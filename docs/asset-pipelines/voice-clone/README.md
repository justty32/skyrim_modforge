# Voice-Clone → `.fuz` — Detailed Implementation Plan

← parent landscape survey: [../01-voice-cloning-fuz.md](../01-voice-cloning-fuz.md) · folder index: [../README.md](../README.md)

**This folder is the *implementation* plan** (the parent file is the *landscape survey*). It exists so that when you sit down at the home machine you can execute mostly without re-deciding. Research/planning only — no ModForge code has been touched.

**Plan date:** 2026-06-09. Author target machine: **Manjaro Linux, 16 GB VRAM NVIDIA GPU, CUDA, Wine/Proton available.** Personal single-player use only; generated voice assets are never redistributed.

---

## Locked decisions (from 2026-06-09 Q&A)

| Topic | Decision | Consequence for this plan |
|-------|----------|---------------------------|
| **Primary TTS engine** | **Layered** — zero-shot **F5-TTS** *or* **Chatterbox** for the MVP, **GPT-SoVITS** as the fidelity/consistency upgrade. | Engines sit behind **one swappable contract** (`text + reference → wav`). Start with no-training zero-shot; add a GPT-SoVITS fine-tune track later for NPCs that need tight voice consistency. See [01-engine-setup.md](01-engine-setup.md). |
| **Lip-sync depth** | **"Mouth moves anyhow is fine"** — accuracy not required, but the mouth should *move* (not be frozen). | Skip the accuracy fight. **Tier 1: FaceFXWrapper/Runalip under Wine** (gives correct movement *for free* if Wine cooperates). **Tier 2 backstop: synthetic envelope-driven `.lip`** written natively in C# (guaranteed Linux-native flapping). **Tier 0 baseline: no lip = static mouth** (always works). See [03-lip-and-audio-encoding.md](03-lip-and-audio-encoding.md). |
| **Scope of this plan** | **Both** — a standalone hand-run pipeline first, then the ModForge `voicelines` CLI step. | [06-standalone-runbook.md](06-standalone-runbook.md) is the copy-paste at-home runbook; [05-modforge-integration.md](05-modforge-integration.md) is the engineering design for folding it into the generator. |

---

## Document index

| File | What it covers | When you need it |
|------|----------------|------------------|
| [01-engine-setup.md](01-engine-setup.md) | Manjaro CUDA prereqs; install F5-TTS / Chatterbox / GPT-SoVITS; the swappable engine contract; VRAM budgets; tuning knobs; determinism. | First thing at home — get an engine producing a cloned WAV from text. |
| [02-voice-data.md](02-voice-data.md) | Extracting vanilla/follower voiceType audio on Linux; building the reference clip (zero-shot) vs the fine-tune dataset (GPT-SoVITS); normalization specs. | Right after install — you need a reference voice before you can clone. |
| [03-lip-and-audio-encoding.md](03-lip-and-audio-encoding.md) | The tiered `.lip` plan (none / Wine / synthetic C#); `.lip` format notes + decode plan; `.xwm` encoding; audio normalization. | When you want the mouth to move and/or want real `.fuz` instead of loose WAV. |
| [04-fuz-and-filenames.md](04-fuz-and-filenames.md) | Native C# `.fuz` writer (byte layout + sketch); the deterministic CK-matched filename rule and how to empirically pin it; on-disk paths; MO2 zip packaging. | When moving from "loose WAV" to packed `.fuz`, and whenever filenames matter (always). |
| [05-modforge-integration.md](05-modforge-integration.md) | Spec design (`voiceTemplate`, `NpcSpec.voiceTemplate`, `voiceLine`); the `voicelines` CLI step; `Generator.Build.Voice.cs`; shell-out + Wine plumbing; env vars; CODE_MAP/SPEC placement. | When the hand-run pipeline works and you want ModForge to drive it. |
| [06-standalone-runbook.md](06-standalone-runbook.md) | The exact at-home MVP: copy-paste commands, one NPC / `MaleNord` / 3 lines, end to end, verification, then progressive enhancement. | **Start here on day one.** It links back into 01–04 as needed. |

---

## The spine (what every tier shares)

```
ModForge emits INFO lines (text + voiceType)
        │
        ▼
[02] reference voice  ──►  [01] TTS engine  ──►  line.wav   (cloned, per line)
                                                  │
                                                  ├─ [03] normalize (mono/16-bit; 16 kHz copy for lip)
                                                  ├─ [03] (optional) .lip  ── tier 0/1/2
                                                  ├─ [03] (optional) .wav → .xwm  (xWMAEncode/Wine)
                                                  └─ [04] pack → .fuz  (native C#)  OR ship loose .wav
                                                            │
                                                            ▼
                                  Data/Sound/Voice/<Plugin>/<VoiceType>/<CK-name>.fuz
                                                            │
                                          [04] bundle into flat MO2 zip  ([05] package step)
```

**The one ModForge superpower:** because ModForge *assigns* the QUST EditorID, INFO FormID, and response index, it can compute the **exact CK-matched target filename deterministically, without the Creation Kit**. That is the single hardest part of the manual community workflow and it is free here. Everything else is plumbing around it. (See [04](04-fuz-and-filenames.md).)

---

## Build-up sequence (smallest proving slice first)

Each step proves one new thing and is independently testable. Do not add a tier until the previous one is confirmed in-game.

1. **Loose WAV, no lip, no xwm, no fuz.** One NPC, `MaleNord`, 3 short lines. Drop plain WAVs at the exact CK-style path. Proves **filename mapping + clone quality + packaging** — the whole spine — with **zero Wine dependency**. ([06](06-standalone-runbook.md))
2. **Pack `.fuz` (zero-lip).** Swap WAV → native-C#-written `FUZE` + version + `0x00000000` + xwm/wav. Proves the C# fuz writer. ([04](04-fuz-and-filenames.md))
3. **Add `.xwm`.** Encode via `xWMAEncode.exe` under Wine. Proves the Wine audio path and shrinks disk. ([03](03-lip-and-audio-encoding.md))
4. **Add lip (movement).** Tier 1 FaceFXWrapper/Runalip under Wine; if Wine fails, Tier 2 synthetic envelope `.lip`. ([03](03-lip-and-audio-encoding.md))
5. **GPT-SoVITS fidelity track.** For any NPC where the zero-shot clone drifts across many lines, fine-tune and switch that voice's engine. ([01](01-engine-setup.md), [02](02-voice-data.md))
6. **ModForge `voicelines` CLI step.** Fold the proven hand pipeline into the generator. ([05](05-modforge-integration.md))

---

## Risks, open questions, things to verify empirically at home

These are the points the plan *cannot* settle from a Windows desk — flagged so you test them deliberately rather than assume.

- **Filename rule must be byte-pinned.** A one-character mismatch = silent line, no error. Extract 2–3 vanilla `.fuz` filenames first and confirm ModForge's name generator reproduces every segment exactly. ([04](04-fuz-and-filenames.md) §"Pinning the rule"). Memory: [[vanilla-nif-paths-must-be-verified]] is the same class of "wrong path = invisible, no error" trap.
- **FaceFXWrapper under Wine is unconfirmed.** It loads CK DLLs in-memory via MemoryModule — the part most likely to break under Wine. Treat Tier 1 as "try it, ~15 min timebox"; fall straight to Tier 2 (synthetic) or Tier 0 (no lip) if it misbehaves. ([03](03-lip-and-audio-encoding.md))
- **`.lip` exact byte layout is not yet captured here** (the two authoritative wikis 403 automated fetches). If you go Tier 2, pin it at implementation by reading `fallout.wiki/wiki/LIP_File_Format` in a browser **and** hex-diffing a couple of extracted vanilla `.lip`. Known facts are recorded in [03](03-lip-and-audio-encoding.md) §"`.lip` format".
- **ffmpeg cannot produce Bethesda-valid `.xwm`.** Use `xWMAEncode.exe` (Wine) or ship loose WAV. Do not trust ffmpeg's xWMA *encoder*. ([03](03-lip-and-audio-encoding.md))
- **Zero-shot clones drift** across many lines / long lines. Budget a normalization + QA pass; the GPT-SoVITS fine-tune track exists precisely for voices where drift is unacceptable. ([01](01-engine-setup.md))
- **`FonixData.cdf`** (needed only if you ever use the CK/FaceFX path) is Bethesda property — copy from your own CK install, never redistribute.
- **xVASynth headless-on-Linux** stays *not chosen* (Linux headless recipe undocumented). Kept only as a "canonical character voice" escape hatch. ([01](01-engine-setup.md) §"Rejected/deferred").
- **MO2 reinstall reverts hand-patched files** — always rebuild into the zip, never hand-edit inside the MO2 mod folder. (Memory [[mo2-reinstall-reverts-manual-pex]].)
- **In-game test is manual** — you run the game yourself (memory [[ingame-test-workflow]]); the plan's structural checks (`*diag`, path/filename verification) come first, real MO2/Proton second.

---

## Legal / ethics guardrail

Personal, single-player, non-redistributed only. Do **not** publish cloned-voice assets — voice-actor and Bethesda rights apply, and `FonixData.cdf` is Bethesda property. Keep all generated audio local. This constraint is consistent across the whole asset-pipelines folder.

---

*Status: implementation plan drafted 2026-06-09 from 2026 web research + ModForge's existing capabilities. Engine APIs (F5-TTS CLI/Python, Chatterbox Python, GPT-SoVITS) and the `.fuz` layout are concrete; the inline-flagged items above need empirical confirmation on the home machine. Written in English to match the sibling reports 01–05; a zh-TW mirror can be added on request.*
