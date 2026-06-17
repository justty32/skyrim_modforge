using System.Text;

namespace ModForge;

// Generates a SPID distribution config as a loose file at the mod folder root:
//   <File>_DISTR.ini   (SPID scans Data/ for every *_DISTR.ini at start-up)
// Pure function (no I/O) — package writes the OarFile. Line format + field semantics verified
// against the SPID 7.3 reference (sub_projs/mod-survey/findings/spid.md):
//   Type = RecordID|StringFilters|FormFilters|LevelFilters|Traits|TypeParam|Chance
// Trailing NONE fields are trimmed; an absent middle field is held open with NONE.
public static class SpidGen
{
    public static OarGen.OarFile Generate(SpidDistributionSpec s)
    {
        var sb = new StringBuilder();
        foreach (var e in s.Entries)
        {
            var line = Line(e);
            if (line.Length > 0) sb.Append(line).Append('\n');
        }
        return new OarGen.OarFile($"{s.File}_DISTR.ini", sb.ToString());
    }

    // Build "Type = f1|f2|...|fN" with trailing NONE fields trimmed. RecordID always present.
    public static string Line(SpidEntrySpec e)
    {
        // Field 6 (TypeParam) is type-dependent: Item=count, Package=insert index, else none.
        string typeParam =
            string.Equals(e.Type, "Item", StringComparison.OrdinalIgnoreCase) && e.Count is int c ? c.ToString() :
            string.Equals(e.Type, "Package", StringComparison.OrdinalIgnoreCase) && e.PackageIndex is int p ? p.ToString() :
            "";

        var fields = new List<string>
        {
            e.Record,                                          // 1 RecordID (never NONE)
            Join(e.StringFilters),                             // 2 StringFilters
            Join(e.FormFilters),                               // 3 FormFilters
            e.LevelFilters ?? "",                              // 4 LevelFilters
            e.Traits ?? "",                                    // 5 Traits
            typeParam,                                         // 6 TypeParam
            e.Chance is int ch ? ch.ToString() : "",           // 7 Chance
        };

        // NONE-fill, then drop trailing NONEs (keep field 1).
        for (int i = 0; i < fields.Count; i++)
            if (string.IsNullOrWhiteSpace(fields[i])) fields[i] = "NONE";
        int last = fields.Count - 1;
        while (last > 0 && fields[last] == "NONE") last--;

        return $"{e.Type} = {string.Join('|', fields.GetRange(0, last + 1))}";
    }

    // Comma-join non-empty filter items; empty list → "" (caller turns into NONE).
    private static string Join(List<string> items)
    {
        if (items is null || items.Count == 0) return "";
        var kept = items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim());
        return string.Join(',', kept);
    }
}
