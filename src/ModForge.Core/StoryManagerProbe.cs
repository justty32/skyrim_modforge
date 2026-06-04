namespace ModForge;

/// <summary>
/// Story Manager (SM) structural probe. Bypasses the spec→build pipeline and constructs a
/// <see cref="SkyrimMod"/> directly in memory so the SM record graph can be structurally unit-tested
/// without owning Skyrim.esm. Throwaway-ish experiment driving the "Story Manager 最小驗證" plan.
///
/// Graph built (additive — we hang under the VANILLA Kill Actor event root, we never author our own SMEN):
///   VANILLA SMEN (Kill Actor, passed in)  ◄── SMBN.Parent
///                                              SMBN  ◄── SMQN.Parent
///                                                        SMQN.Quests = [ our MFSM_AvengeQuest ]
///   MFSM_AvengeQuest: StartGameEnabled=false, Event=KillActor, alias "Victim"
///                     (FindMatchingRefFromEvent), startup stage Index=10.
///
/// API 釘樁 — pinned Mutagen 0.53.1 types (verified by compiling):
///   - Groups: mod.StoryManagerBranchNodes / mod.StoryManagerQuestNodes / mod.StoryManagerEventNodes
///   - StoryManagerBranchNode / StoryManagerQuestNode : AStoryManagerNode
///       AStoryManagerNode.Parent : IFormLinkNullable&lt;IStoryManagerNodeGetter&gt;  (SetTo(FormKey) / .FormKey)
///   - StoryManagerQuestNode.Quests : ExtendedList&lt;StoryManagerQuest&gt;
///   - StoryManagerQuest.Quest : IFormLinkNullable&lt;IQuestGetter&gt;
///   - Quest.Event : RecordType  (a Skyrim "Quest Event" 4-char code; placeholder until smtree)
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

        // TODO(smtree): set to the real Kill Actor event code printed by the smtree CLI command.
        // Placeholder so the SM linkage compiles/tests; Quest.Event is a Mutagen RecordType? — any
        // non-null 4-char code satisfies the structural test. "KILL" is a stand-in, NOT verified vanilla.
        quest.Event = new RecordType("KILL");

        // Alias "Victim" filled from the Kill Actor event's killed reference. EventData/FromEvent get
        // tuned against smtree output during in-game testing (EventData is a MemorySlice<byte>? blob,
        // FromEvent a RecordType? — both left default/minimal here for the structural probe).
        var alias = new QuestAlias
        {
            ID = 0,
            Name = "Victim",
            FindMatchingRefFromEvent = new FindMatchingRefFromEvent(),
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
