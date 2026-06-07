using ModForge;

namespace ModForge.Tests;

// Generator.Validate is the pre-build gate. These lock its core checks so a regression can't let an
// invalid spec through to Build (which assumes a valid spec).
public class ValidateTests
{
    [Fact]
    public void CleanSpec_NoProblems()
    {
        var problems = Generator.Validate(new ModSpec
        {
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Npcs   = { new NpcSpec  { EditorId = "Npc", Name = "Npc", Greeting = "Hi." } },
            Dialogue = { new DialogueSpec { EditorId = "D", QuestEditorId = "Q", SpeakerNpcEditorId = "Npc", Prompt = "T", Responses = { "x" } } },
        });
        Assert.Empty(problems);
    }

    [Fact]
    public void HelloLine_NeedsNoPrompt()
    {
        // A hello:true greeting is the NPC's auto-spoken line, not a player topic — no prompt required.
        var problems = Generator.Validate(new ModSpec
        {
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Npcs   = { new NpcSpec  { EditorId = "Npc", Name = "Npc", Greeting = "Hi." } },
            Dialogue = { new DialogueSpec { EditorId = "H", QuestEditorId = "Q", SpeakerNpcEditorId = "Npc", Hello = true, Responses = { "Well met." } } },
        });
        Assert.Empty(problems);
    }

    [Fact]
    public void NonHelloLine_StillRequiresPrompt()
    {
        var problems = Generator.Validate(new ModSpec
        {
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Npcs   = { new NpcSpec  { EditorId = "Npc", Name = "Npc", Greeting = "Hi." } },
            Dialogue = { new DialogueSpec { EditorId = "D", QuestEditorId = "Q", SpeakerNpcEditorId = "Npc", Responses = { "x" } } },
        });
        Assert.Contains(problems, p => p.Contains("empty prompt"));
    }

    [Fact]
    public void EmptyEditorId_IsRejected()
    {
        var problems = Generator.Validate(new ModSpec { MiscItems = { new MiscSpec { EditorId = "", Name = "x" } } });
        Assert.NotEmpty(problems);
    }

    [Fact]
    public void DuplicateEditorId_IsRejected()
    {
        var problems = Generator.Validate(new ModSpec
        {
            MiscItems = { new MiscSpec { EditorId = "Dup", Name = "a" } },
            Books     = { new BookSpec { EditorId = "Dup", Name = "b" } },
        });
        Assert.Contains(problems, p => p.Contains("Dup"));
    }

    [Fact]
    public void Dialogue_UnknownQuest_IsRejected()
    {
        var problems = Generator.Validate(new ModSpec
        {
            Npcs = { new NpcSpec { EditorId = "Npc", Name = "Npc", Greeting = "Hi." } },
            Dialogue = { new DialogueSpec { EditorId = "D", QuestEditorId = "NoSuchQuest", SpeakerNpcEditorId = "Npc", Prompt = "T", Responses = { "x" } } },
        });
        Assert.NotEmpty(problems);
    }

    [Fact]
    public void Script_UnknownTarget_IsRejected()
    {
        var problems = Generator.Validate(new ModSpec
        {
            Scripts = { new ScriptAttachSpec { TargetEditorId = "Ghost", ScriptName = "S" } },
        });
        Assert.NotEmpty(problems);
    }

    [Fact]
    public void Package_EmptyTemplate_IsRejected()
    {
        var problems = Generator.Validate(new ModSpec
        {
            Packages = { new PackageSpec { EditorId = "P", Template = "" } },
        });
        Assert.NotEmpty(problems);
    }
}
