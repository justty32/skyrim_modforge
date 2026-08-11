using System.Text;

namespace ModForge;

public static partial class Generator
{
    public static IEnumerable<McmControlSpec> McmGlobalControls(McmSpec m) =>
        m.Pages.SelectMany(p => p.Content).Where(c => !string.IsNullOrWhiteSpace(c.Global));

    public static bool HasMcmGlobalBindings(McmSpec m) => McmGlobalControls(m).Any();

    public static string McmGlobalPropertyName(int index) => $"MF_Global_{index}";
    public static string McmGlobalSetterName(int index) => $"MF_SetGlobal_{index}";

    public static string McmGlobalScriptName(McmSpec m)
    {
        var safe = new string((m.ModName ?? "MCM").Select(c =>
            char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
        return $"MF_MCM_{safe}_Globals";
    }

    // MCM Helper invokes each setter through control.action.CallFunction. The GLOB itself persists in
    // the save; the ModSettingBool remains the player's durable MCM setting/default.
    public static string GenerateMcmGlobalScriptSource(McmSpec m)
    {
        var controls = McmGlobalControls(m).ToList();
        if (controls.Count == 0) return "";
        var sb = new StringBuilder();
        sb.Append("Scriptname ").Append(McmGlobalScriptName(m)).AppendLine(" extends MCM_ConfigBase").AppendLine();
        for (int i = 0; i < controls.Count; i++)
            sb.Append("GlobalVariable Property ").Append(McmGlobalPropertyName(i)).AppendLine(" Auto");
        sb.AppendLine();
        for (int i = 0; i < controls.Count; i++)
        {
            var prop = McmGlobalPropertyName(i);
            sb.Append("Function ").Append(McmGlobalSetterName(i)).AppendLine("(bool value)")
              .Append("    If ").AppendLine(prop)
              .AppendLine("        If value")
              .Append("            ").Append(prop).AppendLine(".SetValue(1.0)")
              .AppendLine("        Else")
              .Append("            ").Append(prop).AppendLine(".SetValue(0.0)")
              .AppendLine("        EndIf")
              .AppendLine("    EndIf")
              .AppendLine("EndFunction")
              .AppendLine();
        }
        return sb.ToString();
    }
}
