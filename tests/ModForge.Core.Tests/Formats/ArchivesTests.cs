using ModForge;

namespace ModForge.Tests;

// Archives.cs (.bsa reading) had zero coverage until 2026-08-13 — there was no archive to test
// against, since Mutagen cannot write one and the repo ships no vanilla .bsa. TestBsa closes that.
//
// The extraction tests matter MORE than they look: a BSA stores its paths with backslashes, which
// is a separator on Windows and an ordinary filename character on Linux. Anything asserting on the
// resulting layout therefore has to assert on a real directory tree (Path.Combine with segments),
// never on a string with a hardcoded separator, or it silently passes on one machine and lies on
// the other. The repo is developed on both.
public class ArchivesTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "mf_bsa_" + Guid.NewGuid().ToString("N"));

    public ArchivesTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* temp dir; best effort */ }
    }

    private string Bsa(params TestBsa.Entry[] entries)
    {
        var path = Path.Combine(_dir, "Test.bsa");
        TestBsa.Write(path, entries);
        return path;
    }

    private string OutDir([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        => Path.Combine(_dir, "out_" + name);

    private static readonly TestBsa.Entry Voice =
        TestBsa.File("sound\\voice\\test.esp\\malenord", "hello.fuz", "FUZ-BODY");
    private static readonly TestBsa.Entry Texture =
        TestBsa.File("textures\\armor", "steel.txt", "TEX");

    // --- List --------------------------------------------------------------------------------

    [Fact]
    public void List_ReturnsEveryEntry_AsTheArchiveStoresIt()
    {
        var listed = Archives.List(Bsa(Voice, Texture));

        // List is a RAW listing — it reports whatever separator the reader used, which is the
        // archive's own backslash on Windows. Normalising here (rather than asserting backslashes)
        // keeps the test honest on Linux, where that is not a separator at all.
        Assert.Equal(
            new[] { "sound/voice/test.esp/malenord/hello.fuz", "textures/armor/steel.txt" },
            listed.Select(p => p.Replace('\\', '/')));
    }

    [Fact]
    public void List_OnAMissingArchive_IsEmpty_NotAThrow()
    {
        Assert.Empty(Archives.List(Path.Combine(_dir, "nope.bsa")));
    }

    // --- Extract -----------------------------------------------------------------------------

    [Fact]
    public void Extract_WritesARealDirectoryTree_OnEveryPlatform()
    {
        var outDir = OutDir();

        int count = Archives.Extract(Bsa(Voice, Texture), outDir);

        Assert.Equal(2, count);
        // Asserted as path SEGMENTS on purpose — see the class comment.
        Assert.True(File.Exists(Path.Combine(outDir, "sound", "voice", "test.esp", "malenord", "hello.fuz")));
        Assert.True(File.Exists(Path.Combine(outDir, "textures", "armor", "steel.txt")));
    }

    [Fact]
    public void Extract_RoundTripsContentByteForByte()
    {
        var outDir = OutDir();

        Archives.Extract(Bsa(Voice), outDir);

        Assert.Equal(Voice.Data,
            File.ReadAllBytes(Path.Combine(outDir, "sound", "voice", "test.esp", "malenord", "hello.fuz")));
    }

    [Fact]
    public void Extract_WithAFilter_TakesOnlyMatchingPaths()
    {
        var outDir = OutDir();

        int count = Archives.Extract(Bsa(Voice, Texture), outDir, "sound/voice");

        // The filter is matched against the archive path, so it has to survive separator style too.
        Assert.Equal(1, count);
        Assert.True(File.Exists(Path.Combine(outDir, "sound", "voice", "test.esp", "malenord", "hello.fuz")));
        Assert.False(Directory.Exists(Path.Combine(outDir, "textures")));
    }

    [Fact]
    public void Extract_FilterIsCaseInsensitive()
    {
        var outDir = OutDir();

        // The CLI lowercases the plugin name when it builds the filter, but archive paths are not
        // guaranteed lowercase, so the comparison must not care.
        Assert.Equal(1, Archives.Extract(Bsa(Voice, Texture), outDir, "MALENORD"));
    }

    [Fact]
    public void Extract_FilterMatchingNothing_WritesNothing_AndReportsZero()
    {
        var outDir = OutDir();

        // The callers treat 0 as "no clips for this voice type" and bail with a message.
        Assert.Equal(0, Archives.Extract(Bsa(Voice, Texture), outDir, "femaleeventoned"));
        Assert.False(Directory.Exists(outDir));
    }

    [Fact]
    public void Extract_OnAMissingArchive_IsZero_NotAThrow()
    {
        Assert.Equal(0, Archives.Extract(Path.Combine(_dir, "nope.bsa"), OutDir()));
    }

    [Fact]
    public void Extract_RejectsAnEntryThatWouldEscapeTheOutputDirectory()
    {
        var outDir = OutDir();
        var evil = Bsa(TestBsa.File("..\\..\\escape", "pwned.txt", "no"));

        // A .bsa is an untrusted input here — it is whatever mod the user pointed the CLI at.
        Assert.Throws<InvalidDataException>(() => Archives.Extract(evil, outDir));
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(outDir)!, "..", "escape", "pwned.txt")));
    }
}
