using System.Linq;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class ValidateSpidTests
{
    private static string[] Validate(ModSpec s) => Generator.Validate(s).ToArray();

    [Fact]
    public void ValidSpec_NoSpidProblems()
    {
        var s = new ModSpec
        {
            SpidDistributions =
            {
                new SpidDistributionSpec
                {
                    File = "MyMod",
                    Entries = { new SpidEntrySpec { Type = "Perk", Record = "0xCF788~Skyrim.esm", StringFilters = { "ActorTypeNPC" } } },
                },
            },
        };
        Assert.Empty(Validate(s).Where(p => p.Contains("spidDistribution")));
    }

    [Fact]
    public void EmptyFile_Reported()
    {
        var s = new ModSpec { SpidDistributions = { new SpidDistributionSpec { File = "" } } };
        Assert.Contains(Validate(s), p => p.Contains("spidDistribution has empty 'file'"));
    }

    [Fact]
    public void UnknownType_Reported()
    {
        var s = new ModSpec
        {
            SpidDistributions = { new SpidDistributionSpec { File = "m",
                Entries = { new SpidEntrySpec { Type = "Banana", Record = "X" } } } },
        };
        Assert.Contains(Validate(s), p => p.Contains("unknown type 'Banana'"));
    }

    [Fact]
    public void EmptyRecord_Reported()
    {
        var s = new ModSpec
        {
            SpidDistributions = { new SpidDistributionSpec { File = "m",
                Entries = { new SpidEntrySpec { Type = "Faction", Record = "" } } } },
        };
        Assert.Contains(Validate(s), p => p.Contains("RecordID is required"));
    }

    [Fact]
    public void ChanceOutOfRange_Reported()
    {
        var s = new ModSpec
        {
            SpidDistributions = { new SpidDistributionSpec { File = "m",
                Entries = { new SpidEntrySpec { Type = "Spell", Record = "X", Chance = 150 } } } },
        };
        Assert.Contains(Validate(s), p => p.Contains("chance 150 out of range"));
    }
}
