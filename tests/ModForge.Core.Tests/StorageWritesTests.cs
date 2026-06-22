using System;
using System.IO;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// J組 — PapyrusUtil StorageUtil per-Form KV writes. A dialogue line / quest stage carries `storageWrites`
// (lightweight flat scalar KV), emitted into the line's TIF result fragment or the stage fragment. The
// save manages StorageUtil automatically, and the three supported targets (speaker/player/none) are pure
// Papyrus expressions, so storageWrites binds NO VMAD property — emission is a pure, offline-verifiable
// string function; only compiling the .psc needs PapyrusUtil headers (main machine).
public class StorageWritesTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static DialogueSpec Line(params StorageWriteSpec[] writes) => new()
    {
        EditorId = "MF_Greet", QuestEditorId = "MF_Q", SpeakerNpcEditorId = "MF_Npc",
        Prompt = "Hello", Responses = { "Hi there." },
        StorageWrites = writes.ToList(),
    };

    // ---- source emission (dialogue TIF) ----

    [Fact]
    public void Storage_SpeakerInt_EmitsSetIntValue()
    {
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "mymod_talked", Target = "speaker", Int = 1 }));
        Assert.Equal("TIF_MF_Greet", Generator.DialogueFragmentScriptName(Line(
            new StorageWriteSpec { Key = "mymod_talked", Target = "speaker", Int = 1 })));
        Assert.Contains("StorageUtil.SetIntValue(akSpeakerRef, \"mymod_talked\", 1)", src);
    }

    [Fact]
    public void Storage_DefaultTarget_IsSpeaker()
    {
        // Empty target defaults to the dialogue speaker (akSpeakerRef).
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "k", Int = 5 }));
        Assert.Contains("StorageUtil.SetIntValue(akSpeakerRef, \"k\", 5)", src);
    }

    [Fact]
    public void Storage_IntDelta_UsesAdjustIntValue()
    {
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "count", Target = "player", Int = 2, Delta = true }));
        Assert.Contains("StorageUtil.AdjustIntValue(Game.GetPlayer(), \"count\", 2)", src);
    }

    [Fact]
    public void Storage_FloatSetAndDelta()
    {
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "ratio", Target = "speaker", Float = 0.25f },
            new StorageWriteSpec { Key = "acc", Target = "speaker", Float = 1.5f, Delta = true }));
        Assert.Contains("StorageUtil.SetFloatValue(akSpeakerRef, \"ratio\", 0.25)", src);
        Assert.Contains("StorageUtil.AdjustFloatValue(akSpeakerRef, \"acc\", 1.5)", src);
    }

    [Fact]
    public void Storage_StringSet_NoneTarget()
    {
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "mood", Target = "none", Str = "happy" }));
        Assert.Contains("StorageUtil.SetStringValue(None, \"mood\", \"happy\")", src);
    }

    [Fact]
    public void Storage_GlobalTarget_AliasesToNone()
    {
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "g", Target = "global", Int = 3 }));
        Assert.Contains("StorageUtil.SetIntValue(None, \"g\", 3)", src);
    }

    [Fact]
    public void NoStorage_NoFragment()
    {
        Assert.Equal("", Generator.GenerateDialogueFragmentSource(Line()));
        Assert.Equal("", Generator.DialogueFragmentScriptName(Line()));
        Assert.False(Generator.HasStorageWrites(Line()));
    }

    // ---- build: a storage-only line attaches the TIF fragment when the .pex is present ----

    [Fact]
    public void Build_StorageOnly_AttachesTifWhenPexPresent()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-sw-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "TIF_MF_Greet.pex"), "");
            var spec = new ModSpec
            {
                PluginName = "Test.esp",
                Quests = { new QuestSpec { EditorId = "MF_Q", Name = "Q" } },
                Npcs = { new NpcSpec { EditorId = "MF_Npc", Name = "Npc" } },
                Dialogue = { Line(new StorageWriteSpec { Key = "k", Target = "speaker", Int = 1 }) },
            };
            var r = TestBuild.OkWithCompiledScripts(spec, dir);
            var info = r.Mod.EnumerateMajorRecords<IDialogResponsesGetter>().Single(i => i.EditorID == "MF_Greet");
            Assert.Contains(info.VirtualMachineAdapter!.Scripts, e => e.Name == "TIF_MF_Greet");
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    // ---- validation ----

    private static ModSpec WithLine(DialogueSpec d) => new()
    {
        PluginName = "Test.esp",
        Quests = { new QuestSpec { EditorId = "MF_Q", Name = "Q" } },
        Npcs = { new NpcSpec { EditorId = "MF_Npc", Name = "Npc" } },
        Dialogue = { d },
    };

    [Fact]
    public void Validate_EmptyKey_Reported()
    {
        var d = Line(new StorageWriteSpec { Key = "", Int = 1 });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("storageWrite") && p.Contains("empty key"));
    }

    [Fact]
    public void Validate_MultipleValueTypes_Reported()
    {
        var d = Line(new StorageWriteSpec { Key = "k", Int = 1, Float = 2f });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("must set exactly one of int/float/str"));
    }

    [Fact]
    public void Validate_DeltaOnString_Reported()
    {
        var d = Line(new StorageWriteSpec { Key = "k", Str = "x", Delta = true });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("delta only applies to int/float"));
    }

    [Fact]
    public void Validate_UnresolvedRefTarget_Reported()
    {
        // A non-keyword target is treated as an arbitrary ref; an unknown one must be reported.
        var d = Line(new StorageWriteSpec { Key = "k", Target = "nonsense", Int = 1 });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("target") && p.Contains("unresolved ref 'nonsense'"));
    }

    [Fact]
    public void Validate_CleanSpec_NoProblems()
    {
        var d = Line(new StorageWriteSpec { Key = "k", Target = "player", Int = 1, Delta = true });
        Assert.DoesNotContain(Validate(WithLine(d)), p => p.Contains("storageWrite"));
    }

    // ---- arbitrary-ref target (a placed-ref / base form, bound as a Form property) ----

    [Fact]
    public void RefTarget_DeclaresFormProperty_AndBodyUsesIt()
    {
        // An external-ref target classifies as Ref → SWRef_0 Form property, body keys on it.
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "metPlayer", Target = "Skyrim.esm:0x000014", Int = 1 }));
        Assert.Contains("Form Property SWRef_0 Auto", src);
        Assert.Contains("StorageUtil.SetIntValue(SWRef_0, \"metPlayer\", 1)", src);
    }

    [Fact]
    public void RefTarget_OnlyRefEntriesDeclareProperties()
    {
        // Entry 0 = player (no prop), entry 1 = ref (SWRef_1 — index is the LIST position, not a ref counter).
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "a", Target = "player", Int = 1 },
            new StorageWriteSpec { Key = "b", Target = "Skyrim.esm:0x000014", Int = 2 }));
        Assert.DoesNotContain("Form Property SWRef_0 Auto", src);
        Assert.Contains("Form Property SWRef_1 Auto", src);
        Assert.Contains("StorageUtil.SetIntValue(Game.GetPlayer(), \"a\", 1)", src);
        Assert.Contains("StorageUtil.SetIntValue(SWRef_1, \"b\", 2)", src);
    }

    [Fact]
    public void Build_RefTarget_BindsFormPropertyToFormKey()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-swref-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "TIF_MF_Greet.pex"), "");
            // Target the in-spec NPC base form by editorId → SWRef_0 binds to its FormKey.
            var spec = new ModSpec
            {
                PluginName = "Test.esp",
                Quests = { new QuestSpec { EditorId = "MF_Q", Name = "Q" } },
                Npcs = { new NpcSpec { EditorId = "MF_Npc", Name = "Npc" } },
                Dialogue = { Line(new StorageWriteSpec { Key = "k", Target = "MF_Npc", Int = 1 }) },
            };
            var r = TestBuild.OkWithCompiledScripts(spec, dir);
            var npc = r.Mod.EnumerateMajorRecords<INpcGetter>().Single(n => n.EditorID == "MF_Npc");
            var info = r.Mod.EnumerateMajorRecords<IDialogResponsesGetter>().Single(i => i.EditorID == "MF_Greet");
            var prop = info.VirtualMachineAdapter!.Scripts.Single(e => e.Name == "TIF_MF_Greet")
                .Properties.OfType<IScriptObjectPropertyGetter>().Single(p => p.Name == "SWRef_0");
            Assert.Equal(npc.FormKey, prop.Object.FormKey);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Validate_ResolvedRefTarget_NoProblem()
    {
        var d = Line(new StorageWriteSpec { Key = "k", Target = "MF_Npc", Int = 1 });
        Assert.DoesNotContain(Validate(WithLine(d)), p => p.Contains("storageWrite") || p.Contains("unresolved ref"));
    }

    // ---- fromJson value source (PapyrusUtil JsonUtil external-config read) ----

    [Fact]
    public void FromJson_Int_EmitsJsonUtilGetPathIntValue_LiteralIsMissingDefault()
    {
        // Path API (GetPath…Value, leading-dot path) — Get…Value reads only JsonUtil's own flat namespace,
        // which is empty for a hand-authored external config (in-game confirmed 2026-06-22).
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "diff", Target = "player", Int = 1,
                FromJson = new JsonReadSpec { File = "MyMod/config.json", Key = "difficulty" } }));
        Assert.Contains(
            "StorageUtil.SetIntValue(Game.GetPlayer(), \"diff\", JsonUtil.GetPathIntValue(\"MyMod/config.json\", \".difficulty\", 1))",
            src);
    }

    [Fact]
    public void FromJson_FloatAndString()
    {
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "rate", Target = "none", Float = 0.5f,
                FromJson = new JsonReadSpec { File = "c.json", Key = "rate" } },
            new StorageWriteSpec { Key = "name", Target = "none", Str = "x",
                FromJson = new JsonReadSpec { File = "c.json", Key = "name" } }));
        Assert.Contains("StorageUtil.SetFloatValue(None, \"rate\", JsonUtil.GetPathFloatValue(\"c.json\", \".rate\", 0.5))", src);
        Assert.Contains("StorageUtil.SetStringValue(None, \"name\", JsonUtil.GetPathStringValue(\"c.json\", \".name\", \"x\"))", src);
    }

    [Fact]
    public void FromJson_DottedPathKept_BareKeyGetsLeadingDot()
    {
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "n", Target = "player", Int = 0,
                FromJson = new JsonReadSpec { File = "c.json", Key = ".tuning.spawnCount" } }));
        Assert.Contains("JsonUtil.GetPathIntValue(\"c.json\", \".tuning.spawnCount\", 0)", src);
    }

    [Fact]
    public void FromJson_WithDelta_AdjustsByJsonValue()
    {
        var src = Generator.GenerateDialogueFragmentSource(Line(
            new StorageWriteSpec { Key = "bonus", Target = "player", Int = 0, Delta = true,
                FromJson = new JsonReadSpec { File = "c.json", Key = "bonus" } }));
        Assert.Contains("StorageUtil.AdjustIntValue(Game.GetPlayer(), \"bonus\", JsonUtil.GetPathIntValue(\"c.json\", \".bonus\", 0))", src);
    }

    [Fact]
    public void Validate_FromJson_EmptyFileOrKey_Reported()
    {
        var noFile = Line(new StorageWriteSpec { Key = "k", Target = "player", Int = 1,
            FromJson = new JsonReadSpec { File = "", Key = "x" } });
        Assert.Contains(Validate(WithLine(noFile)), p => p.Contains("fromJson has empty file"));
        var noKey = Line(new StorageWriteSpec { Key = "k", Target = "player", Int = 1,
            FromJson = new JsonReadSpec { File = "c.json", Key = "" } });
        Assert.Contains(Validate(WithLine(noKey)), p => p.Contains("fromJson has empty key"));
    }

    // ---- stage-fragment storage writes (host is a quest STAGE, no akSpeakerRef) ----

    private static QuestSpec StageStorageQuest() => new()
    {
        EditorId = "MF_Q", Name = "Q",
        Stages =
        {
            new StageSpec
            {
                Index = 10,
                StorageWrites = { new StorageWriteSpec { Key = "won", Target = "player", Int = 1, Delta = true } },
            },
        },
    };

    [Fact]
    public void StageStorage_EmitsInStageFragment()
    {
        var q = StageStorageQuest();
        Assert.True(Generator.QuestNeedsFragmentScript(q));
        var src = Generator.GenerateQuestFragmentSource(q);
        var frag = src.Split("Function Fragment_Stage_0010_Item00000()")[1].Split("EndFunction")[0];
        Assert.Contains("StorageUtil.AdjustIntValue(Game.GetPlayer(), \"won\", 1)", frag);
    }

    [Fact]
    public void StageRefTarget_DeclaresPrefixedFormProperty()
    {
        // A stage ref target namespaces its Form property by the stage prefix (S0010_) so stages don't collide.
        var q = new QuestSpec
        {
            EditorId = "MF_Q", Name = "Q",
            Stages = { new StageSpec { Index = 10,
                StorageWrites = { new StorageWriteSpec { Key = "k", Target = "Skyrim.esm:0x000014", Int = 1 } } } },
        };
        var src = Generator.GenerateQuestFragmentSource(q);
        Assert.Contains("Form Property S0010_SWRef_0 Auto", src);
        Assert.Contains("StorageUtil.SetIntValue(S0010_SWRef_0, \"k\", 1)", src);
    }

    [Fact]
    public void Validate_StageSpeakerTarget_Rejected()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q",
                Stages = { new StageSpec { Index = 10,
                    StorageWrites = { new StorageWriteSpec { Key = "k", Target = "speaker", Int = 1 } } } } } },
        };
        Assert.Contains(Validate(spec), p => p.Contains("target 'speaker' is only valid on a dialogue line"));
    }

    [Fact]
    public void Validate_StageDefaultTarget_RejectedAsSpeaker()
    {
        // Empty target defaults to speaker, which a stage cannot key on.
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q",
                Stages = { new StageSpec { Index = 10,
                    StorageWrites = { new StorageWriteSpec { Key = "k", Int = 1 } } } } } },
        };
        Assert.Contains(Validate(spec), p => p.Contains("target 'speaker' is only valid on a dialogue line"));
    }

    // ---- SM-event-driven storage: runs in the OnStory<Event> handler, not the stage fragment ----

    private static QuestSpec CastMagicStorageQuest() => new()
    {
        EditorId = "MFSkill_Q", Name = "Q", Type = "None",
        Stages =
        {
            new StageSpec
            {
                Index = 10,
                StorageWrites = { new StorageWriteSpec { Key = "casts", Target = "player", Int = 1, Delta = true } },
            },
        },
        StoryEvent = new QuestStoryEventSpec { Event = "CastMagic" },
    };

    [Fact]
    public void StoryEventStorage_RunsInOnStoryHandler_NotStageFragment()
    {
        var q = CastMagicStorageQuest();
        Assert.True(Generator.StoryHandlerNeeded(q));
        var src = Generator.GenerateQuestFragmentSource(q);
        Assert.Contains("Event OnStoryCastMagic", src);
        var handler = src.Split("Event OnStoryCastMagic")[1];
        Assert.Contains("StorageUtil.AdjustIntValue(Game.GetPlayer(), \"casts\", 1)", handler);
        // The stage fragment must NOT also carry it (it never runs for an SM quest — would double-count).
        var stageFrag = src.Split("Function Fragment_Stage_0010_Item00000()")[1].Split("EndFunction")[0];
        Assert.DoesNotContain("StorageUtil", stageFrag);
    }
}
