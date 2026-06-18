namespace ModForge;

// --- JContainers JFormDB persistence (Idea #20 in-world skill tree, Phase 0 persistence layer) -------
//
// A dialogue line can carry `persist` (write nested per-Form state) and/or `syncPerks` (apply perks
// from stored ranks) — both emit Papyrus into the line's TIF result fragment (see
// Generator.JContainers.cs). The Form key the state hangs on is the NPC you're talking to ("speaker")
// or the player.
//
// Lifecycle (answers design unknown U5): only the root-DB PATH API (JFormDB.solveXxxSetter / solveXxx)
// is exposed. JContainers owns those roots and persists them with the save, so there is NO
// JValue.object()/retain()/release() handle to balance — the retain/release footgun is avoided by
// construction, not by careful pairing.

// One `persist` block: a set of nested JFormDB writes keyed on a Form, performed when the line is picked.
public sealed class PersistSpec
{
    // JFormDB storageName — the namespace bucket; becomes the first path component (".<storage><path>").
    // e.g. "ModForgeNpcSkills". One storage holds every NPC's entry, keyed by the NPC Form.
    public string Storage { get; set; } = "";
    // The Form the nested state hangs on: "speaker" (akSpeakerRef — the NPC you're talking to) or
    // "player" (Game.GetPlayer()). Both are ObjectReference/Actor, which extend Form. Default "speaker".
    public string Key { get; set; } = "speaker";
    // The writes performed, in order. Each sets one leaf at `path` under the storage.
    public List<PersistEntrySpec> Set { get; set; } = new();
}

// One JFormDB write. `path` is the subpath UNDER the storage (e.g. ".Endurance.nodes.Adaptation"); it is
// concatenated after ".<storage>". Exactly one of int/float/str/form carries the value — the emitter
// picks solveIntSetter / solveFltSetter / solveStrSetter / solveFormSetter. `delta` (int/float only)
// switches to read-add-write so a counter can accumulate (e.g. combat XP → ratio) without a literal.
public sealed class PersistEntrySpec
{
    public string Path { get; set; } = "";
    public int? Int { get; set; }
    public float? Float { get; set; }
    public string? Str { get; set; }            // null = not a string entry; "" = set to empty string
    public string Form { get; set; } = "";       // ref → a Form value (solveFormSetter), bound as a property
    public bool Delta { get; set; }              // int/float: add to the current stored value instead of replace
}

// One `syncPerks` block (design §四 SyncPerks): idempotent perk application from JFormDB node ranks.
// For each node the fragment reads its rank and AddPerk (rank >= minRank) / RemovePerk (below) on the
// key actor, so an NPC's perks track its stored skill state. Safe to run on every pick.
public sealed class SyncPerksSpec
{
    public string Storage { get; set; } = "";      // JFormDB storageName (same bucket as the persist writes)
    public string Key { get; set; } = "speaker";   // actor whose perks are synced ("speaker" | "player")
    public List<SyncPerkNodeSpec> Nodes { get; set; } = new();
}

// One perk-sync node. `path` is the subpath to the node's stored rank int (read with solveInt). `perk`
// (ref → PERK) is added when the stored rank >= `minRank`, removed below it. Perks bind as Form
// properties on the TIF script.
public sealed class SyncPerkNodeSpec
{
    public string Path { get; set; } = "";
    public string Perk { get; set; } = "";
    public int MinRank { get; set; } = 1;
}
