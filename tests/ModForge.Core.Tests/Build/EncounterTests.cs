using System.IO;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;

namespace ModForge.Core.Tests;

// Regression tests for encounter spawns + encounter zones. All MASTER-FREE: every ref is in-spec
// (no vanilla template clone / vanilla-cell override), so Generator.Build never opens Skyrim.esm.
// Each test builds a spec -> mod, writes it to a temp .esp, reads it back, and asserts the binary
// round-tripped the records/links — the same path the CLI build + dump exercise.
public sealed class EncounterTests
{
    private static readonly ModKey OutKey = ModKey.FromNameAndExtension("EncTest.esp");

    // Build a spec and read the result back from a freshly-written binary (proves on-disk round-trip).
    private static ISkyrimModGetter Roundtrip(ModSpec spec, out System.Collections.Generic.IReadOnlyList<string> warnings)
    {
        var result = Generator.Build(spec, OutKey);
        warnings = result.Warnings;
        // Unique temp DIR so the file keeps the mod's ModKey name (Mutagen aligns filename to ModKey);
        // PluginIo.Write is the exact path the CLI build uses (NoCheck on the alignment).
        var dir = Path.Combine(Path.GetTempPath(), $"enc_{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, OutKey.FileName);
        PluginIo.Write(result.Mod, path);
        return SkyrimMod.CreateFromBinaryOverlay(new ModPath(path), SkyrimRelease.SkyrimSE);
    }

    private static ModSpec DenSpec()
    {
        return new ModSpec
        {
            PluginName = "EncTest.esp",
            LeveledNpcs =
            {
                new LeveledNpcSpec { EditorId = "MF_BanditList", ChanceNone = 0,
                    Entries = { new LeveledEntrySpec { Reference = "Skyrim.esm:0x01A341", Level = 1, Count = 1 } } },
            },
            Factions = { new FactionSpec { EditorId = "MF_DenFaction", Name = "Den" } },
            EncounterZones =
            {
                new EncounterZoneSpec { EditorId = "MF_DenZone", MinLevel = 4, MaxLevel = 12,
                    Owner = "MF_DenFaction", Rank = 0, Flags = { "MatchPcBelowMinimumLevel" } },
            },
            Cells = { new CellSpec { EditorId = "MF_Den", Name = "Den", EncounterZone = "MF_DenZone" } },
            Placements =
            {
                // A leveled-actor spawn: base is an IN-SPEC LeveledNpc, so it auto-detects as an ACHR.
                new PlacementSpec { Base = "MF_BanditList", Cell = "MF_Den",
                    Position = new Vec3 { X = 10, Y = 20, Z = 0 }, EncounterZone = "MF_DenZone" },
            },
        };
    }

    // An in-spec LeveledNpc (LVLN) base must NOT auto-detect as an ACHR: Skyrim CTDs at load with an
    // LVLN as an ACHR base (the engine calls NPC_-only vtable methods on the LVLN). Without an explicit
    // kind:npc the spawn stays a plain PlacedObject (REFR). The correct actor idiom is an NPC_ whose
    // template chain references the list (LvlBandit*, not the raw list). See ca379dc + the SPEC-world
    // LVLN-CTD gotcha.
    [Fact]
    public void LeveledNpcBase_DoesNotAutoBecomeAchr()
    {
        var mod = Roundtrip(DenSpec(), out _);
        var lvln = mod.EnumerateMajorRecords<ILeveledNpcGetter>().Single(l => l.EditorID == "MF_BanditList");

        Assert.Empty(mod.EnumerateMajorRecords<IPlacedNpcGetter>());            // NOT an ACHR
        var refr = mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single();   // a plain REFR instead
        Assert.Equal(lvln.FormKey, refr.Base.FormKey);
    }

    [Fact]
    public void EncounterZone_RoundTrips_LevelRangeAndFlags()
    {
        var mod = Roundtrip(DenSpec(), out _);
        var ecz = mod.EnumerateMajorRecords<IEncounterZoneGetter>().Single();

        Assert.Equal("MF_DenZone", ecz.EditorID);
        Assert.Equal(4, ecz.MinLevel);
        Assert.Equal(12, ecz.MaxLevel);
        Assert.True(ecz.Flags.HasFlag(EncounterZone.Flag.MatchPcBelowMinimumLevel));
        // Owner wired to the in-spec faction.
        var fac = mod.EnumerateMajorRecords<IFactionGetter>().Single();
        Assert.Equal(fac.FormKey, ecz.Owner.FormKey);
    }

    // A raw LVLN can be neither an ACHR base (CTD) nor a REFR base — it's a list, not a placeable form.
    // So a placement on an in-spec LVLN base must WARN regardless of kind, not just when kind:npc (the
    // no-kind case used to fall through to a silent, equally-invalid PlacedObject).
    [Fact]
    public void LeveledNpcBase_WithoutKind_StillWarns()
    {
        Roundtrip(DenSpec(), out var warnings);   // DenSpec's placement has no explicit kind
        Assert.Contains(warnings, w => w.Contains("LeveledNpc list (LVLN)"));
    }

    [Fact]
    public void Cell_And_Spawn_Reference_TheZone()
    {
        var mod = Roundtrip(DenSpec(), out _);
        var ecz = mod.EnumerateMajorRecords<IEncounterZoneGetter>().Single();
        var cell = mod.EnumerateMajorRecords<ICellGetter>().Single();
        var spawn = mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single();   // REFR — LVLN base doesn't auto-ACHR

        Assert.Equal(ecz.FormKey, cell.EncounterZone.FormKey);    // XEZN on the cell
        Assert.Equal(ecz.FormKey, spawn.EncounterZone.FormKey);   // XEZN on the spawn
    }

    [Fact]
    public void MaxLevelZero_MeansUncapped_AndRoundTrips()
    {
        var spec = DenSpec();
        spec.EncounterZones[0].MaxLevel = 0;   // uncapped, scales with the player
        var mod = Roundtrip(spec, out _);
        var ecz = mod.EnumerateMajorRecords<IEncounterZoneGetter>().Single();
        Assert.Equal(0, ecz.MaxLevel);
        Assert.Equal(4, ecz.MinLevel);
    }

    // ---- Validate guardrails ------------------------------------------------------------------

    [Fact]
    public void Validate_FlagsBadEncounterZoneFlag()
    {
        var spec = DenSpec();
        spec.EncounterZones[0].Flags = new() { "TotallyNotAFlag" };
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_DenZone") && p.Contains("invalid flag"));
    }

    [Fact]
    public void Validate_FlagsMinGreaterThanMax()
    {
        var spec = DenSpec();
        spec.EncounterZones[0].MinLevel = 20;
        spec.EncounterZones[0].MaxLevel = 10;   // real cap below min => invalid
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_DenZone") && p.Contains("minLevel") && p.Contains("maxLevel"));
    }

    [Fact]
    public void Validate_AllowsMinAboveZero_WhenMaxIsUncapped()
    {
        var spec = DenSpec();
        spec.EncounterZones[0].MinLevel = 20;
        spec.EncounterZones[0].MaxLevel = 0;    // uncapped => min<=max check skipped
        var problems = Generator.Validate(spec);
        Assert.DoesNotContain(problems, p => p.Contains("minLevel") && p.Contains("maxLevel"));
    }

    [Fact]
    public void Validate_FlagsUnresolvedCellZoneRef()
    {
        var spec = DenSpec();
        spec.Cells[0].EncounterZone = "MF_NoSuchZone";
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("MF_Den") && p.Contains("encounterZone") && p.Contains("unresolved"));
    }

    [Fact]
    public void Validate_FlagsUnresolvedSpawnZoneRef()
    {
        var spec = DenSpec();
        spec.Placements[0].EncounterZone = "MF_NoSuchZone";
        var problems = Generator.Validate(spec);
        Assert.Contains(problems, p => p.Contains("encounterZone") && p.Contains("unresolved"));
    }

    // DenSpec's only validation problem is its (deliberately invalid) raw LVLN placement base — proving
    // both that we flag the LVLN base and that a well-formed encounter (zone/faction/cell/spawn wiring)
    // adds no spurious validation noise. (The LVLN base models the common mistake the guardrail catches;
    // the correct idiom is an NPC_ whose template chain references the list.)
    [Fact]
    public void Validate_DenSpec_OnlyProblemIsTheLvlnBase()
    {
        var problems = Generator.Validate(DenSpec());
        Assert.Single(problems);
        Assert.Contains("MF_BanditList", problems[0]);
        Assert.Contains("LVLN", problems[0]);
    }
}
