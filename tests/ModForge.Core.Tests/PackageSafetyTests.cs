using ModForge;
using Xunit;

namespace ModForge.Tests;

public class PackageSafetyTests
{
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
