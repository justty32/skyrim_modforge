// =====================================================================================
//  Dispatch + help text for the generation/packaging/translation commands.
//
//  Both halves live here on purpose: a new command adds ONE case below and ONE line to
//  CoreUsage, and Program.cs never changes. Returning null means "not one of my shapes"
//  and lets Main fall through to the diagnostics table and then to Usage().
// =====================================================================================

internal static partial class Program
{
    private static int? DispatchCore(string[] args) => args[0] switch
    {
        "gen" when args.Length == 2 => GenCmd(args[1]),
        "build" when args.Length == 3 => BuildCmd(args[1], args[2]),
        // --sync-requires: write the masters the build actually links back into the spec's
        // requires[] (capture pulls them in by the dozen; the spec diff is the review surface).
        "build" when args.Length == 4 && args[3] == "--sync-requires" => BuildCmd(args[1], args[2], true),
        "voicelines" when args.Length is 3 or 4 => VoicelinesCmd(args[1], args[2], args.Length == 4 ? args[3] : null),
        "voicediag" when args.Length == 3 => VoiceDiagCmd(args[1], args[2]),
        "extract-voices" when args.Length is 4 or 5 => ExtractVoicesCmd(args[1], args[2], args[3], args.Length == 5 ? args[4] : "Skyrim.esm"),
        "voice-annotate" when args.Length == 5 => VoiceAnnotateCmd(args[1], args[2], args[3], args[4]),
        "compile" when args.Length == 3 => CompileCmd(args[1], args[2]),
        "texexport" when args.Length == 4 => TexExport(args[1], args[2], args[3]),
        "nifexport" when args.Length == 4 => NifExport(args[1], args[2], args[3]),
        "texpath" when args.Length == 4 => TexPath(args[1], args[2], args[3]),
        "package" when args.Length == 3 => PackageCmd(args[1], args[2], null),
        "package" when args.Length == 5 && args[3] == "--assets" => PackageCmd(args[1], args[2], args[4]),
        "validate" when args.Length == 2 => ValidateCmd(args[1]),
        "extract" when args.Length == 3 => Run(() => ExtractCmd(args[1], args[2])),
        "apply" when args.Length == 4 => Run(() => ApplyCmd(args[1], args[2], args[3])),
        "applyloc" when args.Length == 4 => ApplyLocalizedCmd(args[1], args[2], args[3]),
        "catalog" when args.Length >= 2 => CatalogCmd(args[1..]),
        "gamedata" when args.Length == 3 => GameData(args[1], args[2]),
        "gamedata" when args.Length == 5 && args[3] == "--strings" => GameData(args[1], args[2], args[4]),
        "questnodes" when args.Length == 3 => QuestNodesCmd(args[1], args[2]),
        "questnodes" when args.Length == 5 && args[3] == "--strings" => QuestNodesCmd(args[1], args[2], args[4]),
        _ => null,
    };

    // A command whose implementation returns void succeeds by not throwing (the catch in
    // Main turns an exception into exit 2) — same contract as the old `Cmd(...); return 0;`.
    private static int Run(Action cmd) { cmd(); return 0; }

    private const string CoreUsage =
        "  gen     <out.esp>\n" +
        "  build   <spec.json> <out.esp> [--sync-requires]   (--sync-requires: write the masters the build links back into the spec's requires[])\n" +
        "  voicelines <spec.json> <built.esp> [--dry-run|--plan] generate .fuz/.wav from dialogue records, or print expected voice paths\n" +
        "  voicediag <spec.json> <built.esp>            offline speaker/template/path check for every dialogue INFO line\n" +
        "  extract-voices <bsaPath> <voiceType> <outDir> [plugin]  extract + convert a voiceType's clips to WAV (plugin default Skyrim.esm; e.g. SofiaFollower.esp for a follower's BSA)\n" +
        "  voice-annotate <esm> <voiceType> <bsaPath> <outDir>  extract clips + write emotion-annotation manifest (JSON)\n" +
        "  compile <script.psc> <outDir>\n" +
        "  package <spec.json> <outModDir> [--assets <dir>]   esp + scripts + bundled Meshes/Textures/Sounds\n" +
        "  validate <spec.json>\n" +
        "  catalog build <out.db> <plugin> [plugin...]  index generic records; plugin order is low-to-high load order\n" +
        "  catalog query <db> <query> [--type <type>] [--plugin <plugin>] [--limit <1-1000>] [--json]  search name/editorId\n" +
        "  catalog get <db> <Plugin.esp:0xFORMID> [--plugin <source>] [--json]  exact FormKey lookup\n" +
        "  catalog sources <db> [--json]                list indexed plugin provenance\n" +
        "  catalog export-json <db> <out.json> [--placeable]  export winners; --placeable keeps Browser base types only\n" +
        "  gamedata <plugin> <outDir> [--strings <dir>] bulk-extract books/dialogue/quests/npcs/items/locations/magic to a folder (for agent reference)\n" +
        "  questnodes <plugin> <outDir> [--strings <dir>]  extract non-empty QUST stage logs as schema-valid quest-node JSON files\n" +
        "  texexport <dataDir> <outDir> <master:0xLTEX>[,…]  LTEX→diffuse .dds from texture BSAs → PNG (Godot WYSIWYG terrain)\n" +
        "  nifexport <dataDir> <outDir> <master:0xFORMID>[,…]  placeable base→model .nif from mesh BSAs (Godot WYSIWYG objects; convert via nif2gltf)\n" +
        "  texpath <dataDir> <outDir> <texPath>[,…]     extract arbitrary texture path(s) from BSAs → <basename>.png (textures a model's glTF)\n" +
        "  extract <in.esp> <strings.json>\n" +
        "  applyloc <in.esp> <strings.json> <outDir>   (Localized UTF-8 _chinese.STRINGS)\n" +
        "  apply   <in.esp> <strings.json> <out.esp>\n";

    // -------------------------------------------------------------------------------
    //  gen
    // -------------------------------------------------------------------------------
    private static int GenCmd(string outPath)
    {
        var key = ModKey.FromNameAndExtension(Path.GetFileName(outPath));
        var mod = Demo.CreateDemoPlugin(key);
        PluginIo.Write(mod, outPath);   // NoCheck + ESL-limit guard, same as every other write path
        Console.WriteLine($"wrote {outPath}  (ESL={mod.IsSmallMaster}, {mod.EnumerateMajorRecords().Count()} records)");
        return 0;
    }

    // package — see Package.cs (PackageCmd; build .esp + compile scripts + lay out MO2/Vortex folder).
    // build/validate/compile — see Program.Build.cs
    // extract/apply/applyloc — see Program.Translate.cs
}
