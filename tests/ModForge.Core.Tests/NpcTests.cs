using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// NPC (NPC_) configuration flags. Essential/Protected gate whether an actor can be killed — needed
// e.g. for a non-lethal brawl (two NPCs that fight but don't die).
public class NpcTests
{
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
}
