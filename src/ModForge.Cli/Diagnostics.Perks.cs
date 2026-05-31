internal static partial class Program
{
    // perkdiag <in.esp> <0xFORMID|entrypoints>: dump a Perk's trunk/effects/conditions, OR (when the
    // 2nd arg is "entrypoints") list every EntryType enum name — the authorable `entryPoint` values.
    private static int PerkDiag(string inPath, string arg)
    {
        if (arg.Equals("entrypoints", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("EntryType values (use the name as a perk effect `entryPoint`):");
            foreach (var v in Enum.GetValues<APerkEntryPointEffect.EntryType>())
                Console.WriteLine($"  {(int)v,3}  {v}");
            return 0;
        }
        uint id = Convert.ToUInt32(arg.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var r in mod.EnumerateMajorRecords<IPerkGetter>())
        {
            if (r.FormKey.ID != id) continue;
            Console.WriteLine($"0x{id:X6}  EditorID={r.EditorID}");
            Console.WriteLine($"  Playable={r.Playable} Hidden={r.Hidden} Trait={r.Trait} Level={r.Level} NumRanks={r.NumRanks}"
                + (r.NextPerk.FormKey.IsNull ? "" : $" NextPerk={r.NextPerk.FormKey}"));
            Console.WriteLine($"  Conditions = {r.Conditions.Count}");
            foreach (var c in r.Conditions.OfType<IConditionFloatGetter>())
                Console.WriteLine($"    cond: {c.Data.GetType().Name} func={c.Data.Function} {c.CompareOperator} {c.ComparisonValue} runOn={c.Data.RunOnType} flags={c.Flags}");
            Console.WriteLine($"  Effects = {r.Effects.Count}");
            foreach (var e in r.Effects)
            {
                if (e is IPerkAbilityEffectGetter ab)
                    Console.WriteLine($"    [ability]    rank={ab.Rank} prio={ab.Priority} ability={ab.Ability.FormKey} conds={ab.Conditions.Count}");
                else if (e is IPerkEntryPointModifyValueGetter mv)
                    Console.WriteLine($"    [entryPoint] {mv.EntryPoint} {mv.Modification} {mv.Value} rank={mv.Rank} prio={mv.Priority} conds={mv.Conditions.Count}");
                else if (e is IAPerkEntryPointEffectGetter ep)
                    Console.WriteLine($"    [entryPoint] {ep.EntryPoint} ({e.GetType().Name})");
                else
                    Console.WriteLine($"    [{e.GetType().Name}]");
            }
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Perk in {Path.GetFileName(inPath)}");
        return 0;
    }
}
