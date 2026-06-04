internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  package — build the .esp, compile any script sources, and lay out an MO2/Vortex-
    //  ready mod folder: <outModDir>/<PluginName> + Scripts/*.pex + Scripts/Source/*.psc.
    //  Shares ReadSpec/BuildSummary/WriteSeq with the other commands in Program.cs.
    // -------------------------------------------------------------------------------
    private static int PackageCmd(string specPath, string outModDir, string? assetsOverride)
    {
        var spec = ReadSpec(specPath);
        var pluginName = string.IsNullOrEmpty(spec.PluginName) ? "Generated.esp" : spec.PluginName;
        Directory.CreateDirectory(outModDir);

        var scriptsDir = Path.Combine(outModDir, "Scripts");
        var sourceDir = Path.Combine(scriptsDir, "Source");
        var specDir = Path.GetDirectoryName(Path.GetFullPath(specPath)) ?? ".";

        // 1) Pre-compile generated quest/dialogue fragment .psc files so the VMAD can be attached
        //    in Build(). These .psc files are generated from the spec (no user authoring required):
        //    * Quest stage→objective fragments: Fragment_Stage_XXXX_Item00000 functions that call
        //      SetObjectiveDisplayed/Completed — makes quests appear in the Active Quests journal.
        //    * Dialogue set-stage fragments: Fragment_0(akSpeakerRef) that calls SetStage(N) when a
        //      dialogue line is picked — advances quest stage without CK scripting.
        //    Both are compiled into a temp dir; Build() checks that dir and only attaches the VMAD
        //    when the .pex is confirmed present (absent .pex → Papyrus error at quest-start).
        var compiledFragmentsDir = Path.Combine(Path.GetTempPath(), "modforge_fragments_" + Path.GetRandomFileName());
        Directory.CreateDirectory(compiledFragmentsDir);
        int autoCompiled = 0;

        void CompileGenerated(string pscSource, string scriptName, string label)
        {
            Directory.CreateDirectory(sourceDir);
            var pscPath = Path.Combine(compiledFragmentsDir, scriptName + ".psc");
            File.WriteAllText(pscPath, pscSource);
            var cr = Papyrus.CompileBest(pscPath, compiledFragmentsDir);
            if (cr.Success)
            {
                Console.WriteLine($"  compiled {label} -> {scriptName}.pex");
                autoCompiled++;
            }
            else
                Console.WriteLine($"  (auto-compile skipped for {label}: {cr.Message.Split('\n')[0]})");
            // Always write the .psc to Scripts/Source for the author's reference.
            File.Copy(pscPath, Path.Combine(sourceDir, scriptName + ".psc"), overwrite: true);
        }

        foreach (var q in spec.Quests)
        {
            var src = Generator.GenerateQuestFragmentSource(q);
            if (!string.IsNullOrEmpty(src))
                CompileGenerated(src, Generator.QuestFragmentScriptName(q), $"quest fragment for '{q.EditorId}'");
        }
        foreach (var d in spec.Dialogue)
        {
            var src = Generator.GenerateDialogueFragmentSource(d);
            if (!string.IsNullOrEmpty(src))
                CompileGenerated(src, Generator.DialogueFragmentScriptName(d), $"dialogue fragment for '{d.EditorId}'");
        }

        // 2) Build the plugin, passing CompiledScriptsDir so WireQuestStages and
        //    AttachDialogueResultScripts wire the VMAD for any fragment whose .pex exists.
        var espPath = Path.Combine(outModDir, pluginName);
        var key = ModKey.FromNameAndExtension(pluginName);
        var result = Generator.Build(spec, key, new BuildOptions { CompiledScriptsDir = compiledFragmentsDir });
        PluginIo.Write(result.Mod, espPath);
        foreach (var w in result.Warnings) Console.WriteLine(w);
        Console.WriteLine(BuildSummary(result.Stats, specPath, espPath));
        WriteSeq(espPath, outModDir);

        // 3) Copy compiled .pex files (fragments + user scripts) into Scripts/.
        Directory.CreateDirectory(scriptsDir);
        foreach (var pex in Directory.GetFiles(compiledFragmentsDir, "*.pex"))
            File.Copy(pex, Path.Combine(scriptsDir, Path.GetFileName(pex)), overwrite: true);

        // 4) User-specified script sources: compile + copy to Scripts/ + Scripts/Source/.
        int compiled = autoCompiled;
        var compiledSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool CompileSource(string? source, string label)
        {
            if (string.IsNullOrEmpty(source)) return false;
            var src = Path.IsPathRooted(source) ? source : Path.Combine(specDir, source);
            if (!compiledSources.Add(Path.GetFullPath(src))) return false;
            if (!File.Exists(src)) { Console.Error.WriteLine($"  ! script source not found: {src}"); return false; }
            var cr = Papyrus.CompileBest(src, scriptsDir);
            if (!cr.Success) { Console.Error.WriteLine(cr.Message); Console.Error.WriteLine($"  ! compile failed: {label}"); return false; }
            Console.WriteLine(cr.Message);
            Directory.CreateDirectory(sourceDir);
            File.Copy(src, Path.Combine(sourceDir, Path.GetFileName(src)), overwrite: true);
            compiled++;
            return true;
        }
        foreach (var sa in spec.Scripts) CompileSource(sa.Source, sa.Source);
        foreach (var d in spec.Dialogue) CompileSource(d.ResultScriptSource, d.ResultScriptSource);

        // 5) Word-wall teaching fragments (generated source, compiled best-effort).
        int wordWallScripts = 0;
        foreach (var ww in spec.WordWalls)
        {
            var scriptName = string.IsNullOrWhiteSpace(ww.ScriptName) ? ww.EditorId + "Script" : ww.ScriptName;
            Directory.CreateDirectory(sourceDir);
            var pscPath = Path.Combine(sourceDir, scriptName + ".psc");
            File.WriteAllText(pscPath, Generator.GenerateWordWallScript(ww));
            wordWallScripts++;
            var cr = Papyrus.CompileBest(pscPath, scriptsDir);
            if (cr.Success) { Console.WriteLine(cr.Message); compiled++; }
            else Console.WriteLine($"  (word-wall script {scriptName}.psc written to Scripts/Source — compile pending: {cr.Message.Split('\n')[0]})");
        }

        // 5b) Ship the generic Script Event dispatcher .pex whenever a quest uses event "ScriptEvent".
        //     It's the universal entry content calls (MFStoryEventDispatch.Fire(kw, ref…)) to fire a
        //     story event; one prebuilt .pex (embedded in this CLI) serves every generated mod.
        if (spec.Quests.Any(q => q.StoryEvent is { } se
                && se.Event.Equals("ScriptEvent", StringComparison.OrdinalIgnoreCase)))
        {
            Directory.CreateDirectory(scriptsDir);
            using var rs = typeof(Program).Assembly.GetManifestResourceStream("MFStoryEventDispatch.pex");
            if (rs is null)
                Console.Error.WriteLine("  ! Script Event dispatcher .pex missing from build — ScriptEvent quests won't fire");
            else
            {
                using var fs = File.Create(Path.Combine(scriptsDir, "MFStoryEventDispatch.pex"));
                rs.CopyTo(fs);
                Console.WriteLine("  + bundled MFStoryEventDispatch.pex (Script Event dispatcher)");
            }
        }

        // 6) External-resource bundling — copy spec's (or --assets) Meshes/Textures/Sounds/….
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

        // Cleanup temp dir.
        try { Directory.Delete(compiledFragmentsDir, recursive: true); } catch { /* best effort */ }

        Console.WriteLine($"packaged -> {outModDir}  ({pluginName} + {compiled} compiled script(s)"
            + (wordWallScripts > 0 ? $" + {wordWallScripts} word-wall fragment(s)" : "") + " under Scripts/)");
        return 0;
    }
}
