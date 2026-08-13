internal static partial class Program
{
    // Diagnostic: print a ConstructibleObject (COBJ / recipe) in full — created object + count,
    // workbench keyword, components (with counts), and the CTDA conditions (function + comparison +
    // first parameter, e.g. HasPerk <perk> / GetItemCount <item>). Vanilla temper recipes use
    // WorkbenchKeyword=CraftingSmithingSharpeningWheel/ArmorTable, CreatedObject=the item being
    // improved, and a HasPerk smithing condition — probe one (e.g. TemperWeaponSteelSword) to learn
    // the shape before authoring temper recipes.
    private static int CobjDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var c in mod.EnumerateMajorRecords<IConstructibleObjectGetter>())
        {
            if (c.FormKey.ID != id) continue;
            Console.WriteLine($"0x{id:X6}  EditorID={c.EditorID}");
            Console.WriteLine($"  CreatedObject = {c.CreatedObject.FormKey}   Count = {c.CreatedObjectCount ?? 1}");
            Console.WriteLine($"  WorkbenchKeyword = {c.WorkbenchKeyword.FormKey}");
            Console.WriteLine($"  Components ({c.Items?.Count ?? 0}):");
            if (c.Items is { } items)
                foreach (var e in items) Console.WriteLine($"    -> {e.Item.Item.FormKey} x{e.Item.Count}");
            Console.WriteLine($"  Conditions ({c.Conditions.Count}):");
            foreach (var cond in c.Conditions)
            {
                string fn = cond.Data.Function.ToString();
                string cmp = cond is IConditionFloatGetter cf ? $" {cond.CompareOperator} {cf.ComparisonValue}" : "";
                string p1 = "";
                if (cond.Data is IHasPerkConditionDataGetter hp) p1 = $"  perk={hp.Perk.Link.FormKey}";
                else if (cond.Data is IGetItemCountConditionDataGetter gic) p1 = $"  item={gic.ItemOrList.Link.FormKey}";
                Console.WriteLine($"    {fn}{cmp}{p1}   flags={cond.Flags}");
            }
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a ConstructibleObject in {Path.GetFileName(inPath)}");
        return 0;
    }
}
