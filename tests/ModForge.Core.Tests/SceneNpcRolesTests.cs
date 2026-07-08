using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Idea #24 §D — the in-game scene-export `npcRoles:` macro tags an EXTERNAL captured NPC (a PROTEUS
// clone / follower base) with a job role and expands it into the low-level records the build already
// handles: a StartGameEnabled host quest, a conditioned Hello greeting (GetIsID gate on the external
// NPC), and a sandbox package attached via NpcPatch. Offline-verifiable; no new record type.
public class SceneNpcRolesTests
{
    // Carlotta (a real vanilla NPC) stands in for a captured clone base — an EXTERNAL ref not in the spec.
    private const string ExtNpc = "Skyrim.esm:0x013B99";

    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    private static ModSpec RoleSpec() => new()
    {
        PluginName = "MFScene.esp",
        NpcRoles = { new SceneNpcRoleSpec { Npc = ExtNpc, Role = "blacksmith", Backstory = "old Legion smith" } },
    };

    // -- validation --------------------------------------------------------------------------------

    [Fact]
    public void Valid_NoProblems() => Assert.Empty(Validate(RoleSpec()));

    [Fact]
    public void Validate_MissingNpc_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp", NpcRoles = { new SceneNpcRoleSpec { Role = "blacksmith" } } };
        Assert.Contains(Validate(s), p => p.Contains("missing npc"));
    }

    [Fact]
    public void Validate_UnknownRole_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp", NpcRoles = { new SceneNpcRoleSpec { Npc = ExtNpc, Role = "wizard" } } };
        Assert.Contains(Validate(s), p => p.Contains("unknown role 'wizard'"));
    }

    // -- expansion (assert on the mutated spec, like SettlementTests) -------------------------------

    [Fact]
    public void Expand_Blacksmith_AddsHostQuestGreetingPackageAndPatch()
    {
        var s = RoleSpec();
        Generator.ExpandNpcRoles(s);

        // StartGameEnabled host quest for the greeting.
        var host = s.Quests.Single();
        Assert.Equal("MF_SceneNpcRolesQ", host.EditorId);
        Assert.True(host.StartGameEnabled);

        // Conditioned greeting on the external NPC.
        var hello = s.Dialogue.Single();
        Assert.True(hello.Hello);
        Assert.Equal(ExtNpc, hello.SpeakerNpcEditorId);
        Assert.Equal("MF_SceneNpcRolesQ", hello.QuestEditorId);
        Assert.NotEmpty(hello.Responses);

        // Sandbox package (editor-location fallback = no explicit location) ...
        var pkg = s.Packages.Single();
        Assert.Equal(Generator.SandboxTemplateRef, pkg.Template);
        Assert.Equal("", pkg.Sandbox.Location);

        // ... attached to the external NPC via an appending NpcPatch.
        var patch = s.NpcPatches.Single();
        Assert.Equal(ExtNpc, patch.OverrideOf);
        Assert.Equal("append", patch.Mode);
        Assert.Contains(pkg.EditorId, patch.Packages);
    }

    [Fact]
    public void Expand_Idempotent_GuardPreventsDoubleExpand()
    {
        var s = RoleSpec();
        Generator.ExpandNpcRoles(s);
        Generator.ExpandNpcRoles(s);   // second call must be a no-op (guard)
        Assert.Single(s.Dialogue);
        Assert.Single(s.Packages);
        Assert.Single(s.NpcPatches);
    }

    [Fact]
    public void Expand_UnknownRole_EmitsNoRecords()
    {
        var s = new ModSpec { PluginName = "M.esp", NpcRoles = { new SceneNpcRoleSpec { Npc = ExtNpc, Role = "wizard" } } };
        Generator.ExpandNpcRoles(s);
        Assert.Empty(s.Dialogue);
        Assert.Empty(s.Packages);
        Assert.Empty(s.NpcPatches);
        Assert.Empty(s.Quests);   // host quest only added when a known role expands
    }

    // -- behaviour invariance: no npcRoles ⇒ the macro adds nothing --------------------------------

    [Fact]
    public void Expand_NoNpcRoles_IsNoOp()
    {
        var s = new ModSpec { PluginName = "M.esp", Npcs = { new NpcSpec { EditorId = "N" } } };
        Generator.ExpandNpcRoles(s);
        Assert.Empty(s.Quests);
        Assert.Empty(s.Dialogue);
        Assert.Empty(s.Packages);
        Assert.Empty(s.NpcPatches);
    }

    // -- build: the enabling fix — the greeting's GetIsID gate targets the EXTERNAL NPC's FormKey ---

    [Fact]
    public void Build_Blacksmith_HelloGatedByGetIsIDOnExternalNpc()
    {
        var s = RoleSpec();
        var r = Generator.Build(s, ModKey.FromNameAndExtension("MFScene.esp"));

        var hello = r.Mod.EnumerateMajorRecords<IDialogTopicGetter>()
            .Single(t => t.Subtype == DialogTopic.SubtypeEnum.Hello);

        // The Hello carries our conditioned greeting INFO (blacksmith line) FIRST, then a gated fallback.
        // Find the blacksmith greeting and assert it is gated GetIsID to the EXTERNAL captured NPC.
        var carlotta = FormKey.Factory("013B99:Skyrim.esm");
        var greeting = hello.Responses.Single(i =>
            i.Responses.Any(rsp => rsp.Text.String != null && rsp.Text.String.Contains("forged")));
        var getIsId = greeting.Conditions
            .Select(c => c.Data)
            .OfType<IGetIsIDConditionDataGetter>()
            .Single();
        Assert.Equal(carlotta, getIsId.Object.Link.FormKey);

        // Every INFO in the Hello (greeting + fallback) is gated to that one NPC — no ungated line leaks.
        Assert.All(hello.Responses, i => Assert.Contains(
            i.Conditions.Select(c => c.Data).OfType<IGetIsIDConditionDataGetter>(),
            g => g.Object.Link.FormKey == carlotta));
    }
}
