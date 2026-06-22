namespace ModForge;

public static partial class Generator
{
    // -------------------------------------------------------------------------------
    //  PapyrusUtil StorageUtil per-form KV GENERATION (J組 — script-template snippets).
    //
    //  A `storageWrites` block writes lightweight per-Form scalar state via PapyrusUtil's StorageUtil
    //  (SetIntValue/AdjustIntValue/…). Unlike JContainers `persist` (nested JFormDB paths, see
    //  Generator.JContainers.cs), StorageUtil is flat string-keyed KV that the save manages automatically —
    //  the "simple + auto-managed" half of the survey's choose-guide. The eval flagged it as the highest-
    //  leverage, most固定 generation point: follower memory, interaction cooldowns, per-NPC flags.
    //
    //  It rides the SAME two host fragments as persist — a dialogue line's TIF result fragment
    //  (Fragment_0(akSpeakerRef)) and a quest STAGE fragment (Fragment_Stage_XXXX, extends Quest, NO
    //  akSpeakerRef). The emitter is body-only for the three EXPRESSION targets (the dialogue speaker, the
    //  player, or None for a global KV) — those bind no VMAD property. An arbitrary-ref target (any other
    //  token) binds as a Form property, exactly like persist's arbitrary-ref key: declared in the script,
    //  bound to the ref's FormKey in the VMAD (Build.Scripts / Build.QuestStages), namespaced by the same
    //  per-stage prefix so several stages never collide. This is the per-NPC / per-object state half:
    //  "remember this fact ON this specific actor/container".
    //
    //  A write's VALUE is normally the int/float/str literal. With an optional `fromJson` source the value
    //  is instead read at runtime from a PapyrusUtil JsonUtil file — JsonUtil.Get{Int,Float,String}Value(
    //  file, key, <literal as the missing default>). That is the "read external config" capability: load a
    //  player-editable / tool-written JSON value into per-Form (or global) StorageUtil state at a dialogue
    //  line / quest stage. The literal you'd otherwise write becomes the fallback when the key is absent.
    //
    //  Compiling the generated .psc needs PapyrusUtil's own .psc on the header path (main machine; like
    //  JContainers — see WAIT_USER); the emission itself is a pure string function, unit-testable with no
    //  Skyrim master / Wine.
    // -------------------------------------------------------------------------------

    /// <summary>How a storageWrites Form key resolves: the dialogue speaker (akSpeakerRef — only a TIF
    /// fragment has it), the player, None (a process-global KV not hung on any Form), or an arbitrary ref
    /// bound as a Form property (any other token — e.g. a placed-ref EDID or a <c>Master:0xFORMID</c>).</summary>
    internal enum StorageTargetKind { Speaker, Player, None, Ref }

    /// <summary>Classify a storageWrites target string. "" / "speaker" → Speaker; "player" → Player;
    /// "none" / "global" → None; anything else → an arbitrary Ref (resolved to a Form, bound as a property).</summary>
    internal static StorageTargetKind ClassifyStorageTarget(string target)
    {
        var k = (target ?? "").Trim();
        if (string.Equals(k, "player", System.StringComparison.OrdinalIgnoreCase)) return StorageTargetKind.Player;
        if (string.Equals(k, "none", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(k, "global", System.StringComparison.OrdinalIgnoreCase)) return StorageTargetKind.None;
        if (k.Length == 0 || string.Equals(k, "speaker", System.StringComparison.OrdinalIgnoreCase)) return StorageTargetKind.Speaker;
        return StorageTargetKind.Ref;
    }

    /// <summary>The Papyrus expression for a storageWrites target. speaker → akSpeakerRef (TIF only),
    /// player → Game.GetPlayer(), none → None (all pure expressions); an arbitrary ref → <paramref
    /// name="refPropertyName"/> (the bound Form property).</summary>
    internal static string StorageTargetExpr(string target, string refPropertyName) =>
        ClassifyStorageTarget(target) switch
        {
            StorageTargetKind.Player => "Game.GetPlayer()",
            StorageTargetKind.None => "None",
            StorageTargetKind.Ref => refPropertyName,
            _ => "akSpeakerRef",
        };

    /// <summary>Property name (namespaced by <paramref name="prefix"/>) holding the arbitrary-ref Form
    /// target of storageWrites entry <paramref name="i"/>. The index is the entry's position in the block's
    /// list — declaration, binding and body must all use the same index.</summary>
    internal static string StorageRefProperty(string prefix, int i) => $"{prefix}SWRef_{i}";

    internal static bool HasStorageWrites(List<StorageWriteSpec>? w) => w is { Count: > 0 };
    internal static bool HasStorageWrites(DialogueSpec d) => HasStorageWrites(d.StorageWrites);
    internal static bool HasStorageWrites(StageSpec s) => HasStorageWrites(s.StorageWrites);

    /// <summary>The arbitrary-ref entries of a storageWrites block (list index → entry). Only these bind a
    /// Form property; speaker/player/none targets are pure expressions and declare nothing.</summary>
    internal static IEnumerable<(int Index, StorageWriteSpec Spec)> StorageRefEntries(IEnumerable<StorageWriteSpec>? writes) =>
        (writes ?? System.Array.Empty<StorageWriteSpec>())
            .Select((w, i) => (i, w))
            .Where(t => ClassifyStorageTarget(t.w.Target) == StorageTargetKind.Ref);

    /// <summary>The Papyrus property DECLARATIONS a storageWrites block needs under <paramref name="prefix"/>
    /// — one <c>Form Property</c> per arbitrary-ref target. Speaker/player/none targets declare nothing.</summary>
    internal static IEnumerable<string> StorageWritesPropertyDecls(string prefix, IEnumerable<StorageWriteSpec>? writes)
    {
        foreach (var (i, _) in StorageRefEntries(writes))
            yield return $"Form Property {StorageRefProperty(prefix, i)} Auto";
    }

    /// <summary>The fragment body lines (unindented; the caller indents) for a storageWrites block under
    /// <paramref name="prefix"/> — one StorageUtil call per entry. `delta` uses Adjust{Int,Float}Value
    /// (read-add-write in one native call); a plain set uses Set{Int,Float,String}Value. The written VALUE
    /// is the int/float/str literal, OR — when `fromJson` is set — a JsonUtil.Get{T}Value read with the
    /// literal as the missing default. String writes have no delta. Empty when the block carries nothing.</summary>
    internal static IEnumerable<string> StorageWritesBody(IEnumerable<StorageWriteSpec>? writes, string prefix = "")
    {
        if (writes is null) yield break;
        int i = -1;
        foreach (var w in writes)
        {
            i++;
            var expr = StorageTargetExpr(w.Target, StorageRefProperty(prefix, i));
            var key = EscapeStr(w.Key);
            if (w.Int is int iv)
            {
                var val = StorageJsonOrLiteral(w, "Int", iv.ToString(System.Globalization.CultureInfo.InvariantCulture));
                yield return w.Delta
                    ? $"StorageUtil.AdjustIntValue({expr}, \"{key}\", {val})"
                    : $"StorageUtil.SetIntValue({expr}, \"{key}\", {val})";
            }
            else if (w.Float is float fv)
            {
                var val = StorageJsonOrLiteral(w, "Float", PapyrusFloat(fv));
                yield return w.Delta
                    ? $"StorageUtil.AdjustFloatValue({expr}, \"{key}\", {val})"
                    : $"StorageUtil.SetFloatValue({expr}, \"{key}\", {val})";
            }
            else if (w.Str is { } sv)   // string set (may be the empty string); no delta form
            {
                var val = StorageJsonOrLiteral(w, "String", $"\"{EscapeStr(sv)}\"");
                yield return $"StorageUtil.SetStringValue({expr}, \"{key}\", {val})";
            }
        }
    }

    /// <summary>The value expression for a storageWrites entry: the literal (<paramref name="literal"/>,
    /// already Papyrus-formatted), or — when `fromJson` is set — a JsonUtil read of the given scalar type
    /// using that literal as the missing default. <paramref name="t"/> is "Int" / "Float" / "String".</summary>
    private static string StorageJsonOrLiteral(StorageWriteSpec w, string t, string literal) =>
        w.FromJson is { } j
            ? $"JsonUtil.Get{t}Value(\"{EscapeStr(j.File)}\", \"{EscapeStr(j.Key)}\", {literal})"
            : literal;
}
