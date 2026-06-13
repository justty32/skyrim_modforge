# Spec `$ref` / `$env` Resolution Layer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a pre-deserialization JSON preprocessor so ModForge specs can pull JSON in from other files / sub-nodes via `$ref` and substitute environment variables via `$env`, turning a "named preset library" into a folder of preset JSON files referenced by `$ref`.

**Architecture:** A pure `JsonNode`-tree resolver in `ModForge.Core` (`SpecRefs`) with injected file-reader + env-lookup delegates (testable without disk/env). The CLI runs it as a single chokepoint between reading the spec file and deserializing into `ModSpec`; the builder is untouched.

**Tech Stack:** C# / .NET 10, `System.Text.Json` + `System.Text.Json.Nodes` (`JsonNode`/`JsonObject`/`JsonArray`/`JsonValue`), xUnit 2.9.2.

Design doc: `docs/superpowers/specs/2026-06-13-spec-refs-env-design.md`.

**Conventions (from CLAUDE.md):** files stay < 300 lines; commit with multiple `-m` flags; offline regression is `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"` (or `scripts/test-offline.sh`); do not push.

---

## File Structure

| File | Responsibility | New/Mod |
|------|----------------|---------|
| `src/ModForge.Core/SpecRefs.cs` | The resolver: `$ref` + `$env` over a `JsonNode` tree; pure engine + `ResolveFile` disk convenience | **New** |
| `tests/ModForge.Core.Tests/SpecRefsTests.cs` | Unit tests for every directive form | **New** |
| `src/ModForge.Cli/Program.cs` | `ResolveSpecJson` chokepoint; `ReadSpec` routes through it; `ReadOpts` gains `NumberHandling` | Mod |
| `src/ModForge.Cli/Program.Build.cs` | `ValidateCmd` resolves before `CheckUnknownFields` + `Deserialize` | Mod |
| `examples/presets/bright-interior.json` | Real preset (LGTM + IMGS) — the "library" file | **New** |
| `examples/spec-refs-demo.json` | Demo spec that `$ref`s the preset and uses `$env` | **New** |
| `docs/SPEC-refs.md` + `docs/SPEC-index.md` | `$ref`/`$env` reference + index link | New/Mod |
| `docs/CODE_MAP.infra.md` | SpecRefs + SpecRefsTests rows | Mod |
| `docs/lifelike/cookbook-presets.md` (+ `docs/zh-TW/...`) | Show `$ref` usage | Mod |
| `CLAUDE.md` | Strike the "抽成具名 preset 庫" TODO; add gotchas | Mod |
| `examples/spec.schema.json` | Best-effort note (directives resolve before deserialize) | Mod |

---

## Task 1: SpecRefs foundation — string `$ref`, pointers, sibling merge, recursion

**Files:**
- Create: `src/ModForge.Core/SpecRefs.cs`
- Test: `tests/ModForge.Core.Tests/SpecRefsTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/ModForge.Core.Tests/SpecRefsTests.cs`:

```csharp
using System.Collections.Generic;
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
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests"`
Expected: FAIL to **compile** — `SpecRefs` does not exist yet.

- [ ] **Step 3: Write the implementation**

Create `src/ModForge.Core/SpecRefs.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModForge;

/// <summary>Thrown when a $ref/$env directive cannot be resolved.</summary>
public sealed class SpecRefException : Exception
{
    public SpecRefException(string message) : base(message) { }
}

/// <summary>
/// Pre-deserialization JSON preprocessor for spec files. Resolves two directives anywhere in the tree:
///   $ref  — splice JSON from a file / file#pointer / same-document pointer. Value is a string,
///           an array (chained deep-merge, later wins), or a long-form { from, pointer } object.
///           Sibling keys deep-merge over the ref result (sibling wins).
///   $env  — substitute an environment variable's value; optional "default", else error.
/// Pure on JsonNode with injected file/env lookups so it is unit-testable without disk or environment.
/// </summary>
public static class SpecRefs
{
    public delegate string? FileReader(string path);  // null = not found
    public delegate string? EnvLookup(string name);   // null = unset

    // A resolution context: the current document root (for #/ pointers), its directory (for relative
    // file refs), and a stable id for that document (same-doc pointer + cycle keys).
    private sealed record Ctx(JsonNode? Root, string BaseDir, string DocId);

    /// <summary>Read a spec file from disk, resolve all directives, return resolved JSON text.</summary>
    public static string ResolveFile(string path)
    {
        var full = Path.GetFullPath(path);
        var root = JsonNode.Parse(File.ReadAllText(full));
        var resolved = Resolve(root, Path.GetDirectoryName(full) ?? ".", ReadOrNull, Environment.GetEnvironmentVariable, full);
        return resolved?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null";

        static string? ReadOrNull(string p) => File.Exists(p) ? File.ReadAllText(p) : null;
    }

    /// <summary>Engine entry. <paramref name="docId"/> identifies the root document for same-doc pointers + cycle keys.</summary>
    public static JsonNode? Resolve(JsonNode? root, string baseDir, FileReader readFile, EnvLookup getEnv, string docId = "<spec>")
        => ResolveNode(root, new Ctx(root, baseDir, docId), readFile, getEnv, new List<string>());

    private static JsonNode? ResolveNode(JsonNode? node, Ctx ctx, FileReader readFile, EnvLookup getEnv, List<string> cycle)
    {
        switch (node)
        {
            case JsonObject obj when obj.ContainsKey("$ref"):
                return ResolveRef(obj, ctx, readFile, getEnv, cycle);
            case JsonObject obj:
            {
                var result = new JsonObject();
                foreach (var kv in obj)
                    result[kv.Key] = ResolveNode(kv.Value, ctx, readFile, getEnv, cycle);
                return result;
            }
            case JsonArray arr:
            {
                var result = new JsonArray();
                foreach (var item in arr)
                    result.Add(ResolveNode(item, ctx, readFile, getEnv, cycle));
                return result;
            }
            default:
                return node?.DeepClone();
        }
    }

    private static JsonNode? ResolveRef(JsonObject obj, Ctx ctx, FileReader readFile, EnvLookup getEnv, List<string> cycle)
    {
        JsonNode? merged = null;
        foreach (var src in ParseSources(obj["$ref"], ctx, readFile, getEnv, cycle))
        {
            var loaded = LoadSource(src, ctx, readFile, getEnv, cycle);
            merged = merged is null ? loaded : DeepMerge(merged, loaded);
        }

        // siblings (everything except $ref) deep-merge on top — sibling wins
        var siblings = new JsonObject();
        foreach (var kv in obj)
            if (kv.Key != "$ref")
                siblings[kv.Key] = ResolveNode(kv.Value, ctx, readFile, getEnv, cycle);
        if (siblings.Count > 0)
            merged = merged is null ? siblings : DeepMerge(merged, siblings);

        return merged;
    }

    private readonly record struct Source(string File, string Pointer);

    private static List<Source> ParseSources(JsonNode? refVal, Ctx ctx, FileReader readFile, EnvLookup getEnv, List<string> cycle)
    {
        if (refVal is JsonValue v && v.TryGetValue<string>(out var s))
            return new() { SplitRef(s) };
        throw new SpecRefException("$ref value must be a string, array, or { from, pointer } object");
    }

    private static Source SplitRef(string s)
    {
        var hash = s.IndexOf('#');
        return hash < 0 ? new Source(s, "") : new Source(s[..hash], s[(hash + 1)..]);
    }

    private static JsonNode? LoadSource(Source src, Ctx ctx, FileReader readFile, EnvLookup getEnv, List<string> cycle)
    {
        JsonNode? root; Ctx targetCtx;
        if (string.IsNullOrEmpty(src.File))
        {
            root = ctx.Root; targetCtx = ctx;
        }
        else
        {
            var path = Path.IsPathRooted(src.File) ? src.File : Path.Combine(ctx.BaseDir, src.File);
            var text = readFile(path) ?? throw new SpecRefException($"$ref file not found: {path}");
            root = JsonNode.Parse(text);
            targetCtx = new Ctx(root, Path.GetDirectoryName(path) ?? "", path);
        }

        var target = Pointer(root, src.Pointer)
            ?? throw new SpecRefException($"$ref pointer not found: {(string.IsNullOrEmpty(src.File) ? ctx.DocId : src.File)}#{src.Pointer}");

        return ResolveNode(target, targetCtx, readFile, getEnv, cycle);
    }

    private static JsonNode? Pointer(JsonNode? root, string pointer)
    {
        if (string.IsNullOrEmpty(pointer)) return root;
        JsonNode? cur = root;
        foreach (var raw in pointer.Split('/'))
        {
            if (raw.Length == 0) continue; // leading empty segment from "/a"
            var token = raw.Replace("~1", "/").Replace("~0", "~"); // RFC 6901 unescape
            switch (cur)
            {
                case JsonObject o when o.TryGetPropertyValue(token, out var next): cur = next; break;
                case JsonArray a when int.TryParse(token, out var i) && i >= 0 && i < a.Count: cur = a[i]; break;
                default: return null;
            }
        }
        return cur;
    }

    private static JsonNode? DeepMerge(JsonNode? baseNode, JsonNode? over)
    {
        if (baseNode is JsonObject b && over is JsonObject o)
        {
            var result = (JsonObject)b.DeepClone();
            foreach (var kv in o)
            {
                if (result.TryGetPropertyValue(kv.Key, out var ex) && ex is JsonObject && kv.Value is JsonObject)
                    result[kv.Key] = DeepMerge(ex, kv.Value);
                else
                    result[kv.Key] = kv.Value?.DeepClone();
            }
            return result;
        }
        return over?.DeepClone() ?? baseNode?.DeepClone();
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests"`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/SpecRefs.cs tests/ModForge.Core.Tests/SpecRefsTests.cs
git commit -m "feat(spec): SpecRefs resolver — string \$ref, pointers, sibling deep-merge, recursion" \
  -m "Pure JsonNode preprocessor with injected file/env lookups. Handles same-doc #/pointer, external file, file#pointer; sibling keys deep-merge over the ref result (sibling wins); nested refs resolve recursively." \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Array-form `$ref` (chained deep-merge, later wins)

**Files:**
- Modify: `src/ModForge.Core/SpecRefs.cs` (replace `ParseSources`)
- Test: `tests/ModForge.Core.Tests/SpecRefsTests.cs` (add)

- [ ] **Step 1: Write the failing test**

Add to `SpecRefsTests`:

```csharp
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
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests.ArrayRef_ChainedDeepMerge_LaterWins"`
Expected: FAIL — `SpecRefException: $ref value must be a string, array, or { from, pointer } object`.

- [ ] **Step 3: Replace `ParseSources` to handle arrays**

In `src/ModForge.Core/SpecRefs.cs`, replace the whole `ParseSources` method with:

```csharp
    private static List<Source> ParseSources(JsonNode? refVal, Ctx ctx, FileReader readFile, EnvLookup getEnv, List<string> cycle)
    {
        switch (refVal)
        {
            case JsonValue v when v.TryGetValue<string>(out var s):
                return new() { SplitRef(s) };
            case JsonArray arr:
            {
                var list = new List<Source>();
                foreach (var item in arr)
                    list.AddRange(ParseSources(item, ctx, readFile, getEnv, cycle));
                return list;
            }
            default:
                throw new SpecRefException("$ref value must be a string, array, or { from, pointer } object");
        }
    }
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests"`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/SpecRefs.cs tests/ModForge.Core.Tests/SpecRefsTests.cs
git commit -m "feat(spec): array-form \$ref — chained deep-merge, later overrides earlier" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Long-form object `$ref` (`{ from, pointer }`)

**Files:**
- Modify: `src/ModForge.Core/SpecRefs.cs` (replace `ParseSources`)
- Test: `tests/ModForge.Core.Tests/SpecRefsTests.cs` (add)

- [ ] **Step 1: Write the failing tests**

Add to `SpecRefsTests`:

```csharp
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
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests.LongFormRef"`
Expected: FAIL — `LongFormRef_FromPlusPointer_Resolves` throws "$ref value must be a string, array, or { from, pointer } object".

- [ ] **Step 3: Replace `ParseSources` to handle the long-form object**

In `src/ModForge.Core/SpecRefs.cs`, replace the whole `ParseSources` method with:

```csharp
    private static List<Source> ParseSources(JsonNode? refVal, Ctx ctx, FileReader readFile, EnvLookup getEnv, List<string> cycle)
    {
        switch (refVal)
        {
            case JsonValue v when v.TryGetValue<string>(out var s):
                return new() { SplitRef(s) };
            case JsonArray arr:
            {
                var list = new List<Source>();
                foreach (var item in arr)
                    list.AddRange(ParseSources(item, ctx, readFile, getEnv, cycle));
                return list;
            }
            case JsonObject o:
            {
                foreach (var kv in o)
                    if (kv.Key is not ("from" or "pointer" or "merge"))
                        throw new SpecRefException($"unknown key '{kv.Key}' in long-form $ref (allowed: from, pointer, merge)");
                // 'from' is itself resolvable so it may be driven by $env.
                var fromNode = ResolveNode(o["from"], ctx, readFile, getEnv, cycle);
                if (fromNode is not JsonValue fv || !fv.TryGetValue<string>(out var from))
                    throw new SpecRefException("long-form $ref requires a string 'from'");
                var baseSrc = SplitRef(from);
                var ptr = o["pointer"] is JsonValue pv && pv.TryGetValue<string>(out var p) ? p : baseSrc.Pointer;
                return new() { new Source(baseSrc.File, ptr) };
            }
            default:
                throw new SpecRefException("$ref value must be a string, array, or { from, pointer } object");
        }
    }
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests"`
Expected: PASS (8 tests).

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/SpecRefs.cs tests/ModForge.Core.Tests/SpecRefsTests.cs
git commit -m "feat(spec): long-form object \$ref — { from, pointer }, from is resolvable, unknown keys rejected" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: `$env` directive (value / default / error) + `$ref`+`$env` conflict

**Files:**
- Modify: `src/ModForge.Core/SpecRefs.cs` (add switch cases + `ResolveEnv`)
- Test: `tests/ModForge.Core.Tests/SpecRefsTests.cs` (add)

- [ ] **Step 1: Write the failing tests**

Add to `SpecRefsTests`:

```csharp
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
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests.Env"`
Expected: FAIL — `Env_Present_SubstitutesValue` returns the object `{ "$env": "MF_DIR" }` (unresolved) instead of `"presets"`, so the `GetValue<string>()` throws / asserts.

- [ ] **Step 3: Add the `$env` and conflict cases**

In `src/ModForge.Core/SpecRefs.cs`, in `ResolveNode`, replace the `$ref` case with these three cases (order matters — conflict first):

```csharp
            case JsonObject obj when obj.ContainsKey("$ref") && obj.ContainsKey("$env"):
                throw new SpecRefException("a node may not contain both $ref and $env");
            case JsonObject obj when obj.ContainsKey("$ref"):
                return ResolveRef(obj, ctx, readFile, getEnv, cycle);
            case JsonObject obj when obj.ContainsKey("$env"):
                return ResolveEnv(obj, ctx, readFile, getEnv, cycle);
```

Then add the `ResolveEnv` method (next to `ResolveRef`):

```csharp
    private static JsonNode? ResolveEnv(JsonObject obj, Ctx ctx, FileReader readFile, EnvLookup getEnv, List<string> cycle)
    {
        var name = obj["$env"] is JsonValue v && v.TryGetValue<string>(out var n)
            ? n : throw new SpecRefException("$env requires a string variable name");
        var val = getEnv(name);
        if (val != null) return JsonValue.Create(val);                 // env value as a JSON string
        if (obj.TryGetPropertyValue("default", out var def))           // default may itself contain $ref/$env
            return ResolveNode(def, ctx, readFile, getEnv, cycle);
        throw new SpecRefException($"$env '{name}' is not set and no default was provided");
    }
```

- [ ] **Step 4: Run to verify they pass**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests"`
Expected: PASS (13 tests).

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/SpecRefs.cs tests/ModForge.Core.Tests/SpecRefsTests.cs
git commit -m "feat(spec): \$env directive — value / default / error; reject \$ref+\$env on one node" \
  -m "Env value inserted as a JSON string; default may carry further directives; \$env can drive a long-form \$ref's 'from'." \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Cycle detection

**Files:**
- Modify: `src/ModForge.Core/SpecRefs.cs` (`LoadSource` cycle guard)
- Test: `tests/ModForge.Core.Tests/SpecRefsTests.cs` (add)

- [ ] **Step 1: Write the failing test**

Add to `SpecRefsTests`:

```csharp
[Fact]
public void SelfReferentialRef_Throws()
{
    var json = """{ "a": { "$ref": "#/b" }, "b": { "$ref": "#/a" } }""";
    var ex = Assert.Throws<SpecRefException>(() => Resolve(json));
    Assert.Contains("cycle", ex.Message);
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests.SelfReferentialRef_Throws"`
Expected: FAIL — `StackOverflowException` / test host crash or hang (infinite recursion), not a `SpecRefException`.

- [ ] **Step 3: Add the cycle guard**

In `src/ModForge.Core/SpecRefs.cs`, replace the whole `LoadSource` method with:

```csharp
    private static JsonNode? LoadSource(Source src, Ctx ctx, FileReader readFile, EnvLookup getEnv, List<string> cycle)
    {
        JsonNode? root; Ctx targetCtx;
        if (string.IsNullOrEmpty(src.File))
        {
            root = ctx.Root; targetCtx = ctx;
        }
        else
        {
            var path = Path.IsPathRooted(src.File) ? src.File : Path.Combine(ctx.BaseDir, src.File);
            var text = readFile(path) ?? throw new SpecRefException($"$ref file not found: {path}");
            root = JsonNode.Parse(text);
            targetCtx = new Ctx(root, Path.GetDirectoryName(path) ?? "", path);
        }

        var target = Pointer(root, src.Pointer)
            ?? throw new SpecRefException($"$ref pointer not found: {(string.IsNullOrEmpty(src.File) ? ctx.DocId : src.File)}#{src.Pointer}");

        var key = $"{targetCtx.DocId}#{src.Pointer}";
        if (cycle.Contains(key))
            throw new SpecRefException($"$ref cycle: {string.Join(" -> ", cycle)} -> {key}");
        cycle.Add(key);
        try { return ResolveNode(target, targetCtx, readFile, getEnv, cycle); }
        finally { cycle.RemoveAt(cycle.Count - 1); }
    }
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests"`
Expected: PASS (14 tests).

- [ ] **Step 5: Commit**

```bash
git add src/ModForge.Core/SpecRefs.cs tests/ModForge.Core.Tests/SpecRefsTests.cs
git commit -m "feat(spec): \$ref cycle detection — (doc#pointer) stack throws on re-entry" \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: Disk round-trip test + CLI wiring

**Files:**
- Test: `tests/ModForge.Core.Tests/SpecRefsTests.cs` (add a `ResolveFile` temp-dir test)
- Modify: `src/ModForge.Cli/Program.cs` (`ReadOpts`, `ResolveSpecJson`, `ReadSpec`)
- Modify: `src/ModForge.Cli/Program.Build.cs` (`ValidateCmd`)

- [ ] **Step 1: Write the failing disk round-trip test**

Add to `SpecRefsTests` (add `using System.IO;` at the top of the file if not present):

```csharp
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
```

(Also add `using System;` for `Guid` if not already imported.)

- [ ] **Step 2: Run to verify it passes already**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "FullyQualifiedName~SpecRefsTests.ResolveFile_RealTempFiles_RoundTrips"`
Expected: PASS — `ResolveFile` was implemented in Task 1; this test pins the disk path. (If it fails to compile for a missing `using`, add it and re-run.)

- [ ] **Step 3: Wire the CLI chokepoint**

In `src/ModForge.Cli/Program.cs`, replace the `ReadOpts` field and `ReadSpec` method (around lines 124-128) with:

```csharp
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        // $env values arrive as JSON strings; allow them in numeric spec fields.
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
    };

    // Single chokepoint: read a spec file and resolve $ref/$env before any deserialize / field check.
    private static string ResolveSpecJson(string path) => SpecRefs.ResolveFile(path);

    private static ModSpec ReadSpec(string path) =>
        JsonSerializer.Deserialize<ModSpec>(ResolveSpecJson(path), ReadOpts)
        ?? throw new InvalidOperationException("spec deserialized to null");
```

- [ ] **Step 4: Route `ValidateCmd` through the chokepoint**

In `src/ModForge.Cli/Program.Build.cs`, replace the first two lines of `ValidateCmd` (the `File.ReadAllText` + `CheckUnknownFields`, around lines 61-62) so both the unknown-field check and the deserialize run on the **resolved** JSON:

```csharp
        var json = ResolveSpecJson(specPath);
        var unknowns = CheckUnknownFields(json, typeof(ModSpec));

        var spec = JsonSerializer.Deserialize<ModSpec>(json, ReadOpts)
            ?? throw new InvalidOperationException("spec deserialized to null");
```

- [ ] **Step 5: Build and run the offline regression**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"`
Expected: PASS — all offline tests green, including the new `SpecRefsTests`. (This also proves `ModForge.Cli` still compiles, since the test build references Core; build the CLI explicitly if unsure: `dotnet build src/ModForge.Cli/ModForge.Cli.csproj`.)

- [ ] **Step 6: Sanity-check an existing example still validates**

Run: `dotnet run --project src/ModForge.Cli -- validate examples/sample_spec.json`
Expected: `valid: sample_spec.json — no problems` (a spec with no `$ref`/`$env` is unchanged by resolution).

- [ ] **Step 7: Commit**

```bash
git add tests/ModForge.Core.Tests/SpecRefsTests.cs src/ModForge.Cli/Program.cs src/ModForge.Cli/Program.Build.cs
git commit -m "feat(cli): resolve \$ref/\$env before deserialize via ResolveSpecJson chokepoint" \
  -m "ReadSpec + ValidateCmd route through SpecRefs.ResolveFile; unknown-field check runs on resolved JSON; ReadOpts allows reading numbers from strings (for \$env)." \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Named preset library file + demo spec

**Files:**
- Create: `examples/presets/bright-interior.json`
- Create: `examples/spec-refs-demo.json`

- [ ] **Step 1: Create the preset library file**

The proven bright-interior values come from `examples/lighting.json` and the design doc. Create `examples/presets/bright-interior.json` (a same-shape fragment with full LGTM + IMGS records, ready to splice under the top-level arrays):

```json
{
  "_": "Named lighting preset: clean readable bright interior. $ref this file into lightingTemplates[]/imageSpaces[] (array form) or pull sub-nodes via #/lgtm and #/imgs.",
  "lgtm": {
    "editorId": "MF_BrightInteriorLGTM",
    "template": "Skyrim.esm:0x0300E2",
    "ambientColor": { "r": 160, "g": 165, "b": 176 },
    "directionalColor": { "r": 220, "g": 220, "b": 210 },
    "fogNearColor": { "r": 165, "g": 170, "b": 180 },
    "fogFarColor": { "r": 120, "g": 130, "b": 145 },
    "fogNear": 0,
    "fogFar": 9000,
    "fogMax": 0.55,
    "directionalAmbient": {
      "scale": 1.0,
      "xPlus": { "r": 175, "g": 178, "b": 184 },
      "xMinus": { "r": 175, "g": 178, "b": 184 },
      "yPlus": { "r": 175, "g": 178, "b": 184 },
      "yMinus": { "r": 175, "g": 178, "b": 184 },
      "zPlus": { "r": 190, "g": 192, "b": 196 },
      "zMinus": { "r": 120, "g": 122, "b": 128 }
    }
  },
  "imgs": {
    "editorId": "MF_BrightInteriorIMGS",
    "template": "Skyrim.esm:0x06DD55",
    "hdrEyeAdaptSpeed": 0.2,
    "hdrBloomThreshold": 0.85,
    "hdrBloomScale": 0.7,
    "brightness": 1.15,
    "contrast": 1.0,
    "saturation": 1.0
  }
}
```

> NOTE: verify the field names against the current `LightingTemplateSpec` / `ImageSpaceSpec` in `src/ModForge.Core/Spec.Lighting.cs` and the values against `examples/lighting.json` before trusting them — copy the exact field names/values that file uses (the design lists representative values; the example must match the live spec schema). Adjust this JSON to match.

- [ ] **Step 2: Create the demo spec that consumes it**

Create `examples/spec-refs-demo.json`. It pulls the two records in via `$ref` long-form (so `$env` can redirect the preset dir) and attaches them to a vanilla interior cell:

```json
{
  "pluginName": "ModForgeSpecRefsDemo.esp",
  "esl": true,
  "_": "Demo of $ref/$env: lighting records come from examples/presets/bright-interior.json; MF_PRESET_DIR can redirect the preset folder.",

  "lightingTemplates": [
    { "$ref": { "from": { "$env": "MF_PRESET_DIR", "default": "presets" }, "pointer": "/bright-interior.json#/lgtm" } }
  ],
  "imageSpaces": [
    { "$ref": { "from": { "$env": "MF_PRESET_DIR", "default": "presets" }, "pointer": "/bright-interior.json#/imgs" } }
  ],

  "cells": [
    {
      "editorId": "RiverwoodSleepingGiantInn",
      "vanilla": "Skyrim.esm:0x0133C6",
      "lightingTemplate": "MF_BrightInteriorLGTM",
      "imageSpace": "MF_BrightInteriorIMGS"
    }
  ]
}
```

> NOTE: `pointer` here is `/bright-interior.json#/lgtm` — but a JSON Pointer cannot contain a file segment. Correct the form: put the **file** in `from` and the **sub-node** in `pointer`. Use:
> `{ "from": { "$env": "MF_PRESET_DIR", "default": "presets" }, "pointer": "/lgtm" }` will resolve a pointer against `presets` (a directory, not a file) — wrong.
> The right construction is a plain string ref so the file + pointer split is unambiguous: replace each `$ref` with
> `{ "$ref": "presets/bright-interior.json#/lgtm" }` (and `#/imgs`). If `$env`-driven dir is wanted, use long form with `from` = the **full file path** and `pointer` = the sub-node:
> `{ "$ref": { "from": "presets/bright-interior.json", "pointer": "/lgtm" } }`.
> Pick the plain-string form for the committed example (simplest, clearly correct); demonstrate `$env` separately on a scalar field (e.g. a comment or a path) so the example does not mislead. Verify `cells[].vanilla` is the correct field name for "attach to an existing vanilla cell" against `src/ModForge.Core/Spec.World.cs` — if the field is named differently (e.g. `vanillaCell`/`base`), use the real name.

- [ ] **Step 3: Validate and build the demo**

Run: `dotnet run --project src/ModForge.Cli -- validate examples/spec-refs-demo.json`
Expected: `valid: spec-refs-demo.json — no problems`.

Run: `dotnet run --project src/ModForge.Cli -- build examples/spec-refs-demo.json /tmp/specrefsdemo.esp`
Expected: build summary printed; the LGTM + IMGS records appear in the counts (no errors).

- [ ] **Step 4: Confirm `$env` redirection works**

Run: `MF_PRESET_DIR=presets dotnet run --project src/ModForge.Cli -- validate examples/spec-refs-demo.json`
Expected: still `valid` (only relevant if the example uses the `$env` long-form; if Step 2 settled on the plain-string form, skip this and instead confirm a deliberately-wrong `MF_PRESET_DIR=nope` makes `build` fail with a clear `$ref file not found` — proving the env actually drives the path).

- [ ] **Step 5: Commit**

```bash
git add examples/presets/bright-interior.json examples/spec-refs-demo.json
git commit -m "examples(spec): named lighting preset file + \$ref/\$env demo spec" \
  -m "examples/presets/bright-interior.json is the first library preset; spec-refs-demo.json pulls its LGTM/IMGS in via \$ref and attaches them to a vanilla interior." \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: Documentation, CODE_MAP, CLAUDE.md, schema note

**Files:**
- Create: `docs/SPEC-refs.md`
- Modify: `docs/SPEC-index.md`, `docs/CODE_MAP.infra.md`, `docs/lifelike/cookbook-presets.md`, `docs/zh-TW/lifelike/cookbook-presets.md`, `CLAUDE.md`, `examples/spec.schema.json`

- [ ] **Step 1: Write `docs/SPEC-refs.md`**

Create `docs/SPEC-refs.md` documenting both directives. Mirror the language style of the sibling `SPEC-*.md` files. Cover, with a short example for each:
- `$ref` string form (file / `file#/pointer` / `#/pointer`), paths relative to the referring document.
- `$ref` array form (chained deep-merge, later wins).
- `$ref` long-form object (`{ from, pointer }`, `from` resolvable / may be `$env`; `merge` reserved).
- Sibling deep-merge over the ref result (sibling wins); object-merge vs array-replace rule.
- `$env` (value / `default` / error-if-missing); env value arrives as a string (numeric fields OK via `AllowReadingFromString`).
- Errors: file/pointer not found, cycle, `$ref`+`$env` on one node, `$env` unset with no default.
- Note: directives resolve **before** deserialization, so they may appear anywhere and never reach `ModSpec`.

Add a link to it from `docs/SPEC-index.md` (follow the existing list format there — read the file first and match the bullet/table style).

- [ ] **Step 2: Update `docs/CODE_MAP.infra.md`**

Read `docs/CODE_MAP.infra.md`, find the CLI / plugin-I/O section, and add rows:
- `src/ModForge.Core/SpecRefs.cs` — "$ref/$env spec preprocessor (JsonNode resolver; ResolveFile disk entry); run by the CLI before deserialize".
- In the Tests subsection: `SpecRefsTests.cs` — "$ref forms (string/array/long-form), $env (value/default/error), cycle, sibling merge, ResolveFile disk round-trip".
- Note on `Program.cs` `ResolveSpecJson` / `ReadSpec` + `Program.Build.cs` `ValidateCmd` running on resolved JSON.

Match the existing table columns/format in that file exactly.

- [ ] **Step 3: Update the cookbook-presets docs**

In `docs/lifelike/cookbook-presets.md`, add a short section explaining that presets can now live in separate files and be pulled in with `$ref` (string/array/long-form) with sibling overrides, and that `$env` parameterizes paths/values. Point to `examples/presets/bright-interior.json` + `examples/spec-refs-demo.json` and `docs/SPEC-refs.md`. Make the matching edit in `docs/zh-TW/lifelike/cookbook-presets.md` in Chinese.

- [ ] **Step 4: Update `CLAUDE.md`**

In the lighting "未做" note, strike "明亮 LGTM/IMGS 抽成具名 preset 庫" (now done via `$ref` preset files; keep "weather/IMGS 掛 region" as still-open). Add a one-line entry under "已落地功能" for the `$ref`/`$env` spec resolution layer, and a gotchas bullet capturing:
- `$ref` paths are relative to the **referring** document (a preset file's own `$ref`s resolve relative to the preset file's dir).
- siblings deep-merge over the ref (sibling wins); arrays replace, objects merge.
- `$env` unset + no `default` is a hard error (by design — no silent empties).
- directives resolve before deserialize, so `spec.schema.json` cannot enforce them.

- [ ] **Step 5: Add the schema note**

In `examples/spec.schema.json`, add a top-level `"$comment"` (or extend an existing description) stating that `$ref` and `$env` are ModForge preprocessor directives resolved before schema-relevant deserialization, so they may appear in place of any value and are not validated by this schema. Do not attempt to encode them structurally.

- [ ] **Step 6: Final offline regression**

Run: `dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"`
Expected: PASS (all offline tests, including `SpecRefsTests`).

- [ ] **Step 7: Commit**

```bash
git add docs/SPEC-refs.md docs/SPEC-index.md docs/CODE_MAP.infra.md \
  docs/lifelike/cookbook-presets.md docs/zh-TW/lifelike/cookbook-presets.md \
  CLAUDE.md examples/spec.schema.json
git commit -m "docs(spec): document \$ref/\$env resolution layer; close named-preset-library TODO" \
  -m "New SPEC-refs.md; CODE_MAP.infra rows for SpecRefs + SpecRefsTests; cookbook-presets (EN+zh) \$ref usage; CLAUDE.md strike + gotchas; spec.schema.json preprocessor note." \
  -m "Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage:**
- `$ref` string (file / file#pointer / same-doc) → Task 1. ✓
- `$ref` array chained merge → Task 2. ✓
- `$ref` long-form object → Task 3. ✓
- Sibling deep-merge, sibling wins; object-merge / array-replace → Task 1 (test + `DeepMerge`). ✓
- `$env` value / default / error → Task 4. ✓
- `$ref`+`$env` conflict error → Task 4. ✓
- Recursion → Task 1 (nested-ref test); cycle detection → Task 5. ✓
- Pre-deserialize pipeline / chokepoint / unknown-field on resolved JSON / `NumberHandling` → Task 6. ✓
- Relationship to `presets{}` (unchanged, becomes same-doc target) → no code change needed; documented Task 8. ✓
- Named preset library file + demo → Task 7. ✓
- Docs / CODE_MAP / CLAUDE.md / schema note → Task 8. ✓

**Placeholder scan:** Tasks 7 Step 2 contains explicit NOTEs that require verifying live field names (`cells[].vanilla`, `LightingTemplateSpec`/`ImageSpaceSpec` fields) and correcting the example before committing — these are verification instructions, not unfilled placeholders; the executor must read the named source files and adjust the JSON to match. The `$ref` long-form-with-file-pointer pitfall is called out so the executor uses the unambiguous string form.

**Type consistency:** `SpecRefs.Resolve(JsonNode?, string, FileReader, EnvLookup, string=)`, `SpecRefs.ResolveFile(string)`, delegates `FileReader`/`EnvLookup`, and `SpecRefException` are used consistently across Tasks 1-6. `ResolveSpecJson` (CLI) wraps `ResolveFile`. `ParseSources` is replaced wholesale in Tasks 2 and 3 (each shows the full method). `LoadSource` is replaced wholesale in Task 5.
