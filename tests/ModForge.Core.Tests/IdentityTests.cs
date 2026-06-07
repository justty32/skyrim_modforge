using System.Linq;
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
    public void PrimaryIdentity_tag_gates_dialogue_on_held_minus_higher_priority()
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
        // Across the built INFOs: exactly two GetInFaction conditions — Merchant >= 1, and the
        // higher-priority Paladin == 0 (the exclusion that makes Merchant the *primary* greeting).
        var gif = r.Mod.EnumerateMajorRecords<IDialogResponsesGetter>()
            .SelectMany(i => i.Conditions)
            .OfType<IConditionFloatGetter>()
            .Where(c => c.Data is IGetInFactionConditionDataGetter)
            .ToList();
        Assert.Equal(2, gif.Count);
        Assert.Contains(gif, c => c.CompareOperator == CompareOperator.GreaterThanOrEqualTo && c.ComparisonValue == 1f);
        Assert.Contains(gif, c => c.CompareOperator == CompareOperator.EqualTo && c.ComparisonValue == 0f);
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
}
