// =====================================================================================
//  ModForge.Cli — thin CLI over ModForge.Core.
//
//  This project owns argv parsing, JSON read/write, console output and exit codes.
//  All generation/translation logic lives in ModForge.Core (Generator/Translator/Demo/
//  Papyrus) and works on objects — so it can also be referenced as a library. The
//  diagnostic (find/dump/*diag) commands live alongside this file in Diagnostics.cs.
// =====================================================================================

internal static partial class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0) { Usage(); return 1; }
        try
        {
            switch (args[0])
            {
                case "gen" when args.Length == 2:      GenCmd(args[1]); return 0;
                case "build" when args.Length == 3:    BuildCmd(args[1], args[2]); return 0;
                case "compile" when args.Length == 3:  return CompileCmd(args[1], args[2]);
                case "package" when args.Length == 3:  return PackageCmd(args[1], args[2]);
                case "validate" when args.Length == 2: return ValidateCmd(args[1]);
                case "extract" when args.Length == 3:  ExtractCmd(args[1], args[2]); return 0;
                case "apply" when args.Length == 4:    ApplyCmd(args[1], args[2], args[3]); return 0;
                case "applyloc" when args.Length == 4: return ApplyLocalizedCmd(args[1], args[2], args[3]);
                case "dump" when args.Length == 2:     return Dump(args[1]);
                case "find" when args.Length is 3 or 4: return Find(args[1], args[2], args.Length == 4 ? args[3] : null);
                case "cellblk" when args.Length is 2 or 3: return CellBlk(args[1], args.Length == 3 ? args[2] : null);
                case "mgefdiag" when args.Length == 3: return MgefDiag(args[1], args[2]);
                case "lightdiag" when args.Length is 2 or 3: return LightDiag(args[1], args.Length == 3 ? args[2] : null);
                case "packagediag" when args.Length == 3: return PackageDiag(args[1], args[2]);
                case "pkgsbytemplate" when args.Length == 3: return PkgsByTemplate(args[1], args[2]);
                case "npcdiag" when args.Length == 3: return NpcDiag(args[1], args[2]);
                case "cstydiag" when args.Length == 3: return CstyDiag(args[1], args[2]);
                case "refpos" when args.Length == 3: return RefPos(args[1], args[2]);
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
        "  packagediag <in.esp> <0xFORMID>              print a Package's template/flags/schedule/data inputs\n" +
        "  npcdiag <in.esp> <0xFORMID>                  print an Npc's race/class/voice/factions/packages/flags (for cross-cell diff vs vanilla)\n" +
        "  cstydiag <in.esp> <0xFORMID>                 print a CombatStyle's offensive/defensive mults + equipment preferences + flags\n" +
        "  refpos <in.esp> <0xFORMID>                   print a placed ref's (REFR/ACHR) position+rotation+base (anchor new placements on known navmesh)\n" +
        "  extract <in.esp> <strings.json>\n" +
        "  applyloc <in.esp> <strings.json> <outDir>   (Localized UTF-8 _chinese.STRINGS)\n" +
        "  apply   <in.esp> <strings.json> <out.esp>");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions ReadOpts = new() { PropertyNameCaseInsensitive = true };

    private static ModSpec ReadSpec(string path) =>
        JsonSerializer.Deserialize<ModSpec>(File.ReadAllText(path), ReadOpts)
        ?? throw new InvalidOperationException("spec deserialized to null");

    // Shared loader (also used by the diagnostic commands in Diagnostics.cs).
    private static ISkyrimMod Load(string path) => PluginIo.Load(path);

    // -------------------------------------------------------------------------------
    //  gen
    // -------------------------------------------------------------------------------
    private static void GenCmd(string outPath)
    {
        var key = ModKey.FromNameAndExtension(Path.GetFileName(outPath));
        var mod = Demo.CreateDemoPlugin(key);
        mod.WriteToBinary(outPath);
        Console.WriteLine($"wrote {outPath}  (ESL={mod.IsSmallMaster}, {mod.EnumerateMajorRecords().Count()} records)");
    }

    // -------------------------------------------------------------------------------
    //  build
    // -------------------------------------------------------------------------------
    private static void BuildCmd(string specPath, string outPath)
    {
        var spec = ReadSpec(specPath);
        var key = ModKey.FromNameAndExtension(Path.GetFileName(outPath));
        var result = Generator.Build(spec, key);
        PluginIo.Write(result.Mod, outPath);
        foreach (var w in result.Warnings) Console.WriteLine(w);
        Console.WriteLine(BuildSummary(result.Stats, specPath, outPath));
    }

    private static string BuildSummary(BuildStats s, string specPath, string outPath) =>
        $"built {outPath} from {Path.GetFileName(specPath)} " +
        $"(ESL={s.Esl}, {s.TopLevelRecords} top-level record(s); {s.DialogueTopics} dialogue topic(s); " +
        $"{s.LinksWired} cross-ref link(s), {s.ExternalLinks} to external master(s); " +
        $"{s.ScriptsAttached} script(s) attached; " +
        $"{s.Placements} placement(s) in {s.NewInteriorCells} new + {s.VanillaInteriorCells} vanilla interior cell(s) + " +
        $"{s.Worldspaces} worldspace(s) [{s.NewExteriorCells} new exterior cell(s)])";

    // -------------------------------------------------------------------------------
    //  validate
    // -------------------------------------------------------------------------------
    private static int ValidateCmd(string specPath)
    {
        var spec = ReadSpec(specPath);
        var problems = Generator.Validate(spec);
        if (problems.Count == 0)
        {
            Console.WriteLine($"valid: {Path.GetFileName(specPath)} — no problems");
            return 0;
        }
        Console.Error.WriteLine($"INVALID: {Path.GetFileName(specPath)} — {problems.Count} problem(s):");
        foreach (var p in problems) Console.Error.WriteLine($"  - {p}");
        return 1;
    }

    // -------------------------------------------------------------------------------
    //  package — build the .esp, compile any script sources, and lay out an MO2/Vortex-
    //  ready mod folder: <outModDir>/<PluginName> + Scripts/*.pex + Scripts/Source/*.psc.
    // -------------------------------------------------------------------------------
    private static int PackageCmd(string specPath, string outModDir)
    {
        var spec = ReadSpec(specPath);
        var pluginName = string.IsNullOrEmpty(spec.PluginName) ? "Generated.esp" : spec.PluginName;
        Directory.CreateDirectory(outModDir);

        // 1) the plugin (Build also does the VMAD script attach by Scriptname)
        var espPath = Path.Combine(outModDir, pluginName);
        var key = ModKey.FromNameAndExtension(pluginName);
        var result = Generator.Build(spec, key);
        PluginIo.Write(result.Mod, espPath);
        foreach (var w in result.Warnings) Console.WriteLine(w);
        Console.WriteLine(BuildSummary(result.Stats, specPath, espPath));

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
            var cr = Papyrus.Compile(src, scriptsDir);
            if (!cr.Success) { Console.Error.WriteLine(cr.Message); Console.Error.WriteLine($"  ! compile failed: {sa.Source}"); continue; }
            Console.WriteLine(cr.Message);
            Directory.CreateDirectory(sourceDir);
            File.Copy(src, Path.Combine(sourceDir, Path.GetFileName(src)), overwrite: true);
            compiled++;
        }

        Console.WriteLine($"packaged -> {outModDir}  ({pluginName} + {compiled} compiled script(s) under Scripts/)");
        return 0;
    }

    // -------------------------------------------------------------------------------
    //  compile
    // -------------------------------------------------------------------------------
    private static int CompileCmd(string scriptPath, string outDir)
    {
        var r = Papyrus.Compile(scriptPath, outDir);
        if (r.Success) Console.WriteLine(r.Message);
        else Console.Error.WriteLine(r.Message);
        return r.ExitCode;
    }

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
        int requested = entries.Count(e => !string.IsNullOrEmpty(e.Target));
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
