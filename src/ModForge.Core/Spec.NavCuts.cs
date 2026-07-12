using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModForge;

// --- navCuts[]: cut vanilla navmesh at RUNTIME with an L_NAVCUT collision volume ---------------
//
// The problem (navmesh plan, symptom ①): you place a house / wall / boulder into a vanilla cell.
// The vanilla navmesh under it is untouched, so the engine still believes that ground is walkable
// and NPCs walk straight INTO your building. Fixing that by editing the NAVM record is expensive
// and risky (triangle indices are positional and the neighbouring cells hold your indices).
//
// The cheap fix is the one Bethesda itself uses: a NAVCUT volume. One placed REFR whose base is
// the engine's hardcoded CollisionMarker (Skyrim.esm:0x000021), whose Havok collision layer is 49
// (L_NAVCUT) and which carries an XPRM box Primitive. At runtime the engine switches OFF every
// navmesh triangle inside that box — no NAVM edit, no NAVI edit, no NIF, no navmesh conflict.
// HearthFires.esm places 1003 of them (that IS the "build a house" dynamic navcut system) and
// Skyrim.esm another 441. VERIFIED against those records (2026-07-12): base 0x000021,
// CollisionLayer = 49, Primitive{Type=Box, Color=(255,255,0), Unknown=0.15}.
//
// 🔴 THE TWO-STAGE GATE (the thing that silently wastes days): the `Obstacle` record flag (bit 25)
// on its own does NOTHING. The engine only cuts navmesh for a collision object whose COLLISION
// LAYER's COLL record carries the NavmeshObstacle flag. Of the 55 vanilla COLL layers only six do
// — L_ANIMSTATIC(2), L_CLUTTER(4), L_PROPS(10), L_DEBRIS_LARGE(20), L_TRANSPARENT_SMALL_ANIM(28)
// and L_NAVCUT(49). **L_STATIC(1) is NOT one of them**, and ordinary statics (houses, walls,
// rocks) collide on L_STATIC. So "clone a vanilla STAT and set the Obstacle flag" cannot work.
// L_NAVCUT is the layer to use, which is why this primitive exists at all.
//
// ⚠️ Four engine limits (CK wiki), all of which the defaults here are built around:
//   1. The engine tests the actor as a ZERO-VOLUME POINT against the box. Leave a gap and an NPC
//      squeezes through it shoulder-first. → the box is INFLATED OUTWARD by `padding` (default 32,
//      about half an actor's width) on X/Y. Do not set padding to 0 on a real obstacle.
//   2. It only applies in the cell the PLAYER is in. Off-screen NPCs teleport along their package
//      as usual; navcuts do not affect them.
//   3. It only affects paths STARTED AFTER the volume switched on. An NPC already walking a path
//      through the box finishes walking through it.
//   4. The volume is still a collision object — but the CollisionMarker base has no physical
//      collision of its own, so it is invisible and non-blocking. (Only a hand-made navcut NIF
//      would need Mass = 0.)
//
// ⚠️ 🎮 STATUS: the mechanism is vanilla-proven and the record shape is byte-verified, but ModForge
// authoring it has NOT been confirmed in-game yet — that is T2.0 in workflows/plans/navmesh.md.
//
// Two ways to author one:
//   * EXPLICIT box — `position` (the box CENTRE) + `size` + a cell or worldspace.
//   * FROM A PLACEMENT — `placement: "<editorId>"`; the centre and size come from that placement's
//     base OBND (scaled by its `scale`, rotated by its `rotationZ`). Same thing PlacementSpec.NavCut
//     does automatically, exposed for when you want to hand-tune one.
//
// `size` is the FULL box size (width × depth × height in game units), NOT half-extents — verified
// against vanilla: HearthFires 00410D's box is 116×52.8×46.9 around a chest whose OBND is 96×49×48.
// ModForge writes it straight into XPRM Bounds. The box is centred on `position` in all three axes,
// so put `position.z` at the MIDDLE of the volume (the vanilla idiom is to centre it on the floor
// so the box straddles the navmesh — see the `placement` path, which does exactly that).
public sealed class NavCutSpec
{
    public string EditorId { get; set; } = "";     // optional: names the REFR
    public string Cell { get; set; } = "";         // interior: in-spec cell editorId OR vanilla <master>:0xFORMID
    public string Worldspace { get; set; } = "";   // exterior: worldspace ref; `position` is world-space
    public string Placement { get; set; } = "";    // convenience: take centre+size from this placement's OBND
    public Vec3? Position { get; set; }            // box CENTRE (cell-local for interiors, world for exteriors)
    public Vec3? Size { get; set; }                // FULL box size (w, d, h) BEFORE padding
    public float RotationZ { get; set; }           // degrees, about Z (the box's yaw)
    public float? Padding { get; set; }            // outward X/Y inflation; null = navmesh.padding (32)
}

// --- placements[].navCut: per-placement control of the AUTO navcut -----------------------------
//
// RULING (2026-07-12, user): "both" — a large blocking placement gets a navcut box AUTOMATICALLY,
// and you can turn that off or hand-tune the box. Hence the three JSON shapes:
//
//   (field omitted)                         → AUTO: cut iff the placement is "blocking" (its base
//                                             OBND clears navmesh.minFootprint + navmesh.minHeight)
//                                             AND it actually covers vanilla navmesh.
//   "navCut": false                         → never cut this one (a fake wall, a backdrop, scenery
//                                             an NPC is *supposed* to be able to walk through).
//   "navCut": true                          → cut it even if it is below the auto thresholds.
//   "navCut": { "size": …, "offset": …,     → cut it with a hand-tuned box (any field you omit
//               "padding": … }                falls back to the auto-derived value).
//
// The auto path needs the base's OBND, which lives in the master — so it can only fire where the
// master link cache is available. On an offline machine (no Skyrim.esm) NOTHING is auto-cut and the
// build is unchanged; that is deliberate, not a degradation to work around.
[JsonConverter(typeof(PlacementNavCutConverter))]
public sealed class PlacementNavCutSpec
{
    public bool Enabled { get; set; } = true;   // false = never cut this placement
    public Vec3? Size { get; set; }             // FULL box size override; null = derive from OBND
    public Vec3? Offset { get; set; }           // shift the box centre relative to the placement
    public float? Padding { get; set; }         // outward X/Y inflation; null = navmesh.padding
}

// Accepts `"navCut": false` / `true` / `{ … }` — the bool forms are the ergonomic 90% case and
// System.Text.Json will not bind a bool to a class without this.
public sealed class PlacementNavCutConverter : JsonConverter<PlacementNavCutSpec>
{
    public override PlacementNavCutSpec? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType is JsonTokenType.True or JsonTokenType.False)
            return new PlacementNavCutSpec { Enabled = r.TokenType == JsonTokenType.True };
        if (r.TokenType == JsonTokenType.Null) return null;
        // Object form: deserialize without this converter (else infinite recursion).
        var inner = new JsonSerializerOptions(o);
        for (int i = inner.Converters.Count - 1; i >= 0; i--)
            if (inner.Converters[i] is PlacementNavCutConverter) inner.Converters.RemoveAt(i);
        using var doc = JsonDocument.ParseValue(ref r);
        return doc.RootElement.Deserialize<PlacementNavCutBody>(inner) is { } b
            ? new PlacementNavCutSpec { Enabled = b.Enabled, Size = b.Size, Offset = b.Offset, Padding = b.Padding }
            : null;
    }

    public override void Write(Utf8JsonWriter w, PlacementNavCutSpec v, JsonSerializerOptions o)
    {
        if (v.Size is null && v.Offset is null && v.Padding is null) { w.WriteBooleanValue(v.Enabled); return; }
        w.WriteStartObject();
        w.WriteBoolean("enabled", v.Enabled);
        if (v.Size is { } s) { w.WritePropertyName("size"); JsonSerializer.Serialize(w, s, o); }
        if (v.Offset is { } f) { w.WritePropertyName("offset"); JsonSerializer.Serialize(w, f, o); }
        if (v.Padding is { } p) w.WriteNumber("padding", p);
        w.WriteEndObject();
    }

    // Plain mirror of PlacementNavCutSpec, free of the converter attribute.
    private sealed class PlacementNavCutBody
    {
        public bool Enabled { get; set; } = true;
        public Vec3? Size { get; set; }
        public Vec3? Offset { get; set; }
        public float? Padding { get; set; }
    }
}

// --- navmesh: the knobs for the navmesh diagnostics + the auto navcut --------------------------
//
// "Blocking" = a placement whose base OBND has a footprint of at least `minFootprint` square units
// AND is at least `minHeight` tall. A house / wall / boulder clears both; a chair (60×60×100 →
// 3600 units²) or a barrel (54×54×80) does not, which is what keeps clutter from being cut and
// keeps the diagnostics from drowning you in noise.
public sealed class NavmeshSpec
{
    public bool Warnings { get; set; } = true;        // P1: warn about off-navmesh NPCs / uncut obstacles
    public bool AutoNavCuts { get; set; } = true;     // auto-cut blocking placements (user ruling 2026-07-12)
    public float MinFootprint { get; set; } = 10000f; // units² (100 × 100) — OBND XY area to count as blocking
    public float MinHeight { get; set; } = 100f;      // units — OBND height to count as blocking
    public float Padding { get; set; } = 32f;         // outward X/Y inflation of every box (≈ half an actor)

    // "This in-spec cell has no navmesh at all, so an NPC in it cannot path." TRUE — and true of EVERY
    // ModForge interior, because we cannot author interior navmesh yet (that is P3 of the navmesh plan).
    // A warning that fires on 100% of custom interiors is noise, not a diagnostic, so it is OFF by
    // default: it is a roadmap fact, not a mistake in your spec. Turn it on if you want the reminder.
    // The geometric checks (an NPC standing off the navmesh of a VANILLA cell, a placement covering
    // uncut vanilla navmesh) stay on — those ARE per-spec mistakes, and they are the ones the in-game
    // editor's output can actually hit.
    public bool WarnEmptyCells { get; set; }
}
