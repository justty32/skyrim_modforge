using System;
using System.IO;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Idea #20 in-world skill tree — Phase 0 JContainers JFormDB persistence. A dialogue line carries
// `persist` (nested per-Form JFormDB writes) and/or `syncPerks` (AddPerk/RemovePerk from stored ranks),
// emitted into the line's TIF result fragment. Only the root-DB path API (solveXxxSetter/solveXxx) is
// used, so there is no retain/release lifecycle (design unknown U5). Pure string emission + the VMAD
// property binding are fully offline-verifiable; only compiling the .psc needs JContainers headers.
public class JContainersPersistTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static DialogueSpec Line(PersistSpec? persist = null, SyncPerksSpec? sync = null) => new()
    {
        EditorId = "MF_Train", QuestEditorId = "MF_Q", SpeakerNpcEditorId = "MF_Npc",
        Prompt = "Train Endurance", Responses = { "Watch and learn." },
        Persist = persist, SyncPerks = sync,
    };

    private static PersistSpec EndurancePersist() => new()
    {
        Storage = "ModForgeNpcSkills", Key = "speaker",
        Set =
        {
            new PersistEntrySpec { Path = ".Endurance.level", Int = 1, Delta = true },
            new PersistEntrySpec { Path = ".Endurance.nodes.Adaptation", Int = 2 },
            new PersistEntrySpec { Path = ".Endurance.ratio", Float = 0.25f, Delta = true },
        },
    };

    // ---- source emission ----

    [Fact]
    public void Persist_EmitsSolveSetterCalls_WithStorageComposedPath()
    {
        var src = Generator.GenerateDialogueFragmentSource(Line(EndurancePersist()));
        Assert.Equal("TIF_MF_Train", Generator.DialogueFragmentScriptName(Line(EndurancePersist())));
        // plain int set → solveIntSetter with the full ".<storage><path>" and createMissingKeys=true.
        Assert.Contains("JFormDB.solveIntSetter(akSpeakerRef, \".ModForgeNpcSkills.Endurance.nodes.Adaptation\", 2, true)", src);
        // delta int → read-add-write.
        Assert.Contains("int __pv0 = JFormDB.solveInt(akSpeakerRef, \".ModForgeNpcSkills.Endurance.level\", 0)", src);
        Assert.Contains("JFormDB.solveIntSetter(akSpeakerRef, \".ModForgeNpcSkills.Endurance.level\", __pv0 + 1, true)", src);
        // delta float → read-add-write with a Papyrus float literal.
        Assert.Contains("float __pv2 = JFormDB.solveFlt(akSpeakerRef, \".ModForgeNpcSkills.Endurance.ratio\", 0.0)", src);
        Assert.Contains("__pv2 + 0.25", src);
    }

    [Fact]
    public void Persist_PlayerKey_UsesGetPlayer()
    {
        var p = new PersistSpec { Storage = "MFPlayer", Key = "player", Set = { new PersistEntrySpec { Path = ".gold", Int = 5 } } };
        var src = Generator.GenerateDialogueFragmentSource(Line(p));
        Assert.Contains("JFormDB.solveIntSetter(Game.GetPlayer(), \".MFPlayer.gold\", 5, true)", src);
    }

    [Fact]
    public void Persist_FormValue_DeclaresAndUsesFormProperty()
    {
        var p = new PersistSpec { Storage = "S", Set = { new PersistEntrySpec { Path = ".lastTrainer", Form = "MF_Npc" } } };
        var src = Generator.GenerateDialogueFragmentSource(Line(p));
        Assert.Contains("Form Property PF_0 Auto", src);
        Assert.Contains("JFormDB.solveFormSetter(akSpeakerRef, \".S.lastTrainer\", PF_0, true)", src);
    }

    [Fact]
    public void SyncPerks_EmitsPerkPropertiesAndRankGate()
    {
        var sync = new SyncPerksSpec
        {
            Storage = "ModForgeNpcSkills", Key = "speaker",
            Nodes =
            {
                new SyncPerkNodeSpec { Path = ".Endurance.nodes.Adaptation", Perk = "MF_AdaptPerk", MinRank = 2 },
                new SyncPerkNodeSpec { Path = ".Endurance.nodes.Windbreaker", Perk = "MF_WindPerk" },
            },
        };
        var src = Generator.GenerateDialogueFragmentSource(Line(sync: sync));
        Assert.Contains("Perk Property SyncPerk_0 Auto", src);
        Assert.Contains("Perk Property SyncPerk_1 Auto", src);
        Assert.Contains("Actor __sp = akSpeakerRef as Actor", src);
        Assert.Contains("If JFormDB.solveInt(akSpeakerRef, \".ModForgeNpcSkills.Endurance.nodes.Adaptation\", 0) >= 2", src);
        Assert.Contains("__sp.AddPerk(SyncPerk_0)", src);
        Assert.Contains("__sp.RemovePerk(SyncPerk_0)", src);
        // default minRank is 1.
        Assert.Contains("If JFormDB.solveInt(akSpeakerRef, \".ModForgeNpcSkills.Endurance.nodes.Windbreaker\", 0) >= 1", src);
        Assert.Contains("__sp.AddPerk(SyncPerk_1)", src);
    }

    [Fact]
    public void NoPersistNoSync_NoFragment()
    {
        Assert.Equal("", Generator.GenerateDialogueFragmentSource(Line()));
        Assert.Equal("", Generator.DialogueFragmentScriptName(Line()));
        Assert.False(Generator.HasPersist(Line()));
        Assert.False(Generator.HasSyncPerks(Line()));
    }

    // ---- build: VMAD property binding (needs the compiled .pex present) ----

    [Fact]
    public void Build_WithPex_BindsFormAndPerkProperties()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-jc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "TIF_MF_Train.pex"), "");
            var spec = new ModSpec
            {
                PluginName = "Test.esp",
                Quests = { new QuestSpec { EditorId = "MF_Q", Name = "Q" } },
                Npcs = { new NpcSpec { EditorId = "MF_Npc", Name = "Npc" } },
                Perks = { new PerkSpec { EditorId = "MF_AdaptPerk", Name = "Adaptation" } },
                Dialogue =
                {
                    Line(
                        new PersistSpec { Storage = "S", Set = { new PersistEntrySpec { Path = ".lastTrainer", Form = "MF_Npc" } } },
                        new SyncPerksSpec { Storage = "S", Nodes = { new SyncPerkNodeSpec { Path = ".n", Perk = "MF_AdaptPerk" } } }),
                },
            };

            var r = TestBuild.OkWithCompiledScripts(spec, dir);
            var npc = r.Mod.Npcs.Single(n => n.EditorID == "MF_Npc");
            var perk = r.Mod.Perks.Single(p => p.EditorID == "MF_AdaptPerk");
            var info = r.Mod.EnumerateMajorRecords<IDialogResponsesGetter>().Single(i => i.EditorID == "MF_Train");
            var props = info.VirtualMachineAdapter!.Scripts.Single(e => e.Name == "TIF_MF_Train").Properties;
            var fp = (IScriptObjectPropertyGetter)props.Single(p => p.Name == "PF_0");
            var pk = (IScriptObjectPropertyGetter)props.Single(p => p.Name == "SyncPerk_0");
            Assert.Equal(npc.FormKey, fp.Object.FormKey);
            Assert.Equal(perk.FormKey, pk.Object.FormKey);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    // ---- validation ----

    private static ModSpec WithLine(DialogueSpec d) => new()
    {
        PluginName = "Test.esp",
        Quests = { new QuestSpec { EditorId = "MF_Q", Name = "Q" } },
        Npcs = { new NpcSpec { EditorId = "MF_Npc", Name = "Npc" } },
        Perks = { new PerkSpec { EditorId = "MF_AdaptPerk", Name = "A" } },
        Dialogue = { d },
    };

    [Fact]
    public void Validate_EmptyStorage_Reported()
    {
        var d = Line(new PersistSpec { Storage = "", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } } });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("persist has empty storage"));
    }

    [Fact]
    public void Validate_UnresolvedRefKey_Reported()
    {
        // A non-token key is treated as an arbitrary ref; an unknown one fails ref resolution.
        var d = Line(new PersistSpec { Storage = "S", Key = "NoSuchRef", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } } });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("unresolved ref 'NoSuchRef'"));
    }

    [Fact]
    public void Validate_ResolvableRefKey_Accepted()
    {
        // A key that resolves to a spec record is a valid arbitrary-ref key.
        var d = Line(new PersistSpec { Storage = "S", Key = "MF_Npc", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } } });
        Assert.DoesNotContain(Validate(WithLine(d)), p => p.Contains("persist key") || p.Contains("unresolved ref 'MF_Npc'"));
    }

    [Fact]
    public void Validate_MultipleValueTypes_Reported()
    {
        var d = Line(new PersistSpec { Storage = "S", Set = { new PersistEntrySpec { Path = ".x", Int = 1, Float = 2f } } });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("must set exactly one of int/float/str/form"));
    }

    [Fact]
    public void Validate_DeltaOnForm_Reported()
    {
        var d = Line(new PersistSpec { Storage = "S", Set = { new PersistEntrySpec { Path = ".x", Form = "MF_Npc", Delta = true } } });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("delta only applies to int/float"));
    }

    [Fact]
    public void Validate_SyncPerks_EmptyPerk_Reported()
    {
        var d = Line(sync: new SyncPerksSpec { Storage = "S", Nodes = { new SyncPerkNodeSpec { Path = ".n", Perk = "" } } });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("syncPerks node '.n' has empty perk ref"));
    }

    [Fact]
    public void Validate_CleanSpec_NoProblems()
    {
        var d = Line(EndurancePersist(), new SyncPerksSpec { Storage = "ModForgeNpcSkills",
            Nodes = { new SyncPerkNodeSpec { Path = ".Endurance.nodes.Adaptation", Perk = "MF_AdaptPerk", MinRank = 2 } } });
        Assert.DoesNotContain(Validate(WithLine(d)), p => p.Contains("persist") || p.Contains("syncPerks"));
    }

    // ---- arbitrary-ref key (the key is a bound Form property, not speaker/player) ----

    [Fact]
    public void Persist_RefKey_DeclaresAndUsesKeyProperty()
    {
        var p = new PersistSpec { Storage = "S", Key = "MF_Npc", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } } };
        var src = Generator.GenerateDialogueFragmentSource(Line(p));
        Assert.Contains("Form Property PKey Auto", src);
        Assert.Contains("JFormDB.solveIntSetter(PKey, \".S.x\", 1, true)", src);
    }

    [Fact]
    public void SyncPerks_RefKey_DeclaresAndUsesKeyProperty()
    {
        var s = new SyncPerksSpec { Storage = "S", Key = "MF_Npc", Nodes = { new SyncPerkNodeSpec { Path = ".n", Perk = "MF_AdaptPerk" } } };
        var src = Generator.GenerateDialogueFragmentSource(Line(sync: s));
        Assert.Contains("Form Property SKey Auto", src);
        Assert.Contains("Actor __sp = SKey as Actor", src);
        Assert.Contains("If JFormDB.solveInt(SKey, \".S.n\", 0) >= 1", src);
    }

    [Fact]
    public void Build_RefKey_BindsKeyPropertyToForm()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-jc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "TIF_MF_Train.pex"), "");
            var spec = new ModSpec
            {
                PluginName = "Test.esp",
                Quests = { new QuestSpec { EditorId = "MF_Q", Name = "Q" } },
                Npcs = { new NpcSpec { EditorId = "MF_Npc", Name = "Npc" } },
                Dialogue = { Line(new PersistSpec { Storage = "S", Key = "MF_Npc", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } } }) },
            };
            var r = TestBuild.OkWithCompiledScripts(spec, dir);
            var npc = r.Mod.Npcs.Single(n => n.EditorID == "MF_Npc");
            var info = r.Mod.EnumerateMajorRecords<IDialogResponsesGetter>().Single(i => i.EditorID == "MF_Train");
            var props = info.VirtualMachineAdapter!.Scripts.Single(e => e.Name == "TIF_MF_Train").Properties;
            var pkey = (IScriptObjectPropertyGetter)props.Single(p => p.Name == "PKey");
            Assert.Equal(npc.FormKey, pkey.Object.FormKey);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    // ---- affinity gate (Sofia F6 blueprint): a GLOB threshold guards the persist / perk sync ----

    [Fact]
    public void Persist_Gate_WrapsWritesInGlobalThresholdAndDeclaresProperty()
    {
        var p = new PersistSpec
        {
            Storage = "ModForgeNpcSkills", Key = "speaker",
            Set = { new PersistEntrySpec { Path = ".Endurance.nodes.Adaptation", Int = 1 } },
            Gate = new GateSpec { Global = "MF_Affinity", AtLeast = 4 },
        };
        var src = Generator.GenerateDialogueFragmentSource(Line(p));
        Assert.Contains("GlobalVariable Property PGate Auto", src);
        Assert.Contains("If PGate.GetValue() >= 4", src);
        // the write sits inside the gate (deeper indent than an ungated write).
        Assert.Contains("    JFormDB.solveIntSetter(akSpeakerRef, \".ModForgeNpcSkills.Endurance.nodes.Adaptation\", 1, true)", src);
        Assert.Contains("EndIf", src);
    }

    [Fact]
    public void Persist_Gate_Band_EmitsBothBounds()
    {
        var p = new PersistSpec { Storage = "S", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } },
            Gate = new GateSpec { Global = "MF_Affinity", AtLeast = 2, AtMost = 6 } };
        var src = Generator.GenerateDialogueFragmentSource(Line(p));
        Assert.Contains("If PGate.GetValue() >= 2 && PGate.GetValue() <= 6", src);
    }

    [Fact]
    public void Persist_Gate_NoThreshold_FallsBackToNonZero()
    {
        var p = new PersistSpec { Storage = "S", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } },
            Gate = new GateSpec { Global = "MF_Flag" } };
        var src = Generator.GenerateDialogueFragmentSource(Line(p));
        Assert.Contains("If PGate.GetValue() != 0", src);
    }

    [Fact]
    public void SyncPerks_Gate_WrapsPerkSyncAndDeclaresProperty()
    {
        var s = new SyncPerksSpec { Storage = "S", Key = "speaker",
            Nodes = { new SyncPerkNodeSpec { Path = ".n", Perk = "MF_AdaptPerk" } },
            Gate = new GateSpec { Global = "MF_Affinity", AtLeast = 4 } };
        var src = Generator.GenerateDialogueFragmentSource(Line(sync: s));
        Assert.Contains("GlobalVariable Property SGate Auto", src);
        Assert.Contains("If SGate.GetValue() >= 4", src);
        Assert.Contains("    Actor __sp = akSpeakerRef as Actor", src);
    }

    [Fact]
    public void Build_Gate_BindsGlobalProperty()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-jc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "TIF_MF_Train.pex"), "");
            var spec = new ModSpec
            {
                PluginName = "Test.esp",
                Quests = { new QuestSpec { EditorId = "MF_Q", Name = "Q" } },
                Npcs = { new NpcSpec { EditorId = "MF_Npc", Name = "Npc" } },
                Globals = { new GlobalSpec { EditorId = "MF_Affinity", Value = 0 } },
                Dialogue =
                {
                    Line(new PersistSpec { Storage = "S", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } },
                        Gate = new GateSpec { Global = "MF_Affinity", AtLeast = 4 } }),
                },
            };
            var r = TestBuild.OkWithCompiledScripts(spec, dir);
            var glob = r.Mod.Globals.Single(g => g.EditorID == "MF_Affinity");
            var info = r.Mod.EnumerateMajorRecords<IDialogResponsesGetter>().Single(i => i.EditorID == "MF_Train");
            var props = info.VirtualMachineAdapter!.Scripts.Single(e => e.Name == "TIF_MF_Train").Properties;
            var gp = (IScriptObjectPropertyGetter)props.Single(p => p.Name == "PGate");
            Assert.Equal(glob.FormKey, gp.Object.FormKey);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Validate_Gate_UnresolvedGlobal_Reported()
    {
        var d = Line(new PersistSpec { Storage = "S", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } },
            Gate = new GateSpec { Global = "NoSuchGlob", AtLeast = 1 } });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("persist gate global") && p.Contains("unresolved ref 'NoSuchGlob'"));
    }

    [Fact]
    public void Validate_Gate_InvertedBand_Reported()
    {
        var spec = WithLine(Line(new PersistSpec { Storage = "S", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } },
            Gate = new GateSpec { Global = "MF_Aff", AtLeast = 6, AtMost = 2 } }));
        spec.Globals.Add(new GlobalSpec { EditorId = "MF_Aff" });
        Assert.Contains(Validate(spec), p => p.Contains("band never satisfiable"));
    }

    // ---- stage-fragment persist/syncPerks (the host is a quest STAGE, not a dialogue line) ----

    private static QuestSpec StagePersistQuest() => new()
    {
        EditorId = "MF_Q", Name = "Q",
        Stages =
        {
            new StageSpec
            {
                Index = 10,
                Persist = new PersistSpec { Storage = "S", Key = "player", Set = { new PersistEntrySpec { Path = ".won", Int = 1, Delta = true } } },
                SyncPerks = new SyncPerksSpec { Storage = "S", Key = "player", Nodes = { new SyncPerkNodeSpec { Path = ".n", Perk = "MF_AdaptPerk" } } },
            },
        },
    };

    [Fact]
    public void StagePersist_EmitsPrefixedPropertiesAndBody()
    {
        var q = StagePersistQuest();
        Assert.True(Generator.QuestNeedsFragmentScript(q));
        var src = Generator.GenerateQuestFragmentSource(q);
        // Properties are namespaced by the stage prefix so multiple stages never collide.
        Assert.Contains("Perk Property S0010_SyncPerk_0 Auto", src);
        var frag = src.Split("Function Fragment_Stage_0010_Item00000()")[1].Split("EndFunction")[0];
        Assert.Contains("int __pv0 = JFormDB.solveInt(Game.GetPlayer(), \".S.won\", 0)", frag);
        Assert.Contains("JFormDB.solveIntSetter(Game.GetPlayer(), \".S.won\", __pv0 + 1, true)", frag);
        Assert.Contains("Actor __sp = Game.GetPlayer() as Actor", frag);
        Assert.Contains("__sp.AddPerk(S0010_SyncPerk_0)", frag);
    }

    [Fact]
    public void StageBuild_WithPex_BindsPerkPropertyAndFragment()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mf-jc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "MF_Q_Stages.pex"), "");
            var spec = new ModSpec
            {
                PluginName = "Test.esp",
                Perks = { new PerkSpec { EditorId = "MF_AdaptPerk", Name = "Adaptation" } },
                Quests = { StagePersistQuest() },
            };
            var r = TestBuild.OkWithCompiledScripts(spec, dir);
            var perk = r.Mod.Perks.Single(p => p.EditorID == "MF_AdaptPerk");
            var quest = r.Mod.Quests.Single(qq => qq.EditorID == "MF_Q");
            var qa = (QuestAdapter)quest.VirtualMachineAdapter!;
            var entry = qa.Scripts.Single(s => s.Name == "MF_Q_Stages");
            var prop = (IScriptObjectPropertyGetter)entry.Properties.Single(p => p.Name == "S0010_SyncPerk_0");
            Assert.Equal(perk.FormKey, prop.Object.FormKey);
            Assert.Contains(qa.Fragments, f => f.Stage == 10 && f.FragmentName == "Fragment_Stage_0010_Item00000");
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Validate_StageSpeakerKey_Rejected()
    {
        // A quest stage has no akSpeakerRef, so "speaker" (the persist default) must be rejected there.
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q",
                Stages = { new StageSpec { Index = 10,
                    Persist = new PersistSpec { Storage = "S", Key = "speaker", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } } } } } } },
        };
        Assert.Contains(Validate(spec), p => p.Contains("'speaker' is only valid on a dialogue line"));
    }

    [Fact]
    public void Validate_StagePlayerKey_Accepted()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { StagePersistQuest() },
            Perks = { new PerkSpec { EditorId = "MF_AdaptPerk", Name = "A" } },
        };
        Assert.DoesNotContain(Validate(spec), p => p.Contains("persist") || p.Contains("syncPerks"));
    }
}
