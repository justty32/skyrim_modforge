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
                // autoStart drives the scene from a controller script, NOT quest-start — so suppress
                // BeginOnQuestStart when a presence gate is declared (otherwise it'd also fire on load).
                if (s.BeginOnQuestStart && s.AutoStart is null) flags |= Scene.Flag.BeginOnQuestStart;
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
                // Per-spec-phase → built-ScenePhase map for pass 2 condition wiring (a phase with an
                // invalid speaker is skipped, so the built index need not match the spec index).
                var phaseMap = new List<(int SpecIndex, ScenePhase Phase)>();
                int actionIndex = 1;   // vanilla SceneActions are 1-based
                for (int p = 0; p < s.Phases.Count; p++)
                {
                    var ph = s.Phases[p];
                    var phase = new ScenePhase();         // always add — keeps phase index aligned with p
                    scene.Phases.Add(phase);              // (an empty phase = "advance when the line/beat finishes")
                    phaseMap.Add((p, phase));             // map spec phase p → built ScenePhase for pass-2 condition wiring
                    // A lineless BEAT phase exists only as a window for non-dialog actions (movement/timer):
                    // emit the phase but no spoken topic / Dialog action.
                    if (ph.Lines.Count == 0) continue;
                    if (!aliasIds.Contains(ph.Speaker))
                    { Warn($"  ! scene '{s.EditorId}' phase {p}: speaker aliasId {ph.Speaker} is not one of the scene's actors — skipping"); continue; }

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

                    var info = new DialogResponses(mod)
                    {
                        EditorID = topic.EditorID,
                        Flags = new DialogResponseFlags(),
                        FavorLevel = FavorLevel.None
                    };
                    var emotion = Enum.TryParse<Emotion>(ph.Emotion, ignoreCase: true, out var em) ? em : Emotion.Neutral;
                    byte rn = 1;
                    foreach (var line in ph.Lines)
                        info.Responses.Add(new DialogResponse { Text = line, ResponseNumber = rn++, Emotion = emotion, EmotionValue = ph.EmotionValue });
                    topic.Responses.Add(info);

                    // The Dialog action: alias `Speaker` says the topic during phase `p`. By default the
                    // speaker headtracks the OTHER actor so they look at each other (vanilla two-NPC
                    // scenes do this). The phase can override the gaze (headtrackActor/headtrackPlayer)
                    // and whether the FaceTarget flag is set (faceTarget).
                    int otherAlias = s.Actors.FirstOrDefault(x => x.AliasId != ph.Speaker)?.AliasId ?? ph.Speaker;
                    int? headtrackActorId;
                    var sceneFlags = default(SceneAction.Flag);
                    if (ph.HeadtrackPlayer)
                    {
                        headtrackActorId = null;
                        sceneFlags |= SceneAction.Flag.HeadtrackPlayer;
                    }
                    else
                    {
                        headtrackActorId = ph.HeadtrackActor switch
                        {
                            -2 => otherAlias,   // default: the other actor (current behavior)
                            -1 => null,         // look at no one
                            _ => ph.HeadtrackActor,
                        };
                    }
                    if (ph.FaceTarget ?? true) sceneFlags |= SceneAction.Flag.FaceTarget;
                    var action = new SceneAction
                    {
                        Type = SceneAction.TypeEnum.Dialog,
                        ActorID = ph.Speaker,
                        Index = (uint)actionIndex++,
                        StartPhase = (uint)p,
                        EndPhase = (uint)p,
                        HeadtrackActorID = headtrackActorId,
                        LoopingMin = 1,
                        LoopingMax = 10,
                        Emotion = emotion,
                        EmotionValue = (uint)Math.Clamp(ph.EmotionValue, 0u, 100u),
                        Flags = sceneFlags,
                    };
                    action.Topic.SetTo(topic);
                    scene.Actions.Add(action);
                    scenePhasesBuilt++;
                }

                // Non-dialog beats (movement/timer) — emitted after the dialogue actions, continuing the
                // shared 1-based action index. Package refs are forward links resolved in WireScenes.
                foreach (var ac in s.Actions)
                {
                    int endPhase = ac.EndPhase < 0 ? ac.StartPhase : ac.EndPhase;
                    var act = new SceneAction
                    {
                        ActorID = ac.Actor,
                        Index = (uint)actionIndex++,
                        StartPhase = (uint)ac.StartPhase,
                        EndPhase = (uint)endPhase,
                    };
                    if (!string.IsNullOrWhiteSpace(ac.Idle))
                    {
                        // PlayIdle action: the animation itself runs via the SceneAdapter phase fragment
                        // (AttachSceneFragments, driven by `package`). But the engine only RUNS a phase
                        // that has at least one SceneAction (every vanilla fragment phase carries a Timer
                        // — decoded from BardSongs* scenes), and an action-less phase never fires its
                        // OnStart fragment. So emit a Timer here: it both makes the phase run (so the
                        // PlayIdle fragment fires) and HOLDS the pose for the duration. Hold = TimerSeconds
                        // if the author set one, else the default.
                        act.Type = SceneAction.TypeEnum.Timer;
                        act.TimerSeconds = ac.TimerSeconds > 0f ? ac.TimerSeconds : Generator.DefaultIdleHoldSeconds;
                    }
                    else if (ac.TimerSeconds > 0f)
                    {
                        act.Type = SceneAction.TypeEnum.Timer;
                        act.TimerSeconds = ac.TimerSeconds;
                    }
                    else
                    {
                        act.Type = SceneAction.TypeEnum.Package;
                        sceneActionWires.Add((s.EditorId, act, ac.Package));
                    }
                    scene.Actions.Add(act);
                }
                scene.LastActionIndex = (uint)(actionIndex - 1);

                // Presence-gated auto-start: attach the reusable controller to the host quest, wired to
                // this scene + the first two actor alias indices + the tuning. The .pex is shipped by
                // Package when any scene has autoStart (mirrors the Script-Event dispatcher).
                if (s.AutoStart is { } au && s.Actors.Count >= 2)
                    AttachSceneController(quest, scene, s.Actors[0].AliasId, s.Actors[1].AliasId, au);

                sceneConditionWires.Add((s, scene, phaseMap));
                scenesBuilt++;
            }
        }

        // Attach MFSceneBanterController (extends Quest) to the host quest with the scene/alias/tuning
        // properties. Reuses an existing QuestAdapter (e.g. from an alias script) by appending to its
        // Scripts list rather than clobbering it. Property names match the .psc's Auto properties.
        private void AttachSceneController(Quest quest, Scene scene, int aliasA, int aliasB, SceneAutoStartSpec au)
        {
            if (quest.VirtualMachineAdapter is not QuestAdapter qad)
            {
                qad = new QuestAdapter { Version = 5, ObjectFormat = 2 };
                quest.VirtualMachineAdapter = qad;
            }
            var entry = new ScriptEntry { Name = Generator.SceneBanterController, Flags = ScriptEntry.Flag.Local };
            var sceneProp = new ScriptObjectProperty { Name = "BanterScene", Flags = ScriptProperty.Flag.Edited };
            sceneProp.Object.SetTo(scene.FormKey);
            entry.Properties.Add(sceneProp);
            entry.Properties.Add(new ScriptIntProperty   { Name = "ActorAliasA",     Data = aliasA, Flags = ScriptProperty.Flag.Edited });
            entry.Properties.Add(new ScriptIntProperty   { Name = "ActorAliasB",     Data = aliasB, Flags = ScriptProperty.Flag.Edited });
            entry.Properties.Add(new ScriptFloatProperty { Name = "TriggerDistance", Data = au.TriggerDistance, Flags = ScriptProperty.Flag.Edited });
            entry.Properties.Add(new ScriptFloatProperty { Name = "PollInterval",    Data = au.PollSeconds, Flags = ScriptProperty.Flag.Edited });
            entry.Properties.Add(new ScriptFloatProperty { Name = "Cooldown",        Data = au.CooldownSeconds, Flags = ScriptProperty.Flag.Edited });
            entry.Properties.Add(new ScriptBoolProperty  { Name = "RequireLOS",      Data = au.RequireLineOfSight, Flags = ScriptProperty.Flag.Edited });
            entry.Properties.Add(new ScriptBoolProperty  { Name = "BrawlOnEnd",      Data = au.BrawlOnEnd, Flags = ScriptProperty.Flag.Edited });
            // Replay policy (controller gates these AND-ed onto the cooldown).
            entry.Properties.Add(new ScriptBoolProperty  { Name = "PlayOnce",          Data = au.PlayOnce, Flags = ScriptProperty.Flag.Edited });
            entry.Properties.Add(new ScriptFloatProperty { Name = "PlayHour",          Data = au.PlayHour, Flags = ScriptProperty.Flag.Edited });
            entry.Properties.Add(new ScriptFloatProperty { Name = "PlayHourTolerance", Data = au.PlayHourTolerance, Flags = ScriptProperty.Flag.Edited });
            if (!string.IsNullOrWhiteSpace(au.GateGlobal))
            {
                var gateProp = new ScriptObjectProperty { Name = "Gate", Flags = ScriptProperty.Flag.Edited };
                entry.Properties.Add(gateProp);
                sceneGateWires.Add((quest.EditorID ?? "?", gateProp, au.GateGlobal));   // GLOB ref resolved in pass 2
            }
            qad.Scripts.Add(entry);
            scriptsAttached++;
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
            // Package-action PACK refs (movement/sandbox/...): now that every record is indexed, resolve
            // each scene Package action's referenced AI package and add it to the action's Packages list.
            foreach (var (sceneEd, action, packageRef) in sceneActionWires)
            {
                if (string.IsNullOrWhiteSpace(packageRef))
                { Warn($"  ! scene '{sceneEd}' package action has no package ref — actor will do nothing"); continue; }
                Resolve($"scene '{sceneEd}' action package", packageRef, fk => action.Packages.Add(fk.ToLink<IPackageGetter>()));
            }
            // Scene controller GateGlobal (replay re-arm token) → the GlobalVariable object property.
            foreach (var (hostEd, prop, globalRef) in sceneGateWires)
                Resolve($"scene controller on quest '{hostEd}' gateGlobal", globalRef, fk => prop.Object.SetTo(fk));

            // Scene-level gate (the whole scene only starts if all pass) + per-phase start/completion
            // gates. Refs are by editorId, so they wire here via the SHARED BuildCondition (mirrors
            // WireQuestStages). A scene with no conditions leaves every list empty (byte-identical).
            foreach (var (s, scene, phaseMap) in sceneConditionWires)
            {
                foreach (var cs in s.Conditions)
                    if (BuildCondition(cs, $"scene '{s.EditorId}' condition") is { } cond)
                        scene.Conditions.Add(cond);

                foreach (var (specIndex, phase) in phaseMap)
                {
                    var ph = s.Phases[specIndex];
                    foreach (var cs in ph.StartConditions)
                        if (BuildCondition(cs, $"scene '{s.EditorId}' phase {specIndex} startCondition") is { } cond)
                            phase.StartConditions.Add(cond);
                    foreach (var cs in ph.CompletionConditions)
                        if (BuildCondition(cs, $"scene '{s.EditorId}' phase {specIndex} completionCondition") is { } cond)
                            phase.CompletionConditions.Add(cond);
                }
            }
        }
    }
}
