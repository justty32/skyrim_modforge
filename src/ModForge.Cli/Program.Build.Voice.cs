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
    private static int VoicelinesCmd(string specPath, string espPath, string? mode)
    {
        if (mode is not null && mode is not "--dry-run" and not "--plan")
        {
            Console.Error.WriteLine($"ERROR: unknown voicelines option '{mode}'. Expected --dry-run or --plan.");
            return 2;
        }

        var spec = ReadSpec(specPath);
        var mod = Load(espPath);
        var cache = mod.ToImmutableLinkCache();
        var specDir = Path.GetDirectoryName(Path.GetFullPath(specPath)) ?? ".";
        var pluginName = Path.GetFileName(espPath);
        var format = spec.VoiceLine?.Format?.ToLowerInvariant() ?? "fuz";
        var npcToTemplate = BuildNpcVoiceTemplateMap(spec);
        var npcToVoiceType = BuildNpcVoiceTypeMap(spec);

        if (mode is "--dry-run" or "--plan")
        {
            PrintVoicePlan(mod, cache, npcToTemplate, npcToVoiceType, pluginName, format);
            return 0;
        }

        var options = new VoiceOptions();
        if (string.IsNullOrEmpty(options.ResolvedTtsBin))
        {
            Console.Error.WriteLine("ERROR: MODFORGE_TTS_BIN not set. Voice generation skipped.");
            return 1;
        }

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

                var targets = Generator.SelectVoiceTargets(res, npcToTemplate, npcToVoiceType);
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

    // -------------------------------------------------------------------------------
    //  voicediag — same speaker/template/path plan as `voicelines --dry-run`, but with
    //  a check-style exit code so scripts can fail before spending time on TTS.
    // -------------------------------------------------------------------------------
    private static int VoiceDiagCmd(string specPath, string espPath)
    {
        var spec = ReadSpec(specPath);
        var mod = Load(espPath);
        var cache = mod.ToImmutableLinkCache();
        var pluginName = Path.GetFileName(espPath);
        var format = spec.VoiceLine?.Format?.ToLowerInvariant() ?? "fuz";
        var entries = PrintVoicePlan(mod, cache, BuildNpcVoiceTemplateMap(spec), BuildNpcVoiceTypeMap(spec), pluginName, format);
        return entries.Any(e => e.SkipReason?.StartsWith("speaker unresolved:", StringComparison.OrdinalIgnoreCase) == true
                             || e.SkipReason?.StartsWith("no speaker", StringComparison.OrdinalIgnoreCase) == true)
            ? 1
            : 0;
    }

    private static Dictionary<string, VoiceTemplateSpec?> BuildNpcVoiceTemplateMap(ModSpec spec)
    {
        var templateById = spec.VoiceTemplates.ToDictionary(t => t.Id, t => t, StringComparer.OrdinalIgnoreCase);
        return spec.Npcs
            .Where(n => !string.IsNullOrEmpty(n.VoiceTemplate))
            .ToDictionary(n => n.EditorId, n => templateById.GetValueOrDefault(n.VoiceTemplate), StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> BuildNpcVoiceTypeMap(ModSpec spec) =>
        spec.Npcs
            .Select(n => new { n.EditorId, VoiceType = Generator.VoiceTypeFolderName(n.VoiceType) })
            .Where(n => !string.IsNullOrWhiteSpace(n.EditorId) && !string.IsNullOrWhiteSpace(n.VoiceType))
            .ToDictionary(n => n.EditorId, n => n.VoiceType!, StringComparer.OrdinalIgnoreCase);

    private static List<VoiceLinePlanEntry> PrintVoicePlan(ISkyrimModGetter mod, ILinkCache cache,
        IReadOnlyDictionary<string, VoiceTemplateSpec?> npcToTemplate,
        IReadOnlyDictionary<string, string> npcToVoiceType,
        string pluginName, string format)
    {
        var entries = Generator.BuildVoiceLinePlan(mod, cache, npcToTemplate, pluginName, format, npcToVoiceType);
        foreach (var e in entries)
        {
            var info = string.IsNullOrWhiteSpace(e.InfoEditorId) ? $"0x{e.InfoFormId:X8}" : $"{e.InfoEditorId} 0x{e.InfoFormId:X8}";
            var speakers = e.Speakers.Count == 0 ? "-" : string.Join(", ", e.Speakers);
            var voiceType = e.VoiceType ?? "-";
            var path = string.IsNullOrWhiteSpace(e.RelativePath) ? "-" : e.RelativePath;
            var template = e.TemplateId ?? "-";
            Console.WriteLine($"INFO {info} topic={e.TopicEditorId} quest={e.QuestEditorId} line={e.ResponseIndex}");
            Console.WriteLine($"  speaker={speakers} source={(string.IsNullOrWhiteSpace(e.ResolutionSource) ? "-" : e.ResolutionSource)} voiceType={voiceType} template={template}");
            Console.WriteLine($"  filename={e.FileName}");
            Console.WriteLine($"  path={path}");
            if (!string.IsNullOrWhiteSpace(e.Text))
                Console.WriteLine($"  text=\"{e.Text}\"");
            if (e.SkipReason is { Length: > 0 })
                Console.WriteLine($"  !! {e.SkipReason}");
        }

        var unresolved = entries.Count(e => e.SkipReason?.StartsWith("speaker unresolved:", StringComparison.OrdinalIgnoreCase) == true);
        var noTemplate = entries.Count(e => e.SkipReason?.StartsWith("no speaker", StringComparison.OrdinalIgnoreCase) == true);
        var empty = entries.Count(e => e.SkipReason == "empty response text");
        var deliverable = entries.Count(e => e.SkipReason is null);
        Console.WriteLine($"Voice plan: {entries.Count} line target(s), {deliverable} deliverable, {empty} empty-text, {noTemplate} missing-template, {unresolved} unresolved.");
        return entries;
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
