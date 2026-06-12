namespace ModForge;

// --- voicelines: speaker resolution for built dialogue INFOs ------------------------------
// Used by the `voicelines` CLI command; lives in Core so the chain is unit-testable against an
// in-memory built mod. One INFO can map to MULTIPLE speakers (GetInFaction): Skyrim voice files
// live under Sound/Voice/<plugin>/<voiceType>/ — one file per voiceType serves every NPC of that
// type, so the CLI generates once per DISTINCT voiceType (SelectVoiceTargets).

/// <summary>One NPC that can speak an INFO + its voice type's EditorID (null when the VTYP is
/// an external-master record the mod-only link cache can't resolve).</summary>
public sealed record VoiceSpeaker(INpcGetter Npc, string? VoiceType);

/// <summary>Outcome of resolving an INFO's speaker(s). When unresolved, <c>Reason</c> says why
/// (the CLI must surface it LOUDLY — a silent skip means silently missing voice files).</summary>
public sealed record VoiceSpeakerResolution(IReadOnlyList<VoiceSpeaker> Speakers, string Source, string? Reason)
{
    public bool Resolved => Speakers.Count > 0;
}

/// <summary>A (voiceType folder, TTS template) pair the CLI should generate a line for.</summary>
public sealed record VoiceTarget(string VoiceType, VoiceTemplateSpec Template, string NpcEditorId);

public static partial class Generator
{
    /// <summary>
    /// Finds who speaks an INFO. Chain (first hit wins):
    ///  1. GetIsID(npc) condition          — custom dialogue / banter / hello (Build's auto gate);
    ///  2. GetIsAliasRef(i) condition      — alias index → host quest's alias → uniqueActor NPC or
    ///                                       forcedReference ACHR → its base NPC;
    ///  3. GetInFaction(f) condition       — ALL plugin NPCs that are members (multi-speaker);
    ///  4. Scene Dialog action             — Build's scene INFOs carry NO conditions; the speaker is
    ///                                       the SCEN action's ActorID alias on the scene's quest.
    /// Only Subject-run conditions count: identity gates are GetInFaction run on the PLAYER ref and
    /// say nothing about the speaker.
    /// </summary>
    public static VoiceSpeakerResolution ResolveVoiceSpeakers(
        IDialogTopicGetter topic, IDialogResponsesGetter info, ISkyrimModGetter mod, ILinkCache cache)
    {
        var notes = new List<string>();

        foreach (var cond in info.Conditions)
        {
            if (cond.Data is not IGetIsIDConditionDataGetter isId || isId.RunOnType != Condition.RunOnType.Subject) continue;
            if (isId.Object.Link.IsNull) { notes.Add("GetIsID has a null object"); continue; }
            if (isId.Object.Link.TryResolve<INpcGetter>(cache) is { } npc) return Single(npc, cache, "GetIsID");
            notes.Add($"GetIsID target {isId.Object.Link.FormKey} did not resolve to an NPC");
        }

        foreach (var cond in info.Conditions)
        {
            if (cond.Data is not IGetIsAliasRefConditionDataGetter ar || ar.RunOnType != Condition.RunOnType.Subject) continue;
            var quest = topic.Quest.TryResolve(cache);
            var alias = quest?.Aliases.FirstOrDefault(a => a.ID == (uint)ar.ReferenceAliasIndex);
            if (alias is null)
            { notes.Add($"GetIsAliasRef alias #{ar.ReferenceAliasIndex} not found on quest '{quest?.EditorID ?? topic.Quest.FormKey.ToString()}'"); continue; }
            if (ResolveAliasNpc(alias, cache, out var why) is { } npc) return Single(npc, cache, "GetIsAliasRef");
            notes.Add($"GetIsAliasRef alias #{ar.ReferenceAliasIndex} ('{alias.Name}'): {why}");
        }

        foreach (var cond in info.Conditions)
        {
            if (cond.Data is not IGetInFactionConditionDataGetter gif || gif.RunOnType != Condition.RunOnType.Subject) continue;
            if (gif.Faction.Link.IsNull) { notes.Add("GetInFaction has a null faction"); continue; }
            var fk = gif.Faction.Link.FormKey;
            var members = mod.Npcs
                .Where(n => n.Factions.Any(f => f.Faction.FormKey == fk))
                .Select(n => MakeSpeaker(n, cache))
                .ToList();
            if (members.Count > 0) return new(members, "GetInFaction", null);
            var facEd = cache.TryResolve<IFactionGetter>(fk, out var fac) ? fac.EditorID : null;
            notes.Add($"GetInFaction '{facEd ?? fk.ToString()}': no NPC in this plugin is a member");
        }

        // Scene fallback: Build emits scene-phase INFOs with no conditions at all — the binding lives
        // on the SCEN record (Dialog action: ActorID alias speaks Topic).
        foreach (var scene in mod.Scenes)
            foreach (var act in scene.Actions)
            {
                if (act.Type != SceneAction.TypeEnum.Dialog || act.Topic.FormKey != topic.FormKey) continue;
                var quest = scene.Quest.TryResolve(cache);
                var alias = act.ActorID is { } id ? quest?.Aliases.FirstOrDefault(a => a.ID == (uint)id) : null;
                if (alias is null)
                { notes.Add($"scene '{scene.EditorID}' speaks this topic but alias #{act.ActorID} not found on its quest"); continue; }
                if (ResolveAliasNpc(alias, cache, out var why) is { } npc) return Single(npc, cache, "SceneAction");
                notes.Add($"scene '{scene.EditorID}' alias #{act.ActorID} ('{alias.Name}'): {why}");
            }

        string reason = notes.Count > 0
            ? string.Join("; ", notes)
            : info.Conditions.Count == 0
                ? "INFO has no conditions and no scene Dialog action references its topic"
                : "no speaker-identifying condition (GetIsID/GetIsAliasRef/GetInFaction run on Subject); "
                  + $"found: {string.Join(", ", info.Conditions.Select(CondFuncName).Distinct())}";
        return new(Array.Empty<VoiceSpeaker>(), "", reason);
    }

    /// <summary>
    /// Reduces resolved speakers to the (voiceType, template) pairs to actually generate: one entry
    /// per DISTINCT voiceType, keeping only speakers that have a voiceTemplate in the spec
    /// (<paramref name="templateByNpcEd"/> = NPC editorId → its resolved template, null when the
    /// id named a template that doesn't exist).
    /// </summary>
    public static List<VoiceTarget> SelectVoiceTargets(
        VoiceSpeakerResolution res,
        IReadOnlyDictionary<string, VoiceTemplateSpec?> templateByNpcEd,
        IReadOnlyDictionary<string, string>? voiceTypeByNpcEd = null)
    {
        var targets = new List<VoiceTarget>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sp in res.Speakers)
        {
            var vt = EffectiveVoiceType(sp, voiceTypeByNpcEd);
            if (seen.Contains(vt)) continue;                  // one file per voiceType folder
            if (!templateByNpcEd.TryGetValue(sp.Npc.EditorID ?? "", out var tpl) || tpl is null) continue;
            seen.Add(vt);
            targets.Add(new VoiceTarget(vt, tpl, sp.Npc.EditorID ?? ""));
        }
        return targets;
    }

    private static VoiceSpeakerResolution Single(INpcGetter npc, ILinkCache cache, string source) =>
        new(new[] { MakeSpeaker(npc, cache) }, source, null);

    private static VoiceSpeaker MakeSpeaker(INpcGetter npc, ILinkCache cache) =>
        new(npc, npc.Voice.TryResolve(cache)?.EditorID);

    private static string EffectiveVoiceType(VoiceSpeaker sp, IReadOnlyDictionary<string, string>? voiceTypeByNpcEd)
    {
        if (!string.IsNullOrEmpty(sp.VoiceType)) return sp.VoiceType!;
        if (voiceTypeByNpcEd is not null
            && sp.Npc.EditorID is { Length: > 0 } ed
            && voiceTypeByNpcEd.TryGetValue(ed, out var vt)
            && !string.IsNullOrWhiteSpace(vt))
            return vt;
        return "DefaultVoice";
    }

    // uniqueActor → the NPC base directly; forcedReference → the placed ACHR → its base NPC.
    private static INpcGetter? ResolveAliasNpc(IQuestAliasGetter alias, ILinkCache cache, out string why)
    {
        if (!alias.UniqueActor.IsNull)
        {
            var npc = alias.UniqueActor.TryResolve(cache);
            why = npc is null ? $"uniqueActor {alias.UniqueActor.FormKey} did not resolve (external master not loaded?)" : "";
            return npc;
        }
        if (!alias.ForcedReference.IsNull)
        {
            if (cache.TryResolve<IPlacedNpcGetter>(alias.ForcedReference.FormKey, out var achr))
            {
                var npc = achr.Base.TryResolve(cache);
                why = npc is null ? $"forced ref {alias.ForcedReference.FormKey}'s base NPC did not resolve" : "";
                return npc;
            }
            why = $"forced ref {alias.ForcedReference.FormKey} is not a placed NPC (ACHR) in this plugin";
            return null;
        }
        why = "alias has neither uniqueActor nor forcedReference (event/findMatching fills are runtime-only)";
        return null;
    }

    private static string CondFuncName(IConditionGetter c)
    {
        var n = c.Data.GetType().Name;
        var i = n.IndexOf("ConditionData", StringComparison.Ordinal);
        return i > 0 ? n[..i] : n;
    }
}
