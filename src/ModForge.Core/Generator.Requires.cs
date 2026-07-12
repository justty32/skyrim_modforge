namespace ModForge;

/// <summary>
/// The declared (<c>requires[]</c>) vs the actual (<see cref="Generator.AnalyzeDependencies"/>) install
/// requirements, compared both ways. <see cref="Declared"/> is false when the spec has no
/// <c>requires:</c> section at all — then nothing is checked and nothing is said.
/// </summary>
public sealed class RequiresCheck
{
    /// <summary>The spec has a <c>requires:</c> section (an EMPTY one counts: "this mod needs nothing").</summary>
    public bool Declared { get; init; }

    /// <summary>Masters the build links that no <c>requires[]</c> entry declares. Build-stopping.</summary>
    public IReadOnlyList<string> Undeclared { get; init; } = Array.Empty<string>();

    /// <summary>Declared plugins the build never links — a stale/copy-pasted line. Warning only.</summary>
    public IReadOnlyList<string> Unused { get; init; } = Array.Empty<string>();

    /// <summary>Ready-to-print lines for <see cref="Undeclared"/>, each naming the spec field behind it.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>Ready-to-print lines for <see cref="Unused"/>.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>Nothing undeclared — the declaration matches what was built.</summary>
    public bool Ok => Undeclared.Count == 0;
}

/// <summary>Result of reconciling <c>requires[]</c> with the build (<see cref="Generator.SyncRequires"/>).</summary>
public sealed class RequiresSync
{
    /// <summary>The <c>requires[]</c> the spec should hold: declared metadata preserved, drift fixed.</summary>
    public IReadOnlyList<RequirementSpec> Entries { get; init; } = Array.Empty<RequirementSpec>();

    public IReadOnlyList<string> Added { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Removed { get; init; } = Array.Empty<string>();
    public bool Changed => Added.Count > 0 || Removed.Count > 0;
}

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  requires[] — the DECLARATIVE half of external-dependency handling (Spec.Requires.cs).
    //
    //  Generator.Dependencies.cs made the dependency set VISIBLE; visibility does not stop the real
    //  failure, which is DRIFT: someone uninstalls PROTEUS, or re-captures without that spell, or
    //  deletes a line — and the plugin's master list quietly changes. Skyrim answers a missing master
    //  by NOT LOADING THE PLUGIN, without an error. `requires[]` is the author's statement of intent,
    //  and build enforces it.
    //
    //  Two rules, and the asymmetry is deliberate:
    //   * a master the build links that nobody declared  → ERROR (the plugin now needs something the
    //     author never signed up for — that is the failure we exist to catch);
    //   * a declared plugin the build never links        → WARNING (a stale line hurts nobody; it just
    //     over-states the requirements, and it may be deliberate — see below).
    //
    //  BACKWARD COMPATIBILITY IS A HARD REQUIREMENT: a spec with NO requires section (null, the shape
    //  of every spec written before this existed) is not checked at all. Writing a requires[] section
    //  is how you OPT IN to the contract. `"requires": []` is an opt-in too — it says "this mod must
    //  stay vanilla-only", and an accidental mod ref then fails the build.
    //
    //  This is spec metadata: NOTHING here reaches the .esp (pinned by a test).
    // -------------------------------------------------------------------------------

    /// <summary>Compare the declared <c>requires[]</c> against the masters the build actually links.</summary>
    public static RequiresCheck CheckRequires(ModSpec spec, IReadOnlyList<MasterDependency> deps)
    {
        if (spec.Requires is null) return new RequiresCheck { Declared = false };   // never declared → never checked

        var declared = new HashSet<string>(
            spec.Requires.Where(r => !string.IsNullOrWhiteSpace(r.Plugin)).Select(r => r.Plugin.Trim()),
            StringComparer.OrdinalIgnoreCase);
        var linked = new HashSet<string>(deps.Select(d => d.Master), StringComparer.OrdinalIgnoreCase);

        var undeclared = new List<string>();
        var errors = new List<string>();
        foreach (var d in deps.Where(d => !d.Vanilla && !declared.Contains(d.Master)))
        {
            undeclared.Add(d.Master);
            var why = d.SpecSources.Count > 0 ? d.SpecSources : d.RecordSources.Select(r => $"record {r}").ToList();
            errors.Add($"  ! requires: {d.Master} is a master of the build but requires[] does not declare it"
                + $" ({d.Links} link(s)){(d.CreationClub ? " [Creation Club]" : "")}"
                + (why.Count > 0 ? $"\n      ← {string.Join("\n      ← ", why.Take(3))}" : ""));
        }

        var unused = new List<string>();
        var warnings = new List<string>();
        foreach (var r in spec.Requires.Where(r => !string.IsNullOrWhiteSpace(r.Plugin) && !linked.Contains(r.Plugin.Trim())))
        {
            unused.Add(r.Plugin.Trim());
            warnings.Add($"  ! requires: declared plugin '{r.Plugin.Trim()}' is never linked by the build — stale line? "
                + "(a mod needed at RUNTIME but referenced by no record is not a master: declare it as "
                + "{ \"name\": \"…\" } instead, which is documentation only)");
        }

        return new RequiresCheck
        {
            Declared = true, Undeclared = undeclared, Unused = unused, Errors = errors, Warnings = warnings,
        };
    }

    /// <summary>
    /// The <c>requires[]</c> the spec SHOULD hold, reconciled with the build (<c>build --sync-requires</c>).
    /// Capture pulls in dependencies in BULK — hand-maintaining the list would make the contract not worth
    /// having — so the fix is one flag, and the resulting spec diff is what makes a dependency change
    /// reviewable in git. Existing entries keep their authored metadata (reason/version/url); doc-only
    /// <c>name</c> entries are never touched; only PLUGIN entries are added/removed.
    /// Pure — it returns a new list, it does not mutate the spec.
    /// </summary>
    public static RequiresSync SyncRequires(IReadOnlyList<RequirementSpec>? declared, IReadOnlyList<MasterDependency> deps)
    {
        var external = deps.Where(d => !d.Vanilla).ToList();
        var needed = new HashSet<string>(external.Select(d => d.Master), StringComparer.OrdinalIgnoreCase);
        var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var entries = new List<RequirementSpec>();
        var removed = new List<string>();
        foreach (var r in declared ?? Array.Empty<RequirementSpec>())
        {
            if (string.IsNullOrWhiteSpace(r.Plugin)) { entries.Add(r); continue; }        // doc-only: keep verbatim
            var plugin = r.Plugin.Trim();
            if (!needed.Contains(plugin)) { removed.Add(plugin); continue; }              // no longer linked: drop
            if (have.Add(plugin)) entries.Add(r);                                         // keep, with its metadata
        }

        var added = new List<string>();
        foreach (var d in external.Where(d => !have.Contains(d.Master)))
        {
            added.Add(d.Master);
            entries.Add(new RequirementSpec { Plugin = d.Master, Reason = SyncReason(d) });
        }

        return new RequiresSync { Entries = entries, Added = added, Removed = removed };
    }

    // Why this master is here, in the form the author can act on: the spec field that pulled it in
    // (the line to delete), falling back to the record that carries the link when no spec field names it.
    private static string SyncReason(MasterDependency d)
    {
        var why = d.SpecSources.Count > 0 ? d.SpecSources[0]
            : d.RecordSources.Count > 0 ? $"record {d.RecordSources[0]}"
            : "linked by the build";
        int eq = why.LastIndexOf(" = ", StringComparison.Ordinal);
        if (eq >= 0) why = why[..eq];                                                     // path only; the value is noise
        return (d.CreationClub ? "[Creation Club] " : "") + why;
    }

    // --- shape check (runs inside Generator.Validate; nothing here needs a built mod) ---------------
    internal static void ValidateRequires(ModSpec spec, List<string> problems)
    {
        if (spec.Requires is null) return;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < spec.Requires.Count; i++)
        {
            var r = spec.Requires[i];
            var plugin = r.Plugin.Trim();
            if (plugin.Length == 0 && string.IsNullOrWhiteSpace(r.Name))
            {
                problems.Add($"requires[{i}]: needs 'plugin' (a .esp/.esm/.esl filename, checked against the build) "
                    + "or 'name' (a requirement with no plugin of its own — documentation only)");
                continue;
            }
            if (plugin.Length == 0) continue;                                             // doc-only entry: nothing to check
            if (!IsPluginFileName(plugin))
                problems.Add($"requires[{i}]: plugin '{plugin}' is not a plugin filename (.esp/.esm/.esl). "
                    + "A mod with no plugin (an SKSE DLL, loose files) goes under 'name' instead");
            if (!seen.Add(plugin))
                problems.Add($"requires[{i}]: plugin '{plugin}' is declared more than once");
        }
    }

    private static bool IsPluginFileName(string s) =>
        s.EndsWith(".esp", StringComparison.OrdinalIgnoreCase)
        || s.EndsWith(".esm", StringComparison.OrdinalIgnoreCase)
        || s.EndsWith(".esl", StringComparison.OrdinalIgnoreCase);
}
