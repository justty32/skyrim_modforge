namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 2: quest stage LOG-ENTRY conditions (CTDA on QLOG) + the stage→objective fragment ---
        // script attach. Stage/objective record data is built in pass 1 (BuildQuests); the
        // log-entry conditions need every record's editorId resolved, so they wire here via the SHARED
        // BuildCondition (e.g. GetStage with the quest as its param). A stage with one log entry attaches
        // all its conditions to that entry — they AND together to gate whether it applies.
        public void WireQuestStages()
        {
            foreach (var q in spec.Quests)
            {
                if (!questsByEd.TryGetValue(q.EditorId, out var qr)) continue;
                var aliasIdx = qr.Aliases.ToDictionary(a => a.Name ?? "", a => (int)a.ID, StringComparer.OrdinalIgnoreCase);

                // Pass 1: wire log-entry CTDA conditions (need all record editorIds resolved).
                foreach (var st in q.Stages)
                {
                    if (st.Conditions.Count == 0) continue;
                    var stage = qr.Stages.FirstOrDefault(s => s.Index == st.Index);
                    var le = stage?.LogEntries.FirstOrDefault();
                    if (le is null) { Warn($"  ! quest '{q.EditorId}' stage {st.Index} has conditions but no log entry to attach them to"); continue; }
                    foreach (var cs in st.Conditions)
                        if (BuildCondition(cs, $"quest '{q.EditorId}' stage {st.Index} condition", aliasIdx) is { } cond)
                            le.Conditions.Add(cond);
                }

                // Pass 2: attach QuestAdapter VMAD with stage-fragment bindings when a compiled .pex
                // exists in options.CompiledScriptsDir. A VMAD referencing an absent .pex would trigger
                // a Papyrus error at quest-start that blocks journal registration, so we ONLY attach when
                // the .pex is confirmed present. The `package` command pre-compiles the generated .psc
                // and then calls Build() with CompiledScriptsDir set, enabling this block.
                if (options?.CompiledScriptsDir is not { } compiledDir) continue;
                var scriptName = Generator.QuestFragmentScriptName(q);
                if (string.IsNullOrEmpty(scriptName)) continue;
                if (!File.Exists(Path.Combine(compiledDir, scriptName + ".pex"))) continue;

                // MERGE into any QuestAdapter already attached (e.g. by BuildStoryManager's
                // AttachAliasScript, which stores alias scripts in qa.Aliases). Overwriting with a fresh
                // adapter here would drop those alias fragments. A vanilla QuestAdapter holds BOTH a
                // quest-level script (FileName + Scripts + stage Fragments) AND per-alias fragments, so
                // the two coexist on one adapter (Version=5/ObjectFormat=2 = vanilla canonical).
                var qa = qr.VirtualMachineAdapter as QuestAdapter
                         ?? new QuestAdapter { Version = 5, ObjectFormat = 2 };
                qa.FileName = scriptName;
                var entry = qa.Scripts.FirstOrDefault(s => string.Equals(s.Name, scriptName, StringComparison.OrdinalIgnoreCase));
                if (entry is null) { entry = new ScriptEntry { Name = scriptName }; qa.Scripts.Add(entry); }

                // Bind a GlobalVariable object-property for every distinct instance global the stages
                // reference (the fragment body calls <prop>.SetValue / UpdateCurrentInstanceGlobal(<prop>)).
                foreach (var gref in q.Stages.SelectMany(s => s.InstanceGlobals).Select(g => g.Global)
                             .Where(g => !string.IsNullOrWhiteSpace(g)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var pname = Generator.InstanceGlobalProperty(gref);
                    if (entry.Properties.Any(p => string.Equals(p.Name, pname, StringComparison.OrdinalIgnoreCase))) continue;
                    var prop = new ScriptObjectProperty { Name = pname, Flags = ScriptProperty.Flag.Edited };
                    if (TryResolveRef(gref, formKeyByEd, out var gfk)) prop.Object.SetTo(gfk);
                    else Warn($"  ! quest '{q.EditorId}' instanceGlobal '{gref}' unresolved");
                    entry.Properties.Add(prop);
                }

                // One QuestScriptFragment per stage that shows/completes an objective OR binds an
                // instance global. Stage = the quest stage number (uint16), StageIndex = log-entry index
                // within stage (always 0 — we emit one log entry per stage), FragmentName = CK-standard
                // function name the engine calls when SetStage() fires.
                foreach (var st in q.Stages.OrderBy(s => s.Index))
                {
                    // A fragment binding is needed when the stage shows/completes an objective, binds an
                    // instance global, OR is the startUpStage that drives the spawn/cooldown trigger.
                    // (Without the binding the engine never calls Fragment_Stage_XXXX even though the
                    // function exists in the .pex — the cause of "startquest spawns nothing".)
                    bool needsFrag = q.Objectives.Any(o => o.ShowStage == st.Index || o.CompleteStage == st.Index)
                                     || st.InstanceGlobals.Count > 0
                                     || Generator.StartupStageTrigger(q) == st.Index;
                    if (!needsFrag) continue;
                    // Stage = quest stage number, StageIndex = log-entry index within the stage
                    // (always 0 — we emit one log entry per stage, matching vanilla convention).
                    // Unknown2 = 1 in every vanilla fragment (confirmed vs MS08, MS13, MQ101 etc.).
                    // Setting it to 0 causes the engine to skip the fragment when SetStage() is
                    // called via Papyrus (though the console setstage is more lenient).
                    qa.Fragments.Add(new QuestScriptFragment
                    {
                        Stage = (ushort)st.Index,
                        Unknown = 0,
                        StageIndex = 0,
                        Unknown2 = 1,
                        ScriptName = scriptName,
                        FragmentName = $"Fragment_Stage_{st.Index:D4}_Item00000",
                    });
                }
                qr.VirtualMachineAdapter = qa;
                scriptsAttached++;
            }
        }
    }
}
