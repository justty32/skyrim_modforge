# 01 — Engine setup (Manjaro, 16 GB VRAM)

← [README](README.md) · next: [02-voice-data.md](02-voice-data.md)

Goal of this file: get **one** engine turning *text + a reference voice* into a cloned WAV, behind a contract that lets you swap engines later. All three engines fit 16 GB VRAM comfortably for **inference**; GPT-SoVITS *fine-tuning* also fits (with batch-size care). 

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

**Determinism:** all three accept a seed. Pin it so a re-run of the same line reproduces the same audio (matters for caching in [05] — don't regenerate unchanged lines).

---

## 1. Manjaro prerequisites (once)

The cleanest path on Manjaro is to **not** rely on system CUDA matching each project; use per-project venvs with PyTorch's bundled CUDA wheels.

- **Driver:** `nvidia` / `nvidia-dkms` (you almost certainly already have it). Verify `nvidia-smi` shows the GPU and a CUDA runtime version.
- **Python:** Manjaro ships a recent Python; for isolation use **`uv`** (fast, 2026-standard) or **miniconda**. Recommendation: `uv` per engine — `uv venv && uv pip install ...`. Conda is fine if you prefer (GPT-SoVITS's installer assumes conda).
- **System libs frequently needed:** `ffmpeg` (audio I/O / resample), `sox` (optional, handy for normalization), `git`, `git-lfs` (model weights). `sudo pacman -S ffmpeg sox git git-lfs`.
- **PyTorch:** install the CUDA wheel matching a recent toolkit, e.g. `pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124` (bump `cu124`→ whatever is current; the wheel bundles its own CUDA, so system CUDA version is irrelevant). Verify: `python -c "import torch; print(torch.cuda.is_available())"` → `True`.

> One venv per engine avoids dependency conflicts (they pin different transformers/torch versions). Disk is cheap; cross-engine conflicts are not.

---

## 2. Engine A — F5-TTS (zero-shot, MVP primary)

Flow-matching DiT TTS; clones from a **5–15 s** reference clip, no training. English+Chinese base model. ~8 GB VRAM for comfortable speed — fine on 16 GB. Clean Python, first-class CLI. **This is the recommended MVP engine: least setup, scriptable immediately.**

**Install:**
```bash
uv venv f5 && source f5/bin/activate
uv pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124
uv pip install f5-tts
```

**One-line inference (CLI):**
```bash
f5-tts_infer-cli \
  --model F5TTS_v1_Base \
  --ref_audio  ref_malenord.wav \
  --ref_text   "transcript of the reference clip exactly as spoken." \
  --gen_text   "Stop right there, criminal scum." \
  --output_dir out/
```

**Python API (what `voicegen.py` calls):**
```python
from f5_tts.api import F5TTS
f5 = F5TTS()                      # loads F5TTS_v1_Base by default
wav, sr, _ = f5.infer(
    ref_file="ref_malenord.wav",
    ref_text="transcript of the reference clip ...",
    gen_text="Stop right there, criminal scum.",
    file_wave="out/line.wav",
    seed=12345,
)
```

**Gotchas / knobs:**
- `ref_text` must match the reference clip's spoken words closely — wrong transcript degrades the clone.
- It supports a **TOML config + multi-voice batch** mode (`-c custom.toml`, `[voices.*]` tags) — useful later for batching a whole NPC's lines in one process load instead of paying model-load per line.
- Keep reference clips clean, single-speaker, no music/SFX (see [02]).

---

## 3. Engine B — Chatterbox (zero-shot, emotion control)

Resemble AI, **MIT license**, 0.5B params, **5–7 GB VRAM**. Zero-shot clone from ~10 s ref. Distinguishing feature: **emotion-exaggeration control** (`exaggeration`) and a `cfg_weight` pacing knob — useful for dramatic NPC delivery. A **Turbo** variant (350M, 1-step) is far faster and supports paralinguistic tags like `[laugh]`/`[cough]`. Blind tests favored it over ElevenLabs.

**Install:**
```bash
uv venv chatterbox && source chatterbox/bin/activate
uv pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124
uv pip install chatterbox-tts
```

**Python API:**
```python
import torchaudio as ta
from chatterbox.tts import ChatterboxTTS
model = ChatterboxTTS.from_pretrained(device="cuda")
wav = model.generate(
    "Stop right there, criminal scum.",
    audio_prompt_path="ref_malenord.wav",   # the voice to clone
    exaggeration=0.5,                         # 0=flat … higher=more dramatic
    cfg_weight=0.5,                           # pacing/adherence
)
ta.save("out/line.wav", wav, model.sr)        # model.sr is the output sample rate
```
(Turbo: `from chatterbox.tts import ChatterboxTurboTTS; ChatterboxTurboTTS.from_pretrained(...)`.)

**Reference clip:** ≥10 s, WAV, 24 kHz+, single speaker, no background noise. Note Chatterbox outputs at `model.sr` (24 kHz-class) — you will downsample for the game/lip steps in [03].

**When to pick Chatterbox over F5:** want emotion exaggeration / paralinguistic tags, or want the lightest/fastest zero-shot. Both are MVP-grade; the contract makes A/B trivial — generate the same 3 lines with each, keep whichever clones your chosen voiceType better.

---

## 4. Engine C — GPT-SoVITS (fine-tune, fidelity/consistency upgrade)

Few-shot: usable clone from ~10–60 s, **best similarity-per-minute** and the Skyrim-community fidelity choice. The win over zero-shot is **consistency across many lines** — a fine-tuned model won't drift the way a single ref clip can. Cost: a one-time training step per voice.

**VRAM:** inference ≥6 GB (RTX 3060+ recommended); the DPO training option wants ≥12 GB — all fine on 16 GB. System RAM ≥16 GB (32 GB nicer for training); reserve ~20 GB disk. If VRAM is tight during GPT-stage training, **reduce `batch_size` first** (it's the dominant VRAM lever).

**Install:** clone `RVC-Boss/GPT-SoVITS`; it ships an installer/conda env and a WebUI for the full **dataset-prep → train → infer** loop, plus `api.py`/`api_v2.py` for headless HTTP inference.
```bash
git clone https://github.com/RVC-Boss/GPT-SoVITS && cd GPT-SoVITS
# follow its install_*.sh / conda env; it bundles ASR + slicing tools for dataset prep
```

**Two phases:**
1. **Train (one-time per voice, WebUI):** feed the cleaned voiceType dataset ([02]); it slices, ASR-transcribes, and fine-tunes a GPT + SoVITS weight pair. Inputs ≈ short clean clips + transcripts. Reduce `batch_size` if OOM.
2. **Infer (scripted):** drive `api_v2.py` over HTTP from `voicegen.py` — POST `{text, ref_audio, ...}`, get WAV. This is the `--engine gptsovits` branch; `--ref` points at the fine-tuned weights + a short prompt clip rather than a raw reference.

**Optional max-fidelity stage: TTS → RVC.** RVC (audio→audio, Linux+CUDA, mature) recolors any TTS output toward a target timbre and *stabilizes* it. The community's top-fidelity pattern is "TTS then RVC". Treat as a later `--rvc-model` post-pass on top of any engine, not a primary engine. Defer until you've decided base quality is the limiter.

---

## 5. Engine D — Fish Speech S2 (modern open clone backend)

Fish Audio S2 is a newer open TTS family with voice cloning, long-form/multispeaker ambitions, and
an SGLang-oriented inference path. Treat it as a heavier but promising engine for important NPCs.
The ModForge-side integration is intentionally a thin wrapper: `voicegen.py --engine fish-s2`
forwards to `MODFORGE_FISH_SPEECH_BIN`, which must write the requested WAV.

**Install sketch:**

```bash
uv venv fish-speech && source fish-speech/bin/activate
uv pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124
uv pip install fish-speech
```

Download the model/codec weights per the Fish Audio docs. For S2 Pro, use the local model directory
as `voiceTemplates[].modelPath`.

**ModForge contract wrapper:**

`MODFORGE_FISH_SPEECH_BIN` should accept:

```bash
fish-s2-wrapper \
  --text "Line to speak." \
  --out out.wav \
  --ref-audio refs/voice.wav \
  --ref-text "Reference transcript." \
  --model models/fish-s2-pro \
  --seed 1234 \
  --language en
```

The wrapper can drive Fish's official CLI, HTTP API, or SGLang server. Keep that detail outside
ModForge so the `.fuz` pipeline does not change when Fish changes its runtime layout.

**Spec example:**

```json
{
  "id": "SeranaFish",
  "engine": "fish-s2",
  "referenceWav": "refs/serana_ref.wav",
  "referenceText": "Keep your eyes open.",
  "modelPath": "models/fish-s2-pro",
  "language": "en",
  "seed": 1234
}
```

**When to pick Fish S2:** when F5 sounds too flat or drifts on longer emotional dialogue and you
want to compare a newer, higher-capacity model. Keep F5 as the fast baseline until Fish is installed
and measured on the same 5-10 Skyrim lines.

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
