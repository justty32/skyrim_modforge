namespace ModForge;

// --- Living-NPC population macro (Idea #23, sub_projs/living-adventurers) --------------------
// A high-level macro: declare a small cast of NAMED, PERSISTENT NPCs who live their own off-stage
// lives (an adventurer taking contracts, a merchant running trade routes, a College apprentice…),
// and the generator EXPANDS it into the low-level records + the reusable controller .pex that runs
// the "abstract ghost-sim + materialize on co-location" loop. Same pass-0 macro-expansion model as
// `settlements:`/`skillTrees:` (Generator.ExpandLivingNpcs, before pass 1).
//
// THE PRODUCT IS THE ON-RAMP: adding one living NPC = one small LivingNpcSpec (a ref, an archetype,
// a few anchors) → the macro emits the alias + markers + deed global + (optional) rumor. archetype
// is a fixed branch in MFLivingNpcAlias.psc, so adding an NPC of an EXISTING archetype is pure data.
//
// Materialization is MoveTo-in/out of ONE persistent ref per NPC (named cast) — no LVLN spawn churn.
// Phase 2+ (NOT this MVP): player interaction (poach/hire/parley), real missive task targets (needs
// roadmap #7-9 LocationAlias fill), alignment/hostile parley branches, an anonymous "crowd" tier.
public sealed class LivingNpcsSpec
{
    // In-game hours between abstract "deeds" for every living NPC (the off-stage sim tick cadence).
    public float SimIntervalHours { get; set; } = 4f;
    // Real seconds between presence polls (how often the controller checks player co-location).
    public float PollInterval { get; set; } = 5f;
    // Optional rumor teller: an npc editorId (or vanilla "<master>:0xFORMID") who voices the 傳唱.
    // When set, each NPC with `rumors` gets a topic on this speaker gated on its deed global. Empty -> no rumor.
    public string RumorSpeaker { get; set; } = "";
    public List<LivingNpcSpec> Npcs { get; set; } = new();
}

public sealed class LivingNpcSpec
{
    // The actor: an in-spec npcs[] editorId (the macro places its persistent ref + forced-fills the
    // alias) OR an external follower's ActorRef base "<master>.esp:0xFORMID" (filled via uniqueActor —
    // give that gorgeous standalone follower a life). Required.
    public string Ref { get; set; } = "";
    // Optional display name — used for the rumor topic prompt ("Any word of <Name>?") and readability.
    // For an in-spec NPC the NpcSpec already has a name; this just labels the rumor menu line.
    public string Name { get; set; } = "";
    // Archetype name → MFLivingNpcAlias int branch. adventurer|mageApprentice|merchant|herbalist|priest|bandit.
    public string Archetype { get; set; } = "adventurer";
    // friendly|neutral|hostile — reserved for Phase-2 parley/interaction wiring (recorded now; MVP ignores).
    public string Alignment { get; set; } = "friendly";
    // Author's note / backstory — drives rumor & future dialogue authoring. Not emitted as a record.
    public string Backstory { get; set; } = "";
    // The places this NPC appears (vanilla cells the player visits). The macro creates an xmarker at each
    // and Presence() materialises the NPC at whichever one matches his current abstract location.
    public List<LivingAnchorSpec> Anchors { get; set; } = new();
    // 傳唱 lines spoken by the section's rumorSpeaker once this NPC's deed global >= 1. Empty -> no rumor.
    public List<string> Rumors { get; set; } = new();
}

// One place a living NPC can appear: a vanilla (or in-spec) cell + a position in it.
public sealed class LivingAnchorSpec
{
    public string Cell { get; set; } = "";          // in-spec cell editorId OR "<master>:0xFORMID"
    public Vec3 Position { get; set; } = new();
    public string Kind { get; set; } = "";          // free-text label (inn/jarlHall/college/camp…); doc only
}
