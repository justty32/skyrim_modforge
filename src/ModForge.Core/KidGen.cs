using System.Globalization;
using System.Text;

namespace ModForge;

// Generates a KID distribution config as a loose file at the mod folder root:
//   <File>_KID.ini   (KID scans Data/ for every *_KID.ini at start-up)
// Pure function (no I/O) — package writes the OarFile. Line format verified against KID
// (sub_projs/mod-survey/findings/keyword-item-distributor-config-*.md):
//   Keyword = <keyword>|<type>|<strings_or_formIDs>|<traits>|<chance>
// Trailing NONE fields are trimmed; a middle gap is held open with NONE (same shape as SPID).
public static class KidGen
{
    public static OarGen.OarFile Generate(KidDistributionSpec s)
    {
        var sb = new StringBuilder();
        foreach (var e in s.Entries)
        {
            var line = Line(e);
            if (line.Length > 0) sb.Append(line).Append('\n');
        }
        return new OarGen.OarFile($"{s.File}_KID.ini", sb.ToString());
    }

    // Build "Keyword = kw|type|filters|traits|chance" with trailing NONE fields trimmed.
    public static string Line(KidEntrySpec e)
    {
        var fields = new List<string>
        {
            e.Keyword,                                          // 1 keyword (never NONE)
            e.Type,                                             // 2 type
            Join(e.Filters),                                    // 3 strings/formIDs filter
            e.Traits ?? "",                                     // 4 traits
            e.Chance is double ch ? ch.ToString("0.###", CultureInfo.InvariantCulture) : "",  // 5 chance
        };

        for (int i = 0; i < fields.Count; i++)
            if (string.IsNullOrWhiteSpace(fields[i])) fields[i] = "NONE";
        int last = fields.Count - 1;
        while (last > 0 && fields[last] == "NONE") last--;

        return $"Keyword = {string.Join('|', fields.GetRange(0, last + 1))}";
    }

    private static string Join(List<string> items)
    {
        if (items is null || items.Count == 0) return "";
        return string.Join(',', items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
    }
}
