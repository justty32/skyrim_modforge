namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Quest fragment-script GENERATION (It. quest-stages).
    //
    //  Stages + log entries are pure RECORD data (built in Generator.Build). But two pieces
    //  of "quest LOGIC" can only run via Papyrus in Skyrim:
    //    * displaying / completing an OBJECTIVE when a stage is reached
    //      (SetObjectiveDisplayed / SetObjectiveCompleted), and
    //    * advancing a STAGE when a dialogue line is picked (GetOwningQuest().SetStage()).
    //  Vanilla does both with CK-generated fragment scripts (QF_<quest>_<formid> stage
    //  fragments + TIF_<formid> dialogue fragments). We can't drive the Creation Kit
    //  headless, so we GENERATE the equivalent Papyrus SOURCE for the author to compile
    //  (the `package` command writes it under Scripts/Source/). The quest base script is
    //  attached to the QUST record via the normal VMAD script mechanism; the per-stage
    //  Fragment_N hooks must be bound in the CK (the one step we can't do on Linux).
    //
    //  This generator is a pure function (string in / string out) so it's unit-testable
    //  with no Skyrim master / Wine dependency.
    // -------------------------------------------------------------------------------

    /// <summary>The Papyrus script Name attached to a quest that has stage→objective wiring or
    /// dialogue-set-stage logic. Empty if the quest needs no fragment script.</summary>
    public static string QuestFragmentScriptName(QuestSpec q) =>
        QuestNeedsFragmentScript(q) ? $"{Sanitize(q.EditorId)}_Stages" : "";

    /// <summary>True when a quest has objectives linked to stages (showStage/completeStage), any stage
    /// binds an instance global (UpdateCurrentInstanceGlobal), or a startUpStage must drive a dynamic
    /// spawn / cooldown gate on quest start (see <see cref="StartupStageTrigger"/>).</summary>
    public static bool QuestNeedsFragmentScript(QuestSpec q) =>
        q.Objectives.Any(o => o.ShowStage >= 0 || o.CompleteStage >= 0)
        || q.Stages.Any(s => s.InstanceGlobals.Count > 0)
        || q.Stages.Any(s => s.GlobalWrites.Count > 0)
        || q.Stages.Any(s => HasPersist(s) || HasSyncPerks(s))
        || q.Stages.Any(s => HasStorageWrites(s))
        || StartupStageTrigger(q) is not null
        || StoryTrigger(q);

    /// <summary>True when the quest's dynamic spawn / cooldown trigger must be driven from an
    /// <c>OnStory&lt;Event&gt;</c> event handler rather than the startUpStage fragment — i.e. the quest is
    /// Story-Manager-driven (has a <c>storyEvent</c>) and declares a <c>spawn</c> or a <c>cooldownHours</c>.
    /// IN-GAME 2026-06-19: an SM-started quest fires OnInit + OnStory&lt;Event&gt; but DOES NOT run the
    /// startUpStage Papyrus fragment (Fragment_Stage_XXXX), so a spawn hung on the startUpStage never
    /// triggered. The reliable hook is the per-event story handler, which fires on every SM delivery.</summary>
    public static bool StoryTrigger(QuestSpec q) =>
        q.StoryEvent is { } se && StoryManagerEvents.TryGet(se.Event, out var d)
        && !string.IsNullOrEmpty(d.StoryHandler)
        && (q.Spawn is not null || se.CooldownHours > 0f);

    /// <summary>True when the quest must emit an <c>OnStory&lt;Event&gt;</c> handler — i.e. it is
    /// Story-Manager-driven (has a <c>storyEvent</c> with a known handler) and has work to run on each SM
    /// delivery: a spawn/cooldown (<see cref="StoryTrigger"/>) OR a JContainers persist/syncPerks block on
    /// any stage. The latter is the "easy in-game trigger" path: an SM-started quest never runs its
    /// startUpStage fragment (in-game 2026-06-19), so a persist hung there would never fire — it must run
    /// in the OnStory handler instead. So "cast a spell → bank skill XP" hangs persist on a CastMagic
    /// quest and lets the handler drive it.</summary>
    public static bool StoryHandlerNeeded(QuestSpec q) =>
        q.StoryEvent is { } se && StoryManagerEvents.TryGet(se.Event, out var d)
        && !string.IsNullOrEmpty(d.StoryHandler)
        && (q.Spawn is not null || se.CooldownHours > 0f
            || q.Stages.Any(s => HasPersist(s) || HasSyncPerks(s))
            || q.Stages.Any(s => HasStorageWrites(s))
            || q.Stages.Any(s => s.GlobalWrites.Count > 0));

    /// <summary>The property-name prefix that namespaces a stage's JContainers persist/syncPerks
    /// properties (so several stages in one quest script never collide). MUST match between the generated
    /// source and the VMAD binding (WireQuestStages).</summary>
    internal static string StagePropPrefix(int stageIndex) => $"S{stageIndex:D4}_";

    /// <summary>The startUpStage index whose fragment must drive the quest's spawn/cooldown on start, or
    /// null if the quest has neither (or no startUpStage to hang them on). `OnInit` is unusable here —
    /// it fires once per quest lifetime, but a Story-Manager encounter relaunches the same quest on every
    /// qualifying event, so the trigger must live on the startUpStage fragment (runs on EVERY start).</summary>
    public static int? StartupStageTrigger(QuestSpec q)
    {
        // Story-Manager quests drive their spawn/cooldown from OnStory<Event> (see StoryTrigger) because
        // the startUpStage Papyrus fragment does NOT run for SM-started quests. Only a NON-storyEvent
        // (StartGameEnabled) spawn quest fires its trigger from the startUpStage fragment.
        bool hasTrigger = q.Spawn is not null && q.StoryEvent is null;
        if (!hasTrigger) return null;
        foreach (var s in q.Stages)
            if (s.StartUpStage) return s.Index;
        return null;   // spawn declared but no startUpStage to fire from (validator warns)
    }

    /// <summary>The Papyrus property name a stage fragment uses to reference an instance global — the
    /// sanitized global editorId (a vanilla ref keeps only its hex tail so it stays identifier-safe).</summary>
    internal static string InstanceGlobalProperty(string globalRef)
    {
        var s = Sanitize(globalRef);
        // A vanilla ref ("Skyrim_esm_0x12345" after sanitize) → "G_0x12345"-style safe stub.
        return char.IsLetter(s.FirstOrDefault()) ? s : "G_" + s;
    }

    /// <summary>The Papyrus source for a quest's stage-fragment script. One Fragment per stage that
    /// displays/completes the objectives linked to it. Function names follow the CK convention
    /// (<c>Fragment_Stage_XXXX_Item00000</c>) so the engine calls them automatically when
    /// <c>SetStage()</c> fires — no CK binding step needed once the .pex is compiled and the
    /// VMAD is attached by <c>package</c>.</summary>
    public static string GenerateQuestFragmentSource(QuestSpec q)
    {
        if (!QuestNeedsFragmentScript(q)) return "";
        var name = QuestFragmentScriptName(q);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Scriptname {name} extends Quest");
        sb.AppendLine("; AUTO-GENERATED by ModForge — quest stage → objective wiring + instance globals.");
        sb.AppendLine("; `package` compiles this and attaches the QuestAdapter VMAD automatically;");
        sb.AppendLine("; no Creation Kit binding step is needed.");
        sb.AppendLine();

        // GlobalVariable properties for every distinct instance global referenced by any stage. The
        // VMAD (WireQuestStages) binds each to its GLOB FormKey; the fragment body uses them by name.
        var instGlobals = q.Stages.SelectMany(s => s.InstanceGlobals).Select(g => g.Global)
            .Concat(q.Stages.SelectMany(s => s.GlobalWrites).Select(g => g.Global))   // K組 plain global writes share the GLOB property pool
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        bool anyProp = false;
        foreach (var g in instGlobals)
        { sb.AppendLine($"GlobalVariable Property {InstanceGlobalProperty(g)} Auto"); anyProp = true; }
        // Per-stage JContainers properties (Form key for an arbitrary-ref key, Form values, Perks),
        // namespaced by the stage prefix so stages don't collide.
        foreach (var s in q.Stages)
            foreach (var decl in JContainersPropertyDecls(StagePropPrefix(s.Index), s.Persist, s.SyncPerks))
            { sb.AppendLine(decl); anyProp = true; }
        if (anyProp) sb.AppendLine();

        // The startUpStage fragment (if any) drives the dynamic spawn / cooldown gate on quest start.
        int? startupTrigger = StartupStageTrigger(q);

        // Every stage that shows/completes an objective, binds an instance global, OR is the startUpStage
        // carrying a spawn/cooldown trigger — ascending.
        var objStages = q.Objectives.SelectMany(o => new[] { o.ShowStage, o.CompleteStage }).Where(s => s >= 0);
        var instStages = q.Stages.Where(s => s.InstanceGlobals.Count > 0).Select(s => (int)s.Index);
        var gwStages = q.Stages.Where(s => s.GlobalWrites.Count > 0).Select(s => (int)s.Index);
        var jcStages = q.Stages.Where(s => HasPersist(s) || HasSyncPerks(s)).Select(s => (int)s.Index);
        var swStages = q.Stages.Where(s => HasStorageWrites(s)).Select(s => (int)s.Index);
        var trigStages = startupTrigger is int st ? new[] { st } : System.Array.Empty<int>();
        var stageNums = objStages.Concat(instStages).Concat(gwStages).Concat(jcStages).Concat(swStages).Concat(trigStages).Distinct().OrderBy(s => s);

        bool hasSpawn = q.Spawn is not null;
        bool hasCooldown = q.StoryEvent is { } sev && sev.CooldownHours > 0f;
        bool storyHandler = StoryHandlerNeeded(q);

        // The trigger body is built from three pieces, shared by the startUpStage fragment (StartGameEnabled
        // spawn) and the OnStory<Event> handler (SM-driven). Splitting them lets the OnStory handler run the
        // JContainers persist/syncPerks BETWEEN the cooldown gate and the spawn (so "cast a spell → bank
        // skill XP" works on the same hook that an SM encounter spawns from).
        void AppendCooldownGate()
        {
            if (!hasCooldown) return;
            sb.AppendLine($"    {EncounterCooldownScript} __cd = self as {EncounterCooldownScript}");
            sb.AppendLine("    if __cd && !__cd.TryFire()");
            sb.AppendLine("        Stop()                                  ; still on cooldown — abort this encounter");
            sb.AppendLine("        return");
            sb.AppendLine("    endif");
        }
        void AppendSpawn()
        {
            if (!hasSpawn) return;
            sb.AppendLine($"    {DynamicSpawnScript} __spawn = self as {DynamicSpawnScript}");
            sb.AppendLine("    if __spawn");
            sb.AppendLine("        __spawn.SpawnNow()");
            sb.AppendLine("    endif");
        }
        // The JContainers persist/syncPerks of every stage that carries one (an SM quest has no per-stage
        // fragment that runs, so its persist lives here in the OnStory handler instead).
        void AppendJcStages()
        {
            foreach (var stage in jcStages)
                foreach (var stSpec in q.Stages.Where(s => s.Index == stage))
                    foreach (var line in JContainersFragmentBody(StagePropPrefix(stage), stSpec.Persist, stSpec.SyncPerks))
                        sb.AppendLine("    " + line);
        }
        // J組 PapyrusUtil StorageUtil per-Form KV writes of every stage that carries one (an SM quest's
        // stage fragment never runs, so its storage writes live in the OnStory handler — same routing as persist).
        void AppendStorageStages()
        {
            foreach (var stage in swStages)
                foreach (var stSpec in q.Stages.Where(s => s.Index == stage))
                    foreach (var line in StorageWritesBody(stSpec.StorageWrites))
                        sb.AppendLine("    " + line);
        }
        // K組 plain global writes — "<global>.SetValue(value)" for each globalWrite of every stage that
        // has one (an SM quest's stage fragment never runs, so its writes live in the OnStory handler).
        void AppendGwStages()
        {
            foreach (var stage in gwStages)
                foreach (var gw in q.Stages.Where(s => s.Index == stage).SelectMany(s => s.GlobalWrites))
                    sb.AppendLine($"    {InstanceGlobalProperty(gw.Global)}.SetValue({gw.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
        }

        foreach (var stage in stageNums)
        {
            // CK-standard name: engine calls Fragment_Stage_XXXX_Item00000 on the script when
            // SetStage(XXXX) fires (first log entry = Item00000). No manual binding required.
            sb.AppendLine($"Function Fragment_Stage_{stage:D4}_Item00000()");
            // startUpStage trigger (NON-storyEvent spawn only): cooldown gate first, then the spawn.
            // SM-driven quests route this through OnStory<Event> below instead (the startUpStage
            // fragment does not run for an SM-started quest — in-game confirmed 2026-06-19).
            if (startupTrigger == stage)
            {
                AppendCooldownGate();
                AppendSpawn();
            }
            foreach (var o in q.Objectives.Where(o => o.ShowStage == stage))
                sb.AppendLine($"    SetObjectiveDisplayed({o.Index})   ; show: {OneLine(o.Text)}");
            foreach (var o in q.Objectives.Where(o => o.CompleteStage == stage))
                sb.AppendLine($"    SetObjectiveCompleted({o.Index})   ; complete: {OneLine(o.Text)}");
            // Instance globals: optionally seed the value, then bind it to this quest instance so
            // objective text "<Global=X>" reads per-instance (gather/count radiant quests).
            foreach (var ig in q.Stages.Where(s => s.Index == stage).SelectMany(s => s.InstanceGlobals))
            {
                var p = InstanceGlobalProperty(ig.Global);
                if (ig.RandomMin is int lo && ig.RandomMax is int hi)
                    sb.AppendLine($"    {p}.SetValue(Utility.RandomInt({lo}, {hi}))");
                else if (ig.Value is float v)
                    sb.AppendLine($"    {p}.SetValue({OneLine(v.ToString(System.Globalization.CultureInfo.InvariantCulture))})");
                sb.AppendLine($"    UpdateCurrentInstanceGlobal({p})");
            }
            // K組 plain global writes for this stage (routed to OnStory below for an SM quest).
            if (!storyHandler)
                foreach (var gw in q.Stages.Where(s => s.Index == stage).SelectMany(s => s.GlobalWrites))
                    sb.AppendLine($"    {InstanceGlobalProperty(gw.Global)}.SetValue({gw.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
            // JContainers JFormDB writes + perk sync for this stage (keyed on player or an arbitrary ref —
            // a stage fragment has no akSpeakerRef). Emitted last so a perk sync sees the just-banked ranks.
            // SKIPPED for an SM quest: that fragment never runs (in-game 2026-06-19), so its persist is
            // routed to the OnStory<Event> handler below instead.
            if (!storyHandler)
                foreach (var stSpec in q.Stages.Where(s => s.Index == stage))
                    foreach (var line in JContainersFragmentBody(StagePropPrefix(stage), stSpec.Persist, stSpec.SyncPerks))
                        sb.AppendLine("    " + line);
            // J組 StorageUtil per-Form KV writes for this stage (routed to OnStory below for an SM quest).
            if (!storyHandler)
                foreach (var stSpec in q.Stages.Where(s => s.Index == stage))
                    foreach (var line in StorageWritesBody(stSpec.StorageWrites))
                        sb.AppendLine("    " + line);
            sb.AppendLine("EndFunction");
            sb.AppendLine();
        }

        // SM-driven quest: drive the work from the per-event OnStory<Event> handler. This is the hook that
        // actually fires for a Story-Manager-started quest (the startUpStage fragment does not — in-game
        // confirmed 2026-06-19). The handler signature comes from StoryManagerEvents; the body is the
        // cooldown gate, then the persist/syncPerks (so a perk sync sees the just-banked ranks), then the
        // spawn, then a Stop() so the SM can relaunch the quest on the next qualifying event (a running
        // quest is never re-started — the cooldown, if any, gates the re-fire).
        if (storyHandler && q.StoryEvent is { } se2 && StoryManagerEvents.TryGet(se2.Event, out var sdef)
            && !string.IsNullOrEmpty(sdef.StoryHandler))
        {
            sb.AppendLine($"Event {sdef.StoryHandler}");
            AppendCooldownGate();
            AppendGwStages();
            AppendJcStages();
            AppendStorageStages();
            AppendSpawn();
            sb.AppendLine("    Stop()                                  ; re-arm: let the SM relaunch this on the next event");
            sb.AppendLine("EndEvent");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string Sanitize(string s)
    {
        var chars = s.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray();
        var r = new string(chars);
        return string.IsNullOrEmpty(r) ? "X" : r;
    }
    private static string OneLine(string s) => s.Replace("\r", " ").Replace("\n", " ");
    private static string PapyrusFloat(float v) => v.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture);
}
