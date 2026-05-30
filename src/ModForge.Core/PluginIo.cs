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
    public static void Write(ISkyrimMod mod, string outPath) =>
        mod.WriteToBinary(outPath, new BinaryWriteParameters { ModKey = ModKeyOption.NoCheck });
}
