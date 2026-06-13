# Voice emotion-annotation index (A) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A `voice-annotate <esm> <voiceType> <VoicesBSA> <outDir>` CLI that extracts a voice type's clips to WAV and writes a JSON manifest tagging each clip with its source INFO's emotion/intensity/line — the (deterministic, game-seeded) annotation index a human then corrects by ear.

**Architecture:** A pure, testable core in `Voice.Annotate.cs` (the manifest model + "build a manifest entry from a resolved INFO" + "parse the INFO FormKey out of a clip filename"). A thin CLI command in `Program.Build.Voice.cs` does the I/O shell (BSA extract via `Archives.Extract`, `.fuz`→WAV via `Fuz.Split`+ffmpeg, esm link-cache, JSON write) and calls the core.

**Tech Stack:** C#/.NET 10, Mutagen.Bethesda.Skyrim 0.49, System.Text.Json, xUnit.

**Verified facts (this session):** clip filename = `<quest>_<topic>_<infoFormId:X8>_<n>.fuz` (`Generator.Build.Voice.cs` `VoiceFileName`). INFO record = `IDialogResponsesGetter`; `info.Responses[i]` is a `DialogResponse` with `.Text` (TranslatedString), `.Emotion` (`Emotion` enum: Neutral/Anger/Disgust/Fear/Sad/Happy/Surprise), `.EmotionValue` (byte 0–100). BSA: `Archives.Extract(bsaPath, tempDir, "sound/voice/<plugin>/<voiceType>/")`; `Fuz.Split(bytes)` → `{Audio, AudioExt}`; ffmpeg → wav (see `ExtractVoicesCmd`). esm load: `SkyrimMod.CreateFromBinaryOverlay(new ModPath(esm), SkyrimRelease.SkyrimSE)` + `.ToImmutableLinkCache()` (lazy — fine even for Skyrim.esm, resolving specific FormKeys does not load 250MB into the heap). FormKey-from-FormID: highByte = `id >> 24`; modKey = `highByte < masters.Count ? masters[highByte] : esmModKey`; localId = `id & 0x00FFFFFF`. Test build: `Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod`.

---

### Task 1: Manifest model + entry builder + filename FormKey parse (the testable core)

**Files:**
- Create: `src/ModForge.Core/Voice.Annotate.cs`
- Test: `tests/ModForge.Core.Tests/VoiceAnnotateTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class VoiceAnnotateTests
{
    // Build an in-memory plugin with one voiced INFO carrying an emotion, resolve it back from a clip
    // filename, and confirm the manifest entry reads the emotion/intensity/text. Offline — no master/BSA.
    [Fact]
    public void Annotation_entry_reads_emotion_intensity_and_text_from_the_info()
    {
        var mod = new SkyrimMod(ModKey.FromNameAndExtension("Test.esp"), SkyrimRelease.SkyrimSE);
        var topic = mod.DialogTopics.AddNew();
        var info = new DialogResponses(mod);
        info.Responses.Add(new DialogResponse { Text = "You'll regret this.", ResponseNumber = 1, Emotion = Emotion.Anger, EmotionValue = 80 });
        topic.Responses.Add(info);
        var cache = mod.ToImmutableLinkCache();

        // a clip filename for this INFO (VoiceFileName builds the same shape)
        string fileName = $"MQ_GREET_{info.FormKey.ID:X8}_1.fuz";
        Assert.True(VoiceAnnotate.TryParseInfoFormKey(fileName, mod.ModHeader.MasterReferences.Select(m => m.Master).ToList(), mod.ModKey, out var fk));
        Assert.Equal(info.FormKey, fk);

        Assert.True(cache.TryResolve<IDialogResponsesGetter>(fk, out var resolved));
        var entry = VoiceAnnotate.BuildEntry("MaleNord/clip.wav", "MaleNord", resolved!, 0);
        Assert.Equal("Anger", entry.Emotion);
        Assert.Equal(80, entry.Intensity);
        Assert.Equal("You'll regret this.", entry.Text);
        Assert.Equal($"0x{info.FormKey.ID:X8}", entry.InfoFormId);
    }

    [Fact]
    public void Bad_filename_returns_false()
    {
        Assert.False(VoiceAnnotate.TryParseInfoFormKey("not_a_voice_file.fuz", new List<ModKey>(), ModKey.FromNameAndExtension("Test.esp"), out _));
    }
}
```

- [ ] **Step 2: Run it — expect FAIL** (no `VoiceAnnotate`).

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~VoiceAnnotateTests"`

- [ ] **Step 3: Implement** — create `src/ModForge.Core/Voice.Annotate.cs`:

```csharp
using System;
using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

// One clip's annotation entry — the unit of the voice-annotation manifest. `emotion`/`intensity` are
// deterministic (read from the source INFO); the user corrects via `override`/`intensityOverride`/`note`.
public sealed class VoiceAnnotation
{
    public string Clip { get; set; } = "";
    public string VoiceType { get; set; } = "";
    public string Text { get; set; } = "";
    public string Emotion { get; set; } = "Neutral";
    public int Intensity { get; set; }
    public string Quest { get; set; } = "";
    public string Topic { get; set; } = "";
    public string InfoFormId { get; set; } = "";
    public string Override { get; set; } = "";
    public int? IntensityOverride { get; set; }
    public string Note { get; set; } = "";
}

public static class VoiceAnnotate
{
    // Parse the INFO FormKey out of a clip filename "<quest>_<topic>_<infoFormId:X8>_<n>.fuz".
    // The 8-hex FormID's high byte indexes the source plugin's master list (else it's the plugin itself).
    public static bool TryParseInfoFormKey(string fileName, IReadOnlyList<ModKey> masters, ModKey esmKey, out FormKey formKey)
    {
        formKey = default;
        var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
        var parts = stem.Split('_');
        if (parts.Length < 4) return false;
        var hex = parts[^2];   // <quest>_<topic>_<FORMID>_<n>
        if (hex.Length != 8 || !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var id)) return false;
        int hi = (int)(id >> 24);
        var modKey = hi >= 0 && hi < masters.Count ? masters[hi] : esmKey;
        formKey = new FormKey(modKey, id & 0x00FFFFFF);
        return true;
    }

    // Build a manifest entry from a resolved INFO. `responseIndex` picks which response line (0-based).
    public static VoiceAnnotation BuildEntry(string clipRelPath, string voiceType, IDialogResponsesGetter info, int responseIndex)
    {
        var resp = responseIndex >= 0 && responseIndex < info.Responses.Count
            ? info.Responses[responseIndex]
            : (info.Responses.Count > 0 ? info.Responses[0] : null);
        string text = "";
        try { text = resp?.Text?.ToString() ?? ""; } catch { text = ""; }   // inline string; defensive
        return new VoiceAnnotation
        {
            Clip = clipRelPath,
            VoiceType = voiceType,
            Text = text,
            Emotion = (resp?.Emotion ?? Emotion.Neutral).ToString(),
            Intensity = resp?.EmotionValue ?? 0,
            InfoFormId = $"0x{info.FormKey.ID:X8}",
        };
    }
}
```

- [ ] **Step 4: Run the test — expect PASS.**

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/Voice.Annotate.cs tests/ModForge.Core.Tests/VoiceAnnotateTests.cs
git commit -m "feat(voice): annotation manifest model + INFO-emotion entry builder + filename FormKey parse" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: The `voice-annotate` CLI command

**Files:**
- Modify: `src/ModForge.Cli/Program.cs` (register the command + help)
- Modify: `src/ModForge.Cli/Program.Build.Voice.cs` (the command body, beside `ExtractVoicesCmd`)

- [ ] **Step 1: Register the command** in `Program.cs`. Beside the `extract-voices` case (search `case "extract-voices"`), add:
```csharp
                case "voice-annotate" when args.Length == 5: return VoiceAnnotateCmd(args[1], args[2], args[3], args[4]);
```
And in the help string (beside the `extract-voices` help line):
```csharp
        "  voice-annotate <esm> <voiceType> <VoicesBSA> <outDir>  extract clips + write emotion-annotation manifest\n" +
```

- [ ] **Step 2: Implement `VoiceAnnotateCmd`** in `Program.Build.Voice.cs` (add the method; reuse the `ExtractVoicesCmd` BSA/Fuz/ffmpeg shape):

```csharp
    private static int VoiceAnnotateCmd(string esmPath, string voiceType, string bsaPath, string outDir)
    {
        if (!File.Exists(esmPath)) { Console.Error.WriteLine($"  ! esm not found: {esmPath}"); return 1; }
        if (!File.Exists(bsaPath)) { Console.Error.WriteLine($"  ! bsa not found: {bsaPath}"); return 1; }
        Directory.CreateDirectory(outDir);

        using var esm = Mutagen.Bethesda.Skyrim.SkyrimMod.CreateFromBinaryOverlay(
            new Mutagen.Bethesda.Plugins.ModPath(esmPath), Mutagen.Bethesda.Skyrim.SkyrimRelease.SkyrimSE);
        var cache = esm.ToImmutableLinkCache();
        var masters = esm.ModHeader.MasterReferences.Select(m => m.Master).ToList();
        var esmFile = Path.GetFileName(esmPath);   // e.g. "Skyrim.esm" / "SofiaFollower.esp"

        // Clips live under Sound/Voice/<esmFile>/<voiceType>/.
        string filter = $"sound/voice/{esmFile.ToLowerInvariant()}/{voiceType.ToLowerInvariant()}/";
        var tempDir = Path.Combine(Path.GetTempPath(), $"modforge_annotate_{Guid.NewGuid()}");
        var entries = new List<ModForge.VoiceAnnotation>();
        try
        {
            int found = Archives.Extract(bsaPath, tempDir, filter);
            if (found == 0) { Console.Error.WriteLine($"  ! no clips for {filter}"); return 1; }
            foreach (var fuzPath in Directory.GetFiles(tempDir, "*.fuz", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(fuzPath);
                var wavName = Path.GetFileNameWithoutExtension(fuzPath) + ".wav";
                var wavPath = Path.Combine(outDir, wavName);
                try
                {
                    var split = Fuz.Split(File.ReadAllBytes(fuzPath));
                    var audioPath = Path.Combine(Path.GetTempPath(), $"ta_{Guid.NewGuid()}.{split.AudioExt}");
                    File.WriteAllBytes(audioPath, split.Audio);
                    var psi = new ProcessStartInfo { FileName = "ffmpeg", Arguments = $"-y -i \"{audioPath}\" \"{wavPath}\"",
                        RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                    using (var p = Process.Start(psi)) p?.WaitForExit();
                    if (File.Exists(audioPath)) File.Delete(audioPath);
                }
                catch (Exception ex) { Console.Error.WriteLine($"    ! {fileName}: {ex.Message}"); }

                ModForge.VoiceAnnotation entry;
                if (ModForge.VoiceAnnotate.TryParseInfoFormKey(fileName, masters, esm.ModKey, out var fk)
                    && cache.TryResolve<Mutagen.Bethesda.Skyrim.IDialogResponsesGetter>(fk, out var info))
                    entry = ModForge.VoiceAnnotate.BuildEntry(wavName, voiceType, info!, 0);
                else
                    entry = new ModForge.VoiceAnnotation { Clip = wavName, VoiceType = voiceType, Emotion = "Neutral", Note = $"INFO not found in {esmFile}" };
                entries.Add(entry);
            }
            var json = System.Text.Json.JsonSerializer.Serialize(entries, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(outDir, "voice-annotations.json"), json);
            Console.WriteLine($"voice-annotate: {entries.Count} clip(s) → {Path.Combine(outDir, "voice-annotations.json")} (+ WAVs)");
            return 0;
        }
        finally { try { Directory.Delete(tempDir, true); } catch { } }
    }
```

(Confirm the `using`s already present at the top of `Program.Build.Voice.cs` cover `System`, `System.IO`, `System.Linq`, `System.Diagnostics`, `System.Collections.Generic` — add any missing. `Archives`/`Fuz` are the same helpers `ExtractVoicesCmd` uses.)

- [ ] **Step 3: Build + smoke-test the offline core** (the CLI integration needs the real BSA/ffmpeg → manual):

Run: `dotnet build src/ModForge.Cli/ModForge.Cli.csproj -c Debug` → Build succeeded.
Then (integration, needs the user's machine): `dotnet run --project src/ModForge.Cli -- voice-annotate "/home/lorkhan/skyrim_mods/unzip/Sofia Follower v.2/Data/SofiaFollower.esp" <SofiaVoiceType> "<SofiaFollower.bsa path>" /tmp/sofia_anno` → produces `/tmp/sofia_anno/voice-annotations.json`. (If the user hasn't a loose BSA, this is an in-game-machine step; the Task-1 unit test already proves the mapping logic.)

- [ ] **Step 4: Commit**

```bash
git add src/ModForge.Cli/Program.cs src/ModForge.Cli/Program.Build.Voice.cs
git commit -m "feat(voice): voice-annotate CLI — extract clips + write emotion manifest" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Docs

**Files:**
- Modify: `docs/SPEC-workflow.md` (Voice section — document `voice-annotate` + the manifest)
- Modify: `docs/CODE_MAP.infra.md` (voice section — `Voice.Annotate.cs` + the CLI command + `VoiceAnnotateTests`)

- [ ] **Step 1: SPEC-workflow.md** — under the Voice section, add a "voice-annotate" subsection: the command signature, what the manifest contains (clip/text/emotion/intensity/quest/topic/infoFormId + the user-filled override/intensityOverride/note), that emotion/intensity are read deterministically from the source INFO (the free first pass), that the user corrects by ear, and that phase B (`voiceTemplates[].referenceLibrary`) will consume the corrected manifest to pick emotion-matched reference clips. Note the source esm can be Skyrim.esm (vanilla voice types) or a mod (Sofia/Vigilant character voices).

- [ ] **Step 2: CODE_MAP.infra.md** — in the voice/translate section, add rows for `Voice.Annotate.cs` (`VoiceAnnotation` model + `VoiceAnnotate.TryParseInfoFormKey`/`BuildEntry`) and the `voice-annotate` CLI command in `Program.Build.Voice.cs`; add `VoiceAnnotateTests.cs` to the Tests column.

- [ ] **Step 3: Full offline regression**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"`
Expected: all green (prior 481 + the new VoiceAnnotate tests).

- [ ] **Step 4: Commit**

```bash
git add docs/SPEC-workflow.md docs/CODE_MAP.infra.md
git commit -m "docs(voice): voice-annotate CLI + manifest in SPEC-workflow + CODE_MAP" -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Self-review notes
- **Spec coverage:** manifest model + deterministic INFO-emotion seeding (Task 1), the `voice-annotate` CLI with BSA extract + WAV + JSON (Task 2), docs (Task 3). The taxonomy (7 emotions + intensity) and override/note fields are in the model. Phase B is explicitly out of this plan (separate spec).
- **Type consistency:** `VoiceAnnotation.{Clip,VoiceType,Text,Emotion,Intensity,Quest,Topic,InfoFormId,Override,IntensityOverride,Note}`, `VoiceAnnotate.TryParseInfoFormKey(fileName, IReadOnlyList<ModKey>, ModKey, out FormKey)`, `VoiceAnnotate.BuildEntry(clipRelPath, voiceType, IDialogResponsesGetter, int)` — used identically across Task 1 (impl+test) and Task 2 (CLI). `Emotion` enum + `IDialogResponsesGetter`/`DialogResponse.Emotion/EmotionValue/Text` verified this session.
- **Placeholder scan:** none. The Quest/Topic fields are left empty by `BuildEntry` (the FormID is the authoritative key; quest/topic are cosmetic context — a future refinement can fill them from the topic, noted but not required for v1). The integration smoke-test (Task 2 Step 3) is explicitly a manual machine step; the unit test (Task 1) covers the logic offline.
