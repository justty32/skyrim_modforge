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
        if (!ValidMcmPackageCount(spec)) return 1;
        if (!ValidSceneSetStages(spec, "package")) return 1;
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
        if (!PrecompileGeneratedFragments(spec, compiledFragmentsDir, sourceDir, out var autoCompiled))
        {
            try { Directory.Delete(compiledFragmentsDir, recursive: true); } catch { /* best effort */ }
            return 1;
        }

        // 2) Build the plugin, passing CompiledScriptsDir so WireQuestStages and
        //    AttachDialogueResultScripts wire the VMAD for any fragment whose .pex exists.
        var espPath = Path.Combine(outModDir, pluginName);
        var key = ModKey.FromNameAndExtension(pluginName);
        var result = Generator.Build(spec, key, new BuildOptions { CompiledScriptsDir = compiledFragmentsDir, SpecDir = specDir });
        // The requires[] contract (Generator.Requires.cs) gates the SHIPPED mod too — an undeclared
        // master is exactly the thing that must not reach a player. No --sync-requires here: syncing is
        // an edit to the spec, and that belongs to `build`.
        if (!RequiresOk(result, specPath, espPath)) return 1;
        PluginIo.Write(result.Mod, espPath);
        foreach (var w in result.Warnings) Console.WriteLine(w);
        foreach (var n in result.Notes) Console.WriteLine(n);   // advisory INFO — nothing is wrong (BuildResult.Notes)
        Console.WriteLine(BuildSummary(result.Stats, specPath, espPath));
        // Install requirements (Generator.Dependencies.cs). Print the same summary `build` does, AND drop
        // a PLAYER-facing REQUIREMENTS.txt into the shipped folder: this is the mod a player installs, and
        // "which other mods must I install first" is the one thing they most need and cannot get anywhere
        // else. It is the shipped form (no spec-field attribution / rebuild advice — see RequiresFileText);
        // `build`'s author-facing <plugin>.requires.txt sidecar stays in the author's working dir.
        foreach (var line in Generator.DependencySummary(result.Dependencies)) Console.WriteLine(line);
        var reqText = Generator.RequiresFileText(pluginName, result.Dependencies, spec.Requires, forShippedMod: true);
        if (reqText is not null)
        {
            var reqPath = Path.Combine(outModDir, "REQUIREMENTS.txt");
            File.WriteAllText(reqPath, reqText);
            Console.WriteLine("wrote REQUIREMENTS.txt (the mods a player must install first, with each one's reason/version/link)");
        }
        WriteSeq(espPath, outModDir);

        // 3) Copy compiled .pex files (fragments + user scripts) into Scripts/.
        Directory.CreateDirectory(scriptsDir);
        foreach (var pex in Directory.GetFiles(compiledFragmentsDir, "*.pex"))
            File.Copy(pex, Path.Combine(scriptsDir, Path.GetFileName(pex)), overwrite: true);

        var (compiled, wordWallScripts) = CompileUserScriptsAndWordWalls(spec, specDir, scriptsDir, sourceDir, autoCompiled);
        ShipFeaturePexFiles(spec, scriptsDir);

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

        WriteActionSystemLooseFiles(spec, outModDir, specDir, assetsSrc, pluginName);

        // Cleanup temp dir.
        try { Directory.Delete(compiledFragmentsDir, recursive: true); } catch { /* best effort */ }

        Console.WriteLine($"packaged -> {outModDir}  ({pluginName} + {compiled} compiled script(s)"
            + (wordWallScripts > 0 ? $" + {wordWallScripts} word-wall fragment(s)" : "") + " under Scripts/)");
        return 0;
    }
}