namespace ModForge;

// A lightweight "identity / class" the player can hold (Paladin, Merchant, Adventurer…). Each is a
// FACTION (the persistent has-it signal — survives saves, future-proofs vanilla GetInFaction gating)
// plus a priority (which identity wins as the "primary" greeting) and optional grants/acquire.
//
// Three faces (see the design doc): Acquire (how you get it — a book OnRead), Gate (what it unlocks —
// `identity`/`primaryIdentity` tags on dialogue → GetInFaction CTDA), Grant (standing abilities given
// on join, removed on leave). Primary identity is resolved purely from data (priority + GetInFaction
// exclusion) — no controller script.
public sealed class IdentitySpec
{
    public string Id { get; set; } = "";              // the tag name used by identity/primaryIdentity gates
    public string Faction { get; set; } = "";          // ref → the holding FACT: a bare in-spec editorId
                                                       // (built if absent) or an external <master>:0xID
    public int Priority { get; set; }                  // higher wins as the primary greeting identity
    public List<string> Grants { get; set; } = new();  // refs → SPELs/abilities added on join, removed on leave
    public List<string> GrantPerks { get; set; } = new(); // refs → PERKs added on join, removed on leave (e.g. a
                                                       // conditional "smite vs undead" perk). The acquire book binds
                                                       // the FIRST (like grants[0]); a default identity grants all.
    public bool Toggle { get; set; }                    // reading the acquire book again removes the identity
    public bool Default { get; set; }                   // every player holds this from the start (baseline)
    public List<ConditionSpec> ActiveWhen { get; set; } = new();  // optional situational gate: the identity only
                                                       // counts as ACTIVE (for identity/primaryIdentity gates) while
                                                       // these CTDA pass — e.g. WornHasKeyword(heavy armor),
                                                       // GetBaseActorValue(Speech)>=X, GetRelationshipRank(npc)>=Y.
                                                       // Player-centric: each condition runs on the PLAYER unless it
                                                       // sets its own runOn. NOTE: activeWhen NARROWS the positive
                                                       // gate only — a held-but-inactive higher identity is still
                                                       // excluded from LOWER primaryIdentity greetings on its faction
                                                       // signal alone (negating a condition bundle isn't expressible
                                                       // in CTDA), so it can fall through to the plain greeting until
                                                       // the Phase-2 controller resolves primary by activeWhen too.
    public IdentityAcquireSpec? OnAcquire { get; set; } // optional performance played when acquired
    public IdentityAutoGrantSpec? AutoGrantWhen { get; set; } // optional: auto-join this identity's faction once the
                                                       // player's ActorValue crosses a threshold (e.g. Dragonborn when
                                                       // DragonSouls ≥ 1). A poll controller grants the FACTION only.

    public string AcquireBook { get; set; } = "";       // ref → a BOOK whose OnRead grants/toggles this identity
    public string AcquireText { get; set; } = "";       // optional yes/no MessageBox prompt shown on read
}

// Optional acquire-time performance.
public sealed class IdentityAcquireSpec
{
    public string Scene { get; set; } = "";   // ref → a scene (editorId) started when the identity is acquired
}

// Optional auto-grant trigger: a poll controller joins the identity's faction once the player's ActorValue
// reaches the threshold (read in Papyrus via Actor.GetActorValue(name) — vanilla, no SKSE). E.g. Dragonborn
// on DragonSouls ≥ 1 (your first absorbed dragon soul). Grants the FACTION signal only (not abilities/perks).
public sealed class IdentityAutoGrantSpec
{
    public string ActorValue { get; set; } = "";   // the ActorValue name (e.g. "DragonSouls")
    public float Threshold { get; set; } = 1f;       // grant once GetActorValue(name) >= this
}
