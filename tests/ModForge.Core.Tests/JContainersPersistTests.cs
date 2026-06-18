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
    public void Validate_BadKey_Reported()
    {
        var d = Line(new PersistSpec { Storage = "S", Key = "stone", Set = { new PersistEntrySpec { Path = ".x", Int = 1 } } });
        Assert.Contains(Validate(WithLine(d)), p => p.Contains("must be 'speaker' or 'player'"));
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
}
