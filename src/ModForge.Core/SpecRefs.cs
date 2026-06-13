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
            case JsonObject obj when obj.ContainsKey("$ref") && obj.ContainsKey("$env"):
                throw new SpecRefException("a node may not contain both $ref and $env");
            case JsonObject obj when obj.ContainsKey("$ref"):
                return ResolveRef(obj, ctx, readFile, getEnv, cycle);
            case JsonObject obj when obj.ContainsKey("$env"):
                return ResolveEnv(obj, ctx, readFile, getEnv, cycle);
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

    private readonly record struct Source(string File, string Pointer);

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
