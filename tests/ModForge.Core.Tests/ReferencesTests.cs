using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Idea #24 referrer (`sc ref`) — `references[]` NAMES an existing placed ref so the rest of the spec
// can point at it by `label`. Two target classes:
//   (B) in-file: ref = a placements[] editorId → forced persistent, label binds to it. Offline.
//   (A) external: ref = "<master>:0xFORMID" → label binds to it; build warns when the vanilla ref is
//       temporary; `anchor` authors our own persistent stand-in. Needs the master link cache.
public class ReferencesTests
{
    private const string SandboxTemplate = "Skyrim.esm:0x01C254";
    private const string CommonChair = "Skyrim.esm:0x0B9C04";   // CommonChair02 (FURN)
    private const string Label = "sofia's chair";

    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    // A room with a chair the in-game editor placed (`sc pl`), a referrer naming it, and an NPC whose
    // sandbox package is anchored on that label — the whole point of the primitive.
    private static ModSpec SofiaSpec()
    {
        var s = new ModSpec { PluginName = "MFRefs.esp" };
        s.Cells.Add(new CellSpec { EditorId = "MFRefRoom", Name = "Ref Room" });
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "MFRef_Chair", Base = CommonChair, Cell = "MFRefRoom",
            Position = new Vec3 { X = 10f, Y = 20f, Z = 30f },
            Rotation = new Vec3 { X = 0f, Y = 0f, Z = 90f },
        });
        s.References.Add(new ReferenceSpec { Ref = "MFRef_Chair", Label = Label, Base = CommonChair });
        s.Npcs.Add(new NpcSpec { EditorId = "MFRefSofia", Name = "Sofia", Race = "Skyrim.esm:0x013746" });
        s.Packages.Add(new PackageSpec
        {
            EditorId = "MFRefSitPkg", Template = SandboxTemplate,
            Sandbox = new SandboxSpec { Location = Label, Radius = 128, AllowSitting = true },
        });
        s.Npcs[0].Packages.Add("MFRefSitPkg");
        return s;
    }

    // --- (B) in-file dependency: the clean path -------------------------------------------------

    [Fact]
    public void InFileRef_ForcesTheTargetPlacementPersistent()
    {
        var mod = TestBuild.Ok(SofiaSpec()).Mod;
        var chair = mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(sb => sb.Cells)
            .SelectMany(c => c.Persistent)
            .OfType<IPlacedObjectGetter>()
            .Single(p => p.EditorID == "MFRef_Chair");
        // A referenced anchor lives in the cell's PERSISTENT group AND carries the 0x400 record flag —
        // that is what "an alias/package can target this ref" needs.
        Assert.True((chair.MajorRecordFlagsRaw & 0x400) != 0, "a referenced ref must carry the persistent flag");
    }

    [Fact]
    public void InFileRef_LabelResolvesAsARefEverywhere_SandboxLocationPointsAtTheChair()
    {
        var mod = TestBuild.Ok(SofiaSpec()).Mod;
        var chair = mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(p => p.EditorID == "MFRef_Chair");

        var pkg = mod.Packages.Single(p => p.EditorID == "MFRefSitPkg");
        var loc = Assert.IsAssignableFrom<IPackageDataLocationGetter>(pkg.Data[0]);   // sandbox slot 0 = Location
        var target = Assert.IsAssignableFrom<ILocationTargetGetter>(loc.Location!.Target);
        Assert.Equal(chair.FormKey, target.Link.FormKey);
        Assert.Equal(128u, loc.Location!.Radius);
    }

    [Fact]
    public void InFileRef_EmitsNoExtraRecords()
    {
        // references[] CONSUMES an existing ref — it must not author one (the chair placement is the
        // only placed record in the spec).
        var mod = TestBuild.Ok(SofiaSpec()).Mod;
        Assert.Single(mod.EnumerateMajorRecords<IPlacedObjectGetter>());
    }

    [Fact]
    public void NoReferences_NothingChanges()
    {
        // Behaviour-unchanged guard: the same spec minus references[] must build clean, and the chair
        // must then be TEMPORARY (nothing forced it persistent).
        var s = SofiaSpec();
        s.References.Clear();
        s.Packages[0].Sandbox.Location = "MFRef_Chair";
        var mod = TestBuild.Ok(s).Mod;
        var chair = mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(p => p.EditorID == "MFRef_Chair");
        Assert.True((chair.MajorRecordFlagsRaw & 0x400) == 0);
    }

    // --- validate --------------------------------------------------------------------------------

    [Fact]
    public void Validate_CleanSpec_NoProblems() => Assert.Empty(Validate(SofiaSpec()));

    [Fact]
    public void Validate_EmptyRefAndLabel_AreProblems()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.References.Add(new ReferenceSpec());
        var p = Validate(s);
        Assert.Contains(p, x => x.Contains("reference[0]") && x.Contains("empty label"));
        Assert.Contains(p, x => x.Contains("reference[0]") && x.Contains("empty ref"));
    }

    [Fact]
    public void Validate_RefIsNotAPlacementEditorId_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.References.Add(new ReferenceSpec { Ref = "NoSuchThing", Label = "x" });
        Assert.Contains(Validate(s), x => x.Contains("NoSuchThing") && x.Contains("placements[] editorId"));
    }

    [Fact]
    public void Validate_DuplicateLabel_IsAProblem()
    {
        var s = SofiaSpec();
        s.References.Add(new ReferenceSpec { Ref = "MFRef_Chair", Label = Label });
        Assert.Contains(Validate(s), x => x.Contains("collides") && x.Contains(Label));
    }

    [Fact]
    public void Validate_LabelCollidingWithAnEditorId_IsAProblem()
    {
        var s = SofiaSpec();
        s.References[0].Label = "MFRef_Chair";   // a label IS a name refs resolve by → must be unique
        Assert.Contains(Validate(s), x => x.Contains("collides") && x.Contains("MFRef_Chair"));
    }

    [Fact]
    public void Validate_LabelShapedLikeAnExternalRef_IsAProblem()
    {
        var s = SofiaSpec();
        s.References[0].Label = "Skyrim.esm:0x00ABCD";
        Assert.Contains(Validate(s), x => x.Contains("label") && x.Contains("plain name"));
    }

    [Fact]
    public void Validate_UnknownAnchor_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.References.Add(new ReferenceSpec { Ref = "Skyrim.esm:0x0D1991", Label = "hoe", Anchor = "teleport" });
        Assert.Contains(Validate(s), x => x.Contains("unknown anchor"));
    }

    [Fact]
    public void Validate_AnchorOnAnInFileRef_IsAProblem()
    {
        var s = SofiaSpec();
        s.References[0].Anchor = "marker";
        Assert.Contains(Validate(s), x => x.Contains("meaningless on an in-file ref"));
    }

    [Fact]
    public void Validate_AnchorWithoutAPlace_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.References.Add(new ReferenceSpec { Ref = "Skyrim.esm:0x0D1991", Label = "hoe", Anchor = "marker" });
        Assert.Contains(Validate(s), x => x.Contains("needs a cell or worldspace"));
    }

    [Fact]
    public void Validate_ReplaceWithoutABase_IsAProblem()
    {
        var s = new ModSpec { PluginName = "M.esp" };
        s.References.Add(new ReferenceSpec
        {
            Ref = "Skyrim.esm:0x0D1991", Label = "hoe", Anchor = "replace",
            Worldspace = "Skyrim.esm:0x00003C", Position = new Vec3 { X = 19265.9f, Y = -12816.5f, Z = -4539f },
        });
        Assert.Contains(Validate(s), x => x.Contains("replace") && x.Contains("`base`"));
    }

    // --- (A) external target: the persistent trap + the anchor fallback ---------------------------

    [Fact]
    public void ExternalRef_WithoutTheLinkCache_StillBindsTheLabel()
    {
        // No master cache in this test build → build can't tell whether the ref is persistent; it warns,
        // but the label still resolves (a package pointing at it gets the vanilla FormKey).
        var s = new ModSpec { PluginName = "MFRefsX.esp" };
        s.References.Add(new ReferenceSpec { Ref = "Skyrim.esm:0x0D1991", Label = "the hoe" });
        s.Packages.Add(new PackageSpec
        {
            EditorId = "P", Template = SandboxTemplate,
            Sandbox = new SandboxSpec { Location = "the hoe", Radius = 100 },
        });
        var r = Generator.Build(s, ModKey.FromNameAndExtension("MFRefsX.esp"),
            new BuildOptions { SkyrimDataPath = "/nonexistent" });
        var pkg = r.Mod.Packages.Single(p => p.EditorID == "P");
        var loc = Assert.IsAssignableFrom<IPackageDataLocationGetter>(pkg.Data[0]);   // sandbox slot 0 = Location
        var target = Assert.IsAssignableFrom<ILocationTargetGetter>(loc.Location!.Target);
        Assert.Equal(FormKey.Factory("0D1991:Skyrim.esm"), target.Link.FormKey);
        Assert.Contains(r.Warnings, w => w.Contains("the hoe") && w.Contains("persistent"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void ExternalTemporaryRef_WarnsAboutPersistence()
    {
        // Skulvar's hoe at the Whiterun stables — a vanilla TEMPORARY scenery ref (the guinea pig the
        // removals/overrides tests use). Naming it is legal, but it's a poor alias/package target.
        var s = new ModSpec { PluginName = "MFRefsT.esp" };
        s.References.Add(new ReferenceSpec { Ref = "Skyrim.esm:0x0D1991", Label = "the hoe" });
        var r = Generator.Build(s, ModKey.FromNameAndExtension("MFRefsT.esp"));
        Assert.Contains(r.Warnings, w => w.Contains("TEMPORARY") && w.Contains("the hoe"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void ExternalRef_AnchorMarker_AuthorsAPersistentXMarkerHeading_AndBindsTheLabelToIt()
    {
        var s = new ModSpec { PluginName = "MFRefsM.esp" };
        s.References.Add(new ReferenceSpec
        {
            Ref = "Skyrim.esm:0x0D1991", Label = "hoe spot", Anchor = "marker",
            Worldspace = "Skyrim.esm:0x00003C",
            Position = new Vec3 { X = 19265.9f, Y = -12816.5f, Z = -4539f },
        });
        var mod = Generator.Build(s, ModKey.FromNameAndExtension("MFRefsM.esp")).Mod;

        var marker = mod.EnumerateMajorRecords<IPlacedObjectGetter>()
            .Single(p => p.EditorID != null && p.EditorID.StartsWith("MFRef_"));
        Assert.Equal(FormKey.Factory("000034:Skyrim.esm"), marker.Base.FormKey);   // XMarkerHeading
        Assert.True((marker.MajorRecordFlagsRaw & 0x400) != 0);
        // marker mode does NOT touch the vanilla ref (it stands beside it, it doesn't replace it).
        Assert.DoesNotContain(mod.EnumerateMajorRecords<IPlacedObjectGetter>(),
            p => p.FormKey == FormKey.Factory("0D1991:Skyrim.esm"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void ExternalRef_AnchorReplace_AuthorsOurOwnPersistentCopy_AndRemovesTheOriginal()
    {
        var s = new ModSpec { PluginName = "MFRefsR.esp" };
        s.References.Add(new ReferenceSpec
        {
            Ref = "Skyrim.esm:0x0D1991", Label = "sofia's hoe", Anchor = "replace",
            Base = "Skyrim.esm:0x02F2F4",   // Hoe01 (the STAT the vanilla ref is built on) — spec wins over the master
            Worldspace = "Skyrim.esm:0x00003C",
            Position = new Vec3 { X = 19265.9f, Y = -12816.5f, Z = -4539f },
            Rotation = new Vec3 { X = 0f, Y = 0f, Z = 90f },
        });
        var mod = Generator.Build(s, ModKey.FromNameAndExtension("MFRefsR.esp")).Mod;

        var copy = mod.EnumerateMajorRecords<IPlacedObjectGetter>()
            .Single(p => p.EditorID != null && p.EditorID.StartsWith("MFRef_"));
        Assert.Equal(FormKey.Factory("02F2F4:Skyrim.esm"), copy.Base.FormKey);
        Assert.True((copy.MajorRecordFlagsRaw & 0x400) != 0, "our stand-in must be persistent — that's the point");
        Assert.Equal(System.MathF.PI / 2f, copy.Placement!.Rotation.Z, 0.001f);

        // the vanilla original is disabled + buried (else there'd be two hoes in the same spot)
        var orig = mod.EnumerateMajorRecords<IPlacedObjectGetter>()
            .Single(p => p.FormKey == FormKey.Factory("0D1991:Skyrim.esm"));
        Assert.True((orig.MajorRecordFlagsRaw & 0x800) != 0, "the replaced original must be InitiallyDisabled");
        Assert.True(orig.Placement!.Position.Z < -30000f, "the replaced original must be buried");
    }
}
