internal static partial class Program
{
    // Diagnostic: full survey of an Npc's link-bearing fields — race/class/voice/outfit, factions
    // (with rank), packages, crimeFaction, template, defaultPackageList, combatStyle, configuration
    // flags, sleeping outfit, etc. Used to diff a vanilla NPC (e.g. Ysolda, who crosses cells daily)
    // against a Mutagen-generated NPC to find which field(s) the engine needs to accept cross-cell
    // Travel — the It.16b "stays in inn" failure mode.
    private static int NpcDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var n in mod.EnumerateMajorRecords<INpcGetter>())
        {
            if (n.FormKey.ID != id) continue;
            string F(IFormLinkGetter<IMajorRecordGetter> l) => l.FormKey.IsNull ? "-" : l.FormKey.ToString();
            Console.WriteLine($"0x{id:X6}  EditorID={n.EditorID}");
            Console.WriteLine($"  Race = {F(n.Race)}   Class = {F(n.Class)}   Voice = {F(n.Voice)}");
            Console.WriteLine($"  DefaultOutfit = {F(n.DefaultOutfit)}   SleepingOutfit = {F(n.SleepingOutfit)}");
            Console.WriteLine($"  CombatStyle = {F(n.CombatStyle)}   CrimeFaction = {F(n.CrimeFaction)}");
            Console.WriteLine($"  Template = {F(n.Template)}   DefaultPackageList = {F(n.DefaultPackageList)}");
            Console.WriteLine($"  SpectatorOverridePackageList = {F(n.SpectatorOverridePackageList)}");
            Console.WriteLine($"  ObserveDeadBodyOverridePackageList = {F(n.ObserveDeadBodyOverridePackageList)}");
            Console.WriteLine($"  GuardWarnOverridePackageList = {F(n.GuardWarnOverridePackageList)}");
            Console.WriteLine($"  CombatOverridePackageList = {F(n.CombatOverridePackageList)}");
            Console.WriteLine($"  Configuration.Flags = {n.Configuration.Flags}");
            if (n.Configuration.Level is INpcLevelGetter lvl) Console.WriteLine($"  Configuration.Level = {lvl.Level}");
            Console.WriteLine($"  MajorFlags = {n.MajorFlags}");
            Console.WriteLine($"  AIData: Aggression={n.AIData.Aggression} Confidence={n.AIData.Confidence} Mood={n.AIData.Mood} Assistance={n.AIData.Assistance} Energy={n.AIData.EnergyLevel} Responsibility={n.AIData.Responsibility}");
            Console.WriteLine($"  Factions ({n.Factions.Count}):");
            foreach (var f in n.Factions) Console.WriteLine($"    -> {f.Faction.FormKey} rank={f.Rank}");
            Console.WriteLine($"  Packages ({n.Packages.Count}):");
            foreach (var p in n.Packages) Console.WriteLine($"    -> {p.FormKey}");
            Console.WriteLine($"  Keywords ({n.Keywords?.Count ?? 0})" + (n.Keywords is null ? "" : ": " + string.Join(", ", n.Keywords.Select(k => k.FormKey.ToString()))));
            Console.WriteLine($"  ActorEffect/Spells ({n.ActorEffect?.Count ?? 0})" + (n.ActorEffect is null ? "" : ": " + string.Join(", ", n.ActorEffect.Select(s => s.FormKey.ToString()))));
            Console.WriteLine($"  Perks = {n.Perks?.Count ?? 0}   Items = {n.Items?.Count ?? 0}   Attacks = {n.Attacks.Count}");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not an Npc in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Diagnostic: print a CombatStyle's offensive/defensive multipliers + the six equipment
    // preferences (Melee/Magic/Ranged/Shout/Unarmed/Staff) + flags. The equipment scores are how
    // the AI decides which combat path to favour — a magic-preferring NPC needs Magic high relative
    // to the others. Use to harvest sensible vanilla values when authoring a custom CombatStyle.
    private static int CstyDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var c in mod.EnumerateMajorRecords<ICombatStyleGetter>())
        {
            if (c.FormKey.ID != id) continue;
            Console.WriteLine($"0x{id:X6}  EditorID={c.EditorID}");
            Console.WriteLine($"  OffensiveMult={c.OffensiveMult}  DefensiveMult={c.DefensiveMult}  GroupOffensiveMult={c.GroupOffensiveMult}");
            Console.WriteLine($"  EquipMult: Melee={c.EquipmentScoreMultMelee}  Magic={c.EquipmentScoreMultMagic}  Ranged={c.EquipmentScoreMultRanged}");
            Console.WriteLine($"             Shout={c.EquipmentScoreMultShout}  Unarmed={c.EquipmentScoreMultUnarmed}  Staff={c.EquipmentScoreMultStaff}");
            Console.WriteLine($"  AvoidThreatChance={c.AvoidThreatChance}");
            Console.WriteLine($"  Flags={c.Flags?.ToString() ?? "-"}   MajorFlags={c.MajorFlags}");
            Console.WriteLine($"  LongRangeStrafeMult={c.LongRangeStrafeMult?.ToString() ?? "-"}");
            Console.WriteLine($"  Melee sub: {(c.Melee is null ? "-" : "set")}   CloseRange sub: {(c.CloseRange is null ? "-" : "set")}   Flight sub: {(c.Flight is null ? "-" : "set")}");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a CombatStyle in {Path.GetFileName(inPath)}");
        return 0;
    }
}
