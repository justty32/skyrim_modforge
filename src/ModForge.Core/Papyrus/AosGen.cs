using System.Text;

namespace ModForge;

// Generates an AnimObject Swapper config as a loose file at the mod folder root:
//   <File>_ANIO.ini   (AOS scans Data/ for every *_ANIO.ini at start-up)
// Pure function (no I/O) — package writes the OarFile. Format verified against AOS
// (sub_projs/mod-survey/findings/animobject-swapper-*.md):
//   [BaseANIO|FILTERS|TRAITS]
//   baseANIO|swap1,swap2,...
// Trailing empty header segments (FILTERS/TRAITS) are trimmed; a TRAITS-only header holds the
// FILTERS slot open with "||".
public static class AosGen
{
    public static OarGen.OarFile Generate(AnimObjectSwapSpec s)
    {
        var sb = new StringBuilder();
        foreach (var e in s.Entries)
        {
            if (string.IsNullOrWhiteSpace(e.Base) || e.Swaps.Count == 0) continue;
            sb.Append('[').Append(Header(e)).Append("]\n");
            sb.Append(e.Base).Append('|').Append(Join(e.Swaps)).Append('\n');
        }
        return new OarGen.OarFile($"{s.File}_ANIO.ini", sb.ToString());
    }

    // "Base|filters|traits" with trailing empty segments trimmed (a gap stays as "||").
    public static string Header(AnimObjectSwapEntrySpec e)
    {
        var segs = new List<string> { e.Base, Join(e.Filters), e.Traits ?? "" };
        int last = segs.Count - 1;
        while (last > 0 && string.IsNullOrEmpty(segs[last])) last--;
        return string.Join('|', segs.GetRange(0, last + 1));
    }

    private static string Join(List<string> items)
    {
        if (items is null || items.Count == 0) return "";
        return string.Join(',', items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
    }
}
