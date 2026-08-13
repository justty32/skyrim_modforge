namespace ModForge;

/// <summary>Tunables for <see cref="Papyrus.Compile"/> (Wine/CK path); null fields fall back to env vars then defaults.</summary>
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

/// <summary>Tunables for <see cref="Papyrus.CompileNative"/> (russo-2025/papyrus-compiler, Linux-native).</summary>
public sealed class PapyrusNativeOptions
{
    /// <summary>Path to the <c>papyrus-compiler</c> binary.
    /// Null → <c>MODFORGE_PAPYRUS_COMPILER_BIN</c> → <c>~/tools/papyrus-compiler</c>.</summary>
    public string? CompilerBin { get; set; }

    /// <summary>Directory with Skyrim SE <c>.psc</c> header files (the base-game script sources).
    /// Null → <c>MODFORGE_PAPYRUS_HEADERS</c> → default Steam Data/Scripts/Source path.</summary>
    public string? HeadersDir { get; set; }

    internal string ResolvedCompilerBin => CompilerBin
        ?? Environment.GetEnvironmentVariable("MODFORGE_PAPYRUS_COMPILER_BIN")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "tools", "papyrus-compiler");

    internal string ResolvedHeadersDir => HeadersDir
        ?? Environment.GetEnvironmentVariable("MODFORGE_PAPYRUS_HEADERS")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "Steam", "steamapps", "common", "Skyrim Special Edition", "Data", "Scripts", "Source");
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
/// For Linux-native compilation without Wine, see <see cref="Papyrus.CompileNative"/>.
/// </summary>
public static class Papyrus
{
    /// <summary>
    /// Compile using whichever backend is available: prefers the native russo-2025
    /// <c>papyrus-compiler</c> binary (no Wine required) when it exists on disk; if native compilation
    /// fails (including incomplete headers), retries with the CK's <c>PapyrusCompiler.exe</c> via Wine.
    /// </summary>
    public static CompileResult CompileBest(string scriptPath, string outDir,
        PapyrusNativeOptions? nativeOpts = null, PapyrusOptions? wineOpts = null)
    {
        var native = nativeOpts ?? new PapyrusNativeOptions();
        if (File.Exists(native.ResolvedCompilerBin))
        {
            var nativeResult = CompileNative(scriptPath, outDir, nativeOpts);
            if (nativeResult.Success) return nativeResult;

            // A native compiler can be installed while its header set is incomplete (the stock
            // Steam Data/Scripts/Source directory commonly omits types such as GlobalVariable).
            // The CK/Wine backend uses ModForge's complete extracted header cache, so let it recover
            // instead of silently dropping a generated fragment from the packaged mod.
            var wineResult = Compile(scriptPath, outDir, wineOpts);
            if (wineResult.Success)
            {
                return new CompileResult
                {
                    Success = true,
                    ExitCode = 0,
                    PexPath = wineResult.PexPath,
                    Message = wineResult.Message + $"\n(native compiler failed first: {nativeResult.Message.Split('\n')[0]})",
                };
            }

            return new CompileResult
            {
                Success = false,
                ExitCode = wineResult.ExitCode,
                Message = nativeResult.Message + "\nWine fallback also failed:\n" + wineResult.Message,
            };
        }
        return Compile(scriptPath, outDir, wineOpts);
    }

    /// <summary>
    /// Compile one <c>.psc</c> to <c>.pex</c> using the russo-2025/papyrus-compiler Linux-native
    /// binary (no Wine). Requires the base-game <c>.psc</c> header files in <see cref="PapyrusNativeOptions.HeadersDir"/>.
    /// Never throws — inspect the returned <see cref="CompileResult"/>.
    /// </summary>
    public static CompileResult CompileNative(string scriptPath, string outDir, PapyrusNativeOptions? options = null)
    {
        options ??= new PapyrusNativeOptions();
        var compilerBin = options.ResolvedCompilerBin;
        var headersDir = options.ResolvedHeadersDir;

        if (!File.Exists(compilerBin))
            return Fail(2, $"papyrus-compiler not found: {compilerBin} (set MODFORGE_PAPYRUS_COMPILER_BIN or copy binary to ~/tools/papyrus-compiler)");
        if (!Directory.Exists(headersDir))
            return Fail(2, $"Papyrus headers dir not found: {headersDir} (set MODFORGE_PAPYRUS_HEADERS)");
        if (!File.Exists(scriptPath))
            return Fail(2, $"script not found: {scriptPath}");

        var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
        var outFull = Path.GetFullPath(outDir);
        Directory.CreateDirectory(outFull);

        var psi = new ProcessStartInfo
        {
            FileName = compilerBin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("compile");
        psi.ArgumentList.Add("-nocache");
        psi.ArgumentList.Add("-h");
        psi.ArgumentList.Add(headersDir);
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(scriptPath);
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(outFull);

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("could not start papyrus-compiler");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        var pex = Path.Combine(outFull, scriptName + ".pex");
        bool pexOk = File.Exists(pex);
        if (!pexOk || proc.ExitCode != 0)
        {
            var msg = (stdout + "\n" + stderr).Trim();
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
    }

    /// <summary>Compile one <c>.psc</c> into <paramref name="outDir"/> via Wine + CK compiler.
    /// Never throws on a compile error or missing prereq — inspect the returned <see cref="CompileResult"/>.</summary>
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
    }

    private static CompileResult Fail(int code, string message) =>
        new() { Success = false, ExitCode = code, Message = message };
}
