using Mutagen.Bethesda.Plugins;

namespace ModForge;

// 一個 SM 事件的定義：原版事件根、Quest.Event 碼、可用的 event-data 槽位（slot 名 → 4-byte 索引）。
public readonly record struct StoryEventDef(FormKey Root, RecordType Code, IReadOnlyDictionary<string, byte[]> Slots);

// 內建「事件名 → 定義」表。一個事件一筆；之後加事件 = 加一筆（值離線從 Skyrim.esm vanilla 解出）。
public static class StoryManagerEvents
{
    private static readonly FormKey KillRoot = new(ModKey.FromNameAndExtension("Skyrim.esm"), 0x013010);

    private static readonly Dictionary<string, StoryEventDef> Defs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["KillActor"] = new StoryEventDef(
                KillRoot,
                new RecordType("KILL"),
                new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["victim"] = new byte[] { 0x52, 0x31, 0x00, 0x00 }, // "R1" = killed actor
                    ["killer"] = new byte[] { 0x52, 0x32, 0x00, 0x00 }, // "R2" = the killer
                }),
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
