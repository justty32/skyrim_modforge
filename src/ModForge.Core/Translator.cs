namespace ModForge;

/// <summary>
/// The translate pipeline: pull every translatable string out of a plugin (<see cref="Extract"/>),
/// and write edited strings back, either inline (<see cref="Apply"/>) or as a Localized UTF-8
/// <c>.STRINGS</c> set (<see cref="ApplyLocalized"/>). Works on a loaded <see cref="ISkyrimMod"/>;
/// the caller owns reading/writing the JSON contract.
/// </summary>
public static class Translator
{
    // -------------------------------------------------------------------------------
    //  Every translatable text slot: where it lives + how to read/write it. Extract
    //  and Apply iterate the SAME Slots(mod) so they stay aligned; apply matches by
    //  (FormKey, Field, Index). Add a record type here to extend coverage.
    // -------------------------------------------------------------------------------
    internal static IEnumerable<Slot> Slots(ISkyrimMod mod)
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

    /// <summary>Pull every translatable string out of a mod (each entry has Source set, Target empty).</summary>
    public static List<StringEntry> Extract(ISkyrimMod mod) =>
        Slots(mod).Select(s => new StringEntry
        {
            FormKey = s.FormKey, Type = s.Type, Field = s.Field, Index = s.Index,
            Source = s.Get() ?? "", Target = "",
        }).ToList();

    // Match key for an entry/slot: (FormKey, Field, Index).
    private static Dictionary<string, string> TargetMap(IEnumerable<StringEntry> translations) =>
        translations.Where(e => !string.IsNullOrEmpty(e.Target))
                    .ToDictionary(e => $"{e.FormKey}|{e.Field}|{e.Index}", e => e.Target);

    /// <summary>
    /// Write each non-empty <see cref="StringEntry.Target"/> back into the mod (inline). Mutates
    /// <paramref name="mod"/> in place; the caller writes it. Returns the number of slots set.
    /// </summary>
    public static int Apply(ISkyrimMod mod, IEnumerable<StringEntry> translations)
    {
        var map = TargetMap(translations);
        int applied = 0;
        foreach (var s in Slots(mod))
            if (map.TryGetValue($"{s.FormKey}|{s.Field}|{s.Index}", out var target)) { s.Set(target); applied++; }
        return applied;
    }

    /// <summary>
    /// Apply translations as a LOCALIZED plugin with UTF-8 <c>&lt;plugin&gt;_chinese.STRINGS</c> — what
    /// Simplified-Chinese SSE expects (its .STRINGS are UTF-8, not GBK). Writes
    /// <paramref name="outDir"/>/&lt;plugin&gt; + <paramref name="outDir"/>/Strings/&lt;plugin&gt;_chinese.{STRINGS,IL,DL}.
    /// Returns (applied, renamed, espPath).
    /// </summary>
    public static (int Applied, int Renamed, string EspPath) ApplyLocalized(
        ISkyrimMod mod, IEnumerable<StringEntry> translations, string outDir)
    {
        var map = TargetMap(translations);

        // Target the Chinese language so string sets land in the Chinese entry + .STRINGS.
        TranslatedString.DefaultLanguage = Language.Chinese;
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

        return (applied, renamed, espPath);
    }
}
