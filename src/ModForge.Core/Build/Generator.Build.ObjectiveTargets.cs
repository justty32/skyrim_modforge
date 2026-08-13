using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
    {
        // --- pass 2: resolve forced alias fills whose target builds after the alias passes (an in-spec
        // placement / xmarker anchor or a map marker). Vanilla and already-built refs were set inline. ---
        public void WireDeferredForcedAliases()
        {
            foreach (var (alias, refStr) in deferredForcedAliases)
            {
                if (TryResolveRef(refStr, formKeyByEd, out var fk))
                    alias.ForcedReference.SetTo(fk);
                else
                    Warn($"  ! forced alias '{alias.Name}' ref '{refStr}' unresolved (no such in-spec editorId or vanilla form)");
            }
        }

        // --- pass 2: objective QSTA targets. Run after aliases are built (BuildStoryManager /
        // BuildStandaloneQuestAliases) so a name→alias-index map exists on the built quest. Each
        // ObjectiveTargetSpec becomes a QuestObjectiveTarget (AliasID + flag + CTDA) on its QOBJ. ---
        public void WireObjectiveTargets()
        {
            foreach (var qs in spec.Quests)
            {
                if (qs.Objectives.All(o => o.Targets.Count == 0)) continue;
                if (string.IsNullOrEmpty(qs.EditorId) || !questsByEd.TryGetValue(qs.EditorId, out var quest))
                    continue;
                var idByName = quest.Aliases.ToDictionary(a => a.Name ?? "", a => (int)a.ID, StringComparer.OrdinalIgnoreCase);

                foreach (var o in qs.Objectives.Where(o => o.Targets.Count > 0))
                {
                    var obj = quest.Objectives.FirstOrDefault(x => x.Index == o.Index);
                    if (obj is null) { Warn($"  ! quest '{qs.EditorId}' objective {o.Index} has targets but no built QOBJ — skipped"); continue; }
                    foreach (var ts in o.Targets)
                    {
                        if (!idByName.TryGetValue(ts.Alias, out var aliasId))
                        { Warn($"  ! quest '{qs.EditorId}' objective {o.Index} target alias '{ts.Alias}' not found — skipped"); continue; }
                        var t = new QuestObjectiveTarget
                        {
                            AliasID = aliasId,
                            Flags = ts.CompassIgnoresLocks ? Quest.TargetFlag.CompassMarkerIgnoresLocks : 0,
                        };
                        foreach (var c in ts.Conditions)
                            if (BuildCondition(c, $"quest '{qs.EditorId}' objective {o.Index} target", idByName) is { } cond)
                                t.Conditions.Add(cond);
                        obj.Targets.Add(t);
                    }
                }
            }
        }
    }
}
