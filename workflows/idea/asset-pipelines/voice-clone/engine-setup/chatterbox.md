# Engine B — Chatterbox (zero-shot, emotion control)

← [engine-setup](README.md)

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
