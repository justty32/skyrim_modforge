using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;

internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  build
    // -------------------------------------------------------------------------------
    private static int BuildCmd(string specPath, string outPath, bool syncRequires = false)
    {
        var spec = ReadSpec(specPath);
        var key = ModKey.FromNameAndExtension(Path.GetFileName(outPath));
        var specDir = Path.GetDirectoryName(Path.GetFullPath(specPath)) ?? ".";
        var result = Generator.Build(spec, key, new BuildOptions { SpecDir = specDir });

        // The requires[] contract is checked BEFORE anything is written: a plugin whose master list the
        // author never declared is not the plugin they asked for (Generator.Requires.cs).
        if (syncRequires) SyncRequiresFile(specPath, spec, result);
        else if (!RequiresOk(result, specPath, outPath)) return 1;

        PluginIo.Write(result.Mod, outPath);
        foreach (var w in result.Warnings) Console.WriteLine(w);
        // Advisory INFO lines — the build is fine, but the spec says something whose in-game meaning is
        // easy to misread and invisible in the output (a references[] label in a package LOCATION slot
        // = an AREA anchor, not "use THAT object"). Not warnings: they must not make a clean build look dirty.
        foreach (var n in result.Notes) Console.WriteLine(n);
        Console.WriteLine(BuildSummary(result.Stats, specPath, outPath));
        if (spec.Annotations.Count > 0)
            // Advisory marker anchors from the in-game editor — deliberately NOT built (Spec.Annotations.cs).
            Console.WriteLine($"{spec.Annotations.Count} annotation(s) — advisory marker anchors, not built");
        if (spec.References.Count > 0)
            // Named EXISTING refs (the in-game referrer) — no records of their own; each `label` is now a
            // name any ref field can resolve (Spec.References.cs).
            Console.WriteLine($"{spec.References.Count} reference(s) — labels bound to existing refs: "
                + string.Join(", ", spec.References.Select(r => $"'{r.Label}'")));
        if (result.Stats.NavCuts > 0)
            // L_NAVCUT volumes (Spec.NavCuts.cs) — vanilla navmesh is switched OFF inside each box at
            // runtime, so NPCs path around what we placed instead of walking into it.
            Console.WriteLine($"{result.Stats.NavCuts} navCut volume(s) — vanilla navmesh cut at runtime (L_NAVCUT / CollisionMarker)");
        if (result.Stats.NavmeshOverrides > 0)
            // Vanilla NAVMs re-emitted UNCHANGED (Spec.NavmeshOverrides.cs). Verify with
            // `navdiag <plugin>`: every mesh's NVNM must byte-match the master's, or we changed
            // something we did not mean to change.
            Console.WriteLine($"{result.Stats.NavmeshOverrides} navmesh override(s) — vanilla NAVM(s) re-emitted unchanged (verify: navdiag {outPath})");
        ReportDependencies(result, outPath, spec);
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
        return 0;
    }

    // -------------------------------------------------------------------------------
    //  requires[] — the DECLARED install requirements, checked against the built masters.
    //
    //  The sidecar (a) records what the plugin needs; it does not stop the plugin from silently
    //  acquiring a NEW need. That is what this does. Asymmetric on purpose:
    //    * a linked master nobody declared → ERROR, and we write NOTHING (the .esp would need a mod
    //      the author never signed up for, and Skyrim answers a missing master by refusing to load
    //      the plugin without a word — the exact failure this feature exists to prevent);
    //    * a declared plugin nothing links → warning (over-stating requirements is not fatal).
    //  A spec with no requires[] section is not checked at all — the contract is opt-in.
    // -------------------------------------------------------------------------------
    private static bool RequiresOk(BuildResult result, string specPath, string outPath)
    {
        foreach (var w in result.Requires.Warnings) Console.WriteLine(w);
        if (result.Requires.Ok) return true;

        Console.Error.WriteLine($"ERROR: requires[] does not declare {result.Requires.Undeclared.Count} master(s) the build links "
            + "— the plugin was NOT written.");
        Console.Error.WriteLine("       Skyrim silently refuses to load a plugin whose masters are missing, so an undeclared");
        Console.Error.WriteLine("       master is an install requirement nobody has been told about:");
        foreach (var e in result.Requires.Errors) Console.Error.WriteLine(e);
        Console.Error.WriteLine("  fix: remove the spec line(s) above to drop the dependency, add the master to requires[],");
        Console.Error.WriteLine($"       or let ModForge write the real set back:  build {specPath} {outPath} --sync-requires");
        return false;
    }

    // `build --sync-requires`: reconcile the spec's requires[] with what the build actually links.
    // Capture (`sc cap`/`sc capp`) drags in dependencies in BULK — hand-maintaining the declaration
    // would make the contract not worth having. Existing entries keep their authored reason/version/url;
    // the resulting SPEC DIFF is the point: a dependency change becomes a reviewable line in git.
    private static void SyncRequiresFile(string specPath, ModSpec spec, BuildResult result)
    {
        var sync = Generator.SyncRequires(spec.Requires, result.Dependencies);
        var raw = JsonNode.Parse(File.ReadAllText(specPath)) as JsonObject
            ?? throw new InvalidOperationException($"{specPath} is not a JSON object");
        var key = raw.Select(kv => kv.Key).FirstOrDefault(k => string.Equals(k, "requires", StringComparison.OrdinalIgnoreCase));

        // The spec we built is the RESOLVED document ($ref/$env spliced in). If requires[] arrived from an
        // include, writing it here would fork the declaration into two files that then drift apart.
        if (key is null && spec.Requires is not null)
        {
            Console.Error.WriteLine($"  ! --sync-requires: requires[] is not in {Path.GetFileName(specPath)} (it came from a $ref include) "
                + "— edit that file, or move requires[] into the top-level spec");
            return;
        }
        if (key is not null && !sync.Changed)
        {
            Console.WriteLine($"requires: already in sync ({sync.Entries.Count} declared)");
            return;
        }

        var arr = JsonSerializer.SerializeToNode(sync.Entries, JsonOpts)!;
        var anchor = key ?? raw.Select(kv => kv.Key).LastOrDefault(k =>
            string.Equals(k, "esl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(k, "pluginName", StringComparison.OrdinalIgnoreCase));

        var rebuilt = new JsonObject();
        if (anchor is null) rebuilt["requires"] = arr;                        // no header fields: put it first
        foreach (var kv in raw)
        {
            if (string.Equals(kv.Key, key, StringComparison.Ordinal)) { rebuilt[kv.Key] = arr; continue; }
            rebuilt[kv.Key] = kv.Value?.DeepClone();
            if (kv.Key == anchor && key is null) rebuilt["requires"] = arr;   // new section, right under the header
        }
        File.WriteAllText(specPath, rebuilt.ToJsonString(JsonOpts) + "\n");
        spec.Requires = sync.Entries.ToList();                                // so the sidecar below reflects it

        Console.WriteLine($"requires: synced {Path.GetFileName(specPath)} ({sync.Entries.Count} declared"
            + (sync.Added.Count > 0 ? $"; +{sync.Added.Count} added: {string.Join(", ", sync.Added)}" : "")
            + (sync.Removed.Count > 0 ? $"; -{sync.Removed.Count} removed: {string.Join(", ", sync.Removed)}" : "") + ")");
    }

    // Non-vanilla masters are an INSTALL REQUIREMENT: Skyrim silently refuses to load a plugin whose
    // masters are missing (no error, no log — the records just are not there). A capture (`sc cap`/
    // `sc capp`) drags one in for every mod-sourced spell/perk/effect/item on the actor, and a
    // hand-written `PROTEUS.esp:0x123` does the same. We deliberately do NOT filter that content
    // (full fidelity beats portability), so the least we can do is SAY it — and say which spec field
    // is responsible, so dropping a dependency is a decision the author can actually act on.
    //
    // The .requires.txt sidecar makes the dependency set durable (a build summary scrolls away, and
    // nothing else in the repo records what an .esp needs). Written next to the plugin like Seq/;
    // `package` prints the same summary but writes NO sidecar — its output folder is the shipped mod.
    private static void ReportDependencies(BuildResult result, string outPath, ModSpec spec)
    {
        foreach (var line in Generator.DependencySummary(result.Dependencies)) Console.WriteLine(line);

        var sidecar = Path.ChangeExtension(Path.GetFullPath(outPath), ".requires.txt");
        // The spec's own requires[] (reason/version/url, and the mods that have no plugin at all) is
        // folded into the sidecar — that file IS the requirements list a player reads.
        var text = Generator.RequiresFileText(Path.GetFileName(outPath), result.Dependencies, spec.Requires);
        if (text is null)
        {
            if (File.Exists(sidecar)) File.Delete(sidecar);   // a stale requirement list is worse than none
            return;
        }
        File.WriteAllText(sidecar, text);
        Console.WriteLine($"wrote {Path.GetFileName(sidecar)} (the install requirements, with the spec field behind each one)");
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
