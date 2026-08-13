namespace ModForge;

public static partial class Generator
{
    // A package target/location ref may name a quest ALIAS instead of a placed reference (C組 #2 —
    // radiant performance packages). Syntax:
    //   "alias:<name>"    → the ref/actor held by the ownerQuest's alias <name>  (target = PackageTargetAlias;
    //                        location = LocationFallback AliasForReference)
    //   "aliasLoc:<name>" → the LOCATION held by the ownerQuest's location alias <name>
    //                        (location = LocationFallback AliasForLocation; not valid as a target)
    // The alias index is resolved against the package's ownerQuest (which must be an in-spec quest, so
    // its aliases can be enumerated). Returns false for a plain ref (placement editorId / external).
    internal static bool TryParseAliasRef(string? refStr, out bool isLocationAlias, out string aliasName)
    {
        isLocationAlias = false; aliasName = "";
        if (string.IsNullOrWhiteSpace(refStr)) return false;
        var s = refStr.Trim();
        if (s.StartsWith("aliasLoc:", StringComparison.OrdinalIgnoreCase))
        { isLocationAlias = true; aliasName = s["aliasLoc:".Length..].Trim(); return true; }
        if (s.StartsWith("alias:", StringComparison.OrdinalIgnoreCase))
        { aliasName = s["alias:".Length..].Trim(); return true; }
        return false;
    }

    // A LOCATION-slot ref (sandbox.location, travel.place, …) may carry an explicit "area:" prefix —
    // the author saying "I MEAN a region here, use whatever's inside the radius", not "lock onto that one
    // object". It exists so the "label in a location slot" INFO note (NoteLabelsUsedAsAreaAnchors →
    // ReferenceSlotKindTests) can stay quiet when the area behaviour was intended. The prefix is legal
    // ONLY on Location slots (PackageRefSlots) — on a SingleRef target it is meaningless and left to fail
    // as an unresolved ref. Stripping is a no-op on any string without the prefix, so old specs are
    // byte-identical.
    private const string AreaPrefix = "area:";

    internal static bool HasAreaPrefix(string? refStr) =>
        !string.IsNullOrWhiteSpace(refStr)
        && refStr.TrimStart().StartsWith(AreaPrefix, StringComparison.OrdinalIgnoreCase);

    // Returns the bare ref (prefix and surrounding whitespace removed) when "area:" is present; otherwise
    // the original string unchanged — so a non-area value resolves exactly as it did before this existed.
    internal static string StripAreaPrefix(string? refStr)
    {
        if (!HasAreaPrefix(refStr)) return refStr ?? "";
        return refStr!.TrimStart()[AreaPrefix.Length..].Trim();
    }

    private sealed partial class BuildContext
    {
        // Resolve a package's "alias:<name>" / "aliasLoc:<name>" ref to the alias index on the package's
        // ownerQuest. Returns true (with idx) if refStr is an alias ref AND it resolves; logs a warning
        // and returns true with idx=-1 if it's an alias ref that can't resolve (so callers don't fall
        // back to treating it as a placement ref). Returns false for a plain ref.
        private bool TryResolveAliasIndex(string refStr, string packageEd, out bool isLocationAlias, out int idx)
        {
            idx = -1;
            if (!TryParseAliasRef(refStr, out isLocationAlias, out var name)) return false;

            var pkg = spec.Packages.FirstOrDefault(p => string.Equals(p.EditorId, packageEd, StringComparison.OrdinalIgnoreCase));
            if (pkg is null || string.IsNullOrWhiteSpace(pkg.OwnerQuest))
            { Warn($"  ! package '{packageEd}' '{refStr}': alias refs need an in-spec 'ownerQuest' on the package"); return true; }

            var quest = spec.Quests.FirstOrDefault(q => string.Equals(q.EditorId, pkg.OwnerQuest, StringComparison.OrdinalIgnoreCase));
            if (quest is null)
            { Warn($"  ! package '{packageEd}' '{refStr}': ownerQuest '{pkg.OwnerQuest}' is not an in-spec quest (alias index can't be resolved)"); return true; }

            var found = quest.Aliases.FindIndex(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
            if (found < 0)
            { Warn($"  ! package '{packageEd}' '{refStr}': no alias '{name}' on ownerQuest '{pkg.OwnerQuest}'"); return true; }

            idx = found;
            return true;
        }
    }
}
