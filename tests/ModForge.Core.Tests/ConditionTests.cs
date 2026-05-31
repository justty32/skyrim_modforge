using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Locks the CTDA function→ConditionData mapping in Generator.Build.Conditions.cs. A wrong mapping
// produces a structurally-valid condition that gates on the WRONG thing (silent in-game failure).
public class ConditionTests
{
    // One dialogue whose INFO carries one condition of every supported function. Build wires them
    // onto the INFO (after the auto GetIsID speaker gate). All param refs are external → no master.
    private static BuildResult BuildAllConditions() => TestBuild.Ok(new ModSpec
    {
        Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
        Npcs   = { new NpcSpec  { EditorId = "Npc", Name = "Npc", Greeting = "Hi." } },
        Dialogue =
        {
            new DialogueSpec
            {
                EditorId = "D", QuestEditorId = "Q", SpeakerNpcEditorId = "Npc",
                Prompt = "T", Responses = { "x" },
                Conditions =
                {
                    new() { Function = "GetCurrentTime",        Comparison = ">=", Value = 20 },
                    new() { Function = "IsInInterior",          Comparison = "==", Value = 1 },
                    new() { Function = "IsInCombat",            Comparison = "==", Value = 1 },
                    new() { Function = "GetRandomPercent",      Comparison = "<",  Value = 25 },
                    new() { Function = "GetActorValuePercent",  Comparison = "<",  Value = 0.5f, ActorValue = "Health" },
                    new() { Function = "GetActorValue",         Comparison = "==", Value = 0, ActorValue = "WaitingForPlayer" },
                    new() { Function = "GetInFaction",          Comparison = "==", Value = 1, Param = "Skyrim.esm:0x05C84E" },
                    new() { Function = "GetGlobalValue",        Comparison = "==", Value = 0, Param = "Skyrim.esm:0x0BCC98" },
                    new() { Function = "GetRelationshipRank",   Comparison = ">=", Value = 1, Param = "Skyrim.esm:0x000014" },
                },
            },
        },
    });

    [Theory]
    [InlineData("GetCurrentTimeConditionData")]
    [InlineData("IsInInteriorConditionData")]
    [InlineData("IsInCombatConditionData")]
    [InlineData("GetRandomPercentConditionData")]
    [InlineData("GetActorValuePercentConditionData")]
    [InlineData("GetActorValueConditionData")]
    [InlineData("GetInFactionConditionData")]
    [InlineData("GetGlobalValueConditionData")]
    [InlineData("GetRelationshipRankConditionData")]
    public void EachFunction_MapsToItsConditionData(string expectedType)
    {
        var r = BuildAllConditions();
        var info = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
                    .Single(t => t.Subtype == DialogTopic.SubtypeEnum.Custom)
                    .Responses.Single();
        Assert.Contains(info.Conditions, c => c.Data.GetType().Name == expectedType);
    }

    // The auto GetIsID speaker gate is always present alongside the spec conditions.
    [Fact]
    public void Info_KeepsAutoGetIsIdGate()
    {
        var r = BuildAllConditions();
        var info = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
                    .Single(t => t.Subtype == DialogTopic.SubtypeEnum.Custom)
                    .Responses.Single();
        Assert.Contains(info.Conditions, c => c.Data.GetType().Name == "GetIsIDConditionData");
    }

    // GetActorValuePercent must carry the parsed ActorValue (else it reads the wrong stat).
    [Fact]
    public void GetActorValuePercent_CarriesActorValue()
    {
        var r = BuildAllConditions();
        var info = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
                    .Single(t => t.Subtype == DialogTopic.SubtypeEnum.Custom)
                    .Responses.Single();
        var av = info.Conditions
            .Select(c => c.Data).OfType<IGetActorValuePercentConditionDataGetter>().Single();
        Assert.Equal(ActorValue.Health, av.ActorValue);
    }

    // Comparison string → CompareOperator, and `or:true` sets the OR flag.
    [Fact]
    public void Comparison_And_OrFlag_Map()
    {
        var r = TestBuild.Ok(new ModSpec
        {
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Npcs   = { new NpcSpec  { EditorId = "Npc", Name = "Npc", Greeting = "Hi." } },
            Dialogue =
            {
                new DialogueSpec
                {
                    EditorId = "D", QuestEditorId = "Q", SpeakerNpcEditorId = "Npc",
                    Prompt = "T", Responses = { "x" },
                    Conditions =
                    {
                        new() { Function = "IsInInterior", Comparison = "<", Value = 1, Or = true },
                        new() { Function = "IsInCombat",   Comparison = "==", Value = 0 },
                    },
                },
            },
        });
        var conds = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
                    .Single(t => t.Subtype == DialogTopic.SubtypeEnum.Custom)
                    .Responses.Single().Conditions;
        var lt = conds.Single(c => c.Data.GetType().Name == "IsInInteriorConditionData");
        Assert.Equal(CompareOperator.LessThan, lt.CompareOperator);
        Assert.True(lt.Flags.HasFlag(Condition.Flag.OR));
    }
}
