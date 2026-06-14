# Engine C — GPT-SoVITS (fine-tune, fidelity/consistency upgrade)

← [engine-setup](README.md)

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
