using System.Diagnostics;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using ModForge;

internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  voicelines — walk the built esp's INFOs, resolve each speaker (Core:
    //  Generator.ResolveVoiceSpeakers — GetIsID / GetIsAliasRef / GetInFaction /
    //  scene Dialog action), and TTS one file per distinct voiceType folder.
    //  An unresolved speaker is a LOUD warning + summary count, never a silent skip.
    // -------------------------------------------------------------------------------
    private static int VoicelinesCmd(string specPath, string espPath)
    {
        var spec = ReadSpec(specPath);
        var mod = Load(espPath);
        var cache = mod.ToImmutableLinkCache();
        var specDir = Path.GetDirectoryName(Path.GetFullPath(specPath)) ?? ".";
        var pluginName = Path.GetFileName(espPath);

        var options = new VoiceOptions();
        if (string.IsNullOrEmpty(options.ResolvedTtsBin))
        {
            Console.Error.WriteLine("ERROR: MODFORGE_TTS_BIN not set. Voice generation skipped.");
            return 1;
        }

        var templateById = spec.VoiceTemplates.ToDictionary(t => t.Id, t => t, StringComparer.OrdinalIgnoreCase);
        var npcToTemplate = spec.Npcs
            .Where(n => !string.IsNullOrEmpty(n.VoiceTemplate))
            .ToDictionary(n => n.EditorId, n => templateById.GetValueOrDefault(n.VoiceTemplate), StringComparer.OrdinalIgnoreCase);

        var format = spec.VoiceLine?.Format?.ToLowerInvariant() ?? "fuz";
        bool skipLip = spec.VoiceLine?.SkipLip ?? false;
        int generated = 0, existing = 0, failed = 0, emptyText = 0, noTemplate = 0, unresolved = 0;

        foreach (var topic in mod.EnumerateMajorRecords<IDialogTopicGetter>())
        {
            if (topic.Quest.IsNull) continue;
            var questEd = topic.Quest.TryResolve(cache)?.EditorID ?? "UnknownQuest";
            var topicEd = topic.EditorID ?? "UnknownTopic";

            foreach (var infoLink in topic.Responses)
            {
                if (!cache.TryResolve<IDialogResponsesGetter>(infoLink.FormKey, out var info)) continue;
                if (info.Responses.Count == 0) continue;
                var infoLabel = $"INFO '{info.EditorID ?? "(no EditorID)"}' [{info.FormKey}] (topic '{topicEd}')";

                var res = Generator.ResolveVoiceSpeakers(topic, info, mod, cache);
                if (!res.Resolved)
                {
                    Console.Error.WriteLine($"  !! UNRESOLVED SPEAKER — NO VOICE GENERATED: {infoLabel}: {res.Reason}");
                    unresolved++;
                    continue;
                }

                var targets = Generator.SelectVoiceTargets(res, npcToTemplate);
                if (targets.Count == 0)
                {
                    var who = string.Join(", ", res.Speakers.Select(s => s.Npc.EditorID ?? s.Npc.FormKey.ToString()));
                    Console.Error.WriteLine($"  !! NO VOICE TEMPLATE — NO VOICE GENERATED: {infoLabel}: "
                        + $"speaker(s) [{who}] resolved via {res.Source} but none has a usable voiceTemplate in the spec");
                    noTemplate++;
                    continue;
                }

                for (int i = 0; i < info.Responses.Count; i++)
                {
                    var text = info.Responses[i].Text?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(text)) { emptyText++; continue; }
                    foreach (var t in targets)
                        switch (GenerateVoiceLine(espPath, pluginName, t, questEd, topicEd, info.FormKey.ID, i + 1,
                                                  text, format, skipLip, specDir, options))
                        {
                            case 1: generated++; break;
                            case 0: existing++; break;
                            default: failed++; break;
                        }
                }
            }
        }

        Console.WriteLine($"Voicelines: {generated} generated, {existing} already on disk, {failed} TTS failure(s), "
            + $"{emptyText} empty-text line(s), {noTemplate} INFO(s) skipped (no voiceTemplate), "
            + $"{unresolved} INFO(s) skipped (speaker unresolved).");
        if (unresolved > 0 || noTemplate > 0)
            Console.Error.WriteLine($"  !! {unresolved + noTemplate} INFO(s) produced NO voice — see '!!' warnings above.");
        return 0;
    }

    // One (text, voiceType) line: TTS → optional xWMA → fuz/wav/xwm via Generator.PackVoiceAudio
    // (which downgrades fuz/xwm to a LOOSE .wav when xWMAEncode is unavailable — never a raw-PCM fuz).
    // Returns 1 = generated, 0 = already on disk, -1 = TTS failed.
    private static int GenerateVoiceLine(string espPath, string pluginName, VoiceTarget target,
        string questEd, string topicEd, uint infoId, int responseIndex,
        string text, string format, bool skipLip, string specDir, VoiceOptions options)
    {
        var stem = Path.GetFileNameWithoutExtension(Generator.VoiceFileName(questEd, topicEd, infoId, responseIndex));
        var targetDir = Path.Combine(Path.GetDirectoryName(espPath) ?? ".", "Sound", "Voice", pluginName, target.VoiceType);
        var stemPath = Path.Combine(targetDir, stem);
        // Any prior output (incl. a loose-wav downgrade from an earlier run) counts as done.
        if (File.Exists(stemPath + ".fuz") || File.Exists(stemPath + ".wav") || File.Exists(stemPath + ".xwm"))
            return 0;   // TODO: hash check for cache

        Console.WriteLine($"  Generating: {target.VoiceType}/{stem} (\"{text}\")");
        var wav = Voice.GenerateWav(text, target.Template, specDir, options);
        if (wav == null) { Console.Error.WriteLine($"    ! FAILED to generate WAV for {stem}"); return -1; }

        byte[]? xwm = format is "xwm" or "fuz" ? Voice.EncodeXwma(wav, options) : null;
        byte[]? lip = format == "fuz" && !skipLip ? Voice.GenerateLip(wav, text, options) : null;

        var pack = Generator.PackVoiceAudio(format, wav, xwm, lip, stem);
        if (pack.Warning != null) Console.Error.WriteLine(pack.Warning);
        Directory.CreateDirectory(targetDir);
        File.WriteAllBytes(stemPath + "." + pack.Ext, pack.Data);
        if (pack.LooseLip != null) File.WriteAllBytes(stemPath + ".lip", pack.LooseLip);
        return 1;
    }

    // -------------------------------------------------------------------------------
    //  extract-voices
    // -------------------------------------------------------------------------------
    private static int ExtractVoicesCmd(string bsaPath, string voiceType, string outDir)
    {
        Console.WriteLine($"Extracting '{voiceType}' from {Path.GetFileName(bsaPath)}...");

        // Skyrim voices are in Sound/Voice/Skyrim.esm/<VoiceType>/*.fuz
        string pathFilter = $"sound/voice/skyrim.esm/{voiceType}/";
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
}
