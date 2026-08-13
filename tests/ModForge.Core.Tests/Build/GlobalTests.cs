using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// GlobalVariable (GLOB) build: short/long/float subtypes, initial value, constant flag, and that a
// generated global is referenceable by editorId from a condition (the whole point — a shared flag a
// CTDA can read with zero Papyrus).
public class GlobalTests
{
    private static ModSpec Spec(params GlobalSpec[] globals)
    {
        var s = new ModSpec { PluginName = "Test.esp" };
        s.Globals.AddRange(globals);
        return s;
    }

    private static IGlobalGetter G(BuildResult r, string ed) =>
        r.Mod.EnumerateMajorRecords<IGlobalGetter>().Single(g => g.EditorID == ed);

    // A short global is a GlobalShort with the initial value.
    [Fact]
    public void Short_BuildsGlobalShort_WithValue()
    {
        var r = TestBuild.Ok(Spec(new GlobalSpec { EditorId = "MF_Flag", Type = "short", Value = 1 }));
        var g = G(r, "MF_Flag");
        var gs = Assert.IsAssignableFrom<IGlobalShortGetter>(g);
        Assert.Equal((short)1, gs.Data);
    }

    // A float global is a GlobalFloat preserving fractional value.
    [Fact]
    public void Float_BuildsGlobalFloat_WithValue()
    {
        var r = TestBuild.Ok(Spec(new GlobalSpec { EditorId = "MF_Chance", Type = "float", Value = 0.25f }));
        var g = G(r, "MF_Chance");
        var gf = Assert.IsAssignableFrom<IGlobalFloatGetter>(g);
        Assert.Equal(0.25f, gf.Data);
    }

    // "long" (and "int" alias) build a GlobalInt.
    [Fact]
    public void Long_BuildsGlobalInt_WithValue()
    {
        var r = TestBuild.Ok(Spec(
            new GlobalSpec { EditorId = "MF_Count", Type = "long", Value = 42 },
            new GlobalSpec { EditorId = "MF_CountB", Type = "int", Value = 7 }));
        Assert.IsAssignableFrom<IGlobalIntGetter>(G(r, "MF_Count"));
        Assert.Equal(42, ((IGlobalIntGetter)G(r, "MF_Count")).Data);
        Assert.IsAssignableFrom<IGlobalIntGetter>(G(r, "MF_CountB"));
    }

    // The constant flag sets the Constant major-record flag.
    [Fact]
    public void Constant_SetsConstantMajorFlag()
    {
        var r = TestBuild.Ok(Spec(new GlobalSpec { EditorId = "MF_Tuning", Type = "float", Value = 1.5f, Constant = true }));
        Assert.True((G(r, "MF_Tuning").MajorRecordFlagsRaw & (int)Global.MajorFlag.Constant) != 0);
    }

    // A built global is referenceable by editorId from a condition's param (GetGlobalValue) — proving
    // the shared-flag use case end to end (condition reads the in-spec GLOB).
    [Fact]
    public void Global_IsReferenceableByEditorId_FromACondition()
    {
        var spec = Spec(new GlobalSpec { EditorId = "MF_Gate", Type = "short", Value = 0 });
        spec.Quests.Add(new QuestSpec { EditorId = "GQ" });
        spec.Npcs.Add(new NpcSpec { EditorId = "GN", Name = "N" });
        spec.Dialogue.Add(new DialogueSpec
        {
            EditorId = "GD", QuestEditorId = "GQ", SpeakerNpcEditorId = "GN",
            Prompt = "hi", Responses = { "hello" },
            Conditions = { new ConditionSpec { Function = "GetGlobalValue", Param = "MF_Gate", Comparison = "==", Value = 0 } },
        });
        var r = TestBuild.Ok(spec);
        var gate = G(r, "MF_Gate").FormKey;
        var data = r.Mod.EnumerateMajorRecords<IDialogResponsesGetter>()
            .SelectMany(i => i.Conditions)
            .Select(c => ((IConditionFloatGetter)c).Data)
            .OfType<IGetGlobalValueConditionDataGetter>()
            .Single();
        Assert.Equal(gate, data.Global.Link.FormKey);
    }

    [Fact]
    public void Validate_FlagsDuplicateEditorId()
    {
        var spec = Spec(
            new GlobalSpec { EditorId = "Dup", Type = "short" },
            new GlobalSpec { EditorId = "Dup", Type = "short" });
        Assert.Contains(Generator.Validate(spec), p => p.Contains("global") && p.Contains("Dup"));
    }

    [Fact]
    public void Validate_FlagsUnknownType()
    {
        var spec = Spec(new GlobalSpec { EditorId = "Bad", Type = "double" });
        Assert.Contains(Generator.Validate(spec), p => p.Contains("global") && p.Contains("type"));
    }
}
