namespace ModForge;

// --- references[]: NAME an EXISTING placed ref so the rest of the spec can point at it -----------
//
// The consumer side of the in-game editor's `sc ref` / `sc refc` REFERRER primitive (Idea #24, plan
// scene-capture-bridge). The player aims at a ref that ALREADY exists — a vanilla chair, or an object
// they placed with `sc pl` — and tags it with a free-form label ("sofia's chair"). ModForge does NOT
// author these refs (they exist already, or they are in this same file's `placements[]`); it consumes
// them as ANCHORS: the `label` becomes a name usable in EVERY "ref" field of the spec (a package's
// sitTarget `target` / travel `place`, a quest alias `forced:`, `linkedRefs`, `enableParent`, an
// objective target, a script Form property …).
//
// 🔴 WHICH SLOT YOU PUT THE LABEL IN DECIDES WHETHER IT LOCKS ONTO THAT ONE OBJECT.
//   SingleRef TARGET slots (patrol.start, follow.target, escort.target, sitTarget.target,
//     activate.target, useMagic.target) → PackageTargetSpecificReference(FormKey): THAT REF, period.
//   LOCATION slots (sandbox.location, sleep.location, travel.place, escort.destination, eat.location,
//     useMagic.location) → LocationTarget + radius: an AREA anchored at that ref's position; the
//     engine then picks whatever furniture/bed/food it likes INSIDE the radius.
// (The full table lives in PackageRefSlots.cs — ONE source of truth, with an anti-rot test; the two
// lists above are a reading aid and may be out of date, the table cannot be.)
// So `sandbox.location: "sofia's chair"` does NOT mean "sit in that chair" — it means "hang around
// where that chair is", and she may sit in a DIFFERENT chair, with NO warning and NO error (builds
// clean, dumps clean, wrong in-game). For "she must use THAT object" always use a SingleRef slot.
// Worked example with a control group: examples/referrer-chair-anchor.json.
// GUARDRAIL: a label landing in a LOCATION slot makes build print an INFO note (BuildResult.Notes —
// never a warning: an area anchor is a legal intent). See Generator.Build.References.cs.
//
// SIBLINGS: `removals[]` (erase existing), `overrides[]` (move existing), `references[]` (NAME
// existing). All three carry an EXISTING ref; none of them creates the thing they point at (the one
// exception is the persistent `anchor` fallback below).
//
// TWO TARGET CLASSES (the load-bearing distinction — plan backlog 🔑「檔內相依關聯」):
//
//   (B) IN-FILE — `ref` is a `placements[]` editorId in THIS spec (the chair the player placed with
//       `sc pl`, which the exporter emitted into placements[] and gave a stable editorId). THE CLEAN
//       PATH: the object is ours, so build FORCES IT PERSISTENT (record flag 0x400 + the cell's
//       Persistent group) — which is exactly what "an alias/package can target this ref" requires.
//       Nothing extra is emitted. Works offline (no master link cache needed).
//
//   (A) EXTERNAL — `ref` is a durable "<master>:0xFORMID" (a vanilla chair). We only name it. THE
//       PERSISTENT TRAP: most vanilla scenery refs live in a cell's TEMPORARY group, and a temporary
//       ref is a poor specific-reference target (the engine can drop it; a quest alias fill / package
//       SingleRef may not hold across save+load). Build looks the ref up in the master link cache and
//       WARNS when it carries no 0x400 flag. `anchor` is the escape hatch:
//         "none" (default) — just name it; you get the warning and decide.
//         "marker"  — author a PERSISTENT XMarkerHeading at the ref's spot and bind the LABEL to the
//                     marker instead. Right when you only need a PLACE (sandbox/travel/patrol anchor).
//         "replace" — author OUR OWN persistent copy of the object (same base/transform) at that spot
//                     and add the vanilla original to `removals[]` (disable + bury, so there is no
//                     duplicate). The label binds to our copy. Right when the anchor must BE THE
//                     OBJECT (sit in THAT chair) — we now own it, so it can be persistent.
//       `base`/`position`/`rotation`/`scale` are what the exporter recorded about the target; the
//       anchor modes need them (they fall back to the master record's values when omitted).
//
// `anchor` is meaningless on an in-file (B) reference — it is already ours and already persistent.
public sealed class ReferenceSpec
{
    public string Ref { get; set; } = "";        // REQUIRED: in-spec placements[] editorId (B) OR "<master>:0xFORMID" (A)
    public string Label { get; set; } = "";      // REQUIRED, unique: the name every other ref field can use
    public string Base { get; set; } = "";       // the target's base form (advisory; used by anchor:"replace")
    public Vec3? Position { get; set; }          // where the target stands (cell-local / world, same contract as PlacementSpec)
    public Vec3? Rotation { get; set; }          // degrees
    public float? Scale { get; set; }            // the target's XSCL (advisory; copied onto an anchor:"replace" copy)
    public string Cell { get; set; } = "";       // interior: in-spec cell editorId OR "<master>:0xFORMID"
    public string Worldspace { get; set; } = ""; // exterior: worldspace ref; position is world-space
    public string Anchor { get; set; } = "";     // "" | "none" | "marker" | "replace" — persistent fallback (external refs only)
    public string Note { get; set; } = "";       // free-form brief for the agent authoring the next round (advisory)
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<ReferenceSpec> References { get; set; } = new(); // NAME an EXISTING placed ref (in-file placements[] editorId, or a vanilla <master>:0xFORMID) so any other ref field can point at it by `label`. The in-game referrer (`sc ref`) feeds this. See Spec.References.cs / Generator.Build.References.cs
}
