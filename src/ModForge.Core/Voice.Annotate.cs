using System;
using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

// One clip's annotation entry — the unit of the voice-annotation manifest. `emotion`/`intensity` are
// deterministic (read from the source INFO); the user corrects via `override`/`intensityOverride`/`note`.
public sealed class VoiceAnnotation
{
    public string Clip { get; set; } = "";
    public string VoiceType { get; set; } = "";
    public string Text { get; set; } = "";
    public string Emotion { get; set; } = "Neutral";
    public int Intensity { get; set; }
    public string Quest { get; set; } = "";
    public string Topic { get; set; } = "";
    public string InfoFormId { get; set; } = "";
    public string Override { get; set; } = "";
    public int? IntensityOverride { get; set; }
    public string Note { get; set; } = "";
}

public static class VoiceAnnotate
{
    // Parse the INFO FormKey out of a clip filename "<quest>_<topic>_<infoFormId:X8>_<n>.fuz".
    // The 8-hex FormID's high byte indexes the source plugin's master list (else it's the plugin itself).
    public static bool TryParseInfoFormKey(string fileName, IReadOnlyList<ModKey> masters, ModKey esmKey, out FormKey formKey)
    {
        formKey = default;
        var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
        var parts = stem.Split('_');
        if (parts.Length < 4) return false;
        var hex = parts[^2];   // <quest>_<topic>_<FORMID>_<n>
        if (hex.Length != 8 || !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var id)) return false;
        int hi = (int)(id >> 24);
        var modKey = hi >= 0 && hi < masters.Count ? masters[hi] : esmKey;
        formKey = new FormKey(modKey, id & 0x00FFFFFF);
        return true;
    }

    // Build a manifest entry from a resolved INFO. `responseIndex` picks which response line (0-based).
    public static VoiceAnnotation BuildEntry(string clipRelPath, string voiceType, IDialogResponsesGetter info, int responseIndex)
    {
        var resp = responseIndex >= 0 && responseIndex < info.Responses.Count
            ? info.Responses[responseIndex]
            : (info.Responses.Count > 0 ? info.Responses[0] : null);
        string text = "";
        try { text = resp?.Text?.ToString() ?? ""; } catch { text = ""; }   // inline string; defensive
        return new VoiceAnnotation
        {
            Clip = clipRelPath,
            VoiceType = voiceType,
            Text = text,
            Emotion = (resp?.Emotion ?? Emotion.Neutral).ToString(),
            Intensity = (int)(resp?.EmotionValue ?? 0u),
            InfoFormId = $"0x{info.FormKey.ID:X8}",
        };
    }
}
