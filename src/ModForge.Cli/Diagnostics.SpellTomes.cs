internal static partial class Program
{
    // Diagnostic: print a Book's Teaches union (Nothing / Spell-> / Skill=) + Flags + model, for one
    // 0xFORMID. Used to discover the exact shape of Book.Teaches against vanilla spell tomes / skill
    // books so the generator can wire it correctly. Also reflects the union member type names.
    private static int BookDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        var dataDir = Path.GetDirectoryName(Path.GetFullPath(inPath))!;
        var readParams = new BinaryReadParameters
        {
            StringsParam = new StringsReadParameters
            { BsaFolderOverride = dataDir, StringsFolderOverride = dataDir, TargetLanguage = Language.English },
        };
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE, readParams);
        foreach (var r in mod.EnumerateMajorRecords<IBookGetter>())
        {
            if (r.FormKey.ID != id) continue;
            // Name is a localized TranslatedString — resolving it can need the (headless-absent)
            // listings path; tolerate that, the diag's real payload is Teaches/Flags/Model.
            string name; try { name = r.Name?.String ?? "-"; } catch { name = "<unresolved>"; }
            Console.WriteLine($"0x{id:X6}  EditorID={r.EditorID}  Name=\"{name}\"");
            Console.WriteLine($"  Flags = {r.Flags}");
            Console.WriteLine($"  Model = {(r.Model?.File.ToString() ?? "-")}");
            Console.WriteLine($"  Value = {r.Value}  Weight = {r.Weight}");
            var teaches = r.Teaches;
            Console.WriteLine($"  Teaches runtime type = {teaches?.GetType().FullName ?? "null"}");
            switch (teaches)
            {
                case IBookSpellGetter sp:
                    Console.WriteLine($"  Teaches = Spell -> {(sp.Spell.FormKey.IsNull ? "-" : sp.Spell.FormKey.ToString())}");
                    break;
                case IBookSkillGetter sk:
                    Console.WriteLine($"  Teaches = Skill = {sk.Skill}");
                    break;
                default:
                    Console.WriteLine($"  Teaches = Nothing");
                    break;
            }
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Book in {Path.GetFileName(inPath)}");
        return 0;
    }
}
