using Mutagen.Bethesda.Plugins;

namespace ModForge;

// 一個 SM 事件的定義：原版事件根、Quest.Event 碼、可用的 event-data 槽位（slot 名 → 4-byte 索引）。
public readonly record struct StoryEventDef(FormKey Root, RecordType Code, IReadOnlyDictionary<string, byte[]> Slots);

// 內建「事件名 → 定義」表。一個事件一筆；之後加事件 = 加一筆（值離線從 Skyrim.esm vanilla 解出）。
public static class StoryManagerEvents
{
    private static readonly ModKey Skyrim = ModKey.FromNameAndExtension("Skyrim.esm");
    private static FormKey Root(uint id) => new(Skyrim, id);

    // Vanilla event-data slot indices (decoded from Skyrim.esm). "R1"/"R2" = ref slots,
    // "L1"/"L2" = location slots. Engine fills the matching ref on the alias at runtime.
    private static readonly byte[] R1 = { 0x52, 0x31, 0x00, 0x00 }; // "R1"
    private static readonly byte[] R2 = { 0x52, 0x32, 0x00, 0x00 }; // "R2"
    private static readonly byte[] L1 = { 0x4C, 0x31, 0x00, 0x00 }; // "L1"
    private static readonly byte[] L2 = { 0x4C, 0x32, 0x00, 0x00 }; // "L2"

    private static Dictionary<string, byte[]> Slots(params (string, byte[])[] pairs)
    {
        var d = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in pairs) d[k] = v;
        return d;
    }

    private static readonly Dictionary<string, StoryEventDef> Defs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Kill Actor — fires when an actor dies. Root SMEN .Type = KillActorEvent.
            ["KillActor"] = new StoryEventDef(
                Root(0x013010),
                new RecordType("KILL"),
                Slots(
                    ("victim", R1),   // R1 = killed actor
                    ("killer", R2))), // R2 = the killer

            // Change Location — fires when an actor (the player) enters a new location.
            // Root SMEN .Type = ChangeLocationEvent. Slots are LOCATION refs, not actors.
            ["ChangeLocation"] = new StoryEventDef(
                Root(0x01320E),
                new RecordType("CLOC"),
                Slots(
                    ("oldLocation", L1),   // L1 = location departed
                    ("newLocation", L2))), // L2 = location entered

            // Cast Magic — fires when an actor casts a spell. Root SMEN .Type = CastMagicEvent.
            ["CastMagic"] = new StoryEventDef(
                Root(0x046829),
                new RecordType("CAST"),
                Slots(
                    ("caster", R1),     // R1 = casting actor
                    ("target", R2),     // R2 = spell target
                    ("location", L1))), // L1 = where it was cast

            // Player Add Item — fires when the player acquires an item.
            // Root SMEN .Type = PlayerAddItem.
            ["AddItem"] = new StoryEventDef(
                Root(0x02C439),
                new RecordType("AIPL"),
                Slots(
                    ("owner", R1),      // R1 = prior owner of the item
                    ("location", L1))), // L1 = where the item was

            // Assault Actor — fires when an actor assaults (attacks) another.
            // Root SMEN .Type = AssaultActorEvent.
            ["Assault"] = new StoryEventDef(
                Root(0x02C494),
                new RecordType("ASSU"),
                Slots(
                    ("victim", R1),     // R1 = assaulted actor
                    ("attacker", R2),   // R2 = the attacker
                    ("location", L1))), // L1 = where it happened

            // Script Event — the GENERIC custom entry. Root SMEN .Type = ScriptEvent (no conditions =
            // listens to every Papyrus SendStoryEvent). A quest under it is gated by a keyword filter
            // (see BuildStoryManager's ScriptEvent branch). Payload maps to Keyword.SendStoryEvent(
            // akLoc=L1, akRef1=R1, akRef2=R2). This is the entry ModForge content fires itself.
            ["ScriptEvent"] = new StoryEventDef(
                Root(0x01379A),
                new RecordType("SCPT"),
                Slots(
                    ("ref1", R1),       // R1 = akRef1
                    ("ref2", R2),       // R2 = akRef2
                    ("loc", L1))),      // L1 = akLoc
        };

    public static IEnumerable<string> Names => Defs.Keys;

    public static bool TryGet(string eventName, out StoryEventDef def) =>
        Defs.TryGetValue(eventName ?? "", out def);

    // "fromEvent:victim" → ("fromEvent","victim"); "forced:A:B" → ("forced","A:B"). 無冒號或冒號在首/尾 = false。
    public static bool TryParseFill(string fill, out string kind, out string arg)
    {
        kind = ""; arg = "";
        if (string.IsNullOrWhiteSpace(fill)) return false;
        int i = fill.IndexOf(':');
        if (i <= 0 || i >= fill.Length - 1) return false;
        kind = fill[..i]; arg = fill[(i + 1)..];
        return true;
    }
}
