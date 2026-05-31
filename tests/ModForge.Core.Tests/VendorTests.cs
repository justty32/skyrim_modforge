using ModForge;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Xunit;

namespace ModForge.Tests;

// Vendor / merchant capability regression tests. All MASTER-FREE: they build in memory and inspect
// the records, never reading Skyrim.esm — vendor data, faction membership, the merchant container,
// and validate guardrails are all master-independent (only external FormKeys are referenced, which
// don't require the master to be present to set/inspect).
public class VendorTests
{
    private static readonly ModKey Key = ModKey.FromNameAndExtension("Test.esp");
    private const uint JobMerchantFactionId = 0x051596;   // Skyrim.esm JobMerchantFaction

    // A minimal, valid vendor spec: a vendor faction, a merchant chest container + its placement,
    // and a shopkeeper NPC who is a member of the faction.
    private static ModSpec VendorSpecFixture() => new()
    {
        PluginName = "Test.esp",
        Factions =
        {
            new FactionSpec
            {
                EditorId = "MF_ShopFaction", Name = "Shop",
                Vendor = new VendorSpec
                {
                    StartHour = 8, EndHour = 20, Radius = 0, BuysStolen = false,
                    SellBuyList = "Skyrim.esm:0x06CB48", NotSellBuyList = true,
                    MerchantContainer = "MF_ShopChestRef",
                },
            },
        },
        Containers =
        {
            new ContainerSpec
            {
                EditorId = "MF_ShopChest", Name = "Merchant Chest",
                Items = { new ContainerEntrySpec { Item = "Skyrim.esm:0x072AE7", Count = 1 } }, // VendorGoldMisc
            },
        },
        Npcs =
        {
            new NpcSpec
            {
                EditorId = "MF_Shopkeeper", Name = "Merchant",
                Race = "Skyrim.esm:0x013746",
                Factions = { "MF_ShopFaction" },
                Greeting = "Looking to buy?",
            },
        },
        Cells = { new CellSpec { EditorId = "MF_Shop", Name = "Shop" } },
        Placements =
        {
            new PlacementSpec { Base = "MF_Shopkeeper", Cell = "MF_Shop" },
            new PlacementSpec { EditorId = "MF_ShopChestRef", Base = "MF_ShopChest", Cell = "MF_Shop" },
        },
    };

    [Fact]
    public void VendorFixture_IsValid()
    {
        Assert.Empty(Generator.Validate(VendorSpecFixture()));
    }

    [Fact]
    public void VendorFaction_CarriesVendorData()
    {
        var result = Generator.Build(VendorSpecFixture(), Key);
        var fact = Assert.Single(result.Mod.Factions, f => f.EditorID == "MF_ShopFaction");

        Assert.True(fact.Flags.HasFlag(Faction.FactionFlag.Vendor));
        Assert.NotNull(fact.VendorValues);
        Assert.Equal(8, fact.VendorValues!.StartHour);
        Assert.Equal(20, fact.VendorValues.EndHour);
        Assert.True(fact.VendorValues.NotSellBuy);
        Assert.False(fact.VendorValues.OnlyBuysStolenItems);
        // SellBuyList resolved to the external VendorItemsMisc FormList.
        Assert.False(fact.VendorBuySellList.IsNull);
        Assert.Equal(0x06CB48u, fact.VendorBuySellList.FormKey.ID);
    }

    [Fact]
    public void VendorFaction_MerchantContainer_PointsAtThePlacedChest()
    {
        var result = Generator.Build(VendorSpecFixture(), Key);
        var fact = Assert.Single(result.Mod.Factions, f => f.EditorID == "MF_ShopFaction");
        var chestRef = Assert.Single(
            result.Mod.EnumerateMajorRecords<IPlacedObjectGetter>(), p => p.EditorID == "MF_ShopChestRef");

        Assert.False(fact.MerchantContainer.IsNull);
        Assert.Equal(chestRef.FormKey, fact.MerchantContainer.FormKey);
        // VendorLocation anchors at the chest too.
        Assert.NotNull(fact.VendorLocation);
        var lt = Assert.IsAssignableFrom<ILocationTargetGetter>(fact.VendorLocation!.Target);
        Assert.Equal(chestRef.FormKey, lt.Link.FormKey);
    }

    [Fact]
    public void MerchantChest_Container_HoldsGold()
    {
        var result = Generator.Build(VendorSpecFixture(), Key);
        var chest = Assert.Single(result.Mod.Containers, c => c.EditorID == "MF_ShopChest");
        Assert.NotNull(chest.Items);
        var gold = Assert.Single(chest.Items!);   // the VendorGoldMisc leveled-gold entry
        Assert.Equal(0x072AE7u, gold.Item.Item.FormKey.ID);
        Assert.True(gold.Item.Count >= 1);         // gold count >= 0 guardrail (here 1 copy of the gold list)
    }

    [Fact]
    public void Shopkeeper_IsMemberOfTheVendorFaction_AndJobMerchantFaction()
    {
        var result = Generator.Build(VendorSpecFixture(), Key);
        var npc = Assert.Single(result.Mod.Npcs, n => n.EditorID == "MF_Shopkeeper");
        var shopFact = Assert.Single(result.Mod.Factions, f => f.EditorID == "MF_ShopFaction");

        // Member of the in-spec vendor faction at rank 0.
        Assert.Contains(npc.Factions, rp => rp.Faction.FormKey == shopFact.FormKey && rp.Rank == 0);
        // Auto-added JobMerchantFaction (so the generic "I'd like to trade" topic's condition matches).
        Assert.Contains(npc.Factions, rp =>
            rp.Faction.FormKey.ModKey.Name.Equals("Skyrim", System.StringComparison.OrdinalIgnoreCase)
            && rp.Faction.FormKey.ID == JobMerchantFactionId);
    }

    [Fact]
    public void Shopkeeper_IsConversable_HasHello()
    {
        var result = Generator.Build(VendorSpecFixture(), Key);
        // A greeting-only NPC gets an auto Hello topic (Misc/Hello) under a StartGameEnabled quest.
        var hello = Assert.Single(
            result.Mod.DialogTopics, t => t.EditorID == "MF_Shopkeeper_Hello");
        Assert.Equal(DialogTopic.SubtypeEnum.Hello, hello.Subtype);
    }

    [Fact]
    public void JobMerchantFaction_NotDuplicated_WhenSpecAlreadyListsIt()
    {
        var spec = VendorSpecFixture();
        spec.Npcs[0].Factions.Add("Skyrim.esm:0x051596");   // author lists JobMerchantFaction explicitly
        var result = Generator.Build(spec, Key);
        var npc = Assert.Single(result.Mod.Npcs, n => n.EditorID == "MF_Shopkeeper");
        Assert.Equal(1, npc.Factions.Count(rp =>
            rp.Faction.FormKey.ModKey.Name.Equals("Skyrim", System.StringComparison.OrdinalIgnoreCase)
            && rp.Faction.FormKey.ID == JobMerchantFactionId));
    }

    // ---- validate guardrails ----

    [Fact]
    public void Validate_RejectsHoursOutOfRange()
    {
        var spec = VendorSpecFixture();
        spec.Factions[0].Vendor!.EndHour = 30;
        Assert.Contains(Generator.Validate(spec), p => p.Contains("endHour") && p.Contains("range"));
    }

    [Fact]
    public void Validate_RejectsStartAfterEnd()
    {
        var spec = VendorSpecFixture();
        spec.Factions[0].Vendor!.StartHour = 20;
        spec.Factions[0].Vendor!.EndHour = 8;
        Assert.Contains(Generator.Validate(spec), p => p.Contains("never opens"));
    }

    [Fact]
    public void Validate_RejectsMissingMerchantContainer()
    {
        var spec = VendorSpecFixture();
        spec.Factions[0].Vendor!.MerchantContainer = "";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("merchantContainer is empty"));
    }

    [Fact]
    public void Validate_RejectsMerchantContainerThatIsNotAPlacement()
    {
        var spec = VendorSpecFixture();
        spec.Factions[0].Vendor!.MerchantContainer = "MF_ShopChest";   // the bare container, not the placed ref
        Assert.Contains(Generator.Validate(spec), p => p.Contains("must be a PLACEMENT editorId"));
    }

    [Fact]
    public void Validate_WarnsVendorNpcWithoutGreeting()
    {
        var spec = VendorSpecFixture();
        spec.Npcs[0].Greeting = "";
        Assert.Contains(Generator.Validate(spec), p => p.Contains("won't be conversable"));
    }

    [Fact]
    public void Validate_RejectsNoCategoriesTraded()
    {
        var spec = VendorSpecFixture();
        spec.Factions[0].Vendor!.SellBuyList = "";
        spec.Factions[0].Vendor!.NotSellBuyList = false;   // no list + DO-sell semantics = trades nothing
        Assert.Contains(Generator.Validate(spec), p => p.Contains("trades no item categories"));
    }
}
