internal static partial class Program
{
    // Diagnostic: print a Quest's stages (QSDT index + log entries QLOG/CTDA + flags) and objectives
    // (QOBJ index + display text + targets), by FormID. This is the shape ModForge mirrors for
    // multi-stage quest progression — probe a vanilla quest (e.g. MS01 0x018B4B) to see how stages,
    // log entries (NNAM text), CompleteQuest flags, and objective↔target wiring look on a real record.
    private static int QuestDiag(string inPath, string formIdOrEditorId)
    {
        // Accept EITHER a hex FormID (0x000801 / 000801) OR an EditorID (MF_ErrandQuest), like `dump`.
        // A non-hex arg parses as an EditorID; a hex arg matches by FormID. (id=null => match by editorId.)
        uint? id = uint.TryParse(formIdOrEditorId.Replace("0x", "", StringComparison.OrdinalIgnoreCase),
            System.Globalization.NumberStyles.HexNumber, null, out var parsed) ? (parsed & 0xFFFFFF) : null;
        // Localized masters (Skyrim.esm) store DisplayText/Entry as BSA-packed string indices; point
        // the strings reader at the plugin's own Data folder so text resolves headless (same as Find).
        var dataDir = Path.GetDirectoryName(Path.GetFullPath(inPath))!;
        var readParams = new BinaryReadParameters
        {
            StringsParam = new StringsReadParameters
            {
                BsaFolderOverride = dataDir, StringsFolderOverride = dataDir, TargetLanguage = Language.English,
            },
        };
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE, readParams);
        string Txt(ITranslatedStringGetter? t) { try { return t?.String ?? ""; } catch { return "<localized>"; } }

        foreach (var q in mod.EnumerateMajorRecords<IQuestGetter>())
        {
            if (id is { } fid ? q.FormKey.ID != fid : !string.Equals(q.EditorID, formIdOrEditorId, StringComparison.OrdinalIgnoreCase)) continue;
            Console.WriteLine($"0x{q.FormKey.ID:X6}  EditorID={q.EditorID}  flags={q.Flags}  priority={q.Priority}  type={q.Type}  event={q.Event}  filter={q.Filter}");
            Console.WriteLine($"  Stages ({q.Stages.Count}):");
            foreach (var s in q.Stages.OrderBy(s => s.Index))
            {
                Console.WriteLine($"    stage[{s.Index}] flags={s.Flags}");
                foreach (var le in s.LogEntries)
                    Console.WriteLine($"      log: flags={le.Flags} conds={le.Conditions.Count} \"{Txt(le.Entry)}\"");
            }
            Console.WriteLine($"  Objectives ({q.Objectives.Count}):");
            foreach (var o in q.Objectives.OrderBy(o => o.Index))
            {
                Console.WriteLine($"    objective[{o.Index}] flags={o.Flags} \"{Txt(o.DisplayText)}\" targets={o.Targets.Count}");
                foreach (var t in o.Targets)
                    Console.WriteLine($"      target: flags={t.Flags} conds={t.Conditions.Count}");
            }
            return 0;
        }
        Console.WriteLine($"{formIdOrEditorId} not a Quest in {Path.GetFileName(inPath)}");
        return 0;
    }
}
