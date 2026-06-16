using Mutagen.Bethesda.Plugins.Cache;

internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  voicediag -- dry, dependency-free voice wiring report for a BUILT plugin.
    //  Walks dialogue INFO records, reuses Core's speaker resolver (GetIsID / alias /
    //  faction / scene action), and prints the CK filename each response expects.
    //  No spec, Skyrim.esm, TTS, xWMA, or FaceFX required.
    // -------------------------------------------------------------------------------
    private static int VoiceDiag(string inPath)
    {
        var mod = Load(inPath);
        var cache = mod.ToImmutableLinkCache();
        var pluginName = Path.GetFileName(inPath);

        Console.WriteLine($"voicediag  {pluginName}");
        Console.WriteLine("layout: Sound/Voice/<plugin>/<voiceType>/<quest10>_<topic15>_<infoFormId8>_<response>.fuz");

        int topics = 0, infos = 0, responses = 0, speakerRows = 0, unresolved = 0;
        foreach (var topic in mod.EnumerateMajorRecords<IDialogTopicGetter>())
        {
            if (topic.Responses.Count == 0) continue;
            topics++;

            var questEd = topic.Quest.TryResolve(cache)?.EditorID ?? "UnknownQuest";
            var topicEd = topic.EditorID ?? "UnknownTopic";
            var topicLabel = $"{topicEd} [{topic.FormKey}]";

            foreach (var infoLink in topic.Responses)
            {
                if (!cache.TryResolve<IDialogResponsesGetter>(infoLink.FormKey, out var info))
                    continue;

                infos++;
                var res = Generator.ResolveVoiceSpeakers(topic, info, mod, cache);
                var infoEd = info.EditorID ?? "(no EditorID)";
                var source = res.Resolved ? res.Source : "UNRESOLVED";
                Console.WriteLine($"\nINFO {infoEd} [{info.FormKey}]  topic={topicLabel}  quest={questEd}  source={source}");

                if (!res.Resolved)
                {
                    unresolved++;
                    Console.WriteLine($"  reason: {res.Reason}");
                }

                for (int i = 0; i < info.Responses.Count; i++)
                {
                    responses++;
                    var file = Generator.VoiceFileName(questEd, topicEd, info.FormKey.ID, i + 1);
                    var text = SafeResponseText(info.Responses[i]);
                    Console.WriteLine($"  response[{i + 1}] file={file}" + (text is null ? "" : $" text=\"{text}\""));

                    foreach (var sp in res.Speakers)
                    {
                        speakerRows++;
                        var voiceType = VoiceTypeLabel(sp);
                        var npc = sp.Npc.EditorID ?? sp.Npc.FormKey.ToString();
                        Console.WriteLine($"    speaker={npc}  voiceType={voiceType}  expected=Sound/Voice/{pluginName}/{VoiceFolder(sp)}/{file}");
                    }
                }
            }
        }

        Console.WriteLine($"\n-- {topics} topic(s), {infos} INFO(s), {responses} response(s), {speakerRows} speaker row(s), {unresolved} unresolved INFO(s)");
        return 0;
    }

    private static string VoiceFolder(VoiceSpeaker sp) =>
        string.IsNullOrEmpty(sp.VoiceType) ? "DefaultVoice" : sp.VoiceType!;

    private static string VoiceTypeLabel(VoiceSpeaker sp)
    {
        if (!string.IsNullOrEmpty(sp.VoiceType)) return sp.VoiceType!;
        var fk = sp.Npc.Voice.FormKey;
        return fk.IsNull ? "DefaultVoice" : $"DefaultVoice (VTYP {fk} not resolved)";
    }

    private static string? SafeResponseText(IDialogResponseGetter response)
    {
        try
        {
            var text = response.Text?.String;
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = text.Replace("\r", " ").Replace("\n", " ");
            return text.Length <= 80 ? text : text[..77] + "...";
        }
        catch
        {
            return "<localized>";
        }
    }
}
