namespace ModForge;

public static partial class Generator
{
    private sealed partial class ValidateContext
    {
        // Shared CTDA validation for scene/phase conditions (mirrors the stage-condition checks).
        private void ValidateSceneCondition(ConditionSpec cs, string label)
        {
            if (string.IsNullOrWhiteSpace(cs.Function))
                Problems.Add($"{label} has empty function");
            else if (!Enum.TryParse<Condition.Function>(cs.Function, true, out _))
                Problems.Add($"{label} invalid function '{cs.Function}'");
            if (!string.IsNullOrWhiteSpace(cs.Comparison)
                && cs.Comparison is not ("==" or "=" or "!=" or ">" or ">=" or "<" or "<=")
                && !Enum.TryParse<CompareOperator>(cs.Comparison, true, out _))
                Problems.Add($"{label} invalid comparison '{cs.Comparison}'");
            CheckRef(cs.Param, $"{label} param");
            if (string.Equals(cs.Function, "IsSceneActionComplete", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(cs.Scene)) CheckRef(cs.Scene, $"{label} scene");
                if (cs.SceneActionIndex < 0) Problems.Add($"{label}: IsSceneActionComplete needs a sceneActionIndex (>= 0)");
            }
        }

        // Shared persist-block validation (dialogue line + quest stage). `allowSpeaker` is false on a
        // quest stage, which has no akSpeakerRef to key on.
        private void ValidatePersistBlock(PersistSpec p, string label, bool allowSpeaker)
        {
            if (string.IsNullOrWhiteSpace(p.Storage)) Problems.Add($"{label} has empty storage");
            ValidatePersistKey(p.Key, $"{label} key", allowSpeaker);
            if (p.Set.Count == 0) Problems.Add($"{label} has no set entries");
            foreach (var e in p.Set)
            {
                if (string.IsNullOrWhiteSpace(e.Path)) Problems.Add($"{label} entry has empty path");
                int vals = (e.Int is not null ? 1 : 0) + (e.Float is not null ? 1 : 0)
                         + (e.Str is not null ? 1 : 0) + (!string.IsNullOrWhiteSpace(e.Form) ? 1 : 0);
                if (vals != 1) Problems.Add($"{label} entry '{e.Path}' must set exactly one of int/float/str/form (got {vals})");
                if (e.Delta && e.Int is null && e.Float is null) Problems.Add($"{label} entry '{e.Path}' delta only applies to int/float");
                if (!string.IsNullOrWhiteSpace(e.Form)) CheckRef(e.Form, $"{label} entry '{e.Path}' form");
            }
            ValidateGate(p.Gate, $"{label} gate");
        }

        private void ValidateSyncPerksBlock(SyncPerksSpec s, string label, bool allowSpeaker)
        {
            if (string.IsNullOrWhiteSpace(s.Storage)) Problems.Add($"{label} has empty storage");
            ValidatePersistKey(s.Key, $"{label} key", allowSpeaker);
            if (s.Nodes.Count == 0) Problems.Add($"{label} has no nodes");
            foreach (var n in s.Nodes)
            {
                if (string.IsNullOrWhiteSpace(n.Path)) Problems.Add($"{label} node has empty path");
                if (string.IsNullOrWhiteSpace(n.Perk)) Problems.Add($"{label} node '{n.Path}' has empty perk ref");
                else CheckRef(n.Perk, $"{label} node '{n.Path}' perk");
            }
            ValidateGate(s.Gate, $"{label} gate");
        }

        // The affinity gate (Sofia F6 blueprint): the GLOB must resolve, and a band must not be inverted.
        private void ValidateGate(GateSpec? g, string label)
        {
            if (g is null) return;
            if (string.IsNullOrWhiteSpace(g.Global)) Problems.Add($"{label} has empty global");
            else CheckRef(g.Global, $"{label} global");
            if (g.AtLeast is float lo && g.AtMost is float hi && lo > hi)
                Problems.Add($"{label} atLeast ({lo}) > atMost ({hi}) — band never satisfiable");
        }

        // A persist/syncPerks Form key is "player", "speaker" (dialogue only — the emitter maps it to the
        // fragment's akSpeakerRef), or an arbitrary resolvable ref (bound as a Form property). Empty
        // defaults to "speaker". On a quest stage (allowSpeaker=false) "speaker" is an error.
        private void ValidatePersistKey(string key, string label, bool allowSpeaker)
        {
            switch (Generator.ClassifyPersistKey(key))
            {
                case Generator.PersistKeyKind.Speaker:
                    if (!allowSpeaker)
                        Problems.Add($"{label} 'speaker' is only valid on a dialogue line (a quest stage has no speaker — use 'player' or a ref)");
                    break;
                case Generator.PersistKeyKind.Player:
                    break;
                default:
                    CheckRef(key, label);   // arbitrary ref → must resolve
                    break;
            }
        }

        // J組 PapyrusUtil StorageUtil per-Form KV writes (dialogue line + quest stage). Each write needs a
        // non-empty key, exactly one of int/float/str, delta only on int/float, and a recognised target. A
        // quest stage has no akSpeakerRef, so target "speaker" (the default) is rejected there (allowSpeaker
        // is false) — use "player" or "none"/"global".
        private void ValidateStorageWrites(List<StorageWriteSpec> writes, string label, bool allowSpeaker)
        {
            foreach (var w in writes)
            {
                if (string.IsNullOrWhiteSpace(w.Key)) Problems.Add($"{label} has empty key");
                int vals = (w.Int is not null ? 1 : 0) + (w.Float is not null ? 1 : 0) + (w.Str is not null ? 1 : 0);
                if (vals != 1) Problems.Add($"{label} '{w.Key}' must set exactly one of int/float/str (got {vals})");
                if (w.Delta && w.Int is null && w.Float is null) Problems.Add($"{label} '{w.Key}' delta only applies to int/float");
                if (!Generator.StorageTargetTokens.Contains((w.Target ?? "").Trim()))
                    Problems.Add($"{label} '{w.Key}' bad target '{w.Target}' (use speaker | player | none/global)");
                else if (!allowSpeaker && Generator.ClassifyStorageTarget(w.Target ?? "") == Generator.StorageTargetKind.Speaker)
                    Problems.Add($"{label} '{w.Key}' target 'speaker' is only valid on a dialogue line (a quest stage has no speaker — use 'player' or 'none')");
            }
        }

        private void ValidateScriptAttachments()
        {
            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "int", "float", "bool", "string", "object" };
            foreach (var sa in spec.Scripts)
            {
                if (string.IsNullOrEmpty(sa.ScriptName)) Problems.Add($"script attach on '{sa.TargetEditorId}' has empty scriptName");
                if (!Ids.Contains(sa.TargetEditorId)) Problems.Add($"script '{sa.ScriptName}' targets unknown record '{sa.TargetEditorId}'");
                CheckScriptProps(sa.ScriptName, sa.Properties, validTypes);
            }
            // Inline MGEF scripts (I組): target is implied (the effect record), so only scriptName + props.
            foreach (var me in spec.MagicEffects)
                foreach (var sa in me.Scripts)
                {
                    if (string.IsNullOrEmpty(sa.ScriptName)) Problems.Add($"magicEffect '{me.EditorId}' inline script has empty scriptName");
                    CheckScriptProps(sa.ScriptName, sa.Properties, validTypes);
                }
        }

        void CheckScriptProps(string scriptName, List<PropertySpec> props, HashSet<string> validTypes)
        {
            foreach (var p in props)
            {
                if (!validTypes.Contains(p.Type)) Problems.Add($"script '{scriptName}' prop '{p.Name}' has invalid type '{p.Type}'");
                if (string.Equals(p.Type, "object", StringComparison.OrdinalIgnoreCase))
                    CheckRef(p.ObjectEditorId, $"script '{scriptName}' prop '{p.Name}' object");
            }
        }
    }
}
