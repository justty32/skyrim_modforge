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
                    new() { Function = "GetQuestCompleted",     Comparison = "==", Value = 1, Param = "Q" },
                    new() { Function = "GetDistance",           Comparison = "<=", Value = 512, Param = "Skyrim.esm:0x000014" },
                    new() { Function = "GetIsCurrentPackage",   Comparison = "==", Value = 1, Param = "Skyrim.esm:0x01C254" },
                    new() { Function = "GetIsVoiceType",        Comparison = "==", Value = 1, Param = "Skyrim.esm:0x0002F7C3" },
                    new() { Function = "GetQuestRunning",        Comparison = "==", Value = 1, Param = "Q" },
                    new() { Function = "GetInCell",             Comparison = "==", Value = 1, Param = "Skyrim.esm:0x0165A8" },
                    new() { Function = "GetInWorldspace",       Comparison = "==", Value = 1, Param = "Skyrim.esm:0x00003C" },
                    new() { Function = "GetEquipped",           Comparison = "==", Value = 1, Param = "Skyrim.esm:0x012EB7" },
                    new() { Function = "GetDeadCount",          Comparison = ">=", Value = 1, Param = "Skyrim.esm:0x01327E" },
                    new() { Function = "GetSitting",            Comparison = "==", Value = 3 },
                    new() { Function = "GetGold",               Comparison = ">=", Value = 100 },
                    new() { Function = "GetMapMarkerVisible",   Comparison = "==", Value = 1 },
                    new() { Function = "GetStageDone",          Comparison = "==", Value = 1, Param = "Q", Stage = 20 },
                    new() { Function = "GetInCurrentLoc",       Comparison = "==", Value = 1, Param = "Skyrim.esm:0x000165A8" },
                    new() { Function = "GetVMQuestVariable",    Comparison = "==", Value = 0, Param = "Q", VariableName = "PlayerInDialogue" },
                    new() { Function = "GetVMScriptVariable",   Comparison = "==", Value = 0, Param = "Skyrim.esm:0x000014", VariableName = "MyProp" },
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
    [InlineData("GetQuestCompletedConditionData")]
    [InlineData("GetDistanceConditionData")]
    [InlineData("GetIsCurrentPackageConditionData")]
    [InlineData("GetIsVoiceTypeConditionData")]
    [InlineData("GetQuestRunningConditionData")]
    [InlineData("GetInCellConditionData")]
    [InlineData("GetInWorldspaceConditionData")]
    [InlineData("GetEquippedConditionData")]
    [InlineData("GetDeadCountConditionData")]
    [InlineData("GetSittingConditionData")]
    [InlineData("GetGoldConditionData")]
    [InlineData("GetMapMarkerVisibleConditionData")]
    [InlineData("GetStageDoneConditionData")]
    [InlineData("GetInCurrentLocConditionData")]
    [InlineData("GetVMQuestVariableConditionData")]
    [InlineData("GetVMScriptVariableConditionData")]
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

    // GetStageDone carries the stage index as a PARAMETER (distinct from the comparison value).
    [Fact]
    public void GetStageDone_CarriesStageParameter()
    {
        var r = BuildAllConditions();
        var info = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
                    .Single(t => t.Subtype == DialogTopic.SubtypeEnum.Custom)
                    .Responses.Single();
        var sd = info.Conditions
            .Select(c => c.Data).OfType<IGetStageDoneConditionDataGetter>().Single();
        Assert.Equal(20, sd.Stage);
    }

    // GetIsAliasRef resolves an alias NAME to the owning quest's alias index (the engine compares
    // the run-on actor against the ref filling that alias). A wrong index gates the wrong actor.
    [Fact]
    public void GetIsAliasRef_ResolvesAliasNameToIndex()
    {
        var r = TestBuild.Ok(new ModSpec
        {
            Quests =
            {
                new QuestSpec
                {
                    EditorId = "Q", Name = "Q",
                    Aliases =
                    {
                        new QuestAliasSpec { Name = "Hero",   Fill = "forced:Skyrim.esm:0x000014" },
                        new QuestAliasSpec { Name = "Victim", Fill = "forced:Skyrim.esm:0x000014" },
                    },
                },
            },
            Npcs = { new NpcSpec { EditorId = "Npc", Name = "Npc", Greeting = "Hi." } },
            Dialogue =
            {
                new DialogueSpec
                {
                    EditorId = "D", QuestEditorId = "Q", SpeakerNpcEditorId = "Npc",
                    Prompt = "T", Responses = { "x" },
                    Conditions = { new() { Function = "GetIsAliasRef", Comparison = "==", Value = 1, Alias = "Victim" } },
                },
            },
        });
        var info = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
                    .Single(t => t.Subtype == DialogTopic.SubtypeEnum.Custom)
                    .Responses.Single();
        var ar = info.Conditions
            .Select(c => c.Data).OfType<IGetIsAliasRefConditionDataGetter>().Single();
        Assert.Equal(1, ar.ReferenceAliasIndex);   // "Victim" is the 2nd alias → index 1
    }

    // A GetIsAliasRef with no owning-quest context (e.g. on an AI package) is dropped with a warning,
    // never emitted as a malformed condition.
    [Fact]
    public void GetIsAliasRef_WithoutQuestContext_IsDropped()
    {
        var r = TestBuild.Raw(new ModSpec   // Raw: this spec intentionally warns (alias-ref dropped)
        {
            Packages =
            {
                new PackageSpec
                {
                    EditorId = "Pkg", Template = "Skyrim.esm:0x01C254",   // Sandbox
                    Conditions = { new() { Function = "GetIsAliasRef", Comparison = "==", Value = 1, Alias = "Victim" } },
                },
            },
        });
        var pkg = r.Mod.EnumerateMajorRecords<IPackageGetter>().Single(p => p.EditorID == "Pkg");
        Assert.DoesNotContain(pkg.Conditions, c => c.Data is IGetIsAliasRefConditionDataGetter);
    }

    // Validate flags a GetIsAliasRef with no alias name (it can't resolve an index without one).
    // Routed through CheckCondition, which validates perk / storyEvent / findMatching conditions.
    [Fact]
    public void Validate_GetIsAliasRef_WithoutAlias_IsReported()
    {
        var spec = new ModSpec
        {
            Perks =
            {
                new PerkSpec
                {
                    EditorId = "P", Name = "P", NumRanks = 1,
                    Conditions = { new() { Function = "GetIsAliasRef", Comparison = "==", Value = 1 } },
                    Effects = { new PerkEffectSpec { Kind = "entryPoint", EntryPoint = "ModAttackDamage", Function = "Multiply", Value = 1f } },
                },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("GetIsAliasRef needs an alias"));
    }

    // GetVMQuestVariable/GetVMScriptVariable carry the property NAME string (else the engine reads
    // nothing) and the form arg (the quest / the object whose attached script is read).
    [Fact]
    public void GetVmVariable_CarriesPropertyNameAndForm()
    {
        var r = BuildAllConditions();
        var info = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
                    .Single(t => t.Subtype == DialogTopic.SubtypeEnum.Custom)
                    .Responses.Single();
        var q = info.Conditions.Select(c => c.Data).OfType<IGetVMQuestVariableConditionDataGetter>().Single();
        Assert.Equal("PlayerInDialogue", q.VariableName);
        Assert.False(q.Quest.Link.IsNull);
        var s = info.Conditions.Select(c => c.Data).OfType<IGetVMScriptVariableConditionDataGetter>().Single();
        Assert.Equal("MyProp", s.VariableName);
        Assert.False(s.Target.Link.IsNull);
    }

    // Validate flags a GetVMQuestVariable missing its variableName (the engine would read nothing).
    [Fact]
    public void Validate_GetVmQuestVariable_WithoutVariableName_IsReported()
    {
        var spec = new ModSpec
        {
            Perks =
            {
                new PerkSpec
                {
                    EditorId = "P", Name = "P", NumRanks = 1,
                    Conditions = { new() { Function = "GetVMQuestVariable", Comparison = "==", Value = 0, Param = "Skyrim.esm:0x000014" } },
                    Effects = { new PerkEffectSpec { Kind = "entryPoint", EntryPoint = "ModAttackDamage", Function = "Multiply", Value = 1f } },
                },
            },
        };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("needs a variableName"));
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
