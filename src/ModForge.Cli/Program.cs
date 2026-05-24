using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
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
                default: Usage(); return 1;
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"ERROR: {e.GetType().Name}: {e.Message}");
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

        foreach (var m in spec.MiscItems)
        {
            var r = mod.MiscItems.AddNew();
            r.EditorID = m.EditorId; r.Name = m.Name; r.Value = m.Value; r.Weight = m.Weight;
        }
        foreach (var b in spec.Books)
        {
            var r = mod.Books.AddNew();
            r.EditorID = b.EditorId; r.Name = b.Name; r.BookText = b.Text;
        }
        foreach (var w in spec.Weapons)
        {
            var r = mod.Weapons.AddNew();
            r.EditorID = w.EditorId; r.Name = w.Name;
        }
        // NPCs + quests are kept in editorId->record maps so dialogue can reference them.
        var npcsByEd = new Dictionary<string, Npc>();
        foreach (var n in spec.Npcs)
        {
            var r = mod.Npcs.AddNew();
            r.EditorID = n.EditorId; r.Name = n.Name;
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

        foreach (var s in spec.Spells)
        {
            var r = mod.Spells.AddNew();
            r.EditorID = s.EditorId; r.Name = s.Name;
        }
        foreach (var p in spec.Potions)
        {
            var r = mod.Ingestibles.AddNew();
            r.EditorID = p.EditorId; r.Name = p.Name; r.Value = p.Value; r.Weight = p.Weight;
        }
        foreach (var a in spec.Armors)
        {
            var r = mod.Armors.AddNew();
            r.EditorID = a.EditorId; r.Name = a.Name;
            r.Value = a.Value; r.Weight = a.Weight; r.ArmorRating = a.ArmorRating;
        }
        foreach (var f in spec.Factions)
        {
            var r = mod.Factions.AddNew();
            r.EditorID = f.EditorId; r.Name = f.Name;
        }
        foreach (var msg in spec.Messages)
        {
            var r = mod.Messages.AddNew();
            r.EditorID = msg.EditorId; r.Name = msg.Name; r.Description = msg.Description;
        }

        // --- pass 2: resolve cross-record references by editorId -> FormLink ---
        // All records exist now, so build one editorId -> FormKey table and wire links
        // that may point forward (e.g. an NPC listed before the faction it belongs to).
        var formKeyByEd = new Dictionary<string, FormKey>();
        var recordsByEd = new Dictionary<string, IMajorRecord>();
        foreach (var r in mod.EnumerateMajorRecords())
            if (!string.IsNullOrEmpty(r.EditorID))
            { formKeyByEd[r.EditorID!] = r.FormKey; recordsByEd[r.EditorID!] = r; }

        int linksWired = 0;
        foreach (var n in spec.Npcs)
        {
            if (n.Factions.Count == 0 || !npcsByEd.TryGetValue(n.EditorId, out var npcRec)) continue;
            foreach (var factionEd in n.Factions)
            {
                if (!formKeyByEd.TryGetValue(factionEd, out var fk))
                {
                    Console.WriteLine($"  ! npc '{n.EditorId}' faction '{factionEd}' not found in spec");
                    continue;
                }
                var rp = new RankPlacement { Rank = 0 };
                rp.Faction.SetTo(fk);
                npcRec.Factions.Add(rp);
                linksWired++;
            }
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
        int total = spec.MiscItems.Count + spec.Books.Count + spec.Weapons.Count + spec.Npcs.Count
                    + spec.Quests.Count + dialogueBuilt
                    + spec.Spells.Count + spec.Potions.Count + spec.Armors.Count
                    + spec.Factions.Count + spec.Messages.Count;
        Console.WriteLine($"built {outPath} from {Path.GetFileName(specPath)} " +
                          $"(ESL={spec.Esl}, {total} top-level record(s); {dialogueBuilt} dialogue topic(s); " +
                          $"{linksWired} cross-ref link(s); {scriptsAttached} script(s) attached)");
    }

    private static ScriptProperty? MakeObjectProp(PropertySpec p, Dictionary<string, FormKey> formKeyByEd)
    {
        if (string.IsNullOrEmpty(p.ObjectEditorId) || !formKeyByEd.TryGetValue(p.ObjectEditorId, out var fk))
            return null;
        var op = new ScriptObjectProperty();
        op.Object.SetTo(fk);
        return op;
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

        foreach (var n in spec.Npcs)
            foreach (var fac in n.Factions)
                if (!factionIds.Contains(fac)) problems.Add($"npc '{n.EditorId}' references unknown faction '{fac}'");

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
                if (string.Equals(p.Type, "object", StringComparison.OrdinalIgnoreCase) && !ids.Contains(p.ObjectEditorId))
                    problems.Add($"script '{sa.ScriptName}' prop '{p.Name}' object references unknown record '{p.ObjectEditorId}'");
            }
        }

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
    private static int Dump(string inPath)
    {
        var mod = Load(inPath);
        var edByFk = new Dictionary<FormKey, string>();
        foreach (var r in mod.EnumerateMajorRecords())
            if (!string.IsNullOrEmpty(r.EditorID)) edByFk[r.FormKey] = r.EditorID!;
        string Ref(FormKey fk) => fk.IsNull ? "<null>" : edByFk.TryGetValue(fk, out var ed) ? ed : fk.ToString();

        Console.WriteLine($"{Path.GetFileName(inPath)} — {mod.EnumerateMajorRecords().Count()} record(s), localized={mod.UsingLocalization}");
        foreach (var r in mod.EnumerateMajorRecords())
        {
            var name = (r as INamedGetter)?.Name;
            Console.WriteLine($"  [{r.FormKey}] {r.GetType().Name} {r.EditorID}" + (name is { } nm ? $"  \"{nm}\"" : ""));

            if (r is INpcGetter npc)
                foreach (var f in npc.Factions)
                    Console.WriteLine($"      faction -> {Ref(f.Faction.FormKey)} (rank {f.Rank})");

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
    public List<PotionSpec> Potions { get; set; } = new();
    public List<ArmorSpec> Armors { get; set; } = new();
    public List<FactionSpec> Factions { get; set; } = new();
    public List<MessageSpec> Messages { get; set; } = new();
    public List<ScriptAttachSpec> Scripts { get; set; } = new();
}
internal sealed class MiscSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } }
internal sealed class BookSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Text { get; set; } = ""; }
internal sealed class WeaponSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; }
internal sealed class NpcSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public List<string> Factions { get; set; } = new(); }
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
internal sealed class SpellSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; }
internal sealed class PotionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } }
internal sealed class ArmorSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } public float ArmorRating { get; set; } }
internal sealed class FactionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; }
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
