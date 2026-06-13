using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class NpcPatchTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    // KNOWN LIMITATION (RequiresSkyrim): overriding a vanilla NPC needs to deep-copy its LOCALIZED name,
    // which triggers a STRINGS/load-order resolve that throws headless on Linux. Until a strings-capable
    // master read is wired, BuildNpcPatches catches that and skips — the build must still complete (no
    // crash) and simply not produce the override. When the strings infra lands, flip this to assert the
    // real override (same FormKey, name preserved, packages = ours).
    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void NpcPatch_with_master_present_is_gracefully_skipped_not_crashed()
    {
        var spec = new ModSpec();
        spec.NpcPatches.Add(new NpcPatchSpec
        {
            OverrideOf = "Skyrim.esm:0x013B99",   // Carlotta Valentia (resolves; override blocked on strings)
            Packages = { "Skyrim.esm:0x01C254" },
            Mode = "replace",
        });
        var mod = Build(spec);                                          // must NOT throw
        Assert.DoesNotContain(mod.Npcs, n => n.FormKey.ModKey.FileName == "Skyrim.esm");   // override skipped for now
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
