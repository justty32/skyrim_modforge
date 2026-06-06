using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Locks the AI-package slot-filling — especially the Sleep template (It.35) and the package-list
// priority order. Template Data filling compares the template FormKey only, so this is master-free.
public class PackageTests
{
    private const string SleepTemplate   = "Skyrim.esm:0x019717";
    private const string SandboxTemplate = "Skyrim.esm:0x01C254";

    private static ModSpec RoutineSpec() => new()
    {
        Packages =
        {
            new PackageSpec
            {
                EditorId = "Night", Template = SleepTemplate,
                Schedule = new PackageScheduleSpec { Hour = 22, DurationInMinutes = 540 },
                Sleep = new SleepSpec { Radius = 1024, LockDoors = false },
            },
            new PackageSpec { EditorId = "Day", Template = SandboxTemplate },
        },
        // List order = priority: Night (scheduled) first, Day (fallback) last.
        Npcs = { new NpcSpec { EditorId = "Npc", Name = "Npc", Packages = { "Night", "Day" } } },
    };

    private static IPackageGetter Pkg(BuildResult r, string ed)
        => r.Mod.EnumerateMajorRecords<IPackageGetter>().Single(p => p.EditorID == ed);

    private static IAPackageDataGetter Slot(IPackageGetter p, sbyte i)
        => p.Data.Single(kv => kv.Key == i).Value;

    // The sleep window is the package Schedule, not a Data slot.
    [Fact]
    public void Sleep_ScheduleWindow_IsWired()
    {
        var night = Pkg(TestBuild.Ok(RoutineSpec()), "Night");
        Assert.Equal((sbyte)22, night.ScheduleHour);
        Assert.Equal(540, night.ScheduleDurationInMinutes);
    }

    // GOTCHA: our generated NPCs have no CK Editor Location, so the bed-search location must anchor on
    // NearSelf (vanilla's NearEditorLocation would silently no-op), at the authored radius.
    [Fact]
    public void Sleep_Location_IsNearSelf_AtRadius()
    {
        var night = Pkg(TestBuild.Ok(RoutineSpec()), "Night");
        var loc = Assert.IsAssignableFrom<IPackageDataLocationGetter>(Slot(night, 0));
        var fb = Assert.IsAssignableFrom<ILocationFallbackGetter>(loc.Location.Target);
        Assert.Equal(LocationTargetRadius.LocationType.NearSelf, fb.Type);
        Assert.Equal(1024u, loc.Location.Radius);
    }

    // GOTCHA: the bed-search slot 1 must be PackageTargetObjectType(TouchActorEffects) — the same enum
    // family that silently no-ops for UseMagic SPELLS, but is CORRECT here for a bed search.
    [Fact]
    public void Sleep_BedSearch_IsTouchActorEffects()
    {
        var night = Pkg(TestBuild.Ok(RoutineSpec()), "Night");
        var tgt = Assert.IsAssignableFrom<IPackageDataTargetGetter>(Slot(night, 1));
        var ot = Assert.IsAssignableFrom<IPackageTargetObjectTypeGetter>(tgt.Target);
        Assert.Equal(TargetObjectType.TouchActorEffects, ot.Type);
    }

    // lockDoors:false override is honoured (slot 15); AllowSleeping default true (slot 18).
    [Fact]
    public void Sleep_LockDoorsOverride_And_AllowSleepingDefault()
    {
        var night = Pkg(TestBuild.Ok(RoutineSpec()), "Night");
        Assert.False(Assert.IsAssignableFrom<IPackageDataBoolGetter>(Slot(night, 15)).Data);  // lockDoors:false
        Assert.True(Assert.IsAssignableFrom<IPackageDataBoolGetter>(Slot(night, 18)).Data);   // allowSleeping default
    }

    // GOTCHA: the NPC's package list is PRIORITY ORDER — Build must preserve spec order (Night, Day).
    [Fact]
    public void NpcPackageList_PreservesSpecOrder()
    {
        var r = TestBuild.Ok(RoutineSpec());
        var npc = r.Mod.EnumerateMajorRecords<INpcGetter>().Single();
        var night = Pkg(r, "Night").FormKey;
        var day = Pkg(r, "Day").FormKey;
        Assert.Equal(new[] { night, day }, npc.Packages.Select(p => p.FormKey).ToArray());
    }

    private const string SitTemplate = "Skyrim.esm:0x0A9277";   // SitTarget (UseItemAt furniture)

    // SitTarget makes an NPC go USE a furniture ref. Slot 16 is the SingleRef target (the chair/
    // furniture placement); decoded from vanilla MQ306EsbernSit. Reuses the deferred-target wiring,
    // so the target may be an in-spec placement editorId.
    private static ModSpec SitSpec() => new()
    {
        PluginName = "Test.esp",
        Cells = { new CellSpec { EditorId = "Room", Name = "Room" } },
        Placements =
        {
            // a furniture ref (vanilla CommonChair01F base) the NPC will sit on
            new PlacementSpec { EditorId = "Chair", Base = "Skyrim.esm:0x06E7A8", Cell = "Room",
                                Position = new Vec3 { X = 0, Y = 200, Z = 0 } },
            new PlacementSpec { Base = "Sitter", Cell = "Room", Kind = "npc" },
        },
        Packages =
        {
            new PackageSpec
            {
                EditorId = "GoSit", Template = SitTemplate,
                SitTarget = new SitTargetSpec { Target = "Chair", WaitTime = 30f, StopMovement = true },
            },
        },
        Npcs = { new NpcSpec { EditorId = "Sitter", Name = "Sitter", Race = "Skyrim.esm:0x013746", Packages = { "GoSit" } } },
    };

    // Slot 16 = SingleRef target → PackageTargetSpecificReference to the furniture placement.
    [Fact]
    public void SitTarget_Slot16_PointsAtFurnitureRef()
    {
        var r = TestBuild.Ok(SitSpec());
        var chair = r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(o => o.EditorID == "Chair");
        var sit = Pkg(r, "GoSit");
        var tgt = Assert.IsAssignableFrom<IPackageDataTargetGetter>(Slot(sit, 16));
        Assert.Equal(PackageDataTarget.Types.SingleRef, tgt.Type);
        var spec = Assert.IsAssignableFrom<IPackageTargetSpecificReferenceGetter>(tgt.Target);
        Assert.Equal(chair.FormKey, spec.Reference.FormKey);

        // The chair is a package SingleRef target, so it must be forced PERSISTENT (else the engine can
        // drop the anchor across save/load and the NPC has nothing to sit on).
        var room = r.Mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(s => s.Cells).Single(c => c.EditorID == "Room");
        Assert.Contains(room.Persistent, p => p.EditorID == "Chair");
    }

    // Slot 3 (Wait Time, float) and slot 4 (Stop Movement Flag, bool) carry the author's values.
    [Fact]
    public void SitTarget_WaitTime_And_StopMovement_AreWired()
    {
        var sit = Pkg(TestBuild.Ok(SitSpec()), "GoSit");
        Assert.Equal(30f, Assert.IsAssignableFrom<IPackageDataFloatGetter>(Slot(sit, 3)).Data);
        Assert.True(Assert.IsAssignableFrom<IPackageDataBoolGetter>(Slot(sit, 4)).Data);
    }

    // An unsupported procedure template emits a warning (and no Data overrides), not a hard failure.
    [Fact]
    public void UnsupportedTemplate_Warns()
    {
        var r = TestBuild.Raw(new ModSpec
        {
            Packages = { new PackageSpec { EditorId = "P", Template = "Skyrim.esm:0x01C338" } }, // UseWeapon — unsupported
        });
        Assert.Contains(r.Warnings, w => w.Contains("not yet supported"));
    }
}
