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

            if ((r is IStaticGetter || r is IActivatorGetter || r is IFurnitureGetter) && r is IModeledGetter mdl && mdl.Model?.File is { } mf)
            {
                Console.WriteLine($"      model: {mf.GivenPath}");
                if (mdl.Model.AlternateTextures is { Count: > 0 } alts)
                    foreach (var at in alts)
                        Console.WriteLine($"        altTexture: name=\"{at.Name}\" index={at.Index} -> {Ref(at.NewTexture.FormKey)}");
            }

            if (r is ITextureSetGetter txst)
            {
                string? S(IAssetLinkGetter? a) => a?.GivenPath;
                var slots = new (string Label, string? Path)[]
                {
                    ("diffuse",     S(txst.Diffuse)),
                    ("normal",      S(txst.NormalOrGloss)),
                    ("mask",        S(txst.EnvironmentMaskOrSubsurfaceTint)),
                    ("glow",        S(txst.GlowOrDetailMap)),
                    ("height",      S(txst.Height)),
                    ("environment", S(txst.Environment)),
                    ("multilayer",  S(txst.Multilayer)),
                    ("backlight",   S(txst.BacklightMaskOrSpecular)),
                };
                Console.WriteLine($"      textureSet: flags={txst.Flags?.ToString() ?? "-"}");
                foreach (var (label, path) in slots)
                    if (!string.IsNullOrEmpty(path)) Console.WriteLine($"        {label} -> {path}");
            }

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

            if (r is IFactionGetter fact && fact.Flags.HasFlag(Faction.FactionFlag.Vendor))
            {
                Console.WriteLine($"      vendor: flags={fact.Flags}");
                if (fact.VendorValues is { } vv)
                    Console.WriteLine($"      vendorValues: hours={vv.StartHour}-{vv.EndHour} radius={vv.Radius} buysStolen={vv.OnlyBuysStolenItems} notSellBuy={vv.NotSellBuy}");
                if (!fact.VendorBuySellList.IsNull)
                    Console.WriteLine($"      sellBuyList -> {Ref(fact.VendorBuySellList.FormKey)}" + (fact.VendorValues is { NotSellBuy: true } ? " (NOT-sell)" : ""));
                if (!fact.MerchantContainer.IsNull)
                    Console.WriteLine($"      merchantContainer -> {Ref(fact.MerchantContainer.FormKey)}");
                if (fact.VendorLocation is { } vloc && vloc.Target is ILocationTargetGetter lt && !lt.Link.FormKey.IsNull)
                    Console.WriteLine($"      vendorLocation -> {Ref(lt.Link.FormKey)} (radius {vloc.Radius})");
            }

            if (r is IPerkGetter prk)
            {
                Console.WriteLine($"      perk: playable={prk.Playable} hidden={prk.Hidden} trait={prk.Trait} level={prk.Level} ranks={prk.NumRanks}"
                    + (prk.NextPerk.FormKey.IsNull ? "" : $" next={Ref(prk.NextPerk.FormKey)}")
                    + $" conditions={prk.Conditions.Count} effects={prk.Effects.Count}");
                foreach (var e in prk.Effects)
                {
                    if (e is IPerkAbilityEffectGetter ab)
                        Console.WriteLine($"        effect[ability] rank={ab.Rank} prio={ab.Priority} -> {Ref(ab.Ability.FormKey)} conds={ab.Conditions.Count}");
                    else if (e is IPerkEntryPointModifyValueGetter mv)
                        Console.WriteLine($"        effect[entryPoint] {mv.EntryPoint} {mv.Modification} {mv.Value} (rank={mv.Rank} prio={mv.Priority} conds={mv.Conditions.Count})");
                    else if (e is IAPerkEntryPointEffectGetter ep)
                        Console.WriteLine($"        effect[entryPoint] {ep.EntryPoint} ({e.GetType().Name})");
                    else
                        Console.WriteLine($"        effect[{e.GetType().Name}]");
                }
            }

            if (r is IRelationshipGetter rel)
                Console.WriteLine($"      relationship: parent={Ref(rel.Parent.FormKey)}  child={Ref(rel.Child.FormKey)}  rank={rel.Rank}");

            if (r is IWeatherGetter wthr)
            {
                int tex = wthr.CloudTextures.Count(t => t is not null);
                static string C(IWeatherColorGetter? c) => c is null ? "-"
                    : $"sr={Rgb(c.Sunrise)} day={Rgb(c.Day)} ss={Rgb(c.Sunset)} ni={Rgb(c.Night)}";
                Console.WriteLine($"      weather: flags={wthr.Flags} wind(speed={wthr.WindSpeed} dir={wthr.WindDirection * 360f:0.#}deg range={wthr.WindDirectionRange * 360f:0.#}deg)"
                    + $" {tex} cloud texture(s)"
                    + (wthr.Precipitation.FormKeyNullable is { } pk && !pk.IsNull ? $" precip={Ref(pk)}" : ""));
                Console.WriteLine($"        skyUpper: {C(wthr.SkyUpperColor)}");
                Console.WriteLine($"        fogNear:  {C(wthr.FogNearColor)}");
                Console.WriteLine($"        sun:      {C(wthr.SunColor)}");
                Console.WriteLine($"        fogDist: day(near={wthr.FogDistanceDayNear} far={wthr.FogDistanceDayFar}) night(near={wthr.FogDistanceNightNear} far={wthr.FogDistanceNightFar})");
            }

            if (r is IClimateGetter clim)
            {
                Console.WriteLine($"      climate: sunrise({clim.SunriseBegin:HH:mm}-{clim.SunriseEnd:HH:mm}) sunset({clim.SunsetBegin:HH:mm}-{clim.SunsetEnd:HH:mm})"
                    + $" moons={clim.Moons} phaseLen={clim.PhaseLength} volatility={clim.Volatility}"
                    + $" sun={clim.SunTexture?.GivenPath ?? "-"} glare={clim.SunGlareTexture?.GivenPath ?? "-"}");
                foreach (var wt in clim.WeatherTypes ?? Enumerable.Empty<IWeatherTypeGetter>())
                    Console.WriteLine($"        weather -> {Ref(wt.Weather.FormKey)} (chance {wt.Chance})");
            }

            if (r is IQuestGetter q)
            {
                Console.WriteLine($"      quest: flags={q.Flags}  priority={q.Priority}");
                foreach (var s in q.Stages)
                {
                    Console.WriteLine($"      stage[{s.Index}] flags={s.Flags}");
                    foreach (var le in s.LogEntries)
                    {
                        var flagStr = le.Flags == default ? "" : $" [{le.Flags}]";
                        Console.WriteLine($"        log{flagStr}: \"{le.Entry?.String}\"" + (le.Conditions.Count > 0 ? $"  ({le.Conditions.Count} cond)" : ""));
                    }
                }
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
