namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: Npc (ACTOR) — kept in npcsByEd so dialogue/packages/wiring can reference them.
        // Quests + word-wall quests follow in their own steps (orchestrator order: NPC then QUST). ---
        public void BuildNpcs()
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
        }

        // --- pass 1: Quest (QUST) — kept in questsByEd so dialogue/aliases can reference them.
        // Stage/objective record data only; log-entry CTDA conditions are wired in pass 2. ---
        public void BuildQuests()
        {
            foreach (var q in spec.Quests)
            {
                var r = mod.Quests.AddNew();
                r.EditorID = q.EditorId; r.Name = q.Name;
                // StartGameEnabled is what makes a dialogue-hosting quest actually run (and thus its
                // dialogue load + evaluate). Priority orders competing dialogue between quests.
                if (q.StartGameEnabled) r.Flags |= Quest.Flag.StartGameEnabled;
                r.Priority = q.Priority;
                // QuestFormVersion (DNAM byte 3) is the quest's data-format version. Mutagen defaults it
                // to 255 (0xFF) — an "unset" sentinel that NO vanilla quest uses (1697/1811 vanilla
                // quests are 0). A 0xFF form version reads as an unknown/future format: the engine still
                // processes basic stage flags (so CompleteQuest fires) but DOES NOT register the quest in
                // the JOURNAL, so its log entries never display. Pin it to the vanilla canonical 0.
                r.QuestFormVersion = 0;
                // Quest TYPE (DNAM) decides JOURNAL-tab visibility: type=None (the Mutagen default) is a
                // background quest the player never sees, so its log entries don't surface and `setstage`
                // shows nothing. An explicit spec type wins; otherwise any quest with journal content
                // (an objective, or a stage with log text) defaults to SideQuest so it actually appears.
                if (Enum.TryParse<Quest.TypeEnum>(q.Type, ignoreCase: true, out var qType))
                    r.Type = qType;
                else if (q.Objectives.Count > 0 || q.Stages.Any(s => !string.IsNullOrEmpty(s.LogEntry)))
                    r.Type = Quest.TypeEnum.SideQuest;
                // "Displayed In HUD" (DNAM flags bit 0x0010) + RunOnce (0x0100) — both present on every
                // vanilla SideQuest with StartGameEnabled (raw DNAM byte pair = 0x11 0x01 = flags 0x0111).
                // DisplayedInHUD: must be set for the quest to appear in the journal at all (Mutagen's
                // Quest.Flag enum doesn't name it, so cast directly). RunOnce: prevents the quest from
                // auto-restarting after completion (matches vanilla SideQuest/MainQuest pattern; harmless
                // for background quests). Set both whenever the quest is given a non-None type.
                if (r.Type != Quest.TypeEnum.None)
                    r.Flags |= (Quest.Flag)0x0010 | (Quest.Flag)0x0100;
                foreach (var o in q.Objectives)
                    // Flags MUST be non-null so Mutagen writes the FNAM subrecord. Vanilla and all
                    // third-party mods have FNAM on every QOBJ; omitting it (Flags=null → no FNAM)
                    // produces a structurally different record the engine may reject for journal display.
                    r.Objectives.Add(new QuestObjective { Index = o.Index, DisplayText = o.Text, Flags = 0 });
                // STAGES (QSDT) + LOG ENTRIES (QLOG). Index + flags + text are pure record data; any
                // log-entry CTDA conditions are wired in pass 2 (WireQuestStageConditions). A bare
                // stage with no log entry is a silent milestone (valid; vanilla does this).
                foreach (var st in q.Stages.OrderBy(s => s.Index))
                {
                    var stage = new QuestStage { Index = st.Index };
                    // StartUpStage: the engine auto-SetStages here on quest start (so an SM-triggered
                    // quest shows its opening log entry / objective with no external SetStage). The
                    // QSDT stage flag byte is always written (Flags is non-nullable); default 0.
                    if (st.StartUpStage) stage.Flags = QuestStage.Flag.StartUpStage;
                    if (!string.IsNullOrEmpty(st.LogEntry) || st.CompleteQuest || st.FailQuest)
                    {
                        var le = new QuestLogEntry();
                        if (!string.IsNullOrEmpty(st.LogEntry)) le.Entry = st.LogEntry;
                        // Flags MUST be non-null (NOT left at its default null): in the SSE QUST format a
                        // log entry is a QSDT (1-byte flags marker) followed by its CNAM text, and Mutagen
                        // OMITS the QSDT subrecord entirely when Flags is null. A CNAM with no preceding
                        // QSDT is malformed — the engine's quest parser desyncs and the JOURNAL UI later
                        // CTDs (access-violation reading a bogus string length) when it renders the quest.
                        // Vanilla always writes QSDT=0 on a plain log entry. So accumulate into a non-null
                        // local and ALWAYS assign (0 when no complete/fail) to force the QSDT marker out.
                        QuestLogEntry.Flag leFlags = 0;
                        if (st.CompleteQuest) leFlags |= QuestLogEntry.Flag.CompleteQuest;
                        if (st.FailQuest) leFlags |= QuestLogEntry.Flag.FailQuest;
                        le.Flags = leFlags;
                        stage.LogEntries.Add(le);
                    }
                    r.Stages.Add(stage);
                }
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

    }
}
