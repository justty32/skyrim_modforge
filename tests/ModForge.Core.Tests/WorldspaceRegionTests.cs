using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Regression tests for the Worldspace (WRLD) + Region (REGN) feature. These run Build/Validate
// purely in-memory (no Skyrim.esm needed — all refs are external FormKeys, resolved structurally),
// then assert on the produced Mutagen records and the validation guardrails.
public class WorldspaceRegionTests
{
    private const string Climate = "Skyrim.esm:0x000812";   // vanilla default climate
    private const string Water = "Skyrim.esm:0x000018";     // DefaultWater
    private const string Tamriel = "Skyrim.esm:0x00003C";   // parent worldspace
    private const string ClearWeather = "Skyrim.esm:0x10E1F2";
    private const string CloudyWeather = "Skyrim.esm:0x10E1F1";

    private static ModKey Out => ModKey.FromNameAndExtension("Test.esp");

    private static WorldspaceSpec MakeWorld(string ed = "TestWorld") => new()
    {
        EditorId = ed,
        Name = "Test World",
        Climate = Climate,
        Water = Water,
        Parent = Tamriel,
        Flags = { "SmallWorld" },
        DefaultLandHeight = -27000f,
        DefaultWaterHeight = -14000f,
    };

    private static RegionSpec MakeRegion(string ed = "TestRegion", string ws = "TestWorld") => new()
    {
        EditorId = ed,
        Worldspace = ws,
        EdgeFallOff = 1024,
        MapColor = "0x3CA0F0",
        WeatherPriority = 60,
        Weather =
        {
            new RegionWeatherEntrySpec { Weather = ClearWeather, Chance = 70 },
            new RegionWeatherEntrySpec { Weather = CloudyWeather, Chance = 30 },
        },
        Area =
        {
            new PointSpec { X = -16384, Y = -16384 },
            new PointSpec { X = 16384, Y = -16384 },
            new PointSpec { X = 16384, Y = 16384 },
            new PointSpec { X = -16384, Y = 16384 },
        },
    };

    private static IWorldspaceGetter BuildWorld(ModSpec spec, string ed = "TestWorld")
    {
        var result = Generator.Build(spec, Out);
        return result.Mod.Worldspaces.First(w => w.EditorID == ed);
    }

    // ---- Worldspace (WRLD) ------------------------------------------------------------------

    [Fact]
    public void Worldspace_WiresClimateWaterParentLinks()
    {
        var spec = new ModSpec { Worldspaces = { MakeWorld() } };
        var w = BuildWorld(spec);

        Assert.Equal("Test World", w.Name?.String);
        Assert.Equal(0x000812u, w.Climate.FormKey.ID);
        Assert.Equal(0x000018u, w.Water.FormKey.ID);
        Assert.NotNull(w.Parent);
        Assert.Equal(0x00003Cu, w.Parent!.Worldspace.FormKey.ID);
        Assert.True(w.Flags.HasFlag(Worldspace.Flag.SmallWorld));
    }

    [Fact]
    public void Worldspace_SetsLandAndWaterDefaults_TheFloodFix()
    {
        var spec = new ModSpec { Worldspaces = { MakeWorld() } };
        var w = BuildWorld(spec);

        Assert.NotNull(w.LandDefaults);
        Assert.Equal(-27000f, w.LandDefaults!.DefaultLandHeight);
        Assert.Equal(-14000f, w.LandDefaults!.DefaultWaterHeight);  // a 0 here floods sub-0 terrain
    }

    [Fact]
    public void Worldspace_SetsMapBoundsAndCamera()
    {
        var world = MakeWorld();
        world.Map.NorthwestX = -4; world.Map.NorthwestY = 4;
        world.Map.SoutheastX = 4; world.Map.SoutheastY = -4;
        world.Map.CameraInitialPitch = 55f;
        var spec = new ModSpec { Worldspaces = { world } };
        var w = BuildWorld(spec);

        Assert.NotNull(w.MapData);
        Assert.Equal((short)-4, w.MapData!.NorthwestCellCoords.X);
        Assert.Equal((short)4, w.MapData!.NorthwestCellCoords.Y);
        Assert.Equal((short)4, w.MapData!.SoutheastCellCoords.X);
        Assert.Equal(55f, w.MapData!.CameraInitialPitch);
    }

    [Fact]
    public void Worldspace_OptionalParentOmitted_LeavesNoParentLink()
    {
        var world = MakeWorld();
        world.Parent = "";
        var spec = new ModSpec { Worldspaces = { world } };
        var w = BuildWorld(spec);
        Assert.True(w.Parent is null || w.Parent.Worldspace.IsNull);
    }

    // ---- Region (REGN) ----------------------------------------------------------------------

    [Fact]
    public void Region_WiresWorldspaceAreaAndWeatherEntries()
    {
        var spec = new ModSpec { Worldspaces = { MakeWorld() }, Regions = { MakeRegion() } };
        var result = Generator.Build(spec, Out);
        var world = result.Mod.Worldspaces.First(x => x.EditorID == "TestWorld");
        var r = result.Mod.Regions.First(x => x.EditorID == "TestRegion");

        // worldspace link resolves to the in-spec worldspace's FormKey (cross-record wiring)
        Assert.Equal(world.FormKey, r.Worldspace.FormKey);

        // area
        Assert.Single(r.RegionAreas);
        Assert.Equal(4, r.RegionAreas[0].RegionPointListData!.Count);
        Assert.Equal(1024u, r.RegionAreas[0].EdgeFallOff);

        // weather table
        Assert.NotNull(r.Weather);
        Assert.Equal(60, r.Weather!.Priority);
        Assert.Equal(2, r.Weather!.Weathers!.Count);
        Assert.Equal(0x10E1F2u, r.Weather!.Weathers![0].Weather.FormKey.ID);
        Assert.Equal(70, r.Weather!.Weathers![0].Chance);
        Assert.Equal(30, r.Weather!.Weathers![1].Chance);

        // map color (RGB round-trips; alpha stripped)
        Assert.NotNull(r.MapColor);
        Assert.Equal(0x3C, r.MapColor!.Value.R);
        Assert.Equal(0xA0, r.MapColor!.Value.G);
        Assert.Equal(0xF0, r.MapColor!.Value.B);
    }

    [Fact]
    public void Region_CanReferenceVanillaWorldspaceDirectly()
    {
        var spec = new ModSpec { Regions = { MakeRegion("R2", ws: Tamriel) } };
        var result = Generator.Build(spec, Out);
        var r = result.Mod.Regions.First();
        Assert.Equal(0x00003Cu, r.Worldspace.FormKey.ID);
    }

    [Fact]
    public void Build_CountsWorldspacesAndRegionsInStats()
    {
        var spec = new ModSpec { Worldspaces = { MakeWorld() }, Regions = { MakeRegion() } };
        var result = Generator.Build(spec, Out);
        Assert.Equal(1, result.Stats.Worldspaces);
        Assert.Equal(1, result.Stats.Regions);
        Assert.Empty(result.Warnings);
    }

    // ---- Validate guardrails ----------------------------------------------------------------

    [Fact]
    public void Validate_CleanSpec_HasNoProblems()
    {
        var spec = new ModSpec { Worldspaces = { MakeWorld() }, Regions = { MakeRegion() } };
        Assert.Empty(Generator.Validate(spec));
    }

    [Fact]
    public void Validate_WorldspaceWithoutClimate_IsFlagged()
    {
        var world = MakeWorld();
        world.Climate = "";
        var spec = new ModSpec { Worldspaces = { world } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("no climate"));
    }

    [Fact]
    public void Validate_WorldspaceBadFlag_IsFlagged()
    {
        var world = MakeWorld();
        world.Flags.Add("NotARealFlag");
        var spec = new ModSpec { Worldspaces = { world } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("invalid flag"));
    }

    [Fact]
    public void Validate_RegionWithoutWeather_IsFlagged()
    {
        var region = MakeRegion();
        region.Weather.Clear();
        var spec = new ModSpec { Worldspaces = { MakeWorld() }, Regions = { region } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("no weather"));
    }

    [Fact]
    public void Validate_RegionWeatherChancesSumZero_IsFlagged()
    {
        var region = MakeRegion();
        foreach (var w in region.Weather) w.Chance = 0;
        var spec = new ModSpec { Worldspaces = { MakeWorld() }, Regions = { region } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("sum to 0"));
    }

    [Fact]
    public void Validate_RegionWithTooFewAreaPoints_IsFlagged()
    {
        var region = MakeRegion();
        region.Area.RemoveRange(0, region.Area.Count - 2);  // leave 2 points
        var spec = new ModSpec { Worldspaces = { MakeWorld() }, Regions = { region } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("need ≥3"));
    }

    [Fact]
    public void Validate_RegionWithoutArea_IsFlagged()
    {
        var region = MakeRegion();
        region.Area.Clear();
        var spec = new ModSpec { Worldspaces = { MakeWorld() }, Regions = { region } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("no area"));
    }

    [Fact]
    public void Validate_RegionWithoutWorldspace_IsFlagged()
    {
        var region = MakeRegion();
        region.Worldspace = "";
        var spec = new ModSpec { Worldspaces = { MakeWorld() }, Regions = { region } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("no worldspace"));
    }

    [Fact]
    public void Validate_RegionBadMapColor_IsFlagged()
    {
        var region = MakeRegion();
        region.MapColor = "not-a-color";
        var spec = new ModSpec { Worldspaces = { MakeWorld() }, Regions = { region } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("mapColor"));
    }

    [Fact]
    public void Validate_DuplicateWorldspaceEditorId_IsFlagged()
    {
        var spec = new ModSpec { Worldspaces = { MakeWorld(), MakeWorld() } };
        Assert.Contains(Generator.Validate(spec), p => p.Contains("duplicate editorId"));
    }
}
