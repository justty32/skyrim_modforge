# Voice-Clone → `.fuz` — Detailed Implementation Plan

← parent landscape survey: [../01-voice-cloning-fuz.md](../01-voice-cloning-fuz.md) · folder index: [../README.md](../README.md)

**This folder started as the *implementation* plan** (the parent file is the *landscape survey*).
As of 2026-06-12, the core ModForge `voicelines` path exists: it can inspect built INFOs, plan
speaker/template/output paths, shell out to a local TTS wrapper, encode with Wine `xWMAEncode.exe`,
and write `.fuz` loose assets. The remaining work is real model setup, lip tooling, quality QA, and
Skyrim/Proton in-game confirmation.

**Plan date:** 2026-06-09. Author target machine: **Manjaro Linux, 16 GB VRAM NVIDIA GPU, CUDA, Wine/Proton available.** Personal single-player use only; generated voice assets are never redistributed.

---

## Locked decisions (from 2026-06-09 Q&A)

| Topic | Decision | Consequence for this plan |
|-------|----------|---------------------------|
| **Primary TTS engine** | **Layered** — zero-shot **F5-TTS** *or* **Chatterbox** for the MVP, **GPT-SoVITS** as the fidelity/consistency upgrade. | Engines sit behind **one swappable contract** (`text + reference → wav`). Start with no-training zero-shot; add a GPT-SoVITS fine-tune track later for NPCs that need tight voice consistency. See [01-engine-setup.md](engine-setup/README.md). |
| **Lip-sync depth** | **"Mouth moves anyhow is fine"** — accuracy not required, but the mouth should *move* (not be frozen). | Skip the accuracy fight. **Tier 1: FaceFXWrapper/Runalip under Wine** (gives correct movement *for free* if Wine cooperates). **Tier 2 backstop: synthetic envelope-driven `.lip`** written natively in C# (guaranteed Linux-native flapping). **Tier 0 baseline: no lip = static mouth** (always works). See [03-lip-and-audio-encoding.md](03-lip-and-audio-encoding.md). |
| **Scope of this plan** | **Both** — a standalone hand-run pipeline first, then the ModForge `voicelines` CLI step. | [06-standalone-runbook.md](06-standalone-runbook.md) is the copy-paste at-home runbook; [05-modforge-integration.md](05-modforge-integration.md) is the engineering design for folding it into the generator. |

---

## Document index

| File | What it covers | When you need it |
|------|----------------|------------------|
| [01-engine-setup.md](engine-setup/README.md) | Manjaro CUDA prereqs; install F5-TTS / Chatterbox / GPT-SoVITS; the swappable engine contract; VRAM budgets; tuning knobs; determinism. | First thing at home — get an engine producing a cloned WAV from text. |
| [02-voice-data.md](02-voice-data.md) | Extracting vanilla/follower voiceType audio on Linux; building the reference clip (zero-shot) vs the fine-tune dataset (GPT-SoVITS); normalization specs. | Right after install — you need a reference voice before you can clone. |
| [03-lip-and-audio-encoding.md](03-lip-and-audio-encoding.md) | The tiered `.lip` plan (none / Wine / synthetic C#); `.lip` format notes + decode plan; `.xwm` encoding; audio normalization. | When you want the mouth to move and/or want real `.fuz` instead of loose WAV. |
| [04-fuz-and-filenames.md](04-fuz-and-filenames.md) | Native C# `.fuz` writer (byte layout + sketch); the deterministic CK-matched filename rule and how to empirically pin it; on-disk paths; MO2 zip packaging. | When moving from "loose WAV" to packed `.fuz`, and whenever filenames matter (always). |
| [05-modforge-integration.md](05-modforge-integration.md) | Spec design (`voiceTemplate`, `NpcSpec.voiceTemplate`, `voiceLine`); the `voicelines` CLI step; `Generator.Build.Voice.cs`; shell-out + Wine plumbing; env vars; CODE_MAP/SPEC placement. | Historical design + cross-check; implementation now lives in the repo. |
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
5. **GPT-SoVITS fidelity track.** For any NPC where the zero-shot clone drifts across many lines, fine-tune and switch that voice's engine. ([01](engine-setup/README.md), [02](02-voice-data.md))
6. **ModForge `voicelines` CLI step.** Implemented structurally. Use `voicediag` / `voicelines --plan`
   first, then generate. Remaining work: real TTS model install + in-game playback confirmation.
   ([05](05-modforge-integration.md))

---

Risks, open questions + legal guardrail → [risks-gotchas.md](risks-gotchas.md)

*Status: plan drafted 2026-06-09; ModForge integration partially landed by 2026-06-12. Fake-TTS +
real xWMA `.fuz` packaging is structurally verified; real TTS model setup, FaceFX/lip behavior, and
Skyrim/Proton playback still need empirical confirmation on the home machine.*
