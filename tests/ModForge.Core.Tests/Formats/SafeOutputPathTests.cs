using ModForge;
using Xunit;

namespace ModForge.Tests;

public class SafeOutputPathTests
{
    [Fact]
    public void ResolveUnder_AllowsNestedRelativePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "modforge-safe-output");
        var result = SafeOutputPath.ResolveUnder(root, "Meshes/actors/test.hkx");
        Assert.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, result,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveUnder_AllowsFilesystemRootAsOutputRoot()
    {
        var root = Path.GetPathRoot(Path.GetFullPath(Path.GetTempPath()))!;
        var result = SafeOutputPath.ResolveUnder(root, "modforge-safe-output/test.hkx");
        Assert.StartsWith(root, result,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../outside.hkx")]
    [InlineData("nested/../../outside.hkx")]
    public void ResolveUnder_RejectsTraversal(string relativePath)
    {
        var root = Path.Combine(Path.GetTempPath(), "modforge-safe-output");
        Assert.Throws<InvalidDataException>(() => SafeOutputPath.ResolveUnder(root, relativePath));
    }
}
