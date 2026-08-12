using System.IO;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class GodotPlacementsValidationTests
{
    private static string WriteTempJson(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mf_gp_invalid_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    private static InvalidDataException LoadInvalid(string json)
    {
        var path = WriteTempJson(json);
        try
        {
            var spec = new GodotPlacementsSpec { Path = path };
            var error = Assert.Throws<InvalidDataException>(
                () => GodotPlacements.Load(spec, "", "TestWorld"));
            Assert.Contains(path, error.Message);
            return error;
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_NullPlacements_ThrowsClearError()
    {
        var error = LoadInvalid("""
            { "version": 1, "coordinate_system": "godot4_y_up", "placements": null }
            """);
        Assert.Contains("placements must be an array, not null", error.Message);
    }

    [Fact]
    public void Load_MissingPlacements_ThrowsClearError()
    {
        var error = LoadInvalid("""
            { "version": 1, "coordinate_system": "godot4_y_up" }
            """);
        Assert.Contains("placements must be an array, not null", error.Message);
    }

    [Theory]
    [InlineData("null", "placements[0] must be an object")]
    [InlineData("{}", "placements[0].base")]
    [InlineData("{\"base\":null}", "placements[0].base")]
    [InlineData("{\"base\":\"Skyrim.esm:0x00003B\",\"rotation\":{\"x\":0,\"y\":0,\"z\":0},\"scale\":1}", "placements[0].position")]
    [InlineData("{\"base\":\"Skyrim.esm:0x00003B\",\"position\":null}", "placements[0].position")]
    [InlineData("{\"base\":\"Skyrim.esm:0x00003B\",\"position\":{\"x\":0,\"y\":0,\"z\":0},\"scale\":1}", "placements[0].rotation")]
    [InlineData("{\"base\":\"Skyrim.esm:0x00003B\",\"position\":{\"x\":0,\"y\":0,\"z\":0},\"rotation\":null}", "placements[0].rotation")]
    [InlineData("{\"base\":\"Skyrim.esm:0x00003B\",\"position\":{},\"rotation\":{\"x\":0,\"y\":0,\"z\":0},\"scale\":1}", "position must contain numeric x, y, and z")]
    [InlineData("{\"base\":\"Skyrim.esm:0x00003B\",\"position\":{\"x\":0,\"y\":0,\"z\":0},\"rotation\":{},\"scale\":1}", "rotation must contain numeric x, y, and z")]
    [InlineData("{\"base\":\"Skyrim.esm:0x00003B\",\"position\":{\"x\":0,\"y\":0,\"z\":0},\"rotation\":{\"x\":0,\"y\":0,\"z\":0}}", "placements[0].scale")]
    [InlineData("{\"base\":\"Skyrim.esm:0x00003B\",\"position\":{\"x\":0,\"y\":0,\"z\":0},\"rotation\":{\"x\":0,\"y\":0,\"z\":0},\"scale\":0}", "placements[0].scale")]
    [InlineData("{\"base\":\"Skyrim.esm:0x00003B\",\"position\":{\"x\":0,\"y\":0,\"z\":0},\"rotation\":{\"x\":0,\"y\":0,\"z\":0},\"scale\":-1}", "placements[0].scale")]
    public void Load_InvalidEntry_ThrowsClearIndexedError(string entry, string expected)
    {
        var json = $$"""
            { "version": 1, "coordinate_system": "godot4_y_up", "placements": [{{entry}}] }
            """;
        var error = LoadInvalid(json);
        Assert.Contains(expected, error.Message);
    }

    [Theory]
    [InlineData("position", "overflows during Skyrim coordinate conversion")]
    [InlineData("rotation", "overflows during degree conversion")]
    public void Load_ConvertedVectorOverflow_Throws(string field, string expected)
    {
        var huge = "{\"x\":3e38,\"y\":0,\"z\":0}";
        var zero = "{\"x\":0,\"y\":0,\"z\":0}";
        var position = field == "position" ? huge : zero;
        var rotation = field == "rotation" ? huge : zero;
        var error = LoadInvalid($$"""
            { "version": 1, "coordinate_system": "godot4_y_up", "placements": [
              { "base": "Skyrim.esm:0x00003B", "position": {{position}}, "rotation": {{rotation}}, "scale": 1 }
            ] }
            """);
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void Load_DuplicateInstanceId_ThrowsCaseInsensitively()
    {
        var error = LoadInvalid("""
            { "version": 1, "coordinate_system": "godot4_y_up", "placements": [
              { "base":"Skyrim.esm:0x00003B", "instanceId":"Marker", "position":{"x":0,"y":0,"z":0}, "rotation":{"x":0,"y":0,"z":0}, "scale":1 },
              { "base":"Skyrim.esm:0x00003B", "instanceId":"marker", "position":{"x":1,"y":0,"z":0}, "rotation":{"x":0,"y":0,"z":0}, "scale":1 }
            ] }
            """);
        Assert.Contains("placements[1].instanceId 'marker' is duplicated", error.Message);
    }
}
