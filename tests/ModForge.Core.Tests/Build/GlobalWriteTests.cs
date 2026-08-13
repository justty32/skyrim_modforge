using System;
using System.IO;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// K組 — first-class `globalWrites` on a quest stage: the generated stage fragment emits
// "<global>.SetValue(value)" (no UpdateCurrentInstanceGlobal). Previously only doable by hand-writing
// a fragment or via a dialogue TIF.
public class GlobalWriteTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static QuestSpec FlagQuest() => new()
    {
        EditorId = "MF_Flag", Name = "Flag",
        Stages =
        {
            new StageSpec { Index = 10, GlobalWrites = { new GlobalWriteSpec { Global = "MF_DidIt", Value = 1 } } },
            new StageSpec { Index = 20, GlobalWrites = { new GlobalWriteSpec { Global = "MF_Count", Value = 5 } } },
        },
    };

    [Fact]
    public void NeedsFragmentScript_TrueFromGlobalWritesAlone()
    {
        Assert.True(Generator.QuestNeedsFragmentScript(FlagQuest()));
    }

    [Fact]
    public void Source_DeclaresGlobalProperty_AndEmitsSetValue_NoInstanceUpdate()
    {
        var src = Generator.GenerateQuestFragmentSource(FlagQuest());
        Assert.Contains("GlobalVariable Property MF_DidIt Auto", src);
        Assert.Contains("MF_DidIt.SetValue(1)", src);
        Assert.Contains("MF_Count.SetValue(5)", src);
        Assert.DoesNotContain("UpdateCurrentInstanceGlobal", src);   // it's a plain write, not an instance bind
    }

    [Fact]
    public void Source_WritePlacedInItsStageFragment()
    {
        var src = Generator.GenerateQuestFragmentSource(FlagQuest());
        var frag10 = src.Split("Function Fragment_Stage_0010_Item00000()")[1].Split("EndFunction")[0];
        Assert.Contains("MF_DidIt.SetValue(1)", frag10);
        Assert.DoesNotContain("MF_Count", frag10);   // stage 20's write is in its own fragment
    }

    // For an SM-driven quest the stage fragment never runs, so the write must live in the OnStory handler.
    [Fact]
    public void Source_SmQuest_RoutesGlobalWriteToOnStoryHandler()
    {
        var q = new QuestSpec
        {
            EditorId = "MF_Cast", Name = "Cast",
            StoryEvent = new QuestStoryEventSpec { Event = "CastMagic" },
            Stages = { new StageSpec { Index = 10, StartUpStage = true, GlobalWrites = { new GlobalWriteSpec { Global = "MF_Cast_Flag", Value = 1 } } } },
        };
        var src = Generator.GenerateQuestFragmentSource(q);
        Assert.Contains("Event OnStory", src);
        var handler = src.Split("Event OnStory")[1];
        Assert.Contains("MF_Cast_Flag.SetValue(1)", handler);
    }

    [Fact]
    public void Build_BindsGlobalObjectProperty()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-gw-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "MF_Flag_Stages.pex"), "");
            var spec = new ModSpec
            {
                PluginName = "Test.esp",
                Globals =
                {
                    new GlobalSpec { EditorId = "MF_DidIt", Type = "short", Value = 0 },
                    new GlobalSpec { EditorId = "MF_Count", Type = "short", Value = 0 },
                },
                Quests = { FlagQuest() },
            };
            var r = TestBuild.OkWithCompiledScripts(spec, dir);
            var quest = r.Mod.Quests.Single(q => q.EditorID == "MF_Flag");
            var qa = (QuestAdapter)quest.VirtualMachineAdapter!;
            var entry = qa.Scripts.Single(s => s.Name == "MF_Flag_Stages");
            var glob = r.Mod.Globals.Single(g => g.EditorID == "MF_DidIt");
            var prop = (IScriptObjectPropertyGetter)entry.Properties.Single(p => p.Name == "MF_DidIt");
            Assert.Equal(glob.FormKey, prop.Object.FormKey);
            Assert.Contains(qa.Fragments, f => f.Stage == 10 && f.FragmentName == "Fragment_Stage_0010_Item00000");
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Validate_EmptyGlobal_Reported()
    {
        var spec = new ModSpec { Quests = { new QuestSpec { EditorId = "Q", Name = "Q",
            Stages = { new StageSpec { Index = 10, GlobalWrites = { new GlobalWriteSpec { Global = "" } } } } } } };
        Assert.Contains(Validate(spec), p => p.Contains("globalWrite has empty 'global'"));
    }
}
