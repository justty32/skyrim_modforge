namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // --- NPCs, CombatStyles, AI Packages, Relationships, Perks ---
        public void ValidateNpcs()
        {
            foreach (var n in spec.Npcs)
            {
                foreach (var fac in n.Factions)
                    if (LooksExternalRef(fac))
                    { if (!TryExternalRef(fac, out _)) Problems.Add($"npc '{n.EditorId}' faction: malformed external ref '{fac}'"); }
                    else if (!factionIds.Contains(fac))
                        Problems.Add($"npc '{n.EditorId}' references unknown faction '{fac}' (in-spec, non-faction or typo; vanilla faction -> <master>:0xFORMID)");
                CheckRef(n.Race, $"npc '{n.EditorId}' race");
                CheckRef(n.Class, $"npc '{n.EditorId}' class");
                CheckRef(n.Outfit, $"npc '{n.EditorId}' outfit");
                CheckRef(n.VoiceType, $"npc '{n.EditorId}' voiceType");
                CheckRef(n.CrimeFaction, $"npc '{n.EditorId}' crimeFaction");
                CheckRef(n.CombatStyle, $"npc '{n.EditorId}' combatStyle");
                foreach (var s in n.Spells) CheckRef(s, $"npc '{n.EditorId}' spell");
                foreach (var it in n.Items)
                {
                    if (string.IsNullOrWhiteSpace(it.Item))
                        Problems.Add($"npc '{n.EditorId}' item has empty ref (need a carriable item: in-spec editorId or <master>:0xFORMID)");
                    else CheckRef(it.Item, $"npc '{n.EditorId}' item");
                    if (it.Count == 0)
                        Problems.Add($"npc '{n.EditorId}' item '{it.Item}' has count 0 (must be non-zero)");
                }
                CheckEnum<Aggression>(n.Aggression, $"npc '{n.EditorId}' aggression");
                CheckEnum<Confidence>(n.Confidence, $"npc '{n.EditorId}' confidence");
                CheckEnum<Assistance>(n.Assistance, $"npc '{n.EditorId}' assistance");
                CheckEnum<Mood>(n.Mood, $"npc '{n.EditorId}' mood");
            }
            foreach (var cs in spec.CombatStyles)
                foreach (var f in cs.Flags)
                    if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.CombatStyle.Flag>(f, true, out _))
                        Problems.Add($"combatStyle '{cs.EditorId}' invalid flag '{f}' (Dueling|Flanking|AllowDualWielding)");

            // AI Packages (PACK): template is required (and must be a well-formed external ref —
            // there are no in-spec procedure templates); refs (template/combatStyle/ownerQuest +
            // sandbox.location) checked; enums (Flag/InterruptFlag/Speed/DayOfWeek) parse-checked.
            foreach (var pk in spec.Packages)
            {
                if (string.IsNullOrWhiteSpace(pk.Template))
                    Problems.Add($"package '{pk.EditorId}' has empty template (need <master>:0xFORMID of a procedure template, e.g. Skyrim.esm:0x01C254 = Sandbox)");
                else if (!LooksExternalRef(pk.Template) || !TryExternalRef(pk.Template, out _))
                    Problems.Add($"package '{pk.EditorId}' template '{pk.Template}' must be a well-formed external <master>:0xFORMID ref");
                CheckRef(pk.CombatStyle, $"package '{pk.EditorId}' combatStyle");
                CheckRef(pk.OwnerQuest,  $"package '{pk.EditorId}' ownerQuest");
                // An alias-capable slot ref may be "alias:<name>"/"aliasLoc:<name>" → validate against the
                // package's in-spec ownerQuest's aliases instead of as a placement/external ref.
                void PkgSlotRef(string refStr, string label)
                {
                    if (!Generator.TryParseAliasRef(refStr, out _, out var aliasName)) { CheckRef(refStr, label); return; }
                    if (string.IsNullOrWhiteSpace(pk.OwnerQuest))
                        Problems.Add($"{label}: 'alias:'/'aliasLoc:' needs an in-spec 'ownerQuest' on the package");
                    else if (spec.Quests.FirstOrDefault(q => string.Equals(q.EditorId, pk.OwnerQuest, StringComparison.OrdinalIgnoreCase)) is not { } oq)
                        Problems.Add($"{label}: ownerQuest '{pk.OwnerQuest}' is not an in-spec quest (alias refs need the quest's aliases)");
                    else if (!oq.Aliases.Any(a => string.Equals(a.Name, aliasName, StringComparison.OrdinalIgnoreCase)))
                        Problems.Add($"{label}: no alias '{aliasName}' on ownerQuest '{pk.OwnerQuest}'");
                }
                PkgSlotRef(pk.Sandbox.Location, $"package '{pk.EditorId}' sandbox.location");
                PkgSlotRef(pk.Sleep.Location,   $"package '{pk.EditorId}' sleep.location");
                PkgSlotRef(pk.Travel.Place, $"package '{pk.EditorId}' travel.place");
                PkgSlotRef(pk.UseMagic.Location, $"package '{pk.EditorId}' useMagic.location");
                PkgSlotRef(pk.UseMagic.Target,   $"package '{pk.EditorId}' useMagic.target");
                CheckRef(pk.UseMagic.Spell,    $"package '{pk.EditorId}' useMagic.spell");
                PkgSlotRef(pk.Patrol.Start,      $"package '{pk.EditorId}' patrol.start");
                if (LooksExternalRef(pk.Template) && TryExternalRef(pk.Template, out var ptfk) && ptfk == PackageTemplates.Patrol
                    && string.IsNullOrWhiteSpace(pk.Patrol.Start))
                    Problems.Add($"package '{pk.EditorId}' uses Patrol template but patrol.start is empty — NPC has no route and won't patrol");
                PkgSlotRef(pk.Follow.Target,     $"package '{pk.EditorId}' follow.target");
                PkgSlotRef(pk.Escort.Target,      $"package '{pk.EditorId}' escort.target");
                PkgSlotRef(pk.Escort.Destination, $"package '{pk.EditorId}' escort.destination");
                if (LooksExternalRef(pk.Template) && TryExternalRef(pk.Template, out var etfk) && etfk == PackageTemplates.Escort
                    && string.IsNullOrWhiteSpace(pk.Escort.Destination))
                    Problems.Add($"package '{pk.EditorId}' uses Escort template but escort.destination is empty — NPC won't lead anywhere (falls back to NearSelf)");
                if (LooksExternalRef(pk.Template) && TryExternalRef(pk.Template, out var tfk) && tfk == PackageTemplates.UseMagic
                    && string.IsNullOrWhiteSpace(pk.UseMagic.Spell))
                    Problems.Add($"package '{pk.EditorId}' uses UseMagic template but useMagic.spell is empty — package will no-op in-game");
                PkgSlotRef(pk.SitTarget.Target, $"package '{pk.EditorId}' sitTarget.target");
                if (LooksExternalRef(pk.Template) && TryExternalRef(pk.Template, out var sttfk) && sttfk == PackageTemplates.SitTarget
                    && string.IsNullOrWhiteSpace(pk.SitTarget.Target))
                    Problems.Add($"package '{pk.EditorId}' uses SitTarget template but sitTarget.target is empty — NPC has no furniture to use and won't sit");
                PkgSlotRef(pk.Activate.Target, $"package '{pk.EditorId}' activate.target");
                if (LooksExternalRef(pk.Template) && TryExternalRef(pk.Template, out var atfk) && atfk == PackageTemplates.Activate
                    && string.IsNullOrWhiteSpace(pk.Activate.Target))
                    Problems.Add($"package '{pk.EditorId}' uses Activate template but activate.target is empty — Activate has nothing to activate");
                PkgSlotRef(pk.Eat.Location, $"package '{pk.EditorId}' eat.location");
                foreach (var f in pk.Flags)
                    if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.Package.Flag>(f, true, out _))
                        Problems.Add($"package '{pk.EditorId}' invalid flag '{f}'");
                foreach (var f in pk.InterruptFlags)
                    if (!Enum.TryParse<Mutagen.Bethesda.Skyrim.Package.InterruptFlag>(f, true, out _))
                        Problems.Add($"package '{pk.EditorId}' invalid interruptFlag '{f}' (e.g. HellosToPlayer, AllowIdleChatter, WorldInteractions)");
                if (!string.IsNullOrEmpty(pk.PreferredSpeed)
                    && !Enum.TryParse<Mutagen.Bethesda.Skyrim.Package.Speed>(pk.PreferredSpeed, true, out _))
                    Problems.Add($"package '{pk.EditorId}' invalid preferredSpeed '{pk.PreferredSpeed}' (Walk|Jog|Run|FastWalk)");
                if (!string.IsNullOrEmpty(pk.Schedule.DayOfWeek)
                    && !Enum.TryParse<Mutagen.Bethesda.Skyrim.Package.DayOfWeek>(pk.Schedule.DayOfWeek, true, out _))
                    Problems.Add($"package '{pk.EditorId}' invalid schedule.dayOfWeek '{pk.Schedule.DayOfWeek}' (Sunday|Monday|…|Weekdays|Weekends|Any)");
            }

            foreach (var rel in spec.Relationships)
            {
                CheckRef(rel.Parent, $"relationship '{rel.EditorId}' parent");
                CheckRef(rel.Child,  $"relationship '{rel.EditorId}' child");
                if (string.IsNullOrWhiteSpace(rel.Parent))
                    Problems.Add($"relationship '{rel.EditorId}' has no parent NPC");
                if (!Enum.TryParse<Relationship.RankType>(rel.Rank, true, out _))
                    Problems.Add($"relationship '{rel.EditorId}' invalid rank '{rel.Rank}' (Lover|Ally|Confidant|Friend|Acquaintance|Rival|Foe|Enemy|Archnemesis)");
            }
            foreach (var n in spec.Npcs) foreach (var pkgRef in n.Packages) CheckRef(pkgRef, $"npc '{n.EditorId}' package");
            foreach (var n in spec.Npcs) foreach (var perkRef in n.Perks) CheckRef(perkRef, $"npc '{n.EditorId}' perk");

            var perkEntryTypes = new HashSet<string>(
                Enum.GetNames<APerkEntryPointEffect.EntryType>(), StringComparer.OrdinalIgnoreCase);
            foreach (var pk in spec.Perks)
            {
                if (string.IsNullOrWhiteSpace(pk.Name)) Problems.Add($"perk '{pk.EditorId}' has empty name");
                if (pk.NumRanks < 1) Problems.Add($"perk '{pk.EditorId}' numRanks must be >= 1 (got {pk.NumRanks})");
                CheckRef(pk.NextPerk, $"perk '{pk.EditorId}' nextPerk");
                foreach (var cs in pk.Conditions) CheckCondition(cs, $"perk '{pk.EditorId}' condition");
                if (pk.Effects.Count == 0) Problems.Add($"perk '{pk.EditorId}' has no effects (a perk with no effects does nothing)");
                foreach (var es in pk.Effects)
                {
                    var kind = (es.Kind ?? "").ToLowerInvariant();
                    if (kind == "ability")
                    {
                        if (string.IsNullOrWhiteSpace(es.Spell)) Problems.Add($"perk '{pk.EditorId}' ability effect has empty spell ref");
                        else CheckRef(es.Spell, $"perk '{pk.EditorId}' ability effect spell");
                    }
                    else if (kind == "entrypoint")
                    {
                        if (string.IsNullOrWhiteSpace(es.EntryPoint)) Problems.Add($"perk '{pk.EditorId}' entryPoint effect has empty entryPoint");
                        else if (!perkEntryTypes.Contains(es.EntryPoint))
                            Problems.Add($"perk '{pk.EditorId}' entryPoint effect has unknown entryPoint '{es.EntryPoint}' (e.g. ModAttackDamage, ModSpellMagnitude, CalculateMyCriticalHitChance)");
                        if (!string.IsNullOrEmpty(es.Function)
                            && es.Function.ToLowerInvariant() is not ("set" or "add" or "multiply" or "mult"))
                            Problems.Add($"perk '{pk.EditorId}' entryPoint effect has invalid function '{es.Function}' (Set|Add|Multiply)");
                    }
                    else if (kind == "addactivatechoice")
                    {
                        // entryPoint defaults to Activate; if given it must be a real EntryType.
                        if (!string.IsNullOrWhiteSpace(es.EntryPoint) && !perkEntryTypes.Contains(es.EntryPoint))
                            Problems.Add($"perk '{pk.EditorId}' addActivateChoice has unknown entryPoint '{es.EntryPoint}'");
                        if (string.IsNullOrWhiteSpace(es.ButtonLabel))
                            Problems.Add($"perk '{pk.EditorId}' addActivateChoice has empty buttonLabel (the '[E] <label>' prompt)");
                        if (string.IsNullOrWhiteSpace(es.Spell) && string.IsNullOrWhiteSpace(es.FragmentBody))
                            Problems.Add($"perk '{pk.EditorId}' addActivateChoice does nothing: give it a 'spell' and/or a 'fragmentBody'");
                        if (!string.IsNullOrWhiteSpace(es.Spell)) CheckRef(es.Spell, $"perk '{pk.EditorId}' addActivateChoice spell");
                    }
                    else if (kind == "settext")
                    {
                        if (!string.IsNullOrWhiteSpace(es.EntryPoint) && !perkEntryTypes.Contains(es.EntryPoint))
                            Problems.Add($"perk '{pk.EditorId}' setText has unknown entryPoint '{es.EntryPoint}'");
                        if (string.IsNullOrWhiteSpace(es.Text))
                            Problems.Add($"perk '{pk.EditorId}' setText has empty text (the new activation prompt)");
                    }
                    else
                        Problems.Add($"perk '{pk.EditorId}' effect has invalid kind '{es.Kind}' (ability|entryPoint|addActivateChoice|setText)");
                    foreach (var cs in es.Conditions) CheckCondition(cs, $"perk '{pk.EditorId}' effect condition");
                }
            }
        }

        // npcPatches[]: override an existing NPC's packages. `overrideOf` must be an existing NPC ref
        // (an external "<master>:0xFORMID" — you can't patch an in-spec new NPC, use npcs[] for those);
        // each package ref must resolve; mode must be replace/prepend/append.
        public void ValidateNpcPatches()
        {
            foreach (var p in spec.NpcPatches)
            {
                if (string.IsNullOrWhiteSpace(p.OverrideOf))
                    Problems.Add("npcPatch has no overrideOf (the existing NPC ref '<master>:0xFORMID')");
                else if (!LooksExternalRef(p.OverrideOf) || !TryExternalRef(p.OverrideOf, out _))
                    Problems.Add($"npcPatch overrideOf '{p.OverrideOf}' must be an existing NPC ref '<master>:0xFORMID' (in-spec new NPCs go in npcs[])");
                if (p.Packages.Count == 0)
                    Problems.Add($"npcPatch '{p.OverrideOf}' has no packages — nothing to change");
                foreach (var pk in p.Packages) CheckRef(pk, $"npcPatch '{p.OverrideOf}' package");
                var mode = (p.Mode ?? "").Trim().ToLowerInvariant();
                if (mode is not ("" or "replace" or "prepend" or "append"))
                    Problems.Add($"npcPatch '{p.OverrideOf}' invalid mode '{p.Mode}' (replace | prepend | append)");
            }
        }
    }
}
