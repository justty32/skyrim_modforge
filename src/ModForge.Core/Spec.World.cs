namespace ModForge;

// --- World: cells, placements, and the linked-reference chains between them --------------

// A new interior cell the plugin creates (reachable in-game via `coc <editorId>`).
// `template` (optional, a vanilla INTERIOR cell ref "<master>:0xFORMID") copies that cell's
// lighting/water environment so a brand-new cell isn't pitch-black; it still needs a floor
// static placed in it (a `placement`) so the player doesn't fall into the void.
public sealed class CellSpec { public string EditorId { get; set; } = ""; public string Name { get; set; } = ""; public string Template { get; set; } = ""; }
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
public sealed class PlacementSpec
{
    public string Base { get; set; } = "";
    public string EditorId { get; set; } = "";     // optional: names this REFR/ACHR so other refs can target it
                                                    // (patrol start, linkedRefs target). Must be unique if set.
    public string Cell { get; set; } = "";        // interior: in-spec editorId OR <master>:0xFORMID
    public string Worldspace { get; set; } = "";   // exterior: worldspace ref; position is world-space
    public string Kind { get; set; } = "";
    public Vec3 Position { get; set; } = new();
    public Vec3 Rotation { get; set; } = new();
    public bool Persistent { get; set; }
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

// --- Worldspace (WRLD) + Region (REGN) ---------------------------------------------------
// "ref" fields here follow the same rule as elsewhere: an in-spec editorId OR an external
// "<master>:0xFORMID". A new WORLDSPACE defines an exterior world (name, climate, water,
// map bounds, parent); a REGION marks an area inside a worldspace and carries the weather
// table that drives which Weather records play there (the hook a Climate/Weather feature
// pairs with). ModForge emits the RECORD layer only — a fully playable exterior still needs
// terrain/LOD/navmesh authored in the Creation Kit (see SPEC.md).

/// <summary>
/// A new exterior worldspace (WRLD). Minimal spec → a valid small world: editorId + name,
/// a climate ref (so the sky/lighting cycle is defined), optional water + parent worldspace,
/// land/water default heights (the flood-fix), and the map camera/bounds data.
/// </summary>
public sealed class WorldspaceSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Climate { get; set; } = "";        // ref → CLMT (e.g. Skyrim.esm:0x000812 = vanilla default climate)
    public string Water { get; set; } = "";           // ref → WATR (optional; e.g. Skyrim.esm:0x000018 = DefaultWater)
    public string LodWater { get; set; } = "";        // ref → WATR for distant LOD water (optional)
    public string Parent { get; set; } = "";          // ref → parent WRLD (optional; child inherits climate/water unless overridden)
    public string InteriorLighting { get; set; } = ""; // ref → LGTM (optional)
    public string Location { get; set; } = "";        // ref → LCTN (optional)
    public string Music { get; set; } = "";           // ref → MUSC (optional)
    public string EncounterZone { get; set; } = "";   // ref → ECZN (optional)

    // Worldspace.Flag names: SmallWorld, CannotFastTravel, NoLodWater, NoLandscape, NoSky,
    // FixedDimensions, NoGrass. A small custom world is usually "SmallWorld" (uses an in-memory
    // grid rather than streamed BSA cells).
    public List<string> Flags { get; set; } = new();

    // Land/water DEFAULTS. CRITICAL: omitting these defaults DefaultWaterHeight to 0 — and any
    // terrain below 0 then floods ("whole world is underwater"). Vanilla Tamriel uses
    // land=-27000, water=-14000. Defaults below mirror Tamriel so a minimal world isn't flooded.
    public float DefaultLandHeight { get; set; } = -27000f;
    public float DefaultWaterHeight { get; set; } = -14000f;

    // Map-menu data. Cell coords bound the world map; the camera fields frame the local-map view.
    // Defaults are a small sane window (a 9×9 cell box centred on origin) — override for a bigger world.
    public WorldMapDataSpec Map { get; set; } = new();
}

/// <summary>Worldspace map-menu bounds + local-map camera (the WNAM/MNAM data).</summary>
public sealed class WorldMapDataSpec
{
    // Inclusive cell-grid corners of the map. NW is top-left (min X, max Y), SE is bottom-right.
    public int NorthwestX { get; set; } = -4;
    public int NorthwestY { get; set; } = 4;
    public int SoutheastX { get; set; } = 4;
    public int SoutheastY { get; set; } = -4;
    public int UsableWidth { get; set; } = 0;   // 0 = engine derives from the cell corners (vanilla Tamriel uses 0)
    public int UsableHeight { get; set; } = 0;
    public float CameraInitialPitch { get; set; } = 50f;
    public float CameraMinHeight { get; set; } = 50000f;
    public float CameraMaxHeight { get; set; } = 80000f;
}

/// <summary>
/// A region (REGN): an area inside a worldspace plus its weather table. The Weather entries
/// are the piece that lets a custom Climate/Weather drive an area — each is a Weather ref with
/// a chance (0–100); the chances are relative weights the engine picks from. Map color shows on
/// the world map. Other RegionData entries (sound/objects/grass/land) are CK-side and omitted.
/// </summary>
public sealed class RegionSpec
{
    public string EditorId { get; set; } = "";
    public string Worldspace { get; set; } = "";       // ref → the WRLD this region lives in (in-spec or vanilla)

    // Polygon outline of the area, in WORLD units (not cell grid). Need ≥3 points to enclose
    // an area. EdgeFallOff is the blend distance (units) at the region border.
    public List<PointSpec> Area { get; set; } = new();
    public uint EdgeFallOff { get; set; } = 0;

    // Weather table: ≥1 entry, each a Weather ref + chance. Chances are relative weights; their
    // sum must be > 0. Priority orders overlapping regions (higher wins). This is the climate hook.
    public byte WeatherPriority { get; set; } = 50;
    public List<RegionWeatherEntrySpec> Weather { get; set; } = new();

    // Map color as 0xRRGGBB (e.g. "0x1DC90A"). Optional — colors the region on the world map.
    public string MapColor { get; set; } = "";
}

/// <summary>One weather slot in a region's weather table: a Weather (WTHR) ref + a chance weight.</summary>
public sealed class RegionWeatherEntrySpec
{
    public string Weather { get; set; } = "";   // ref → WTHR (e.g. Skyrim.esm:0x10E1F0 = SkyrimClear)
    public int Chance { get; set; } = 100;        // relative weight 0–100
    public string Global { get; set; } = "";      // optional ref → GLOB that scales the chance
}

/// <summary>A 2-D world-space point (used for a region's area polygon).</summary>
public sealed class PointSpec
{
    public float X { get; set; }
    public float Y { get; set; }
}
