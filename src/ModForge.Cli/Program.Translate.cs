internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  extract
    // -------------------------------------------------------------------------------
    private static void ExtractCmd(string inPath, string jsonPath)
    {
        var mod = Load(inPath);
        var entries = Translator.Extract(mod);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(entries, JsonOpts));
        Console.WriteLine($"extracted {entries.Count} string(s) from {Path.GetFileName(inPath)} -> {jsonPath}");
        foreach (var e in entries.Take(20))
            Console.WriteLine($"  {e.FormKey} {e.Type}.{e.Field}[{e.Index}] = \"{e.Source}\"");
        if (entries.Count > 20) Console.WriteLine($"  … +{entries.Count - 20} more");
    }

    // -------------------------------------------------------------------------------
    //  apply
    // -------------------------------------------------------------------------------
    private static void ApplyCmd(string inPath, string jsonPath, string outPath)
    {
        var entries = JsonSerializer.Deserialize<List<StringEntry>>(File.ReadAllText(jsonPath)) ?? new();
        // Distinct (FormKey|Field|Index) targets — matches what Translator.Apply can actually set.
        int requested = entries.Where(e => !string.IsNullOrEmpty(e.Target))
                               .Select(e => $"{e.FormKey}|{e.Field}|{e.Index}").Distinct().Count();
        var mod = Load(inPath);
        int applied = Translator.Apply(mod, entries);
        PluginIo.Write(mod, outPath);
        Console.WriteLine($"applied {applied}/{requested} translation(s) -> {Path.GetFileName(outPath)}");
    }

    // -------------------------------------------------------------------------------
    //  applyloc
    // -------------------------------------------------------------------------------
    private static int ApplyLocalizedCmd(string inPath, string jsonPath, string outDir)
    {
        var entries = JsonSerializer.Deserialize<List<StringEntry>>(File.ReadAllText(jsonPath)) ?? new();
        var mod = Load(inPath);
        var (applied, renamed, espPath) = Translator.ApplyLocalized(mod, entries, outDir);
        Console.WriteLine($"applyloc: {applied} string(s) -> {espPath} + {renamed} Strings/*_chinese.* file(s) (UTF-8)");
        return 0;
    }
}
