using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class HazardTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    [Fact]
    public void Hazard_record_builds_scalars_flags_and_wires_spell()
    {
        var spec = new ModSpec();
        spec.Spells.Add(new SpellSpec { EditorId = "MF_Burn", Name = "Burn" });
        spec.Hazards.Add(new HazardSpec
        {
            EditorId = "MF_FireHaz", Name = "Flames", Model = "Meshes/x.nif",
            Radius = 150f, Lifetime = 5f, TargetInterval = 1f, Limit = 3,
            Spell = "MF_Burn", Flags = { "DropToGround", "AffectsPlayerOnly" },
        });
        var mod = Build(spec);
        var h = mod.Hazards.Single(x => x.EditorID == "MF_FireHaz");
        Assert.Equal(150f, h.Radius);
        Assert.Equal(5f, h.Lifetime);
        Assert.Equal(1f, h.TargetInterval);
        Assert.Equal(3u, h.Limit);
        Assert.Equal("Meshes/x.nif", h.Model!.File);
        Assert.True(h.Flags.HasFlag(Hazard.Flag.DropToGround));
        Assert.True(h.Flags.HasFlag(Hazard.Flag.AffectsPlayerOnly));
        var spell = mod.Spells.Single(s => s.EditorID == "MF_Burn");
        Assert.Equal(spell.FormKey, h.Spell.FormKey);     // wired in pass 2
    }

    [Fact]
    public void Magic_effect_spawn_hazard_associates_the_hazard()
    {
        var spec = new ModSpec();
        spec.Hazards.Add(new HazardSpec { EditorId = "MF_Haz", Model = "Meshes/x.nif", Radius = 100f });
        spec.MagicEffects.Add(new MagicEffectSpec
        {
            EditorId = "MF_DropHaz", Name = "Drop Hazard",
            Archetype = "SpawnHazard", Association = "MF_Haz",
        });
        var mod = Build(spec);
        var haz = mod.Hazards.Single(h => h.EditorID == "MF_Haz");
        var mgef = mod.MagicEffects.Single(m => m.EditorID == "MF_DropHaz");
        var arch = (MagicEffectArchetype)mgef.Archetype!;
        Assert.Equal(MagicEffectArchetype.TypeEnum.SpawnHazard, arch.Type);
        Assert.Equal(haz.FormKey, arch.Association.FormKey);
    }

    [Fact]
    public void Placement_with_a_hazard_base_makes_a_PlacedHazard()
    {
        var spec = new ModSpec();
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Hazards.Add(new HazardSpec { EditorId = "MF_Haz", Model = "Meshes/x.nif", Radius = 100f });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Trap", Base = "MF_Haz", Cell = "Room",
            Position = new Vec3 { X = 1, Y = 2, Z = 3 },
        });
        var mod = Build(spec);
        var haz = mod.Hazards.Single(h => h.EditorID == "MF_Haz");
        var cell = mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).Single(c => c.EditorID == "Room");
        var placed = cell.Temporary.Concat(cell.Persistent).OfType<IPlacedHazardGetter>().Single(r => r.EditorID == "Trap");
        Assert.Equal(haz.FormKey, placed.Hazard.FormKey);
    }
}
