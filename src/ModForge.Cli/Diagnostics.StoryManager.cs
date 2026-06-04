internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  smtree — list Story Manager EVENT roots (SMEN) from any plugin. Use it to discover
    //  a vanilla event root's FormID + Type (e.g. the Kill Actor root Skyrim.esm:0x013010)
    //  when adding a new event to StoryManagerEvents. Run `smtree Skyrim.esm`.
    // -------------------------------------------------------------------------------
    private static int SmTree(string inPath)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        int n = 0;
        foreach (var en in mod.EnumerateMajorRecords<IStoryManagerEventNodeGetter>())
        {
            var mark = (en.EditorID ?? "").Contains("Kill", StringComparison.OrdinalIgnoreCase) ? "   <-- KILL?" : "";
            Console.WriteLine($"{en.FormKey}  EditorID={en.EditorID}  Type={en.Type}  Flags={en.Flags}  Max={en.MaxConcurrentQuests}{mark}");
            n++;
        }
        Console.WriteLine($"({n} event roots)");
        return 0;
    }
}
