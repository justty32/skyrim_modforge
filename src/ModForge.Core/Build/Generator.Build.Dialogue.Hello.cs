namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // -------------------------------------------------------------------------------
        //  Hello (greeting) topics. Split out of Generator.Build.Dialogue.cs (2026-08-27) —
        //  greetings are a genuinely separate shape from player topics: they carry NO branch,
        //  NPC-initiated rather than player-menu, and several greetings COMBINE into a single
        //  Hello topic per (speaker, quest) instead of getting one topic each.
        // -------------------------------------------------------------------------------

        // A conditioned greeting (hello:true): build its INFO and stash it under (speaker|quest).
        // It gets no topic here — BuildNpcHellos folds every stashed INFO into that NPC's one Hello.
        private void BuildConditionedHello(DialogueSpec d, Quest quest,
                                           Dictionary<string, List<DialogResponses>> conditionedHellos)
        {
            var hinfo = new DialogResponses(mod)
            {
                EditorID = d.EditorId,
                Flags = new DialogResponseFlags { Flags = DialogueInfoFlags(d) },
                FavorLevel = FavorLevel.None,
            };
            dialogResponsesByEd[d.EditorId] = hinfo;
            var hem = Enum.TryParse<Emotion>(d.Emotion, ignoreCase: true, out var he) ? he : Emotion.Neutral;
            byte hrn = 1;
            foreach (var line in d.Responses)
                hinfo.Responses.Add(new DialogResponse { Text = line, ResponseNumber = hrn++, Emotion = hem, EmotionValue = d.EmotionValue });
            if (!string.IsNullOrEmpty(d.SpeakerNpcEditorId) && npcsByEd.TryGetValue(d.SpeakerNpcEditorId, out var hspk))
            {
                var hc = new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = new GetIsIDConditionData() };
                ((GetIsIDConditionData)hc.Data).Object.Link.SetTo(hspk);
                hinfo.Conditions.Add(hc);
            }
            // External captured NPC (Idea #24 §D): speaker is a "<plugin>.esp:0xID" base NPC not
            // in this spec — gate on its FormKey so the greeting is still bound to that one NPC.
            else if (!string.IsNullOrEmpty(d.SpeakerNpcEditorId) && TryResolveRef(d.SpeakerNpcEditorId, formKeyByEd, out var hextFk))
            {
                var hc = new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = new GetIsIDConditionData() };
                ((GetIsIDConditionData)hc.Data).Object.Link.SetTo(hextFk);
                hinfo.Conditions.Add(hc);
            }
            else if (string.IsNullOrEmpty(d.SpeakerNpcEditorId))
                Warn($"  ! dialogue '{d.EditorId}' hello: no speaker — every NPC would use this greeting");
            else
                Warn($"  ! dialogue '{d.EditorId}' hello: speaker '{d.SpeakerNpcEditorId}' not found in spec or as an external ref — greeting has NO speaker gate");
            var hkey = d.SpeakerNpcEditorId + "|" + quest.FormKey;
            if (!conditionedHellos.TryGetValue(hkey, out var hlist))
                conditionedHellos[hkey] = hlist = new List<DialogResponses>();
            hlist.Add(hinfo);
        }

        // Emit the Hello topics themselves: one per (speaker, quest) for NPCs that have dialogue,
        // plus one per greeting-only NPC (which also needs its own host quest).
        private void BuildNpcHellos(Dictionary<string, List<DialogResponses>> conditionedHellos)
        {
            // Hello (greeting) per speaking NPC: WITHOUT one the NPC is not conversable — activating it
            // never opens the dialogue menu, so the player topics above never surface (you just get
            // voicetype mumbles). Vanilla talkable NPCs all carry a Hello (Category=Misc, Subtype=Hello,
            // SNAM='HELO', no branch, gated on GetIsID). Emit one per (speaker, quest), keyed so multiple
            // topics from the same NPC share a single Hello.
            var npcSpecByEd = spec.Npcs.Where(n => !string.IsNullOrEmpty(n.EditorId))
                                       .GroupBy(n => n.EditorId).ToDictionary(g => g.Key, g => g.First());
            // Local: emit one Hello (Misc/Hello/HELO, no branch, GetIsID(speaker), ENAM+CNAM) under a quest.
            // speakerFk gates the fallback greeting via GetIsID. Accepts an in-spec NPC's FormKey OR an
            // external captured NPC's FormKey (Idea #24 §D) — GetIsIDConditionData.Object.Link takes a FormKey.
            void MakeHello(string npcEd, FormKey speakerFk, Quest quest, string? greetingLine)
            {
                var hello = mod.DialogTopics.AddNew();
                hello.EditorID = $"{SanitizeEd(npcEd)}_{quest.EditorID}_Hello";
                hello.Quest.SetTo(quest);
                hello.Category = DialogTopic.CategoryEnum.Misc;
                hello.Subtype = DialogTopic.SubtypeEnum.Hello;
                hello.SubtypeName = new RecordType("HELO");
                hello.Priority = 50f;   // no Branch — Hello is NPC-initiated, not a player menu branch
                // Conditioned greetings (hello:true dialogue) FIRST — the engine plays the first INFO whose
                // conditions pass, so the state-specific greetings must precede the unconditional fallback.
                if (conditionedHellos.TryGetValue(npcEd + "|" + quest.FormKey, out var conds))
                    foreach (var ci in conds) hello.Responses.Add(ci);
                // Plain greeting LAST = the fallback when no conditioned greeting matches.
                var greet = !string.IsNullOrWhiteSpace(greetingLine) ? greetingLine! : "Yes? What do you need?";
                var hinfo = new DialogResponses(mod) { Flags = new DialogResponseFlags(), FavorLevel = FavorLevel.None };
                hinfo.Responses.Add(new DialogResponse { Text = greet, ResponseNumber = 1, Emotion = Emotion.Neutral, EmotionValue = 50 });
                var hcond = new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = new GetIsIDConditionData() };
                ((GetIsIDConditionData)hcond.Data).Object.Link.SetTo(speakerFk);
                hinfo.Conditions.Add(hcond);
                hello.Responses.Add(hinfo);
            }

            var helloDone = new HashSet<string>();      // (speakerEd|quest) pairs already given a Hello
            var helloedNpcs = new HashSet<string>();     // npc editorIds that now have SOME Hello
            foreach (var d in spec.Dialogue)
            {
                if (string.IsNullOrEmpty(d.SpeakerNpcEditorId)) continue;
                if (string.IsNullOrEmpty(d.QuestEditorId) || !questsByEd.TryGetValue(d.QuestEditorId, out var quest)) continue;
                // In-spec NPC → its FormKey; else an external captured NPC (§D) resolved by ref. Skip only
                // if neither resolves (a truly unknown speaker — the conditioned INFO already warned).
                FormKey speakerFk;
                if (npcsByEd.TryGetValue(d.SpeakerNpcEditorId, out var speaker)) speakerFk = speaker.FormKey;
                else if (!TryResolveRef(d.SpeakerNpcEditorId, formKeyByEd, out speakerFk)) continue;
                if (!helloDone.Add(d.SpeakerNpcEditorId + "|" + quest.FormKey)) continue;
                npcSpecByEd.TryGetValue(d.SpeakerNpcEditorId, out var ns);
                MakeHello(d.SpeakerNpcEditorId, speakerFk, quest, ns?.Greeting);
                helloedNpcs.Add(d.SpeakerNpcEditorId);
            }

            // Greeting-only NPCs (e.g. a hireable follower that uses the vanilla DialogueFollower topics
            // and has NO custom dialogue[]): they still need a Hello to be conversable at all — a custom
            // NPC is NOT made talkable by vanilla generic/follower dialogue alone (IN-GAME confirmed: a
            // follower with PotentialFollowerFaction + Ally relationship but no Hello just mumbles, the
            // "Follow me" topic never shows because the dialogue camera never opens). Give each such NPC
            // its own StartGameEnabled host quest + a Hello so activating it opens dialogue; the vanilla
            // follow/trade/dismiss topics then surface on top.
            foreach (var n in spec.Npcs)
            {
                if (string.IsNullOrEmpty(n.EditorId) || string.IsNullOrWhiteSpace(n.Greeting)) continue;
                if (helloedNpcs.Contains(n.EditorId)) continue;
                if (!npcsByEd.TryGetValue(n.EditorId, out var speaker)) continue;

                var gq = mod.Quests.AddNew();
                gq.EditorID = n.EditorId + "_GreetQuest";
                gq.Name = string.Empty;
                gq.Flags |= Quest.Flag.StartGameEnabled;   // must run so its Hello is served
                gq.Priority = 50;
                MakeHello(n.EditorId, speaker.FormKey, gq, n.Greeting);
                helloedNpcs.Add(n.EditorId);
            }
        }
    }
}