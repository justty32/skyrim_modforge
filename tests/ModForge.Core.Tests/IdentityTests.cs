using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Locks in the lightweight identity/class system: each identity = a FACT (persistent signal) +
// priority + optional grants/acquire. Acquire via MFIdentityBook OnRead; gate via identity/
// primaryIdentity → GetInFaction CTDA. Design: docs/superpowers/specs/2026-06-06-identity-system-design.md.
public class IdentityTests
{
    [Fact]
    public void IdentitySpec_defaults()
    {
        var i = new IdentitySpec();
        Assert.Equal("", i.Id);
        Assert.Equal("", i.Faction);
        Assert.Equal(0, i.Priority);
        Assert.Empty(i.Grants);
        Assert.False(i.Toggle);
        Assert.False(i.Default);
        Assert.Null(i.OnAcquire);
    }

    [Fact]
    public void Identity_builds_a_FACT_for_a_bare_faction_editorId()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Identities = { new IdentitySpec { Id = "Paladin", Faction = "MF_FactPaladin", Priority = 30 } },
        };
        var r = TestBuild.Ok(spec);
        Assert.Contains(r.Mod.EnumerateMajorRecords<IFactionGetter>(), f => f.EditorID == "MF_FactPaladin");
    }

    [Fact]
    public void Identity_does_not_rebuild_an_external_or_declared_faction()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Factions = { new FactionSpec { EditorId = "MF_Declared", Name = "Declared" } },
            Identities =
            {
                new IdentitySpec { Id = "A", Faction = "MF_Declared" },          // already a factions[] entry
                new IdentitySpec { Id = "B", Faction = "Skyrim.esm:0x01BCC0" },  // external — use as-is
            },
        };
        var r = TestBuild.Ok(spec);
        // Only the one declared FACT is built (no dup for MF_Declared, none for the external).
        Assert.Single(r.Mod.EnumerateMajorRecords<IFactionGetter>());
    }

    [Fact]
    public void Validate_flags_duplicate_identity_id_and_bad_grant()
    {
        var spec = new ModSpec
        {
            Identities =
            {
                new IdentitySpec { Id = "Paladin", Faction = "MF_F1", Grants = { "NoSuchSpell" } },
                new IdentitySpec { Id = "Paladin", Faction = "MF_F2" },   // dup id
            },
        };
        var probs = Generator.Validate(spec);
        Assert.Contains(probs, p => p.Contains("duplicate identity id 'Paladin'"));
        Assert.Contains(probs, p => p.Contains("grant") && p.Contains("NoSuchSpell"));
    }

    [Fact]
    public void PrimaryIdentity_tag_gates_dialogue_on_held_plus_the_controller_primary_global()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Npcs = { new NpcSpec { EditorId = "Guard", Name = "Guard" } },
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Identities =
            {
                new IdentitySpec { Id = "Paladin", Faction = "MF_FactPaladin", Priority = 30 },
                new IdentitySpec { Id = "Merchant", Faction = "MF_FactMerchant", Priority = 20 },
            },
            Dialogue =
            {
                new DialogueSpec
                {
                    EditorId = "Hi", QuestEditorId = "Q", SpeakerNpcEditorId = "Guard",
                    Responses = { "Good day, merchant." }, PrimaryIdentity = "Merchant",
                },
            },
        };
        var r = TestBuild.Ok(spec);
        var info = r.Mod.EnumerateMajorRecords<IDialogResponsesGetter>().Single(i =>
            i.Conditions.Any(c => c.Data is IGetInFactionConditionDataGetter));
        // primaryIdentity now resolves via the controller-maintained MF_PrimaryIdentity global, NOT a
        // higher-priority faction-exclusion chain: GetInFaction(Merchant)>=1 AND GetGlobalValue==code.
        var gif = info.Conditions.OfType<IConditionFloatGetter>().Where(c => c.Data is IGetInFactionConditionDataGetter).ToList();
        Assert.Single(gif);   // own held only — no Paladin==0 exclusion anymore
        Assert.Equal(CompareOperator.GreaterThanOrEqualTo, gif[0].CompareOperator);
        var glob = info.Conditions.OfType<IConditionFloatGetter>().Single(c => c.Data is IGetGlobalValueConditionDataGetter);
        Assert.Equal(CompareOperator.EqualTo, glob.CompareOperator);
        Assert.Equal(2f, glob.ComparisonValue);   // Merchant is the 2nd identity → code 2
        var primaryGlob = r.Mod.EnumerateMajorRecords<IGlobalGetter>().Single(g => g.EditorID == "MF_PrimaryIdentity");
        Assert.Equal(primaryGlob.FormKey, ((IGetGlobalValueConditionDataGetter)glob.Data).Global.Link.FormKey);
    }

    [Fact]
    public void PrimaryIdentity_use_builds_controller_quest_and_two_globals()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Npcs = { new NpcSpec { EditorId = "Guard", Name = "Guard" } },
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Identities = { new IdentitySpec { Id = "Merchant", Faction = "MF_FactMerchant", Priority = 20 } },
            Dialogue = { new DialogueSpec { EditorId = "Hi", QuestEditorId = "Q", SpeakerNpcEditorId = "Guard",
                Responses = { "Hi." }, PrimaryIdentity = "Merchant" } },
        };
        var r = TestBuild.Ok(spec);
        Assert.Contains(r.Mod.EnumerateMajorRecords<IGlobalGetter>(), g => g.EditorID == "MF_PrimaryIdentity");
        Assert.Contains(r.Mod.EnumerateMajorRecords<IGlobalGetter>(), g => g.EditorID == "MF_IdentityOverride");
        var quest = r.Mod.EnumerateMajorRecords<IQuestGetter>().Single(q => q.EditorID == "MF_IdentityControllerQuest");
        Assert.True(quest.Flags.HasFlag(Quest.Flag.StartGameEnabled));
        var entry = quest.VirtualMachineAdapter!.Scripts.Single(e => e.Name == "MFIdentityController");
        Assert.Contains(entry.Properties, p => p.Name == "Primary");
        Assert.Contains(entry.Properties, p => p.Name == "Override");
        var codes = (IScriptIntListPropertyGetter)entry.Properties.Single(p => p.Name == "Codes");
        Assert.Equal(new[] { 1 }, codes.Data.ToArray());   // single identity → code 1
    }

    [Fact]
    public void Identity_acquireBook_gets_MFIdentityBook_script_with_bound_props()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Books = { new BookSpec { EditorId = "MF_Tome", Name = "Paladin Tome", Template = "Skyrim.esm:0x0ED161" } },
            Identities =
            {
                new IdentitySpec
                {
                    Id = "Paladin", Faction = "MF_FactPaladin", Priority = 30,
                    Grants = { "Skyrim.esm:0x0005AD5C" }, AcquireBook = "MF_Tome", Toggle = false,
                },
            },
        };
        var r = TestBuild.Ok(spec);
        var book = r.Mod.EnumerateMajorRecords<IBookGetter>().Single(b => b.EditorID == "MF_Tome");
        Assert.NotNull(book.VirtualMachineAdapter);
        var entry = book.VirtualMachineAdapter!.Scripts.Single(e => e.Name == "MFIdentityBook");
        var faction = (IScriptObjectPropertyGetter)entry.Properties.Single(p => p.Name == "TheFaction");
        var palFk = r.Mod.EnumerateMajorRecords<IFactionGetter>().Single(f => f.EditorID == "MF_FactPaladin").FormKey;
        Assert.Equal(palFk, faction.Object.FormKey);
        Assert.Contains(entry.Properties, p => p.Name == "GrantAbility");
        Assert.Contains(entry.Properties.OfType<IScriptBoolPropertyGetter>(), p => p.Name == "Toggle" && p.Data == false);
    }

    [Fact]
    public void Identity_grantPerks_binds_GrantPerk_on_the_acquire_book_and_Perks_on_the_default_quest()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Books = { new BookSpec { EditorId = "MF_Tome", Name = "Tome", Template = "Skyrim.esm:0x0ED161" } },
            Perks =
            {
                new PerkSpec { EditorId = "MF_PerkA", Name = "A" },
                new PerkSpec { EditorId = "MF_PerkB", Name = "B" },
            },
            Identities =
            {
                new IdentitySpec { Id = "Paladin", Faction = "MF_FactPaladin", Priority = 30,
                    AcquireBook = "MF_Tome", GrantPerks = { "MF_PerkA" } },
                new IdentitySpec { Id = "Adventurer", Faction = "MF_FactAdv", Default = true,
                    GrantPerks = { "MF_PerkB" } },
            },
        };
        var r = TestBuild.Ok(spec);
        var perkA = r.Mod.EnumerateMajorRecords<IPerkGetter>().Single(p => p.EditorID == "MF_PerkA").FormKey;
        var perkB = r.Mod.EnumerateMajorRecords<IPerkGetter>().Single(p => p.EditorID == "MF_PerkB").FormKey;

        // Book binds GrantPerk = grantPerks[0].
        var book = r.Mod.EnumerateMajorRecords<IBookGetter>().Single(b => b.EditorID == "MF_Tome");
        var bookEntry = book.VirtualMachineAdapter!.Scripts.Single(e => e.Name == "MFIdentityBook");
        var gp = (IScriptObjectPropertyGetter)bookEntry.Properties.Single(p => p.Name == "GrantPerk");
        Assert.Equal(perkA, gp.Object.FormKey);

        // Default-grant quest binds Perks[] for the default identity.
        var quest = r.Mod.EnumerateMajorRecords<IQuestGetter>().Single(q => q.EditorID == "MF_IdentityDefaultQuest");
        var qEntry = quest.VirtualMachineAdapter!.Scripts.Single(e => e.Name == "MFIdentityDefault");
        var perks = (IScriptObjectListPropertyGetter)qEntry.Properties.Single(p => p.Name == "Perks");
        Assert.Equal(perkB, Assert.Single(perks.Objects).Object.FormKey);
    }

    [Fact]
    public void Default_identity_builds_a_StartGameEnabled_granter_quest_with_faction_and_grant_lists()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Identities =
            {
                new IdentitySpec
                {
                    Id = "Adventurer", Faction = "MF_FactAdventurer", Priority = 0, Default = true,
                    Grants = { "Skyrim.esm:0x0005AD5C" },
                },
                new IdentitySpec { Id = "Paladin", Faction = "MF_FactPaladin", Priority = 30 }, // non-default → excluded
            },
        };
        var r = TestBuild.Ok(spec);
        var quest = r.Mod.EnumerateMajorRecords<IQuestGetter>().Single(q => q.EditorID == "MF_IdentityDefaultQuest");
        Assert.True(quest.Flags.HasFlag(Quest.Flag.StartGameEnabled));

        var entry = quest.VirtualMachineAdapter!.Scripts.Single(e => e.Name == "MFIdentityDefault");
        var factions = (IScriptObjectListPropertyGetter)entry.Properties.Single(p => p.Name == "Factions");
        var advFk = r.Mod.EnumerateMajorRecords<IFactionGetter>().Single(f => f.EditorID == "MF_FactAdventurer").FormKey;
        var palFk = r.Mod.EnumerateMajorRecords<IFactionGetter>().Single(f => f.EditorID == "MF_FactPaladin").FormKey;
        Assert.Equal(advFk, Assert.Single(factions.Objects).Object.FormKey);   // only the default identity's faction
        Assert.DoesNotContain(factions.Objects, o => o.Object.FormKey == palFk);

        var grants = (IScriptObjectListPropertyGetter)entry.Properties.Single(p => p.Name == "Grants");
        Assert.Equal(FormKey.Factory("05AD5C:Skyrim.esm"), Assert.Single(grants.Objects).Object.FormKey);
    }

    [Fact]
    public void No_default_identity_builds_no_granter_quest()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Identities = { new IdentitySpec { Id = "Paladin", Faction = "MF_FactPaladin", Priority = 30 } },
        };
        var r = TestBuild.Ok(spec);
        Assert.DoesNotContain(r.Mod.EnumerateMajorRecords<IQuestGetter>(), q => q.EditorID == "MF_IdentityDefaultQuest");
    }

    [Fact]
    public void ActiveWhen_narrows_an_identity_gate_and_defaults_to_running_on_the_player()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Npcs = { new NpcSpec { EditorId = "NPC", Name = "Guard", Race = "Skyrim.esm:0x013746" } },
            Identities =
            {
                new IdentitySpec
                {
                    Id = "Paladin", Faction = "MF_FactPaladin", Priority = 30,
                    ActiveWhen = { new ConditionSpec { Function = "WornHasKeyword", Param = "Skyrim.esm:0x06BBD2", Comparison = "==", Value = 1 } },
                },
            },
            Dialogue =
            {
                new DialogueSpec { EditorId = "Hail", QuestEditorId = "Q", SpeakerNpcEditorId = "NPC", Hello = true,
                    Responses = { "Well met." }, PrimaryIdentity = "Paladin" },
            },
        };
        var r = TestBuild.Ok(spec);
        var info = r.Mod.EnumerateMajorRecords<IDialogResponsesGetter>().Single(i => i.Conditions.Count > 0
            && i.Conditions.Any(c => c.Data is IGetInFactionConditionDataGetter));
        // GetInFaction(Paladin)>=1 AND the activeWhen WornHasKeyword, run on the player (ref 0x14).
        Assert.Contains(info.Conditions, c => c.Data is IWornHasKeywordConditionDataGetter);
        var worn = info.Conditions.First(c => c.Data is IWornHasKeywordConditionDataGetter);
        Assert.Equal(Condition.RunOnType.Reference, worn.Data.RunOnType);
        Assert.Equal(FormKey.Factory("000014:Skyrim.esm"), worn.Data.Reference.FormKey);
    }

    [Fact]
    public void ActiveWhen_does_not_taint_the_higher_priority_exclusion_of_a_lower_primaryIdentity()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Quests = { new QuestSpec { EditorId = "Q", Name = "Q" } },
            Npcs = { new NpcSpec { EditorId = "NPC", Name = "Guard", Race = "Skyrim.esm:0x013746" } },
            Identities =
            {
                new IdentitySpec { Id = "Paladin", Faction = "MF_FactPaladin", Priority = 30,
                    ActiveWhen = { new ConditionSpec { Function = "WornHasKeyword", Param = "Skyrim.esm:0x06BBD2", Comparison = "==", Value = 1 } } },
                new IdentitySpec { Id = "Adventurer", Faction = "MF_FactAdventurer", Priority = 0, Default = true },
            },
            Dialogue =
            {
                new DialogueSpec { EditorId = "HailAdv", QuestEditorId = "Q", SpeakerNpcEditorId = "NPC", Hello = true,
                    Responses = { "Safe travels." }, PrimaryIdentity = "Adventurer" },
            },
        };
        var r = TestBuild.Ok(spec);
        var info = r.Mod.EnumerateMajorRecords<IDialogResponsesGetter>().Single(i =>
            i.Conditions.Any(c => c.Data is IGetInFactionConditionDataGetter));
        // The Adventurer greeting resolves primary via the controller global (GetGlobalValue==AdvCode), NOT
        // by re-evaluating the higher Paladin's activeWhen — no WornHasKeyword leaks into a LOWER greeting.
        Assert.DoesNotContain(info.Conditions, c => c.Data is IWornHasKeywordConditionDataGetter);
        Assert.Single(info.Conditions.Where(c => c.Data is IGetInFactionConditionDataGetter)); // own held only
        Assert.Single(info.Conditions.Where(c => c.Data is IGetGlobalValueConditionDataGetter)); // primary == AdvCode
    }

    [Fact]
    public void Default_identity_granter_omits_the_Grants_list_when_no_default_grants()
    {
        var spec = new ModSpec
        {
            PluginName = "Test.esp",
            Identities = { new IdentitySpec { Id = "Adventurer", Faction = "MF_FactAdventurer", Default = true } },
        };
        var r = TestBuild.Ok(spec);
        var quest = r.Mod.EnumerateMajorRecords<IQuestGetter>().Single(q => q.EditorID == "MF_IdentityDefaultQuest");
        var entry = quest.VirtualMachineAdapter!.Scripts.Single(e => e.Name == "MFIdentityDefault");
        Assert.Contains(entry.Properties, p => p.Name == "Factions");
        Assert.DoesNotContain(entry.Properties, p => p.Name == "Grants");   // no grants → no empty list property
    }
}
