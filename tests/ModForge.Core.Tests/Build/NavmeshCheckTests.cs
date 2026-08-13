using System.Linq;
using Mutagen.Bethesda.Plugins;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// P1 navmesh diagnostics (navmesh plan) — turn the two SILENT, in-game-only failures into build
// warnings: "my NPC just stands there" (② off-navmesh ACHR) and "NPCs walk through my house"
// (① uncut obstacle). Reading vanilla navmesh geometry needs Skyrim.esm, so most of this is
// RequiresSkyrim; the offline half is the one check we can make with no master at all — and it
// happens to be the highest-value one.
public class NavmeshCheckTests
{
    private const string WhiterunWorld = "Skyrim.esm:0x01A26F";
    private const string Carlotta = "Skyrim.esm:0x013B99";
    private static readonly ModKey Key = ModKey.FromNameAndExtension("MFNavChk.esp");

    // --- offline: an in-spec interior cell has NO navmesh, and we know that for a fact ------------

    private static ModSpec EmptyRoomWithAnNpc(bool warnEmptyCells)
    {
        var s = new ModSpec
        {
            PluginName = "MFNavChk.esp",
            Navmesh = new NavmeshSpec { WarnEmptyCells = warnEmptyCells },
        };
        s.Cells.Add(new CellSpec { EditorId = "MFEmptyRoom", Name = "Empty Room" });
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Stuck", Base = Carlotta, Kind = "npc", Cell = "MFEmptyRoom",
            Position = new Vec3 { X = 0f, Y = 0f, Z = 0f },
        });
        return s;
    }

    [Fact]
    public void CustomInteriorCell_IsSilentByDefault()
    {
        // TRUE but not actionable: ModForge cannot author interior navmesh (P3 of the plan), so this
        // would fire on literally every custom interior ever built. A warning that always fires is
        // noise. It is opt-in, and the docs/plan carry the fact instead.
        var r = Generator.Build(EmptyRoomWithAnNpc(warnEmptyCells: false), Key,
            new BuildOptions { SkyrimDataPath = "/nonexistent" });
        Assert.DoesNotContain(r.Warnings, w => w.Contains("NO navmesh"));
    }

    [Fact]
    public void CustomInteriorCell_WarnsWhenAskedTo()
    {
        // No master needed: we KNOW we never authored navmesh there. So this one check still works on
        // the offline machine, where every geometric check is necessarily silent.
        var r = Generator.Build(EmptyRoomWithAnNpc(warnEmptyCells: true), Key,
            new BuildOptions { SkyrimDataPath = "/nonexistent" });
        Assert.Contains(r.Warnings, w => w.Contains("NO navmesh") && w.Contains("stand still"));
    }

    [Fact]
    public void OneWarningPerCell_NotPerNpc()
    {
        var s = EmptyRoomWithAnNpc(warnEmptyCells: true);
        for (int i = 1; i < 4; i++)
            s.Placements.Add(new PlacementSpec
            {
                EditorId = $"Stuck{i}", Base = Carlotta, Kind = "npc", Cell = "MFEmptyRoom",
                Position = new Vec3 { X = i * 50f, Y = 0f, Z = 0f },
            });
        var r = Generator.Build(s, Key, new BuildOptions { SkyrimDataPath = "/nonexistent" });
        Assert.Single(r.Warnings.Where(w => w.Contains("NO navmesh")));
    }

    [Fact]
    public void WarningsCanBeSwitchedOff()
    {
        var s = EmptyRoomWithAnNpc(warnEmptyCells: true);
        s.Navmesh.Warnings = false;
        var r = Generator.Build(s, Key, new BuildOptions { SkyrimDataPath = "/nonexistent" });
        Assert.DoesNotContain(r.Warnings, w => w.Contains("navmesh"));
    }

    [Fact]
    public void WithoutTheMasterCache_AVanillaCellSaysNothingAboutNavmesh()
    {
        // "Unknown" must never read as "no navmesh" — the offline machine must not be spammed with
        // warnings it cannot act on (CLAUDE.md rule ①). The build still warns about the MISSING MASTER
        // (BuildPlacements does), but nothing navmesh-flavoured.
        var s = new ModSpec { PluginName = "MFNavChk.esp" };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Guy", Base = Carlotta, Kind = "npc", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7470f, Z = -3561.8f },
        });
        var r = Generator.Build(s, Key, new BuildOptions { SkyrimDataPath = "/nonexistent" });
        Assert.DoesNotContain(r.Warnings, w => w.Contains("navmesh:"));
    }

    // --- ② the off-navmesh NPC (needs the real geometry) -------------------------------------------

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void NpcOnVanillaNavmesh_IsSilent()
    {
        // (21750, -7470, -3561.8) on the Whiterun main street — read straight off Skyrim.esm's navmesh
        // (the triangle's own interpolated height at that point). This is the "correct" case.
        var s = new ModSpec { PluginName = "MFNavChk.esp" };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Walker", Base = Carlotta, Kind = "npc", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7470f, Z = -3561.8f },
        });
        var r = Generator.Build(s, Key);
        Assert.DoesNotContain(r.Warnings, w => w.Contains("navmesh:"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void NpcOffTheNavmesh_IsWarnedThatItWillNotMove()
    {
        // Same street, but 2500 units north — inside the buildings / off the walkable surface entirely.
        var s = new ModSpec { PluginName = "MFNavChk.esp" };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Statue", Base = Carlotta, Kind = "npc", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -4900f, Z = -3560f },
        });
        var r = Generator.Build(s, Key);
        Assert.Contains(r.Warnings, w => w.Contains("Statue") && w.Contains("off the navmesh") && w.Contains("will NOT move"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void NpcFloatingAboveTheNavmesh_IsWarned()
    {
        // Right spot, wrong height — the classic "guessed z" bug. A marker/ACHR authored a storey up
        // is off-mesh vertically even though its XY is perfect.
        var s = new ModSpec { PluginName = "MFNavChk.esp" };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Floater", Base = Carlotta, Kind = "npc", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7470f, Z = -3000f },   // ~560 units up
        });
        var r = Generator.Build(s, Key);
        Assert.Contains(r.Warnings, w => w.Contains("Floater") && w.Contains("ABOVE the navmesh"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void DeliberatelyOffStageActor_CanOptOut()
    {
        // livingNpcs parks its actors under Tamriel at (0, -9000, -5000) — ~530 units below the terrain
        // — and MoveTo's them into the world from a script. They never path from there, so the warning
        // would be a false alarm. This is the ONE legitimate use of navmeshCheck: false.
        var s = new ModSpec { PluginName = "MFNavChk.esp" };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Parked", Base = Carlotta, Kind = "npc", Worldspace = "Skyrim.esm:0x00003C",
            Position = new Vec3 { X = 0f, Y = -9000f, Z = -5000f },
            NavmeshCheck = false,
        });
        Assert.DoesNotContain(Generator.Build(s, Key).Warnings, w => w.Contains("navmesh:"));

        s.Placements[0].NavmeshCheck = true;   // and without the opt-out it DOES fire
        Assert.Contains(Generator.Build(s, Key).Warnings, w => w.Contains("Parked") && w.Contains("navmesh"));
    }

    // --- ① the uncut obstacle ---------------------------------------------------------------------

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void BlockingPlacementWithAutoOff_IsWarnedAboutHowManyTrianglesItCovers()
    {
        var s = new ModSpec { PluginName = "MFNavChk.esp", Navmesh = new NavmeshSpec { AutoNavCuts = false } };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "House", Base = "Skyrim.esm:0x0DCD68", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7625f, Z = -3570f },
        });
        var r = Generator.Build(s, Key);
        var w = Assert.Single(r.Warnings.Where(x => x.Contains("House") && x.Contains("vanilla navmesh triangle")));
        Assert.Contains("NPCs will walk into it", w);
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void AnExplicitNavCutOverThePlacement_SilencesTheObstacleWarning()
    {
        var s = new ModSpec { PluginName = "MFNavChk.esp", Navmesh = new NavmeshSpec { AutoNavCuts = false } };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "House", Base = "Skyrim.esm:0x0DCD68", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7625f, Z = -3570f },
        });
        s.NavCuts.Add(new NavCutSpec { EditorId = "Cut", Placement = "House" });
        var r = Generator.Build(s, Key);
        Assert.Equal(1, r.Stats.NavCuts);
        Assert.DoesNotContain(r.Warnings, x => x.Contains("NPCs will walk into it"));
    }

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void Clutter_NeverWarns()
    {
        // A sign post on the busiest navmesh in the game. If this warned, every spec with scenery in it
        // would drown in noise — which is exactly what the size thresholds exist to prevent.
        var s = new ModSpec { PluginName = "MFNavChk.esp", Navmesh = new NavmeshSpec { AutoNavCuts = false } };
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "Post", Base = "Skyrim.esm:0x09625E", Worldspace = WhiterunWorld,
            Position = new Vec3 { X = 21750f, Y = -7625f, Z = -3570f },
        });
        var r = Generator.Build(s, Key);
        Assert.DoesNotContain(r.Warnings, x => x.Contains("navmesh:"));
    }

    // --- the shipped spike spec must stay clean ----------------------------------------------------

    [Fact]
    [Trait("Category", "RequiresSkyrim")]
    public void TheSpikeSpec_PlacesEveryMarkerAndNpcOnLiveNavmesh()
    {
        // examples/navcut_spike_spec.json is a falsification experiment: if any of its markers were off
        // the navmesh the patrol would silently never start and the result would be meaningless. The P1
        // check is exactly the tool that guarantees that — so hold the spike to it.
        var json = File.ReadAllText(Path.Combine(RepoRoot(), "examples", "navcut_spike_spec.json"));
        var spec = System.Text.Json.JsonSerializer.Deserialize<ModSpec>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        Assert.Empty(Generator.Validate(spec));
        var r = Generator.Build(spec, ModKey.FromNameAndExtension("ModForgeNavcutSpike.esp"));
        Assert.Equal(1, r.Stats.NavCuts);                        // exactly ONE navcut — the experiment
        Assert.Empty(r.Warnings);                                 // nothing off-mesh, nothing uncut
    }

    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "examples"))) d = d.Parent;
        return d?.FullName ?? throw new DirectoryNotFoundException("repo root (with examples/) not found");
    }
}
