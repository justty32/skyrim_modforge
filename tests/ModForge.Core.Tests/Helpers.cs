using Mutagen.Bethesda.Plugins;
using ModForge;

namespace ModForge.Tests;

// Shared helpers. Every test here builds a spec PURELY IN MEMORY — no Skyrim.esm, no disk.
// That holds as long as the spec uses no feature that clones a vanilla record or overrides a
// vanilla cell (weapon/book/misc/potion `template`, `cells`, placements into a vanilla cell).
// Dialogue / banter / packages / conditions / relationships all build master-free.
internal static class TestBuild
{
    public static readonly ModKey Key = ModKey.FromNameAndExtension("Test.esp");

    // Build a spec and assert it produced no warnings (the default expectation for a clean spec).
    public static BuildResult Ok(ModSpec spec)
    {
        var result = Generator.Build(spec, Key);
        Assert.True(result.Warnings.Count == 0,
            "unexpected build warnings:\n  " + string.Join("\n  ", result.Warnings));
        return result;
    }

    // Build without asserting on warnings (for specs that intentionally warn).
    public static BuildResult Raw(ModSpec spec) => Generator.Build(spec, Key);

    // Build with a compiled-scripts dir (the `package` path): enables the fragment-VMAD attach steps
    // (WireQuestStages, AttachDialogueResultScripts, AttachSceneFragments) for any .pex present there.
    public static BuildResult OkWithCompiledScripts(ModSpec spec, string compiledDir)
    {
        var result = Generator.Build(spec, Key, new BuildOptions { CompiledScriptsDir = compiledDir });
        Assert.True(result.Warnings.Count == 0,
            "unexpected build warnings:\n  " + string.Join("\n  ", result.Warnings));
        return result;
    }
}
