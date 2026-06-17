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
    // A组 #5 location-aware encounter sugar (ChangeLocation events): LocType keyword refs. The build
    // appends one GetKeywordDataForCurrentLocation condition per keyword to the quest's event conditions,
    // OR'd together — so the encounter quest only fires when the player's new location has ANY of these
    // LocType keywords (e.g. ["LocTypeBanditCamp", "LocTypeDungeon"]). Sugar over hand-writing the CTDA.
    public List<string> LocationFilter { get; set; } = new();
    // A组 #6 anti-spam cooldown (ChangeLocation events): minimum GAME HOURS between firings. The build
    // creates a "<quest>_LastFired" GLOB (GameDaysPassed of last trigger) + ships the reusable
    // MFEncounterCooldown quest script, which stamps it on start and is gated against re-firing within
    // the window. 0 = no cooldown. ⚠ runtime behaviour verified on the main machine (see WAIT_USER).
    public float CooldownHours { get; set; }
}

// 一條 quest alias。fill 語法："fromEvent:<slot>"（拿事件帶來的 ref）、"forced:<ref>"（寫死特定 ref）、
// "uniqueActor:<ref>"（指向某個唯一 NPC base，<ref> 同 forced 解析）、
// "createObject:<ref>@<targetAlias>"（quest 啟動時在 <targetAlias> 持有的 ref 處生成一個 <ref> 的新實例，
// 例如在施法者腳邊生一個寶箱；<targetAlias> 須為同 quest 內另一個 ref 型 alias，不能是 location）或
// "findMatching:closest"|"findMatching:any"（在 loaded area 裏找一個既有的、符合本 alias `Conditions` 的 ref；
// closest=最近的一個，any=第一個符合的。= QuestAlias 旗標 MatchingRefInLoadedArea[+MatchingRefClosest]，
// 解自 vanilla MQGreybeardCall 的 Bystander aliases）、
// "findMatchingLocation:<locTypeKeyword>[@<parentLocationAlias>]"（#7 radiant LocationAlias：建一個 Location 型
// alias，用 Find Matching Location 挑一個 LocType keyword 符合的子地點，可選縮限在另一個 location alias 範圍內；
// = QuestAlias.Type=Location + LocationAliasReference{Keyword, AliasID=parent}。Missives 的 Alias_Dungeon/Alias_Inn）或
// "findInLocationAlias:<locationAlias>[#<refTypeLCRT>]"（#8 radiant 在地點內找 ref：建一個 Reference 型 alias，
// 在另一個 location alias 所指地點範圍內 Find Matching Reference，可帶 RefType（LCRT，如地城 BossChest）
// 與/或本 alias 的 `Conditions` 挑 ref（地城 boss、寶箱）；= QuestAlias.Type=Reference +
// LocationAliasReference{AliasID=locationAlias, RefType}。Missives 的 Alias_target/Alias_chest。
// 註：不是 FindMatchingRefNearAlias（ALNA，離線驗證＝只 LinkedRefChild，非地點內搜尋））。
public sealed class QuestAliasSpec
{
    public string Name { get; set; } = "";
    public string Fill { get; set; } = "";
    public bool Optional { get; set; }
    // Set true to let this alias fill with a ref another running quest has reserved (e.g. a town NPC
    // held by a Freeform quest). Off by default (vanilla-faithful). uniqueActor forces it on anyway.
    public bool AllowReserved { get; set; }
    // Only used by the "findMatching:closest|any" fill: the CTDA conditions that filter WHICH ref in the
    // loaded area the engine picks (e.g. HasKeyword ActorTypeNPC on the candidate = nearest NPC, GetIsID
    // for a specific base). Reuses the existing ConditionSpec type and is wired onto QuestAlias.Conditions.
    public List<ConditionSpec> Conditions { get; set; } = new();
    // Optional Papyrus ALIAS script attached to THIS alias (stored on the quest's QuestAdapter.Aliases
    // VMAD — a QuestFragmentAlias bound to this alias's ID). It reacts to events on WHATEVER ref fills
    // the alias, including one created at runtime (createObject) or matched at runtime (findMatching)
    // that no base-object script could ever reach. Classic use = OnActivate: `Script` extends
    // ReferenceAlias and defines `Event OnActivate(ObjectReference akActionRef)`; activating the aliased
    // ref runs it (e.g. call MFStoryEventDispatch.Fire to chain a story event). `ScriptSource` is the
    // .psc `package` compiles (resolved like a ScriptAttach source); `ScriptProperties` bind its Auto
    // properties (same shape as a dialogue ResultProperties). The user supplies the compiled .pex, so
    // the VMAD is attached unconditionally (like a user ScriptAttach / ResultScript).
    public string Script { get; set; } = "";
    public string ScriptSource { get; set; } = "";
    public List<PropertySpec> ScriptProperties { get; set; } = new();
}
