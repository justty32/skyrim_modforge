using System.Diagnostics;
using System.Text.Json;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;

internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  build
    // -------------------------------------------------------------------------------
    private static void BuildCmd(string specPath, string outPath)
    {
        var spec = ReadSpec(specPath);
        var key = ModKey.FromNameAndExtension(Path.GetFileName(outPath));
        var specDir = Path.GetDirectoryName(Path.GetFullPath(specPath)) ?? ".";
        var result = Generator.Build(spec, key, new BuildOptions { SpecDir = specDir });
        PluginIo.Write(result.Mod, outPath);
        foreach (var w in result.Warnings) Console.WriteLine(w);
        Console.WriteLine(BuildSummary(result.Stats, specPath, outPath));
        WriteSeq(outPath, Path.GetDirectoryName(Path.GetFullPath(outPath)) ?? ".");
        if (spec.Weathers.Count > 0)
        {
            string prefix = spec.Esl ? "FE<slot>" : "<XX>";
            string detail = spec.Esl
                ? "(XX=ESL slot index — find with 'help' or check MO2 load order light-plugin list)"
                : "(XX=load order index in hex — check MO2 right panel, decimal→hex)";
            for (int i = 0; i < spec.Weathers.Count; i++)
                Console.WriteLine($"  Weather test: sw {prefix}{0x800 + i:X06}  → {spec.Weathers[i].EditorId}  {(i == 0 ? detail : "")}");
        }
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

    // voicelines / extract-voices — see Program.Build.Voice.cs (speaker resolution lives in
    // ModForge.Core: Generator.ResolveVoiceSpeakers / SelectVoiceTargets / PackVoiceAudio).

    private static string BuildSummary(BuildStats s, string specPath, string outPath) =>
        $"built {outPath} from {Path.GetFileName(specPath)} " +
        $"(ESL={s.Esl}, {s.TopLevelRecords} top-level record(s); {s.Perks} perk(s); {s.DialogueTopics} dialogue topic(s); " +
        $"{s.Scenes} scene(s) in {s.ScenePhases} phase(s); " +
        $"{s.LinksWired} cross-ref link(s), {s.ExternalLinks} to external master(s); " +
        $"{s.ScriptsAttached} script(s) attached; " +
        $"{s.Placements} placement(s) in {s.NewInteriorCells} new + {s.VanillaInteriorCells} vanilla interior cell(s) + " +
        $"{s.Worldspaces} worldspace(s) [{s.NewExteriorCells} new exterior cell(s), {s.NavmeshCells} navmeshed]; " +
        $"{s.Regions} region(s); {s.EncounterZones} encounter zone(s); {s.WordWalls} word wall(s))";

    // -------------------------------------------------------------------------------
    //  validate
    // -------------------------------------------------------------------------------
    private static int ValidateCmd(string specPath)
    {
        var json = ResolveSpecJson(specPath);
        var unknowns = CheckUnknownFields(json, typeof(ModSpec));

        var spec = JsonSerializer.Deserialize<ModSpec>(json, ReadOpts)
            ?? throw new InvalidOperationException("spec deserialized to null");
        var semantic = Generator.Validate(spec);

        var all = unknowns.Concat(semantic).ToList();
        if (all.Count == 0)
        {
            Console.WriteLine($"valid: {Path.GetFileName(specPath)} — no problems");
            return 0;
        }
        Console.Error.WriteLine($"INVALID: {Path.GetFileName(specPath)} — {all.Count} problem(s):");
        foreach (var p in all) Console.Error.WriteLine($"  - {p}");
        return 1;
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
}
