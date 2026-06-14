using System.Collections.Generic;
using System.Text.Json.Nodes;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// BDI/PIE config generation, verified against real DMK/BFCO (BDI) and Stormcloaks (PIE) files.
public class BdiGenTests
{
    [Fact]
    public void Bdi_EmitsFlatArray_AtCorrectPath()
    {
        var f = BdiGen.Generate(new BehaviorDataSpec
        {
            File = "DirecionalMovement_BDI",
            Entries = new List<BdiEntrySpec>
            {
                new() { ProjectPath = "Actors", Type = "kInt", Name = "DirecionalCycleMoveset", Value = 0 },
                new() { ProjectPath = "Actors", Type = "kBool", Name = "DMKLeftShift", Value = 0 },
            },
        });
        Assert.Equal("SKSE/Plugins/BehaviorDataInjector/DirecionalMovement_BDI.json", f.RelPath);
        var arr = (JsonArray)JsonNode.Parse(f.Content)!;
        Assert.Equal(2, arr.Count);
        Assert.Equal("DirecionalCycleMoveset", (string?)arr[0]!["name"]);
        Assert.Equal("kInt", (string?)arr[0]!["type"]);
        Assert.Equal("Actors", (string?)arr[0]!["projectPath"]);
    }

    [Fact]
    public void Bdi_EventEntry_OmitsValue()
    {
        var f = BdiGen.Generate(new BehaviorDataSpec
        {
            File = "x",
            Entries = new List<BdiEntrySpec> { new() { Type = "kEvent", Name = "MF_OnVow" } },
        });
        var entry = (JsonObject)((JsonArray)JsonNode.Parse(f.Content)!)[0]!;
        Assert.False(entry.ContainsKey("value"));
        Assert.Equal("kEvent", (string?)entry["type"]);
    }

    [Fact]
    public void Bdi_NonEventEntry_KeepsValue()
    {
        var f = BdiGen.Generate(new BehaviorDataSpec
        {
            File = "x",
            Entries = new List<BdiEntrySpec> { new() { Type = "kFloat", Name = "n", Value = 1.5f } },
        });
        var entry = (JsonObject)((JsonArray)JsonNode.Parse(f.Content)!)[0]!;
        Assert.Equal(1.5, (double)entry["value"]!);
    }

    [Fact]
    public void Pie_EmitsMacroTable_AtCorrectPath()
    {
        var f = PieGen.Generate(new PayloadMacroSpec
        {
            File = "VikingAxe", Section = "Intensify",
            Macros = new List<PieMacroSpec>
            {
                new() { Name = "enableIframe", Command = "@SETGHOST|1" },
                new() { Name = "$disableIframe", Command = "@SETGHOST|0" },
            },
        });
        Assert.Equal("SKSE/PayloadInterpreter/Config/VikingAxe.ini", f.RelPath);
        Assert.Contains("[Intensify]", f.Content);
        Assert.Contains("$enableIframe = @SETGHOST|1", f.Content);
        Assert.Contains("$disableIframe = @SETGHOST|0", f.Content); // already-$ name not doubled
    }
}
