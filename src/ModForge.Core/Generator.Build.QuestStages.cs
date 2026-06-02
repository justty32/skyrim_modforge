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

            // Quest stage→objective fragment script: we generate the .psc SOURCE for CK compilation
            // (emitted by `package`), but do NOT attach the QuestAdapter VMAD to the record until a
            // compiled .pex exists. Reason: a VMAD referencing an absent .pex triggers a Papyrus error
            // at quest-start that prevents the quest from properly initialising its journal state —
            // setstage fires the stage flags (CompleteQuest popup) but the journal never updates. The
            // correct workflow is: generate → package → open in CK → compile scripts → save; the CK
            // then wires the VMAD and binds the stage fragments. (WireQuestStages still runs for
            // log-entry CTDA conditions above; the script-attach block below is intentionally absent.)
        }
    }
}
