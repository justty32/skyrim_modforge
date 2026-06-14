using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;

internal static partial class Program
{
    // gamedata <plugin> <outDir> — bulk-extract a plugin's human-readable content into one folder,
    // for AI agents to reference (plot/lore discussion, mod survey). Memory-safe: lazy read-only
    // OVERLAY, streamed (no full materialize, no .ToList of a record group), so it runs on the
    // 250 MB Skyrim.esm without blowing up. Localized masters (Skyrim/DLC) need their .STRINGS:
    // we scan EVERY .bsa beside the plugin for "<base>_english.*" and open the overlay pointed at a
    // BSA-free temp Strings/ folder; a non-localized mod (inline strings) is opened as-is.
    //
    // Output files (one walk each; dialogue uses the typed DIAL group for topic grouping):
    //   books.md  dialogue.md  quests.md  npcs.tsv  items.tsv  locations.tsv  magic.tsv  summary.txt
    private static int GameData(string pluginPath, string outDir, string? stringsOverride = null)
    {
        pluginPath = Path.GetFullPath(pluginPath);
        if (!File.Exists(pluginPath)) { Console.Error.WriteLine($"  ! not found: {pluginPath}"); return 1; }
        Directory.CreateDirectory(outDir);
        var dataDir = Path.GetDirectoryName(pluginPath) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(pluginPath);

        // Decide string strategy from a cheap first overlay.
        BinaryReadParameters prm = BinaryReadParameters.Default;
        bool localized;
        using (var probe = SkyrimMod.CreateFromBinaryOverlay(new ModPath(pluginPath), SkyrimRelease.SkyrimSE))
            localized = probe.UsingLocalization;
        if (localized)
        {
            // --strings <dir>: use a caller-supplied STRINGS folder (e.g. a Chinese 漢化 dir with
            // <base>_English.* symlinked to the localized strings) instead of auto-provisioning English
            // from a BSA. Lets us extract vanilla content in any language; default path unchanged.
            var stringsDir = stringsOverride ?? ProvisionEnglishStringsAnyBsa(dataDir, baseName);
            if (stringsDir is not null)
                prm = BinaryReadParameters.Default with
                {
                    StringsParam = new StringsReadParameters
                    {
                        TargetLanguage = Language.English,
                        StringsFolderOverride = stringsDir,
                        BsaFolderOverride = stringsDir,   // BSA-free dir → no load-order archive scan
                    },
                };
            else
                Console.Error.WriteLine($"  ! {baseName} is localized but no <base>_english.* found in any .bsa beside it — names/text may be blank");
        }

        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(pluginPath), SkyrimRelease.SkyrimSE, prm);
        string Str(Mutagen.Bethesda.Strings.ITranslatedStringGetter? s) { try { return s?.String ?? ""; } catch { return "<unresolved>"; } }

        long nBook = 0, nInfo = 0, nQuest = 0, nNpc = 0, nItem = 0, nLoc = 0, nMagic = 0;

        // --- Dialogue: typed DIAL group keeps topic→INFO grouping (prompt, quest, speaker gate). ---
        using (var w = new StreamWriter(Path.Combine(outDir, "dialogue.md")))
        {
            w.WriteLine($"# Dialogue — {baseName}\n");
            foreach (var topic in mod.DialogTopics.Records)
            {
                var prompt = Str(topic.Name);
                var infos = topic.Responses;
                if (infos.Count == 0 && prompt.Length == 0) continue;
                w.WriteLine($"## {topic.FormKey.ID:X6} {topic.EditorID}  [{topic.Category}/{topic.Subtype}]"
                    + (prompt.Length > 0 ? $"  prompt=\"{prompt}\"" : ""));
                foreach (var info in infos)
                {
                    // Speaker gate (GetIsID) — the usual "only this NPC says it".
                    string spk = "";
                    foreach (var c in info.Conditions)
                    {
                        var data = (c as IConditionFloatGetter)?.Data;
                        if (data is IGetIsIDConditionDataGetter g && !g.Object.Link.FormKey.IsNull)
                        { spk = $"  (speaker {g.Object.Link.FormKey.ID:X6})"; break; }
                    }
                    foreach (var resp in info.Responses)
                    {
                        var line = Str(resp.Text);
                        if (line.Length == 0) continue;
                        w.WriteLine($"- {line}{spk}");
                        spk = "";
                        nInfo++;
                    }
                }
                w.WriteLine();
            }
        }

        // --- Everything else in ONE streamed major-record pass. ---
        using var wBook  = new StreamWriter(Path.Combine(outDir, "books.md"));
        using var wQuest = new StreamWriter(Path.Combine(outDir, "quests.md"));
        using var wNpc   = new StreamWriter(Path.Combine(outDir, "npcs.tsv"));
        using var wItem  = new StreamWriter(Path.Combine(outDir, "items.tsv"));
        using var wLoc   = new StreamWriter(Path.Combine(outDir, "locations.tsv"));
        using var wMagic = new StreamWriter(Path.Combine(outDir, "magic.tsv"));
        wBook.WriteLine($"# Books — {baseName}\n");
        wQuest.WriteLine($"# Quests — {baseName}\n");
        wNpc.WriteLine("formid\teditorid\tname");
        wItem.WriteLine("formid\teditorid\ttype\tname");
        wLoc.WriteLine("formid\teditorid\ttype\tname");
        wMagic.WriteLine("formid\teditorid\ttype\tname");

        foreach (var r in mod.EnumerateMajorRecords())
        {
            string id = r.FormKey.ID.ToString("X6");
            switch (r)
            {
                case IBookGetter bk:
                    wBook.WriteLine($"## {id}  {bk.EditorID}  \"{Str(bk.Name)}\"");
                    wBook.WriteLine("```");
                    wBook.WriteLine(Str(bk.BookText));
                    wBook.WriteLine("```\n");
                    nBook++;
                    break;
                case IQuestGetter q:
                    wQuest.WriteLine($"## {id}  {q.EditorID}  \"{Str(q.Name)}\"");
                    foreach (var st in q.Stages)
                        foreach (var le in st.LogEntries)
                        {
                            var txt = Str(le.Entry);
                            if (txt.Length > 0) wQuest.WriteLine($"- [stage {st.Index}] {txt}");
                        }
                    foreach (var ob in q.Objectives)
                    {
                        var txt = Str(ob.DisplayText);
                        if (txt.Length > 0) wQuest.WriteLine($"- [obj {ob.Index}] {txt}");
                    }
                    wQuest.WriteLine();
                    nQuest++;
                    break;
                case INpcGetter npc:
                    wNpc.WriteLine($"{id}\t{npc.EditorID}\t{Str(npc.Name)}");
                    nNpc++;
                    break;
                case ICellGetter cell when !string.IsNullOrEmpty(Str(cell.Name)) || !string.IsNullOrEmpty(cell.EditorID):
                    wLoc.WriteLine($"{id}\t{cell.EditorID}\tCELL\t{Str(cell.Name)}"); nLoc++; break;
                case IWorldspaceGetter wrld:
                    wLoc.WriteLine($"{id}\t{wrld.EditorID}\tWRLD\t{Str(wrld.Name)}"); nLoc++; break;
                case ILocationGetter loc:
                    wLoc.WriteLine($"{id}\t{loc.EditorID}\tLCTN\t{Str(loc.Name)}"); nLoc++; break;
                case ISpellGetter sp:
                    wMagic.WriteLine($"{id}\t{sp.EditorID}\tSPEL\t{Str(sp.Name)}"); nMagic++; break;
                case IShoutGetter sh:
                    wMagic.WriteLine($"{id}\t{sh.EditorID}\tSHOU\t{Str(sh.Name)}"); nMagic++; break;
                case IScrollGetter sc:
                    wMagic.WriteLine($"{id}\t{sc.EditorID}\tSCRL\t{Str(sc.Name)}"); nMagic++; break;
                case IMagicEffectGetter mg:
                    wMagic.WriteLine($"{id}\t{mg.EditorID}\tMGEF\t{Str(mg.Name)}"); nMagic++; break;
                case IWeaponGetter wp:
                    wItem.WriteLine($"{id}\t{wp.EditorID}\tWEAP\t{Str(wp.Name)}"); nItem++; break;
                case IArmorGetter ar:
                    wItem.WriteLine($"{id}\t{ar.EditorID}\tARMO\t{Str(ar.Name)}"); nItem++; break;
                case IIngestibleGetter ing:
                    wItem.WriteLine($"{id}\t{ing.EditorID}\tALCH\t{Str(ing.Name)}"); nItem++; break;
                case IIngredientGetter ig:
                    wItem.WriteLine($"{id}\t{ig.EditorID}\tINGR\t{Str(ig.Name)}"); nItem++; break;
                case IMiscItemGetter mi:
                    wItem.WriteLine($"{id}\t{mi.EditorID}\tMISC\t{Str(mi.Name)}"); nItem++; break;
            }
        }

        File.WriteAllText(Path.Combine(outDir, "summary.txt"),
            $"{baseName}  localized={localized}\n" +
            $"books={nBook} dialogue_lines={nInfo} quests={nQuest} npcs={nNpc} items={nItem} locations={nLoc} magic={nMagic}\n");
        Console.WriteLine($"{baseName}: books={nBook} dialogue_lines={nInfo} quests={nQuest} npcs={nNpc} items={nItem} loc={nLoc} magic={nMagic} -> {outDir}");
        return 0;
    }

    // Generalized strings provisioner: scan EVERY .bsa in dataDir for "strings/<base>_english.*" and
    // extract them to a per-base BSA-free temp folder, named in ModKey case (Linux is case-sensitive).
    // Returns the folder, or null if no matching strings were found in any archive.
    private static string? ProvisionEnglishStringsAnyBsa(string dataDir, string baseName)
    {
        var dir = Path.Combine(Path.GetTempPath(), "modforge-gamedata-strings", baseName, "Strings");
        Directory.CreateDirectory(dir);
        if (File.Exists(Path.Combine(dir, $"{baseName}_English.STRINGS"))) return dir;
        var want = $"strings/{baseName.ToLowerInvariant()}_english.";
        bool any = false;
        foreach (var bsa in Directory.EnumerateFiles(dataDir, "*.bsa"))
        {
            try
            {
                foreach (var f in Archive.CreateReader(GameRelease.SkyrimSE, bsa).Files)
                {
                    var p = f.Path.Replace('\\', '/');
                    if (!p.StartsWith(want, StringComparison.OrdinalIgnoreCase)) continue;
                    var ext = Path.GetExtension(p).ToUpperInvariant();
                    File.WriteAllBytes(Path.Combine(dir, $"{baseName}_English{ext}"), f.GetSpan().ToArray());
                    any = true;
                }
            }
            catch { /* unreadable archive — skip */ }
        }
        return any ? dir : null;
    }
}
