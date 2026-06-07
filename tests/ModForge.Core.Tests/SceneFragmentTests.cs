using System.IO;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Locks in the pure scene phase-fragment generator (PlayIdle). String in / string out, no Skyrim
// master / Wine. Convention decoded from vanilla SF_BardSongsBallad01Scene (Task 0 spike):
// `extends Scene`, one Fragment_<phase> per idle phase calling <alias>.GetActorRef().PlayIdle(<idle>).
public class SceneFragmentTests
{
    private static SceneSpec OneIdleScene() => new()
    {
        EditorId = "MF_OathScene", QuestEditorId = "MF_OathQuest",
        Actors = { new SceneActorSpec { AliasId = 0 } },
        Phases =
        {
            new ScenePhaseSpec { Speaker = 0, Lines = { "" } },   // phase 0: kneel
            new ScenePhaseSpec { Speaker = 0, Lines = { "" } },   // phase 1: stand
        },
        Actions =
        {
            new SceneActionSpec { Actor = 0, StartPhase = 0, Idle = "Skyrim.esm:0x000A0000" },
            new SceneActionSpec { Actor = 0, StartPhase = 1, Idle = "Skyrim.esm:0x000B0000" },
        },
    };

    [Fact]
    public void Scene_with_idle_actions_needs_fragment_script()
    {
        var s = OneIdleScene();
        Assert.True(Generator.SceneNeedsFragmentScript(s));
        Assert.Equal("SF_MF_OathScene", Generator.SceneFragmentScriptName(s));
    }

    [Fact]
    public void Scene_fragment_source_has_extends_Scene_and_one_function_per_idle_phase()
    {
        var src = Generator.GenerateSceneFragmentSource(OneIdleScene());
        Assert.Contains("Scriptname SF_MF_OathScene extends Scene", src);
        Assert.Contains("Function Fragment_0()", src);   // phase 0 idle
        Assert.Contains("Function Fragment_1()", src);   // phase 1 idle
        Assert.Contains("GetActorRef()", src);           // vanilla method (NOT GetActorReference)
        Assert.Contains("PlayIdle", src);
    }

    [Fact]
    public void Scene_without_idle_actions_gets_no_fragment_script()
    {
        var s = new SceneSpec
        {
            EditorId = "MF_Plain", QuestEditorId = "MF_Q",
            Actors = { new SceneActorSpec() },
            Phases = { new ScenePhaseSpec { Speaker = 0, Lines = { "Hi" } } },
            Actions = { new SceneActionSpec { Actor = 0, StartPhase = 0 } },   // dialog, no idle
        };
        Assert.False(Generator.SceneNeedsFragmentScript(s));
        Assert.Equal("", Generator.SceneFragmentScriptName(s));
        Assert.Equal("", Generator.GenerateSceneFragmentSource(s));
    }

    // A minimal buildable single-actor scene with two idle phases, hosted by MF_OathQuest.
    private static ModSpec MinimalOathSpec() => new()
    {
        PluginName = "Test.esp",
        Quests = { new QuestSpec { EditorId = "MF_OathQuest", Name = "Oath" } },
        Npcs = { new NpcSpec { EditorId = "Oath", Name = "Oathtaker" } },
        Scenes =
        {
            new SceneSpec
            {
                EditorId = "MF_OathScene", QuestEditorId = "MF_OathQuest",
                Actors = { new SceneActorSpec { AliasId = 0, Npc = "Oath", Name = "Oath" } },
                Phases =
                {
                    new ScenePhaseSpec { Speaker = 0, Lines = { "I pledge." } },
                    new ScenePhaseSpec { Speaker = 0, Lines = { "It is done." } },
                },
                Actions =
                {
                    new SceneActionSpec { Actor = 0, StartPhase = 0, Idle = "Skyrim.esm:0x000A0000" },
                    new SceneActionSpec { Actor = 0, StartPhase = 1, Idle = "Skyrim.esm:0x000B0000" },
                },
            },
        },
    };

    [Fact]
    public void Scene_fragments_attached_when_pex_present()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        File.WriteAllBytes(Path.Combine(dir, "SF_MF_OathScene.pex"), System.Array.Empty<byte>());

        var r = TestBuild.OkWithCompiledScripts(MinimalOathSpec(), dir);
        var sc = r.Mod.EnumerateMajorRecords<ISceneGetter>().Single(x => x.EditorID == "MF_OathScene");

        Assert.NotNull(sc.VirtualMachineAdapter);
        var frags = sc.VirtualMachineAdapter!.ScriptFragments!;
        Assert.Equal("SF_MF_OathScene", frags.FileName);
        // Canonical vanilla VMAD values — wrong values make the engine silently skip the fragment.
        Assert.Equal(2, frags.ExtraBindDataVersion);
        Assert.Equal(ScriptEntry.Flag.Local, sc.VirtualMachineAdapter.Scripts.Single().Flags);
        var pf = frags.PhaseFragments;
        Assert.Equal(2, pf.Count);
        Assert.Equal(new byte[] { 0, 1 }, pf.Select(f => f.Index).OrderBy(i => i).ToArray());
        Assert.All(pf, f => Assert.Equal("SF_MF_OathScene", f.ScriptName));
        Assert.All(pf, f => Assert.True(f.Flags.HasFlag(ScenePhaseFragment.Flag.OnStart)));
        Assert.All(pf, f => Assert.Equal(16777216u, f.Unknown));   // 0x01000000 — or the engine skips it
        Assert.Contains(pf, f => f.FragmentName == "Fragment_0");
        Assert.Contains(pf, f => f.FragmentName == "Fragment_1");

        // Idle_<phase> + Actor_<phase> properties bound on the single script entry; Actor_* points at
        // the host quest with the actor's alias index.
        var props = sc.VirtualMachineAdapter.Scripts.Single(e => e.Name == "SF_MF_OathScene").Properties;
        var hostQuest = r.Mod.EnumerateMajorRecords<IQuestGetter>().Single(q => q.EditorID == "MF_OathQuest");
        var actor0 = (IScriptObjectPropertyGetter)props.Single(p => p.Name == "Actor_0");
        Assert.Equal(hostQuest.FormKey, actor0.Object.FormKey);
        Assert.Equal(0, actor0.Alias);
        Assert.Contains(props, p => p.Name == "Idle_0");
        Assert.Contains(props, p => p.Name == "Idle_1");
    }

    [Fact]
    public void Scene_fragments_not_attached_without_pex()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;   // empty — no SF_*.pex
        var r = TestBuild.OkWithCompiledScripts(MinimalOathSpec(), dir);
        var sc = r.Mod.EnumerateMajorRecords<ISceneGetter>().Single(x => x.EditorID == "MF_OathScene");
        Assert.Null(sc.VirtualMachineAdapter);
    }

    [Fact]
    public void Validate_accepts_an_idle_only_action()
    {
        Assert.Empty(Generator.Validate(MinimalOathSpec()));
    }

    [Fact]
    public void Validate_allows_idle_plus_timer_as_a_pose_hold()
    {
        var spec = MinimalOathSpec();
        spec.Scenes[0].Actions[0].TimerSeconds = 3f;   // idle + hold duration — allowed
        Assert.Empty(Generator.Validate(spec));
    }

    [Fact]
    public void Validate_rejects_an_action_that_sets_both_idle_and_package()
    {
        var spec = MinimalOathSpec();
        spec.Scenes[0].Actions[0].Package = "Skyrim.esm:0x016FAA";   // idle AND package — mutually exclusive
        Assert.Contains(Generator.Validate(spec),
            p => p.Contains("sets both idle and package"));
    }
}
