using System;
using System.IO;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// #9 UpdateCurrentInstanceGlobal fragment codegen — gather/count radiant quests bind a GLOB to the
// quest instance so objective text "<Global=X>" reads per-instance. Decoded from Missives.
public class InstanceGlobalTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static QuestSpec GatherQuest() => new()
    {
        EditorId = "MF_Gather", Name = "Gather",
        Stages =
        {
            new StageSpec
            {
                Index = 10, StartUpStage = true,
                InstanceGlobals =
                {
                    new InstanceGlobalSpec { Global = "MF_ItemTotal", RandomMin = 3, RandomMax = 6 },
                    new InstanceGlobalSpec { Global = "MF_ItemCount", Value = 0 },
                },
            },
            new StageSpec { Index = 20 },
        },
        Objectives = { new ObjectiveSpec { Index = 10, Text = "Collect <Global=MF_ItemCount>/<Global=MF_ItemTotal>", ShowStage = 10, CompleteStage = 20 } },
    };

    [Fact]
    public void NeedsFragmentScript_TrueFromInstanceGlobalsAlone()
    {
        var q = new QuestSpec
        {
            EditorId = "Q", Name = "Q",
            Stages = { new StageSpec { Index = 5, InstanceGlobals = { new InstanceGlobalSpec { Global = "G" } } } },
        };
        Assert.True(Generator.QuestNeedsFragmentScript(q));       // no objectives, still needs the script
    }

    [Fact]
    public void Source_DeclaresDistinctGlobalProperties()
    {
        var src = Generator.GenerateQuestFragmentSource(GatherQuest());
        Assert.Contains("GlobalVariable Property MF_ItemTotal Auto", src);
        Assert.Contains("GlobalVariable Property MF_ItemCount Auto", src);
    }

    [Fact]
    public void Source_RandomRange_EmitsRandomIntThenUpdate()
    {
        var src = Generator.GenerateQuestFragmentSource(GatherQuest());
        Assert.Contains("MF_ItemTotal.SetValue(Utility.RandomInt(3, 6))", src);
        Assert.Contains("UpdateCurrentInstanceGlobal(MF_ItemTotal)", src);
    }

    [Fact]
    public void Source_FixedValue_EmitsSetValueThenUpdate()
    {
        var src = Generator.GenerateQuestFragmentSource(GatherQuest());
        Assert.Contains("MF_ItemCount.SetValue(0)", src);
        Assert.Contains("UpdateCurrentInstanceGlobal(MF_ItemCount)", src);
    }

    [Fact]
    public void Source_BindOnly_NoSetValue()
    {
        var q = new QuestSpec
        {
            EditorId = "Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, InstanceGlobals = { new InstanceGlobalSpec { Global = "MF_Bind" } } } },
        };
        var src = Generator.GenerateQuestFragmentSource(q);
        Assert.Contains("UpdateCurrentInstanceGlobal(MF_Bind)", src);
        Assert.DoesNotContain("MF_Bind.SetValue", src);
    }

    [Fact]
    public void Source_ObjectiveAndInstanceGlobal_ShareOneStageFragment()
    {
        var src = Generator.GenerateQuestFragmentSource(GatherQuest());
        // Stage 10 has both an objective-display and the instance globals → ONE Fragment_Stage_0010.
        Assert.Single(src.Split("Function Fragment_Stage_0010_Item00000()").Skip(1));
        var frag10 = src.Split("Function Fragment_Stage_0010_Item00000()")[1].Split("EndFunction")[0];
        Assert.Contains("SetObjectiveDisplayed(10)", frag10);
        Assert.Contains("UpdateCurrentInstanceGlobal(MF_ItemTotal)", frag10);
    }

    [Fact]
    public void Build_WithCompiledPex_BindsGlobalObjectProperties()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-ig-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "MF_Gather_Stages.pex"), "");
            var spec = new ModSpec
            {
                PluginName = "Test.esp",
                Globals =
                {
                    new GlobalSpec { EditorId = "MF_ItemTotal", Type = "short", Value = 0 },
                    new GlobalSpec { EditorId = "MF_ItemCount", Type = "short", Value = 0 },
                },
                Quests = { GatherQuest() },
            };
            var r = TestBuild.OkWithCompiledScripts(spec, dir);
            var quest = r.Mod.Quests.Single(q => q.EditorID == "MF_Gather");
            var qa = (QuestAdapter)quest.VirtualMachineAdapter!;
            var entry = qa.Scripts.Single(s => s.Name == "MF_Gather_Stages");
            var total = r.Mod.Globals.Single(g => g.EditorID == "MF_ItemTotal");
            var prop = (IScriptObjectPropertyGetter)entry.Properties.Single(p => p.Name == "MF_ItemTotal");
            Assert.Equal(total.FormKey, prop.Object.FormKey);
            // Stage 10 fragment is bound (it has both an objective and instance globals).
            Assert.Contains(qa.Fragments, f => f.Stage == 10 && f.FragmentName == "Fragment_Stage_0010_Item00000");
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Validate_EmptyGlobal_Reported()
    {
        var spec = new ModSpec { Quests = { new QuestSpec { EditorId = "Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, InstanceGlobals = { new InstanceGlobalSpec { Global = "" } } } } } } };
        Assert.Contains(Validate(spec), p => p.Contains("instanceGlobal has empty 'global'"));
    }

    [Fact]
    public void Validate_OneSidedRandom_Reported()
    {
        var spec = new ModSpec { Quests = { new QuestSpec { EditorId = "Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, InstanceGlobals = {
                new InstanceGlobalSpec { Global = "G", RandomMin = 3 } } } } } } };
        Assert.Contains(Validate(spec), p => p.Contains("needs both randomMin and randomMax"));
    }

    [Fact]
    public void Validate_RandomMinGreaterThanMax_Reported()
    {
        var spec = new ModSpec { Quests = { new QuestSpec { EditorId = "Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, InstanceGlobals = {
                new InstanceGlobalSpec { Global = "G", RandomMin = 9, RandomMax = 2 } } } } } } };
        Assert.Contains(Validate(spec), p => p.Contains("randomMin 9 > randomMax 2"));
    }

    [Fact]
    public void Validate_RandomAndValueConflict_Reported()
    {
        var spec = new ModSpec { Quests = { new QuestSpec { EditorId = "Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, InstanceGlobals = {
                new InstanceGlobalSpec { Global = "G", RandomMin = 1, RandomMax = 4, Value = 2 } } } } } } };
        Assert.Contains(Validate(spec), p => p.Contains("both a random range and a fixed value"));
    }
}
