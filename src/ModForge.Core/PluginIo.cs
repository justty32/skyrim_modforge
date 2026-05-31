namespace ModForge;

/// <summary>Read/write helpers for Skyrim plugins (the byte layer Mutagen owns).</summary>
public static class PluginIo
{
    /// <summary>Load a plugin fully into memory (mutable) for inspection or string-apply.</summary>
    public static ISkyrimMod Load(string path) =>
        SkyrimMod.CreateFromBinary(new ModPath(path), SkyrimRelease.SkyrimSE);

    /// <summary>
    /// Write a mod to disk. Uses <see cref="ModKeyOption.NoCheck"/> so the output filename may
    /// differ from the mod's ModKey (we never want the alignment check to abort a write).
    /// </summary>
    public static void Write(ISkyrimMod mod, string outPath)
    {
        // An ESL (Small master) may hold at most 2048 records — FormIDs 0x800–0xFFF. Mutagen's
        // compaction check throws a raw FormIDCompactionOutOfBoundsException at write; pre-empt it
        // with an actionable message (a generated spec can grow large without the author noticing).
        if (mod.IsSmallMaster)
        {
            int count = mod.EnumerateMajorRecords().Count();
            if (count > 2048)
                throw new InvalidOperationException(
                    $"ESL plugin has {count} records but the light-master limit is 2048 (FormIDs 0x800–0xFFF). " +
                    "Set \"esl\": false in the spec, or split the content across multiple plugins.");
        }
        mod.WriteToBinary(outPath, new BinaryWriteParameters { ModKey = ModKeyOption.NoCheck });
    }
}
