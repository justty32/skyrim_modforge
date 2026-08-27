namespace ModForge;

public static partial class Generator
{
    // Combine a dialogue spec's INFO (ENAM) behaviour flags into one DialogResponses.Flag value.
    // Shared by the hello and player-topic build sites so both honour the same set.
    internal static DialogResponses.Flag DialogueInfoFlags(DialogueSpec d)
    {
        DialogResponses.Flag f = default;
        if (d.Goodbye)            f |= DialogResponses.Flag.Goodbye;
        if (d.SayOnce)            f |= DialogResponses.Flag.SayOnce;
        if (d.WalkAway)           f |= DialogResponses.Flag.WalkAway;
        if (d.Random)             f |= DialogResponses.Flag.Random;
        if (d.InvisibleContinue)  f |= DialogResponses.Flag.InvisibleContinue;
        if (d.ForceSubtitle)      f |= DialogResponses.Flag.ForceSubtitle;
        return f;
    }

    // The editorId / dialogResponsesByEd key of variant INFO <i> of a dialogue batch (M組). Stable across
    // pass 1 (build) and pass 2 (condition wiring) so each variant's gates land on the right INFO.
    internal static string DialogueVariantId(string parentEditorId, int i) => $"{parentEditorId}_v{i}";

    internal sealed partial class BuildContext
    {
        // Player DialogTopics by editorId (topic & INFO share an editorId, so formKeyByEd collides —
        // this is the reliable way to resolve a dialogue's TOPIC, e.g. for an ENAM LinkTo target).
        private readonly Dictionary<string, DialogTopic> dialogTopicsByEd = new();
        // Stats counters (accumulated across the steps, read by ToResult).
        private int dialogueBuilt;

        // --- pass 1: native dialogue — Quest -> DialogBranch -> DialogTopic -> DialogResponses(INFO),
        // a DialogView (DLVW) per quest, and a Hello per speaking NPC (incl. greeting-only NPCs).
        // (Writes valid records; making the line actually surface in-game still needs quest-flag
        // tuning + Proton testing — see docs/lifelike/gotchas.md.)
        public void BuildDialogue()
        {
            var branchesByQuest = new Dictionary<string, (Quest quest, List<DialogBranch> branches)>();
            // Each player topic needs a priority that is DISTINCT ACROSS THE WHOLE PLUGIN (not just per
            // quest). The engine sorts ALL running quests' top-level player topics by priority while
            // building the menu, BEFORE applying GetIsID conditions; any priority collision among them
            // collapses the menu so NOTHING shows (and the NPCs' whole dialogue, incl. Hellos, dies).
            // Per-quest-distinct is NOT enough — two NPCs each with a priority-90 topic still collide.
            // Vanilla never has two simultaneously-loaded top-level topics share a priority. So use one
            // GLOBAL descending counter for every player topic in the plugin (90, 89, 88, …).
            float nextTopicPriority = 90f;
            // Conditioned greetings (hello:true): vanilla puts ALL of an NPC's greetings as multiple INFOs
            // inside ONE Hello topic and plays the FIRST whose conditions pass (INFO order = precedence;
            // 237/297 vanilla Hello topics have >1 INFO). Separate Hello topics do NOT compose — the engine
            // serves one topic's INFO and ignores the rest. So stash each conditioned greeting's INFO here,
            // keyed (speaker|quest); MakeHello assembles them into the single Hello topic (conditioned first,
            // plain greeting last). Identity/user conditions wire in pass 2 via dialogResponsesByEd.
            var conditionedHellos = new Dictionary<string, List<DialogResponses>>();
            foreach (var d in spec.Dialogue)
            {
                if (string.IsNullOrEmpty(d.QuestEditorId) || !questsByEd.TryGetValue(d.QuestEditorId, out var quest))
                {
                    Warn($"  ! dialogue '{d.EditorId}' skipped: quest '{d.QuestEditorId}' not found in spec");
                    continue;
                }

                // A greeting (hello:true) produces no topic of its own — it is stashed and later
                // assembled into the ONE Hello topic that NPC gets. See Generator.Build.Dialogue.Hello.cs.
                if (d.Hello)
                {
                    BuildConditionedHello(d, quest, conditionedHellos);
                    dialogueBuilt++;
                    continue;
                }

                BuildPlayerTopic(d, quest, ref nextTopicPriority, branchesByQuest);
                dialogueBuilt++;
            }

            BuildDialogueViews(branchesByQuest);
            BuildNpcHellos(conditionedHellos);
        }

        // One player-facing dialogue entry: its DialogBranch + DialogTopic, the parent INFO, and any
        // M組 variant INFOs. Advances the plugin-global topic priority (hence `ref`) and records the
        // branch under its quest so BuildDialogueViews can tie them together.
        private void BuildPlayerTopic(DialogueSpec d, Quest quest, ref float nextTopicPriority,
                                      Dictionary<string, (Quest quest, List<DialogBranch> branches)> branchesByQuest)
        {
            var branch = mod.DialogBranches.AddNew();
            branch.EditorID = d.EditorId + "_Br";
            branch.Quest.SetTo(quest);
            branch.Category = DialogBranch.CategoryType.Player;
            // TopLevel = this branch is a top-level menu option shown the moment you talk to the NPC
            // (vs. a sub-branch reachable only from another topic, via an ENAM LinkTo). A normal
            // dialogue is top-level; a tree SUB-topic sets topLevel:false so it only appears when linked.
            branch.Flags = d.TopLevel ? DialogBranch.Flag.TopLevel : default(DialogBranch.Flag);

            var topic = mod.DialogTopics.AddNew();
            topic.EditorID = d.EditorId;
            topic.Quest.SetTo(quest);
            topic.Branch.SetTo(branch);
            topic.Category = DialogTopic.CategoryEnum.Topic;
            topic.Subtype = DialogTopic.SubtypeEnum.Custom;
            // SNAM must be the 4-char subtype code "CUST" (matches the Custom enum). Leaving it
            // RecordType.Null writes SNAM=0x00000000, which CRASHES the engine at load when it
            // builds the dialogue-topic index (vanilla Custom topics all carry SNAM='CUST').
            topic.SubtypeName = new RecordType("CUST");
            topic.Name = d.Prompt;
            // plugin-global distinct descending priority (90, 89, 88, …) — see note above.
            topic.Priority = nextTopicPriority;
            nextTopicPriority -= 1f;
            branch.StartingTopic.SetTo(topic);
            dialogTopicsByEd[d.EditorId] = topic;   // for ENAM LinkTo resolution in pass 2

            // INFO carries the spoken response(s). Leave ResponseData null (so it uses our own
            // Responses, not a shared INFO) and Prompt null (the menu line comes from topic.Name).
            // Flags (ENAM) + FavorLevel (CNAM) MUST be present: a vanilla player INFO always carries
            // both, and an INFO missing ENAM is treated as invalid — a topic whose only INFO is
            // invalid is silently dropped from the menu (so the topic never appears at all).
            // The auto GetIsID speaker gate every INFO under this topic carries (without it EVERY NPC
            // would speak the line). Shared by the parent INFO and each variant INFO.
            void AddSpeakerGate(DialogResponses inf)
            {
                if (string.IsNullOrEmpty(d.SpeakerNpcEditorId)) return;
                if (npcsByEd.TryGetValue(d.SpeakerNpcEditorId, out var speaker))
                {
                    var cond = new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = new GetIsIDConditionData() };
                    ((GetIsIDConditionData)cond.Data).Object.Link.SetTo(speaker);
                    inf.Conditions.Add(cond);
                }
                // External captured NPC (Idea #24 §D): resolve a "<plugin>.esp:0xID" base NPC by FormKey.
                else if (TryResolveRef(d.SpeakerNpcEditorId, formKeyByEd, out var extFk))
                {
                    var cond = new ConditionFloat { CompareOperator = CompareOperator.EqualTo, ComparisonValue = 1f, Data = new GetIsIDConditionData() };
                    ((GetIsIDConditionData)cond.Data).Object.Link.SetTo(extFk);
                    inf.Conditions.Add(cond);
                }
                else
                    Warn($"  ! dialogue '{d.EditorId}' speaker '{d.SpeakerNpcEditorId}' not found in spec or as an external ref — line has NO speaker gate (any NPC may say it)");
            }
            var emotion = Enum.TryParse<Emotion>(d.Emotion, ignoreCase: true, out var em) ? em : Emotion.Neutral;

            // Parent INFO — built unless this is a pure variant batch (M組: variants set + no own
            // responses, so the entry is just a batch header and emits no parent line).
            if (d.Responses.Count > 0 || d.Variants.Count == 0)
            {
                var info = new DialogResponses(mod)
                {
                    EditorID = d.EditorId,
                    // Goodbye (DialogResponses.Flag, not the per-line DialogResponse.Flag) closes the menu
                    // after the line — set for recruit/dismiss-style lines that carry a result fragment.
                    Flags = new DialogResponseFlags { Flags = DialogueInfoFlags(d) },
                    FavorLevel = FavorLevel.None,
                };
                dialogResponsesByEd[d.EditorId] = info;
                byte rn = 1;
                foreach (var line in d.Responses)
                    info.Responses.Add(new DialogResponse { Text = line, ResponseNumber = rn++, Emotion = emotion, EmotionValue = d.EmotionValue });
                AddSpeakerGate(info);
                topic.Responses.Add(info);
            }

            // M組 variant INFOs — each a sibling under THIS topic with the Random flag (the engine
            // random-picks among siblings whose conditions currently pass = ambient-commentary variety).
            // Shares the speaker gate; the parent's shared conditions/templates/identity + each variant's
            // own conditions wire in pass 2 (WireDialogueConditions), keyed by DialogueVariantId.
            for (int vi = 0; vi < d.Variants.Count; vi++)
            {
                var v = d.Variants[vi];
                DialogResponses.Flag vflags = DialogResponses.Flag.Random;
                if (v.SayOnce) vflags |= DialogResponses.Flag.SayOnce;
                var vinfo = new DialogResponses(mod)
                {
                    EditorID = DialogueVariantId(d.EditorId, vi),
                    Flags = new DialogResponseFlags { Flags = vflags },
                    FavorLevel = FavorLevel.None,
                };
                dialogResponsesByEd[DialogueVariantId(d.EditorId, vi)] = vinfo;
                var vem = !string.IsNullOrEmpty(v.Emotion) && Enum.TryParse<Emotion>(v.Emotion, ignoreCase: true, out var ve) ? ve : emotion;
                var vev = v.EmotionValue ?? d.EmotionValue;
                byte vrn = 1;
                foreach (var line in v.Responses)
                    vinfo.Responses.Add(new DialogResponse { Text = line, ResponseNumber = vrn++, Emotion = vem, EmotionValue = vev });
                AddSpeakerGate(vinfo);
                topic.Responses.Add(vinfo);
            }

            if (!branchesByQuest.TryGetValue(d.QuestEditorId, out var bag))
                branchesByQuest[d.QuestEditorId] = bag = (quest, new List<DialogBranch>());
            bag.branches.Add(branch);
        }

        // DialogView (DLVW) per quest — see the comment inside.
        private void BuildDialogueViews(Dictionary<string, (Quest quest, List<DialogBranch> branches)> branchesByQuest)
        {
            // DialogView (DLVW) per quest: ties the player branches to the quest. Every vanilla dialogue
            // branch belongs to a view; without it the engine never serves the quest's player topics, so
            // the NPC can't even be talked to (activating it opens no dialogue camera). ENAM/DNAM mirror
            // vanilla defaults (4 zero bytes / single 1 byte).
            foreach (var (questEd, bag) in branchesByQuest)
            {
                var view = mod.DialogViews.AddNew();
                view.EditorID = questEd + "_View";
                view.Quest.SetTo(bag.quest);
                foreach (var b in bag.branches)
                    view.Branches.Add(b);
                view.ENAM = new byte[] { 0, 0, 0, 0 };  // mirror vanilla DLVW
                view.DNAM = new byte[] { 1 };            // single flag byte vanilla views carry
            }
        }


        // --- pass 2: dialogue TREE links (ENAM LinkTo + PNAM PreviousDialog). Runs after every topic +
        // INFO exists so a line can link forward or back to another. LinkTo targets a TOPIC (resolved via
        // dialogTopicsByEd, since topic & INFO share an editorId); PreviousDialog targets an INFO. ---
        public void WireDialogueLinks()
        {
            foreach (var d in spec.Dialogue)
            {
                if (d.Hello) continue;
                if (!dialogResponsesByEd.TryGetValue(d.EditorId, out var info)) continue;

                foreach (var target in d.LinkTo)
                {
                    if (dialogTopicsByEd.TryGetValue(target, out var t))
                        info.LinkTo.Add(new FormLink<IDialogTopicGetter>(t.FormKey));
                    else if (TryExternalRef(target, out var fk))   // a vanilla topic
                        info.LinkTo.Add(new FormLink<IDialogTopicGetter>(fk));
                    else
                        Warn($"  ! dialogue '{d.EditorId}' linkTo '{target}' is not a built dialogue topic or a <master>:0xID ref");
                }

                if (!string.IsNullOrWhiteSpace(d.PreviousDialog))
                {
                    if (dialogResponsesByEd.TryGetValue(d.PreviousDialog, out var prev))
                        info.PreviousDialog.SetTo(prev.FormKey);
                    else if (TryExternalRef(d.PreviousDialog, out var pfk))
                        info.PreviousDialog.SetTo(pfk);
                    else
                        Warn($"  ! dialogue '{d.EditorId}' previousDialog '{d.PreviousDialog}' is not a built dialogue INFO or a <master>:0xID ref");
                }
            }
        }
    }
}