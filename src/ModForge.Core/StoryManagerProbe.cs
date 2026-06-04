namespace ModForge;

/// <summary>
/// Story Manager (SM) structural probe. Bypasses the spec→build pipeline and constructs a
/// <see cref="SkyrimMod"/> directly in memory so the SM record graph can be structurally unit-tested
/// without owning Skyrim.esm. Throwaway-ish experiment driving the "Story Manager 最小驗證" plan.
///
/// Graph built (additive — we hang under the VANILLA Kill Actor event root, we never author our own SMEN):
///   VANILLA SMEN (Kill Actor = Skyrim.esm:0x013010, passed in)  ◄── SMBN.Parent
///                                              SMBN  ◄── SMQN.Parent
///                                                        SMQN.Quests = [ our MFSM_AvengeQuest ]
///   MFSM_AvengeQuest: StartGameEnabled=false, Event="KILL", alias "Victim"
///                     (FindMatchingRefFromEvent: FromEvent="KILL", EventData="R1" = killed actor),
///                     startup stage Index=10. Our SMBN carries NO conditions, so it attempts on every
///                     kill (vanilla WIKillEventsBranchNode gates with 6 conditions — we want it permissive).
/// Real values (event code, EventData, root FormID) decoded from Skyrim.esm's WIKill quests, not guessed.
///
/// API 釘樁 — pinned Mutagen 0.53.1 types (verified by compiling):
///   - Groups: mod.StoryManagerBranchNodes / mod.StoryManagerQuestNodes / mod.StoryManagerEventNodes
///   - StoryManagerBranchNode / StoryManagerQuestNode : AStoryManagerNode
///       AStoryManagerNode.Parent : IFormLinkNullable&lt;IStoryManagerNodeGetter&gt;  (SetTo(FormKey) / .FormKey)
///   - StoryManagerQuestNode.Quests : ExtendedList&lt;StoryManagerQuest&gt;
///   - StoryManagerQuest.Quest : IFormLinkNullable&lt;IQuestGetter&gt;
///   - Quest.Event : RecordType  (a Skyrim "Quest Event" 4-char code; "KILL" for Kill Actor)
///   - Quest.NextAliasID : uint? ; Quest.Flags : Quest.Flag (StartGameEnabled is a flag, not a bool prop)
///   - QuestAlias.ID : uint ; QuestAlias.Name : string ; QuestAlias.FindMatchingRefFromEvent : FindMatchingRefFromEvent
///   - FindMatchingRefFromEvent.EventData : Noggog.MemorySlice&lt;byte&gt;? ; .FromEvent : RecordType?
/// </summary>
public static class StoryManagerProbe
{
    public static SkyrimMod BuildProbe(FormKey killActorEventRoot)
    {
        var mod = new SkyrimMod(ModKey.FromNameAndExtension("MFSM_Probe.esp"), SkyrimRelease.SkyrimSE);

        // --- Template quest started by the SM Kill Actor event ---
        var quest = mod.Quests.AddNew();
        quest.EditorID = "MFSM_AvengeQuest";
        quest.Name = "Avenge";

        // SM starts this; it must NOT also auto-run at game start. StartGameEnabled lives on Quest.Flags
        // (there is no standalone bool property in Mutagen 0.53.1) — leave the flag CLEARED.
        quest.Flags &= ~Quest.Flag.StartGameEnabled;

        // Quest Event code for the Kill Actor SM event. CONFIRMED against Skyrim.esm: every vanilla
        // WIKill0x quest hung under the Kill Actor root (0x013010) carries Event "KILL".
        quest.Event = new RecordType("KILL");

        // Alias "Victim" filled from the Kill Actor event's killed reference. CONFIRMED recipe from the
        // vanilla WIKill quests: FromEvent="KILL", EventData = ASCII "R1\0\0" (52 31 00 00) = event ref
        // slot 1 (the killed actor; slot 2 "R2" is the killer). Matches SendStoryEvent(akRef1, akRef2).
        var alias = new QuestAlias
        {
            ID = 0,
            Name = "Victim",
            FindMatchingRefFromEvent = new FindMatchingRefFromEvent
            {
                FromEvent = new RecordType("KILL"),
                EventData = new byte[] { 0x52, 0x31, 0x00, 0x00 },
            },
        };
        quest.Aliases.Add(alias);
        quest.NextAliasID = 1;

        // Startup stage so `sqv` shows the quest running once SM starts it.
        quest.Stages.Add(new QuestStage { Index = 10 });

        // --- SMBN: additive child hung under the VANILLA Kill Actor SMEN ---
        var branch = mod.StoryManagerBranchNodes.AddNew();
        branch.EditorID = "MFSM_AvengeBranch";
        branch.Parent.SetTo(killActorEventRoot);

        // --- SMQN: parents our branch, holds our quest ---
        var qnode = mod.StoryManagerQuestNodes.AddNew();
        qnode.EditorID = "MFSM_AvengeQuestNode";
        qnode.Parent.SetTo(branch);
        var entry = new StoryManagerQuest();
        entry.Quest.SetTo(quest);
        qnode.Quests.Add(entry);

        return mod;
    }
}
