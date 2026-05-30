namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Build — generate a plugin from a structured spec (the data-driven generator).
    //  Layer between an LLM (NL -> spec) and Mutagen (spec -> valid plugin). Extend by
    //  adding a list to ModSpec + a loop here. Object in, object out: the caller owns
    //  reading the spec and writing the result; warnings are collected, never printed.
    // -------------------------------------------------------------------------------
    /// <summary>
    /// Build a mod from a spec. The result holds the in-memory <see cref="ISkyrimMod"/> (caller
    /// writes it), the non-fatal warnings, and build stats. Run <see cref="Validate"/> first.
    /// </summary>
    public static BuildResult Build(ModSpec spec, ModKey outputKey, BuildOptions? options = null)
    {
        var warnings = new List<string>();
        void Warn(string message) => warnings.Add(message);
        var mod = new SkyrimMod(outputKey, SkyrimRelease.SkyrimSE);

        // --- Master link-caches (read-only overlays of Skyrim.esm etc.) -----------------------
        // Used by (a) weapon/book *templating* — cloning a vanilla record so a generated item gets
        // a real model/animation/equip data and doesn't CRASH on equip/read — and (b) vanilla
        // cell/worldspace placement further down. Declared up here so the item-build loops reach it.
        var skyrimData = options?.SkyrimDataPath
            ?? Environment.GetEnvironmentVariable("MODFORGE_SKYRIM_DATA")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            ".local", "share", "Steam", "steamapps", "common", "Skyrim Special Edition", "Data");
        var masterCaches = new Dictionary<string, ILinkCache<ISkyrimMod, ISkyrimModGetter>?>(StringComparer.OrdinalIgnoreCase);
        var masterDisposables = new List<IDisposable>();

        ILinkCache<ISkyrimMod, ISkyrimModGetter>? MasterCache(string masterName)
        {
            if (masterCaches.TryGetValue(masterName, out var cached)) return cached;
            var path = Path.Combine(skyrimData, masterName);
            ILinkCache<ISkyrimMod, ISkyrimModGetter>? cache = null;
            if (!File.Exists(path))
                Warn($"  ! master '{masterName}' not found at {path} (set MODFORGE_SKYRIM_DATA to your Data folder)");
            else
            {
                // NOTE: Skyrim.esm is LOCALIZED, so its TranslatedString fields (Name/Description/
                // BookText) live in .STRINGS inside a BSA. We must NOT DeepCopy those (it triggers
                // an all-string-source resolve that needs the plugins.txt/load-order listings path,
                // absent headless on Linux). The weapon/book clone uses a TranslationMask to skip
                // exactly those fields (we override them anyway), so no string resolution happens.
                var getter = SkyrimMod.CreateFromBinaryOverlay(new ModPath(path), SkyrimRelease.SkyrimSE);
                masterDisposables.Add(getter);
                cache = getter.ToImmutableLinkCache<ISkyrimMod, ISkyrimModGetter>();
            }
            masterCaches[masterName] = cache;
            return cache;
        }

        // Resolve a vanilla record (by "<master>:0xFORMID" ref) to clone from. False (caller warns)
        // if the ref is malformed or the master/record can't be found.
        bool TryResolveTemplate<T>(string templateRef, out T? tmpl) where T : class, ISkyrimMajorRecordGetter
        {
            tmpl = null;
            if (string.IsNullOrWhiteSpace(templateRef)) return false;
            int colon = templateRef.IndexOf(':');
            if (colon <= 0 || !TryExternalRef(templateRef, out var fk)) return false;
            var cache = MasterCache(templateRef[..colon].Trim());
            return cache is not null && cache.TryResolve<T>(fk, out tmpl);
        }

        // Copy a master cell's ENVIRONMENT data (water height/type/textures, lighting + template,
        // region list, imagespace, music, acoustic space, encounter zone, location, ownership,
        // sky/weather-from-region) onto an override cell. CRITICAL: an override CELL that omits
        // these does NOT inherit them from the master at runtime — the engine resets them to
        // defaults. The worst offender is WaterHeight -> 0, which floods any terrain below sea
        // level (the "whole world is underwater" bug); a missing interior LightingTemplate -> a
        // pitch-black room. We DELIBERATELY skip the localized Name (copying it needs the BSA/
        // load-order string lookup, absent headless) and the child reference lists (we ADD our ref
        // to Temporary; vanilla refs stay in the master). Every field copied here is inline or a
        // FormLink — no string resolution, so no plugins.txt dependency.
        void CopyCellEnv(ICellGetter src, Cell dst)
        {
            dst.Flags = src.Flags;
            if (src.Grid is { } g)
                dst.Grid = new CellGrid { Point = new Noggog.P2Int(g.Point.X, g.Point.Y), Flags = g.Flags };
            dst.Lighting = src.Lighting?.DeepCopy();
            dst.WaterHeight = src.WaterHeight;
            dst.WaterNoiseTexture = src.WaterNoiseTexture;
            dst.WaterEnvironmentMap = src.WaterEnvironmentMap;
            dst.LightingTemplate.SetTo(src.LightingTemplate);
            dst.Water.SetTo(src.Water);
            dst.Location.SetTo(src.Location);
            dst.Owner.SetTo(src.Owner);
            dst.SkyAndWeatherFromRegion.SetTo(src.SkyAndWeatherFromRegion);
            dst.AcousticSpace.SetTo(src.AcousticSpace);
            dst.EncounterZone.SetTo(src.EncounterZone);
            dst.Music.SetTo(src.Music);
            dst.ImageSpace.SetTo(src.ImageSpace);
            if (src.Regions is { } regions)
            {
                dst.Regions = new Noggog.ExtendedList<IFormLinkGetter<IRegionGetter>>();
                foreach (var rg in regions) dst.Regions.Add(rg);
            }
        }

        // Copy a master WORLDSPACE's inline data onto a minimal override that hosts our new cell.
        // CRITICAL: a worldspace override that omits LandDefaults resets DefaultWaterHeight to 0 —
        // and Tamriel's real default is -14000, so any terrain between -14000 and 0 gets flooded
        // ("the whole world is underwater"). We copy land/water defaults, water forms, climate, map,
        // bounds, parent, lighting, etc. but NOT the localized Name or the giant child structures
        // (SubCells block tree — we build our own; TopCell/LargeReferences/OffsetData). All copied
        // fields are inline / FormLink / sub-objects — no localized string resolution. (Skipped:
        // the AssetLink LOD/water/map TEXTURE paths — cosmetic, and getter≠setter type.)
        void CopyWorldspaceEnv(IWorldspaceGetter src, Worldspace dst)
        {
            dst.Flags = src.Flags;
            dst.ObjectBoundsMin = src.ObjectBoundsMin;
            dst.ObjectBoundsMax = src.ObjectBoundsMax;
            dst.WorldMapOffsetScale = src.WorldMapOffsetScale;
            dst.DistantLodMultiplier = src.DistantLodMultiplier;
            dst.LodWaterHeight = src.LodWaterHeight;
            dst.LandDefaults = src.LandDefaults?.DeepCopy();   // DefaultWaterHeight (-14000) = THE flood fix
            dst.MaxHeight = src.MaxHeight?.DeepCopy();
            dst.MapData = src.MapData?.DeepCopy();
            dst.Parent = src.Parent?.DeepCopy();
            dst.Water.SetTo(src.Water);
            dst.LodWater.SetTo(src.LodWater);
            dst.Climate.SetTo(src.Climate);
            dst.Location.SetTo(src.Location);
            dst.EncounterZone.SetTo(src.EncounterZone);
            dst.InteriorLighting.SetTo(src.InteriorLighting);
            dst.Music.SetTo(src.Music);
        }

        foreach (var m in spec.MiscItems)
        {
            var r = mod.MiscItems.AddNew();
            // A model-less MISC doesn't crash (inventory is an icon) but has NO 3D mesh when dropped
            // in the world. Optional `template` clones a vanilla misc (e.g. Skyrim.esm:0x063B42
            // GemRuby) for its model + keywords. Mask out the localized Name (we set it below).
            if (!string.IsNullOrWhiteSpace(m.Template)
                && TryResolveTemplate<IMiscItemGetter>(m.Template, out var tmpl) && tmpl is not null)
                r.DeepCopyIn(tmpl, out _, new MiscItem.TranslationMask(defaultOn: true) { Name = false });
            r.EditorID = m.EditorId; r.Name = m.Name; r.Value = m.Value; r.Weight = m.Weight;
        }
        foreach (var b in spec.Books)
        {
            var r = mod.Books.AddNew();
            // A model-less BOOK CRASHES on read (the reading view loads the 3D book mesh). Clone a
            // vanilla book (`template`: "<master>:0xFORMID", e.g. Skyrim.esm:0x0ED161) so it gets a
            // model + sounds + keywords, then override identity + text. DeepCopyIn keeps OUR FormKey.
            if (!string.IsNullOrWhiteSpace(b.Template))
            {
                if (TryResolveTemplate<IBookGetter>(b.Template, out var tmpl) && tmpl is not null)
                    // Skip the localized strings (Name/BookText) — we set them below, and copying
                    // them would resolve .STRINGS via the (headless-absent) load-order listing.
                    r.DeepCopyIn(tmpl, out _, new Book.TranslationMask(defaultOn: true) { Name = false, BookText = false });
                else
                    Warn($"  ! book '{b.EditorId}': template '{b.Template}' not resolved — book will lack a model and may CRASH on read");
            }
            else
                Warn($"  ! book '{b.EditorId}': no `template` — a model-less book CRASHES on read; set template to a vanilla book (e.g. Skyrim.esm:0x0ED161 Book1CheapNordsArise)");
            r.EditorID = b.EditorId; r.Name = b.Name; r.BookText = b.Text;
        }
        foreach (var w in spec.Weapons)
        {
            var r = mod.Weapons.AddNew();
            // A bare WEAP (no model / first-person model / animation type / equip slot) CRASHES on
            // equip — structurally valid but not in-game functional. Clone a vanilla weapon of the
            // desired type (`template`: "<master>:0xFORMID", e.g. Skyrim.esm:0x012EB7 = IronSword)
            // via DeepCopyIn — that brings the model, 1st-person model, equip slot, animation type,
            // skill, sounds, impact + type/material keywords — then override identity + stats below.
            // DeepCopyIn keeps OUR FormKey (record stays in this plugin; the template's sub-forms
            // become FormLinks into its master).
            if (!string.IsNullOrWhiteSpace(w.Template))
            {
                if (TryResolveTemplate<IWeaponGetter>(w.Template, out var tmpl) && tmpl is not null)
                    // Skip the localized strings (Name/Description) — we set Name below, and copying
                    // them would resolve .STRINGS via the (headless-absent) load-order listing.
                    r.DeepCopyIn(tmpl, out _, new Weapon.TranslationMask(defaultOn: true) { Name = false, Description = false });
                else
                    Warn($"  ! weapon '{w.EditorId}': template '{w.Template}' not resolved — weapon will lack a model and may CRASH on equip");
            }
            else
                Warn($"  ! weapon '{w.EditorId}': no `template` — a model-less weapon CRASHES on equip; set template to a vanilla weapon (e.g. Skyrim.esm:0x012EB7 IronSword)");
            r.EditorID = w.EditorId; r.Name = w.Name;
            // Stats override the template's. speed/reach default to 1.0 so the weapon is swingable;
            // when templated, keep the clone's Data (anim type/skill/stagger/flags) and only restate
            // speed/reach + the basic stats.
            r.BasicStats = new WeaponBasicStats { Damage = w.Damage, Value = w.Value, Weight = w.Weight };
            r.Data ??= new WeaponData();
            r.Data.Speed = w.Speed > 0 ? w.Speed : (r.Data.Speed > 0 ? r.Data.Speed : 1.0f);
            r.Data.Reach = w.Reach > 0 ? w.Reach : (r.Data.Reach > 0 ? r.Data.Reach : 1.0f);
        }
        // NPCs + quests are kept in editorId->record maps so dialogue can reference them.
        var npcsByEd = new Dictionary<string, Npc>();
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

        var questsByEd = new Dictionary<string, Quest>();
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

        // Native dialogue: Quest -> DialogBranch -> DialogTopic -> DialogResponses(INFO).
        // (Writes valid records; making the line actually surface in-game still needs
        // quest-flag tuning + Proton testing — see docs/lifelike/gotchas.md.)
        int dialogueBuilt = 0;
        var branchesByQuest = new Dictionary<string, (Quest quest, List<DialogBranch> branches)>();
        foreach (var d in spec.Dialogue)
        {
            if (string.IsNullOrEmpty(d.QuestEditorId) || !questsByEd.TryGetValue(d.QuestEditorId, out var quest))
            {
                Warn($"  ! dialogue '{d.EditorId}' skipped: quest '{d.QuestEditorId}' not found in spec");
                continue;
            }

            var branch = mod.DialogBranches.AddNew();
            branch.EditorID = d.EditorId + "_Br";
            branch.Quest.SetTo(quest);
            branch.Category = DialogBranch.CategoryType.Player;
            // TopLevel = this branch is a top-level menu option shown the moment you talk to the NPC
            // (vs. a sub-branch reachable only from another topic). Without it the prompt never appears.
            branch.Flags = DialogBranch.Flag.TopLevel;

            var topic = mod.DialogTopics.AddNew();
            topic.EditorID = d.EditorId;
            topic.Quest.SetTo(quest);
            topic.Branch.SetTo(branch);
            topic.Category = DialogTopic.CategoryEnum.Topic;
            topic.Subtype = DialogTopic.SubtypeEnum.Custom;
            // SNAM must be the 4-char subtype code "CUST" (matches the Custom enum). Leaving it
            // RecordType.Null writes SNAM=0x00000000, which CRASHES the engine at load when it
            // builds the dialogue-topic index (vanilla Custom topics all carry SNAM='CUST').
            topic.SubtypeName = new RecordType("CUST");
            topic.Name = d.Prompt;
            topic.Priority = 50f;
            branch.StartingTopic.SetTo(topic);

            // INFO carries the spoken response(s). Leave ResponseData null (so it uses our own
            // Responses, not a shared INFO) and Prompt null (the menu line comes from topic.Name).
            // Flags (ENAM) + FavorLevel (CNAM) MUST be present: a vanilla player INFO always carries
            // both, and an INFO missing ENAM is treated as invalid — a topic whose only INFO is
            // invalid is silently dropped from the menu (so the topic never appears at all).
            var info = new DialogResponses(mod) { Flags = new DialogResponseFlags(), FavorLevel = FavorLevel.None };
            var emotion = Enum.TryParse<Emotion>(d.Emotion, ignoreCase: true, out var em) ? em : Emotion.Neutral;
            byte rn = 1;
            foreach (var line in d.Responses)
                info.Responses.Add(new DialogResponse { Text = line, ResponseNumber = rn++, Emotion = emotion, EmotionValue = d.EmotionValue });

            if (!string.IsNullOrEmpty(d.SpeakerNpcEditorId))
            {
                if (npcsByEd.TryGetValue(d.SpeakerNpcEditorId, out var speaker))
                {
                    var cond = new ConditionFloat
                    {
                        CompareOperator = CompareOperator.EqualTo,
                        ComparisonValue = 1f,
                        Data = new GetIsIDConditionData(),
                    };
                    ((GetIsIDConditionData)cond.Data).Object.Link.SetTo(speaker);
                    info.Conditions.Add(cond);
                }
                else
                    // No GetIsID gate => EVERY NPC would speak this line. Warn (validate also catches this).
                    Warn($"  ! dialogue '{d.EditorId}' speaker '{d.SpeakerNpcEditorId}' not found in spec — line has NO speaker gate (any NPC may say it)");
            }
            topic.Responses.Add(info);
            if (!branchesByQuest.TryGetValue(d.QuestEditorId, out var bag))
                branchesByQuest[d.QuestEditorId] = bag = (quest, new List<DialogBranch>());
            bag.branches.Add(branch);
            dialogueBuilt++;
        }

        // DialogView (DLVW) per quest: ties the player branches to the quest. Every vanilla dialogue
        // branch belongs to a view; without it the engine never serves the quest's player topics, so
        // the NPC can't even be talked to (activating it opens no dialogue camera). ENAM/DNAM mirror
        // vanilla defaults (4 zero bytes / single 1 byte).
        foreach (var (questEd, bag) in branchesByQuest)
        {
            var view = mod.DialogViews.AddNew();
            view.EditorID = questEd + "_View";
            view.Quest.SetTo(bag.quest);
            foreach (var b in bag.branches)
                view.Branches.Add(b);
            view.ENAM = new byte[] { 0, 0, 0, 0 };  // mirror vanilla DLVW
            view.DNAM = new byte[] { 1 };            // single flag byte vanilla views carry
        }

        // Hello (greeting) per speaking NPC: WITHOUT one the NPC is not conversable — activating it
        // never opens the dialogue menu, so the player topics above never surface (you just get
        // voicetype mumbles). Vanilla talkable NPCs all carry a Hello (Category=Misc, Subtype=Hello,
        // SNAM='HELO', no branch, gated on GetIsID). Emit one per (speaker, quest), keyed so multiple
        // topics from the same NPC share a single Hello.
        var npcSpecByEd = spec.Npcs.Where(n => !string.IsNullOrEmpty(n.EditorId))
                                   .GroupBy(n => n.EditorId).ToDictionary(g => g.Key, g => g.First());
        var helloDone = new HashSet<string>();
        foreach (var d in spec.Dialogue)
        {
            if (string.IsNullOrEmpty(d.SpeakerNpcEditorId)) continue;
            if (string.IsNullOrEmpty(d.QuestEditorId) || !questsByEd.TryGetValue(d.QuestEditorId, out var quest)) continue;
            if (!npcsByEd.TryGetValue(d.SpeakerNpcEditorId, out var speaker)) continue;
            if (!helloDone.Add(d.SpeakerNpcEditorId + "|" + quest.FormKey)) continue;

            var hello = mod.DialogTopics.AddNew();
            hello.EditorID = d.SpeakerNpcEditorId + "_Hello";
            hello.Quest.SetTo(quest);
            hello.Category = DialogTopic.CategoryEnum.Misc;
            hello.Subtype = DialogTopic.SubtypeEnum.Hello;
            hello.SubtypeName = new RecordType("HELO");
            hello.Priority = 50f;   // no Branch — Hello is NPC-initiated, not a player menu branch

            var greet = npcSpecByEd.TryGetValue(d.SpeakerNpcEditorId, out var ns) && !string.IsNullOrWhiteSpace(ns.Greeting)
                ? ns.Greeting
                : "Yes? What do you need?";
            var hinfo = new DialogResponses(mod) { Flags = new DialogResponseFlags(), FavorLevel = FavorLevel.None };
            hinfo.Responses.Add(new DialogResponse { Text = greet, ResponseNumber = 1, Emotion = Emotion.Neutral, EmotionValue = 50 });
            var hcond = new ConditionFloat
            {
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1f,
                Data = new GetIsIDConditionData(),
            };
            ((GetIsIDConditionData)hcond.Data).Object.Link.SetTo(speaker);
            hinfo.Conditions.Add(hcond);
            hello.Responses.Add(hinfo);
        }

        foreach (var me in spec.MagicEffects)
        {
            var r = mod.MagicEffects.AddNew();
            r.EditorID = me.EditorId;
            if (!string.IsNullOrEmpty(me.Name)) r.Name = me.Name;
            if (!string.IsNullOrEmpty(me.Description)) r.Description = me.Description;
            r.BaseCost = me.BaseCost;
            // Archetype: Type (what it does) + ActorValue (what it acts on). Association (summon/bound
            // form) is a ref, wired in pass 2. MagicSkill/ResistValue default to None (-1) when unset.
            var arch = new MagicEffectArchetype();
            if (Enum.TryParse<MagicEffectArchetype.TypeEnum>(me.Archetype, ignoreCase: true, out var at)) arch.Type = at;
            arch.ActorValue = Enum.TryParse<ActorValue>(me.ActorValue, ignoreCase: true, out var av) ? av : ActorValue.None;
            r.Archetype = arch;
            r.MagicSkill = Enum.TryParse<ActorValue>(me.MagicSkill, ignoreCase: true, out var sk) ? sk : ActorValue.None;
            r.ResistValue = Enum.TryParse<ActorValue>(me.ResistValue, ignoreCase: true, out var rv) ? rv : ActorValue.None;
            if (Enum.TryParse<CastType>(me.CastType, ignoreCase: true, out var mct)) r.CastType = mct;
            if (Enum.TryParse<TargetType>(me.TargetType, ignoreCase: true, out var mtt)) r.TargetType = mtt;
            foreach (var f in me.Flags)
                if (Enum.TryParse<MagicEffect.Flag>(f, ignoreCase: true, out var fl)) r.Flags |= fl;
        }
        foreach (var s in spec.Spells)
        {
            var r = mod.Spells.AddNew();
            r.EditorID = s.EditorId; r.Name = s.Name;
            if (Enum.TryParse<SpellType>(s.SpellType, ignoreCase: true, out var st)) r.Type = st;
            if (Enum.TryParse<CastType>(s.CastType, ignoreCase: true, out var ct)) r.CastType = ct;
            if (Enum.TryParse<TargetType>(s.TargetType, ignoreCase: true, out var tt)) r.TargetType = tt;
            if (s.BaseCost > 0) r.BaseCost = s.BaseCost;
            if (s.ChargeTime > 0) r.ChargeTime = s.ChargeTime;
        }
        foreach (var p in spec.Potions)
        {
            var r = mod.Ingestibles.AddNew();
            // A model-less ALCH drinks fine (no model load) but has NO 3D mesh when dropped. Optional
            // `template` clones a vanilla potion (e.g. Skyrim.esm:0x039BE5 RestoreHealth06) for the
            // bottle model + keywords + consume sound. Mask out the localized Name (set below); CLEAR
            // the cloned effects so pass-2 WireEffects adds only THIS spec's effects (no duplicates).
            if (!string.IsNullOrWhiteSpace(p.Template)
                && TryResolveTemplate<IIngestibleGetter>(p.Template, out var tmpl) && tmpl is not null)
            {
                r.DeepCopyIn(tmpl, out _, new Ingestible.TranslationMask(defaultOn: true) { Name = false });
                r.Effects.Clear();
            }
            r.EditorID = p.EditorId; r.Name = p.Name; r.Value = p.Value; r.Weight = p.Weight;
        }
        foreach (var a in spec.Armors)
        {
            var r = mod.Armors.AddNew();
            r.EditorID = a.EditorId; r.Name = a.Name;
            r.Value = a.Value; r.Weight = a.Weight; r.ArmorRating = a.ArmorRating;
            // BodyTemplate = the armor class (light/heavy/clothing) + which biped slots it fills.
            if (!string.IsNullOrEmpty(a.ArmorType) || a.Slots.Count > 0)
            {
                var bt = new BodyTemplate { ArmorType = ParseArmorType(a.ArmorType) };
                foreach (var slot in a.Slots)
                    if (Enum.TryParse<BipedObjectFlag>(slot, ignoreCase: true, out var f)) bt.FirstPersonFlags |= f;
                    else Warn($"  ! armor '{a.EditorId}' unknown slot '{slot}' (e.g. Body, Head, Hands, Feet, Forearms, Calves, Shield)");
                r.BodyTemplate = bt;
            }
        }
        foreach (var f in spec.Factions)
        {
            var r = mod.Factions.AddNew();
            r.EditorID = f.EditorId; r.Name = f.Name;
        }
        // Relationship (RELA): scalar Rank now; Parent/Child NPC refs wired in pass 2.
        foreach (var rel in spec.Relationships)
        {
            var r = mod.Relationships.AddNew();
            r.EditorID = rel.EditorId;
            r.Rank = Enum.TryParse<Relationship.RankType>(rel.Rank, ignoreCase: true, out var rk)
                ? rk : Relationship.RankType.Ally;
        }
        // Class (CLAS): no FormLinks (all enums/weight dicts), so fully built in pass 1. An npc's
        // `class` ref can point at one (resolved in pass 2 — it's in formKeyByEd by then). StatWeights
        // (Health/Magicka/Stamina) drive the actor's attribute distribution; SkillWeights favour skills.
        foreach (var cl in spec.Classes)
        {
            var r = mod.Classes.AddNew();
            r.EditorID = cl.EditorId;
            if (!string.IsNullOrEmpty(cl.Name)) r.Name = cl.Name;
            if (!string.IsNullOrEmpty(cl.Description)) r.Description = cl.Description;
            if (Enum.TryParse<Skill>(cl.Teaches, ignoreCase: true, out var teach)) r.Teaches = teach;
            r.MaxTrainingLevel = (byte)Math.Clamp(cl.MaxTrainingLevel, 0, 255);
            // All-zero stat weights would be a degenerate distribution; default to balanced.
            bool anyStat = cl.HealthWeight != 0 || cl.MagickaWeight != 0 || cl.StaminaWeight != 0;
            r.StatWeights[BasicStat.Health]  = (byte)Math.Clamp(anyStat ? cl.HealthWeight  : 1, 0, 255);
            r.StatWeights[BasicStat.Magicka] = (byte)Math.Clamp(anyStat ? cl.MagickaWeight : 1, 0, 255);
            r.StatWeights[BasicStat.Stamina] = (byte)Math.Clamp(anyStat ? cl.StaminaWeight : 1, 0, 255);
            foreach (var (skillName, w) in cl.SkillWeights)
                if (Enum.TryParse<Skill>(skillName, ignoreCase: true, out var sk))
                    r.SkillWeights[sk] = (byte)Math.Clamp(w, 0, 255);
                else Warn($"  ! class '{cl.EditorId}' skillWeight '{skillName}' is not a Skill — skipped");
        }
        foreach (var msg in spec.Messages)
        {
            var r = mod.Messages.AddNew();
            r.EditorID = msg.EditorId; r.Name = msg.Name; r.Description = msg.Description;
        }

        // Long-tail record types (pass 1: scalar fields; keywords/effects/outfit-items wired in pass 2).
        foreach (var i in spec.Ingredients)
        {
            var r = mod.Ingredients.AddNew();
            r.EditorID = i.EditorId; r.Name = i.Name; r.Value = i.Value; r.Weight = i.Weight;
        }
        foreach (var a in spec.Ammunitions)
        {
            var r = mod.Ammunitions.AddNew();
            r.EditorID = a.EditorId; r.Name = a.Name; r.Value = a.Value; r.Weight = a.Weight; r.Damage = a.Damage;
        }
        foreach (var s in spec.Scrolls)
        {
            var r = mod.Scrolls.AddNew();
            r.EditorID = s.EditorId; r.Name = s.Name; r.Value = s.Value; r.Weight = s.Weight;
            if (Enum.TryParse<SpellType>(s.SpellType, ignoreCase: true, out var st)) r.Type = st;
            if (Enum.TryParse<CastType>(s.CastType, ignoreCase: true, out var ct)) r.CastType = ct;
            if (Enum.TryParse<TargetType>(s.TargetType, ignoreCase: true, out var tt)) r.TargetType = tt;
            if (s.BaseCost > 0) r.BaseCost = s.BaseCost;
        }
        foreach (var sg in spec.SoulGems)
        {
            var r = mod.SoulGems.AddNew();
            r.EditorID = sg.EditorId; r.Name = sg.Name; r.Value = sg.Value; r.Weight = sg.Weight;
            if (Enum.TryParse<SoulGem.Level>(sg.MaximumCapacity, ignoreCase: true, out var lv)) r.MaximumCapacity = lv;
        }
        foreach (var k in spec.Keys)
        {
            var r = mod.Keys.AddNew();
            r.EditorID = k.EditorId; r.Name = k.Name; r.Value = k.Value; r.Weight = k.Weight;
        }
        foreach (var kw in spec.Keywords)
        {
            var r = mod.Keywords.AddNew();
            r.EditorID = kw.EditorId;
        }
        foreach (var o in spec.Outfits)
        {
            var r = mod.Outfits.AddNew();
            r.EditorID = o.EditorId; r.Items = new();
        }
        foreach (var st in spec.Statics)
        {
            var r = mod.Statics.AddNew();
            r.EditorID = st.EditorId;
            if (!string.IsNullOrEmpty(st.Model)) { r.Model = new Model(); r.Model.File.GivenPath = st.Model; }
        }
        foreach (var ac in spec.Activators)
        {
            var r = mod.Activators.AddNew();
            r.EditorID = ac.EditorId; r.Name = ac.Name;
            if (!string.IsNullOrEmpty(ac.Model)) { r.Model = new Model(); r.Model.File.GivenPath = ac.Model; }
        }

        // Leveled lists + containers: create the records now (scalar fields + flags); their
        // entries reference other forms, so they're wired in pass 2 with the ref resolver.
        foreach (var li in spec.LeveledItems)
        {
            var r = mod.LeveledItems.AddNew();
            r.EditorID = li.EditorId;
            r.ChanceNone = new Noggog.Percent(Math.Clamp(li.ChanceNone, 0, 100) / 100.0);
            r.Flags = ParseFlags<LeveledItem.Flag>(li.Flags);
            r.Entries = new();
        }
        foreach (var ln in spec.LeveledNpcs)
        {
            var r = mod.LeveledNpcs.AddNew();
            r.EditorID = ln.EditorId;
            r.ChanceNone = new Noggog.Percent(Math.Clamp(ln.ChanceNone, 0, 100) / 100.0);
            r.Flags = ParseFlags<LeveledNpc.Flag>(ln.Flags);
            r.Entries = new();
        }
        foreach (var ct in spec.Containers)
        {
            var r = mod.Containers.AddNew();
            r.EditorID = ct.EditorId; r.Name = ct.Name; r.Weight = ct.Weight;
            r.Items = new();
        }

        // ConstructibleObject (COBJ): created in pass 1 (editorId only, so it registers in the
        // formKey table); createdObject/workbench/component refs are wired in pass 2.
        foreach (var co in spec.Recipes)
        {
            var r = mod.ConstructibleObjects.AddNew();
            r.EditorID = co.EditorId;
            r.CreatedObjectCount = (ushort)Math.Clamp(co.Count, 1, ushort.MaxValue);
        }

        // CombatStyle (CSTY): no FormLinks (all floats + a Flag enum), so fully built in pass 1.
        // An npc's `combatStyle` ref can point at one (resolved in pass 2 alongside race/class/etc.).
        // The six EquipmentScoreMult* fields are the AI's weapon-preference scores — set Magic high
        // for a mage NPC. See cstydiag on csVampireMagic 0x02DFB5 for the gold-standard mage values.
        foreach (var cs in spec.CombatStyles)
        {
            var r = mod.CombatStyles.AddNew();
            r.EditorID = cs.EditorId;
            r.OffensiveMult = cs.OffensiveMult;
            r.DefensiveMult = cs.DefensiveMult;
            r.GroupOffensiveMult = cs.GroupOffensiveMult;
            r.EquipmentScoreMultMelee   = cs.EquipMultMelee;
            r.EquipmentScoreMultMagic   = cs.EquipMultMagic;
            r.EquipmentScoreMultRanged  = cs.EquipMultRanged;
            r.EquipmentScoreMultShout   = cs.EquipMultShout;
            r.EquipmentScoreMultUnarmed = cs.EquipMultUnarmed;
            r.EquipmentScoreMultStaff   = cs.EquipMultStaff;
            r.AvoidThreatChance = cs.AvoidThreatChance;
            r.Flags = ParseFlags<Mutagen.Bethesda.Skyrim.CombatStyle.Flag>(cs.Flags);
        }

        // AI Package (PACK): pass-1 sets scalar fields (flags, interrupt flags, speed, schedule).
        // Template/CombatStyle/OwnerQuest + Sandbox `Data` dictionary inputs are wired in pass 2.
        // Type is Package (=18), never PackageTemplate — those are vanilla-defined.
        foreach (var pk in spec.Packages)
        {
            var r = mod.Packages.AddNew();
            r.EditorID = pk.EditorId;
            r.Type = Mutagen.Bethesda.Skyrim.Package.Types.Package;
            // Flags + interrupt flags (lifelike-NPC switches: HellosToPlayer / AllowIdleChatter /
            // WorldInteractions / RandomConversations / ReactionToPlayerActions / …)
            r.Flags = ParseFlags<Package.Flag>(pk.Flags);
            r.InterruptFlags = ParseFlags<Package.InterruptFlag>(pk.InterruptFlags);
            if (Enum.TryParse<Package.Speed>(pk.PreferredSpeed, ignoreCase: true, out var spd))
                r.PreferredSpeed = spd;
            // Schedule — -1/0 = "any" (vanilla DefaultSandbox uses month=-1 hour=-1 minute=-1).
            r.ScheduleMonth = (sbyte)Math.Clamp(pk.Schedule.Month, -1, 11);
            if (Enum.TryParse<Package.DayOfWeek>(pk.Schedule.DayOfWeek, ignoreCase: true, out var dow))
                r.ScheduleDayOfWeek = dow;
            r.ScheduleDate = (byte)Math.Clamp(pk.Schedule.Date, 0, 31);
            r.ScheduleHour = (sbyte)Math.Clamp(pk.Schedule.Hour, -1, 23);
            r.ScheduleMinute = (sbyte)Math.Clamp(pk.Schedule.Minute, -1, 59);
            r.ScheduleDurationInMinutes = Math.Max(0, pk.Schedule.DurationInMinutes);
        }

        // Interior cells nest CellBlock(type 2, label=block) -> CellSubBlock(type 3, label=sub) ->
        // Cell, and Skyrim groups them BY FORMID: block = id % 10, sub = (id / 10) % 10 (decimal,
        // 24-bit ID — verified by walking Skyrim.esm, e.g. WhiterunBanneredMare 0x01605E/dec 90206
        // is in block 6 / sub 0). This is CRITICAL for OVERRIDES: a vanilla-cell override placed in
        // the wrong block GRUP is never matched against the master cell, so the engine SILENTLY
        // IGNORES it (the It.10 bug — placed objects + lighting didn't apply; we'd hardcoded 0/0).
        // get-or-add the correct (block, sub) GRUP for any cell's FormID.
        var cellsByEd = new Dictionary<string, Cell>();
        var interiorSubs = new Dictionary<(int Block, int Sub), CellSubBlock>();
        CellSubBlock InteriorSubFor(FormKey fk)
        {
            int id = (int)fk.ID;
            int blk = id % 10, sub = (id / 10) % 10;
            if (interiorSubs.TryGetValue((blk, sub), out var cached)) return cached;
            var block = mod.Cells.Records.FirstOrDefault(
                b => b.BlockNumber == blk && b.GroupType == GroupTypeEnum.InteriorCellBlock);
            if (block is null)
            {
                block = new CellBlock { BlockNumber = blk, GroupType = GroupTypeEnum.InteriorCellBlock };
                mod.Cells.Records.Add(block);
            }
            var subBlock = new CellSubBlock { BlockNumber = sub, GroupType = GroupTypeEnum.InteriorCellSubBlock };
            block.SubBlocks.Add(subBlock);
            interiorSubs[(blk, sub)] = subBlock;
            return subBlock;
        }
        foreach (var c in spec.Cells)
        {
            var cell = new Cell(mod, c.EditorId) { Flags = Cell.Flag.IsInteriorCell };
            // A cell with no Lighting/LightingTemplate renders PITCH BLACK in-game. Optionally copy a
            // vanilla interior cell's lighting/water ENV (same `template` pattern as It.8 item models):
            // point `template` at a known-good vanilla interior (e.g. a player home). Floor still comes
            // from a placed static — without one the player falls into the void.
            if (!string.IsNullOrWhiteSpace(c.Template))
            {
                if (TryResolveTemplate<ICellGetter>(c.Template, out var tmplCell) && tmplCell is not null)
                {
                    if (tmplCell.Flags.HasFlag(Cell.Flag.IsInteriorCell)) CopyCellEnv(tmplCell, cell);
                    else Warn($"  ! cell '{c.EditorId}' template '{c.Template}' is exterior — ignored (need an interior cell)");
                }
                else Warn($"  ! cell '{c.EditorId}' template '{c.Template}' unresolved — created without lighting (may render black)");
            }
            cell.Flags |= Cell.Flag.IsInteriorCell;   // CopyCellEnv overwrote Flags — keep it interior
            if (!string.IsNullOrEmpty(c.Name)) cell.Name = c.Name;
            InteriorSubFor(cell.FormKey).Cells.Add(cell);
            if (!string.IsNullOrEmpty(c.EditorId)) cellsByEd[c.EditorId] = cell;
        }

        // --- pass 2: resolve cross-record references by editorId -> FormLink ---
        // All records exist now, so build one editorId -> FormKey table and wire links
        // that may point forward (e.g. an NPC listed before the faction it belongs to).
        var formKeyByEd = new Dictionary<string, FormKey>();
        var recordsByEd = new Dictionary<string, IMajorRecord>();
        foreach (var r in mod.EnumerateMajorRecords())
            if (!string.IsNullOrEmpty(r.EditorID))
            { formKeyByEd[r.EditorID!] = r.FormKey; recordsByEd[r.EditorID!] = r; }

        // Resolve a ref (in-spec editorId OR external <master>:0xFORMID) and run `set`.
        int linksWired = 0, extLinks = 0;
        void Resolve(string what, string refStr, Action<FormKey> set)
        {
            if (string.IsNullOrWhiteSpace(refStr)) return;
            if (TryResolveRef(refStr, formKeyByEd, out var fk))
            {
                set(fk);
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
            }
            else Warn($"  ! {what} ref '{refStr}' unresolved (need in-spec editorId or <master>:0xFORMID)");
        }

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
            foreach (var factionRef in n.Factions)
                Resolve($"npc '{n.EditorId}' faction", factionRef, fk =>
                {
                    var rp = new RankPlacement { Rank = 0 };
                    rp.Faction.SetTo(fk);
                    npcRec.Factions.Add(rp);
                });
        }

        // Relationship Parent/Child NPC refs (Parent usually the in-spec NPC, Child the player).
        foreach (var rel in spec.Relationships)
        {
            if (!recordsByEd.TryGetValue(rel.EditorId, out var rec) || rec is not IRelationship r) continue;
            Resolve($"relationship '{rel.EditorId}' parent", rel.Parent, fk => r.Parent.SetTo(fk));
            Resolve($"relationship '{rel.EditorId}' child",  rel.Child,  fk => r.Child.SetTo(fk));
        }

        // Keywords on armor/weapon/misc (all implement the IKeyworded aspect).
        void WireKeywords(string ed, List<string> kws)
        {
            if (kws.Count == 0) return;
            if (!recordsByEd.TryGetValue(ed, out var rec) || rec is not IKeyworded<IKeywordGetter> kw)
            { Warn($"  ! '{ed}' takes no keywords (or not found)"); return; }
            kw.Keywords ??= new();
            foreach (var kref in kws)
                Resolve($"'{ed}' keyword", kref, fk => kw.Keywords!.Add(new FormLink<IKeywordGetter>(fk)));
        }
        foreach (var a in spec.Armors) WireKeywords(a.EditorId, a.Keywords);
        foreach (var w in spec.Weapons) WireKeywords(w.EditorId, w.Keywords);
        foreach (var m in spec.MiscItems) WireKeywords(m.EditorId, m.Keywords);
        foreach (var i in spec.Ingredients) WireKeywords(i.EditorId, i.Keywords);
        foreach (var a in spec.Ammunitions) WireKeywords(a.EditorId, a.Keywords);
        foreach (var s in spec.Scrolls) WireKeywords(s.EditorId, s.Keywords);
        foreach (var sg in spec.SoulGems) WireKeywords(sg.EditorId, sg.Keywords);
        foreach (var k in spec.Keys) WireKeywords(k.EditorId, k.Keywords);
        foreach (var ac in spec.Activators) WireKeywords(ac.EditorId, ac.Keywords);

        // Magic effects on spells/potions (both implement IHasEffects). Each Effect links a
        // vanilla/in-spec MagicEffect (a ref) and carries EffectData (magnitude/area/duration).
        void WireEffects(string ed, List<EffectSpec> effects)
        {
            if (effects.Count == 0) return;
            if (!recordsByEd.TryGetValue(ed, out var rec) || rec is not IHasEffects he)
            { Warn($"  ! '{ed}' takes no magic effects (or not found)"); return; }
            foreach (var es in effects)
                Resolve($"'{ed}' effect", es.MagicEffect, fk =>
                {
                    var eff = new Effect();
                    eff.BaseEffect.SetTo(fk);
                    eff.Data = new EffectData { Magnitude = es.Magnitude, Area = es.Area, Duration = es.Duration };
                    he.Effects.Add(eff);
                });
        }
        foreach (var s in spec.Spells) WireEffects(s.EditorId, s.Effects);
        // Spell EquipType (EQUP ref) — needed for a hand spell to be equippable/castable.
        foreach (var s in spec.Spells)
        {
            if (string.IsNullOrWhiteSpace(s.EquipType)) continue;
            if (recordsByEd.TryGetValue(s.EditorId, out var rec) && rec is ISpell sp)
                Resolve($"spell '{s.EditorId}' equipType", s.EquipType, fk => sp.EquipmentType.SetTo(fk));
        }
        foreach (var p in spec.Potions) WireEffects(p.EditorId, p.Effects);
        foreach (var i in spec.Ingredients) WireEffects(i.EditorId, i.Effects);
        foreach (var s in spec.Scrolls) WireEffects(s.EditorId, s.Effects);

        // MagicEffect refs wired in pass 2 (may point forward, or at vanilla forms): the archetype
        // `association` (summon/bound form) + the visual `projectile`/`castingArt`/`hitEffectArt`/
        // `explosion`. Resolve() skips empty refs, so only authored ones are wired.
        foreach (var me in spec.MagicEffects)
        {
            if (!recordsByEd.TryGetValue(me.EditorId, out var rec) || rec is not IMagicEffect mgef) continue;
            if (mgef.Archetype is IMagicEffectArchetype a)
                Resolve($"magicEffect '{me.EditorId}' association", me.Association, fk => a.Association.SetTo(fk));
            Resolve($"magicEffect '{me.EditorId}' projectile",   me.Projectile,   fk => mgef.Projectile.SetTo(fk));
            Resolve($"magicEffect '{me.EditorId}' castingArt",   me.CastingArt,   fk => mgef.CastingArt.SetTo(fk));
            Resolve($"magicEffect '{me.EditorId}' hitEffectArt", me.HitEffectArt, fk => mgef.HitEffectArt.SetTo(fk));
            Resolve($"magicEffect '{me.EditorId}' explosion",    me.Explosion,    fk => mgef.Explosion.SetTo(fk));
        }

        // AI Package (PACK) pass-2: resolve template/combatStyle/ownerQuest refs and dispatch Data
        // slot filling by the vanilla procedure template referenced. Each template has its own
        // sbyte-indexed named slot schema (discover via `packagediag <Skyrim.esm> <templateFormId>`);
        // we mirror what vanilla concrete packages emit so the engine gets explicit values.
        const uint SandboxTemplateId  = 0x01C254;  // Skyrim.esm: editorId "Sandbox"   — 12 slots
        const uint TravelTemplateId   = 0x016FAA;  // Skyrim.esm: editorId "Travel"    —  3 slots
        const uint UseMagicTemplateId = 0x0504F5;  // Skyrim.esm: editorId "UseMagic"  — 11 active slots (2-12)
        const uint PatrolTemplateId   = 0x017723;  // Skyrim.esm: editorId "Patrol"    —  6 slots
        const uint FollowTemplateId   = 0x019B2C;  // Skyrim.esm: editorId "Follow"    —  6 slots
        const uint EscortTemplateId   = 0x023B73;  // Skyrim.esm: editorId "Escort"    — 15 slots (9 active)
        var skyrimEsm = ModKey.FromNameAndExtension("Skyrim.esm");

        // Some templates' SingleRef slot 0 points at a PLACEMENT created later (the placement loop
        // runs after this PACK loop) — or at a vanilla ref (e.g. the player). Defer that ref-wiring
        // uniformly: collect (package, slot, slotName, editorId, ref) here, then after placements
        // register their editorIds, emit each as a PackageTargetSpecificReference. Used by Patrol
        // ("Patrol Start", slot 0) and Follow ("Target to Follow", slot 0).
        var deferredTargetWires = new List<(IPackage Pack, sbyte Slot, string SlotName, string Ed, string Ref)>();
        // Same deferred problem for PackageDataLocation slots that point at an in-spec placement
        // (Escort "Destination", slot 3 — naturally an authored marker created in the placement loop
        // below). Collected here, resolved after placements register via MakeLocationSlot.
        var deferredLocationWires = new List<(IPackage Pack, sbyte Slot, string SlotName, string Ed, string Ref, uint Radius)>();

        // Build a PackageDataLocation: an authored placed-ref → LocationTarget anchored at that
        // ref, else LocationFallback(NearSelf) — anchors at the actor's current position with no
        // external dependency. NEVER use NearEditorLocation: it needs a CK-set Editor Location on
        // the NPC; Mutagen-generated NPCs don't have one, so sandbox/travel silently no-ops in-game.
        PackageDataLocation MakeLocationSlot(string slotName, string ownerLabel, string refStr, uint radius)
        {
            if (!string.IsNullOrWhiteSpace(refStr)
                && TryResolveRef(refStr, formKeyByEd, out var fk))
            {
                linksWired++;
                if (LooksExternalRef(refStr)) extLinks++;
                return new PackageDataLocation
                {
                    Name = slotName,
                    Location = new LocationTargetRadius
                    {
                        Target = new LocationTarget { Link = new FormLink<IPlacedGetter>(fk) },
                        Radius = radius,
                    }
                };
            }
            if (!string.IsNullOrWhiteSpace(refStr))
                Warn($"  ! {ownerLabel} location '{refStr}' unresolved — falling back to NearSelf");
            return new PackageDataLocation
            {
                Name = slotName,
                Location = new LocationTargetRadius
                {
                    Target = new LocationFallback { Type = LocationTargetRadius.LocationType.NearSelf },
                    Radius = radius,
                }
            };
        }

        foreach (var pk in spec.Packages)
        {
            if (!recordsByEd.TryGetValue(pk.EditorId, out var rec) || rec is not IPackage pack) continue;
            Resolve($"package '{pk.EditorId}' template",    pk.Template,    fk => pack.PackageTemplate.SetTo(fk));
            Resolve($"package '{pk.EditorId}' combatStyle", pk.CombatStyle, fk => pack.CombatStyle.SetTo(fk));
            Resolve($"package '{pk.EditorId}' ownerQuest",  pk.OwnerQuest,  fk => pack.OwnerQuest.SetTo(fk));

            var tfk = pack.PackageTemplate.FormKey;
            if (tfk.IsNull || tfk.ModKey != skyrimEsm)
            {
                if (!string.IsNullOrWhiteSpace(pk.Template))
                    Warn($"  ! package '{pk.EditorId}': template '{pk.Template}' not a Skyrim.esm template; no Data overrides emitted (template defaults apply)");
                continue;
            }

            if (tfk.ID == SandboxTemplateId)
            {
                // Mirrors DefaultSandboxCurrentLocation256 (Skyrim.esm:0x0956B8) — concrete sandboxes
                // explicitly set all 12 named slots; we do the same so behaviour is deterministic.
                var sb = pk.Sandbox;
                pack.Data[0] = MakeLocationSlot("Location", $"package '{pk.EditorId}' sandbox", sb.Location, sb.Radius);
                void SBool(sbyte slot, string name, bool? user, bool def)
                    => pack.Data[slot] = new PackageDataBool { Name = name, Data = user ?? def };
                SBool(1,  "Allow Eating",            sb.AllowEating,           true);
                SBool(3,  "Allow Sleeping",          sb.AllowSleeping,         true);
                SBool(4,  "Allow Conversation",      sb.AllowConversation,     true);
                SBool(5,  "Allow Idle Markers",      sb.AllowIdleMarkers,      true);
                SBool(6,  "Allow Sitting",           sb.AllowSitting,          true);
                SBool(7,  "Allow Wandering",         sb.AllowWandering,        true);
                SBool(14, "Unlock On Arrival?",      sb.UnlockOnArrival,       false);
                SBool(25, "Prefered Path Only?",     sb.PreferredPathOnly,     false);
                SBool(27, "RideHorseIfPossible",     sb.RideHorseIfPossible,   false);
                SBool(31, "Allow Special Furniture", sb.AllowSpecialFurniture, true);
                pack.Data[29] = new PackageDataFloat { Name = "Energy", Data = sb.Energy ?? 50f };
            }
            else if (tfk.ID == TravelTemplateId)
            {
                // Travel template has just 3 slots — 0=Place (PackageDataLocation), 2=RideHorse,
                // 4=PreferPath. `place` is REQUIRED in practice — Travel without a destination
                // ref means "travel to nowhere" (NearSelf), i.e. the NPC just stands. radius=0 is
                // template default (= arrive at exact point); set non-zero for "arrive within R".
                var tv = pk.Travel;
                if (string.IsNullOrWhiteSpace(tv.Place))
                    Warn($"  ! package '{pk.EditorId}' travel: no `place` ref — Travel will fall back to NearSelf (NPC stays put)");
                pack.Data[0] = MakeLocationSlot("Place to Travel", $"package '{pk.EditorId}' travel", tv.Place, tv.Radius);
                pack.Data[2] = new PackageDataBool { Name = "Ride Horse if possible?", Data = tv.RideHorse ?? false };
                pack.Data[4] = new PackageDataBool { Name = "Prefer Preferred Path?", Data = tv.PreferPath ?? false };
            }
            else if (tfk.ID == UseMagicTemplateId)
            {
                // UseMagic template active slots are 2-12 (slots 0/1 are inherited APackageData
                // placeholders; we don't touch them — vanilla concrete packages also skip them).
                // Slot 3 (Spell) MUST be a PackageTargetObjectID with a FormLink to the specific
                // SPEL record — discovered by scanning all 46 vanilla UseMagic packages with
                // `pkgsbytemplate` (round-1 in-game failure: PackageTargetObjectType silently
                // no-ops). Slot 4 (Target) MUST be set: PackageTargetSelf for self-cast spells
                // (Candlelight/Healing/Ward), PackageTargetSpecificReference for cast-at-X.
                var um = pk.UseMagic;
                pack.Data[2] = MakeLocationSlot("Location", $"package '{pk.EditorId}' usemagic", um.Location, um.Radius);

                if (string.IsNullOrWhiteSpace(um.Spell))
                {
                    Warn($"  ! package '{pk.EditorId}' usemagic: no `spell` ref — package will no-op (engine has nothing to cast)");
                }
                else if (TryResolveRef(um.Spell, formKeyByEd, out var spellFk))
                {
                    linksWired++;
                    if (LooksExternalRef(um.Spell)) extLinks++;
                    pack.Data[3] = new PackageDataTarget
                    {
                        Name = "Spell",
                        Type = PackageDataTarget.Types.Target,
                        Target = new PackageTargetObjectID { Reference = new FormLink<IObjectIdGetter>(spellFk) },
                    };
                }
                else
                {
                    Warn($"  ! package '{pk.EditorId}' usemagic spell '{um.Spell}' unresolved — package will no-op");
                }

                if (!string.IsNullOrWhiteSpace(um.Target)
                    && TryResolveRef(um.Target, formKeyByEd, out var tgtFk))
                {
                    linksWired++;
                    if (LooksExternalRef(um.Target)) extLinks++;
                    pack.Data[4] = new PackageDataTarget
                    {
                        Name = "Target",
                        Type = PackageDataTarget.Types.SingleRef,
                        Target = new PackageTargetSpecificReference { Reference = new FormLink<IPlacedGetter>(tgtFk) },
                    };
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(um.Target))
                        Warn($"  ! package '{pk.EditorId}' usemagic target '{um.Target}' unresolved — defaulting to PackageTargetSelf");
                    pack.Data[4] = new PackageDataTarget
                    {
                        Name = "Target",
                        Type = PackageDataTarget.Types.SingleRef,
                        Target = new PackageTargetSelf(),
                    };
                }

                void UBool(sbyte slot, string name, bool? user, bool def)
                    => pack.Data[slot] = new PackageDataBool { Name = name, Data = user ?? def };
                void UFloat(sbyte slot, string name, float? user, float def)
                    => pack.Data[slot] = new PackageDataFloat { Name = name, Data = user ?? def };
                void UInt(sbyte slot, string name, uint? user, uint def)
                    => pack.Data[slot] = new PackageDataInt { Name = name, Data = user ?? def };
                UBool(5,  "HoldWhenBlocked", um.HoldWhenBlocked, true);
                UFloat(6, "CastTimeMin",     um.CastTimeMin,     2f);
                UFloat(7, "CastTimeMax",     um.CastTimeMax,     3f);
                UFloat(8, "CooldownTimeMin", um.CooldownTimeMin, 1f);
                UFloat(9, "CooldownTimeMax", um.CooldownTimeMax, 3f);
                UInt (10, "NumToCastMin",    um.NumToCastMin,    1u);
                UInt (11, "NumToCastMax",    um.NumToCastMax,    1u);
                UBool(12, "DualCast",        um.DualCast,        false);
            }
            else if (tfk.ID == PatrolTemplateId)
            {
                // Patrol slots: 0 Patrol Start (deferred — points at a marker placement created in
                // the placement loop below), 1 Patrol Radius (float), 2 Repeatable?, 4 Start At
                // Nearest?, 6 Ride Horse if Possible?, 8 Static Pathing?. The route itself is the
                // linkedRefs chain off the start marker, wired after placements.
                var pt = pk.Patrol;
                if (string.IsNullOrWhiteSpace(pt.Start))
                    Warn($"  ! package '{pk.EditorId}' patrol: no `start` ref — NPC has no route and won't patrol");
                else
                    deferredTargetWires.Add((pack, 0, "Patrol Start", pk.EditorId, pt.Start));
                pack.Data[1] = new PackageDataFloat { Name = "Patrol Radius",            Data = pt.Radius ?? 150f };
                pack.Data[2] = new PackageDataBool  { Name = "Repeatable?",              Data = pt.Repeatable ?? true };
                pack.Data[4] = new PackageDataBool  { Name = "Start At Nearest?",        Data = pt.StartAtNearest ?? true };
                pack.Data[6] = new PackageDataBool  { Name = "Ride Horse if Possible?",  Data = pt.RideHorse ?? false };
                pack.Data[8] = new PackageDataBool  { Name = "Static Pathing?",          Data = pt.StaticPathing ?? false };
            }
            else if (tfk.ID == FollowTemplateId)
            {
                // Follow slots: 0 Target to Follow (deferred — defaults to the player 0x000014, all
                // vanilla "FollowsPlayer" packages emit PackageTargetSpecificReference(000014); can
                // also be an in-spec NPC placement to follow another actor), 1 Min Radius (float),
                // 2 Max Radius (float), 4 Accompany?, 6 Ride Horse?, 8 Need LOS?.
                var fo = pk.Follow;
                var tgt = string.IsNullOrWhiteSpace(fo.Target) ? "Skyrim.esm:0x000014" : fo.Target;
                deferredTargetWires.Add((pack, 0, "Target to Follow", pk.EditorId, tgt));
                pack.Data[1] = new PackageDataFloat { Name = "Min Radius:", Data = fo.MinRadius ?? 128f };
                pack.Data[2] = new PackageDataFloat { Name = "Max Radius:", Data = fo.MaxRadius ?? 256f };
                pack.Data[4] = new PackageDataBool  { Name = "Accompany?", Data = fo.Accompany ?? true };
                pack.Data[6] = new PackageDataBool  { Name = "Ride Horse?", Data = fo.RideHorse ?? false };
                pack.Data[8] = new PackageDataBool  { Name = "Need LOS?", Data = fo.NeedLineOfSight ?? false };
            }
            else if (tfk.ID == EscortTemplateId)
            {
                // Escort slots (9 active of 15): 11 Target to Escort (deferred SingleRef — who the NPC
                // leads; defaults to the player 0x000014, like Follow), 2 Number of Followers (int),
                // 3 Destination (PackageDataLocation — deferred so it can be an authored marker, REQUIRED),
                // 4 Distance to Wait for Follower(s) (float), 5 Follower Min Distance, 6 Follower Max
                // Distance, 13 Ride Horse?, 15 PreferPreferredPath?, 17 Run If Behind Distance.
                // Escort = the NPC LEADS the escorted target to the destination, pausing if they lag past
                // "Distance to Wait". The dual of Follow: here the NPC walks ahead, the target tags along.
                var es = pk.Escort;
                var tgt = string.IsNullOrWhiteSpace(es.Target) ? "Skyrim.esm:0x000014" : es.Target;
                deferredTargetWires.Add((pack, 11, "Target to Escort", pk.EditorId, tgt));
                if (string.IsNullOrWhiteSpace(es.Destination))
                    Warn($"  ! package '{pk.EditorId}' escort: no `destination` ref — Escort will fall back to NearSelf (NPC won't lead anywhere)");
                deferredLocationWires.Add((pack, 3, "Destination", pk.EditorId, es.Destination, es.Radius));
                pack.Data[2]  = new PackageDataInt   { Name = "Number of Followers:",            Data = es.NumberOfFollowers ?? 1u };
                pack.Data[4]  = new PackageDataFloat { Name = "Distance to Wait for Follower(s):", Data = es.WaitDistance ?? 512f };
                pack.Data[5]  = new PackageDataFloat { Name = "Follower Min Distance:",          Data = es.FollowerMinDistance ?? 120f };
                pack.Data[6]  = new PackageDataFloat { Name = "Follower Max Distance:",          Data = es.FollowerMaxDistance ?? 256f };
                pack.Data[13] = new PackageDataBool  { Name = "Ride Horse?",                     Data = es.RideHorse ?? false };
                pack.Data[15] = new PackageDataBool  { Name = "PreferPreferredPath?",            Data = es.PreferPreferredPath ?? false };
                pack.Data[17] = new PackageDataFloat { Name = "Run If Behind Distance",          Data = es.RunIfBehindDistance ?? 500f };
            }
            else
            {
                Warn($"  ! package '{pk.EditorId}': template {tfk} is not yet supported (known: sandbox=0x01C254, travel=0x016FAA, usemagic=0x0504F5, patrol=0x017723, follow=0x019B2C, escort=0x023B73) — emitting package with no Data overrides; template defaults apply");
            }
        }

        // NPC.Packages — assign each ref'd package to the NPC's package list (run-order: the engine
        // picks the first one whose conditions+schedule match, so order matters; we add in spec order).
        foreach (var n in spec.Npcs)
        {
            if (n.Packages.Count == 0) continue;
            if (!npcsByEd.TryGetValue(n.EditorId, out var npcRec)) continue;
            foreach (var pkgRef in n.Packages)
                Resolve($"npc '{n.EditorId}' package", pkgRef, fk =>
                    npcRec.Packages.Add(new FormLink<IPackageGetter>(fk)));
        }

        // Outfit (OTFT) contents: each item is a ref (in-spec armor/weapon or external).
        foreach (var o in spec.Outfits)
        {
            if (!recordsByEd.TryGetValue(o.EditorId, out var rec) || rec is not IOutfit outfit) continue;
            outfit.Items ??= new();
            foreach (var itemRef in o.Items)
                Resolve($"outfit '{o.EditorId}' item", itemRef, fk => outfit.Items!.Add(new FormLink<IOutfitTargetGetter>(fk)));
        }

        // World placement: put a base form (npc/object) into a cell at a position/rotation.
        // The target cell is either an in-spec interior cell, or (phase 2) a VANILLA cell we
        // override. NPC base -> PlacedNpc (ACHR), other -> PlacedObject (REFR).
        //
        // Vanilla-cell override (the careful bit): we resolve the cell's *context* from a link
        // cache over its master and GetOrAddAsOverride it into our mod (Mutagen puts it in the
        // right block/worldspace). GetOrAddAsOverride deep-copies the whole cell INCLUDING its
        // children, so we immediately CLEAR Persistent+Temporary — the vanilla references still
        // come from the master at load time (omitting them doesn't delete them); we only ADD our
        // new ref. That avoids re-stating (and conflicting on) every vanilla reference.
        // (master link-cache infra `MasterCache` + `TryResolveTemplate` are defined at the top of
        // Build now, so the weapon/book item loops can clone vanilla templates.)
        var vanillaCellOverrides = new Dictionary<FormKey, ICell>();

        ICell? VanillaCellOverride(string cellRef)
        {
            if (!TryExternalRef(cellRef, out var fk)) return null;
            if (vanillaCellOverrides.TryGetValue(fk, out var existing)) return existing;
            var masterName = cellRef[..cellRef.IndexOf(':')].Trim();
            var cache = MasterCache(masterName);
            if (cache is null) return null;
            if (!cache.TryResolve<ICellGetter>(fk, out var vanilla))
            { Warn($"  ! vanilla cell '{cellRef}' not found in {masterName}"); return null; }
            if (!vanilla.Flags.HasFlag(Cell.Flag.IsInteriorCell))
            { Warn($"  ! vanilla cell '{cellRef}' is exterior — only interior vanilla cells supported (phase 2); skipped"); return null; }

            // Manual override (NOT GetOrAddAsOverride, which deep-copies the localized Name → needs
            // the BSA/load-order string lookup, absent headless). Same-FormKey override that copies
            // the cell's inline ENVIRONMENT data (lighting/water/etc.) via CopyCellEnv — omitting it
            // makes the engine reset those to defaults (e.g. a black interior with no lighting).
            // Localized Name is skipped; vanilla refs stay in the master, we only ADD ours.
            var ov = new Cell(fk, SkyrimRelease.SkyrimSE);
            CopyCellEnv(vanilla, ov);
            InteriorSubFor(fk).Cells.Add(ov);
            vanillaCellOverrides[fk] = ov;
            return ov;
        }

        // --- Exterior / worldspace placement (It.7d phase 3) ---------------------------------
        // An exterior cell lives inside a WRLD, nested WorldspaceBlock(type 4, /32 grid) ->
        // WorldspaceSubBlock(type 5, /8 grid) -> Cell(grid x,y). To add a ref to the world we
        // OVERRIDE the existing master cell at the target grid (same careful, Flags+Grid-only
        // override as the interior vanilla case — no localized deep-copy). We host it on a minimal
        // Worldspace override that re-states only OUR block tree (vanilla cells stay in the master).
        var worldspaceOverrides = new Dictionary<FormKey, Worldspace>();
        var exteriorCells = new Dictionary<(FormKey Ws, int X, int Y), Cell>();
        int worldspaceCount = 0, exteriorNewCells = 0;

        Worldspace WorldspaceOverride(FormKey wsFk, IWorldspaceGetter? src)
        {
            if (worldspaceOverrides.TryGetValue(wsFk, out var ex)) return ex;
            var ws = new Worldspace(wsFk, SkyrimRelease.SkyrimSE); // override that hosts our block tree
            if (src is not null) CopyWorldspaceEnv(src, ws);       // carry land/water defaults etc.
            // Headless can't resolve the master's LOCALIZED worldspace Name; an omitted Name makes the
            // override blank it -> saves/HUD show "unknown location". Restate a plain Name for known
            // worldspaces. (TODO: a spec field for arbitrary worldspaces.)
            if (wsFk.ModKey.Name.Equals("Skyrim", StringComparison.OrdinalIgnoreCase) && wsFk.ID == 0x00003C)
                ws.Name = "Skyrim";
            mod.Worldspaces.Add(ws);
            worldspaceOverrides[wsFk] = ws;
            worldspaceCount++;
            return ws;
        }

        // The existing master exterior cell at grid (cx,cy), or null if that grid is ungenerated.
        ICellGetter? FindMasterExteriorCell(string masterName, FormKey wsFk, int cx, int cy)
        {
            var cache = MasterCache(masterName);
            if (cache is null) return null;
            if (!cache.TryResolve<IWorldspaceGetter>(wsFk, out var ws))
            { Warn($"  ! worldspace {wsFk} not found in {masterName}"); return null; }
            short bx = (short)FloorDiv(cx, 32), by = (short)FloorDiv(cy, 32);
            short sx = (short)FloorDiv(cx, 8),  sy = (short)FloorDiv(cy, 8);
            foreach (var block in ws.SubCells)
            {
                if (block.BlockNumberX != bx || block.BlockNumberY != by) continue;
                foreach (var sub in block.Items)
                {
                    if (sub.BlockNumberX != sx || sub.BlockNumberY != sy) continue;
                    foreach (var c in sub.Items)
                        if (c.Grid?.Point is { } p && p.X == cx && p.Y == cy) return c;
                }
            }
            return null;
        }

        // Get-or-add the exterior cell at grid (cx,cy) inside the worldspace override's block tree.
        Cell? ExteriorCell(string worldspaceRef, int cx, int cy)
        {
            if (!TryExternalRef(worldspaceRef, out var wsFk))
            { Warn($"  ! placement worldspace '{worldspaceRef}' must be an external <master>:0xFORMID ref"); return null; }
            var key = (wsFk, cx, cy);
            if (exteriorCells.TryGetValue(key, out var cached)) return cached;

            var masterName = worldspaceRef[..worldspaceRef.IndexOf(':')].Trim();
            var existing = FindMasterExteriorCell(masterName, wsFk, cx, cy);

            // Resolve the master worldspace so the override can carry its land/water defaults + name.
            IWorldspaceGetter? wsSrc = null;
            MasterCache(masterName)?.TryResolve<IWorldspaceGetter>(wsFk, out wsSrc);
            var ws = WorldspaceOverride(wsFk, wsSrc);
            short bx = (short)FloorDiv(cx, 32), by = (short)FloorDiv(cy, 32);
            short sx = (short)FloorDiv(cx, 8),  sy = (short)FloorDiv(cy, 8);
            var block = ws.SubCells.FirstOrDefault(b => b.BlockNumberX == bx && b.BlockNumberY == by);
            if (block is null)
            { block = new WorldspaceBlock { BlockNumberX = bx, BlockNumberY = by, GroupType = GroupTypeEnum.ExteriorCellBlock }; ws.SubCells.Add(block); }
            var sub = block.Items.FirstOrDefault(s => s.BlockNumberX == sx && s.BlockNumberY == sy);
            if (sub is null)
            { sub = new WorldspaceSubBlock { BlockNumberX = sx, BlockNumberY = sy, GroupType = GroupTypeEnum.ExteriorCellSubBlock }; block.Items.Add(sub); }

            Cell cell;
            if (existing is not null)
            {
                // Override the master cell (same FormKey). Copy the cell's inline ENVIRONMENT data
                // (Flags, Grid, water height/type, lighting, regions, imagespace, …) via CopyCellEnv.
                // Omitting these does NOT inherit from the master — the engine defaults them, e.g.
                // WaterHeight -> 0 floods sub-sea-level terrain ("whole world underwater"). Localized
                // Name skipped; vanilla refs stay in the master, we only ADD ours.
                cell = new Cell(existing.FormKey, SkyrimRelease.SkyrimSE);
                CopyCellEnv(existing, cell);
            }
            else
            {
                // Ungenerated grid (no master cell). Make a NEW exterior cell at the grid: structurally
                // valid, but a land-less exterior cell created this way is NOT in-game verified.
                Warn($"  ! exterior grid ({cx},{cy}) has no master cell in {masterName} — creating a NEW cell (structural only, not in-game verified)");
                cell = new Cell(mod, $"MF_Ext_{(cx < 0 ? "m" : "")}{Math.Abs(cx)}_{(cy < 0 ? "m" : "")}{Math.Abs(cy)}")
                { Grid = new CellGrid { Point = new Noggog.P2Int(cx, cy) } };
                exteriorNewCells++;
            }
            sub.Items.Add(cell);
            exteriorCells[key] = cell;
            return cell;
        }

        int placed = 0, vanillaCells = 0;
        // editorId → the placed REFR/ACHR, for wiring linkedRefs / patrol-start after all exist.
        var placementsByEd = new Dictionary<string, IPlaced>();
        // EditorIds that a deferred wire (SingleRef target or Destination location) points at — these
        // placements must be persistent so the engine doesn't drop the anchor the package depends on.
        var deferredAnchorEds = new HashSet<string>(
            deferredTargetWires.Select(w => w.Ref)
                .Concat(deferredLocationWires.Select(w => w.Ref))
                .Where(r => !string.IsNullOrWhiteSpace(r) && !LooksExternalRef(r)),
            StringComparer.OrdinalIgnoreCase);
        foreach (var pl in spec.Placements)
        {
            ICell? cell;
            if (!string.IsNullOrWhiteSpace(pl.Worldspace))
            {
                // Exterior: the world position picks the grid cell in the worldspace.
                int cx = PosToGrid(pl.Position.X), cy = PosToGrid(pl.Position.Y);
                cell = ExteriorCell(pl.Worldspace, cx, cy);
                if (cell is null) { Warn($"  ! placement: worldspace '{pl.Worldspace}' unresolved — skipped"); continue; }
            }
            else if (LooksExternalRef(pl.Cell))
            {
                int before = vanillaCellOverrides.Count;
                cell = VanillaCellOverride(pl.Cell);
                if (cell is null) { Warn($"  ! placement: vanilla cell '{pl.Cell}' unresolved — skipped"); continue; }
                if (vanillaCellOverrides.Count > before) vanillaCells++;
            }
            else if (!cellsByEd.TryGetValue(pl.Cell, out var inSpec))
            { Warn($"  ! placement: cell '{pl.Cell}' not found in spec — skipped"); continue; }
            else cell = inSpec;

            if (!TryResolveRef(pl.Base, formKeyByEd, out var baseFk))
            { Warn($"  ! placement: base '{pl.Base}' unresolved — skipped"); continue; }

            var placement = new Placement
            {
                Position = new Noggog.P3Float(pl.Position.X, pl.Position.Y, pl.Position.Z),
                Rotation = new Noggog.P3Float(Deg2Rad(pl.Rotation.X), Deg2Rad(pl.Rotation.Y), Deg2Rad(pl.Rotation.Z)),
            };

            // Explicit kind wins; otherwise an in-spec NPC base -> npc, anything else -> object.
            bool isNpc = pl.Kind.Equals("npc", StringComparison.OrdinalIgnoreCase)
                || (string.IsNullOrEmpty(pl.Kind) && recordsByEd.TryGetValue(pl.Base, out var br) && br is INpc);

            IPlaced placedRec;
            if (isNpc) { var a = new PlacedNpc(mod); a.Base.SetTo(baseFk); a.Placement = placement; placedRec = a; }
            else       { var o = new PlacedObject(mod); o.Base.SetTo(baseFk); o.Placement = placement; placedRec = o; }

            // Named placements register so other refs (patrol start, linkedRefs target) can find
            // them. A placement that's a linkedRefs *target* must persist across save/load to be a
            // stable anchor, so we force it Persistent (markers are cheap; this avoids the engine
            // dropping a temporary ref another ref points at).
            if (!string.IsNullOrWhiteSpace(pl.EditorId))
            {
                // A placement editorId that collides with an already-registered record would
                // silently clobber that record's FormKey here, breaking any ref to the original.
                // validate enforces uniqueness, but Build can run without it — so warn.
                if (formKeyByEd.ContainsKey(pl.EditorId))
                    Warn($"  ! placement editorId '{pl.EditorId}' collides with an existing record — its FormKey is now overwritten (run validate to catch this)");
                placedRec.EditorID = pl.EditorId;
                formKeyByEd[pl.EditorId] = placedRec.FormKey;
                recordsByEd[pl.EditorId] = (IMajorRecord)placedRec;
                placementsByEd[pl.EditorId] = placedRec;
            }

            // A placement is a stable anchor that must persist across save/load if: it's an explicit
            // persistent, it's a linkedRefs source, or another record's deferred wire points at it —
            // a package SingleRef target (patrol start / follow / escort target) or a package
            // Destination location. The engine can drop a temporary ref that something else links to.
            bool linkTarget = pl.LinkedRefs.Count > 0
                || (!string.IsNullOrWhiteSpace(pl.EditorId) && deferredAnchorEds.Contains(pl.EditorId));
            (pl.Persistent || linkTarget ? cell.Persistent : cell.Temporary).Add(placedRec);
            placed++;
        }

        // Linked References between placements (the Patrol route, etc.). Done after ALL placements
        // exist so a marker can link forward to one defined later in the list (and the last back to
        // the first to loop). null keyword = the default link the patrol engine follows.
        foreach (var pl in spec.Placements)
        {
            if (pl.LinkedRefs.Count == 0 || string.IsNullOrWhiteSpace(pl.EditorId)) continue;
            if (!placementsByEd.TryGetValue(pl.EditorId, out var src)) continue;
            // LinkedReferences (XLKR) lives on REFR (IPlacedObject) and ACHR (IPlacedNpc) separately
            // — no shared settable interface — so pick the concrete list.
            var list = (src as IPlacedObject)?.LinkedReferences ?? (src as IPlacedNpc)?.LinkedReferences;
            if (list is null) continue;
            foreach (var lr in pl.LinkedRefs)
            {
                if (!TryResolveRef(lr.Target, formKeyByEd, out var tgtFk))
                { Warn($"  ! placement '{pl.EditorId}' linkedRef target '{lr.Target}' unresolved — skipped"); continue; }
                var link = new LinkedReferences();
                link.Reference.SetTo(new FormLink<IPlacedGetter>(tgtFk));
                if (!string.IsNullOrWhiteSpace(lr.Keyword) && TryResolveRef(lr.Keyword, formKeyByEd, out var kwFk))
                    link.KeywordOrReference.SetTo(new FormLink<IKeywordLinkedReferenceGetter>(kwFk));
                list.Add(link);
                linksWired++;
                if (LooksExternalRef(lr.Target)) extLinks++;
            }
        }

        // Deferred SingleRef slot-0 targets (Patrol "Patrol Start", Follow "Target to Follow") —
        // emitted now that placements exist, as PackageTargetSpecificReference. The ref is an in-spec
        // placement (e.g. a patrol marker, or an NPC to follow) or a vanilla ref (e.g. the player).
        foreach (var (pack, slot, slotName, ed, refStr) in deferredTargetWires)
        {
            if (!TryResolveRef(refStr, formKeyByEd, out var tgtFk))
            { Warn($"  ! package '{ed}' {slotName} '{refStr}' unresolved — package will no-op"); continue; }
            pack.Data[slot] = new PackageDataTarget
            {
                Name = slotName,
                Type = PackageDataTarget.Types.SingleRef,
                Target = new PackageTargetSpecificReference { Reference = new FormLink<IPlacedGetter>(tgtFk) },
            };
            linksWired++;
            if (LooksExternalRef(refStr)) extLinks++;
        }

        // Deferred PackageDataLocation slots (Escort "Destination") — resolved now that placements
        // exist. MakeLocationSlot handles vanilla refs, in-spec placements, and the NearSelf fallback.
        foreach (var (pack, slot, slotName, ed, refStr, radius) in deferredLocationWires)
            pack.Data[slot] = MakeLocationSlot(slotName, $"package '{ed}' escort", refStr, radius);

        // Leveled-list entries + container contents (each references an item/npc by ref).
        foreach (var li in spec.LeveledItems)
        {
            if (!recordsByEd.TryGetValue(li.EditorId, out var rec) || rec is not ILeveledItem lvl) continue;
            lvl.Entries ??= new();
            foreach (var e in li.Entries)
                Resolve($"leveledItem '{li.EditorId}' entry", e.Reference, fk =>
                {
                    var entry = new LeveledItemEntry { Data = new LeveledItemEntryData { Level = e.Level, Count = e.Count } };
                    entry.Data!.Reference.SetTo(fk);
                    lvl.Entries!.Add(entry);
                });
        }
        foreach (var ln in spec.LeveledNpcs)
        {
            if (!recordsByEd.TryGetValue(ln.EditorId, out var rec) || rec is not ILeveledNpc lvl) continue;
            lvl.Entries ??= new();
            foreach (var e in ln.Entries)
                Resolve($"leveledNpc '{ln.EditorId}' entry", e.Reference, fk =>
                {
                    var entry = new LeveledNpcEntry { Data = new LeveledNpcEntryData { Level = e.Level, Count = e.Count } };
                    entry.Data!.Reference.SetTo(fk);
                    lvl.Entries!.Add(entry);
                });
        }
        foreach (var ct in spec.Containers)
        {
            if (!recordsByEd.TryGetValue(ct.EditorId, out var rec) || rec is not IContainer cont) continue;
            cont.Items ??= new();
            foreach (var e in ct.Items)
                Resolve($"container '{ct.EditorId}' item", e.Item, fk =>
                {
                    var ci = new ContainerItem { Count = e.Count };
                    ci.Item.SetTo(fk);
                    cont.Items!.Add(new ContainerEntry { Item = ci });
                });
        }

        // Recipes (COBJ): wire createdObject + workbench keyword + component refs. Workbench defaults
        // to the forge (CraftingSmithingForge) when unset, so a weapon/armor recipe just works.
        foreach (var co in spec.Recipes)
        {
            if (!recordsByEd.TryGetValue(co.EditorId, out var rec) || rec is not IConstructibleObject cobj) continue;
            Resolve($"recipe '{co.EditorId}' createdObject", co.CreatedObject, fk => cobj.CreatedObject.SetTo(fk));
            var bench = string.IsNullOrWhiteSpace(co.Workbench) ? "Skyrim.esm:0x088105" : co.Workbench;
            Resolve($"recipe '{co.EditorId}' workbench", bench, fk => cobj.WorkbenchKeyword.SetTo(fk));
            cobj.Items ??= new();
            foreach (var comp in co.Components)
                Resolve($"recipe '{co.EditorId}' component", comp.Item, fk =>
                {
                    var ci = new ContainerItem { Count = comp.Count };
                    ci.Item.SetTo(fk);
                    cobj.Items!.Add(new ContainerEntry { Item = ci });
                });
        }

        // Attach Papyrus scripts (VMAD) to any record by editorId. The VMAD setter is
        // not on the IHaveVirtualMachineAdapter interface (get-only) and its type varies
        // (Quest -> QuestAdapter, most others -> VirtualMachineAdapter), so we reflect
        // the concrete property + create the right adapter. ScriptEntry.Name must match
        // the compiled .pex's Scriptname; typed properties are set in the ESP (Flag.Edited).
        int scriptsAttached = 0;
        foreach (var sa in spec.Scripts)
        {
            if (!recordsByEd.TryGetValue(sa.TargetEditorId, out var target))
            { Warn($"  ! script attach: target '{sa.TargetEditorId}' not found"); continue; }

            var vmadProp = target.GetType().GetProperty("VirtualMachineAdapter");
            if (vmadProp is null || !vmadProp.CanWrite)
            { Warn($"  ! '{sa.TargetEditorId}' ({target.GetType().Name}) takes no script"); continue; }

            var vmad = vmadProp.GetValue(target);
            if (vmad is null)
            {
                vmad = System.Activator.CreateInstance(vmadProp.PropertyType);
                vmadProp.SetValue(target, vmad);
            }
            var scriptsList = (System.Collections.IList)vmad!.GetType().GetProperty("Scripts")!.GetValue(vmad)!;

            var entry = new ScriptEntry { Name = sa.ScriptName };
            foreach (var p in sa.Properties)
            {
                ScriptProperty? sp = (p.Type ?? "").ToLowerInvariant() switch
                {
                    "int"    => new ScriptIntProperty { Data = p.Int },
                    "float"  => new ScriptFloatProperty { Data = p.Float },
                    "bool"   => new ScriptBoolProperty { Data = p.Bool },
                    "string" => new ScriptStringProperty { Data = p.Str },
                    "object" => MakeObjectProp(p, formKeyByEd),
                    _        => null,
                };
                if (sp is null) { Warn($"  ! script '{sa.ScriptName}' prop '{p.Name}' bad type/ref '{p.Type}'"); continue; }
                sp.Name = p.Name;
                sp.Flags = ScriptProperty.Flag.Edited;
                entry.Properties.Add(sp);
            }
            scriptsList.Add(entry);
            scriptsAttached++;
        }

        if (spec.Esl) mod.IsSmallMaster = true;
        // Release the master overlays now: every template clone / cell-env copy above is eager
        // (DeepCopyIn / CopyCellEnv), and FormLinks only hold FormKeys, so nothing the write needs
        // depends on the caches still being open. The caller writes the returned mod.
        foreach (var d in masterDisposables) d.Dispose();

        int total = spec.MiscItems.Count + spec.Books.Count + spec.Weapons.Count + spec.Npcs.Count
                    + spec.Quests.Count + dialogueBuilt
                    + spec.Spells.Count + spec.Potions.Count + spec.Armors.Count
                    + spec.Factions.Count + spec.Messages.Count + spec.Cells.Count
                    + spec.LeveledItems.Count + spec.LeveledNpcs.Count + spec.Containers.Count
                    + spec.Ingredients.Count + spec.Ammunitions.Count + spec.Scrolls.Count
                    + spec.SoulGems.Count + spec.Keys.Count + spec.Keywords.Count
                    + spec.Outfits.Count + spec.Statics.Count + spec.Activators.Count
                    + spec.MagicEffects.Count + spec.Classes.Count + spec.Packages.Count
                    + spec.CombatStyles.Count + spec.Relationships.Count + spec.Recipes.Count;
                    // (Placements are reported separately in stats, so not folded into `total`.)
        return new BuildResult
        {
            Mod = mod,
            Warnings = warnings,
            Stats = new BuildStats
            {
                Esl = spec.Esl,
                TopLevelRecords = total,
                DialogueTopics = dialogueBuilt,
                LinksWired = linksWired,
                ExternalLinks = extLinks,
                ScriptsAttached = scriptsAttached,
                Placements = placed,
                NewInteriorCells = spec.Cells.Count,
                VanillaInteriorCells = vanillaCells,
                Worldspaces = worldspaceCount,
                NewExteriorCells = exteriorNewCells,
            },
        };
    }

    private static ScriptProperty? MakeObjectProp(PropertySpec p, Dictionary<string, FormKey> formKeyByEd)
    {
        if (string.IsNullOrEmpty(p.ObjectEditorId) || !TryResolveRef(p.ObjectEditorId, formKeyByEd, out var fk))
            return null;
        var op = new ScriptObjectProperty();
        op.Object.SetTo(fk);
        return op;
    }
}
