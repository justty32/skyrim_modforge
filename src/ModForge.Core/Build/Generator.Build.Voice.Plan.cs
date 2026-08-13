namespace ModForge;

/// <summary>One planned voice file for one INFO response and one voiceType folder.</summary>
public sealed record VoiceLinePlanEntry(
    string QuestEditorId,
    string TopicEditorId,
    string InfoEditorId,
    uint InfoFormId,
    int ResponseIndex,
    string Text,
    string ResolutionSource,
    IReadOnlyList<string> Speakers,
    string? VoiceType,
    string FileName,
    string RelativePath,
    bool HasVoiceTemplate,
    string? TemplateId,
    string? SkipReason,
    string Emotion = "Neutral",
    int Intensity = 0);

public static partial class Generator
{
    private static readonly Dictionary<uint, string> SkyrimVoiceTypeFolders = new()
    {
        [0x013AD1] = "MaleYoungEager",
        [0x013AD2] = "MaleEvenToned",
        [0x013ADD] = "FemaleEvenToned",
        [0x013ADC] = "FemaleYoungEager",
        [0x013AE0] = "FemaleSultry",
        [0x013AE4] = "FemaleCondescending",
        [0x013AE6] = "MaleNord",
        [0x013AE7] = "FemaleNord",
        [0x0E5003] = "MaleNordCommander",
        [0x0EA267] = "MaleEvenTonedAccented",
    };

    /// <summary>Best-effort conversion from a spec voiceType ref to the Sound/Voice folder name.</summary>
    public static string? VoiceTypeFolderName(string? voiceTypeRef)
    {
        if (string.IsNullOrWhiteSpace(voiceTypeRef)) return null;
        var s = voiceTypeRef.Trim();
        var colon = s.IndexOf(':');
        if (colon < 0) return IsSafeVoiceFolder(s) ? s : null;

        var master = s[..colon].Trim();
        var idPart = s[(colon + 1)..].Trim();
        if (idPart.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) idPart = idPart[2..];
        if (!uint.TryParse(idPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id)) return null;
        id &= 0x00FFFFFF;

        if (master.Equals("Skyrim.esm", StringComparison.OrdinalIgnoreCase)
            && SkyrimVoiceTypeFolders.TryGetValue(id, out var folder))
            return folder;
        return null;
    }

    /// <summary>
    /// Builds the deliverability plan used by the CLI dry-run/diagnostic paths: each dialogue INFO
    /// response is mapped to the speaker(s), voiceType folder, and engine voice filename the game
    /// will look for under Sound/Voice/&lt;plugin&gt;/&lt;voiceType&gt;/.
    /// </summary>
    public static List<VoiceLinePlanEntry> BuildVoiceLinePlan(
        ISkyrimModGetter mod,
        ILinkCache cache,
        IReadOnlyDictionary<string, VoiceTemplateSpec?> templateByNpcEd,
        string pluginName,
        string format,
        IReadOnlyDictionary<string, string>? voiceTypeByNpcEd = null,
        IReadOnlyDictionary<FormKey, (string VoiceType, VoiceTemplateSpec? Template)>? externalByFormKey = null)
    {
        var entries = new List<VoiceLinePlanEntry>();
        var ext = NormalizeVoiceFormat(format);

        foreach (var topic in mod.EnumerateMajorRecords<IDialogTopicGetter>())
        {
            if (topic.Quest.IsNull) continue;
            var questEd = topic.Quest.TryResolve(cache)?.EditorID ?? "UnknownQuest";
            var topicEd = topic.EditorID ?? "UnknownTopic";

            foreach (var infoLink in topic.Responses)
            {
                if (!cache.TryResolve<IDialogResponsesGetter>(infoLink.FormKey, out var info)) continue;
                if (info.Responses.Count == 0) continue;

                var infoEd = info.EditorID ?? "";

                // External speaker (voiceSpeakers[]): bypass NPC resolution — the speaker is a master NPC
                // the mod-only cache can't load. Emit straight to its declared voiceType + template.
                if (ResolveExternalSpeakerVoice(info, externalByFormKey) is { } extv)
                {
                    for (int i = 0; i < info.Responses.Count; i++)
                    {
                        var resp = info.Responses[i];
                        var t = resp.Text?.ToString() ?? "";
                        var fn = VoiceFileName(questEd, topicEd, info.FormKey.ID, i + 1, ext);
                        var rel = Path.Combine("Sound", "Voice", pluginName, extv.VoiceType, fn);
                        var skip = string.IsNullOrWhiteSpace(t) ? "empty response text"
                            : extv.Template is null ? "voiceSpeakers entry names a template that doesn't exist" : null;
                        entries.Add(new(questEd, topicEd, infoEd, info.FormKey.ID, i + 1, t, "voiceSpeakers",
                            new[] { extv.Speaker.ToString() }, extv.VoiceType, fn, rel,
                            extv.Template is not null, extv.Template?.Id, skip,
                            resp.Emotion.ToString(), (int)resp.EmotionValue));
                    }
                    continue;
                }

                var res = ResolveVoiceSpeakers(topic, info, mod, cache);
                for (int i = 0; i < info.Responses.Count; i++)
                {
                    var resp = info.Responses[i];
                    var text = resp.Text?.ToString() ?? "";
                    var emotion = resp.Emotion.ToString();
                    var intensity = (int)resp.EmotionValue;
                    var fileName = VoiceFileName(questEd, topicEd, info.FormKey.ID, i + 1, ext);

                    if (!res.Resolved)
                    {
                        entries.Add(new(questEd, topicEd, infoEd, info.FormKey.ID, i + 1, text, "",
                            Array.Empty<string>(), null, fileName, "", false, null,
                            $"speaker unresolved: {res.Reason}", emotion, intensity));
                        continue;
                    }

                    foreach (var group in res.Speakers.GroupBy(
                        s => EffectiveVoiceType(s, voiceTypeByNpcEd),
                        StringComparer.OrdinalIgnoreCase))
                    {
                        var speakers = group
                            .Select(s => s.Npc.EditorID ?? s.Npc.FormKey.ToString())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                            .ToArray();

                        VoiceTemplateSpec? tpl = null;
                        var templatedSpeaker = group.FirstOrDefault(s =>
                            templateByNpcEd.TryGetValue(s.Npc.EditorID ?? "", out var t) && t is not null);
                        if (templatedSpeaker is not null)
                            templateByNpcEd.TryGetValue(templatedSpeaker.Npc.EditorID ?? "", out tpl);

                        var voiceType = group.Key;
                        var rel = Path.Combine("Sound", "Voice", pluginName, voiceType, fileName);
                        var skipReason = string.IsNullOrWhiteSpace(text)
                            ? "empty response text"
                            : tpl is null
                                ? "no speaker in this voiceType has a usable voiceTemplate in the spec"
                                : null;

                        entries.Add(new(questEd, topicEd, infoEd, info.FormKey.ID, i + 1, text, res.Source,
                            speakers, voiceType, fileName, rel, tpl is not null, tpl?.Id, skipReason,
                            emotion, intensity));
                    }
                }
            }
        }

        return entries;
    }

    private static string NormalizeVoiceFormat(string? format)
    {
        var f = string.IsNullOrWhiteSpace(format) ? "fuz" : format.Trim().TrimStart('.').ToLowerInvariant();
        return f is "wav" or "xwm" or "fuz" ? f : "fuz";
    }

    private static bool IsSafeVoiceFolder(string s) =>
        s.Length > 0 && s.All(c => char.IsLetterOrDigit(c) || c == '_');
}
