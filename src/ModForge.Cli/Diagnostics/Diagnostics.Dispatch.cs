// =====================================================================================
//  Dispatch + help text for the read-only inspection commands (dump / find / *diag).
//
//  Both halves live here on purpose: a new *diag adds ONE case below and ONE line to
//  DiagnosticsUsage, and Program.cs never changes. Returning null means "not one of my
//  shapes" and lets Main fall through to Usage().
// =====================================================================================

internal static partial class Program
{
    private static int? DispatchDiagnostics(string[] args) => args[0] switch
    {
        "dump" when args.Length == 2 => Dump(args[1]),
        "find" when args.Length is 3 or 4 => Find(args[1], args[2], args.Length == 4 ? args[3] : null),
        "cellblk" when args.Length is 2 or 3 => CellBlk(args[1], args.Length == 3 ? args[2] : null),
        "cellrefs" when args.Length == 3 => CellRefs(args[1], args[2]),
        "mgefdiag" when args.Length == 3 => MgefDiag(args[1], args[2]),
        "enchdiag" when args.Length == 3 => EnchDiag(args[1], args[2]),
        "lightdiag" when args.Length is 2 or 3 => LightDiag(args[1], args.Length == 3 ? args[2] : null),
        "lgtmdiag" when args.Length is 2 or 3 => LgtmDiag(args[1], args.Length == 3 ? args[2] : null),
        "imgsdiag" when args.Length is 2 or 3 => ImgsDiag(args[1], args.Length == 3 ? args[2] : null),
        "packagediag" when args.Length == 3 => PackageDiag(args[1], args[2]),
        "pkgsbytemplate" when args.Length == 3 => PkgsByTemplate(args[1], args[2]),
        "npcdiag" when args.Length == 3 => NpcDiag(args[1], args[2]),
        "cstydiag" when args.Length == 3 => CstyDiag(args[1], args[2]),
        "perkdiag" when args.Length == 3 => PerkDiag(args[1], args[2]),
        "questdiag" when args.Length == 3 => QuestDiag(args[1], args[2]),
        "txstdiag" when args.Length is 2 or 3 => TxstDiag(args[1], args.Length == 3 ? args[2] : null),
        "cobjdiag" when args.Length == 3 => CobjDiag(args[1], args[2]),
        "weatherdiag" when args.Length == 3 => WeatherDiag(args[1], args[2]),
        "climatediag" when args.Length == 3 => ClimateDiag(args[1], args[2]),
        "worlddiag" when args.Length == 3 => WorldDiag(args[1], args[2]),
        "landdiag" when args.Length is 2 or 3 or 4 => LandDiag(args[1], args.Length >= 3 ? args[2] : null, args.Length == 4 ? int.Parse(args[3]) : 1),
        "navdiag" when args.Length == 2 => NavDiag(args[1], null, null, null),
        "navdiag" when args.Length == 3 => NavDiag(args[1], args[2], null, null),
        "navdiag" when args.Length == 5 => NavDiag(args[1], args[2], int.Parse(args[3]), int.Parse(args[4])),
        "regndiag" when args.Length == 3 => RegnDiag(args[1], args[2]),
        "eczndiag" when args.Length == 3 => EcznDiag(args[1], args[2]),
        "refpos" when args.Length == 3 => RefPos(args[1], args[2]),
        "bookdiag" when args.Length == 3 => BookDiag(args[1], args[2]),
        "booktext" when args.Length == 3 => BookText(args[1], args[2]),
        "infodiag" when args.Length == 6 && args[4] == "--strings" => InfoDiag(args[1], args[2], args[3], args[5]),
        "infodiag" when args.Length == 5 && args[3] == "--strings" => InfoDiag(args[1], args[2], null, args[4]),
        "infodiag" when args.Length is 3 or 4 => InfoDiag(args[1], args[2], args.Length == 4 ? args[3] : null),
        "factdiag" when args.Length == 3 => FactDiag(args[1], args[2]),
        "reladiag" when args.Length == 3 => RelaDiag(args[1], args[2]),
        "shoutdiag" when args.Length == 3 => ShoutDiag(args[1], args[2]),
        "scenediag" when args.Length == 3 => SceneDiag(args[1], args[2]),
        "scnscan" when args.Length is 2 or 3 => ScnScan(args[1], args.Length == 3 ? args[2] : null),
        "smtree" when args.Length == 2 => SmTree(args[1]),
        "smsub" when args.Length == 3 => SmSub(args[1], args[2]),
        "smcheck" when args.Length == 2 => SmCheck(args[1]),
        "identitydiag" when args.Length == 2 => IdentityDiag(args[1]),
        _ => null,
    };

    private const string DiagnosticsUsage =
        "  dump    <in.esp>\n" +
        "  smcheck <plugin>                              check local Story Manager graph and quest aliases\n" +

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
        "  navdiag <in.esp> [0xCELL | 0xWRLD <x> <y>]   dump NAVM geometry (verts/tris/cross-mesh edges/door tris/grid); with no id, byte-diffs every overridden NAVM's NVNM against its master (the navmesh GO/NO-GO gate)\n" +
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
        "  identitydiag <in.esp>                        dump the identity system wiring (controller registry, default grants, acquire books, globals)";
}
