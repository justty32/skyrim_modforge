namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  Word wall / shout teaching — the LEARNABLE layer.
    //
    //  A custom SHOU + 3 WOOP is record-correct but the player can never USE the shout
    //  until its words are LEARNED. Vanilla does this at a word wall: a WordWallTrigger
    //  activator the player walks into fires a script that grants the shout + teaches the
    //  word. We mirror that: emit a teaching QUEST + a generated Papyrus fragment that
    //  calls Game.GetPlayer().AddShout(shout) + .TeachWord(word), attach it to the quest
    //  (VMAD), and place a WordWallTrigger activator that — once its instance script is
    //  CK-wired to start the quest — fires the learning.
    //
    //  HONEST LIMITS (cannot be confirmed here, no Skyrim / no Papyrus compiler):
    //   * The generated .psc is a COMPILE-READY SCAFFOLD. The CK compile + property
    //     binding (Shout/Word object props) and the in-game learning are UNCONFIRMED.
    //   * The word-wall GLOW VFX (the blue word lighting up on the wall) is a CK/mesh
    //     concern (an Imagespace-modifier + a custom word activator mesh), NOT emitted.
    // -------------------------------------------------------------------------------

    /// <summary>The vanilla <c>WordWallTrigger</c> activator (Skyrim.esm:0x05095E) — the default
    /// base placed for a word wall when a spec doesn't override <c>triggerBase</c>.</summary>
    internal const string VanillaWordWallTrigger = "Skyrim.esm:0x05095E";

    /// <summary>The Scriptname used for a word wall's generated fragment when none is given.</summary>
    internal static string WordWallScriptName(WordWallSpec ww) =>
        string.IsNullOrWhiteSpace(ww.ScriptName) ? ww.EditorId + "Script" : ww.ScriptName;

    /// <summary>
    /// Generate a compile-ready Papyrus quest-fragment scaffold for a word wall. On first activation
    /// (a one-shot guard) it grants the shout and teaches the word to the player — exactly the
    /// learn-a-word effect a word wall has. The Shout/Word object properties are filled in the ESP
    /// VMAD by Build; the CK only needs to compile this against its sources.
    /// </summary>
    public static string GenerateWordWallScript(WordWallSpec ww)
    {
        var scriptName = WordWallScriptName(ww);
        var wordOrdinal = ww.WordIndex switch { 1 => "first", 2 => "second", 3 => "third", _ => "first" };
        return $@"Scriptname {scriptName} extends Quest
{{ModForge: word-wall teaching fragment — grants {ww.Shout} + teaches its {wordOrdinal} word.
  GENERATED SCAFFOLD: must be compiled by the Creation Kit. In-game learning is UNCONFIRMED.}}

; The shout this wall teaches, and the single Word of Power it unlocks (word {ww.WordIndex} of 3).
; Both are bound as VMAD object properties by ModForge — do not clear them in the CK.
Shout Property WordWallShout Auto
WordOfPower Property WordWallWord Auto

; One-shot guard so re-entering the trigger doesn't re-run the learning.
Bool taught = false

; Call this from the WordWallTrigger instance's OnTriggerEnter (filter to the player), or start
; this quest from the trigger. AddShout makes the shout appear in the menu; TeachWord marks the
; word known (UnlockWord, gated on a dragon soul, makes it usable at that charge level).
Function TeachFromWall()
    If taught
        Return
    EndIf
    taught = true
    Actor player = Game.GetPlayer()
    If WordWallShout
        player.AddShout(WordWallShout)
    EndIf
    If WordWallWord
        Game.TeachWord(WordWallWord)
        Game.UnlockWord(WordWallWord)
    EndIf
    Debug.Notification(""Word learned: "" + WordWallWord)
EndFunction

Event OnInit()
    ; Auto-start hook: when the trigger starts this quest, OnInit fires the learning once.
    TeachFromWall()
EndEvent
";
    }

    private sealed partial class BuildContext
    {
        private int wordWallsBuilt;

        // --- pass 2: word-wall teaching fragment (VMAD). Attach the generated <ScriptName> to each ---
        // word wall's quest with two object properties — WordWallShout (→ SHOU) and WordWallWord
        // (→ WOOP) — that the generated .psc reads to AddShout + TeachWord. The quest's VMAD is a
        // QuestAdapter (concrete). The .psc itself is emitted by `package` (GenerateWordWallScript).
        public void AttachWordWallScripts()
        {
            foreach (var ww in spec.WordWalls)
            {
                if (string.IsNullOrWhiteSpace(ww.EditorId)) continue;
                if (!recordsByEd.TryGetValue(ww.EditorId, out var rec) || rec is not IQuest quest)
                { Warn($"  ! wordWall '{ww.EditorId}': teaching quest record missing — script not attached"); continue; }

                quest.VirtualMachineAdapter ??= new QuestAdapter();
                var entry = new ScriptEntry { Name = WordWallScriptName(ww) };

                // Shout object property — required; without it AddShout is a no-op.
                if (TryResolveRef(ww.Shout, formKeyByEd, out var shoutFk))
                {
                    var sp = new ScriptObjectProperty { Name = "WordWallShout", Flags = ScriptProperty.Flag.Edited };
                    sp.Object.SetTo(shoutFk);
                    entry.Properties.Add(sp);
                    linksWired++;
                }
                else Warn($"  ! wordWall '{ww.EditorId}': shout ref '{ww.Shout}' unresolved — WordWallShout property unset");

                // Word object property — explicit `word`, else the shout's word at `wordIndex` (in-spec only).
                var wordRef = ww.Word;
                if (string.IsNullOrWhiteSpace(wordRef))
                {
                    var sh = spec.Shouts.FirstOrDefault(s => string.Equals(s.EditorId, ww.Shout, StringComparison.OrdinalIgnoreCase));
                    int wi = Math.Clamp(ww.WordIndex, 1, 3) - 1;
                    if (sh is not null && wi < sh.Words.Count) wordRef = sh.Words[wi].Word;
                }
                if (!string.IsNullOrWhiteSpace(wordRef) && TryResolveRef(wordRef, formKeyByEd, out var wordFk))
                {
                    var wp = new ScriptObjectProperty { Name = "WordWallWord", Flags = ScriptProperty.Flag.Edited };
                    wp.Object.SetTo(wordFk);
                    entry.Properties.Add(wp);
                    linksWired++;
                }
                else Warn($"  ! wordWall '{ww.EditorId}': word ref unresolved (word/wordIndex) — WordWallWord property unset");

                quest.VirtualMachineAdapter.Scripts.Add(entry);
                scriptsAttached++;
            }
        }

        // Word walls built (placed triggers) — read by ToResult into BuildStats.WordWalls.
        public int WordWallsBuilt => wordWallsBuilt;
    }
}
