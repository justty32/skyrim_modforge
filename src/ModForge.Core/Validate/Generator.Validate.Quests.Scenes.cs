namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Continues ValidateQuestsAndDialogue (Generator.Validate.Quests.cs) over SCEN records.
        // Split out verbatim; the only change is re-indenting the autoStart tail, whose leading
        // whitespace had drifted (braces sat at the wrong column — cosmetic only).

        // SCENE (SCEN): host quest must exist; actors need a unique aliasId + an NPC; every phase
        // must name a speaker that is one of the scene's actors and carry at least one line.
        private void ValidateScenes()
        {
            Problems.AddRange(Generator.ValidateSceneSetStages(spec));
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
                    bool hasSetStage = ac.SetStage is not null;
                    bool hasPackage = !string.IsNullOrWhiteSpace(ac.Package);
                    bool hasTimer = ac.TimerSeconds > 0f;
                    // An idle action MAY also carry timerSeconds (the pose-hold duration) — they're not
                    // exclusive. What's exclusive: idle vs package, and package vs timer.
                    if (hasIdle && hasPackage)
                        Problems.Add($"scene '{sc.EditorId}' action {i} sets both idle and package (idle plays an animation, package runs a PACK — pick one)");
                    if (hasPackage && hasTimer)
                        Problems.Add($"scene '{sc.EditorId}' action {i} sets both package and timerSeconds — pick one");
                    if (hasSetStage && (hasIdle || hasPackage))
                        Problems.Add($"scene '{sc.EditorId}' action {i} combines setStage with idle or package — use separate actions");
                    if (!hasIdle && !hasSetStage && !hasPackage && !hasTimer)
                        Problems.Add($"scene '{sc.EditorId}' action {i} must set one of idle, setStage, package, or timerSeconds");
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
        }
    }
}
