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
        var externalByFormKey = BuildExternalVoiceMap(spec);

        if (mode is "--dry-run" or "--plan")
        {
            PrintVoicePlan(mod, cache, npcToTemplate, npcToVoiceType, pluginName, format, externalByFormKey);
            return 0;
        }

        var options = new VoiceOptions();
        if (string.IsNullOrEmpty(options.ResolvedTtsBin))
        {
            Console.Error.WriteLine("ERROR: MODFORGE_TTS_BIN not set. Voice generation skipped.");
            return 1;
        }

        bool skipLip = spec.VoiceLine?.SkipLip ?? false;
        if (format == "fuz" && !skipLip
            && string.IsNullOrEmpty(options.ResolvedLipGenExe)
            && string.IsNullOrEmpty(options.ResolvedFaceFxExe))
            Console.Error.WriteLine("  !! No lip tool configured (MODFORGE_LIPGEN / MODFORGE_FACEFX) — "
                + ".fuz files will ship WITHOUT lip sync (mouths won't move). "
                + "Set MODFORGE_LIPGEN to the CK LipGenerator.exe, or voiceLine.skipLip:true to silence this.");

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

                List<VoiceTarget> targets;
                // External speaker (voiceSpeakers[]): generate to the declared voiceType + template,
                // bypassing NPC resolution (the speaker is a master NPC the mod-only cache can't load).
                if (Generator.ResolveExternalSpeakerVoice(info, externalByFormKey) is { } extv)
                {
                    if (extv.Template is null)
                    {
                        Console.Error.WriteLine($"  !! NO VOICE TEMPLATE — NO VOICE GENERATED: {infoLabel}: "
                            + $"voiceSpeakers binds {extv.Speaker} to voiceType '{extv.VoiceType}' but its template id is unknown");
                        noTemplate++;
                        continue;
                    }
                    targets = new() { new VoiceTarget(extv.VoiceType, extv.Template, extv.Speaker.ToString()) };
                }
                else
                {
                    var res = Generator.ResolveVoiceSpeakers(topic, info, mod, cache);
                    if (!res.Resolved)
                    {
                        Console.Error.WriteLine($"  !! UNRESOLVED SPEAKER — NO VOICE GENERATED: {infoLabel}: {res.Reason}");
                        unresolved++;
                        continue;
                    }
                    targets = Generator.SelectVoiceTargets(res, npcToTemplate, npcToVoiceType);
                    if (targets.Count == 0)
                    {
                        var who = string.Join(", ", res.Speakers.Select(s => s.Npc.EditorID ?? s.Npc.FormKey.ToString()));
                        Console.Error.WriteLine($"  !! NO VOICE TEMPLATE — NO VOICE GENERATED: {infoLabel}: "
                            + $"speaker(s) [{who}] resolved via {res.Source} but none has a usable voiceTemplate in the spec");
                        noTemplate++;
                        continue;
                    }
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
        var entries = PrintVoicePlan(mod, cache, BuildNpcVoiceTemplateMap(spec), BuildNpcVoiceTypeMap(spec), pluginName, format, BuildExternalVoiceMap(spec));
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

    // Build the external-speaker voice map (voiceSpeakers[]): FormKey of the gated NPC → (voiceType
    // folder, the voiceTemplate to clone with). Lets `voicelines`/`voicediag` voice lines whose speaker
    // is an NPC from another master (e.g. an existing follower) that the mod-only cache can't resolve.
    private static Dictionary<FormKey, (string VoiceType, VoiceTemplateSpec? Template)> BuildExternalVoiceMap(ModSpec spec)
    {
        var templateById = spec.VoiceTemplates.ToDictionary(t => t.Id, t => (VoiceTemplateSpec?)t, StringComparer.OrdinalIgnoreCase);
        var d = new Dictionary<FormKey, (string, VoiceTemplateSpec?)>();
        foreach (var vs in spec.VoiceSpeakers)
        {
            if (!TryParseSpecRef(vs.Speaker, out var fk))
            { Console.Error.WriteLine($"  !! voiceSpeakers: bad speaker ref '{vs.Speaker}' (need <master>:0xFORMID)"); continue; }
            var vt = Generator.VoiceTypeFolderName(vs.VoiceType) ?? vs.VoiceType;
            d[fk] = (vt, templateById.GetValueOrDefault(vs.Template));
        }
        return d;
    }

    // Parse a "<master>:0xFORMID" spec ref into a FormKey (24-bit local id).
    private static bool TryParseSpecRef(string s, out FormKey fk)
    {
        fk = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var i = s.IndexOf(':');
        if (i <= 0) return false;
        var plugin = s[..i].Trim();
        var hex = s[(i + 1)..].Trim();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        if (!uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var id)) return false;
        try { fk = new FormKey(ModKey.FromNameAndExtension(plugin), id & 0x00FFFFFF); return true; }
        catch { return false; }
    }

    private static Dictionary<string, string> BuildNpcVoiceTypeMap(ModSpec spec) =>
        spec.Npcs
            .Select(n => new { n.EditorId, VoiceType = Generator.VoiceTypeFolderName(n.VoiceType) })
            .Where(n => !string.IsNullOrWhiteSpace(n.EditorId) && !string.IsNullOrWhiteSpace(n.VoiceType))
            .ToDictionary(n => n.EditorId, n => n.VoiceType!, StringComparer.OrdinalIgnoreCase);

    private static List<VoiceLinePlanEntry> PrintVoicePlan(ISkyrimModGetter mod, ILinkCache cache,
        IReadOnlyDictionary<string, VoiceTemplateSpec?> npcToTemplate,
        IReadOnlyDictionary<string, string> npcToVoiceType,
        string pluginName, string format,
        IReadOnlyDictionary<FormKey, (string VoiceType, VoiceTemplateSpec? Template)>? externalByFormKey = null)
    {
        var entries = Generator.BuildVoiceLinePlan(mod, cache, npcToTemplate, pluginName, format, npcToVoiceType, externalByFormKey);
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
}
