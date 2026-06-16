using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Tests;

// Build + validate for PlacementSpec fields: Scale, InitiallyDisabled, EnableParent, Lock,
// Ownership, Count. All master-free (no Skyrim.esm).
public class PlacementSpecFieldsTests
{
    // --- helpers -----------------------------------------------------------------------

    private static ModSpec BaseSpec()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Statics.Add(new StaticSpec { EditorId = "Obj", Model = @"Clutter\Box.nif" });
        return spec;
    }

    private static IPlacedObjectGetter Object(BuildResult r, string ed) =>
        r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(o => o.EditorID == ed);

    private static IPlacedNpcGetter Npc(BuildResult r, string ed) =>
        r.Mod.EnumerateMajorRecords<IPlacedNpcGetter>().Single(o => o.EditorID == ed);

    // --- Scale -------------------------------------------------------------------------

    [Fact]
    public void Scale_NonDefault_IsWrittenToRecord()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "P", Base = "Obj", Cell = "Room", Scale = 2.5f });
        var r = TestBuild.Ok(spec);
        Assert.Equal(2.5f, Object(r, "P").Scale);
    }

    [Fact]
    public void Scale_DefaultOne_IsNotWritten()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "P", Base = "Obj", Cell = "Room", Scale = 1f });
        var r = TestBuild.Ok(spec);
        // Scale=1.0 (the default) should produce null XSCL (omitted from record).
        Assert.Null(Object(r, "P").Scale);
    }

    // --- InitiallyDisabled -------------------------------------------------------------

    [Fact]
    public void InitiallyDisabled_SetsHeaderFlag()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "P", Base = "Obj", Cell = "Room", InitiallyDisabled = true });
        var r = TestBuild.Ok(spec);
        Assert.True((Object(r, "P").MajorRecordFlagsRaw & 0x800) != 0, "InitiallyDisabled flag 0x800 not set");
    }

    [Fact]
    public void InitiallyDisabled_False_DoesNotSetFlag()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "P", Base = "Obj", Cell = "Room", InitiallyDisabled = false });
        var r = TestBuild.Ok(spec);
        Assert.True((Object(r, "P").MajorRecordFlagsRaw & 0x800) == 0, "InitiallyDisabled flag 0x800 unexpectedly set");
    }

    // --- EnableParent ------------------------------------------------------------------

    [Fact]
    public void EnableParent_SetEnable_WiresReference()
    {
        var spec = BaseSpec();
        // Marker acts as the enable parent
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Marker", Base = "Obj", Cell = "Room",
            Position = new Vec3 { X = 0, Y = 100, Z = 0 },
        });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Child", Base = "Obj", Cell = "Room",
            InitiallyDisabled = true,
            EnableParent = new EnableParentSpec { Ref = "Marker", Flag = "SetEnable" },
        });
        var r = TestBuild.Ok(spec);
        var child = Object(r, "Child");
        var marker = Object(r, "Marker");
        Assert.NotNull(child.EnableParent);
        Assert.Equal(marker.FormKey, child.EnableParent!.Reference.FormKey);
        // SetEnable = no flag
        Assert.True((child.EnableParent.Flags & EnableParent.Flag.SetEnableStateToOppositeOfParent) == 0);
    }

    [Fact]
    public void EnableParent_SetDisable_SetsOppositeFlag()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "Ctrl", Base = "Obj", Cell = "Room" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Child", Base = "Obj", Cell = "Room",
            EnableParent = new EnableParentSpec { Ref = "Ctrl", Flag = "SetDisable" },
        });
        var r = TestBuild.Ok(spec);
        var child = Object(r, "Child");
        Assert.NotNull(child.EnableParent);
        Assert.True((child.EnableParent!.Flags & EnableParent.Flag.SetEnableStateToOppositeOfParent) != 0);
    }

    [Fact]
    public void EnableParent_PopIn_SetsPopInFlag()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "Ctrl", Base = "Obj", Cell = "Room" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Child", Base = "Obj", Cell = "Room",
            EnableParent = new EnableParentSpec { Ref = "Ctrl", Flag = "PopIn" },
        });
        var r = TestBuild.Ok(spec);
        var child = Object(r, "Child");
        Assert.NotNull(child.EnableParent);
        Assert.True((child.EnableParent!.Flags & EnableParent.Flag.PopIn) != 0);
    }

    [Fact]
    public void EnableParent_UnresolvedRef_Warns_NoXesp()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Child", Base = "Obj", Cell = "Room",
            EnableParent = new EnableParentSpec { Ref = "NonExistent", Flag = "SetEnable" },
        });
        var r = TestBuild.Raw(spec);
        Assert.Contains(r.Warnings, w => w.Contains("Child") && w.Contains("enableParent") && w.Contains("unresolved"));
        Assert.Null(Object(r, "Child").EnableParent);
    }

    // --- Lock --------------------------------------------------------------------------

    [Theory]
    [InlineData("novice",     1)]
    [InlineData("apprentice", 25)]
    [InlineData("adept",      50)]
    [InlineData("expert",     75)]
    [InlineData("master",     100)]
    [InlineData("inaccessible", 255)]
    [InlineData("50",         50)]
    public void Lock_Level_IsWrittenCorrectly(string levelName, int expectedRaw)
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Door", Base = "Obj", Cell = "Room",
            Lock = new LockSpec { Level = levelName },
        });
        var r = TestBuild.Ok(spec);
        var rec = Object(r, "Door");
        Assert.NotNull(rec.Lock);
        Assert.Equal(expectedRaw, (int)rec.Lock!.Level);
    }

    [Fact]
    public void Lock_RequiresKey_IsWritten()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Door", Base = "Obj", Cell = "Room",
            Lock = new LockSpec { Level = "requiresKey" },
        });
        var r = TestBuild.Ok(spec);
        var rec = Object(r, "Door");
        Assert.NotNull(rec.Lock);
        Assert.Equal(LockLevel.RequiresKey, rec.Lock!.Level);
    }

    // --- Ownership ---------------------------------------------------------------------

    [Fact]
    public void Ownership_Owner_IsLinkedToNpc()
    {
        var spec = BaseSpec();
        spec.Npcs.Add(new NpcSpec { EditorId = "Bob", Name = "Bob", Race = "Skyrim.esm:0x013746" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Chest", Base = "Obj", Cell = "Room",
            Ownership = new OwnershipSpec { Owner = "Bob" },
        });
        var r = TestBuild.Ok(spec);
        var chest = Object(r, "Chest");
        // Owner FormKey should match the in-spec NPC
        var bobFk = r.Mod.EnumerateMajorRecords<INpcGetter>().Single(n => n.EditorID == "Bob").FormKey;
        Assert.False(chest.Owner.IsNull, "Owner link should not be null");
        Assert.Equal(bobFk, chest.Owner.FormKey);
    }

    [Fact]
    public void Ownership_UnresolvedOwner_Warns_NoXown()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "Chest", Base = "Obj", Cell = "Room",
            Ownership = new OwnershipSpec { Owner = "Nobody" },
        });
        var r = TestBuild.Raw(spec);
        Assert.Contains(r.Warnings, w => w.Contains("Chest") && w.Contains("ownership") && w.Contains("unresolved"));
        Assert.True(Object(r, "Chest").Owner.IsNull, "Owner should remain null on unresolved ref");
    }

    // --- Count -------------------------------------------------------------------------

    [Fact]
    public void Count_PositiveValue_SetsItemCount()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "P", Base = "Obj", Cell = "Room", Count = 50 });
        var r = TestBuild.Ok(spec);
        Assert.Equal(50, Object(r, "P").ItemCount);
    }

    [Fact]
    public void Count_Zero_LeavesItemCountNull()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "P", Base = "Obj", Cell = "Room", Count = 0 });
        var r = TestBuild.Ok(spec);
        Assert.Null(Object(r, "P").ItemCount);
    }

    // --- Validate errors ---------------------------------------------------------------

    [Fact]
    public void Validate_Scale_Zero_IsError()
    {
        var spec = BaseSpec();
        spec.Cells.Add(new CellSpec { EditorId = "C2", Name = "C" });
        spec.Placements.Add(new PlacementSpec { EditorId = "P", Base = "Obj", Cell = "Room", Scale = 0f });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("scale") && p.Contains("0"));
    }

    [Fact]
    public void Validate_Scale_Negative_IsError()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "P", Base = "Obj", Cell = "Room", Scale = -1f });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("scale"));
    }

    [Fact]
    public void Validate_EnableParent_EmptyRef_IsError()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "P", Base = "Obj", Cell = "Room",
            EnableParent = new EnableParentSpec { Ref = "", Flag = "SetEnable" },
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("enableParent") && p.Contains("empty ref"));
    }

    [Fact]
    public void Validate_EnableParent_BadFlag_IsError()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "Ctrl", Base = "Obj", Cell = "Room" });
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "P", Base = "Obj", Cell = "Room",
            EnableParent = new EnableParentSpec { Ref = "Ctrl", Flag = "BadFlag" },
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("enableParent") && p.Contains("flag") && p.Contains("BadFlag"));
    }

    [Fact]
    public void Validate_Lock_EmptyLevel_IsError()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "P", Base = "Obj", Cell = "Room",
            Lock = new LockSpec { Level = "" },
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("lock") && p.Contains("empty level"));
    }

    [Fact]
    public void Validate_Lock_InvalidLevel_IsError()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "P", Base = "Obj", Cell = "Room",
            Lock = new LockSpec { Level = "legendary" },
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("lock") && p.Contains("level") && p.Contains("legendary"));
    }

    [Fact]
    public void Validate_Ownership_EmptyOwner_IsError()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "P", Base = "Obj", Cell = "Room",
            Ownership = new OwnershipSpec { Owner = "" },
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("ownership") && p.Contains("empty owner"));
    }

    [Fact]
    public void Validate_Ownership_NegativeRank_IsError()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec
        {
            EditorId = "P", Base = "Obj", Cell = "Room",
            Ownership = new OwnershipSpec { Owner = "Obj", Rank = -1 },  // bad: negative rank
        });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("rank") && p.Contains("0"));
    }

    [Fact]
    public void Validate_Count_Negative_IsError()
    {
        var spec = BaseSpec();
        spec.Placements.Add(new PlacementSpec { EditorId = "P", Base = "Obj", Cell = "Room", Count = -5 });
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("count") && p.Contains("0"));
    }
}
