internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  Helpers used by PackageCmd (Package.cs) for the two bulkiest chores: compiling
    //  generator-produced Papyrus fragment source, and shipping this CLI's embedded
    //  prebuilt .pex files into the mod's Scripts/ folder. Split out to keep Package.cs
    //  a readable top-to-bottom flow; no behavior change from when these were local
    //  functions nested in PackageCmd.
    // -------------------------------------------------------------------------------

    // Compiles one generator-produced .psc (quest/dialogue/scene/perk fragment) into
    // compiledFragmentsDir and copies the .psc to Scripts/Source for reference. Returns
    // true on successful compile (caller tallies autoCompiled).
    private static bool CompileGeneratedFragment(string pscSource, string scriptName, string label,
        string compiledFragmentsDir, string sourceDir)
    {
        Directory.CreateDirectory(sourceDir);
        var pscPath = Path.Combine(compiledFragmentsDir, scriptName + ".psc");
        File.WriteAllText(pscPath, pscSource);
        var cr = Papyrus.CompileBest(pscPath, compiledFragmentsDir);
        bool success = cr.Success;
        if (success)
            Console.WriteLine($"  compiled {label} -> {scriptName}.pex");
        else
        {
            Console.WriteLine($"  (auto-compile skipped for {label}: {cr.Message.Split('\n')[0]})");
            foreach (var line in cr.Message.Split('\n').Skip(1))
                Console.WriteLine($"    {line.TrimEnd('\r')}");
        }
        // Always write the .psc to Scripts/Source for the author's reference.
        File.Copy(pscPath, Path.Combine(sourceDir, scriptName + ".psc"), overwrite: true);
        return success;
    }

    internal static bool ValidMcmPackageCount(ModSpec spec)
    {
        if (spec.McmConfigs.Count <= 1) return true;
        Console.Error.WriteLine($"package: mcmConfigs has {spec.McmConfigs.Count} entries; "
            + "MCM Helper supports one config.json/settings.ini pair per host plugin");
        return false;
    }

    internal static bool CompileRequiredMcmBridges(ModSpec spec,
        Func<string, string, string, bool> compile)
    {
        bool success = true;
        foreach (var m in spec.McmConfigs.Where(Generator.HasMcmGlobalBindings))
            success &= compile(Generator.GenerateMcmGlobalScriptSource(m), Generator.McmGlobalScriptName(m),
                $"MCM global bridge for '{m.ModName}'");
        if (!success)
            Console.Error.WriteLine("package: required MCM global bridge failed to compile; no ESP was written");
        return success;
    }

    internal static bool CompileRequiredSceneFragments(ModSpec spec,
        Func<string, string, string, bool> compile)
    {
        bool requiredSuccess = true;
        foreach (var scene in spec.Scenes)
        {
            var source = Generator.GenerateSceneFragmentSource(scene);
            if (string.IsNullOrEmpty(source)) continue;
            bool compiled = compile(source, Generator.SceneFragmentScriptName(scene),
                $"scene fragment for '{scene.EditorId}'");
            if (scene.Actions.Any(action => action.SetStage is not null)) requiredSuccess &= compiled;
        }
        if (!requiredSuccess)
            Console.Error.WriteLine("package: required scene SetStage fragment failed to compile; no ESP was written");
        return requiredSuccess;
    }

    // Ships one of this CLI's embedded prebuilt .pex resources into scriptsDir. Every
    // generated mod that needs the feature gets the same shared .pex.
    private static void ShipEmbeddedPex(string scriptsDir, string name, string label, string onError)
    {
        Directory.CreateDirectory(scriptsDir);
        using var rs = typeof(Program).Assembly.GetManifestResourceStream(name);
        if (rs is null) { Console.Error.WriteLine($"  ! {name} missing from build — {onError}"); return; }
        using var fs = File.Create(Path.Combine(scriptsDir, name)); rs.CopyTo(fs);
        Console.WriteLine($"  + bundled {name} ({label})");
    }

    // Writes one action-system loose config file (OAR/BDI/PIE/SPID/MCM/FLM/KID/BOS/AOS/
    // SkyPatcher) relative to the mod's output root.
    private static void WriteLooseFile(OarGen.OarFile f, string outModDir)
    {
        var dest = SafeOutputPath.ResolveUnder(outModDir, f.RelPath);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.WriteAllText(dest, f.Content);
    }
}
