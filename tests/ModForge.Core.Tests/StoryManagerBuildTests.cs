using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class StoryManagerBuildTests
{
    private static ModSpec SpecWithKillQuest()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFSM_Avenge",
            Name = "Avenge",
            Stages = { new StageSpec { Index = 10 } },
            StoryEvent = new QuestStoryEventSpec { Event = "KillActor" },
            Aliases = { new QuestAliasSpec { Name = "Victim", Fill = "fromEvent:victim" } },
        });
        return spec;
    }

    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    [Fact]
    public void StoryEvent_quest_gets_event_and_clears_startgame()
    {
        var mod = Build(SpecWithKillQuest());
        var q = mod.Quests.Single(x => x.EditorID == "MFSM_Avenge");
        Assert.Equal(new RecordType("KILL"), q.Event);
        Assert.False(q.Flags.HasFlag(Quest.Flag.StartGameEnabled));
        var alias = Assert.Single(q.Aliases);
        Assert.Equal("Victim", alias.Name);
        Assert.NotNull(alias.FindMatchingRefFromEvent);
        Assert.Equal(new RecordType("KILL"), alias.FindMatchingRefFromEvent!.FromEvent);
        Assert.Equal(new byte[] { 0x52, 0x31, 0x00, 0x00 }, alias.FindMatchingRefFromEvent.EventData!.Value.ToArray());
    }

    [Fact]
    public void StoryEvent_quest_generates_branch_and_questnode()
    {
        var mod = Build(SpecWithKillQuest());
        var q = mod.Quests.Single(x => x.EditorID == "MFSM_Avenge");
        var branch = Assert.Single(mod.StoryManagerBranchNodes);
        var qnode = Assert.Single(mod.StoryManagerQuestNodes);
        Assert.Empty(mod.StoryManagerEventNodes);
        Assert.Equal(0x013010u, branch.Parent.FormKey.ID);
        Assert.Equal(branch.FormKey, qnode.Parent.FormKey);
        Assert.Equal(q.FormKey, Assert.Single(qnode.Quests).Quest.FormKey);
        Assert.True(qnode.PreviousSibling.FormKey.IsNull);   // single quest node = chain head, no prev sibling
    }

    [Fact]
    public void Multiple_storyevent_quests_share_one_branch_and_chain_questnode_siblings()
    {
        // Decoded from vanilla + confirmed in-game: an event root has ONE branch with all the event's
        // quests as sibling QUEST NODES under it (N separate branches → only the head's quest fires).
        // The quest-node siblings must be chained via PreviousSibling (first = null head) or the engine's
        // sibling walk misses all-but-one.
        var spec = new ModSpec();
        foreach (var ed in new[] { "Q1", "Q2", "Q3" })
            spec.Quests.Add(new QuestSpec
            {
                EditorId = ed, Name = ed,
                StoryEvent = new QuestStoryEventSpec { Event = "KillActor" },
                Aliases = { new QuestAliasSpec { Name = "Victim", Fill = "fromEvent:victim" } },
            });
        var mod = Build(spec);

        // exactly ONE shared branch under the kill root
        var branch = Assert.Single(mod.StoryManagerBranchNodes);
        Assert.Equal(0x013010u, branch.Parent.FormKey.ID);

        // three quest nodes, all parented to that branch, chained head→q1→q2→q3
        var n1 = mod.StoryManagerQuestNodes.Single(x => x.EditorID == "Q1_SMQuestNode");
        var n2 = mod.StoryManagerQuestNodes.Single(x => x.EditorID == "Q2_SMQuestNode");
        var n3 = mod.StoryManagerQuestNodes.Single(x => x.EditorID == "Q3_SMQuestNode");
        Assert.All(new[] { n1, n2, n3 }, n => Assert.Equal(branch.FormKey, n.Parent.FormKey));
        Assert.True(n1.PreviousSibling.FormKey.IsNull);     // head
        Assert.Equal(n1.FormKey, n2.PreviousSibling.FormKey);
        Assert.Equal(n2.FormKey, n3.PreviousSibling.FormKey);
    }

    [Fact]
    public void Quest_without_storyevent_is_unchanged_no_sm_nodes()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec { EditorId = "Plain", Name = "Plain", StartGameEnabled = true });
        var mod = Build(spec);
        Assert.Empty(mod.StoryManagerBranchNodes);
        Assert.Empty(mod.StoryManagerQuestNodes);
        var q = mod.Quests.Single(x => x.EditorID == "Plain");
        Assert.True(q.Flags.HasFlag(Quest.Flag.StartGameEnabled));
    }

    [Fact]
    public void Forced_alias_sets_forced_reference()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFSM_Forced", Name = "F",
            StoryEvent = new QuestStoryEventSpec { Event = "KillActor" },
            Aliases = { new QuestAliasSpec { Name = "Boss", Fill = "forced:Skyrim.esm:0x000007" } },
        });
        var mod = Build(spec);
        var q = mod.Quests.Single(x => x.EditorID == "MFSM_Forced");
        var alias = Assert.Single(q.Aliases);
        Assert.Equal(0x000007u, alias.ForcedReference.FormKey.ID);
    }

    [Fact]
    public void Forced_alias_resolves_in_spec_editorid()
    {
        var spec = new ModSpec();
        spec.Npcs.Add(new NpcSpec { EditorId = "MyBoss", Name = "Boss" });
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFSM_InSpec", Name = "I",
            StoryEvent = new QuestStoryEventSpec { Event = "KillActor" },
            Aliases = { new QuestAliasSpec { Name = "Boss", Fill = "forced:MyBoss" } },
        });
        var mod = Build(spec);
        var q = mod.Quests.Single(x => x.EditorID == "MFSM_InSpec");
        var npc = mod.Npcs.Single(x => x.EditorID == "MyBoss");
        var alias = Assert.Single(q.Aliases);
        Assert.Equal(npc.FormKey, alias.ForcedReference.FormKey);
    }

    [Fact]
    public void UniqueActor_alias_sets_unique_actor_link()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFSM_Unique", Name = "U",
            StoryEvent = new QuestStoryEventSpec { Event = "KillActor" },
            Aliases = { new QuestAliasSpec { Name = "Target", Fill = "uniqueActor:Skyrim.esm:0x01414D" } },
        });
        var mod = Build(spec);
        var q = mod.Quests.Single(x => x.EditorID == "MFSM_Unique");
        var alias = Assert.Single(q.Aliases);
        Assert.Equal(0x01414Du, alias.UniqueActor.FormKey.ID);
        Assert.True(alias.ForcedReference.FormKey.IsNull);   // only UniqueActor set, not ForcedReference
        // AllowReserved is mandatory on unique-actor aliases (vanilla sets it universally); without
        // it a reserved unique NPC can't be grabbed and the required alias blocks quest start.
        Assert.True(alias.Flags.GetValueOrDefault().HasFlag(QuestAlias.Flag.AllowReserved));
    }

    [Fact]
    public void UniqueActor_alias_resolves_in_spec_editorid()
    {
        var spec = new ModSpec();
        spec.Npcs.Add(new NpcSpec { EditorId = "MyHero", Name = "Hero" });
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFSM_UniqueInSpec", Name = "UI",
            StoryEvent = new QuestStoryEventSpec { Event = "KillActor" },
            Aliases = { new QuestAliasSpec { Name = "Hero", Fill = "uniqueActor:MyHero" } },
        });
        var mod = Build(spec);
        var q = mod.Quests.Single(x => x.EditorID == "MFSM_UniqueInSpec");
        var npc = mod.Npcs.Single(x => x.EditorID == "MyHero");
        var alias = Assert.Single(q.Aliases);
        Assert.Equal(npc.FormKey, alias.UniqueActor.FormKey);
    }
}
