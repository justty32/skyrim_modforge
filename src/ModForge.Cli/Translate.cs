internal static partial class Program
{
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
}
