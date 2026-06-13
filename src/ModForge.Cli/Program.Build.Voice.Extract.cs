using System.Diagnostics;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using ModForge;

internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  extract-voices
    // -------------------------------------------------------------------------------
    private static int ExtractVoicesCmd(string bsaPath, string voiceType, string outDir, string plugin = "Skyrim.esm")
    {
        Console.WriteLine($"Extracting '{voiceType}' ({plugin}) from {Path.GetFileName(bsaPath)}...");

        // Voices live in Sound/Voice/<plugin>/<VoiceType>/*.fuz. Vanilla = Skyrim.esm; a follower's BSA
        // (e.g. SofiaFollower.bsa) keys on its own plugin name — pass it to clone an existing follower.
        string pathFilter = $"sound/voice/{plugin.ToLowerInvariant()}/{voiceType}/";
        var tempDir = Path.Combine(Path.GetTempPath(), $"modforge_extract_{Guid.NewGuid()}");

        try
        {
            int count = Archives.Extract(bsaPath, tempDir, pathFilter);
            if (count == 0)
            {
                Console.Error.WriteLine($"  ! No files found for filter: {pathFilter}");
                return 1;
            }

            Console.WriteLine($"  Extracted {count} .fuz files. Converting to .wav...");
            Directory.CreateDirectory(outDir);

            int converted = 0;
            var fuzFiles = Directory.GetFiles(tempDir, "*.fuz", SearchOption.AllDirectories);

            foreach (var fuzPath in fuzFiles)
            {
                try
                {
                    var result = Fuz.Split(File.ReadAllBytes(fuzPath));
                    var audioPath = Path.Combine(Path.GetTempPath(), $"temp_audio_{Guid.NewGuid()}.{result.AudioExt}");
                    File.WriteAllBytes(audioPath, result.Audio);

                    var wavName = Path.GetFileNameWithoutExtension(fuzPath) + ".wav";
                    var wavPath = Path.Combine(outDir, wavName);

                    // ffmpeg -i in.xwm out.wav
                    var psi = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-y -i \"{audioPath}\" \"{wavPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    };

                    using var proc = Process.Start(psi);
                    proc?.WaitForExit();

                    if (File.Exists(wavPath)) converted++;
                    if (File.Exists(audioPath)) File.Delete(audioPath);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"    ! Failed to process {Path.GetFileName(fuzPath)}: {ex.Message}");
                }
            }

            Console.WriteLine($"Done! {converted} WAV files written to {outDir}");
            return 0;
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    // -------------------------------------------------------------------------------
    //  voice-annotate — extract a voiceType's clips to WAV AND write an emotion-annotation
    //  manifest. Each clip's source INFO (FormID is in the filename) is looked up in <esm>
    //  for its game-assigned Emotion/EmotionValue/line — the deterministic first-pass index
    //  the user then corrects by ear (manifest `override`/`note` fields).
    // -------------------------------------------------------------------------------
    private static int VoiceAnnotateCmd(string esmPath, string voiceType, string bsaPath, string outDir)
    {
        if (!File.Exists(esmPath)) { Console.Error.WriteLine($"  ! esm not found: {esmPath}"); return 1; }
        if (!File.Exists(bsaPath)) { Console.Error.WriteLine($"  ! bsa not found: {bsaPath}"); return 1; }
        Directory.CreateDirectory(outDir);

        using var esm = SkyrimMod.CreateFromBinaryOverlay(new Mutagen.Bethesda.Plugins.ModPath(esmPath), SkyrimRelease.SkyrimSE);
        var cache = esm.ToImmutableLinkCache();
        var masters = esm.ModHeader.MasterReferences.Select(m => m.Master).ToList();
        var esmFile = Path.GetFileName(esmPath);

        string filter = $"sound/voice/{esmFile.ToLowerInvariant()}/{voiceType.ToLowerInvariant()}/";
        var tempDir = Path.Combine(Path.GetTempPath(), $"modforge_annotate_{Guid.NewGuid()}");
        var entries = new List<VoiceAnnotation>();
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

                VoiceAnnotation entry;
                if (VoiceAnnotate.TryParseInfoFormKey(fileName, masters, esm.ModKey, out var fk)
                    && cache.TryResolve<IDialogResponsesGetter>(fk, out var info))
                    entry = VoiceAnnotate.BuildEntry(wavName, voiceType, info!, 0);
                else
                    entry = new VoiceAnnotation { Clip = wavName, VoiceType = voiceType, Emotion = "Neutral", Note = $"INFO not found in {esmFile}" };
                entries.Add(entry);
            }
            var json = System.Text.Json.JsonSerializer.Serialize(entries, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(outDir, "voice-annotations.json"), json);
            Console.WriteLine($"voice-annotate: {entries.Count} clip(s) → {Path.Combine(outDir, "voice-annotations.json")} (+ WAVs)");
            return 0;
        }
        finally { try { Directory.Delete(tempDir, true); } catch { } }
    }
}
