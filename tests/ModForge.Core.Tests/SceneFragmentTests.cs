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
}
