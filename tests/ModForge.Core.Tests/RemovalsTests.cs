using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Idea #24 §E eraser — `removals[]` disables an EXISTING vanilla placed ref: override it into our mod
// (parent cell/worldspace pulled in automatically), set InitiallyDisabled (0x800) + bury (Z −30000).
// Resolving the ref needs the master link cache → RequiresSkyrim.
public class RemovalsTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    [Fact]
    public void Validate_NonExternalRemoval_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp", Removals = { "SomeEditorId" } };
        Assert.Contains(Validate(s), p => p.Contains("removal") && p.Contains("SomeEditorId"));
    }

    [Fact]
    public void Validate_ExternalRemoval_NoProblem()
    {
        var s = new ModSpec { PluginName = "M.esp", Removals = { "Skyrim.esm:0x0D1991" } };
        Assert.DoesNotContain(Validate(s), p => p.Contains("removal"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Removal_OverridesVanillaRef_DisabledAndBuried()
    {
        // WhiterunStablesSkulvarHoe5 (a placed clutter object in an exterior cell).
        var s = new ModSpec { PluginName = "MFRem.esp", Removals = { "Skyrim.esm:0x0D1991" } };
        var mod = Generator.Build(s, ModKey.FromNameAndExtension("MFRem.esp")).Mod;

        var hoe = mod.EnumerateMajorRecords<IPlacedObjectGetter>()
            .Single(r => r.FormKey == FormKey.Factory("0D1991:Skyrim.esm"));
        Assert.True((hoe.MajorRecordFlagsRaw & 0x800) != 0, "removed ref must be InitiallyDisabled");
        // Buried far below its original Z (~ -4603) so a havok object can't linger where it stood.
        Assert.True(hoe.Placement!.Position.Z < -30000, "removed ref should be buried");
        // The parent (exterior) cell came in as an override automatically.
        Assert.NotEmpty(mod.Worldspaces);
    }
}
