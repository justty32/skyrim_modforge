namespace ModForge;

// WordWall: the LEARNABLE layer that turns a record-correct SHOU (see Spec.Shouts.cs) into a usable
// shout. It emits (a) a teaching QUEST whose generated Papyrus fragment calls
// Game.GetPlayer().AddShout(shout) + TeachWord(word), and (b) a WordWallTrigger ACTIVATOR placement
// at `cell`/`worldspace`+`position` the player walks into to fire it. `wordIndex` (1|2|3) picks which
// of the shout's three words is taught. The generated .psc is written to Scripts/Source by `package`;
// it must be CK-compiled (UNCONFIRMED in-game — see docs). The word-wall GLOW VFX is a separate
// CK/mesh concern.
public sealed class WordWallSpec
{
    public string EditorId { get; set; } = "";           // editorId of the teaching quest (must be unique)
    public string Name { get; set; } = "";               // quest display name (optional)
    public string Shout { get; set; } = "";              // ref → SHOU (in-spec or vanilla) the wall teaches
    public int WordIndex { get; set; } = 1;              // 1|2|3 — which of the shout's three words to teach
    public string Word { get; set; } = "";               // optional explicit WOOP ref; defaults to shout.words[wordIndex-1].word for in-spec shouts
    public string ScriptName { get; set; } = "";         // generated Papyrus script name; defaults to "<editorId>Script"
    // Where the trigger is placed. Same two modes as a placement (interior cell ref OR worldspace).
    public string TriggerEditorId { get; set; } = "";    // optional editorId for the placed trigger REFR
    public string TriggerBase { get; set; } = "";        // optional ACTI ref to place; defaults to vanilla WordWallTrigger (Skyrim.esm:0x05095E)
    public string Cell { get; set; } = "";               // interior: in-spec editorId OR <master>:0xFORMID
    public string Worldspace { get; set; } = "";         // exterior: worldspace ref; position is world-space
    public Vec3 Position { get; set; } = new();
    public Vec3 Rotation { get; set; } = new();
}
