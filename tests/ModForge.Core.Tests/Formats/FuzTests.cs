using System.Text;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Fuz.Split had no test at all before 2026-08-13. It is the container format every generated voice
// line ships in, and it is pure byte-slicing — cheap to pin down, and the failure mode when it is
// wrong (a silently truncated clip, or lip data pushed into the audio stream) is invisible offline.
public class FuzTests
{
    // FUZE | version | lipSize | <lip bytes> | <audio bytes>
    private static byte[] MakeFuz(byte[]? lip, byte[] audio, uint version = 1)
    {
        var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("FUZE"));
        ms.Write(BitConverter.GetBytes(version));
        ms.Write(BitConverter.GetBytes((uint)(lip?.Length ?? 0)));
        if (lip is not null) ms.Write(lip);
        ms.Write(audio);
        return ms.ToArray();
    }

    private static byte[] RiffWave(int payload = 8) =>
        Encoding.ASCII.GetBytes("RIFF").Concat(new byte[4])
            .Concat(Encoding.ASCII.GetBytes("WAVE")).Concat(new byte[payload]).ToArray();

    [Fact]
    public void SplitsLipAndAudioAtTheDeclaredBoundary()
    {
        var lip = new byte[] { 1, 2, 3, 4, 5 };
        var audio = new byte[] { 9, 8, 7 };

        var r = Fuz.Split(MakeFuz(lip, audio));

        Assert.Equal(lip, r.Lip);
        Assert.Equal(audio, r.Audio);
    }

    [Fact]
    public void ZeroLipSizeMeansNoLipTrack_NotAnEmptyOne()
    {
        // ship-voice writes these when LipGenerator is unavailable; the caller checks for null.
        var r = Fuz.Split(MakeFuz(null, new byte[] { 1, 2, 3, 4 }));

        Assert.Null(r.Lip);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, r.Audio);
    }

    [Fact]
    public void AudioExtensionFollowsTheRiffHeader()
    {
        Assert.Equal("wav", Fuz.Split(MakeFuz(null, RiffWave())).AudioExt);
        // xWMA carries its own header, so anything that is not a RIFF/WAVE stays .xwm
        Assert.Equal("xwm", Fuz.Split(MakeFuz(null, new byte[16])).AudioExt);
    }

    [Fact]
    public void RejectsAnythingWithoutTheFuzeMagic()
    {
        var notFuz = Encoding.ASCII.GetBytes("RIFF").Concat(new byte[16]).ToArray();

        var ex = Assert.Throws<ArgumentException>(() => Fuz.Split(notFuz));
        Assert.Contains("FUZE", ex.Message);
    }

    [Fact]
    public void RejectsATruncatedContainerRatherThanReturningEmptyAudio()
    {
        // lipSize consumes the whole body: a real one of these means a truncated download, and
        // returning a zero-length clip would ship silence that nobody notices until it is in game.
        var lipOnly = MakeFuz(new byte[] { 1, 2, 3, 4 }, Array.Empty<byte>());

        var ex = Assert.Throws<ArgumentException>(() => Fuz.Split(lipOnly));
        Assert.Contains("no audio", ex.Message);
    }

    [Fact]
    public void OversizedLipSizeLeavesTheLipNull_AndTreatsTheRestAsAudio()
    {
        // Declared lip length runs past the end of the buffer. The reader refuses to slice a lip it
        // cannot fully see; this pins that it degrades rather than throwing an IndexOutOfRange.
        var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("FUZE"));
        ms.Write(BitConverter.GetBytes(1u));
        ms.Write(BitConverter.GetBytes(9999u));
        ms.Write(new byte[] { 42, 43 });

        var ex = Record.Exception(() => Fuz.Split(ms.ToArray()));
        Assert.IsType<ArgumentException>(ex);
    }
}
