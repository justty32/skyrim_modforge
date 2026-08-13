using System.Text;

namespace ModForge.Tests;

// A minimal Skyrim SE (.bsa version 105) WRITER, for tests only.
//
// WHY THIS EXISTS: Mutagen 0.53.1 reads archives but cannot create them (Mutagen.Bethesda.Archives
// exposes CreateReader and nothing else), and this repo has no vanilla .bsa to lean on — the offline
// machine has no game install, and committing a real archive would be committing Bethesda assets.
// So Archives.cs was untestable until something could produce an archive. Same approach the other
// binary-format tests take (Vhgt / Vnml / NavmeshPatch / Fuz all build their bytes by hand).
//
// Only what Archives.cs actually exercises is implemented: uncompressed, directory + file names
// included, no embedded names. Do not grow this into a general BSA library — if a test ever needs
// compression or embedded names, that is the moment to ask whether the production code should own a
// writer instead.
internal static class TestBsa
{
    internal readonly record struct Entry(string Folder, string Name, byte[] Data);

    public static Entry File(string folder, string name, string text) =>
        new(folder, name, Encoding.UTF8.GetBytes(text));

    private const uint ArchiveFlags = 0x1 | 0x2;   // IncludeDirectoryNames | IncludeFileNames

    public static void Write(string path, params Entry[] entries)
    {
        // Preserve declaration order; a real archive is sorted by hash, but nothing in Archives.cs
        // looks a file up by hash — it enumerates — so sorting would only add unverifiable ceremony.
        var folders = entries.GroupBy(e => e.Folder, StringComparer.OrdinalIgnoreCase).ToList();

        int totalFolderNameLength = folders.Sum(f => f.Key.Length + 1);   // +1 for the trailing null
        int totalFileNameLength = entries.Sum(e => e.Name.Length + 1);

        const int headerSize = 36;
        int folderRecordsSize = 24 * folders.Count;                       // v105 folder record is 24 bytes
        int folderBlocksSize = folders.Sum(f => 1 + f.Key.Length + 1 + 16 * f.Count());
        int dataStart = headerSize + folderRecordsSize + folderBlocksSize + totalFileNameLength;

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.ASCII);

        w.Write(Encoding.ASCII.GetBytes("BSA\0"));
        w.Write(105u);                       // SSE
        w.Write((uint)headerSize);           // offset to the folder records
        w.Write(ArchiveFlags);
        w.Write((uint)folders.Count);
        w.Write((uint)entries.Length);
        w.Write((uint)totalFolderNameLength);
        w.Write((uint)totalFileNameLength);
        w.Write(0u);                         // fileFlags — content-type bitfield, unread here

        // --- folder records -----------------------------------------------------------------
        // Each points at its own name+file-record block. The stored value is biased by
        // totalFileNameLength, which readers subtract back off (a quirk of the format).
        int blockOffset = headerSize + folderRecordsSize;
        foreach (var f in folders)
        {
            w.Write(Hash.Folder(f.Key));
            w.Write((uint)f.Count());
            w.Write(0u);                                     // padding (v105 only)
            w.Write((ulong)(blockOffset + totalFileNameLength));
            blockOffset += 1 + f.Key.Length + 1 + 16 * f.Count();
        }

        // --- folder name + file record blocks -------------------------------------------------
        int dataOffset = dataStart;
        foreach (var f in folders)
        {
            var name = f.Key;
            w.Write((byte)(name.Length + 1));                // bzstring: length INCLUDES the null
            w.Write(Encoding.ASCII.GetBytes(name));
            w.Write((byte)0);

            foreach (var e in f)
            {
                w.Write(Hash.FileName(e.Name));
                w.Write((uint)e.Data.Length);                // no compression bit — archive is raw
                w.Write((uint)dataOffset);
                dataOffset += e.Data.Length;
            }
        }

        // --- file name block: every name, null-terminated, in file-record order ---------------
        foreach (var e in entries)
        {
            w.Write(Encoding.ASCII.GetBytes(e.Name));
            w.Write((byte)0);
        }

        foreach (var e in entries) w.Write(e.Data);

        w.Flush();
        System.IO.File.WriteAllBytes(path, ms.ToArray());
    }

    // Bethesda's archive hash. Archives.cs never looks a file up by hash, so this only has to be
    // self-consistent — but a wrong hash would be a landmine for any future test that does, so it
    // is the real algorithm. The .nif/.kf/.dds/.wav extension tweak is deliberately omitted: it
    // only perturbs those four extensions and no test uses them.
    private static class Hash
    {
        public static ulong Folder(string folder) => Gen(Normalize(folder), "");

        public static ulong FileName(string name)
        {
            var lower = Normalize(name);
            var ext = Path.GetExtension(lower);
            return Gen(Path.GetFileNameWithoutExtension(lower), ext);
        }

        private static string Normalize(string s) => s.ToLowerInvariant().Replace('/', '\\');

        private static uint Part(string s)
        {
            uint h = 0;
            foreach (char c in s) h = (h * 0x1003f) + (byte)c;
            return h;
        }

        private static ulong Gen(string stem, string ext)
        {
            ulong hash = 0;
            if (stem.Length > 0)
                hash = (byte)stem[^1]
                     | (stem.Length > 2 ? (ulong)(byte)stem[^2] << 8 : 0ul)
                     | ((ulong)stem.Length << 16)
                     | ((ulong)(byte)stem[0] << 24);
            if (stem.Length > 3)
                hash += (ulong)Part(stem[1..^2]) << 32;
            if (ext.Length > 0)
                hash += (ulong)Part(ext) << 32;
            return hash;
        }
    }
}
