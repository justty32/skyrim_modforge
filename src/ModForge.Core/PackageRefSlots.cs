namespace ModForge;

// --- WHICH SLOT A REF GOES INTO DECIDES WHETHER IT LOCKS ONTO THAT ONE OBJECT -------------------
//
// Every string field of a package template's sub-spec that takes a ref, classified. This is the ONE
// source of truth for the distinction that `references[]` (Idea #24 referrer) lives or dies on:
//
//   SingleRef     → PackageTargetSpecificReference(FormKey): the engine acts on THAT REF, no other.
//   Location      → LocationTarget(FormKey) + radius: an AREA anchored at that ref's position — the
//                   engine then picks whatever furniture/bed/food it likes INSIDE the radius.
//   NotAPlacedRef → a string slot that is not a placed-ref anchor at all (a base form; an enum name).
//
// So `sandbox.location: "sofia's chair"` does NOT mean "sit in that chair" — it means "hang around
// where that chair is", and she may sit in a DIFFERENT chair, with no warning and no error (builds
// clean, dumps clean, wrong in-game). BuildReferences uses this table to print an INFO line whenever
// a `references[]` label lands in a Location slot (Generator.Build.References.cs) — the label says
// "I care about THIS object", the slot says "any object around here", and only the author can say
// which was meant. Nothing here changes what is built.
//
// EVERY SingleRef/Location row below is filled by a DEFERRED wire (deferredTargetWires /
// deferredLocationWires → WireDeferredTargets / WireDeferredLocations), never inside BuildPackageData:
// that step runs before BuildPlacements/BuildReferences, so an eager resolve can only see base records and
// silently misses in-file placement editorIds and references[] labels. A new template's ref slot MUST be
// deferred too. (NotAPlacedRef rows are base forms/enums and do resolve eagerly.)
//
// ANTI-ROT: PackageRefSlotsTests reflects over every sub-spec of PackageSpec and fails if any string
// field is missing from this table — so a NEW package template cannot quietly add an unclassified
// ref slot. Add the template's slots here in the same commit that adds its Apply*Data builder.
internal enum PackageSlotKind
{
    SingleRef,
    Location,
    NotAPlacedRef,
}

/// <param name="Path">The spec path an author writes, e.g. "sandbox.location".</param>
/// <param name="Kind">What the builder turns a ref in this slot into.</param>
/// <param name="Get">Reads the slot's raw ref string off a PackageSpec.</param>
/// <param name="Radius">The radius that pairs with a Location slot (null for the other kinds).</param>
internal sealed record PackageRefSlot(
    string Path,
    PackageSlotKind Kind,
    Func<PackageSpec, string> Get,
    Func<PackageSpec, uint>? Radius = null);

internal static class PackageRefSlots
{
    // Slot numbers/names in the comments are the vanilla template's (see PackageTemplates.cs and the
    // Apply*Data builders in Generator.Build.Packages*.cs, which are what these entries mirror).
    public static readonly IReadOnlyList<PackageRefSlot> All = new PackageRefSlot[]
    {
        // --- SingleRef target slots → PackageTargetSpecificReference: THAT ref, period ------------
        new("patrol.start",     PackageSlotKind.SingleRef, p => p.Patrol.Start),      // slot 0  Patrol Start
        new("follow.target",    PackageSlotKind.SingleRef, p => p.Follow.Target),     // slot 0  Target to Follow
        new("escort.target",    PackageSlotKind.SingleRef, p => p.Escort.Target),     // slot 11 Target to Escort
        new("sitTarget.target", PackageSlotKind.SingleRef, p => p.SitTarget.Target),  // slot 16 Target (the furniture)
        new("activate.target",  PackageSlotKind.SingleRef, p => p.Activate.Target),   // slot 0  Target (the object)
        new("useMagic.target",  PackageSlotKind.SingleRef, p => p.UseMagic.Target),   // slot 4  Target (who to cast on)

        // --- location slots → LocationTarget + radius: an AREA around that ref --------------------
        new("sandbox.location",   PackageSlotKind.Location, p => p.Sandbox.Location,    p => p.Sandbox.Radius),   // slot 0 Location
        new("sleep.location",     PackageSlotKind.Location, p => p.Sleep.Location,      p => p.Sleep.Radius),     // slot 0 Sleep Location
        new("travel.place",       PackageSlotKind.Location, p => p.Travel.Place,        p => p.Travel.Radius),    // slot 0 Place to Travel
        new("escort.destination", PackageSlotKind.Location, p => p.Escort.Destination,  p => p.Escort.Radius),    // slot 3 Destination
        new("eat.location",       PackageSlotKind.Location, p => p.Eat.Location,        p => p.Eat.Radius),       // slot 0 Eat Location
        new("useMagic.location",  PackageSlotKind.Location, p => p.UseMagic.Location,   p => p.UseMagic.Radius),  // slot 2 Location

        // --- string fields that are NOT placed-ref anchors (listed so the anti-rot test can tell
        //     "classified as not-a-ref" from "someone forgot to classify it") ----------------------
        new("useMagic.spell",     PackageSlotKind.NotAPlacedRef, p => p.UseMagic.Spell),      // a SPEL BASE form (PackageTargetObjectID), never a REFR
        new("schedule.dayOfWeek", PackageSlotKind.NotAPlacedRef, p => p.Schedule.DayOfWeek),  // an enum name ("Any"/"Weekdays"/…), not a ref
        new("editorId",           PackageSlotKind.NotAPlacedRef, p => p.EditorId),
        new("template",           PackageSlotKind.NotAPlacedRef, p => p.Template),            // the vanilla PACK procedure template (a base form)
        new("preferredSpeed",     PackageSlotKind.NotAPlacedRef, p => p.PreferredSpeed),      // an enum name (Walk/Jog/Run/FastWalk)
        new("combatStyle",        PackageSlotKind.NotAPlacedRef, p => p.CombatStyle),         // CSTY base form
        new("ownerQuest",         PackageSlotKind.NotAPlacedRef, p => p.OwnerQuest),          // QUST base form
    };

    public static IEnumerable<PackageRefSlot> OfKind(PackageSlotKind kind) => All.Where(s => s.Kind == kind);

    /// <summary>The SingleRef slots, comma-joined — what to tell an author to use instead of a location slot.</summary>
    public static readonly string SingleRefPaths =
        string.Join(", ", All.Where(s => s.Kind == PackageSlotKind.SingleRef).Select(s => s.Path));
}
