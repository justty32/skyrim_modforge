internal static partial class Program
{
    // Diagnostic: print a Shout's three WordsOfPower rows (each Word/Spell FormLink + RecoveryTime)
    // and its MenuDisplayObject, to compare a generated SHOU against a vanilla one (e.g.
    // UnrelentingForceShout 0x013E07) without an in-game cycle. A shout that fires correctly has
    // exactly the right Word->Spell pairing per row; this surfaces it. Avoids the localized Name.
    private static int ShoutDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var r in mod.EnumerateMajorRecords<IShoutGetter>())
        {
            if (r.FormKey.ID != id) continue;
            string F(IFormLinkGetter<IMajorRecordGetter> l) => l.FormKey.IsNull ? "-" : l.FormKey.ToString();
            Console.WriteLine($"0x{id:X6}  EditorID={r.EditorID}");
            Console.WriteLine($"  MenuDisplayObject = {F(r.MenuDisplayObject)}");
            Console.WriteLine($"  WordsOfPower ({r.WordsOfPower.Count}):");
            int i = 0;
            foreach (var w in r.WordsOfPower)
                Console.WriteLine($"    [{i++}] Word={F(w.Word)}  Spell={F(w.Spell)}  RecoveryTime={w.RecoveryTime}");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Shout in {Path.GetFileName(inPath)}");
        return 0;
    }
}
