using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// F組 #3 — dynamic near-player navmesh spawn. quest.spawn → MFDynamicSpawn quest script with
// SpawnForm/Count/Min/MaxDistance/SnapToNavmesh props. Runtime behaviour (PlaceAtMe + EnableAI snap)
// verified on the main machine; offline locks the record/VMAD wiring.
public class DynamicSpawnTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();
    private static ISkyrimMod Build(ModSpec s) => Generator.Build(s, ModKey.FromNameAndExtension("Test.esp")).Mod;

    private static ModSpec SpawnSpecMod(SpawnSpec spawn) => new()
    {
        // A startUpStage is required: spawn fires from that stage's fragment on quest start (not OnInit).
        Quests = { new QuestSpec { EditorId = "MFSpawnQ", Name = "Spawn", StartGameEnabled = true, Spawn = spawn,
            Stages = { new StageSpec { Index = 10, StartUpStage = true } } } },
    };

    private static SpawnSpec Bandits() => new()
    {
        Form = "Skyrim.esm:0x0001BCC0", Count = 3, MinDistance = 1200f, MaxDistance = 3500f, SnapToNavmesh = true,
    };

    private static IScriptEntryGetter SpawnScript(ISkyrimMod mod)
    {
        var q = mod.Quests.Single(x => x.EditorID == "MFSpawnQ");
        var qa = (QuestAdapter)q.VirtualMachineAdapter!;
        return qa.Scripts.Single(s => s.Name == "MFDynamicSpawn");
    }

    [Fact]
    public void Spawn_AttachesScriptWithFormAndCount()
    {
        var entry = SpawnScript(Build(SpawnSpecMod(Bandits())));
        var fp = (IScriptObjectPropertyGetter)entry.Properties.Single(p => p.Name == "SpawnForm");
        Assert.False(fp.Object.FormKey.IsNull);
        Assert.Equal(3, ((IScriptIntPropertyGetter)entry.Properties.Single(p => p.Name == "Count")).Data);
    }

    [Fact]
    public void Spawn_WiresDistancesAndSnapFlag()
    {
        var entry = SpawnScript(Build(SpawnSpecMod(Bandits())));
        Assert.Equal(1200f, ((IScriptFloatPropertyGetter)entry.Properties.Single(p => p.Name == "MinDistance")).Data);
        Assert.Equal(3500f, ((IScriptFloatPropertyGetter)entry.Properties.Single(p => p.Name == "MaxDistance")).Data);
        Assert.True(((IScriptBoolPropertyGetter)entry.Properties.Single(p => p.Name == "SnapToNavmesh")).Data);
    }

    [Fact]
    public void NoSpawn_NoScript()
    {
        var q = Build(new ModSpec { Quests = { new QuestSpec { EditorId = "Q", Name = "Q", StartGameEnabled = true } } })
            .Quests.Single(x => x.EditorID == "Q");
        Assert.Null(q.VirtualMachineAdapter);
    }

    [Fact]
    public void Spawn_CoexistsWithCooldownAndLocationFilter()
    {
        // A full location-aware encounter: ChangeLocation + locationFilter + cooldown + spawn all on one quest.
        var spec = new ModSpec
        {
            Keywords = { new KeywordSpec { EditorId = "LocTypeBanditCamp" } },
            Quests =
            {
                new QuestSpec
                {
                    EditorId = "MFSpawnQ", Name = "Ambush",
                    Stages = { new StageSpec { Index = 10, StartUpStage = true } },
                    StoryEvent = new QuestStoryEventSpec
                    {
                        Event = "ChangeLocation",
                        LocationFilter = { "LocTypeBanditCamp" },
                        CooldownHours = 6f,
                    },
                    Aliases = { new QuestAliasSpec { Name = "Loc", Fill = "fromEvent:newLocation" } },
                    Spawn = Bandits(),
                },
            },
        };
        var mod = Build(spec);
        var qa = (QuestAdapter)mod.Quests.Single(x => x.EditorID == "MFSpawnQ").VirtualMachineAdapter!;
        // Both the cooldown and the spawn scripts share the one adapter.
        Assert.Contains(qa.Scripts, s => s.Name == "MFDynamicSpawn");
        Assert.Contains(qa.Scripts, s => s.Name == "MFEncounterCooldown");
    }

    [Fact]
    public void Validate_Clean()
    {
        Assert.DoesNotContain(Validate(SpawnSpecMod(Bandits())), p => p.Contains("spawn"));
    }

    [Fact]
    public void Spawn_StartupStageFragment_CallsSpawnNow()
    {
        // The startUpStage fragment — not OnInit — drives the spawn, so it re-fires on every quest start.
        var q = new QuestSpec
        {
            EditorId = "MFSpawnQ", Name = "Spawn", StartGameEnabled = true, Spawn = Bandits(),
            Stages = { new StageSpec { Index = 10, StartUpStage = true } },
        };
        Assert.Equal(10, Generator.StartupStageTrigger(q));
        var src = Generator.GenerateQuestFragmentSource(q);
        Assert.Contains("Function Fragment_Stage_0010_Item00000()", src);
        Assert.Contains("self as MFDynamicSpawn", src);
        Assert.Contains("__spawn.SpawnNow()", src);
    }

    [Fact]
    public void Spawn_StartupStage_BindsFragmentInVmad()
    {
        // The QuestScriptFragment binding MUST be emitted for the startUpStage — without it the engine
        // never calls Fragment_Stage_XXXX even though the function is in the .pex (the "startquest
        // spawns nothing" bug). Needs CompiledScriptsDir + a present .pex to run WireQuestStages pass 2.
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mf-spawn-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(dir);
        try
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "MFSpawnQ_Stages.pex"), "");
            var r = TestBuild.OkWithCompiledScripts(SpawnSpecMod(Bandits()), dir);
            var qa = (QuestAdapter)r.Mod.Quests.Single(q => q.EditorID == "MFSpawnQ").VirtualMachineAdapter!;
            Assert.Contains(qa.Fragments, f => f.Stage == 10 && f.FragmentName == "Fragment_Stage_0010_Item00000");
        }
        finally { if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void Spawn_WithoutStartupStage_IsReported()
    {
        var s = new ModSpec { Quests = { new QuestSpec { EditorId = "Q", Name = "Q", Spawn = Bandits() } } };
        Assert.Contains(Validate(s), p => p.Contains("startUpStage"));
        Assert.Null(Generator.StartupStageTrigger(s.Quests[0]));
    }

    [Fact]
    public void Validate_EmptyForm_Reported()
    {
        Assert.Contains(Validate(SpawnSpecMod(new SpawnSpec { Form = "", Count = 1 })),
            p => p.Contains("spawn has empty 'form'"));
    }

    [Fact]
    public void Validate_ZeroCount_Reported()
    {
        Assert.Contains(Validate(SpawnSpecMod(new SpawnSpec { Form = "Skyrim.esm:0x1", Count = 0 })),
            p => p.Contains("spawn.count must be >= 1"));
    }

    [Fact]
    public void Validate_MinGreaterThanMax_Reported()
    {
        Assert.Contains(Validate(SpawnSpecMod(new SpawnSpec { Form = "Skyrim.esm:0x1", Count = 1, MinDistance = 5000f, MaxDistance = 1000f })),
            p => p.Contains("minDistance") && p.Contains("maxDistance"));
    }
}
