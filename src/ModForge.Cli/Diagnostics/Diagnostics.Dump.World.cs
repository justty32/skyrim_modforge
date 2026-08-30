internal static partial class Program
{
    // World-structure portion of the per-record detail chain (extracted from
    // DumpRecordInventoryAndWorld). Covers worldspace/region/cell/encounterZone,
    // placements (placed NPC + placed object with linked refs/teleport), and
    // leveled/container/recipe lists.
    private static void DumpRecordWorld(
        IMajorRecordGetter r, Func<FormKey, string> Ref,
        Dictionary<FormKey, string> edByFk, HashSet<FormKey> inSpecLvln)
    {
        if (r is IWorldspaceGetter wg)
        {
            int blocks = wg.SubCells.Count;
            int cells = wg.SubCells.SelectMany(b => b.Items).SelectMany(s => s.Items).Count();
            Console.WriteLine($"      worldspace: {blocks} block(s), {cells} exterior cell(s)"
                + $" nameSet={wg.Name is not null}"
                + (wg.LandDefaults is { } wld ? $" defaultLand={wld.DefaultLandHeight} defaultWater={wld.DefaultWaterHeight}" : " defaultWater=<none>"));
            if (!wg.Climate.IsNull) Console.WriteLine($"      climate -> {Ref(wg.Climate.FormKey)}");
            if (!wg.Water.IsNull) Console.WriteLine($"      water -> {Ref(wg.Water.FormKey)}");
            if (!wg.LodWater.IsNull) Console.WriteLine($"      lodWater -> {Ref(wg.LodWater.FormKey)}");
            if (wg.Parent?.Worldspace is { IsNull: false } pw) Console.WriteLine($"      parent -> {Ref(pw.FormKey)}");
            if (!wg.Music.IsNull) Console.WriteLine($"      music -> {Ref(wg.Music.FormKey)}");
            if (wg.MapData is { } wmd) Console.WriteLine($"      map: nw={wmd.NorthwestCellCoords} se={wmd.SoutheastCellCoords} pitch={wmd.CameraInitialPitch} camH={wmd.CameraMinHeight}..{wmd.CameraMaxHeight}");
        }

        if (r is IRegionGetter rgn)
        {
            Console.WriteLine($"      region: worldspace -> {(rgn.Worldspace.IsNull ? "<none>" : Ref(rgn.Worldspace.FormKey))}"
                + (rgn.MapColor is { } mc ? $" mapColor=#{mc.R:X2}{mc.G:X2}{mc.B:X2}" : "")
                + $" area(s)={rgn.RegionAreas.Count}");
            foreach (var a in rgn.RegionAreas)
                Console.WriteLine($"        area: {a.RegionPointListData?.Count ?? 0} point(s) edgeFallOff={a.EdgeFallOff}");
            if (rgn.Weather is { Weathers: { Count: > 0 } wls } rw)
            {
                Console.WriteLine($"        weather: priority={rw.Priority} {wls.Count} entry(s)");
                foreach (var we in wls)
                    Console.WriteLine($"          weather -> {Ref(we.Weather.FormKey)} (chance {we.Chance})");
            }
        }

        if (r is ICellGetter cg)
            Console.WriteLine($"      cell: interior={cg.Flags.HasFlag(Cell.Flag.IsInteriorCell)}"
                + (cg.Grid?.Point is { } gp ? $" grid=({gp.X},{gp.Y})" : "")
                + (cg.WaterHeight is { } wh ? $" water={wh}" : " water=<none>")
                + (cg.LightingTemplate.IsNull ? "" : $" lightTmpl={cg.LightingTemplate.FormKey}")
                + (cg.ImageSpace.IsNull ? "" : $" imageSpace={cg.ImageSpace.FormKey}")
                + (cg.EncounterZone.IsNull ? "" : $" encZone -> {Ref(cg.EncounterZone.FormKey)}")
                + $" persistent={cg.Persistent.Count} temporary={cg.Temporary.Count}");

        if (r is IEncounterZoneGetter ecz)
        {
            var maxStr = ecz.MaxLevel == 0 ? "uncapped" : ecz.MaxLevel.ToString();
            Console.WriteLine($"      encZone: levels [{ecz.MinLevel}..{maxStr}] rank={ecz.Rank} flags={ecz.Flags}"
                + (ecz.Owner.IsNull ? "" : $" owner -> {Ref(ecz.Owner.FormKey)}")
                + (ecz.Location.IsNull ? "" : $" location -> {Ref(ecz.Location.FormKey)}"));
        }

        if (r is IPlacedNpcGetter pnpc && pnpc.Placement is { } pp)
        {
            // A leveled-actor spawn = ACHR whose base is a LeveledNpc. Detected by in-spec LVLN
            // membership, or (for a vanilla base, whose record we can't see) the LChar* naming.
            bool lvlBase = inSpecLvln.Contains(pnpc.Base.FormKey)
                || (edByFk.TryGetValue(pnpc.Base.FormKey, out var bed) && bed.StartsWith("LChar", StringComparison.OrdinalIgnoreCase));
            Console.WriteLine($"      placed npc -> base {Ref(pnpc.Base.FormKey)}{(lvlBase ? " (LEVELED spawn)" : "")} @ ({pp.Position.X:0.#}, {pp.Position.Y:0.#}, {pp.Position.Z:0.#})"
                + (pnpc.EncounterZone.IsNull ? "" : $"  encZone -> {Ref(pnpc.EncounterZone.FormKey)}"));
            foreach (var lr in pnpc.LinkedReferences) Console.WriteLine($"        linkedRef -> {Ref(lr.Reference.FormKey)}{(lr.KeywordOrReference.IsNull ? "" : $" (keyword {lr.KeywordOrReference.FormKey})")}");
        }

        if (r is IPlacedObjectGetter pobj && pobj.Placement is { } op)
        {
            Console.WriteLine($"      placed obj -> base {Ref(pobj.Base.FormKey)} @ ({op.Position.X:0.#}, {op.Position.Y:0.#}, {op.Position.Z:0.#})"
                + (pobj.EncounterZone.IsNull ? "" : $"  encZone -> {Ref(pobj.EncounterZone.FormKey)}"));
            foreach (var lr in pobj.LinkedReferences) Console.WriteLine($"        linkedRef -> {Ref(lr.Reference.FormKey)}{(lr.KeywordOrReference.IsNull ? "" : $" (keyword {lr.KeywordOrReference.FormKey})")}");
            if (pobj.TeleportDestination is { } td)   // load-door XTEL: partner door + arrival point
                Console.WriteLine($"        teleport -> door {Ref(td.Door.FormKey)} arrive @ ({td.Position.X:0.#}, {td.Position.Y:0.#}, {td.Position.Z:0.#}) rot ({td.Rotation.X:0.###}, {td.Rotation.Y:0.###}, {td.Rotation.Z:0.###})");
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
            foreach (var cond in cobj.Conditions)
            {
                string p1 = cond.Data switch
                {
                    IHasPerkConditionDataGetter hp        => $" perk={Ref(hp.Perk.Link.FormKey)}",
                    IGetItemCountConditionDataGetter gic   => $" item={Ref(gic.ItemOrList.Link.FormKey)}",
                    IGetGlobalValueConditionDataGetter ggv => $" global={Ref(ggv.Global.Link.FormKey)}",
                    _ => "",
                };
                string cmp = cond is IConditionFloatGetter cf ? $" {cond.CompareOperator} {cf.ComparisonValue}" : "";
                Console.WriteLine($"        condition -> {cond.Data.Function}{cmp}{p1}{(cond.Flags.HasFlag(Condition.Flag.OR) ? " [OR]" : "")}");
            }
        }
    }
}
