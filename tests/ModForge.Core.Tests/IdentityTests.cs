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
}
