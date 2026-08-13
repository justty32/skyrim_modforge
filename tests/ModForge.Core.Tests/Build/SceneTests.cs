using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Locks in the SCEN (multi-actor conversation) build shape against the vanilla scene structure
// discovered via `scenediag` (dunIronbindBeemJaMourning / MQSkyHavenSparring): a host quest with one
// UniqueActor-bound QuestAlias per actor, a Scene whose SceneActors reference those alias indices, and
// one Scene-subtype (SNAM='SCEN') DialogTopic+INFO per phase that a Dialog SceneAction points at.
// All master-free (in-spec NPCs + quest, no vanilla clone/cell).
public class SceneTests
{
    // Two NPCs trading two lines each, hosted by one quest. No master needed.
    private static ModSpec TwoActorScene() => new()
    {
        PluginName = "Test.esp",
        Quests = { new QuestSpec { EditorId = "SQ", Name = "SceneQuest" } },
        Npcs =
        {
            new NpcSpec { EditorId = "A", Name = "Actor A" },
            new NpcSpec { EditorId = "B", Name = "Actor B" },
        },
        Scenes =
        {
            new SceneSpec
            {
                EditorId = "Sc", QuestEditorId = "SQ",
                Actors =
                {
                    new SceneActorSpec { AliasId = 0, Npc = "A", Name = "A" },
                    new SceneActorSpec { AliasId = 1, Npc = "B", Name = "B" },
                },
                Phases =
                {
                    new ScenePhaseSpec { Speaker = 0, Lines = { "A says hi." }, Emotion = "Happy" },
                    new ScenePhaseSpec { Speaker = 1, Lines = { "B says hi back." } },
                    new ScenePhaseSpec { Speaker = 0, Lines = { "A: bye." }, Emotion = "Sad" },
                },
            },
        },
    };

    private static ISceneGetter TheScene(BuildResult r) => r.Mod.EnumerateMajorRecords<ISceneGetter>().Single();
    private static IQuestGetter HostQuest(BuildResult r) =>
        r.Mod.EnumerateMajorRecords<IQuestGetter>().Single(q => q.EditorID == "SQ");

    [Fact]
    public void SceneActionSpec_Idle_defaults_empty()
    {
        var ac = new SceneActionSpec();
        Assert.Equal("", ac.Idle);
    }

    // An idle action plays via a SceneAdapter phase fragment (compiled by `package`), but the engine
    // only RUNS a phase that has a SceneAction — every vanilla fragment phase carries a Timer. So an
    // idle action emits a Timer (to run the phase + hold the pose), NOT a Package. A pure build (no
    // compiled scripts) still attaches no VMAD — the scene stays a plain dialogue+timer scene.
    [Fact]
    public void Idle_action_emits_a_Timer_so_the_phase_runs()
    {
        var spec = TwoActorScene();   // 3 spoken phases → 3 Dialog actions
        spec.Scenes[0].Actions.Add(new SceneActionSpec { Actor = 0, StartPhase = 0, Idle = "Skyrim.esm:0x000A0000" });
        var r = TestBuild.Ok(spec);
        var sc = TheScene(r);
        Assert.Equal(4, sc.Actions.Count);   // 3 Dialog + 1 Timer (the idle's phase-run/hold)
        var timer = sc.Actions.Single(a => a.Type == SceneAction.TypeEnum.Timer);
        Assert.Equal(0u, timer.StartPhase);
        Assert.Equal(Generator.DefaultIdleHoldSeconds, timer.TimerSeconds);
        Assert.DoesNotContain(sc.Actions, a => a.Type == SceneAction.TypeEnum.Package);
        Assert.Null(sc.VirtualMachineAdapter);   // pure build (no compiled scripts) attaches no SceneAdapter
    }

    // An idle action's explicit timerSeconds becomes the pose-hold duration.
    [Fact]
    public void Idle_action_honors_explicit_timerSeconds_as_hold()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Actions.Add(new SceneActionSpec { Actor = 0, StartPhase = 0, Idle = "Skyrim.esm:0x000A0000", TimerSeconds = 6f });
        var sc = TheScene(TestBuild.Ok(spec));
        Assert.Equal(6f, sc.Actions.Single(a => a.Type == SceneAction.TypeEnum.Timer).TimerSeconds);
    }

    // The Scene record exists, hosted by the named quest, with BeginOnQuestStart (default trigger).
    [Fact]
    public void Scene_IsBuilt_HostedByQuest_BeginOnQuestStart()
    {
        var r = TestBuild.Ok(TwoActorScene());
        var sc = TheScene(r);
        Assert.Equal("Sc", sc.EditorID);
        Assert.Equal(HostQuest(r).FormKey, sc.Quest.FormKey);
        Assert.True(sc.Flags?.HasFlag(Scene.Flag.BeginOnQuestStart) == true);
    }

    // One QuestAlias per actor on the HOST quest, each UniqueActor-bound to the named NPC base record.
    [Fact]
    public void HostQuest_GetsOneUniqueActorAliasPerActor()
    {
        var r = TestBuild.Ok(TwoActorScene());
        var q = HostQuest(r);
        var aliases = q.Aliases.OfType<IQuestAliasGetter>().ToList();
        Assert.Equal(2, aliases.Count);
        var a = r.Mod.EnumerateMajorRecords<INpcGetter>().Single(n => n.EditorID == "A").FormKey;
        var b = r.Mod.EnumerateMajorRecords<INpcGetter>().Single(n => n.EditorID == "B").FormKey;
        Assert.Equal(a, aliases.Single(x => x.ID == 0).UniqueActor.FormKey);
        Assert.Equal(b, aliases.Single(x => x.ID == 1).UniqueActor.FormKey);
    }

    // The Scene's SceneActors reference the alias INDICES (not NPC FormKeys directly).
    [Fact]
    public void SceneActors_ReferenceAliasIndices()
    {
        var r = TestBuild.Ok(TwoActorScene());
        var ids = TheScene(r).Actors.Select(x => x.ID).OrderBy(x => x).ToList();
        Assert.Equal(new uint[] { 0, 1 }, ids);
    }

    // One phase + one Dialog action + one Scene/SCEN topic per phase; each action points at its topic.
    [Fact]
    public void EachPhase_GetsDialogActionAndSceneTopic()
    {
        var r = TestBuild.Ok(TwoActorScene());
        var sc = TheScene(r);
        Assert.Equal(3, sc.Phases.Count);
        Assert.Equal(3, sc.Actions.Count);
        Assert.All(sc.Actions, a => Assert.Equal(SceneAction.TypeEnum.Dialog, a.Type));
        // Every action's Topic resolves to a Scene-category, SNAM='SCEN' DialogTopic.
        var topicsByFk = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>().ToDictionary(t => t.FormKey);
        foreach (var act in sc.Actions)
        {
            Assert.False(act.Topic.FormKey.IsNull);
            var topic = topicsByFk[act.Topic.FormKey];
            Assert.Equal(DialogTopic.CategoryEnum.Scene, topic.Category);
            Assert.Equal("SCEN", topic.SubtypeName.Type);
            Assert.Single(topic.Responses);
        }
    }

    // The speaking ActorID + emotion on each Dialog action mirror the phase spec, in order.
    [Fact]
    public void DialogActions_CarrySpeakerAndEmotion_InPhaseOrder()
    {
        var r = TestBuild.Ok(TwoActorScene());
        var acts = TheScene(r).Actions.OrderBy(a => a.StartPhase).ToList();
        Assert.Equal(new int?[] { 0, 1, 0 }, acts.Select(a => a.ActorID).ToArray());
        Assert.Equal(Emotion.Happy, acts[0].Emotion);
        Assert.Equal(Emotion.Sad, acts[2].Emotion);
        // The OTHER actor is the headtrack target (two NPCs look at each other).
        Assert.Equal(1, acts[0].HeadtrackActorID);
        Assert.Equal(0, acts[1].HeadtrackActorID);
    }

    // The spoken line is on the topic's INFO response, with the phase's emotion.
    [Fact]
    public void SceneTopics_CarryTheSpokenLines()
    {
        var r = TestBuild.Ok(TwoActorScene());
        var sceneTopics = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
            .Where(t => t.Category == DialogTopic.CategoryEnum.Scene).ToList();
        Assert.Equal(3, sceneTopics.Count);
        var lines = sceneTopics.SelectMany(t => t.Responses).SelectMany(i => i.Responses)
            .Select(rsp => rsp.Text?.String).ToList();
        Assert.Contains("A says hi.", lines);
        Assert.Contains("B says hi back.", lines);
        Assert.Contains("A: bye.", lines);
    }

    // --- non-dialog scene actions (movement / timer beats, IDEAS §1b) --------------------------

    // The two-actor scene + a leading lineless BEAT phase (phase 0) that runs a Package action
    // (actor 1 walks a vanilla Travel package) and a Timer action (a 2s pause). The three dialogue
    // phases shift to indices 1/2/3.
    private static FormKey TravelPack => new(ModKey.FromNameAndExtension("Skyrim.esm"), 0x016FAA);
    private static ModSpec ActionScene()
    {
        var spec = TwoActorScene();
        var sc = spec.Scenes[0];
        sc.Phases.Insert(0, new ScenePhaseSpec());   // beat phase: no lines
        sc.Actions.Add(new SceneActionSpec { Actor = 1, Package = "Skyrim.esm:0x016FAA", StartPhase = 0, EndPhase = 0 });
        sc.Actions.Add(new SceneActionSpec { Actor = 0, TimerSeconds = 2f, StartPhase = 0, EndPhase = 0 });
        return spec;
    }

    // A Package action: Type=Package, the actor that performs it, the phase window, and the PACK
    // FormKey in Packages. Its Index continues past the dialogue actions.
    [Fact]
    public void Actions_PackageBeat_BuildsPackageSceneAction()
    {
        var r = TestBuild.Ok(ActionScene());
        var pkg = TheScene(r).Actions.Single(a => a.Type == SceneAction.TypeEnum.Package);
        Assert.Equal(1, pkg.ActorID);
        Assert.Equal(0u, pkg.StartPhase);
        Assert.Equal(0u, pkg.EndPhase);
        Assert.Contains(TravelPack, pkg.Packages.Select(p => p.FormKey));
        Assert.True(pkg.Index > 3);   // after the 3 dialogue actions
    }

    // A Timer action: Type=Timer, TimerSeconds set, no Packages, no Topic.
    [Fact]
    public void Actions_TimerBeat_BuildsTimerSceneAction()
    {
        var r = TestBuild.Ok(ActionScene());
        var timer = TheScene(r).Actions.Single(a => a.Type == SceneAction.TypeEnum.Timer);
        Assert.Equal(0, timer.ActorID);
        Assert.Equal(2f, timer.TimerSeconds);
        Assert.Empty(timer.Packages);
        Assert.True(timer.Topic.FormKey.IsNull);
    }

    // A lineless beat phase emits a ScenePhase (so actions can span it) but NO Dialog action / topic.
    [Fact]
    public void BeatPhase_EmitsScenePhase_ButNoDialogActionOrTopic()
    {
        var r = TestBuild.Ok(ActionScene());
        var sc = TheScene(r);
        Assert.Equal(4, sc.Phases.Count);                                            // 1 beat + 3 dialogue
        Assert.Equal(3, sc.Actions.Count(a => a.Type == SceneAction.TypeEnum.Dialog)); // only lined phases
        Assert.Equal(3, r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
            .Count(t => t.Category == DialogTopic.CategoryEnum.Scene));               // one topic per line
        Assert.Equal(5, sc.Actions.Count);                                           // 3 dialogue + 2 non-dialog
    }

    // LastActionIndex counts dialogue AND non-dialog actions.
    [Fact]
    public void Actions_LastActionIndex_CountsAll()
    {
        var r = TestBuild.Ok(ActionScene());
        Assert.Equal(5u, TheScene(r).LastActionIndex);
    }

    [Fact]
    public void Validate_Action_FlagsBothPackageAndTimer()
    {
        var spec = ActionScene();
        spec.Scenes[0].Actions[0].TimerSeconds = 1f;   // already has a package
        Assert.Contains(Generator.Validate(spec), p => p.Contains("action") && p.Contains("both package and timerSeconds"));
    }

    [Fact]
    public void Validate_Action_FlagsPhaseOutOfRange()
    {
        var spec = ActionScene();
        spec.Scenes[0].Actions[0].EndPhase = 99;
        Assert.Contains(Generator.Validate(spec), p => p.Contains("action") && p.Contains("phase"));
    }

    [Fact]
    public void Validate_Action_FlagsUnknownActor()
    {
        var spec = ActionScene();
        spec.Scenes[0].Actions[0].Actor = 9;   // not alias 0 or 1
        Assert.Contains(Generator.Validate(spec), p => p.Contains("action") && p.Contains("not one of the scene's actors"));
    }

    [Fact]
    public void Validate_FlagsLinelessPhaseWithNoCoveringAction()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Phases.Insert(0, new ScenePhaseSpec());   // beat phase, but NO action covers it
        Assert.Contains(Generator.Validate(spec), p => p.Contains("no lines") && p.Contains("no action"));
    }

    // --- autoStart: presence-gated repeating Scene ---------------------------------------------

    private static ModSpec AutoStartScene()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].AutoStart = new SceneAutoStartSpec
        {
            TriggerDistance = 1024f, RequireLineOfSight = true,
            CooldownSeconds = 30f, PollSeconds = 4f,
        };
        return spec;
    }

    private static IScriptEntryGetter Controller(IQuestGetter q) =>
        ((IQuestAdapterGetter)q.VirtualMachineAdapter!).Scripts
            .Single(e => e.Name == "MFSceneBanterController");

    // autoStart present → the Scene no longer auto-plays on quest start (the controller starts it).
    [Fact]
    public void AutoStart_ClearsBeginOnQuestStart()
    {
        var r = TestBuild.Ok(AutoStartScene());
        Assert.False(TheScene(r).Flags?.HasFlag(Scene.Flag.BeginOnQuestStart) == true);
    }

    // autoStart present → the reusable controller script is attached to the host quest, with the
    // Scene bound as an object property and the two actor alias indices as int properties.
    [Fact]
    public void AutoStart_AttachesController_WithSceneAndAliasProps()
    {
        var r = TestBuild.Ok(AutoStartScene());
        var entry = Controller(HostQuest(r));
        var sceneFk = TheScene(r).FormKey;
        var sceneProp = (IScriptObjectPropertyGetter)entry.Properties.Single(p => p.Name == "BanterScene");
        Assert.Equal(sceneFk, sceneProp.Object.FormKey);
        Assert.Equal(0, ((IScriptIntPropertyGetter)entry.Properties.Single(p => p.Name == "ActorAliasA")).Data);
        Assert.Equal(1, ((IScriptIntPropertyGetter)entry.Properties.Single(p => p.Name == "ActorAliasB")).Data);
    }

    // autoStart config values flow onto the controller's float/bool properties.
    [Fact]
    public void AutoStart_WiresConfigProps()
    {
        var r = TestBuild.Ok(AutoStartScene());
        var entry = Controller(HostQuest(r));
        float F(string n) => ((IScriptFloatPropertyGetter)entry.Properties.Single(p => p.Name == n)).Data;
        Assert.Equal(1024f, F("TriggerDistance"));
        Assert.Equal(4f, F("PollInterval"));
        Assert.Equal(30f, F("Cooldown"));
        Assert.True(((IScriptBoolPropertyGetter)entry.Properties.Single(p => p.Name == "RequireLOS")).Data);
    }

    // No autoStart → no controller script, BeginOnQuestStart kept (regression).
    [Fact]
    public void NoAutoStart_NoController_KeepsBeginOnQuestStart()
    {
        var r = TestBuild.Ok(TwoActorScene());
        Assert.True(TheScene(r).Flags?.HasFlag(Scene.Flag.BeginOnQuestStart) == true);
        var adapter = HostQuest(r).VirtualMachineAdapter as IQuestAdapterGetter;
        Assert.True(adapter is null || adapter.Scripts.All(e => e.Name != "MFSceneBanterController"));
    }

    // brawlOnEnd → the controller carries a BrawlOnEnd bool property (it StartCombats the two actors
    // when the scene finishes — "they come to blows after the argument").
    [Fact]
    public void AutoStart_BrawlOnEnd_WiresBoolProp()
    {
        var spec = AutoStartScene();
        spec.Scenes[0].AutoStart!.BrawlOnEnd = true;
        var r = TestBuild.Ok(spec);
        var entry = Controller(HostQuest(r));
        Assert.True(((IScriptBoolPropertyGetter)entry.Properties.Single(p => p.Name == "BrawlOnEnd")).Data);
    }

    // --- replay policy props on the controller --------------------------------------------------

    // playOnce → a PlayOnce bool property on the controller.
    [Fact]
    public void AutoStart_PlayOnce_WiresBoolProp()
    {
        var spec = AutoStartScene();
        spec.Scenes[0].AutoStart!.PlayOnce = true;
        var r = TestBuild.Ok(spec);
        var entry = Controller(HostQuest(r));
        Assert.True(((IScriptBoolPropertyGetter)entry.Properties.Single(p => p.Name == "PlayOnce")).Data);
    }

    // playHour / tolerance → float properties (default PlayHour -1 = any time).
    [Fact]
    public void AutoStart_PlayHour_WiresFloatProps()
    {
        var spec = AutoStartScene();
        spec.Scenes[0].AutoStart!.PlayHour = 12f;
        spec.Scenes[0].AutoStart!.PlayHourTolerance = 2f;
        var r = TestBuild.Ok(spec);
        var entry = Controller(HostQuest(r));
        float F(string n) => ((IScriptFloatPropertyGetter)entry.Properties.Single(p => p.Name == n)).Data;
        Assert.Equal(12f, F("PlayHour"));
        Assert.Equal(2f, F("PlayHourTolerance"));
    }

    // gateGlobal → a Gate object property bound to the in-spec GlobalVariable's FormKey.
    [Fact]
    public void AutoStart_GateGlobal_WiresGlobalObjectProp()
    {
        var spec = AutoStartScene();
        spec.Globals.Add(new GlobalSpec { EditorId = "MF_BanterGate", Type = "short", Value = 0 });
        spec.Scenes[0].AutoStart!.GateGlobal = "MF_BanterGate";
        var r = TestBuild.Ok(spec);
        var entry = Controller(HostQuest(r));
        var gate = r.Mod.EnumerateMajorRecords<IGlobalGetter>().Single(g => g.EditorID == "MF_BanterGate").FormKey;
        var prop = (IScriptObjectPropertyGetter)entry.Properties.Single(p => p.Name == "Gate");
        Assert.Equal(gate, prop.Object.FormKey);
    }

    // No gateGlobal → no Gate object property (regression — default autoStart unchanged).
    [Fact]
    public void AutoStart_NoGateGlobal_NoGateProp()
    {
        var r = TestBuild.Ok(AutoStartScene());
        var entry = Controller(HostQuest(r));
        Assert.DoesNotContain(entry.Properties, p => p.Name == "Gate");
    }

    [Fact]
    public void Validate_AutoStart_FlagsUnknownGateGlobal()
    {
        var spec = AutoStartScene();
        spec.Scenes[0].AutoStart!.GateGlobal = "NoSuchGlobal";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("gateGlobal") || (p.Contains("NoSuchGlobal")));
    }

    [Fact]
    public void Validate_AutoStart_FlagsNonStartGameEnabledQuest()
    {
        var spec = AutoStartScene();
        spec.Quests[0].StartGameEnabled = false;
        Assert.Contains(Generator.Validate(spec), p => p.Contains("autoStart") && p.Contains("StartGameEnabled"));
    }

    // --- per-phase headtrack / facing control --------------------------------------------------

    // (a) DEFAULT phase (no headtrack fields set) → looks at the OTHER actor + FaceTarget flag set.
    //     Locks the no-regression default: byte-identical to the pre-feature build.
    [Fact]
    public void DefaultPhase_HeadtracksOtherActor_AndFacesTarget()
    {
        var r = TestBuild.Ok(TwoActorScene());
        var acts = TheScene(r).Actions.OrderBy(a => a.StartPhase).ToList();
        Assert.Equal(1, acts[0].HeadtrackActorID);   // speaker 0 → other actor 1
        Assert.Equal(0, acts[1].HeadtrackActorID);   // speaker 1 → other actor 0
        Assert.All(acts, a => Assert.True(a.Flags?.HasFlag(SceneAction.Flag.FaceTarget) == true));
        Assert.All(acts, a => Assert.False(a.Flags?.HasFlag(SceneAction.Flag.HeadtrackPlayer) == true));
    }

    // (b) headtrackPlayer:true → HeadtrackPlayer flag set, HeadtrackActorID left null.
    [Fact]
    public void HeadtrackPlayer_SetsFlag_AndLeavesActorIdNull()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Phases[0].HeadtrackPlayer = true;
        var r = TestBuild.Ok(spec);
        var act = TheScene(r).Actions.OrderBy(a => a.StartPhase).First();
        Assert.True(act.Flags?.HasFlag(SceneAction.Flag.HeadtrackPlayer) == true);
        Assert.Null(act.HeadtrackActorID);
    }

    // (c) headtrackActor:<specific aliasId> → HeadtrackActorID == that id.
    [Fact]
    public void HeadtrackActor_SetsExplicitAliasId()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Phases[0].HeadtrackActor = 0;   // speaker 0 explicitly looks at itself (alias 0)
        var r = TestBuild.Ok(spec);
        var act = TheScene(r).Actions.OrderBy(a => a.StartPhase).First();
        Assert.Equal(0, act.HeadtrackActorID);
    }

    // (d) headtrackActor:-1 → no headtrack (HeadtrackActorID null), and FaceTarget:false → no flag.
    [Fact]
    public void HeadtrackActorMinusOne_AndFaceTargetFalse_NoHeadtrackNoFace()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Phases[0].HeadtrackActor = -1;
        spec.Scenes[0].Phases[0].FaceTarget = false;
        var r = TestBuild.Ok(spec);
        var act = TheScene(r).Actions.OrderBy(a => a.StartPhase).First();
        Assert.Null(act.HeadtrackActorID);
        Assert.False(act.Flags?.HasFlag(SceneAction.Flag.FaceTarget) == true);
    }

    // (e) validate catches an out-of-range headtrackActor.
    [Fact]
    public void Validate_FlagsOutOfRangeHeadtrackActor()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Phases[0].HeadtrackActor = 9;   // not alias 0 or 1
        Assert.Contains(Generator.Validate(spec), p => p.Contains("headtrackActor"));
    }

    // (e2) validate catches headtrackPlayer + a non-default headtrackActor together (ambiguous).
    [Fact]
    public void Validate_FlagsHeadtrackPlayerAndActorTogether()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Phases[0].HeadtrackPlayer = true;
        spec.Scenes[0].Phases[0].HeadtrackActor = 1;
        Assert.Contains(Generator.Validate(spec), p => p.Contains("both headtrackPlayer and headtrackActor"));
    }

    // --- validate guardrails -------------------------------------------------------------------

    [Fact]
    public void Validate_FlagsUnknownQuest()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].QuestEditorId = "NoSuchQuest";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("unknown quest"));
    }

    [Fact]
    public void Validate_FlagsPhaseSpeakerNotAnActor()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Phases[0].Speaker = 9;   // not alias 0 or 1
        Assert.Contains(Generator.Validate(spec), p => p.Contains("is not one of the scene's actors"));
    }

    [Fact]
    public void Validate_FlagsEmptyActorsAndPhases()
    {
        var spec = new ModSpec
        {
            Quests = { new QuestSpec { EditorId = "SQ" } },
            Scenes = { new SceneSpec { EditorId = "Sc", QuestEditorId = "SQ" } },
        };
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("has no actors"));
        Assert.Contains(problems, p => p.Contains("has no phases"));
    }

    [Fact]
    public void Validate_FlagsUnknownNpcRef()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Actors[0].Npc = "Ghost";   // not an in-spec record nor external ref
        Assert.Contains(Generator.Validate(spec), p => p.Contains("unresolved ref 'Ghost'"));
    }

    // --- conditions (scene-level gate + per-phase start/completion gates) -----------------------

    // A scene-level condition list -> scene.Conditions (the whole scene only starts if these pass).
    [Fact]
    public void SceneLevelConditions_AreBuilt_OnTheSceneRecord()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Conditions.Add(new ConditionSpec
        {
            Function = "GetGlobalValue", Param = "Skyrim.esm:0x00003A93", Comparison = "==", Value = 1f,
        });
        var r = TestBuild.Ok(spec);
        var sc = TheScene(r);
        Assert.Single(sc.Conditions);
    }

    // Per-phase startConditions / completionConditions -> the matching built ScenePhase, by index.
    [Fact]
    public void PhaseConditions_AreBuilt_OnTheMatchingPhase_ByIndex()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Phases[1].StartConditions.Add(new ConditionSpec
        {
            Function = "GetGlobalValue", Param = "Skyrim.esm:0x00003A93", Comparison = "==", Value = 1f,
        });
        spec.Scenes[0].Phases[2].CompletionConditions.Add(new ConditionSpec
        {
            Function = "GetGlobalValue", Param = "Skyrim.esm:0x00003A93", Comparison = ">=", Value = 1f,
        });
        var r = TestBuild.Ok(spec);
        var phases = TheScene(r).Phases.ToList();
        Assert.Equal(3, phases.Count);
        Assert.Empty(phases[0].StartConditions);
        Assert.Empty(phases[0].CompletionConditions);
        Assert.Single(phases[1].StartConditions);
        Assert.Empty(phases[1].CompletionConditions);
        Assert.Empty(phases[2].StartConditions);
        Assert.Single(phases[2].CompletionConditions);
    }

    // No-regression: a scene with no conditions has empty condition lists everywhere.
    [Fact]
    public void NoConditions_LeavesAllConditionListsEmpty()
    {
        var r = TestBuild.Ok(TwoActorScene());
        var sc = TheScene(r);
        Assert.Empty(sc.Conditions);
        Assert.All(sc.Phases, p => { Assert.Empty(p.StartConditions); Assert.Empty(p.CompletionConditions); });
    }

    // IsSceneActionComplete in a phase CompletionCondition: scene defaults to the OWNING scene, the
    // action index is carried — the standard "advance this phase when its line is done" gate.
    [Fact]
    public void CompletionCondition_IsSceneActionComplete_DefaultsToOwningScene()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Phases[0].CompletionConditions.Add(
            new ConditionSpec { Function = "IsSceneActionComplete", Comparison = "==", Value = 1, SceneActionIndex = 0 });
        var r = TestBuild.Ok(spec);
        var sc = TheScene(r);
        var cond = Assert.Single(sc.Phases[0].CompletionConditions);
        var data = Assert.IsAssignableFrom<IIsSceneActionCompleteConditionDataGetter>(cond.Data);
        Assert.Equal(sc.FormKey, data.Scene.Link.FormKey);   // defaulted to this scene
        Assert.Equal(0, data.SceneActionIndex);
    }

    [Fact]
    public void Validate_IsSceneActionComplete_Without_Index_Is_Reported()
    {
        var spec = TwoActorScene();
        spec.Scenes[0].Phases[0].CompletionConditions.Add(
            new ConditionSpec { Function = "IsSceneActionComplete", Comparison = "==", Value = 1 });   // no index
        Assert.Contains(Generator.Validate(spec), p => p.Contains("IsSceneActionComplete needs a sceneActionIndex"));
    }
}
