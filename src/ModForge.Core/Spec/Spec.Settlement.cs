namespace ModForge;

// --- Settlement population macro (Idea #22, mod-survey 🏘️ #1) ----------------------------
// A high-level macro: declare "a populated settlement" — named residents who LIVE in a cell,
// each with a sleep/work/wander routine anchored to placed refs, optional shop + faction — and
// the generator EXPANDS it into the low-level records every battle-tested build pass already
// handles (NPC packages/factions, ACHR placements, vendor FACT + merchant container). Same
// pass-0 macro-expansion model as `skillTrees:` (Generator.ExpandSettlements, before pass 1).
//
// MVP scope = NAMED RESIDENTS + STATIC ACHR (the deterministic, offline-verifiable quadrant):
// each resident is a unique in-spec NPC placed at a spawn marker, given 2–3 schedule packages
// bound to USER-PLACED anchor refs (bed / workstation / wander marker). No runtime script — the
// whole macro is pure data expansion, so it is fully verifiable offline (no .pex burden).
//
// ANCHOR PHILOSOPHY (mirrors the recipe ironlaw): `home`/`work`/`spawnAt` are editorIds of refs
// the AUTHOR already placed (in the Godot editor or hand-written placements[]). The macro only
// BINDS packages to those anchors — it never conjures abstract sandbox points (a purely abstract
// sandbox = NPC stands idle, three-way confirmed). Place a bed/forge/marker, give it an editorId,
// and the macro wires the routine to it.
//
// Phase 2 (NOT this MVP): `crowd:` anonymous masses (leveled-static / spawn-controller .pex),
// `reaction: flee|fight` (needs a `flee` PACK template), inline npc, RELA friend-net beyond the
// simple `friendlyResidents` flag, advanced per-weekday/seasonal routines.
public sealed class SettlementSpec
{
    public string EditorId { get; set; } = "";
    // Where residents live: an in-spec cell editorId OR a vanilla cell ref "<master>:0xFORMID"
    // (same resolution as a placement's `cell`). All resident ACHR + the merchant chest land here.
    public string Cell { get; set; } = "";
    // The shared "townsfolk" faction every resident joins (locals, mutual standing). Empty -> the
    // macro auto-creates "<editorId>_Faction". Set to an existing in-spec/vanilla FACT to reuse one.
    public string SettlementFaction { get; set; } = "";
    // Optional crime/citizen faction applied to every resident (npc.CrimeFaction) — grants
    // city-traversal rights (e.g. Skyrim.esm:0x0267EA = CrimeFactionWhiterun). Empty -> none.
    public string CrimeFaction { get; set; } = "";
    // Settlement-default daily routine; a resident inherits it unless its own `routine` overrides.
    public RoutineSpec DailyRoutine { get; set; } = new();
    // When true, generate pairwise Friend RELA between residents (they treat each other as friends —
    // won't turn hostile, assist in a brawl). Default OFF: in a large settlement an all-friendly net
    // is N*(N-1) records and rarely wanted. Opt in per settlement.
    public bool FriendlyResidents { get; set; }
    public List<ResidentSpec> Residents { get; set; } = new();
}

// A daily routine = named time windows. Hours are 0..24 (game hours). A window may wrap midnight
// (from > to, e.g. sleep 22->7). Omit a window to skip that behaviour. Whatever time is left over
// becomes the always-on "wander the settlement" sandbox (lowest priority, no schedule).
public sealed class RoutineSpec
{
    public RoutineWindowSpec? Sleep { get; set; }  // -> Sleep package anchored at the resident's `home`
    public RoutineWindowSpec? Work { get; set; }   // -> Sandbox package (small radius) anchored at `work`
}
public sealed class RoutineWindowSpec
{
    public int From { get; set; } = -1;  // start hour (0..24); -1 = unset (window ignored)
    public int To { get; set; } = -1;    // end hour (0..24); -1 = unset
}

public sealed class ResidentSpec
{
    public string Npc { get; set; } = "";       // ref -> an existing npcs[] NpcSpec editorId (the resident)
    public string Home { get; set; } = "";       // ref -> a placed bed REFR editorId (Sleep package anchor)
    public string Work { get; set; } = "";       // ref -> a placed workstation REFR editorId (Work package anchor); optional
    // Where the resident's ACHR spawns. Prefer a placed XMarkerHeading editorId (take its coords —
    // matches the Godot place-a-marker workflow). If empty, `spawnPosition` is used as a fallback.
    public string SpawnAt { get; set; } = "";
    public Vec3? SpawnPosition { get; set; }     // fallback spawn coords when `spawnAt` is not given
    // Optional: makes this resident a shopkeeper (vendor FACT + merchant chest + JobMerchantFaction).
    public SettlementVendorSpec? Vendor { get; set; }
    // Optional per-resident routine override (merged over the settlement's dailyRoutine).
    public RoutineSpec? Routine { get; set; }
}

// Turns a resident into a working merchant. Expands to a Vendor-flagged FACT (the resident joins it)
// + a placed merchant container holding `gold` (the shop's till), wired via the existing Build.Vendor
// path. `sellBuyList` is a FormList ref of VendorItem keywords (vanilla, e.g. Skyrim.esm:0x06CB48 =
// VendorItemsMisc) naming the categories traded.
public sealed class SettlementVendorSpec
{
    public string SellBuyList { get; set; } = ""; // ref -> FormList of VendorItem keywords
    public int StartHour { get; set; } = 8;        // shop opens (0..24)
    public int EndHour { get; set; } = 18;         // shop closes (0..24)
    public int Gold { get; set; } = 500;           // gold placed in the merchant chest (the trade float)
    public bool NotSellBuyList { get; set; }       // sellBuyList is a NOT-sell list (sell everything except)
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<SettlementSpec> Settlements { get; set; } = new(); // populated settlement (Idea #22; Spec.Settlement.cs) — macro-expands to npcs/packages/placements/factions/containers

    // Guard so the settlement macro-expansion (Generator.ExpandSettlements) runs at most once. Not serialized.
    [System.Text.Json.Serialization.JsonIgnore] internal bool SettlementsExpanded { get; set; }
}
