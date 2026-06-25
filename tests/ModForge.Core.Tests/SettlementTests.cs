using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Idea #22 — the `settlements:` population macro expands a compact "populated settlement" into the
// low-level records every build pass already handles: per-resident ACHR placement + schedule packages
// bound to anchor refs + faction membership + (optional) vendor FACT/chest. MVP = named residents +
// static ACHR (deterministic, offline-verifiable). No new record type, no runtime script.
public class SettlementTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    // Two residents in one settlement: a smith (work + vendor) and a cook (routine override). Anchors
    // (home/work/spawn) are placed XMarkerHeading refs the macro binds packages to.
    private static ModSpec TownSpec() => new()
    {
        PluginName = "MFTown.esp",
        Npcs =
        {
            new NpcSpec { EditorId = "Smith", Name = "Brelin" },
            new NpcSpec { EditorId = "Cook",  Name = "Millie" },
        },
        Placements =
        {
            new PlacementSpec { EditorId = "SmithBed",   Base = "Skyrim.esm:0x00000034", Cell = "Skyrim.esm:0x01605E", Position = new Vec3 { X = 1, Y = 2, Z = 3 } },
            new PlacementSpec { EditorId = "SmithForge", Base = "Skyrim.esm:0x00000034", Cell = "Skyrim.esm:0x01605E", Position = new Vec3 { X = 4, Y = 5, Z = 6 } },
            new PlacementSpec { EditorId = "SmithSpawn", Base = "Skyrim.esm:0x00000034", Cell = "Skyrim.esm:0x01605E", Position = new Vec3 { X = 7, Y = 8, Z = 9 } },
            new PlacementSpec { EditorId = "CookBed",    Base = "Skyrim.esm:0x00000034", Cell = "Skyrim.esm:0x01605E", Position = new Vec3 { X = 10, Y = 11, Z = 12 } },
            new PlacementSpec { EditorId = "CookSpawn",  Base = "Skyrim.esm:0x00000034", Cell = "Skyrim.esm:0x01605E", Position = new Vec3 { X = 13, Y = 14, Z = 15 } },
        },
        Settlements =
        {
            new SettlementSpec
            {
                EditorId = "Town", Cell = "Skyrim.esm:0x01605E", CrimeFaction = "Skyrim.esm:0x0267EA",
                DailyRoutine = new RoutineSpec
                {
                    Sleep = new RoutineWindowSpec { From = 22, To = 7 },
                    Work = new RoutineWindowSpec { From = 8, To = 18 },
                },
                Residents =
                {
                    new ResidentSpec
                    {
                        Npc = "Smith", Home = "SmithBed", Work = "SmithForge", SpawnAt = "SmithSpawn",
                        Vendor = new SettlementVendorSpec { SellBuyList = "Skyrim.esm:0x06CB48", StartHour = 9, EndHour = 18, Gold = 500 },
                    },
                    // No work anchor; routine override drops sleep an hour earlier.
                    new ResidentSpec
                    {
                        Npc = "Cook", Home = "CookBed", SpawnAt = "CookSpawn",
                        Routine = new RoutineSpec { Sleep = new RoutineWindowSpec { From = 21, To = 6 } },
                    },
                },
            },
        },
    };

    [Fact]
    public void Valid_NoProblems()
    {
        Assert.Empty(Validate(TownSpec()));
    }

    [Fact]
    public void Expand_PlacesResidentAchrAtSpawnMarkerCoords()
    {
        var s = TownSpec();
        Generator.ExpandSettlements(s);
        var smith = s.Placements.Single(p => p.EditorId == "Town_SmithRef");
        Assert.Equal("Smith", smith.Base);
        Assert.Equal("Skyrim.esm:0x01605E", smith.Cell);
        Assert.Equal(7f, smith.Position.X);   // SmithSpawn marker coords
        Assert.Equal(9f, smith.Position.Z);
    }

    [Fact]
    public void Expand_SpawnPositionFallbackWhenNoMarker()
    {
        var s = new ModSpec
        {
            PluginName = "MF.esp",
            Npcs = { new NpcSpec { EditorId = "N" } },
            Settlements =
            {
                new SettlementSpec
                {
                    EditorId = "T", Cell = "Skyrim.esm:0x01605E",
                    Residents = { new ResidentSpec { Npc = "N", SpawnPosition = new Vec3 { X = 100, Y = 200, Z = 300 } } },
                },
            },
        };
        Generator.ExpandSettlements(s);
        var achr = s.Placements.Single(p => p.EditorId == "T_NRef");
        Assert.Equal(200f, achr.Position.Y);
    }

    [Fact]
    public void Expand_BuildsScheduledPackagesWithAnchorsAndWrappingDuration()
    {
        var s = TownSpec();
        Generator.ExpandSettlements(s);
        var sleep = s.Packages.Single(p => p.EditorId == "Town_Smith_Sleep");
        Assert.Equal(Generator.SleepTemplateRef, sleep.Template);
        Assert.Equal(22, sleep.Schedule.Hour);
        Assert.Equal(540, sleep.Schedule.DurationInMinutes);   // 22->7 wraps midnight = 9h
        Assert.Equal("SmithBed", sleep.Sleep.Location);

        var work = s.Packages.Single(p => p.EditorId == "Town_Smith_Work");
        Assert.Equal(Generator.SandboxTemplateRef, work.Template);
        Assert.Equal(8, work.Schedule.Hour);
        Assert.Equal(600, work.Schedule.DurationInMinutes);    // 8->18 = 10h
        Assert.Equal("SmithForge", work.Sandbox.Location);
    }

    [Fact]
    public void Expand_AlwaysOnWanderPackageHasNoScheduleAndIsLast()
    {
        var s = TownSpec();
        Generator.ExpandSettlements(s);
        var wander = s.Packages.Single(p => p.EditorId == "Town_Smith_Wander");
        Assert.Equal(Generator.SandboxTemplateRef, wander.Template);
        Assert.Equal(-1, wander.Schedule.Hour);                // no schedule = always-on fallback
        Assert.Equal("SmithSpawn", wander.Sandbox.Location);
        // npc.Packages: scheduled (by hour: work@8 before sleep@22) then wander last.
        var smith = s.Npcs.Single(n => n.EditorId == "Smith");
        Assert.Equal(new[] { "Town_Smith_Work", "Town_Smith_Sleep", "Town_Smith_Wander" }, smith.Packages.ToArray());
    }

    [Fact]
    public void Expand_ResidentRoutineOverridesSettlementDefault_AndNoWorkPackageWithoutAnchor()
    {
        var s = TownSpec();
        Generator.ExpandSettlements(s);
        // Cook overrode sleep to 21:00; inherits no work anchor -> no work package.
        var sleep = s.Packages.Single(p => p.EditorId == "Town_Cook_Sleep");
        Assert.Equal(21, sleep.Schedule.Hour);
        Assert.DoesNotContain(s.Packages, p => p.EditorId == "Town_Cook_Work");
        var cook = s.Npcs.Single(n => n.EditorId == "Cook");
        Assert.Equal(new[] { "Town_Cook_Sleep", "Town_Cook_Wander" }, cook.Packages.ToArray());
    }

    [Fact]
    public void Expand_AutoCreatesSettlementFaction_JoinsResidents_AppliesCrimeFaction()
    {
        var s = TownSpec();
        Generator.ExpandSettlements(s);
        Assert.Contains(s.Factions, f => f.EditorId == "Town_Faction");
        foreach (var ed in new[] { "Smith", "Cook" })
        {
            var npc = s.Npcs.Single(n => n.EditorId == ed);
            Assert.Contains("Town_Faction", npc.Factions);
            Assert.Equal("Skyrim.esm:0x0267EA", npc.CrimeFaction);
        }
    }

    [Fact]
    public void Expand_ReusesExplicitSettlementFaction()
    {
        var s = TownSpec();
        s.Settlements[0].SettlementFaction = "Skyrim.esm:0x000001";
        Generator.ExpandSettlements(s);
        Assert.DoesNotContain(s.Factions, f => f.EditorId == "Town_Faction");
        Assert.Contains("Skyrim.esm:0x000001", s.Npcs.Single(n => n.EditorId == "Smith").Factions);
    }

    [Fact]
    public void Expand_Vendor_BuildsMerchantFactionChestAndJoinsResident()
    {
        var s = TownSpec();
        Generator.ExpandSettlements(s);
        var fact = s.Factions.Single(f => f.EditorId == "Town_Smith_Merchant");
        Assert.NotNull(fact.Vendor);
        Assert.Equal("Town_Smith_MerchantChestRef", fact.Vendor!.MerchantContainer);
        Assert.Equal((ushort)9, fact.Vendor.StartHour);
        var chest = s.Containers.Single(c => c.EditorId == "Town_Smith_MerchantChest");
        Assert.Equal(Generator.GoldRef, chest.Items.Single().Item);
        Assert.Equal(500, chest.Items.Single().Count);
        Assert.Contains(s.Placements, p => p.EditorId == "Town_Smith_MerchantChestRef" && p.Persistent);
        Assert.Contains("Town_Smith_Merchant", s.Npcs.Single(n => n.EditorId == "Smith").Factions);
        // The cook is not a vendor.
        Assert.DoesNotContain(s.Factions, f => f.EditorId == "Town_Cook_Merchant");
    }

    [Fact]
    public void Expand_FriendlyResidents_OffByDefault_OnGeneratesPairwiseRela()
    {
        var off = TownSpec();
        Generator.ExpandSettlements(off);
        Assert.Empty(off.Relationships);

        var on = TownSpec();
        on.Settlements[0].FriendlyResidents = true;
        Generator.ExpandSettlements(on);
        var rela = Assert.Single(on.Relationships);   // 2 residents -> 1 pair
        Assert.Equal("Smith", rela.Parent);
        Assert.Equal("Cook", rela.Child);
        Assert.Equal("Friend", rela.Rank);
    }

    [Fact]
    public void Expand_IsIdempotent()
    {
        var s = TownSpec();
        Generator.ExpandSettlements(s);
        var packages = s.Packages.Count;
        Generator.ExpandSettlements(s);
        Assert.Equal(packages, s.Packages.Count);
    }

    [Fact]
    public void Build_SleepLocationResolvesToInSpecBedAnchor()
    {
        // Regression: sandbox/sleep location slots are deferred so an IN-SPEC placement anchor
        // (registered only in the placement loop) resolves instead of falling back to NearSelf.
        var s = TownSpec();
        var result = Generator.Build(s, ModKey.FromNameAndExtension("MFTown.esp"));
        var sleepPkg = result.Mod.Packages.Single(p => p.EditorID == "Town_Smith_Sleep");
        var loc = sleepPkg.Data[0] as IPackageDataLocationGetter;
        Assert.NotNull(loc);
        var target = loc!.Location?.Target as ILocationTargetGetter;
        Assert.NotNull(target);   // a concrete ref target, NOT a NearSelf LocationFallback
    }

    // --- validation ---

    [Fact]
    public void Validate_RejectsResidentNpcNotInSpec()
    {
        var s = TownSpec();
        s.Settlements[0].Residents[0].Npc = "Ghost";
        Assert.Contains(Validate(s), p => p.Contains("Ghost") && p.Contains("in-spec npcs[]"));
    }

    [Fact]
    public void Validate_RequiresSpawnPoint()
    {
        var s = TownSpec();
        s.Settlements[0].Residents[1].SpawnAt = "";
        s.Settlements[0].Residents[1].SpawnPosition = null;
        Assert.Contains(Validate(s), p => p.Contains("needs a spawn point"));
    }

    [Fact]
    public void Validate_SleepWindowNeedsHomeAnchor()
    {
        var s = TownSpec();
        s.Settlements[0].Residents[0].Home = "";
        Assert.Contains(Validate(s), p => p.Contains("sleep window but no `home`"));
    }

    [Fact]
    public void Validate_RejectsUnknownAnchorRef()
    {
        var s = TownSpec();
        s.Settlements[0].Residents[0].Work = "Nowhere";
        Assert.Contains(Validate(s), p => p.Contains("Nowhere") && p.Contains("not a placed ref"));
    }

    [Fact]
    public void Validate_RejectsDuplicateResidentAndBadVendorHours()
    {
        var s = TownSpec();
        s.Settlements[0].Residents[1].Npc = "Smith";              // duplicate
        s.Settlements[0].Residents[0].Vendor!.EndHour = 30;       // out of range
        var problems = Validate(s);
        Assert.Contains(problems, p => p.Contains("duplicate resident npc"));
        Assert.Contains(problems, p => p.Contains("vendor.endHour"));
    }

    [Fact]
    public void Validate_RejectsMissingCellAndNoResidents()
    {
        var s = new ModSpec
        {
            PluginName = "MF.esp",
            Settlements = { new SettlementSpec { EditorId = "Empty" } },
        };
        var problems = Validate(s);
        Assert.Contains(problems, p => p.Contains("missing cell"));
        Assert.Contains(problems, p => p.Contains("no residents"));
    }
}
