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

    [Fact]
    public void UniqueActor_unresolved_ref_is_an_error()
    {
        var problems = Generator.Validate(QuestWith(
            new QuestStoryEventSpec { Event = "KillActor" },
            new QuestAliasSpec { Name = "X", Fill = "uniqueActor:NoSuchEditorId" }));
        Assert.Contains(problems, p => p.Contains("uniqueActor") && p.Contains("NoSuchEditorId"));
    }

    [Fact]
    public void UniqueActor_valid_ref_has_no_problems()
    {
        var problems = Generator.Validate(QuestWith(
            new QuestStoryEventSpec { Event = "KillActor" },
            new QuestAliasSpec { Name = "Target", Fill = "uniqueActor:Skyrim.esm:0x01414D" }));
        Assert.DoesNotContain(problems, p => p.Contains("uniqueActor"));
    }

    [Fact]
    public void Unknown_fill_kind_is_still_rejected()
    {
        var problems = Generator.Validate(QuestWith(
            new QuestStoryEventSpec { Event = "KillActor" },
            new QuestAliasSpec { Name = "X", Fill = "bogusKind:whatever" }));
        Assert.Contains(problems, p => p.Contains("bogusKind") && p.Contains("unsupported"));
    }

    [Fact]
    public void ScriptEvent_without_keyword_is_an_error()
    {
        var problems = Generator.Validate(QuestWith(new QuestStoryEventSpec { Event = "ScriptEvent" }));
        Assert.Contains(problems, p => p.Contains("ScriptEvent") && p.Contains("keyword"));
    }

    [Fact]
    public void ScriptEvent_keyword_must_be_declared_in_spec_keywords()
    {
        var problems = Generator.Validate(QuestWith(
            new QuestStoryEventSpec { Event = "ScriptEvent", Keyword = "MFSE_Undeclared" }));
        Assert.Contains(problems, p => p.Contains("MFSE_Undeclared") && p.Contains("not declared"));
    }

    [Fact]
    public void ScriptEvent_with_declared_keyword_has_no_keyword_problem()
    {
        var spec = QuestWith(
            new QuestStoryEventSpec { Event = "ScriptEvent", Keyword = "MFSE_KW" },
            new QuestAliasSpec { Name = "T", Fill = "fromEvent:ref1" });
        spec.Keywords.Add(new KeywordSpec { EditorId = "MFSE_KW" });
        var problems = Generator.Validate(spec);
        Assert.DoesNotContain(problems, p => p.Contains("keyword"));
    }
}
