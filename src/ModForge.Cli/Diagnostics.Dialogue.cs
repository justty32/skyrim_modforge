internal static partial class Program
{
    // -------------------------------------------------------------------------------
    //  Dialogue / faction / relationship probes — built to investigate the It.26
    //  paid-hireling bug: the recruit topic vanishes once the player has >=500 gold.
    //  The suspicion is a CTDA the NPC fails (e.g. GetRelationshipRank reading 0 because
    //  static player RELA is ignored at runtime). These dump the FULL condition set so
    //  the discriminating condition is visible WITHOUT an in-game cycle.
    // -------------------------------------------------------------------------------

    // Strip the binary-overlay/getter/"ConditionData" suffixes off a condition-data class
    // name to recover the CTDA function name (e.g. "GetRelationshipRank").
    private static string CondFunc(IConditionDataGetter d)
    {
        var n = d.GetType().Name;
        foreach (var suf in new[] { "BinaryOverlay", "Getter", "ConditionData" })
            if (n.EndsWith(suf)) n = n[..^suf.Length];
        return n;
    }

    // Best-effort render of one CTDA function argument. Handles FormLinks, the "Link"-wrapped
    // object/global args, and plain enum/numeric values; returns null for args worth hiding.
    private static string? CondArg(object? v)
    {
        switch (v)
        {
            case null: return null;
            case Mutagen.Bethesda.Plugins.IFormLinkGetter g: return g.FormKey.IsNull ? null : g.FormKey.ToString();
            case string s: return s.Length == 0 ? null : s;
            case bool or System.Enum: return v.ToString();
            case System.IConvertible c: return c.ToString(CultureInfo.InvariantCulture);
        }
        // Wrapper args (FunctionArgumentObject / global slot) expose an inner .Link FormLink.
        var link = v.GetType().GetProperty("Link")?.GetValue(v);
        if (link is Mutagen.Bethesda.Plugins.IFormLinkGetter gl) return gl.FormKey.IsNull ? null : gl.FormKey.ToString();
        return null;
    }

    // Print one condition (CTDA) in full: function, operator, comparison value, run-on, and
    // every function argument — the data we need to find the condition Sera fails.
    private static void PrintCondition(IConditionGetter c, string indent)
    {
        var d = c.Data;
        string cmp = c switch
        {
            IConditionFloatGetter f  => f.ComparisonValue.ToString(CultureInfo.InvariantCulture),
            IConditionGlobalGetter g => g.ComparisonValue.FormKey.IsNull ? "-" : $"global:{g.ComparisonValue.FormKey}",
            _ => "?",
        };
        // Surface the per-function arguments via reflection (skip the base/unused plumbing).
        var args = new List<string>();
        const System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        foreach (var p in d.GetType().GetProperties(bf))
        {
            var name = p.Name;
            if (name is "RunOnType" or "Reference" || name.StartsWith("Unknown") || name.StartsWith("Unused")) continue;
            if (p.GetIndexParameters().Length != 0) continue;
            object? val; try { val = p.GetValue(d); } catch { continue; }
            if (CondArg(val) is { } shown) args.Add($"{name}={shown}");
        }
        var runOn = $"runOn={d.RunOnType}" + (d.Reference.FormKey.IsNull ? "" : $" ref={d.Reference.FormKey}");
        var orFlag = c.Flags.HasFlag(Condition.Flag.OR) ? " [OR]" : "";
        Console.WriteLine($"{indent}{CondFunc(d)} {c.CompareOperator} {cmp}{orFlag}"
            + $"  ({runOn}{(args.Count > 0 ? "; " + string.Join(", ", args) : "")})");
    }

    // Diagnostic: dump dialogue INFO records (responses + FULL CTDA conditions) for a quest or a
    // single topic. <formId> is matched first as a DialogTopic; if none, as a Quest (dumping every
    // topic that quest owns). Optional [substr] filters topics by EditorID (case-insensitive) — use
    // it to narrow a big quest like DialogueFollower down to the hire topics. Response text is a
    // localized-string landmine on master overlays, so it's read best-effort; conditions are inline
    // binary and always print (they're the point).
    private static int InfoDiag(string inPath, string formIdHex, string? substr)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        var topics = mod.EnumerateMajorRecords<IDialogTopicGetter>().ToList();

        // A localized prompt/response throws on a strings-less overlay; fail soft to null.
        static string? Text(Func<string?> read) { try { return read(); } catch { return null; } }

        var direct = topics.Where(t => t.FormKey.ID == id).ToList();
        var byQuest = direct.Count > 0 ? direct
            : topics.Where(t => t.Quest.FormKey.ID == id).ToList();
        if (!string.IsNullOrEmpty(substr) && direct.Count == 0)
            byQuest = byQuest.Where(t => t.EditorID is { } e && e.Contains(substr, StringComparison.OrdinalIgnoreCase)).ToList();

        if (byQuest.Count == 0)
        {
            Console.WriteLine($"0x{id:X6}: no DialogTopic with that FormID, and no topics owned by a quest with that FormID"
                + (string.IsNullOrEmpty(substr) ? "" : $" matching '{substr}'") + $" in {Path.GetFileName(inPath)}");
            return 0;
        }

        foreach (var t in byQuest)
        {
            var prompt = Text(() => t.Name?.String);
            Console.WriteLine($"TOPIC 0x{t.FormKey.ID:X6}  {t.EditorID ?? "-"}  cat={t.Category} sub={t.Subtype}"
                + $"  prio={t.Priority}  quest={t.Quest.FormKey}  branch={(t.Branch.FormKey.IsNull ? "-" : t.Branch.FormKey.ToString())}"
                + (prompt is { } pr ? $"  prompt=\"{pr}\"" : ""));
            int ii = 0;
            foreach (var info in t.Responses)
            {
                Console.WriteLine($"  INFO[{ii++}] 0x{info.FormKey.ID:X6}  flags={info.Flags?.Flags.ToString() ?? "-"}  favor={info.FavorLevel}"
                    + $"  prompt=\"{Text(() => info.Prompt?.String) ?? ""}\"  responses={info.Responses.Count}  conditions={info.Conditions.Count}");
                foreach (var resp in info.Responses)
                    Console.WriteLine($"    response[{resp.ResponseNumber}] ({resp.Emotion}): \"{Text(() => resp.Text?.String) ?? "<localized>"}\"");
                foreach (var c in info.Conditions)
                    PrintCondition(c, "    cond: ");
                // The result script (TIF__ fragment) that runs when the line is picked — the part we
                // must replicate to author a custom paid recruit (take gold + join follower system).
                if (info.VirtualMachineAdapter is { } vmad)
                {
                    string Frag(IScriptFragmentGetter? f) => f is null ? "-" : $"{f.ScriptName}.{f.FragmentName}";
                    var sf = vmad.ScriptFragments;
                    Console.WriteLine($"    VMAD: file={sf?.FileName ?? "-"}  OnBegin={Frag(sf?.OnBegin)}  OnEnd={Frag(sf?.OnEnd)}  scripts=[{string.Join(", ", vmad.Scripts.Select(s => s.Name))}]");
                }
            }
        }
        Console.WriteLine($"-- {byQuest.Count} topic(s)");
        return 0;
    }

    // Diagnostic: dump a FACT (faction) record — flags, ranks, and inter-faction relations.
    // Faction membership is the gate the paid-hireling recruit line keys on (PotentialHireling
    // 0x0BCC9A), so this confirms a faction's flags/relations when reasoning about why a recruit
    // condition passes or fails.
    private static int FactDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        foreach (var f in mod.EnumerateMajorRecords<IFactionGetter>())
        {
            if (f.FormKey.ID != id) continue;
            Console.WriteLine($"0x{id:X6}  EditorID={f.EditorID}");
            Console.WriteLine($"  Flags = {f.Flags}");
            Console.WriteLine($"  Ranks ({f.Ranks.Count}):");
            foreach (var rk in f.Ranks)
            {
                static string? T(Func<string?> r) { try { return r(); } catch { return "<localized>"; } }
                Console.WriteLine($"    rank {rk.Number}: male=\"{T(() => rk.Title?.Male?.String)}\" female=\"{T(() => rk.Title?.Female?.String)}\"");
            }
            Console.WriteLine($"  Relations ({f.Relations.Count}):");
            foreach (var rel in f.Relations)
                Console.WriteLine($"    -> {rel.Target.FormKey} modifier={rel.Modifier} reaction={rel.Reaction}");
            return 0;
        }
        Console.WriteLine($"0x{id:X6} not a Faction in {Path.GetFileName(inPath)}");
        return 0;
    }

    // Diagnostic: dump RELA (relationship) records. <formId> is matched first as a RELA itself; if
    // none, every RELA whose Parent or Child is that FormID is listed (so you can ask "what static
    // relationships involve this actor?"). The known finding: vanilla has zero RELA referencing the
    // player (0x14) — player relationship rank is always script-set at runtime, never static.
    private static int RelaDiag(string inPath, string formIdHex)
    {
        uint id = Convert.ToUInt32(formIdHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16) & 0xFFFFFF;
        using var mod = SkyrimMod.CreateFromBinaryOverlay(new ModPath(inPath), SkyrimRelease.SkyrimSE);
        var all = mod.EnumerateMajorRecords<IRelationshipGetter>();
        void Print(IRelationshipGetter r) => Console.WriteLine(
            $"0x{r.FormKey.ID:X6}  {r.EditorID ?? "-"}  parent={r.Parent.FormKey} child={r.Child.FormKey}"
            + $"  rank={r.Rank}  assoc={(r.AssociationType.FormKey.IsNull ? "-" : r.AssociationType.FormKey.ToString())}  flags={r.Flags}");

        var self = all.FirstOrDefault(r => r.FormKey.ID == id);
        if (self is not null) { Print(self); return 0; }

        int hits = 0;
        foreach (var r in all)
        {
            if (r.Parent.FormKey.ID != id && r.Child.FormKey.ID != id) continue;
            Print(r);
            hits++;
        }
        Console.WriteLine($"-- 0x{id:X6} is not a RELA; {hits} RELA(s) reference it as parent/child in {Path.GetFileName(inPath)}");
        return 0;
    }
}
