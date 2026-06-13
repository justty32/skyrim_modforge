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

    /// <summary>Path to the CK official <c>LipGenerator.exe</c> (run under Wine). Null → <c>MODFORGE_LIPGEN</c>.
    /// Preferred over FaceFXWrapper: it auto-finds <c>FonixData.cdf</c> next to its own exe, so no cdf path
    /// is needed. Ships with the CK at <c>Tools/LipGen/LipGenerator/LipGenerator.exe</c>.</summary>
    public string? LipGenExe { get; set; }

    /// <summary>LipGenerator language vocabulary (USEnglish / French / German / …). Null → USEnglish.
    /// Note this is the CK's language name space, distinct from the TTS <c>--language</c> code.</summary>
    public string? LipLanguage { get; set; }

    public string? ResolvedTtsBin => TtsBin ?? Environment.GetEnvironmentVariable("MODFORGE_TTS_BIN");
    public string? ResolvedXwmaEncodeExe => XwmaEncodeExe ?? Environment.GetEnvironmentVariable("MODFORGE_XWMAENCODE");
    public string? ResolvedFaceFxExe => FaceFxExe ?? Environment.GetEnvironmentVariable("MODFORGE_FACEFX");
    public string? ResolvedFonixDataCdf => FonixDataCdf ?? Environment.GetEnvironmentVariable("MODFORGE_FONIXDATA");
    public string? ResolvedLipGenExe => LipGenExe ?? Environment.GetEnvironmentVariable("MODFORGE_LIPGEN");
    public string ResolvedLipLanguage => string.IsNullOrWhiteSpace(LipLanguage) ? "USEnglish" : LipLanguage;
}

public static partial class Voice
{
    /// <summary>
    /// Builds the TTS command-line argument list for one line. Pure (no I/O) so it is unit-testable.
    /// Optional template fields only emit a flag when set, so engine defaults stay in charge otherwise.
    /// </summary>
    public static List<string> BuildTtsArgs(string text, VoiceTemplateSpec template, string specDir, string outWav,
        string? emotion = null, int? intensity = null)
    {
        var args = new List<string>
        {
            "--engine", template.Engine,
            "--text", text,
            "--out", outWav,
        };

        // Delivery emotion sourced from the dialogue INFO record (not the spec). Part of the
        // ModForge↔voicegen protocol; engines without expressive control note+ignore it.
        if (!string.IsNullOrWhiteSpace(emotion))
        {
            args.Add("--emotion");
            args.Add(emotion);
        }
        if (intensity.HasValue)
        {
            args.Add("--intensity");
            args.Add(intensity.Value.ToString(CultureInfo.InvariantCulture));
        }

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
    public static byte[]? GenerateWav(string text, VoiceTemplateSpec template, string specDir, VoiceOptions options,
        string? emotion = null, int? intensity = null)
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

            foreach (var arg in BuildTtsArgs(text, template, specDir, tempWav, emotion, intensity))
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
