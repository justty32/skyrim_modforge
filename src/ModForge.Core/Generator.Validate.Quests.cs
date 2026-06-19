namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // --- quests, dialogue, scenes, script attachments ---
        public void ValidateQuestsAndDialogue()
        {
            // Quest stage indices unique + ascending; objective↔stage refs; log-entry conditions valid.
            var stageIndexByQuest = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var q in spec.Quests)
            {
                var seen = new HashSet<int>();
                int prev = -1;
                int startUpStages = 0;
                foreach (var st in q.Stages)
                {
                    if (st.StartUpStage) startUpStages++;
                    if (!seen.Add(st.Index))
                        Problems.Add($"quest '{q.EditorId}' has duplicate stage index {st.Index}");
                    if (st.Index <= prev)
                        Problems.Add($"quest '{q.EditorId}' stage index {st.Index} is not ascending (must list stages in increasing order)");
                    prev = st.Index;
                    if (st.CompleteQuest && st.FailQuest)
                        Problems.Add($"quest '{q.EditorId}' stage {st.Index} sets both completeQuest and failQuest");
                    foreach (var cs in st.Conditions)
                    {
                        if (string.IsNullOrWhiteSpace(cs.Function))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} condition has empty function");
                        else if (!Enum.TryParse<Condition.Function>(cs.Function, true, out _))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} condition invalid function '{cs.Function}'");
                        if (!string.IsNullOrWhiteSpace(cs.Comparison)
                            && cs.Comparison is not ("==" or "=" or "!=" or ">" or ">=" or "<" or "<=")
                            && !Enum.TryParse<CompareOperator>(cs.Comparison, true, out _))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} condition invalid comparison '{cs.Comparison}'");
                        CheckRef(cs.Param, $"quest '{q.EditorId}' stage {st.Index} condition param");
                    }
                    foreach (var ig in st.InstanceGlobals)
                    {
                        if (string.IsNullOrWhiteSpace(ig.Global))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} instanceGlobal has empty 'global'");
                        else
                            CheckRef(ig.Global, $"quest '{q.EditorId}' stage {st.Index} instanceGlobal");
                        bool hasRandom = ig.RandomMin is not null || ig.RandomMax is not null;
                        if (hasRandom && (ig.RandomMin is null || ig.RandomMax is null))
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} instanceGlobal '{ig.Global}' needs both randomMin and randomMax (or neither)");
                        else if (ig.RandomMin is int lo && ig.RandomMax is int hi && lo > hi)
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} instanceGlobal '{ig.Global}' randomMin {lo} > randomMax {hi}");
                        if (hasRandom && ig.Value is not null)
                            Problems.Add($"quest '{q.EditorId}' stage {st.Index} instanceGlobal '{ig.Global}' sets both a random range and a fixed value (use one)");
                    }
                    // Stage-fragment persist/syncPerks (Idea #20 Phase 0). A stage has no akSpeakerRef, so
                    // "speaker" is rejected — the key must be "player" or an arbitrary ref.
                    if (st.Persist is { } stp) ValidatePersistBlock(stp, $"quest '{q.EditorId}' stage {st.Index} persist", allowSpeaker: false);
                    if (st.SyncPerks is { } sts) ValidateSyncPerksBlock(sts, $"quest '{q.EditorId}' stage {st.Index} syncPerks", allowSpeaker: false);
                }
                stageIndexByQuest[q.EditorId] = seen;
                if (startUpStages > 1)
                    Problems.Add($"quest '{q.EditorId}' marks {startUpStages} stages as startUpStage (at most one allowed — the engine auto-runs exactly one on quest start)");
                if (q.Spawn is { } sp)   // F組 #3 dynamic spawn
                {
                    if (string.IsNullOrWhiteSpace(sp.Form)) Problems.Add($"quest '{q.EditorId}' spawn has empty 'form' (an ActorBase/LeveledNpc to spawn)");
                    else CheckRef(sp.Form, $"quest '{q.EditorId}' spawn.form");
                    if (sp.Count < 1) Problems.Add($"quest '{q.EditorId}' spawn.count must be >= 1 (got {sp.Count})");
                    if (sp.MinDistance < 0 || sp.MaxDistance < 0) Problems.Add($"quest '{q.EditorId}' spawn distances must be >= 0");
                    else if (sp.MinDistance > sp.MaxDistance) Problems.Add($"quest '{q.EditorId}' spawn.minDistance {sp.MinDistance} > maxDistance {sp.MaxDistance}");
                }
                // spawn / cooldownHours fire from the startUpStage fragment on quest start (OnInit is
                // unreliable — it runs once per quest lifetime, not on every SM relaunch). Without a
                // startUpStage there is nothing to trigger them, so the encounter silently does nothing.
                bool needsStartup = q.Spawn is not null || (q.StoryEvent is { } cdSe && cdSe.CooldownHours > 0f);
                if (needsStartup && startUpStages == 0)
                    Problems.Add($"quest '{q.EditorId}' declares spawn/cooldownHours but has no startUpStage — these trigger from the startUpStage fragment on quest start; add a stage with startUpStage:true or nothing will fire");
                var objIdx = new HashSet<int>();
                var aliasNames = new HashSet<string>(q.Aliases.Select(a => a.Name), StringComparer.OrdinalIgnoreCase);
                foreach (var o in q.Objectives)
                {
                    if (!objIdx.Add(o.Index))
                        Problems.Add($"quest '{q.EditorId}' has duplicate objective index {o.Index}");
                    if (o.ShowStage >= 0 && !seen.Contains(o.ShowStage))
                        Problems.Add($"quest '{q.EditorId}' objective {o.Index} showStage {o.ShowStage} has no matching stage");
                    if (o.CompleteStage >= 0 && !seen.Contains(o.CompleteStage))
                        Problems.Add($"quest '{q.EditorId}' objective {o.Index} completeStage {o.CompleteStage} has no matching stage");
                    foreach (var t in o.Targets)
                        if (string.IsNullOrWhiteSpace(t.Alias))
                            Problems.Add($"quest '{q.EditorId}' objective {o.Index} has a target with no alias");
                        else if (!aliasNames.Contains(t.Alias))
                            Problems.Add($"quest '{q.EditorId}' objective {o.Index} target alias '{t.Alias}' is not an alias on this quest");
                }
            }

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
                // A `hello:true` line is the NPC's auto-spoken greeting (Misc/Hello), not a player menu
                // option, so it has no prompt by design — only require a prompt for normal player topics.
                if (!d.Hello && string.IsNullOrEmpty(d.Prompt)) Problems.Add($"dialogue '{d.EditorId}' has empty prompt");
                if (d.Responses.Count == 0) Problems.Add($"dialogue '{d.EditorId}' has no response lines");
                if (!Enum.TryParse<Emotion>(d.Emotion, true, out _))
                    Problems.Add($"dialogue '{d.EditorId}' invalid emotion '{d.Emotion}' (Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise|Puzzled)");
            }

            // SCENE (SCEN): host quest must exist; actors need a unique aliasId + an NPC; every phase
            // must name a speaker that is one of the scene's actors and carry at least one line.
            foreach (var sc in spec.Scenes)
            {
                if (!questIds.Contains(sc.QuestEditorId))
                    Problems.Add($"scene '{sc.EditorId}' references unknown quest '{sc.QuestEditorId}'");
                if (sc.Actors.Count == 0)
                    Problems.Add($"scene '{sc.EditorId}' has no actors (a scene needs at least two NPCs talking to each other)");
                var sceneAliasIds = new HashSet<int>();
                foreach (var a in sc.Actors)
                {
                    if (a.AliasId < 0) Problems.Add($"scene '{sc.EditorId}' actor has negative aliasId {a.AliasId}");
                    else if (!sceneAliasIds.Add(a.AliasId)) Problems.Add($"scene '{sc.EditorId}' duplicate actor aliasId {a.AliasId}");
                    if (string.IsNullOrWhiteSpace(a.Npc)) Problems.Add($"scene '{sc.EditorId}' actor (alias {a.AliasId}) has empty npc ref");
                    else CheckRef(a.Npc, $"scene '{sc.EditorId}' actor (alias {a.AliasId}) npc");
                }
                foreach (var cs in sc.Conditions)
                    ValidateSceneCondition(cs, $"scene '{sc.EditorId}' condition");
                if (sc.Phases.Count == 0)
                    Problems.Add($"scene '{sc.EditorId}' has no phases (nothing is spoken)");
                // A phase that is COVERED by a non-dialog action may be a lineless "beat" phase (the
                // window in which the actor moves / the scene waits); only a lineless phase that NO
                // action spans is an authoring mistake.
                var coveredPhases = new HashSet<int>();
                foreach (var ac in sc.Actions)
                {
                    int end = ac.EndPhase < 0 ? ac.StartPhase : ac.EndPhase;
                    for (int p = ac.StartPhase; p <= end; p++) coveredPhases.Add(p);
                }
                for (int i = 0; i < sc.Phases.Count; i++)
                {
                    var ph = sc.Phases[i];
                    if (ph.Lines.Count == 0)
                    {
                        if (!coveredPhases.Contains(i))
                            Problems.Add($"scene '{sc.EditorId}' phase {i} has no lines and no action covers it (a beat phase needs an action)");
                        continue;   // a beat phase has no speaker/emotion to validate
                    }
                    if (!sceneAliasIds.Contains(ph.Speaker))
                        Problems.Add($"scene '{sc.EditorId}' phase {i} speaker aliasId {ph.Speaker} is not one of the scene's actors");
                    if (!Enum.TryParse<Emotion>(ph.Emotion, true, out _))
                        Problems.Add($"scene '{sc.EditorId}' phase {i} invalid emotion '{ph.Emotion}' (Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise|Puzzled)");
                    if (ph.HeadtrackActor >= 0 && !sceneAliasIds.Contains(ph.HeadtrackActor))
                        Problems.Add($"scene '{sc.EditorId}' phase {i} headtrackActor {ph.HeadtrackActor} is not one of the scene's actors");
                    if (ph.HeadtrackPlayer && ph.HeadtrackActor != -2)
                        Problems.Add($"scene '{sc.EditorId}' phase {i} sets both headtrackPlayer and headtrackActor — pick one");
                    foreach (var cs in ph.StartConditions)
                        ValidateSceneCondition(cs, $"scene '{sc.EditorId}' phase {i} startCondition");
                    foreach (var cs in ph.CompletionConditions)
                        ValidateSceneCondition(cs, $"scene '{sc.EditorId}' phase {i} completionCondition");
                }
                // Non-dialog actions: each runs an actor over a phase window, doing EXACTLY ONE of an
                // idle (PlayIdle via a phase fragment), a package (movement/sandbox/...), or a timer (a pause).
                for (int i = 0; i < sc.Actions.Count; i++)
                {
                    var ac = sc.Actions[i];
                    if (!sceneAliasIds.Contains(ac.Actor))
                        Problems.Add($"scene '{sc.EditorId}' action {i} actor aliasId {ac.Actor} is not one of the scene's actors");
                    bool hasIdle = !string.IsNullOrWhiteSpace(ac.Idle);
                    bool hasPackage = !string.IsNullOrWhiteSpace(ac.Package);
                    bool hasTimer = ac.TimerSeconds > 0f;
                    // An idle action MAY also carry timerSeconds (the pose-hold duration) — they're not
                    // exclusive. What's exclusive: idle vs package, and package vs timer.
                    if (hasIdle && hasPackage)
                        Problems.Add($"scene '{sc.EditorId}' action {i} sets both idle and package (idle plays an animation, package runs a PACK — pick one)");
                    if (hasPackage && hasTimer)
                        Problems.Add($"scene '{sc.EditorId}' action {i} sets both package and timerSeconds — pick one");
                    if (!hasIdle && !hasPackage && !hasTimer)
                        Problems.Add($"scene '{sc.EditorId}' action {i} must set one of idle, package, or timerSeconds");
                    if (hasIdle) CheckRef(ac.Idle, $"scene '{sc.EditorId}' action {i} idle");
                    if (hasPackage) CheckRef(ac.Package, $"scene '{sc.EditorId}' action {i} package");
                    int end = ac.EndPhase < 0 ? ac.StartPhase : ac.EndPhase;
                    if (ac.StartPhase < 0 || ac.StartPhase >= sc.Phases.Count || end < ac.StartPhase || end >= sc.Phases.Count)
                        Problems.Add($"scene '{sc.EditorId}' action {i} phase window {ac.StartPhase}..{end} is out of range (0..{sc.Phases.Count - 1})");
                }
                if (sc.AutoStart is { } au)
                {
                    // The controller arms in OnInit, which fires when the host quest starts — so the
                    // quest must run on its own (StartGameEnabled), and it needs ≥2 actors to play.
                    var hostQuest = spec.Quests.FirstOrDefault(q => q.EditorId == sc.QuestEditorId);
                    if (hostQuest is not null && !hostQuest.StartGameEnabled)
                        Problems.Add($"scene '{sc.EditorId}' autoStart requires host quest '{sc.QuestEditorId}' to be StartGameEnabled");
                    if (sc.Actors.Count < 2)
                        Problems.Add($"scene '{sc.EditorId}' autoStart needs at least two actors");
                    if (au.PollSeconds <= 0) Problems.Add($"scene '{sc.EditorId}' autoStart pollSeconds must be > 0");
                    if (au.TriggerDistance <= 0) Problems.Add($"scene '{sc.EditorId}' autoStart triggerDistance must be > 0");
                    if (au.CooldownSeconds < 0) Problems.Add($"scene '{sc.EditorId}' autoStart cooldownSeconds must be >= 0");
                    // Replay policy: a noon-style window must be a real hour; a gate must resolve to a GLOB.
                    if (au.PlayHour >= 0 && au.PlayHour > 24)
                        Problems.Add($"scene '{sc.EditorId}' autoStart playHour {au.PlayHour} must be 0..24 (or -1 for any time)");
                    if (au.PlayHourTolerance <= 0)
                        Problems.Add($"scene '{sc.EditorId}' autoStart playHourTolerance must be > 0");
                    if (!string.IsNullOrWhiteSpace(au.GateGlobal))
                        if (au.GateGlobal is { } gg)
                                CheckRef(gg, $"scene '{sc.EditorId}' autoStart gateGlobal");
                        }
                        }

                        ValidateScriptAttachments();
                        }
        // Shared CTDA validation for scene/phase conditions (mirrors the stage-condition checks).
        private void ValidateSceneCondition(ConditionSpec cs, string label)
        {
            if (string.IsNullOrWhiteSpace(cs.Function))
                Problems.Add($"{label} has empty function");
            else if (!Enum.TryParse<Condition.Function>(cs.Function, true, out _))
                Problems.Add($"{label} invalid function '{cs.Function}'");
            if (!string.IsNullOrWhiteSpace(cs.Comparison)
                && cs.Comparison is not ("==" or "=" or "!=" or ">" or ">=" or "<" or "<=")
                && !Enum.TryParse<CompareOperator>(cs.Comparison, true, out _))
                Problems.Add($"{label} invalid comparison '{cs.Comparison}'");
            CheckRef(cs.Param, $"{label} param");
            if (string.Equals(cs.Function, "IsSceneActionComplete", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(cs.Scene)) CheckRef(cs.Scene, $"{label} scene");
                if (cs.SceneActionIndex < 0) Problems.Add($"{label}: IsSceneActionComplete needs a sceneActionIndex (>= 0)");
            }
        }

        // Shared persist-block validation (dialogue line + quest stage). `allowSpeaker` is false on a
        // quest stage, which has no akSpeakerRef to key on.
        private void ValidatePersistBlock(PersistSpec p, string label, bool allowSpeaker)
        {
            if (string.IsNullOrWhiteSpace(p.Storage)) Problems.Add($"{label} has empty storage");
            ValidatePersistKey(p.Key, $"{label} key", allowSpeaker);
            if (p.Set.Count == 0) Problems.Add($"{label} has no set entries");
            foreach (var e in p.Set)
            {
                if (string.IsNullOrWhiteSpace(e.Path)) Problems.Add($"{label} entry has empty path");
                int vals = (e.Int is not null ? 1 : 0) + (e.Float is not null ? 1 : 0)
                         + (e.Str is not null ? 1 : 0) + (!string.IsNullOrWhiteSpace(e.Form) ? 1 : 0);
                if (vals != 1) Problems.Add($"{label} entry '{e.Path}' must set exactly one of int/float/str/form (got {vals})");
                if (e.Delta && e.Int is null && e.Float is null) Problems.Add($"{label} entry '{e.Path}' delta only applies to int/float");
                if (!string.IsNullOrWhiteSpace(e.Form)) CheckRef(e.Form, $"{label} entry '{e.Path}' form");
            }
            ValidateGate(p.Gate, $"{label} gate");
        }

        private void ValidateSyncPerksBlock(SyncPerksSpec s, string label, bool allowSpeaker)
        {
            if (string.IsNullOrWhiteSpace(s.Storage)) Problems.Add($"{label} has empty storage");
            ValidatePersistKey(s.Key, $"{label} key", allowSpeaker);
            if (s.Nodes.Count == 0) Problems.Add($"{label} has no nodes");
            foreach (var n in s.Nodes)
            {
                if (string.IsNullOrWhiteSpace(n.Path)) Problems.Add($"{label} node has empty path");
                if (string.IsNullOrWhiteSpace(n.Perk)) Problems.Add($"{label} node '{n.Path}' has empty perk ref");
                else CheckRef(n.Perk, $"{label} node '{n.Path}' perk");
            }
            ValidateGate(s.Gate, $"{label} gate");
        }

        // The affinity gate (Sofia F6 blueprint): the GLOB must resolve, and a band must not be inverted.
        private void ValidateGate(GateSpec? g, string label)
        {
            if (g is null) return;
            if (string.IsNullOrWhiteSpace(g.Global)) Problems.Add($"{label} has empty global");
            else CheckRef(g.Global, $"{label} global");
            if (g.AtLeast is float lo && g.AtMost is float hi && lo > hi)
                Problems.Add($"{label} atLeast ({lo}) > atMost ({hi}) — band never satisfiable");
        }

        // A persist/syncPerks Form key is "player", "speaker" (dialogue only — the emitter maps it to the
        // fragment's akSpeakerRef), or an arbitrary resolvable ref (bound as a Form property). Empty
        // defaults to "speaker". On a quest stage (allowSpeaker=false) "speaker" is an error.
        private void ValidatePersistKey(string key, string label, bool allowSpeaker)
        {
            switch (Generator.ClassifyPersistKey(key))
            {
                case Generator.PersistKeyKind.Speaker:
                    if (!allowSpeaker)
                        Problems.Add($"{label} 'speaker' is only valid on a dialogue line (a quest stage has no speaker — use 'player' or a ref)");
                    break;
                case Generator.PersistKeyKind.Player:
                    break;
                default:
                    CheckRef(key, label);   // arbitrary ref → must resolve
                    break;
            }
        }

        private void ValidateScriptAttachments()
        {
            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "int", "float", "bool", "string", "object" };
            foreach (var sa in spec.Scripts)
            {
                if (string.IsNullOrEmpty(sa.ScriptName)) Problems.Add($"script attach on '{sa.TargetEditorId}' has empty scriptName");
                if (!Ids.Contains(sa.TargetEditorId)) Problems.Add($"script '{sa.ScriptName}' targets unknown record '{sa.TargetEditorId}'");
                foreach (var p in sa.Properties)
                {
                    if (!validTypes.Contains(p.Type)) Problems.Add($"script '{sa.ScriptName}' prop '{p.Name}' has invalid type '{p.Type}'");
                    if (string.Equals(p.Type, "object", StringComparison.OrdinalIgnoreCase))
                        CheckRef(p.ObjectEditorId, $"script '{sa.ScriptName}' prop '{p.Name}' object");
                }
            }
        }
    }
}
