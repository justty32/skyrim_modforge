namespace ModForge;

public static partial class Generator
{
    private static HashSet<string> GodotReservedIds(
        SkyrimMod mod, ModSpec spec, List<PlacementSpec> placements)
    {
        var ids = new HashSet<string>(
            mod.EnumerateMajorRecords()
                .Where(r => !string.IsNullOrWhiteSpace(r.EditorID))
                .Select(r => r.EditorID!),
            StringComparer.OrdinalIgnoreCase);
        ids.UnionWith(placements.Where(p => !string.IsNullOrWhiteSpace(p.EditorId)).Select(p => p.EditorId));
        ids.UnionWith(spec.Worldspaces.Where(w => !string.IsNullOrWhiteSpace(w.EditorId)).Select(w => w.EditorId));
        ids.UnionWith(spec.Regions.Where(r => !string.IsNullOrWhiteSpace(r.EditorId)).Select(r => r.EditorId));
        ids.UnionWith(spec.MapMarkers.Where(m => !string.IsNullOrWhiteSpace(m.EditorId)).Select(m => m.EditorId));
        ids.UnionWith(spec.References.Where(r => !string.IsNullOrWhiteSpace(r.Label)).Select(r => r.Label.Trim()));
        ids.UnionWith(spec.WordWalls.Select(w =>
            string.IsNullOrWhiteSpace(w.TriggerEditorId) ? w.EditorId + "Trigger" : w.TriggerEditorId));
        ids.UnionWith(spec.NavCuts.Where(n => !string.IsNullOrWhiteSpace(n.EditorId)).Select(n => n.EditorId));
        ids.UnionWith(placements
            .Where(p => !string.IsNullOrWhiteSpace(p.EditorId) && (p.NavCut?.Enabled ?? spec.Navmesh.AutoNavCuts))
            .Select(p => "MFNavCut_" + p.EditorId));
        ids.UnionWith(spec.References.Select((r, i) => (Ref: r, Index: i + 1))
            .Where(x => (x.Ref.Anchor ?? "").Equals("marker", StringComparison.OrdinalIgnoreCase)
                     || (x.Ref.Anchor ?? "").Equals("replace", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"MFRef_{SanitizeEd(x.Ref.Label.Trim())}_{x.Index}"));
        return ids;
    }

    private static List<PlacementSpec> LoadValidatedGodotPlacements(
        GodotPlacementsSpec gpSpec, string specDir, string worldspaceId, ModSpec spec,
        Dictionary<string, FormKey> formKeyByEd, HashSet<string> reservedIds,
        Dictionary<string, string> importedIdSources)
    {
        var imported = GodotPlacements.Load(gpSpec, specDir, worldspaceId);
        var sourcePath = GodotPlacements.ResolvePath(gpSpec, specDir);
        for (int i = 0; i < imported.Count; i++)
        {
            var pl = imported[i];
            if (!string.IsNullOrWhiteSpace(pl.EditorId) && !reservedIds.Add(pl.EditorId))
                throw InvalidGodot(sourcePath, worldspaceId,
                    $"placements[{i}].instanceId '{pl.EditorId}' collides with an existing or planned editorId");
            if (!string.IsNullOrWhiteSpace(pl.EditorId))
                importedIdSources.Add(pl.EditorId,
                    $"godotPlacements '{sourcePath}' for worldspace '{worldspaceId}': placements[{i}]");
            bool external = LooksExternalRef(pl.Base);
            if ((external && !ValidGodotExternalRef(pl.Base))
                || (!external && !formKeyByEd.ContainsKey(pl.Base)))
                throw InvalidGodot(sourcePath, worldspaceId,
                    $"placements[{i}].base '{pl.Base}' is not a resolvable in-spec editorId or canonical " +
                    "<master>:0xFORMID ref (1-6 hex digits, or 8 with leading 00)");
            if (spec.LeveledNpcs.Any(n => n.EditorId.Equals(pl.Base, StringComparison.OrdinalIgnoreCase)))
                throw InvalidGodot(sourcePath, worldspaceId,
                    $"placements[{i}].base '{pl.Base}' is a LeveledNpc list (LVLN); " +
                    "LVLN placement bases cause CTD — use an NPC_ actor");
        }
        return imported;
    }

    private static void EnsureUniqueGodotImportedEditorIds(
        SkyrimMod mod, IReadOnlyDictionary<string, string> importedIdSources)
    {
        if (importedIdSources.Count == 0) return;

        var counts = mod.EnumerateMajorRecords()
            .Where(r => !string.IsNullOrWhiteSpace(r.EditorID)
                     && importedIdSources.ContainsKey(r.EditorID!))
            .GroupBy(r => r.EditorID!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (var (editorId, source) in importedIdSources)
        {
            counts.TryGetValue(editorId, out var count);
            if (count != 1)
                throw new InvalidDataException(
                    $"{source}.instanceId '{editorId}' must identify exactly one final plugin record; " +
                    $"found {count} (it collides with a record generated later in the build)");
        }
    }

    private static InvalidDataException InvalidGodot(string path, string worldspaceId, string detail) =>
        new($"godotPlacements '{path}' for worldspace '{worldspaceId}': {detail}");

    private static bool ValidGodotExternalRef(string value)
    {
        int colon = value.IndexOf(':');
        if (colon <= 0 || !LooksExternalRef(value)) return false;
        var hex = value[(colon + 1)..].Trim();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        if (hex.Length is < 1 or > 8 || hex.Any(c => !Uri.IsHexDigit(c))) return false;
        return hex.Length <= 6 || (hex.Length == 8 && hex.StartsWith("00", StringComparison.Ordinal));
    }
}
