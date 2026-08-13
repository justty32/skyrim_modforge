using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// A CTDA's `param` / `reference` is an ARBITRARY ref — it may name a PLACED ref: an in-spec
// placements[] editorId ("GetDistance <that chair>") or a references[] label. Those editorIds only
// enter the ref table in BuildPlacements / BuildReferences, which run LATER than several steps that
// author conditions (WirePerks, BuildStoryManager, BuildStandaloneQuestAliases, WireScenes). Those
// four therefore DEFER their conditions (DeferCondition → WireDeferredConditions); dialogue, banter
// and package conditions were already ordered after the placement passes.
//
// These tests are the nails: each of the four call sites must resolve a placement editorId AND a
// references[] label, in both the `param` and the `reference` (run-on) slot — and must still WARN
// when the ref is genuinely bogus. Master-free (new interior cell, no Skyrim.esm).
public class ConditionPlacedRefTests
{
    // A cell + a static + a placement of it ("Chair"), and a references[] label naming that placement.
    private static ModSpec BaseSpec()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Statics.Add(new StaticSpec { EditorId = "ChairBase", Model = @"Clutter\Chair.nif" });
        spec.Placements.Add(new PlacementSpec { EditorId = "Chair", Base = "ChairBase", Cell = "Room" });
        spec.References.Add(new ReferenceSpec { Label = "the chair", Ref = "Chair" });
        return spec;
    }

    private static FormKey ChairKey(BuildResult r) =>
        r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(o => o.EditorID == "Chair").FormKey;

    // The form the CTDA's function points at (its `param`). Every positive case here uses GetDistance,
    // whose param IS the placed ref being tested.
    private static FormKey ParamOf(IConditionGetter c) =>
        ((IGetDistanceConditionDataGetter)((IConditionFloatGetter)c).Data).Target.Link.FormKey;

    // The CTDA's run-on ref (the `reference` field, used when RunOn=Reference).
    private static FormKey ReferenceOf(IConditionGetter c) =>
        ((IConditionFloatGetter)c).Data.Reference.FormKey;

    // A GetDistance gate on <ref>: the canonical "is the player near THAT object" condition.
    private static ConditionSpec Near(string paramRef) =>
        new() { Function = "GetDistance", Comparison = "<=", Value = 512, Param = paramRef };

    // --- 1. perk conditions (WirePerks — runs at pass-2 step ~103, long before placements) --------

    [Fact]
    public void PerkCondition_ParamIsPlacementEditorId_ResolvesToThePlacedRef()
    {
        var spec = BaseSpec();
        spec.Perks.Add(new PerkSpec { EditorId = "P", Name = "P", Conditions = { Near("Chair") } });
        var r = TestBuild.Ok(spec);
        var perk = r.Mod.Perks.Single();
        Assert.Equal(ChairKey(r), ParamOf(perk.Conditions.Single()));
    }

    [Fact]
    public void PerkCondition_ParamIsReferenceLabel_ResolvesToThePlacedRef()
    {
        var spec = BaseSpec();
        spec.Perks.Add(new PerkSpec { EditorId = "P", Name = "P", Conditions = { Near("the chair") } });
        var r = TestBuild.Ok(spec);
        Assert.Equal(ChairKey(r), ParamOf(r.Mod.Perks.Single().Conditions.Single()));
    }

    [Fact]
    public void PerkEffectCondition_ParamIsReferenceLabel_ResolvesToThePlacedRef()
    {
        var spec = BaseSpec();
        spec.Perks.Add(new PerkSpec
        {
            EditorId = "P", Name = "P",
            Effects =
            {
                new PerkEffectSpec
                {
                    Kind = "entryPoint", EntryPoint = "ModAttackDamage", Function = "multiply", Value = 1.5f,
                    Conditions = { Near("the chair") },
                },
            },
        });
        var r = TestBuild.Ok(spec);
        var effect = r.Mod.Perks.Single().Effects.Single();
        var tab = effect.Conditions.Single();   // the PerkCondition tab (attached by the deferred finalizer)
        Assert.Equal(ChairKey(r), ParamOf(tab.Conditions.Single()));
    }

    [Fact]
    public void PerkEffectCondition_AllConditionsBogus_LeavesNoEmptyPerkConditionTab()
    {
        // Regression on the finalizer: the tab is only attached when a condition actually built —
        // vanilla never emits an empty PRKC tab, and the old eager code checked `Count > 0`.
        var spec = BaseSpec();
        spec.Perks.Add(new PerkSpec
        {
            EditorId = "P", Name = "P",
            Effects =
            {
                new PerkEffectSpec
                {
                    Kind = "entryPoint", EntryPoint = "ModAttackDamage", Function = "multiply", Value = 1.5f,
                    Conditions = { Near("NoSuchRef") },
                },
            },
        });
        var r = TestBuild.Raw(spec);
        Assert.Empty(r.Mod.Perks.Single().Effects.Single().Conditions);
        Assert.Contains(r.Warnings, w => w.Contains("perk 'P' effect condition") && w.Contains("unresolved"));
    }

    [Fact]
    public void PerkCondition_BogusParam_StillWarns()
    {
        var spec = BaseSpec();
        spec.Perks.Add(new PerkSpec { EditorId = "P", Name = "P", Conditions = { Near("NoSuchRef") } });
        var r = TestBuild.Raw(spec);
        Assert.Empty(r.Mod.Perks.Single().Conditions);
        Assert.Contains(r.Warnings, w => w.Contains("perk 'P' condition") && w.Contains("unresolved"));
    }

    // --- 2. Story Manager storyEvent conditions (BuildStoryManager — pass-2 step ~87) -------------

    [Fact]
    public void StoryEventCondition_ParamIsPlacementEditorId_ResolvesToThePlacedRef()
    {
        var spec = BaseSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q",
            StoryEvent = new QuestStoryEventSpec { Event = "ChangeLocation", Conditions = { Near("Chair") } },
        });
        var r = TestBuild.Ok(spec);
        var quest = r.Mod.Quests.Single(q => q.EditorID == "Q");
        Assert.Equal(ChairKey(r), ParamOf(quest.EventConditions.Single()));
    }

    [Fact]
    public void StoryEventCondition_ParamIsReferenceLabel_ResolvesToThePlacedRef()
    {
        var spec = BaseSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q",
            StoryEvent = new QuestStoryEventSpec { Event = "ChangeLocation", Conditions = { Near("the chair") } },
        });
        var r = TestBuild.Ok(spec);
        var quest = r.Mod.Quests.Single(q => q.EditorID == "Q");
        Assert.Equal(ChairKey(r), ParamOf(quest.EventConditions.Single()));
    }

    [Fact]
    public void StoryEventCondition_RunOnReferenceIsLabel_ResolvesTheRunOnRef()
    {
        // The `reference` slot (RunOn=Reference) is the OTHER ref field on a CTDA and was equally broken.
        var spec = BaseSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q",
            StoryEvent = new QuestStoryEventSpec
            {
                Event = "ChangeLocation",
                Conditions =
                {
                    new ConditionSpec
                    {
                        Function = "GetMapMarkerVisible", Comparison = "==", Value = 1,
                        RunOn = "Reference", Reference = "the chair",
                    },
                },
            },
        });
        var r = TestBuild.Ok(spec);
        var quest = r.Mod.Quests.Single(q => q.EditorID == "Q");
        var cond = quest.EventConditions.Single();
        Assert.Equal(Condition.RunOnType.Reference, ((IConditionFloatGetter)cond).Data.RunOnType);
        Assert.Equal(ChairKey(r), ReferenceOf(cond));
    }

    [Fact]
    public void StoryEventCondition_LocationFilterStillEmitsAfterTheEventConditions()
    {
        // Order nail: the deferred queue drains in enqueue order, so EventConditions stays
        // [storyEvent conditions…, locationFilter…] exactly as the eager code emitted it.
        var spec = BaseSpec();
        spec.Keywords.Add(new KeywordSpec { EditorId = "LocTypeTest" });
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q",
            StoryEvent = new QuestStoryEventSpec
            {
                Event = "ChangeLocation",
                Conditions = { Near("the chair") },
                LocationFilter = { "LocTypeTest" },
            },
        });
        var r = TestBuild.Ok(spec);
        var quest = r.Mod.Quests.Single(q => q.EditorID == "Q");
        Assert.Equal(2, quest.EventConditions.Count);
        Assert.IsType<GetDistanceConditionData>(((IConditionFloatGetter)quest.EventConditions[0]).Data);
        Assert.IsType<GetKeywordDataForCurrentLocationConditionData>(
            ((IConditionFloatGetter)quest.EventConditions[1]).Data);
    }

    // --- 3. quest-alias match conditions (BuildQuestAliases, via BOTH the SM path and the ---------
    //        standalone path — WireAliasMatchConditions is shared)

    [Fact]
    public void StandaloneAliasMatchCondition_ParamIsReferenceLabel_ResolvesToThePlacedRef()
    {
        var spec = BaseSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q",
            Aliases =
            {
                new QuestAliasSpec
                {
                    Name = "Target", Fill = "findMatching:closest", Optional = true,
                    Conditions = { Near("the chair") },
                },
            },
        });
        var r = TestBuild.Ok(spec);
        var alias = r.Mod.Quests.Single(q => q.EditorID == "Q").Aliases.Single();
        Assert.Equal(ChairKey(r), ParamOf(alias.Conditions!.Single()));
    }

    [Fact]
    public void StoryEventAliasMatchCondition_ParamIsPlacementEditorId_ResolvesToThePlacedRef()
    {
        var spec = BaseSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q",
            StoryEvent = new QuestStoryEventSpec { Event = "ChangeLocation" },
            Aliases =
            {
                new QuestAliasSpec
                {
                    Name = "Target", Fill = "findMatching:any", Optional = true,
                    Conditions = { Near("Chair") },
                },
            },
        });
        var r = TestBuild.Ok(spec);
        var alias = r.Mod.Quests.Single(q => q.EditorID == "Q").Aliases.Single();
        Assert.Equal(ChairKey(r), ParamOf(alias.Conditions!.Single()));
    }

    [Fact]
    public void AliasMatchCondition_BogusParam_StillWarns()
    {
        var spec = BaseSpec();
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q",
            Aliases =
            {
                new QuestAliasSpec
                {
                    Name = "Target", Fill = "findMatching:closest", Optional = true,
                    Conditions = { Near("NoSuchRef") },
                },
            },
        });
        var r = TestBuild.Raw(spec);
        Assert.Contains(r.Warnings, w => w.Contains("alias 'Target' findMatching condition") && w.Contains("unresolved"));
    }

    // --- 4. scene + phase conditions (WireScenes — pass-2 step ~93) -------------------------------

    private static ModSpec SceneSpec2(ConditionSpec? sceneCond, ConditionSpec? phaseCond)
    {
        var spec = BaseSpec();
        spec.Quests.Add(new QuestSpec { EditorId = "SQ", Name = "SQ" });
        spec.Npcs.Add(new NpcSpec { EditorId = "A", Name = "A", Race = "Skyrim.esm:0x013746" });
        spec.Npcs.Add(new NpcSpec { EditorId = "B", Name = "B", Race = "Skyrim.esm:0x013746" });
        var phase = new ScenePhaseSpec { Speaker = 0, Lines = { "hi" } };
        if (phaseCond is not null) phase.StartConditions.Add(phaseCond);
        var scene = new SceneSpec
        {
            EditorId = "Sc", QuestEditorId = "SQ",
            Actors =
            {
                new SceneActorSpec { AliasId = 0, Npc = "A", Name = "A" },
                new SceneActorSpec { AliasId = 1, Npc = "B", Name = "B" },
            },
            Phases = { phase },
        };
        if (sceneCond is not null) scene.Conditions.Add(sceneCond);
        spec.Scenes.Add(scene);
        return spec;
    }

    [Fact]
    public void SceneCondition_ParamIsReferenceLabel_ResolvesToThePlacedRef()
    {
        var r = TestBuild.Ok(SceneSpec2(Near("the chair"), null));
        var scene = r.Mod.Scenes.Single();
        Assert.Equal(ChairKey(r), ParamOf(scene.Conditions!.Single()));
    }

    [Fact]
    public void ScenePhaseStartCondition_ParamIsPlacementEditorId_ResolvesToThePlacedRef()
    {
        var r = TestBuild.Ok(SceneSpec2(null, Near("Chair")));
        var phase = r.Mod.Scenes.Single().Phases!.Single();
        Assert.Equal(ChairKey(r), ParamOf(phase.StartConditions!.Single()));
    }

    [Fact]
    public void SceneCondition_BogusParam_StillWarns()
    {
        var r = TestBuild.Raw(SceneSpec2(Near("NoSuchRef"), null));
        Assert.Contains(r.Warnings, w => w.Contains("scene 'Sc' condition") && w.Contains("unresolved"));
    }

    [Fact]
    public void SceneCondition_OwningSceneStillDefaultsForIsSceneActionComplete()
    {
        // The deferred queue must carry the owningScene FormKey through, or a scene condition's
        // IsSceneActionComplete loses its implicit "this scene" default.
        var spec = SceneSpec2(
            new ConditionSpec
            {
                Function = "IsSceneActionComplete", Comparison = "==", Value = 1, SceneActionIndex = 0,
            },
            null);
        var r = TestBuild.Ok(spec);
        var scene = r.Mod.Scenes.Single();
        var data = (IIsSceneActionCompleteConditionDataGetter)((IConditionFloatGetter)scene.Conditions!.Single()).Data;
        Assert.Equal(scene.FormKey, data.Scene.Link.FormKey);
    }
}
