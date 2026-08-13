using ModForge;

namespace ModForge.Tests;

// livingNpcs macro (Idea #23) at the EXPANSION level — Generator.ExpandLivingNpcs is a pure
// spec -> spec rewrite, so unlike LivingNpcTests (which builds, places anchors into vanilla cells,
// and is therefore Category=RequiresSkyrim in full) every case here runs headless.
//
// WHY THIS FILE EXISTS SEPARATELY: on an offline machine `Category!=RequiresSkyrim` skipped every
// test that reached this macro, leaving a 216-line expander at 4% line coverage. What the expander
// decides — which fill each ref gets, which records carry which editorId, which topic is gated on
// which global — is all observable on the spec, before a single record is built.
public class LivingNpcExpansionTests
{
    private static ModSpec WithNpcs(params LivingNpcSpec[] living)
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.LivingNpcs = new LivingNpcsSpec();
        foreach (var l in living) spec.LivingNpcs.Npcs.Add(l);
        return spec;
    }

    private static LivingNpcSpec Kjeld() => new()
    {
        Ref = "Kjeld", Name = "Kjeld", Archetype = "adventurer",
        Anchors = { new LivingAnchorSpec { Cell = "Skyrim.esm:0x0133C6", Position = new Vec3 { X = 1 } } },
    };

    // --- guards -----------------------------------------------------------------------------

    [Fact]
    public void NoLivingSection_ExpandsNothing_ButStillMarksExpanded()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };

        Generator.ExpandLivingNpcs(spec);

        Assert.True(spec.LivingNpcsExpanded);
        Assert.Empty(spec.Quests);
        Assert.Empty(spec.Placements);
        Assert.Empty(spec.Packages);
    }

    [Fact]
    public void EmptyNpcList_EmitsNoSharedRecords()
    {
        var spec = new ModSpec { PluginName = "Test.esp", LivingNpcs = new LivingNpcsSpec() };

        Generator.ExpandLivingNpcs(spec);

        // The shared hold marker / sandbox package are only worth emitting if somebody uses them.
        Assert.Empty(spec.Placements);
        Assert.Empty(spec.Packages);
        Assert.Empty(spec.Quests);
    }

    [Fact]
    public void IsIdempotent_SecondCallAddsNothing()
    {
        var spec = WithNpcs(Kjeld());

        Generator.ExpandLivingNpcs(spec);
        var counts = (spec.Quests.Count, spec.Placements.Count, spec.Globals.Count,
                      spec.FormLists.Count, spec.Packages.Count, spec.Scripts.Count);
        Generator.ExpandLivingNpcs(spec);

        Assert.Equal(counts, (spec.Quests.Count, spec.Placements.Count, spec.Globals.Count,
                              spec.FormLists.Count, spec.Packages.Count, spec.Scripts.Count));
    }

    [Fact]
    public void BlankRef_IsSkipped_LeavingNoAliasForIt()
    {
        var spec = WithNpcs(new LivingNpcSpec { Ref = "  " }, Kjeld());

        Generator.ExpandLivingNpcs(spec);

        // validation reports the blank ref; the macro must not emit a half-built alias for it.
        var ctrl = spec.Quests.Single(q => q.EditorId == "MFLiving_Ctrl");
        Assert.Single(ctrl.Aliases);
        Assert.Equal("Living1_Kjeld", ctrl.Aliases[0].Name);
    }

    // --- shared records ---------------------------------------------------------------------

    [Fact]
    public void EmitsSharedHoldMarker_BuriedUnderTamriel()
    {
        var spec = WithNpcs(Kjeld());

        Generator.ExpandLivingNpcs(spec);

        var hold = spec.Placements.Single(p => p.EditorId == "MFLiving_HoldMarker");
        Assert.Equal("xmarker", hold.Kind);
        Assert.Equal("Skyrim.esm:0x00003C", hold.Worldspace);
        Assert.True(hold.Position.Z < 0, "the hold marker is deliberately buried off-stage");
    }

    [Fact]
    public void EmitsSharedSandboxPackage_OnTheVanillaSandboxTemplate()
    {
        var spec = WithNpcs(Kjeld());

        Generator.ExpandLivingNpcs(spec);

        var pkg = spec.Packages.Single(p => p.EditorId == "MFLiving_SandboxHere");
        Assert.Equal(Generator.SandboxTemplateRef, pkg.Template);
        Assert.NotNull(pkg.Sandbox);
    }

    [Fact]
    public void ControllerQuest_IsStartGameEnabled_TypeNone()
    {
        var spec = WithNpcs(Kjeld());

        Generator.ExpandLivingNpcs(spec);

        var ctrl = spec.Quests.Single(q => q.EditorId == "MFLiving_Ctrl");
        Assert.True(ctrl.StartGameEnabled);
        Assert.Equal("None", ctrl.Type);
    }

    [Fact]
    public void ControllerScript_CarriesTheSectionCadence_AndAliasCount()
    {
        var spec = WithNpcs(Kjeld(), new LivingNpcSpec { Ref = "Falas" });
        spec.LivingNpcs!.SimIntervalHours = 7f;
        spec.LivingNpcs.PollInterval = 3f;

        Generator.ExpandLivingNpcs(spec);

        var script = spec.Scripts.Single(s => s.ScriptName == Generator.LivingControllerScript);
        Assert.Equal("MFLiving_Ctrl", script.TargetEditorId);
        Assert.Equal(7f, script.Properties.Single(p => p.Name == "SimIntervalHours").Float);
        Assert.Equal(3f, script.Properties.Single(p => p.Name == "PollInterval").Float);
        // AliasCount counts the SECTION's npcs, which is what the controller loops over.
        Assert.Equal(2, script.Properties.Single(p => p.Name == "AliasCount").Int);
    }

    // --- per-NPC fill -----------------------------------------------------------------------

    [Fact]
    public void InSpecNpc_GetsForcedFill_AndAnOffStageRefThatOptsOutOfTheNavmeshCheck()
    {
        var spec = WithNpcs(Kjeld());

        Generator.ExpandLivingNpcs(spec);

        var alias = spec.Quests.Single().Aliases.Single();
        Assert.Equal("forced:MFLiving_KjeldRef", alias.Fill);
        Assert.False(alias.AllowReserved);

        var placed = spec.Placements.Single(p => p.EditorId == "MFLiving_KjeldRef");
        Assert.Equal("Kjeld", placed.Base);
        Assert.Equal("npc", placed.Kind);
        // Parked below the terrain on purpose — the alias script MoveTo's it in, so "this NPC will
        // never reach anything" is the intended state, not a spec error.
        Assert.False(placed.NavmeshCheck);
    }

    [Fact]
    public void ExternalRef_UsesUniqueActorFill_AllowsReserved_AndPlacesNoRef()
    {
        var spec = WithNpcs(new LivingNpcSpec { Ref = "SofiaFollower.esp:0x001234" });

        Generator.ExpandLivingNpcs(spec);

        var alias = spec.Quests.Single().Aliases.Single();
        Assert.Equal("uniqueActor:SofiaFollower.esp:0x001234", alias.Fill);
        // A standalone follower's ref is usually already reserved by its own quest.
        Assert.True(alias.AllowReserved);
        Assert.DoesNotContain(spec.Placements, p => p.Kind == "npc");
        // An external NPC has no in-spec editorId to prefix with, so records key on the index.
        Assert.Equal("Living0_N0", alias.Name);
    }

    [Fact]
    public void InSpecNpc_GetsTheSharedSandboxPackage_ExactlyOnce()
    {
        var spec = WithNpcs(Kjeld());
        var npc = new NpcSpec { EditorId = "Kjeld", Name = "Kjeld" };
        npc.Packages.Add("MFLiving_SandboxHere");   // author already added it by hand
        spec.Npcs.Add(npc);

        Generator.ExpandLivingNpcs(spec);

        Assert.Equal(1, npc.Packages.Count(p => p == "MFLiving_SandboxHere"));
    }

    [Fact]
    public void HostileInSpecNpc_BecomesAggressive_ButAnExplicitAggressionWins()
    {
        var spec = WithNpcs(
            new LivingNpcSpec { Ref = "Bandit", Alignment = "hostile" },
            new LivingNpcSpec { Ref = "Careful", Alignment = "hostile" });
        var bandit = new NpcSpec { EditorId = "Bandit" };
        var careful = new NpcSpec { EditorId = "Careful", Aggression = "Unaggressive" };
        spec.Npcs.Add(bandit);
        spec.Npcs.Add(careful);

        Generator.ExpandLivingNpcs(spec);

        Assert.Equal("Aggressive", bandit.Aggression);
        Assert.Equal("Unaggressive", careful.Aggression);
    }

    [Theory]
    [InlineData("adventurer", 0)]
    [InlineData("mageApprentice", 1)]
    [InlineData("merchant", 2)]
    [InlineData("herbalist", 3)]
    [InlineData("priest", 4)]
    [InlineData("bandit", 5)]
    [InlineData("  BANDIT  ", 5)]   // trimmed + case-insensitive
    [InlineData("nonsense", 0)]     // unknown falls back to adventurer; validation reports it
    public void ArchetypeName_MapsToTheAliasScriptBranch(string archetype, int code)
    {
        var spec = WithNpcs(new LivingNpcSpec { Ref = "Kjeld", Archetype = archetype });

        Generator.ExpandLivingNpcs(spec);

        var props = spec.Quests.Single().Aliases.Single().ScriptProperties;
        Assert.Equal(code, props.Single(p => p.Name == "Archetype").Int);
    }

    [Fact]
    public void AliasScript_PointsAtTheHoldMarker_AnchorsList_AndDeedGlobal()
    {
        var spec = WithNpcs(Kjeld());

        Generator.ExpandLivingNpcs(spec);

        var alias = spec.Quests.Single().Aliases.Single();
        Assert.Equal(Generator.LivingAliasScript, alias.Script);
        Assert.Equal("MFLiving_HoldMarker", alias.ScriptProperties.Single(p => p.Name == "HoldMarker").ObjectEditorId);
        Assert.Equal("MFLiving_Kjeld_Anchors", alias.ScriptProperties.Single(p => p.Name == "Anchors").ObjectEditorId);
        Assert.Equal("MFLiving_Kjeld_Deeds", alias.ScriptProperties.Single(p => p.Name == "DeedCount").ObjectEditorId);
    }

    // --- anchors and counters ---------------------------------------------------------------

    [Fact]
    public void EachAnchor_BecomesAnXMarker_AndTheFormListKeepsSpecOrder()
    {
        var spec = WithNpcs(new LivingNpcSpec
        {
            Ref = "Kjeld",
            Anchors =
            {
                new LivingAnchorSpec { Cell = "CellA", Position = new Vec3 { X = 1 } },
                new LivingAnchorSpec { Cell = "CellB", Position = new Vec3 { X = 2 } },
            },
        });

        Generator.ExpandLivingNpcs(spec);

        var a0 = spec.Placements.Single(p => p.EditorId == "MFLiving_Kjeld_A0");
        var a1 = spec.Placements.Single(p => p.EditorId == "MFLiving_Kjeld_A1");
        Assert.Equal("xmarker", a0.Kind);
        Assert.Equal("CellA", a0.Cell);
        Assert.Equal(2f, a1.Position.X);

        var flst = spec.FormLists.Single(f => f.EditorId == "MFLiving_Kjeld_Anchors");
        Assert.Equal(new[] { "MFLiving_Kjeld_A0", "MFLiving_Kjeld_A1" }, flst.Items);
    }

    [Fact]
    public void AnchorlessNpc_StillGetsAnEmptyAnchorsList()
    {
        // Validation flags this, but the alias script property must still resolve to something.
        var spec = WithNpcs(new LivingNpcSpec { Ref = "Kjeld" });

        Generator.ExpandLivingNpcs(spec);

        Assert.Empty(spec.FormLists.Single(f => f.EditorId == "MFLiving_Kjeld_Anchors").Items);
    }

    [Fact]
    public void EachNpc_GetsALongDeedGlobalStartingAtZero()
    {
        var spec = WithNpcs(Kjeld());

        Generator.ExpandLivingNpcs(spec);

        var deed = spec.Globals.Single(g => g.EditorId == "MFLiving_Kjeld_Deeds");
        Assert.Equal("long", deed.Type);
        Assert.Equal(0f, deed.Value);
    }

    // --- rumors -----------------------------------------------------------------------------

    [Fact]
    public void Rumor_IsGatedOnTheDeedGlobal_AndNamesTheNpc()
    {
        var spec = WithNpcs(new LivingNpcSpec
        {
            Ref = "Kjeld", Name = "Kjeld", Rumors = { "Kjeld cleared a barrow." },
        });
        spec.LivingNpcs!.RumorSpeaker = "Bard";

        Generator.ExpandLivingNpcs(spec);

        var rumor = spec.Dialogue.Single(d => d.EditorId == "MFLiving_Kjeld_Rumor");
        Assert.Equal("Bard", rumor.SpeakerNpcEditorId);
        Assert.Equal("MFLiving_Ctrl", rumor.QuestEditorId);
        Assert.Equal("Any word of Kjeld?", rumor.Prompt);
        Assert.Equal(new[] { "Kjeld cleared a barrow." }, rumor.Responses);

        var cond = rumor.Conditions.Single();
        Assert.Equal("GetGlobalValue", cond.Function);
        Assert.Equal("MFLiving_Kjeld_Deeds", cond.Param);
        Assert.Equal(">=", cond.Comparison);
        Assert.Equal(1f, cond.Value);
    }

    [Fact]
    public void UnnamedNpc_GetsTheGenericRumorPrompt()
    {
        var spec = WithNpcs(new LivingNpcSpec { Ref = "Kjeld", Rumors = { "Someone cleared a barrow." } });
        spec.LivingNpcs!.RumorSpeaker = "Bard";

        Generator.ExpandLivingNpcs(spec);

        Assert.Equal("Heard any rumors lately?", spec.Dialogue.Single().Prompt);
    }

    [Fact]
    public void NoSpeaker_OrNoRumors_EmitsNoRumorTopic()
    {
        var noSpeaker = WithNpcs(new LivingNpcSpec { Ref = "Kjeld", Rumors = { "line" } });
        var noRumors = WithNpcs(new LivingNpcSpec { Ref = "Kjeld" });
        noRumors.LivingNpcs!.RumorSpeaker = "Bard";

        Generator.ExpandLivingNpcs(noSpeaker);
        Generator.ExpandLivingNpcs(noRumors);

        Assert.Empty(noSpeaker.Dialogue);
        Assert.Empty(noRumors.Dialogue);
    }

    // --- interactions (the favor layer) ------------------------------------------------------

    [Fact]
    public void Interactions_EmitAFavorGlobal_AndOneTopicPerKnownKind()
    {
        var spec = WithNpcs(new LivingNpcSpec
        {
            Ref = "Kjeld", Interactions = { "fund", "praise", "parley" },
        });

        Generator.ExpandLivingNpcs(spec);

        Assert.Equal("long", spec.Globals.Single(g => g.EditorId == "MFLiving_Kjeld_Favor").Type);
        Assert.Equal(3, spec.Dialogue.Count);
        foreach (var kind in new[] { "fund", "praise", "parley" })
        {
            var dlg = spec.Dialogue.Single(d => d.EditorId == $"MFLiving_Kjeld_Act_{kind}");
            Assert.Equal("Kjeld", dlg.SpeakerNpcEditorId);
            Assert.Equal("MFLiving_Kjeld_Favor", dlg.SetGlobal!.Global);
        }
    }

    [Theory]
    [InlineData("fund", 1f, false)]
    [InlineData("praise", 1f, true)]   // only makes sense once he has actually done something
    [InlineData("parley", 5f, false)]  // de-escalation is worth more than coin
    public void EachInteractionKind_CarriesItsFavorDelta_AndDeedGate(string kind, float delta, bool gated)
    {
        var spec = WithNpcs(new LivingNpcSpec { Ref = "Kjeld", Interactions = { kind } });

        Generator.ExpandLivingNpcs(spec);

        var dlg = spec.Dialogue.Single();
        Assert.Equal(delta, dlg.SetGlobal!.Delta);
        Assert.Equal(gated, dlg.Conditions.Count == 1);
        if (gated) Assert.Equal("MFLiving_Kjeld_Deeds", dlg.Conditions[0].Param);
    }

    [Fact]
    public void UnknownInteractionKind_EmitsNoTopic_ButTheFavorGlobalStillExists()
    {
        var spec = WithNpcs(new LivingNpcSpec { Ref = "Kjeld", Interactions = { "bribe" } });

        Generator.ExpandLivingNpcs(spec);

        Assert.Empty(spec.Dialogue);   // validation reports the unknown kind
        Assert.Contains(spec.Globals, g => g.EditorId == "MFLiving_Kjeld_Favor");
    }

    [Fact]
    public void NoInteractions_EmitsNoFavorGlobal()
    {
        var spec = WithNpcs(Kjeld());

        Generator.ExpandLivingNpcs(spec);

        Assert.DoesNotContain(spec.Globals, g => g.EditorId.EndsWith("_Favor"));
    }
}
