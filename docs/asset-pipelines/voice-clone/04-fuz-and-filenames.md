# 04 — `.fuz` writer + the deterministic filename rule

← [README](README.md) · prev: [03-lip-and-audio-encoding.md](03-lip-and-audio-encoding.md) · next: [05-modforge-integration.md](05-modforge-integration.md)

Two things here, both native C# (no Wine): write the `.fuz` container, and compute the **exact CK-matched filename** the engine demands. The filename is the make-or-break detail — a one-character mismatch = silent line, no error.

---

## 1. The `.fuz` container (verified layout)

`.fuz` = optional `.lip` blob + `.xwm`(or WAV) audio, behind a 12-byte header. Read from suglasp's `convert_fuz_to_xwm.ps1` (parses fuz in code — authoritative, reimplementable):

| Offset | Bytes | Field |
|--------|-------|-------|
| 0 | 4 | Magic `FUZE` (ASCII) |
| 4 | 4 | Version / unknown |
| 8 | 4 | `FuzLipSize` — uint32, size of the lip section |
| 12 | `FuzLipSize` | `.lip` data (omitted entirely if size == 0) |
| 12 + FuzLipSize | rest | `.xwm` (or WAV) audio stream |

So `audioLen = fileLength − 12 − FuzLipSize`. If `FuzLipSize == 0`, audio sits immediately after the 12-byte header.

**Native C# writer (implemented in `Generator.Build.Voice.cs`):**
```csharp
// versionBytes: capture the 4 bytes from a vanilla .fuz once and hardcode (commonly a small constant).
static byte[] WriteFuz(byte[] xwmOrWav, byte[]? lip)
{
    using var ms = new MemoryStream();
    using var w  = new BinaryWriter(ms);
    w.Write(Encoding.ASCII.GetBytes("FUZE")); // 0: magic
    w.Write((uint)1);                           // 4: version
    w.Write((uint)(lip?.Length ?? 0));         // 8: FuzLipSize
    if (lip is { Length: > 0 }) w.Write(lip);  // 12: lip (omit if zero)
    w.Write(xwmOrWav);                          // audio
    return ms.ToArray();
}
```
Zero-lip path: `WriteFuz(xwm, null)` → `FUZE` + version + `0x00000000` + xwm. The writer is native;
Wine is only needed for xWMA encoding or FaceFX lip generation.

> Architecture note: this matches ModForge's posture everywhere else — emit the bytes natively when the format is small and verified (like Mutagen records), shell out only for the big opaque formats. A native fuz writer removes the Wine fuz-tool dependency entirely.

---

## 2. The filename rule (the hard part — and ModForge's superpower)

**On-disk path:** `Data/Sound/Voice/<PluginName.esp>/<VoiceType>/<filename>.fuz`
- first segment = **plugin filename exactly** (e.g. `MyMod.esp`)
- second segment = **voiceType EditorID** (e.g. `MaleNord`)

**Filename convention (implemented shape):** `(Quest)_(Topic)_(HexBaseID)_(LineNumber)`
e.g. `MyQuest_MyTopic_000113C9_1.fuz`. It encodes:
- parent **quest EditorID**
- the **topic/INFO context** string
- the **8-digit hex FormID of the INFO/response record**
- a **1-based response index** within the INFO

CK generates these automatically and **they cannot be changed** — the audio file must match exactly or the engine won't play it.

**Why this is free for ModForge:** ModForge *is* the generator. It assigns the QUST EditorID, the INFO FormID, and the response index via Mutagen. So it already holds every input to this filename **and can compute it deterministically without ever opening the Creation Kit.** This is the single hardest part of the manual community workflow, and it's the thing ModForge is uniquely positioned to nail. Everything else in this pipeline is generic plumbing; *this* is the differentiator.

Implementation note: current generator truncates Quest EditorID to 10 chars, Topic EditorID to 15
chars, strips non-alphanumeric/underscore chars, uses 8-digit uppercase INFO FormID and 1-based
response index. Use `voicediag` / `voicelines --plan` to inspect every planned filename.

---

## 3. Pinning the exact rule (do this FIRST, empirically)

The *shape* is confirmed; the *exact string formatting* is not byte-verified — specifically: segment truncation/length caps, casing, how a **blank topic** is rendered, and how non-alphanumerics in EditorIDs are handled. Get this wrong and lines go silent with no error (same failure class as [[vanilla-nif-paths-must-be-verified]]).

**Procedure (≈30 min, before trusting any generated name):**
1. Extract a handful of **vanilla** `.fuz` filenames for a known quest (Lazy Voice Finder, or unpack a Voices BSA, [02]).
2. Open that quest's QUST/DIAL/INFO in **SSEEdit** to see the real EditorIDs, INFO FormIDs, and response indices.
3. Reverse the mapping: confirm exactly how each filename segment is derived (does Topic use the DIAL EditorID or the INFO? truncated to N chars? upper/lower? blank-topic placeholder?).
4. Write a tiny ModForge unit test: feed known quest/INFO/index → assert it reproduces the extracted vanilla names character-for-character.
5. Repeat for an INFO with **multiple responses** (confirms the 1-based line index) and one with an **empty/auto topic**.

Record the pinned rule here once confirmed. This is the highest-value verification in the whole feature.

---

## 4. Loose WAV vs packed `.fuz` (per build)

Both play in-game. The engine accepts WAV/XWM/FUZ as voice files.

| Output | Mouth | Wine needed | Disk | Use when |
|--------|-------|-------------|------|----------|
| loose `.wav` (44.1k/16/mono) | static | no | large | **MVP** — proves the spine with zero Wine |
| `.fuz` (zero-lip, WAV inside) | static | no | large | native fuz writer proven, still no Wine |
| `.fuz` (zero-lip, xwm inside) | static | xwm only | small | xwm path proven |
| `.fuz` (lip + xwm) | moves | xwm (+lip if Tier 1) | small | full result |

The filename rule (§2/§3) is **identical regardless of container** — only the extension/contents change. So pin filenames once, vary the container freely.

---

## 5. Packaging into the MO2 folder

Voice files are loose assets. They are not embedded in the `.esp`/`.esm`. `package` copies
`Sound/...` only when it is provided as `--assets <dir>` or via `spec.assets`; it does not
automatically search another build directory for generated voice output. Output tree:
```
<zip root>/
  MyMod.esp
  Sound/Voice/MyMod.esp/MaleNord/MyQuest_MyTopic_000113C9_1.fuz
  ...
```

Safe workflows:

```bash
# A: generate voice directly into the final mod folder after package builds the plugin
dotnet run --project src/ModForge.Cli -- package spec.json OutModDir
dotnet run --project src/ModForge.Cli -- voicelines spec.json OutModDir/MyMod.esp

# B: generate voice in a staging dir, then bundle that Sound/ tree
dotnet run --project src/ModForge.Cli -- build spec.json Staging/MyMod.esp
dotnet run --project src/ModForge.Cli -- voicelines spec.json Staging/MyMod.esp
dotnet run --project src/ModForge.Cli -- package spec.json OutModDir --assets Staging
```

No `.seq` interaction beyond the normal dialogue quest `.seq`. Per memory
[[mo2-reinstall-reverts-manual-pex]], always rebuild into the mod folder — never hand-place files in
the live MO2 mod folder, they'll be reverted on reinstall.

---

## 6. What "done" looks like

- A C# `WriteFuz` exists and unit tests cover the FUZE header; **real F5-TTS + real xWMA generation
  produced playing `.fuz` files — in-game confirmed 2026-06-13** (cloned MaleNord voice on a custom
  NPC in the Sleeping Giant Inn, `ModForgeVoiceTest.zip`).
- `voicediag` / `voicelines --plan` list the exact path for each INFO before generation; the
  deterministic FormID→filename map was verified to survive a repackage (diff planned vs shipped).
- Still untested: lip sync (FaceFX not yet set up → static mouth), the loose-`.wav` fallback in-game,
  and a multi-response / blank-topic name edge case.

Those checks harden the already-implemented [05] `voicelines` step and [06]'s in-game runbook.

---

### Sources
fuz layout: suglasp `convert_fuz_to_xwm.ps1`, [Fallout Wiki FUZ File](https://fallout.wiki/wiki/FUZ_File). Filenames/paths: [CK Wiki "generate voice files by batch"](https://ck.uesp.net/wiki/How_to_generate_voice_files_by_batch), [Beyond Skyrim Voice Line Implementation](https://wiki.beyondskyrim.org/wiki/Arcane_University:Voice_Line_Implementation).
