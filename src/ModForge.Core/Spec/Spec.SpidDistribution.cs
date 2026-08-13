namespace ModForge;

// SPID (Spell Perk Item Distributor) distribution config spec DTOs.
// Produces a LOOSE FILE (no esp record): <File>_DISTR.ini at the mod folder root (= Data/).
// SPID reads every *_DISTR.ini at start-up and attaches records to NPC actorbases by filter,
// with no ESP patch — the standard compatibility layer for follower/NPC mods.
// Syntax verified against the SPID 7.3 reference + real ini files
// (sub_projs/mod-survey/findings/spid.md).
public sealed class SpidDistributionSpec
{
    public string File { get; set; } = "";              // output filename stem → <File>_DISTR.ini
    public List<SpidEntrySpec> Entries { get; set; } = new();
}

// One distribution line:
//   Type = RecordID|StringFilters|FormFilters|LevelFilters|Traits|TypeParam|Chance
// Trailing NONE fields are trimmed on emit. RecordID is mandatory (cannot be NONE).
public sealed class SpidEntrySpec
{
    // Distribution type — Spell|Perk|Item|Shout|LevSpell|Package|Outfit|SleepOutfit|Keyword|DeathItem|Faction|Skin.
    // SleepOutfit/Skin must be explicit (SPID can't infer them from the form type).
    public string Type { get; set; } = "";
    // RecordID (field 1, required): "0xFormID~Plugin.esp" or an EditorID. Skyrim/DLC may drop the ~plugin suffix.
    public string Record { get; set; } = "";
    // StringFilters (field 2): keyword/actorbase EditorID/display name. Joined by ',' (OR);
    // '-x' excludes, '*x' partial-match, 'a+b' requires both (AND within one expression).
    public List<string> StringFilters { get; set; } = new();
    // FormFilters (field 3): Faction/Race/Class/CombatStyle/Outfit/NPC_/Spell/VoiceType/FormList FormID|EditorID.
    // Joined by ',' (OR); '-x' excludes. No wildcards here.
    public List<string> FormFilters { get; set; } = new();
    // LevelFilters (field 4): raw passthrough — actor range "25/50" / "10/" / "/40", or skill "14(50/100)".
    public string LevelFilters { get; set; } = "";
    // Traits (field 5): raw passthrough — letters joined by '/' (M/F/U/S/C/L/T), '-' negates (e.g. "M/U", "-C").
    public string Traits { get; set; } = "";
    // TypeParam (field 6): Item → item count; Package → package insert index. Ignored for other types.
    public int? Count { get; set; }
    public int? PackageIndex { get; set; }
    // Chance (field 7): 0–100 distribution chance (non-unique NPCs only). Null → omitted (SPID defaults to 100).
    public int? Chance { get; set; }
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<SpidDistributionSpec> SpidDistributions { get; set; } = new(); // SPID _DISTR.ini (loose)
}
