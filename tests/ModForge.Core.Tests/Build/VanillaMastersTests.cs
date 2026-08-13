using ModForge;
using Xunit;

namespace ModForge.Tests;

// The build's file-system boundary, now testable on its own (Build/VanillaMasters.cs).
// Before it was extracted, the only way to observe any of this was to run a whole build and
// read the warnings list — these assertions had no reachable seam at all.
public class VanillaMastersTests
{
    [Fact]
    public void MissingDataFolder_WarnsOncePerMaster_AndCachesTheMiss()
    {
        var warnings = new List<string>();
        using var masters = new VanillaMasters(
            Path.Combine(Path.GetTempPath(), "modforge-no-such-data-dir"), warnings.Add);

        Assert.Null(masters.Cache("Skyrim.esm"));
        Assert.Null(masters.Cache("Skyrim.esm"));   // second ask must be served from the cache

        var warning = Assert.Single(warnings);
        Assert.Contains("Skyrim.esm", warning);
        Assert.Contains("MODFORGE_SKYRIM_DATA", warning);
    }

    [Fact]
    public void EachMissingMaster_GetsItsOwnWarning()
    {
        var warnings = new List<string>();
        using var masters = new VanillaMasters(
            Path.Combine(Path.GetTempPath(), "modforge-no-such-data-dir"), warnings.Add);

        masters.Cache("Skyrim.esm");
        masters.Cache("Dawnguard.esm");

        Assert.Equal(2, warnings.Count);
        Assert.Contains(warnings, w => w.Contains("Dawnguard.esm"));
    }

    [Fact]
    public void MasterNamesAreCaseInsensitive()
    {
        var warnings = new List<string>();
        using var masters = new VanillaMasters(
            Path.Combine(Path.GetTempPath(), "modforge-no-such-data-dir"), warnings.Add);

        masters.Cache("Skyrim.esm");
        masters.Cache("SKYRIM.ESM");

        // One warning, not two: load order is case-insensitive, and so is this cache.
        Assert.Single(warnings);
    }

    [Fact]
    public void DisposeIsSafeWithNothingOpen()
    {
        var masters = new VanillaMasters(
            Path.Combine(Path.GetTempPath(), "modforge-no-such-data-dir"), _ => { });
        masters.Cache("Skyrim.esm");   // resolved to null, so nothing was opened
        masters.Dispose();
        masters.Dispose();             // Finish() disposes in a finally; a double call must not throw
    }
}
