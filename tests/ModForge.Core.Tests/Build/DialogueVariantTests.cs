using System;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// M組 — Dialogue INFO array batch (`variants`). One DialogueSpec entry declares many sibling INFOs under
// one topic, each with the Random flag, sharing the entry's speaker gate + conditions + templates +
// identity, plus its own extra conditions and lines. The ambient-commentary generator (FCO's 265-line
// shared-gate pain point). Fully offline-verifiable: builds records, no Skyrim master needed.
public class DialogueVariantTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static ModSpec SpecWith(DialogueSpec d, params ConditionTemplateSpec[] templates) => new()
    {
        PluginName = "Test.esp",
        Quests = { new QuestSpec { EditorId = "MF_Q", Name = "Q" } },
        Npcs = { new NpcSpec { EditorId = "MF_Npc", Name = "Npc" } },
        ConditionTemplates = templates.ToList(),
        Dialogue = { d },
    };

    private static DialogueSpec Batch() => new()
    {
        EditorId = "MF_Banter", QuestEditorId = "MF_Q", SpeakerNpcEditorId = "MF_Npc",
        Prompt = "Comment", TopLevel = true,
        Variants =
        {
            new DialogueVariantSpec { Responses = { "Nice weather." } },
            new DialogueVariantSpec { Responses = { "I'm tired." }, Emotion = "Sad" },
            new DialogueVariantSpec { Responses = { "Once only." }, SayOnce = true },
        },
    };

    private static IDialogTopicGetter Topic(BuildResult r, string ed) =>
        r.Mod.EnumerateMajorRecords<IDialogTopicGetter>().Single(t => t.EditorID == ed);

    // ---- build: one topic, one INFO per variant, all with Random + speaker gate ----

    [Fact]
    public void Variants_BuildOneInfoPerVariant_UnderOneTopic()
    {
        var r = TestBuild.Ok(SpecWith(Batch()));
        var topic = Topic(r, "MF_Banter");
        // Pure batch (parent has no responses) → exactly 3 INFOs, one per variant.
        Assert.Equal(3, topic.Responses.Count);
        // Each variant INFO carries the Random flag and the GetIsID speaker gate.
        foreach (var info in topic.Responses)
        {
            Assert.True(info.Flags!.Flags.HasFlag(DialogResponses.Flag.Random));
            Assert.Contains(info.Conditions, c => c.Data is IGetIsIDConditionDataGetter);
        }
        // The lines landed on distinct INFOs.
        var texts = topic.Responses.SelectMany(i => i.Responses.Select(rr => rr.Text.String)).ToList();
        Assert.Contains("Nice weather.", texts);
        Assert.Contains("I'm tired.", texts);
        Assert.Contains("Once only.", texts);
    }

    [Fact]
    public void Variant_SayOnce_SetsFlag()
    {
        var r = TestBuild.Ok(SpecWith(Batch()));
        var topic = Topic(r, "MF_Banter");
        var once = topic.Responses.Single(i => i.Responses.Any(rr => rr.Text.String == "Once only."));
        Assert.True(once.Flags!.Flags.HasFlag(DialogResponses.Flag.SayOnce));
        var notOnce = topic.Responses.Single(i => i.Responses.Any(rr => rr.Text.String == "Nice weather."));
        Assert.False(notOnce.Flags!.Flags.HasFlag(DialogResponses.Flag.SayOnce));
    }

    [Fact]
    public void ParentWithResponses_AddsExtraSiblingInfo()
    {
        var d = Batch();
        d.Responses.Add("Parent line.");
        var r = TestBuild.Ok(SpecWith(d));
        var topic = Topic(r, "MF_Banter");
        // parent INFO + 3 variants = 4.
        Assert.Equal(4, topic.Responses.Count);
        Assert.Contains(topic.Responses, i => i.EditorID == "MF_Banter");
    }

    // ---- shared + per-variant conditions ----

    [Fact]
    public void SharedConditions_ApplyToEveryVariant()
    {
        var d = Batch();
        // A shared gate: only when it's night (GetCurrentTime >= 20).
        d.Conditions.Add(new ConditionSpec { Function = "GetCurrentTime", Comparison = ">=", Value = 20 });
        var r = TestBuild.Ok(SpecWith(d));
        var topic = Topic(r, "MF_Banter");
        // Every variant INFO carries the shared GetCurrentTime gate (plus its speaker gate).
        Assert.All(topic.Responses, info =>
            Assert.Contains(info.Conditions, c => c.Data is IGetCurrentTimeConditionDataGetter));
    }

    [Fact]
    public void SharedTemplate_ExpandsOntoEveryVariant()
    {
        var d = Batch();
        d.UseConditionTemplates.Add("nightOnly");
        var tmpl = new ConditionTemplateSpec
        {
            Name = "nightOnly",
            Conditions = { new ConditionSpec { Function = "GetCurrentTime", Comparison = ">=", Value = 20 } },
        };
        var r = TestBuild.Ok(SpecWith(d, tmpl));
        var topic = Topic(r, "MF_Banter");
        Assert.All(topic.Responses, info =>
            Assert.Contains(info.Conditions, c => c.Data is IGetCurrentTimeConditionDataGetter));
    }

    [Fact]
    public void VariantOwnConditions_OnlyOnThatVariant()
    {
        var d = new DialogueSpec
        {
            EditorId = "MF_Banter", QuestEditorId = "MF_Q", SpeakerNpcEditorId = "MF_Npc", Prompt = "C",
            Variants =
            {
                new DialogueVariantSpec { Responses = { "Indoors." },
                    Conditions = { new ConditionSpec { Function = "IsInInterior", Comparison = "==", Value = 1 } } },
                new DialogueVariantSpec { Responses = { "Anywhere." } },
            },
        };
        var r = TestBuild.Ok(SpecWith(d));
        var topic = Topic(r, "MF_Banter");
        var indoors = topic.Responses.Single(i => i.Responses.Any(rr => rr.Text.String == "Indoors."));
        var anywhere = topic.Responses.Single(i => i.Responses.Any(rr => rr.Text.String == "Anywhere."));
        Assert.Contains(indoors.Conditions, c => c.Data is IIsInInteriorConditionDataGetter);
        Assert.DoesNotContain(anywhere.Conditions, c => c.Data is IIsInInteriorConditionDataGetter);
    }

    // ---- validation ----

    [Fact]
    public void Validate_EmptyVariantResponses_Reported()
    {
        var d = new DialogueSpec { EditorId = "MF_B", QuestEditorId = "MF_Q", SpeakerNpcEditorId = "MF_Npc", Prompt = "C",
            Variants = { new DialogueVariantSpec() } };
        Assert.Contains(Validate(SpecWith(d)), p => p.Contains("variant 0 has no response lines"));
    }

    [Fact]
    public void Validate_BatchWithoutParentResponses_NoEmptyLinesError()
    {
        // A pure variant batch (no parent responses) must NOT trip "has no response lines".
        Assert.DoesNotContain(Validate(SpecWith(Batch())), p => p.Contains("has no response lines"));
    }

    [Fact]
    public void Validate_VariantsOnHello_Reported()
    {
        var d = new DialogueSpec { EditorId = "MF_B", QuestEditorId = "MF_Q", SpeakerNpcEditorId = "MF_Npc",
            Hello = true, Variants = { new DialogueVariantSpec { Responses = { "Hi." } } } };
        Assert.Contains(Validate(SpecWith(d)), p => p.Contains("variants are not supported on a hello line"));
    }

    [Fact]
    public void Validate_VariantBadCondition_Reported()
    {
        var d = new DialogueSpec { EditorId = "MF_B", QuestEditorId = "MF_Q", SpeakerNpcEditorId = "MF_Npc", Prompt = "C",
            Variants = { new DialogueVariantSpec { Responses = { "x" },
                Conditions = { new ConditionSpec { Function = "NoSuchFunc" } } } } };
        Assert.Contains(Validate(SpecWith(d)), p => p.Contains("variant 0") && p.Contains("NoSuchFunc"));
    }

    [Fact]
    public void Validate_CleanBatch_NoProblems()
    {
        Assert.Empty(Validate(SpecWith(Batch())));
    }
}
