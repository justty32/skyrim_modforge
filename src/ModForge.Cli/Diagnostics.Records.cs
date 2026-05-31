internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  Targeted single-record diagnostics. Each reads a plugin via a lazy read-only
    //  OVERLAY (so a 250 MB master isn't fully materialized) and prints the functional
    //  field set of one record — to diff a generated record against a vanilla one, or to
    //  harvest sensible vanilla values when authoring a new spec. None resolve localized
    //  Name (a string landmine on master overlays); all key on the 24-bit FormID.
    // -------------------------------------------------------------------------------

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
            // Load-door teleport (XTEL): the partner door it links to + where the player materialises.
            // Probe a vanilla door pair this way to copy a proven-walkable arrival point.
            if (r is IPlacedObjectGetter po && po.TeleportDestination is { } td)
            {
                Console.WriteLine($"  teleport -> door {td.Door.FormKey}");
                Console.WriteLine($"    arrive position = ({td.Position.X:0.##}, {td.Position.Y:0.##}, {td.Position.Z:0.##})");
                Console.WriteLine($"    arrive rotation = ({td.Rotation.X:0.###}, {td.Rotation.Y:0.###}, {td.Rotation.Z:0.###}) rad");
            }
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
}
