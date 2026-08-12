using System.IO;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class GodotPlacementsTests
{
    private const float K = 0.014286f;          // m/unit
    private const float R2D = 180f / MathF.PI;  // radians → degrees

    private static string WriteTempJson(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mf_gp_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Load_BasicConversion_PositionCorrect()
    {
        var path = WriteTempJson("""
            {
              "version": 1,
              "coordinate_system": "godot4_y_up",
              "placements": [
                { "base": "Skyrim.esm:0x000001", "position": {"x":1.0,"y":2.0,"z":3.0}, "rotation": {"x":0,"y":0,"z":0}, "scale":1.0 }
              ]
            }
            """);
        try
        {
            var spec = new GodotPlacementsSpec { Path = path, OriginX = 1, OriginY = 2 };
            var results = GodotPlacements.Load(spec, "", "TestWorld");

            Assert.Single(results);
            var p = results[0];
            Assert.Equal("TestWorld", p.Worldspace);
            Assert.Equal(1 * 4096f + 1.0f / K, p.Position.X, precision: 1);
            Assert.Equal(2 * 4096f - 3.0f / K, p.Position.Y, precision: 1);  // Z flipped
            Assert.Equal(2.0f / K,              p.Position.Z, precision: 1);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_RotationConvertedToDegrees_AndAxisRemapped()
    {
        // A Godot yaw (rotation about +Y/up) must become a Skyrim yaw (rotation about +Z/up),
        // not a Skyrim Y(north-axis) rotation — the axes follow the same change of basis as position.
        var path = WriteTempJson("""
            {
              "version": 1,
              "coordinate_system": "godot4_y_up",
              "placements": [
                { "base": "Skyrim.esm:0x000001", "position": {"x":0,"y":0,"z":0},
                  "rotation": {"x":0.0,"y":1.5707963,"z":0.0}, "scale":1.0 }
              ]
            }
            """);
        try
        {
            var spec = new GodotPlacementsSpec { Path = path };
            var results = GodotPlacements.Load(spec, "", "TestWorld");
            Assert.Equal(0f,  results[0].Rotation.X, precision: 3);
            Assert.Equal(0f,  results[0].Rotation.Y, precision: 3);
            Assert.Equal(90f, results[0].Rotation.Z, precision: 3);   // Godot +Y yaw (π/2) → Skyrim +Z yaw 90°
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_RotationAxisRemap_GodotZToNegativeSkyrimY()
    {
        // Godot +Z(south) rotation → Skyrim −Y(north) rotation (sign flips, mirroring the position flip).
        var path = WriteTempJson("""
            {
              "version": 1,
              "coordinate_system": "godot4_y_up",
              "placements": [
                { "base": "Skyrim.esm:0x000001", "position": {"x":0,"y":0,"z":0},
                  "rotation": {"x":0.0,"y":0.0,"z":1.5707963}, "scale":1.0 }
              ]
            }
            """);
        try
        {
            var spec = new GodotPlacementsSpec { Path = path };
            var results = GodotPlacements.Load(spec, "", "TestWorld");
            Assert.Equal(0f,   results[0].Rotation.X, precision: 3);
            Assert.Equal(-90f, results[0].Rotation.Y, precision: 3);
            Assert.Equal(0f,   results[0].Rotation.Z, precision: 3);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_InstanceId_MapsToEditorId()
    {
        var path = WriteTempJson("""
            {
              "version": 1,
              "coordinate_system": "godot4_y_up",
              "placements": [
                { "base": "Skyrim.esm:0x000001", "instanceId": "MyMarker01",
                  "position": {"x":0,"y":0,"z":0}, "rotation": {"x":0,"y":0,"z":0}, "scale":1.0 },
                { "base": "Skyrim.esm:0x000002",
                  "position": {"x":0,"y":0,"z":0}, "rotation": {"x":0,"y":0,"z":0}, "scale":1.0 }
              ]
            }
            """);
        try
        {
            var spec = new GodotPlacementsSpec { Path = path };
            var results = GodotPlacements.Load(spec, "", "TestWorld");
            Assert.Equal("MyMarker01", results[0].EditorId);
            Assert.Equal("",           results[1].EditorId);   // absent → ""
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_ScalePassthrough()
    {
        var path = WriteTempJson("""
            {
              "version": 1,
              "coordinate_system": "godot4_y_up",
              "placements": [
                { "base": "Skyrim.esm:0x000001", "position": {"x":0,"y":0,"z":0}, "rotation": {"x":0,"y":0,"z":0}, "scale":2.5 }
              ]
            }
            """);
        try
        {
            var spec = new GodotPlacementsSpec { Path = path };
            var results = GodotPlacements.Load(spec, "", "TestWorld");
            Assert.Equal(2.5f, results[0].Scale);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_UnsupportedCoordSystem_Throws()
    {
        var path = WriteTempJson("""
            { "version": 1, "coordinate_system": "blender_z_up", "placements": [] }
            """);
        try
        {
            var spec = new GodotPlacementsSpec { Path = path };
            Assert.Throws<NotSupportedException>(() => GodotPlacements.Load(spec, "", "TestWorld"));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void Load_UnsupportedFormatVersion_Throws(int version)
    {
        var path = WriteTempJson($$"""
            { "version": {{version}}, "coordinate_system": "godot4_y_up", "placements": [] }
            """);
        try
        {
            var spec = new GodotPlacementsSpec { Path = path };
            var error = Assert.Throws<NotSupportedException>(
                () => GodotPlacements.Load(spec, "", "TestWorld"));
            Assert.Contains($"format version '{version}'", error.Message);
            Assert.Contains("only version 1 is supported", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_MissingFormatVersion_Throws()
    {
        var path = WriteTempJson("""
            { "coordinate_system": "godot4_y_up", "placements": [] }
            """);
        try
        {
            var spec = new GodotPlacementsSpec { Path = path };
            var error = Assert.Throws<NotSupportedException>(
                () => GodotPlacements.Load(spec, "", "TestWorld"));
            Assert.Contains("format version '<missing>'", error.Message);
            Assert.Contains("only version 1 is supported", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_FileNotFound_Throws()
    {
        var spec = new GodotPlacementsSpec { Path = "/nonexistent/placements.json" };
        Assert.Throws<FileNotFoundException>(() => GodotPlacements.Load(spec, "", "TestWorld"));
    }

    [Fact]
    public void Load_OriginOffset_AppliedToPosition()
    {
        var path = WriteTempJson("""
            {
              "version": 1,
              "coordinate_system": "godot4_y_up",
              "placements": [
                { "base": "Skyrim.esm:0x000001", "position": {"x":0,"y":0,"z":0}, "rotation": {"x":0,"y":0,"z":0}, "scale":1.0 }
              ]
            }
            """);
        try
        {
            var spec = new GodotPlacementsSpec { Path = path, OriginX = 3, OriginY = -2 };
            var results = GodotPlacements.Load(spec, "", "TestWorld");
            Assert.Equal(3 * 4096f, results[0].Position.X, precision: 1);
            Assert.Equal(-2 * 4096f, results[0].Position.Y, precision: 1);
        }
        finally { File.Delete(path); }
    }
}
