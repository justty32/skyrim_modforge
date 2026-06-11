using System.Text;

namespace ModForge;

/// <summary>What to write for one voice line: the payload + extension, an optional loose .lip
/// sidecar, and a warning the CLI must print when the requested format had to be downgraded.</summary>
public sealed record VoicePackResult(byte[] Data, string Ext, byte[]? LooseLip, string? Warning);

public static partial class Generator
{
    /// <summary>
    /// Decides the final on-disk payload for a generated voice line. NEVER packs raw PCM WAV into
    /// a .fuz (whether the engine plays it is unverified — most likely silent): when xWMA encoding
    /// failed and format is fuz/xwm, the WAV is shipped LOOSE at the same basename instead (the
    /// engine does load loose .wav voice files), with a warning for the CLI to surface. If a .lip
    /// was generated but the .fuz can't be packed, the .lip is emitted loose alongside.
    /// </summary>
    public static VoicePackResult PackVoiceAudio(string format, byte[] wav, byte[]? xwm, byte[]? lip, string baseName)
    {
        switch (format)
        {
            case "wav":
                return new(wav, "wav", null, null);
            case "xwm":
                if (xwm is not null) return new(xwm, "xwm", null, null);
                return new(wav, "wav", null,
                    $"    !! {baseName}: xWMAEncode failed or MODFORGE_XWMAENCODE not set — "
                    + $"wrote loose {baseName}.wav instead of .xwm (the engine loads loose .wav voice files)");
            default:    // "fuz"
                if (xwm is not null) return new(WriteFuz(xwm, lip), "fuz", null, null);
                return new(wav, "wav", lip,
                    $"    !! {baseName}: xWMAEncode failed or MODFORGE_XWMAENCODE not set — "
                    + $"wrote loose {baseName}.wav next to the intended .fuz instead of packing raw PCM into a .fuz (likely silent in-engine); "
                    + (lip is not null
                        ? $"lip data written loose as {baseName}.lip (lip sync may be missing/odd)"
                        : "lip sync will be missing"));
        }
    }

    /// <summary>
    /// Writes a Skyrim .fuz container (header + optional .lip + audio).
    /// </summary>
    /// <param name="audio">The audio stream (typically .xwm or .wav).</param>
    /// <param name="lip">Optional .lip lipsync data (null = zero size).</param>
    /// <returns>The complete .fuz file bytes.</returns>
    public static byte[] WriteFuz(byte[] audio, byte[]? lip)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        // Header: 12 bytes
        w.Write(Encoding.ASCII.GetBytes("FUZE")); // 0: magic
        w.Write((uint)1);                         // 4: version (0x01000000 little-endian)
        w.Write((uint)(lip?.Length ?? 0));        // 8: lip size

        if (lip is { Length: > 0 })
            w.Write(lip);

        w.Write(audio);

        return ms.ToArray();
    }

    /// <summary>
    /// Computes the deterministic voice filename used by the Skyrim engine.
    /// Format: [QuestID]_[TopicID]_[InfoFormID]_[Index].fuz
    /// QuestID: first 10 chars of Quest EditorID.
    /// TopicID: first 15 chars of Topic EditorID.
    /// InfoFormID: 8-digit hex FormID of the INFO record.
    /// Index: 1-based index of the response within the INFO.
    /// </summary>
    public static string VoiceFileName(string questEd, string topicEd, uint infoFormId, int responseIndex, string ext = "fuz")
    {
        var qPart = SanitizeVoicePart(questEd, 10);
        var tPart = SanitizeVoicePart(topicEd, 15);
        var fPart = infoFormId.ToString("X8");
        var iPart = responseIndex.ToString();

        return $"{qPart}_{tPart}_{fPart}_{iPart}.{ext.TrimStart('.')}";
    }

    private static string SanitizeVoicePart(string s, int maxLen)
    {
        // Skyrim voice filenames typically strip non-alphanumerics but keep underscores.
        // Actually, the CK just truncates the raw EditorID.
        var r = s ?? "";
        if (r.Length > maxLen)
            r = r.Substring(0, maxLen);

        // Remove any characters that might be illegal in a filename just in case,
        // although EditorIDs are already quite restricted.
        var sb = new StringBuilder();
        foreach (var c in r)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
        }
        return sb.ToString();
    }
}
