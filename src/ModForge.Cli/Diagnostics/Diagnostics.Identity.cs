internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  identitydiag — reconstruct the lightweight identity/class system's wiring from a
    //  BUILT plugin (the generated .esp). Reads the controller quest's VMAD to recover the
    //  identity registry (faction ↔ code), the default-grant quest, the acquire books, and
    //  the two control globals — so you can verify the whole identity build at a glance and
    //  debug an in-game issue without the CK. Run `identitydiag <ModForgeIdentity.esp>`.
    // -------------------------------------------------------------------------------
    private static int IdentityDiag(string inPath)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);

        // FormKey -> EditorID for in-plugin records (identity factions/globals/quests live here).
        var ed = new Dictionary<FormKey, string>();
        foreach (var r in mod.EnumerateMajorRecords<ISkyrimMajorRecordGetter>())
            if (!string.IsNullOrEmpty(r.EditorID)) ed[r.FormKey] = r.EditorID!;
        string Ref(FormKey fk) => fk.IsNull ? "-" : ed.TryGetValue(fk, out var e) ? e : $"0x{fk.ID:X6}:{fk.ModKey.FileName}";

        static IScriptEntryGetter? Script(IHaveVirtualMachineAdapterGetter? h, string name) =>
            h?.VirtualMachineAdapter?.Scripts.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        static IScriptPropertyGetter? Prop(IScriptEntryGetter e, string name) =>
            e.Properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        Console.WriteLine($"identitydiag  {Path.GetFileName(inPath)}");

        // --- control globals -------------------------------------------------------
        Console.WriteLine("\n=== control globals (GLOB) ===");
        foreach (var name in new[] { "MF_PrimaryIdentity", "MF_IdentityOverride" })
        {
            var g = mod.Globals.FirstOrDefault(x => string.Equals(x.EditorID, name, StringComparison.OrdinalIgnoreCase));
            if (g is null) { Console.WriteLine($"  {name}: (absent — no primaryIdentity/override dialogue)"); continue; }
            float? v = g switch { IGlobalShortGetter s => s.Data, IGlobalIntGetter i => i.Data, IGlobalFloatGetter f => f.Data, _ => null };
            Console.WriteLine($"  {name} = {v?.ToString() ?? "?"} (initial)");
        }

        // --- controller registry (faction <-> code) --------------------------------
        Console.WriteLine("\n=== primary-identity controller (MFIdentityController) ===");
        var controller = mod.Quests.FirstOrDefault(q => Script(q, "MFIdentityController") is not null);
        if (controller is null) Console.WriteLine("  (no controller quest — dialogue uses no primaryIdentity)");
        else
        {
            var s = Script(controller, "MFIdentityController")!;
            Console.WriteLine($"  quest {Ref(controller.FormKey)}  startGameEnabled={controller.Flags.HasFlag(Quest.Flag.StartGameEnabled)}");
            var facs = (Prop(s, "Factions") as IScriptObjectListPropertyGetter)?.Objects.Select(o => o.Object.FormKey).ToList() ?? new();
            var codes = (Prop(s, "Codes") as IScriptIntListPropertyGetter)?.Data.ToList() ?? new();
            Console.WriteLine($"  Primary={Ref((Prop(s, "Primary") as IScriptObjectPropertyGetter)?.Object.FormKey ?? default)}  Override={Ref((Prop(s, "Override") as IScriptObjectPropertyGetter)?.Object.FormKey ?? default)}");
            Console.WriteLine("  identities (priority DESC):");
            for (int i = 0; i < facs.Count; i++)
                Console.WriteLine($"    code {(i < codes.Count ? codes[i].ToString() : "?")}  faction {Ref(facs[i])}");
        }

        // --- default-grant quest ---------------------------------------------------
        Console.WriteLine("\n=== default-identity granter (MFIdentityDefault) ===");
        var defq = mod.Quests.FirstOrDefault(q => Script(q, "MFIdentityDefault") is not null);
        if (defq is null) Console.WriteLine("  (none — no identity is default:true)");
        else
        {
            var s = Script(defq, "MFIdentityDefault")!;
            var facs = (Prop(s, "Factions") as IScriptObjectListPropertyGetter)?.Objects.Select(o => Ref(o.Object.FormKey)) ?? Enumerable.Empty<string>();
            var grants = (Prop(s, "Grants") as IScriptObjectListPropertyGetter)?.Objects.Select(o => Ref(o.Object.FormKey)) ?? Enumerable.Empty<string>();
            var perks = (Prop(s, "Perks") as IScriptObjectListPropertyGetter)?.Objects.Select(o => Ref(o.Object.FormKey)) ?? Enumerable.Empty<string>();
            Console.WriteLine($"  quest {Ref(defq.FormKey)}  factions=[{string.Join(", ", facs)}]  grants=[{string.Join(", ", grants)}]  perks=[{string.Join(", ", perks)}]");
        }

        // --- auto-grant trigger ----------------------------------------------------
        Console.WriteLine("\n=== auto-grant trigger (MFIdentityAutoGrant) ===");
        var agq = mod.Quests.FirstOrDefault(q => Script(q, "MFIdentityAutoGrant") is not null);
        if (agq is null) Console.WriteLine("  (none — no identity uses autoGrantWhen)");
        else
        {
            var s = Script(agq, "MFIdentityAutoGrant")!;
            var facs = (Prop(s, "Factions") as IScriptObjectListPropertyGetter)?.Objects.Select(o => Ref(o.Object.FormKey)).ToList() ?? new();
            var avs = (Prop(s, "AvNames") as IScriptStringListPropertyGetter)?.Data.ToList() ?? new();
            var thr = (Prop(s, "Thresholds") as IScriptFloatListPropertyGetter)?.Data.ToList() ?? new();
            for (int i = 0; i < facs.Count; i++)
                Console.WriteLine($"  {facs[i]} ← GetActorValue(\"{(i < avs.Count ? avs[i] : "?")}\") >= {(i < thr.Count ? thr[i].ToString() : "?")}");
        }

        // --- acquire books ---------------------------------------------------------
        Console.WriteLine("\n=== acquire books (MFIdentityBook) ===");
        int books = 0;
        foreach (var b in mod.Books)
        {
            var s = Script(b, "MFIdentityBook");
            if (s is null) continue;
            books++;
            var fac = Ref((Prop(s, "TheFaction") as IScriptObjectPropertyGetter)?.Object.FormKey ?? default);
            var grant = Ref((Prop(s, "GrantAbility") as IScriptObjectPropertyGetter)?.Object.FormKey ?? default);
            var perk = Ref((Prop(s, "GrantPerk") as IScriptObjectPropertyGetter)?.Object.FormKey ?? default);
            var scene = Ref((Prop(s, "AcquireScene") as IScriptObjectPropertyGetter)?.Object.FormKey ?? default);
            var toggle = (Prop(s, "Toggle") as IScriptBoolPropertyGetter)?.Data ?? false;
            Console.WriteLine($"  {Ref(b.FormKey)}: faction={fac}  grant={grant}  perk={perk}  scene={scene}  toggle={toggle}");
        }
        if (books == 0) Console.WriteLine("  (none)");

        return 0;
    }
}
