namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  JContainers JFormDB persistence GENERATION (Idea #20 in-world skill tree, Phase 0).
    //
    //  A dialogue line can carry `persist` (write nested per-Form state) and/or `syncPerks` (apply perks
    //  from stored ranks). We emit the Papyrus into the line's TIF result fragment (GenerateDialogue-
    //  FragmentSource) and bind any Form/Perk values as VMAD object properties — the same machinery as
    //  setStage / rewardItem (Generator.Build.Scripts.cs AttachDialogueResultScripts).
    //
    //  Lifecycle note (answers design unknown U5): we expose ONLY the root-DB path API
    //  (JFormDB.solveXxxSetter / solveXxx). JContainers owns those roots and persists them with the save,
    //  so there is NO JValue.object()/retain()/release() handle to balance — the retain/release footgun
    //  is avoided by construction. Compiling the generated .psc needs JContainers' own .psc on the header
    //  path (main machine; see WAIT_USER); these are pure functions (string in/out), unit-testable with
    //  no Skyrim master / Wine.
    // -------------------------------------------------------------------------------

    /// <summary>The Papyrus expression for a JFormDB Form key inside a dialogue TIF fragment.
    /// "player" → Game.GetPlayer(); anything else ("speaker", default) → the spoken-to NPC akSpeakerRef.
    /// Both are ObjectReference/Actor, which extend Form, so they pass straight to JFormDB.</summary>
    internal static string JFormDbKeyExpr(string key) =>
        string.Equals(key?.Trim(), "player", System.StringComparison.OrdinalIgnoreCase)
            ? "Game.GetPlayer()" : "akSpeakerRef";

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

    /// <summary>Property name on the TIF script holding the Form value of persist entry <paramref name="i"/>.</summary>
    internal static string PersistFormProperty(int i) => $"PF_{i}";
    /// <summary>Property name on the TIF script holding the PERK of syncPerks node <paramref name="i"/>.</summary>
    internal static string SyncPerkProperty(int i) => $"SyncPerk_{i}";

    /// <summary>The Form-valued persist entries of a block (declaration index → entry). Only these bind a
    /// Form property; int/float/string entries carry literals and declare nothing.</summary>
    internal static IEnumerable<(int Index, PersistEntrySpec Entry)> PersistFormEntries(PersistSpec? p) =>
        (p?.Set ?? new()).Select((e, i) => (i, e)).Where(t => !string.IsNullOrWhiteSpace(t.e.Form));

    /// <summary>True when a dialogue line carries any JFormDB persist write.</summary>
    internal static bool HasPersist(DialogueSpec d) => d.Persist is { } p && p.Set.Count > 0;
    /// <summary>True when a dialogue line carries any perk-sync node.</summary>
    internal static bool HasSyncPerks(DialogueSpec d) => d.SyncPerks is { } s && s.Nodes.Count > 0;

    /// <summary>The Papyrus property DECLARATIONS a line's persist/syncPerks need (Form/Perk Auto
    /// properties). Int/float/string persist values are literals and declare nothing.</summary>
    internal static IEnumerable<string> JContainersPropertyDecls(DialogueSpec d)
    {
        if (d.Persist is { } p)
            foreach (var (i, _) in PersistFormEntries(p))
                yield return $"Form Property {PersistFormProperty(i)} Auto";
        if (d.SyncPerks is { } s)
            for (int i = 0; i < s.Nodes.Count; i++)
                yield return $"Perk Property {SyncPerkProperty(i)} Auto";
    }

    /// <summary>The Fragment_0 body lines (unindented; the caller indents) for a line's persist writes,
    /// then its perk sync. Empty when the line carries neither.</summary>
    internal static IEnumerable<string> JContainersFragmentBody(DialogueSpec d)
    {
        if (d.Persist is { } p && p.Set.Count > 0)
        {
            var key = JFormDbKeyExpr(p.Key);
            for (int i = 0; i < p.Set.Count; i++)
                foreach (var line in EmitPersistEntry(key, JFormDbPath(p.Storage, p.Set[i].Path), p.Set[i], i))
                    yield return line;
        }
        if (d.SyncPerks is { } s && s.Nodes.Count > 0)
        {
            var key = JFormDbKeyExpr(s.Key);
            // GetPlayer()/akSpeakerRef are already Actor-or-subtype; cast once so AddPerk/RemovePerk resolve.
            yield return $"Actor __sp = {key} as Actor";
            yield return "If __sp";
            for (int i = 0; i < s.Nodes.Count; i++)
            {
                var n = s.Nodes[i];
                var path = JFormDbPath(s.Storage, n.Path);
                var prop = SyncPerkProperty(i);
                yield return $"    If JFormDB.solveInt({key}, \"{path}\", 0) >= {System.Math.Max(1, n.MinRank)}";
                yield return $"        __sp.AddPerk({prop})       ; rank >= {System.Math.Max(1, n.MinRank)}";
                yield return "    Else";
                yield return $"        __sp.RemovePerk({prop})";
                yield return "    EndIf";
            }
            yield return "EndIf";
        }
    }

    // One persist write → 1-2 Papyrus lines. `i` makes the read-add-write temp unique across entries.
    private static IEnumerable<string> EmitPersistEntry(string key, string path, PersistEntrySpec e, int i)
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
            yield return $"JFormDB.solveFormSetter({key}, \"{path}\", {PersistFormProperty(i)}, true)";
        else if (e.Str is { } sv)   // string set (may be the empty string)
            yield return $"JFormDB.solveStrSetter({key}, \"{path}\", \"{EscapeStr(sv)}\", true)";
    }

    // Papyrus string literal escaping — backslash then double-quote, and flatten newlines.
    private static string EscapeStr(string s) =>
        OneLine(s).Replace("\\", "\\\\").Replace("\"", "\\\"");
}
