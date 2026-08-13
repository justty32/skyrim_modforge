using System.Text.Json.Serialization;

namespace ModForge;

// -------------------------------------------------------------------------------
//  requires[] — the DECLARED install requirements of this mod (Generator.Requires.cs checks them).
//
//  Generator.Dependencies.cs already RECORDS what the build actually needs. This is the other half:
//  what the author SAYS it needs. Build compares the two, both ways — because the real failure is
//  drift: a mod is removed / a capture is re-taken / a line is deleted, and the plugin silently
//  stops loading (Skyrim drops a plugin with a missing master without a word).
//
//  Two entry kinds, on purpose:
//   * `plugin` — a master the BUILD is expected to link ("PROTEUS.esp"). CHECKED both ways.
//   * `name`   — a requirement with no plugin of its own: an SKSE DLL (PapyrusUtil, JContainers),
//                a loose-file framework. It can never appear in the master list, so it is
//                DOCUMENTATION ONLY and is never checked — but it still belongs in the sidecar,
//                which is the requirements list a player reads.
//
//  `version` is DOCUMENTATION ONLY and ModForge will never verify it: a Skyrim plugin carries no
//  mod version. Its TES4/HEDR "version" is the FILE FORMAT version (1.70/1.71 for every SSE plugin
//  alike — PROTEUS 3.4 and a two-record test .esp are indistinguishable); CNAM/SNAM are free text
//  (mostly "DEFAULT"/empty/marketing prose). The only place a real version lives is the mod
//  manager's metadata (MO2 `meta.ini` version=, from Nexus), which is not in the plugin and not
//  visible to a build. See docs/for_agent_cli.md.
// -------------------------------------------------------------------------------
[JsonConverter(typeof(RequirementConverter))]
public sealed class RequirementSpec
{
    /// <summary>Master filename the build should link, e.g. <c>PROTEUS.esp</c>. Checked both ways.</summary>
    public string Plugin { get; set; } = "";

    /// <summary>A requirement with no plugin (SKSE DLL / loose files). Documentation only — never checked.</summary>
    public string Name { get; set; } = "";

    /// <summary>Documentation only — NOT verifiable (a plugin carries no mod version). Printed for humans.</summary>
    public string Version { get; set; } = "";

    /// <summary>Why this mod is needed ("the captured player's spells"). Auto-filled by <c>--sync-requires</c>.</summary>
    public string Reason { get; set; } = "";

    /// <summary>Where to get it (a Nexus URL) — goes into the sidecar the player reads.</summary>
    public string Url { get; set; } = "";

    /// <summary>What to call this in a message: the plugin if there is one, else the name.</summary>
    public string Label => string.IsNullOrWhiteSpace(Plugin) ? Name : Plugin;
}

/// <summary>
/// Accepts <c>"requires": ["PROTEUS.esp"]</c> (the 90% case) as well as the object form. A bare string
/// is always a <c>plugin</c> — a requirement with no plugin has nothing to check, so it must say so
/// explicitly with <c>{ "name": … }</c>.
/// </summary>
public sealed class RequirementConverter : JsonConverter<RequirementSpec>
{
    public override RequirementSpec? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.Null) return null;
        if (r.TokenType == JsonTokenType.String) return new RequirementSpec { Plugin = r.GetString() ?? "" };

        var inner = new JsonSerializerOptions(o);
        for (int i = inner.Converters.Count - 1; i >= 0; i--)
            if (inner.Converters[i] is RequirementConverter) inner.Converters.RemoveAt(i);
        using var doc = JsonDocument.ParseValue(ref r);
        return doc.RootElement.Deserialize<RequirementBody>(inner) is { } b
            ? new RequirementSpec { Plugin = b.Plugin, Name = b.Name, Version = b.Version, Reason = b.Reason, Url = b.Url }
            : null;
    }

    // Round-trips to the shorthand when there is nothing else to say (what --sync-requires writes back).
    public override void Write(Utf8JsonWriter w, RequirementSpec v, JsonSerializerOptions o)
    {
        if (v.Name.Length == 0 && v.Version.Length == 0 && v.Reason.Length == 0 && v.Url.Length == 0)
        {
            w.WriteStringValue(v.Plugin);
            return;
        }
        w.WriteStartObject();
        if (v.Plugin.Length > 0) w.WriteString("plugin", v.Plugin);
        if (v.Name.Length > 0) w.WriteString("name", v.Name);
        if (v.Version.Length > 0) w.WriteString("version", v.Version);
        if (v.Reason.Length > 0) w.WriteString("reason", v.Reason);
        if (v.Url.Length > 0) w.WriteString("url", v.Url);
        w.WriteEndObject();
    }

    // Plain mirror of RequirementSpec, free of the converter attribute (else Read recurses).
    private sealed class RequirementBody
    {
        public string Plugin { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
