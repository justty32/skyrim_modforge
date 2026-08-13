namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  JContainers JFormDB persistence GENERATION (Idea #20 in-world skill tree, Phase 0).
    //
    //  A `persist` block writes nested per-Form state; a `syncPerks` block applies perks from stored
    //  ranks. Both can hang on TWO host fragments — the same emitter serves either:
    //    * a dialogue line's TIF result fragment (Fragment_0(akSpeakerRef)), and
    //    * a quest STAGE fragment (Fragment_Stage_XXXX, extends Quest — NO akSpeakerRef).
    //  Any Form/Perk value (and an arbitrary-ref key) binds as a VMAD object property — the same
    //  machinery as setStage / rewardItem (Generator.Build.Scripts.cs / Generator.Build.QuestStages.cs).
    //
    //  Property names are namespaced by a `prefix` so a quest script with persist on several stages does
    //  not collide ("" for a dialogue TIF — one Fragment_0; "S0010_" for quest stage 10). Function-local
    //  temps (__pv<i>, __sp) need no prefix: each stage fragment is its own Papyrus function scope.
    //
    //  Lifecycle note (answers design unknown U5): we expose ONLY the root-DB path API
    //  (JFormDB.solveXxxSetter / solveXxx). JContainers owns those roots and persists them with the save,
    //  so there is NO JValue.object()/retain()/release() handle to balance — the retain/release footgun
    //  is avoided by construction. Compiling the generated .psc needs JContainers' own .psc on the header
    //  path (main machine; see WAIT_USER); these are pure functions (string in/out), unit-testable with
    //  no Skyrim master / Wine.
    // -------------------------------------------------------------------------------

    /// <summary>How a persist/syncPerks Form key resolves: the dialogue speaker (akSpeakerRef — only a
    /// TIF fragment has it), the player, or an arbitrary ref bound as a Form property.</summary>
    internal enum PersistKeyKind { Speaker, Player, Ref }

    /// <summary>Classify a persist/syncPerks key string. "" / "speaker" → Speaker; "player" → Player;
    /// anything else → an arbitrary ref (resolved to a Form and bound as a property).</summary>
    internal static PersistKeyKind ClassifyPersistKey(string key)
    {
        var k = (key ?? "").Trim();
        if (string.Equals(k, "player", System.StringComparison.OrdinalIgnoreCase)) return PersistKeyKind.Player;
        if (k.Length == 0 || string.Equals(k, "speaker", System.StringComparison.OrdinalIgnoreCase)) return PersistKeyKind.Speaker;
        return PersistKeyKind.Ref;
    }

    /// <summary>The Papyrus expression for a JFormDB Form key. "speaker" → akSpeakerRef (TIF only),
    /// "player" → Game.GetPlayer(), an arbitrary ref → <paramref name="keyPropertyName"/> (the bound
    /// Form property). All three are Form (or a subtype), so they pass straight to JFormDB.</summary>
    internal static string JFormDbKeyExpr(string key, string keyPropertyName) =>
        ClassifyPersistKey(key) switch
        {
            PersistKeyKind.Player => "Game.GetPlayer()",
            PersistKeyKind.Speaker => "akSpeakerRef",
            _ => keyPropertyName,
        };

    /// <summary>Full JFormDB path: ".&lt;storage&gt;" + the entry subpath (normalized to start with '.').
    /// e.g. storage "ModForgeNpcSkills" + ".Endurance.nodes.Adaptation".</summary>
    internal static string JFormDbPath(string storage, string sub)
    {
        var s = (sub ?? "").Trim();
        var st = (storage ?? "").Trim();
        if (s.Length == 0) return "." + st;
        if (!s.StartsWith(".")) s = "." + s;
        return "." + st + s;
    }

    /// <summary>Property name (namespaced by <paramref name="prefix"/>) holding the Form value of persist
    /// entry <paramref name="i"/>.</summary>
    internal static string PersistFormProperty(string prefix, int i) => $"{prefix}PF_{i}";
    /// <summary>Property name (namespaced by <paramref name="prefix"/>) holding the PERK of syncPerks
    /// node <paramref name="i"/>.</summary>
    internal static string SyncPerkProperty(string prefix, int i) => $"{prefix}SyncPerk_{i}";
    /// <summary>Property name (namespaced) holding the arbitrary-ref Form key of a persist block.</summary>
    internal static string PersistKeyProperty(string prefix) => $"{prefix}PKey";
    /// <summary>Property name (namespaced) holding the arbitrary-ref Form key of a syncPerks block.</summary>
    internal static string SyncKeyProperty(string prefix) => $"{prefix}SKey";
    /// <summary>Property name (namespaced) holding the affinity-gate GlobalVariable of a persist block.</summary>
    internal static string PersistGateProperty(string prefix) => $"{prefix}PGate";
    /// <summary>Property name (namespaced) holding the affinity-gate GlobalVariable of a syncPerks block.</summary>
    internal static string SyncGateProperty(string prefix) => $"{prefix}SGate";

    /// <summary>True when a gate carries a GlobalVariable to bind/read.</summary>
    internal static bool HasGate(GateSpec? g) => g is { } x && !string.IsNullOrWhiteSpace(x.Global);

    /// <summary>The Papyrus boolean condition for an affinity gate: the bound GLOB's value vs the
    /// threshold(s). Both bounds → a band; neither → "non-zero" (a boolean flag).</summary>
    internal static string GateCondition(GateSpec g, string prop)
    {
        var parts = new List<string>();
        if (g.AtLeast is float lo) parts.Add($"{prop}.GetValue() >= {PapyrusFloat(lo)}");
        if (g.AtMost is float hi) parts.Add($"{prop}.GetValue() <= {PapyrusFloat(hi)}");
        return parts.Count == 0 ? $"{prop}.GetValue() != 0" : string.Join(" && ", parts);
    }

    /// <summary>Wrap a block's body lines in its affinity gate (If/EndIf, inner lines indented). No gate →
    /// the body passes through unchanged.</summary>
    private static IEnumerable<string> GateWrap(GateSpec? gate, string prop, List<string> body)
    {
        if (!HasGate(gate)) { foreach (var l in body) yield return l; yield break; }
        yield return $"If {GateCondition(gate!, prop)}";
        foreach (var l in body) yield return "    " + l;
        yield return "EndIf";
    }

    /// <summary>The Form-valued persist entries of a block (declaration index → entry). Only these bind a
    /// Form property; int/float/string entries carry literals and declare nothing.</summary>
    internal static IEnumerable<(int Index, PersistEntrySpec Entry)> PersistFormEntries(PersistSpec? p) =>
        (p?.Set ?? new()).Select((e, i) => (i, e)).Where(t => !string.IsNullOrWhiteSpace(t.e.Form));

    /// <summary>True when a persist block carries any JFormDB write.</summary>
    internal static bool HasPersist(PersistSpec? p) => p is { } x && x.Set.Count > 0;
    /// <summary>True when a syncPerks block carries any node.</summary>
    internal static bool HasSyncPerks(SyncPerksSpec? s) => s is { } x && x.Nodes.Count > 0;
    internal static bool HasPersist(DialogueSpec d) => HasPersist(d.Persist);
    internal static bool HasSyncPerks(DialogueSpec d) => HasSyncPerks(d.SyncPerks);
    internal static bool HasPersist(StageSpec s) => HasPersist(s.Persist);
    internal static bool HasSyncPerks(StageSpec s) => HasSyncPerks(s.SyncPerks);

    /// <summary>The Papyrus property DECLARATIONS a persist/syncPerks pair needs under <paramref
    /// name="prefix"/>: a Form key property (only for an arbitrary-ref key), a Form property per
    /// Form-valued persist write, and a Perk property per syncPerks node. Int/float/string persist values
    /// are literals and declare nothing.</summary>
    internal static IEnumerable<string> JContainersPropertyDecls(string prefix, PersistSpec? persist, SyncPerksSpec? sync)
    {
        if (persist is { } p && p.Set.Count > 0)
        {
            if (ClassifyPersistKey(p.Key) == PersistKeyKind.Ref)
                yield return $"Form Property {PersistKeyProperty(prefix)} Auto";
            foreach (var (i, _) in PersistFormEntries(p))
                yield return $"Form Property {PersistFormProperty(prefix, i)} Auto";
            if (HasGate(p.Gate))
                yield return $"GlobalVariable Property {PersistGateProperty(prefix)} Auto";
        }
        if (sync is { } s && s.Nodes.Count > 0)
        {
            if (ClassifyPersistKey(s.Key) == PersistKeyKind.Ref)
                yield return $"Form Property {SyncKeyProperty(prefix)} Auto";
            for (int i = 0; i < s.Nodes.Count; i++)
                yield return $"Perk Property {SyncPerkProperty(prefix, i)} Auto";
            if (HasGate(s.Gate))
                yield return $"GlobalVariable Property {SyncGateProperty(prefix)} Auto";
        }
    }
    /// <summary>Property declarations for a dialogue line (prefix "").</summary>
    internal static IEnumerable<string> JContainersPropertyDecls(DialogueSpec d) =>
        JContainersPropertyDecls("", d.Persist, d.SyncPerks);

    /// <summary>The fragment body lines (unindented; the caller indents) for a persist/syncPerks pair
    /// under <paramref name="prefix"/> — persist writes first, then perk sync (so a sync sees the ranks
    /// just persisted). Empty when the pair carries neither.</summary>
    internal static IEnumerable<string> JContainersFragmentBody(string prefix, PersistSpec? persist, SyncPerksSpec? sync)
    {
        if (persist is { } p && p.Set.Count > 0)
        {
            var key = JFormDbKeyExpr(p.Key, PersistKeyProperty(prefix));
            var body = new List<string>();
            for (int i = 0; i < p.Set.Count; i++)
                body.AddRange(EmitPersistEntry(key, JFormDbPath(p.Storage, p.Set[i].Path), p.Set[i], PersistFormProperty(prefix, i), i));
            // The writes run unconditionally, OR only while the affinity gate's GLOB passes its threshold.
            foreach (var line in GateWrap(p.Gate, PersistGateProperty(prefix), body))
                yield return line;
        }
        if (sync is { } s && s.Nodes.Count > 0)
        {
            var key = JFormDbKeyExpr(s.Key, SyncKeyProperty(prefix));
            var body = new List<string>
            {
                // The key actor: cast once so AddPerk/RemovePerk resolve (akSpeakerRef/GetPlayer()/a ref are
                // ObjectReference-or-subtype; `as Actor` returns None for a non-actor ref — the If guards it).
                $"Actor __sp = {key} as Actor",
                "If __sp",
            };
            for (int i = 0; i < s.Nodes.Count; i++)
            {
                var n = s.Nodes[i];
                var path = JFormDbPath(s.Storage, n.Path);
                var prop = SyncPerkProperty(prefix, i);
                int rank = System.Math.Max(1, n.MinRank);
                body.Add($"    If JFormDB.solveInt({key}, \"{path}\", 0) >= {rank}");
                body.Add($"        __sp.AddPerk({prop})       ; rank >= {rank}");
                body.Add("    Else");
                body.Add($"        __sp.RemovePerk({prop})");
                body.Add("    EndIf");
            }
            body.Add("EndIf");
            foreach (var line in GateWrap(s.Gate, SyncGateProperty(prefix), body))
                yield return line;
        }
    }
    /// <summary>Fragment body for a dialogue line (prefix "").</summary>
    internal static IEnumerable<string> JContainersFragmentBody(DialogueSpec d) =>
        JContainersFragmentBody("", d.Persist, d.SyncPerks);

    // One persist write → 1-2 Papyrus lines. `formProp` is the bound Form-property name for a form entry;
    // `i` makes the read-add-write temp unique across entries within this fragment function.
    private static IEnumerable<string> EmitPersistEntry(string key, string path, PersistEntrySpec e, string formProp, int i)
    {
        if (e.Int is int iv)
        {
            if (e.Delta)
            {
                yield return $"int __pv{i} = JFormDB.solveInt({key}, \"{path}\", 0)";
                yield return $"JFormDB.solveIntSetter({key}, \"{path}\", __pv{i} + {iv}, true)";
            }
            else yield return $"JFormDB.solveIntSetter({key}, \"{path}\", {iv}, true)";
        }
        else if (e.Float is float fv)
        {
            if (e.Delta)
            {
                yield return $"float __pv{i} = JFormDB.solveFlt({key}, \"{path}\", 0.0)";
                yield return $"JFormDB.solveFltSetter({key}, \"{path}\", __pv{i} + {PapyrusFloat(fv)}, true)";
            }
            else yield return $"JFormDB.solveFltSetter({key}, \"{path}\", {PapyrusFloat(fv)}, true)";
        }
        else if (!string.IsNullOrWhiteSpace(e.Form))
            yield return $"JFormDB.solveFormSetter({key}, \"{path}\", {formProp}, true)";
        else if (e.Str is { } sv)   // string set (may be the empty string)
            yield return $"JFormDB.solveStrSetter({key}, \"{path}\", \"{EscapeStr(sv)}\", true)";
    }

    // Papyrus string literal escaping — backslash then double-quote, and flatten newlines.
    private static string EscapeStr(string s) =>
        OneLine(s).Replace("\\", "\\\\").Replace("\"", "\\\"");
}
