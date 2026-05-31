namespace ModForge;

public static partial class Generator
{
    // Vanilla-canonical PerkConditionTabCount per entry-point type (extracted from Skyrim.esm's 375
    // PERK records). This byte is the entry point's intrinsic number of condition tabs (e.g. attacker /
    // target / weapon contexts) — the game uses it to size the per-tab condition array, so it must be
    // non-zero and match vanilla even when we author conditions on tab 0 only. Unlisted entry points
    // fall back to 2 (a safe non-zero default ≥ the single tab-0 group we ever emit).
    private static readonly Dictionary<APerkEntryPointEffect.EntryType, byte> EntryPointTabCount = new()
    {
        [APerkEntryPointEffect.EntryType.ApplyBashingSpell] = 2,
        [APerkEntryPointEffect.EntryType.ApplyCombatHitSpell] = 3,
        [APerkEntryPointEffect.EntryType.ApplyReanimateSpell] = 3,
        [APerkEntryPointEffect.EntryType.ApplySneakingSpell] = 1,
        [APerkEntryPointEffect.EntryType.ApplyWeaponSwingSpell] = 3,
        [APerkEntryPointEffect.EntryType.CalculateMyCriticalHitChance] = 3,
        [APerkEntryPointEffect.EntryType.CalculateMyCriticalHitDamage] = 3,
        [APerkEntryPointEffect.EntryType.CalculateWeaponDamage] = 3,
        [APerkEntryPointEffect.EntryType.CanDualCastSpell] = 2,
        [APerkEntryPointEffect.EntryType.CanPickpocketEquippedItem] = 3,
        [APerkEntryPointEffect.EntryType.FilterActivation] = 2,
        [APerkEntryPointEffect.EntryType.MakeLockpicksUnbreakable] = 1,
        [APerkEntryPointEffect.EntryType.ModAlchemyEffectiveness] = 1,
        [APerkEntryPointEffect.EntryType.ModArmorRating] = 2,
        [APerkEntryPointEffect.EntryType.ModArmorWeight] = 2,
        [APerkEntryPointEffect.EntryType.ModAttackDamage] = 3,
        [APerkEntryPointEffect.EntryType.ModBashingDamage] = 2,
        [APerkEntryPointEffect.EntryType.ModBowZoom] = 2,
        [APerkEntryPointEffect.EntryType.ModBuyPrices] = 2,
        [APerkEntryPointEffect.EntryType.ModCommandedActorLimit] = 2,
        [APerkEntryPointEffect.EntryType.ModDetectionLight] = 2,
        [APerkEntryPointEffect.EntryType.ModDetectionSneakSkill] = 2,
        [APerkEntryPointEffect.EntryType.ModEnchantmentPower] = 3,
        [APerkEntryPointEffect.EntryType.ModFallingDamage] = 1,
        [APerkEntryPointEffect.EntryType.ModIncomingDamage] = 3,
        [APerkEntryPointEffect.EntryType.ModIncomingSpellMagnitude] = 2,
        [APerkEntryPointEffect.EntryType.ModIncomingStagger] = 2,
        [APerkEntryPointEffect.EntryType.ModIngredientsHarvested] = 2,
        [APerkEntryPointEffect.EntryType.ModInitialIngredientEffectsLearned] = 2,
        [APerkEntryPointEffect.EntryType.ModLockpickingCrimeChance] = 2,
        [APerkEntryPointEffect.EntryType.ModLockpickingKeyRewardChance] = 2,
        [APerkEntryPointEffect.EntryType.ModLockpickSweetSpot] = 2,
        [APerkEntryPointEffect.EntryType.ModNumAppliedEnchantmentsAllowed] = 1,
        [APerkEntryPointEffect.EntryType.ModPercentBlocked] = 1,
        [APerkEntryPointEffect.EntryType.ModPickpocketChance] = 3,
        [APerkEntryPointEffect.EntryType.ModPlayerIntimidation] = 2,
        [APerkEntryPointEffect.EntryType.ModPlayerMagicSlowdown] = 2,
        [APerkEntryPointEffect.EntryType.ModPoisonDoseCount] = 3,
        [APerkEntryPointEffect.EntryType.ModPotionsCreated] = 2,
        [APerkEntryPointEffect.EntryType.ModPowerAttackDamage] = 3,
        [APerkEntryPointEffect.EntryType.ModPowerAttackStamina] = 2,
        [APerkEntryPointEffect.EntryType.ModRecoverArrowChance] = 1,
        [APerkEntryPointEffect.EntryType.ModSellPrices] = 2,
        [APerkEntryPointEffect.EntryType.ModShieldDefectArrowChance] = 1,
        [APerkEntryPointEffect.EntryType.ModShoutOk] = 1,
        [APerkEntryPointEffect.EntryType.ModSkillUse] = 1,
        [APerkEntryPointEffect.EntryType.ModSneakAttackMult] = 3,
        [APerkEntryPointEffect.EntryType.ModSoulGemRecharge] = 2,
        [APerkEntryPointEffect.EntryType.ModSoulPercentCapturedToWeapon] = 3,
        [APerkEntryPointEffect.EntryType.ModSpellCastingSoundEvent] = 2,
        [APerkEntryPointEffect.EntryType.ModSpellCost] = 2,
        [APerkEntryPointEffect.EntryType.ModSpellDuration] = 3,
        [APerkEntryPointEffect.EntryType.ModSpellMagnitude] = 3,
        [APerkEntryPointEffect.EntryType.ModSpellRange] = 2,
        [APerkEntryPointEffect.EntryType.ModTargetDamageResistance] = 3,
        [APerkEntryPointEffect.EntryType.ModTargetStagger] = 2,
        [APerkEntryPointEffect.EntryType.ModTemperingHealth] = 2,
        [APerkEntryPointEffect.EntryType.ModWardMagickaAbsorptionPct] = 2,
        [APerkEntryPointEffect.EntryType.PurifyAlchemyIngredients] = 1,
        [APerkEntryPointEffect.EntryType.SetBooleanGraphVariable] = 1,
        [APerkEntryPointEffect.EntryType.SetLockpickStartingArc] = 1,
        [APerkEntryPointEffect.EntryType.SetSweepAttack] = 2,
        [APerkEntryPointEffect.EntryType.ShouldApplyPlacedItem] = 3,
    };

    private sealed partial class BuildContext
    {
        // --- pass 1: Perk (PERK) trunk — scalar flags/level/ranks. ---
        // Effects, perk-level + effect-level conditions, ability-spell links and NextPerk are refs,
        // so they're wired in pass 2 (WirePerks). Creating the record here registers its editorId for
        // forward refs (NextPerk chains, npcs[].perks).
        public void BuildPerks()
        {
            foreach (var pk in spec.Perks)
            {
                var r = mod.Perks.AddNew();
                r.EditorID = pk.EditorId;
                if (!string.IsNullOrEmpty(pk.Name)) r.Name = pk.Name;
                if (!string.IsNullOrEmpty(pk.Description)) r.Description = pk.Description;
                r.Playable = pk.Playable;
                r.Hidden = pk.Hidden;
                r.Trait = pk.Trait;
                r.Level = (byte)Math.Clamp(pk.Level, 0, 255);
                r.NumRanks = (byte)Math.Clamp(pk.NumRanks, 1, 255);   // a perk needs at least one rank
            }
        }

        // --- pass 2: Perk effects (ability spell links / entry-point modify-value), perk-level CTDA ---
        // conditions, effect-level PerkConditions, the NextPerk chain, and npcs[].perks grants — all
        // refs, so wired once every record's editorId is known. The SHARED BuildCondition() turns a
        // ConditionSpec into a Mutagen Condition; perk-level conditions take plain Conditions, while
        // EFFECT-level conditions are wrapped in a PerkCondition (RunOnTabIndex grouping like vanilla).
        public void WirePerks()
        {
            foreach (var pk in spec.Perks)
            {
                if (!recordsByEd.TryGetValue(pk.EditorId, out var rec) || rec is not IPerk perk) continue;
                Resolve($"perk '{pk.EditorId}' nextPerk", pk.NextPerk, fk => perk.NextPerk.SetTo(fk));

                // Perk-level conditions (plain Condition list on the perk trunk).
                foreach (var cs in pk.Conditions)
                    if (BuildCondition(cs, $"perk '{pk.EditorId}' condition") is { } c)
                        perk.Conditions.Add(c);

                foreach (var es in pk.Effects)
                {
                    APerkEffect? effect = null;
                    switch ((es.Kind ?? "").ToLowerInvariant())
                    {
                        case "ability":
                        {
                            if (!TryResolveRef(es.Spell, formKeyByEd, out var fk))
                            { Warn($"  ! perk '{pk.EditorId}' ability effect: spell ref '{es.Spell}' unresolved — effect skipped"); continue; }
                            var ab = new PerkAbilityEffect { Rank = (byte)Math.Clamp(es.Rank, 0, 255), Priority = (byte)Math.Clamp(es.Priority, 0, 255) };
                            ab.Ability.SetTo(fk);
                            linksWired++;
                            if (LooksExternalRef(es.Spell)) extLinks++;
                            effect = ab;
                            break;
                        }
                        case "entrypoint":
                        {
                            if (!Enum.TryParse<APerkEntryPointEffect.EntryType>(es.EntryPoint, ignoreCase: true, out var entry))
                            { Warn($"  ! perk '{pk.EditorId}' entryPoint effect: unknown entryPoint '{es.EntryPoint}' — effect skipped"); continue; }
                            var func = es.Function?.ToLowerInvariant() switch
                            {
                                "set"      => PerkEntryPointModifyValue.ModificationType.Set,
                                "add"      => PerkEntryPointModifyValue.ModificationType.Add,
                                "multiply" or "mult" => PerkEntryPointModifyValue.ModificationType.Multiply,
                                _          => PerkEntryPointModifyValue.ModificationType.Multiply,
                            };
                            effect = new PerkEntryPointModifyValue
                            {
                                EntryPoint = entry,
                                Modification = func,
                                Value = es.Value,
                                Rank = (byte)Math.Clamp(es.Rank, 0, 255),
                                Priority = (byte)Math.Clamp(es.Priority, 0, 255),
                                // PerkConditionTabCount is the entry point's FIXED number of condition tabs
                                // (a property of the EntryPoint, NOT of how many conditions we author). The
                                // engine sizes its per-tab condition array from this byte; leaving it 0 while
                                // a PRKC tab is present overflows that array -> garbage FormID lookup -> hard
                                // CTD while "Loading Files". Set the vanilla-canonical count. (Root-caused
                                // 2026-05-31 from a CrashLoggerSSE log on ModForgePerks "Deadly Strikes".)
                                PerkConditionTabCount = EntryPointTabCount.GetValueOrDefault(entry, (byte)2),
                            };
                            break;
                        }
                        default:
                            Warn($"  ! perk '{pk.EditorId}' effect has unknown kind '{es.Kind}' (ability|entryPoint) — skipped");
                            continue;
                    }

                    // Effect-level conditions: each ConditionSpec becomes a Condition, all grouped under a
                    // single PerkCondition (RunOnTabIndex 0) — vanilla perks tab-group these, but one tab
                    // covers the common case ("only when …").
                    if (es.Conditions.Count > 0)
                    {
                        var pcond = new PerkCondition { RunOnTabIndex = 0 };
                        foreach (var cs in es.Conditions)
                            if (BuildCondition(cs, $"perk '{pk.EditorId}' effect condition") is { } c)
                                pcond.Conditions.Add(c);
                        if (pcond.Conditions.Count > 0) effect.Conditions.Add(pcond);
                    }
                    perk.Effects.Add(effect);
                }
            }

            // Perks granted to NPCs (npcs[].perks → npc.Perks as PerkPlacements). Each placement carries
            // the perk's NumRanks so a multi-rank perk applies fully.
            var perkRanksByEd = spec.Perks.Where(p => !string.IsNullOrEmpty(p.EditorId))
                .GroupBy(p => p.EditorId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (byte)Math.Clamp(g.First().NumRanks, 1, 255), StringComparer.OrdinalIgnoreCase);
            foreach (var n in spec.Npcs)
            {
                if (n.Perks.Count == 0) continue;
                if (!npcsByEd.TryGetValue(n.EditorId, out var npcRec)) continue;
                npcRec.Perks ??= new();
                foreach (var perkRef in n.Perks)
                    Resolve($"npc '{n.EditorId}' perk", perkRef, fk =>
                    {
                        byte ranks = perkRanksByEd.TryGetValue(perkRef, out var rk) ? rk : (byte)1;
                        var pp = new PerkPlacement { Rank = ranks };
                        pp.Perk.SetTo(fk);
                        npcRec.Perks!.Add(pp);
                    });
            }
        }
    }
}
