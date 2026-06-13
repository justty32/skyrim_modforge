using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

public class XMarkerKindTests
{
    private static ISkyrimMod Build(ModSpec spec) =>
        Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp")).Mod;

    // Placed into an in-spec interior cell → fully offline (no master needed).
    [Fact]
    public void Xmarker_kind_defaults_base_and_forces_persistent()
    {
        var spec = new ModSpec();
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "MeetSpot", Kind = "xmarker", Cell = "Room",
            Position = new Vec3 { X = 10, Y = 20, Z = 30 },
        });
        var mod = Build(spec);
        var cell = mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).Single(c => c.EditorID == "Room");
        var anchor = cell.Persistent.OfType<IPlacedObjectGetter>().Single(r => r.EditorID == "MeetSpot");
        Assert.Equal(0x3Bu, anchor.Base.FormKey.ID);                 // defaulted to vanilla XMarker
        Assert.Equal("Skyrim.esm", anchor.Base.FormKey.ModKey.FileName);
    }

    [Fact]
    public void XmarkerHeading_kind_defaults_to_heading_base()
    {
        var spec = new ModSpec();
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Facing", Kind = "xmarkerHeading", Cell = "Room",
            Position = new Vec3 { X = 0, Y = 0, Z = 0 },
        });
        var mod = Build(spec);
        var cell = mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).Single(c => c.EditorID == "Room");
        var anchor = cell.Persistent.OfType<IPlacedObjectGetter>().Single(r => r.EditorID == "Facing");
        Assert.Equal(0x34u, anchor.Base.FormKey.ID);                 // XMarkerHeading
    }

    // A forced: alias can target the xmarker anchor (resolved by the deferred-forced-alias pass since the
    // placement builds after the alias pass).
    [Fact]
    public void Forced_alias_resolves_to_an_xmarker_anchor()
    {
        var spec = new ModSpec();
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Placements.Add(new PlacementSpec { EditorId = "Anchor", Kind = "xmarker", Cell = "Room", Position = new Vec3() });
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Q", Name = "Q", Type = "SideQuest",
            Stages = { new StageSpec { Index = 10, StartUpStage = true } },
            Aliases = { new QuestAliasSpec { Name = "Spot", Fill = "forced:Anchor" } },
        });
        var mod = Build(spec);
        var alias = mod.Quests.Single(q => q.EditorID == "Q").Aliases.Single();
        Assert.False(alias.ForcedReference.IsNull);
        var cell = mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).Single(c => c.EditorID == "Room");
        var anchor = cell.Persistent.OfType<IPlacedObjectGetter>().Single(r => r.EditorID == "Anchor");
        Assert.Equal(anchor.FormKey, alias.ForcedReference.FormKey);
    }
}
