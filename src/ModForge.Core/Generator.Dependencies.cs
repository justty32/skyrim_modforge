using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ModForge;

/// <summary>
/// One master the built plugin needs at load time, and WHY it needs it.
/// A plugin whose masters are not all installed is <b>silently dropped</b> by Skyrim, so a
/// non-vanilla master is a hard install requirement for whoever receives the plugin.
/// </summary>
public sealed class MasterDependency
{
    /// <summary>Master filename, e.g. <c>PROTEUS.esp</c>.</summary>
    public required string Master { get; init; }

    /// <summary>Ships with the base game + official DLC — every install has it (<see cref="Generator.VanillaMasters"/>).</summary>
    public bool Vanilla { get; init; }

    /// <summary>Creation Club / Anniversary content — owned per account, NOT on every install.</summary>
    public bool CreationClub { get; init; }

    /// <summary>How many FormLinks in the built plugin point into this master.</summary>
    public int Links { get; init; }

    /// <summary>Spec fields that name this master, e.g. <c>capturedNpcs[0].spells[3] = PROTEUS.esp:0x08073D</c>.</summary>
    public IReadOnlyList<string> SpecSources { get; init; } = Array.Empty<string>();

    /// <summary>Built records that reference it, e.g. <c>Npc:MFCapHatak</c> — catches links no spec field names.</summary>
    public IReadOnlyList<string> RecordSources { get; init; } = Array.Empty<string>();
}

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  External-dependency visibility (backlog: 外部 mod 依賴的可見性).
    //
    //  Naming `PROTEUS.esp:0x08073D` anywhere in a spec makes PROTEUS.esp a MASTER of the output, and
    //  Skyrim SILENTLY refuses to load a plugin whose masters are missing — no error, no log, the
    //  records simply are not there. `sc cap`/`sc capp` make that happen in BULK (a player clone drags
    //  in every mod that gave the player a spell / perk / active effect / item), but a hand-written
    //  spec does exactly the same thing. Not filtering that content is deliberate (full fidelity beats
    //  portability — the user's call), so this pass changes NOTHING about the output: it only makes the
    //  dependency set, and the per-field attribution, VISIBLE at build time.
    //
    //  Two sources, on purpose:
    //   * the built MOD is the authority on WHICH masters exist — it catches links that no spec string
    //     names (a deep-copied record drags its own refs along: npcPatches, template clones, cell env);
    //   * the SPEC gives the ATTRIBUTION that makes it actionable ("which line do I delete?").
    // -------------------------------------------------------------------------------

    /// <summary>The masters every Skyrim SE install has. Everything else is an install requirement.</summary>
    public static readonly IReadOnlyList<string> VanillaMasters = new[]
    {
        "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm",
    };

    // Creation Club plugins (ccBGSSSE001-Fish.esm, ccQDRSSE001-SurvivalMode.esl, the AE _ResourcePack.esl).
    // NOT vanilla: CC content is owned per account, so a CC master is exactly as fatal to a player who
    // lacks it as any Nexus mod. Flagged separately only so the report can name the reason.
    private static readonly Regex CcMaster =
        new(@"^(cc[A-Za-z]{3}SSE\d{3}|_ResourcePack)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsVanillaMaster(string master) =>
        VanillaMasters.Any(v => string.Equals(v, master, StringComparison.OrdinalIgnoreCase));

    public static bool IsCreationClubMaster(string master) => CcMaster.IsMatch(master);

    /// <summary>
    /// Every master the built plugin depends on, with attribution. Read-only over both inputs — it
    /// never touches the mod, so the .esp is byte-for-byte identical whether this runs or not.
    /// Ordered: non-vanilla first (most links first), then vanilla.
    /// </summary>
    public static IReadOnlyList<MasterDependency> AnalyzeDependencies(ISkyrimModGetter mod, ModSpec spec)
    {
        // --- the built mod: authoritative master set + which records pulled each one in ---
        var links = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var records = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var linked = new HashSet<FormKey>();                    // every external form the plugin actually links

        void Hit(FormKey fk, string record)
        {
            if (fk.ModKey.IsNull || fk.ModKey == mod.ModKey) return;
            var name = fk.ModKey.FileName.String;
            links[name] = links.GetValueOrDefault(name) + 1;
            linked.Add(fk);
            var list = records.TryGetValue(name, out var l) ? l : records[name] = new List<string>();
            if (!list.Contains(record)) list.Add(record);
        }

        foreach (var rec in mod.EnumerateMajorRecords())
        {
            var who = $"{rec.GetType().Name}:{rec.EditorID ?? rec.FormKey.ID.ToString("X6")}";
            Hit(rec.FormKey, who);                              // an OVERRIDE record masters the plugin it came from
            foreach (var link in rec.EnumerateFormLinks()) Hit(link.FormKey, who);
        }

        // --- the spec: which authored field named each master (pre-macro-expansion, see ExpandMacros) ---
        var specSources = spec.AuthoredRefSources ?? SpecRefSources(spec);

        return links.Keys
            .Select(m => new MasterDependency
            {
                Master = m,
                Vanilla = IsVanillaMaster(m),
                CreationClub = IsCreationClubMaster(m),
                Links = links[m],
                SpecSources = specSources.TryGetValue(m, out var s) ? Causal(s, linked) : Array.Empty<string>(),
                RecordSources = records[m],
            })
            .OrderBy(d => d.Vanilla)
            .ThenByDescending(d => d.Links)
            .ThenBy(d => d.Master, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Keep only the spec fields that CAUSED a link. A spec may name a form the build never links —
    // `capturedNpcs[].activeEffects[]` is recorded fidelity, not a record, so a mod's magic effect can
    // appear there while the master is really pulled in by `spells[]`. Reporting the non-causal line
    // would be a lie: deleting it would not drop the dependency. (No causal field left → the caller
    // falls back to the RECORD attribution, which is what a deep-copied ref looks like anyway.)
    private static IReadOnlyList<string> Causal(IReadOnlyList<string> sources, HashSet<FormKey> linked) =>
        sources.Where(s =>
        {
            int eq = s.LastIndexOf(" = ", StringComparison.Ordinal);
            return eq >= 0 && TryExternalRef(s[(eq + 3)..], out var fk) && linked.Contains(fk);
        }).ToList();

    /// <summary>
    /// Walk the spec object graph and collect every <c>&lt;master&gt;:0xFORMID</c> string with the JSON
    /// path that holds it (<c>capturedNpcs[0].spells[17]</c>). Pure — this is the attribution half.
    /// Take it BEFORE macro expansion, or a captured NPC reports through the <c>npcs[]</c> the macro
    /// generated instead of the <c>capturedNpcs[]</c> the author actually wrote.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> SpecRefSources(ModSpec spec)
    {
        var hits = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        int budget = 500_000;                                   // pathological-spec guard (never hit in practice)
        Walk(spec, "", hits, new HashSet<object>(ReferenceEqualityComparer.Instance), ref budget);
        return hits.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static void Walk(object? node, string path, Dictionary<string, List<string>> hits, HashSet<object> seen, ref int budget)
    {
        if (node is null || budget-- <= 0) return;

        switch (node)
        {
            case string s:
                if (LooksExternalRef(s)) Record(s[..s.IndexOf(':')].Trim(), $"{path} = {s}", hits);
                return;

            case JsonElement je:                                // raw-JSON spec sections (lighting/weather/…)
                WalkJson(je, path, hits, ref budget);
                return;

            case IDictionary dict:                              // keyed spec sections: packages["Guard_Sandbox"]…
                if (!seen.Add(dict)) return;
                foreach (DictionaryEntry e in dict)
                    Walk(e.Value, $"{path}[\"{e.Key}\"]", hits, seen, ref budget);
                return;

            case IEnumerable list:
                if (!seen.Add(list)) return;
                int i = 0;
                foreach (var item in list) Walk(item, $"{path}[{i++}]", hits, seen, ref budget);
                return;
        }

        var t = node.GetType();
        if (t.IsPrimitive || t.IsEnum || node is decimal or DateTime) return;
        if (t.Assembly != typeof(ModSpec).Assembly) return;     // never wander into Mutagen / BCL graphs
        if (!seen.Add(node)) return;

        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
            object? v;
            try { v = p.GetValue(node); } catch { continue; }
            if (v is null) continue;
            var name = char.ToLowerInvariant(p.Name[0]) + p.Name[1..];   // spec JSON is camelCase
            Walk(v, path.Length == 0 ? name : $"{path}.{name}", hits, seen, ref budget);
        }
    }

    private static void WalkJson(JsonElement je, string path, Dictionary<string, List<string>> hits, ref int budget)
    {
        if (budget-- <= 0) return;
        switch (je.ValueKind)
        {
            case JsonValueKind.String:
                var s = je.GetString();
                if (s is not null && LooksExternalRef(s)) Record(s[..s.IndexOf(':')].Trim(), $"{path} = {s}", hits);
                return;
            case JsonValueKind.Object:
                foreach (var p in je.EnumerateObject()) WalkJson(p.Value, $"{path}.{p.Name}", hits, ref budget);
                return;
            case JsonValueKind.Array:
                int i = 0;
                foreach (var item in je.EnumerateArray()) WalkJson(item, $"{path}[{i++}]", hits, ref budget);
                return;
        }
    }

    private static void Record(string master, string source, Dictionary<string, List<string>> hits)
    {
        var list = hits.TryGetValue(master, out var l) ? l : hits[master] = new List<string>();
        list.Add(source);                                        // paths are unique by construction
    }

    // -------------------------------------------------------------------------------
    //  Reporting. INFORMATIONAL, never a warning — mod-sourced content is what the author asked for.
    //  A spec that only touches vanilla must print NOTHING (this must not become background noise).
    // -------------------------------------------------------------------------------

    private const int ConsoleSources = 3;                        // per master, in the build summary
    private const int FileSources = 25;                          // per master, in the sidecar

    /// <summary>Build-summary lines. Empty when every master is vanilla.</summary>
    public static IReadOnlyList<string> DependencySummary(IReadOnlyList<MasterDependency> deps)
    {
        var external = deps.Where(d => !d.Vanilla).ToList();
        if (external.Count == 0) return Array.Empty<string>();

        var lines = new List<string>
        {
            $"{external.Count} non-vanilla master(s) — the plugin will NOT load for anyone missing them (Skyrim drops it silently):",
        };
        foreach (var d in external)
        {
            lines.Add($"  {d.Master}  ({d.Links} link(s)){(d.CreationClub ? "  [Creation Club — owned per account, not on every install]" : "")}");
            var why = Attribution(d);
            foreach (var s in why.Take(ConsoleSources)) lines.Add($"      ← {s}");
            if (why.Count > ConsoleSources) lines.Add($"      … +{why.Count - ConsoleSources} more");
        }
        return lines;
    }

    // The spec field when we know it (that is the line to delete); otherwise the records that dragged
    // the master in (a deep-copied record's own refs name no spec field at all).
    private static List<string> Attribution(MasterDependency d) =>
        d.SpecSources.Count > 0 ? d.SpecSources.ToList() : d.RecordSources.Select(r => $"record {r}").ToList();

    /// <summary>
    /// The <c>&lt;plugin&gt;.requires.txt</c> sidecar — a durable record of what this plugin needs
    /// (a build summary scrolls away, and nothing else anywhere says what an .esp depends on).
    /// Null when there is nothing non-vanilla to report: no file should be written.
    /// </summary>
    public static string? RequiresFileText(string pluginName, IReadOnlyList<MasterDependency> deps)
    {
        var external = deps.Where(d => !d.Vanilla).ToList();
        if (external.Count == 0) return null;

        var sb = new StringBuilder();
        sb.AppendLine($"# {pluginName} — install requirements (generated by `modforge build`)");
        sb.AppendLine("#");
        sb.AppendLine("# Every master listed below must be INSTALLED AND ENABLED, or Skyrim silently refuses to");
        sb.AppendLine("# load this plugin: no error, no log line, the records simply are not there in-game.");
        sb.AppendLine("# Under each master are the spec fields that name a form the plugin links from it.");
        sb.AppendLine("# To drop a dependency, remove ALL of that master's lines from the spec and rebuild.");
        sb.AppendLine("#");
        sb.AppendLine("# vanilla (in every install, no action needed): "
            + string.Join(", ", deps.Where(d => d.Vanilla).Select(d => d.Master).DefaultIfEmpty("none")));
        sb.AppendLine();
        sb.AppendLine($"requires {external.Count} non-vanilla master(s):");
        foreach (var d in external)
        {
            sb.AppendLine();
            sb.AppendLine($"{d.Master}  ({d.Links} link(s)){(d.CreationClub ? "  [Creation Club — owned per account, not on every install]" : "")}");
            var why = Attribution(d);
            foreach (var s in why.Take(FileSources)) sb.AppendLine($"    {s}");
            if (why.Count > FileSources) sb.AppendLine($"    … +{why.Count - FileSources} more");
            if (d.SpecSources.Count > 0 && d.RecordSources.Count > 0)
                sb.AppendLine($"    records: {string.Join(", ", d.RecordSources.Take(FileSources))}");
        }
        return sb.ToString();
    }
}
