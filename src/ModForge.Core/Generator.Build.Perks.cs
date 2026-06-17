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

                // addActivateChoice effects carrying a fragmentBody, in declaration order (= FragmentIndex).
                var fragChoices = new List<PerkEntryPointAddActivateChoice>();
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
                        case "addactivatechoice":
                        {
                            if (ParsePerkEntry(es.EntryPoint, pk.EditorId) is not { } ept) continue;
                            var ch = new PerkEntryPointAddActivateChoice
                            {
                                EntryPoint = ept,
                                Rank = (byte)Math.Clamp(es.Rank, 0, 255),
                                Priority = (byte)Math.Clamp(es.Priority, 0, 255),
                                PerkConditionTabCount = EntryPointTabCount.GetValueOrDefault(ept, (byte)2),
                                // ReplaceDefault now; RunImmediately + FragmentIndex added in AttachPerkFragments
                                // (only when the compiled fragment .pex is present).
                                Flags = new PerkScriptFlag
                                {
                                    Flags = es.ReplaceDefault ? PerkScriptFlag.Flag.ReplaceDefault : default,
                                    FragmentIndex = 0,
                                },
                            };
                            if (!string.IsNullOrWhiteSpace(es.ButtonLabel)) ch.ButtonLabel = es.ButtonLabel;
                            if (!string.IsNullOrWhiteSpace(es.Spell))
                            {
                                if (TryResolveRef(es.Spell, formKeyByEd, out var afk))
                                { ch.Spell.SetTo(afk); linksWired++; if (LooksExternalRef(es.Spell)) extLinks++; }
                                else Warn($"  ! perk '{pk.EditorId}' addActivateChoice: spell ref '{es.Spell}' unresolved");
                            }
                            if (!string.IsNullOrWhiteSpace(es.FragmentBody)) fragChoices.Add(ch);
                            effect = ch;
                            break;
                        }
                        case "settext":
                        {
                            if (ParsePerkEntry(es.EntryPoint, pk.EditorId) is not { } ept) continue;
                            var stx = new PerkEntryPointSetText
                            {
                                EntryPoint = ept,
                                Rank = (byte)Math.Clamp(es.Rank, 0, 255),
                                Priority = (byte)Math.Clamp(es.Priority, 0, 255),
                                PerkConditionTabCount = EntryPointTabCount.GetValueOrDefault(ept, (byte)2),
                                Flags = new PerkScriptFlag { Flags = default, FragmentIndex = 0 },
                            };
                            if (!string.IsNullOrWhiteSpace(es.Text)) stx.Text = es.Text;
                            if (!string.IsNullOrWhiteSpace(es.ButtonLabel)) stx.ButtonLabel = es.ButtonLabel;
                            effect = stx;
                            break;
                        }
                        default:
                            Warn($"  ! perk '{pk.EditorId}' effect has unknown kind '{es.Kind}' (ability|entryPoint|addActivateChoice|setText) — skipped");
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

                // Attach the perk fragment VMAD if any choice ships a fragment AND its .pex is compiled
                // (package path). Without the .pex we leave the choices fragment-less (spell-only/inert),
                // mirroring quest/dialogue fragment gating — a VMAD pointing at an absent .pex errors on load.
                if (fragChoices.Count > 0)
                    AttachPerkFragments(pk, perk, fragChoices);
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

        // Parse a perk choice/text EntryType (defaults to "Activate" when unset). null = unknown name.
        private APerkEntryPointEffect.EntryType? ParsePerkEntry(string name, string ed)
        {
            var n = string.IsNullOrWhiteSpace(name) ? "Activate" : name;
            if (Enum.TryParse<APerkEntryPointEffect.EntryType>(n, ignoreCase: true, out var e)) return e;
            Warn($"  ! perk '{ed}' addActivateChoice/setText: unknown entryPoint '{name}' — effect skipped");
            return null;
        }

        // Attach the PerkAdapter VMAD binding each fragment-bearing addActivateChoice to a Fragment_<i>
        // function (script extends Perk). Gated on the compiled .pex (package path). ⚠ Byte fields
        // (Version/ObjectFormat, ExtraBindDataVersion, IndexedScriptFragment Unknowns) + the fragment
        // signature need a main-machine xEdit compare vs a real Immersive Interactions perk (WAIT_USER).
        private void AttachPerkFragments(PerkSpec pk, IPerk perk, List<PerkEntryPointAddActivateChoice> choices)
        {
            if (options?.CompiledScriptsDir is not { } dir) return;
            var scriptName = Generator.PerkFragmentScriptName(pk);
            if (string.IsNullOrEmpty(scriptName) || !File.Exists(Path.Combine(dir, scriptName + ".pex"))) return;

            var pa = perk.VirtualMachineAdapter as PerkAdapter ?? new PerkAdapter { Version = 4, ObjectFormat = 2 };
            if (!pa.Scripts.Any(s => string.Equals(s.Name, scriptName, StringComparison.OrdinalIgnoreCase)))
                pa.Scripts.Add(new ScriptEntry { Name = scriptName });
            pa.ScriptFragments ??= new PerkScriptFragments { FileName = "", ExtraBindDataVersion = 2 };

            for (int i = 0; i < choices.Count; i++)
            {
                choices[i].Flags!.Flags |= PerkScriptFlag.Flag.RunImmediately;   // run the fragment on activate
                choices[i].Flags!.FragmentIndex = (ushort)i;
                pa.ScriptFragments.Fragments.Add(new IndexedScriptFragment
                {
                    FragmentIndex = (ushort)i,
                    ScriptName = scriptName,
                    FragmentName = $"Fragment_{i}",
                });
            }
            perk.VirtualMachineAdapter = pa;
            scriptsAttached++;
        }
    }
}
