# 06 — Standalone at-home runbook (start here)

← [README](README.md) · prev: [05-modforge-integration.md](05-modforge-integration.md)

The copy-paste path for the home machine (Manjaro, 16 GB VRAM). No ModForge code changes — this proves the pipeline by hand first ([05] automates it afterward). Each step proves one thing; don't advance until the current one works. Links jump to the detail docs when you need the *why*.

> Before you start, have ready: a built test plugin (`.esp`) from ModForge with **one NPC** whose `VoiceType` is `MaleNord` and **3 short dialogue lines**; MO2 + Skyrim SE under Proton; the Skyrim Voices BSA on disk.

---

## Step 0 — Environment sanity (~10 min)

```bash
nvidia-smi                       # GPU + CUDA runtime visible
python -c "import torch; print(torch.cuda.is_available())"   # after installing torch in a venv → True
sudo pacman -S --needed ffmpeg sox git git-lfs
```
Install one engine venv ([01] §2 — F5-TTS recommended):
```bash
uv venv ~/voice-work/f5 && source ~/voice-work/f5/bin/activate
uv pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124
uv pip install f5-tts
```
**Proves:** GPU + engine import. → detail: [01](01-engine-setup.md).

---

## Step 1 — Get a reference voice (~20 min)

Extract ~10–20 clean `MaleNord` lines as WAV and pick one good 5–15 s clip + its transcript.
- Easiest: **Lazy Voice Finder** under Wine → search voiceType `MaleNord` → export WAV ([02] §2 Route A).
- Save `~/voice-work/refs/MaleNord_ref.wav` and write its exact spoken words to `~/voice-work/refs/MaleNord_ref.txt`.

**Proves:** you have a clonable reference. → detail: [02](02-voice-data.md).

---

## Step 2 — Generate the 3 lines (~10 min)

```bash
source ~/voice-work/f5/bin/activate
REF=~/voice-work/refs/MaleNord_ref.wav
RT="$(cat ~/voice-work/refs/MaleNord_ref.txt)"
mkdir -p ~/voice-work/out
i=1
for LINE in "Halt! You've violated the law." "Pay the fine or it's off to jail." "Wait... I know you."; do
  f5-tts_infer-cli --model F5TTS_v1_Base --ref_audio "$REF" --ref_text "$RT" \
    --gen_text "$LINE" --output_dir ~/voice-work/out/ 
  mv ~/voice-work/out/infer_cli_out.wav ~/voice-work/out/line_$i.wav   # adjust to actual output name
  i=$((i+1))
done
```
Listen. If the clone drifts/garbles, try a different reference clip (Step 1) or A/B with Chatterbox ([01] §3) before blaming the pipeline.

**Proves:** text → on-voice WAV. → detail: [01](01-engine-setup.md).

---

## Step 3 — Normalize to the game format (~2 min)

```bash
for f in ~/voice-work/out/line_*.wav; do
  ffmpeg -y -i "$f" -ac 1 -ar 44100 -sample_fmt s16 "${f%.wav}_44k.wav"
done
```
**Proves:** PCM 16-bit mono 44.1 kHz — the safe in-game format. → detail: [03](03-lip-and-audio-encoding.md) §1.

---

## Step 4 — Pin the filename rule, then place loose WAV (~40 min — the make-or-break step)

This is the highest-risk, highest-value step. A wrong name = silent line, **no error**.

1. Extract 2–3 **vanilla** `.fuz` filenames and open the owning quest in **SSEEdit**; reverse-engineer how each segment of `(Quest)_(Topic)_(HexBaseID)_(LineNumber)` is derived ([04] §3).
2. Open **your** built test plugin in SSEEdit; read your NPC's quest EditorID, the INFO FormIDs, and response indices for the 3 lines.
3. Construct each target name by hand, e.g. `MyQuest_MyTopic_000113C9_1.wav`.
4. Place the normalized WAVs (use the **plain `.wav` extension**, no fuz yet):
   ```
   <MO2 mod>/Sound/Voice/<YourPlugin.esp>/MaleNord/MyQuest_MyTopic_000113C9_1.wav
   ```
   …but per [[mo2-reinstall-reverts-manual-pex]], assemble this into your **build zip / mod folder via your normal packaging**, not by hand-editing the live MO2 folder.

**Proves (in-game, manual):** launch via MO2/Proton, trigger the dialogue → **audio plays with subtitles, mouth static.** This validates filename mapping + clone quality + packaging — the entire spine — with **zero Wine dependency**. → detail: [04](04-fuz-and-filenames.md).

> If a line is silent: it's almost always the filename (segment casing/truncation/topic). Re-derive from a vanilla example. This is why Step 4.1 exists.

---

## Step 5 — Pack `.fuz` (zero-lip) (~30 min)

Swap loose WAV → `.fuz`. For the hand run, either use a tool (Yakitori under Wine) or a throwaway script implementing [04] §1:
```
FUZE + <4 version bytes from a vanilla fuz> + 0x00000000 + <xwm or wav bytes>
```
Name it `..._1.fuz` (same rule, `.fuz` extension). Re-test in-game.

**Proves:** the fuz container works and your version bytes are right. (This is the logic that becomes `WriteFuz` in [05].) → detail: [04](04-fuz-and-filenames.md) §1.

---

## Step 6 — Add `.xwm` (optional, ~15 min)

```bash
wine xWMAEncode.exe ~/voice-work/out/line_1_44k.wav ~/voice-work/out/line_1.xwm
```
Pack the `.xwm` into the fuz instead of WAV. Re-test.

**Proves:** the Wine audio path + smaller files. (ffmpeg can't do this — [03] §4.) → detail: [03](03-lip-and-audio-encoding.md) §4.

---

## Step 7 — Make the mouth move (~15-min timebox, then decide)

You only need *movement*, not accuracy ([03] §2):
```bash
cp ~/voice-work/out/line_1.wav line16.wav   # need 16k/16/mono:
ffmpeg -y -i ~/voice-work/out/line_1.wav -ac 1 -ar 16000 -sample_fmt s16 line16.wav
wine FaceFXWrapper.exe Skyrim USEnglish /path/FonixData.cdf line16.wav line16.wav out.lip "Halt! You've violated the law."
```
- **Works under Wine** → pack `out.lip` into the fuz (`FuzLipSize` = lip length). Mouth moves, accurately, for free. Done.
- **Fails under Wine** → either accept static mouth (Tier 0) or build the native synthetic envelope `.lip` (Tier 2, [03] §2/§3) — a later coding task, not a blocker.

**Proves:** lip movement end to end (or confirms you fall back to Tier 0/2). → detail: [03](03-lip-and-audio-encoding.md) §2.

---

## Step 8 — Scale up the fidelity track (only if zero-shot drifts)

If across many/longer lines the F5/Chatterbox clone wanders, fine-tune **GPT-SoVITS** for that voice ([01] §4, [02] §3b) and regenerate that NPC's lines through `--engine gptsovits`. Everything downstream (Steps 3–7) is unchanged — only Step 2's engine swaps.

---

## Step 9 — Hand it to ModForge

Once Steps 1–7 are a reliable manual recipe, implement the `voicelines` CLI step + `Generator.Build.Voice.cs` per [05]. The runbook *is* the spec: each manual step maps to a pipeline stage in [05] §2.

---

## Quick reference — the whole MVP in one screen

```
0  venv + f5-tts                                          → engine imports
1  Lazy Voice Finder → MaleNord_ref.wav (+ .txt)          → reference voice
2  f5-tts_infer-cli  × 3 lines                            → line_N.wav
3  ffmpeg -ac 1 -ar 44100 -sample_fmt s16                 → game-format wav
4  SSEEdit → derive CK filename → place via packaging     → AUDIO PLAYS (static mouth)  ★ spine proven
5  pack FUZE+ver+0x0+audio                                → .fuz works
6  wine xWMAEncode                                        → smaller files
7  wine FaceFXWrapper (15-min timebox)                    → mouth moves (or Tier 0/2)
8  (if drift) GPT-SoVITS fine-tune                        → consistent voice
9  implement voicelines step                              → ModForge automates it
```
★ Step 4 is the one that proves ModForge's unique advantage (deterministic CK filenames) and carries the silent-failure risk — give it the most care.
