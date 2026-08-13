using System.IO;
using Mutagen.Bethesda.Plugins;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class GodotPlacementsBuildValidationTests
{
    private static string WritePlacements(string baseRef, string instanceId)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mf_gp_build_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, $$"""
            { "version": 1, "coordinate_system": "godot4_y_up", "placements": [
              { "base": "{{baseRef}}", "instanceId": "{{instanceId}}",
                "position":{"x":0,"y":0,"z":0}, "rotation":{"x":0,"y":0,"z":0}, "scale":1 }
            ] }
            """);
        return path;
    }

    private static ModSpec SpecWithGodot(string path, string worldspaceId = "GodotWorld")
    {
        var spec = new ModSpec { Esl = false };
        spec.Worldspaces.Add(new WorldspaceSpec
        {
            EditorId = worldspaceId,
            Name = worldspaceId,
            Climate = "Skyrim.esm:0x000812",
            GodotPlacements = new GodotPlacementsSpec { Path = path },
        });
        return spec;
    }

    private static InvalidDataException BuildInvalid(ModSpec spec) =>
        Assert.Throws<InvalidDataException>(() => Generator.Build(
            spec, ModKey.FromNameAndExtension("Test.esp")));

    [Fact]
    public void Build_InstanceIdCollidesWithAuthoredPlacement_Throws()
    {
        var path = WritePlacements("Skyrim.esm:0x00003B", "Marker");
        try
        {
            var spec = SpecWithGodot(path);
            spec.Placements.Add(new PlacementSpec { EditorId = "marker", Base = "Skyrim.esm:0x00003B" });

            var error = BuildInvalid(spec);
            Assert.Contains(path, error.Message);
            Assert.Contains("placements[0].instanceId 'Marker' collides", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_InstanceIdCollidesAcrossGodotFiles_Throws()
    {
        var firstPath = WritePlacements("Skyrim.esm:0x00003B", "Marker");
        var secondPath = WritePlacements("Skyrim.esm:0x00003B", "marker");
        try
        {
            var spec = SpecWithGodot(firstPath, "FirstWorld");
            spec.Worldspaces.Add(SpecWithGodot(secondPath, "SecondWorld").Worldspaces[0]);

            var error = BuildInvalid(spec);
            Assert.Contains(secondPath, error.Message);
            Assert.Contains("worldspace 'SecondWorld'", error.Message);
            Assert.Contains("instanceId 'marker' collides", error.Message);
        }
        finally { File.Delete(firstPath); File.Delete(secondPath); }
    }

    [Fact]
    public void Build_UnresolvableImportedBase_Throws()
    {
        var path = WritePlacements("Skyrim.esm:notHex", "Marker");
        try
        {
            var error = BuildInvalid(SpecWithGodot(path));
            Assert.Contains("placements[0].base 'Skyrim.esm:notHex'", error.Message);
            Assert.Contains("not a resolvable", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_NonZeroHighByteExternalBase_ThrowsInsteadOfMasking()
    {
        var path = WritePlacements("Skyrim.esm:0xDEADBEEF", "Marker");
        try
        {
            var error = BuildInvalid(SpecWithGodot(path));
            Assert.Contains("base 'Skyrim.esm:0xDEADBEEF'", error.Message);
            Assert.Contains("8 with leading 00", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_EightDigitLeadingZeroExternalBase_IsAccepted()
    {
        var path = WritePlacements("Skyrim.esm:0x0000003B", "Marker");
        try
        {
            var result = Generator.Build(
                SpecWithGodot(path), ModKey.FromNameAndExtension("Test.esp"));
            Assert.Equal(1, result.Stats.Placements);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_InstanceIdCollidesWithExistingStatic_Throws()
    {
        var path = WritePlacements("Skyrim.esm:0x00003B", "SharedId");
        try
        {
            var spec = SpecWithGodot(path);
            spec.Statics.Add(new StaticSpec { EditorId = "sharedid", Model = "Meshes\\marker.nif" });

            var error = BuildInvalid(spec);
            Assert.Contains("instanceId 'SharedId' collides with an existing or planned editorId", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_InstanceIdCollidesWithLaterMapMarker_Throws()
    {
        var path = WritePlacements("Skyrim.esm:0x00003B", "SharedId");
        try
        {
            var spec = SpecWithGodot(path);
            spec.MapMarkers.Add(new MapMarkerSpec { EditorId = "sharedid" });

            var error = BuildInvalid(spec);
            Assert.Contains("instanceId 'SharedId' collides with an existing or planned editorId", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_InstanceIdCollidesWithLaterNavCut_Throws()
    {
        var path = WritePlacements("Skyrim.esm:0x00003B", "CutId");
        try
        {
            var spec = SpecWithGodot(path);
            spec.NavCuts.Add(new NavCutSpec { EditorId = "cutid" });

            var error = BuildInvalid(spec);
            Assert.Contains("instanceId 'CutId' collides with an existing or planned editorId", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_InstanceIdCollidesWithLaterRegion_Throws()
    {
        var path = WritePlacements("Skyrim.esm:0x00003B", "RegionId");
        try
        {
            var spec = SpecWithGodot(path);
            spec.Regions.Add(new RegionSpec { EditorId = "regionid" });

            var error = BuildInvalid(spec);
            Assert.Contains("instanceId 'RegionId' collides with an existing or planned editorId", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_InstanceIdCollidesWithGeneratedReferenceAnchor_Throws()
    {
        var path = WritePlacements("Skyrim.esm:0x00003B", "MFRef_my_label_1");
        try
        {
            var spec = SpecWithGodot(path);
            spec.References.Add(new ReferenceSpec { Label = "my label", Anchor = "marker" });

            var error = BuildInvalid(spec);
            Assert.Contains("instanceId 'MFRef_my_label_1' collides with an existing or planned editorId", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_NullReferenceAnchor_DoesNotCrashReservation()
    {
        var path = WritePlacements("Skyrim.esm:0x00003B", "Marker");
        try
        {
            var spec = SpecWithGodot(path);
            spec.References.Add(new ReferenceSpec { Label = "other", Anchor = null! });

            var result = Generator.Build(spec, ModKey.FromNameAndExtension("Test.esp"));
            Assert.Equal(1, result.Stats.Placements);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_InSpecLeveledNpcBase_ThrowsCtdDiagnostic()
    {
        var path = WritePlacements("BadList", "Marker");
        try
        {
            var spec = SpecWithGodot(path);
            spec.LeveledNpcs.Add(new LeveledNpcSpec { EditorId = "BadList" });

            var error = BuildInvalid(spec);
            Assert.Contains("LeveledNpc list", error.Message);
            Assert.Contains("cause CTD", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_InstanceIdCollidesWithLateGeneratedExteriorCell_Throws()
    {
        var path = WritePlacements("Skyrim.esm:0x00003B", "GodotWorld_Cell_0_0");
        try
        {
            var error = BuildInvalid(SpecWithGodot(path));
            Assert.Contains(path, error.Message);
            Assert.Contains("placements[0].instanceId 'GodotWorld_Cell_0_0'", error.Message);
            Assert.Contains("found 2", error.Message);
            Assert.Contains("generated later", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Build_InstanceIdCollidesWithLateGeneratedMcmQuest_Throws()
    {
        var path = WritePlacements("Skyrim.esm:0x00003B", "MF_MCM_Menu");
        try
        {
            var spec = SpecWithGodot(path);
            spec.McmConfigs.Add(new McmSpec { ModName = "Menu" });

            var error = BuildInvalid(spec);
            Assert.Contains(path, error.Message);
            Assert.Contains("placements[0].instanceId 'MF_MCM_Menu'", error.Message);
            Assert.Contains("found 2", error.Message);
        }
        finally { File.Delete(path); }
    }
}
