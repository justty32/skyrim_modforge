using System.Diagnostics;

namespace ModForge;

// Lip-sync (.lip) generation. Two backends behind one GenerateLip entry point:
//   • CK official LipGenerator.exe (MODFORGE_LIPGEN) — preferred; ships with the Creation Kit at
//     Tools/LipGen/LipGenerator/, auto-finds FonixData.cdf next to its own exe.
//   • community FaceFXWrapper.exe (MODFORGE_FACEFX + MODFORGE_FONIXDATA) — legacy fallback.
// Both shell out under Wine; WinePath() (in Voice.cs) converts Unix temp paths to Z:\ form.
public static partial class Voice
{
    /// <summary>
    /// Builds the CK <c>LipGenerator.exe</c> argument list (run under Wine). Pure (no I/O) so it is
    /// unit-testable. Signature: <c>LipGenerator &lt;wav&gt; &lt;text&gt; -Language:&lt;lang&gt; -OutputFileName:&lt;lip&gt;</c>.
    /// <paramref name="wavWinPath"/> / <paramref name="lipWinPath"/> are expected pre-converted to
    /// Windows form (Z:\…); FonixData.cdf is auto-found next to the exe so no cdf path is passed.
    /// </summary>
    public static List<string> BuildLipGenArgs(string exe, string wavWinPath, string text, string lipWinPath, string language)
        => new()
        {
            exe,
            wavWinPath,
            text,
            $"-Language:{language}",
            $"-OutputFileName:{lipWinPath}",
        };

    /// <summary>
    /// Generates a Skyrim <c>.lip</c> lip-sync file from a WAV + transcript. Prefers the CK official
    /// <c>LipGenerator.exe</c> (<c>MODFORGE_LIPGEN</c>); falls back to the community FaceFXWrapper
    /// (<c>MODFORGE_FACEFX</c> + <c>MODFORGE_FONIXDATA</c>). Returns null when no lip tool is configured.
    /// </summary>
    public static byte[]? GenerateLip(byte[] wav, string text, VoiceOptions options)
    {
        var lipGen = options.ResolvedLipGenExe;
        if (!string.IsNullOrEmpty(lipGen) && File.Exists(lipGen))
            return GenerateLipCk(wav, text, lipGen, options);

        return GenerateLipFaceFx(wav, text, options);
    }

    // CK official LipGenerator.exe path: writes <out>.lip next to the input wav unless -OutputFileName
    // is given. We pass an explicit Windows output path so the temp roundtrip is unambiguous.
    private static byte[]? GenerateLipCk(byte[] wav, string text, string exe, VoiceOptions options)
    {
        var tempWav = Path.Combine(Path.GetTempPath(), $"modforge_lipgen_in_{Guid.NewGuid()}.wav");
        var tempLip = Path.Combine(Path.GetTempPath(), $"modforge_lipgen_out_{Guid.NewGuid()}.lip");
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
            foreach (var arg in BuildLipGenArgs(exe, WinePath(tempWav), text, WinePath(tempLip), options.ResolvedLipLanguage))
                psi.ArgumentList.Add(arg);

            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("could not start wine for LipGenerator");
            proc.WaitForExit();

            if (File.Exists(tempLip) && new FileInfo(tempLip).Length > 0)
                return File.ReadAllBytes(tempLip);

            return null;
        }
        finally
        {
            if (File.Exists(tempWav)) File.Delete(tempWav);
            if (File.Exists(tempLip)) File.Delete(tempLip);
        }
    }

    // Legacy community FaceFXWrapper.exe path (kept as a fallback).
    private static byte[]? GenerateLipFaceFx(byte[] wav, string text, VoiceOptions options)
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
}
