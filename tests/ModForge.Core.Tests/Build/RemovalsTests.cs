using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

    // -- JSON round-trip: bare string shorthand + the object form carrying label/note ----------------
    // No round-trip coverage existed for this shape before; this is the safety net for the additive
    // label/note change (mirrors RequiresTests.Json_AcceptsTheStringShorthand_AndAbsenceMeansNull).
    [Fact]
    public void Json_AcceptsTheStringShorthand_AndTheObjectForm()
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var spec = JsonSerializer.Deserialize<ModSpec>(
            """
            {"pluginName":"X.esp","removals":[
                "Skyrim.esm:0x0D1991",
                {"ref":"Skyrim.esm:0x0D1992","label":"the barrel","note":"cleared for the shelf"}
            ]}
            """, opts)!;

        Assert.Equal(2, spec.Removals.Count);
        Assert.Equal("Skyrim.esm:0x0D1991", spec.Removals[0].Ref);
        Assert.Equal("", spec.Removals[0].Label);
        Assert.Equal("", spec.Removals[0].Note);

        Assert.Equal("Skyrim.esm:0x0D1992", spec.Removals[1].Ref);
        Assert.Equal("the barrel", spec.Removals[1].Label);
        Assert.Equal("cleared for the shelf", spec.Removals[1].Note);

        // A plain ref (no label/note) round-trips back to the bare-string shorthand.
        Assert.Equal(
            """["Skyrim.esm:0x0D1991"]""",
            JsonSerializer.Serialize(new List<RemovalSpec> { "Skyrim.esm:0x0D1991" }));

        // An annotated removal serializes as the object form.
        var annotated = new List<RemovalSpec>
        {
            new() { Ref = "Skyrim.esm:0x0D1992", Label = "the barrel", Note = "cleared for the shelf" },
        };
        Assert.Equal(
            """[{"ref":"Skyrim.esm:0x0D1992","label":"the barrel","note":"cleared for the shelf"}]""",
            JsonSerializer.Serialize(annotated));
    }
}
