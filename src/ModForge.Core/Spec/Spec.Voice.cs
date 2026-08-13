namespace ModForge;

// --- Voice Cloning: templates, engines, and output settings ----------------------------

public sealed class VoiceTemplateSpec
{
    public string Id { get; set; } = "";             // unique name for this template
    public string Engine { get; set; } = "f5";        // f5 | chatterbox | gptsovits | xtts | fish-s2
    public string ReferenceWav { get; set; } = "";    // path to a zero-shot reference clip (rel. to spec)
    public string ReferenceText { get; set; } = "";   // required transcript for some engines (f5)
    public string ModelPath { get; set; } = "";       // optional: path to a fine-tuned model directory
    public string RvcModel { get; set; } = "";        // optional: path to an RVC model for timbre stabilization
    public string Language { get; set; } = "en";
    public int? Seed { get; set; }                     // for deterministic output
    public float? Exaggeration { get; set; }          // engine-specific emotion scale
    public float? Speed { get; set; }                 // playback speed adjustment (e.g. 0.8 for slower)
}

public sealed class VoiceLineSpec
{
    public bool SkipLip { get; set; }                 // true = emit zero-size .lip (static mouth)
    public string Format { get; set; } = "fuz";      // fuz | wav | xwm
}

// Bind an EXTERNAL dialogue speaker — an NPC from another master that the mod-only link cache can't
// resolve (e.g. an existing follower like Sofia) — to a voiceType + voiceTemplate. Without this, a
// line gated on GetIsID(<external NPC>) has an unresolvable speaker, so `voicelines`/`voicediag`
// can't tell which voiceType folder to put the file in or which template to clone with. `speaker` is
// the NPC ref the line's GetIsID gates on (`<master>:0xFORMID`); `voiceType` is that NPC's voiceType
// (the folder name under Sound/Voice/<plugin>/); `template` is a voiceTemplates[] id to generate with.
public sealed class VoiceSpeakerSpec
{
    public string Speaker { get; set; } = "";
    public string VoiceType { get; set; } = "";
    public string Template { get; set; } = "";
}
