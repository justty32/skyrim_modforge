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
