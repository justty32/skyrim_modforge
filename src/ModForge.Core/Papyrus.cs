namespace ModForge;

/// <summary>Tunables for <see cref="Papyrus.Compile"/>; null fields fall back to env vars then defaults.</summary>
public sealed class PapyrusOptions
{
    /// <summary>Path to <c>PapyrusCompiler.exe</c>. Null → <c>MODFORGE_PAPYRUS_COMPILER</c> → Steam default.</summary>
    public string? CompilerExe { get; set; }

    /// <summary>Dir with the base <c>.psc</c> + <c>TESV_Papyrus_Flags.flg</c>. Null → <c>MODFORGE_PAPYRUS_BASE</c> → cache default.</summary>
    public string? BaseScripts { get; set; }

    internal string ResolvedCompilerExe => CompilerExe
        ?? Environment.GetEnvironmentVariable("MODFORGE_PAPYRUS_COMPILER")
        ?? "/home/lorkhan/.local/share/Steam/steamapps/common/Skyrim Special Edition 1946180/Papyrus Compiler/PapyrusCompiler.exe";

    internal string ResolvedBaseScripts => BaseScripts
        ?? Environment.GetEnvironmentVariable("MODFORGE_PAPYRUS_BASE")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".cache", "modforge", "papyrus", "Source", "Scripts");
}

/// <summary>Outcome of a <see cref="Papyrus.Compile"/> call.</summary>
public sealed class CompileResult
{
    /// <summary>True when a <c>.pex</c> was produced and the compiler reported no failure.</summary>
    public required bool Success { get; init; }
    /// <summary>0 = ok, 1 = compile failed, 2 = prerequisite missing.</summary>
    public required int ExitCode { get; init; }
    /// <summary>Path to the produced <c>.pex</c> (when <see cref="Success"/>).</summary>
    public string? PexPath { get; init; }
    /// <summary>Human-readable summary (success line) or error/compiler output.</summary>
    public required string Message { get; init; }
}

/// <summary>
/// Drive the Creation Kit's <c>PapyrusCompiler.exe</c> under Wine: <c>.psc → .pex</c>.
/// GOTCHA the compiler returns exit code 0 even on failure → we scrape stdout ("Failed on")
/// and confirm the .pex was actually produced.
/// </summary>
public static class Papyrus
{
    /// <summary>Compile one <c>.psc</c> into <paramref name="outDir"/>. Never throws on a compile
    /// error or missing prereq — inspect the returned <see cref="CompileResult"/>.</summary>
    public static CompileResult Compile(string scriptPath, string outDir, PapyrusOptions? options = null)
    {
        options ??= new PapyrusOptions();
        var compilerExe = options.ResolvedCompilerExe;
        var baseScripts = options.ResolvedBaseScripts;
        var flags = Path.Combine(baseScripts, "TESV_Papyrus_Flags.flg");

        if (!File.Exists(compilerExe))
            return Fail(2, $"PapyrusCompiler not found: {compilerExe} (set MODFORGE_PAPYRUS_COMPILER)");
        if (!File.Exists(flags))
            return Fail(2, $"flags file not found: {flags} (set MODFORGE_PAPYRUS_BASE to the extracted Source/Scripts)");
        if (!File.Exists(scriptPath))
            return Fail(2, $"script not found: {scriptPath}");

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
        psi.ArgumentList.Add(compilerExe);
        psi.ArgumentList.Add(scriptName);
        psi.ArgumentList.Add($"-f={flags}");
        psi.ArgumentList.Add($"-i={baseScripts};{scriptDir}");
        psi.ArgumentList.Add($"-o={outFull}");

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("could not start wine");
        var stdout = proc.StandardOutput.ReadToEnd();
        proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        var pex = Path.Combine(outFull, scriptName + ".pex");
        bool pexOk = File.Exists(pex);
        bool failed = !pexOk || stdout.Contains("Failed on") || stdout.Contains("compilation failed");
        if (failed)
        {
            var msg = stdout.Trim();
            return new CompileResult
            {
                Success = false, ExitCode = 1,
                Message = $"compile FAILED for {scriptName}" + (msg.Length > 0 ? "\n" + msg : ""),
            };
        }
        return new CompileResult
        {
            Success = true, ExitCode = 0, PexPath = pex,
            Message = $"compiled {scriptName} -> {pex} ({new FileInfo(pex).Length} bytes)",
        };

        static CompileResult Fail(int code, string message) =>
            new() { Success = false, ExitCode = code, Message = message };
    }
}
