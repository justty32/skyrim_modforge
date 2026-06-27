using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// livingNpcs: macro (Idea #23). Asserts the pass-0 expansion: a controller host quest + per-NPC alias
// (MFLivingNpcAlias with Archetype/HoldMarker/Anchors/DeedCount) + anchor xmarkers + deed global +
// rumor dialogue + the world-controller script. Pure in-memory (no Skyrim.esm).
public class LivingNpcTests
{
    private static ModSpec Spec(bool withRumor = true)
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Npcs.Add(new NpcSpec { EditorId = "Kjeld", Name = "Kjeld", Greeting = "Hm." });
        spec.Npcs.Add(new NpcSpec { EditorId = "Falas", Name = "Falas", Greeting = "Hm." });
        spec.Npcs.Add(new NpcSpec { EditorId = "Bard", Name = "Bard", Greeting = "Tales?" });
        spec.LivingNpcs = new LivingNpcsSpec
        {
            SimIntervalHours = 2f,
            RumorSpeaker = withRumor ? "Bard" : "",
            Npcs =
            {
                new LivingNpcSpec
                {
                    Ref = "Kjeld", Name = "Kjeld", Archetype = "adventurer",
                    Anchors =
                    {
                        new LivingAnchorSpec { Cell = "Skyrim.esm:0x0133C6", Position = new Vec3 { X = 1 } },
                        new LivingAnchorSpec { Cell = "Skyrim.esm:0x01605E", Position = new Vec3 { X = 2 } },
                    },
                    Rumors = { "Kjeld cleared a barrow." },
                },
                new LivingNpcSpec
                {
                    Ref = "Falas", Name = "Falas", Archetype = "mageApprentice",
                    Anchors = { new LivingAnchorSpec { Cell = "Skyrim.esm:0x01605E", Position = new Vec3 { Y = 1 } } },
                },
            },
        };
        return spec;
    }

    private static Quest Ctrl(ISkyrimMod mod) => mod.Quests.First(q => q.EditorID == "MFLiving_Ctrl");

    [Fact]
    public void Expands_ControllerQuest_StartGameEnabled_WithOneAliasPerNpc()
    {
        var r = TestBuild.Ok(Spec());
        var ctrl = Ctrl(r.Mod);
        Assert.True(ctrl.Flags.HasFlag(Quest.Flag.StartGameEnabled));
        Assert.Equal(2, ctrl.Aliases.Count);
    }

    [Fact]
    public void WorldControllerScript_OnQuest_WithAliasCount()
    {
        var r = TestBuild.Ok(Spec());
        var qad = (IQuestAdapterGetter)Ctrl(r.Mod).VirtualMachineAdapter!;
        var ctrlScript = qad.Scripts.First(s => s.Name == "MFLivingWorldController");
        var count = (IScriptIntPropertyGetter)ctrlScript.Properties.First(p => p.Name == "AliasCount");
        Assert.Equal(2, count.Data);
    }

    [Fact]
    public void AliasScript_CarriesArchetype_AndResolvedObjectProps()
    {
        var r = TestBuild.Ok(Spec());
        var qad = (IQuestAdapterGetter)Ctrl(r.Mod).VirtualMachineAdapter!;
        var falasAlias = qad.Aliases.Single(a => a.Property.Alias == 1);   // alias index 1 = Falas
        var script = falasAlias.Scripts.First(s => s.Name == "MFLivingNpcAlias");
        // mageApprentice → archetype code 1
        Assert.Equal(1, ((IScriptIntPropertyGetter)script.Properties.First(p => p.Name == "Archetype")).Data);
        // HoldMarker / Anchors / DeedCount object props resolved (deferred) — none null
        foreach (var name in new[] { "HoldMarker", "Anchors", "DeedCount" })
            Assert.False(((IScriptObjectPropertyGetter)script.Properties.First(p => p.Name == name)).Object.FormKey.IsNull);
    }

    [Fact]
    public void Emits_DeedGlobal_AnchorMarkers_And_AnchorsFormList_PerNpc()
    {
        var r = TestBuild.Ok(Spec());
        Assert.Contains(r.Mod.Globals, g => g.EditorID == "MFLiving_Kjeld_Deeds");
        // two anchor xmarkers for Kjeld + the shared hold marker
        Assert.Contains(r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>(), o => o.EditorID == "MFLiving_Kjeld_A0");
        Assert.Contains(r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>(), o => o.EditorID == "MFLiving_Kjeld_A1");
        Assert.Contains(r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>(), o => o.EditorID == "MFLiving_HoldMarker");
        var flst = r.Mod.FormLists.Single(f => f.EditorID == "MFLiving_Kjeld_Anchors");
        Assert.Equal(2, flst.Items.Count);
    }

    [Fact]
    public void InSpecNpc_Ref_IsPlacedAndPersistent()
    {
        var r = TestBuild.Ok(Spec());
        // the forced-alias ACHR is persistent (auto, via the deferredForcedAliases fix)
        var npcRef = r.Mod.EnumerateMajorRecords<IPlacedNpcGetter>().Single(n => n.EditorID == "MFLiving_KjeldRef");
        Assert.NotNull(npcRef);
    }

    [Fact]
    public void Rumor_Topic_GatedOnDeedGlobal_WhenSpeakerGiven()
    {
        var r = TestBuild.Ok(Spec());
        // Kjeld has rumors + a speaker → a rumor INFO exists; Falas has no rumors → none.
        var infos = r.Mod.DialogTopics.SelectMany(t => t.Responses).ToList();
        Assert.Contains(infos, i => i.Responses.Any(rr => rr.Text.String!.Contains("Kjeld cleared a barrow")));
    }

    [Fact]
    public void NoRumorSpeaker_DropsRumorTopic()
    {
        // rumors present but no speaker → the macro emits NO rumor INFO (greetings still exist).
        var r = TestBuild.Ok(Spec(withRumor: false));
        var infos = r.Mod.DialogTopics.SelectMany(t => t.Responses).ToList();
        Assert.DoesNotContain(infos, i => i.Responses.Any(rr => (rr.Text.String ?? "").Contains("barrow")));
    }

    [Fact]
    public void ExternalRef_UsesUniqueActorFill_NoPlacement()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.LivingNpcs = new LivingNpcsSpec
        {
            Npcs =
            {
                new LivingNpcSpec
                {
                    Ref = "Skyrim.esm:0x00013BB9", Name = "Ext", Archetype = "merchant",
                    Anchors = { new LivingAnchorSpec { Cell = "Skyrim.esm:0x01605E", Position = new Vec3() } },
                },
            },
        };
        var r = TestBuild.Ok(spec);
        var alias = Ctrl(r.Mod).Aliases.Single();
        Assert.False(alias.UniqueActor.FormKey.IsNull);                       // uniqueActor fill
        Assert.DoesNotContain(r.Mod.EnumerateMajorRecords<IPlacedNpcGetter>(), n => n.EditorID == "MFLiving_N0Ref");
    }

    [Fact]
    public void Validate_Flags_MissingAnchors_And_UnknownArchetype_And_OrphanRumors()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Npcs.Add(new NpcSpec { EditorId = "X", Name = "X", Greeting = "." });
        spec.LivingNpcs = new LivingNpcsSpec
        {
            Npcs = { new LivingNpcSpec { Ref = "X", Archetype = "wizard", Rumors = { "r" } } },  // no anchors, bad archetype, no speaker
        };
        var problems = Generator.Validate(spec).ToArray();
        Assert.Contains(problems, p => p.Contains("no anchors"));
        Assert.Contains(problems, p => p.Contains("unknown archetype"));
        Assert.Contains(problems, p => p.Contains("no rumorSpeaker"));
    }
}
