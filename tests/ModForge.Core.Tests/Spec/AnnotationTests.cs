using System.Linq;
using System.Text.Json;
using Mutagen.Bethesda.Plugins;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Idea #24 P1 — the in-game editor's unified marker system exports advisory coordinate anchors
// into `annotations:`. A human / AI agent reads them to author real spec sections in the next
// round; build must treat them as inert (no records, no warnings).
public class AnnotationTests
{
    [Fact]
    public void Annotations_deserialize_from_spec_json()
    {
        var json = """
        {
          "pluginName": "T.esp",
          "annotations": [
            { "seq": 1, "label": "goat", "kind": "note", "note": "face the door",
              "position": { "x": 1.5, "y": -2.0, "z": 3.0 }, "angleZ": 90.0,
              "cell": "Skyrim.esm:0x01605E" }
          ]
        }
        """;
        var spec = JsonSerializer.Deserialize<ModSpec>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var a = Assert.Single(spec.Annotations);
        Assert.Equal(1, a.Seq);
        Assert.Equal("goat", a.Label);
        Assert.Equal("note", a.Kind);
        Assert.Equal("face the door", a.Note);   // the free-form agent brief rides along
        Assert.Equal(1.5f, a.Position.X);
        Assert.Equal(90.0f, a.AngleZ);
        Assert.Equal("Skyrim.esm:0x01605E", a.Cell);
        Assert.Equal("", a.Worldspace);
    }

    [Fact]
    public void Annotations_build_is_inert()
    {
        var bare = new ModSpec { PluginName = "T.esp" };
        var with = new ModSpec
        {
            PluginName = "T.esp",
            Annotations =
            {
                new AnnotationSpec { Seq = 1, Label = "hill-top", Kind = "note" },
                new AnnotationSpec { Seq = 2, Label = "p1", Kind = "navmesh" },
            },
        };
        var a = Generator.Build(bare, ModKey.FromNameAndExtension("Test.esp"));
        var b = Generator.Build(with, ModKey.FromNameAndExtension("Test.esp"));
        Assert.Equal(a.Mod.EnumerateMajorRecords().Count(), b.Mod.EnumerateMajorRecords().Count());
        Assert.DoesNotContain(b.Warnings,
            w => w.Contains("annotation", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Annotations_pass_validate()
    {
        var spec = new ModSpec
        {
            PluginName = "T.esp",
            Annotations = { new AnnotationSpec { Label = "x", Kind = "navmesh", Seq = 2 } },
        };
        Assert.Empty(Generator.Validate(spec));
    }
}
