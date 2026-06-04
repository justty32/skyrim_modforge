namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: SCENE (SCEN) — two NPCs talking to EACH OTHER, hosted by a quest ---------------
        // A vanilla Scene's participants are the host QUEST's ALIASES (not direct NPC refs). So from one
        // SceneSpec we emit, end to end (confirmed structurally against vanilla dunIronbindBeemJaMourning
        // and MQSkyHavenSparring via `scenediag`):
        //   * one QuestAlias per actor on the host quest, `UniqueActor`-bound to the named NPC (alias-fill);
        //   * one Scene-subtype DialogTopic (Category=Scene, SNAM='SCEN') + INFO per phase line, carrying
        //     the spoken response (this is where the words live — the Dialog action only POINTS at it);
        //   * the Scene record: its SceneActors reference the alias indices, its Phases are the ordered
        //     conversation beats, and each Dialog SceneAction ties (speaking alias, phase) -> the topic.
        // NPC binding (UniqueActor) is a forward ref to records built later in pass 1, so it's deferred to
        // WireScenes() in pass 2 (mirrors how relationships/packages resolve their NPC refs).
        public void BuildScenes()
        {
            foreach (var s in spec.Scenes)
            {
                if (string.IsNullOrEmpty(s.QuestEditorId) || !questsByEd.TryGetValue(s.QuestEditorId, out var quest))
                { Warn($"  ! scene '{s.EditorId}' skipped: quest '{s.QuestEditorId}' not found in spec"); continue; }
                if (s.Actors.Count == 0)
                { Warn($"  ! scene '{s.EditorId}' skipped: no actors"); continue; }

                // 1) QuestAliases on the host quest — one per actor, keyed by the spec's aliasId. The NPC
                //    that fills each (UniqueActor) is wired in pass 2 (the NPC record may not exist yet).
                var aliasIds = new HashSet<int>();
                foreach (var a in s.Actors)
                {
                    if (!aliasIds.Add(a.AliasId))
                    { Warn($"  ! scene '{s.EditorId}' actor aliasId {a.AliasId} duplicated — skipping the dup"); continue; }
                    var alias = new QuestAlias
                    {
                        ID = (uint)a.AliasId,
                        Name = string.IsNullOrWhiteSpace(a.Name) ? a.Npc : a.Name,
                        // Unique-actor-filled aliases need this flag set (vanilla scene actor aliases that
                        // bind a specific NPC carry it); the actual UniqueActor link is set in pass 2.
                        Flags = QuestAlias.Flag.UsesStoredText,
                    };
                    quest.Aliases.Add(alias);
                    if ((uint)a.AliasId >= quest.NextAliasID) quest.NextAliasID = (uint)a.AliasId + 1;
                    sceneAliasWires.Add((s.EditorId, a.AliasId, a.Npc, alias));
                }

                // 2) The Scene record + its actors (referencing the alias indices).
                var scene = mod.Scenes.AddNew();
                scene.EditorID = s.EditorId;
                scene.Quest.SetTo(quest);
                var flags = default(Scene.Flag);
                if (s.BeginOnQuestStart) flags |= Scene.Flag.BeginOnQuestStart;
                if (s.StopQuestOnEnd) flags |= Scene.Flag.StopQuestOnEnd;
                scene.Flags = flags;
                foreach (var a in s.Actors)
                    scene.Actors.Add(new SceneActor
                    {
                        ID = (uint)a.AliasId,
                        // DeathEnd/CombatEnd/DialoguePause are what every vanilla conversation actor carries
                        // (stop the scene if a participant dies/fights; pause while another line plays).
                        BehaviorFlags = SceneActor.BehaviorFlag.DeathEnd | SceneActor.BehaviorFlag.CombatEnd
                                       | SceneActor.BehaviorFlag.DialoguePause,
                    });

                // 3) One Phase + one Dialog Action + one Scene/SCEN Topic+INFO per phase line. The action's
                //    Topic link is the binding the engine plays; the topic's INFO holds the words.
                int actionIndex = 1;   // vanilla SceneActions are 1-based
                for (int p = 0; p < s.Phases.Count; p++)
                {
                    var ph = s.Phases[p];
                    if (!aliasIds.Contains(ph.Speaker))
                    { Warn($"  ! scene '{s.EditorId}' phase {p}: speaker aliasId {ph.Speaker} is not one of the scene's actors — skipping"); continue; }

                    scene.Phases.Add(new ScenePhase());   // an empty phase = "advance when the line finishes"

                    // The Scene-subtype topic carrying this phase's line. Category=Scene + SNAM='SCEN' is the
                    // shape every vanilla scene line uses; a null/Custom subtype here would be wrong (the
                    // engine dispatches scene dialogue on the Scene category).
                    var topic = mod.DialogTopics.AddNew();
                    topic.EditorID = $"{s.EditorId}_P{p}";
                    topic.Quest.SetTo(quest);
                    topic.Category = DialogTopic.CategoryEnum.Scene;
                    topic.Subtype = DialogTopic.SubtypeEnum.Scene;
                    topic.SubtypeName = new RecordType("SCEN");
                    topic.Priority = 50f;   // no Branch — scene topics aren't player menu options

                    var info = new DialogResponses(mod) { Flags = new DialogResponseFlags(), FavorLevel = FavorLevel.None };
                    var emotion = Enum.TryParse<Emotion>(ph.Emotion, ignoreCase: true, out var em) ? em : Emotion.Neutral;
                    byte rn = 1;
                    foreach (var line in ph.Lines)
                        info.Responses.Add(new DialogResponse { Text = line, ResponseNumber = rn++, Emotion = emotion, EmotionValue = ph.EmotionValue });
                    topic.Responses.Add(info);

                    // The Dialog action: alias `Speaker` says the topic during phase `p`. HeadtrackActorID
                    // points at the OTHER actor so they look at each other (vanilla two-NPC scenes do this).
                    int otherAlias = s.Actors.FirstOrDefault(x => x.AliasId != ph.Speaker)?.AliasId ?? ph.Speaker;
                    var action = new SceneAction
                    {
                        Type = SceneAction.TypeEnum.Dialog,
                        ActorID = ph.Speaker,
                        Index = (uint)actionIndex++,
                        StartPhase = (uint)p,
                        EndPhase = (uint)p,
                        HeadtrackActorID = otherAlias,
                        LoopingMin = 1,
                        LoopingMax = 10,
                        Emotion = emotion,
                        EmotionValue = (uint)Math.Clamp(ph.EmotionValue, 0u, 100u),
                        Flags = SceneAction.Flag.FaceTarget,
                    };
                    action.Topic.SetTo(topic);
                    scene.Actions.Add(action);
                    scenePhasesBuilt++;
                }
                scene.LastActionIndex = (uint)(actionIndex - 1);
                scenesBuilt++;
            }
        }

        // --- pass 2: bind each scene actor's QuestAlias to the NPC that fills it (UniqueActor link) ---
        // Deferred from pass 1 because the in-spec NPC base record is created in BuildNpcs but a
        // forward-referenced one (or a vanilla <master>:0xFORMID) only resolves once the formKey table exists.
        public void WireScenes()
        {
            foreach (var (sceneEd, aliasId, npcRef, alias) in sceneAliasWires)
            {
                if (string.IsNullOrWhiteSpace(npcRef))
                { Warn($"  ! scene '{sceneEd}' alias #{aliasId} has no npc — alias will be empty"); continue; }
                Resolve($"scene '{sceneEd}' alias #{aliasId} npc", npcRef, fk => alias.UniqueActor.SetTo(fk));
            }
        }
    }
}
