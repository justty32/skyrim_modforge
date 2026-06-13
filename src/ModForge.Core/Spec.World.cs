namespace ModForge;

// --- World: cells, placements, and the linked-reference chains between them --------------

// A new interior cell the plugin creates (reachable in-game via `coc <editorId>`).
// `template` (optional, a vanilla INTERIOR cell ref "<master>:0xFORMID") copies that cell's
// lighting/water environment so a brand-new cell isn't pitch-black; it still needs a floor
// static placed in it (a `placement`) so the player doesn't fall into the void.
public sealed class CellSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Template { get; set; } = ""; public string EncounterZone { get; set; } = ""; public string LightingTemplate { get; set; } = ""; public string ImageSpace { get; set; } = ""; public string Music { get; set; } = ""; public CellLightingSpec? Lighting { get; set; } } // lightingTemplate/imageSpace: in-spec editorId OR vanilla <master>:0xFORMID; music: ref → MUSC; lighting: inline XCLL overrides
public sealed class Vec3 { public float X { get; set; } public float Y { get; set; } public float Z { get; set; } }
// Place a base form (npc/object, in-spec or external) into the world at a position/rotation.
// TWO targeting modes:
//   * INTERIOR: set `cell` to an in-spec interior cell editorId (It.7d-p1) OR a vanilla interior
//     cell ref "<master>:0xFORMID" (It.7d-p2). `position` is local to that cell.
//   * EXTERIOR: set `worldspace` to a worldspace ref "<master>:0xFORMID" (e.g. Tamriel =
//     Skyrim.esm:0x00003C, find via `find <Skyrim.esm> <name> Worldspace`); `position` is the
//     WORLD position. The cell at floor(x/4096),floor(y/4096) is found in the master and
//     overridden to add this ref (It.7d-p3). `worldspace` wins over `cell` if both are set.
// `rotation` is in degrees. `kind` ("npc"|"object") is inferred for in-spec bases, "object" else.
// LEVELED-ACTOR SPAWN: `base` may be a LeveledNpc (LVLN) list — in-spec or vanilla — instead of a
// concrete NPC. The placed ACHR then rolls a level-appropriate actor from that list at load (the
// dungeon-population pattern). A leveled-npc base is treated as `kind: "npc"` automatically; pair it
// with the cell's / this ref's `encounterZone` to control the level range it rolls within.
//
// MARKERS (XMarker / XMarkerHeading): these are first-class placement bases — the invisible
// staging refs vanilla uses as Travel destinations, patrol nodes, scene marks and `coc` targets.
// Place one with `base` = the vanilla marker static (XMarker = Skyrim.esm:0x00003B, XMarkerHeading
// = Skyrim.esm:0x000034 — heading carries a facing direction), give it an `editorId`, and it can be
// targeted by Travel `place`, Patrol `start`, the `linkedRefs` route chain, escort `destination`,
// etc. GOTCHA: a marker REFR does NOT snap to the floor the way an actor does, so anchor it on
// coords PROVEN walkable (`refpos <plugin> <0xFORMID>` to copy a reachable vanilla ref) or inside a
// hand-navmeshed interior — a guessed exterior z lands off-navmesh and pathing silently fails.
//
// LOAD DOORS (teleport pair): a `placement` whose `base` is a DOOR record (a *load* door, e.g.
// FarmhouseLDoor01 = Skyrim.esm:0x029CB0) and whose `teleport` names the PARTNER door placement
// links two cells — the player walks through one door and arrives at the other. Author TWO door
// placements, each `teleport`-pointing at the other (typically in different cells). Build writes
// each door's TeleportDestination (XTEL): partner door FormKey + the partner's position/rotation
// (where the player materialises). Partner may be another in-spec door OR a vanilla door ref
// "<master>:0xFORMID". Doors with a teleport are forced persistent (the engine must keep the link).
public sealed class PlacementSpec
{
    public string Base { get; set; } = "";
    public string EditorId { get; set; } = "";     // optional: names this REFR/ACHR so other refs can target it
                                                    // (patrol start, linkedRefs target, teleport partner). Must be unique if set.
    public string Cell { get; set; } = "";        // interior: in-spec editorId OR <master>:0xFORMID
    public string Worldspace { get; set; } = "";   // exterior: worldspace ref; position is world-space
    public string Kind { get; set; } = "";
    public Vec3 Position { get; set; } = new();
    public Vec3 Rotation { get; set; } = new();
    public bool Persistent { get; set; }
    // Load-door teleport: the PARTNER door this door teleports to (a placement editorId, or a vanilla
    // door ref "<master>:0xFORMID"). Set on BOTH doors of the pair (each pointing at the other) to
    // make a walk-through link. `base` must be a DOOR record. The arrival point is the partner's
    // position/rotation, written into this door's XTEL automatically.
    public string Teleport { get; set; } = "";
    // Optional encounter-zone ref (in-spec ECZN editorId or vanilla <master>:0xFORMID) for THIS
    // placed ref (its XEZN). A per-ref override of the cell's zone — usually leave empty and let the
    // ref inherit the cell's encounterZone, but set it to scope a single spawn to its own zone.
    public string EncounterZone { get; set; } = "";
    // Linked References on this placed ref. Each points to another placement (by its editorId) or
    // a vanilla placed ref, optionally tagged with a keyword. With no keyword, the link is the
    // engine's "default" linked ref — which is what a Patrol route follows from marker to marker.
    public List<LinkedRefSpec> LinkedRefs { get; set; } = new();
}
// One Linked Reference: `target` is the linked placed ref (a placement editorId or external ref);
// `keyword` (optional ref → KYWD) tags the link. Empty keyword = the null/default link.
public sealed class LinkedRefSpec
{
    public string Target { get; set; } = "";
    public string Keyword { get; set; } = "";
}
// EncounterZone (ECZN): controls level scaling + respawn for an area. A cell's `encounterZone`
// (and/or a placed spawn's `encounterZone`) points at one; the engine rolls leveled-actor spawns
// in that zone to a level inside [minLevel, maxLevel]. `minLevel`/`maxLevel` are 0–255 (a single
// byte each); maxLevel 0 = "no upper cap" (vanilla idiom — e.g. HelgenZone is min 6 / max 0, scaling
// up with the player forever). `owner` (optional ref → FACT or NPC) ties the zone to a faction/actor
// for crime/ownership; `location` (optional ref → LCTN) links it to a map location. `flags`:
//   NeverResets               — the zone never respawns its actors/loot (cleared dungeons stay cleared)
//   MatchPcBelowMinimumLevel  — if the player is below minLevel, spawns match the PLAYER (else clamp to minLevel)
//   DisableCombatBoundary     — actors may chase the player out of the zone
public sealed class EncounterZoneSpec
{
    public string EditorId { get; set; } = "";
    public int MinLevel { get; set; }              // 0–255; the floor of the spawn level range
    public int MaxLevel { get; set; }              // 0–255; 0 = uncapped (scales with the player)
    public string Owner { get; set; } = "";        // optional ref → FACT/NPC owner
    public string Location { get; set; } = "";     // optional ref → LCTN
    public int Rank { get; set; }                  // owner rank (0 if no owner)
    public List<string> Flags { get; set; } = new(); // EncounterZone.Flag names (see above)
}
