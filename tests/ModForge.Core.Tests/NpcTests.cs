using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// NPC (NPC_) configuration flags + inventory (Items). Essential/Protected gate whether an actor can be
// killed (needed e.g. for a non-lethal brawl). Inventory entries are master-free (only FormKeys set).
public class NpcTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("Test.esp");
    private const uint Gold001 = 0x00000F;        // Skyrim.esm Gold001

    private static INpcGetter BuildNpc(NpcSpec n)
    {
        var spec = new ModSpec { PluginName = "Test.esp", Npcs = { n } };
        return TestBuild.Ok(spec).Mod.EnumerateMajorRecords<INpcGetter>().Single();
    }

    [Fact]
    public void Essential_SetsConfigFlag()
    {
        var npc = BuildNpc(new NpcSpec { EditorId = "N", Name = "N", Essential = true });
        Assert.True(npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Essential));
    }

    [Fact]
    public void Protected_SetsConfigFlag()
    {
        var npc = BuildNpc(new NpcSpec { EditorId = "N", Name = "N", Protected = true });
        Assert.True(npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Protected));
    }

    [Fact]
    public void Npc_NotEssentialOrProtected_ByDefault()
    {
        var npc = BuildNpc(new NpcSpec { EditorId = "N", Name = "N" });
        Assert.False(npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Essential));
        Assert.False(npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Protected));
    }

    // ---- inventory (Items) ----

    // An NPC carrying a vanilla weapon, 100 gold, and an in-spec custom weapon (forward ref).
    private static ModSpec InventoryFixture() => new()
    {
        PluginName = "Test.esp",
        Weapons =
        {
            // declared AFTER the NPC in spec order to exercise the forward-ref-safe path
            new WeaponSpec { EditorId = "MF_CustomBlade", Name = "Custom Blade", Template = "Skyrim.esm:0x012EB7" },
        },
        Npcs =
        {
            new NpcSpec
            {
                EditorId = "MF_Carrier", Name = "Carrier",
                Race = "Skyrim.esm:0x013746",
                Items =
                {
                    new NpcItemSpec { Item = "Skyrim.esm:0x012EB7", Count = 1 },   // vanilla iron sword
                    new NpcItemSpec { Item = "Skyrim.esm:0x00000F", Count = 100 }, // gold
                    new NpcItemSpec { Item = "MF_CustomBlade", Count = 1 },         // in-spec (forward) ref
                },
            },
        },
    };

    [Fact]
    public void InventoryFixture_IsValid()
    {
        Assert.Empty(Generator.Validate(InventoryFixture()));
    }

    [Fact]
    public void Npc_CarriesAllItems_WithCounts()
    {
        var result = Generator.Build(InventoryFixture(), Key);
        var npc = Assert.Single(result.Mod.Npcs, n => n.EditorID == "MF_Carrier");

        Assert.NotNull(npc.Items);
        Assert.Equal(3, npc.Items!.Count);

        // 100 gold present with the right count.
        var gold = Assert.Single(npc.Items!, e => e.Item.Item.FormKey.ID == Gold001);
        Assert.Equal(100, gold.Item.Count);

        // vanilla iron sword present at count 1.
        var sword = Assert.Single(npc.Items!, e => e.Item.Item.FormKey.ID == 0x012EB7);
        Assert.Equal(1, sword.Item.Count);
    }

    [Fact]
    public void Npc_InspecWeaponRef_ResolvesForward()
    {
        var result = Generator.Build(InventoryFixture(), Key);
        var npc = Assert.Single(result.Mod.Npcs, n => n.EditorID == "MF_Carrier");
        var blade = Assert.Single(result.Mod.Weapons, w => w.EditorID == "MF_CustomBlade");

        // the in-spec custom weapon (declared after the NPC) resolved to its FormKey.
        Assert.Contains(npc.Items!, e => e.Item.Item.FormKey == blade.FormKey && e.Item.Count == 1);
    }

    [Fact]
    public void Npc_NoItems_LeavesInventoryNullOrEmpty()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Npcs = { new NpcSpec { EditorId = "MF_Empty", Name = "Empty", Race = "Skyrim.esm:0x013746" } },
        };
        var result = Generator.Build(spec, Key);
        var npc = Assert.Single(result.Mod.Npcs, n => n.EditorID == "MF_Empty");
        Assert.True(npc.Items == null || npc.Items.Count == 0);
    }

    [Fact]
    public void Build_WarnsOnUnresolvedItemRef()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Npcs =
            {
                new NpcSpec
                {
                    EditorId = "MF_BadItem", Name = "Bad", Race = "Skyrim.esm:0x013746",
                    Items = { new NpcItemSpec { Item = "NoSuchThing", Count = 1 } },
                },
            },
        };
        var result = Generator.Build(spec, Key);
        Assert.Contains(result.Warnings, w => w.Contains("item") && w.Contains("NoSuchThing"));
    }

    // ---- validate guardrails ----

    [Fact]
    public void Validate_RejectsEmptyItemRef()
    {
        var spec = InventoryFixture();
        spec.Npcs[0].Items[0].Item = "";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("item") && p.Contains("MF_Carrier"));
    }

    [Fact]
    public void Validate_RejectsZeroCount()
    {
        var spec = InventoryFixture();
        spec.Npcs[0].Items[0].Count = 0;
        Assert.Contains(Generator.Validate(spec), p => p.Contains("count") && p.Contains("MF_Carrier"));
    }

    [Fact]
    public void Validate_RejectsUnknownItemRef()
    {
        var spec = InventoryFixture();
        spec.Npcs[0].Items[0].Item = "NotAReference";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("item") && p.Contains("NotAReference"));
    }
}
