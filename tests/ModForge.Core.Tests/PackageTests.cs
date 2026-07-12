using System;
using System.Linq;
using Mutagen.Bethesda.Plugins;
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

    // --- eat.location / useMagic.location / useMagic.target: the three slots that were resolved EAGERLY ---
    //
    // BuildPackageData runs BEFORE BuildPlacements and BuildReferences, so at that moment the ref table
    // holds base records only. These three slots used to resolve there — which meant an in-file placement
    // editorId or a references[] label could never be seen: they missed and fell back to NearSelf/Self
    // ("! package 'X' eat location 'sofia's chair' unresolved"), while the other nine ref slots had been on
    // the deferred wires all along. They are deferred now; these tests pin all three ways in.

    private const string UseMagicTemplate = "Skyrim.esm:0x0504F5";
    private const string Candlelight      = "Skyrim.esm:0x043324";   // SPEL (a base form — still resolved eagerly)
    private const string ChairBase        = "Skyrim.esm:0x0B9C04";   // CommonChair02 (FURN)
    private const string VanillaRef       = "Skyrim.esm:0x0D1991";   // an existing placed ref
    private const string Player           = "Skyrim.esm:0x000014";

    // A room + a chair placement, optionally labelled by references[]. `slot` fills the package(s) under test.
    private static ModSpec DeferredSlotSpec(Action<ModSpec> packages, bool label = true)
    {
        var s = new ModSpec
        {
            PluginName = "Test.esp",
            Cells = { new CellSpec { EditorId = "Room", Name = "Room" } },
            Placements =
            {
                new PlacementSpec { EditorId = "MF_Chair", Base = ChairBase, Cell = "Room",
                                    Position = new Vec3 { X = 10, Y = 20, Z = 30 } },
            },
            Npcs = { new NpcSpec { EditorId = "Npc", Name = "Npc", Race = "Skyrim.esm:0x013746" } },
        };
        if (label) s.References.Add(new ReferenceSpec { Ref = "MF_Chair", Label = "sofia's chair" });
        packages(s);
        foreach (var p in s.Packages) s.Npcs[0].Packages.Add(p.EditorId);
        return s;
    }

    private static FormKey Chair(BuildResult r)
        => r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(o => o.EditorID == "MF_Chair").FormKey;

    private static FormKey LocLink(IPackageGetter p, sbyte slot)
        => Assert.IsAssignableFrom<ILocationTargetGetter>(
               Assert.IsAssignableFrom<IPackageDataLocationGetter>(Slot(p, slot)).Location!.Target).Link.FormKey;

    private static FormKey SingleRefLink(IPackageGetter p, sbyte slot)
        => Assert.IsAssignableFrom<IPackageTargetSpecificReferenceGetter>(
               Assert.IsAssignableFrom<IPackageDataTargetGetter>(Slot(p, slot)).Target).Reference.FormKey;

    // (1) a references[] LABEL now resolves in all three slots — this is what silently fell back before.
    [Theory]
    [InlineData("sofia's chair")]   // a references[] label   (BuildReferences runs after BuildPackageData)
    [InlineData("MF_Chair")]        // an in-file placement editorId (BuildPlacements likewise)
    public void EatLocation_And_UseMagicLocationAndTarget_ResolveALabelOrInFilePlacement(string reff)
    {
        var r = TestBuild.Ok(DeferredSlotSpec(s =>
        {
            s.Packages.Add(new PackageSpec { EditorId = "GoEat", Template = EatTemplate,
                Eat = new EatSpec { Location = reff, Radius = 700 } });
            s.Packages.Add(new PackageSpec { EditorId = "Cast", Template = UseMagicTemplate,
                UseMagic = new UseMagicSpec { Location = reff, Radius = 256, Spell = Candlelight, Target = reff } });
        }));
        var chair = Chair(r);

        Assert.Equal(chair, LocLink(Pkg(r, "GoEat"), 0));        // eat.location      → LocationTarget(chair)
        Assert.Equal(chair, LocLink(Pkg(r, "Cast"), 2));         // useMagic.location → LocationTarget(chair)
        Assert.Equal(chair, SingleRefLink(Pkg(r, "Cast"), 4));   // useMagic.target   → SpecificReference(chair)

        // …and, being a package anchor now, the chair is forced persistent (else the engine may drop it).
        var room = r.Mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(b => b.Cells).Single(c => c.EditorID == "Room");
        Assert.Contains(room.Persistent, p => p.EditorID == "MF_Chair");
    }

    // A LABEL in eat.location / useMagic.location is an AREA anchor, not a lock-on — the guardrail note
    // (ReferenceSlotKindTests) must fire for these two exactly as it does for sandbox/sleep/travel/escort.
    // It could not be trusted before: the slot didn't resolve at all.
    [Fact]
    public void LabelInEatOrUseMagicLocation_StillNotesTheAreaAnchorTrap()
    {
        var r = TestBuild.Ok(DeferredSlotSpec(s =>
        {
            s.Packages.Add(new PackageSpec { EditorId = "GoEat", Template = EatTemplate,
                Eat = new EatSpec { Location = "sofia's chair", Radius = 700 } });
            s.Packages.Add(new PackageSpec { EditorId = "Cast", Template = UseMagicTemplate,
                UseMagic = new UseMagicSpec { Location = "sofia's chair", Spell = Candlelight, Target = "sofia's chair" } });
        }));
        Assert.Equal(2, r.Notes.Count);                                        // the two LOCATION slots…
        Assert.Contains(r.Notes, n => n.Contains("eat.location") && n.Contains("radius 700"));
        Assert.Contains(r.Notes, n => n.Contains("useMagic.location"));
        Assert.DoesNotContain(r.Notes, n => n.Contains("'Cast' useMagic.target"));   // …never the SingleRef one
    }

    // (2) REGRESSION: a vanilla FormID in these slots worked before the deferral and must be untouched by
    // it — same slot, same payload, same FormKey, no warning. (The three example/scratch specs that fill
    // them with vanilla refs also byte-compare md5-identical across the change.)
    [Fact]
    public void VanillaFormIdInAllThreeSlots_IsBitForBitTheOldBehaviour()
    {
        var r = TestBuild.Ok(DeferredSlotSpec(s =>
        {
            s.Packages.Add(new PackageSpec { EditorId = "GoEat", Template = EatTemplate,
                Eat = new EatSpec { Location = VanillaRef, Radius = 700 } });
            s.Packages.Add(new PackageSpec { EditorId = "Cast", Template = UseMagicTemplate,
                UseMagic = new UseMagicSpec { Location = VanillaRef, Radius = 256, Spell = Candlelight, Target = Player } });
        }, label: false));

        var vanilla = FormKey.Factory("0D1991:Skyrim.esm");
        var eat = Pkg(r, "GoEat");
        Assert.Equal(vanilla, LocLink(eat, 0));
        Assert.Equal(700u, Assert.IsAssignableFrom<IPackageDataLocationGetter>(Slot(eat, 0)).Location!.Radius);

        var cast = Pkg(r, "Cast");
        Assert.Equal(vanilla, LocLink(cast, 2));
        Assert.Equal(256u, Assert.IsAssignableFrom<IPackageDataLocationGetter>(Slot(cast, 2)).Location!.Radius);
        Assert.Equal(FormKey.Factory("000014:Skyrim.esm"), SingleRefLink(cast, 4));
        Assert.Empty(r.Notes);
    }

    // …and an empty useMagic.target still emits PackageTargetSelf in slot 4 (the self-cast default: an
    // EMPTY slot 4 means the engine casts at nothing). Deferring the ref must not lose the default.
    [Fact]
    public void EmptyUseMagicTarget_IsStillPackageTargetSelf()
    {
        var r = TestBuild.Ok(DeferredSlotSpec(s =>
            s.Packages.Add(new PackageSpec { EditorId = "Cast", Template = UseMagicTemplate,
                UseMagic = new UseMagicSpec { Spell = Candlelight } }), label: false));
        var tgt = Assert.IsAssignableFrom<IPackageDataTargetGetter>(Slot(Pkg(r, "Cast"), 4));
        Assert.IsAssignableFrom<IPackageTargetSelfGetter>(tgt.Target);
    }

    // (3) A ref that really IS wrong must still WARN — the fix must not silence the diagnostic by making
    // everything "resolve later". Unresolved location → NearSelf; unresolved useMagic target → Self.
    [Fact]
    public void RefThatResolvesToNothing_StillWarns_AndFallsBack()
    {
        var r = TestBuild.Raw(DeferredSlotSpec(s =>
        {
            s.Packages.Add(new PackageSpec { EditorId = "GoEat", Template = EatTemplate,
                Eat = new EatSpec { Location = "NoSuchChair" } });
            s.Packages.Add(new PackageSpec { EditorId = "Cast", Template = UseMagicTemplate,
                UseMagic = new UseMagicSpec { Location = "NoSuchChair", Spell = Candlelight, Target = "NoSuchChair" } });
        }, label: false));

        Assert.Contains(r.Warnings, w => w.Contains("package 'GoEat' eat location 'NoSuchChair' unresolved")
                                      && w.Contains("NearSelf"));
        Assert.Contains(r.Warnings, w => w.Contains("package 'Cast' location 'NoSuchChair' unresolved")
                                      && w.Contains("NearSelf"));
        Assert.Contains(r.Warnings, w => w.Contains("package 'Cast' Target 'NoSuchChair' unresolved")
                                      && w.Contains("PackageTargetSelf"));

        var eatLoc = Assert.IsAssignableFrom<IPackageDataLocationGetter>(Slot(Pkg(r, "GoEat"), 0));
        Assert.Equal(LocationTargetRadius.LocationType.NearSelf,
            Assert.IsAssignableFrom<ILocationFallbackGetter>(eatLoc.Location!.Target).Type);
        Assert.IsAssignableFrom<IPackageTargetSelfGetter>(
            Assert.IsAssignableFrom<IPackageDataTargetGetter>(Slot(Pkg(r, "Cast"), 4)).Target);
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
