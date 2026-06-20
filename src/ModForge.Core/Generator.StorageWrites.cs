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
    //  akSpeakerRef). The emitter is body-only: every supported target (the dialogue speaker, the player,
    //  or None for a global KV) is a pure Papyrus EXPRESSION, so storageWrites binds NO VMAD property and
    //  slots into the existing fragment machinery with no binding-site changes. An arbitrary-ref target
    //  (a bound Form property) is deliberately deferred — speaker/player/none cover the dominant cases.
    //
    //  Compiling the generated .psc needs PapyrusUtil's own .psc on the header path (main machine; like
    //  JContainers — see WAIT_USER); the emission itself is a pure string function, unit-testable with no
    //  Skyrim master / Wine.
    // -------------------------------------------------------------------------------

    /// <summary>How a storageWrites Form key resolves: the dialogue speaker (akSpeakerRef — only a TIF
    /// fragment has it), the player, or None (a process-global KV not hung on any Form).</summary>
    internal enum StorageTargetKind { Speaker, Player, None }

    /// <summary>Classify a storageWrites target string. "" / "speaker" → Speaker; "player" → Player;
    /// "none" / "global" → None. (Validation rejects any other token; this defaults unknown → Speaker.)</summary>
    internal static StorageTargetKind ClassifyStorageTarget(string target)
    {
        var k = (target ?? "").Trim();
        if (string.Equals(k, "player", System.StringComparison.OrdinalIgnoreCase)) return StorageTargetKind.Player;
        if (string.Equals(k, "none", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(k, "global", System.StringComparison.OrdinalIgnoreCase)) return StorageTargetKind.None;
        return StorageTargetKind.Speaker;
    }

    /// <summary>The Papyrus expression for a storageWrites target — all three are pure expressions (no
    /// bound property): speaker → akSpeakerRef (TIF only), player → Game.GetPlayer(), none → None.</summary>
    internal static string StorageTargetExpr(string target) =>
        ClassifyStorageTarget(target) switch
        {
            StorageTargetKind.Player => "Game.GetPlayer()",
            StorageTargetKind.None => "None",
            _ => "akSpeakerRef",
        };

    /// <summary>The recognised storageWrites target tokens (validation checks against this).</summary>
    internal static readonly HashSet<string> StorageTargetTokens =
        new(StringComparer.OrdinalIgnoreCase) { "", "speaker", "player", "none", "global" };

    internal static bool HasStorageWrites(List<StorageWriteSpec>? w) => w is { Count: > 0 };
    internal static bool HasStorageWrites(DialogueSpec d) => HasStorageWrites(d.StorageWrites);
    internal static bool HasStorageWrites(StageSpec s) => HasStorageWrites(s.StorageWrites);

    /// <summary>The fragment body lines (unindented; the caller indents) for a storageWrites block —
    /// one StorageUtil call per entry. `delta` uses Adjust{Int,Float}Value (read-add-write in one native
    /// call); a plain set uses Set{Int,Float,String}Value. String writes have no delta. Empty when the
    /// block carries nothing.</summary>
    internal static IEnumerable<string> StorageWritesBody(IEnumerable<StorageWriteSpec>? writes)
    {
        if (writes is null) yield break;
        foreach (var w in writes)
        {
            var expr = StorageTargetExpr(w.Target);
            var key = EscapeStr(w.Key);
            if (w.Int is int iv)
                yield return w.Delta
                    ? $"StorageUtil.AdjustIntValue({expr}, \"{key}\", {iv})"
                    : $"StorageUtil.SetIntValue({expr}, \"{key}\", {iv})";
            else if (w.Float is float fv)
                yield return w.Delta
                    ? $"StorageUtil.AdjustFloatValue({expr}, \"{key}\", {PapyrusFloat(fv)})"
                    : $"StorageUtil.SetFloatValue({expr}, \"{key}\", {PapyrusFloat(fv)})";
            else if (w.Str is { } sv)   // string set (may be the empty string); no delta form
                yield return $"StorageUtil.SetStringValue({expr}, \"{key}\", \"{EscapeStr(sv)}\")";
        }
    }
}
