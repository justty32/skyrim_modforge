using Mutagen.Bethesda.Plugins;

namespace ModForge;

// 一個 SM 事件的定義：原版事件根、Quest.Event 碼、可用的 event-data 槽位（slot 名 → 4-byte 索引）、
// 以及此事件在 Quest 腳本上的 `OnStory<Event>` 處理器簽名（不含 "Event " 前綴與 "EndEvent"）。
// StoryHandler 是 SM-encounter 觸發的關鍵：SM 啟動的 quest 不會自動跑 startUpStage 的 Papyrus
// fragment（實機 2026-06-19 確認：OnInit/OnStoryXxx 都觸發、但 Fragment_Stage_XXXX 不跑），所以
// spawn/cooldown 觸發必須掛在這個 `OnStory<Event>` 事件裏（每次 SM 投遞事件都可靠觸發）。
public readonly record struct StoryEventDef(FormKey Root, RecordType Code, IReadOnlyDictionary<string, byte[]> Slots, string StoryHandler);

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
                    ("killer", R2)),  // R2 = the killer
                "OnStoryKillActor(ObjectReference akVictim, ObjectReference akKiller, Location akLocation, int aiCrimeStatus, int aiRelationshipRank)"),

            // Change Location — fires when an actor (the player) enters a new location.
            // Root SMEN .Type = ChangeLocationEvent. Slots are LOCATION refs, not actors.
            ["ChangeLocation"] = new StoryEventDef(
                Root(0x01320E),
                new RecordType("CLOC"),
                Slots(
                    ("oldLocation", L1),   // L1 = location departed
                    ("newLocation", L2)),  // L2 = location entered
                "OnStoryChangeLocation(ObjectReference akActor, Location akOldLocation, Location akNewLocation)"),

            // Cast Magic — fires when an actor casts a spell. Root SMEN .Type = CastMagicEvent.
            ["CastMagic"] = new StoryEventDef(
                Root(0x046829),
                new RecordType("CAST"),
                Slots(
                    ("caster", R1),     // R1 = casting actor
                    ("target", R2),     // R2 = spell target
                    ("location", L1)),  // L1 = where it was cast
                "OnStoryCastMagic(ObjectReference akCastingActor, ObjectReference akSpellTarget, Location akLocation, Form akSpell)"),

            // Player Add Item — fires when the player acquires an item.
            // Root SMEN .Type = PlayerAddItem.
            ["AddItem"] = new StoryEventDef(
                Root(0x02C439),
                new RecordType("AIPL"),
                Slots(
                    ("owner", R1),      // R1 = prior owner of the item
                    ("location", L1)),  // L1 = where the item was
                "OnStoryAddToPlayer(ObjectReference akOwner, ObjectReference akContainer, Location akLocation, Form akItemBase, int aiAcquireType)"),

            // Assault Actor — fires when an actor assaults (attacks) another.
            // Root SMEN .Type = AssaultActorEvent.
            ["Assault"] = new StoryEventDef(
                Root(0x02C494),
                new RecordType("ASSU"),
                Slots(
                    ("victim", R1),     // R1 = assaulted actor
                    ("attacker", R2),   // R2 = the attacker
                    ("location", L1)),  // L1 = where it happened
                "OnStoryAssaultActor(ObjectReference akVictim, ObjectReference akAttacker, Location akLocation, int aiCrime)"),

            // Craft Item — fires when the player crafts an item at a workbench/forge/etc.
            // Root SMEN .Type = CraftItem. Vanilla WICraftItem03 fills R1 = the workbench used.
            ["CraftItem"] = new StoryEventDef(
                Root(0x039D86),
                new RecordType("CRFT"),
                Slots(
                    ("workbench", R1)), // R1 = the crafting station used
                "OnStoryCraftItem(ObjectReference akBench, Location akLocation, Form akCreatedItem)"),

            // Player Remove Item — fires when an item leaves the player's inventory (sold/dropped/given).
            // Root SMEN .Type = PlayerRemoveItem. Vanilla WIRemoveItem01: R1 = new owner, R2 = the item.
            ["PlayerRemoveItem"] = new StoryEventDef(
                Root(0x02C6AC),
                new RecordType("REMP"),
                Slots(
                    ("owner", R1),   // R1 = who received the item
                    ("item", R2)),   // R2 = the item removed
                "OnStoryRemoveFromPlayer(ObjectReference akOwner, ObjectReference akItem, Location akLocation, Form akItemBase, int aiRemoveType)"),

            // Arrest — fires when a guard arrests an actor. Root SMEN .Type = ArrestEvent.
            // Vanilla DGArrestQuest: R1 = the arresting guard, R2 = the criminal.
            ["Arrest"] = new StoryEventDef(
                Root(0x06B369),
                new RecordType("ARRT"),
                Slots(
                    ("guard", R1),      // R1 = the arresting guard
                    ("criminal", R2)),  // R2 = the arrested actor
                "OnStoryArrest(ObjectReference akArrestingGuard, ObjectReference akCriminal, Location akLocation, int aiCrime)"),

            // Increase Level — fires when the player levels up. Root SMEN .Type = IncreaseLevel.
            // No event ref slots (vanilla LEVL quests fill aliases via forced/findMatching, never
            // fromEvent) — gate the start with storyEvent.conditions (e.g. GetLevel >= N).
            ["IncreaseLevel"] = new StoryEventDef(
                Root(0x05BD79),
                new RecordType("LEVL"),
                Slots(),
                "OnStoryIncreaseLevel(int aiNewLevel)"),

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
                    ("loc", L1)),       // L1 = akLoc
                "OnStoryScript(Keyword akKeyword, Location akLocation, ObjectReference akRef1, ObjectReference akRef2, int aiValue1, int aiValue2)"),
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

    // createObject arg "<ref>@<aliasName>" → (ref, aliasName). The ref itself may contain ':'
    // (Plugin.esm:0xID), so we split on the LAST '@' (a FormID never contains '@'). Missing or
    // edge-positioned '@' = false.
    public static bool TryParseCreateObject(string arg, out string objectRef, out string targetAlias)
    {
        objectRef = ""; targetAlias = "";
        if (string.IsNullOrWhiteSpace(arg)) return false;
        int at = arg.LastIndexOf('@');
        if (at <= 0 || at >= arg.Length - 1) return false;
        objectRef = arg[..at]; targetAlias = arg[(at + 1)..];
        return true;
    }
}
