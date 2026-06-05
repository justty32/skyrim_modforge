namespace ModForge;

// Story Manager 觸發宣告。掛在 QuestSpec 上 = 此 quest 可被 SM 的某事件啟動。
// event = 友善事件名（查 StoryManagerEvents 表）。conditions = 事件條件（沿用既有 ConditionSpec）。
public sealed class QuestStoryEventSpec
{
    public string Event { get; set; } = "";
    public List<ConditionSpec> Conditions { get; set; } = new();
    // Only for event == "ScriptEvent": the editorId of a KYWD (declare it in spec.keywords) the
    // dispatcher passes to SendStoryEvent. The SM branch gets a GetEventData/GetIsID Keyword filter,
    // so this quest starts only when content fires THAT keyword. Ignored for engine-native events.
    public string Keyword { get; set; } = "";
}

// 一條 quest alias。fill 語法："fromEvent:<slot>"（拿事件帶來的 ref）、"forced:<ref>"（寫死特定 ref）、
// "uniqueActor:<ref>"（指向某個唯一 NPC base，<ref> 同 forced 解析）或
// "createObject:<ref>@<targetAlias>"（quest 啟動時在 <targetAlias> 持有的 ref 處生成一個 <ref> 的新實例，
// 例如在施法者腳邊生一個寶箱；<targetAlias> 須為同 quest 內另一個 ref 型 alias，不能是 location）。
public sealed class QuestAliasSpec
{
    public string Name { get; set; } = "";
    public string Fill { get; set; } = "";
    public bool Optional { get; set; }
    // Set true to let this alias fill with a ref another running quest has reserved (e.g. a town NPC
    // held by a Freeform quest). Off by default (vanilla-faithful). uniqueActor forces it on anyway.
    public bool AllowReserved { get; set; }
}
