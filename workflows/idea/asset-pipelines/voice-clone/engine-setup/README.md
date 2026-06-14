# 01 — Engine setup (Manjaro, 16 GB VRAM)

← [voice-clone](../README.md) · next: [02-voice-data.md](../02-voice-data.md)

Goal: get **one** engine turning *text + a reference voice* into a cloned WAV, behind a contract that lets you swap engines later. All engines fit 16 GB VRAM comfortably for **inference**; GPT-SoVITS *fine-tuning* also fits (with batch-size care).

## 各引擎

| 引擎 | 角色 | 檔 |
|------|------|----|
| **F5-TTS** | MVP primary（zero-shot，最少設定） | [f5.md](f5.md) |
| **Chatterbox** | MVP alt（emotion/tags、最輕） | [chatterbox.md](chatterbox.md) |
| **GPT-SoVITS** | fidelity/consistency upgrade（fine-tune） | [gptsovits.md](gptsovits.md) |
| **Fish Speech S2** | modern clone comparison | [fish-speech.md](fish-speech.md) |

---

## 0. The swappable engine contract

Every engine reduces to the same shell-callable shape. Plan all of them to satisfy this so ModForge (and your hand scripts) never special-case an engine beyond a name + a few knobs:

```
voicegen  --engine {f5|fish-s2|chatterbox|gptsovits}
          --ref     <reference.wav>          # zero-shot ref clip, OR a fine-tuned model dir
          --ref-text "<transcript of ref>"   # F5 needs this; Chatterbox/GPT-SoVITS optional
          --text    "<line to speak>"
          --out     <line.wav>
          [--seed N] [--exaggeration F] [--speed F] [--lang en]
```

Concretely this is a thin Python wrapper (`voicegen.py`) in a venv, dispatching on `--engine`. ModForge later shells out to exactly this (`MODFORGE_TTS_BIN`), the same way it shells out to the Papyrus compiler. Keep the wrapper engine-agnostic; per-engine quirks live inside it.

**Determinism:** all engines accept a seed. Pin it so a re-run of the same line reproduces the same audio (matters for caching in [05] — don't regenerate unchanged lines).

---

## 1. Manjaro prerequisites (once)

The cleanest path on Manjaro is to **not** rely on system CUDA matching each project; use per-project venvs with PyTorch's bundled CUDA wheels.

- **Driver:** `nvidia` / `nvidia-dkms` (you almost certainly already have it). Verify `nvidia-smi` shows the GPU and a CUDA runtime version.
- **Python:** Manjaro ships a recent Python; for isolation use **`uv`** (fast, 2026-standard) or **miniconda**. Recommendation: `uv` per engine — `uv venv && uv pip install ...`. Conda is fine if you prefer (GPT-SoVITS's installer assumes conda).
- **System libs frequently needed:** `ffmpeg` (audio I/O / resample), `sox` (optional, handy for normalization), `git`, `git-lfs` (model weights). `sudo pacman -S ffmpeg sox git git-lfs`.
- **PyTorch:** install the CUDA wheel matching a recent toolkit, e.g. `pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124` (bump `cu124`→ whatever is current; the wheel bundles its own CUDA, so system CUDA version is irrelevant). Verify: `python -c "import torch; print(torch.cuda.is_available())"` → `True`. *(Note: on Blackwell/RTX-50 the working wheel is **cu128**, not cu124 — see landed voice notes.)*

> One venv per engine avoids dependency conflicts (they pin different transformers/torch versions). Disk is cheap; cross-engine conflicts are not.

---

## 6. VRAM / fit summary (16 GB)

| Engine | Inference VRAM | Training? | Drift across many lines | Setup effort | Role |
|--------|---------------|-----------|-------------------------|--------------|------|
| **F5-TTS** | ~8 GB | none (zero-shot) | moderate | low | MVP primary |
| **Chatterbox** | 5–7 GB | none (zero-shot) | moderate; emotion knob | low | MVP alt (emotion/tags) |
| **GPT-SoVITS** | ≥6 GB (≥12 GB DPO) | yes, fits 16 GB | low (fine-tuned) | medium (train step) | fidelity/consistency upgrade |
| **Fish Speech S2** | model-dependent; S2 Pro is heavier | optional | expected low/moderate | medium/high | modern clone comparison |
| RVC (post-pass) | small | yes | — (stabilizes) | medium | optional max-fidelity recolor |

All comfortably within 16 GB. Nothing here forces cloud or quantization.

---

## 7. Rejected / deferred engines (and why, so you don't reconsider blindly)

- **xVASynth / xVATrainer** — the Skyrim-native, "*is* a known character" choice, but no documented **headless-Linux** recipe (Electron front-end is Windows; Python backend is *architecturally* runnable but undocumented as a driven service). Kept as an escape hatch if you specifically want a canonical vanilla-character timbre and are willing to reverse its backend. Not the default.
- **XTTS v2 (Coqui)** — easy zero-shot, but Coqui is defunct (weights/forks remain). F5/Chatterbox are healthier maintained equivalents. Use only if a fork proves more convenient.
- **Qwen3-TTS** — strong 2026 entrant; viable under the same contract if F5/Chatterbox/Fish disappoint. Not wired yet.
- **Piper** — Linux-native, CPU-fast, *robotic, no cloning*. Keep as an **instant placeholder** to validate the [03]/[04]/[06] plumbing without waiting on GPU/clone quality.
- **ElevenLabs** — best-in-class but cloud + ToS + cost. Personal-use fallback only; defeats the local-on-Manjaro goal.

---

## 8. What "done" looks like for this file

You can run `voicegen.py --engine f5 --ref ref.wav --ref-text "..." --text "Hello." --out hello.wav` and get an intelligible, on-voice WAV. That single capability unblocks [06]'s entire MVP. Everything after is packaging.

---

### Sources
F5-TTS: [SWivid/F5-TTS](https://github.com/SWivid/F5-TTS), [CLI docs (DeepWiki)](https://deepwiki.com/SWivid/F5-TTS/3.2-command-line-interface), [f5-tts PyPI](https://pypi.org/project/f5-tts/). Chatterbox: [resemble-ai/chatterbox](https://github.com/resemble-ai/chatterbox), [chatterbox-tts PyPI](https://pypi.org/project/chatterbox-tts/), [Chatterbox Turbo](https://www.resemble.ai/chatterbox-turbo/). GPT-SoVITS: [RVC-Boss/GPT-SoVITS](https://github.com/RVC-Boss/GPT-SoVITS). Fish Speech: [fishaudio/fish-speech](https://github.com/fishaudio/fish-speech), [Fish Audio self-hosted inference](https://docs.fish.audio/developer-guide/self-hosting/running-inference), [Fish Audio S2 technical report](https://arxiv.org/abs/2603.08823). Landscape comparisons: [BentoML 2026 OSS TTS](https://www.bentoml.com/blog/exploring-the-world-of-open-source-text-to-speech-models), [SiliconFlow voice-cloning 2026](https://www.siliconflow.com/articles/en/best-open-source-models-for-voice-cloning).
