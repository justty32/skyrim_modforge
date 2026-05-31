namespace ModForge;

public static partial class Generator
{
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
