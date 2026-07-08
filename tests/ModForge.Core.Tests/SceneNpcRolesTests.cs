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
    // Uthgerd (a non-vendor Whiterun NPC) stands in for a captured clone base — an EXTERNAL ref not in the spec.
    private const string ExtNpc = "Skyrim.esm:0x0918E2";

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

    // -- vendor: a blacksmith with a companion placement gets a shop (FACT + chest + membership) ------

    [Fact]
    public void Expand_Blacksmith_WithCompanionPlacement_AddsVendorShop()
    {
        var s = RoleSpec();
        // The scene puts the captured NPC in the world (kind:npc, base == npc) — the shop co-locates here.
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "SmithRef", Base = ExtNpc, Kind = "npc",
            Worldspace = "Skyrim.esm:0x00003C", Position = new Vec3 { X = 180, Y = -9000, Z = -3800 },
        });
        Generator.ExpandNpcRoles(s);

        // Vendor FACT with the blacksmith buy/sell list + a placed merchant chest holding gold.
        var vendorFac = s.Factions.Single(f => f.Vendor is not null);
        Assert.Equal("Skyrim.esm:0x066333", vendorFac.Vendor!.SellBuyList);
        var chest = s.Containers.Single();
        // Stocked with the vanilla blacksmith merchant leveled lists (gold + weapons/armor/misc), not a
        // flat gold pile — so the barter shows real wares + a vendor gold pool.
        Assert.Contains(chest.Items, e => e.Item == "Skyrim.esm:0x072AE9");   // VendorGoldBlacksmith
        Assert.True(chest.Items.Count >= 3, "chest should hold gold + weapon/armor/misc stock lists");
        var chestRef = s.Placements.Single(p => p.Base == chest.EditorId);
        Assert.Equal("Skyrim.esm:0x00003C", chestRef.Worldspace);   // co-located with the smith
        Assert.Equal(180f, chestRef.Position.X);

        // The override joins the vendor FACT + vanilla JobMerchantFaction (surfaces "I'd like to trade").
        var patch = s.NpcPatches.Single();
        Assert.Contains(vendorFac.EditorId, patch.Factions);
        Assert.Contains(Generator.JobMerchantFactionRef, patch.Factions);
    }

    // In-spec NPC (a fresh clone stand-in — the only kind that can be PLACED & appear): package + vendor
    // FACT attach DIRECTLY to the NpcSpec (no NpcPatch), and BuildNpcs auto-adds JobMerchant.
    [Fact]
    public void Expand_Blacksmith_InSpecNpc_AttachesDirectly_NoPatch()
    {
        var s = new ModSpec
        {
            PluginName = "M.esp",
            Npcs = { new NpcSpec { EditorId = "Smith", Name = "Brynja", Race = "Skyrim.esm:0x013746" } },
            Placements = { new PlacementSpec { EditorId = "SmithRef", Base = "Smith", Kind = "npc",
                Worldspace = "Skyrim.esm:0x00003C", Position = new Vec3 { X = 1, Y = 2, Z = 3 } } },
            NpcRoles = { new SceneNpcRoleSpec { Npc = "Smith", Role = "blacksmith" } },
        };
        Generator.ExpandNpcRoles(s);

        var smith = s.Npcs.Single();
        var vendorFac = s.Factions.Single(f => f.Vendor is not null);
        Assert.Contains("MFRole_Smith_1_Sandbox", smith.Packages);   // package attached directly
        Assert.Contains(vendorFac.EditorId, smith.Factions);          // vendor FACT joined directly
        Assert.Empty(s.NpcPatches);                                   // in-spec path uses NO NpcPatch
    }

    [Fact]
    public void Expand_Blacksmith_NoCompanionPlacement_SkipsVendor()
    {
        var s = RoleSpec();   // no placements[]
        Generator.ExpandNpcRoles(s);
        Assert.Empty(s.Containers);
        Assert.DoesNotContain(s.Factions, f => f.Vendor is not null);
        Assert.Empty(s.NpcPatches.Single().Factions);   // greeting + package still present, just no shop
    }

    [Fact]
    public void Build_Blacksmith_OverrideNpcJoinsVendorAndJobMerchantFactions()
    {
        var s = RoleSpec();
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "SmithRef", Base = ExtNpc, Kind = "npc",
            Worldspace = "Skyrim.esm:0x00003C", Position = new Vec3 { X = 1, Y = 2, Z = 3 },
        });
        var r = Generator.Build(s, ModKey.FromNameAndExtension("MFScene.esp"));

        var npc = r.Mod.Npcs.Single(n => n.FormKey == FormKey.Factory("0918E2:Skyrim.esm"));
        Assert.Contains(npc.Factions, rp => rp.Faction.FormKey == FormKey.Factory("051596:Skyrim.esm")); // JobMerchant
        // the generated vendor FACT is in-plugin; assert the NPC is in SOME in-plugin faction too
        Assert.Contains(npc.Factions, rp => rp.Faction.FormKey.ModKey == r.Mod.ModKey);
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
        var uthgerd = FormKey.Factory("0918E2:Skyrim.esm");
        var greeting = hello.Responses.Single(i =>
            i.Responses.Any(rsp => rsp.Text.String != null && rsp.Text.String.Contains("forged")));
        var getIsId = greeting.Conditions
            .Select(c => c.Data)
            .OfType<IGetIsIDConditionDataGetter>()
            .Single();
        Assert.Equal(uthgerd, getIsId.Object.Link.FormKey);

        // Every INFO in the Hello (greeting + fallback) is gated to that one NPC — no ungated line leaks.
        Assert.All(hello.Responses, i => Assert.Contains(
            i.Conditions.Select(c => c.Data).OfType<IGetIsIDConditionDataGetter>(),
            g => g.Object.Link.FormKey == uthgerd));
    }
}
