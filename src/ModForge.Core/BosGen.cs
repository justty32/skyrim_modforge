using System.Globalization;
using System.Text;

namespace ModForge;

// Generates a Base Object Swapper config as a loose file at the mod folder root:
//   <File>_SWAP.ini   (BOS scans Data/ for every *_SWAP.ini at start-up)
// Pure function (no I/O) — package writes the OarFile. Format verified against BOS
// (sub_projs/mod-survey/findings/base-object-swapper-config.md):
//   [Forms]  /  [Forms|cond1,cond2]
//   baseFormID|swapFormID[|properties][|chance]
// A middle gap is held open with an EMPTY field (BOS "||" = skip), trailing empties are trimmed.
public static class BosGen
{
    public static OarGen.OarFile Generate(ObjectSwapSpec s)
    {
        var sb = new StringBuilder();
        foreach (var g in s.Groups)
        {
            sb.Append("[Forms");
            if (g.Conditions.Count > 0) sb.Append('|').Append(Join(g.Conditions, ','));
            sb.Append("]\n");
            foreach (var e in g.Entries)
            {
                var line = Line(e);
                if (line.Length > 0) sb.Append(line).Append('\n');
            }
            sb.Append('\n');
        }
        return new OarGen.OarFile($"{s.File}_SWAP.ini", sb.ToString());
    }

    // "base|swaps|properties|chance" with trailing empty fields trimmed (gaps held as "||").
    public static string Line(ObjectSwapEntrySpec e)
    {
        var fields = new List<string>
        {
            e.Base,                                            // 1 base form (never empty)
            Join(e.Swaps, ','),                                // 2 swap form(s) (random pick if many)
            e.Properties ?? "",                                // 3 transform string
            e.Chance is double ch ? ch.ToString("0.###", CultureInfo.InvariantCulture) : "",  // 4 chance
        };
        int last = fields.Count - 1;
        while (last > 0 && string.IsNullOrEmpty(fields[last])) last--;
        return string.Join('|', fields.GetRange(0, last + 1));
    }

    private static string Join(List<string> items, char sep)
    {
        if (items is null || items.Count == 0) return "";
        return string.Join(sep, items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
    }
}
