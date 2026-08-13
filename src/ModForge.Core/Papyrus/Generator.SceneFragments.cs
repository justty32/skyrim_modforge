namespace ModForge;

public static partial class Generator
{
    /// <summary>Validate typed targets and declared stages for restricted scene SetStage actions.
    /// An external quest's stages cannot be inspected offline and remain an author contract.</summary>
    public static List<string> ValidateSceneSetStages(ModSpec spec)
    {
        var problems = new List<string>();
        var quests = spec.Quests.GroupBy(q => q.EditorId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var scene in spec.Scenes)
            for (int i = 0; i < scene.Actions.Count; i++)
            {
                var setStage = scene.Actions[i].SetStage;
                if (setStage is null) continue;
                if (setStage.Stage < 0 || setStage.Stage > ushort.MaxValue)
                    problems.Add($"scene '{scene.EditorId}' action {i} setStage.stage must be present and in 0..65535");
                var target = string.IsNullOrWhiteSpace(setStage.Quest) ? scene.QuestEditorId : setStage.Quest;
                if (LooksExternalRef(target))
                {
                    if (!TryExternalRef(target, out _))
                        problems.Add($"scene '{scene.EditorId}' action {i} setStage quest has malformed external ref '{target}' (expect <master>:0xFORMID)");
                    continue;
                }
                if (!quests.TryGetValue(target, out var quest))
                {
                    problems.Add($"scene '{scene.EditorId}' action {i} setStage quest '{target}' is not an in-spec quest or external quest ref");
                    continue;
                }
                if (setStage.Stage >= 0 && setStage.Stage <= ushort.MaxValue
                    && !quest.Stages.Any(stage => stage.Index == setStage.Stage))
                    problems.Add($"scene '{scene.EditorId}' action {i} setStage {setStage.Stage} has no matching stage in quest '{target}'");
            }
        return problems;
    }

    // -------------------------------------------------------------------------------
    //  Scene phase-fragment GENERATION (restricted phase-begin actions) — the THIRD fragment family.
    //
    //  SceneAction.TypeEnum has no native "play animation" beat (only Dialog/Package/Timer).
    //  Skyrim's standard way to animate a scene actor is a SCEN SceneAdapter whose per-phase
    //  begin fragment calls `<alias>.GetActorRef().PlayIdle(<idle>)`. An "idle action"
    //  (SceneActionSpec.Idle non-empty) compiles to exactly that.
    //
    //  Decoded (Task 0 spike, 2026-06-07) from vanilla SF_BardSongsBallad01Scene /
    //  SF_MQ201EscapeScene: the SF_ script `extends Scene`, declares a `ReferenceAlias` property
    //  per actor (bound by `package` to the host-quest alias) and an `Idle` property per idle,
    //  and fires `<Alias>.GetActorRef().PlayIdle(<Idle>)`. The vanilla method is GetActorRef()
    //  (NOT GetActorReference — that does not exist on ReferenceAlias).
    //
    //  Like GenerateQuestFragmentSource this is a pure function (string in / string out),
    //  unit-testable with no Skyrim master / Wine dependency. The matching VMAD + property
    //  binding lives in Generator.Build.Scripts.AttachSceneFragments (driven by `package`).
    // -------------------------------------------------------------------------------

    /// <summary>Default seconds an idle action holds its phase open (and so the pose) when the author
    /// sets no explicit <see cref="SceneActionSpec.TimerSeconds"/>. The phase MUST carry a Timer or the
    /// engine won't run it and the OnStart fragment never fires (decoded from vanilla BardSongs* scenes,
    /// where every fragment phase has a Timer).</summary>
    public const float DefaultIdleHoldSeconds = 2.0f;

    /// <summary>True when a scene has at least one fragment-backed action.</summary>
    public static bool SceneNeedsFragmentScript(SceneSpec s) =>
        s.Actions.Any(a => !string.IsNullOrWhiteSpace(a.Idle) || a.SetStage is not null);

    /// <summary>The Papyrus script Name for a scene's phase-fragment script, or empty if none needed.</summary>
    public static string SceneFragmentScriptName(SceneSpec s) =>
        SceneNeedsFragmentScript(s) ? $"SF_{Sanitize(s.EditorId)}" : "";

    /// <summary>The idle actions that drive fragments: one per phase (first wins if several share a
    /// phase), ascending by phase. The phase index doubles as the fragment counter (Fragment_&lt;phase&gt;)
    /// — collision-free because there is at most one idle per phase.</summary>
    internal static IEnumerable<SceneActionSpec> SceneIdleActions(SceneSpec s) =>
        s.Actions
            .Where(a => !string.IsNullOrWhiteSpace(a.Idle))
            .GroupBy(a => a.StartPhase)
            .Select(g => g.First())
            .OrderBy(a => a.StartPhase);

    internal static IEnumerable<(SceneActionSpec Action, int ActionIndex)> SceneSetStageActions(SceneSpec s) =>
        s.Actions.Select((a, i) => (Action: a, ActionIndex: i)).Where(x => x.Action.SetStage is not null);

    internal static IEnumerable<int> SceneFragmentPhases(SceneSpec s) =>
        SceneIdleActions(s).Select(a => a.StartPhase)
            .Concat(SceneSetStageActions(s).Select(x => x.Action.StartPhase))
            .Distinct().OrderBy(x => x);

    /// <summary>Papyrus source with one Fragment per phase that has a restricted action. Idle actors,
    /// idle forms, and SetStage quests are bound as script properties by package.</summary>
    public static string GenerateSceneFragmentSource(SceneSpec s)
    {
        if (!SceneNeedsFragmentScript(s)) return "";
        var name = SceneFragmentScriptName(s);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Scriptname {name} extends Scene Hidden");
        sb.AppendLine("; AUTO-GENERATED by ModForge — restricted scene phase actions.");
        sb.AppendLine("; `package` compiles this and attaches the SceneAdapter VMAD + properties automatically.");
        sb.AppendLine();

        var idleByPhase = SceneIdleActions(s).ToDictionary(a => a.StartPhase);
        var setStagesByPhase = SceneSetStageActions(s).GroupBy(x => x.Action.StartPhase)
            .ToDictionary(g => g.Key, g => g.ToList());
        foreach (var phase in SceneFragmentPhases(s))
        {
            if (idleByPhase.TryGetValue(phase, out var idle))
            {
                sb.AppendLine($"Idle Property Idle_{phase} Auto");
                sb.AppendLine($"ReferenceAlias Property Actor_{phase} Auto");
            }
            if (setStagesByPhase.TryGetValue(phase, out var setStages))
                foreach (var (_, actionIndex) in setStages)
                    sb.AppendLine($"Quest Property SetStageQuest_{actionIndex} Auto");
            sb.AppendLine($"Function Fragment_{phase}()");
            if (idle is not null)
            {
                sb.AppendLine($"    ; phase {phase}: alias {idle.Actor} plays idle {OneLine(idle.Idle)}");
                sb.AppendLine($"    Actor a = Actor_{phase}.GetActorRef()");
                sb.AppendLine("    if a");
                sb.AppendLine($"        a.PlayIdle(Idle_{phase})");
                sb.AppendLine("    endif");
            }
            if (setStages is not null)
                foreach (var (action, actionIndex) in setStages)
                    sb.AppendLine($"    SetStageQuest_{actionIndex}.SetStage({action.SetStage!.Stage})");
            sb.AppendLine("EndFunction");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
