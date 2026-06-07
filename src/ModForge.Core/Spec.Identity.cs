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
    public bool Toggle { get; set; }                    // reading the acquire book again removes the identity
    public bool Default { get; set; }                   // every player holds this from the start (baseline)
    public IdentityAcquireSpec? OnAcquire { get; set; } // optional performance played when acquired

    public string AcquireBook { get; set; } = "";       // ref → a BOOK whose OnRead grants/toggles this identity
    public string AcquireText { get; set; } = "";       // optional yes/no MessageBox prompt shown on read
}

// Optional acquire-time performance.
public sealed class IdentityAcquireSpec
{
    public string Scene { get; set; } = "";   // ref → a scene (editorId) started when the identity is acquired
}
