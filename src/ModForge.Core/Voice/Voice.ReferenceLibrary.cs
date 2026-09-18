using System.Text.Json;

namespace ModForge;

public static partial class Voice
{
    /// <summary>Read an annotation array and apply the user's corrections before selection.</summary>
    public static List<VoiceAnnotation> LoadReferenceLibrary(string manifestPath)
    {
        try
        {
            var entries = JsonSerializer.Deserialize<List<VoiceAnnotation>>(File.ReadAllText(manifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (entries is null)
                throw new JsonException("Expected an annotation array, not null.");
            foreach (var entry in entries)
            {
                if (entry is null || string.IsNullOrWhiteSpace(entry.Clip))
                    throw new JsonException("Every annotation must have a non-empty clip path.");
                if (!string.IsNullOrWhiteSpace(entry.Override)) entry.Emotion = entry.Override;
                if (entry.IntensityOverride.HasValue) entry.Intensity = entry.IntensityOverride.Value;
            }
            return entries;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new InvalidDataException($"referenceLibrary '{manifestPath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Pure selection over corrected annotations. Missing emotion/intensity means Neutral/0.
    /// No matching emotion falls back to Neutral; no Neutral returns null (use the template reference).
    /// Ordinal FormID, clip and transcript break ties independently of manifest enumeration order.
    /// </summary>
    public static VoiceAnnotation? SelectReferenceClip(string? emotion, int? intensity,
        IReadOnlyList<VoiceAnnotation> correctedManifest)
    {
        var target = string.IsNullOrWhiteSpace(emotion) ? "Neutral" : emotion;
        var matching = correctedManifest.Where(entry =>
            string.Equals(entry.Emotion, target, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matching.Count == 0)
            matching = correctedManifest.Where(entry =>
                string.Equals(entry.Emotion, "Neutral", StringComparison.OrdinalIgnoreCase)).ToList();
        return matching.OrderBy(entry => Math.Abs((long)entry.Intensity - (intensity ?? 0)))
            .ThenBy(entry => entry.InfoFormId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Clip, StringComparer.Ordinal)
            .ThenBy(entry => entry.Text, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
