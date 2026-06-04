using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Structural tests for the Story Manager probe (StoryManagerProbe.BuildProbe). These pin the SM
// record graph — quest event/alias and the PNAM parent linkage (branch→vanilla SMEN, qnode→branch).
// In-game SM cast-selection is out of scope (no Skyrim.esm in CI); these verify STRUCTURE only.
public class StoryManagerProbeTests
{
    // Stand-in for the vanilla Skyrim.esm Kill Actor SMEN FormKey (the real one is discovered by the
    // smtree CLI command at runtime). Any Skyrim.esm-rooted FormKey works for the structural assertions.
    private static readonly FormKey FakeKillRoot =
        new(ModKey.FromNameAndExtension("Skyrim.esm"), 0x0ABCDE);

    [Fact]
    public void Quest_has_event_and_fromevent_alias()
    {
        var mod = StoryManagerProbe.BuildProbe(FakeKillRoot);
        var q = Assert.Single(mod.Quests);
        Assert.NotNull(q.Event);
        // Mutagen 0.53.1 has no standalone StartGameEnabled bool; it is a Quest.Flag — assert it's clear.
        Assert.False(q.Flags.HasFlag(Quest.Flag.StartGameEnabled));
        var alias = Assert.Single(q.Aliases);
        Assert.Equal("Victim", alias.Name);
        Assert.NotNull(alias.FindMatchingRefFromEvent);
        Assert.Contains(q.Stages, s => s.Index == 10);
    }

    [Fact]
    public void Branch_parents_vanilla_root_and_questnode_parents_branch()
    {
        var mod = StoryManagerProbe.BuildProbe(FakeKillRoot);
        var branch = Assert.Single(mod.StoryManagerBranchNodes);
        var qnode = Assert.Single(mod.StoryManagerQuestNodes);
        Assert.Empty(mod.StoryManagerEventNodes);            // additive: no SMEN of our own
        Assert.Equal(FakeKillRoot, branch.Parent.FormKey);
        Assert.Equal(branch.FormKey, qnode.Parent.FormKey);
        var entry = Assert.Single(qnode.Quests);
        Assert.Equal(Assert.Single(mod.Quests).FormKey, entry.Quest.FormKey);
    }
}
