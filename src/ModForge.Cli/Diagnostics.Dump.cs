internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  dump — read a plugin back and print its records + the key things generation
    //  wires up (names, npc faction membership, VMAD scripts, dialogue, quest
    //  objectives). Round-trip verification helper + a way to inspect any .esp.
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

        var masters = mod.MasterReferences;
        Console.WriteLine($"{Path.GetFileName(inPath)} — {records.Count} record(s), "
            + $"localized={mod.UsingLocalization}, master(s)=[{string.Join(", ", masters.Select(m => m.Master.FileName.ToString()))}]");
        foreach (var r in records)
        {
            var name = (r as INamedGetter)?.Name;
            // Overlay getters report type names like "BookBinaryOverlay" — trim the suffix to the record name.
            var typeName = r.GetType().Name.Replace("BinaryOverlay", "");
            Console.WriteLine($"  [{r.FormKey}] {typeName} {r.EditorID}" + (name is { } nm ? $"  \"{nm}\"" : ""));

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
                // Scene actor aliases live on the host quest — surface their NPC binding (UniqueActor).
                foreach (var al in q.Aliases.OfType<IQuestAliasGetter>())
                    if (!al.UniqueActor.FormKey.IsNull)
                        Console.WriteLine($"      alias[{al.ID}] \"{al.Name}\" -> uniqueActor {Ref(al.UniqueActor.FormKey)}");
            }

            if (r is ISceneGetter sc)
            {
                Console.WriteLine($"      scene: quest={Ref(sc.Quest.FormKey)}  flags={sc.Flags}  "
                    + $"{sc.Actors.Count} actor(s), {sc.Phases.Count} phase(s), {sc.Actions.Count} action(s)");
                foreach (var a in sc.Actors)
                    Console.WriteLine($"        actor alias #{a.ID}  behavior={a.BehaviorFlags}");
                foreach (var act in sc.Actions)
                    Console.WriteLine($"        action: {act.Type} alias #{act.ActorID} phase {act.StartPhase}"
                        + (act.Topic.FormKey.IsNull ? "" : $" -> topic {Ref(act.Topic.FormKey)}")
                        + (act.Type == SceneAction.TypeEnum.Dialog ? $" ({act.Emotion})" : ""));
            }
        }
        return 0;
    }
}
