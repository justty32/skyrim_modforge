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
                case "voicelines" when args.Length is 3 or 4: return VoicelinesCmd(args[1], args[2], args.Length == 4 ? args[3] : null);
                case "voicediag" when args.Length == 3: return VoiceDiagCmd(args[1], args[2]);
                case "extract-voices" when args.Length is 4 or 5: return ExtractVoicesCmd(args[1], args[2], args[3], args.Length == 5 ? args[4] : "Skyrim.esm");
                case "voice-annotate" when args.Length == 5: return VoiceAnnotateCmd(args[1], args[2], args[3], args[4]);
                case "compile" when args.Length == 3:  return CompileCmd(args[1], args[2]);
                case "texexport" when args.Length == 4: return TexExport(args[1], args[2], args[3]);
                case "package" when args.Length == 3:  return PackageCmd(args[1], args[2], null);
                case "package" when args.Length == 5 && args[3] == "--assets": return PackageCmd(args[1], args[2], args[4]);
                case "validate" when args.Length == 2: return ValidateCmd(args[1]);
                case "extract" when args.Length == 3:  ExtractCmd(args[1], args[2]); return 0;
                case "apply" when args.Length == 4:    ApplyCmd(args[1], args[2], args[3]); return 0;
                case "applyloc" when args.Length == 4: return ApplyLocalizedCmd(args[1], args[2], args[3]);
                case "dump" when args.Length == 2:     return Dump(args[1]);
                case "gamedata" when args.Length == 3: return GameData(args[1], args[2]);
                case "gamedata" when args.Length == 5 && args[3] == "--strings": return GameData(args[1], args[2], args[4]);
                case "find" when args.Length is 3 or 4: return Find(args[1], args[2], args.Length == 4 ? args[3] : null);
                case "cellblk" when args.Length is 2 or 3: return CellBlk(args[1], args.Length == 3 ? args[2] : null);
                case "cellrefs" when args.Length == 3: return CellRefs(args[1], args[2]);
                case "mgefdiag" when args.Length == 3: return MgefDiag(args[1], args[2]);
                case "enchdiag" when args.Length == 3: return EnchDiag(args[1], args[2]);
                case "lightdiag" when args.Length is 2 or 3: return LightDiag(args[1], args.Length == 3 ? args[2] : null);
                case "lgtmdiag" when args.Length is 2 or 3: return LgtmDiag(args[1], args.Length == 3 ? args[2] : null);
                case "imgsdiag" when args.Length is 2 or 3: return ImgsDiag(args[1], args.Length == 3 ? args[2] : null);
                case "packagediag" when args.Length == 3: return PackageDiag(args[1], args[2]);
                case "pkgsbytemplate" when args.Length == 3: return PkgsByTemplate(args[1], args[2]);
                case "npcdiag" when args.Length == 3: return NpcDiag(args[1], args[2]);
                case "cstydiag" when args.Length == 3: return CstyDiag(args[1], args[2]);
                case "perkdiag" when args.Length == 3: return PerkDiag(args[1], args[2]);
                case "questdiag" when args.Length == 3: return QuestDiag(args[1], args[2]);
                case "txstdiag" when args.Length is 2 or 3: return TxstDiag(args[1], args.Length == 3 ? args[2] : null);
                case "cobjdiag" when args.Length == 3: return CobjDiag(args[1], args[2]);
                case "weatherdiag" when args.Length == 3: return WeatherDiag(args[1], args[2]);
                case "climatediag" when args.Length == 3: return ClimateDiag(args[1], args[2]);
                case "worlddiag" when args.Length == 3: return WorldDiag(args[1], args[2]);
                case "landdiag" when args.Length is 2 or 3 or 4: return LandDiag(args[1], args.Length >= 3 ? args[2] : null, args.Length == 4 ? int.Parse(args[3]) : 1);
                case "regndiag" when args.Length == 3: return RegnDiag(args[1], args[2]);
                case "eczndiag" when args.Length == 3: return EcznDiag(args[1], args[2]);
                case "refpos" when args.Length == 3: return RefPos(args[1], args[2]);
                case "bookdiag" when args.Length == 3: return BookDiag(args[1], args[2]);
                case "booktext" when args.Length == 3: return BookText(args[1], args[2]);
                case "infodiag" when args.Length == 6 && args[4] == "--strings": return InfoDiag(args[1], args[2], args[3], args[5]);
                case "infodiag" when args.Length == 5 && args[3] == "--strings": return InfoDiag(args[1], args[2], null, args[4]);
                case "infodiag" when args.Length is 3 or 4: return InfoDiag(args[1], args[2], args.Length == 4 ? args[3] : null);
                case "factdiag" when args.Length == 3: return FactDiag(args[1], args[2]);
                case "reladiag" when args.Length == 3: return RelaDiag(args[1], args[2]);
                case "shoutdiag" when args.Length == 3: return ShoutDiag(args[1], args[2]);
                case "scenediag" when args.Length == 3: return SceneDiag(args[1], args[2]);
                case "scnscan" when args.Length is 2 or 3: return ScnScan(args[1], args.Length == 3 ? args[2] : null);
                case "smtree" when args.Length == 2: return SmTree(args[1]);
                case "identitydiag" when args.Length == 2: return IdentityDiag(args[1]);
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
        "  voicelines <spec.json> <built.esp> [--dry-run|--plan] generate .fuz/.wav from dialogue records, or print expected voice paths\n" +
        "  voicediag <spec.json> <built.esp>            offline speaker/template/path check for every dialogue INFO line\n" +
        "  extract-voices <bsaPath> <voiceType> <outDir> [plugin]  extract + convert a voiceType's clips to WAV (plugin default Skyrim.esm; e.g. SofiaFollower.esp for a follower's BSA)\n" +
        "  voice-annotate <esm> <voiceType> <bsaPath> <outDir>  extract clips + write emotion-annotation manifest (JSON)\n" +
        "  compile <script.psc> <outDir>\n" +
        "  package <spec.json> <outModDir> [--assets <dir>]   esp + scripts + bundled Meshes/Textures/Sounds\n" +
        "  validate <spec.json>\n" +
        "  dump    <in.esp>\n" +
        "  gamedata <plugin> <outDir>                   bulk-extract books/dialogue/quests/npcs/items/locations/magic to a folder (for agent reference)\n" +

        "  find    <in.esp> <query> [type]              search editorId/name -> Skyrim.esm:0xFORMID\n" +
        "  cellblk <in.esp> [0xFORMID]                  show interior cell block/sub-block (FormID grouping)\n" +
        "  cellrefs <in.esp> <0xFORMID>                 dump one interior cell's placed refs (base+pos+rot[rad]+scale) as CSV — reverse a vanilla cell into placements[]\n" +
        "  mgefdiag <in.esp> <0xFORMID>                 print a MagicEffect's fields (compare gen vs vanilla)\n" +
        "  enchdiag <in.esp> <0xFORMID>                 print an Enchantment (ENCH/ObjectEffect)'s type/cost/effects\n" +
        "  lightdiag <in.esp> [0xFORMID]                a Light's radius/color/flags (no id: list room-fill lights)\n" +
        "  lgtmdiag <in.esp> [0xFORMID]                a LightingTemplate's ambient/directional/fog/DALC (no id: list all)\n" +
        "  imgsdiag <in.esp> [0xFORMID]                an ImageSpace's HDR/cinematic/tint (no id: list all)\n" +
        "  packagediag <in.esp> <0xFORMID>              print a Package's template/flags/schedule/data inputs\n" +
        "  npcdiag <in.esp> <0xFORMID>                  print an Npc's race/class/voice/factions/packages/flags (for cross-cell diff vs vanilla)\n" +
        "  cstydiag <in.esp> <0xFORMID>                 print a CombatStyle's offensive/defensive mults + equipment preferences + flags\n" +
        "  perkdiag <in.esp> <0xFORMID|entrypoints>     print a Perk's flags/effects/conditions, or list every EntryType name\n" +
        "  questdiag <in.esp> <0xFORMID>                print a Quest's stages (log entries + flags) + objectives (display text + targets)\n" +
        "  cobjdiag <in.esp> <0xFORMID>                 print a recipe's (COBJ) createdObject/count/workbench/components/conditions\n" +
        "  txstdiag <in.esp> [0xFORMID]                 a TextureSet's 8 texture-map slots+flags (no id: list all TXST)\n" +
        "  landdiag <in.esp> [wsEditorId] [maxCells]    dump LAND texture layers (BTXT/ATXT quad+layer+tex, VTXT pts) — byte-verify vs vanilla\n" +
        "  texexport <dataDir> <outDir> <master:0xLTEX>[,…]  LTEX→diffuse .dds from texture BSAs → PNG (Godot WYSIWYG terrain)\n" +
        "  weatherdiag <in.esp> <0xFORMID>              print a Weather's flags/colours/clouds/fog (compare gen vs vanilla)\n" +
        "  climatediag <in.esp> <0xFORMID>              print a Climate's weather list/sun-times/moons/textures\n" +
        "  worlddiag <in.esp> <0xFORMID>                print a Worldspace's climate/water/parent + map bounds + land/water defaults\n" +
        "  regndiag <in.esp> <0xFORMID>                 print a Region's worldspace/area/mapColor + weather table (priority + weather refs + chances)\n" +
        "  eczndiag <in.esp> <0xFORMID>                 print an EncounterZone's level range/rank/flags/owner/location\n" +
        "  bookdiag <in.esp> <0xFORMID>                 print a Book's Teaches (spell/skill/nothing) + flags + model (e.g. a vanilla spell tome)\n" +
        "  booktext <esm> <0xFORMID>                    print a Book's localized Name + full BookText (lore prose; extracts English STRINGS from the master's BSA)\n" +
        "  refpos <in.esp> <0xFORMID>                   print a placed ref's (REFR/ACHR) position+rotation+base (anchor new placements on known navmesh)\n" +
        "  infodiag <in.esp> <0xFORMID> [substr]        dump dialogue INFO responses + FULL CTDA conditions for a topic, or every topic a quest owns (substr filters EditorID)\n" +
        "  factdiag <in.esp> <0xFORMID>                 print a Faction's flags/ranks/inter-faction relations (the paid-hireling gate is faction membership)\n" +
        "  reladiag <in.esp> <0xFORMID>                 print a RELA, or every RELA referencing the FormID as parent/child (player has zero static RELA)\n" +
        "  shoutdiag <in.esp> <0xFORMID>                print a Shout's 3 WordsOfPower rows (Word/Spell/RecoveryTime)\n" +
        "  scenediag <in.esp> <0xFORMID>                print a SCEN's host quest + actors (alias indices) + phases + actions (which alias speaks which topic)\n" +
        "  scnscan <in.esp> [max]                       list scenes with non-Dialog actions (Package/Timer) — find vanilla movement/furniture/timer scenes to model §1b performances on\n" +
        "  smtree <Skyrim.esm>                          list Story Manager event roots (discover an event root FormID)\n" +
        "  identitydiag <in.esp>                        dump the identity system wiring (controller registry, default grants, acquire books, globals)\n" +
        "  extract <in.esp> <strings.json>\n" +
        "  applyloc <in.esp> <strings.json> <outDir>   (Localized UTF-8 _chinese.STRINGS)\n" +
        "  apply   <in.esp> <strings.json> <out.esp>");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        // $env values arrive as JSON strings; allow them in numeric spec fields.
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
    };

    // Single chokepoint: read a spec file and resolve $ref/$env before any deserialize / field check.
    private static string ResolveSpecJson(string path) => SpecRefs.ResolveFile(path);

    private static ModSpec ReadSpec(string path) =>
        JsonSerializer.Deserialize<ModSpec>(ResolveSpecJson(path), ReadOpts)
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

    // package — see Package.cs (PackageCmd; build .esp + compile scripts + lay out MO2/Vortex folder).
    // build/validate/compile — see Program.Build.cs
    // extract/apply/applyloc — see Program.Translate.cs
}
