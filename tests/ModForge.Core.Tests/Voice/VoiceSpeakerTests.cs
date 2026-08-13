using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Speaker resolution for the `voicelines` flow (Generator.ResolveVoiceSpeakers /
// SelectVoiceTargets / PackVoiceAudio) against in-memory built mods. The chain:
// GetIsID → GetIsAliasRef (alias → uniqueActor / forcedReference ACHR → base NPC) →
// GetInFaction (every plugin member, one target per distinct voiceType) → scene Dialog
// action fallback → loud, reasoned failure (never a silent skip).
public class VoiceSpeakerTests
{
    private static ILinkCache Cache(ISkyrimMod mod) => mod.ToImmutableLinkCache();

    private static IDialogTopicGetter Topic(ISkyrimMod mod, string editorId) =>
        mod.EnumerateMajorRecords<IDialogTopicGetter>().Single(t => t.EditorID == editorId);

    // -------------------------------------------------------- 1) GetIsID (the Build auto gate)
    [Fact]
    public void GetIsID_condition_resolves_the_single_speaker()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Npcs = { new NpcSpec { EditorId = "A", Name = "A" } },
            Dialogue = { new DialogueSpec { EditorId = "D1", QuestEditorId = "Q", SpeakerNpcEditorId = "A", Prompt = "Hi?", Responses = { "Hello." } } },
        };
        var mod = TestBuild.Ok(spec).Mod;
        var topic = Topic(mod, "D1");

        var res = Generator.ResolveVoiceSpeakers(topic, topic.Responses[0], mod, Cache(mod));

        Assert.True(res.Resolved, res.Reason ?? "");
        Assert.Equal("GetIsID", res.Source);
        Assert.Equal("A", Assert.Single(res.Speakers).Npc.EditorID);
    }

    // ------------------------------------------- 2) GetIsAliasRef → forced ref (ACHR) → base NPC
    [Fact]
    public void GetIsAliasRef_resolves_through_a_forced_ref_to_the_placed_npcs_base()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q", Aliases = { new QuestAliasSpec { Name = "Spk", Fill = "forced:ARef" } } } },
            Npcs = { new NpcSpec { EditorId = "A", Name = "A" } },
            Cells = { new CellSpec { EditorId = "Room", Name = "Room" } },
            Placements = { new PlacementSpec { Base = "A", Cell = "Room", Kind = "npc", EditorId = "ARef", Persistent = true } },
            // Deliberately speakerless: the gate is added below as a vanilla-style alias condition.
            Dialogue = { new DialogueSpec { EditorId = "D1", QuestEditorId = "Q", Prompt = "Hi?", Responses = { "Hello." } } },
        };
        var mod = TestBuild.Raw(spec).Mod;
        // BuildStandaloneQuestAliases runs BEFORE BuildPlacements, so a 'forced:' fill naming an
        // in-spec placement editorId can't resolve at build time — bind the ACHR here; the unit
        // under test is the resolver chain, not the alias-fill pass.
        var achr = mod.EnumerateMajorRecords<IPlacedNpc>().Single(p => p.EditorID == "ARef");
        mod.Quests.Single(q => q.EditorID == "Q").Aliases[0].ForcedReference.SetTo(achr.FormKey);
        var topic = mod.DialogTopics.Single(t => t.EditorID == "D1");
        topic.Responses[0].Conditions.Add(new ConditionFloat
        {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = 1f,
            Data = new GetIsAliasRefConditionData { ReferenceAliasIndex = 0 },
        });

        var res = Generator.ResolveVoiceSpeakers(topic, topic.Responses[0], mod, Cache(mod));

        Assert.True(res.Resolved, res.Reason ?? "");
        Assert.Equal("GetIsAliasRef", res.Source);
        Assert.Equal("A", Assert.Single(res.Speakers).Npc.EditorID);
    }

    // ------------------------- 3) GetInFaction → all members, deduped to one target per voiceType
    [Fact]
    public void GetInFaction_resolves_every_member_and_targets_dedupe_by_voicetype()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Factions = { new FactionSpec { EditorId = "F", Name = "F" } },
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Npcs =
            {
                new NpcSpec { EditorId = "N1", Name = "N1", Factions = { "F" } },
                new NpcSpec { EditorId = "N2", Name = "N2", Factions = { "F" } },
                new NpcSpec { EditorId = "N3", Name = "N3", Factions = { "F" } },
            },
            Dialogue =
            {
                new DialogueSpec
                {
                    EditorId = "D1", QuestEditorId = "Q", Prompt = "Hi?", Responses = { "Hello." },
                    Conditions = { new ConditionSpec { Function = "GetInFaction", Param = "F", Comparison = "==", Value = 1 } },
                },
            },
        };
        var mod = TestBuild.Raw(spec).Mod;
        // In-plugin voice types: N1 + N3 share VT_A, N2 gets VT_B.
        var vtA = mod.VoiceTypes.AddNew(); vtA.EditorID = "VT_A";
        var vtB = mod.VoiceTypes.AddNew(); vtB.EditorID = "VT_B";
        foreach (var n in mod.Npcs) n.Voice.SetTo(n.EditorID == "N2" ? vtB : vtA);

        var topic = mod.DialogTopics.Single(t => t.EditorID == "D1");
        var res = Generator.ResolveVoiceSpeakers(topic, topic.Responses[0], mod, Cache(mod));

        Assert.True(res.Resolved, res.Reason ?? "");
        Assert.Equal("GetInFaction", res.Source);
        Assert.Equal(new[] { "N1", "N2", "N3" }, res.Speakers.Select(s => s.Npc.EditorID).OrderBy(x => x).ToArray());

        // All three NPCs have templates → 2 generations (one per DISTINCT voiceType folder).
        var tpl = new VoiceTemplateSpec { Id = "t" };
        var all = new Dictionary<string, VoiceTemplateSpec?>(StringComparer.OrdinalIgnoreCase)
        { ["N1"] = tpl, ["N2"] = tpl, ["N3"] = tpl };
        var targets = Generator.SelectVoiceTargets(res, all);
        Assert.Equal(new[] { "VT_A", "VT_B" }, targets.Select(t => t.VoiceType).OrderBy(x => x).ToArray());

        // Only a member WITH a voiceTemplate claims its voiceType.
        var onlyN2 = new Dictionary<string, VoiceTemplateSpec?>(StringComparer.OrdinalIgnoreCase) { ["N2"] = tpl };
        Assert.Equal("VT_B", Assert.Single(Generator.SelectVoiceTargets(res, onlyN2)).VoiceType);
    }

    [Fact]
    public void BuildVoiceLinePlan_reports_speaker_voicetype_filename_and_path()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            VoiceTemplates = { new VoiceTemplateSpec { Id = "tpl" } },
            Factions = { new FactionSpec { EditorId = "F", Name = "F" } },
            Quests = { new QuestSpec { EditorId = "QuestLongName", Name = "Q" } },
            Npcs =
            {
                new NpcSpec { EditorId = "N1", Name = "N1", Factions = { "F" }, VoiceTemplate = "tpl" },
                new NpcSpec { EditorId = "N2", Name = "N2", Factions = { "F" } },
            },
            Dialogue =
            {
                new DialogueSpec
                {
                    EditorId = "TopicLongNameForVoice",
                    QuestEditorId = "QuestLongName",
                    Prompt = "Hi?",
                    Responses = { "Hello." },
                    Conditions = { new ConditionSpec { Function = "GetInFaction", Param = "F", Comparison = "==", Value = 1 } },
                },
            },
        };
        var mod = TestBuild.Raw(spec).Mod;
        var vtA = mod.VoiceTypes.AddNew(); vtA.EditorID = "VT_A";
        var vtB = mod.VoiceTypes.AddNew(); vtB.EditorID = "VT_B";
        foreach (var n in mod.Npcs) n.Voice.SetTo(n.EditorID == "N1" ? vtA : vtB);
        var topic = mod.DialogTopics.Single(t => t.EditorID == "TopicLongNameForVoice");
        var info = topic.Responses[0];
        var templates = new Dictionary<string, VoiceTemplateSpec?>(StringComparer.OrdinalIgnoreCase)
        {
            ["N1"] = spec.VoiceTemplates[0],
        };

        var plan = Generator.BuildVoiceLinePlan(mod, Cache(mod), templates, "Test.esp", "fuz")
            .OrderBy(p => p.VoiceType)
            .ToArray();

        Assert.Equal(2, plan.Length);
        Assert.Equal("QuestLongN_TopicLongNameFo_" + info.FormKey.ID.ToString("X8") + "_1.fuz", plan[0].FileName);
        Assert.Equal(Path.Combine("Sound", "Voice", "Test.esp", "VT_A", plan[0].FileName), plan[0].RelativePath);
        Assert.Equal("N1", Assert.Single(plan[0].Speakers));
        Assert.Equal("VT_A", plan[0].VoiceType);
        Assert.True(plan[0].HasVoiceTemplate);
        Assert.Null(plan[0].SkipReason);

        Assert.Equal("VT_B", plan[1].VoiceType);
        Assert.Equal("N2", Assert.Single(plan[1].Speakers));
        Assert.False(plan[1].HasVoiceTemplate);
        Assert.Contains("voiceTemplate", plan[1].SkipReason);
    }

    // External speaker (voiceSpeakers[]): a line gated on GetIsID of an NPC from ANOTHER master (the
    // mod-only cache can't resolve it) still plans a voice file — voiceType + template come from the map.
    [Fact]
    public void BuildVoiceLinePlan_resolves_an_external_speaker_via_voiceSpeakers_map()
    {
        var ext = Mutagen.Bethesda.Plugins.FormKey.Factory("0012C4:Other.esp");
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Dialogue =
            {
                new DialogueSpec
                {
                    EditorId = "D1", QuestEditorId = "Q", Prompt = "Hi?", Responses = { "Hello." },
                    // no in-spec speaker — gated manually on an EXTERNAL NPC (like an existing follower)
                    Conditions = { new ConditionSpec { Function = "GetIsID", Param = "Other.esp:0x0012C4", Comparison = "==", Value = 1 } },
                },
            },
        };
        var mod = TestBuild.Ok(spec).Mod;
        var topic = Topic(mod, "D1");

        // mod-only cache CAN'T resolve the external speaker — without the map, it's unresolved.
        var bare = Generator.BuildVoiceLinePlan(mod, Cache(mod), new Dictionary<string, VoiceTemplateSpec?>(), "Test.esp", "fuz");
        Assert.Contains("speaker unresolved", Assert.Single(bare).SkipReason);

        var tpl = new VoiceTemplateSpec { Id = "follower-f5" };
        var external = new Dictionary<Mutagen.Bethesda.Plugins.FormKey, (string, VoiceTemplateSpec?)>
        { [ext] = ("FollowerVoiceType", tpl) };
        var plan = Assert.Single(Generator.BuildVoiceLinePlan(
            mod, Cache(mod), new Dictionary<string, VoiceTemplateSpec?>(), "Test.esp", "fuz", null, external));

        Assert.Equal("voiceSpeakers", plan.ResolutionSource);
        Assert.Equal("FollowerVoiceType", plan.VoiceType);
        Assert.Equal("follower-f5", plan.TemplateId);
        Assert.True(plan.HasVoiceTemplate);
        Assert.Null(plan.SkipReason);
        Assert.Equal(Path.Combine("Sound", "Voice", "Test.esp", "FollowerVoiceType", plan.FileName), plan.RelativePath);
    }

    // ------------------- 4) Scene phase INFO (no conditions) → SCEN Dialog action → alias → NPC
    [Fact]
    public void Scene_phase_info_resolves_via_the_scene_dialog_action()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "SQ", Name = "SQ" } },
            Npcs = { new NpcSpec { EditorId = "A", Name = "A" }, new NpcSpec { EditorId = "B", Name = "B" } },
            Scenes =
            {
                new SceneSpec
                {
                    EditorId = "Sc", QuestEditorId = "SQ",
                    Actors =
                    {
                        new SceneActorSpec { AliasId = 0, Npc = "A", Name = "A" },
                        new SceneActorSpec { AliasId = 1, Npc = "B", Name = "B" },
                    },
                    Phases = { new ScenePhaseSpec { Speaker = 1, Lines = { "B speaks." } } },
                },
            },
        };
        var mod = TestBuild.Ok(spec).Mod;
        var topic = Topic(mod, "Sc_P0");
        var info = topic.Responses[0];
        Assert.Empty(info.Conditions);   // scene INFOs carry no conditions — the SCEN action binds the speaker

        var res = Generator.ResolveVoiceSpeakers(topic, info, mod, Cache(mod));

        Assert.True(res.Resolved, res.Reason ?? "");
        Assert.Equal("SceneAction", res.Source);
        Assert.Equal("B", Assert.Single(res.Speakers).Npc.EditorID);
    }

    // -------------------------------------------------- 5) unresolvable → reported with a reason
    [Fact]
    public void Unresolvable_speaker_is_reported_with_a_reason_not_silent()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Dialogue = { new DialogueSpec { EditorId = "D1", QuestEditorId = "Q", Prompt = "?", Responses = { "Who says this?" } } },
        };
        var mod = TestBuild.Raw(spec).Mod;
        var topic = Topic(mod, "D1");

        var res = Generator.ResolveVoiceSpeakers(topic, topic.Responses[0], mod, Cache(mod));

        Assert.False(res.Resolved);
        Assert.False(string.IsNullOrWhiteSpace(res.Reason));
        Assert.Empty(Generator.SelectVoiceTargets(res, new Dictionary<string, VoiceTemplateSpec?>()));
    }

    // A player-targeted GetInFaction (identity gate: runOn Reference → PlayerRef) says nothing
    // about the SPEAKER and must not be misread as one.
    [Fact]
    public void Player_targeted_GetInFaction_is_not_treated_as_the_speaker()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Factions = { new FactionSpec { EditorId = "F", Name = "F" } },
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Npcs = { new NpcSpec { EditorId = "N1", Name = "N1", Factions = { "F" } } },
            Dialogue =
            {
                new DialogueSpec
                {
                    EditorId = "D1", QuestEditorId = "Q", Prompt = "?", Responses = { "Line." },
                    Conditions =
                    {
                        new ConditionSpec
                        {
                            Function = "GetInFaction", Param = "F", Comparison = ">=", Value = 1,
                            RunOn = "Reference", Reference = "Skyrim.esm:0x000014",
                        },
                    },
                },
            },
        };
        var mod = TestBuild.Raw(spec).Mod;
        var topic = Topic(mod, "D1");

        var res = Generator.ResolveVoiceSpeakers(topic, topic.Responses[0], mod, Cache(mod));

        Assert.False(res.Resolved);
        Assert.Contains("GetInFaction", res.Reason!);
    }

    // ----------------------------------------- B) fuz downgrade: never pack raw PCM into a .fuz
    [Fact]
    public void PackVoiceAudio_fuz_without_xwm_falls_back_to_loose_wav_with_warning()
    {
        byte[] wav = { 1, 2, 3 };
        byte[] lip = { 9 };

        var p = Generator.PackVoiceAudio("fuz", wav, null, lip, "Q_T_00000801_1");

        Assert.Equal("wav", p.Ext);        // loose .wav next to the intended .fuz path
        Assert.Same(wav, p.Data);          // raw PCM is never wrapped in a FUZE container
        Assert.Same(lip, p.LooseLip);      // generated lip survives as a loose sidecar
        Assert.NotNull(p.Warning);
        Assert.Contains("xWMAEncode", p.Warning!);
        Assert.Contains("MODFORGE_XWMAENCODE", p.Warning!);
        Assert.Contains("lip", p.Warning!);
    }

    [Fact]
    public void PackVoiceAudio_fuz_with_xwm_packs_a_real_fuz_silently()
    {
        var p = Generator.PackVoiceAudio("fuz", new byte[] { 1 }, new byte[] { 4, 5, 6 }, null, "f");

        Assert.Equal("fuz", p.Ext);
        Assert.Null(p.Warning);
        Assert.Null(p.LooseLip);
        Assert.Equal((byte)'F', p.Data[0]);   // FUZE magic
    }
}
