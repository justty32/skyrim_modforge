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

    // -------------------------------------------------------------------------------
    //  smsub — dump the Story Manager SUBTREE (branch nodes + quest nodes) under a given
    //  event root FormID, reflecting every scalar/link property so we can byte-compare a
    //  vanilla event's children against ModForge's generated nodes. Run:
    //    smsub Skyrim.esm 0x01320E      (vanilla ChangeLocation children)
    //    smsub MyMod.esp  0x01320E      (our generated branch/quest nodes under the same root)
    // -------------------------------------------------------------------------------
    private static int SmSub(string inPath, string rootHex)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        uint rootId = Convert.ToUInt32(rootHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16);

        var branches = mod.EnumerateMajorRecords<IStoryManagerBranchNodeGetter>().ToList();
        var qnodes = mod.EnumerateMajorRecords<IStoryManagerQuestNodeGetter>().ToList();

        // Branches whose Parent FormID == root (the event's direct branch children).
        var rootBranches = branches.Where(b => b.Parent.FormKey.ID == rootId).ToList();
        Console.WriteLine($"== branches under root 0x{rootId:X6} in {mod.ModKey.FileName}: {rootBranches.Count} ==");
        foreach (var b in rootBranches)
        {
            Console.WriteLine($"\n[SMBN] {b.FormKey}  EditorID={b.EditorID}");
            DumpProps(b, "    ");
            var kids = qnodes.Where(q => q.Parent.FormKey == b.FormKey).ToList();
            Console.WriteLine($"    -> {kids.Count} quest node(s):");
            foreach (var q in kids)
            {
                Console.WriteLine($"\n    [SMQN] {q.FormKey}  EditorID={q.EditorID}");
                DumpProps(q, "        ");
            }
        }
        // Also any quest nodes parented DIRECTLY to the root (some events skip the branch).
        var directQ = qnodes.Where(q => q.Parent.FormKey.ID == rootId).ToList();
        if (directQ.Count > 0)
        {
            Console.WriteLine($"\n== quest nodes parented DIRECTLY to root: {directQ.Count} ==");
            foreach (var q in directQ) { Console.WriteLine($"\n[SMQN] {q.FormKey}  EditorID={q.EditorID}"); DumpProps(q, "    "); }
        }
        return 0;
    }

    // smcheck — verify local SMBN/SMQN graph and quest-alias shapes without a load order.
    private static int SmCheck(string inPath)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        var issues = StoryManagerDiagnostics.Analyze(mod);
        foreach (var issue in issues)
            Console.WriteLine($"{issue.Code}\t{issue.Message}");
        Console.WriteLine($"({issues.Count} Story Manager issue(s))");
        return issues.Count == 0 ? 0 : 1;
    }

    // Reflect every public instance property; print scalars, enums, FormLinks, and list counts.
    private static void DumpProps(object rec, string indent)
    {
        foreach (var p in rec.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0) continue;
            object? v;
            try { v = p.GetValue(rec); } catch { continue; }
            if (v is null) { continue; }
            var t = v.GetType();
            string s;
            if (v is Mutagen.Bethesda.Plugins.IFormLinkGetter fl) { if (fl.FormKey.IsNull) continue; s = fl.FormKey.ToString(); }
            else if (v is System.Collections.ICollection col) { if (col.Count == 0) continue; s = $"[{col.Count} item(s)]"; }
            else if (t.IsPrimitive || v is string || t.IsEnum || v is Enum) s = v.ToString() ?? "";
            else if (t.Namespace?.StartsWith("System") == true) s = v.ToString() ?? "";
            else continue; // skip nested record getters / aspect wrappers we don't care about
            Console.WriteLine($"{indent}{p.Name} = {s}");
            // For the SMQN Quests list, recurse to show each entry's quest FormKey + flags.
            if (v is System.Collections.IEnumerable en2 && p.Name.Contains("Quest", StringComparison.OrdinalIgnoreCase) && v is not string)
                foreach (var item in en2)
                {
                    if (item is null) continue;
                    foreach (var ip in item.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        object? iv; try { iv = ip.GetValue(item); } catch { continue; }
                        if (iv is Mutagen.Bethesda.Plugins.IFormLinkGetter ifl && !ifl.FormKey.IsNull)
                            Console.WriteLine($"{indent}    .{ip.Name} = {ifl.FormKey}");
                        else if (iv is not null && (iv.GetType().IsEnum || iv.GetType().IsPrimitive) && !(iv is bool b && !b) && !(iv is int i0 && i0 == 0))
                            Console.WriteLine($"{indent}    .{ip.Name} = {iv}");
                    }
                }
        }
    }
}
