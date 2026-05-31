using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Locks in the custom-dialogue gotchas that each cost an in-game debugging cycle (It.23–26).
// See docs/lifelike/gotchas.md "Dialogue" + memory dialogue-debugging-breakthrough.
public class DialogueTests
{
    // A minimal conversational spec: one quest, one speaker NPC, two player topics. No master needed.
    private static ModSpec TwoTopicSpec() => new()
    {
        PluginName = "Test.esp",
        Quests = { new QuestSpec { EditorId = "Q", Name = "Quest" } },
        Npcs   = { new NpcSpec  { EditorId = "Npc", Name = "Npc", Greeting = "Hello there." } },
        Dialogue =
        {
            new DialogueSpec { EditorId = "D1", QuestEditorId = "Q", SpeakerNpcEditorId = "Npc",
                Prompt = "Topic one", Responses = { "Line one." } },
            new DialogueSpec { EditorId = "D2", QuestEditorId = "Q", SpeakerNpcEditorId = "Npc",
                Prompt = "Topic two", Responses = { "Line two." } },
        },
    };

    private static System.Collections.Generic.List<IDialogTopicGetter> Topics(BuildResult r)
        => r.Mod.EnumerateMajorRecords<IDialogTopicGetter>().ToList();

    // GOTCHA: a Custom topic with SNAM=null crashes at main menu — every player topic must carry SNAM='CUST'.
    [Fact]
    public void CustomPlayerTopics_HaveCustSubtypeName()
    {
        var r = TestBuild.Ok(TwoTopicSpec());
        var custom = Topics(r).Where(t => t.Subtype == DialogTopic.SubtypeEnum.Custom).ToList();
        Assert.Equal(2, custom.Count);
        Assert.All(custom, t => Assert.Equal("CUST", t.SubtypeName.Type));
    }

    // GOTCHA: an INFO missing ENAM (Flags) + CNAM (FavorLevel) is treated as invalid and the topic is
    // silently dropped from the menu. Build must set Flags (→ENAM) and FavorLevel (→CNAM) on every INFO.
    [Fact]
    public void EveryInfo_HasEnamAndCnam()
    {
        var r = TestBuild.Ok(TwoTopicSpec());
        var infos = Topics(r).SelectMany(t => t.Responses).ToList();
        Assert.NotEmpty(infos);
        Assert.All(infos, i => Assert.NotNull(i.Flags));          // ENAM present
        // FavorLevel is a non-nullable enum (→ CNAM is always emitted); None is the expected default.
        Assert.All(infos, i => Assert.Equal(FavorLevel.None, i.FavorLevel));
    }

    // GOTCHA: without a Hello the NPC isn't conversable at all. Build auto-emits one Hello topic per
    // speaker: Misc / Hello / SNAM='HELO', no branch, gated GetIsID(speaker).
    [Fact]
    public void Speaker_GetsHelloTopic_HeloNoBranchGetIsID()
    {
        var r = TestBuild.Ok(TwoTopicSpec());
        var hello = Topics(r).Single(t => t.Subtype == DialogTopic.SubtypeEnum.Hello);
        Assert.Equal("HELO", hello.SubtypeName.Type);
        Assert.True(hello.Branch.IsNull, "Hello must have no branch");
        var info = hello.Responses.Single();
        Assert.Contains(info.Conditions, c => c.Data.GetType().Name == "GetIsIDConditionData");
    }

    // GOTCHA: a quest's player dialogue is never served without a DialogView (DLVW) per quest.
    [Fact]
    public void EachQuest_GetsOneDialogView()
    {
        var r = TestBuild.Ok(TwoTopicSpec());
        Assert.Single(r.Mod.EnumerateMajorRecords<IDialogViewGetter>());
    }

    // GOTCHA: the host quest must be Start-Game-Enabled or its dialogue never loads; the player
    // branch must be Top-Level or the topic is a sub-branch, not a menu option.
    [Fact]
    public void EveryQuest_IsStartGameEnabled_AndEveryBranch_IsTopLevel()
    {
        var r = TestBuild.Ok(TwoTopicSpec());
        var quests = r.Mod.EnumerateMajorRecords<IQuestGetter>().ToList();
        Assert.NotEmpty(quests);
        Assert.All(quests, q => Assert.True(q.Flags.HasFlag(Quest.Flag.StartGameEnabled)));
        var branches = r.Mod.EnumerateMajorRecords<IDialogBranchGetter>().ToList();
        Assert.NotEmpty(branches);
        Assert.All(branches, b => Assert.True(b.Flags?.HasFlag(DialogBranch.Flag.TopLevel) == true));
    }

    // GOTCHA: two simultaneously-valid top-level topics sharing a Priority collapse the menu and mute
    // the NPC. Build hands out a DISTINCT descending priority per topic.
    [Fact]
    public void MultipleTopics_GetDistinctPriorities()
    {
        var r = TestBuild.Ok(TwoTopicSpec());
        var prios = Topics(r)
            .Where(t => t.Subtype == DialogTopic.SubtypeEnum.Custom)
            .Select(t => t.Priority).ToList();
        Assert.Equal(prios.Count, prios.Distinct().Count());
    }
}
