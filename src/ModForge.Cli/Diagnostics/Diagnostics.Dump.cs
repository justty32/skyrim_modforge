internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  dump — read a plugin back and print its records + the key things generation
    //  wires up (names, npc faction membership, VMAD scripts, dialogue, quest
    //  objectives). Round-trip verification helper + a way to inspect any .esp.
    //
    //  The per-record DETAIL printing is a long `if (r is IXxxGetter)` chain, split
    //  across two helpers purely by POSITION (not theme) so the printed output stays
    //  byte-for-byte identical: DumpRecordInventoryAndWorld covers the first run of
    //  record types (npc → recipe), DumpRecordMagicAiAndText the rest (spell → scene).
    // -------------------------------------------------------------------------------
    private static int Dump(string inPath)
    {
        // Read-only inspection → overlay (lazy, no full materialize). Enumerate the records ONCE
        // (the overlay re-parses the group on each pass) and reuse the list throughout.
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        var records = mod.EnumerateMajorRecords().ToList();
        var edByFk = new Dictionary<FormKey, string>();
        foreach (var r in records)
            if (!string.IsNullOrEmpty(r.EditorID)) edByFk[r.FormKey] = r.EditorID!;
        string Ref(FormKey fk) => fk.IsNull ? "<null>" : edByFk.TryGetValue(fk, out var ed) ? ed : fk.ToString();
        // In-spec LeveledNpc FormKeys — so a placed ACHR pointing at one is shown as a LEVELED spawn.
        var inSpecLvln = records.OfType<ILeveledNpcGetter>().Select(l => l.FormKey).ToHashSet();

        var masters = mod.MasterReferences;
        Console.WriteLine($"{Path.GetFileName(inPath)} — {records.Count} record(s), "
            + $"localized={mod.UsingLocalization}, master(s)=[{string.Join(", ", masters.Select(m => m.Master.FileName.ToString()))}]");
        foreach (var r in records)
        {
            var name = (r as INamedGetter)?.Name;
            // Overlay getters report type names like "BookBinaryOverlay" — trim the suffix to the record name.
            var typeName = r.GetType().Name.Replace("BinaryOverlay", "");
            Console.WriteLine($"  [{r.FormKey}] {typeName} {r.EditorID}" + (name is { } nm ? $"  \"{nm}\"" : ""));

            DumpRecordInventoryAndWorld(r, Ref, edByFk, inSpecLvln);
            DumpRecordMagicAiAndText(r, Ref);
        }
        return 0;
    }

    // First positional half of the per-record detail chain: actors, inventory items, the magic
    // CARRIERS (effects/enchantment) on them, world structure (worldspace/region/cell/encZone),
    // placements + their linked refs/teleports, and the leveled/container/recipe lists.
    private static void DumpRecordInventoryAndWorld(
        IMajorRecordGetter r, Func<FormKey, string> Ref,
        Dictionary<FormKey, string> edByFk, HashSet<FormKey> inSpecLvln)
    {
        if (r is INpcGetter npc)
        {
            if (!npc.Race.IsNull)          Console.WriteLine($"      race -> {Ref(npc.Race.FormKey)}");
            if (!npc.Class.IsNull)         Console.WriteLine($"      class -> {Ref(npc.Class.FormKey)}");
            bool autoCalc = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.AutoCalcStats);
            if (npc.Configuration.Level is INpcLevelGetter lvl && (lvl.Level != 1 || autoCalc))
                Console.WriteLine($"      level={lvl.Level} autoCalcStats={autoCalc}");
            if (!npc.DefaultOutfit.IsNull) Console.WriteLine($"      outfit -> {Ref(npc.DefaultOutfit.FormKey)}");
            if (!npc.Voice.IsNull) Console.WriteLine($"      voice -> {Ref(npc.Voice.FormKey)}");
            if (!npc.CrimeFaction.IsNull) Console.WriteLine($"      crimeFaction -> {Ref(npc.CrimeFaction.FormKey)}");
            if (!npc.CombatStyle.IsNull) Console.WriteLine($"      combatStyle -> {Ref(npc.CombatStyle.FormKey)}");
            if (npc.ActorEffect is { Count: > 0 } actEff)
                foreach (var sp in actEff) Console.WriteLine($"      spell -> {Ref(sp.FormKey)}");
            if (npc.AIData is { } aid && (aid.Aggression != 0 || aid.Confidence != 0 || aid.Assistance != 0))
                Console.WriteLine($"      aiData: Aggression={aid.Aggression} Confidence={aid.Confidence} Assistance={aid.Assistance} Mood={aid.Mood} Energy={aid.EnergyLevel}");
            foreach (var f in npc.Factions)
                Console.WriteLine($"      faction -> {Ref(f.Faction.FormKey)} (rank {f.Rank})");
            foreach (var pkg in npc.Packages)
                Console.WriteLine($"      package -> {Ref(pkg.FormKey)}");
            if (npc.Perks is { Count: > 0 } perks)
                foreach (var perkPlace in perks) Console.WriteLine($"      perk -> {Ref(perkPlace.Perk.FormKey)} (rank {perkPlace.Rank})");
        }

        if (r is IKeywordedGetter<IKeywordGetter> kwd && kwd.Keywords is { Count: > 0 } kws)
            foreach (var k in kws)
                Console.WriteLine($"      keyword -> {Ref(k.FormKey)}");

        if (r is IWeaponGetter wpn)
        {
            if (wpn.BasicStats is { } bs) Console.WriteLine($"      damage={bs.Damage} value={bs.Value} weight={bs.Weight}");
            if (wpn.Data is { } wd) Console.WriteLine($"      speed={wd.Speed} reach={wd.Reach} anim={wd.AnimationType}");
            if (wpn.Model?.File is { } wmf) Console.WriteLine($"      model={wmf}");        // null model => CRASH on equip
            if (wpn.FirstPersonModel.FormKeyNullable is { } fpk) Console.WriteLine($"      firstPersonModel -> {fpk}");
            if (!wpn.ObjectEffect.IsNull) Console.WriteLine($"      enchantment -> {Ref(wpn.ObjectEffect.FormKey)} (charge={wpn.EnchantmentAmount})");
        }

        if (r is IBookGetter bk)
        {
            if (bk.Model?.File is { } bmf)
                Console.WriteLine($"      model={bmf}");                                   // null model => CRASH on read
            var teach = bk.Teaches switch
            {
                IBookSpellGetter sp => $"spell -> {(sp.Spell.FormKey.IsNull ? "-" : Ref(sp.Spell.FormKey))}",
                IBookSkillGetter sk => $"skill = {sk.Skill}",
                _ => "nothing",
            };
            Console.WriteLine($"      book: teaches={teach} flags={bk.Flags}");
        }

        // External-resource pipeline: the per-record sound FormLinks + SNDR records (the model
        // paths print via the IModeledGetter blocks below).
        if (r is IActivatorGetter acti)
        {
            if (!acti.ActivationSound.IsNull) Console.WriteLine($"      activationSound -> {Ref(acti.ActivationSound.FormKey)}");
            if (!acti.LoopingSound.IsNull) Console.WriteLine($"      loopingSound -> {Ref(acti.LoopingSound.FormKey)}");
        }
        if (r is IMiscItemGetter misc)
        {
            if (!misc.PickUpSound.IsNull) Console.WriteLine($"      pickUpSound -> {Ref(misc.PickUpSound.FormKey)}");
            if (!misc.PutDownSound.IsNull) Console.WriteLine($"      putDownSound -> {Ref(misc.PutDownSound.FormKey)}");
        }
        if (r is IWeaponGetter wsnd)
        {
            if (!wsnd.PickUpSound.IsNull) Console.WriteLine($"      pickUpSound -> {Ref(wsnd.PickUpSound.FormKey)}");
            if (!wsnd.PutDownSound.IsNull) Console.WriteLine($"      putDownSound -> {Ref(wsnd.PutDownSound.FormKey)}");
        }
        if (r is ISoundDescriptorGetter snd)
        {
            Console.WriteLine($"      sndr: category -> {Ref(snd.Category.FormKey)} outputModel -> {Ref(snd.OutputModel.FormKey)} priority={snd.Priority}");
            foreach (var sf in snd.SoundFiles) Console.WriteLine($"        soundFile={sf.GivenPath}");
        }

        if (r is IArmorGetter arm)
        {
            if (arm.BodyTemplate is { } bt)
                Console.WriteLine($"      armorRating={arm.ArmorRating} armorType={bt.ArmorType} slots=[{bt.FirstPersonFlags}]");
            if (!arm.ObjectEffect.IsNull) Console.WriteLine($"      enchantment -> {Ref(arm.ObjectEffect.FormKey)}");
        }

        if (r is IObjectEffectGetter oe)
            Console.WriteLine($"      ench: enchantType={oe.EnchantType} cast={oe.CastType} target={oe.TargetType}"
                + $" cost={oe.EnchantmentCost} amount={oe.EnchantmentAmount} chargeTime={oe.ChargeTime} effects={oe.Effects.Count}");

        if (r is IHasEffectsGetter eff && eff.Effects.Count > 0)
            foreach (var e in eff.Effects)
                Console.WriteLine($"      effect -> {Ref(e.BaseEffect.FormKey)} (mag={e.Data?.Magnitude} area={e.Data?.Area} dur={e.Data?.Duration})");

        if (r is IWordOfPowerGetter woop)
            Console.WriteLine($"      word: translation=\"{woop.Translation?.String}\"");

        if (r is IShoutGetter shout)
        {
            if (!shout.MenuDisplayObject.IsNull)
                Console.WriteLine($"      menuDisplayObject -> {Ref(shout.MenuDisplayObject.FormKey)}");
            int wi = 0;
            foreach (var w in shout.WordsOfPower)
                Console.WriteLine($"      word[{wi++}] -> {Ref(w.Word.FormKey)}  spell -> {Ref(w.Spell.FormKey)}  recovery={w.RecoveryTime}");
        }

        DumpRecordWorld(r, Ref, edByFk, inSpecLvln);
    }
}
