using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Locks the AI-package slot-filling — especially the Sleep template (It.35) and the package-list
// priority order. Template Data filling compares the template FormKey only, so this is master-free.
public class PackageTests
{
    private const string SleepTemplate    = "Skyrim.esm:0x019717";
    private const string SandboxTemplate  = "Skyrim.esm:0x01C254";
    private const string ActivateTemplate = "Skyrim.esm:0x019B2D";
    private const string EatTemplate      = "Skyrim.esm:0x019714";

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

    // --- Activate template (Skyrim.esm:0x019B2D): NPC walks to a ref and activates it ---
    private static ModSpec ActivateSpec(string target = "MF_ActivateLever", uint? num = null) => new()
    {
        Cells = { new CellSpec { EditorId = "MF_Cell", Name = "MF Cell" } },
        Packages =
        {
            new PackageSpec
            {
                EditorId = "DoActivate", Template = ActivateTemplate,
                Activate = new ActivateSpec { Target = target, NumberToActivate = num },
            },
        },
        Npcs = { new NpcSpec { EditorId = "Npc", Name = "Npc", Packages = { "DoActivate" } } },
        Placements =
        {
            new PlacementSpec { Base = "Npc", Cell = "MF_Cell", Position = new Vec3() },
            new PlacementSpec
            {
                EditorId = "MF_ActivateLever", Base = "Skyrim.esm:0x000034", Cell = "MF_Cell",
                Position = new Vec3(),
            },
        },
    };

    // Slot 0 = the activate target, emitted as a SingleRef PackageTargetSpecificReference to the placed ref.
    [Fact]
    public void Activate_Target_IsSpecificReference()
    {
        var r = TestBuild.Ok(ActivateSpec());
        var lever = r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(p => p.EditorID == "MF_ActivateLever");
        var tgt = Assert.IsAssignableFrom<IPackageDataTargetGetter>(Slot(Pkg(r, "DoActivate"), 0));
        var sr = Assert.IsAssignableFrom<IPackageTargetSpecificReferenceGetter>(tgt.Target);
        Assert.Equal(lever.FormKey, sr.Reference.FormKey);
    }

    // Slot 2 = Number to Activate, defaulting to 1 and honouring an override.
    [Fact]
    public void Activate_NumberToActivate_DefaultAndOverride()
    {
        Assert.Equal(1f, Assert.IsAssignableFrom<IPackageDataIntGetter>(Slot(Pkg(TestBuild.Ok(ActivateSpec()), "DoActivate"), 2)).Data);
        Assert.Equal(3f, Assert.IsAssignableFrom<IPackageDataIntGetter>(Slot(Pkg(TestBuild.Ok(ActivateSpec(num: 3u)), "DoActivate"), 2)).Data);
    }

    // The activated in-spec ref is forced Persistent (a deferred SingleRef anchor must survive save/load).
    [Fact]
    public void Activate_TargetRef_ForcedPersistent()
    {
        var r = TestBuild.Ok(ActivateSpec());
        var cell = r.Mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(b => b.Cells).Single(c => c.EditorID == "MF_Cell");
        Assert.Contains(cell.Persistent, p => p.EditorID == "MF_ActivateLever");
    }

    // --- Eat template (Skyrim.esm:0x019714): location-based "go eat" sandbox variant ---
    private static ModSpec EatSpecMod(EatSpec? eat = null) => new()
    {
        Packages =
        {
            new PackageSpec { EditorId = "GoEat", Template = EatTemplate, Eat = eat ?? new EatSpec() },
        },
        Npcs = { new NpcSpec { EditorId = "Npc", Name = "Npc", Packages = { "GoEat" } } },
    };

    // Slot 0 = Eat Location; with no `location` ref it anchors NearSelf at the default radius 500.
    [Fact]
    public void Eat_Location_IsNearSelf_AtDefaultRadius()
    {
        var loc = Assert.IsAssignableFrom<IPackageDataLocationGetter>(Slot(Pkg(TestBuild.Ok(EatSpecMod()), "GoEat"), 0));
        var fb = Assert.IsAssignableFrom<ILocationFallbackGetter>(loc.Location.Target);
        Assert.Equal(LocationTargetRadius.LocationType.NearSelf, fb.Type);
        Assert.Equal(500u, loc.Location.Radius);
    }

    // Fixed scaffolding: slot 1 Food Criteria = Creatures, slot 5 Chair Target = SelfActorEffects (decoded values).
    [Fact]
    public void Eat_FixedScaffolding_IsEmitted()
    {
        var p = Pkg(TestBuild.Ok(EatSpecMod()), "GoEat");
        Assert.Equal(TargetObjectType.Creatures,
            Assert.IsAssignableFrom<IPackageTargetObjectTypeGetter>(Assert.IsAssignableFrom<IPackageDataTargetGetter>(Slot(p, 1)).Target).Type);
        Assert.Equal(TargetObjectType.SelfActorEffects,
            Assert.IsAssignableFrom<IPackageTargetObjectTypeGetter>(Assert.IsAssignableFrom<IPackageDataTargetGetter>(Slot(p, 5)).Target).Type);
    }

    // Named bools default (AllowSitting=true slot 29, AllowWandering=true slot 30) and honour overrides.
    [Fact]
    public void Eat_NamedBools_DefaultAndOverride()
    {
        var def = Pkg(TestBuild.Ok(EatSpecMod()), "GoEat");
        Assert.True(Assert.IsAssignableFrom<IPackageDataBoolGetter>(Slot(def, 29)).Data);   // AllowSitting default true
        Assert.True(Assert.IsAssignableFrom<IPackageDataBoolGetter>(Slot(def, 30)).Data);   // AllowWandering default true

        var over = Pkg(TestBuild.Ok(EatSpecMod(new EatSpec { AllowSitting = false, MinWanderDistance = 99f, NumFoodItems = 2u })), "GoEat");
        Assert.False(Assert.IsAssignableFrom<IPackageDataBoolGetter>(Slot(over, 29)).Data);  // AllowSitting:false
        Assert.Equal(99f, Assert.IsAssignableFrom<IPackageDataFloatGetter>(Slot(over, 35)).Data);  // MinWanderDistance
        Assert.Equal(2f, Assert.IsAssignableFrom<IPackageDataIntGetter>(Slot(over, 10)).Data);     // NumFoodItems
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
