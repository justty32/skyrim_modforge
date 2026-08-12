using ModForge;
using Xunit;

namespace ModForge.Tests;

public class PackageSafetyTests
{
    [Fact]
    public void FailedRequiredSceneSetStageCompilation_BlocksPackageBuild()
    {
        var spec = new ModSpec
        {
            Scenes =
            {
                new SceneSpec
                {
                    EditorId = "S", QuestEditorId = "Q",
                    Actions = { new SceneActionSpec { SetStage = new SceneSetStageSpec { Stage = 10 } } },
                },
            },
        };
        Assert.False(Program.CompileRequiredSceneFragments(spec, (_, _, _) => false));
    }

    [Fact]
    public void FailedIdleOnlySceneCompilation_RemainsBestEffort()
    {
        var spec = new ModSpec
        {
            Scenes =
            {
                new SceneSpec
                {
                    EditorId = "S", QuestEditorId = "Q",
                    Actions = { new SceneActionSpec { Idle = "Skyrim.esm:0x1" } },
                },
            },
        };
        Assert.True(Program.CompileRequiredSceneFragments(spec, (_, _, _) => false));
    }

    [Fact]
    public void MultipleMcmConfigs_BlockPackageBeforeOutput()
    {
        var spec = new ModSpec
        {
            McmConfigs =
            {
                new McmSpec { ModName = "First" },
                new McmSpec { ModName = "Second" },
            },
        };

        Assert.False(Program.ValidMcmPackageCount(spec));
    }

    [Fact]
    public void FailedRequiredMcmBridgeCompilation_BlocksPackageBuild()
    {
        var spec = new ModSpec
        {
            McmConfigs =
            {
                new McmSpec { ModName = "Required Bridge", Pages = { new McmPageSpec { Name = "General",
                    Content = { new McmControlSpec { Type = "toggle", Id = "bGate:General",
                        SourceType = "ModSettingBool", Global = "MF_Gate" } } } } },
            },
        };
        bool compilerCalled = false;

        var ready = Program.CompileRequiredMcmBridges(spec, (source, scriptName, label) =>
        {
            compilerCalled = true;
            Assert.Contains("extends MCM_ConfigBase", source);
            Assert.Equal(Generator.McmGlobalScriptName(spec.McmConfigs[0]), scriptName);
            return false;
        });

        Assert.True(compilerCalled);
        Assert.False(ready);
    }
}
