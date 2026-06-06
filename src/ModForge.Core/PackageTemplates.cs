namespace ModForge;

// The vanilla AI-package PROCEDURE TEMPLATES (PACK records in Skyrim.esm) the generator knows how to
// fill Data slots for. ONE source of truth — referenced by both the builder
// (Generator.Build.Packages.cs, which dispatches slot-filling per template) and the validator
// (Generator.Validate.cs, which warns on missing required inputs per template) so these FormIDs can
// never drift apart. Discover any template's named slot schema with `packagediag <Skyrim.esm> <id>`.
//
// Why not the Mutagen.Bethesda.FormKeys.SkyrimSE library: its current builds target Mutagen 3.x while
// this project is pinned to Mutagen 0.53.1 — a heavy, version-mismatched dependency for seven constants.
// A spec author still names a template by external ref ("Skyrim.esm:0x01C254") in JSON; these symbols
// are only for the generator's own internal matching.
internal static class PackageTemplates
{
    private static readonly ModKey Skyrim = ModKey.FromNameAndExtension("Skyrim.esm");
    private static FormKey Of(uint id) => new(Skyrim, id);

    public static readonly FormKey Sandbox  = Of(0x01C254);  // 12 slots
    public static readonly FormKey Sleep    = Of(0x019717);  // 14 author slots (+ fixed bed-search 1/2/6/8)
    public static readonly FormKey Travel   = Of(0x016FAA);  //  3 slots
    public static readonly FormKey UseMagic = Of(0x0504F5);  // 11 active slots (2–12)
    public static readonly FormKey Patrol   = Of(0x017723);  //  6 slots
    public static readonly FormKey Follow   = Of(0x019B2C);  //  6 slots
    public static readonly FormKey Escort   = Of(0x023B73);  // 15 slots (9 active)
    public static readonly FormKey SitTarget = Of(0x0A9277); //  3 author slots (16/3/4) — sit at furniture
    public static readonly FormKey Activate = Of(0x019B2D);  //  2 author slots (0 Target, 2 Number)
    public static readonly FormKey Eat      = Of(0x019714);  // location sandbox-variant (fixed food/chair search)
}
