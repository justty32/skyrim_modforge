# Engine D — Fish Speech S2 (modern open clone backend)

← [engine-setup](README.md)

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
