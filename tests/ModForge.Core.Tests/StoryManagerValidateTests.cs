using System.Linq;
using ModForge;
using Xunit;

public class StoryManagerValidateTests
{
    private static ModSpec QuestWith(QuestStoryEventSpec se, params QuestAliasSpec[] aliases)
    {
        var spec = new ModSpec();
        var q = new QuestSpec { EditorId = "Q", Name = "Q", StoryEvent = se };
        q.Aliases.AddRange(aliases);
        spec.Quests.Add(q);
        return spec;
    }

    [Fact]
    public void Unknown_event_is_an_error()
    {
        var problems = Generator.Validate(QuestWith(new QuestStoryEventSpec { Event = "Nope" }));
        Assert.Contains(problems, p => p.Contains("Nope") && p.Contains("storyEvent"));
    }

    [Fact]
    public void Unknown_fromevent_slot_is_an_error()
    {
        var problems = Generator.Validate(QuestWith(
            new QuestStoryEventSpec { Event = "KillActor" },
            new QuestAliasSpec { Name = "X", Fill = "fromEvent:bogus" }));
        Assert.Contains(problems, p => p.Contains("bogus"));
    }

    [Fact]
    public void Bad_fill_syntax_is_an_error()
    {
        var problems = Generator.Validate(QuestWith(
            new QuestStoryEventSpec { Event = "KillActor" },
            new QuestAliasSpec { Name = "X", Fill = "garbage" }));
        Assert.Contains(problems, p => p.Contains("fill") && p.Contains("garbage"));
    }

    [Fact]
    public void Valid_killactor_quest_has_no_problems()
    {
        var problems = Generator.Validate(QuestWith(
            new QuestStoryEventSpec { Event = "KillActor" },
            new QuestAliasSpec { Name = "Victim", Fill = "fromEvent:victim" }));
        Assert.DoesNotContain(problems, p => p.Contains("storyEvent") || p.Contains("fill"));
    }
}
