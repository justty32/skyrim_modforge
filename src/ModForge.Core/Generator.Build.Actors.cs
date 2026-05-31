namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: Npc (ACTOR) + Quest (QUST) — kept in editorId maps so dialogue can reference them ---
        public void BuildNpcsAndQuests()
        {
            foreach (var n in spec.Npcs)
            {
                var r = mod.Npcs.AddNew();
                r.EditorID = n.EditorId; r.Name = n.Name;
                // A fixed level + AutoCalcStats is what makes the `class` actually drive the actor's
                // attribute (H/M/S) + skill distribution; without them the engine uses flat defaults.
                if (n.Level > 0) r.Configuration.Level = new NpcLevel { Level = (short)Math.Clamp(n.Level, 1, short.MaxValue) };
                if (n.AutoCalcStats) r.Configuration.Flags |= NpcConfiguration.Flag.AutoCalcStats;
                // Configuration.Flag.Unique: marks the actor as a one-off (vs a leveled/respawning
                // template instance). Vanilla cross-cell-travelling NPCs (Ysolda, Carlotta, …) all
                // have this. Suspected to matter for the engine's persistent AI-tracking that lets a
                // package decide "I'm at the inn now, schedule says it's market time, walk to market".
                if (n.Unique) r.Configuration.Flags |= NpcConfiguration.Flag.Unique;
                // AIData — Aggression/Confidence are the difference between "fights" and "flees".
                // Default Aggression=Unaggressive + Confidence=Cowardly means the NPC runs from any
                // threat (the "mage just flees, never casts" symptom). Author Aggression=Aggressive
                // (defends) + Confidence=Brave (stays) for a normal combatant; Frenzied + Foolhardy
                // for a fanatic. EnergyLevel defaults to 50 for vanilla actors.
                if (Enum.TryParse<Aggression>(n.Aggression, ignoreCase: true, out var ag)) r.AIData.Aggression = ag;
                if (Enum.TryParse<Confidence>(n.Confidence, ignoreCase: true, out var cf)) r.AIData.Confidence = cf;
                if (Enum.TryParse<Assistance>(n.Assistance, ignoreCase: true, out var asst)) r.AIData.Assistance = asst;
                if (Enum.TryParse<Mood>(n.Mood, ignoreCase: true, out var md)) r.AIData.Mood = md;
                if (n.EnergyLevel > 0) r.AIData.EnergyLevel = (byte)Math.Clamp(n.EnergyLevel, 0, 100);
                if (!string.IsNullOrEmpty(n.EditorId)) npcsByEd[n.EditorId] = r;
            }

            foreach (var q in spec.Quests)
            {
                var r = mod.Quests.AddNew();
                r.EditorID = q.EditorId; r.Name = q.Name;
                // StartGameEnabled is what makes a dialogue-hosting quest actually run (and thus its
                // dialogue load + evaluate). Priority orders competing dialogue between quests.
                if (q.StartGameEnabled) r.Flags |= Quest.Flag.StartGameEnabled;
                r.Priority = q.Priority;
                foreach (var o in q.Objectives)
                    r.Objectives.Add(new QuestObjective { Index = o.Index, DisplayText = o.Text });
                if (!string.IsNullOrEmpty(q.EditorId)) questsByEd[q.EditorId] = r;
            }
        }

        // --- pass 2: NPC cross-record refs (race/class/outfit/voice/crime/combatStyle/spells/factions) ---
        public void WireNpcs()
        {
            foreach (var n in spec.Npcs)
            {
                if (!npcsByEd.TryGetValue(n.EditorId, out var npcRec)) continue;
                Resolve($"npc '{n.EditorId}' race",   n.Race,   fk => npcRec.Race.SetTo(fk));
                Resolve($"npc '{n.EditorId}' class",  n.Class,  fk => npcRec.Class.SetTo(fk));
                Resolve($"npc '{n.EditorId}' outfit", n.Outfit, fk => npcRec.DefaultOutfit.SetTo(fk));
                Resolve($"npc '{n.EditorId}' voiceType", n.VoiceType, fk => npcRec.Voice.SetTo(fk));
                // CrimeFaction (e.g. CrimeFactionWhiterun): the faction that the NPC's crimes are
                // reported to AND that marks them as a recognised "citizen of X". Vanilla NPCs that
                // freely cross between a city's interior cells and exterior worldspace all have one
                // of these set; without it the engine refuses door-teleport travel as if the NPC
                // were trespassing.
                Resolve($"npc '{n.EditorId}' crimeFaction", n.CrimeFaction, fk => npcRec.CrimeFaction.SetTo(fk));
                // CombatStyle: HOW the AI fights — picks magic/melee/staff based on equipMult* weights.
                Resolve($"npc '{n.EditorId}' combatStyle", n.CombatStyle, fk => npcRec.CombatStyle.SetTo(fk));
                // Spells: populates npc.ActorEffect — the AI's spell list. Combat AI consults this when
                // its CombatStyle says "prefer magic"; without spells, no casting (the spell list is
                // empty). Reuse the existing ref resolver — works for in-spec spells AND vanilla refs.
                if (n.Spells.Count > 0)
                {
                    npcRec.ActorEffect ??= new();
                    foreach (var spellRef in n.Spells)
                        Resolve($"npc '{n.EditorId}' spell", spellRef, fk =>
                            npcRec.ActorEffect!.Add(new FormLink<ISpellRecordGetter>(fk)));
                }
                bool joinsVendorFaction = false;
                foreach (var factionRef in n.Factions)
                {
                    if (!LooksExternalRef(factionRef) && vendorFactionEds.Contains(factionRef)) joinsVendorFaction = true;
                    Resolve($"npc '{n.EditorId}' faction", factionRef, fk =>
                    {
                        var rp = new RankPlacement { Rank = 0 };
                        rp.Faction.SetTo(fk);
                        npcRec.Factions.Add(rp);
                    });
                }
                // A member of an in-spec VENDOR faction must also be in JobMerchantFaction (Skyrim.esm:
                // 0x051596): the vanilla generic "I'd like to trade" topic (DialogueGeneric.OfferServices
                // Topic) gates on GetInFaction JobMerchantFaction + GetOffersServicesNow. Add it once
                // (unless the spec already lists it) so the trade prompt actually surfaces.
                if (joinsVendorFaction)
                {
                    var jobMerchant = new FormKey(ModKey.FromNameAndExtension("Skyrim.esm"), 0x051596);
                    bool already = npcRec.Factions.Any(rp => rp.Faction.FormKey == jobMerchant);
                    if (!already)
                    {
                        var rp = new RankPlacement { Rank = 0 };
                        rp.Faction.SetTo(jobMerchant);
                        npcRec.Factions.Add(rp);
                        linksWired++; extLinks++;
                    }
                }
            }
        }

        // --- pass 2: Relationship Parent/Child NPC refs (Parent usually the in-spec NPC, Child the player) ---
        public void WireRelationships()
        {
            foreach (var rel in spec.Relationships)
            {
                if (!recordsByEd.TryGetValue(rel.EditorId, out var rec) || rec is not IRelationship r) continue;
                Resolve($"relationship '{rel.EditorId}' parent", rel.Parent, fk => r.Parent.SetTo(fk));
                Resolve($"relationship '{rel.EditorId}' child",  rel.Child,  fk => r.Child.SetTo(fk));
            }
        }

        // --- pass 2: NPC.Packages — assign each ref'd package to the NPC's package list ---
        // (run-order: the engine picks the first one whose conditions+schedule match, so order
        // matters; we add in spec order).
        public void WireNpcPackages()
        {
            foreach (var n in spec.Npcs)
            {
                if (n.Packages.Count == 0) continue;
                if (!npcsByEd.TryGetValue(n.EditorId, out var npcRec)) continue;
                foreach (var pkgRef in n.Packages)
                    Resolve($"npc '{n.EditorId}' package", pkgRef, fk =>
                        npcRec.Packages.Add(new FormLink<IPackageGetter>(fk)));
            }
        }

        // --- pass 2: Outfit (OTFT) contents — each item is a ref (in-spec armor/weapon or external) ---
        public void WireOutfits()
        {
            foreach (var o in spec.Outfits)
            {
                if (!recordsByEd.TryGetValue(o.EditorId, out var rec) || rec is not IOutfit outfit) continue;
                outfit.Items ??= new();
                foreach (var itemRef in o.Items)
                    Resolve($"outfit '{o.EditorId}' item", itemRef, fk => outfit.Items!.Add(new FormLink<IOutfitTargetGetter>(fk)));
            }
        }
    }
}
