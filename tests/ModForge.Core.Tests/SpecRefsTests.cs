using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Xunit;

namespace ModForge.Core.Tests;

public class SpecRefsTests
{
    // Helpers: in-memory file/env maps so the engine never touches disk or the real environment.
    private static SpecRefs.FileReader Files(Dictionary<string, string> files) =>
        path => files.TryGetValue(path, out var t) ? t : null;

    private static SpecRefs.EnvLookup Env(Dictionary<string, string> env) =>
        name => env.TryGetValue(name, out var v) ? v : null;

    private static readonly SpecRefs.FileReader NoFiles = _ => null;
    private static readonly SpecRefs.EnvLookup NoEnv = _ => null;

    private static JsonNode? Resolve(string json, SpecRefs.FileReader? files = null, SpecRefs.EnvLookup? env = null) =>
        SpecRefs.Resolve(JsonNode.Parse(json), "", files ?? NoFiles, env ?? NoEnv);

    [Fact]
    public void SameDocPointer_SplicesSubNode()
    {
        var json = """
        { "presets": { "x": { "a": 1, "b": 2 } },
          "thing": { "$ref": "#/presets/x" } }
        """;
        var r = Resolve(json)!;
        Assert.Equal(1, r["thing"]!["a"]!.GetValue<int>());
        Assert.Equal(2, r["thing"]!["b"]!.GetValue<int>());
    }

    [Fact]
    public void ExternalFile_WholeFile_SplicesContent()
    {
        var files = new Dictionary<string, string> { ["p.json"] = """{ "a": 1 }""" };
        var r = Resolve("""{ "thing": { "$ref": "p.json" } }""", Files(files))!;
        Assert.Equal(1, r["thing"]!["a"]!.GetValue<int>());
    }

    [Fact]
    public void ExternalFile_WithPointer_SplicesSubNode()
    {
        var files = new Dictionary<string, string> { ["p.json"] = """{ "items": { "k": { "v": 9 } } }""" };
        var r = Resolve("""{ "thing": { "$ref": "p.json#/items/k" } }""", Files(files))!;
        Assert.Equal(9, r["thing"]!["v"]!.GetValue<int>());
    }

    [Fact]
    public void SiblingKeys_DeepMergeOverRef_SiblingWins()
    {
        var json = """
        { "presets": { "x": { "fogNear": 0, "fogFar": 9000, "color": { "r": 1, "g": 2 } } },
          "thing": { "$ref": "#/presets/x", "fogFar": 12000, "color": { "g": 50 } } }
        """;
        var r = Resolve(json)!["thing"]!;
        Assert.Equal(0, r["fogNear"]!.GetValue<int>());      // from ref
        Assert.Equal(12000, r["fogFar"]!.GetValue<int>());   // sibling overrides
        Assert.Equal(1, r["color"]!["r"]!.GetValue<int>());  // nested object merges, not replaces
        Assert.Equal(50, r["color"]!["g"]!.GetValue<int>()); // nested sibling overrides
    }

    [Fact]
    public void NestedRef_TargetContainsAnotherRef_ResolvesRecursively()
    {
        var json = """
        { "base": { "v": 7 },
          "mid": { "$ref": "#/base" },
          "top": { "$ref": "#/mid" } }
        """;
        var r = Resolve(json)!;
        Assert.Equal(7, r["top"]!["v"]!.GetValue<int>());
    }

    [Fact]
    public void ArrayRef_ChainedDeepMerge_LaterWins()
    {
        var json = """
        { "presets": {
            "base": { "fogNear": 0, "fogFar": 9000, "tint": { "r": 1, "g": 1 } },
            "warm": { "fogFar": 5000, "tint": { "g": 9 } } },
          "thing": { "$ref": [ "#/presets/base", "#/presets/warm" ], "fogNear": 3 } }
        """;
        var r = Resolve(json)!["thing"]!;
        Assert.Equal(3, r["fogNear"]!.GetValue<int>());     // sibling wins over both
        Assert.Equal(5000, r["fogFar"]!.GetValue<int>());   // warm (later) overrides base
        Assert.Equal(1, r["tint"]!["r"]!.GetValue<int>());  // base survives
        Assert.Equal(9, r["tint"]!["g"]!.GetValue<int>());  // warm overrides
    }

    [Fact]
    public void LongFormRef_FromPlusPointer_Resolves()
    {
        var files = new Dictionary<string, string> { ["presets/light.json"] = """{ "bright": { "lux": 42 } }""" };
        var json = """{ "thing": { "$ref": { "from": "presets/light.json", "pointer": "/bright" } } }""";
        var r = Resolve(json, Files(files))!["thing"]!;
        Assert.Equal(42, r["lux"]!.GetValue<int>());
    }

    [Fact]
    public void LongFormRef_UnknownKey_Throws()
    {
        var json = """{ "thing": { "$ref": { "from": "p.json", "bogus": 1 } } }""";
        Assert.Throws<SpecRefException>(() => Resolve(json, Files(new() { ["p.json"] = "{}" })));
    }

    [Fact]
    public void Env_Present_SubstitutesValue()
    {
        var json = """{ "dir": { "$env": "MF_DIR" } }""";
        var r = Resolve(json, env: Env(new() { ["MF_DIR"] = "presets" }))!;
        Assert.Equal("presets", r["dir"]!.GetValue<string>());
    }

    [Fact]
    public void Env_Missing_UsesDefault()
    {
        var json = """{ "dir": { "$env": "MF_DIR", "default": "fallback" } }""";
        var r = Resolve(json)!;
        Assert.Equal("fallback", r["dir"]!.GetValue<string>());
    }

    [Fact]
    public void Env_MissingNoDefault_Throws()
    {
        var json = """{ "dir": { "$env": "MF_DIR" } }""";
        Assert.Throws<SpecRefException>(() => Resolve(json));
    }

    [Fact]
    public void RefAndEnvTogether_Throws()
    {
        var json = """{ "x": { "$ref": "#/a", "$env": "MF_DIR" }, "a": {} }""";
        Assert.Throws<SpecRefException>(() => Resolve(json));
    }

    [Fact]
    public void Env_DrivesLongFormRefFrom()
    {
        var files = new Dictionary<string, string> { ["presets/light.json"] = """{ "bright": { "lux": 7 } }""" };
        var json = """
        { "thing": { "$ref": { "from": { "$env": "MF_PRESET", "default": "presets/light.json" }, "pointer": "/bright" } } }
        """;
        var r = Resolve(json, Files(files))!["thing"]!;
        Assert.Equal(7, r["lux"]!.GetValue<int>());
    }

    [Fact]
    public void SelfReferentialRef_Throws()
    {
        var json = """{ "a": { "$ref": "#/b" }, "b": { "$ref": "#/a" } }""";
        var ex = Assert.Throws<SpecRefException>(() => Resolve(json));
        Assert.Contains("cycle", ex.Message);
    }

    [Fact]
    public void ResolveFile_RealTempFiles_RoundTrips()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf_specrefs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "preset.json"), """{ "fogFar": 9000 }""");
            File.WriteAllText(Path.Combine(dir, "spec.json"),
                """{ "name": "x", "light": { "$ref": "preset.json", "fogFar": 12000 } }""");

            var resolved = SpecRefs.ResolveFile(Path.Combine(dir, "spec.json"));
            var node = JsonNode.Parse(resolved)!;
            Assert.Equal(12000, node["light"]!["fogFar"]!.GetValue<int>());
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
