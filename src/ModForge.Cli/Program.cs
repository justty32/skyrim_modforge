using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Strings.DI;

// =====================================================================================
//  ModForge.Cli — AI Skyrim mod authoring toolchain (Mutagen, Linux).
//
//      gen     <out.esp>                            write a demo plugin (for testing)
//      extract <in.esp> <strings.json>              pull every translatable string -> JSON
//      apply   <in.esp> <strings.json> <out.esp>    write the JSON's targets back
//
//  Translate workflow: extract -> (AI fills each entry's "target") -> apply.
//  The deterministic Mutagen layer reads/writes the bytes; the AI only edits text.
// =====================================================================================

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0) { Usage(); return 1; }
        try
        {
            switch (args[0])
            {
                case "gen" when args.Length == 2:     Gen(args[1]); return 0;
                case "build" when args.Length == 3:   Build(args[1], args[2]); return 0;
                case "compile" when args.Length == 3: return Compile(args[1], args[2]);
                case "package" when args.Length == 3: return Package(args[1], args[2]);
                case "validate" when args.Length == 2: return Validate(args[1]);
                case "extract" when args.Length == 3: Extract(args[1], args[2]); return 0;
                case "apply" when args.Length == 4:   Apply(args[1], args[2], args[3]); return 0;
                case "applyloc" when args.Length == 4: return ApplyLocalized(args[1], args[2], args[3]);
                case "dump" when args.Length == 2:    return Dump(args[1]);
                case "find" when args.Length is 3 or 4: return Find(args[1], args[2], args.Length == 4 ? args[3] : null);
                case "cellblk" when args.Length is 2 or 3: return CellBlk(args[1], args.Length == 3 ? args[2] : null);
                case "mgefdiag" when args.Length == 3: return MgefDiag(args[1], args[2]);
                case "lightdiag" when args.Length is 2 or 3: return LightDiag(args[1], args.Length == 3 ? args[2] : null);
                default: Usage(); return 1;
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"ERROR: {e.GetType().Name}: {e.Message}");
            if (Environment.GetEnvironmentVariable("MODFORGE_DEBUG") is not null)
                Console.Error.WriteLine(e.ToString());
            return 2;
        }
    }

    private static void Usage() => Console.WriteLine(
        "ModForge.Cli\n" +
        "  gen     <out.esp>\n" +
        "  build   <spec.json> <out.esp>\n" +
        "  compile <script.psc> <outDir>\n" +
        "  package <spec.json> <outModDir>\n" +
        "  validate <spec.json>\n" +
        "  dump    <in.esp>\n" +
        "  find    <in.esp> <query> [type]              search editorId/name -> Skyrim.esm:0xFORMID\n" +
        "  cellblk <in.esp> [0xFORMID]                  show interior cell block/sub-block (FormID grouping)\n" +
        "  mgefdiag <in.esp> <0xFORMID>                 print a MagicEffect's fields (compare gen vs vanilla)\n" +
        "  lightdiag <in.esp> [0xFORMID]                a Light's radius/color/flags (no id: list room-fill lights)\n" +
        "  extract <in.esp> <strings.json>\n" +
        "  applyloc <in.esp> <strings.json> <outDir>   (Localized UTF-8 _chinese.STRINGS)\n" +
        "  apply   <in.esp> <strings.json> <out.esp>");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions ReadOpts = new() { PropertyNameCaseInsensitive = true };

    private static ISkyrimMod Load(string path) =>
        SkyrimMod.CreateFromBinary(new ModPath(path), SkyrimRelease.SkyrimSE);

    // output filename may differ from the mod's ModKey -> skip the alignment check.
    private static void Write(ISkyrimMod mod, string outPath) =>
        mod.WriteToBinary(outPath, new BinaryWriteParameters { ModKey = ModKeyOption.NoCheck });

    // -------------------------------------------------------------------------------
    //  Every translatable text slot: where it lives + how to read/write it. extract
    //  and apply iterate the SAME Slots(mod) so they stay aligned; apply matches by
    //  (FormKey, Field, Index). Add a record type here to extend coverage.
    // -------------------------------------------------------------------------------
    private static IEnumerable<Slot> Slots(ISkyrimMod mod)
    {
        foreach (var rec in mod.EnumerateMajorRecords())
        {
            var fk = rec.FormKey.ToString();
            var typeName = rec.GetType().Name; // concrete record type (e.g. "Ingestible")

            if (rec is IDialogTopic) continue; // handled in the dedicated dialogue pass

            if (rec is INamed named && named.Name is { } nm)
                yield return new Slot(fk, typeName, "Name", 0, () => named.Name, v => named.Name = v);

            if (rec is IBook book && book.BookText?.String is { } body)
                yield return new Slot(fk, typeName, "BookText", 0, () => book.BookText?.String, v => book.BookText = v);

            if (rec is INpc npc && npc.ShortName?.String is { } sn)
                yield return new Slot(fk, typeName, "ShortName", 0, () => npc.ShortName?.String, v => npc.ShortName = v);

            if (rec is IQuest quest)
            {
                foreach (var obj in quest.Objectives)
                {
                    if (obj.DisplayText?.String is { } ot)
                    {
                        var captured = obj;
                        yield return new Slot(fk, typeName, "Objective", obj.Index,
                            () => captured.DisplayText?.String, v => captured.DisplayText = v);
                    }
                }
            }
        }

        foreach (var topic in mod.DialogTopics)
        {
            var tfk = topic.FormKey.ToString();
            if (topic.Name?.String is { } prompt)
                yield return new Slot(tfk, "DialogTopic", "Prompt", 0, () => topic.Name?.String, v => topic.Name = v);

            foreach (var info in topic.Responses)
            {
                var ifk = info.FormKey.ToString();
                foreach (var resp in info.Responses)
                {
                    if (resp.Text?.String is { } line)
                    {
                        var captured = resp;
                        yield return new Slot(ifk, "DialogResponse", "Text", resp.ResponseNumber,
                            () => captured.Text?.String, v => captured.Text = v);
                    }
                }
            }
        }
    }

    // -------------------------------------------------------------------------------
    //  extract
    // -------------------------------------------------------------------------------
    private static void Extract(string inPath, string jsonPath)
    {
        var mod = Load(inPath);
        var entries = Slots(mod).Select(s => new StringEntry
        {
            FormKey = s.FormKey, Type = s.Type, Field = s.Field, Index = s.Index,
            Source = s.Get() ?? "", Target = "",
        }).ToList();

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(entries, JsonOpts));
        Console.WriteLine($"extracted {entries.Count} string(s) from {Path.GetFileName(inPath)} -> {jsonPath}");
        foreach (var e in entries.Take(20))
            Console.WriteLine($"  {e.FormKey} {e.Type}.{e.Field}[{e.Index}] = \"{e.Source}\"");
        if (entries.Count > 20) Console.WriteLine($"  … +{entries.Count - 20} more");
    }

    // -------------------------------------------------------------------------------
    //  apply
    // -------------------------------------------------------------------------------
    private static void Apply(string inPath, string jsonPath, string outPath)
    {
        var entries = JsonSerializer.Deserialize<List<StringEntry>>(File.ReadAllText(jsonPath)) ?? new();
        var map = entries
            .Where(e => !string.IsNullOrEmpty(e.Target))
            .ToDictionary(e => $"{e.FormKey}|{e.Field}|{e.Index}", e => e.Target);

        var mod = Load(inPath);
        int applied = 0;
        foreach (var s in Slots(mod))
        {
            if (map.TryGetValue($"{s.FormKey}|{s.Field}|{s.Index}", out var target))
            {
                s.Set(target);
                applied++;
            }
        }

        Write(mod, outPath);
        Console.WriteLine($"applied {applied}/{map.Count} translation(s) -> {Path.GetFileName(outPath)}");
    }

    // -------------------------------------------------------------------------------
    //  gen — demo plugin (general content + dialogue + ESL); also a translate target
    // -------------------------------------------------------------------------------
    private static void Gen(string outPath)
    {
        var key = ModKey.FromNameAndExtension(Path.GetFileName(outPath));
        var mod = new SkyrimMod(key, SkyrimRelease.SkyrimSE);

        var misc = mod.MiscItems.AddNew();
        misc.EditorID = "MF_DemoMisc"; misc.Name = "Forged Trinket"; misc.Value = 10; misc.Weight = 0.5f;

        var book = mod.Books.AddNew();
        book.EditorID = "MF_DemoBook"; book.Name = "Forged Note";
        book.BookText = "A short note, here in English, ready to be translated.";

        var npc = mod.Npcs.AddNew();
        npc.EditorID = "MF_DemoNpc"; npc.Name = "Aldric the Forged";

        var quest = mod.Quests.AddNew();
        quest.EditorID = "MF_DemoQuest"; quest.Name = "A Forged Errand";
        quest.Objectives.Add(new QuestObjective { Index = 10, DisplayText = "Speak with Aldric" });

        var branch = mod.DialogBranches.AddNew();
        branch.EditorID = "MF_DemoBranch"; branch.Quest.SetTo(quest);
        branch.Category = DialogBranch.CategoryType.Player;

        var topic = mod.DialogTopics.AddNew();
        topic.EditorID = "MF_DemoTopic"; topic.Quest.SetTo(quest); topic.Branch.SetTo(branch);
        topic.Category = DialogTopic.CategoryEnum.Topic;
        topic.Subtype = DialogTopic.SubtypeEnum.Custom;
        topic.SubtypeName = RecordType.Null;
        topic.Name = "Tell me about this place.";
        topic.Priority = 50f;
        branch.StartingTopic.SetTo(topic);

        var info = new DialogResponses(mod) { Prompt = "Greeting" };
        info.Responses.Add(new DialogResponse
        {
            Text = "Welcome, traveler. Everything here was forged on Linux.",
            ResponseNumber = 1,
            Emotion = Emotion.Neutral,
        });
        var cond = new ConditionFloat
        {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = 1f,
            Data = new GetIsIDConditionData(),
        };
        ((GetIsIDConditionData)cond.Data).Object.Link.SetTo(npc);
        info.Conditions.Add(cond);
        topic.Responses.Add(info);

        mod.IsSmallMaster = true; // ESL flag (≤4096 new records)

        mod.WriteToBinary(outPath);
        Console.WriteLine($"wrote {outPath}  (ESL={mod.IsSmallMaster}, {mod.EnumerateMajorRecords().Count()} records)");
    }

    // -------------------------------------------------------------------------------
    //  build — generate a plugin from a structured spec (the data-driven generator).
    //  Layer between an LLM (NL -> spec) and Mutagen (spec -> valid plugin). Extend by
    //  adding a list to ModSpec + a loop here. (It.2+: quests/dialogue, more types.)
    // -------------------------------------------------------------------------------
    private static void Build(string specPath, string outPath)
    {
        var spec = JsonSerializer.Deserialize<ModSpec>(File.ReadAllText(specPath), ReadOpts)
                   ?? throw new InvalidOperationException("spec deserialized to null");
        var key = ModKey.FromNameAndExtension(Path.GetFileName(outPath));
        var mod = new SkyrimMod(key, SkyrimRelease.SkyrimSE);

        // --- Master link-caches (read-only overlays of Skyrim.esm etc.) -----------------------
        // Used by (a) weapon/book *templating* — cloning a vanilla record so a generated item gets
        // a real model/animation/equip data and doesn't CRASH on equip/read — and (b) vanilla
        // cell/worldspace placement further down. Declared up here so the item-build loops reach it.
        var skyrimData = Environment.GetEnvironmentVariable("MODFORGE_SKYRIM_DATA")
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
                Console.WriteLine($"  ! master '{masterName}' not found at {path} (set MODFORGE_SKYRIM_DATA to your Data folder)");
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
                    Console.WriteLine($"  ! book '{b.EditorId}': template '{b.Template}' not resolved — book will lack a model and may CRASH on read");
            }
            else
                Console.WriteLine($"  ! book '{b.EditorId}': no `template` — a model-less book CRASHES on read; set template to a vanilla book (e.g. Skyrim.esm:0x0ED161 Book1CheapNordsArise)");
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
                    Console.WriteLine($"  ! weapon '{w.EditorId}': template '{w.Template}' not resolved — weapon will lack a model and may CRASH on equip");
            }
            else
                Console.WriteLine($"  ! weapon '{w.EditorId}': no `template` — a model-less weapon CRASHES on equip; set template to a vanilla weapon (e.g. Skyrim.esm:0x012EB7 IronSword)");
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
            if (!string.IsNullOrEmpty(n.EditorId)) npcsByEd[n.EditorId] = r;
        }

        var questsByEd = new Dictionary<string, Quest>();
        foreach (var q in spec.Quests)
        {
            var r = mod.Quests.AddNew();
            r.EditorID = q.EditorId; r.Name = q.Name;
            foreach (var o in q.Objectives)
                r.Objectives.Add(new QuestObjective { Index = o.Index, DisplayText = o.Text });
            if (!string.IsNullOrEmpty(q.EditorId)) questsByEd[q.EditorId] = r;
        }

        // Native dialogue: Quest -> DialogBranch -> DialogTopic -> DialogResponses(INFO).
        // (Writes valid records; making the line actually surface in-game still needs
        // quest-flag tuning + Proton testing — see NOTES.md / the parent spike.)
        int dialogueBuilt = 0;
        foreach (var d in spec.Dialogue)
        {
            if (string.IsNullOrEmpty(d.QuestEditorId) || !questsByEd.TryGetValue(d.QuestEditorId, out var quest))
            {
                Console.WriteLine($"  ! dialogue '{d.EditorId}' skipped: quest '{d.QuestEditorId}' not found in spec");
                continue;
            }

            var branch = mod.DialogBranches.AddNew();
            branch.EditorID = d.EditorId + "_Br";
            branch.Quest.SetTo(quest);
            branch.Category = DialogBranch.CategoryType.Player;

            var topic = mod.DialogTopics.AddNew();
            topic.EditorID = d.EditorId;
            topic.Quest.SetTo(quest);
            topic.Branch.SetTo(branch);
            topic.Category = DialogTopic.CategoryEnum.Topic;
            topic.Subtype = DialogTopic.SubtypeEnum.Custom;
            topic.SubtypeName = RecordType.Null;
            topic.Name = d.Prompt;
            topic.Priority = 50f;
            branch.StartingTopic.SetTo(topic);

            var info = new DialogResponses(mod) { Prompt = "Greeting" };
            byte rn = 1;
            foreach (var line in d.Responses)
                info.Responses.Add(new DialogResponse { Text = line, ResponseNumber = rn++, Emotion = Emotion.Neutral });

            if (!string.IsNullOrEmpty(d.SpeakerNpcEditorId) &&
                npcsByEd.TryGetValue(d.SpeakerNpcEditorId, out var speaker))
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
            topic.Responses.Add(info);
            dialogueBuilt++;
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
                    else Console.WriteLine($"  ! armor '{a.EditorId}' unknown slot '{slot}' (e.g. Body, Head, Hands, Feet, Forearms, Calves, Shield)");
                r.BodyTemplate = bt;
            }
        }
        foreach (var f in spec.Factions)
        {
            var r = mod.Factions.AddNew();
            r.EditorID = f.EditorId; r.Name = f.Name;
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
                else Console.WriteLine($"  ! class '{cl.EditorId}' skillWeight '{skillName}' is not a Skill — skipped");
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
            r.CreatedObjectCount = (ushort)Math.Max(1, co.Count);
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
                    else Console.WriteLine($"  ! cell '{c.EditorId}' template '{c.Template}' is exterior — ignored (need an interior cell)");
                }
                else Console.WriteLine($"  ! cell '{c.EditorId}' template '{c.Template}' unresolved — created without lighting (may render black)");
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
            else Console.WriteLine($"  ! {what} ref '{refStr}' unresolved (need in-spec editorId or <master>:0xFORMID)");
        }

        foreach (var n in spec.Npcs)
        {
            if (!npcsByEd.TryGetValue(n.EditorId, out var npcRec)) continue;
            Resolve($"npc '{n.EditorId}' race",   n.Race,   fk => npcRec.Race.SetTo(fk));
            Resolve($"npc '{n.EditorId}' class",  n.Class,  fk => npcRec.Class.SetTo(fk));
            Resolve($"npc '{n.EditorId}' outfit", n.Outfit, fk => npcRec.DefaultOutfit.SetTo(fk));
            foreach (var factionRef in n.Factions)
                Resolve($"npc '{n.EditorId}' faction", factionRef, fk =>
                {
                    var rp = new RankPlacement { Rank = 0 };
                    rp.Faction.SetTo(fk);
                    npcRec.Factions.Add(rp);
                });
        }

        // Keywords on armor/weapon/misc (all implement the IKeyworded aspect).
        void WireKeywords(string ed, List<string> kws)
        {
            if (kws.Count == 0) return;
            if (!recordsByEd.TryGetValue(ed, out var rec) || rec is not IKeyworded<IKeywordGetter> kw)
            { Console.WriteLine($"  ! '{ed}' takes no keywords (or not found)"); return; }
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
            { Console.WriteLine($"  ! '{ed}' takes no magic effects (or not found)"); return; }
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
            { Console.WriteLine($"  ! vanilla cell '{cellRef}' not found in {masterName}"); return null; }
            if (!vanilla.Flags.HasFlag(Cell.Flag.IsInteriorCell))
            { Console.WriteLine($"  ! vanilla cell '{cellRef}' is exterior — only interior vanilla cells supported (phase 2); skipped"); return null; }

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
            { Console.WriteLine($"  ! worldspace {wsFk} not found in {masterName}"); return null; }
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
            { Console.WriteLine($"  ! placement worldspace '{worldspaceRef}' must be an external <master>:0xFORMID ref"); return null; }
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
                Console.WriteLine($"  ! exterior grid ({cx},{cy}) has no master cell in {masterName} — creating a NEW cell (structural only, not in-game verified)");
                cell = new Cell(mod, $"MF_Ext_{(cx < 0 ? "m" : "")}{Math.Abs(cx)}_{(cy < 0 ? "m" : "")}{Math.Abs(cy)}")
                { Grid = new CellGrid { Point = new Noggog.P2Int(cx, cy) } };
                exteriorNewCells++;
            }
            sub.Items.Add(cell);
            exteriorCells[key] = cell;
            return cell;
        }

        int placed = 0, vanillaCells = 0;
        foreach (var pl in spec.Placements)
        {
            ICell? cell;
            if (!string.IsNullOrWhiteSpace(pl.Worldspace))
            {
                // Exterior: the world position picks the grid cell in the worldspace.
                int cx = PosToGrid(pl.Position.X), cy = PosToGrid(pl.Position.Y);
                cell = ExteriorCell(pl.Worldspace, cx, cy);
                if (cell is null) { Console.WriteLine($"  ! placement: worldspace '{pl.Worldspace}' unresolved — skipped"); continue; }
            }
            else if (LooksExternalRef(pl.Cell))
            {
                int before = vanillaCellOverrides.Count;
                cell = VanillaCellOverride(pl.Cell);
                if (cell is null) { Console.WriteLine($"  ! placement: vanilla cell '{pl.Cell}' unresolved — skipped"); continue; }
                if (vanillaCellOverrides.Count > before) vanillaCells++;
            }
            else if (!cellsByEd.TryGetValue(pl.Cell, out var inSpec))
            { Console.WriteLine($"  ! placement: cell '{pl.Cell}' not found in spec — skipped"); continue; }
            else cell = inSpec;

            if (!TryResolveRef(pl.Base, formKeyByEd, out var baseFk))
            { Console.WriteLine($"  ! placement: base '{pl.Base}' unresolved — skipped"); continue; }

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

            (pl.Persistent ? cell.Persistent : cell.Temporary).Add(placedRec);
            placed++;
        }

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
            { Console.WriteLine($"  ! script attach: target '{sa.TargetEditorId}' not found"); continue; }

            var vmadProp = target.GetType().GetProperty("VirtualMachineAdapter");
            if (vmadProp is null || !vmadProp.CanWrite)
            { Console.WriteLine($"  ! '{sa.TargetEditorId}' ({target.GetType().Name}) takes no script"); continue; }

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
                if (sp is null) { Console.WriteLine($"  ! script '{sa.ScriptName}' prop '{p.Name}' bad type/ref '{p.Type}'"); continue; }
                sp.Name = p.Name;
                sp.Flags = ScriptProperty.Flag.Edited;
                entry.Properties.Add(sp);
            }
            scriptsList.Add(entry);
            scriptsAttached++;
        }

        if (spec.Esl) mod.IsSmallMaster = true;
        Write(mod, outPath);
        foreach (var d in masterDisposables) d.Dispose();   // release the master overlays (overrides are deep-copied)
        int total = spec.MiscItems.Count + spec.Books.Count + spec.Weapons.Count + spec.Npcs.Count
                    + spec.Quests.Count + dialogueBuilt
                    + spec.Spells.Count + spec.Potions.Count + spec.Armors.Count
                    + spec.Factions.Count + spec.Messages.Count + spec.Cells.Count
                    + spec.LeveledItems.Count + spec.LeveledNpcs.Count + spec.Containers.Count
                    + spec.Ingredients.Count + spec.Ammunitions.Count + spec.Scrolls.Count
                    + spec.SoulGems.Count + spec.Keys.Count + spec.Keywords.Count
                    + spec.Outfits.Count + spec.Statics.Count + spec.Activators.Count;
        Console.WriteLine($"built {outPath} from {Path.GetFileName(specPath)} " +
                          $"(ESL={spec.Esl}, {total} top-level record(s); {dialogueBuilt} dialogue topic(s); " +
                          $"{linksWired} cross-ref link(s), {extLinks} to external master(s); " +
                          $"{scriptsAttached} script(s) attached; " +
                          $"{placed} placement(s) in {spec.Cells.Count} new + {vanillaCells} vanilla interior cell(s) + " +
                          $"{worldspaceCount} worldspace(s) [{exteriorNewCells} new exterior cell(s)])");
    }

    private static ScriptProperty? MakeObjectProp(PropertySpec p, Dictionary<string, FormKey> formKeyByEd)
    {
        if (string.IsNullOrEmpty(p.ObjectEditorId) || !TryResolveRef(p.ObjectEditorId, formKeyByEd, out var fk))
            return null;
        var op = new ScriptObjectProperty();
        op.Object.SetTo(fk);
        return op;
    }

    // Armor class string -> enum. Accepts shorthand (light/heavy/clothing) or the enum names;
    // anything unrecognised (incl. empty) falls back to Clothing.
    private static ArmorType ParseArmorType(string s) => s.Trim().ToLowerInvariant() switch
    {
        "light" or "lightarmor" => ArmorType.LightArmor,
        "heavy" or "heavyarmor" => ArmorType.HeavyArmor,
        _ => ArmorType.Clothing,
    };

    // Skyrim stores placement rotation in radians; specs author it in (friendlier) degrees.
    private static float Deg2Rad(float deg) => deg * (float)Math.PI / 180f;

    // Exterior worldspace cells are 4096 units square. A world position maps to cell grid
    // coords by floor(pos/4096); those map to the WRLD group nesting by floor(grid/8) (sub-block)
    // and floor(grid/32) (block) — VERIFIED against Tamriel (cell (7,-41) -> block (0,-2),
    // sub-block (0,-6)). NOTE: this must be FLOOR division (toward -inf), not C#'s truncating `/`
    // ((-41)/8 == -5, but floor is -6) — negative coordinates would land in the wrong group.
    private const int CellSize = 4096;
    private static int FloorDiv(int a, int b) => (int)Math.Floor((double)a / b);
    private static int PosToGrid(float pos) => (int)Math.Floor(pos / CellSize);

    // OR together a list of flag names (case-insensitive) into one enum value; unknown names
    // are ignored (validate is responsible for reporting them).
    private static T ParseFlags<T>(List<string> names) where T : struct, Enum
    {
        long acc = 0;
        foreach (var n in names)
            if (Enum.TryParse<T>(n, ignoreCase: true, out var v)) acc |= Convert.ToInt64(v);
        return (T)Enum.ToObject(typeof(T), acc);
    }

    // -------------------------------------------------------------------------------
    //  Reference resolver (It.7b). A "ref" string is EITHER an in-spec editorId, OR an
    //  external vanilla/master form "<master>:0xFORMID" (e.g. "Skyrim.esm:0x013746").
    //  External refs become a FormKey on the named master directly; Mutagen adds the
    //  master to the output's masters list on write (MastersListContent = Iterate).
    //  Discover external FormIDs with the `find` command.
    // -------------------------------------------------------------------------------
    private static bool LooksExternalRef(string s)
    {
        int i = s.IndexOf(':');
        if (i <= 0) return false;
        var master = s[..i].Trim();
        return master.EndsWith(".esm", StringComparison.OrdinalIgnoreCase)
            || master.EndsWith(".esp", StringComparison.OrdinalIgnoreCase)
            || master.EndsWith(".esl", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryExternalRef(string s, out FormKey fk)
    {
        fk = default;
        int i = s.IndexOf(':');
        if (i <= 0) return false;
        var master = s[..i].Trim();
        if (!LooksExternalRef(s)) return false;
        var idPart = s[(i + 1)..].Trim();
        if (idPart.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) idPart = idPart[2..];
        if (!uint.TryParse(idPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id)) return false;
        fk = new FormKey(ModKey.FromNameAndExtension(master), id & 0x00FFFFFF);  // mask off the master-index byte
        return true;
    }

    // Resolve a ref to a FormKey: external "<master>:0xID" first, else in-spec editorId.
    private static bool TryResolveRef(string s, Dictionary<string, FormKey> formKeyByEd, out FormKey fk)
    {
        if (string.IsNullOrWhiteSpace(s)) { fk = default; return false; }
        if (TryExternalRef(s, out fk)) return true;
        return formKeyByEd.TryGetValue(s, out fk);
    }

    // -------------------------------------------------------------------------------
    //  compile — drive the Creation Kit's PapyrusCompiler.exe under Wine: .psc -> .pex.
    //  Base script sources + the flags file come from the CK's Scripts.zip (extract
    //  once to MODFORGE_PAPYRUS_BASE; default ~/.cache/modforge/papyrus/Source/Scripts).
    //  GOTCHA: the compiler returns exit code 0 even on failure -> scrape stdout
    //  ("Failed on") and confirm the .pex was actually produced.
    // -------------------------------------------------------------------------------
    private static readonly string PapyrusCompilerExe =
        Environment.GetEnvironmentVariable("MODFORGE_PAPYRUS_COMPILER")
        ?? "/home/lorkhan/.local/share/Steam/steamapps/common/Skyrim Special Edition 1946180/Papyrus Compiler/PapyrusCompiler.exe";
    private static readonly string PapyrusBaseScripts =
        Environment.GetEnvironmentVariable("MODFORGE_PAPYRUS_BASE")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".cache", "modforge", "papyrus", "Source", "Scripts");

    private static int Compile(string scriptPath, string outDir)
    {
        var flags = Path.Combine(PapyrusBaseScripts, "TESV_Papyrus_Flags.flg");
        if (!File.Exists(PapyrusCompilerExe))
        { Console.Error.WriteLine($"ERROR: PapyrusCompiler not found: {PapyrusCompilerExe} (set MODFORGE_PAPYRUS_COMPILER)"); return 2; }
        if (!File.Exists(flags))
        { Console.Error.WriteLine($"ERROR: flags file not found: {flags} (set MODFORGE_PAPYRUS_BASE to the extracted Source/Scripts)"); return 2; }
        if (!File.Exists(scriptPath))
        { Console.Error.WriteLine($"ERROR: script not found: {scriptPath}"); return 2; }

        var dir = Path.GetDirectoryName(scriptPath);
        var scriptDir = Path.GetFullPath(string.IsNullOrEmpty(dir) ? "." : dir);
        var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
        var outFull = Path.GetFullPath(outDir);
        Directory.CreateDirectory(outFull);

        var psi = new ProcessStartInfo
        {
            FileName = "wine",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(PapyrusCompilerExe);
        psi.ArgumentList.Add(scriptName);
        psi.ArgumentList.Add($"-f={flags}");
        psi.ArgumentList.Add($"-i={PapyrusBaseScripts};{scriptDir}");
        psi.ArgumentList.Add($"-o={outFull}");

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("could not start wine");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        var pex = Path.Combine(outFull, scriptName + ".pex");
        bool pexOk = File.Exists(pex);
        bool failed = !pexOk || stdout.Contains("Failed on") || stdout.Contains("compilation failed");
        if (failed)
        {
            Console.Error.WriteLine($"compile FAILED for {scriptName}");
            var msg = stdout.Trim();
            if (msg.Length > 0) Console.Error.WriteLine(msg);
            return 1;
        }
        Console.WriteLine($"compiled {scriptName} -> {pex} ({new FileInfo(pex).Length} bytes)");
        return 0;
    }

    // -------------------------------------------------------------------------------
    //  package — build the .esp, compile any script sources, and lay out an MO2/Vortex-
    //  ready mod folder: <outModDir>/<PluginName> + Scripts/*.pex + Scripts/Source/*.psc.
    //  (A script entry with a `source` .psc gets compiled; its VMAD attach happened in
    //  Build by Scriptname.)
    // -------------------------------------------------------------------------------
    private static int Package(string specPath, string outModDir)
    {
        var spec = JsonSerializer.Deserialize<ModSpec>(File.ReadAllText(specPath), ReadOpts)
                   ?? throw new InvalidOperationException("spec deserialized to null");
        var pluginName = string.IsNullOrEmpty(spec.PluginName) ? "Generated.esp" : spec.PluginName;
        Directory.CreateDirectory(outModDir);

        // 1) the plugin (Build also does the VMAD script attach by Scriptname)
        Build(specPath, Path.Combine(outModDir, pluginName));

        // 2) compile each referenced script source -> Scripts/*.pex; copy .psc -> Scripts/Source/
        var scriptsDir = Path.Combine(outModDir, "Scripts");
        var sourceDir = Path.Combine(scriptsDir, "Source");
        var specDir = Path.GetDirectoryName(Path.GetFullPath(specPath)) ?? ".";
        int compiled = 0;
        foreach (var sa in spec.Scripts)
        {
            if (string.IsNullOrEmpty(sa.Source)) continue;
            var src = Path.IsPathRooted(sa.Source) ? sa.Source : Path.Combine(specDir, sa.Source);
            if (!File.Exists(src)) { Console.Error.WriteLine($"  ! script source not found: {src}"); continue; }
            if (Compile(src, scriptsDir) != 0) { Console.Error.WriteLine($"  ! compile failed: {sa.Source}"); continue; }
            Directory.CreateDirectory(sourceDir);
            File.Copy(src, Path.Combine(sourceDir, Path.GetFileName(src)), overwrite: true);
            compiled++;
        }

        Console.WriteLine($"packaged -> {outModDir}  ({pluginName} + {compiled} compiled script(s) under Scripts/)");
        return 0;
    }

    // -------------------------------------------------------------------------------
    //  validate — semantic guardrail for (LLM-authored) specs: editorId presence +
    //  uniqueness, and referential integrity (dialogue→quest/npc, npc→faction,
    //  script→target, object-property→record, property types). Returns non-zero if any
    //  problem so an NL→spec front can self-correct before build/package.
    // -------------------------------------------------------------------------------
    private static int Validate(string specPath)
    {
        var spec = JsonSerializer.Deserialize<ModSpec>(File.ReadAllText(specPath), ReadOpts)
                   ?? throw new InvalidOperationException("spec deserialized to null");
        var problems = new List<string>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var npcIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var questIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var factionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cellIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Reg(string ed, string what, HashSet<string>? typed = null)
        {
            if (string.IsNullOrWhiteSpace(ed)) { problems.Add($"{what}: empty editorId"); return; }
            if (!ids.Add(ed)) problems.Add($"duplicate editorId '{ed}' (at {what})");
            typed?.Add(ed);
        }

        foreach (var m in spec.MiscItems) Reg(m.EditorId, "miscItem");
        foreach (var b in spec.Books) Reg(b.EditorId, "book");
        foreach (var w in spec.Weapons) Reg(w.EditorId, "weapon");
        foreach (var n in spec.Npcs) Reg(n.EditorId, "npc", npcIds);
        foreach (var q in spec.Quests) Reg(q.EditorId, "quest", questIds);
        foreach (var s in spec.Spells) Reg(s.EditorId, "spell");
        foreach (var p in spec.Potions) Reg(p.EditorId, "potion");
        foreach (var a in spec.Armors) Reg(a.EditorId, "armor");
        foreach (var f in spec.Factions) Reg(f.EditorId, "faction", factionIds);
        foreach (var msg in spec.Messages) Reg(msg.EditorId, "message");
        foreach (var d in spec.Dialogue) Reg(d.EditorId, "dialogue");
        foreach (var c in spec.Cells)
        {
            Reg(c.EditorId, "cell", cellIds);
            if (!string.IsNullOrWhiteSpace(c.Template) && !TryExternalRef(c.Template, out _))
                problems.Add($"cell '{c.EditorId}' template '{c.Template}' must be an external <master>:0xFORMID interior-cell ref");
        }
        foreach (var li in spec.LeveledItems) Reg(li.EditorId, "leveledItem");
        foreach (var ln in spec.LeveledNpcs) Reg(ln.EditorId, "leveledNpc");
        foreach (var ct in spec.Containers) Reg(ct.EditorId, "container");
        foreach (var i in spec.Ingredients) Reg(i.EditorId, "ingredient");
        foreach (var a in spec.Ammunitions) Reg(a.EditorId, "ammunition");
        foreach (var s in spec.Scrolls) Reg(s.EditorId, "scroll");
        foreach (var sg in spec.SoulGems) Reg(sg.EditorId, "soulGem");
        foreach (var k in spec.Keys) Reg(k.EditorId, "key");
        foreach (var kw in spec.Keywords) Reg(kw.EditorId, "keyword");
        foreach (var o in spec.Outfits) Reg(o.EditorId, "outfit");
        foreach (var st in spec.Statics) Reg(st.EditorId, "static");
        foreach (var ac in spec.Activators) Reg(ac.EditorId, "activator");
        foreach (var me in spec.MagicEffects) Reg(me.EditorId, "magicEffect");
        foreach (var co in spec.Recipes) Reg(co.EditorId, "recipe");
        foreach (var cl in spec.Classes) Reg(cl.EditorId, "class");

        // A ref must be an in-spec editorId OR a well-formed external "<master>:0xFORMID".
        void CheckRef(string r, string what)
        {
            if (string.IsNullOrWhiteSpace(r)) return;
            if (LooksExternalRef(r))
            { if (!TryExternalRef(r, out _)) problems.Add($"{what}: malformed external ref '{r}' (expect <master>:0xFORMID)"); }
            else if (!ids.Contains(r))
                problems.Add($"{what}: unresolved ref '{r}' (unknown in-spec editorId; for vanilla forms use <master>:0xFORMID)");
        }

        foreach (var n in spec.Npcs)
        {
            foreach (var fac in n.Factions)
                if (LooksExternalRef(fac))
                { if (!TryExternalRef(fac, out _)) problems.Add($"npc '{n.EditorId}' faction: malformed external ref '{fac}'"); }
                else if (!factionIds.Contains(fac))
                    problems.Add($"npc '{n.EditorId}' references unknown faction '{fac}' (in-spec, non-faction or typo; vanilla faction -> <master>:0xFORMID)");
            CheckRef(n.Race, $"npc '{n.EditorId}' race");
            CheckRef(n.Class, $"npc '{n.EditorId}' class");
            CheckRef(n.Outfit, $"npc '{n.EditorId}' outfit");
        }
        foreach (var a in spec.Armors) foreach (var k in a.Keywords) CheckRef(k, $"armor '{a.EditorId}' keyword");
        foreach (var w in spec.Weapons) foreach (var k in w.Keywords) CheckRef(k, $"weapon '{w.EditorId}' keyword");
        // `template` = a vanilla record to clone (model/anim) — must be a well-formed external ref.
        foreach (var w in spec.Weapons) if (!string.IsNullOrWhiteSpace(w.Template) && !TryExternalRef(w.Template, out _))
            problems.Add($"weapon '{w.EditorId}' template: malformed external ref '{w.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x012EB7)");
        foreach (var b in spec.Books) if (!string.IsNullOrWhiteSpace(b.Template) && !TryExternalRef(b.Template, out _))
            problems.Add($"book '{b.EditorId}' template: malformed external ref '{b.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x0ED161)");
        foreach (var m in spec.MiscItems) if (!string.IsNullOrWhiteSpace(m.Template) && !TryExternalRef(m.Template, out _))
            problems.Add($"miscItem '{m.EditorId}' template: malformed external ref '{m.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x063B42)");
        foreach (var p in spec.Potions) if (!string.IsNullOrWhiteSpace(p.Template) && !TryExternalRef(p.Template, out _))
            problems.Add($"potion '{p.EditorId}' template: malformed external ref '{p.Template}' (expect <master>:0xFORMID, e.g. Skyrim.esm:0x039BE5)");
        foreach (var m in spec.MiscItems) foreach (var k in m.Keywords) CheckRef(k, $"miscItem '{m.EditorId}' keyword");

        var armorTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "light", "heavy", "clothing", "lightarmor", "heavyarmor" };
        foreach (var a in spec.Armors)
        {
            if (!string.IsNullOrEmpty(a.ArmorType) && !armorTypes.Contains(a.ArmorType))
                problems.Add($"armor '{a.EditorId}' has invalid armorType '{a.ArmorType}' (light|heavy|clothing)");
            foreach (var slot in a.Slots)
                if (!Enum.TryParse<BipedObjectFlag>(slot, ignoreCase: true, out _))
                    problems.Add($"armor '{a.EditorId}' has invalid slot '{slot}' (e.g. Body, Head, Hands, Feet, Forearms, Calves, Shield)");
        }

        // A spell/potion effect needs a MagicEffect ref (in-spec or, normally, a vanilla one).
        void CheckEffects(string ed, List<EffectSpec> effects, string kind)
        {
            foreach (var e in effects)
                if (string.IsNullOrWhiteSpace(e.MagicEffect)) problems.Add($"{kind} '{ed}' has an effect with empty magicEffect ref");
                else CheckRef(e.MagicEffect, $"{kind} '{ed}' effect magicEffect");
        }
        foreach (var s in spec.Spells) CheckEffects(s.EditorId, s.Effects, "spell");
        foreach (var p in spec.Potions) CheckEffects(p.EditorId, p.Effects, "potion");

        // MagicEffect (MGEF): every authored enum string must parse; association (if set) is a ref.
        void CheckEnum<TEnum>(string val, string what) where TEnum : struct, Enum
        { if (!string.IsNullOrWhiteSpace(val) && !Enum.TryParse<TEnum>(val, ignoreCase: true, out _)) problems.Add($"{what} '{val}' invalid"); }
        foreach (var me in spec.MagicEffects)
        {
            CheckEnum<MagicEffectArchetype.TypeEnum>(me.Archetype, $"magicEffect '{me.EditorId}' archetype");
            CheckEnum<ActorValue>(me.ActorValue, $"magicEffect '{me.EditorId}' actorValue");
            CheckEnum<ActorValue>(me.MagicSkill, $"magicEffect '{me.EditorId}' magicSkill");
            CheckEnum<ActorValue>(me.ResistValue, $"magicEffect '{me.EditorId}' resistValue");
            CheckEnum<CastType>(me.CastType, $"magicEffect '{me.EditorId}' castType");
            CheckEnum<TargetType>(me.TargetType, $"magicEffect '{me.EditorId}' targetType");
            foreach (var f in me.Flags) CheckEnum<MagicEffect.Flag>(f, $"magicEffect '{me.EditorId}' flag");
            CheckRef(me.Association, $"magicEffect '{me.EditorId}' association");
            CheckRef(me.Projectile, $"magicEffect '{me.EditorId}' projectile");
            CheckRef(me.CastingArt, $"magicEffect '{me.EditorId}' castingArt");
            CheckRef(me.HitEffectArt, $"magicEffect '{me.EditorId}' hitEffectArt");
            CheckRef(me.Explosion, $"magicEffect '{me.EditorId}' explosion");
        }
        foreach (var cl in spec.Classes)
        {
            CheckEnum<Skill>(cl.Teaches, $"class '{cl.EditorId}' teaches");
            foreach (var sk in cl.SkillWeights.Keys) CheckEnum<Skill>(sk, $"class '{cl.EditorId}' skillWeight key");
        }

        foreach (var d in spec.Dialogue)
        {
            if (!questIds.Contains(d.QuestEditorId)) problems.Add($"dialogue '{d.EditorId}' references unknown quest '{d.QuestEditorId}'");
            if (!string.IsNullOrEmpty(d.SpeakerNpcEditorId) && !npcIds.Contains(d.SpeakerNpcEditorId))
                problems.Add($"dialogue '{d.EditorId}' references unknown speaker npc '{d.SpeakerNpcEditorId}'");
            if (string.IsNullOrEmpty(d.Prompt)) problems.Add($"dialogue '{d.EditorId}' has empty prompt");
        }

        var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "int", "float", "bool", "string", "object" };
        foreach (var sa in spec.Scripts)
        {
            if (string.IsNullOrEmpty(sa.ScriptName)) problems.Add($"script attach on '{sa.TargetEditorId}' has empty scriptName");
            if (!ids.Contains(sa.TargetEditorId)) problems.Add($"script '{sa.ScriptName}' targets unknown record '{sa.TargetEditorId}'");
            foreach (var p in sa.Properties)
            {
                if (!validTypes.Contains(p.Type)) problems.Add($"script '{sa.ScriptName}' prop '{p.Name}' has invalid type '{p.Type}'");
                if (string.Equals(p.Type, "object", StringComparison.OrdinalIgnoreCase))
                    CheckRef(p.ObjectEditorId, $"script '{sa.ScriptName}' prop '{p.Name}' object");
            }
        }

        foreach (var pl in spec.Placements)
        {
            CheckRef(pl.Base, "placement base");
            if (!string.IsNullOrWhiteSpace(pl.Worldspace))
            {
                // Exterior placement: worldspace + world position (cell is derived, not authored).
                // Worldspaces aren't built in-spec, so the ref must be a well-formed external one.
                if (!LooksExternalRef(pl.Worldspace) || !TryExternalRef(pl.Worldspace, out _))
                    problems.Add($"placement worldspace '{pl.Worldspace}' must be a well-formed external <master>:0xFORMID ref (find it: find <Skyrim.esm> <name> Worldspace)");
            }
            else if (string.IsNullOrWhiteSpace(pl.Cell)) problems.Add("placement has empty cell (and no worldspace — set one or the other)");
            else if (LooksExternalRef(pl.Cell))
            { if (!TryExternalRef(pl.Cell, out _)) problems.Add($"placement: malformed external cell ref '{pl.Cell}' (expect <master>:0xFORMID)"); }
            else if (!cellIds.Contains(pl.Cell)) problems.Add($"placement references unknown cell '{pl.Cell}' (in-spec cell editorId or <master>:0xFORMID)");
            if (!string.IsNullOrEmpty(pl.Kind) && !pl.Kind.Equals("npc", StringComparison.OrdinalIgnoreCase) && !pl.Kind.Equals("object", StringComparison.OrdinalIgnoreCase))
                problems.Add($"placement kind '{pl.Kind}' invalid (npc|object)");
        }

        foreach (var li in spec.LeveledItems)
        {
            foreach (var e in li.Entries) CheckRef(e.Reference, $"leveledItem '{li.EditorId}' entry");
            foreach (var f in li.Flags) if (!Enum.TryParse<LeveledItem.Flag>(f, true, out _)) problems.Add($"leveledItem '{li.EditorId}' invalid flag '{f}'");
        }
        foreach (var ln in spec.LeveledNpcs)
        {
            foreach (var e in ln.Entries) CheckRef(e.Reference, $"leveledNpc '{ln.EditorId}' entry");
            foreach (var f in ln.Flags) if (!Enum.TryParse<LeveledNpc.Flag>(f, true, out _)) problems.Add($"leveledNpc '{ln.EditorId}' invalid flag '{f}'");
        }
        foreach (var ct in spec.Containers)
            foreach (var e in ct.Items) CheckRef(e.Item, $"container '{ct.EditorId}' item");

        foreach (var co in spec.Recipes)
        {
            if (string.IsNullOrWhiteSpace(co.CreatedObject)) problems.Add($"recipe '{co.EditorId}' has empty createdObject");
            else CheckRef(co.CreatedObject, $"recipe '{co.EditorId}' createdObject");
            CheckRef(co.Workbench, $"recipe '{co.EditorId}' workbench");   // empty -> defaults to forge
            if (co.Components.Count == 0) problems.Add($"recipe '{co.EditorId}' has no components (nothing to consume)");
            foreach (var comp in co.Components) CheckRef(comp.Item, $"recipe '{co.EditorId}' component");
        }

        foreach (var s in spec.Spells)
        {
            if (!string.IsNullOrEmpty(s.SpellType) && !Enum.TryParse<SpellType>(s.SpellType, true, out _)) problems.Add($"spell '{s.EditorId}' invalid spellType '{s.SpellType}'");
            if (!string.IsNullOrEmpty(s.CastType) && !Enum.TryParse<CastType>(s.CastType, true, out _)) problems.Add($"spell '{s.EditorId}' invalid castType '{s.CastType}'");
            if (!string.IsNullOrEmpty(s.TargetType) && !Enum.TryParse<TargetType>(s.TargetType, true, out _)) problems.Add($"spell '{s.EditorId}' invalid targetType '{s.TargetType}'");
        }

        // --- long-tail record types: keyword/effect refs + enum fields ---
        foreach (var i in spec.Ingredients)
        {
            foreach (var k in i.Keywords) CheckRef(k, $"ingredient '{i.EditorId}' keyword");
            CheckEffects(i.EditorId, i.Effects, "ingredient");
        }
        foreach (var a in spec.Ammunitions)
            foreach (var k in a.Keywords) CheckRef(k, $"ammunition '{a.EditorId}' keyword");
        foreach (var s in spec.Scrolls)
        {
            foreach (var k in s.Keywords) CheckRef(k, $"scroll '{s.EditorId}' keyword");
            CheckEffects(s.EditorId, s.Effects, "scroll");
            if (!string.IsNullOrEmpty(s.SpellType) && !Enum.TryParse<SpellType>(s.SpellType, true, out _)) problems.Add($"scroll '{s.EditorId}' invalid spellType '{s.SpellType}'");
            if (!string.IsNullOrEmpty(s.CastType) && !Enum.TryParse<CastType>(s.CastType, true, out _)) problems.Add($"scroll '{s.EditorId}' invalid castType '{s.CastType}'");
            if (!string.IsNullOrEmpty(s.TargetType) && !Enum.TryParse<TargetType>(s.TargetType, true, out _)) problems.Add($"scroll '{s.EditorId}' invalid targetType '{s.TargetType}'");
        }
        foreach (var sg in spec.SoulGems)
        {
            foreach (var k in sg.Keywords) CheckRef(k, $"soulGem '{sg.EditorId}' keyword");
            if (!string.IsNullOrEmpty(sg.MaximumCapacity) && !Enum.TryParse<SoulGem.Level>(sg.MaximumCapacity, true, out _))
                problems.Add($"soulGem '{sg.EditorId}' invalid maximumCapacity '{sg.MaximumCapacity}' (None|Petty|Lesser|Common|Greater|Grand)");
        }
        foreach (var k in spec.Keys)
            foreach (var kw in k.Keywords) CheckRef(kw, $"key '{k.EditorId}' keyword");
        foreach (var ac in spec.Activators)
            foreach (var kw in ac.Keywords) CheckRef(kw, $"activator '{ac.EditorId}' keyword");
        foreach (var o in spec.Outfits)
            foreach (var it in o.Items) CheckRef(it, $"outfit '{o.EditorId}' item");

        if (problems.Count == 0)
        {
            Console.WriteLine($"valid: {Path.GetFileName(specPath)} — {ids.Count} record(s), no problems");
            return 0;
        }
        Console.Error.WriteLine($"INVALID: {Path.GetFileName(specPath)} — {problems.Count} problem(s):");
        foreach (var p in problems) Console.Error.WriteLine($"  - {p}");
        return 1;
    }

    // -------------------------------------------------------------------------------
    //  applyloc — like `apply`, but writes a LOCALIZED plugin with UTF-8
    //  <plugin>_chinese.STRINGS — what Simplified-Chinese SSE expects (verified against
    //  the official CHS translation: its .STRINGS are UTF-8, not GBK). Output is a
    //  folder: <outDir>/<plugin> + <outDir>/Strings/<plugin>_chinese.{STRINGS,IL,DL}.
    // -------------------------------------------------------------------------------
    private static int ApplyLocalized(string inPath, string jsonPath, string outDir)
    {
        var entries = JsonSerializer.Deserialize<List<StringEntry>>(File.ReadAllText(jsonPath)) ?? new();
        var map = entries.Where(e => !string.IsNullOrEmpty(e.Target))
                         .ToDictionary(e => $"{e.FormKey}|{e.Field}|{e.Index}", e => e.Target);

        // Target the Chinese language so string sets land in the Chinese entry + .STRINGS.
        TranslatedString.DefaultLanguage = Language.Chinese;

        var mod = Load(inPath);
        mod.UsingLocalization = true;

        int applied = 0;
        foreach (var s in Slots(mod))
            if (map.TryGetValue($"{s.FormKey}|{s.Field}|{s.Index}", out var t)) { s.Set(t); applied++; }

        Directory.CreateDirectory(outDir);
        var stringsDir = Path.Combine(outDir, "Strings");
        Directory.CreateDirectory(stringsDir);
        var espPath = Path.Combine(outDir, mod.ModKey.FileName);

        var sw = new StringsWriter(GameRelease.SkyrimSE, mod.ModKey, stringsDir, new Utf8EncodingProvider());
        mod.WriteToBinary(espPath, new BinaryWriteParameters
        {
            ModKey = ModKeyOption.NoCheck,
            StringsWriter = sw,
            TargetLanguageOverride = Language.Chinese,
        });
        sw.Dispose();   // flush the .STRINGS files before we rename them

        // Skyrim loads <plugin>_<lang>.STRINGS with a LOWERCASE language suffix; Mutagen
        // writes "_Chinese" — rename to "_chinese" (matters on case-sensitive Linux/Proton,
        // and matches the official CHS mod's naming).
        int renamed = 0;
        foreach (var file in Directory.GetFiles(stringsDir))
        {
            var name = Path.GetFileName(file);
            var lower = name.Replace("_Chinese.", "_chinese.");
            if (!string.Equals(lower, name, StringComparison.Ordinal))
            { File.Move(file, Path.Combine(stringsDir, lower), overwrite: true); renamed++; }
        }

        Console.WriteLine($"applyloc: {applied} string(s) -> {espPath} + {renamed} Strings/*_chinese.* file(s) (UTF-8)");
        return 0;
    }

    // -------------------------------------------------------------------------------
    //  dump — read a plugin back and print its records + the key things generation
    //  wires up (names, npc faction membership, VMAD scripts, dialogue, quest
    //  objectives). Round-trip verification helper + a way to inspect any .esp.
    // -------------------------------------------------------------------------------
    // Search a (possibly huge, e.g. Skyrim.esm) plugin for records whose EditorID or Name
    // contains <query> (case-insensitive). Reads via a lazy read-only OVERLAY so a 250 MB
    // master doesn't get fully materialized. Prints a resolver-ready "<master>:0xFORMID" ref,
    // the record type, EditorID and Name. Optional [type] (e.g. Weapon, Npc, Keyword) filters
    // by record kind, letting the overlay skip whole groups instead of parsing everything.
    private static int Find(string inPath, string query, string? typeName)
    {
        // Vanilla masters are localized: Name is a string index whose text lives in BSA-packed
        // .STRINGS. Point the strings reader at the plugin's own Data folder (BSA override) so it
        // resolves names WITHOUT the game-environment/plugins.txt lookup (absent on Linux).
        var dataDir = Path.GetDirectoryName(Path.GetFullPath(inPath))!;
        var readParams = new BinaryReadParameters
        {
            StringsParam = new StringsReadParameters
            {
                BsaFolderOverride = dataDir,
                StringsFolderOverride = dataDir,
                TargetLanguage = Language.English,
            },
        };
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE, readParams);

        IEnumerable<IMajorRecordGetter> records;
        if (!string.IsNullOrEmpty(typeName))
        {
            var t = typeof(ISkyrimModGetter).Assembly
                .GetType($"Mutagen.Bethesda.Skyrim.I{typeName}Getter", throwOnError: false, ignoreCase: true);
            if (t is null)
            {
                Console.Error.WriteLine(
                    $"Unknown record type '{typeName}'. Examples: Weapon, Armor, Ammunition, Npc, " +
                    "MiscItem, Ingredient, Ingestible, Book, Key, SoulGem, Keyword, Race, Class, " +
                    "Faction, Spell, MagicEffect, Perk, Outfit, LeveledItem, LeveledNpc, Location, Cell, Furniture.");
                return 2;
            }
            records = mod.EnumerateMajorRecords(t, throwIfUnknown: false);
        }
        else
        {
            records = mod.EnumerateMajorRecords();
        }

        // Name is a localized string (BSA-packed for vanilla); resolving it needs the game's
        // archive load order, which isn't available headless on Linux. EditorID + FormID are
        // stored inline and always read. So resolve Name best-effort: on the first failure,
        // stop trying (deterministic) and search EditorID only.
        bool namesOk = true;
        string? NameOf(IMajorRecordGetter r)
        {
            if (!namesOk) return null;
            try { return (r as INamedGetter)?.Name; }
            catch { namesOk = false; return null; }
        }

        var q = query.ToLowerInvariant();
        const int cap = 300;
        int total = 0, shown = 0;
        foreach (var r in records)
        {
            var ed = r.EditorID;
            var name = NameOf(r);
            bool hit = (ed is { } e && e.ToLowerInvariant().Contains(q))
                    || (name is { } n && n.ToLowerInvariant().Contains(q));
            if (!hit) continue;
            total++;
            if (shown++ < cap)
            {
                var fk = r.FormKey;
                Console.WriteLine($"{fk.ModKey}:0x{fk.ID:X6}  {TypeLabel(r)}  {ed}"
                    + (name is { } nm ? $"  \"{nm}\"" : ""));
            }
        }
        Console.WriteLine($"-- {total} match(es)" + (total > cap ? $", showing first {cap}" : "")
            + (namesOk ? "" : "  [names unresolved: search matched EditorID only — see note]"));
        return 0;
    }

    // Diagnostic: walk a plugin's interior CELL block tree and print the block/sub-block each
    // interior cell lives in. Skyrim groups interior cells BY FORMID (block = id % 10, sub =
    // (id/10) % 10); an override in the wrong GRUP is silently ignored by the engine, so this is
    // how you verify a vanilla-cell override landed in the right block WITHOUT an in-game cycle.
    // Optional 0xFORMID arg filters to one cell.
    private static int CellBlk(string inPath, string? formIdHex)
    {
        uint? target = null;
        if (formIdHex is not null)
            target = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        int shown = 0;
        foreach (var block in mod.Cells.Records)
        {
            foreach (var sub in block.SubBlocks)
            {
                foreach (var c in sub.Cells)
                {
                    uint id = c.FormKey.ID;
                    if (target is { } t && id != t) continue;
                    if (target is null && shown >= 60) { Console.WriteLine("…(capped at 60)"); return 0; }
                    Console.WriteLine($"0x{id:X6} (dec {id})  block={block.BlockNumber} sub={sub.BlockNumber}  {c.EditorID}"
                        + $"   [id%10={id % 10}, (id/10)%10={(id / 10) % 10}]");
                    shown++;
                }
            }
        }
        if (target is not null && shown == 0) Console.WriteLine($"0x{target:X6} not found as an interior cell");
        return 0;
    }

    // Diagnostic: print a MagicEffect's full functional field set from any plugin, to compare a
    // generated MGEF against a vanilla one (this is how the It.12 "Recover flag cancels an instant
    // heal" bug was found). Avoids Name/Description (localized string landmine on master overlays).
    private static int MgefDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var r in mod.EnumerateMajorRecords<IMagicEffectGetter>())
        {
            if (r.FormKey.ID != id) continue;
            string F(IFormLinkGetter<IMajorRecordGetter> l) => l.FormKey.IsNull ? "-" : l.FormKey.ToString();
            Console.WriteLine($"0x{id:X6}  EditorID={r.EditorID}");
            Console.WriteLine($"  archetype.Type = {r.Archetype.Type}");
            Console.WriteLine($"  archetype.ActorValue = {r.Archetype.ActorValue}");
            Console.WriteLine($"  archetype.Association = {(r.Archetype.AssociationKey.FormKey.IsNull ? "-" : r.Archetype.AssociationKey.FormKey.ToString())}");
            Console.WriteLine($"  Flags = {r.Flags}");
            Console.WriteLine($"  BaseCost = {r.BaseCost}");
            Console.WriteLine($"  MagicSkill = {r.MagicSkill}   ResistValue = {r.ResistValue}   SecondActorValue = {r.SecondActorValue}");
            Console.WriteLine($"  CastType = {r.CastType}   TargetType = {r.TargetType}");
            Console.WriteLine($"  TaperWeight={r.TaperWeight} TaperCurve={r.TaperCurve} TaperDuration={r.TaperDuration} SkillUsageMult={r.SkillUsageMultiplier}");
            Console.WriteLine($"  MenuDisplayObject={F(r.MenuDisplayObject)} CastingArt={F(r.CastingArt)} HitEffectArt={F(r.HitEffectArt)} Projectile={F(r.Projectile)} Explosion={F(r.Explosion)}");
            Console.WriteLine($"  Keywords={(r.Keywords is null ? "-" : string.Join(",", r.Keywords.Select(k => k.FormKey.ToString())))}");
            Console.WriteLine($"  PerkToApply={F(r.PerkToApply)} EquipAbility={F(r.EquipAbility)} Conditions={r.Conditions.Count}");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a MagicEffect in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Diagnostic: print a Light's radius/color/flags (one 0xFORMID) — or, with no FormID, list every
    // Light that's a decent general ROOM fill (big radius, omnidirectional, on by default, not carried)
    // so we can pick a believable interior light for a generated cell.
    private static int LightDiag(string inPath, string? formIdHex)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        uint? target = formIdHex is null ? null
            : Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        int shown = 0;
        foreach (var l in mod.EnumerateMajorRecords<ILightGetter>())
        {
            if (target is { } t) { if (l.FormKey.ID != t) continue; }
            else
            {
                // room-fill heuristic: radius >= 512, not carried/spot/off-by-default
                bool carried = l.Flags.HasFlag(Light.Flag.CanBeCarried);
                bool spot = l.Flags.HasFlag(Light.Flag.SpotLight) || l.Flags.HasFlag(Light.Flag.ShadowSpotlight);
                bool off = l.Flags.HasFlag(Light.Flag.OffByDefault);
                if (l.Radius < 512 || carried || spot || off) continue;
                if (shown++ >= 40) { Console.WriteLine("…(capped)"); break; }
            }
            Console.WriteLine($"0x{l.FormKey.ID:X6}  {l.EditorID,-34} radius={l.Radius,4} "
                + $"color=({l.Color.R},{l.Color.G},{l.Color.B}) fade={l.FadeValue} flags={l.Flags}");
            if (target is not null) return 0;
        }
        if (target is not null) Console.WriteLine($"0x{target:X6} not a Light in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Concrete Mutagen record class -> friendly type name (strip overlay/getter suffixes).
    private static string TypeLabel(IMajorRecordGetter r)
    {
        var n = r.GetType().Name;
        foreach (var suf in new[] { "BinaryOverlay", "Getter" })
            if (n.EndsWith(suf)) n = n[..^suf.Length];
        return n;
    }

    private static int Dump(string inPath)
    {
        var mod = Load(inPath);
        var edByFk = new Dictionary<FormKey, string>();
        foreach (var r in mod.EnumerateMajorRecords())
            if (!string.IsNullOrEmpty(r.EditorID)) edByFk[r.FormKey] = r.EditorID!;
        string Ref(FormKey fk) => fk.IsNull ? "<null>" : edByFk.TryGetValue(fk, out var ed) ? ed : fk.ToString();

        var masters = mod.MasterReferences;
        Console.WriteLine($"{Path.GetFileName(inPath)} — {mod.EnumerateMajorRecords().Count()} record(s), "
            + $"localized={mod.UsingLocalization}, master(s)=[{string.Join(", ", masters.Select(m => m.Master.FileName.ToString()))}]");
        foreach (var r in mod.EnumerateMajorRecords())
        {
            var name = (r as INamedGetter)?.Name;
            Console.WriteLine($"  [{r.FormKey}] {r.GetType().Name} {r.EditorID}" + (name is { } nm ? $"  \"{nm}\"" : ""));

            if (r is INpcGetter npc)
            {
                if (!npc.Race.IsNull)          Console.WriteLine($"      race -> {Ref(npc.Race.FormKey)}");
                if (!npc.Class.IsNull)         Console.WriteLine($"      class -> {Ref(npc.Class.FormKey)}");
                bool autoCalc = npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.AutoCalcStats);
                if (npc.Configuration.Level is INpcLevelGetter lvl && (lvl.Level != 1 || autoCalc))
                    Console.WriteLine($"      level={lvl.Level} autoCalcStats={autoCalc}");
                if (!npc.DefaultOutfit.IsNull) Console.WriteLine($"      outfit -> {Ref(npc.DefaultOutfit.FormKey)}");
                foreach (var f in npc.Factions)
                    Console.WriteLine($"      faction -> {Ref(f.Faction.FormKey)} (rank {f.Rank})");
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
            }

            if (r is IBookGetter bk && bk.Model?.File is { } bmf)
                Console.WriteLine($"      model={bmf}");                                       // null model => CRASH on read

            if (r is IArmorGetter arm && arm.BodyTemplate is { } bt)
                Console.WriteLine($"      armorRating={arm.ArmorRating} armorType={bt.ArmorType} slots=[{bt.FirstPersonFlags}]");

            if (r is IHasEffectsGetter eff && eff.Effects.Count > 0)
                foreach (var e in eff.Effects)
                    Console.WriteLine($"      effect -> {Ref(e.BaseEffect.FormKey)} (mag={e.Data?.Magnitude} area={e.Data?.Area} dur={e.Data?.Duration})");

            if (r is IWorldspaceGetter wg)
            {
                int blocks = wg.SubCells.Count;
                int cells = wg.SubCells.SelectMany(b => b.Items).SelectMany(s => s.Items).Count();
                Console.WriteLine($"      worldspace: {blocks} block(s), {cells} exterior cell(s)"
                    + $" nameSet={wg.Name is not null}"
                    + (wg.LandDefaults is { } wld ? $" defaultWater={wld.DefaultWaterHeight}" : " defaultWater=<none>"));
            }

            if (r is ICellGetter cg)
                Console.WriteLine($"      cell: interior={cg.Flags.HasFlag(Cell.Flag.IsInteriorCell)}"
                    + (cg.Grid?.Point is { } gp ? $" grid=({gp.X},{gp.Y})" : "")
                    + (cg.WaterHeight is { } wh ? $" water={wh}" : " water=<none>")
                    + (cg.LightingTemplate.IsNull ? "" : $" lightTmpl={cg.LightingTemplate.FormKey}")
                    + $" persistent={cg.Persistent.Count} temporary={cg.Temporary.Count}");

            if (r is IPlacedNpcGetter pnpc && pnpc.Placement is { } pp)
                Console.WriteLine($"      placed npc -> base {Ref(pnpc.Base.FormKey)} @ ({pp.Position.X:0.#}, {pp.Position.Y:0.#}, {pp.Position.Z:0.#})");

            if (r is IPlacedObjectGetter pobj && pobj.Placement is { } op)
                Console.WriteLine($"      placed obj -> base {Ref(pobj.Base.FormKey)} @ ({op.Position.X:0.#}, {op.Position.Y:0.#}, {op.Position.Z:0.#})");

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
            }

            if (r is ISpellGetter spG && (spG.Type != SpellType.Spell || spG.CastType != CastType.ConstantEffect || spG.BaseCost > 0))
                Console.WriteLine($"      spell: type={spG.Type} cast={spG.CastType} target={spG.TargetType} cost={spG.BaseCost}");

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

            if ((r is IStaticGetter || r is IActivatorGetter) && r is IModeledGetter mdl && mdl.Model?.File is { } mf)
                Console.WriteLine($"      model: {mf.GivenPath}");

            if ((r is IMiscItemGetter || r is IIngestibleGetter) && r is IModeledGetter im && im.Model?.File is { } imf)
                Console.WriteLine($"      model={imf}");      // null model => no 3D mesh when dropped

            if (r is IHaveVirtualMachineAdapterGetter hv && hv.VirtualMachineAdapter is { } vm)
                foreach (var se in vm.Scripts)
                    Console.WriteLine($"      script: {se.Name} [{se.Properties.Count} prop(s)]");

            if (r is IDialogTopicGetter dt)
                Console.WriteLine($"      prompt: \"{dt.Name?.String}\"  ({dt.Responses.Count} INFO group(s))");

            if (r is IQuestGetter q)
                foreach (var o in q.Objectives)
                    Console.WriteLine($"      objective[{o.Index}]: \"{o.DisplayText?.String}\"");
        }
        return 0;
    }
}

// UTF-8 for every language — Simplified-Chinese SSE reads UTF-8 .STRINGS (not GBK).
internal sealed class Utf8EncodingProvider : IMutagenEncodingProvider
{
    public IMutagenEncoding GetEncoding(GameRelease release, Language language) => MutagenEncoding._utf8;
}

// A translatable text slot: where it lives + accessors. extract reads Get(); apply calls Set(target).
internal sealed class Slot
{
    public string FormKey { get; }
    public string Type { get; }
    public string Field { get; }
    public int Index { get; }
    public Func<string?> Get { get; }
    public Action<string> Set { get; }

    public Slot(string formKey, string type, string field, int index, Func<string?> get, Action<string> set)
    {
        FormKey = formKey; Type = type; Field = field; Index = index; Get = get; Set = set;
    }
}

internal sealed class StringEntry
{
    public string FormKey { get; set; } = "";
    public string Type { get; set; } = "";
    public string Field { get; set; } = "";
    public int Index { get; set; }
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
}

// --- ESP generator spec (the structured IR; deserialized case-insensitively) ---------
internal sealed class ModSpec
{
    public string PluginName { get; set; } = "Generated.esp";
    public bool Esl { get; set; } = true;
    public List<MiscSpec> MiscItems { get; set; } = new();
    public List<BookSpec> Books { get; set; } = new();
    public List<WeaponSpec> Weapons { get; set; } = new();
    public List<NpcSpec> Npcs { get; set; } = new();
    public List<QuestSpec> Quests { get; set; } = new();
    public List<DialogueSpec> Dialogue { get; set; } = new();
    public List<SpellSpec> Spells { get; set; } = new();
    public List<MagicEffectSpec> MagicEffects { get; set; } = new();
    public List<PotionSpec> Potions { get; set; } = new();
    public List<ArmorSpec> Armors { get; set; } = new();
    public List<FactionSpec> Factions { get; set; } = new();
    public List<MessageSpec> Messages { get; set; } = new();
    public List<ScriptAttachSpec> Scripts { get; set; } = new();
    public List<CellSpec> Cells { get; set; } = new();
    public List<PlacementSpec> Placements { get; set; } = new();
    public List<LeveledItemSpec> LeveledItems { get; set; } = new();
    public List<LeveledNpcSpec> LeveledNpcs { get; set; } = new();
    public List<ContainerSpec> Containers { get; set; } = new();
    public List<IngredientSpec> Ingredients { get; set; } = new();
    public List<AmmunitionSpec> Ammunitions { get; set; } = new();
    public List<ScrollSpec> Scrolls { get; set; } = new();
    public List<SoulGemSpec> SoulGems { get; set; } = new();
    public List<KeySpec> Keys { get; set; } = new();
    public List<KeywordSpec> Keywords { get; set; } = new();
    public List<OutfitSpec> Outfits { get; set; } = new();
    public List<StaticSpec> Statics { get; set; } = new();
    public List<ActivatorSpec> Activators { get; set; } = new();
    public List<RecipeSpec> Recipes { get; set; } = new();
    public List<ClassSpec> Classes { get; set; } = new();
}
// "ref" fields below accept EITHER an in-spec editorId OR an external "<master>:0xFORMID"
// (e.g. "Skyrim.esm:0x013746" — find them with the `find` command). External refs auto-add
// the master on write (Mutagen MastersListContent=Iterate).
internal sealed class MiscSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<string> Keywords { get; set; } = new(); public string Template { get; set; } = ""; }
internal sealed class BookSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Text { get; set; } = ""; public string Template { get; set; } = ""; }
internal sealed class WeaponSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public ushort Damage { get; set; } public float Speed { get; set; } public float Reach { get; set; } public List<string> Keywords { get; set; } = new(); public string Template { get; set; } = ""; }
internal sealed class NpcSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> Factions { get; set; } = new();
    public string Race { get; set; } = "";       // ref (e.g. Skyrim.esm:0x013746 = NordRace)
    public string Class { get; set; } = "";       // ref
    public string Outfit { get; set; } = "";      // ref -> DefaultOutfit
    public int Level { get; set; }                 // fixed level (0 = leave default); needed for class stat auto-calc
    public bool AutoCalcStats { get; set; }        // derive H/M/S + skills from level + class (else flat defaults)
}
internal sealed class QuestSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public List<ObjectiveSpec> Objectives { get; set; } = new(); }
internal sealed class ObjectiveSpec { public ushort Index { get; set; } public string Text { get; set; } = ""; }
// A dialogue topic: shown under QuestEditorId's branch; targets SpeakerNpcEditorId (GetIsID).
internal sealed class DialogueSpec
{
    public string EditorId { get; set; } = "";
    public string QuestEditorId { get; set; } = "";
    public string SpeakerNpcEditorId { get; set; } = "";
    public string Prompt { get; set; } = "";
    public List<string> Responses { get; set; } = new();
}
internal sealed class SpellSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<EffectSpec> Effects { get; set; } = new();
    public string SpellType { get; set; } = "";   // Spell|Power|LesserPower|Ability|Disease|Poison|Voice
    public string CastType { get; set; } = "";     // FireAndForget|Concentration|ConstantEffect
    public string TargetType { get; set; } = "";    // Self|Touch|Aimed|TargetActor|TargetLocation
    public uint BaseCost { get; set; }
    public float ChargeTime { get; set; }
}
internal sealed class PotionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<EffectSpec> Effects { get; set; } = new(); public string Template { get; set; } = ""; }
// MagicEffect (MGEF): the building block a spell/potion/ingredient/scroll `effect` points at — lets a
// spec define its OWN effect instead of only reusing vanilla ones. `archetype` (MagicEffectArchetype.
// TypeEnum: ValueModifier = the common damage/heal/fortify, plus SummonCreature, Bound, Light,
// Paralysis, …) acts on `actorValue` (Health/Magicka/Stamina/…). `magicSkill` is the school
// (Alteration/Conjuration/Destruction/Illusion/Restoration), `resistValue` the AV that resists it
// (ResistFire/PoisonResist/…). `flags` (Hostile/Detrimental/Recover/NoArea/NoDuration/…) drive UI +
// behaviour. `association` (a ref) is the summoned/bound form for those archetypes. The per-effect
// magnitude/area/duration stay on the spell/potion's `effects[]` entry (not here).
internal sealed class MagicEffectSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Archetype { get; set; } = "ValueModifier";
    public string ActorValue { get; set; } = "";   // affected AV, e.g. Health
    public string MagicSkill { get; set; } = "";    // school, e.g. Destruction
    public string ResistValue { get; set; } = "";    // resisted by, e.g. ResistFire
    public string CastType { get; set; } = "";        // FireAndForget|Concentration|ConstantEffect
    public string TargetType { get; set; } = "";       // Self|Touch|Aimed|TargetActor|TargetLocation
    public float BaseCost { get; set; }
    public List<string> Flags { get; set; } = new();
    public string Association { get; set; } = "";       // summon/bound form ref (optional)
    // Visual/projectile refs (optional, usually vanilla) — needed for an Aimed spell to have a
    // visible traveling bolt + cast/impact FX. The projectile carries its own model + impact.
    public string Projectile { get; set; } = "";        // PROJ — the thing that travels (Aimed)
    public string CastingArt { get; set; } = "";        // ARTO — FX at the caster's hands
    public string HitEffectArt { get; set; } = "";      // ARTO — FX at the impact point
    public string Explosion { get; set; } = "";          // EXPL — AoE explosion on impact
}
internal sealed class ArmorSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public float ArmorRating { get; set; } public string ArmorType { get; set; } = ""; public List<string> Slots { get; set; } = new(); public List<string> Keywords { get; set; } = new(); }
// One magic effect on a spell/potion: a MagicEffect ref + magnitude/area/duration (EffectData).
internal sealed class EffectSpec { public string MagicEffect { get; set; } = ""; public float Magnitude { get; set; } public int Area { get; set; } public int Duration { get; set; } }
internal sealed class FactionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; }
// Class (CLAS): an actor's "profession" — drives its attribute distribution + favoured skills (and,
// for trainers, what it `teaches`). An npc's `class` ref can point at one. `healthWeight`/
// `magickaWeight`/`staminaWeight` are the BasicStat distribution (relative %, ~sum 100); `skillWeights`
// maps a Skill name (OneHanded/Destruction/Sneak/…) to a 0–255 favour. `teaches` (a Skill) +
// `maxTrainingLevel` matter only for trainer NPCs.
internal sealed class ClassSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Teaches { get; set; } = "";
    public int MaxTrainingLevel { get; set; }
    public int HealthWeight { get; set; }
    public int MagickaWeight { get; set; }
    public int StaminaWeight { get; set; }
    public Dictionary<string, int> SkillWeights { get; set; } = new();
}
internal sealed class MessageSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Description { get; set; } = ""; }
// Attach a compiled Papyrus script (by Scriptname) to a record (by editorId), with
// typed properties. type ∈ int|float|bool|string|object; object resolves ObjectEditorId.
internal sealed class ScriptAttachSpec
{
    public string TargetEditorId { get; set; } = "";
    public string ScriptName { get; set; } = "";
    public string Source { get; set; } = "";   // optional .psc path (rel. to spec) for `package` to compile
    public List<PropertySpec> Properties { get; set; } = new();
}
internal sealed class PropertySpec
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int Int { get; set; }
    public float Float { get; set; }
    public bool Bool { get; set; }
    public string Str { get; set; } = "";
    public string ObjectEditorId { get; set; } = "";
}
// A new interior cell the plugin creates (reachable in-game via `coc <editorId>`).
// `template` (optional, a vanilla INTERIOR cell ref "<master>:0xFORMID") copies that cell's
// lighting/water environment so a brand-new cell isn't pitch-black; it still needs a floor
// static placed in it (a `placement`) so the player doesn't fall into the void.
internal sealed class CellSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Template { get; set; } = ""; }
internal sealed class Vec3 { public float X { get; set; } public float Y { get; set; } public float Z { get; set; } }
// Place a base form (npc/object, in-spec or external) into the world at a position/rotation.
// TWO targeting modes:
//   * INTERIOR: set `cell` to an in-spec interior cell editorId (It.7d-p1) OR a vanilla interior
//     cell ref "<master>:0xFORMID" (It.7d-p2). `position` is local to that cell.
//   * EXTERIOR: set `worldspace` to a worldspace ref "<master>:0xFORMID" (e.g. Tamriel =
//     Skyrim.esm:0x00003C, find via `find <Skyrim.esm> <name> Worldspace`); `position` is the
//     WORLD position. The cell at floor(x/4096),floor(y/4096) is found in the master and
//     overridden to add this ref (It.7d-p3). `worldspace` wins over `cell` if both are set.
// `rotation` is in degrees. `kind` ("npc"|"object") is inferred for in-spec bases, "object" else.
internal sealed class PlacementSpec
{
    public string Base { get; set; } = "";
    public string Cell { get; set; } = "";        // interior: in-spec editorId OR <master>:0xFORMID
    public string Worldspace { get; set; } = "";   // exterior: worldspace ref; position is world-space
    public string Kind { get; set; } = "";
    public Vec3 Position { get; set; } = new();
    public Vec3 Rotation { get; set; } = new();
    public bool Persistent { get; set; }
}
// One entry in a leveled list: a ref (item or npc) that appears at >= Level, Count copies.
internal sealed class LeveledEntrySpec { public string Reference { get; set; } = ""; public short Level { get; set; } = 1; public short Count { get; set; } = 1; }
// LeveledItem (LVLI) / LeveledNpc (LVLN): chanceNone (0-100), flag names, weighted entries.
internal sealed class LeveledItemSpec { public string EditorId { get; set; } = ""; public int ChanceNone { get; set; } public List<string> Flags { get; set; } = new(); public List<LeveledEntrySpec> Entries { get; set; } = new(); }
internal sealed class LeveledNpcSpec { public string EditorId { get; set; } = ""; public int ChanceNone { get; set; } public List<string> Flags { get; set; } = new(); public List<LeveledEntrySpec> Entries { get; set; } = new(); }
// Container (CONT): named, with a list of item refs + counts.
internal sealed class ContainerEntrySpec { public string Item { get; set; } = ""; public int Count { get; set; } = 1; }
internal sealed class ContainerSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public float Weight { get; set; } public List<ContainerEntrySpec> Items { get; set; } = new(); }
// One required ingredient in a recipe: a *ref* (in-spec or vanilla) + how many are consumed.
internal sealed class RecipeComponentSpec { public string Item { get; set; } = ""; public int Count { get; set; } = 1; }
// ConstructibleObject (COBJ): a crafting recipe. `createdObject` (a *ref*, usually an in-spec item)
// is made in `count` copies at the `workbench` (a Keyword *ref*; defaults to the forge —
// Skyrim.esm:0x088105 CraftingSmithingForge) by consuming the `components`. Perk/skill gating
// (Conditions) is not yet a spec field — a recipe with components but no condition shows whenever
// you have the materials.
internal sealed class RecipeSpec
{
    public string EditorId { get; set; } = "";
    public string CreatedObject { get; set; } = "";
    public int Count { get; set; } = 1;
    public string Workbench { get; set; } = "";   // bench keyword ref; empty -> forge
    public List<RecipeComponentSpec> Components { get; set; } = new();
}

// --- Long-tail record types (same spec-class + build-loop pattern) ---------------------
// Ingredient (INGR): an alchemy reagent — value/weight + `effects` (reuses the spell/potion
// effect pipeline) + keywords.
internal sealed class IngredientSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<EffectSpec> Effects { get; set; } = new(); public List<string> Keywords { get; set; } = new(); }
// Ammunition (AMMO): arrow/bolt — value/weight + `damage` (float) + keywords.
internal sealed class AmmunitionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public float Damage { get; set; } public List<string> Keywords { get; set; } = new(); }
// Scroll (SCRL): a one-shot spell-as-item — value/weight + `effects` + spell cast fields.
internal sealed class ScrollSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<EffectSpec> Effects { get; set; } = new(); public string SpellType { get; set; } = ""; public string CastType { get; set; } = ""; public string TargetType { get; set; } = ""; public uint BaseCost { get; set; } public List<string> Keywords { get; set; } = new(); }
// SoulGem (SLGM): value/weight + `maximumCapacity` (None|Petty|Lesser|Common|Greater|Grand) + keywords.
internal sealed class SoulGemSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public string MaximumCapacity { get; set; } = ""; public List<string> Keywords { get; set; } = new(); }
// Key (KEYM): value/weight + keywords.
internal sealed class KeySpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public List<string> Keywords { get; set; } = new(); }
// Keyword (KYWD): just an editorId — define your own so in-spec records can reference it in
// their `keywords` lists (e.g. a custom "VendorItemFood" category).
internal sealed class KeywordSpec { public string EditorId { get; set; } = ""; }
// Outfit (OTFT): a named set of item *refs* (armors/weapons) an NPC can wear; an npc `outfit`
// ref can point at an in-spec outfit's editorId.
internal sealed class OutfitSpec { public string EditorId { get; set; } = ""; public List<string> Items { get; set; } = new(); }
// Static (STAT): a world mesh — just `model` (a .nif path; reference a vanilla mesh in the BSA).
// A placement base for scenery; no Name (statics are nameless).
internal sealed class StaticSpec { public string EditorId { get; set; } = ""; public string Model { get; set; } = ""; }
// Activator (ACTI): an interactable world object — name + `model` + keywords (+ a script via
// `scripts`). A placement base you can walk up to / attach behaviour to.
internal sealed class ActivatorSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Model { get; set; } = ""; public List<string> Keywords { get; set; } = new(); }
