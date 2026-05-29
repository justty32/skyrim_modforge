internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  dump — read a plugin back and print its records + the key things generation
    //  wires up (names, npc faction membership, VMAD scripts, dialogue, quest
    //  objectives). Round-trip verification helper + a way to inspect any .esp.
    // -------------------------------------------------------------------------------
    // Search a (possibly huge, e.g. Skyrim.esm) plugin for records whose EditorID or Name
    // contains <query> (case-insensitive). Reads via a lazy read-only OVERLAY so a 250 MB
    // master doesn't get fully materialized. Prints a resolver-ready "<master>:0xFORMID" ref,
    // the record type, EditorID and Name. Optional [type] (e.g. Weapon, Npc, Keyword) filters
    // by record kind, letting the overlay skip whole groups instead of parsing everything.
    private static int Find(string inPath, string query, string? typeName)
    {
        // Vanilla masters are localized: Name is a string index whose text lives in BSA-packed
        // .STRINGS. Point the strings reader at the plugin's own Data folder (BSA override) so it
        // resolves names WITHOUT the game-environment/plugins.txt lookup (absent on Linux).
        var dataDir = Path.GetDirectoryName(Path.GetFullPath(inPath))!;
        var readParams = new BinaryReadParameters
        {
            StringsParam = new StringsReadParameters
            {
                BsaFolderOverride = dataDir,
                StringsFolderOverride = dataDir,
                TargetLanguage = Language.English,
            },
        };
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE, readParams);

        IEnumerable<IMajorRecordGetter> records;
        if (!string.IsNullOrEmpty(typeName))
        {
            var t = typeof(ISkyrimModGetter).Assembly
                .GetType($"Mutagen.Bethesda.Skyrim.I{typeName}Getter", throwOnError: false, ignoreCase: true);
            if (t is null)
            {
                Console.Error.WriteLine(
                    $"Unknown record type '{typeName}'. Examples: Weapon, Armor, Ammunition, Npc, " +
                    "MiscItem, Ingredient, Ingestible, Book, Key, SoulGem, Keyword, Race, Class, " +
                    "Faction, Spell, MagicEffect, Perk, Outfit, LeveledItem, LeveledNpc, Location, Cell, Furniture.");
                return 2;
            }
            records = mod.EnumerateMajorRecords(t, throwIfUnknown: false);
        }
        else
        {
            records = mod.EnumerateMajorRecords();
        }

        // Name is a localized string (BSA-packed for vanilla); resolving it needs the game's
        // archive load order, which isn't available headless on Linux. EditorID + FormID are
        // stored inline and always read. So resolve Name best-effort: on the first failure,
        // stop trying (deterministic) and search EditorID only.
        bool namesOk = true;
        string? NameOf(IMajorRecordGetter r)
        {
            if (!namesOk) return null;
            try { return (r as INamedGetter)?.Name; }
            catch { namesOk = false; return null; }
        }

        var q = query.ToLowerInvariant();
        const int cap = 300;
        int total = 0, shown = 0;
        foreach (var r in records)
        {
            var ed = r.EditorID;
            var name = NameOf(r);
            bool hit = (ed is { } e && e.ToLowerInvariant().Contains(q))
                    || (name is { } n && n.ToLowerInvariant().Contains(q));
            if (!hit) continue;
            total++;
            if (shown++ < cap)
            {
                var fk = r.FormKey;
                Console.WriteLine($"{fk.ModKey}:0x{fk.ID:X6}  {TypeLabel(r)}  {ed}"
                    + (name is { } nm ? $"  \"{nm}\"" : ""));
            }
        }
        Console.WriteLine($"-- {total} match(es)" + (total > cap ? $", showing first {cap}" : "")
            + (namesOk ? "" : "  [names unresolved: search matched EditorID only — see note]"));
        return 0;
    }

    // Diagnostic: walk a plugin's interior CELL block tree and print the block/sub-block each
    // interior cell lives in. Skyrim groups interior cells BY FORMID (block = id % 10, sub =
    // (id/10) % 10); an override in the wrong GRUP is silently ignored by the engine, so this is
    // how you verify a vanilla-cell override landed in the right block WITHOUT an in-game cycle.
    // Optional 0xFORMID arg filters to one cell.
    private static int CellBlk(string inPath, string? formIdHex)
    {
        uint? target = null;
        if (formIdHex is not null)
            target = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        int shown = 0;
        foreach (var block in mod.Cells.Records)
        {
            foreach (var sub in block.SubBlocks)
            {
                foreach (var c in sub.Cells)
                {
                    uint id = c.FormKey.ID;
                    if (target is { } t && id != t) continue;
                    if (target is null && shown >= 60) { Console.WriteLine("…(capped at 60)"); return 0; }
                    Console.WriteLine($"0x{id:X6} (dec {id})  block={block.BlockNumber} sub={sub.BlockNumber}  {c.EditorID}"
                        + $"   [id%10={id % 10}, (id/10)%10={(id / 10) % 10}]");
                    shown++;
                }
            }
        }
        if (target is not null && shown == 0) Console.WriteLine($"0x{target:X6} not found as an interior cell");
        return 0;
    }

    // Diagnostic: print a MagicEffect's full functional field set from any plugin, to compare a
    // generated MGEF against a vanilla one (this is how the It.12 "Recover flag cancels an instant
    // heal" bug was found). Avoids Name/Description (localized string landmine on master overlays).
    private static int MgefDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var r in mod.EnumerateMajorRecords<IMagicEffectGetter>())
        {
            if (r.FormKey.ID != id) continue;
            string F(IFormLinkGetter<IMajorRecordGetter> l) => l.FormKey.IsNull ? "-" : l.FormKey.ToString();
            Console.WriteLine($"0x{id:X6}  EditorID={r.EditorID}");
            Console.WriteLine($"  archetype.Type = {r.Archetype.Type}");
            Console.WriteLine($"  archetype.ActorValue = {r.Archetype.ActorValue}");
            Console.WriteLine($"  archetype.Association = {(r.Archetype.AssociationKey.FormKey.IsNull ? "-" : r.Archetype.AssociationKey.FormKey.ToString())}");
            Console.WriteLine($"  Flags = {r.Flags}");
            Console.WriteLine($"  BaseCost = {r.BaseCost}");
            Console.WriteLine($"  MagicSkill = {r.MagicSkill}   ResistValue = {r.ResistValue}   SecondActorValue = {r.SecondActorValue}");
            Console.WriteLine($"  CastType = {r.CastType}   TargetType = {r.TargetType}");
            Console.WriteLine($"  TaperWeight={r.TaperWeight} TaperCurve={r.TaperCurve} TaperDuration={r.TaperDuration} SkillUsageMult={r.SkillUsageMultiplier}");
            Console.WriteLine($"  MenuDisplayObject={F(r.MenuDisplayObject)} CastingArt={F(r.CastingArt)} HitEffectArt={F(r.HitEffectArt)} Projectile={F(r.Projectile)} Explosion={F(r.Explosion)}");
            Console.WriteLine($"  Keywords={(r.Keywords is null ? "-" : string.Join(",", r.Keywords.Select(k => k.FormKey.ToString())))}");
            Console.WriteLine($"  PerkToApply={F(r.PerkToApply)} EquipAbility={F(r.EquipAbility)} Conditions={r.Conditions.Count}");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a MagicEffect in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Diagnostic: print a Light's radius/color/flags (one 0xFORMID) — or, with no FormID, list every
    // Light that's a decent general ROOM fill (big radius, omnidirectional, on by default, not carried)
    // so we can pick a believable interior light for a generated cell.
    private static int LightDiag(string inPath, string? formIdHex)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        uint? target = formIdHex is null ? null
            : Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        int shown = 0;
        foreach (var l in mod.EnumerateMajorRecords<ILightGetter>())
        {
            if (target is { } t) { if (l.FormKey.ID != t) continue; }
            else
            {
                // room-fill heuristic: radius >= 512, not carried/spot/off-by-default
                bool carried = l.Flags.HasFlag(Light.Flag.CanBeCarried);
                bool spot = l.Flags.HasFlag(Light.Flag.SpotLight) || l.Flags.HasFlag(Light.Flag.ShadowSpotlight);
                bool off = l.Flags.HasFlag(Light.Flag.OffByDefault);
                if (l.Radius < 512 || carried || spot || off) continue;
                if (shown++ >= 40) { Console.WriteLine("…(capped)"); break; }
            }
            Console.WriteLine($"0x{l.FormKey.ID:X6}  {l.EditorID,-34} radius={l.Radius,4} "
                + $"color=({l.Color.R},{l.Color.G},{l.Color.B}) fade={l.FadeValue} flags={l.Flags}");
            if (target is not null) return 0;
        }
        if (target is not null) Console.WriteLine($"0x{target:X6} not a Light in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Diagnostic: print a placed reference's (REFR/ACHR) position + rotation + base form, by FormID.
    // Position is cell-LOCAL for interiors, WORLD coords for exteriors. Used to anchor new placements
    // (e.g. patrol markers) at a point KNOWN to be on navmesh — copy a vanilla reachable ref's coords
    // rather than guessing, since static markers don't snap to the floor the way actors do.
    private static int RefPos(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var r in mod.EnumerateMajorRecords<IPlacedGetter>())
        {
            if (r.FormKey.ID != id) continue;
            var (pos, rot, baseFk, kind) = r switch
            {
                IPlacedObjectGetter o => (o.Placement?.Position, o.Placement?.Rotation, o.Base.FormKey, "PlacedObject (REFR)"),
                IPlacedNpcGetter a    => (a.Placement?.Position, a.Placement?.Rotation, a.Base.FormKey, "PlacedNpc (ACHR)"),
                _ => ((Noggog.P3Float?)null, (Noggog.P3Float?)null, default(FormKey), "Placed"),
            };
            Console.WriteLine($"0x{id:X6}  {kind}  EditorID={r.EditorID ?? "-"}");
            Console.WriteLine($"  base = {baseFk}");
            if (pos is { } p) Console.WriteLine($"  position = ({p.X:0.##}, {p.Y:0.##}, {p.Z:0.##})  (cell-local for interiors, world for exteriors)");
            if (rot is { } ro) Console.WriteLine($"  rotation = ({ro.X:0.###}, {ro.Y:0.###}, {ro.Z:0.###}) rad");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a placed reference in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Diagnostic: list all packages in a master whose PackageTemplate FormID matches a target.
    // Used to find vanilla CONCRETE packages that use a given procedure template (Sandbox /
    // Travel / UseMagic / …) so a new spec author can copy their slot patterns. Necessary because
    // `find` only matches EditorIDs — a template-based package often has no template name in its ID.
    private static int PkgsByTemplate(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        int hits = 0;
        foreach (var p in mod.EnumerateMajorRecords<IPackageGetter>())
        {
            if (p.PackageTemplate.FormKey.ID != id) continue;
            Console.WriteLine($"  {p.FormKey}  {p.EditorID}  type={p.Type}  slots={p.Data.Count}  flags={p.Flags}");
            hits++;
        }
        Console.WriteLine($"-- {hits} package(s) with PackageTemplate=0x{id:X6} in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Diagnostic: print a Package's template / flags / interrupt flags / schedule / refs and,
    // crucially, its Data dictionary — each entry's sbyte key, Name, concrete subtype
    // (PackageDataLocation/Float/Bool/Int/Target/…) and its key field(s). Used to learn the
    // input schema of a vanilla TEMPLATE (Sandbox / Travel / Find / UseItemAt / EatSleep …)
    // so a spec can author the right inputs.
    private static int PackageDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var p in mod.EnumerateMajorRecords<IPackageGetter>())
        {
            if (p.FormKey.ID != id) continue;
            string F(IFormLinkGetter<IMajorRecordGetter> l) => l.FormKey.IsNull ? "-" : l.FormKey.ToString();
            Console.WriteLine($"0x{id:X6}  EditorID={p.EditorID}");
            Console.WriteLine($"  Type = {p.Type}");
            Console.WriteLine($"  PackageTemplate -> {(p.PackageTemplate.FormKey.IsNull ? "-" : p.PackageTemplate.FormKey.ToString())}");
            Console.WriteLine($"  Flags = {p.Flags}");
            Console.WriteLine($"  InterruptFlags = {p.InterruptFlags}");
            Console.WriteLine($"  InterruptOverride = {p.InterruptOverride}   PreferredSpeed = {p.PreferredSpeed}");
            Console.WriteLine($"  Schedule: month={p.ScheduleMonth} dayOfWeek={p.ScheduleDayOfWeek} date={p.ScheduleDate} "
                + $"hour={p.ScheduleHour} minute={p.ScheduleMinute} durationMin={p.ScheduleDurationInMinutes}");
            Console.WriteLine($"  CombatStyle = {F(p.CombatStyle)}   OwnerQuest = {F(p.OwnerQuest)}");
            Console.WriteLine($"  Conditions = {p.Conditions.Count}");
            Console.WriteLine($"  DataInputVersion = {p.DataInputVersion}");
            Console.WriteLine($"  Unknown={p.Unknown} Unknown2={p.Unknown2} Unknown3.len={p.Unknown3.Length} Unknown4={p.Unknown4?.ToString() ?? "-"}");
            Console.WriteLine($"  XnamMarker.len={p.XnamMarker.Length}");
            Console.WriteLine($"  Data ({p.Data.Count} entry/entries):");
            foreach (var kv in p.Data)
            {
                var d = kv.Value;
                var concrete = d.GetType().Name;
                foreach (var suf in new[] { "BinaryOverlay", "Getter" }) if (concrete.EndsWith(suf)) concrete = concrete[..^suf.Length];
                var extra = "";
                switch (d)
                {
                    case IPackageDataLocationGetter loc:
                        var t = loc.Location.Target;
                        var ttype = t.GetType().Name;
                        foreach (var suf in new[] { "BinaryOverlay", "Getter" }) if (ttype.EndsWith(suf)) ttype = ttype[..^suf.Length];
                        var tlink = (t as ILocationTargetGetter)?.Link.FormKey;
                        var fbk = (t as ILocationFallbackGetter);
                        extra = $" radius={loc.Location.Radius} target={ttype}"
                            + (tlink is { } fk && !fk.IsNull ? $"({fk})" : "")
                            + (fbk is not null ? $"(type={fbk.Type},data={fbk.Data})" : "");
                        break;
                    case IPackageDataFloatGetter f: extra = $" value={f.Data}"; break;
                    case IPackageDataIntGetter i:   extra = $" value={i.Data}"; break;
                    case IPackageDataBoolGetter b:  extra = $" value={b.Data}"; break;
                    case IPackageDataTargetGetter tg:
                        var tgt = tg.Target.GetType().Name;
                        foreach (var suf in new[] { "BinaryOverlay", "Getter" }) if (tgt.EndsWith(suf)) tgt = tgt[..^suf.Length];
                        // Print the concrete target's key field — used to confirm a built UseMagic
                        // slot 3 ("Spell") got the right TargetObjectType enum, slot 4 ("Target")
                        // points at the right placed ref, etc.
                        var inner = tg.Target switch
                        {
                            IPackageTargetObjectTypeGetter ot       => $"({ot.Type})",
                            IPackageTargetObjectIDGetter      oid   => oid.Reference.FormKey.IsNull ? "" : $"({oid.Reference.FormKey})",
                            IPackageTargetSpecificReferenceGetter s => s.Reference.FormKey.IsNull   ? "" : $"({s.Reference.FormKey})",
                            IPackageTargetLinkedReferenceGetter  lk => lk.Keyword.FormKey.IsNull    ? "" : $"(keyword={lk.Keyword.FormKey})",
                            IPackageTargetSelfGetter          self  => "(self)",
                            _                                       => "",
                        };
                        extra = $" type={tg.Type} target={tgt}{inner}";
                        break;
                    case IPackageDataTopicGetter tp: extra = $" topics={tp.Topics.Count}"; break;
                    case IPackageDataObjectListGetter ol: extra = $" data={ol.Data}"; break;
                }
                Console.WriteLine($"    [{kv.Key,3}] {concrete}  Name=\"{d.Name}\"  Flags={d.Flags}{extra}");
            }
            Console.WriteLine($"  ProcedureTree: {p.ProcedureTree.Count} branch(es)");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Package in {Path.GetFileName(inPath)}");
        return 0;
    }

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

    // Concrete Mutagen record class -> friendly type name (strip overlay/getter suffixes).
    private static string TypeLabel(IMajorRecordGetter r)
    {
        var n = r.GetType().Name;
        foreach (var suf in new[] { "BinaryOverlay", "Getter" })
            if (n.EndsWith(suf)) n = n[..^suf.Length];
        return n;
    }

    private static int Dump(string inPath)
    {
        var mod = Load(inPath);
        var edByFk = new Dictionary<FormKey, string>();
        foreach (var r in mod.EnumerateMajorRecords())
            if (!string.IsNullOrEmpty(r.EditorID)) edByFk[r.FormKey] = r.EditorID!;
        string Ref(FormKey fk) => fk.IsNull ? "<null>" : edByFk.TryGetValue(fk, out var ed) ? ed : fk.ToString();

        var masters = mod.MasterReferences;
        Console.WriteLine($"{Path.GetFileName(inPath)} — {mod.EnumerateMajorRecords().Count()} record(s), "
            + $"localized={mod.UsingLocalization}, master(s)=[{string.Join(", ", masters.Select(m => m.Master.FileName.ToString()))}]");
        foreach (var r in mod.EnumerateMajorRecords())
        {
            var name = (r as INamedGetter)?.Name;
            Console.WriteLine($"  [{r.FormKey}] {r.GetType().Name} {r.EditorID}" + (name is { } nm ? $"  \"{nm}\"" : ""));

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
            }

            if (r is IBookGetter bk && bk.Model?.File is { } bmf)
                Console.WriteLine($"      model={bmf}");                                       // null model => CRASH on read

            if (r is IArmorGetter arm && arm.BodyTemplate is { } bt)
                Console.WriteLine($"      armorRating={arm.ArmorRating} armorType={bt.ArmorType} slots=[{bt.FirstPersonFlags}]");

            if (r is IHasEffectsGetter eff && eff.Effects.Count > 0)
                foreach (var e in eff.Effects)
                    Console.WriteLine($"      effect -> {Ref(e.BaseEffect.FormKey)} (mag={e.Data?.Magnitude} area={e.Data?.Area} dur={e.Data?.Duration})");

            if (r is IWorldspaceGetter wg)
            {
                int blocks = wg.SubCells.Count;
                int cells = wg.SubCells.SelectMany(b => b.Items).SelectMany(s => s.Items).Count();
                Console.WriteLine($"      worldspace: {blocks} block(s), {cells} exterior cell(s)"
                    + $" nameSet={wg.Name is not null}"
                    + (wg.LandDefaults is { } wld ? $" defaultWater={wld.DefaultWaterHeight}" : " defaultWater=<none>"));
            }

            if (r is ICellGetter cg)
                Console.WriteLine($"      cell: interior={cg.Flags.HasFlag(Cell.Flag.IsInteriorCell)}"
                    + (cg.Grid?.Point is { } gp ? $" grid=({gp.X},{gp.Y})" : "")
                    + (cg.WaterHeight is { } wh ? $" water={wh}" : " water=<none>")
                    + (cg.LightingTemplate.IsNull ? "" : $" lightTmpl={cg.LightingTemplate.FormKey}")
                    + $" persistent={cg.Persistent.Count} temporary={cg.Temporary.Count}");

            if (r is IPlacedNpcGetter pnpc && pnpc.Placement is { } pp)
            {
                Console.WriteLine($"      placed npc -> base {Ref(pnpc.Base.FormKey)} @ ({pp.Position.X:0.#}, {pp.Position.Y:0.#}, {pp.Position.Z:0.#})");
                foreach (var lr in pnpc.LinkedReferences) Console.WriteLine($"        linkedRef -> {Ref(lr.Reference.FormKey)}{(lr.KeywordOrReference.IsNull ? "" : $" (keyword {lr.KeywordOrReference.FormKey})")}");
            }

            if (r is IPlacedObjectGetter pobj && pobj.Placement is { } op)
            {
                Console.WriteLine($"      placed obj -> base {Ref(pobj.Base.FormKey)} @ ({op.Position.X:0.#}, {op.Position.Y:0.#}, {op.Position.Z:0.#})");
                foreach (var lr in pobj.LinkedReferences) Console.WriteLine($"        linkedRef -> {Ref(lr.Reference.FormKey)}{(lr.KeywordOrReference.IsNull ? "" : $" (keyword {lr.KeywordOrReference.FormKey})")}");
            }

            if (r is ILeveledItemGetter lvli && lvli.Entries is { Count: > 0 } lies)
                foreach (var e in lies) if (e.Data is { } d) Console.WriteLine($"      lvli entry -> {Ref(d.Reference.FormKey)} (lvl {d.Level} x{d.Count})");

            if (r is ILeveledNpcGetter lvln && lvln.Entries is { Count: > 0 } lnes)
                foreach (var e in lnes) if (e.Data is { } d) Console.WriteLine($"      lvln entry -> {Ref(d.Reference.FormKey)} (lvl {d.Level} x{d.Count})");

            if (r is IContainerGetter contG && contG.Items is { Count: > 0 } items)
                foreach (var e in items) Console.WriteLine($"      contains -> {Ref(e.Item.Item.FormKey)} x{e.Item.Count}");

            if (r is IConstructibleObjectGetter cobj)
            {
                Console.WriteLine($"      recipe: makes {cobj.CreatedObjectCount ?? 1}x {Ref(cobj.CreatedObject.FormKey)}"
                    + $" at {Ref(cobj.WorkbenchKeyword.FormKey)}");
                if (cobj.Items is { } comps)
                    foreach (var c in comps) Console.WriteLine($"        component -> {Ref(c.Item.Item.FormKey)} x{c.Item.Count}");
            }

            if (r is ISpellGetter spG && (spG.Type != SpellType.Spell || spG.CastType != CastType.ConstantEffect || spG.BaseCost > 0))
                Console.WriteLine($"      spell: type={spG.Type} cast={spG.CastType} target={spG.TargetType} cost={spG.BaseCost}"
                    + (spG.EquipmentType.IsNull ? "" : $" equip={Ref(spG.EquipmentType.FormKey)}"));

            if (r is ICombatStyleGetter csG)
            {
                Console.WriteLine($"      cs: off={csG.OffensiveMult} def={csG.DefensiveMult} group={csG.GroupOffensiveMult}"
                    + $" equip(melee={csG.EquipmentScoreMultMelee} magic={csG.EquipmentScoreMultMagic} ranged={csG.EquipmentScoreMultRanged}"
                    + $" shout={csG.EquipmentScoreMultShout} unarmed={csG.EquipmentScoreMultUnarmed} staff={csG.EquipmentScoreMultStaff})"
                    + $" avoid={csG.AvoidThreatChance} flags={csG.Flags?.ToString() ?? "-"}");
            }

            if (r is IPackageGetter pkgG)
            {
                var tmpl = pkgG.PackageTemplate.FormKey;
                Console.WriteLine($"      package: type={pkgG.Type} template={(tmpl.IsNull ? "-" : Ref(tmpl))}"
                    + $" flags={pkgG.Flags} interrupt={pkgG.InterruptFlags} speed={pkgG.PreferredSpeed}"
                    + $" schedule(h={pkgG.ScheduleHour} m={pkgG.ScheduleMinute} dur={pkgG.ScheduleDurationInMinutes} dow={pkgG.ScheduleDayOfWeek})"
                    + $" data={pkgG.Data.Count} slot(s)"
                    + (pkgG.CombatStyle.FormKey.IsNull ? "" : $" cs={Ref(pkgG.CombatStyle.FormKey)}")
                    + (pkgG.OwnerQuest.FormKey.IsNull ? "" : $" quest={Ref(pkgG.OwnerQuest.FormKey)}"));
            }

            if (r is IClassGetter cls)
            {
                var stats = string.Join(",", cls.StatWeights.Select(kv => $"{kv.Key}:{kv.Value}"));
                var skills = string.Join(",", cls.SkillWeights.Where(kv => kv.Value > 0).Select(kv => $"{kv.Key}:{kv.Value}"));
                Console.WriteLine($"      class: teaches={cls.Teaches?.ToString() ?? "-"} maxTrain={cls.MaxTrainingLevel} stats=[{stats}] skills=[{skills}]");
            }

            if (r is IMagicEffectGetter mgef)
            {
                var assoc = mgef.Archetype.AssociationKey.FormKey;
                Console.WriteLine($"      mgef: archetype={mgef.Archetype.Type} av={mgef.Archetype.ActorValue} skill={mgef.MagicSkill}"
                    + $" resist={mgef.ResistValue} cast={mgef.CastType} target={mgef.TargetType} cost={mgef.BaseCost} flags={mgef.Flags}"
                    + (assoc.IsNull ? "" : $" assoc={Ref(assoc)}"));
            }

            if (r is IAmmunitionGetter ammo)
                Console.WriteLine($"      ammo: damage={ammo.Damage} value={ammo.Value} weight={ammo.Weight}");

            if (r is IScrollGetter scrl)
                Console.WriteLine($"      scroll: type={scrl.Type} cast={scrl.CastType} target={scrl.TargetType} cost={scrl.BaseCost} value={scrl.Value}");

            if (r is ISoulGemGetter slgm)
                Console.WriteLine($"      soulgem: capacity={slgm.MaximumCapacity} value={slgm.Value}");

            if (r is IOutfitGetter otft && otft.Items is { Count: > 0 } oitems)
                foreach (var it in oitems) Console.WriteLine($"      outfit item -> {Ref(it.FormKey)}");

            if ((r is IStaticGetter || r is IActivatorGetter) && r is IModeledGetter mdl && mdl.Model?.File is { } mf)
                Console.WriteLine($"      model: {mf.GivenPath}");

            if ((r is IMiscItemGetter || r is IIngestibleGetter) && r is IModeledGetter im && im.Model?.File is { } imf)
                Console.WriteLine($"      model={imf}");      // null model => no 3D mesh when dropped

            if (r is IHaveVirtualMachineAdapterGetter hv && hv.VirtualMachineAdapter is { } vm)
                foreach (var se in vm.Scripts)
                    Console.WriteLine($"      script: {se.Name} [{se.Properties.Count} prop(s)]");

            if (r is IDialogTopicGetter dt)
                Console.WriteLine($"      topic: prompt=\"{dt.Name?.String}\"  category={dt.Category}  subtype={dt.Subtype}  quest={Ref(dt.Quest.FormKey)}  branch={Ref(dt.Branch.FormKey)}  ({dt.Responses.Count} INFO group(s))");

            if (r is IDialogBranchGetter db)
                Console.WriteLine($"      branch: category={db.Category}  flags={db.Flags?.ToString() ?? "-"}  quest={Ref(db.Quest.FormKey)}  startingTopic={Ref(db.StartingTopic.FormKey)}");

            if (r is IDialogResponsesGetter info)
            {
                foreach (var resp in info.Responses)
                    Console.WriteLine($"      response[{resp.ResponseNumber}] ({resp.Emotion}): \"{resp.Text?.String}\"");
                foreach (var c in info.Conditions)
                {
                    // Surface the GetIsID speaker gate (the usual "only this NPC says it" condition).
                    var data = (c as IConditionFloatGetter)?.Data;
                    var tgt = (data as IGetIsIDConditionDataGetter)?.Object.Link.FormKey;
                    Console.WriteLine($"      condition: {data?.GetType().Name ?? c.GetType().Name}{(tgt is { } fk ? $" -> {fk}" : "")}");
                }
            }

            if (r is IRelationshipGetter rel)
                Console.WriteLine($"      relationship: parent={Ref(rel.Parent.FormKey)}  child={Ref(rel.Child.FormKey)}  rank={rel.Rank}");

            if (r is IQuestGetter q)
            {
                Console.WriteLine($"      quest: flags={q.Flags}  priority={q.Priority}");
                foreach (var o in q.Objectives)
                    Console.WriteLine($"      objective[{o.Index}]: \"{o.DisplayText?.String}\"");
            }
        }
        return 0;
    }
}
