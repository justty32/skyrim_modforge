using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// The DECLARED install requirements (Spec.Requires.cs / Generator.Requires.cs) checked against the
// masters the build actually links (Generator.Dependencies.cs).
//
// Dependencies.cs made the dependency set visible; visibility does not stop DRIFT — a mod removed, a
// capture retaken, a line deleted — and a plugin with a missing master is dropped by Skyrim WITHOUT A
// WORD. requires[] is the author's declaration and build enforces it, both ways.
//
// Two load-bearing invariants nailed below:
//   * a spec with NO requires[] section behaves EXACTLY as before (dozens of shipped specs);
//   * requires[] is spec metadata — not one byte of it reaches the .esp.
public class RequiresTests
{
    private const string HumanRace = "Skyrim.esm:0x013746";
    private const string ProteusSpell = "PROTEUS.esp:0x08073D";
    private const string XpmseSpell = "XPMSE.esp:0x000D64";
    private const string FireBolt = "Skyrim.esm:0x012FCD";

    // The capture shape: a player clone whose spells drag two mods in as masters.
    private static ModSpec CaptureSpec(params RequirementSpec[]? requires)
    {
        var spec = new ModSpec
        {
            PluginName = "MFCap.esp",
            Requires = requires is null ? null : requires.ToList(),
            CapturedNpcs =
            {
                new CapturedNpcSpec
                {
                    EditorId = "MFCapHatak", Name = "Hatak", Race = HumanRace,
                    Spells = { FireBolt, XpmseSpell, ProteusSpell },
                },
            },
        };
        return spec;
    }

    private static RequirementSpec Req(string plugin) => new() { Plugin = plugin };

    // -- THE negative case: an old spec (no requires[]) must be untouched by all of this -------------

    [Fact]
    public void NoRequiresSection_IsNeverChecked()
    {
        var result = TestBuild.Ok(CaptureSpec(requires: null));      // two mod masters, nothing declared

        Assert.False(result.Requires.Declared);
        Assert.True(result.Requires.Ok);                             // …and it does NOT fail the build
        Assert.Empty(result.Requires.Errors);
        Assert.Empty(result.Requires.Warnings);
        Assert.Empty(Generator.Validate(CaptureSpec(requires: null)));
        // The (a) reporting is unchanged: the masters are still listed, informationally.
        Assert.Contains("PROTEUS.esp", string.Join("\n", Generator.DependencySummary(result.Dependencies)));
    }

    [Fact]
    public void RequiresSection_DoesNotChangeTheWrittenPlugin()
    {
        byte[] Write(ModSpec spec)
        {
            using var ms = new MemoryStream();
            Generator.Build(spec, TestBuild.Key).Mod.WriteToBinary(ms,
                new Mutagen.Bethesda.Plugins.Binary.Parameters.BinaryWriteParameters
                {
                    ModKey = Mutagen.Bethesda.Plugins.Binary.Parameters.ModKeyOption.NoCheck,
                });
            return ms.ToArray();
        }

        // Declaring requirements is spec metadata: same bytes, declared or not, right or wrong.
        var declared = CaptureSpec(
            new RequirementSpec { Plugin = "PROTEUS.esp", Version = "3.4", Reason = "the clone's spells", Url = "https://example" },
            Req("XPMSE.esp"));
        Assert.Equal(Write(CaptureSpec(requires: null)), Write(declared));
    }

    // -- used but NOT declared → error (the whole point) --------------------------------------------

    [Fact]
    public void UndeclaredMaster_FailsTheBuildCheck_AndNamesTheSpecField()
    {
        var result = TestBuild.Ok(CaptureSpec(Req("XPMSE.esp")));    // PROTEUS is linked but not declared

        Assert.True(result.Requires.Declared);
        Assert.False(result.Requires.Ok);
        Assert.Equal(new[] { "PROTEUS.esp" }, result.Requires.Undeclared);
        var error = Assert.Single(result.Requires.Errors);
        Assert.Contains("PROTEUS.esp", error);
        Assert.Contains("capturedNpcs[0].spells[2]", error);         // the line to delete, or to declare
        Assert.Empty(result.Requires.Warnings);
    }

    [Fact]
    public void FullyDeclared_Passes_Quietly()
    {
        var result = TestBuild.Ok(CaptureSpec(Req("PROTEUS.esp"), Req("xpmse.esp")));   // case-insensitive

        Assert.True(result.Requires.Ok);
        Assert.Empty(result.Requires.Errors);
        Assert.Empty(result.Requires.Warnings);
    }

    // An EMPTY requires[] is a declaration too: "this mod stays vanilla-only". Any mod ref then fails.
    [Fact]
    public void EmptyRequires_ForbidsEveryNonVanillaMaster()
    {
        Assert.False(TestBuild.Ok(CaptureSpec()).Requires.Ok);       // requires: []  + two mod masters

        var vanilla = new ModSpec { PluginName = "Plain.esp", Requires = new() };
        vanilla.Npcs.Add(new NpcSpec { EditorId = "MFPlain", Name = "Plain", Race = HumanRace, Spells = { FireBolt } });
        var clean = TestBuild.Ok(vanilla);
        Assert.True(clean.Requires.Declared);
        Assert.True(clean.Requires.Ok);                              // vanilla masters are not requirements
        Assert.Empty(clean.Requires.Warnings);
    }

    // -- declared but NOT used → warning only (over-stating requirements is not fatal) ---------------

    [Fact]
    public void DeclaredButUnlinkedPlugin_Warns()
    {
        var result = TestBuild.Ok(CaptureSpec(Req("PROTEUS.esp"), Req("XPMSE.esp"), Req("Wyrmstooth.esp")));

        Assert.True(result.Requires.Ok);                             // a stale line does not stop the build
        Assert.Equal(new[] { "Wyrmstooth.esp" }, result.Requires.Unused);
        Assert.Contains("never linked", Assert.Single(result.Requires.Warnings));
    }

    // A requirement with no plugin of its own (SKSE DLL / loose files) can never be a master, so it is
    // documentation only — it must NOT be mistaken for a stale declaration.
    [Fact]
    public void PluginlessRequirement_IsNeverChecked_ButIsReported()
    {
        var papyrusUtil = new RequirementSpec { Name = "PapyrusUtil SE", Reason = "storageWrites", Url = "https://nexus/13048" };
        var result = TestBuild.Ok(CaptureSpec(Req("PROTEUS.esp"), Req("XPMSE.esp"), papyrusUtil));

        Assert.True(result.Requires.Ok);
        Assert.Empty(result.Requires.Warnings);                      // not "unused" — it has nothing to link

        var file = Generator.RequiresFileText("MFCap.esp", result.Dependencies, result.Requires.Declared
            ? CaptureSpec(Req("PROTEUS.esp"), Req("XPMSE.esp"), papyrusUtil).Requires : null)!;
        Assert.Contains("PapyrusUtil SE", file);
        Assert.Contains("storageWrites", file);
    }

    // A version can be DECLARED but never verified: a Skyrim plugin carries no mod version (TES4/HEDR's
    // "version" is the file FORMAT version — 1.71 for PROTEUS 3.4 and for a two-record test .esp alike).
    // The sidecar must say so where a reader can see it, and no check may pretend otherwise.
    [Fact]
    public void DeclaredVersion_IsDocumentationOnly()
    {
        var spec = CaptureSpec(new RequirementSpec { Plugin = "PROTEUS.esp", Version = ">=3.4" }, Req("XPMSE.esp"));
        var result = TestBuild.Ok(spec);

        Assert.True(result.Requires.Ok);                             // a version never makes a build fail…
        var file = Generator.RequiresFileText("MFCap.esp", result.Dependencies, spec.Requires)!;
        Assert.Contains(">=3.4", file);                              // …it is printed for a human…
        Assert.Contains("NOT verified", file);                       // …and labelled as unverifiable.
    }

    // -- --sync-requires: the auto-declare path (capture drags dependencies in by the dozen) ---------

    [Fact]
    public void Sync_AddsWhatIsLinked_KeepsAuthoredMetadata_DropsWhatIsStale()
    {
        var deps = TestBuild.Ok(CaptureSpec(requires: null)).Dependencies;
        var declared = new List<RequirementSpec>
        {
            new() { Name = "PapyrusUtil SE", Reason = "storageWrites" },                  // no plugin: untouched
            new() { Plugin = "PROTEUS.esp", Version = "3.4", Reason = "hand-written" },   // linked: kept AS IS
            new() { Plugin = "Wyrmstooth.esp" },                                          // stale: dropped
        };

        var sync = Generator.SyncRequires(declared, deps);

        Assert.True(sync.Changed);
        Assert.Equal(new[] { "XPMSE.esp" }, sync.Added);
        Assert.Equal(new[] { "Wyrmstooth.esp" }, sync.Removed);
        Assert.Equal(new[] { "PapyrusUtil SE", "PROTEUS.esp", "XPMSE.esp" }, sync.Entries.Select(e => e.Label).ToArray());
        Assert.Equal("hand-written", sync.Entries[1].Reason);        // authored metadata survives a sync
        Assert.Equal("3.4", sync.Entries[1].Version);
        Assert.Contains("capturedNpcs[0].spells[1]", sync.Entries[2].Reason);   // auto reason = the causal field

        // Syncing an already-correct declaration is a no-op (so the spec file is not rewritten).
        Assert.False(Generator.SyncRequires(sync.Entries, deps).Changed);
    }

    [Fact]
    public void Sync_OfAnUndeclaredSpec_MakesTheCheckPass()
    {
        var spec = CaptureSpec(requires: null);
        var sync = Generator.SyncRequires(spec.Requires, TestBuild.Ok(spec).Dependencies);

        spec.Requires = sync.Entries.ToList();
        var after = TestBuild.Ok(spec);
        Assert.True(after.Requires.Declared);
        Assert.True(after.Requires.Ok);
        Assert.Empty(after.Requires.Warnings);
    }

    // -- shape (`validate`) + JSON surface ----------------------------------------------------------

    [Fact]
    public void Validate_RejectsAMalformedRequirement()
    {
        var spec = CaptureSpec(
            new RequirementSpec(),                       // neither plugin nor name
            new RequirementSpec { Plugin = "PapyrusUtil" },   // not a plugin filename → belongs under `name`
            Req("PROTEUS.esp"), Req("proteus.esp"));    // declared twice

        var problems = Generator.Validate(spec);

        Assert.Contains(problems, p => p.StartsWith("requires[0]:"));
        Assert.Contains(problems, p => p.Contains("requires[1]") && p.Contains("not a plugin filename"));
        Assert.Contains(problems, p => p.Contains("requires[3]") && p.Contains("more than once"));
    }

    [Fact]
    public void Json_AcceptsTheStringShorthand_AndAbsenceMeansNull()
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var spec = JsonSerializer.Deserialize<ModSpec>(
            """{"pluginName":"X.esp","requires":["PROTEUS.esp",{"name":"PapyrusUtil SE","version":"4.4"}]}""", opts)!;
        Assert.Equal(2, spec.Requires!.Count);
        Assert.Equal("PROTEUS.esp", spec.Requires[0].Plugin);
        Assert.Equal("PapyrusUtil SE", spec.Requires[1].Name);
        Assert.Equal("4.4", spec.Requires[1].Version);

        Assert.Null(JsonSerializer.Deserialize<ModSpec>("""{"pluginName":"X.esp"}""", opts)!.Requires);

        // A plugin-only entry round-trips back to the shorthand (what --sync-requires writes).
        Assert.Equal("""["PROTEUS.esp"]""", JsonSerializer.Serialize(new List<RequirementSpec> { Req("PROTEUS.esp") }));
    }
}
