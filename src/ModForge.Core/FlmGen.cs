using System.Text;

namespace ModForge;

// Generates a FormList Manipulator config as a loose file at the mod folder root:
//   <File>_FLM.ini   (FLM scans Data/ for every *_FLM.ini at start-up)
// Pure function (no I/O) — package writes the OarFile. Line format verified against FLM v1.8.1
// (sub_projs/mod-survey/findings/formlist-manipulator-config-core.md / -advanced.md). Definitions
// (Filter/Alias/Group/Collection) are emitted before the FormList operation lines that reference them.
public static class FlmGen
{
    public static OarGen.OarFile Generate(FormListInjectSpec s)
    {
        var sb = new StringBuilder();
        sb.Append("[General]\n");

        foreach (var f in s.Filters)
            sb.Append("Filter = ").Append(f.Name).Append('|').Append(Join(f.Conditions)).Append('\n');
        foreach (var a in s.Aliases)
            sb.Append("Alias = ").Append(a.Name).Append('|').Append(Join(a.Items)).Append('\n');
        foreach (var g in s.Groups)
            sb.Append("Group = ").Append(g.Name).Append('|').Append(Join(g.Items)).Append('\n');
        foreach (var c in s.Collections)
        {
            sb.Append("Collection = ").Append(c.Name).Append('|').Append(c.FormType).Append('|').Append(Join(c.Keywords));
            if (!string.IsNullOrWhiteSpace(c.Filter)) sb.Append('|').Append(Ref(c.Filter));
            sb.Append('\n');
        }
        foreach (var e in s.Entries)
        {
            sb.Append("FormList = ").Append(e.Target).Append('|').Append(Join(e.Forms));
            if (!string.IsNullOrWhiteSpace(e.Filter)) sb.Append('|').Append(Ref(e.Filter));
            sb.Append('\n');
        }
        return new OarGen.OarFile($"{s.File}_FLM.ini", sb.ToString());
    }

    // A filter reference in a FormList/Collection line is "#FilterName"; tolerate an author-supplied '#'.
    private static string Ref(string name) =>
        name.StartsWith('#') ? name : "#" + name.Trim();

    // Comma-join non-empty tokens (the FLM list separator).
    private static string Join(List<string> items)
    {
        if (items is null || items.Count == 0) return "";
        return string.Join(", ", items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
    }
}
