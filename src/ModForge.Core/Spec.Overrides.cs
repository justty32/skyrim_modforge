namespace ModForge;

// Transform override of an EXISTING placed ref (Idea #24 P4 editor — "move that vanilla wall").
// Each entry re-stamps position/rotation (and optionally scale) on a ref some OTHER plugin
// authored, via GetOrAddAsOverride — the sibling of `removals[]` (remove existing / move
// existing live side by side; both resolve through the master link cache).
//
// Shape decision (2026-07-11, spec §「既有 ref 的 override 形狀」): a TOP-LEVEL list, NOT a
// `placements[].overrideOf` field — PlacementSpec's other members (base/teleport/lock/owner/
// linkedRefs/...) are all meaningless on an override, and an override needs no cell/worldspace
// attribution (the resolved context brings its parent chain along). See the spec for the full
// rationale.
//
// SEMANTICS: `position`/`rotation` (degrees) are REQUIRED — the new full transform, not a delta.
// `scale` is OPTIONAL: null/omitted = keep whatever scale the original record has; 1.0 = reset
// to default (XSCL dropped). The in-game editor always emits the live scale explicitly.
public sealed class OverrideSpec
{
    public string Ref { get; set; } = "";        // "<master>:0xFORMID" of the existing placed ref (REFR/ACHR)
    public Vec3 Position { get; set; } = new();
    public Vec3 Rotation { get; set; } = new();  // degrees, same contract as PlacementSpec
    public float? Scale { get; set; }            // null = keep original; 1.0 = explicit default
    public string Label { get; set; } = "";      // short human label for the moved thing. Inert documentation only — the build never reads this
    public string Note { get; set; } = "";       // free-form note (why it was moved). Inert documentation only — the build never reads this
}
