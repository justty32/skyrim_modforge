namespace ModForge;

// --- Actors: NPCs, factions, and the relationships between them -------------------------

public sealed class NpcSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> Factions { get; set; } = new();
    public string Race { get; set; } = "";       // ref (e.g. Skyrim.esm:0x013746 = NordRace)
    public string Class { get; set; } = "";       // ref
    public string Outfit { get; set; } = "";      // ref -> DefaultOutfit
    public int Level { get; set; }                 // fixed level (0 = leave default); needed for class stat auto-calc
    public bool AutoCalcStats { get; set; }        // derive H/M/S + skills from level + class (else flat defaults)
    public List<string> Packages { get; set; } = new(); // refs to PACK records (in-spec or external) — assigned to this NPC's package list
    public string VoiceType { get; set; } = "";      // ref → VTYP (e.g. Skyrim.esm:0x013AE6 = MaleNord); without one, NPC is silent (no hello/idle chatter)
    public string CrimeFaction { get; set; } = "";   // ref → FACT (e.g. Skyrim.esm:0x0267EA = CrimeFactionWhiterun); marks the NPC as a member of a city's crime/citizen circle — grants city-traversal rights (without it, cross-cell Travel through city gates is silently rejected)
    public bool Unique { get; set; }                  // Configuration.Flag.Unique — engine treats the actor as a one-off (vs leveled spawn); seems to matter for AI tracking + cross-cell travel
    public List<string> Spells { get; set; } = new(); // refs → SPEL records; populates npc.ActorEffect — the AI's spell list, what combat AI considers casting (combined with combatStyle's magic preference)
    public string CombatStyle { get; set; } = "";    // ref → CSTY; HOW the AI fights (magic vs melee preference, aggression, group flank). Without one, the engine uses a default that may not pick spells from `spells`.
    // AIData — controls WHETHER the NPC fights at all (separate system from CombatStyle which is
    // HOW). Mutagen-generated NPCs default to Aggression=Unaggressive + Confidence=Cowardly which
    // means they FLEE from any threat, regardless of CombatStyle or spell list. For a combatant set
    // at minimum Aggression=Aggressive (defends when attacked) + Confidence=Brave (doesn't flee).
    public string Aggression { get; set; } = "";     // Unaggressive|Aggressive|VeryAggressive|Frenzied (default: Unaggressive — won't initiate, won't defend either)
    public string Confidence { get; set; } = "";     // Cowardly|Cautious|Average|Brave|Foolhardy (default: Cowardly — flees any threat)
    public string Assistance { get; set; } = "";     // HelpsNobody|HelpsAllies|HelpsFriendsAndAllies (default: HelpsNobody)
    public string Mood { get; set; } = "";           // Neutral|Angry|Fear|Happy|Sad|Surprised|Puzzled|Disgusted
    public int EnergyLevel { get; set; }              // 0..100 — vanilla actors typically 50
    // Greeting (Hello) line. When this NPC is the speaker of any custom `dialogue[]`, Build auto-emits
    // a Hello topic (Category=Misc, Subtype=Hello, SNAM='HELO') gated on GetIsID(this NPC). This is
    // what makes the NPC CONVERSABLE — without a Hello, activating the NPC never opens the dialogue
    // menu, so the player topics never surface (you just get voicetype mumbles). Empty => a neutral
    // default line is used so the NPC still works.
    public string Greeting { get; set; } = "";
}
public sealed class FactionSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; }
// Relationship (RELA): a directed bond between two NPCs (`parent` and `child`) at a `rank`. The
// player's NPC *base* record is `Skyrim.esm:0x000014` (NOT `0x000007`, which is PlayerRef — the
// placed ACHR; pointing a RELA at it is a type mismatch that CRASHES on load). `child` defaults to
// `0x000014`, so the common case (an NPC's relationship TO the player) is just `parent` + `rank`.
// Rank (RankType): Lover, Ally, Confidant,
// Friend, Acquaintance, Rival, Foe, Enemy, Archnemesis. **Why it matters for followers:** the vanilla
// DialogueFollower quest's free "Follow me, I need your help" topic is gated on
// `GetRelationshipRank player >= Ally`, so a custom hireable follower needs an Ally relationship to
// the player (plus membership in PotentialFollowerFaction `Skyrim.esm:0x05C84D`).
public sealed class RelationshipSpec
{
    public string EditorId { get; set; } = "";
    public string Parent { get; set; } = "";                  // ref → NPC (the relationship's owner); usually the custom NPC
    public string Child { get; set; } = "Skyrim.esm:0x000014"; // ref → NPC; defaults to the Player NPC base (0x000014, NOT PlayerRef 0x000007)
    public string Rank { get; set; } = "Ally";                // RankType enum name
}
