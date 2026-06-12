using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ModForge;

/// <summary>Tunables for the voice cloning pipeline; null fields fall back to env vars.</summary>
public sealed class VoiceOptions
{
    /// <summary>Path to the TTS script/binary (e.g. voicegen.py venv wrapper). Null → <c>MODFORGE_TTS_BIN</c>.</summary>
    public string? TtsBin { get; set; }

    /// <summary>Path to <c>xWMAEncode.exe</c> (run under Wine). Null → <c>MODFORGE_XWMAENCODE</c>.</summary>
    public string? XwmaEncodeExe { get; set; }

    /// <summary>Path to <c>FaceFXWrapper.exe</c> (run under Wine). Null → <c>MODFORGE_FACEFX</c>.</summary>
    public string? FaceFxExe { get; set; }

    /// <summary>Path to <c>FonixData.cdf</c>. Null → <c>MODFORGE_FONIXDATA</c>.</summary>
    public string? FonixDataCdf { get; set; }

    public string? ResolvedTtsBin => TtsBin ?? Environment.GetEnvironmentVariable("MODFORGE_TTS_BIN");
    public string? ResolvedXwmaEncodeExe => XwmaEncodeExe ?? Environment.GetEnvironmentVariable("MODFORGE_XWMAENCODE");
    public string? ResolvedFaceFxExe => FaceFxExe ?? Environment.GetEnvironmentVariable("MODFORGE_FACEFX");
    public string? ResolvedFonixDataCdf => FonixDataCdf ?? Environment.GetEnvironmentVariable("MODFORGE_FONIXDATA");
}

public static class Voice
{
    /// <summary>
    /// Builds the TTS command-line argument list for one line. Pure (no I/O) so it is unit-testable.
    /// Optional template fields only emit a flag when set, so engine defaults stay in charge otherwise.
    /// </summary>
    public static List<string> BuildTtsArgs(string text, VoiceTemplateSpec template, string specDir, string outWav)
    {
        var args = new List<string>
        {
            "--engine", template.Engine,
            "--text", text,
            "--out", outWav,
        };

        if (!string.IsNullOrWhiteSpace(template.ReferenceWav))
        {
            args.Add("--ref-wav");
            args.Add(Path.Combine(specDir, template.ReferenceWav));
        }

        if (!string.IsNullOrWhiteSpace(template.ReferenceText))
        {
            args.Add("--ref-text");
            args.Add(template.ReferenceText);
        }

        if (!string.IsNullOrWhiteSpace(template.ModelPath))
        {
            args.Add("--model");
            args.Add(Path.Combine(specDir, template.ModelPath));
        }

        if (!string.IsNullOrWhiteSpace(template.RvcModel))
        {
            args.Add("--rvc");
            args.Add(Path.Combine(specDir, template.RvcModel));
        }

        if (template.Seed.HasValue)
        {
            args.Add("--seed");
            args.Add(template.Seed.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (template.Speed.HasValue)
        {
            args.Add("--speed");
            args.Add(template.Speed.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (template.Exaggeration.HasValue)
        {
            args.Add("--exaggeration");
            args.Add(template.Exaggeration.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(template.Language))
        {
            args.Add("--language");
            args.Add(template.Language);
        }

        return args;
    }

    /// <summary>
    /// Generates a voice file (WAV) from text using a template and the configured TTS engine.
    /// </summary>
    public static byte[]? GenerateWav(string text, VoiceTemplateSpec template, string specDir, VoiceOptions options)
    {
        var bin = options.ResolvedTtsBin;
        if (string.IsNullOrEmpty(bin) || !File.Exists(bin)) return null;

        var tempWav = Path.Combine(Path.GetTempPath(), $"modforge_voice_{Guid.NewGuid()}.wav");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = bin,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            foreach (var arg in BuildTtsArgs(text, template, specDir, tempWav))
                psi.ArgumentList.Add(arg);

            Console.WriteLine($"    TTS Command: {bin} {string.Join(" ", psi.ArgumentList)}");
            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("could not start voicegen");
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode == 0 && File.Exists(tempWav))
                return File.ReadAllBytes(tempWav);

            if (!string.IsNullOrEmpty(stderr))
                Console.Error.WriteLine($"    TTS Stderr: {stderr}");
            return null;
        }
        finally
        {
            if (File.Exists(tempWav)) File.Delete(tempWav);
        }
    }

    /// <summary>
    /// Encodes a WAV file to xWMA using xWMAEncode.exe under Wine.
    /// </summary>
    public static byte[]? EncodeXwma(byte[] wav, VoiceOptions options)
    {
        var exe = options.ResolvedXwmaEncodeExe;
        if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return null;

        var tempWav = Path.Combine(Path.GetTempPath(), $"modforge_xwma_in_{Guid.NewGuid()}.wav");
        var tempXwm = Path.Combine(Path.GetTempPath(), $"modforge_xwma_out_{Guid.NewGuid()}.xwm");
        File.WriteAllBytes(tempWav, wav);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wine",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add(exe);
            psi.ArgumentList.Add(WinePath(tempWav));
            psi.ArgumentList.Add(WinePath(tempXwm));

            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("could not start wine for xWMAEncode");
            proc.WaitForExit();

            if (File.Exists(tempXwm))
                return File.ReadAllBytes(tempXwm);

            return null;
        }
        finally
        {
            if (File.Exists(tempWav)) File.Delete(tempWav);
            if (File.Exists(tempXwm)) File.Delete(tempXwm);
        }
    }

    /// <summary>
    /// Generates a .lip file from a WAV file and text using FaceFXWrapper.exe under Wine.
    /// </summary>
    public static byte[]? GenerateLip(byte[] wav, string text, VoiceOptions options)
    {
        var exe = options.ResolvedFaceFxExe;
        var cdf = options.ResolvedFonixDataCdf;
        if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return null;
        if (string.IsNullOrEmpty(cdf) || !File.Exists(cdf)) return null;

        var tempWav = Path.Combine(Path.GetTempPath(), $"modforge_lip_in_{Guid.NewGuid()}.wav");
        var tempLip = Path.Combine(Path.GetTempPath(), $"modforge_lip_out_{Guid.NewGuid()}.lip");
        File.WriteAllBytes(tempWav, wav);

        try
        {
            // FaceFXWrapper Skyrim USEnglish FonixData.cdf in.wav resampled.wav out.lip "text"
            // Note: resampled.wav is optional or handled by the wrapper usually.
            var psi = new ProcessStartInfo
            {
                FileName = "wine",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add(exe);
            psi.ArgumentList.Add("Skyrim");
            psi.ArgumentList.Add("USEnglish");
            psi.ArgumentList.Add(WinePath(cdf));
            psi.ArgumentList.Add(WinePath(tempWav));
            psi.ArgumentList.Add(WinePath(tempWav)); // use same for resampled
            psi.ArgumentList.Add(WinePath(tempLip));
            psi.ArgumentList.Add(text);

            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("could not start wine for FaceFXWrapper");
            proc.WaitForExit();

            if (File.Exists(tempLip))
                return File.ReadAllBytes(tempLip);

            return null;
        }
        finally
        {
            if (File.Exists(tempWav)) File.Delete(tempWav);
            if (File.Exists(tempLip)) File.Delete(tempLip);
        }
    }

    private static string WinePath(string path)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winepath",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("-w");
            psi.ArgumentList.Add(path);

            using var proc = Process.Start(psi);
            if (proc is null) return path;
            var stdout = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            return proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(stdout) ? stdout : path;
        }
        catch
        {
            return path;
        }
    }
}
