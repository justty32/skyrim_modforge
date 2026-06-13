using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class ObjectiveTargetTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    // A non-storyEvent quest with aliases forced to a vanilla ref builds fully offline (external refs
    // resolve to a FormKey without loading the master).
    [Fact]
    public void Objective_target_emits_QSTA_with_alias_index_flag_and_condition()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFQM_Q", Name = "Q", Type = "SideQuest",
            Stages = { new StageSpec { Index = 10, StartUpStage = true } },
            Aliases =
            {
                new QuestAliasSpec { Name = "Bystander", Fill = "forced:Skyrim.esm:0x000014" },
                new QuestAliasSpec { Name = "Goal",      Fill = "forced:Skyrim.esm:0x000014" },
            },
            Objectives =
            {
                new ObjectiveSpec
                {
                    Index = 10, Text = "Reach the goal", ShowStage = 10,
                    Targets =
                    {
                        new ObjectiveTargetSpec
                        {
                            Alias = "Goal", CompassIgnoresLocks = true,
                            Conditions = { new ConditionSpec { Function = "GetInFaction", Comparison = "==", Value = 1, Param = "Skyrim.esm:0x05C84E" } },
                        },
                    },
                },
            },
        });

        var mod = Build(spec);
        var q = mod.Quests.Single(x => x.EditorID == "MFQM_Q");
        var goalId = q.Aliases.Single(a => a.Name == "Goal").ID;
        var obj = q.Objectives.Single(o => o.Index == 10);
        var t = Assert.Single(obj.Targets);
        Assert.Equal((int)goalId, t.AliasID);
        Assert.True(t.Flags.HasFlag(Quest.TargetFlag.CompassMarkerIgnoresLocks));
        Assert.Single(t.Conditions);
    }

    [Fact]
    public void Objective_target_naming_missing_alias_is_a_validate_problem()
    {
        var spec = new ModSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "MFQM_Bad", Name = "Bad", Type = "SideQuest",
            Stages = { new StageSpec { Index = 10, StartUpStage = true } },
            Aliases = { new QuestAliasSpec { Name = "Real", Fill = "forced:Skyrim.esm:0x000014" } },
            Objectives =
            {
                new ObjectiveSpec { Index = 10, Text = "x", ShowStage = 10,
                    Targets = { new ObjectiveTargetSpec { Alias = "Ghost" } } },
            },
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MFQM_Bad") && p.Contains("Ghost"));
    }
}
