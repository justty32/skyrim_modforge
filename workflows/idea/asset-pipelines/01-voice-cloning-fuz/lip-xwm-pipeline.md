# Voice-Cloning → Skyrim SE `.fuz` Voiced-Dialogue Pipeline — §4–7 lip、xwm、組裝、端到端

← [入口](../01-voice-cloning-fuz.md)

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
