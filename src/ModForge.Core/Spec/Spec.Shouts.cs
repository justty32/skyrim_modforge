namespace ModForge;

// --- Dragon shouts: Shout (SHOU) + the three Words of Power (WOOP) it is built from -------

// WordOfPower (WOOP): one of the three words a dragon shout is built from. `translation` is the
// dragon-tongue romanization the game shows on a word wall and in the shouts menu (e.g. "Fus");
// `name` is the readable display the menu uses for the word (often the same text). A word carries
// NO behaviour itself — the behaviour comes from the Spell that the SHOU pairs it with. A custom
// shout references three words by `editorId` (define them here, in any order — the SHOU's word
// rows decide the unlock order).
public sealed class WordOfPowerSpec
{
    public string EditorId { get; set; } = "";
    public string Translation { get; set; } = "";   // dragon-tongue romanization shown in-game (WNAM)
    public string Name { get; set; } = "";            // readable display name (TNAM); defaults to Translation when empty
}
// One of a Shout's three word rows: the WordOfPower unlocked at this tier (`word` ref → WOOP), the
// Spell cast when the shout is used at this word count (`spell` ref → a Voice SPEL), and the
// cooldown in seconds before it can be used again (`recoveryTime`, >= 0). Tier 0 = the 1-word
// shout, tier 1 = 2-word, tier 2 = 3-word (longer charge, bigger effect, longer recovery).
public sealed class ShoutWordSpec
{
    public string Word { get; set; } = "";       // ref → WordOfPower (in-spec editorId or <master>:0xFORMID)
    public string Spell { get; set; } = "";       // ref → SPEL (a Voice-cast spell that delivers the effect)
    public float RecoveryTime { get; set; }        // seconds of cooldown after using this tier (>= 0)
}
// Shout (SHOU): a dragon shout. References exactly three Words of Power (`words[]`), each paired
// with a delivering Spell + recovery time. `menuDisplayObject` (optional, ref → a STAT/MSTT) is the
// 3D object the shouts menu shows for the shout. NOTE: this builds the RECORDS — the player still
// has to LEARN the words (word walls / teachword / unlockword) via a quest or script for the shout
// to be usable; that is out of scope for the record set (see docs).
public sealed class ShoutSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string MenuDisplayObject { get; set; } = "";   // ref → STAT/MSTT shown in the shouts menu (optional)
    public List<ShoutWordSpec> Words { get; set; } = new();
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<WordOfPowerSpec> WordsOfPower { get; set; } = new();

    public List<ShoutSpec> Shouts { get; set; } = new();
}
