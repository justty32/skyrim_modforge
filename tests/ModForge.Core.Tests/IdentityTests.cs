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
}
