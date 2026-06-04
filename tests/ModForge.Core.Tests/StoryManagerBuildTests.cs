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
}
