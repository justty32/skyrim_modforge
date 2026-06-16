namespace ModForge;

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

    // Flat terrain cells to generate. Each gets a CELL + LAND record (height=0, flat normals) placed
    // in the worldspace's SubCell block tree. Enter in-game with: cow <editorId> X Y
    public List<WorldspaceCellSpec> Cells { get; set; } = new();

    // 非平坦地形（可選）。存在時忽略 Cells，依 PNG 尺寸自動衍生 cell grid 並生起伏 LAND。
    // 與 Cells 互斥；兩者都填時 heightmap 優先且 build 發 warn。null = 走平坦 Cells 路徑（行為不變）。
    public HeightmapSpec? Heightmap { get; set; }

    // Godot 4 物件擺放（可選）。指向 Godot HTerrain plugin 匯出的 placements.json；
    // ModForge 將 godot4_y_up 座標系轉換後合流進 placements[] pipeline，效果等同手寫 placements。
    // OriginX/Y 必須與 heightmap 的一致（決定 Godot 原點對應的 cell 網格座標）。
    public GodotPlacementsSpec? GodotPlacements { get; set; }
}

/// <summary>
/// Godot 4 HTerrain plugin 匯出的物件擺放 JSON。格式：
/// { "version":1, "coordinate_system":"godot4_y_up", "placements":[...] }
/// position 單位公尺、rotation 單位 radians；ModForge 換算成 Skyrim game units + degrees。
/// OriginX/Y = Godot 場景左下角（西南角）對應的 Skyrim cell 網格座標（與 heightmap 一致）。
/// </summary>
public sealed class GodotPlacementsSpec
{
    public string Path { get; set; } = "";   // placements.json 路徑，相對 spec 檔
    public int OriginX { get; set; }          // Godot 場景左下角對到的 cell X（與 heightmap OriginX 一致）
    public int OriginY { get; set; }          // 同上 Y
}

/// <summary>
/// 一張覆蓋整個 worldspace 部分區域的 16-bit grayscale PNG heightmap。ModForge 依尺寸切成
/// N×M 個 cell（寬必須 = N×32+1、高 = M×32+1，相鄰格共用邊緣欄 → seam 零誤差）。
/// 高度 = MinHeight + (png/65535)×(MaxHeight−MinHeight)，game units。
/// </summary>
public sealed class HeightmapSpec
{
    public string Path { get; set; } = "";       // PNG 路徑，相對 spec 檔
    public int OriginX { get; set; }              // PNG 左下角像素對到的 cell 座標 X
    public int OriginY { get; set; }              // 同上 Y（左下=西南角；影像往上 = cell +Y/北）
    public float MinHeight { get; set; }          // png=0     → 此高度（game units）
    public float MaxHeight { get; set; } = 4000f; // png=65535 → 此高度
}

/// <summary>
/// A flat terrain cell inside a new worldspace. Adds a CELL + LAND (flat terrain at Z=0) so the
/// player can enter via <c>cow &lt;worldspace editorId&gt; X Y</c> without falling through the void.
/// X and Y are exterior cell-grid coordinates (1 unit = 4096 game units); (0,0) is the origin.
/// </summary>
public sealed class WorldspaceCellSpec
{
    public int X { get; set; }
    public int Y { get; set; }
    // Terrain height in game units. Offset stored as Height/8 in VHGT (Skyrim's scale factor).
    // Default 4000 puts terrain ~280m above sea level (Z=0), safely above water. 0 = sea level.
    public float Height { get; set; } = 4000f;
    // Generate a flat navmesh (NAVM) for this cell so NPCs can path on it.
    // A 4-vertex quad (2 triangles) covering the full 4096×4096 cell is emitted; a NAVI index
    // entry is created for cross-cell path queries. No edge-links to neighbors — pathfinding
    // works within the cell but NPCs cannot cross to adjacent cells without matching neighbor NAVMs.
    public bool Navmesh { get; set; }
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
