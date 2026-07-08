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
            Console.WriteLine($"  (auto-compile skipped for {label}: {cr.Message.Split('\n')[0]})");
        // Always write the .psc to Scripts/Source for the author's reference.
        File.Copy(pscPath, Path.Combine(sourceDir, scriptName + ".psc"), overwrite: true);
        return success;
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
        var dest = Path.Combine(outModDir, f.RelPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.WriteAllText(dest, f.Content);
    }
}
