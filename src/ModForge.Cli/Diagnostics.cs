internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  find — search a (possibly huge) plugin for records by EditorID / Name.
    //  Plus TypeLabel, the friendly-record-type helper shared with `dump`.
    //  The per-record inspection commands live in Diagnostics.Records.cs; the dialogue/
    //  faction/relationship CTDA probes in Diagnostics.Dialogue.cs; the full round-trip
    //  dumper in Diagnostics.Dump.cs.
    // -------------------------------------------------------------------------------
    // Search a (possibly huge, e.g. Skyrim.esm) plugin for records whose EditorID or Name
    // contains <query> (case-insensitive). Reads via a lazy read-only OVERLAY so a 250 MB
    // master doesn't get fully materialized. Prints a resolver-ready "<master>:0xFORMID" ref,
    // the record type, EditorID and Name. Optional [type] (e.g. Weapon, Npc, Keyword) filters
    // by record kind, letting the overlay skip whole groups instead of parsing everything.
    private static int Find(string inPath, string query, string? typeName)
    {
        // Vanilla masters are localized: Name is a string index whose text lives in BSA-packed
        // .STRINGS. Point the strings reader at the plugin's own Data folder (BSA override) so it
        // resolves names WITHOUT the game-environment/plugins.txt lookup (absent on Linux).
        var dataDir = Path.GetDirectoryName(Path.GetFullPath(inPath))!;
        var readParams = new BinaryReadParameters
        {
            StringsParam = new StringsReadParameters
            {
                BsaFolderOverride = dataDir,
                StringsFolderOverride = dataDir,
                TargetLanguage = Language.English,
            },
        };
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE, readParams);

        IEnumerable<IMajorRecordGetter> records;
        if (!string.IsNullOrEmpty(typeName))
        {
            // Friendly short aliases → the Mutagen type name (the reflection below uses I<Type>Getter).
            typeName = typeName.ToLowerInvariant() switch
            {
                "idle" => "IdleAnimation",   // PlayIdle scene-action idle discovery
                _ => typeName,
            };
            var t = typeof(ISkyrimModGetter).Assembly
                .GetType($"Mutagen.Bethesda.Skyrim.I{typeName}Getter", throwOnError: false, ignoreCase: true);
            if (t is null)
            {
                Console.Error.WriteLine(
                    $"Unknown record type '{typeName}'. Examples: Weapon, Armor, Ammunition, Npc, " +
                    "MiscItem, Ingredient, Ingestible, Book, Key, SoulGem, Keyword, Race, Class, " +
                    "Faction, Spell, MagicEffect, Perk, Outfit, LeveledItem, LeveledNpc, Location, " +
                    "Cell, Furniture, IdleAnimation (alias: idle).");
                return 2;
            }
            records = mod.EnumerateMajorRecords(t, throwIfUnknown: false);
        }
        else
        {
            records = mod.EnumerateMajorRecords();
        }

        // Name is a localized string (BSA-packed for vanilla); resolving it needs the game's
        // archive load order, which isn't available headless on Linux. EditorID + FormID are
        // stored inline and always read. So resolve Name best-effort: on the first failure,
        // stop trying (deterministic) and search EditorID only.
        bool namesOk = true;
        string? NameOf(IMajorRecordGetter r)
        {
            if (!namesOk) return null;
            try { return (r as INamedGetter)?.Name; }
            catch { namesOk = false; return null; }
        }

        var q = query.ToLowerInvariant();
        const int cap = 300;
        int total = 0, shown = 0;
        foreach (var r in records)
        {
            var ed = r.EditorID;
            var name = NameOf(r);
            bool hit = (ed is { } e && e.ToLowerInvariant().Contains(q))
                    || (name is { } n && n.ToLowerInvariant().Contains(q));
            if (!hit) continue;
            total++;
            if (shown++ < cap)
            {
                var fk = r.FormKey;
                Console.WriteLine($"{fk.ModKey}:0x{fk.ID:X6}  {TypeLabel(r)}  {ed}"
                    + (name is { } nm ? $"  \"{nm}\"" : ""));
            }
        }
        Console.WriteLine($"-- {total} match(es)" + (total > cap ? $", showing first {cap}" : "")
            + (namesOk ? "" : "  [names unresolved: search matched EditorID only — see note]"));
        return 0;
    }

    // Concrete Mutagen record class -> friendly type name (strip overlay/getter suffixes).
    // Shared by `find` and `dump`.
    private static string TypeLabel(IMajorRecordGetter r)
    {
        var n = r.GetType().Name;
        foreach (var suf in new[] { "BinaryOverlay", "Getter" })
            if (n.EndsWith(suf)) n = n[..^suf.Length];
        return n;
    }
}
