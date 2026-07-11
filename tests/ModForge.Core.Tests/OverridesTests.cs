using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Idea #24 numpad editor — `overrides[]` re-stamps the transform of an EXISTING placed ref:
// override it into our mod (parent cell/worldspace pulled in automatically, same machinery as
// removals), set the new position/rotation, touch scale only when the spec says so.
// Resolving the ref needs the master link cache → RequiresSkyrim.
public class OverridesTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static OverrideSpec Hoe(float z = -4539f, float? scale = null) => new()
    {
        Ref = "Skyrim.esm:0x0D1991",   // WhiterunStablesSkulvarHoe5 — same guinea pig as RemovalsTests
        Position = new Vec3 { X = 19265.9f, Y = -12816.5f, Z = z },
        Rotation = new Vec3 { X = 0f, Y = 0f, Z = 90f },
        Scale = scale,
    };

    [Fact]
    public void Validate_NonExternalOverride_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.Overrides.Add(new OverrideSpec { Ref = "SomeEditorId" });
        Assert.Contains(Validate(s), p => p.Contains("override") && p.Contains("SomeEditorId"));
    }

    [Fact]
    public void Validate_EmptyRef_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.Overrides.Add(new OverrideSpec());
        Assert.Contains(Validate(s), p => p.Contains("override") && p.Contains("empty ref"));
    }

    [Fact]
    public void Validate_RefInBothOverridesAndRemovals_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp", Removals = { "Skyrim.esm:0x0D1991" } };
        s.Overrides.Add(Hoe());
        Assert.Contains(Validate(s), p => p.Contains("override") && p.Contains("removals"));
    }

    [Fact]
    public void Validate_ExternalOverride_NoProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.Overrides.Add(Hoe());
        Assert.DoesNotContain(Validate(s), p => p.Contains("override"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Override_RestampsTransform_NotDisabled()
    {
        var s = new ModSpec { PluginName = "MFOv.esp" };
        s.Overrides.Add(Hoe());
        var mod = Generator.Build(s, ModKey.FromNameAndExtension("MFOv.esp")).Mod;

        var hoe = mod.EnumerateMajorRecords<IPlacedObjectGetter>()
            .Single(r => r.FormKey == FormKey.Factory("0D1991:Skyrim.esm"));
        Assert.Equal(-4539f, hoe.Placement!.Position.Z, 1f);
        Assert.Equal(System.MathF.PI / 2f, hoe.Placement!.Rotation.Z, 0.001f);  // 90° in radians
        // A moved ref is NOT a removed ref: no InitiallyDisabled, no bury.
        Assert.True((hoe.MajorRecordFlagsRaw & 0x800) == 0, "override must not disable the ref");
        // Scale null = keep the original record's scale (the hoe has none → stays none).
        Assert.Null(hoe.Scale);
        // The parent (exterior) cell came in as an override automatically.
        Assert.NotEmpty(mod.Worldspaces);
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Override_ScaleExplicit_Applied_And_OneDropsXscl()
    {
        var s = new ModSpec { PluginName = "MFOv2.esp" };
        s.Overrides.Add(Hoe(scale: 1.5f));
        var mod = Generator.Build(s, ModKey.FromNameAndExtension("MFOv2.esp")).Mod;
        var hoe = mod.EnumerateMajorRecords<IPlacedObjectGetter>()
            .Single(r => r.FormKey == FormKey.Factory("0D1991:Skyrim.esm"));
        Assert.Equal(1.5f, hoe.Scale!.Value, 0.001f);

        var s2 = new ModSpec { PluginName = "MFOv3.esp" };
        s2.Overrides.Add(Hoe(scale: 1f));   // explicit 1.0 = engine default = XSCL dropped
        var mod2 = Generator.Build(s2, ModKey.FromNameAndExtension("MFOv3.esp")).Mod;
        var hoe2 = mod2.EnumerateMajorRecords<IPlacedObjectGetter>()
            .Single(r => r.FormKey == FormKey.Factory("0D1991:Skyrim.esm"));
        Assert.Null(hoe2.Scale);
    }
}
