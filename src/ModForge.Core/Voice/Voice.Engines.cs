namespace ModForge;

public enum VoiceEngineStatus { Wired, Reserved }

public sealed record VoiceEngine(string Name, IReadOnlyList<string> Aliases,
    VoiceEngineStatus Status, string? WrapperEnvironmentVariable, string? WrapperContract);

/// <summary>Engine availability and pre-process decisions; no environment probing or I/O.</summary>
public static class VoiceEngines
{
    public const int ReservedExitCode = 3;
    public const int ReservedLineResult = -2;
    public const string ReservedSkipReason = "skipped: engine reserved";

    public static IReadOnlyList<VoiceEngine> All { get; } = Array.AsReadOnly(new[]
    {
        new VoiceEngine("f5", Array.Empty<string>(), VoiceEngineStatus.Wired, null, null),
        new VoiceEngine("fish-s2", Array.AsReadOnly(new[] { "fish", "fishspeech", "fish-speech" }),
            VoiceEngineStatus.Wired, "MODFORGE_FISH_SPEECH_BIN", null),
        Reserved("chatterbox", "MODFORGE_CHATTERBOX_BIN"),
        Reserved("gptsovits", "MODFORGE_GPTSOVITS_BIN"),
        Reserved("xtts", "MODFORGE_XTTS_BIN"),
    });

    private static VoiceEngine Reserved(string name, string variable) => new(name,
        Array.Empty<string>(), VoiceEngineStatus.Reserved, variable,
        $"Future wiring requires {variable} to point to a wrapper executable/script, plus an explicit "
        + "voicegen.py routing implementation and registry promotion to wired (setting the variable alone does not enable it). "
        + "The wrapper must accept --engine/--text/--out and optional --ref-wav/--ref-text/--model/--rvc/"
        + "--seed/--speed/--exaggeration/--language/--emotion/--intensity with the existing argument semantics; "
        + "write a valid, nonempty WAV to --out; return exit 0 only on success and nonzero on failure.");

    public static VoiceEngine? Resolve(string? name) => All.FirstOrDefault(e =>
        string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)
        || e.Aliases.Contains(name ?? "", StringComparer.OrdinalIgnoreCase));

    /// <summary>Checked before cache access or any external process in voicelines.</summary>
    public static string? GenerationSkipReason(VoiceTemplateSpec template) =>
        Resolve(template.Engine)?.Status == VoiceEngineStatus.Reserved ? ReservedSkipReason : null;

    public static string? ValidationWarning(VoiceTemplateSpec template) =>
        Resolve(template.Engine) is { Status: VoiceEngineStatus.Reserved } engine
            ? $"WARNING: voiceTemplate '{template.Id}': engine '{engine.Name}' is reserved and not wired; "
                + $"voicelines will skip all of its voice lines. {engine.WrapperContract}"
            : null;

    /// <summary>Apply the same reserved decision to dry-run/diagnostic targets, preserving other skips.</summary>
    public static List<VoiceLinePlanEntry> ApplyToPlan(IEnumerable<VoiceLinePlanEntry> entries,
        IEnumerable<VoiceTemplateSpec> templates)
    {
        var byId = templates.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        return entries.Select(e => e.SkipReason is null && e.TemplateId is not null
            && byId.TryGetValue(e.TemplateId, out var template) && GenerationSkipReason(template) is { } reason
                ? e with { SkipReason = reason } : e).ToList();
    }

    public static int ExitCode(int reservedSkipped) => reservedSkipped > 0 ? ReservedExitCode : 0;
}
