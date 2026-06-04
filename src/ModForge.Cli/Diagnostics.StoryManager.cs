internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  smtree — list Story Manager EVENT roots (SMEN) from any plugin.
    //  Hunting the vanilla "Kill Actor" event root so we can replace the placeholder
    //  Quest.Event ("KILL") in StoryManagerProbe with the real event code + parent. Run
    //  `smtree Skyrim.esm` and look for the row marked "<-- KILL?".
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

    // -------------------------------------------------------------------------------
    //  smprobe — write the Story Manager structural probe plugin (StoryManagerProbe).
    //  Hangs an additive SMBN→SMQN→Quest graph under the VANILLA Kill Actor event root
    //  (the 0xKILLROOT FormID, against Skyrim.esm). Discover that FormID with `smtree`.
    // -------------------------------------------------------------------------------
    private static int SmProbe(string outPath, string killRootHex)
    {
        uint id = Convert.ToUInt32(killRootHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        var killRoot = new FormKey(ModKey.FromNameAndExtension("Skyrim.esm"), id);
        var mod = StoryManagerProbe.BuildProbe(killRoot);
        PluginIo.Write(mod, outPath);   // NoCheck + ESL-limit guard, same as every other write path
        Console.WriteLine($"wrote {outPath}  (kill root {killRoot})");
        return 0;
    }
}
