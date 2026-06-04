internal static partial class Program
{
    // Second positional half of the dump per-record detail chain (continues from
    // DumpRecordInventoryAndWorld in Diagnostics.Dump.cs). Covers spells/combat-style/packages/
    // class, the magic-effect/ammo/scroll/soulgem/outfit records, statics+texture-set models,
    // script VMAD + dialogue, faction/perk/relationship, weather/climate, and quests/scenes.
    // The split point is purely positional so the printed output is byte-for-byte unchanged.
    private static void DumpRecordMagicAiAndText(IMajorRecordGetter r, Func<FormKey, string> Ref)
    {
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
}
