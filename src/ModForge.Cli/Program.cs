using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;

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
                case "extract" when args.Length == 3: Extract(args[1], args[2]); return 0;
                case "apply" when args.Length == 4:   Apply(args[1], args[2], args[3]); return 0;
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
        "  extract <in.esp> <strings.json>\n" +
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
            var typeName = rec.GetType().Name.TrimStart('I');

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

        if (spec.Esl) mod.IsSmallMaster = true;
        Write(mod, outPath);
        int total = spec.MiscItems.Count + spec.Books.Count + spec.Weapons.Count + spec.Npcs.Count
                    + spec.Quests.Count + dialogueBuilt;
        Console.WriteLine($"built {outPath} from {Path.GetFileName(specPath)} " +
                          $"(ESL={spec.Esl}, {total} top-level record(s); {dialogueBuilt} dialogue topic(s))");
    }
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
}
internal sealed class MiscSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public uint Value { get; set; } public float Weight { get; set; } }
internal sealed class BookSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Text { get; set; } = ""; }
internal sealed class WeaponSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; }
internal sealed class NpcSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; }
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
