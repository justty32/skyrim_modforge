namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: proactive banter — unprompted NPC lines (the vanilla HirelingIdles pattern) ---
        // All banter entries sharing a (speaker, quest) collapse into ONE ambient topic: Category=Misc,
        // SNAM='IDLE', no branch, one Random-flagged INFO per entry. The engine plays a matching one
        // unprompted whenever the speaker has idle chatter enabled (a Sandbox/follow package with the
        // AllowIdleChatter interrupt flag). Each INFO gets the auto GetIsID(speaker) gate here; its
        // situational conditions are appended in pass 2 (WireBanterConditions) once the formKey table exists.
        public void BuildBanter()
        {
            var topicByKey = new Dictionary<(string quest, string npc), DialogTopic>();
            int idx = 0;
            foreach (var b in spec.Banter)
            {
                if (string.IsNullOrEmpty(b.QuestEditorId) || !questsByEd.TryGetValue(b.QuestEditorId, out var quest))
                { Warn($"  ! banter '{b.EditorId}' skipped: quest '{b.QuestEditorId}' not found in spec"); continue; }
                if (string.IsNullOrEmpty(b.SpeakerNpcEditorId) || !npcsByEd.TryGetValue(b.SpeakerNpcEditorId, out var speaker))
                { Warn($"  ! banter '{b.EditorId}' skipped: speaker '{b.SpeakerNpcEditorId}' not found in spec"); continue; }

                var key = (b.QuestEditorId, b.SpeakerNpcEditorId);
                if (!topicByKey.TryGetValue(key, out var topic))
                {
                    topic = mod.DialogTopics.AddNew();
                    topic.EditorID = b.SpeakerNpcEditorId + "_Banter";
                    topic.Quest.SetTo(quest);
                    topic.Category = DialogTopic.CategoryEnum.Misc;     // ambient / NPC-initiated, like Hello
                    topic.Subtype = DialogTopic.SubtypeEnum.Idle;
                    topic.SubtypeName = new RecordType("IDLE");          // SNAM the engine dispatches idle dialogue on
                    topic.Priority = 50f;                                // no Branch — Idle is not a player menu option
                    topicByKey[key] = topic;
                }

                // Random (ENAM) lets the engine random-pick among idle INFOs whose conditions pass; CNAM via FavorLevel.
                var info = new DialogResponses(mod)
                {
                    Flags = new DialogResponseFlags { Flags = DialogResponses.Flag.Random },
                    FavorLevel = FavorLevel.None,
                };
                var emotion = Enum.TryParse<Emotion>(b.Emotion, ignoreCase: true, out var em) ? em : Emotion.Neutral;
                byte rn = 1;
                foreach (var line in b.Responses)
                    info.Responses.Add(new DialogResponse { Text = line, ResponseNumber = rn++, Emotion = emotion, EmotionValue = b.EmotionValue });

                var cond = new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = new GetIsIDConditionData() };
                ((GetIsIDConditionData)cond.Data).Object.Link.SetTo(speaker);
                info.Conditions.Add(cond);

                topic.Responses.Add(info);
                banterInfos.Add((b, info, string.IsNullOrEmpty(b.EditorId) ? $"banter #{idx}" : $"banter '{b.EditorId}'"));
                idx++;
                banterBuilt++;
            }
        }

        // pass 2: append each banter INFO's situational conditions (after its auto GetIsID speaker gate).
        public void WireBanterConditions()
        {
            foreach (var (b, info, label) in banterInfos)
                foreach (var c in b.Conditions)
                    if (BuildCondition(c, $"{label} condition") is { } cond) info.Conditions.Add(cond);
        }
    }
}
