internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  compile — drive the Creation Kit's PapyrusCompiler.exe under Wine: .psc -> .pex.
    //  Base script sources + the flags file come from the CK's Scripts.zip (extract
    //  once to MODFORGE_PAPYRUS_BASE; default ~/.cache/modforge/papyrus/Source/Scripts).
    //  GOTCHA: the compiler returns exit code 0 even on failure -> scrape stdout
    //  ("Failed on") and confirm the .pex was actually produced.
    // -------------------------------------------------------------------------------
    private static readonly string PapyrusCompilerExe =
        Environment.GetEnvironmentVariable("MODFORGE_PAPYRUS_COMPILER")
        ?? "/home/lorkhan/.local/share/Steam/steamapps/common/Skyrim Special Edition 1946180/Papyrus Compiler/PapyrusCompiler.exe";
    private static readonly string PapyrusBaseScripts =
        Environment.GetEnvironmentVariable("MODFORGE_PAPYRUS_BASE")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".cache", "modforge", "papyrus", "Source", "Scripts");

    private static int Compile(string scriptPath, string outDir)
    {
        var flags = Path.Combine(PapyrusBaseScripts, "TESV_Papyrus_Flags.flg");
        if (!File.Exists(PapyrusCompilerExe))
        { Console.Error.WriteLine($"ERROR: PapyrusCompiler not found: {PapyrusCompilerExe} (set MODFORGE_PAPYRUS_COMPILER)"); return 2; }
        if (!File.Exists(flags))
        { Console.Error.WriteLine($"ERROR: flags file not found: {flags} (set MODFORGE_PAPYRUS_BASE to the extracted Source/Scripts)"); return 2; }
        if (!File.Exists(scriptPath))
        { Console.Error.WriteLine($"ERROR: script not found: {scriptPath}"); return 2; }

        var dir = Path.GetDirectoryName(scriptPath);
        var scriptDir = Path.GetFullPath(string.IsNullOrEmpty(dir) ? "." : dir);
        var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
        var outFull = Path.GetFullPath(outDir);
        Directory.CreateDirectory(outFull);

        var psi = new ProcessStartInfo
        {
            FileName = "wine",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(PapyrusCompilerExe);
        psi.ArgumentList.Add(scriptName);
        psi.ArgumentList.Add($"-f={flags}");
        psi.ArgumentList.Add($"-i={PapyrusBaseScripts};{scriptDir}");
        psi.ArgumentList.Add($"-o={outFull}");

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("could not start wine");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        var pex = Path.Combine(outFull, scriptName + ".pex");
        bool pexOk = File.Exists(pex);
        bool failed = !pexOk || stdout.Contains("Failed on") || stdout.Contains("compilation failed");
        if (failed)
        {
            Console.Error.WriteLine($"compile FAILED for {scriptName}");
            var msg = stdout.Trim();
            if (msg.Length > 0) Console.Error.WriteLine(msg);
            return 1;
        }
        Console.WriteLine($"compiled {scriptName} -> {pex} ({new FileInfo(pex).Length} bytes)");
        return 0;
    }

    // -------------------------------------------------------------------------------
    //  package — build the .esp, compile any script sources, and lay out an MO2/Vortex-
    //  ready mod folder: <outModDir>/<PluginName> + Scripts/*.pex + Scripts/Source/*.psc.
    //  (A script entry with a `source` .psc gets compiled; its VMAD attach happened in
    //  Build by Scriptname.)
    // -------------------------------------------------------------------------------
    private static int Package(string specPath, string outModDir)
    {
        var spec = JsonSerializer.Deserialize<ModSpec>(File.ReadAllText(specPath), ReadOpts)
                   ?? throw new InvalidOperationException("spec deserialized to null");
        var pluginName = string.IsNullOrEmpty(spec.PluginName) ? "Generated.esp" : spec.PluginName;
        Directory.CreateDirectory(outModDir);

        // 1) the plugin (Build also does the VMAD script attach by Scriptname)
        Build(specPath, Path.Combine(outModDir, pluginName));

        // 2) compile each referenced script source -> Scripts/*.pex; copy .psc -> Scripts/Source/
        var scriptsDir = Path.Combine(outModDir, "Scripts");
        var sourceDir = Path.Combine(scriptsDir, "Source");
        var specDir = Path.GetDirectoryName(Path.GetFullPath(specPath)) ?? ".";
        int compiled = 0;
        foreach (var sa in spec.Scripts)
        {
            if (string.IsNullOrEmpty(sa.Source)) continue;
            var src = Path.IsPathRooted(sa.Source) ? sa.Source : Path.Combine(specDir, sa.Source);
            if (!File.Exists(src)) { Console.Error.WriteLine($"  ! script source not found: {src}"); continue; }
            if (Compile(src, scriptsDir) != 0) { Console.Error.WriteLine($"  ! compile failed: {sa.Source}"); continue; }
            Directory.CreateDirectory(sourceDir);
            File.Copy(src, Path.Combine(sourceDir, Path.GetFileName(src)), overwrite: true);
            compiled++;
        }

        Console.WriteLine($"packaged -> {outModDir}  ({pluginName} + {compiled} compiled script(s) under Scripts/)");
        return 0;
    }
}
