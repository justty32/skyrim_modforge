using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// M組 — named, reusable condition templates expanded onto dialogue INFOs (FCO's 265-line shared-gate
// use case). useConditionTemplates appends the template's conditions exactly like inline conditions.
public class ConditionTemplateTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static ModSpec WithTemplate(params string[] used) => new()
    {
        ConditionTemplates =
        {
            new ConditionTemplateSpec
            {
                Name = "InTavern",
                Conditions =
                {
                    new ConditionSpec { Function = "IsInInterior", Comparison = "==", Value = 1 },
                    new ConditionSpec { Function = "GetRandomPercent", Comparison = "<", Value = 30 },
                },
            },
        },
        Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
        Npcs = { new NpcSpec { EditorId = "Npc", Name = "Npc", Greeting = "Hi." } },
        Dialogue =
        {
            new DialogueSpec
            {
                EditorId = "D", QuestEditorId = "Q", SpeakerNpcEditorId = "Npc",
                Prompt = "T", Responses = { "x" },
                Conditions = { new ConditionSpec { Function = "IsInCombat", Comparison = "==", Value = 0 } },
                UseConditionTemplates = used.ToList(),
            },
        },
    };

    [Fact]
    public void Template_ExpandsOntoInfo_AfterInlineConditions()
    {
        var r = TestBuild.Ok(WithTemplate("InTavern"));
        var conds = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
            .Single(t => t.Subtype == DialogTopic.SubtypeEnum.Custom)
            .Responses.Single().Conditions;
        // auto GetIsID speaker gate + inline IsInCombat + template's IsInInterior & GetRandomPercent.
        Assert.Contains(conds, c => c.Data is IIsInCombatConditionDataGetter);
        Assert.Contains(conds, c => c.Data is IIsInInteriorConditionDataGetter);
        Assert.Contains(conds, c => c.Data is IGetRandomPercentConditionDataGetter);
    }

    [Fact]
    public void UnknownTemplate_Reported()
    {
        Assert.Contains(Validate(WithTemplate("Nope")),
            p => p.Contains("unknown template 'Nope'"));
    }

    [Fact]
    public void DuplicateTemplateName_Reported()
    {
        var s = new ModSpec
        {
            ConditionTemplates =
            {
                new ConditionTemplateSpec { Name = "Dup" },
                new ConditionTemplateSpec { Name = "Dup" },
            },
        };
        Assert.Contains(Validate(s), p => p.Contains("'Dup' is defined more than once"));
    }

    [Fact]
    public void TemplateWithBadCondition_Reported()
    {
        var s = new ModSpec
        {
            ConditionTemplates =
            {
                new ConditionTemplateSpec { Name = "T",
                    Conditions = { new ConditionSpec { Function = "NotARealFunction", Comparison = "==", Value = 1 } } },
            },
        };
        Assert.Contains(Validate(s), p => p.Contains("unsupported condition function 'NotARealFunction'"));
    }
}
