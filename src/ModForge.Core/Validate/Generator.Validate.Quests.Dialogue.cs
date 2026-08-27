namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Continues ValidateQuestsAndDialogue (Generator.Validate.Quests.cs) over the M組 condition
        // templates and the DIAL/INFO entries. Split out verbatim; no logic changed.

        // M組 condition templates: non-empty unique names; each condition structurally valid.
        private void ValidateConditionTemplates()
        {
            var seenTemplates = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var t in spec.ConditionTemplates)
            {
                if (string.IsNullOrWhiteSpace(t.Name)) { Problems.Add("conditionTemplate has empty 'name'"); continue; }
                if (!seenTemplates.Add(t.Name)) Problems.Add($"conditionTemplate '{t.Name}' is defined more than once");
                foreach (var c in t.Conditions) CheckCondition(c, $"conditionTemplate '{t.Name}'");
            }
        }

        // DIAL/INFO: host quest, linkTo/previousDialog targets, setStage against the quest's own
        // stage indices, speaker, condition templates, identity/global/reward side effects, and the
        // M組 variant batches.
        private void ValidateDialogueRecords(Dictionary<string, HashSet<int>> stageIndexByQuest)
        {
            var dialogueIds = new HashSet<string>(spec.Dialogue.Select(x => x.EditorId), StringComparer.OrdinalIgnoreCase);
            foreach (var d in spec.Dialogue)
            {
                if (!questIds.Contains(d.QuestEditorId)) Problems.Add($"dialogue '{d.EditorId}' references unknown quest '{d.QuestEditorId}'");
                foreach (var lt in d.LinkTo)
                    if (!dialogueIds.Contains(lt) && !LooksExternalRef(lt))
                        Problems.Add($"dialogue '{d.EditorId}' linkTo '{lt}' is not a known dialogue editorId or a <master>:0xID ref");
                if (!string.IsNullOrWhiteSpace(d.PreviousDialog) && !dialogueIds.Contains(d.PreviousDialog) && !LooksExternalRef(d.PreviousDialog))
                    Problems.Add($"dialogue '{d.EditorId}' previousDialog '{d.PreviousDialog}' is not a known dialogue editorId or a <master>:0xID ref");
                if (d.SetStage >= 0)
                {
                    if (!stageIndexByQuest.TryGetValue(d.QuestEditorId, out var stages) || !stages.Contains(d.SetStage))
                        Problems.Add($"dialogue '{d.EditorId}' setStage {d.SetStage} has no matching stage in quest '{d.QuestEditorId}'");
                }
                if (!string.IsNullOrEmpty(d.SpeakerNpcEditorId) && !npcIds.Contains(d.SpeakerNpcEditorId))
                    Problems.Add($"dialogue '{d.EditorId}' references unknown speaker npc '{d.SpeakerNpcEditorId}'");
                // M組: every referenced condition template must exist.
                foreach (var tname in d.UseConditionTemplates)
                    if (!spec.ConditionTemplates.Any(t => string.Equals(t.Name, tname, System.StringComparison.OrdinalIgnoreCase)))
                        Problems.Add($"dialogue '{d.EditorId}' useConditionTemplates references unknown template '{tname}'");
                if (!string.IsNullOrWhiteSpace(d.SetPrimaryIdentity)
                    && !string.Equals(d.SetPrimaryIdentity, "auto", System.StringComparison.OrdinalIgnoreCase)
                    && !spec.Identities.Any(i => string.Equals(i.Id, d.SetPrimaryIdentity, System.StringComparison.OrdinalIgnoreCase)))
                    Problems.Add($"dialogue '{d.EditorId}' setPrimaryIdentity '{d.SetPrimaryIdentity}' is not a known identity id (or 'auto')");
                if (d.SetGlobal is { } sg)
                {
                    if (string.IsNullOrWhiteSpace(sg.Global)) Problems.Add($"dialogue '{d.EditorId}' setGlobal has empty global ref");
                    else
                    {
                        CheckRef(sg.Global, $"dialogue '{d.EditorId}' setGlobal global");
                        var target = spec.Globals.FirstOrDefault(g => string.Equals(g.EditorId, sg.Global, StringComparison.OrdinalIgnoreCase));
                        if (target?.Constant == true)
                            Problems.Add($"dialogue '{d.EditorId}' setGlobal targets constant global '{sg.Global}'");
                    }
                    if (sg.Value.HasValue == sg.Delta.HasValue)
                        Problems.Add($"dialogue '{d.EditorId}' setGlobal must set exactly one of value or delta");
                }
                if (!string.IsNullOrWhiteSpace(d.RewardItem)) CheckRef(d.RewardItem, $"dialogue '{d.EditorId}' rewardItem");
                // A dialogue line runs in a TIF fragment, so "speaker" (akSpeakerRef) is allowed here.
                if (d.Persist is { } pst) ValidatePersistBlock(pst, $"dialogue '{d.EditorId}' persist", allowSpeaker: true);
                if (d.SyncPerks is { } syp) ValidateSyncPerksBlock(syp, $"dialogue '{d.EditorId}' syncPerks", allowSpeaker: true);
                ValidateStorageWrites(d.StorageWrites, $"dialogue '{d.EditorId}' storageWrite", allowSpeaker: true);
                // A `hello:true` line is the NPC's auto-spoken greeting (Misc/Hello), not a player menu
                // option, so it has no prompt by design — only require a prompt for normal player topics.
                if (!d.Hello && string.IsNullOrEmpty(d.Prompt)) Problems.Add($"dialogue '{d.EditorId}' has empty prompt");
                // A normal entry needs response lines; a M組 variant batch may carry its lines in `variants`
                // instead (the parent `responses` is then an optional extra sibling, allowed to be empty).
                if (d.Responses.Count == 0 && d.Variants.Count == 0) Problems.Add($"dialogue '{d.EditorId}' has no response lines");
                if (!Enum.TryParse<Emotion>(d.Emotion, true, out _))
                    Problems.Add($"dialogue '{d.EditorId}' invalid emotion '{d.Emotion}' (Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise|Puzzled)");
                // M組 INFO batch variants: a Hello path doesn't emit variants; each variant needs lines, a
                // valid emotion, and its own conditions must be well-formed.
                if (d.Variants.Count > 0 && d.Hello)
                    Problems.Add($"dialogue '{d.EditorId}' variants are not supported on a hello line");
                for (int vi = 0; vi < d.Variants.Count; vi++)
                {
                    var v = d.Variants[vi];
                    if (v.Responses.Count == 0) Problems.Add($"dialogue '{d.EditorId}' variant {vi} has no response lines");
                    if (!string.IsNullOrEmpty(v.Emotion) && !Enum.TryParse<Emotion>(v.Emotion, true, out _))
                        Problems.Add($"dialogue '{d.EditorId}' variant {vi} invalid emotion '{v.Emotion}'");
                    foreach (var c in v.Conditions) CheckCondition(c, $"dialogue '{d.EditorId}' variant {vi}");
                }
            }
        }
    }
}
