namespace ModForge;

/// <summary>
/// Writes a Skyrim <c>.seq</c> (sequence) file — the manifest of Start-Game-Enabled quests the
/// engine must FORCE-START when the plugin is loaded into an *existing* save.
///
/// Why it matters: a SGE quest's dialogue/scenes only load while the quest is RUNNING. On a NEW
/// game every SGE quest starts during init, so dialogue is there immediately. But when a plugin is
/// added to a save made before it existed, the SGE quest does NOT auto-start on the first load —
/// so its custom dialogue is missing until a save+reload kicks it. The <c>.seq</c> file is the
/// engine's "start these on load" list that closes that gap (this is why third-party followers like
/// Sofia ship one, and why ModForge dialogue needed a save/reload before). Must live at
/// <c>Data/Seq/&lt;pluginBaseName&gt;.seq</c>.
///
/// Format: a flat array of 4-byte little-endian quest FormIDs, nothing else.
///
/// FormID index byte: we write the plugin's on-disk self index (its master count) in the high byte
/// | the local id. The engine resolves a <c>.seq</c>'s FormIDs relative to the file's owning plugin
/// (identified by the filename) and remaps that self index to the runtime load order — which is what
/// makes a build-time-generated file load-order independent (the same basis the GenerateSEQ /
/// xEdit "Create SEQ File" tools use). If a corner case ever proves otherwise, regenerate with
/// xEdit's "Create SEQ File" under the actual load order.
/// </summary>
public static class SeqFile
{
    /// <summary>
    /// Write <c>&lt;dataDir&gt;/Seq/&lt;espBaseName&gt;.seq</c> listing every Start-Game-Enabled quest in
    /// the plugin at <paramref name="espPath"/>. Returns the FormKeys written (empty list ⇒ the
    /// plugin has no SGE quest, so no file is written).
    /// </summary>
    public static IReadOnlyList<FormKey> Write(string espPath, string dataDir)
    {
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(espPath), SkyrimRelease.SkyrimSE);
        var masterCount = (uint)mod.ModHeader.MasterReferences.Count;
        var sge = mod.Quests
                     .Where(q => q.Flags.HasFlag(Quest.Flag.StartGameEnabled))
                     .Select(q => q.FormKey)
                     .ToList();
        if (sge.Count == 0) return sge;

        var seqDir = Path.Combine(dataDir, "Seq");
        Directory.CreateDirectory(seqDir);
        var seqPath = Path.Combine(seqDir, Path.GetFileNameWithoutExtension(espPath) + ".seq");

        var bytes = new byte[sge.Count * 4];
        for (int i = 0; i < sge.Count; i++)
        {
            uint formId = (masterCount << 24) | (sge[i].ID & 0x00FFFFFF);
            BitConverter.GetBytes(formId).CopyTo(bytes, i * 4);   // host is little-endian; .seq is little-endian
        }
        File.WriteAllBytes(seqPath, bytes);
        return sge;
    }
}
