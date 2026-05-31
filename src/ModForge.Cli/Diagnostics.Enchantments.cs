internal static partial class Program
{
    // Diagnostic: print an Enchantment / ObjectEffect (ENCH)'s functional field set + its MGEF-based
    // effects — to compare a generated ENCH against a vanilla one (e.g. Skyrim.esm:0x10FB96
    // EnchWeaponFrostDamageBase = Enchantment/FireAndForget/Touch). Avoids Name (localized string
    // landmine on master overlays); prints each effect's BaseEffect FormKey + magnitude/area/duration.
    private static int EnchDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var r in mod.EnumerateMajorRecords<IObjectEffectGetter>())
        {
            if (r.FormKey.ID != id) continue;
            Console.WriteLine($"0x{id:X6}  EditorID={r.EditorID}");
            Console.WriteLine($"  EnchantType = {r.EnchantType}");
            Console.WriteLine($"  CastType = {r.CastType}   TargetType = {r.TargetType}");
            Console.WriteLine($"  EnchantmentCost = {r.EnchantmentCost}   EnchantmentAmount = {r.EnchantmentAmount}   ChargeTime = {r.ChargeTime}");
            Console.WriteLine($"  Flags = {r.Flags}   BaseEnchantment = {(r.BaseEnchantment.FormKey.IsNull ? "-" : r.BaseEnchantment.FormKey.ToString())}");
            Console.WriteLine($"  Effects = {r.Effects.Count}");
            foreach (var e in r.Effects)
                Console.WriteLine($"    effect -> {e.BaseEffect.FormKey} (mag={e.Data?.Magnitude} area={e.Data?.Area} dur={e.Data?.Duration})");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not an ObjectEffect (ENCH) in {Path.GetFileName(inPath)}");
        return 0;
    }
}
