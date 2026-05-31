namespace ModForge;

// --- Dialogue: quests, topics, CTDA conditions, and Papyrus script attachment -----------

public sealed class QuestSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<ObjectiveSpec> Objectives { get; set; } = new();
    // StartGameEnabled (default true): the quest auto-starts on game load, which is REQUIRED for any
    // dialogue it hosts to be loaded/evaluated. A quest that never runs = its dialogue never surfaces.
    public bool StartGameEnabled { get; set; } = true;
    public byte Priority { get; set; } = 50;   // higher wins when multiple quests offer dialogue to the same NPC
}
public sealed class ObjectiveSpec { public ushort Index { get; set; } public string Text { get; set; } = ""; }
// A dialogue topic: shown under QuestEditorId's branch; targets SpeakerNpcEditorId (GetIsID).
public sealed class DialogueSpec
{
    public string EditorId { get; set; } = "";
    public string QuestEditorId { get; set; } = "";
    public string SpeakerNpcEditorId { get; set; } = "";
    public string Prompt { get; set; } = "";
    public List<string> Responses { get; set; } = new();
    public string Emotion { get; set; } = "Neutral";   // Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise — applied to all response lines
    public uint EmotionValue { get; set; } = 50;        // 0..100 intensity
    // Result fragment — Papyrus that runs when the line is PICKED (the INFO's OnEnd fragment). This is
    // the only way to *do* something on a dialogue choice (take gold, join the follower system, set a
    // stage). ResultScript is the fragment's Scriptname (must `Extends TopicInfo` and define
    // `Function Fragment_0(ObjectReference akSpeakerRef)`); ResultScriptSource is the .psc for `package`
    // to compile; ResultProperties bind that script's Auto properties (same shape as a ScriptAttachSpec).
    public string ResultScript { get; set; } = "";
    public string ResultScriptSource { get; set; } = "";
    public List<PropertySpec> ResultProperties { get; set; } = new();
    // Goodbye closes the dialogue menu after this line — vanilla recruit/dismiss lines all set it.
    public bool Goodbye { get; set; }
    // Extra CTDA gates on the INFO (beyond the auto GetIsID speaker gate). e.g. only show a paid
    // recruit line when the player can afford it and isn't already following.
    public List<ConditionSpec> Conditions { get; set; } = new();
}
// A CTDA condition (a static gate) usable on a dialogue INFO or an AI package. `function` picks the
// condition function; `param` is its form argument (a ref → faction/item/global/quest/npc); `comparison`
// + `value` are the numeric test; `runOn`/`reference` pick WHOSE value is read (Subject = the
// speaker/package owner; Reference = a named ref such as the player 0x14). `or` OR-chains with the next.
// Supported functions: GetInFaction, GetItemCount, GetGlobalValue, GetStage, GetIsID, GetRelationshipRank,
// GetActorValue (uses `actorValue` instead of `param`).
public sealed class ConditionSpec
{
    public string Function { get; set; } = "";
    public string Comparison { get; set; } = ">=";   // == | != | > | >= | < | <=
    public float Value { get; set; }
    public string Param { get; set; } = "";           // the function's form argument (a ref)
    public string ActorValue { get; set; } = "";       // the ActorValue name for GetActorValue (e.g. WaitingForPlayer)
    public string RunOn { get; set; } = "Subject";     // Subject | Target | Reference | CombatTarget | ...
    public string Reference { get; set; } = "";        // the ref read when RunOn=Reference (e.g. player Skyrim.esm:0x000014)
    public bool Or { get; set; }                        // OR with the NEXT condition (default AND)
}
// Attach a compiled Papyrus script (by Scriptname) to a record (by editorId), with
// typed properties. type ∈ int|float|bool|string|object; object resolves ObjectEditorId.
public sealed class ScriptAttachSpec
{
    public string TargetEditorId { get; set; } = "";
    public string ScriptName { get; set; } = "";
    public string Source { get; set; } = "";   // optional .psc path (rel. to spec) for `package` to compile
    public List<PropertySpec> Properties { get; set; } = new();
}
public sealed class PropertySpec
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int Int { get; set; }
    public float Float { get; set; }
    public bool Bool { get; set; }
    public string Str { get; set; } = "";
    public string ObjectEditorId { get; set; } = "";
}
