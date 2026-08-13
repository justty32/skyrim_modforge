namespace ModForge;

// --- In-world skill tree (Idea #20) -----------------------------------------------------
// A high-level macro: declare a clickable, in-world perk tree and the generator EXPANDS it into
// the low-level records (globals, node/line activators, placements, MFSkillNode script attach).
// IN-GAME CONFIRMED pattern (2026-06-21) — zero external-mod dependency. The player walks up to a
// floating star, activates it, and (if its prerequisite is owned and a point is available) learns
// the node's `ability`: the star lights up (PlayAnimation "OwnedWild"), the connector line lights
// (PlayAnimation "Unlock"), a point is spent, and the ability is added to the player.
//
// MVP scope = a VERTICAL LINEAR CHAIN: `nodes` are an ordered list stacked bottom→top, each node
// gated on the one below it, connected by a vertical line. (Branching / 2-D layouts need diagonal
// line-orientation calibration that can't be verified offline — a documented future extension.)
//
// ART: the default node/line meshes are Campfire's star/line nifs. They are NOT a master dependency
// — bundle the kit (the 2 .nif + their all-vanilla textures) as loose files via the spec `assets`
// field (see examples/assets/skilltree). Override `nodeModel`/`lineModel` to use your own meshes.
//
// The MFSkillNode.pex (node behaviour) ships automatically with `package` when any skillTree exists.
public sealed class SkillTreeSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    // Where the tree is placed: an in-spec interior cell editorId OR a vanilla interior cell ref
    // "<master>:0xFORMID" (e.g. Skyrim.esm:0x01605E = Whiterun Bannered Mare). Same resolution as a
    // placement's `cell`.
    public string Cell { get; set; } = "";
    // World position of the ROOT (bottom) node inside that cell. Nodes stack upward in +Z from here.
    public Vec3 Origin { get; set; } = new();
    // Vertical gap between adjacent nodes. Default 65 = the calibrated spacing at which the default
    // line mesh fits at scale 1.0 (the IN-GAME-CONFIRMED value). Other values uniformly scale the line.
    public float Spacing { get; set; } = 65f;
    // The shared skill-point pool global. Empty -> the generator auto-creates "<editorId>_Points"
    // seeded with `startingPoints`. Set to an existing in-spec/vanilla GLOB editorId to drive points
    // from elsewhere (e.g. earned via gameplay).
    public string PointsGlobal { get; set; } = "";
    public int StartingPoints { get; set; } = 3;     // initial value when auto-creating the points global
    // Mesh overrides (Data-relative paths). Empty -> the bundled Campfire star / line kit.
    public string NodeModel { get; set; } = "";
    public string LineModel { get; set; } = "";
    public List<SkillNodeSpec> Nodes { get; set; } = new();
}

// One node in the chain. Ordered: node[i] is gated on node[i-1] (node[0] is the always-available root).
public sealed class SkillNodeSpec
{
    public string EditorId { get; set; } = "";   // unique within the tree
    public string Name { get; set; } = "";        // shown on the activate prompt + learn notification
    public string Ability { get; set; } = "";     // ref → a SPEL (in-spec spells[] ability or vanilla) granted on learn
}
