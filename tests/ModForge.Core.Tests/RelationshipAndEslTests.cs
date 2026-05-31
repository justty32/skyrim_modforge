using System;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// RELA + ESL guards — both are CRASH/abort gotchas (main-menu crash; ESL over-cap write throw).
public class RelationshipAndEslTests
{
    // GOTCHA: RelationshipSpec.Child defaults to the player NPC_ base 0x000014, NOT PlayerRef 0x000007
    // (a RELA pointing at the placed ACHR 0x07 crashes at main menu). Lock the default in.
    [Fact]
    public void Relationship_DefaultChild_IsPlayerNpcBase_Not_PlayerRef()
    {
        var r = TestBuild.Ok(new ModSpec
        {
            Npcs = { new NpcSpec { EditorId = "Npc", Name = "Npc" } },
            Relationships = { new RelationshipSpec { EditorId = "Rel", Parent = "Npc" } },
        });
        var rel = r.Mod.EnumerateMajorRecords<IRelationshipGetter>().Single();
        Assert.Equal(0x000014u, rel.Child.FormKey.ID);
        Assert.NotEqual(0x000007u, rel.Child.FormKey.ID);
        Assert.Equal("Skyrim.esm", rel.Child.FormKey.ModKey.FileName);
    }

    [Fact]
    public void Esl_DrivesIsSmallMaster()
    {
        Assert.True(Generator.Build(new ModSpec { Esl = true,  MiscItems = { new MiscSpec { EditorId = "M", Name = "M" } } }, TestBuild.Key).Mod.IsSmallMaster);
        Assert.False(Generator.Build(new ModSpec { Esl = false, MiscItems = { new MiscSpec { EditorId = "M", Name = "M" } } }, TestBuild.Key).Mod.IsSmallMaster);
    }

    // GOTCHA: an ESL may hold at most 2048 records; PluginIo.Write pre-empts Mutagen's raw compaction
    // exception with an actionable message. Verify the guard fires (not a silent/raw throw at write).
    [Fact]
    public void Esl_Over2048Records_Write_Throws_Actionable()
    {
        var spec = new ModSpec { Esl = true };
        for (int i = 0; i < 2049; i++)
            spec.MiscItems.Add(new MiscSpec { EditorId = $"M{i}", Name = "M" });
        var mod = Generator.Build(spec, TestBuild.Key).Mod;

        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mf-esl-{Guid.NewGuid():N}.esp");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => PluginIo.Write(mod, path));
            Assert.Contains("2048", ex.Message);
        }
        finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    }
}
