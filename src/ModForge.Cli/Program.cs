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

internal static partial class Program
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

    private static ISkyrimMod Load(string path) =>
        SkyrimMod.CreateFromBinary(new ModPath(path), SkyrimRelease.SkyrimSE);

    // output filename may differ from the mod's ModKey -> skip the alignment check.
    private static void Write(ISkyrimMod mod, string outPath) =>
        mod.WriteToBinary(outPath, new BinaryWriteParameters { ModKey = ModKeyOption.NoCheck });
}
