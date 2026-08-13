namespace ModForge;

// AnimObject Swapper (AOS) config spec DTOs.
// Produces a LOOSE FILE (no esp record): <File>_ANIO.ini (AOS scans Data/ for every *_ANIO.ini).
// AOS swaps the ANIO (the prop an actor holds during an idle — a mug, a book) by condition, with no
// ESP override — used for follower/role characterization (Sofia holds a special mug while drinking).
// Swaps the HELD OBJECT, not the animation (that's OAR). Syntax verified against AOS
// (sub_projs/mod-survey/findings/animobject-swapper-*.md):
//   [BaseANIO|FILTERS|TRAITS]
//   baseANIO|swap1,swap2,swap3        (AOS picks one at random per idle)
// FILTERS/TRAITS header segments are optional; trailing empty segments are trimmed.
public sealed class AnimObjectSwapSpec
{
    public string File { get; set; } = "";                   // output stem → <File>_ANIO.ini
    public List<AnimObjectSwapEntrySpec> Entries { get; set; } = new();
}

public sealed class AnimObjectSwapEntrySpec
{
    // The original ANIO to swap: "0xFormID~Plugin.esp" or an EditorID.
    public string Base { get; set; } = "";
    // Replacement ANIO(s); several → AOS picks one at random per idle invocation.
    public List<string> Swaps { get; set; } = new();
    // Optional FILTERS (header segment 2): NPC base/Faction/Race/Keyword/Spell/Location by ref;
    // "+x" AND-requires, "-x" excludes, "*x" string-match. Empty → unconditional.
    public List<string> Filters { get; set; } = new();
    // Optional TRAITS (header segment 3): "M"/"F" sex, "C"/"-C" child. Empty → any.
    public string Traits { get; set; } = "";
}
