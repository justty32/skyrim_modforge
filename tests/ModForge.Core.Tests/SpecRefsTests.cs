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
}
