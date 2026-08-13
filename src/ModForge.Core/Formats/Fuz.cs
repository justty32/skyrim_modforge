using System.Text;

namespace ModForge;

public static class Fuz
{
    public record FuzSplitResult(byte[]? Lip, byte[] Audio, string AudioExt);

    /// <summary>
    /// Splits a .fuz container into its constituent .lip and audio (.xwm/.wav) parts.
    /// </summary>
    public static FuzSplitResult Split(byte[] fuzData)
    {
        if (fuzData.Length < 12 || Encoding.ASCII.GetString(fuzData, 0, 4) != "FUZE")
            throw new ArgumentException("Not a valid .fuz file (missing FUZE magic)");

        uint version = BitConverter.ToUInt32(fuzData, 4);
        uint lipSize = BitConverter.ToUInt32(fuzData, 8);

        byte[]? lip = null;
        if (lipSize > 0 && fuzData.Length >= 12 + lipSize)
        {
            lip = new byte[lipSize];
            Array.Copy(fuzData, 12, lip, 0, lipSize);
        }

        int audioOffset = (int)(12 + lipSize);
        int audioSize = fuzData.Length - audioOffset;
        if (audioSize <= 0) throw new ArgumentException(".fuz file contains no audio data");

        byte[] audio = new byte[audioSize];
        Array.Copy(fuzData, audioOffset, audio, 0, audioSize);

        // Detect extension: xWMA starts with 'RIFF' and then 'WAVE' with format 'XWMA' (0x161)
        // or just 'RIFF' ... 'WAVE' ...
        // Actually, ffmpeg doesn't care about the extension much, but .xwm is typical.
        string ext = "xwm";
        if (audio.Length > 12 && Encoding.ASCII.GetString(audio, 8, 4) == "WAVE")
            ext = "wav";

        return new FuzSplitResult(lip, audio, ext);
    }
}
