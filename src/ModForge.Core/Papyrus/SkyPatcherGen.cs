using System.Text;

namespace ModForge;

// Generates a SkyPatcher config as a loose file:
//   SKSE/Plugins/SkyPatcher/<recordType>/<File>.ini
// Pure function (no I/O) — package writes the OarFile. Format verified against SkyPatcher
// (sub_projs/mod-survey/findings/skypatcher-records-and-config.md): flat lines, no [section] headers,
// "filterKey=value:...:modKey=value:..." — filters (AND) first, then modifications.
public static class SkyPatcherGen
{
    public static OarGen.OarFile Generate(SkyPatcherSpec s)
    {
        var sb = new StringBuilder();
        foreach (var p in s.Patches)
        {
            var line = Line(p);
            if (line.Length > 0) sb.Append(line).Append('\n');
        }
        var rt = (s.RecordType ?? "").Trim();
        return new OarGen.OarFile($"SKSE/Plugins/SkyPatcher/{rt}/{s.File}.ini", sb.ToString());
    }

    // "filterK=v:filterK=v:modK=v:modK=v" — colon-delimited, no spaces.
    public static string Line(SkyPatcherLineSpec p)
    {
        var parts = new List<string>();
        foreach (var f in p.Filters)
            if (!string.IsNullOrWhiteSpace(f.Key)) parts.Add($"{f.Key.Trim()}={f.Value?.Trim()}");
        foreach (var m in p.Mods)
            if (!string.IsNullOrWhiteSpace(m.Key)) parts.Add($"{m.Key.Trim()}={m.Value?.Trim()}");
        return string.Join(':', parts);
    }
}
