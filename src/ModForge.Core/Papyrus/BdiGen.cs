using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ModForge;

// Generates loose config files for the two companion injectors:
//   BDI  → SKSE/Plugins/BehaviorDataInjector/<File>.json   (flat JSON array of graph vars/events)
//   PIE  → SKSE/PayloadInterpreter/Config/<File>.ini        (named macro table)
// Pure functions (no I/O). Schemas verified against DMK/BFCO (BDI) and Stormcloaks (PIE).
public static class BdiGen
{
    private static readonly JsonSerializerOptions Pretty =
        new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static OarGen.OarFile Generate(BehaviorDataSpec b)
    {
        var arr = new JsonArray();
        foreach (var e in b.Entries)
        {
            var o = new JsonObject
            {
                ["projectPath"] = string.IsNullOrEmpty(e.ProjectPath) ? "Actors" : e.ProjectPath,
                ["type"] = e.Type,
                ["name"] = e.Name,
            };
            // kEvent carries no value (verified against real BDI configs).
            if (!string.Equals(e.Type, "kEvent", StringComparison.OrdinalIgnoreCase))
                o["value"] = e.Value;
            arr.Add(o);
        }
        return new OarGen.OarFile($"SKSE/Plugins/BehaviorDataInjector/{b.File}.json", arr.ToJsonString(Pretty));
    }
}

// PIE macro table (companion; small). Emits the .ini macro form $name = command under [Section].
public static class PieGen
{
    public static OarGen.OarFile Generate(PayloadMacroSpec m)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(m.Section)) sb.Append('[').Append(m.Section).Append(']').Append('\n');
        foreach (var macro in m.Macros)
        {
            var name = macro.Name.StartsWith('$') ? macro.Name : "$" + macro.Name;
            sb.Append(name).Append(" = ").Append(macro.Command).Append('\n');
        }
        return new OarGen.OarFile($"SKSE/PayloadInterpreter/Config/{m.File}.ini", sb.ToString());
    }
}
