namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
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
                // FOOTGUN: AutoCalcStats derives H/M/S from the actor's CLASS — with no class the calc
                // yields ~0 health, so an Essential NPC spawns in permanent bleedout (looks DEAD; you'd
                // have to `resurrect` it in console). Warn loudly; the fix is to give the NPC a `class`.
                if (n.AutoCalcStats && string.IsNullOrWhiteSpace(n.Class))
                    Warn($"  ! npc '{n.EditorId}': autoCalcStats set but NO class — stats calc to ~0 HP; an essential NPC will spawn in permanent bleedout (appears dead). Give it a `class`.");
                // Player capture with no voiceType: expected (the player's base TESNPC commonly has
                // none), not a bug — user-decided 2026-07-12 "as-captured, no fallback" (no vanilla
                // voice gets guessed in). Warn purely for visibility so a silent clone isn't a surprise.
                if (n.IsPlayer && string.IsNullOrWhiteSpace(n.VoiceType))
                    Warn($"  ! npc '{n.EditorId}' is a player capture — no voiceType (the player has none); the clone will be silent (no hello/idle/combat vocals). This is expected, not a bug.");
                // --- Explicit stats (DNAM) — the non-autocalc route. For an actor WITHOUT the
                // AutoCalcStats flag the engine reads these values verbatim, so this is how a
                // captured real actor (`sc capp`) keeps its true health pool and skill spread
                // instead of a class+level ESTIMATE. Skill values are bytes; H/M/S are ushorts.
                // Both routes at once is a spec error (autocalc recomputes at load and the
                // authored numbers are ignored) — warn rather than silently pick one.
                bool explicitStats = n.Health > 0 || n.Magicka > 0 || n.Stamina > 0 || n.Skills.Count == 18;
                if (explicitStats)
                {
                    if (n.AutoCalcStats)
                        Warn($"  ! npc '{n.EditorId}': explicit health/magicka/stamina/skills AND autoCalcStats — the engine recomputes stats from class+level and the authored numbers are ignored. Drop one.");
                    var ps = r.PlayerSkills ??= new PlayerSkills();
                    if (n.Health > 0) ps.Health = (ushort)Math.Clamp(n.Health, 0, ushort.MaxValue);
                    if (n.Magicka > 0) ps.Magicka = (ushort)Math.Clamp(n.Magicka, 0, ushort.MaxValue);
                    if (n.Stamina > 0) ps.Stamina = (ushort)Math.Clamp(n.Stamina, 0, ushort.MaxValue);
                    if (n.Skills.Count == 18)
                    {
                        // Spec order == Mutagen's Skill enum order (OneHanded=6 … Enchanting=23),
                        // which is the engine's ActorValue order the capture DLL exports in.
                        var skillOrder = Enum.GetValues<Skill>();   // 18 members, sorted by value
                        for (int si = 0; si < skillOrder.Length && si < n.Skills.Count; si++)
                            ps.SkillValues[skillOrder[si]] = (byte)Math.Clamp(n.Skills[si], 0, byte.MaxValue);
                    }
                }
                // Configuration.Flag.Unique: marks the actor as a one-off (vs a leveled/respawning
                // template instance). Vanilla cross-cell-travelling NPCs (Ysolda, Carlotta, …) all
                // have this. Suspected to matter for the engine's persistent AI-tracking that lets a
                // package decide "I'm at the inn now, schedule says it's market time, walk to market".
                if (n.Unique) r.Configuration.Flags |= NpcConfiguration.Flag.Unique;
                // Essential = unkillable (bleedout + recover); Protected = only the player can kill it.
                // Essential is what keeps a non-lethal brawl (scene brawlOnEnd) from ending in a corpse.
                if (n.Essential) r.Configuration.Flags |= NpcConfiguration.Flag.Essential;
                if (n.Protected) r.Configuration.Flags |= NpcConfiguration.Flag.Protected;
                // --- Appearance recipe (record-local half; ref'd parts — hairColor/faceTexture/
                // headParts — are wired in pass 2). This is the TESNPC RECIPE only: without baked
                // FaceGeom .nif + facetint .dds assets the engine renders a custom face gray/dark
                // (body shape, skin tone, hair colour and identity are still correct). Baking is a
                // later milestone (plans/captured-npcs-consumption.md Phase 2).
                if (n.Female) r.Configuration.Flags |= NpcConfiguration.Flag.Female;
                if (n.Weight is { } wt) r.Weight = wt;
                if (n.Height is { } ht) r.Height = ht;
                if (n.BodyTint is { } bt)
                    r.TextureLighting = System.Drawing.Color.FromArgb(
                        Math.Clamp(bt.R, 0, 255), Math.Clamp(bt.G, 0, 255), Math.Clamp(bt.B, 0, 255));
                // NAM9 face morphs: the spec carries the engine's 18-slot array (RE::TESNPC::FaceData::
                // Morphs order, which byte-matches Mutagen's NpcFaceMorph declaration order — both are
                // NAM9 file order; verified 2026-07-11, table in the plan). Slot 18 (kUnk) stays 0.
                if (n.FaceMorphs.Count == 18)
                {
                    var m = n.FaceMorphs;
                    r.FaceMorph = new NpcFaceMorph
                    {
                        NoseLongVsShort = m[0], NoseUpVsDown = m[1],
                        JawUpVsDown = m[2], JawNarrowVsWide = m[3], JawForwardVsBack = m[4],
                        CheeksUpVsDown = m[5], CheeksForwardVsBack = m[6],
                        EyesUpVsDown = m[7], EyesInVsOut = m[8],
                        BrowsUpVsDown = m[9], BrowsInVsOut = m[10], BrowsForwardVsBack = m[11],
                        LipsUpVsDown = m[12], LipsInVsOut = m[13],
                        ChinNarrowVsWide = m[14], ChinUpVsDown = m[15], ChinUnderbiteVsOverbite = m[16],
                        EyesForwardVsBack = m[17],
                    };
                }
                if (n.FaceParts.Count == 4)
                    r.FaceParts = new NpcFaceParts
                    {
                        Nose = (uint)n.FaceParts[0], Unknown = (uint)n.FaceParts[1],
                        Eyes = (uint)n.FaceParts[2], Mouth = (uint)n.FaceParts[3],
                    };
                foreach (var t in n.TintLayers)
                    r.TintLayers.Add(new TintLayer
                    {
                        Index = (ushort)Math.Clamp(t.Index, 0, ushort.MaxValue),
                        Preset = (short)Math.Clamp(t.Preset, short.MinValue, short.MaxValue),
                        // spec value is the engine's raw 0–100 (TINV/DLL scale); Mutagen views it 0–1
                        InterpolationValue = t.Value / 100f,
                        Color = t.Color is { } tc
                            ? System.Drawing.Color.FromArgb(Math.Clamp(tc.A, 0, 255),
                                Math.Clamp(tc.R, 0, 255), Math.Clamp(tc.G, 0, 255), Math.Clamp(tc.B, 0, 255))
                            : null,
                    });
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
                // Appearance refs (the record-local recipe half is set in pass 1 / BuildNpcs):
                // HCLF hair colour → a CLFM record; FTST face texture set (Mutagen property is
                // HeadTexture); PNAM head parts (hair/eyes/brows/scars — a modded HDPT such as a
                // high-poly head simply makes that mod a master of the output).
                Resolve($"npc '{n.EditorId}' hairColor", n.HairColor, fk => npcRec.HairColor.SetTo(fk));
                Resolve($"npc '{n.EditorId}' faceTexture", n.FaceTexture, fk => npcRec.HeadTexture.SetTo(fk));
                foreach (var hp in n.HeadParts)
                    Resolve($"npc '{n.EditorId}' headPart", hp, fk =>
                        npcRec.HeadParts.Add(new FormLink<IHeadPartGetter>(fk)));
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
                // Inventory items (CNTO): what the actor physically carries. A weapon/armor here is
                // auto-equipped if it's the actor's best, so this is how you arm an NPC; everything
                // drops as loot on death. Forward-ref-safe (an in-spec weapon may be declared after
                // the NPC) — same Resolve path as spells/factions. npc.Items may be null on a fresh
                // NPC, so initialize it. Mirrors the container CNTO build (ContainerEntry/Item).
                if (n.Items.Count > 0)
                {
                    npcRec.Items ??= new();
                    foreach (var it in n.Items)
                        Resolve($"npc '{n.EditorId}' item", it.Item, fk =>
                        {
                            var ci = new ContainerItem { Count = it.Count };
                            ci.Item.SetTo(fk);
                            npcRec.Items!.Add(new ContainerEntry { Item = ci });
                        });
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
