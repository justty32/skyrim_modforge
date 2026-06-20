using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModForge;

// Generates the MCM Helper config tree as loose files:
//   MCM/Config/<identity>/config.json    (the menu layout)
//   MCM/Config/<identity>/settings.ini   (default values for every ModSetting* control)
// `identity` is the host plugin's filename stem (e.g. MyMod.esp -> "MyMod"). MCM Helper's DLL keys
// the config folder on FormUtil::GetModName(quest) = path(plugin).stem() (src/ConfigStore.cpp ->
// FormUtil.cpp:55), so the folder name and the config.json `modName` field (a self plugin-requirement)
// MUST be the plugin stem — the Papyrus `ModName` property is NOT consulted for the folder (only an
// error-page display fallback). Using the spec's modName here makes MCM Helper look in the wrong folder
// ("Failed to open file: MCM/Config/<plugin>/config.json" → "check json syntax"), confirmed in-game
// 2026-06-20. The spec's modName becomes the displayName fallback. Pure functions (no I/O) — `package`
// writes the OarFiles. Format verified against sub_projs/mod-survey/findings/mcm-helper-config-json.md.
public static class McmGen
{
    private static readonly JsonSerializerOptions Pretty =
        new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // Control types that store a value (need id + sourceType, get a settings.ini line).
    private static readonly HashSet<string> ValueTypes = new(StringComparer.OrdinalIgnoreCase)
    { "toggle", "hiddenToggle", "slider", "stepper", "enum", "keymap" };

    public static List<OarGen.OarFile> Generate(McmSpec m, string identity)
    {
        var dir = $"MCM/Config/{identity}";
        return new List<OarGen.OarFile>
        {
            new($"{dir}/config.json", BuildConfigJson(m, identity)),
            new($"{dir}/settings.ini", BuildSettingsIni(m)),
        };
    }

    public static string BuildConfigJson(McmSpec m, string identity)
    {
        // `modName` is the folder/self-plugin-requirement (the plugin stem); `displayName` is the menu
        // label (falls back to the spec's modName, then the identity). Both are schema-required.
        var root = new JsonObject { ["modName"] = identity };
        root["displayName"] = !string.IsNullOrEmpty(m.DisplayName) ? m.DisplayName
            : !string.IsNullOrEmpty(m.ModName) ? m.ModName : identity;

        var pages = new JsonArray();
        foreach (var p in m.Pages)
        {
            var page = new JsonObject { ["pageDisplayName"] = p.Name };
            if (!string.IsNullOrWhiteSpace(p.CursorFillMode)) page["cursorFillMode"] = p.CursorFillMode;
            var content = new JsonArray();
            foreach (var c in p.Content) content.Add(BuildControl(c));
            page["content"] = content;
            pages.Add(page);
        }
        root["pages"] = pages;
        return root.ToJsonString(Pretty);
    }

    private static JsonObject BuildControl(McmControlSpec c)
    {
        var o = new JsonObject();
        if (!string.IsNullOrEmpty(c.Text)) o["text"] = c.Text;
        if (!string.IsNullOrEmpty(c.Help)) o["help"] = c.Help;
        o["type"] = c.Type;
        if (!string.IsNullOrEmpty(c.Id)) o["id"] = c.Id;

        var vo = BuildValueOptions(c);
        if (vo is not null) o["valueOptions"] = vo;

        if (c.GroupControl is int gc) o["groupControl"] = gc;
        if (c.GroupCondition is int gd)
            o["groupCondition"] = c.GroupConditionNot ? new JsonObject { ["NOT"] = gd } : JsonValue.Create(gd);
        if (!string.IsNullOrWhiteSpace(c.GroupBehavior)) o["groupBehavior"] = c.GroupBehavior;
        if (c.Position is int pos) o["position"] = pos;
        return o;
    }

    private static JsonObject? BuildValueOptions(McmControlSpec c)
    {
        bool hasValue = IsValueControl(c);
        bool any = hasValue || c.Min is not null || c.Max is not null || c.Step is not null
            || !string.IsNullOrEmpty(c.FormatString) || c.Options.Count > 0 || c.ShortNames.Count > 0;
        if (!any) return null;

        var vo = new JsonObject();
        if (!string.IsNullOrEmpty(c.SourceType)) vo["sourceType"] = c.SourceType;
        if (c.Min is double mn) vo["min"] = mn;
        if (c.Max is double mx) vo["max"] = mx;
        if (c.Step is double st) vo["step"] = st;
        if (!string.IsNullOrEmpty(c.FormatString)) vo["formatString"] = c.FormatString;
        if (c.Options.Count > 0) vo["options"] = ToArray(c.Options);
        if (c.ShortNames.Count > 0) vo["shortNames"] = ToArray(c.ShortNames);
        if (hasValue && DefaultNode(c) is { } dv) vo["defaultValue"] = dv;
        return vo;
    }

    // config.json defaultValue, typed by sourceType (bool/int/float/string).
    private static JsonNode? DefaultNode(McmControlSpec c) => (c.SourceType ?? "").ToLowerInvariant() switch
    {
        "modsettingbool"   => JsonValue.Create(c.DefaultBool),
        "modsettingint"    => JsonValue.Create((int)c.DefaultNumber),
        "modsettingfloat"  => JsonValue.Create(c.DefaultNumber),
        "modsettingstring" => JsonValue.Create(c.DefaultString),
        _ => null,
    };

    // settings.ini: one [Section] block per distinct section, key=default for every value control
    // whose id is "key:Section". Controls without a sourceType/id (header/empty) are skipped.
    public static string BuildSettingsIni(McmSpec m)
    {
        // Preserve first-seen section order; within a section, first-seen key order.
        var sections = new List<string>();
        var lines = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in m.Pages)
            foreach (var c in p.Content)
            {
                if (!IsValueControl(c)) continue;
                var (key, section) = SplitId(c.Id);
                if (key.Length == 0 || section.Length == 0) continue;   // malformed id → validate reports it
                if (!lines.ContainsKey(section)) { lines[section] = new(); sections.Add(section); }
                lines[section].Add($"{key}={IniValue(c)}");
            }

        var sb = new StringBuilder();
        foreach (var section in sections)
        {
            sb.Append('[').Append(section).Append("]\n");
            foreach (var l in lines[section]) sb.Append(l).Append('\n');
            sb.Append('\n');
        }
        return sb.ToString();
    }

    private static string IniValue(McmControlSpec c) => (c.SourceType ?? "").ToLowerInvariant() switch
    {
        "modsettingbool"   => c.DefaultBool ? "1" : "0",
        "modsettingint"    => ((int)c.DefaultNumber).ToString(CultureInfo.InvariantCulture),
        "modsettingfloat"  => c.DefaultNumber.ToString("0.0###", CultureInfo.InvariantCulture),
        "modsettingstring" => c.DefaultString,
        _ => "0",
    };

    // A control carries a value iff it's a value-type AND has a sourceType (header/empty never do).
    private static bool IsValueControl(McmControlSpec c) =>
        ValueTypes.Contains(c.Type ?? "") && !string.IsNullOrEmpty(c.SourceType);

    // "key:Section" → (key, section). Missing ':' → ("", "") so the caller skips/validate reports.
    public static (string key, string section) SplitId(string id)
    {
        if (string.IsNullOrEmpty(id)) return ("", "");
        int i = id.IndexOf(':');
        if (i <= 0 || i >= id.Length - 1) return ("", "");
        return (id[..i].Trim(), id[(i + 1)..].Trim());
    }

    private static JsonArray ToArray(List<string> items)
    {
        var a = new JsonArray();
        foreach (var s in items) a.Add(s);
        return a;
    }
}
