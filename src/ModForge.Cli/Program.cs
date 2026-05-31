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
                case "package" when args.Length == 3:  return PackageCmd(args[1], args[2], null);
                case "package" when args.Length == 5 && args[3] == "--assets": return PackageCmd(args[1], args[2], args[4]);
                case "validate" when args.Length == 2: return ValidateCmd(args[1]);
                case "extract" when args.Length == 3:  ExtractCmd(args[1], args[2]); return 0;
                case "apply" when args.Length == 4:    ApplyCmd(args[1], args[2], args[3]); return 0;
                case "applyloc" when args.Length == 4: return ApplyLocalizedCmd(args[1], args[2], args[3]);
                case "dump" when args.Length == 2:     return Dump(args[1]);
                case "find" when args.Length is 3 or 4: return Find(args[1], args[2], args.Length == 4 ? args[3] : null);
                case "cellblk" when args.Length is 2 or 3: return CellBlk(args[1], args.Length == 3 ? args[2] : null);
                case "mgefdiag" when args.Length == 3: return MgefDiag(args[1], args[2]);
                case "enchdiag" when args.Length == 3: return EnchDiag(args[1], args[2]);
                case "lightdiag" when args.Length is 2 or 3: return LightDiag(args[1], args.Length == 3 ? args[2] : null);
                case "packagediag" when args.Length == 3: return PackageDiag(args[1], args[2]);
                case "pkgsbytemplate" when args.Length == 3: return PkgsByTemplate(args[1], args[2]);
                case "npcdiag" when args.Length == 3: return NpcDiag(args[1], args[2]);
                case "cstydiag" when args.Length == 3: return CstyDiag(args[1], args[2]);
                case "perkdiag" when args.Length == 3: return PerkDiag(args[1], args[2]);
                case "txstdiag" when args.Length is 2 or 3: return TxstDiag(args[1], args.Length == 3 ? args[2] : null);
                case "cobjdiag" when args.Length == 3: return CobjDiag(args[1], args[2]);
                case "weatherdiag" when args.Length == 3: return WeatherDiag(args[1], args[2]);
                case "climatediag" when args.Length == 3: return ClimateDiag(args[1], args[2]);
                case "worlddiag" when args.Length == 3: return WorldDiag(args[1], args[2]);
                case "regndiag" when args.Length == 3: return RegnDiag(args[1], args[2]);
                case "eczndiag" when args.Length == 3: return EcznDiag(args[1], args[2]);
                case "refpos" when args.Length == 3: return RefPos(args[1], args[2]);
                case "bookdiag" when args.Length == 3: return BookDiag(args[1], args[2]);
                case "infodiag" when args.Length is 3 or 4: return InfoDiag(args[1], args[2], args.Length == 4 ? args[3] : null);
                case "factdiag" when args.Length == 3: return FactDiag(args[1], args[2]);
                case "reladiag" when args.Length == 3: return RelaDiag(args[1], args[2]);
                case "shoutdiag" when args.Length == 3: return ShoutDiag(args[1], args[2]);
                case "scenediag" when args.Length == 3: return SceneDiag(args[1], args[2]);
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
        "  package <spec.json> <outModDir> [--assets <dir>]   esp + scripts + bundled Meshes/Textures/Sounds\n" +
        "  validate <spec.json>\n" +
        "  dump    <in.esp>\n" +
        "  find    <in.esp> <query> [type]              search editorId/name -> Skyrim.esm:0xFORMID\n" +
        "  cellblk <in.esp> [0xFORMID]                  show interior cell block/sub-block (FormID grouping)\n" +
        "  mgefdiag <in.esp> <0xFORMID>                 print a MagicEffect's fields (compare gen vs vanilla)\n" +
        "  enchdiag <in.esp> <0xFORMID>                 print an Enchantment (ENCH/ObjectEffect)'s type/cost/effects\n" +
        "  lightdiag <in.esp> [0xFORMID]                a Light's radius/color/flags (no id: list room-fill lights)\n" +
        "  packagediag <in.esp> <0xFORMID>              print a Package's template/flags/schedule/data inputs\n" +
        "  npcdiag <in.esp> <0xFORMID>                  print an Npc's race/class/voice/factions/packages/flags (for cross-cell diff vs vanilla)\n" +
        "  cstydiag <in.esp> <0xFORMID>                 print a CombatStyle's offensive/defensive mults + equipment preferences + flags\n" +
        "  perkdiag <in.esp> <0xFORMID|entrypoints>     print a Perk's flags/effects/conditions, or list every EntryType name\n" +
        "  cobjdiag <in.esp> <0xFORMID>                 print a recipe's (COBJ) createdObject/count/workbench/components/conditions\n" +
        "  txstdiag <in.esp> [0xFORMID]                 a TextureSet's 8 texture-map slots+flags (no id: list all TXST)\n" +
        "  weatherdiag <in.esp> <0xFORMID>              print a Weather's flags/colours/clouds/fog (compare gen vs vanilla)\n" +
        "  climatediag <in.esp> <0xFORMID>              print a Climate's weather list/sun-times/moons/textures\n" +
        "  worlddiag <in.esp> <0xFORMID>                print a Worldspace's climate/water/parent + map bounds + land/water defaults\n" +
        "  regndiag <in.esp> <0xFORMID>                 print a Region's worldspace/area/mapColor + weather table (priority + weather refs + chances)\n" +
        "  eczndiag <in.esp> <0xFORMID>                 print an EncounterZone's level range/rank/flags/owner/location\n" +
        "  bookdiag <in.esp> <0xFORMID>                 print a Book's Teaches (spell/skill/nothing) + flags + model (e.g. a vanilla spell tome)\n" +
        "  refpos <in.esp> <0xFORMID>                   print a placed ref's (REFR/ACHR) position+rotation+base (anchor new placements on known navmesh)\n" +
        "  infodiag <in.esp> <0xFORMID> [substr]        dump dialogue INFO responses + FULL CTDA conditions for a topic, or every topic a quest owns (substr filters EditorID)\n" +
        "  factdiag <in.esp> <0xFORMID>                 print a Faction's flags/ranks/inter-faction relations (the paid-hireling gate is faction membership)\n" +
        "  reladiag <in.esp> <0xFORMID>                 print a RELA, or every RELA referencing the FormID as parent/child (player has zero static RELA)\n" +
        "  shoutdiag <in.esp> <0xFORMID>                print a Shout's 3 WordsOfPower rows (Word/Spell/RecoveryTime)\n" +
        "  scenediag <in.esp> <0xFORMID>                print a SCEN's host quest + actors (alias indices) + phases + actions (which alias speaks which topic)\n" +
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
        PluginIo.Write(mod, outPath);   // NoCheck + ESL-limit guard, same as every other write path
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
        WriteSeq(outPath, Path.GetDirectoryName(Path.GetFullPath(outPath)) ?? ".");
    }

    // A Start-Game-Enabled quest hosting dialogue needs a Data/Seq/<plugin>.seq entry, or its
    // dialogue won't surface on a pre-existing save until a save+reload (new games are unaffected).
    // See ModForge.SeqFile. Writes next to the plugin so the Seq/ folder lands in the same Data root.
    private static void WriteSeq(string espPath, string dataDir)
    {
        var quests = SeqFile.Write(espPath, dataDir);
        if (quests.Count > 0)
            Console.WriteLine($"wrote Seq/{Path.GetFileNameWithoutExtension(espPath)}.seq ({quests.Count} start-game-enabled quest(s) — needed for dialogue on existing saves)");
    }

    private static string BuildSummary(BuildStats s, string specPath, string outPath) =>
        $"built {outPath} from {Path.GetFileName(specPath)} " +
        $"(ESL={s.Esl}, {s.TopLevelRecords} top-level record(s); {s.Perks} perk(s); {s.DialogueTopics} dialogue topic(s); " +
        $"{s.Scenes} scene(s) in {s.ScenePhases} phase(s); " +
        $"{s.LinksWired} cross-ref link(s), {s.ExternalLinks} to external master(s); " +
        $"{s.ScriptsAttached} script(s) attached; " +
        $"{s.Placements} placement(s) in {s.NewInteriorCells} new + {s.VanillaInteriorCells} vanilla interior cell(s) + " +
        $"{s.Worldspaces} worldspace(s) [{s.NewExteriorCells} new exterior cell(s)]; " +
        $"{s.Regions} region(s); {s.EncounterZones} encounter zone(s))";

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
    private static int PackageCmd(string specPath, string outModDir, string? assetsOverride)
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
        WriteSeq(espPath, outModDir);   // Data/Seq/<plugin>.seq alongside the plugin in the mod folder

        // 2) compile each referenced script source -> Scripts/*.pex; copy .psc -> Scripts/Source/
        var scriptsDir = Path.Combine(outModDir, "Scripts");
        var sourceDir = Path.Combine(scriptsDir, "Source");
        var specDir = Path.GetDirectoryName(Path.GetFullPath(specPath)) ?? ".";
        int compiled = 0;
        var compiledSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool CompileSource(string? source, string label)
        {
            if (string.IsNullOrEmpty(source)) return false;
            var src = Path.IsPathRooted(source) ? source : Path.Combine(specDir, source);
            if (!compiledSources.Add(Path.GetFullPath(src))) return false;   // same .psc referenced twice
            if (!File.Exists(src)) { Console.Error.WriteLine($"  ! script source not found: {src}"); return false; }
            var cr = Papyrus.Compile(src, scriptsDir);
            if (!cr.Success) { Console.Error.WriteLine(cr.Message); Console.Error.WriteLine($"  ! compile failed: {label}"); return false; }
            Console.WriteLine(cr.Message);
            Directory.CreateDirectory(sourceDir);
            File.Copy(src, Path.Combine(sourceDir, Path.GetFileName(src)), overwrite: true);
            compiled++;
            return true;
        }
        foreach (var sa in spec.Scripts) CompileSource(sa.Source, sa.Source);
        // Dialogue result-script fragments (the INFO OnEnd TIF) are compiled the same way.
        foreach (var d in spec.Dialogue) CompileSource(d.ResultScriptSource, d.ResultScriptSource);

        // 3) external-resource bundling — copy the spec's (or --assets) Meshes/Textures/Sounds/…
        //    sub-trees next to the .esp so the packaged mod is self-contained / MO2-ready.
        var assetsSrc = !string.IsNullOrWhiteSpace(assetsOverride) ? assetsOverride
                      : !string.IsNullOrWhiteSpace(spec.Assets)
                            ? (Path.IsPathRooted(spec.Assets) ? spec.Assets : Path.Combine(specDir, spec.Assets))
                            : null;
        if (!string.IsNullOrWhiteSpace(assetsSrc))
        {
            var br = Assets.Bundle(assetsSrc, outModDir);
            foreach (var w in br.Warnings) Console.Error.WriteLine(w);
            if (br.FilesCopied > 0)
                Console.WriteLine($"bundled {br.FilesCopied} asset file(s) ({br.BytesCopied / 1024.0:0.#} KiB) " +
                    $"from {assetsSrc} -> [{string.Join(", ", br.CopiedFolders)}]");
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
