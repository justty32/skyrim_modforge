using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class NpcPatchTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    // RequiresSkyrim: overriding a vanilla NPC deep-copies its LOCALIZED name. MasterCache now provisions
    // the vanilla English .STRINGS (extracted from Skyrim - Interface.bsa into a loose temp folder), so the
    // override resolves headless: same FormKey (it IS an override of the master record), the real English
    // name preserved, and the package list replaced with ours.
    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void NpcPatch_overrides_vanilla_npc_preserving_name_and_swapping_packages()
    {
        var sandbox = FormKey.Factory("01C254:Skyrim.esm");
        var carlotta = FormKey.Factory("013B99:Skyrim.esm");
        var spec = new ModSpec();
        spec.NpcPatches.Add(new NpcPatchSpec
        {
            OverrideOf = "Skyrim.esm:0x013B99",   // Carlotta Valentia
            Packages = { "Skyrim.esm:0x01C254" }, // Sandbox
            Mode = "replace",
        });
        var mod = Build(spec);
        var ov = Assert.Single(mod.Npcs.Where(n => n.FormKey == carlotta));   // the override landed
        Assert.False(string.IsNullOrWhiteSpace(ov.Name?.String), "the vanilla English name resolved");
        Assert.Single(ov.Packages);
        Assert.Equal(sandbox, ov.Packages[0].FormKey);                        // packages replaced with ours
    }

    // Offline: a spec carrying an npcPatch builds without crashing (the patch is skipped when the master
    // isn't resolvable — graceful degradation, not an exception).
    [Fact]
    public void NpcPatch_builds_without_crashing()
    {
        var spec = new ModSpec();
        spec.NpcPatches.Add(new NpcPatchSpec { OverrideOf = "Skyrim.esm:0x013B99", Packages = { "Skyrim.esm:0x01C254" } });
        var mod = Build(spec);
        Assert.NotNull(mod);
    }

    [Fact]
    public void NpcPatch_validation_flags_bad_overrideOf_empty_packages_and_bad_mode()
    {
        var spec = new ModSpec();
        spec.NpcPatches.Add(new NpcPatchSpec { OverrideOf = "NotAnExternalRef", Packages = { }, Mode = "swap" });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("overrideOf") && p.Contains("NotAnExternalRef"));
        Assert.Contains(problems, p => p.Contains("no packages"));
        Assert.Contains(problems, p => p.Contains("invalid mode") && p.Contains("swap"));
    }
}
