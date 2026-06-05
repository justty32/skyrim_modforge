internal static partial class Program
{
    // Diagnostic: dump a FACT (faction) record — flags, ranks, and inter-faction relations.
    // Faction membership is the gate the paid-hireling recruit line keys on (PotentialHireling
    // 0x0BCC9A), so this confirms a faction's flags/relations when reasoning about why a recruit
    // condition passes or fails.
    private static int FactDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        // Best-effort resolver over THIS plugin only (resolves in-spec list/chest editorIds; vanilla
        // forms print as their FormKey — we don't open masters here, same as the other *diag commands).
        var edByFk = new Dictionary<FormKey, string>();
        foreach (var r in mod.EnumerateMajorRecords())
            if (!string.IsNullOrEmpty(r.EditorID)) edByFk[r.FormKey] = r.EditorID!;
        string Ref(FormKey fk) => fk.IsNull ? "-" : edByFk.TryGetValue(fk, out var ed) ? $"{ed} ({fk})" : fk.ToString();

        foreach (var f in mod.EnumerateMajorRecords<IFactionGetter>())
        {
            if (f.FormKey.ID != id) continue;
            Console.WriteLine($"0x{id:X6}  EditorID={f.EditorID}");
            Console.WriteLine($"  Flags = {f.Flags}   (vendor={f.Flags.HasFlag(Faction.FactionFlag.Vendor)})");
            Console.WriteLine($"  Ranks ({f.Ranks.Count}):");
            foreach (var rk in f.Ranks)
            {
                static string? T(Func<string?> r) { try { return r(); } catch { return "<localized>"; } }
                Console.WriteLine($"    rank {rk.Number}: male=\"{T(() => rk.Title?.Male?.String)}\" female=\"{T(() => rk.Title?.Female?.String)}\"");
            }
            Console.WriteLine($"  Relations ({f.Relations.Count}):");
            foreach (var rel in f.Relations)
                Console.WriteLine($"    -> {rel.Target.FormKey} modifier={rel.Modifier} reaction={rel.Reaction}");
            // Vendor block: VendorValues (hours/radius/buy-stolen/not-sell) + buy-sell list + merchant
            // chest + vendor location — compare a generated vendor FACT against a vanilla merchant.
            if (f.VendorValues is { } vv)
            {
                Console.WriteLine($"  VendorValues: startHour={vv.StartHour} endHour={vv.EndHour} radius={vv.Radius} "
                    + $"buysStolen={vv.OnlyBuysStolenItems} notSellBuy={vv.NotSellBuy}");
                Console.WriteLine($"  VendorBuySellList = {Ref(f.VendorBuySellList.FormKey)}"
                    + (vv.NotSellBuy ? "  [NOT-sell list: trades everything EXCEPT these]" : "  [trades THESE categories]"));
                if (!f.VendorBuySellList.IsNull
                    && mod.EnumerateMajorRecords<IFormListGetter>().FirstOrDefault(l => l.FormKey == f.VendorBuySellList.FormKey) is { } fl)
                    foreach (var it in fl.Items) Console.WriteLine($"      item -> {Ref(it.FormKey)}");
                Console.WriteLine($"  MerchantContainer = {Ref(f.MerchantContainer.FormKey)}");
                if (f.VendorLocation is { } loc && loc.Target is ILocationTargetGetter lt)
                    Console.WriteLine($"  VendorLocation -> {Ref(lt.Link.FormKey)} radius={loc.Radius}");
                else
                    Console.WriteLine("  VendorLocation: <none>");
            }
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Faction in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Diagnostic: dump RELA (relationship) records. <formId> is matched first as a RELA itself; if
    // none, every RELA whose Parent or Child is that FormID is listed (so you can ask "what static
    // relationships involve this actor?"). The known finding: vanilla has zero RELA referencing the
    // player (0x14) — player relationship rank is always script-set at runtime, never static.
    private static int RelaDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        var all = mod.EnumerateMajorRecords<IRelationshipGetter>().ToList();   // materialize once: enumerated twice below (overlay re-parses otherwise)
        void Print(IRelationshipGetter r) => Console.WriteLine(
            $"0x{r.FormKey.ID:X6}  {r.EditorID ?? "-"}  parent={r.Parent.FormKey} child={r.Child.FormKey}"
            + $"  rank={r.Rank}  assoc={(r.AssociationType.FormKey.IsNull ? "-" : r.AssociationType.FormKey.ToString())}  flags={r.Flags}");

        var self = all.FirstOrDefault(r => r.FormKey.ID == id);
        if (self is not null) { Print(self); return 0; }

        int hits = 0;
        foreach (var r in all)
        {
            if (r.Parent.FormKey.ID != id && r.Child.FormKey.ID != id) continue;
            Print(r);
            hits++;
        }
        Console.WriteLine($"-- 0x{id:X6} is not a RELA; {hits} RELA(s) reference it as parent/child in {Path.GetFileName(inPath)}");
        return 0;
    }
}
