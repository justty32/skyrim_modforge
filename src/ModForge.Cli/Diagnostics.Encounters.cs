internal static partial class Program
{
    // eczndiag — print an EncounterZone's level range / rank / flags / owner / location (compare a
    // generated zone against a vanilla one, e.g. eczndiag <Skyrim.esm> 0x0F94A6 = HelgenZone).
    private static int EcznDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var z in mod.EnumerateMajorRecords<IEncounterZoneGetter>())
        {
            if (z.FormKey.ID != id) continue;
            var maxStr = z.MaxLevel == 0 ? "uncapped (scales with player)" : z.MaxLevel.ToString();
            Console.WriteLine($"0x{id:X6}  EditorID={z.EditorID}");
            Console.WriteLine($"  levels: min={z.MinLevel}  max={maxStr}");
            Console.WriteLine($"  rank={z.Rank}");
            Console.WriteLine($"  flags={z.Flags}");
            Console.WriteLine($"  owner={(z.Owner.IsNull ? "-" : z.Owner.FormKey.ToString())}");
            Console.WriteLine($"  location={(z.Location.IsNull ? "-" : z.Location.FormKey.ToString())}");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not an EncounterZone in {Path.GetFileName(inPath)}");
        return 0;
    }
}
