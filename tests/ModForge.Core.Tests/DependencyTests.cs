using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// External-master visibility (Generator.Dependencies.cs). Referencing `PROTEUS.esp:0x08073D` in a spec
// makes PROTEUS.esp a MASTER, and Skyrim silently drops a plugin whose masters are missing. We do not
// filter mod-sourced content (the user's call: full fidelity beats portability) — we make it visible.
//
// The load-bearing invariant, nailed below: this is PURE VISIBILITY. Analysis must not touch the mod,
// so the .esp is byte-for-byte the same whether it runs or not.
public class DependencyTests
{
    private const string HumanRace = "Skyrim.esm:0x013746";
    private const string ProteusSpell = "PROTEUS.esp:0x08073D";
    private const string XpmseSpell = "XPMSE.esp:0x000D64";
    private const string FireBolt = "Skyrim.esm:0x012FCD";

    private static MasterDependency? Dep(BuildResult r, string master) =>
        r.Dependencies.FirstOrDefault(d => d.Master == master);

    // A player clone: vanilla spells plus the two the mods gave them (the MFCapHatak shape).
    private static ModSpec CaptureSpec() => new()
    {
        PluginName = "MFCap.esp",
        CapturedNpcs =
        {
            new CapturedNpcSpec
            {
                EditorId = "MFCapHatak", Name = "Hatak", Race = HumanRace,
                Spells = { FireBolt, XpmseSpell, ProteusSpell },
            },
        },
    };

    // -- the negative case: a plain vanilla spec must say NOTHING (this must not become noise) ------

    [Fact]
    public void VanillaOnlySpec_ReportsNoExternalMaster()
    {
        var spec = new ModSpec { PluginName = "Plain.esp" };
        spec.Npcs.Add(new NpcSpec { EditorId = "MFPlain", Name = "Plain", Race = HumanRace, Spells = { FireBolt } });

        var result = TestBuild.Ok(spec);

        Assert.All(result.Dependencies, d => Assert.True(d.Vanilla, $"{d.Master} should be vanilla"));
        Assert.Contains(result.Dependencies, d => d.Master == "Skyrim.esm");
        Assert.Empty(Generator.DependencySummary(result.Dependencies));            // prints nothing
        Assert.Null(Generator.RequiresFileText("Plain.esp", result.Dependencies));  // writes no sidecar
    }

    // -- the real case: every mod-sourced ref is listed, with the SPEC FIELD that pulled it in -------

    [Fact]
    public void CapturedNpc_ListsEachModMaster()
    {
        var result = TestBuild.Ok(CaptureSpec());

        var external = result.Dependencies.Where(d => !d.Vanilla).Select(d => d.Master).ToList();
        Assert.Equal(new[] { "PROTEUS.esp", "XPMSE.esp" }, external.OrderBy(m => m).ToArray());
        Assert.Contains(result.Dependencies, d => d.Master == "Skyrim.esm" && d.Vanilla);
    }

    [Fact]
    public void Attribution_NamesTheAuthoredSpecField()
    {
        var result = TestBuild.Ok(CaptureSpec());

        // The point of the whole feature: not "you depend on PROTEUS" but "THIS line depends on PROTEUS".
        Assert.Equal(new[] { $"capturedNpcs[0].spells[2] = {ProteusSpell}" }, Dep(result, "PROTEUS.esp")!.SpecSources);
        Assert.Equal(new[] { $"capturedNpcs[0].spells[1] = {XpmseSpell}" }, Dep(result, "XPMSE.esp")!.SpecSources);

        // …and the record that carries it, so the esp side is checkable too.
        Assert.Contains("Npc:MFCapHatak", Dep(result, "PROTEUS.esp")!.RecordSources);
    }

    // The macro-expansion trap: capturedNpcs[] becomes npcs[] before any record is built, so a naive
    // walk of the built spec reports a field that exists in NO file the author wrote.
    [Fact]
    public void Attribution_SurvivesMacroExpansion()
    {
        var spec = CaptureSpec();
        Generator.ExpandMacros(spec);              // what `package` does before it calls Build
        var result = Generator.Build(spec, TestBuild.Key);

        Assert.Equal(new[] { $"capturedNpcs[0].spells[2] = {ProteusSpell}" }, Dep(result, "PROTEUS.esp")!.SpecSources);
        Assert.DoesNotContain(Dep(result, "PROTEUS.esp")!.SpecSources, s => s.StartsWith("npcs["));
    }

    [Fact]
    public void Attribution_ReachesHandWrittenSpecsToo()
    {
        // Scope is NOT capture — any spec field naming a mod does this. A placement's base is enough.
        var spec = new ModSpec { PluginName = "Hand.esp" };
        spec.Cells.Add(new CellSpec { EditorId = "MFRoom", Name = "Room" });
        spec.Placements.Add(new PlacementSpec { EditorId = "MFThing", Base = "PROTEUS.esp:0x000123", Cell = "MFRoom" });

        var result = TestBuild.Raw(spec);

        Assert.Equal(new[] { "placements[0].base = PROTEUS.esp:0x000123" }, Dep(result, "PROTEUS.esp")!.SpecSources);
    }

    // -- classification ----------------------------------------------------------------------------

    [Theory]
    [InlineData("Skyrim.esm", true)]
    [InlineData("Update.esm", true)]
    [InlineData("Dawnguard.esm", true)]
    [InlineData("HearthFires.esm", true)]
    [InlineData("Dragonborn.esm", true)]
    [InlineData("PROTEUS.esp", false)]
    [InlineData("ccBGSSSE001-Fish.esm", false)]     // Creation Club: owned per account, NOT every install
    [InlineData("_ResourcePack.esl", false)]
    public void VanillaMasters_AreTheBaseGamePlusDlcOnly(string master, bool vanilla) =>
        Assert.Equal(vanilla, Generator.IsVanillaMaster(master));

    [Theory]
    [InlineData("ccBGSSSE001-Fish.esm", true)]
    [InlineData("ccQDRSSE001-SurvivalMode.esl", true)]
    [InlineData("_ResourcePack.esl", true)]
    [InlineData("PROTEUS.esp", false)]
    [InlineData("ccc_mod.esp", false)]              // "cc" prefix alone is not Creation Club
    public void CreationClubMasters_AreFlaggedButStillRequired(string master, bool cc)
    {
        Assert.Equal(cc, Generator.IsCreationClubMaster(master));
        if (cc) Assert.False(Generator.IsVanillaMaster(master));   // flagged ≠ excused
    }

    // -- reporting ---------------------------------------------------------------------------------

    [Fact]
    public void Summary_AndSidecar_NameTheMasterAndTheField()
    {
        var deps = TestBuild.Ok(CaptureSpec()).Dependencies;

        var summary = string.Join("\n", Generator.DependencySummary(deps));
        Assert.Contains("2 non-vanilla master(s)", summary);
        Assert.Contains("PROTEUS.esp", summary);
        Assert.Contains("capturedNpcs[0].spells[2]", summary);
        Assert.DoesNotContain("Skyrim.esm  (", summary);            // vanilla is not an install requirement

        var file = Generator.RequiresFileText("MFCap.esp", deps)!;
        Assert.Contains("MFCap.esp — install requirements", file);
        Assert.Contains("PROTEUS.esp", file);
        Assert.Contains($"capturedNpcs[0].spells[2] = {ProteusSpell}", file);
        Assert.Contains("vanilla (in every install, no action needed): Skyrim.esm", file);
    }

    // -- THE nail: pure visibility. The analysis must not perturb the plugin by one byte. ------------

    [Fact]
    public void Analysis_DoesNotChangeTheWrittenPlugin()
    {
        byte[] Write(ModSpec spec, bool analyze)
        {
            var mod = Generator.Build(spec, TestBuild.Key).Mod;
            if (analyze)
            {
                Generator.AnalyzeDependencies(mod, spec);
                Generator.AnalyzeDependencies(mod, spec);   // twice — an accumulating side effect would show
            }
            using var ms = new MemoryStream();
            mod.WriteToBinary(ms, new Mutagen.Bethesda.Plugins.Binary.Parameters.BinaryWriteParameters
            {
                ModKey = Mutagen.Bethesda.Plugins.Binary.Parameters.ModKeyOption.NoCheck,
            });
            return ms.ToArray();
        }

        Assert.Equal(Write(CaptureSpec(), analyze: false), Write(CaptureSpec(), analyze: true));
    }
}
