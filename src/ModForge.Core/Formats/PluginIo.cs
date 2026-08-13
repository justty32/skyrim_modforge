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
        // A plugin that references NO external form ends up MASTERLESS, which the Skyrim engine
        // silently refuses to load: no error, the records report as missing to console
        // help/setstage, and the plugin is dropped even when enabled in the load order
        // (in-game confirmed 2026-06-20 — a masterless ESL did not load; the identical content
        // mastering Skyrim.esm loaded and ran). Every real Skyrim plugin masters Skyrim.esm, so
        // when the build produced zero external references we add Skyrim.esm as the sole master
        // and write the masters list verbatim (NoCheck) — Mutagen still maps each record's
        // own-ModKey FormKey to the correct master index, so FormIDs stay correct. When external
        // refs DO exist we keep the default Iterate, letting Mutagen compute the exact master set.
        bool hasExternalRef = mod.EnumerateMajorRecords()
            .SelectMany(r => r.EnumerateFormLinks())
            .Any(l => !l.FormKey.IsNull && l.FormKey.ModKey != mod.ModKey);
        if (!hasExternalRef)
        {
            var skyrim = ModKey.FromNameAndExtension("Skyrim.esm");
            if (!mod.ModHeader.MasterReferences.Any(m => m.Master == skyrim))
                mod.ModHeader.MasterReferences.Add(new MasterReference { Master = skyrim });
        }

        mod.WriteToBinary(outPath, new BinaryWriteParameters
        {
            ModKey = ModKeyOption.NoCheck,
            MastersListContent = hasExternalRef
                ? MastersListContentOption.Iterate
                : MastersListContentOption.NoCheck,
        });

        // NOTE: a "NVNM parent-byte shift" post-pass once lived here. It was a mis-diagnosis made
        // under stale-ESP test conditions and has been removed. Mutagen already writes the
        // authoritative NVNM layout (Version | Magic | ParentWorldspace | Coords/Cell |
        // VertexCount | ...), so the bytes ship untouched. See docs/engine-internals.md
        // ("Programmatic navmesh") for the full story.
    }
}
