namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 2: quest stage LOG-ENTRY conditions (CTDA on QLOG) + the stage→objective fragment ---
        // script attach. Stage/objective record data is built in pass 1 (BuildNpcsAndQuests); the
        // log-entry conditions need every record's editorId resolved, so they wire here via the SHARED
        // BuildCondition (e.g. GetStage with the quest as its param). A stage with one log entry attaches
        // all its conditions to that entry — they AND together to gate whether it applies.
        public void WireQuestStages()
        {
            foreach (var q in spec.Quests)
            {
                if (!questsByEd.TryGetValue(q.EditorId, out var qr)) continue;
                foreach (var st in q.Stages)
                {
                    if (st.Conditions.Count == 0) continue;
                    var stage = qr.Stages.FirstOrDefault(s => s.Index == st.Index);
                    var le = stage?.LogEntries.FirstOrDefault();
                    if (le is null) { Warn($"  ! quest '{q.EditorId}' stage {st.Index} has conditions but no log entry to attach them to"); continue; }
                    foreach (var cs in st.Conditions)
                        if (BuildCondition(cs, $"quest '{q.EditorId}' stage {st.Index} condition") is { } cond)
                            le.Conditions.Add(cond);
                }
            }

            // Quest stage→objective fragment script: when a quest has objectives linked to stages,
            // attach its generated fragment script (<quest>_Stages) to the QUST via the QuestAdapter
            // VMAD so the record references the script the CK will bind stage fragments to. The .psc
            // source is emitted by `package` (QuestFragmentScriptName / GenerateQuestFragmentSource).
            foreach (var q in spec.Quests)
            {
                var scriptName = QuestFragmentScriptName(q);
                if (string.IsNullOrEmpty(scriptName)) continue;
                if (!questsByEd.TryGetValue(q.EditorId, out var qr)) continue;
                qr.VirtualMachineAdapter ??= new QuestAdapter();
                // avoid a duplicate if the author also hand-attached the same script
                if (!qr.VirtualMachineAdapter.Scripts.Any(s => string.Equals(s.Name, scriptName, StringComparison.OrdinalIgnoreCase)))
                {
                    qr.VirtualMachineAdapter.Scripts.Add(new ScriptEntry { Name = scriptName });
                    scriptsAttached++;
                }
            }
        }
    }
}
