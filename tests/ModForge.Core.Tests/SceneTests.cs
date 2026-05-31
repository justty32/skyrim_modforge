using System.Linq;
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
}
