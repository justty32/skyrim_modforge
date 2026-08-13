namespace ModForge;

// Base Object Swapper (BOS, powerofthree, Nexus 49669) config spec DTOs.
// Produces a LOOSE FILE (no esp record): <File>_SWAP.ini (BOS scans Data/ for every *_SWAP.ini).
// BOS replaces a base object with another when a reference is loaded, with no ESP override — used
// for scene dressing (swap a vanilla clutter form for a richer one, gate by location/keyword).
// Syntax verified against BOS (sub_projs/mod-survey/findings/base-object-swapper-config.md):
//   [Forms]                         (or [Forms|cond1,cond2] for a conditional group)
//   baseFormID|swapFormID[|properties][|chance]
// MVP = the [Forms] section (single/random swap, optional transform string + chance, optional
// per-group conditions). The standalone [Properties]/[References] sections are out of scope.
public sealed class ObjectSwapSpec
{
    public string File { get; set; } = "";                  // output stem → <File>_SWAP.ini
    public List<ObjectSwapGroupSpec> Groups { get; set; } = new();
}

// One [Forms] section. `conditions` (optional) become the "[Forms|c1,c2]" filter — Location/Region/
// Keyword/Cell/Worldspace by EditorID/FormID; "-x" excludes. Empty → an unconditional "[Forms]".
public sealed class ObjectSwapGroupSpec
{
    public List<string> Conditions { get; set; } = new();
    public List<ObjectSwapEntrySpec> Entries { get; set; } = new();
}

// baseFormID|swapFormID[|properties][|chance]
public sealed class ObjectSwapEntrySpec
{
    // The base form to replace: "0xFormID~Plugin.esp" or an EditorID.
    public string Base { get; set; } = "";
    // Replacement form(s): one, or several (BOS picks one at random per reference).
    public List<string> Swaps { get; set; } = new();
    // Optional transform string (raw passthrough), e.g. "scale(1.2/1.2),rot(0/0,0/0,45/45)".
    public string Properties { get; set; } = "";
    // Optional 0.0–100.0 swap chance. Null → omitted (BOS defaults to 100 = always).
    public double? Chance { get; set; }
}
